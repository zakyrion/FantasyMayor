#!/usr/bin/env python3
"""Generate INDEX.md from doc frontmatter.

INDEX.md is the agent's doc map and the single source of the start-reading list.
It is built in 2 passes: pass 1 = this script rebuilds the structural
skeleton BETWEEN the generated markers; pass 2 = the agent curates descriptions,
statuses, and context. Everything below the END marker is the agent zone and is
PRESERVED across runs. To change a skeleton description/status, edit the source doc's
first line / frontmatter, then run:

    python3 Tools/gen_index.py

Data comes from each project doc's YAML frontmatter (see DOC_STANDARD.md
"Frontmatter"): category, read (always|trigger|reference|archive), trigger, tags, status.
The one-line description is each doc's own first content line (no second source of truth).

Obsidian Canvas files (`.canvas`) are also catalogued in a "Canvas map" section.
Canvases have no frontmatter, so their entry is derived automatically: title =
filename, description = the canvas's group labels (add a group to give a canvas a
meaningful description).

A doc-lint pass runs after generation (DOC_STANDARD's checklist, mechanized):
broken relative .md/.canvas links (frontmatter + body, code fences skipped),
code_refs names that no longer resolve in Assets/**/*.cs (drift anchors),
and category-scoped field misuse (status / code_refs outside Category A).
Lint issues do NOT block the INDEX write; they are reported and exit code is 1.
"""
import json, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Directories that are third-party, generated, or tooling — never indexed.
PRUNE = {
    "Library", "Packages", "obj", "Logs", "GeneratedAssets",
    ".git", ".claude", ".cmaestro", ".ai", ".idea", ".vscode", ".plastic",
    ".sdd-flow", "skills", "Tools",
    os.path.join("Assets", "Plugins"), os.path.join("Assets", "ThirdParty"),
    os.path.join("Assets", "Packages"), os.path.join("Assets", "TextMesh Pro"),
    os.path.join("Assets", "TutorialInfo"),
}

READ_ORDER = {"always": 0, "trigger": 1, "reference": 2, "archive": 3}

# Pass-1 / pass-2 boundary. The script owns everything between the markers and rewrites
# it every run; the agent owns everything BELOW the END marker, which is preserved.
GEN_START = ("<!-- BEGIN GENERATED — Tools/gen_index.py rebuilds everything between these "
             "markers; edits here are overwritten -->")
GEN_END = ("<!-- END GENERATED — content below is the agent zone (pass 2), preserved across "
           "runs -->")

GUARDRAIL = (
    "> ⚠️ **Key file — the single entry point for all doc navigation. Keep it short and "
    "informative.** Built in 2 passes: (1) `python3 Tools/gen_index.py` "
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
            "status": None, "title": None, "desc": None, "_block": "", "_body": ""}
    body = txt
    if txt.startswith("---\n"):
        end = txt.find("\n---", 4)
        block = txt[4:end]
        body = txt[end + 4:]
        meta["_block"] = block
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
    meta["_body"] = body
    return meta


# ----------------------------------------------------------------------- doc lint
LINK_RE = re.compile(r"\[[^\]]*\]\(([^)\s]+?\.(?:md|canvas))(?:#[^)]*)?\)")
FENCE_RE = re.compile(r"```.*?```", re.S)


def _code_ref_names(block):
    """Bare symbol names from a frontmatter code_refs block (nested-by-kind lists)."""
    names, in_refs = [], False
    for ln in block.splitlines():
        if re.match(r"^code_refs:\s*$", ln):
            in_refs = True
            continue
        if in_refs:
            m = re.match(r"^\s+\w+:\s*\[(.*)\]\s*$", ln)
            if m:
                names += [n.strip() for n in m.group(1).split(",") if n.strip()]
            elif ln.strip() and not ln.startswith((" ", "\t")):
                in_refs = False
    return names


DESC_BUDGET_CHARS = 120   # first content line = the INDEX description

# No body-line budget. It only ever applied to Category A (= Flows), and Flows are exempt by
# decision (user, 2026-07-17 — DOC_STANDARD → Size budgets): a flow contract is sized by the
# behavior it owns, not by a line count. The description budget below is unrelated and stays —
# that line IS the INDEX entry, so its limit is a layout fact, not a size opinion.


def lint_budgets(docs):
    """Description budget only. WARN-level: reported, never blocks."""
    warns = []
    for p, m in docs:
        if len(m["desc"]) > DESC_BUDGET_CHARS:
            warns.append(f"{p}: first content line {len(m['desc'])} chars > {DESC_BUDGET_CHARS}")
    return warns


