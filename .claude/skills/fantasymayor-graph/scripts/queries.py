"""The answers — one handler per command, each an independent entry point over a loaded graph that prints to stdout.
The handlers stand in the order of the COMMANDS table at the bottom of the file."""
from __future__ import annotations

import sys
import time
from collections import Counter, deque

from build import build_graph
from ecs_facts import KEY_ROLE_WARNING

INT_MAX = 2147483647


def answer_build(graph, request):
    # the CLI already refreshed a stale graph; this rebuilds on demand
    started = time.time()
    doc = build_graph(graph.root)
    print(f"fantasymayor-graph: {doc['meta']['files']} files -> {len(doc['nodes'])} nodes, {len(doc['edges'])} edges, "
          f"{len(doc['warnings'])} warnings ({time.time() - started:.2f}s)")


def answer_check(graph, request):
    errors = [f"dangling edge {end}: {e[end]} ({e['rel']} @ {e['source_location']})"
              for e in graph.edges for end in ("src", "dst") if e[end] not in graph.nodes]
    tables = graph.ecs_tables
    errors += [f"table owner not a node: {t['owner']} @ {t['source_location']}"
               for t in tables.get("tables", []) if t["owner"] not in graph.nodes]
    errors += [f"set owner not a node: {s['owner']} @ {s['source_location']}"
               for s in tables.get("sets", []) if s["owner"] and s["owner"] not in graph.nodes]
    errors += [f"archetype without components: {i}" for i, n in graph.nodes.items()
               if n.get("kind") == "archetype" and not n.get("components")]
    errors += [f"recipe instance not a node: {name} -> {inst['id']}" for name, result in graph.recipe_instances.items()
               for inst in result["instances"] if inst["id"] not in graph.nodes]
    confidence = Counter(e["confidence"] for e in graph.edges)
    print(f"fantasymayor-graph check · curated={graph.meta.get('curated')} · warnings={len(graph.warnings)} · "
          f"INFERRED={confidence['INFERRED']} · AMBIGUOUS={confidence['AMBIGUOUS']}")
    for warning in graph.warnings:
        print(f"  warn: {warning}")
    if errors:
        print(f"  {len(errors)} integrity error(s):")
        for error in errors[:40]:
            print(f"    {error}")
        sys.exit(1)
    print("  integrity: clean")


def answer_stats(graph, request):
    print(f"fantasymayor-graph · schema {graph.meta['schema']} · {graph.meta['files']} files · "
          f"generated {graph.meta['generated']} · curated={graph.meta.get('curated')}")
    print(f"nodes {len(graph.nodes)} · edges {len(graph.edges)} · warnings {len(graph.warnings)}")
    print("  kinds: " + counted(n.get("kind") for n in graph.nodes.values()))
    print("  edges: " + counted(e["rel"] for e in graph.edges))
    print("  system roles: " + counted(n.get("role") for n in graph.nodes.values() if n.get("kind") == "system"))
    print("  recipe instances: " + ", ".join(f"{name}={len(r['instances'])}" for name, r in graph.recipe_instances.items()))


def counted(values) -> str:
    return ", ".join(f"{k}={v}" for k, v in sorted(Counter(values).items()))


def answer_pattern(graph, request):
    result = graph.recipe_instances[request.recipe]
    print(f"== {request.recipe} ==  (decided by {result['decided_by']})")
    for inst in result["instances"]:
        extra = "".join(f"  {k}={v}" for k, v in inst.items()
                        if k not in ("id", "source_location", "decided_by") and v not in (None, [], ""))
        print(f"  {inst['id']:48} [{inst['decided_by']}]  {inst['source_location']}{extra}")
    for group, members in result["groups"].items():
        print(f"  -- {group} ({len(members)})")
        for member in members:
            print(f"     {member}")
    for deviation in result["deviations"]:
        print(f"  ⚠ {deviation}")
    if not result["instances"]:
        print(f"  (0 instances — {result['empty_reason']})")
    else:
        print(f"({len(result['instances'])} instance(s), {len(result['deviations'])} deviation(s))")


