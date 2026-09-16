"""The live instances of the 15 Patterns/ recipes. RECIPE_SIGNATURES is the source of truth for "recipe -> signature":
what each signature looks for in the graph, where its decision comes from, and — when it finds nothing — what it
did not find. references/recipe-signatures.md is its reading."""
from __future__ import annotations

from collections import defaultdict
from dataclasses import asdict, dataclass, field as dataclass_field
from typing import Callable

from ecs_facts import STATE_SUFFIX, bare_name
from type_facts import CONFIG_BASES
from view_pairs import UNMARKED_SUBSCRIPTION_WARNING

VIEW_BOUNDARY_TYPES = {"EntityStorages", "EntityStore"}
GENERIC_CONFIG_LOADER = "ConfigLoaderSystem"
PRESENTATION_LAYER = "Assets/Presentation/"


@dataclass
class Signature:
    finder: Callable
    decided_by: str
    looks_for: str


@dataclass
class RecipeResult:
    instances: list = dataclass_field(default_factory=list)       # [{id, source_location, decided_by, …}]
    groups: dict = dataclass_field(default_factory=dict)          # group name -> ids
    deviations: list = dataclass_field(default_factory=list)
    decided_by: str = ""
    empty_reason: str | None = None


RECIPE_SIGNATURES = {
    "PATTERN_COMPONENT": Signature(lambda s, t, d: instances_of_kind(d, "component"), "kind",
                                   "declared struct of kind component (IComponent or …Component, not an event)"),
    "PATTERN_TAG": Signature(lambda s, t, d: find_tags(t, d), "kind", "declared struct : ITag"),
    "PATTERN_EVENT": Signature(lambda s, t, d: instances_of_kind(d, "event"), "kind",
                               "declared struct named …Event or …EventComponent"),
    "PATTERN_CONFIG": Signature(lambda s, t, d: find_configs(t, d), "base",
                                "non-abstract class whose ancestry reaches ScriptableObject"),
    "PATTERN_CONFIG_LOADER": Signature(lambda s, t, d: find_config_loaders(s, t, d), "di",
                                       "registration with AppState.ConfigLoading whose impl is exposed As IUniTaskSystem"),
    "PATTERN_PIPELINE_STAGE": Signature(lambda s, t, d: instances_with_role(d, "pipeline_stage"), "base",
                                        "class with role pipeline_stage (IPrioritizedUniTaskSystem<MapGenerationStep>)"),
    "PATTERN_ORCHESTRATOR_SUBSYSTEM": Signature(lambda s, t, d: find_orchestrator_families(t, d), "lexical",
                                                "abstract class collected by a constructor collection (hosts edge)"),
    "PATTERN_REACTIVE_SYSTEM": Signature(lambda s, t, d: instances_with_role(d, "reactive"), "base | marker",
                                         "class with role reactive"),
    "PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM": Signature(lambda s, t, d: find_reactive_orchestrators(d), "lexical",
                                                     "class with role reactive and an outgoing hosts edge"),
    "PATTERN_PERFRAME_SYSTEM": Signature(lambda s, t, d: instances_with_role(d, "per_frame"), "base | marker",
                                         "class with role per_frame"),
    "PATTERN_CLEANUP_SYSTEM": Signature(lambda s, t, d: instances_with_role(d, "cleanup"), "lexical",
                                        "class with role cleanup (EventTag sweep + DeleteEntity)"),
    "PATTERN_VIEW_SYSTEM": Signature(lambda s, t, d: find_view_subscribers(s, d), "marker",
                                     "subscribes edge from a [ViewSubscriber] class to its view"),
    "PATTERN_TRANSACTION_ENTITY": Signature(lambda s, t, d: find_transaction_entities(t, d), "label-tag",
                                            "archetype carrying a label tag of role transaction"),
    "PATTERN_POLYMORPHIC_CATALOGUE": Signature(lambda s, t, d: find_polymorphic_catalogues(s, t, d), "lexical",
                                               "abstract SO base B + SO container with [SerializeField] B[] + a "
                                               "…KindComponent named after B carried by an archetype"),
    "ADDRESSABLE_PATTERNS": Signature(lambda s, t, d: find_addressable_injections(d), "lexical",
                                      "constructor parameter of type IAddressable (injects via ctor)"),
}
RECIPE_NAMES = list(RECIPE_SIGNATURES)


def find_recipe_instances(sources, types, draft):
    for name, signature in RECIPE_SIGNATURES.items():
        result = signature.finder(sources, types, draft)
        result.decided_by = signature.decided_by
        if not result.instances:
            result.empty_reason = f"no {signature.looks_for} found"
        draft.recipe_instances[name] = asdict(result)


