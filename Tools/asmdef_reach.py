#!/usr/bin/env python3
"""asmdef reachability — can assembly A reference type T?

Parses every *.asmdef under Assets/ into an assembly reference graph (name + GUID refs resolved),
and maps each type to its owning assembly (nearest ancestor .asmdef), to answer the layering
questions roslyn / ecs-graph / di-graph do NOT:

    python3 Tools/asmdef_reach.py can <Assembly> <Type>        # can <Assembly> use <Type>? + shortest ref path
    python3 Tools/asmdef_reach.py path <AsmA> <AsmB>           # shortest reference path AsmA -> AsmB
    python3 Tools/asmdef_reach.py refs <Assembly>              # direct + transitive references
    python3 Tools/asmdef_reach.py assembly-of <Type>          # which assembly(ies) define <Type>

Rule: A can use T iff T's assembly is A itself or in A's transitive reference closure. Scope is
Assets/ assemblies only (package/engine assemblies referenced by name appear as leaf targets).
"""
from __future__ import annotations

import json
import re
import sys
from collections import defaultdict, deque
from pathlib import Path

GUID_RE = re.compile(r"guid:\s*([0-9a-fA-F]{32})")


def find_root(start: Path) -> Path:
    p = start.resolve()
    for c in [p, *p.parents]:
        if (c / "Assets").is_dir():
            return c
    sys.exit("asmdef_reach: no Assets/ found above CWD.")


def load_asmdefs(root: Path):
    """Return (graph, dirs). graph: name -> set(referenced assembly names, GUIDs resolved).
    dirs: [(dir, name)] sorted deepest-first so the nearest ancestor asmdef owns a given .cs."""
    guid2name = {}
    raw = {}   # name -> refs list
    dirs = []
    for a in (root / "Assets").rglob("*.asmdef"):
        try:
            d = json.loads(a.read_text("utf-8"))
        except Exception:
            continue
        name = d.get("name")
        if not name:
            continue
        raw[name] = d.get("references", []) or []
        dirs.append((a.parent, name))
        meta = Path(str(a) + ".meta")
        if meta.exists():
            m = GUID_RE.search(meta.read_text("utf-8", errors="ignore"))
            if m:
                guid2name[m.group(1).lower()] = name
    graph = defaultdict(set)
    for name, refs in raw.items():
        graph[name]  # ensure the node exists even with no refs
        for r in refs:
            if r.startswith("GUID:"):
                r = guid2name.get(r[5:].lower())
            if r:
                graph[name].add(r)
    dirs.sort(key=lambda t: len(str(t[0])), reverse=True)
    return graph, dirs


def assembly_of_path(path: Path, dirs):
    for d, name in dirs:            # deepest-first: nearest ancestor asmdef wins
        try:
            path.relative_to(d)
            return name
        except ValueError:
            continue
    return None


def assemblies_defining(root: Path, typ: str, dirs):
    rx = re.compile(r"\b(?:class|struct|interface|enum)\s+" + re.escape(typ) + r"\b")
    hits = set()
    for cs in (root / "Assets").rglob("*.cs"):
        try:
            if rx.search(cs.read_text("utf-8", errors="ignore")):
                asm = assembly_of_path(cs, dirs)
                if asm:
                    hits.add(asm)
        except Exception:
            continue
    return hits


def closure(graph, start):
    seen = {start}
    q = deque([start])
    while q:
        for nxt in graph.get(q.popleft(), ()):
            if nxt not in seen:
                seen.add(nxt)
                q.append(nxt)
    return seen


def shortest_path(graph, a, b):
    if a == b:
        return [a]
    seen = {a}
    q = deque([(a, [a])])
    while q:
        cur, path = q.popleft()
        for nxt in sorted(graph.get(cur, ())):
            if nxt in seen:
                continue
            if nxt == b:
                return path + [nxt]
            seen.add(nxt)
            q.append((nxt, path + [nxt]))
    return None


def main():
    args = sys.argv[1:]
    if not args:
        sys.exit(__doc__)
    root = find_root(Path("."))
    graph, dirs = load_asmdefs(root)
    known = set(graph) | {n for _, n in dirs}
    cmd = args[0]

    if cmd == "refs" and len(args) == 2:
        asm = args[1]
        if asm not in known:
            sys.exit(f"asmdef_reach: unknown assembly '{asm}'.")
        direct = sorted(graph.get(asm, set()))
        trans = sorted(closure(graph, asm) - {asm})
        print(f"{asm} — direct references ({len(direct)}): {', '.join(direct) or '(none)'}")
        print(f"{asm} — transitive closure ({len(trans)}): {', '.join(trans) or '(none)'}")
        return

    if cmd == "path" and len(args) == 3:
        p = shortest_path(graph, args[1], args[2])
        print(" -> ".join(p) if p else f"NOT reachable: {args[1]} cannot reference {args[2]}.")
        return

    if cmd == "assembly-of" and len(args) == 2:
        hits = assemblies_defining(root, args[1], dirs)
        print(f"{args[1]} defined in: {', '.join(sorted(hits)) or '(not found under Assets/)'}")
        return

    if cmd == "can" and len(args) == 3:
        asm, typ = args[1], args[2]
        if asm not in known:
            sys.exit(f"asmdef_reach: unknown assembly '{asm}'.")
        hits = assemblies_defining(root, typ, dirs)
        if not hits:
            sys.exit(f"asmdef_reach: type '{typ}' not found under Assets/.")
        reach = closure(graph, asm)
        visible = [h for h in sorted(hits) if h in reach]
        if visible:
            for h in visible:
                print(f"YES — {asm} can use {typ} (in {h}) via {' -> '.join(shortest_path(graph, asm, h))}")
        else:
            print(f"NO — {asm} cannot use {typ} (in {', '.join(sorted(hits))}); no reference path.")
        return

    sys.exit(__doc__)


if __name__ == "__main__":
    main()
