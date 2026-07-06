---
category: C
read: trigger
trigger: "on/after 2026-07-14 — run the AI-flow metrics check (python3 Tools/ai_flow_metrics.py --since 2026-07-07) and judge against the expectations here"
tags: [ai-flow, metrics, process]
related:
  - "[LISP_RULES_GUIDE](LISP_RULES_GUIDE.md)"
  - "[DOC_STANDARD](DOC_STANDARD.md)"
---

# AI-flow regime change — expectations & metrics check (due 2026-07-14)

Follow-up protocol for the 2026-07-06 AI-flow overhaul: baseline, expected outcomes, falsifiers,
and what to do if an expectation fails. Measurement: `python3 Tools/ai_flow_metrics.py --since 2026-07-07`
(NOT 07-06 — that day's data is contaminated by the audit itself: hook self-tests and the milestone
curator run).

## What changed (commits `28d664c`, `109984b`)

```lisp
(grep-family discovery   → budgeted ALLOW, 4/task)     ;; was: hard deny — 28 denials/mo of retry friction
(.cs read budget         → 12/task + task ritual)      ;; was: 8/session, 23 STOPs/mo, manual bump begging
(discovery-scout         → Sonnet, embedded charter)   ;; was: Haiku, re-read SEARCH_POLICY every run, median 23 calls
(doc edits               → plain Edit; obsidian = search-only)  ;; was: vault_patch heading-targeting, 17 target-miss errors
(doc-sync cadence        → per-milestone batch)        ;; was: per-task, 15 curator runs/mo
(ARCHITECTURE.md         → frozen, rosters evicted)    ;; 21.0KB → 11.6KB; hook turns agent edits into user-ask
(rule style              → s-expr decision tables)     ;; DOC_STANDARD Rule Style; Patterns/ converted
(GLOSSARY.md             → term → code anchors)        ;; the "агент хз що шукати" fix
```

## Baseline (30 days pre-2026-07-06; 70 main transcripts, 100 subagent runs)

Embedded in `Tools/ai_flow_metrics.py` and printed next to current numbers on every run.

## Expected outcomes — falsifiers — actions

| Metric | Expect | Falsifier → action |
|---|---|---|
| grep-family hook denials | ~0/mo (budget absorbs real need) | >5/mo → budget too small OR grep-first habit returned; inspect transcripts before touching the limit |
| grep-budget consumption | mean ≪ 4/task, most tasks 0-1 | consistently 4/4 → allowance re-legitimized grep-first; consider 4→2 + stronger policy wording |
| .cs read-budget STOPs | <5/mo, none from stale budgets | STOPs with empty grants / old counters → `task` ritual not being run; add hook-side nudge (deny message reminds to run it) |
| subagent tool-calls median | 5–12 (Sonnet scout converges) | >12 → Sonnet didn't fix wandering; revisit scout charter (anchors requirement in briefs) or revert to Haiku + tighter prompts |
| docs-curator spawns | ≤2 per 30d (milestone cadence) | per-task pattern returns → CLAUDE.md cadence rule being ignored; strengthen or gate it |
| obsidian heading-target errors | 0 (vault_patch retired) | >0 → someone edits via vault_patch again; check which agent, fix its charter |
| roslyn solutionPath errors | 0 (charter names FantasyMayor.sln) | >0 → charter line not read; move the .sln into the tool-call examples |
| s-expr tables (manual check) | extended in kind, no prose regrowth in edited rule blocks | prose bullets reappear inside/next to lisp fences → strengthen DOC_STANDARD worked example, notify user |
| task-ritual adherence (manual) | `task` run after each confirmed statement | 2-3 missed → implement the hook nudge (planned fallback, ~10 lines in search-gate.py) |

## Procedure (for the agent running this check)

1. `python3 Tools/ai_flow_metrics.py --since 2026-07-07` — table prints current vs baseline.
2. Manual checks: (a) grep transcripts for `search-gate.py task` occurrences vs count of confirmed
   engineering tasks; (b) `git log --since=2026-07-07 -p -- '*.md'` — eyeball edited lisp fences for
   in-kind extension.
3. Report to the user: metric table, verdict per expectation (met / failed → proposed action from
   the table), and an overall "did it get better" answer grounded in the diff.
4. After the report: user decides on actions; update or retire this doc (it is a one-shot protocol,
   not standing policy — retire = delete, per no-deferred-tails).
