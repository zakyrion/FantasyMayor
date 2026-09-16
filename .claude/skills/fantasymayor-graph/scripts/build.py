"""build_graph — ONE deterministic pass over the game code that writes the whole graph: ECS, DI, ancestry, markers,
roles, view pairs, the tag audit and the recipe instances. No LLM, no incremental mode, no second step."""
from __future__ import annotations

from pathlib import Path

from di_facts import connect_hosts, reconcile_di_facts
from ecs_facts import expand_archetypes, reconcile_ecs_facts
from graph_draft import GraphDraft
from graph_store import write_graph
from recipes import find_recipe_instances
from roles import decide_roles
from source_reading import read_sources
from tag_law import audit_tag_law
from type_facts import know_types
from view_pairs import pair_view_subscribers

SCAN_ROOTS = ["Assets/Domains", "Assets/Presentation", "Assets/Modules", "Assets/Scripts", "Assets/Flows"]


def build_graph(root: Path) -> dict:
    source_files = list_source_files(root)
    sources = read_sources(root, source_files)
    draft = GraphDraft(sources.parse_warnings)
    types = know_types(sources, draft)
    expand_archetypes(sources, types, draft)
    reconcile_ecs_facts(sources, draft)
    reconcile_di_facts(sources, draft)
    connect_hosts(sources, draft)
    pair_view_subscribers(sources, types, draft)
    audit_tag_law(types, draft)
    decide_roles(sources, types, draft)
    find_recipe_instances(sources, types, draft)
    graph_doc = write_graph(root, source_files, draft)
    return graph_doc


def list_source_files(root: Path) -> list[Path]:
    return sorted(path for scan_root in SCAN_ROOTS if (root / scan_root).is_dir()
                  for path in (root / scan_root).rglob("*.cs"))
