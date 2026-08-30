---
category: C
read: always
tags: [contract, process, rules]
related:
  - "[INDEX](INDEX.md)"
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[DOC_STANDARD](DOC_STANDARD.md)"
---

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

<!-- BEGIN SDD-FLOW: pointer -->
The sdd-flow canon is installed at `.sdd-flow/` (`FLOW_CONTRACT.md` + `references/`), this project's declarations at `.sdd-flow/project.md`; invoke it via `/sdd-flow:start | resume | close`, `/sdd-research`, `/sdd-project-init`.
<!-- END SDD-FLOW: pointer -->

## Working Contract: Research → Plan → Execute

The lifecycle itself — the gates, deliverable kinds, the two research passes and their
findings gate, provenance, disproven and attempted, the FLOW document, acceptance and
close — is the sdd-flow canon at `.sdd-flow/FLOW_CONTRACT.md`; the notation glossary is
`.sdd-flow/references/CLOJURE_NOTATION.md`. This project's own declarations — entry
document, knowledge tools, meters, bans, ceremonies, FLOW shape — are
`.sdd-flow/project.md`, read after the canon. Neither is restated here: this file says
what is FantasyMayor's and points at the canon for what is general.

**HARD GATE — no actions before a confirmed task statement.** For any engineering task,
first restate it and ask your own clarifying questions in the same message; then STOP and
wait for explicit confirmation. Planning, reading-for-implementation, or editing before
that confirmation is a process violation. The duty to ask is yours.

What this project adds to each phase:

1. **Research** — tool-first: the bounded `mcp__roslyn__*` tools and the ECS/DI graph CLIs
   (`ecsg.py` / `dig.py`), then targeted code reads — a file's header comment is its
   contract, and you read the files you will edit yourself, once. Any surviving doc's claim
   about code is a HYPOTHESIS: verify names via `Tools/doc_lint.py` / roslyn before relying
   on it.
2. **Plan** — when new code will consume types across an asmdef boundary, verify the
   consuming `.asmdef` references (or run `Tools/asmdef_reach.py`) BEFORE asking for GO. A
   stated effect whose value-source is not given (e.g. "add an id" with no id source) is an
   ask exactly like a missing field, never a licence to stub. There are no chat-only
   engineering plans and no self-deleting `PLAN_` files.
3. **Execute** — edit under the standing invariants: ECS writes via
   `entity.AddComponent(value)` (the Friflo upsert path, never `ref`-mutation),
   instance-by-default, zero-allocation systems. On completion harvest durable facts, drop
   the executed plan, leave its tombstone, and retain the file under the fate rules in
   `DOC_STANDARD.md`.

# FantasyMayor: Project Context & Architectural Decisions

## Start Working
- **Read `INDEX.md` first — and by default ONLY `INDEX.md`** (plain `Read`). It is the generated doc map and the single key to every doc and canvas: it carries each file's read-priority (`always` / `trigger` / `reference`) plus a one-line description. Let INDEX drive all navigation — do **not** preload anything it does not send you to.
- Follow INDEX's read-priority: read the docs it marks `read: always` next; open `trigger` docs only when their condition holds, and `reference` docs on demand.
- The `always` set is dynamic: a Category A FLOW with `status: partial` is active work. Read it
  after the standing always-docs and reconstruct its current stage, its Findings with their
  provenance, unresolved decisions, its Disproven hypotheses, its Attempted-and-dropped
  approaches, any linked `Flows/RESEARCH_*.md` document, and the next plan item (the canon
  file owns what each of those sections means). If several
  active FLOWs exist, report the conflict and ask which one to resume.
- `INDEX.md` is built in **2 passes**: (1) `python3 Tools/gen_index.py` rebuilds the structural skeleton between its `BEGIN/END GENERATED` markers from each doc's frontmatter + first line; (2) the agent curates descriptions / statuses / context. Re-run pass 1 after any frontmatter change; never edit between the markers, and keep the agent zone below the END marker short and informative.