def answer_systems(graph, request):
    rows = sorted(((n.get("role") or "?", n.get("priority"), i) for i, n in graph.nodes.items()
                   if n.get("kind") == "system" and (not request.role or n.get("role") == request.role)),
                  key=lambda r: (r[0], r[1] if r[1] is not None else 1 << 62, r[2]))
    current = None
    for role, priority, node_id in rows:
        if role != current:
            print(f"[{role}]")
            current = role
        shown = "-" if priority is None else "int.MaxValue" if priority == INT_MAX else str(priority)
        print(f"  {shown:>12}  {node_id}  ({graph.nodes[node_id].get('decided_by')})")
    print(f"({len(rows)} systems)")


def answer_tags(graph, request):
    print("== archetypes: main tag | label tags ==")
    for archetype_id, node in sorted(graph.nodes.items()):
        if node.get("kind") == "archetype":
            print(f"  {archetype_id:56} {node.get('main_tag') or '—':36} {', '.join(node.get('label_tags', [])) or ''}")
    print("== deviations from one main tag + labels ==")
    for deviation in graph.tag_audit:
        print(f"  ⚠ {deviation['kind']}: {deviation['archetype_or_owner']} [{', '.join(deviation['tags'])}] "
              f"@ {deviation['source_location']}")
    print(f"({len(graph.tag_audit)} deviation(s) from one main tag + labels)")


def answer_explain(graph, request):
    node_id = graph.find_node(request.node)
    node = graph.nodes[node_id]
    print(f"== {node_id} ==")
    for facet in ("kind", "namespace", "source_location", "abstract", "declared", "role", "decided_by", "priority",
                  "base_anchor", "anchor_events", "held_events", "components", "main_tag", "label_tags",
                  "lifetime", "installer", "contract", "state", "markers"):
        if node.get(facet) not in (None, [], False):
            print(f"  {facet}: {node[facet]}")
    for title, edges, end, arrow in (("outgoing", graph.outgoing[node_id], "dst", "-{rel}->"),
                                     ("incoming", graph.incoming[node_id], "src", "<-{rel}-")):
        if edges:
            print(f"  {title}:")
        for edge in sorted(edges, key=lambda e: (e["rel"], e[end])):
            print(f"    {arrow.format(rel=edge['rel'])} {edge[end]}{edge_details(edge)}")
    tables = [t for t in graph.ecs_tables.get("tables", []) if node_id in (t["key"], t["owner"], t.get("archetype"))]
    if tables:
        print("  tables:")
    for table in tables:
        print(f"    {table['role']} key={table['key']} value_type={table['value_type']} "
              f"owner={table['owner']}.{table['field']} archetype={table.get('archetype')} @ {table['source_location']}")


def edge_details(edge) -> str:
    details = f"  via {edge['via']}"
    if edge.get("args"):
        details += f"  args<{', '.join(edge['args'])}>"
    if edge.get("app_state"):
        details += f"  app_state={edge['app_state']}"
    if edge.get("collection"):
        details += "  [collection]"
    if edge["confidence"] != "EXTRACTED":
        details += f"  ({edge['confidence']})"
    return details


def answer_neighbors(graph, request):
    node_id = graph.find_node(request.node)
    rels = set(filter(None, request.rel.split(",")))
    print(f"== neighbors of {node_id} ==")
    for edge in graph.outgoing[node_id]:
        if not rels or edge["rel"] in rels:
            print(f"  -{edge['rel']}-> {edge['dst']}{edge_details(edge)}")
    for edge in graph.incoming[node_id]:
        if not rels or edge["rel"] in rels:
            print(f"  <-{edge['rel']}- {edge['src']}{edge_details(edge)}")