def instances_of_kind(draft, kind: str) -> RecipeResult:
    return RecipeResult([instance(i, n, "kind") for i, n in sorted(draft.nodes.items())
                         if n.get("kind") == kind and n.get("declared")])


def instances_with_role(draft, role: str) -> RecipeResult:
    result = RecipeResult([instance(i, n, n.get("decided_by")) for i, n in sorted(draft.nodes.items())
                           if n.get("role") == role and n.get("kind") == "system"])
    if role == "per_frame":
        result.deviations = [f"marker needed: {i} — role undecided @ {n.get('source_location')} "
                             f"[rule system/marker-required]"
                             for i, n in sorted(draft.nodes.items()) if n.get("role") == "undecided"]
    return result


def instance(node_id: str, node: dict, decided_by, **facets) -> dict:
    return {"id": node_id, "source_location": node.get("source_location", ""), "decided_by": decided_by, **facets}


def deviate(result: RecipeResult, draft, message: str, rule: str):
    """A deviation of a recipe instance is a violation like any other: it is named in `pattern` AND warned in
    `check`, and it carries the id of the rule it breaks."""
    result.deviations.append(f"{message} [rule {rule}]")
    draft.warn(message, rule)


def find_tags(types, draft) -> RecipeResult:
    result = RecipeResult()
    for tag_id, node in sorted(draft.nodes.items()):
        if node.get("kind") != "tag" or not node.get("declared"):
            continue
        marked = types.markers.get(tag_id)
        label = marked.label if marked else None
        result.instances.append(instance(tag_id, node, "kind", tag="label" if label else "main", label_role=label))
        result.groups.setdefault("label" if label else "main", []).append(tag_id)
    result.deviations = [f"{d['kind']}: {d['archetype_or_owner']} [{', '.join(d['tags'])}] @ {d['source_location']} "
                         f"[rule {d['rule']}]" for d in draft.tag_audit]
    return result


def find_configs(types, draft) -> RecipeResult:
    result = instances_of_kind(draft, "config")
    result.groups["abstract_bases"] = sorted(
        i for i, n in draft.nodes.items() if n.get("declared") and n.get("abstract") and n.get("kind") == "other"
        and {a.node for a in types.ancestry.get(i, [])} & CONFIG_BASES)
    return result


def find_config_loaders(sources, types, draft) -> RecipeResult:
    exposed_as_step = {(e.src, e.source_location) for e in draft.edges
                       if e.rel == "exposes" and e.dst == "IUniTaskSystem" and not e.args}
    result = RecipeResult()
    for edge in (e for e in draft.edges if e.rel == "registers"):
        if edge.app_state == "ConfigLoading" and (edge.dst, edge.source_location) in exposed_as_step:
            result.instances.append({"id": edge.dst, "args": list(edge.args), "source_location": edge.source_location,
                                     "decided_by": "di"})
        elif edge.app_state == "InstanceObjects":
            result.groups.setdefault("instance_objects", []).append(edge.dst)

    # the generic loader is the only one: never a hand-written loader, never a descendant per config, never a config
    # as a component, never an entity table to query configs from
    for declaration in (d for d in sources.declarations if d["kind"] == "class"):
        name, node_id = declaration["name"], declaration["id"]
        ancestors = {a.node for a in types.ancestry.get(node_id, [])}
        if "Config" in name and name.endswith(("Loader", "LoaderSystem")) and name != GENERIC_CONFIG_LOADER:
            deviate(result, draft, f"hand-written config loader {node_id} @ {declaration['source_location']}",
                    "config/loader")
        if GENERIC_CONFIG_LOADER in ancestors:
            deviate(result, draft, f"{node_id} @ {declaration['source_location']} descends from "
                                   f"{GENERIC_CONFIG_LOADER} — the loader is used closed over its config, never "
                                   f"inherited per config", "config/loader")
    for node_id, node in sorted(draft.nodes.items()):
        if node.get("kind") == "component" and node["name"].endswith("ConfigComponent"):
            deviate(result, draft, f"{node_id} carries authored data as a component", "config/loader")
    carried = {e.dst for e in draft.edges if e.rel == "has"}
    for node_id in sorted(carried):
        if draft.nodes.get(node_id, {}).get("name", "").endswith("Config"):
            deviate(result, draft, f"archetype column {node_id} makes an entity table of authored configs",
                    "config/loader")
    result.deviations = sorted(set(result.deviations))
    return result


