#!/usr/bin/env python3
"""PreToolUse gate — enforces the project search policy (.claude/SEARCH_POLICY.md).

Applies ONLY to the main agent (subagents are exempt — they are the discovery path). In the main
session it denies source DISCOVERY over Assets/**/*.cs (Grep/Glob sweeps + Bash rg/grep/find + direct
graph-QUERY CLI ecsg/dig) and routes everything to @agent-discovery-scout, and it budgets unique .cs reads
(for editing). On budget exhaustion it denies with a STOP message telling the agent to ask the user.

Reads the hook JSON from stdin; prints a deny decision (or nothing = allow) and exits 0. Fail-open:
any internal error → allow, so the gate can never wedge the main loop.

Admin (run by the user / on user authorization — not gated, writes only under ~/.claude):
    python3 search-gate.py reset            # clear all session budgets (gives a fresh allowance now)
    python3 search-gate.py bump <N>         # raise the limit to N on every active session budget file
"""
import json
import os
import re
import sys
from pathlib import Path

DEFAULT_LIMIT = 8
STATE_DIR = Path.home() / ".claude" / ".search-budget"
SCOUT = ("→ for code structure/refs/symbols use the roslyn-mcp tools (mcp__roslyn__*); "
         "for ECS/DI/orchestration/docs delegate to @agent-discovery-scout. "
         "See .claude/SEARCH_POLICY.md.")

SOURCE_RE = re.compile(r"Assets/.*\.cs$")
# Graph QUERY CLIs only — they dump raw graph TEXT into context (use the bounded MCP / the scout instead).
# BUILD scripts (build_graph/build_di_graph) are intentionally NOT gated: they only write artifacts + a
# stat line, so a re-build is maintenance, not discovery — gating them would break the skills themselves.
# `dig.py` (not bare `dig`, which collides with the Unix DNS tool).
GRAPH_CLI_EXES = {"ecsg", "ecsg.py", "ecs-graph", "dig.py", "di-graph"}
CODE_SEARCH_EXES = {"rg", "ag", "ack"}            # dedicated source-search tools
GREP_FIND_EXES = {"grep", "egrep", "fgrep", "find"}  # general; gated only over Assets
# wrappers to skip when finding a pipeline segment's real executable
WRAPPERS = {"python", "python3", "uv", "run", "time", "nice", "env", "sudo", "command", "exec", "xargs"}


def deny(reason: str):
    print(json.dumps({"hookSpecificOutput": {
        "hookEventName": "PreToolUse",
        "permissionDecision": "deny",
        "permissionDecisionReason": reason,
    }}))
    sys.exit(0)


# ----------------------------------------------------------------- budget state
def _state_path(session_id: str) -> Path:
    safe = re.sub(r"[^A-Za-z0-9_.-]", "_", session_id or "unknown")
    return STATE_DIR / f"{safe}.json"


def _with_lock(path: Path, fn):
    import fcntl
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    lock = STATE_DIR / ".lock"
    with open(lock, "w") as lf:
        fcntl.flock(lf, fcntl.LOCK_EX)
        try:
            return fn(path)
        finally:
            fcntl.flock(lf, fcntl.LOCK_UN)


def _load(path: Path):
    if path.exists():
        try:
            d = json.loads(path.read_text("utf-8"))
            d.setdefault("files", [])
            d.setdefault("limit", DEFAULT_LIMIT)
            return d
        except Exception:
            pass
    return {"files": [], "limit": DEFAULT_LIMIT}


def budget_check(session_id: str, file_path: str) -> bool:
    """True = allow, False = deny (limit reached for a new file)."""
    def op(path):
        d = _load(path)
        if file_path in d["files"]:
            return True  # already counted — re-reading for editing is free
        if len(d["files"]) < d["limit"]:
            d["files"].append(file_path)
            path.write_text(json.dumps(d), "utf-8")
            return True
        return False
    return _with_lock(_state_path(session_id), op)


# ----------------------------------------------------------------- admin
def admin(argv) -> bool:
    if not argv:
        return False
    cmd = argv[0]
    if cmd == "reset":
        if STATE_DIR.exists():
            for f in STATE_DIR.glob("*.json"):
                f.unlink()
        print("search-gate: budgets reset")
        return True
    if cmd == "bump" and len(argv) >= 2:
        n = int(argv[1])
        STATE_DIR.mkdir(parents=True, exist_ok=True)
        for f in STATE_DIR.glob("*.json"):
            d = _load(f)
            d["limit"] = n
            f.write_text(json.dumps(d), "utf-8")
        print(f"search-gate: limit bumped to {n}")
        return True
    return False


# ----------------------------------------------------------------- gate
def grep_glob_targets_source(ti: dict) -> bool:
    glob = (ti.get("glob") or "")
    pattern = (ti.get("pattern") or "")
    typ = (ti.get("type") or "")
    path = (ti.get("path") or "")
    if glob.endswith(".md") or typ == "md" or pattern.endswith(".md"):
        return False  # docs-only search is fine
    if path and "Assets" not in path and path not in (".", ""):
        return False  # explicitly searching somewhere other than the game code
    return True  # whole-repo or Assets-scoped source sweep


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
    """Gate by each pipeline segment's EXECUTABLE — so a mere mention of 'ecsg' or 'grep' as an
    argument (e.g. `grep ecsg CLAUDE.md`) is NOT gated; only an actual invocation is."""
    for seg in re.split(r"\|\||&&|;|\||\n", cmd):
        exe = _segment_exe(seg)
        if exe in GRAPH_CLI_EXES:
            return ("Graph queries run via the scout, not the main agent. " + SCOUT)
        if exe in CODE_SEARCH_EXES:
            return ("Source search (rg/ag/ack) in the main session is gated. " + SCOUT)
        if exe in GREP_FIND_EXES and "Assets" in seg:
            return ("Source search over Assets in the main session is gated. " + SCOUT)
    return None


def main():
    if admin(sys.argv[1:]):
        return
    try:
        data = json.load(sys.stdin)
    except Exception:
        return  # no/garbled input → allow
    # Subagents (scouts) are exempt — they ARE the discovery path.
    if data.get("agent_id") or data.get("agent_type"):
        return

    tool = data.get("tool_name", "")
    ti = data.get("tool_input", {}) or {}

    if tool in ("Grep", "Glob"):
        if grep_glob_targets_source(ti):
            deny("Source discovery in the main session is gated. " + SCOUT)
        return

    if tool == "Bash":
        reason = bash_is_gated(ti.get("command", "") or "")
        if reason:
            deny(reason)
        return

    if tool == "Read":
        fp = ti.get("file_path", "") or ""
        if SOURCE_RE.search(fp):
            if not budget_check(data.get("session_id", ""), fp):
                deny("STOP — main-agent .cs read budget reached this session. Do NOT read more source. "
                     "Tell the user what you are searching for, what you already found, and why more source "
                     "is needed — they can help directly or authorize a budget bump "
                     "(python3 .claude/hooks/search-gate.py bump <N>). Otherwise " + SCOUT)
        return
    # Edit / Write / Task / everything else → allow.


if __name__ == "__main__":
    try:
        main()
    except Exception:
        # Fail-open: a gate bug must never block the main loop.
        sys.exit(0)
