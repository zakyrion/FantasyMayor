#!/usr/bin/env python3
"""Generate INDEX.md from doc frontmatter.

INDEX.md is the agent's doc map and the single source of the start-reading list.
It is GENERATED — never hand-edit it. Edit the source docs' frontmatter, then run:

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
    L.append("Generated doc map for FantasyMayor. **Do not hand-edit** — run "
             "`python3 Tools/gen_index.py` after changing any doc's frontmatter. "
             "Data source: each doc's frontmatter (`category`/`read`/`trigger`/`status`) "
             "and its first line. See `DOC_STANDARD.md`.")
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

    out = "\n".join(L) + "\n"
    open(os.path.join(ROOT, "INDEX.md"), "w", encoding="utf-8").write(out)
    print(f"INDEX.md written: {len(docs)} docs "
          f"({len(always)} always, {len(trigger)} trigger, {len(ref)} reference), "
          f"{len(canvases)} canvas")


if __name__ == "__main__":
    main()
