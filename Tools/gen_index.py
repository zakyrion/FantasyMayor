#!/usr/bin/env python3
"""Generate INDEX.md from doc frontmatter.

INDEX.md is the agent's doc map and the single source of the start-reading list.
It is built in 2 passes (like graphify): pass 1 = this script rebuilds the structural
skeleton BETWEEN the generated markers; pass 2 = the agent curates descriptions,
statuses, and context. Everything below the END marker is the agent zone and is
PRESERVED across runs. To change a skeleton description/status, edit the source doc's
first line / frontmatter, then run:

    python3 Tools/gen_index.py

Data comes from each project doc's YAML frontmatter (see DOC_STANDARD.md
"Frontmatter"): category, read (always|trigger|reference), trigger, tags, status.
The one-line description is each doc's own first content line (no second source of truth).

Obsidian Canvas files (`.canvas`) are also catalogued in a "Canvas map" section.
Canvases have no frontmatter, so their entry is derived automatically: title =
filename, description = the canvas's group labels (add a group to give a canvas a
meaningful description).
"""
import json, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Directories that are third-party, generated, or tooling — never indexed.
PRUNE = {
    "Library", "Packages", "obj", "Logs", "graphify-out", "GeneratedAssets",
    ".git", ".claude", ".cmaestro", ".ai", ".idea", ".vscode", ".plastic",
    "skills", "Tools",
    os.path.join("Assets", "Plugins"), os.path.join("Assets", "ThirdParty"),
    os.path.join("Assets", "Packages"), os.path.join("Assets", "TextMesh Pro"),
    os.path.join("Assets", "TutorialInfo"),
}

READ_ORDER = {"always": 0, "trigger": 1, "reference": 2}

# Pass-1 / pass-2 boundary. The script owns everything between the markers and rewrites
# it every run; the agent owns everything BELOW the END marker, which is preserved.
GEN_START = ("<!-- BEGIN GENERATED — Tools/gen_index.py rebuilds everything between these "
             "markers; edits here are overwritten -->")
GEN_END = ("<!-- END GENERATED — content below is the agent zone (pass 2), preserved across "
           "runs -->")

GUARDRAIL = (
    "> ⚠️ **Key file — the single entry point for all doc navigation. Keep it short and "
    "informative.** Built in 2 passes (like graphify): (1) `python3 Tools/gen_index.py` "
    "rebuilds the skeleton between the markers from each doc's frontmatter + first line; "
    "(2) the agent curates descriptions, statuses, and context. To change a description or "
    "status, edit the doc's first line / `status` frontmatter and re-run pass 1 — do not edit "
    "between the markers. The agent zone below the END marker is preserved across runs."
)

DEFAULT_AGENT_ZONE = (
    "## Context & Notes (agent-maintained — pass 2)\n"
    "\n"
    "Curate what the script can't derive: current focus, stale docs, cross-doc orientation. "
    "Keep it short. Preserved across `gen_index.py` runs.\n"
    "\n"
    "_None yet._\n"
)


def _walk_files():
    """Yield repo-relative paths of all files outside the PRUNE set."""
    for dirpath, dirnames, filenames in os.walk(ROOT):
        rel = os.path.relpath(dirpath, ROOT)
        # prune
        dirnames[:] = [d for d in dirnames
                       if os.path.normpath(os.path.join(rel, d)) not in PRUNE
                       and d not in PRUNE]
        for fn in filenames:
            yield os.path.normpath(os.path.join(rel, fn))


def find_docs():
    return sorted(p for p in _walk_files()
                  if p.endswith(".md") and os.path.basename(p) != "INDEX.md")


def find_canvases():
    return sorted(p for p in _walk_files() if p.endswith(".canvas"))


def parse(relpath):
    txt = open(os.path.join(ROOT, relpath), encoding="utf-8").read()
    meta = {"category": None, "read": None, "trigger": None, "tags": [],
            "status": None, "title": None, "desc": None}
    body = txt
    if txt.startswith("---\n"):
        end = txt.find("\n---", 4)
        block = txt[4:end]
        body = txt[end + 4:]
        for key in ("category", "read", "status"):
            m = re.search(rf'^{key}:\s*(.+?)\s*$', block, re.M)
            if m:
                meta[key] = m.group(1).strip().strip('"')
        mt = re.search(r'^trigger:\s*"?(.+?)"?\s*$', block, re.M)
        if mt:
            meta["trigger"] = mt.group(1).strip()
        mtags = re.search(r'^tags:\s*\[(.*?)\]\s*$', block, re.M)
        if mtags:
            meta["tags"] = [t.strip() for t in mtags.group(1).split(",") if t.strip()]
    # title = first H1; desc = first real prose line (skips headings, fences & their
    # contents, blockquotes, lists, tables). desc is captured independently of title.
    in_fence = False
    for ln in body.splitlines():
        s = ln.strip()
        if s.startswith("```"):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        if not meta["title"] and s.startswith("# "):
            meta["title"] = s[2:].strip()
            continue
        if meta["desc"] is None and s and not s.startswith(("#", ">", "|", "- ", "* ")):
            meta["desc"] = s
    if not meta["title"]:
        meta["title"] = os.path.basename(relpath)
    if meta["desc"] is None:
        meta["desc"] = ""
    return meta


