"""Knowing the types: a node for every declaration with every name in the raw facts resolved to it, the markers
bound to their classes and tags, and the ancestry — inherits edges and the transitive ancestors of every type."""
from __future__ import annotations

from dataclasses import dataclass, field as dataclass_field

from graph_draft import Edge

CONFIG_BASES = {"ScriptableObject", "SerializedScriptableObject"}
VIEW_FOLDER = "/Views/"
INSTALLER_BASES = {"IInstaller", "LifetimeScope"}
TYPE_FIELDS_OF_SITES = ("type", "types", "components", "tags", "state")


@dataclass
class Markers:
    role: str | None = None                       # per_frame | reactive
    views: list = dataclass_field(default_factory=list)
    label: str | None = None                      # none | transaction


@dataclass(frozen=True)
class Ancestor:
    node: str
    args: tuple


@dataclass
class TypeFacts:
    markers: dict        # id -> Markers
    ancestry: dict       # id -> [Ancestor]


def know_types(sources, draft) -> TypeFacts:
    declare_nodes(sources, draft)
    markers = read_markers(sources, draft)
    ancestry = build_ancestry(sources, draft)
    return TypeFacts(markers, ancestry)


def declare_nodes(sources, draft):
    # One id per declared type: its nesting chain, or namespace + chain when two namespaces declare the same chain.
    # Partial declarations of one type share the id.
    namespaces_of_chain = {}
    for declaration in sources.declarations:
        namespaces_of_chain.setdefault(declaration["chain"], set()).add(declaration["namespace"])
    id_of = {}
    for declaration in sources.declarations:
        chain, namespace = declaration["chain"], declaration["namespace"]
        node_id = chain if len(namespaces_of_chain[chain]) == 1 else f"{namespace}.{chain}"
        id_of[(namespace, chain)] = node_id
        declaration["id"] = node_id

        bases = {b.name for b in declaration["bases"]}
        if declaration["kind"] == "struct":
            kind = ("tag" if "ITag" in bases
                    else "event" if declaration["name"].endswith(("Event", "EventComponent"))
                    else "component" if "IComponent" in bases or declaration["name"].endswith("Component")
                    else "data")
        elif declaration["kind"] == "interface":
            kind = "interface"
        elif declaration["kind"] == "class" and (declaration["name"].endswith("Installer") or bases & INSTALLER_BASES):
            kind = "installer"
        else:
            kind = "other"
        draft.add_node(node_id, name=chain, kind=kind, namespace=namespace,
                       source_location=declaration["source_location"], declared=True,
                       abstract=declaration["abstract"] or draft.nodes.get(node_id, {}).get("abstract", False))

    # Every type name in a raw fact becomes the id of its node, in place; the owner class too.
    for site in [*sources.ecs_sites, *sources.di_sites, *sources.subscription_sites]:
        site["owner"] = id_of.get((site["owner_namespace"], site["owner"]), "")
        namespaces = sources.usings.get(site["file"], {""})
        for key in TYPE_FIELDS_OF_SITES:
            if isinstance(site.get(key), str):
                site[key] = draft.resolve(site[key], namespaces, site["source_location"])
            elif isinstance(site.get(key), list):
                site[key] = [draft.resolve(name, namespaces, site["source_location"]) for name in site[key]]
        if site.get("site") in ("holder_call", "typed_call"):
            # a type parameter of the enclosing generic method is a template slot, not a type
            site["targs"] = [name if name in site.get("enc_tps", []) else
                             draft.resolve(name, namespaces, site["source_location"]) for name in site["targs"]]
        for type_use in [*site.get("type_args", []), *site.get("as_targets", []),
                         *site.get("construct_params", {}).values()]:
            draft.resolve_type_use(type_use, namespaces, site["source_location"])


def read_markers(sources, draft) -> dict:
    markers = {}
    for declaration in sources.declarations:
        namespaces = sources.usings.get(declaration["file"], {""})
        for attribute in declaration["attributes"]:
            name = attribute["name"].removesuffix("Attribute")
            argument = attribute["args"][0] if attribute["args"] else ""
            if name not in ("SystemRole", "ViewSubscriber", "TagLabel"):
                continue
            marked = markers.setdefault(declaration["id"], Markers())
            if name == "SystemRole":
                marked.role = {"PerFrame": "per_frame", "Reactive": "reactive"}.get(argument.rsplit(".", 1)[-1])
            elif name == "TagLabel":
                marked.label = argument.rsplit(".", 1)[-1].lower() if argument else "none"
            else:
                view_name = argument.removeprefix("typeof(").removesuffix(")").strip().rsplit(".", 1)[-1]
                view = draft.resolve(view_name, namespaces, declaration["source_location"])
                if view is not None:
                    marked.views.append(view)
    for node_id, marked in markers.items():
        draft.add_node(node_id, markers={"role": marked.role, "views": sorted(marked.views), "label": marked.label})
    return markers


def build_ancestry(sources, draft) -> dict:
    # inherits: every written base, its generic arguments on the edge; a base outside the game code is a node too
    for declaration in sources.declarations:
        namespaces = sources.usings.get(declaration["file"], {""})
        for base in declaration["bases"]:
            base_id = draft.resolve(base.name, namespaces, declaration["source_location"])
            if base_id is not None:
                draft.add_edge(Edge(declaration["id"], base_id, "inherits", "base", declaration["source_location"],
                                    args=base.args))

    parents = {}
    for edge in draft.edges:
        if edge.rel == "inherits":
            parents.setdefault(edge.src, []).append(Ancestor(edge.dst, edge.args))
    ancestry = {}
    for node_id in draft.nodes:
        ancestors, visited, stack = [], set(), list(parents.get(node_id, []))
        while stack:
            ancestor = stack.pop()
            if ancestor in visited:   # a shared ancestor is walked once
                continue
            visited.add(ancestor)
            ancestors.append(ancestor)
            stack.extend(parents.get(ancestor.node, []))
        ancestry[node_id] = ancestors

    for declaration in sources.declarations:
        if declaration["kind"] != "class":
            continue
        ancestor_names = {a.node for a in ancestry.get(declaration["id"], [])}
        if not draft.nodes[declaration["id"]].get("abstract") and ancestor_names & CONFIG_BASES:
            draft.add_node(declaration["id"], kind="config")
        elif VIEW_FOLDER in "/" + declaration["folder"] and "MonoBehaviour" in ancestor_names:
            draft.add_node(declaration["id"], kind="view")
    return ancestry
