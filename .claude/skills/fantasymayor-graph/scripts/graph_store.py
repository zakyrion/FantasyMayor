"""Where the graph lives and whether it can be read: the project root, the freshness of graph.json, writing it,
and loading it with both directions of every edge."""
from __future__ import annotations

import json
import sys
import time
from collections import defaultdict
from pathlib import Path

ARTIFACT_DIR = ".fantasymayor-graph"
SCHEMA = 1
SCRIPTS_DIR = Path(__file__).resolve().parent


def find_project_root(start: Path) -> Path:
    for candidate in [start.resolve(), *start.resolve().parents]:
        if (candidate / "Assets").is_dir():
            return candidate
    sys.exit("fantasymayor-graph: no project root — no Assets/ folder above the start directory.")


def is_graph_fresh(root: Path) -> bool:
    """Fresh only when graph.json exists at the current schema, the .cs count under its roots is the same, and no .cs
    and no script of this skill is newer than it."""
    graph_path = root / ARTIFACT_DIR / "graph.json"
    if not graph_path.exists():
        return False
    meta = json.loads(graph_path.read_text("utf-8")).get("meta", {})
    source_files = [f for scan_root in meta.get("roots", []) for f in (root / scan_root).rglob("*.cs")]
    built_at = graph_path.stat().st_mtime_ns
    return (meta.get("schema") == SCHEMA
            and meta.get("files") == len(source_files)
            and all(f.stat().st_mtime_ns <= built_at for f in source_files)
            and all(s.stat().st_mtime_ns <= built_at for s in SCRIPTS_DIR.glob("*.py")))


def write_graph(root: Path, source_files, draft) -> dict:
    graph_doc = {"meta": {"schema": SCHEMA,
                          "roots": sorted({"/".join(f.relative_to(root).parts[:2]) for f in source_files}),
                          "files": len(source_files),
                          "generated": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
                          "curated": True},
                 **draft.to_document()}
    graph_path = root / ARTIFACT_DIR / "graph.json"
    graph_path.parent.mkdir(exist_ok=True)
    graph_path.write_text(json.dumps(graph_doc, indent=2, ensure_ascii=False), "utf-8")
    return graph_doc


class LoadedGraph:
    def __init__(self, root: Path, doc: dict):
        self.root = root
        self.meta = doc["meta"]
        self.nodes = doc["nodes"]
        self.edges = doc["edges"]
        self.ecs_tables = doc["ecs_tables"]
        self.tag_audit = doc["tag_audit"]
        self.recipe_instances = doc["recipes"]
        self.warnings = doc["warnings"]
        self.outgoing, self.incoming = defaultdict(list), defaultdict(list)
        for edge in self.edges:
            self.outgoing[edge["src"]].append(edge)
            self.incoming[edge["dst"]].append(edge)

    def find_node(self, query: str) -> str:
        """An id, else the one node named exactly so (any case), else the one node whose name contains the query.
        No match and several matches end the command with code 1."""
        if query in self.nodes:
            return query
        lowered = query.lower()
        exact = [i for i, n in self.nodes.items() if n.get("name", i).lower() == lowered]
        if len(exact) == 1:
            return exact[0]
        containing = [i for i, n in self.nodes.items() if lowered in n.get("name", i).lower()]
        if len(containing) == 1:
            return containing[0]
        if not containing:
            sys.exit(f"fantasymayor-graph: no node matches '{query}'.")
        candidates = "\n".join(f"  {i}  [{self.nodes[i].get('kind')}]" for i in sorted(exact or containing)[:25])
        sys.exit(f"fantasymayor-graph: '{query}' is ambiguous — candidates:\n{candidates}")


def load_graph(root: Path) -> LoadedGraph:
    doc = json.loads((root / ARTIFACT_DIR / "graph.json").read_text("utf-8"))
    return LoadedGraph(root, doc)
