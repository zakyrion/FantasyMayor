"""GraphDraft — the graph while the build writes it: nodes, edges, the ECS tables, the tag audit, the recipe
instances and the warnings. A class-API of primitives; every build step writes its own sections through them."""
from __future__ import annotations

from collections import defaultdict
from dataclasses import dataclass


@dataclass(frozen=True)
class Edge:
    src: str
    dst: str
    rel: str
    via: str
    source_location: str
    args: tuple = ()
    app_state: str | None = None
    collection: bool = False
    confidence: str = "EXTRACTED"


class GraphDraft:
    def __init__(self, warnings):
        self.nodes: dict[str, dict] = {}
        self.name_index: dict[str, list[str]] = defaultdict(list)   # bare type name -> ids of its declarations
        self.edges: list[Edge] = []
        self.edge_keys: set[tuple] = set()
        self.ecs_tables: dict = {}
        self.tag_audit: list[dict] = []
        self.recipe_instances: dict[str, dict] = {}
        self.warnings: list[str] = list(warnings)

    def add_node(self, node_id: str, **facets):
        """A new node takes every facet. An existing node takes every facet that is set, except that a guessed
        kind never overrides the kind of a declared node."""
        node = self.nodes.get(node_id)
        if node is None:
            node = self.nodes[node_id] = {"name": node_id}
        if facets.get("declared") and not node.get("declared"):
            self.name_index[facets.get("name", node_id).rsplit(".", 1)[-1]].append(node_id)
        guessed = facets.get("declared") is False
        for key, value in facets.items():
            if value is None or (guessed and node.get("declared") and key in ("kind", "declared")):
                continue
            node[key] = value

    def resolve(self, name: str, namespaces, location: str) -> str | None:
        """A type name written in a file -> the id of its node. A nested type is reachable by a bare name only when
        no top-level type carries that name; several top-level candidates are narrowed by the namespaces the file
        sees. None means the reference does not become an edge — the AMBIGUOUS warning says why."""
        if not name:
            return None
        declared = self.name_index.get(name, [])
        candidates = [i for i in declared if "." not in self.nodes[i]["name"]] or declared

        if len(candidates) == 1:
            return candidates[0]
        if not candidates:
            if name not in self.nodes:
                kind = next((kind for suffix, kind in (("Tag", "tag"), ("Event", "event"), ("Component", "component"),
                                                       ("Installer", "installer"), ("Config", "config"))
                             if name.endswith(suffix)), "data")
                self.add_node(name, name=name, kind=kind, declared=False)
            return name

        visible = [i for i in candidates if self.nodes[i].get("namespace") in namespaces]
        if len(visible) == 1:
            return visible[0]
        self.warn(f"AMBIGUOUS {name} [{', '.join(sorted(candidates))}] @ {location}")
        return None

    def resolve_type_use(self, type_use, namespaces, location: str):
        """A written type use takes the id of its node; a collection keeps its generic name and its element takes
        the id."""
        if not type_use.is_collection:
            type_use.name = self.resolve(type_use.name, namespaces, location)
        if type_use.element is not None:
            type_use.element.name = self.resolve(type_use.element.name, namespaces, location)

    def add_edge(self, edge: Edge):
        """Every registration and exposure is its own fact (26 registrations of one generic loader are 26 edges);
        any other edge is one per (src, dst, rel, via, args, app_state). No end, or a loop, is no edge."""
        if not edge.src or not edge.dst or edge.src == edge.dst:
            return
        key = (edge.src, edge.dst, edge.rel, edge.via, edge.args, edge.app_state)
        if edge.rel in ("registers", "exposes"):
            key += (edge.source_location,)
        if key in self.edge_keys:
            return
        self.edge_keys.add(key)
        self.edges.append(edge)

    def warn(self, message: str):
        self.warnings.append(message)

    def to_document(self) -> dict:
        edges = []
        for edge in sorted(self.edges, key=lambda e: (e.src, e.rel, e.dst, e.via, e.source_location, e.args)):
            entry = {"src": edge.src, "dst": edge.dst, "rel": edge.rel, "via": edge.via}
            if edge.args:
                entry["args"] = list(edge.args)
            if edge.app_state:
                entry["app_state"] = edge.app_state
            if edge.collection:
                entry["collection"] = True
            entry["source_location"] = edge.source_location
            entry["confidence"] = edge.confidence
            edges.append(entry)
        return {
            "nodes": dict(sorted(self.nodes.items())),
            "edges": edges,
            "ecs_tables": self.ecs_tables,
            "tag_audit": sorted(self.tag_audit, key=lambda d: (d["kind"], d["archetype_or_owner"], d["source_location"])),
            "recipes": self.recipe_instances,
            "warnings": sorted(set(self.warnings)),
        }
