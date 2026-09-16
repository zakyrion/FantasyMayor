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
STORE_TYPE = "EntityStore"
ENTITY_TYPE = "Entity"
KEY_ROLE_WARNING = "key-role:"
STATE_SUFFIX = "StateComponent"      # the state (stage) column of a row
KIND_SUFFIX = "KindComponent"        # the kind column of a row
ARITY_CAP = 5                        # type arguments per set in an archetype declaration
ARCHETYPE_RECEIVER = "archetype"     # what the receiver of a birth call says it is


@dataclass
class ArchetypeDeclaration:
    id: str
    components: list
    tags: list
    via: str
    source_location: str


def expand_archetypes(sources, types, draft):
    holders = sources.holders

    # a member returning a live Archetype takes the store as a parameter — the holder binds to no store of its own
    for (holder, member), declared in sorted(holders.items()):
        if declared["singleton"]:
            continue   # the manifest names its columns one Add<T>() at a time, not as one list of type arguments
        if STORE_TYPE not in declared["params"]:
            draft.warn(f"archetype member {holder}.{member} @ {declared['source_location']} takes no {STORE_TYPE} "
                       f"parameter — the store comes as a parameter", "archetype/shape")
        for part, written in (("columns", declared["components"]), ("tags", declared["tags"])):
            if len(written) > ARITY_CAP:
                draft.warn(f"archetype {holder}.{member} @ {declared['source_location']} names {len(written)} "
                           f"{part} [{', '.join(written)}] — a set of an archetype declaration takes at most "
                           f"{ARITY_CAP} type arguments", "archetype/arity-cap")

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
                   tag_order=tags, source_location=declaration.source_location)
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
    audit_key_spaces(sources, draft)
    resolve_priorities(sources, draft)
    check_singleton_manifest(sources, draft)
    audit_births(sources, draft)
    audit_state_and_kind_columns(sources, draft)
    audit_entity_links(sources, draft)


