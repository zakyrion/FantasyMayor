#!/usr/bin/env python3
"""AI-flow metrics: compare the post-2026-07-06 agent-workflow regime against the audit baseline.

Scans Claude Code transcripts for this project and prints current-period metrics next to the
baseline captured in the 2026-07-06 audit (30 days, 70 main transcripts). Companion doc with
expectations and decision rules: AI_FLOW_METRICS.md.

Usage: python3 Tools/ai_flow_metrics.py [--since 2026-07-06]
"""
import argparse
import json
import re
import time
from collections import Counter
from pathlib import Path

PROJ = Path.home() / ".claude" / "projects" / "-Users-serhiikharsun-Documents-HomeProjects-FantasyMayor"

BASELINE = {
    "grep-family hook denials": "28 (25 grep/find + 3 rg, 18 sessions)",
    ".cs read-budget STOPs": "23",
    "grep-family uses (allowed)": "n/a (was hard-denied)",
    "scout/subagent runs": "100 subagent transcripts / 46 discovery-scout spawns",
    "subagent tool-calls median": "23 (max 72, Haiku scout era)",
    "docs-curator spawns": "15 (per-task cadence)",
    "obsidian heading-target errors": "17",
    "obsidian bad-args errors": "4",
    "roslyn solutionPath errors": "5",
}

DENY = {
    "grep-budget denial (new regime)": re.compile(r"grep budget spent"),
    "legacy-style grep denial": re.compile(r"Source (search|discovery).*(gated|main session)"),
    ".cs read-budget STOP": re.compile(r"read budget reached"),
}
OBS_TARGET = re.compile(r"heading not found|target not found", re.I)
OBS_ARGS = re.compile(r"Input validation error", re.I)
ROSLYN_PATH = re.compile(r"(solution path|sourceFile) is required", re.I)


def text_of(content):
    if isinstance(content, str):
        return content
    if isinstance(content, list):
        return " ".join(x.get("text", "") for x in content if isinstance(x, dict))
    return ""


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--since", default="2026-07-06")
    args = ap.parse_args()
    cutoff = time.mktime(time.strptime(args.since, "%Y-%m-%d"))

    counts = Counter()
    sessions = 0
    grep_uses = Counter()  # allowed grep-family tool_use in main sessions, by session

    for f in sorted(PROJ.glob("*.jsonl")):
        if f.stat().st_mtime < cutoff:
            continue
        sessions += 1
        id2tool = {}
        for line in f.open():
            try:
                rec = json.loads(line)
            except Exception:
                continue
            content = (rec.get("message") or {}).get("content")
            if not isinstance(content, list):
                continue
            for b in content:
                if not isinstance(b, dict):
                    continue
                if b.get("type") == "tool_use":
                    name = b.get("name", "")
                    id2tool[b.get("id", "")] = name
                    ti = b.get("input") or {}
                    if name in ("Grep", "Glob") and "Assets" in json.dumps(ti):
                        grep_uses[f.stem] += 1
                    elif name == "Bash" and re.search(r"\b(rg|ag|ack)\b", ti.get("command", "") or ""):
                        grep_uses[f.stem] += 1
                    if name in ("Task", "Agent"):
                        st = ti.get("subagent_type", "?")
                        counts[f"spawn:{st}"] += 1
                elif b.get("type") == "tool_result" and b.get("is_error"):
                    tname = id2tool.get(b.get("tool_use_id", ""), "?")
                    text = text_of(b.get("content"))
                    for label, pat in DENY.items():
                        if pat.search(text):
                            counts[label] += 1
                    if tname.startswith("mcp__obsidian"):
                        if OBS_TARGET.search(text):
                            counts["obsidian heading-target errors"] += 1
                        elif OBS_ARGS.search(text):
                            counts["obsidian bad-args errors"] += 1
                    if tname.startswith("mcp__roslyn") and ROSLYN_PATH.search(text):
                        counts["roslyn solutionPath errors"] += 1

    # subagent transcripts (scout convergence): tool calls per run, mtime-filtered
    calls_per_run = []
    for f in PROJ.glob("*/subagents/*.jsonl"):
        if f.stat().st_mtime < cutoff:
            continue
        n = 0
        for line in f.open():
            n += line.count('"type":"tool_use"') + line.count('"type": "tool_use"')
        calls_per_run.append(n)
    calls_per_run.sort()

    print(f"period since {args.since}: {sessions} main transcripts, {len(calls_per_run)} subagent runs")
    print(f"\n{'metric':40s} {'now':>12s}   baseline (30d pre-2026-07-06)")
    def row(label, now):
        print(f"{label:40s} {str(now):>12s}   {BASELINE.get(label, '—')}")
    row("grep-family hook denials",
        counts["grep-budget denial (new regime)"] + counts["legacy-style grep denial"])
    row(".cs read-budget STOPs", counts[".cs read-budget STOP"])
    used = sum(grep_uses.values())
    row("grep-family uses (allowed)", f"{used} ({len(grep_uses)} sess)")
    med = calls_per_run[len(calls_per_run) // 2] if calls_per_run else 0
    mx = calls_per_run[-1] if calls_per_run else 0
    row("subagent tool-calls median", f"{med} (max {mx})")
    row("docs-curator spawns", counts["spawn:docs-curator"])
    row("obsidian heading-target errors", counts["obsidian heading-target errors"])
    row("obsidian bad-args errors", counts["obsidian bad-args errors"])
    row("roslyn solutionPath errors", counts["roslyn solutionPath errors"])
    print("\nspawns by agent:", {k[6:]: v for k, v in counts.items() if k.startswith("spawn:")})
    print("\nDecision rules & expectations: AI_FLOW_METRICS.md")


if __name__ == "__main__":
    main()
