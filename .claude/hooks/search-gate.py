#!/usr/bin/env python3
"""PreToolUse gate — enforces the project search policy (.claude/SEARCH_POLICY.md).

Applies ONLY to the main agent (subagents are exempt — they are the discovery path). In the main
session it BUDGETS source discovery over Assets/**/*.cs (Grep/Glob sweeps + Bash rg/grep/find:
first GREP_LIMIT ops per task are allowed, then deny→scout), hard-denies Bash viewers
(cat/head/tail/sed/awk over .cs — they bypass the Read budget), and budgets unique .cs reads
(for editing). On read-budget exhaustion it denies with a STOP message telling the agent to ask
the user. The graph query CLIs (ecsg.py / dig.py) are NOT gated — they return distilled, bounded
facts (the graph MCP servers were retired in favour of the CLIs).

A "session" here = ONE task: the user runs /clear or /compact between tasks. /compact keeps the
session_id, so budgets are re-armed by the `task` command at each confirmed task statement.

Reads the hook JSON from stdin; prints a deny decision (or nothing = allow) and exits 0. Fail-open:
any internal error → allow, so the gate can never wedge the main loop.

Admin (run by the main agent on task confirmation, or by the user — not gated, writes only under ~/.claude):
    python3 search-gate.py task <path...>   # NEW-TASK ritual: reset all budgets + grants, then grant
                                            # the confirmed statement's «Працюй тільки в» .cs files.
                                            # The user's confirmation of the statement IS the
                                            # authorization — the main agent runs this itself.
    python3 search-gate.py reset            # clear all session budgets AND grants (fresh allowance now)
    python3 search-gate.py bump <N>         # raise the .cs-read limit to N on every active session budget
    python3 search-gate.py grant <path...>  # add task-scope .cs files without resetting budgets
    python3 search-gate.py grant            # list active grants
"""
import json
import os
import re
import sys
from pathlib import Path

DEFAULT_LIMIT = 12       # unique .cs files readable per task (grants excluded)
DEFAULT_GREP_LIMIT = 4   # grep-family discovery ops per task (Grep/Glob over source, bash rg/grep/find)
STATE_DIR = Path.home() / ".claude" / ".search-budget"
GRANTS_PATH = STATE_DIR / "_grants.json"  # user-granted task scope; cleared by `reset`
SCOUT = ("→ for ECS/DI facts run the graph CLIs directly (ecsg.py / dig.py) or delegate to "
         "@agent-discovery-scout; for code structure/refs/symbols use roslyn-mcp (mcp__roslyn__*) "
         "or the scout. See .claude/SEARCH_POLICY.md.")

SOURCE_RE = re.compile(r"Assets/.*\.cs$")
# The graph QUERY CLIs (ecsg.py / dig.py) and BUILD scripts (build_graph/build_di_graph) are NOT gated:
# they read a distilled fact store (or write gitignored artifacts) and return bounded, structured output,
# not raw source dumps — the main agent is expected to run them directly for ECS/DI facts (the graph MCP
# servers were retired in favour of the CLIs). Only raw source discovery over Assets/**/*.cs is gated.
CODE_SEARCH_EXES = {"rg", "ag", "ack"}            # dedicated source-search tools
GREP_FIND_EXES = {"grep", "egrep", "fgrep", "find"}  # general; gated only over Assets
# viewers/streamers that would bypass the Read budget; gated only when a .cs under Assets is named
VIEW_EXES = {"cat", "head", "tail", "sed", "awk", "less", "more", "strings"}
CS_IN_SEG_RE = re.compile(r"Assets/\S*\.cs\b")
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
            d.setdefault("greps", 0)
            d.setdefault("grep_limit", DEFAULT_GREP_LIMIT)
            return d
        except Exception:
            pass
    return {"files": [], "limit": DEFAULT_LIMIT, "greps": 0, "grep_limit": DEFAULT_GREP_LIMIT}


def _load_grants():
    if GRANTS_PATH.exists():
        try:
            return json.loads(GRANTS_PATH.read_text("utf-8")).get("files", [])
        except Exception:
            pass
    return []