def connect_component_access(sources, draft):
    # the payload write standing in the same member as the CreateEvent that made the entity is that one raise, not a
    # second pulse; any other AddComponent of an event type is a pulse assembled by hand
    raised_here = {(s["owner"], s["enclosing_member"], s["type"]) for s in sources.ecs_sites
                   if s["site"] == "create_event" and s["type"]}
    sets, late_writes, tags_add_sites, singleton_used = [], [], [], set()
    for site in sources.ecs_sites:
        owner, location = site["owner"], site["source_location"]
        if site["site"] == "access" and site["type"]:
            written = draft.nodes[site["type"]]
            written_name = written["name"]
            if site["via"] == "AddComponent" and written.get("kind") == "event":
                if (owner, site["enclosing_member"], site["type"]) in raised_here:
                    continue   # the payload write of that CreateEvent — the emits edge anchors it
                draft.warn(f"{owner} adds event {written_name} to a live entity @ {location} — an event is raised "
                           f"by one CreateEvent call, and a pulse assembled by hand loses its tag or its stamp",
                           "event/raise")
            draft.add_edge(Edge(owner, site["type"], site["access"], site["via"], location,
                                confidence=site["confidence"]))
            if site["access"] == "removes":
                draft.warn(f"{owner} calls {site['via']}<{written_name}> @ {location} — after birth there is no "
                           f"removal of a component and no removal of a tag", "birth/no-late-structural")
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
            carriers = sorted(i for i, components in archetype_components.items() if key in components)
            location, role = index_field["source_location"], key_role(key or "")
            tables.append({"key": key, "value_type": index_field["type"].args[1], "owner": declaration["id"],
                           "field": index_field["name"], "via": "ComponentIndex",
                           "source_location": location, "role": role,
                           "archetype": carriers[0] if len(carriers) == 1 else None})
            if key and not carriers:
                draft.warn(f"key {key} of {declaration['id']}.{index_field['name']} @ {location} is named by no "
                           f"declared archetype — a table is a key component plus a discriminator, together in one "
                           f"declared archetype", "table/is")
            elif role == "data" and len(carriers) > 1:
                draft.warn(f"data column {key} keys {declaration['id']}.{index_field['name']} @ {location} across "
                           f"{len(carriers)} tables [{', '.join(carriers)}] — a data column never keys the indexes "
                           f"of two different tables", "key/data")
        if declaration["kind"] != "struct":
            continue
        indexed = next((b for b in declaration["bases"] if b.name == "IIndexedComponent" and b.args), None)
        if indexed is not None:
            indexed_components[declaration["id"]] = indexed.args[0]
        plain_fields = [f for f in declaration["fields"] if not f["is_const"]]
        if len(plain_fields) == 1:   # the PK wrapper shape — its value type even when it is never indexed itself
            component_field_types[declaration["id"]] = plain_fields[0]["type"].name
    for table in tables:   # a key component declares the indexed-component contract and returns its value by it
        if table["key"] and draft.nodes.get(table["key"], {}).get("declared") \
                and table["key"] not in indexed_components:
            draft.warn(f"key {table['key']} of {table['owner']}.{table['field']} @ {table['source_location']} "
                       f"declares no IIndexedComponent<…> contract", "index/key-equality")
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
                       f"{node['name'].replace('Component', 'FKComponent')}", "key/pk-single-carrier")
        elif node["role"] == "fk":
            owner_key = node["name"].replace("FKComponent", "Component")
            if owner_key in draft.nodes:
                draft.add_edge(Edge(node_id, owner_key, "fk_of", "key-role law (suffix)",
                                    node.get("source_location", "")))
            else:
                draft.warn(f"{KEY_ROLE_WARNING} FK {node['name']} has no owner key {owner_key} in the graph",
                           "name/suffix")

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
            if draft.nodes.get(owner_key, {}).get("declared"):   # no contract, no value to compare — never silent
                draft.warn(f"{KEY_ROLE_WARNING} owner key {owner_key} declares no IIndexedComponent<…> contract and "
                           f"no single value field, so the value of {table['key']} cannot be compared with it",
                           "index/key-equality")
            continue
        if owner_value == table["value_type"]:
            draft.add_edge(Edge(table["key"], owner_key, "fk_of", "ComponentIndex (TValue match)",
                                table["source_location"]))
        else:
            draft.warn(f"{KEY_ROLE_WARNING} {table['key']} indexed as "
                       f"ComponentIndex<{table['key']},{table['value_type']}> in "
                       f"{table['owner']} @ {table['source_location']}, but {owner_key} declares "
                       f"IIndexedComponent<{owner_value}> — value types diverge", "key/fk-value-parity")

    # references: archetype -> archetype through has(A, FK) + fk_of(FK, PK) + has(B, PK), one per fk_of signal
    for edge in [e for e in draft.edges if e.rel == "fk_of"]:
        for referencing in has_by_part.get(edge.src, ()):
            for referenced in has_by_part.get(edge.dst, ()):
                if referencing != referenced:
                    draft.add_edge(Edge(referencing, referenced, "references", edge.via, edge.source_location,
                                        confidence=edge.confidence))


def bare_name(node_id: str, draft) -> str:
    """The written name of a node without its nesting chain — what the suffix laws are read from."""
    return draft.nodes.get(node_id, {}).get("name", node_id).rsplit(".", 1)[-1]


def key_space(name: str) -> str | None:
    """The key space a column stands in: its name without the role suffix — HexIdComponent and HexIdFKComponent
    both stand in the Hex space."""
    return next((name[: -len(suffix)] for suffix in ("IdFKComponent", "IdComponent", "FKComponent", "Component")
                 if name.endswith(suffix) and len(name) > len(suffix)), None)


