"""The ECS half of the graph: archetypes expanded from their holders with main and label tags, the bindings of every
owner class to them, and the reconciliation of the raw ECS facts — accesses, disposals, ComponentIndex tables, the
key-role law, priorities and the singleton manifest."""
from __future__ import annotations

from collections import defaultdict
from dataclasses import dataclass

from graph_draft import Edge

EVENT_FRAME = "EventFrameComponent"
EVENT_TAG = "EventTag"
TEMPLATE_QUEUE_LIMIT = 10000
SYSTEM_BASES = {"UpdatedSystem", "LateUpdatedSystem"}
KEY_ROLE_WARNING = "key-role:"


@dataclass
class ArchetypeDeclaration:
    id: str
    components: list
    tags: list
    via: str
    source_location: str


def expand_archetypes(sources, types, draft):
    holders = sources.holders

    # concrete holder members and the singleton manifest are archetypes by their declaration alone
    for (holder, member), declared in holders.items():
        if declared["type_params"]:
            continue
        namespaces = sources.usings.get(declared["file"], {""})
        register_archetype(ArchetypeDeclaration(
            f"{holder}.{member}",
            [draft.resolve(n, namespaces, declared["source_location"]) for n in declared["components"]],
            [draft.resolve(n, namespaces, declared["source_location"]) for n in declared["tags"]],
            f"{holder}.{member}", declared["source_location"]), types.markers, draft)

    # every binding: a generic holder member closes over the binding's arguments into an archetype of its own, and
    # the owner class is bound to the archetype it named
    bindings = [s for s in sources.ecs_sites if s["site"] == "holder_call"] + walk_templates(sources)
    for binding in bindings:
        declared = holders.get((binding["holder"], binding["member"]))
        if declared is None or None in binding["targs"]:
            continue
        archetype_id = f"{binding['holder']}.{binding['member']}"
        if declared["type_params"]:
            if len(binding["targs"]) != len(declared["type_params"]):
                draft.warn(f"holder call {archetype_id} at {binding['source_location']} ({binding['owner']}): "
                           f"expected {len(declared['type_params'])} type arg(s), got {len(binding['targs'])} — "
                           f"unresolved (forwarding template?)")
                continue
            namespaces = sources.usings.get(declared["file"], {""})
            substitution = dict(zip(declared["type_params"], binding["targs"]))
            closed = [[substitution[n] if n in substitution else draft.resolve(n, namespaces, declared["source_location"])
                       for n in declared[part]] for part in ("components", "tags")]
            archetype_id = f"{archetype_id}<{','.join(binding['targs'])}>"
            register_archetype(ArchetypeDeclaration(archetype_id, closed[0], closed[1],
                                                    f"{binding['holder']}.{binding['member']}",
                                                    binding["source_location"]), types.markers, draft)
        bind_owner(archetype_id, binding, draft)

    # AnyComponents over event types only anchors or holds those events, like EventArchetypes.Of does
    for site in (s for s in sources.ecs_sites if s["site"] == "any_components" and s["owner"]):
        only_events = bool(site["types"]) and all(t and draft.nodes[t].get("kind") == "event" for t in site["types"])
        owner = draft.nodes[site["owner"]]
        anchor = anchor_of(site, draft)
        if anchor == "base":
            owner["base_anchor"] = "event" if only_events or owner.get("base_anchor") == "event" else "table"
        if only_events and anchor != "other-base":
            facet = "anchor_events" if anchor == "base" else "held_events"
            owner[facet] = sorted(set(owner.get(facet, [])) | set(site["types"]))


def register_archetype(declaration: ArchetypeDeclaration, markers: dict, draft):
    tags = [t for t in declaration.tags if t]
    label_tags = [t for t in tags if t in markers and markers[t].label is not None]
    main_tags = [t for t in tags if t not in label_tags]
    draft.add_node(declaration.id, name=declaration.id, kind="archetype",
                   components=sorted({c for c in declaration.components if c}),
                   main_tag=main_tags[0] if len(main_tags) == 1 else None, label_tags=sorted(label_tags),
                   source_location=declaration.source_location)
    for part in [*declaration.components, *tags]:
        draft.add_edge(Edge(declaration.id, part, "has", declaration.via, declaration.source_location))


