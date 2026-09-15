#!/usr/bin/env python3
"""fantasymayor-graph — ONE CLI over ONE graph of FantasyMayor's code: ECS, DI, roles, markers, recipe instances.

Every command answers from a fresh graph: a stale one is rebuilt first (a full pass, well under a second); a build
that fails ends the command with its traceback — never an answer from the old graph.

    python3 fmgraph.py [--path DIR] <command> …      commands: see COMMANDS in queries.py and SKILL.md
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from build import build_graph
from graph_store import find_project_root, is_graph_fresh, load_graph
from queries import COMMANDS
from recipes import RECIPE_NAMES


def main():
    request = parse_request(sys.argv[1:])
    root = find_project_root(Path(request.path))
    fresh = is_graph_fresh(root)
    refresh_graph(root, fresh)
    graph = load_graph(root)
    answer_request(graph, request)


def parse_request(argv) -> argparse.Namespace:
    parser = argparse.ArgumentParser(prog="fmgraph.py", description="fantasymayor-graph — one graph, one CLI")
    parser.add_argument("--path", default=".", help="start dir; the project root is the first dir with Assets/ above it")
    commands = parser.add_subparsers(dest="command", required=True)
    for name in COMMANDS:
        command = commands.add_parser(name)
        if name == "pattern":
            command.add_argument("recipe", choices=RECIPE_NAMES)
        elif name == "search":
            command.add_argument("keyword")
        elif name in ("explain", "neighbors", "bfs", "resolve", "consumers", "installer", "state"):
            command.add_argument("node")
        if name in ("systems", "search"):
            command.add_argument("--role", default="", help="filter by role")
        if name in ("neighbors", "bfs"):
            command.add_argument("--rel", default="", help="comma-separated relations")
        if name == "bfs":
            command.add_argument("--depth", type=int, default=2)
            command.add_argument("--in", dest="incoming", action="store_true", help="traverse incoming edges")
    return parser.parse_args(argv)


def refresh_graph(root: Path, fresh: bool):
    if not fresh:
        build_graph(root)
        print("fantasymayor-graph: stale -> rebuilt", file=sys.stderr)


def answer_request(graph, request):
    COMMANDS[request.command](graph, request)


if __name__ == "__main__":
    main()