def parse_canvas(relpath):
    """Derive a catalog entry for an Obsidian Canvas (JSONCanvas).

    Canvases carry no frontmatter, so this is best-effort and tolerant: a
    malformed/in-flight canvas degrades to a placeholder instead of aborting.
    title = filename; desc = group labels, else first text node, else placeholder.
    """
    title = os.path.basename(relpath)[:-len(".canvas")]
    try:
        data = json.load(open(os.path.join(ROOT, relpath), encoding="utf-8"))
        nodes = data.get("nodes", [])
        labels = [n.get("label", "").strip() for n in nodes
                  if n.get("type") == "group" and n.get("label", "").strip()]
        if labels:
            desc = " · ".join(labels)
        else:
            desc = "(canvas)"
            for n in nodes:
                if n.get("type") == "text":
                    lines = n.get("text", "").strip().splitlines()
                    if lines and lines[0].strip():
                        desc = lines[0].strip()
                        break
        if len(desc) > 120:
            desc = desc[:117].rstrip() + "…"
    except Exception:
        desc = "(unparsed canvas)"
    # escape pipes so free-form labels never break the markdown table
    desc = desc.replace("|", "\\|")
    return {"title": title, "desc": desc}


def main():
    docs = [(p, parse(p)) for p in find_docs()]
    canvases = [(p, parse_canvas(p)) for p in find_canvases()]
    # validate (markdown docs only — canvases have no schema to validate)
    errs = []
    for p, m in docs:
        if m["category"] not in ("A", "B", "C"):
            errs.append(f"{p}: bad category {m['category']!r}")
        if m["read"] not in READ_ORDER:
            errs.append(f"{p}: bad read {m['read']!r}")
        if m["read"] == "trigger" and not m["trigger"]:
            errs.append(f"{p}: read:trigger but no trigger text")
        if m["read"] != "trigger" and m["trigger"]:
            errs.append(f"{p}: trigger set but read is {m['read']}")
    if errs:
        sys.stderr.write("INDEX generation aborted:\n  " + "\n  ".join(errs) + "\n")
        sys.exit(1)

    always = sorted([(p, m) for p, m in docs if m["read"] == "always"],
                    key=lambda x: x[0])
    trigger = sorted([(p, m) for p, m in docs if m["read"] == "trigger"],
                     key=lambda x: x[0])
    ref = sorted([(p, m) for p, m in docs if m["read"] == "reference"],
                 key=lambda x: x[0])

    L = []
    L.append("---")
    L.append("category: C")
    L.append("read: always")
    L.append("tags: [index, navigation]")
    L.append("related:")
    L.append('  - "[DOC_STANDARD](DOC_STANDARD.md)"')
    L.append('  - "[ARCHITECTURE](ARCHITECTURE.md)"')
    L.append("---")
    L.append("")
    L.append("# INDEX")
    L.append("")
    L.append(GUARDRAIL)
    L.append("")
    L.append(GEN_START)
    L.append("")
    L.append(f"Totals: {len(docs)} docs — {len(always)} always · "
             f"{len(trigger)} trigger · {len(ref)} reference"
             f" · {len(canvases)} canvas.")
    L.append("")

    L.append("## Read at start (always)")
    L.append("")
    L.append("Read these every session before doing anything else.")
    L.append("")
    for p, m in always:
        L.append(f"- [{m['title']}]({p}) — {m['desc']}")
    L.append("")

    L.append("## Read on demand (by trigger)")
    L.append("")
    L.append("Do **not** preload. Read only when the trigger condition holds.")
    L.append("")
    L.append("| Doc | Read it… | What it is |")
    L.append("|---|---|---|")
    for p, m in trigger:
        L.append(f"| [{m['title']}]({p}) | {m['trigger']} | {m['desc']} |")
    L.append("")

    L.append("## Reference map (on demand)")
    L.append("")
    L.append("Per-module navigation docs. `status` mirrors each module's `## Current State`.")
    L.append("")
    L.append("| Doc | Cat | Status | What it is |")
    L.append("|---|---|---|---|")
    for p, m in ref:
        L.append(f"| [{m['title']}]({p}) | {m['category']} | "
                 f"{m['status'] or '—'} | {m['desc']} |")
    L.append("")

    L.append("## Canvas map (on demand)")
    L.append("")
    L.append("Visual maps (Obsidian Canvas). Read/edit via Obsidian MCP; not preloaded.")
    L.append("")
    L.append("| Canvas | What it maps |")
    L.append("|---|---|")
    for p, m in canvases:
        L.append(f"| [{m['title']}]({p}) | {m['desc']} |")
    L.append("")
    L.append(GEN_END)

    # Preserve the agent zone (everything below the previous END marker) across runs.
    index_path = os.path.join(ROOT, "INDEX.md")
    existing_tail = ""
    if os.path.exists(index_path):
        old = open(index_path, encoding="utf-8").read()
        i = old.find(GEN_END)
        if i != -1:
            existing_tail = old[i + len(GEN_END):]

    out = "\n".join(L)
    out += existing_tail if existing_tail.strip() else "\n\n" + DEFAULT_AGENT_ZONE
    if not out.endswith("\n"):
        out += "\n"
    open(index_path, "w", encoding="utf-8").write(out)
    print(f"INDEX.md written: {len(docs)} docs "
          f"({len(always)} always, {len(trigger)} trigger, {len(ref)} reference), "
          f"{len(canvases)} canvas")


if __name__ == "__main__":
    main()
