#!/usr/bin/env python3
"""PreToolUse gate — keeps the MAIN agent OUT of the graph artifact dirs (ad-hoc read/write) and turns
any main-agent edit of a permission-gated policy doc (ARCHITECTURE.md) into a user-approval ASK.

Both graphs are DETERMINISTIC now: `build_graph.py` (ecs) and `build_di_graph.py` (di) each extract AND
curate in ONE call (no LLM STEP-2 for either graph anymore), so the main agent MAY run either directly.
What stays denied is ad-hoc reaching into the `.ecs-graph/` / `.di-graph/` artifacts — query through the
read-only CLIs `ecsg.py` / `dig.py`, rebuild through the two build scripts, and never poke the JSON by hand.

This closes the Bash bypass that a Write/Edit-only ban misses: a redirect, an inline
`python3 -c "... json.dump(open('.ecs-graph/graph.json','w'))"`, `tee`, `sed -i`, `cp`/`mv`,
or `cat`/`jq` reaching into the graph dirs.

Applies ONLY to the main agent; subagents are exempt. Reads the hook JSON from stdin; prints a deny/ask
decision (or nothing = allow) and exits 0. Fail-open: any internal error → allow, so the gate can never
wedge the main loop.
"""
import json
import re
import sys

GRAPH_DIR_RE = re.compile(r"\.(?:ecs|di)-graph\b")   # matches .ecs-graph / .di-graph anywhere in a path/command
# Both graph builders are deterministic (extract + curate in one call), alongside the read-only query CLIs —
# the MAIN agent may run all of them directly, including the writes they make into their own graph dir.
GRAPH_EXES = {"ecsg.py", "dig.py", "build_graph.py", "build_di_graph.py"}
# Permission-gated policy docs: an agent edit becomes a user-approval ASK, not a silent write.
APPROVAL_DOCS = ("ARCHITECTURE.md",)
BASH_WRITE_EXES = {"sed", "tee", "cp", "mv", "rm", "truncate"}  # write-capable bash vectors onto a gated doc
APPROVAL_ASK = ("ARCHITECTURE.md is policy: it changes ONLY with the user's explicit permission "
                "(see its header banner). Approving this prompt IS that permission. If the user did not "
                "order this change, cancel and flag the needed change back to them instead.")
# wrappers to skip when finding a pipeline segment's real executable
WRAPPERS = {"python", "python3", "uv", "run", "time", "nice", "env", "sudo", "command", "exec", "xargs"}

GRAPH_DIR_DENY = ("Direct Bash access to .ecs-graph/ / .di-graph/ (writing or reading the graph "
                  "artifacts ad-hoc) is denied in the main session. Query via ecsg.py / dig.py; "
                  "rebuild via build_graph.py / build_di_graph.py — both are deterministic one-pass builds.")


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


def _needs_approval(path: str) -> bool:
    p = path.replace("\\", "/")
    return any(p == d or p.endswith("/" + d) for d in APPROVAL_DOCS)


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
    """Deny a Bash command that reaches into the graph dirs, unless its executable is one of the sanctioned
    graph tools (the two deterministic builders + the read-only query CLIs). Gate per pipeline segment by its
    EXECUTABLE, so a mere mention of a graph dir as an argument to `cat`/`jq`/`sed` is what triggers the ban."""
    for seg in re.split(r"\|\||&&|;|\||\n", cmd):
        exe = _segment_exe(seg)
        if GRAPH_DIR_RE.search(seg) and exe not in GRAPH_EXES:
            return GRAPH_DIR_DENY
        if exe in BASH_WRITE_EXES and any(d in seg for d in APPROVAL_DOCS):
            return "APPROVAL_ASK"
    return None


def main():
    try:
        data = json.load(sys.stdin)
    except Exception:
        return  # no/garbled input → allow
    # Subagents (scouts, etc.) are exempt.
    if data.get("agent_id") or data.get("agent_type"):
        return

    tool = data.get("tool_name", "")
    ti = data.get("tool_input", {}) or {}

    if tool in ("Write", "Edit"):
        fp = (ti.get("file_path", "") or "").replace("\\", "/")
        if GRAPH_DIR_RE.search(fp):
            deny("Writing the graph artifacts (.ecs-graph/ / .di-graph/) by hand is denied — they are "
                 "generated. Rebuild via build_graph.py / build_di_graph.py.")
        if _needs_approval(fp):
            ask(APPROVAL_ASK)
        return

    if tool == "Bash":
        reason = bash_is_gated(ti.get("command", "") or "")
        if reason == "APPROVAL_ASK":
            ask(APPROVAL_ASK)
        elif reason:
            deny(reason)
        return
    # everything else → allow.


if __name__ == "__main__":
    try:
        main()
    except Exception:
        # Fail-open: a gate bug must never block the main loop.
        sys.exit(0)
