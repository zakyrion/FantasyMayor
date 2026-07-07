#!/usr/bin/env python3
"""PreToolUse gate — forbids the MAIN agent from BUILDING/CURATING the DI graph and from reaching into
either graph dir ad-hoc, and turns any main-agent edit of a FROZEN policy doc (ARCHITECTURE.md) into a
user-approval ASK.

The ecs-graph is DETERMINISTIC now: `build_graph.py` extracts AND curates (`curate()`) in one call, so the
main agent MAY run it directly. The di-graph build (`build_di_graph.py`) + its STEP-2 AI curation still
belong to the **docs-curator** subagent (CLAUDE.md → Doc Curation). Writing/reading the `.ecs-graph/` /
`.di-graph/` artifacts ad-hoc stays denied — query through the read-only CLIs `ecsg.py` / `dig.py`.

This closes the Bash bypass that a Write/Edit-only ban misses: a redirect, an inline
`python3 -c "... json.dump(open('.ecs-graph/graph.json','w'))"`, `tee`, `sed -i`, `cp`/`mv`,
or `cat`/`jq` reaching into the graph dirs. It also denies the main agent the STEP-2 curation
checklists (`references/ecs-patterns.md` / `di-patterns.md`) — the curator's charter.

Applies ONLY to the main agent; subagents (docs-curator, scouts) are exempt — they ARE the
build/curation path. Reads the hook JSON from stdin; prints a deny decision (or nothing =
allow) and exits 0. Fail-open: any internal error → allow, so the gate can never wedge the
main loop.

Known limit (honest): a hook sees the command string, not the contents of an external script
it runs. `python3 /some/where/curate.py` whose write logic is hidden inside the file and never
names a graph dir on the command line is NOT caught here — that residual is covered by the
checklist-read ban above and the standing behavioral rule (delegate graph STEP-2 to docs-curator).
"""
import json
import re
import sys

GRAPH_DIR_RE = re.compile(r"\.(?:ecs|di)-graph\b")   # matches .ecs-graph / .di-graph anywhere in a path/command
BUILD_EXES = {"build_di_graph.py"}                    # di-graph build+STEP-2 is still the docs-curator's LLM job
# ecs-graph is DETERMINISTIC now (build_graph.py = extract + curate() in one call), so the MAIN agent may run
# it directly, alongside the read-only query CLIs.
QUERY_EXES = {"ecsg.py", "dig.py", "build_graph.py"}
# Policy-frozen docs (status: frozen): an agent edit becomes a user-approval ASK, not a silent write.
FROZEN_DOCS = ("ARCHITECTURE.md",)
BASH_WRITE_EXES = {"sed", "tee", "cp", "mv", "rm", "truncate"}  # write-capable bash vectors onto a frozen doc
FROZEN_ASK = ("ARCHITECTURE.md is FROZEN policy (see its header banner) — agents do not edit it. "
              "If the user explicitly ordered this policy change, they can approve this prompt; "
              "otherwise flag the needed change back to the user instead of editing.")
# wrappers to skip when finding a pipeline segment's real executable (mirrors search-gate.py)
WRAPPERS = {"python", "python3", "uv", "run", "time", "nice", "env", "sudo", "command", "exec", "xargs"}
# di-graph STEP-2 checklist — reading it is the curator's job. (ecs-patterns.md is now IMPLEMENTED in
# build_graph.py curate(), so it is a plain readable spec, no longer gated.)
CHECKLISTS = ("skills/di-graph/references/di-patterns.md",)

DELEGATE = ("Graph build + STEP-2 curation is the docs-curator's job (CLAUDE.md → Doc Curation): "
            "delegate to @agent-docs-curator, scoped to a graph STEP-2 run. The main agent may only "
            "QUERY the graphs via ecsg.py / dig.py (queries + stats).")


def deny(reason: str):
    print(json.dumps({"hookSpecificOutput": {
        "hookEventName": "PreToolUse",
        "permissionDecision": "deny",
        "permissionDecisionReason": reason,
    }}))
    sys.exit(0)


def ask(reason: str):
    print(json.dumps({"hookSpecificOutput": {
        "hookEventName": "PreToolUse",
        "permissionDecision": "ask",
        "permissionDecisionReason": reason,
    }}))
    sys.exit(0)


def _is_frozen(path: str) -> bool:
    p = path.replace("\\", "/")
    return any(p == d or p.endswith("/" + d) for d in FROZEN_DOCS)


def _segment_exe(segment: str) -> str:
    """The real executable of a pipeline segment: skip env assignments (VAR=val) and wrappers
    (python3, sudo, ...), then return the basename of the first command token."""
    toks = segment.strip().split()
    i = 0
    while i < len(toks):
        t = toks[i]
        if re.match(r"^[A-Za-z_][A-Za-z0-9_]*=", t):  # VAR=value
            i += 1
            continue
        if t.rsplit("/", 1)[-1] in WRAPPERS:
            i += 1
            continue
        break
    if i >= len(toks):
        return ""
    return toks[i].rsplit("/", 1)[-1]  # basename, so /path/ecsg.py -> ecsg.py


def bash_is_gated(cmd: str):
    """Deny a Bash command that builds or reaches into the graph dirs, unless it is a
    read-only ecsg.py / dig.py query. Gate per pipeline segment by its EXECUTABLE, so a
    mere mention of 'ecsg' as an argument is not what triggers the query allowance."""
    for seg in re.split(r"\|\||&&|;|\||\n", cmd):
        exe = _segment_exe(seg)
        if exe in BUILD_EXES:
            return ("Running the di-graph build script (build_di_graph.py) in the main session is denied — "
                    "its STEP-2 curation is the docs-curator's LLM job. " + DELEGATE)
        if GRAPH_DIR_RE.search(seg) and exe not in QUERY_EXES:
            return ("Direct Bash access to .ecs-graph/ / .di-graph/ (writing or reading the graph "
                    "artifacts ad-hoc) is denied in the main session. Query via ecsg.py / dig.py; "
                    "to build or curate, " + DELEGATE)
        if exe in BASH_WRITE_EXES and any(d in seg for d in FROZEN_DOCS):
            return "FROZEN_ASK"
    return None


def main():
    try:
        data = json.load(sys.stdin)
    except Exception:
        return  # no/garbled input → allow
    # Subagents (docs-curator, scouts) are exempt — they ARE the build/curation path.
    if data.get("agent_id") or data.get("agent_type"):
        return

    tool = data.get("tool_name", "")
    ti = data.get("tool_input", {}) or {}

    if tool in ("Write", "Edit"):
        fp = (ti.get("file_path", "") or "").replace("\\", "/")
        if GRAPH_DIR_RE.search(fp):
            deny("Writing the graph artifacts (.ecs-graph/ / .di-graph/) in the main session is denied. "
                 + DELEGATE)
        if _is_frozen(fp):
            ask(FROZEN_ASK)
        return

    if tool == "Bash":
        reason = bash_is_gated(ti.get("command", "") or "")
        if reason == "FROZEN_ASK":
            ask(FROZEN_ASK)
        elif reason:
            deny(reason)
        return

    if tool == "Read":
        fp = (ti.get("file_path", "") or "").replace("\\", "/")
        if any(fp.endswith(c) for c in CHECKLISTS):
            deny("The graph STEP-2 curation checklist is the docs-curator's charter, not a main-agent read. "
                 + DELEGATE)
        return
    # everything else → allow.


if __name__ == "__main__":
    try:
        main()
    except Exception:
        # Fail-open: a gate bug must never block the main loop.
        sys.exit(0)
