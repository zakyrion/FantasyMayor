#!/usr/bin/env python3
"""PreToolUse gate — stops EVERY agent (subagents included) from a text search over the game's C# that carries
no reason, keeps the MAIN agent OUT of the graph artifact dirs (ad-hoc read/write) and turns any main-agent edit of
a permission-gated policy doc (ARCHITECTURE.md) into a user-approval ASK.

The graph is DETERMINISTIC: `fmgraph.py build` extracts AND curates in ONE call (no LLM), so the main agent
MAY run it directly. What stays denied is ad-hoc reaching into the `.fantasymayor-graph/` artifacts — query and
rebuild through the one CLI `fmgraph.py`, never poke the JSON by hand.

This closes the Bash bypass that a Write/Edit-only ban misses: a redirect, an inline
`python3 -c "... json.dump(open('.fantasymayor-graph/graph.json','w'))"`, `tee`, `sed -i`, `cp`/`mv`,
or `cat`/`jq` reaching into the graph dirs.

The C# search gate binds every agent; the graph-dir and policy-doc gates apply only to the main agent. Reads the hook JSON from stdin; prints a deny/ask
decision (or nothing = allow) and exits 0. Fail-open: any internal error → allow, so the gate can never
wedge the main loop.
"""
import json
import re
import sys

GRAPH_DIR_RE = re.compile(r"\.fantasymayor-graph\b")   # matches .fantasymayor-graph anywhere in a path/command
# The one graph CLI builds and queries — the MAIN agent may run it directly, including the writes it makes
# into its own graph dir.
GRAPH_EXES = {"fmgraph.py"}
# Permission-gated policy docs: an agent edit becomes a user-approval ASK, not a silent write.
APPROVAL_DOCS = ("ARCHITECTURE.md",)
BASH_WRITE_EXES = {"sed", "tee", "cp", "mv", "rm", "truncate"}  # write-capable bash vectors onto a gated doc
APPROVAL_ASK = ("ARCHITECTURE.md is policy: it changes ONLY with the user's explicit permission "
                "(see its header banner). Approving this prompt IS that permission. If the user did not "
                "order this change, cancel and flag the needed change back to them instead.")
# wrappers to skip when finding a pipeline segment's real executable
WRAPPERS = {"python", "python3", "uv", "run", "time", "nice", "env", "sudo", "command", "exec", "xargs"}

# A text search over the game's C# is the last resort, for every agent — the main one and every subagent alike: the
# structural questions have tools that answer them exactly. A search that is really needed says why, inline.
SEARCH_EXES = {"grep", "egrep", "fgrep", "rg", "ag", "ack"}
SEARCH_REASON_RE = re.compile(r"#\s*why:\s*\S")
CS_HINT_RE = re.compile(r"\.cs\b|--include=[\"']?\*\.cs|(?:-t|--type)[= ]?cs\b")
NOT_CS_RE = re.compile(r"\.(?:puml|uxml|uss|asset|prefab|unity|meta|asmdef|json|md|shader|hlsl)\b")
CODE_SEARCH_DENY = (
    "A text search over the game's C# (Assets/) is the last resort. Ask the tool that knows first:\n"
    "  ECS / DI / roles / recipes / game states / members / enum values — "
    "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py "
    "<explain|neighbors|state|installer|consumers|resolve|systems|pattern|members|member|bfs|search> …\n"
    "  declarations, references, callers, implementations, type hierarchy — the roslyn MCP tools "
    "(ToolSearch \"roslyn\"; find_references, find_callers, search_symbols, go_to_definition, get_type_hierarchy)\n"
    "When a text search is really the right tool — a literal value, a comment, a string — run it through Bash with "
    "the reason on the same command line:  grep -rn 'X' Assets --include='*.cs'   # why: <what no tool answers>")

GRAPH_DIR_DENY = ("Direct Bash access to .fantasymayor-graph/ (writing or reading the graph artifacts ad-hoc) "
                  "is denied in the main session. Query and rebuild via fmgraph.py — one deterministic CLI.")


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
    return toks[i].rsplit("/", 1)[-1]  # basename, so /path/fmgraph.py -> fmgraph.py


def bash_is_gated(cmd: str):
    """Deny a Bash command that reaches into the graph dirs, unless its executable is the sanctioned
    graph tool (the one graph CLI fmgraph.py). Gate per pipeline segment by its
    EXECUTABLE, so a mere mention of a graph dir as an argument to `cat`/`jq`/`sed` is what triggers the ban."""
    for seg in re.split(r"\|\||&&|;|\||\n", cmd):
        exe = _segment_exe(seg)
        if GRAPH_DIR_RE.search(seg) and exe not in GRAPH_EXES:
            return GRAPH_DIR_DENY
        if exe in BASH_WRITE_EXES and any(d in seg for d in APPROVAL_DOCS):
            return "APPROVAL_ASK"
    return None


def bash_searches_code(cmd: str) -> bool:
    """A grep / rg / ag over C# under Assets without an inline `# why:` reason."""
    if SEARCH_REASON_RE.search(cmd):
        return False
    for seg in re.split(r"\|\||&&|;|\||\n", cmd):
        if _segment_exe(seg) not in SEARCH_EXES:
            continue
        if CS_HINT_RE.search(seg) or ("Assets" in seg and not NOT_CS_RE.search(seg)):
            return True
    return False


def grep_tool_searches_code(ti: dict) -> bool:
    """The Grep tool over C# under Assets — it has no place for a reason, so a needed search goes through Bash."""
    path = (ti.get("path") or "").replace("\\", "/")
    scope = " ".join(str(ti.get(k) or "") for k in ("glob", "type"))
    if path and "Assets" not in path:
        return False
    return bool(CS_HINT_RE.search(path + " " + scope)) or ti.get("type") == "cs" \
        or ("Assets" in path and not NOT_CS_RE.search(path + " " + scope))


def main():
    try:
        data = json.load(sys.stdin)
    except Exception:
        return  # no/garbled input → allow
    tool = data.get("tool_name", "")
    ti = data.get("tool_input", {}) or {}

    # Binds every agent, subagents included — they default to grep when nothing stops them.
    if tool == "Bash" and bash_searches_code(ti.get("command", "") or ""):
        deny(CODE_SEARCH_DENY)
    if tool == "Grep" and grep_tool_searches_code(ti):
        deny(CODE_SEARCH_DENY)

    # Subagents are exempt from everything below: the graph-dir and policy-doc gates guard the main session.
    if data.get("agent_id") or data.get("agent_type"):
        return

    if tool in ("Write", "Edit"):
        fp = (ti.get("file_path", "") or "").replace("\\", "/")
        if GRAPH_DIR_RE.search(fp):
            deny("Writing the graph artifacts (.fantasymayor-graph/) by hand is denied — they are generated. "
                 "Rebuild via fmgraph.py build.")
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