def find_orchestrator_families(types, draft) -> RecipeResult:
    hosts = [e for e in draft.edges if e.rel == "hosts"]
    contracts = sorted({e.dst for e in hosts})
    members = [(i, n) for i, n in sorted(draft.nodes.items())
               if n.get("declared") and not n.get("abstract")
               and any(a.node in contracts for a in types.ancestry.get(i, []))]
    return RecipeResult([instance(i, n, "lexical") for i, n in members],
                        {"contracts": contracts, "hosts": sorted({e.src for e in hosts})})


def find_reactive_orchestrators(draft) -> RecipeResult:
    hosting = {e.src for e in draft.edges if e.rel == "hosts"}
    return RecipeResult([instance(i, n, n.get("decided_by")) for i, n in sorted(draft.nodes.items())
                         if n.get("role") == "reactive" and i in hosting])


def find_view_subscribers(sources, draft) -> RecipeResult:
    subscriptions = sorted((e for e in draft.edges if e.rel == "subscribes"), key=lambda e: (e.src, e.via))
    result = RecipeResult([instance(i, draft.nodes[i], "marker") for i in sorted({e.src for e in subscriptions})],
                          {"views": sorted({e.dst for e in subscriptions}),
                           "subscriptions": [f"{e.src} -> {e.dst} via {e.via}" for e in subscriptions]})
    result.deviations = [w for w in draft.warnings if w.startswith(UNMARKED_SUBSCRIPTION_WARNING)]

    # view-boundary: a view raises a C# event for its system — it never creates an entity or an event itself, and
    # never receives the store. The population is the view LAYER: everything declared in a Views/ folder.
    views = {i for i, n in draft.nodes.items() if n.get("view_layer")}
    for site in (s for s in sources.ecs_sites if s["owner"] in views):
        if site["site"] == "create_event":
            deviate(result, draft, f"view-boundary: {site['owner']} raises {site['type']} "
                                   f"@ {site['source_location']}", "view/boundary")
        elif site["site"] == "create_entity":
            deviate(result, draft, f"view-boundary: {site['owner']} calls {site['via']} "
                                   f"@ {site['source_location']}", "view/boundary")
    for declaration in (d for d in sources.declarations if d["id"] in views):
        received = [(f["name"], f["type"], f["source_location"]) for f in declaration["fields"]]
        received += [(p["name"], p["type"], p["source_location"])
                     for parameters in declaration["constructors"] + declaration["inject_methods"]
                     + declaration["construct_methods"] for p in parameters]
        for name, type_use, location in received:
            if type_use.name in VIEW_BOUNDARY_TYPES:
                deviate(result, draft, f"view-boundary: {declaration['id']} receives {type_use.name} {name} "
                                       f"@ {location}", "view/boundary")
    result.deviations = sorted(set(result.deviations))
    return result


def find_transaction_entities(types, draft) -> RecipeResult:
    transaction_labels = {i for i, m in types.markers.items() if m.label == "transaction"}
    result = RecipeResult([instance(i, n, "label-tag", label_tags=n.get("label_tags", []))
                           for i, n in sorted(draft.nodes.items())
                           if n.get("kind") == "archetype" and transaction_labels & set(n.get("label_tags", []))])
    for transaction in result.instances:
        check_transaction_home(transaction["id"], result, draft)
        check_transaction_archetype(transaction["id"], result, draft)
    result.deviations = sorted(set(result.deviations))
    return result


def home_of(source_location: str) -> str:
    """The home of a declaration: the layer and, inside the domain layer, the domain — `Assets/Domains/Actions/`."""
    parts = source_location.split("/")
    return "/".join(parts[:3]) + "/" if len(parts) > 3 else "/".join(parts[:2]) + "/"


def check_transaction_home(archetype_id: str, result: RecipeResult, draft):
    """ONE home: the verb domain that owns the entity owns every write into it. The graph attributes a write it can
    attribute — a column no other archetype carries — and the disposal of the row; a writer standing outside the
    home, and presentation above all, is the second home the law forbids."""
    archetype = draft.nodes[archetype_id]
    home = home_of(archetype.get("source_location", ""))
    if home.startswith(PRESENTATION_LAYER):
        deviate(result, draft, f"transaction entity {archetype_id} is declared in {home} — the home of a "
                               f"transaction is a verb domain, never presentation", "transaction/one-home")
    carriers = defaultdict(set)
    for node_id, node in draft.nodes.items():
        for column in (node.get("components", []) if node.get("kind") == "archetype" else []):
            carriers[column].add(node_id)
    own_columns = {c for c in archetype.get("components", []) if carriers[c] == {archetype_id}}

    touches = [(e.src, e.source_location, f"writes {e.dst}") for e in draft.edges
               if e.rel == "writes" and e.dst in own_columns]
    touches += [(e.src, e.source_location, "deletes the row") for e in draft.edges
                if e.rel == "disposes" and e.dst == archetype_id]
    for owner, location, what in sorted(set(touches)):
        if not location.startswith(home):
            deviate(result, draft, f"{owner} {what} of the transaction entity {archetype_id} @ {location}, outside "
                                   f"its home {home} — the verb domain that owns the entity owns every write into "
                                   f"it", "transaction/one-home")