def answer_search(graph, request):
    keyword = request.keyword.lower()
    hits = sorted(((i, n) for i, n in graph.nodes.items()
                   if keyword in i.lower() or keyword in (n.get("kind") or "") or keyword in (n.get("role") or "")),
                  key=lambda hit: (hit[1].get("kind") or "", hit[0]))
    hits = [(i, n) for i, n in hits if not request.role or n.get("role") == request.role]
    for node_id, node in hits:
        print(f"  {node_id:52} [{node.get('role') or node.get('kind')}]  {node.get('source_location', '')}")
    print(f"  ({len(hits)} matches)")


def answer_bfs(graph, request):
    node_id = graph.find_node(request.node)
    rels = set(filter(None, request.rel.split(",")))
    adjacency, end = (graph.incoming, "src") if request.incoming else (graph.outgoing, "dst")
    arrow = "<-" if request.incoming else "->"
    print(f"== BFS {'in' if request.incoming else 'out'} from {node_id} (depth {request.depth}) ==")
    seen, frontier = {node_id}, deque([(node_id, 0)])
    while frontier:
        current, depth = frontier.popleft()
        if depth >= request.depth:
            continue
        for edge in adjacency[current]:
            reached = edge[end]
            if (rels and edge["rel"] not in rels) or reached in seen:
                continue
            seen.add(reached)
            print(f"  {'  ' * depth}{arrow}{edge['rel']}: {reached} [{graph.nodes.get(reached, {}).get('kind', '?')}]")
            frontier.append((reached, depth + 1))


def answer_tables(graph, request):
    tables = graph.ecs_tables.get("tables", [])
    keys = sorted({t["key"] for t in tables if t["key"]})
    for key in keys:
        entries = sorted((t for t in tables if t["key"] == key), key=lambda t: (t["role"], t["owner"]))
        print(f"{key}  [{'+'.join(sorted({t['role'] for t in entries}))}]  ({len(entries)} uses)")
        for table in entries:
            print(f"    {table['role']:4} value_type={table['value_type']}  archetype={table.get('archetype')}  "
                  f"owner={table['owner']}.{table['field']}  @ {table['source_location']}")
    print(f"({len(tables)} table entries, {len(keys)} distinct key components)")


def answer_spaces(graph, request):
    def space_of(name):
        return next((name[: -len(suffix)] for suffix in ("IdFKComponent", "IdComponent", "FKComponent", "Component")
                     if name.endswith(suffix) and len(name) > len(suffix)), None)

    tables = graph.ecs_tables.get("tables", [])
    spaces = {}
    for node_id, node in graph.nodes.items():
        space = space_of(node.get("name", node_id)) if node.get("kind") == "component" else None
        if space is None:
            continue
        entry = spaces.setdefault(space, {"pk": None, "fk": None, "idx": []})
        if node.get("role") in ("pk", "fk"):
            entry[node["role"]] = node_id
        elif any(t["key"] == node_id for t in tables):
            entry["idx"].append(node_id)
    conflations = [w for w in graph.warnings if w.startswith(KEY_ROLE_WARNING)]
    for space, entry in sorted(spaces.items()):
        members = [("PK", entry["pk"]), ("FK", entry["fk"]), *(("IDX", i) for i in entry["idx"])]
        if not any(member for _, member in members):
            continue
        print(f"== {space} ==")
        for label, member in (m for m in members if m[1]):
            carriers = sorted({e["src"] for e in graph.incoming[member] if e["rel"] == "has"})
            print(f"  {label}: {member}  carriers=[{', '.join(carriers)}]  "
                  f"map-sites={sum(1 for t in tables if t['key'] == member)}")
        for conflation in (w for w in conflations if any(f" {m} " in w or w.endswith(m) for _, m in members if m)):
            print(f"  ⚠ {conflation}")
    print(f"({len(conflations)} key-role conflation(s) remaining)" if conflations else "(key-role law: clean)")