def _granted(file_path: str, grants) -> bool:
    """Match by exact path or by repo-relative suffix (Read passes absolute paths;
    grants are usually registered as repo-relative Assets/... paths)."""
    for g in grants:
        g = g.lstrip("./")
        if file_path == g or file_path.endswith("/" + g):
            return True
    return False


def budget_check(session_id: str, file_path: str) -> bool:
    """True = allow, False = deny (limit reached for a new file)."""
    def op(path):
        if _granted(file_path, _load_grants()):
            return True  # user-granted task scope — never counted
        d = _load(path)
        if file_path in d["files"]:
            return True  # already counted — re-reading for editing is free
        if len(d["files"]) < d["limit"]:
            d["files"].append(file_path)
            path.write_text(json.dumps(d), "utf-8")
            return True
        return False
    return _with_lock(_state_path(session_id), op)


def grep_budget_check(session_id: str):
    """(allowed, spent, limit) — consumes one grep-family discovery op if under the limit."""
    def op(path):
        d = _load(path)
        if d["greps"] < d["grep_limit"]:
            d["greps"] += 1
            path.write_text(json.dumps(d), "utf-8")
            return True, d["greps"], d["grep_limit"]
        return False, d["greps"], d["grep_limit"]
    return _with_lock(_state_path(session_id), op)


# ----------------------------------------------------------------- admin
def admin(argv) -> bool:
    if not argv:
        return False
    cmd = argv[0]
    if cmd == "task":
        # New-task ritual: fresh budgets + fresh grants in one call (session = one task).
        if STATE_DIR.exists():
            for f in STATE_DIR.glob("*.json"):
                f.unlink()
        files = list(dict.fromkeys(argv[1:]))
        if files:
            STATE_DIR.mkdir(parents=True, exist_ok=True)
            GRANTS_PATH.write_text(json.dumps({"files": files}), "utf-8")
        print(f"search-gate: new task — budgets reset, {len(files)} path(s) granted")
        return True
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
            if f.name == GRANTS_PATH.name:
                continue
            d = _load(f)
            d["limit"] = n
            f.write_text(json.dumps(d), "utf-8")
        print(f"search-gate: limit bumped to {n}")
        return True
    if cmd == "grant":
        files = _load_grants()
        if len(argv) >= 2:
            STATE_DIR.mkdir(parents=True, exist_ok=True)
            for f in argv[1:]:
                if f not in files:
                    files.append(f)
            GRANTS_PATH.write_text(json.dumps({"files": files}), "utf-8")
            print(f"search-gate: granted {len(argv) - 1} path(s); active grants: {len(files)}")
        else:
            print("search-gate: active grants:" if files else "search-gate: no active grants")
            for f in files:
                print(f"  {f}")
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


def bash_gate_kind(cmd: str):
    """Classify a Bash command by each pipeline segment's EXECUTABLE — a mere mention of 'grep'
    as an argument (e.g. `grep ecsg CLAUDE.md`) is NOT gated; only an actual invocation is.
    Returns ('deny', reason) for viewers over .cs (hard deny — bypasses the Read budget),
    ('grep', None) for a grep-family source search (budgeted), or (None, None)."""
    is_grep = False
    for seg in re.split(r"\|\||&&|;|\||\n", cmd):
        exe = _segment_exe(seg)
        if exe in VIEW_EXES and CS_IN_SEG_RE.search(seg):
            return "deny", ("Viewing .cs source through Bash bypasses the Read budget. "
                            "Use the Read tool on the file you are editing (budgeted). " + SCOUT)
        if exe in CODE_SEARCH_EXES or (exe in GREP_FIND_EXES and "Assets" in seg):
            is_grep = True
    return ("grep", None) if is_grep else (None, None)


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
            ok, spent, limit = grep_budget_check(data.get("session_id", ""))
            if not ok:
                deny(f"grep budget spent ({spent}/{limit} this task) — no more raw source sweeps. " + SCOUT)
        return

    if tool == "Bash":
        kind, reason = bash_gate_kind(ti.get("command", "") or "")
        if kind == "deny":
            deny(reason)
        elif kind == "grep":
            ok, spent, limit = grep_budget_check(data.get("session_id", ""))
            if not ok:
                deny(f"grep budget spent ({spent}/{limit} this task) — no more raw source sweeps. " + SCOUT)
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
