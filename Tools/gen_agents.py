#!/usr/bin/env python3
"""gen_agents — deterministic canon sync: CLAUDE.md SHARED blocks → AGENTS.md GENERATED zones.

CLAUDE.md is the semantic source of the dual-agent process contract. Every block wrapped in
    <!-- BEGIN SHARED: <block-id> -->
    ...canon content...
    <!-- END SHARED: <block-id> -->
is copied VERBATIM into the AGENTS.md zone wrapped in
    <!-- BEGIN GENERATED: <block-id> (Tools/gen_agents.py — never edit inside) -->
    ...generated content...
    <!-- END GENERATED: <block-id> -->
Text outside the zones is agent-specific and never touched.

Usage:
  python3 Tools/gen_agents.py          # build: rewrite every GENERATED zone from canon
  python3 Tools/gen_agents.py --check  # process-lint: report drifted zones, exit 1

Fail-loud: duplicate ids, unclosed/nested markers, or a block-id present on one side only
abort with exit 2 — a half-marked canon is worse than none.
"""

import argparse
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
CANON = REPO / "CLAUDE.md"
DERIVED = REPO / "AGENTS.md"

SHARED_BEGIN = re.compile(r"^<!-- BEGIN SHARED: ([a-z0-9-]+) -->\s*$")
SHARED_END = re.compile(r"^<!-- END SHARED: ([a-z0-9-]+) -->\s*$")
GEN_BEGIN = re.compile(r"^<!-- BEGIN GENERATED: ([a-z0-9-]+)\b.*-->\s*$")
GEN_END = re.compile(r"^<!-- END GENERATED: ([a-z0-9-]+) -->\s*$")


def fail(message: str):
    print(f"gen_agents: {message}", file=sys.stderr)
    sys.exit(2)


def extract(lines, begin_re, end_re, name):
    """Return {block-id: (content_start, end_marker_index, content_lines)}."""
    blocks = {}
    open_id, content_start = None, None
    for i, line in enumerate(lines):
        begin, end = begin_re.match(line), end_re.match(line)
        if begin:
            if open_id is not None:
                fail(f"{name}: BEGIN {begin.group(1)!r} inside unclosed block {open_id!r}")
            open_id, content_start = begin.group(1), i + 1
            if open_id in blocks:
                fail(f"{name}: duplicate block id {open_id!r}")
        elif end:
            if open_id is None or end.group(1) != open_id:
                fail(f"{name}: END {end.group(1)!r} without matching BEGIN")
            blocks[open_id] = (content_start, i, lines[content_start:i])
            open_id = None
    if open_id is not None:
        fail(f"{name}: unclosed block {open_id!r}")
    return blocks


def main():
    ap = argparse.ArgumentParser(description="Sync AGENTS.md GENERATED zones from CLAUDE.md canon")
    ap.add_argument("--check", action="store_true", help="report drift and exit 1; write nothing")
    args = ap.parse_args()

    canon_lines = CANON.read_text(encoding="utf-8").splitlines()
    derived_lines = DERIVED.read_text(encoding="utf-8").splitlines()
    shared = extract(canon_lines, SHARED_BEGIN, SHARED_END, "CLAUDE.md")
    zones = extract(derived_lines, GEN_BEGIN, GEN_END, "AGENTS.md")

    canon_only = sorted(set(shared) - set(zones))
    derived_only = sorted(set(zones) - set(shared))
    if canon_only or derived_only:
        fail(f"block sets differ — canon-only: {canon_only or '—'}; derived-only: {derived_only or '—'}")

    drifted = [bid for bid in sorted(shared) if shared[bid][2] != zones[bid][2]]

    if args.check:
        if drifted:
            for bid in drifted:
                print(f"gen_agents: DRIFT in zone {bid!r} — AGENTS.md differs from canon")
            print(f"gen_agents: {len(drifted)} of {len(shared)} zone(s) drifted · rebuild: python3 Tools/gen_agents.py")
            sys.exit(1)
        print(f"gen_agents: {len(shared)} zone(s) in sync with CLAUDE.md canon")
        return

    if not drifted:
        print(f"gen_agents: {len(shared)} zone(s) already in sync")
        return

    # Rebuild: splice canon content into each zone, jumping from content start to END marker.
    replacements = {zones[bid][0]: (zones[bid][1], shared[bid][2]) for bid in shared}
    rebuilt, i = [], 0
    while i < len(derived_lines):
        if i in replacements:
            end_index, content = replacements.pop(i)
            rebuilt.extend(content)
            i = end_index  # the END marker line is appended by the normal path
            continue
        rebuilt.append(derived_lines[i])
        i += 1
    DERIVED.write_text("\n".join(rebuilt) + "\n", encoding="utf-8")
    print(f"gen_agents: rebuilt {len(drifted)} zone(s): {', '.join(drifted)}")


if __name__ == "__main__":
    main()
