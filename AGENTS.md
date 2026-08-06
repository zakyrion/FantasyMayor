---
category: C
read: reference
tags: [contract, process, codex]
related:
  - "[CLAUDE](CLAUDE.md)"
  - "[INDEX](INDEX.md)"
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# AGENTS.md

Codex bootstrap: full FantasyMayor process contract, Codex-native — HARD GATE, go/done, notation ext, tools, policies.

<!-- Codex-native contract DERIVED from CLAUDE.md (the semantic source; Claude Code loads
     that file, Codex loads this one). Process semantics are identical by contract; only
     the tool surface differs. Any process change lands in CLAUDE.md first, then syncs
     here in the same pass. On disagreement CLAUDE.md wins — report drift, don't improvise.
     Universal Clojure notation is NOT restated here: it autoloads from ~/.codex/AGENTS.md
     (the Codex-native copy of the canonical glossary). -->

## Working Contract: Research → Plan → Execute
Every engineering task runs in three phases — ENTERED only through a confirmed task
statement (the HARD GATE): confirming the statement is what opens Research and Plan
for that task; Execute opens only on its own fresh `go` for the implementation map.
The precise gate semantics are `go-contract` / `done-contract` below.
1. **Research** — gather facts, do not accumulate source. **Tool-first + code**: ECS/DI
   structure via your installed `ecs-graph` / `di-graph` skills (never by grepping for
   `AddComponent<T>()` / `builder.Register<…>()` — generic-typed wiring is invisible to
   text search); targeted code reads (a file's header comment is its contract) — read the
   files you will edit yourself, once. In = distilled findings, not raw dumps. Any doc's
   claim about code is a HYPOTHESIS — verify names via `Tools/doc_lint.py` / a grep for
   the declaration before relying on it.
2. **Plan** — restate the task via the Engineering Task Template, ask clarifying
   questions, and **wait for explicit confirmation** before any edit (the HARD GATE
   below). Surface ALL open decisions in ONE consolidated pass and BATCH the questions —
   a stated effect whose value-source is not given is an ask exactly like a missing
   field, never a licence to stub; do not drip questions across rounds. When new code
   will consume types across an asmdef boundary, verify the consuming `.asmdef`
   references (or run `Tools/asmdef_reach.py`) BEFORE asking for GO.
   Persist the plan as an on-disk artifact **only for multi-session programs**;
   single-session tasks stay in the chat.
3. **Execute** — edit under the standing invariants (ECS writes via `entity.AddComponent(value)` — the
   Friflo upsert path, never `ref`-mutation; instance-by-default, zero-allocation systems).

## Start Working
- **Read `INDEX.md` first — and by default ONLY `INDEX.md`.** It is the generated doc map
  and the single key to every doc and canvas: read-priority (`always` / `trigger` /
  `reference`) plus a one-line description per file. Let INDEX drive all navigation — do
  **not** preload anything it does not send you to.
- Follow INDEX's read-priority: read the docs it marks `read: always` next; open
  `trigger` docs only when their condition holds, and `reference` docs on demand.
- `INDEX.md` is built in **2 passes**: (1) `python3 Tools/gen_index.py` rebuilds the
  skeleton between its `BEGIN/END GENERATED` markers; (2) the agent curates the zone
  below the END marker. Re-run pass 1 after any frontmatter change; never edit between
  the markers.
- For a session-start context reload, run your installed `fantasymayor-session-start`
  skill.

## Documentation Access (Codex tool surface)
- Read/edit any `.md` and all code via plain file tools (read / edit / write).
- Doc SEARCH: grep over `*.md`. (The repo is an Obsidian vault; Obsidian integration is a
  Claude-side channel — you do not need it.)
- **Reading canvases:** read a `.canvas` via `Tools/read_canvas.sh <file.canvas>` (a `jq`
  projection — node text/labels + edges, no positions).
- **`INDEX.md` is 2-pass, not free-form** — the generated skeleton is owned by
  `gen_index.py`; only the agent zone below the END marker is authored, and doc edits
  beyond mechanical passes need the user's permission first.

## User Process Contract
- User-defined process and repository rules are mandatory and override agent-default workflows.
- Do not substitute personal heuristics, convenience patterns, or alternate execution flow when the user has already defined the process.
- Ignoring these instructions is considered a serious execution failure and must be treated as carrying complaint and legal escalation risk.

## Expected behavior
- You are my assistant. Your task is to implement my intent and propose your own options for solving problems.
- Our work is built on dialogue. I value discussing all important details in advance.
- Start work only when I explicitly instruct you to do so, or when you no longer have unresolved questions.
- Questions have higher priority than solving the task quickly.

## Discovery
- Default discovery path is your own: `ecs-graph` / `di-graph` skills → targeted code
  reads (grep as needed). There are no helper scout agents on the Codex side — do the
  haystack sweeps yourself and report distilled `file:line` facts, not dumps.

## Doc ownership
- The surviving doc genres (Flows, Patterns, root policy docs) are authored only with the
  user's approval. Never auto-edit Flows/Patterns/INDEX-zone/canvas after code changes —
  ask first; mechanical passes are free (`gen_index.py` pass 1, graph rebuilds, `doc_lint.py`).
- The `ecs-graph` / `di-graph` knowledge graphs are **derived, not authored**: refresh via
  their build scripts (deterministic, no LLM); never hand-edit `.ecs-graph/` / `.di-graph/`.
- `ARCHITECTURE.md` changes **only with the user's explicit permission** — without it, propose the change instead of writing it.

## Engineering Task Template
- **HARD GATE — no actions before a confirmed task statement. For any engineering task you MUST first restate the task using the template below AND, if you have any doubt that you understood the task correctly, ask me your own clarifying questions in the same message. Then STOP and wait for my explicit confirmation. Only AFTER I confirm the statement may you create a plan or do any work. Forming a plan, reading-for-implementation, or editing anything before that confirmation is a process violation. The duty to ask is yours: when in doubt, ask me — do not assume, and do not wait for me to question you. This overrides any default "just start planning" behavior.**
- Use the template for engineering tasks by default: coding, architecture changes,
  refactors, documentation, config-flow work, and other repository changes. Not required
  for casual conversation or pure Q&A without repository changes.
- If one or more blocks are missing in an engineering task request, do not silently
  invent them — ask for each missing block. Every block should be present unless the user
  explicitly opts out — **except «Роби за шаблоном» (OPTIONAL)**: its absence is never an
  ask; pick a pattern yourself only when one clearly fits, never gate on it.

What a confirmation ("go") authorizes, and what "done" means:

```clojure
(def go-contract
  {:authorizes "only the actions needed for the :result of the CURRENT confirmed task map"
   :research-go "a task whose :result is findings/a plan authorizes research and planning — NEVER implementation"
   :implementation-go "implementation born from research = a NEW task map + a NEW go"
   :expires "on completion of the confirmed task, or when its scope materially changes"
   :revoked-by "an interrupt or a user question — answer only, zero actions until a fresh go"})
```

```clojure
(def done-contract
  {:result :required                    ;; the stated :result is met
   :accept :when-present                ;; every :accept meter reads its :target — "almost" does not exist
   :diagnostic :when-code-changed       ;; careful re-read of the edited scope; Codex has no C# analyzer
   :runtime :when-only-user-can-verify  ;; Unity-side check stays the user's authority
   :commit :only-when-requested
   :never "a checked checkbox or the mere fact of editing files"})
```

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

- The HARD GATE and the missing-block rules apply UNCHANGED to Clojure input: an absent
  key means "ask about that block, aiming the question at the specific map" — never "no
  constraints". The exceptions are `:pattern` and `:accept`: both optional, an absent one
  is never an ask — never gate on them.
- The universal notation is defined ONCE — the canonical glossary autoloads for you from
  `~/.codex/AGENTS.md` (Codex-native copy; canon home `~/.claude/CLAUDE.md`). The block
  below adds PROJECT-scoped readings only:

```clojure
(def notation-ecs-ext  ;; 2026-07-17 — project-scoped notation extension (ECS); universal forms stay global
  {:entity-shape "(def <Archetype> {:archetype … :tag … :pk … :fk … :kind … :state … :data …}) — one map = one entity; keys anchor to tag-law / key-role-law (ARCHITECTURE.md)"
   :set-cardinality "the FIELD decides the #{} reading: singular-valued key (:home, :tag) → global 'one of'; collection-valued key (:data, :fk) → ALL members, unordered, no duplicates (= ECS composition)"
   :tag-never-set "a #{} under :tag is not alternative syntax — it DISPLAYS a Tag Law violation (2 identity tags)"})
```

## Code Knowledge Policy (tool-first — module MDs abolished 2026-07-09)
- **Module/domain/presentation MD files do not exist and must NEVER be recreated.**
  A stale doc poisons context worse than no doc.
- Knowledge lives in four non-rotting forms:
  1. **Derived** — ECS/DI relationships via the `ecs-graph` / `di-graph` skills; doc
     symbol claims are lint-checked by `Tools/doc_lint.py`.
  2. **Code comments at distance zero** — intent, non-obvious invariants, and contracts
     live in a short comment ON the thing itself, updated in the same diff. A comment
     about ANOTHER file is a rot seed — link by name only.
  3. **Dated records** — the "why" of a change belongs in the commit message;
     cross-domain target contracts are dated FLOW docs (`Flows/FLOW_<NAME>.md`).
  4. **Decreed rules** — `ARCHITECTURE.md` (policy), `ECS_CONVENTIONS.md` (point-of-code
     rules), `Patterns/` (recipes). They change only by the user's decision.
- If you add or change an ECS entity archetype, refresh the ecs-graph; after changing DI
  wiring, refresh the di-graph. Both builds are deterministic — run them yourself.
- `DOC_STANDARD.md` governs the surviving doc genres. `ECS_CONVENTIONS.md` — read before
  writing or editing any ECS system, component, event, config, or query.

## OpenSpec Policy

```clojure
(def openspec-policy  ;; 2026-08-05 — removed from the active workflow of BOTH agents
  {:project-integration :removed        ;; skills + openspec/ deleted
   :cli :available                      ;; global `openspec` CLI stays installed
   :future-use :explicit-only           ;; never triggers by itself — only by the user's direct ask
   :reconsider-only-when "довготривала capability spec із реальними delta requirements"})
```

## Unity Build Policy
- This is a Unity project. Do not run Unity project builds from the agent side.
- Do not run `dotnet build`, `msbuild`, `xbuild`, or Unity CLI build commands.
- If compile validation is needed, request a Unity-side check from the user. Codex has no
  C# analyzer surface — re-read your edits carefully before asking; the user's Unity
  check is the authority, not a substitute for your own review.
- Do not read Unity scene files (`.unity` or other scene-serialized assets) unless the
  user explicitly allows it in the current task.
- Never generate or hand-write Unity `.meta` files. If a `.meta` file is needed, stop and
  ask the user.
- Unity-side authoring (prefabs, `.asset`, scenes, addressable entries) is the user's:
  report it as a user-side fact, never as open code work.

## Code Quality And Review Bar
- Write code that is ready to pass strict review.
- Review incoming code as if done by a senior engineer with 20 years of experience in a very critical mood.
- Prefer instance-based design; introduce `static` only when there is a clear architectural reason.
- Default to the SIMPLEST structure that solves the task. Patterns serve the problem —
  reach for one ONLY when cardinality or real complexity demands it, never because a doc
  or code comment mentions it.
- Never ship a knowingly-wrong placeholder behind "out of scope". When a value needs a
  real source you do not yet have, surface it as a decision during Plan — do not
  implement a stub and present it as done.
- Fail loud: throw on missing/invalid prerequisites; never silent return or
  log-warning-and-skip; cancellation is the only quiet return.
- No automated-test proposals: the project's net is fail-loud throws + the user's
  playtest loop.

## Code Documentation Policy
- Add comments only where the logic stops being simple and unambiguous.
- Prefer short targeted comments for non-obvious algorithmic constraints, decisions, and invariants.
- Do not add boilerplate XML documentation by default.