def walk_templates(sources) -> list[dict]:
    """The bindings a template walk reaches. A generic method relaying its own type parameters into another call is a
    template: every concrete typed call seeds a walk that substitutes the arguments hop by hop until it reaches a
    holder."""
    holders = sources.holders
    templates, seeds = {}, []
    for site in (s for s in sources.ecs_sites if s["site"] == "typed_call"):
        if site["enc_tps"] and all(t in site["enc_tps"] for t in site["targs"]):
            template = templates.setdefault((site["owner"].rsplit(".", 1)[-1], site["enc_method"]),
                                            {"type_params": site["enc_tps"], "forwards": []})
            template["forwards"].append((site["target_class"], site["method"], site["targs"]))
        else:
            seeds.append(site)

    reached = []
    queue = [(s["target_class"], s["method"], s["targs"], s) for s in seeds
             if (s["target_class"], s["method"]) in templates or (s["target_class"], s["method"]) in holders]
    seen, steps = set(), 0
    while queue and steps < TEMPLATE_QUEUE_LIMIT:   # a relay cycle has no guaranteed progress — the limit ends it
        steps += 1
        target_class, method, targs, origin = queue.pop()
        signature = (target_class, method, tuple(targs), origin["owner"])
        if signature in seen:
            continue
        seen.add(signature)
        if (target_class, method) in holders:
            reached.append({**origin, "site": "holder_call", "holder": target_class, "member": method,
                            "targs": list(targs)})
            continue
        template = templates.get((target_class, method))
        if template is None or len(template["type_params"]) != len(targs):
            continue   # a hop into neither a holder nor a template of the same arity reaches no archetype
        substitution = dict(zip(template["type_params"], targs))
        for forward_class, forward_method, forward_targs in template["forwards"]:
            queue.append((substitution.get(forward_class, forward_class), forward_method,
                          [substitution.get(t, t) for t in forward_targs], origin))
    return reached


def bind_owner(archetype_id: str, binding: dict, draft):
    """How the binding's owner class is bound to an archetype: anchored in base(...) or held in its body; an event
    archetype names its events on the owner's facets, any other archetype is a reads edge."""
    owner_id = binding["owner"]
    archetype, owner = draft.nodes[archetype_id], draft.nodes[owner_id]
    is_event = EVENT_FRAME in archetype.get("components", []) and archetype.get("main_tag") == EVENT_TAG
    anchor = anchor_of(binding, draft)
    if anchor == "base":
        owner["base_anchor"] = "event" if is_event or owner.get("base_anchor") == "event" else "table"
    if not is_event:
        draft.add_edge(Edge(owner_id, archetype_id, "reads", f"{binding['holder']}.{binding['member']}",
                            binding["source_location"]))
        return
    if anchor == "other-base":
        return   # an event archetype handed to a base other than a system's is neither an anchor nor held
    facet = "anchor_events" if anchor == "base" else "held_events"
    events = [c for c in archetype["components"] if c != EVENT_FRAME]
    owner[facet] = sorted(set(owner.get(facet, [])) | set(events))


def anchor_of(site: dict, draft) -> str:
    """Where a site binds its owner class: "base" — inside base(...) of a class whose direct base is UpdatedSystem or
    LateUpdatedSystem, a system's anchor; "other-base" — inside base(...) of any other base, neither an anchor nor a
    held archetype; "body" — anywhere else, this(...) included. MarkerShapeAnalyzer measures the same."""
    if site["anchor"] != "base":
        return "body"
    on_system_base = any(e.rel == "inherits" and e.src == site["owner"] and e.dst in SYSTEM_BASES for e in draft.edges)
    return "base" if on_system_base else "other-base"


def reconcile_ecs_facts(sources, draft):
    connect_component_access(sources, draft)
    attribute_disposals(sources, draft)
    attach_tables(sources, draft)
    apply_key_role_law(sources, draft)
    resolve_priorities(sources, draft)
    check_singleton_manifest(sources, draft)