def lint(docs):
    """DOC_STANDARD's checklist, mechanized. Returns a list of issue strings."""
    issues = []
    # one-time corpus of game source for code_refs drift anchors
    blob = "\n".join(
        open(os.path.join(ROOT, p), encoding="utf-8", errors="ignore").read()
        for p in _walk_files() if p.startswith("Assets") and p.endswith(".cs"))
    for p, m in docs:
        d = os.path.dirname(p)
        normalized = p.replace(os.sep, "/")
        # broken relative links — frontmatter `related` + body (code fences skipped:
        # examples inside fences are illustrations, not live links)
        targets = LINK_RE.findall(m["_block"]) + LINK_RE.findall(FENCE_RE.sub("", m["_body"]))
        for t in targets:
            if t.startswith(("http://", "https://")):
                continue
            here = os.path.normpath(os.path.join(d, t))       # doc-relative (standard)
            root = os.path.normpath(t)                         # vault-root-relative (tolerated)
            if not os.path.exists(os.path.join(ROOT, here)) and \
               not os.path.exists(os.path.join(ROOT, root)):
                issues.append(f"{p}: broken link → {t}")
        # category-scoped fields
        if m["category"] == "A":
            is_flow = (normalized.startswith("Flows/FLOW_") or
                       normalized.startswith("Flows/Archive/FLOW_"))
            if not m["status"]:
                issues.append(f"{p}: Category A doc missing status")
            elif m["status"] not in ("partial", "implemented"):
                issues.append(f"{p}: bad Category A status {m['status']!r}")
            if is_flow:
                headings = ["# 1 · Request", "# 2 · Contract", "# 3 · Plan"]
                positions = [m["_body"].find(h) for h in headings]
                if any(i == -1 for i in positions) or positions != sorted(positions):
                    issues.append(f"{p}: Category A FLOW needs Request → Contract → Plan sections in order")
                if m["status"] == "partial" and m["read"] != "always":
                    issues.append(f"{p}: active partial FLOW must be read: always")
                if m["read"] == "always" and m["status"] != "partial":
                    issues.append(f"{p}: implemented FLOW cannot stay in the session-start set")
            if m["read"] == "archive":
                # FLOW_* = archived task flows; RESEARCH_* = archived deep-research maps (DOC_STANDARD research-map genre)
                if not (normalized.startswith("Flows/Archive/FLOW_") or
                        normalized.startswith("Flows/Archive/RESEARCH_")):
                    issues.append(f"{p}: read: archive requires Flows/Archive/FLOW_*.md or RESEARCH_*.md")
                if m["status"] != "implemented":
                    issues.append(f"{p}: archived FLOW must be implemented")
                if re.search(r"^code_refs:", m["_block"], re.M):
                    issues.append(f"{p}: archived FLOW must not carry current-code code_refs")
            elif normalized.startswith("Flows/Archive/"):
                issues.append(f"{p}: a FLOW under Flows/Archive/ must use read: archive")
        else:
            if m["read"] == "archive":
                issues.append(f"{p}: read: archive is Category A only")
            if m["status"]:
                issues.append(f"{p}: status is Category A only (category {m['category']})")
            if re.search(r"^code_refs:", m["_block"], re.M):
                issues.append(f"{p}: code_refs is Category A only (category {m['category']})")
        # code_refs drift anchors — every name must still exist in game source
        for name in _code_ref_names(m["_block"]):
            if not re.search(rf"\b{re.escape(name)}\b", blob):
                issues.append(f"{p}: code_refs name no longer resolves → {name}")
    return issues


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
    archive = sorted([(p, m) for p, m in docs if m["read"] == "archive"],
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
             f"{len(trigger)} trigger · {len(ref)} reference · {len(archive)} archive"
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
    L.append("Reference docs read on demand.")
    L.append("")
    L.append("| Doc | Cat | Status | What it is |")
    L.append("|---|---|---|---|")
    for p, m in ref:
        L.append(f"| [{m['title']}]({p}) | {m['category']} | "
                 f"{m['status'] or '—'} | {m['desc']} |")
    L.append("")

    L.append("## Task history (archive)")
    L.append("")
    L.append(f"{len(archive)} completed FLOW document(s) are retained under "
             "`Flows/Archive/`. They preserve task history and are searched on demand; "
             "they are not startup context or current-code claims.")
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
          f"({len(always)} always, {len(trigger)} trigger, {len(ref)} reference, "
          f"{len(archive)} archive), "
          f"{len(canvases)} canvas")

    # doc lint — INDEX is already written; issues are doc drift to fix in the docs
    warns = lint_budgets(docs)
    if warns:
        sys.stderr.write(f"BUDGET: {len(warns)} over-budget doc(s) (warn-only):\n  "
                         + "\n  ".join(warns) + "\n")
    issues = lint(docs)
    if issues:
        sys.stderr.write(f"LINT: {len(issues)} issue(s) — INDEX itself is written; "
                         "fix the docs:\n  " + "\n  ".join(issues) + "\n")
        sys.exit(1)
    print("LINT: clean")


if __name__ == "__main__":
    main()