def answer_resolve(graph, request):
    written = request.node.replace(" ", "")
    name, _, arguments = written.partition("<")
    args = [a for a in arguments.rstrip(">").split(",") if a] if arguments else []
    node_id = graph.find_node(name)
    exposures = [e for e in graph.incoming[node_id] if e["rel"] == "exposes" and (not args or e.get("args", []) == args)]
    shown = f"{node_id}<{', '.join(args)}>" if args else node_id
    print(f"== resolve {shown} ==  ({len(exposures)} implementation(s) — what fills IReadOnlyList<{shown}>)")
    for edge in sorted(exposures, key=lambda e: (e["src"], e["source_location"])):
        registration = next((r for r in graph.incoming[edge["src"]]
                             if r["rel"] == "registers" and r["source_location"] == edge["source_location"]), {})
        implementation = graph.nodes.get(edge["src"], {})
        registered_args = f"<{', '.join(registration['args'])}>" if registration.get("args") else ""
        print(f"  {edge['src'] + registered_args:56} {implementation.get('lifetime', '?'):10} "
              f"via {implementation.get('installer', '?')}  @ {edge['source_location']}")
    if not exposures:
        print("  (none — not registered as a contract, or a concrete type)")


def answer_consumers(graph, request):
    node_id = graph.find_node(request.node)
    injections = sorted((e for e in graph.incoming[node_id] if e["rel"] == "injects"), key=lambda e: e["src"])
    print(f"== consumers of {node_id} ==  ({len(injections)} injector(s))")
    for edge in injections:
        print(f"  {edge['src']:48}{edge_details(edge)}  @ {edge['source_location']}")


def answer_installer(graph, request):
    node_id = graph.find_node(request.node)
    registrations = sorted((e for e in graph.outgoing[node_id] if e["rel"] == "registers"),
                           key=lambda e: (e["dst"], e["source_location"]))
    print(f"== {node_id} registers {len(registrations)} service(s) ==")
    for edge in registrations:
        exposed = [x["dst"] + (f"<{', '.join(x['args'])}>" if x.get("args") else "")
                   for x in graph.outgoing[edge["dst"]]
                   if x["rel"] == "exposes" and x["source_location"] == edge["source_location"]]
        registered_args = f"<{', '.join(edge['args'])}>" if edge.get("args") else ""
        print(f"  {edge['dst'] + registered_args:52} {edge['via']:20}"
              f"{'  As ' + ', '.join(exposed) if exposed else ''}{'  app_state=' + edge['app_state'] if edge.get('app_state') else ''}")


def answer_state(graph, request):
    node_id = graph.find_node(request.node)
    systems = sorted((e for e in graph.incoming[node_id] if e["rel"] == "runs_in"), key=lambda e: e["src"])
    print(f"== systems running in {node_id} ==  ({len(systems)})")
    for edge in systems:
        print(f"  {edge['src']:48}{edge_details(edge)}  @ {edge['source_location']}")


def answer_unresolved(graph, request):
    bound = {e["dst"] for e in graph.edges if e["rel"] in ("registers", "exposes")}
    missing = {}
    for edge in (e for e in graph.edges if e["rel"] == "injects" and e["dst"] not in bound):
        missing.setdefault(edge["dst"], set()).add(edge["src"])
    if not missing:
        print("fantasymayor-graph: no unresolved injections — every injected dependency is registered or exposed.")
        return
    print(f"== unresolved injections ==  ({len(missing)} type(s) injected but never registered or exposed)")
    for dependency, injectors in sorted(missing.items()):
        print(f"  {dependency:40} <- {', '.join(sorted(injectors))}")


COMMANDS = {
    "build": answer_build,
    "check": answer_check,
    "stats": answer_stats,
    "pattern": answer_pattern,
    "systems": answer_systems,
    "tags": answer_tags,
    "explain": answer_explain,
    "neighbors": answer_neighbors,
    "search": answer_search,
    "bfs": answer_bfs,
    "tables": answer_tables,
    "spaces": answer_spaces,
    "resolve": answer_resolve,
    "consumers": answer_consumers,
    "installer": answer_installer,
    "state": answer_state,
    "unresolved": answer_unresolved,
}