def connect_component_access(sources, draft):
    sets, late_writes, tags_add_sites, singleton_used = [], [], [], set()
    for site in sources.ecs_sites:
        owner, location = site["owner"], site["source_location"]
        if site["site"] == "access" and site["type"]:
            written_name = draft.nodes[site["type"]]["name"]
            if site["via"] == "AddComponent" and written_name.endswith("Event"):
                continue   # the payload write right after CreateEvent — the emits edge anchors it
            draft.add_edge(Edge(owner, site["type"], site["access"], site["via"], location,
                                confidence=site["confidence"]))
            if site["singleton"]:
                singleton_used.add(site["type"])
            if site["via"] == "AddComponent" and written_name.endswith("Tag"):
                late_writes.append({"owner": owner, "component": site["type"], "source_location": location})
        elif site["site"] == "set" and (site["components"] or site["tags"]):
            sets.append({"owner": owner, "components": [c for c in site["components"] if c],
                         "tags": [t for t in site["tags"] if t], "via": site["via"], "source_location": location})
        elif site["site"] == "create_event" and site["type"]:
            draft.add_node(site["type"], kind="event")
            draft.add_edge(Edge(owner, site["type"], "emits", "CreateEvent", location))
        elif site["site"] == "tags_add":
            tags_add_sites.append({"owner": owner, "tags": [t for t in site["types"] if t], "source_location": location})
    draft.ecs_tables.update(sets=sets, late_writes=late_writes, tags_add_sites=tags_add_sites,
                            singleton_used=sorted(singleton_used))


def attribute_disposals(sources, draft):
    event_sweepers = {s["owner"] for s in draft.ecs_tables["sets"] if EVENT_TAG in s["tags"]}
    owner_archetypes = defaultdict(set)
    for edge in draft.edges:
        if edge.rel == "reads" and draft.nodes.get(edge.dst, {}).get("kind") == "archetype":
            owner_archetypes[edge.src].add(edge.dst)

    dispose_sites = []
    for site in (s for s in sources.ecs_sites if s["site"] == "delete_entity"):
        owner, location = site["owner"], site["source_location"]
        dispose_sites.append({"owner": owner, "source_location": location})
        if owner in event_sweepers:
            continue   # ripe events are deleted by the event sweeper, never a row type of its own
        candidates = owner_archetypes.get(owner, set())
        if len(candidates) == 1:
            draft.add_edge(Edge(owner, next(iter(candidates)), "disposes", "DeleteEntity", location,
                                confidence="INFERRED"))
        else:
            draft.warn(f"entity DeleteEntity at {location} ({owner}) — {len(candidates)} candidate archetypes via "
                       f"its own bindings — attribute by hand")
    draft.ecs_tables["dispose_sites"] = dispose_sites


def attach_tables(sources, draft):
    archetype_components = {i: n.get("components", []) for i, n in draft.nodes.items() if n.get("kind") == "archetype"}
    tables, indexed_components, component_field_types = [], {}, {}
    for declaration in sources.declarations:
        namespaces = sources.usings.get(declaration["file"], {""})
        for index_field in (f for f in declaration["fields"]
                            if f["type"].name == "ComponentIndex" and len(f["type"].args) == 2):
            key = draft.resolve(index_field["type"].args[0], namespaces, index_field["source_location"])
            carriers = [i for i, components in archetype_components.items() if key in components]
            tables.append({"key": key, "value_type": index_field["type"].args[1], "owner": declaration["id"],
                           "field": index_field["name"], "via": "ComponentIndex",
                           "source_location": index_field["source_location"], "role": key_role(key or ""),
                           "archetype": carriers[0] if len(carriers) == 1 else None})
        if declaration["kind"] != "struct":
            continue
        indexed = next((b for b in declaration["bases"] if b.name == "IIndexedComponent" and b.args), None)
        if indexed is not None:
            indexed_components[declaration["id"]] = indexed.args[0]
        plain_fields = [f for f in declaration["fields"] if not f["is_const"]]
        if len(plain_fields) == 1:   # the PK wrapper shape — its value type even when it is never indexed itself
            component_field_types[declaration["id"]] = plain_fields[0]["type"].name
    draft.ecs_tables.update(
        tables=tables,
        index_usages=[{"owner": s["owner"], "field": s["field"], "source_location": s["source_location"]}
                      for s in sources.ecs_sites if s["site"] == "index_usage" and s["owner"]],
        indexed_components=indexed_components, component_field_types=component_field_types)


def key_role(name: str) -> str:
    return "fk" if name.endswith("FKComponent") else "pk" if name.endswith("IdComponent") else "data"