def check_transaction_archetype(archetype_id: str, result: RecipeResult, draft):
    """ONE archetypal form for the whole session, with every column it will ever have — the stage column included,
    so a step of the session is a value written into the row, never a row of another shape."""
    archetype = draft.nodes[archetype_id]
    stages = [c for c in archetype.get("components", []) if bare_name(c, draft).endswith(STATE_SUFFIX)]
    if not stages:
        deviate(result, draft, f"transaction entity {archetype_id} carries no stage column "
                               f"@ {archetype.get('source_location', '')} — one archetypal form holds the whole "
                               f"session, and the stage of the session is a column of it", "transaction/one-archetype")


def find_polymorphic_catalogues(sources, types, draft) -> RecipeResult:
    def reaches_scriptable_object(node_id):
        return bool({a.node for a in types.ancestry.get(node_id, [])} & CONFIG_BASES)

    carried_components = {e.dst for e in draft.edges if e.rel == "has"}
    result = RecipeResult()
    for base_id, base in sorted(draft.nodes.items()):
        if not (base.get("declared") and base.get("abstract") and reaches_scriptable_object(base_id)):
            continue
        containers = sorted({
            d["id"] for d in sources.declarations
            if not d["abstract"] and reaches_scriptable_object(d["id"])
            for f in d["fields"] if f["type"].is_array and "SerializeField" in f["attributes"]
            and draft.resolve(f["type"].name, sources.usings.get(d["file"], {""}), f["source_location"]) == base_id})
        kind_component = base["name"].removesuffix("Config") + "KindComponent"
        if containers and base["name"].endswith("Config") and kind_component in carried_components:
            result.instances.append(instance(base_id, base, "lexical", containers=containers,
                                             kind_component=kind_component))
            check_catalogue_shape(base_id, base, kind_component, sources, result, draft)
    return result


def check_catalogue_shape(base_id: str, base: dict, kind_component: str, sources, result: RecipeResult, draft):
    """The abstract base holds ONLY the shared key, and the row of the catalogue carries the reference key of the
    subject's space, the family label once there are several archetypes, and a kind column written at birth alone."""
    for declaration in (d for d in sources.declarations if d["id"] == base_id):
        keys = [f["name"] for f in declaration["fields"] if not f["is_const"]]
        if len(keys) != 1:
            deviate(result, draft, f"catalogue base {base_id} declares {len(keys)} fields [{', '.join(keys)}] "
                                   f"@ {declaration['source_location']} — the abstract base holds only the shared "
                                   f"key, and the kinds add their own parameters", "catalogue/config-shape")

    rows = sorted(i for i, n in draft.nodes.items()
                  if n.get("kind") == "archetype" and kind_component in n.get("components", []))
    for row in rows:
        node = draft.nodes[row]
        if not any(draft.nodes.get(c, {}).get("name", "").endswith("FKComponent") for c in node.get("components", [])):
            deviate(result, draft, f"catalogue row {row} carries no reference key of the subject's space",
                    "catalogue/row-shape")
        if len(rows) > 1 and not node.get("label_tags"):
            deviate(result, draft, f"catalogue row {row} is one of {len(rows)} archetypes of {base_id} and carries "
                                   f"no family label", "catalogue/row-shape")
    # a write standing in the member that creates the row is the birth write; any other write is a second one
    births = {(s["owner"], s["enclosing_member"]) for s in sources.ecs_sites if s["site"] == "create_entity"}
    for site in (s for s in sources.ecs_sites if s["site"] == "access" and s["access"] == "writes"
                 and s["type"] == kind_component and (s["owner"], s["enclosing_member"]) not in births):
        deviate(result, draft, f"catalogue kind column {kind_component} is written by {site['owner']} "
                               f"@ {site['source_location']}, outside the member that creates the row — the kind "
                               f"column is written once, at birth", "catalogue/row-shape")


def find_addressable_injections(draft) -> RecipeResult:
    injecting = sorted({e.src for e in draft.edges if e.rel == "injects" and e.via == "ctor" and e.dst == "IAddressable"})
    return RecipeResult([instance(i, draft.nodes[i], "lexical") for i in injecting])