def audit_key_spaces(sources, draft):
    """Three laws over the key spaces. An entity holds one component per type, so it holds at most one foreign key
    into a space. An enum space — a space with no primary-key table — carries a data column on the owner's side.
    And a duplicate primary-key value returns both rows in silence, so uniqueness is checked, and thrown on, where
    the key is allocated; the member that writes the key is where the reader can look for that throw."""
    tables = draft.ecs_tables

    for archetype_id, archetype in sorted(draft.nodes.items()):
        if archetype.get("kind") != "archetype":
            continue
        spaces = defaultdict(list)
        for column in archetype.get("components", []):
            if key_role(bare_name(column, draft)) == "fk":
                spaces[key_space(bare_name(column, draft))].append(column)
        for space, keys in sorted(spaces.items()):
            if len(keys) > 1:
                draft.warn(f"archetype {archetype_id} carries {len(keys)} foreign keys into the {space} space "
                           f"[{', '.join(sorted(keys))}] @ {archetype.get('source_location', '')} — one component "
                           f"per type means at most one foreign key into a space, and a second reference is a pair "
                           f"of types of its own", "index/one-fk-per-space")

    enums = {d["name"] for d in sources.declarations if d["kind"] == "enum"}
    indexed, carried = tables["indexed_components"], {c for n in draft.nodes.values()
                                                      if n.get("kind") == "archetype" for c in n.get("components", [])}
    for fk_id in sorted(i for i in draft.nodes
                        if key_role(bare_name(i, draft)) == "fk" and indexed.get(i) in enums):
        value = indexed[fk_id]
        owner_columns = [c for c, v in indexed.items()
                         if v == value and c != fk_id and c in carried and key_role(bare_name(c, draft)) == "data"]
        if not owner_columns:
            draft.warn(f"{bare_name(fk_id, draft)} references the {value} space and no archetype carries a data "
                       f"column over {value} — in a space of enumeration values, with no primary-key table, the "
                       f"owner's side is a data column and the reference side the column with the reference suffix",
                       "key/enum-space")

    throwing = {(t["owner"], t["enclosing_member"]) for t in sources.throw_sites}
    indexed_pks = {t["key"] for t in tables["tables"] if t["role"] == "pk" and t["key"]}
    for site in (s for s in sources.ecs_sites
                 if s["site"] == "access" and s["access"] == "writes" and s["type"] in indexed_pks):
        if (site["owner"], site["enclosing_member"]) not in throwing:
            draft.warn(f"{site['owner']}.{site['enclosing_member']} writes the indexed primary key "
                       f"{bare_name(site['type'], draft)} @ {site['source_location']} and throws nowhere in that "
                       f"member — a duplicate key value returns both rows without an error, so uniqueness is "
                       f"checked where the key is allocated and throws there", "index/pk-uniqueness")


def audit_births(sources, draft):
    """A row is born by a creation call ON ITS ARCHETYPE. A creation whose receiver names no archetype, or that
    composes the row out of components and tags at the call site, is the bare creation on the store the law
    forbids — the composition belongs in an archetype declaration."""
    for site in (s for s in sources.ecs_sites if s["site"] == "create_entity"):
        receiver, composed = site["receiver"], site["via"] == "CreateEntity" and site["argc"] > 0
        if ARCHETYPE_RECEIVER in receiver.lower() and not composed:
            continue
        reason = (f"composes the row out of {site['argc']} argument(s) at the call site"
                  if composed else f"calls {site['via']} on '{receiver}', which names no archetype")
        draft.warn(f"{site['owner']} {reason} @ {site['source_location']} — an entity is born by a creation call on "
                   f"its archetype, never by a bare creation on the store", "archetype/birth")


def audit_state_and_kind_columns(sources, draft):
    """The state of a row and the kind of a row are COLUMNS OVER AN ENUMERATION — never a tag toggled on the live
    row (the tag law holds that half) and never a second main tag. The kind column is written once, at birth."""
    enums = {d["name"] for d in sources.declarations if d["kind"] == "enum"}
    for declaration in sources.declarations:
        name = declaration["name"]
        role = ("state" if name.endswith(STATE_SUFFIX) else "kind" if name.endswith(KIND_SUFFIX) else None)
        if role is None or declaration["kind"] != "struct":
            continue
        valued = [f for f in declaration["fields"] if not f["is_const"]]
        if not any(f["type"].name in enums for f in valued):
            written = ", ".join(f"{f['type'].name} {f['name']}" for f in valued) or "nothing"
            draft.warn(f"{role} column {name} @ {declaration['source_location']} carries {written} — the {role} of a "
                       f"row is a column over an enumeration", f"tag/{role}-column")

    births = {(s["owner"], s["enclosing_member"]) for s in sources.ecs_sites if s["site"] == "create_entity"}
    for site in (s for s in sources.ecs_sites if s["site"] == "access" and s["access"] == "writes" and s["type"]):
        if bare_name(site["type"], draft).endswith(KIND_SUFFIX) \
                and (site["owner"], site["enclosing_member"]) not in births:
            draft.warn(f"{site['owner']} writes the kind column {bare_name(site['type'], draft)} in "
                       f"{site['enclosing_member']} @ {site['source_location']}, outside the member that creates the "
                       f"row — the kind of a row is written once, at birth", "tag/kind-column")