def apply_key_role_law(sources, draft):
    has_by_part = defaultdict(set)
    for edge in draft.edges:
        if edge.rel == "has":
            has_by_part[edge.dst].add(edge.src)

    for node_id, node in list(draft.nodes.items()):
        if node.get("kind") != "component":
            continue
        node["role"] = key_role(node["name"])
        if node["role"] == "pk" and len(has_by_part[node_id]) > 1:
            draft.warn(f"{KEY_ROLE_WARNING} PK {node['name']} is carried by {len(has_by_part[node_id])} archetypes "
                       f"[{', '.join(sorted(has_by_part[node_id]))}] — foreign carriers must switch to "
                       f"{node['name'].replace('Component', 'FKComponent')}")
        elif node["role"] == "fk":
            owner_key = node["name"].replace("FKComponent", "Component")
            if owner_key in draft.nodes:
                draft.add_edge(Edge(node_id, owner_key, "fk_of", "key-role law (suffix)",
                                    node.get("source_location", "")))
            else:
                draft.warn(f"{KEY_ROLE_WARNING} FK {node['name']} has no owner key {owner_key} in the graph")

    # The same relation by evidence: an FK really looked up through ComponentIndex<FK, TValue> whose TValue is the
    # owner PK's own value type — a separate fk_of edge, so a divergence between the two signals stays visible.
    tables = draft.ecs_tables
    looked_up = {(u["owner"], u["field"]) for u in tables["index_usages"]}
    for table in tables["tables"]:
        if table["role"] != "fk" or (table["owner"], table["field"]) not in looked_up:
            continue
        owner_key = table["key"].replace("FKComponent", "Component")
        owner_value = tables["indexed_components"].get(owner_key) or tables["component_field_types"].get(owner_key)
        if owner_value is None:
            continue
        if owner_value == table["value_type"]:
            draft.add_edge(Edge(table["key"], owner_key, "fk_of", "ComponentIndex (TValue match)",
                                table["source_location"]))
        else:
            draft.warn(f"{KEY_ROLE_WARNING} {table['key']} indexed as "
                       f"ComponentIndex<{table['key']},{table['value_type']}> in "
                       f"{table['owner']} @ {table['source_location']}, but {owner_key} declares "
                       f"IIndexedComponent<{owner_value}> — value types diverge")

    # references: archetype -> archetype through has(A, FK) + fk_of(FK, PK) + has(B, PK), one per fk_of signal
    for edge in [e for e in draft.edges if e.rel == "fk_of"]:
        for referencing in has_by_part.get(edge.src, ()):
            for referenced in has_by_part.get(edge.dst, ()):
                if referencing != referenced:
                    draft.add_edge(Edge(referencing, referenced, "references", edge.via, edge.source_location,
                                        confidence=edge.confidence))


def resolve_priorities(sources, draft):
    const_table = {}
    for declaration in sources.declarations:
        const_table.update(declaration["consts"])
    for declaration in (d for d in sources.declarations if d["priority"]):
        expression = declaration["priority"]
        if expression.lstrip("-").isdigit():
            value = int(expression)
        elif "." in expression:
            suffix_hits = {v for k, v in const_table.items() if k.endswith("." + expression)}
            value = const_table.get(expression, suffix_hits.pop() if len(suffix_hits) == 1 else None)
        else:
            value = const_table.get(f"{declaration['chain']}.{expression}")
        if value is None:
            draft.warn(f"unresolved Priority expression on {declaration['id']}: '{expression}'")
            continue
        draft.add_node(declaration["id"], priority=value)


def check_singleton_manifest(sources, draft):
    used = set(draft.ecs_tables["singleton_used"])
    manifests = [f"{h}.{m}" for (h, m), declared in sources.holders.items() if declared["singleton"]]
    if used and len(manifests) != 1:
        draft.warn(f"singleton manifest: expected exactly one Singleton archetype, found {len(manifests)}")
    elif used:
        declared_components = set(draft.nodes[manifests[0]].get("components", []))
        missing, unused = sorted(used - declared_components), sorted(declared_components - used)
        if missing:
            draft.warn(f"singleton manifest: used but undeclared [{', '.join(missing)}]")
        if unused:
            draft.warn(f"singleton manifest: declared but unused [{', '.join(unused)}]")

    carried = {c for n in draft.nodes.values() if n.get("kind") == "archetype" for c in n.get("components", [])}
    for edge in draft.edges:
        written = draft.nodes.get(edge.dst, {})
        if edge.rel == "writes" and written.get("kind") == "component" and edge.dst not in carried:
            draft.warn(f"writes {edge.dst} by {edge.src} @ {edge.source_location}: not part of any declared "
                       f"archetype (orphaned column?)")
