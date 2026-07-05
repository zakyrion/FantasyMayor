#!/usr/bin/env python3
"""PreToolUse gate — forbids the MAIN agent from BUILDING or CURATING the ECS/DI graphs.

The mechanical build (`build_graph.py` / `build_di_graph.py`) AND the STEP-2 AI curation
(writing the curated `.ecs-graph/graph.json` / `.di-graph/graph.json`) belong to the
**docs-curator** subagent (CLAUDE.md → Doc Curation). The main agent may only QUERY the
graphs through the read-only CLIs `ecsg.py` / `dig.py` (whose internal auto-`--update` is
the CLI's own self-heal, not the agent curating).

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
BUILD_EXES = {"build_graph.py", "build_di_graph.py"}  # mutation entry points → docs-curator
QUERY_EXES = {"ecsg.py", "dig.py"}                    # read-only query CLIs → allowed for the main agent
# wrappers to skip when finding a pipeline segment's real executable (mirrors search-gate.py)
WRAPPERS = {"python", "python3", "uv", "run", "time", "nice", "env", "sudo", "command", "exec", "xargs"}
# STEP-2 curation checklists — reading them is the curator's job, not the main agent's
CHECKLISTS = ("skills/ecs-graph/references/ecs-patterns.md",
              "skills/di-graph/references/di-patterns.md")

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
            return ("Running the graph build script (build_graph.py / build_di_graph.py) in the main "
                    "session is denied — building the skeleton is part of the curation pipeline. " + DELEGATE)
        if GRAPH_DIR_RE.search(seg) and exe not in QUERY_EXES:
            return ("Direct Bash access to .ecs-graph/ / .di-graph/ (writing or reading the graph "
                    "artifacts ad-hoc) is denied in the main session. Query via ecsg.py / dig.py; "
                    "to build or curate, " + DELEGATE)
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
        return

    if tool == "Bash":
        reason = bash_is_gated(ti.get("command", "") or "")
        if reason:
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