def audit_entity_links(sources, draft):
    """A saved entity descriptor, read by where it is saved. In a row it is a join kept in a column: a join is a
    lookup by key value at the point of use, never a stored reference from the row of one table into the row of
    another. Anywhere else it is a link that outlives the call, and a link that persists is a domain key —
    a direct descriptor belongs only to a runtime-only link, which the graph cannot tell apart."""
    for declaration in sources.declarations:
        kind = draft.nodes.get(declaration["id"], {}).get("kind")
        for stored in (f for f in declaration["fields"] if not f["is_const"] and holds_entity(f["type"])):
            if kind in ("component", "event"):   # a column of a row — an event payload is a column too
                draft.warn(f"column {declaration['id']}.{stored['name']} stores an {ENTITY_TYPE} "
                           f"@ {stored['source_location']} — a join is a lookup by key value at the point of use, "
                           f"never a reference from the row of one table stored in the row of another",
                           "table/join-at-use")
            elif kind in ("other", "system", "view", "config", "installer"):
                draft.warn(f"{declaration['id']}.{stored['name']} stores an {ENTITY_TYPE} descriptor "
                           f"@ {stored['source_location']} — a persistent link is a domain key, the identity column "
                           f"of the owner with the reference column pointing at it", "link/persistent-key")


def holds_entity(type_use) -> bool:
    """A written type that holds an entity descriptor: Entity itself, an array or collection of them, or a generic
    with one among its arguments."""
    written = [type_use.name, *type_use.args, type_use.element.name if type_use.element else ""]
    return any(name.rsplit(".", 1)[-1] == ENTITY_TYPE for name in written if name)


def resolve_priorities(sources, draft):
    const_table = {}
    for declaration in sources.declarations:
        const_table.update(declaration["consts"])
    for declaration in (d for d in sources.declarations if d["priority"]):
        expression = declaration["priority"]
        if expression.lstrip("-").isdigit():
            value = int(expression)
            draft.warn(f"Priority of {declaration['id']} @ {declaration['source_location']} is the literal "
                       f"{expression} — Priority returns a named const int of the project's priority holder",
                       "system/priority-source")
        elif "." in expression:
            suffix_hits = {v for k, v in const_table.items() if k.endswith("." + expression)}
            value = const_table.get(expression, suffix_hits.pop() if len(suffix_hits) == 1 else None)
        else:
            value = const_table.get(f"{declaration['chain']}.{expression}")
            if value is not None:
                draft.warn(f"Priority of {declaration['id']} @ {declaration['source_location']} is '{expression}', "
                           f"a const of the class itself — Priority returns a named const int of the project's "
                           f"priority holder", "system/priority-source")
        if value is None:
            draft.warn(f"unresolved Priority expression on {declaration['id']}: '{expression}'")
            continue
        draft.add_node(declaration["id"], priority=value)


def check_singleton_manifest(sources, draft):
    used = set(draft.ecs_tables["singleton_used"])
    manifests = [f"{h}.{m}" for (h, m), declared in sources.holders.items() if declared["singleton"]]
    if used and len(manifests) != 1:
        draft.warn(f"singleton manifest: expected exactly one Singleton archetype, found {len(manifests)}",
                   "singleton/manifest")
    elif used:
        declared_components = set(draft.nodes[manifests[0]].get("components", []))
        missing, unused = sorted(used - declared_components), sorted(declared_components - used)
        if missing:
            draft.warn(f"singleton manifest: used but undeclared [{', '.join(missing)}]", "singleton/manifest")
        if unused:
            draft.warn(f"singleton manifest: declared but unused [{', '.join(unused)}]", "singleton/manifest")

    carried = {c for n in draft.nodes.values() if n.get("kind") == "archetype" for c in n.get("components", [])}
    for edge in draft.edges:
        written = draft.nodes.get(edge.dst, {})
        if edge.rel == "writes" and written.get("kind") == "component" and edge.dst not in carried:
            draft.warn(f"writes {edge.dst} by {edge.src} @ {edge.source_location}: no declared archetype names this "
                       f"column", "archetype/no-orphan-column")