## Documentation Access
- **This repo is an Obsidian vault, but plain file tools are the doc EDIT path.**
  Read/edit any `.md` via plain `Read` / `Edit` / `Write` — Obsidian sees on-disk
  changes. Do NOT edit via `vault_patch`/`vault_write` (heading-targeted patching
  against a moving doc was the project's top tool-error source). Move/delete via
  `vault_move` / `vault_delete` (they keep vault links intact) or flag the user.
- **Obsidian MCP is the doc SEARCH/NAVIGATION path:** `search_query` / `search_simple` /
  `vault_get_document_map`, plus a heading-scoped `vault_read` to pull ONE section of a
  large doc. If the server is down (Obsidian closed), `Grep` over `*.md` replaces search.
- **Code** files always use `Read` / `Edit` / `Write`.
- **Reading canvases:** by default read a `.canvas` via **`Tools/read_canvas.sh <file.canvas>`**
  (a `jq` projection — node `text`/`label` + edges, no positions); use full
  `vault_read` / `vault_write` only when you need to edit layout/positions.
- **`INDEX.md` is 2-pass, not free-form:** pass 1 — `gen_index.py` owns and rewrites
  the skeleton between the `BEGIN/END GENERATED` markers (never hand-edit there);
  pass 2 — the agent authors the zone below the END marker (preserved across runs).
  The Obsidian-write rule governs only that agent zone, not the generated skeleton.
- **Canvases** are JSONCanvas `.canvas` files catalogued automatically in INDEX's
  `Canvas map` (title = filename, desc = the canvas's group labels — add a group
  label to give a canvas a meaningful description). Convention: repo-root,
  `UPPER_SNAKE_CASE.canvas`.

## User Process Contract
- User-defined process and repository rules are mandatory and override agent-default workflows.
- Do not substitute personal heuristics, convenience patterns, or alternate execution flow when the user has already defined the process.
- Ignoring these instructions is considered a serious execution failure and must be treated as carrying complaint and legal escalation risk.

## Expected behavior
- You are my assistant. Your task is to implement my intent and propose your own options for solving problems.
- Our work is built on dialogue. I value discussing all important details in advance.
- Start work only when I explicitly instruct you to do so, or when you no longer have unresolved questions.
- Questions have higher priority than solving the task quickly.
- Use Context7 when I need library/API documentation or code generation, setup or configuration steps and you have some doubts about it.

## Doc ownership (no curator agent)
- The surviving doc genres (Flows, Patterns, root policy docs) are the **main agent's** to author with the
  user's approval — there is no curator agent. Category C policy and Category B patterns stay with the main
  agent / user.
- The `ecs-graph` / `di-graph` knowledge graphs are **derived, not authored**: refresh them by running
  their build scripts directly (both deterministic, one pass, no LLM — see Code Knowledge Policy); never
  hand-edit the `.ecs-graph/` / `.di-graph/` artifacts (the graph-gate hook enforces this).
- `ARCHITECTURE.md` changes **only with the user's explicit permission**: the graph-gate hook turns any agent
  edit into a user-approval ask, and approving that prompt IS the permission. Without it, propose the change
  to the user instead of writing it.

## Engineering Task Template
- **HARD GATE — no actions before a confirmed task statement. For any engineering task you MUST first restate the task using the template below AND, if you have any doubt that you understood the task correctly, ask me your own clarifying questions in the same message. Then STOP and wait for my explicit confirmation. Only AFTER I confirm the statement may you create a plan or do any work. Forming a plan, entering plan mode, reading-for-implementation, or editing anything before that confirmation is a process violation. The duty to ask is yours: when in doubt, ask me — do not assume, and do not wait for me to question you. This overrides any default "just start planning" behavior.**
- The confirmed statement's first write is its Category A FLOW. Creating or updating that
  FLOW is Research/Plan work; it never authorizes implementation. `DOC_STANDARD.md` owns
  the exact three-stage structure and active/contract/archive lifecycle.
- The gate holds `:plan` and `:mutation` deliverables. A request whose deliverable kind is
  `:answer` takes the answer lane instead — no gate and no FLOW; the canon's
  `deliverable-kind` and `answer-contract` own the rule.





- Use the following template for engineering tasks by default. Engineering tasks include coding, architecture changes, refactors, documentation, config-flow work, and other repository changes.
- Do not require this template for casual conversation or pure Q&A that does not ask for repository changes.
- Show this template to the user when they are defining an engineering task so they can see and reuse it.
- Expect engineering task requests to follow this format unless the user explicitly tells you to ignore it for the current request.
- If one or more blocks are missing in an engineering task request, do not silently invent them. Ask the user for each missing block separately and keep the discussion focused on filling those gaps.
- Blocks may be short, but every block should be present for engineering tasks unless the user explicitly opts out — **except «Роби за шаблоном», which is OPTIONAL.** Its absence is never a reason to ask and never a missing block; pick a pattern yourself only when one clearly fits, and never block, plan, or gate on it.

```text
Задача:
[що саме треба зробити]

Працюй тільки в:
- [файл/папка]
- [файл/папка]

Не дивись:
- [що поза scope]
- [що не треба аналізувати]

Роби за шаблоном (ОПЦІОНАЛЬНО — можна взагалі не вказувати):
- [Patterns/PATTERN_*.md — picker з описами: ARCHITECTURE.md → Pattern Recipes]
- [опціонально другий шаблон]

Архітектурні рішення:
- [що вже вирішено]
- [інваріанти, які не треба переосмислювати]

Не потрібно:
- [що не імплементувати / не перевіряти]

Результат:
- [який вихід очікується]
```

**EQUAL ALTERNATIVE — the Clojure statement.** Same standing as the prose template;
the user picks either form per task. Shape: one map per mechanic, a vector of maps
for a batch:

```clojure
[{:task :add-event                            ;; :task = id мапи — сусідні посилаються ним
  :goal "подія-сигнал: район побудовано"       ;; лапки = абстрактний лист, можна перепитати
  :where Actions.BuildDistrictAction.Events}  ;; голий символ = якір, дослівно

 {:task :create-spawner-system
  :listen :add-event                          ;; keyword = посилання на :task-id сусідньої мапи
  :pattern PATTERN_REACTIVE_SYSTEM            ;; = «Роби за шаблоном»
  :name :by-naming-policy                     ;; агент пропонує за політикою, юзер вето
  :do "реактивна система за шаблоном"
  :skip "AP-spending"                         ;; = «Не потрібно»
  :result "пульс події → префаб району на гексі"}]   ;; = «Результат»
```

Field ↔ template-block mapping: `:where` = «Працюй тільки в», `:off-limits` = «Не дивись»,
`:pattern` = «Роби за шаблоном», `:decided` = «Архітектурні рішення», `:skip` + `:result` =
«Не потрібно» + «Результат».

- The HARD GATE and the missing-block rules apply UNCHANGED: an absent key means "ask about
  that block, aiming the question at the specific map" — never "no constraints". The
  exceptions are `:pattern` («Роби за шаблоном») and `:accept` (measurable done-check):
  both optional, so an absent one is never an ask — never gate on them.


These two are DELIBERATE LOCAL PATCHES, not canon: the sdd-flow canon carries no
equivalent for the progress shape or for a blocked outcome, and the owner declined to
promote them upward (2026-08-30). They live here until the canon grows a home for them;
everything else about the FLOW document is `.sdd-flow/FLOW_CONTRACT.md`.

```clojure
(def flow-progress
  {:home :plan
   :shape {:status #{:active :blocked}
           :completed #{}
           :current ?
           :remaining #{}
           :resume-context "the smallest sufficient state for the next session"
           :blocker "required only while :status is :blocked"}
   :update "after a material plan transition and before a session handoff"
   :resume "apply confirmed amendments, then continue from :current"
   :compatibility "older FLOWs without this shape are reconstructed from their existing Plan"})

(def blocked-outcome
  {:frontmatter {:read :always :status :partial}
   :requires "a concrete missing authority, user/runtime decision, or external-state change"
   :record #{:blocker :needed-authority :next-action}
   :resume "re-enter through the unresolved blocker; blocked is never complete"
   :never #{:archive :implemented}})
```

- The notation is defined ONCE — universal forms live only in the canonical glossary
  (`~/.claude/CLAUDE.md` → "Clojure instruction notation", already in every agent's context);
  authoring spec for Clojure rules inside docs: `DOC_STANDARD.md` → Rule Style. The block
  below does NOT restate it — it adds PROJECT-scoped readings only (allowed by the
  glossary's project-scope clause):

```clojure
(def notation-ecs-ext  ;; 2026-07-17 — project-scoped notation extension (ECS); universal forms stay global
  {:entity-shape "(def <Archetype> {:archetype … :tag … :pk … :fk … :kind … :state … :data …}) — one map = one entity; keys anchor to tag-law / key-role-law (ARCHITECTURE.md)"
   :set-cardinality "the FIELD decides the #{} reading: singular-valued key (:home, :tag) → global 'one of'; collection-valued key (:data, :fk) → ALL members, unordered, no duplicates (= ECS composition)"
   :tag-never-set "a #{} under :tag is not alternative syntax — it DISPLAYS a Tag Law violation (2 identity tags)"})
```

## Code Knowledge Policy (tool-first — module MDs abolished 2026-07-09)
- **Module/domain/presentation MD files do not exist and must NEVER be recreated.** They rotted faster
  than curation could keep up; a stale doc poisons context worse than no doc.
- Knowledge lives in four non-rotting forms:
  1. **Derived** — code STRUCTURE via `roslyn-mcp`; ECS/DI relationships via `ecs-graph`/`di-graph`;
     doc symbol claims are lint-checked by `Tools/doc_lint.py`.
  2. **Code comments at distance zero** — intent, non-obvious invariants, and contracts live in a short
     comment ON the thing itself (class header / method), updated in the same diff. A comment about
     ANOTHER file is a rot seed — link by name only, or move the fact to its owner.
  3. **Dated records** — every engineering task has a dated Category A FLOW; its verbatim request and
     decision log preserve the change across sessions. The commit message remains the execution record.
  4. **Decreed rules** — `ARCHITECTURE.md` (policy), `ECS_CONVENTIONS.md` (point-of-code rules),
     `Patterns/` (recipes). They change only by the user's decision, never by code drift.
- If you add or change an ECS entity archetype, refresh the ecs-graph (`build_graph.py` / `/ecs-graph`) —
  the sole archetype/event registry. Likewise refresh the di-graph (`build_di_graph.py` / `/di-graph`) after
  changing DI wiring (registrations, `[Inject]`, Boot composition). Both builds are deterministic — run them yourself.
- `DOC_STANDARD.md` governs the surviving doc genres (Flows, Patterns, root policy docs).

## OpenSpec Policy
Evaluated 2026-08-05: the generated OpenSpec skills over-triggered and duplicated the
task-map workflow (proposal/design/tasks ≈ task map; checkbox-done ≠ `:accept`-done), and
its main specs would compete with `Flows/FLOW_*.md`. Full evaluation record: the removal commit.

```clojure
(def openspec-policy  ;; 2026-08-05 — removed from the active workflow of BOTH agents
  {:project-integration :removed        ;; skills (.claude/.codex) + openspec/ deleted
   :cli :available                      ;; global `openspec` CLI stays installed
   :future-use :explicit-only           ;; never triggers by itself — only by the user's direct ask
   :reconsider-only-when "довготривала capability spec із реальними delta requirements"})
```

## Unity Build Policy
- This is a Unity project.
- Do not run Unity project builds from the agent side.
- Do not run `dotnet build`, `msbuild`, `xbuild`, or Unity CLI build commands for this repository.
- If compile validation is needed, request a Unity-side check from the user.
- **Pre-check BEFORE asking:** first run `mcp__roslyn__get_diagnostics` (solutionPath:
  `FantasyMayor.sln`, scoped to the edited project/files) and fix what it reports. Unity stays the
  authority — roslyn's workspace misses codegen and types added/renamed since the last Unity regen —
  so the pre-check filters plain C# errors out of the round-trip; it never replaces the user's check.
- Do not read Unity scene files such as `.unity` or other scene-serialized assets unless the user explicitly allows it in the current task.
- Never generate or hand-write Unity `.meta` files under any circumstances. If a `.meta` file is needed, stop and ask the user.

## Code Quality And Review Bar
- Write code that is ready to pass strict review.
- Review incoming code as if done by a senior engineer with 20 years of experience in a very critical mood.
- Prefer instance-based design; introduce `static` only when there is a clear architectural reason.
- Default to the SIMPLEST structure that solves the task. Patterns (orchestrator/subsystem fan-out, snapshot-before-iterate, …) serve the problem — reach for one ONLY when cardinality or real complexity demands it, never because a doc or code comment mentions it. A one-element set needs no snapshot; a one-line tag swap needs no subsystem family.
- Never ship a knowingly-wrong placeholder (e.g. a hardcoded id) behind "out of scope". When a value needs a real source you do not yet have, surface it as a decision during Plan — do not implement a stub and present it as done.

## Code Documentation Policy
- Add comments only where the logic stops being simple and unambiguous.
- Prefer short targeted comments for non-obvious algorithmic constraints, decisions, and invariants.
- Do not add boilerplate XML documentation by default.
