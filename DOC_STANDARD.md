---
category: C
read: trigger
trigger: "before authoring or reviewing any .md (Flows / Patterns / policy) for standard compliance"
tags: [docs, conventions]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[INDEX](INDEX.md)"
---

# DOC_STANDARD.md

Single source of truth for how to write Markdown docs in this project.
Read this before creating or editing any `.md` file.

> **Module/domain/presentation MDs are ABOLISHED (2026-07-09) — never recreate them.** Present-tense
> narrative about code state rots faster than curation keeps up. Module knowledge lives in code
> comments (distance zero), tools (`roslyn` / `ecs-graph` / `di-graph`), commit messages, and the
> genres below. `Tools/doc_lint.py` lint-checks every doc's symbol claims against the code.

---

## Rule 0 — Documentation is written for the AI agent, not for a human

Every `.md` file in this project is written for an AI agent that will act on it. Write for the
**least-capable agent likely to read it**: explicit, literal, no implied reasoning chains.

## Rule 1 — Only non-rotting genres

A doc may hold ONLY content that does not decay when code drifts:

```clojure
(def doc-genres
  {:flow-contract "Flows/FLOW_<TASK>.md | Flows/Archive/FLOW_<TASK>.md"  ;; ONE persistent Category A artifact per engineering task — Rule 2
   :recipe        "Patterns/PATTERN_*.md"  ;; one-approach-per-file skeleton for a code role; changes when the convention changes
   :policy        "root *.md"              ;; ARCHITECTURE / ECS_CONVENTIONS / CLAUDE / this file / GENERAL_UI_STYLE / GLOSSARY
   :never         "present-tense mirror of code state"})  ;; current-state prose, rosters, wiring — tools own those
```

```clojure
;; ── structural facts: a tool owns them, a doc NEVER does ────────────────────
(def tool-owns
  {#{types signatures references hierarchy outline}                   roslyn-mcp
   #{writers readers reactive-consumers archetypes PK-FK priorities}  ecs-graph
   #{registered-as Lifetime installer injectors collections GameMode} di-graph
   #{asmdef-reachability layering}                                    Tools/asmdef_reach.py
   :doc-symbol-claims                                                 Tools/doc_lint.py
   :domain-term->code-anchor                                          GLOSSARY.md})
```

A live code symbol may appear in a doc only as an ANCHOR (bare name) — doc-lint verifies it exists.
A symbol that no longer exists in the code may NOT appear as an anchor — state it inside a `"string"`
as the past fact it now is.

---

## Rule 2 — A Category A doc has THREE stages, and they rot at different rates

The split is not organisational. Each stage has a different **lifetime**, and holding them in one
undifferentiated file lets the shortest-lived content drag down the longest-lived.

```clojure
(def stages  ;; user 2026-08-06 — the three named sections of every Category A doc, in this order
  {:request  {:owns "the user's own statement of the task, plus the dated log of his decisions"
              :lifetime :immutable                  ;; a dated historical fact — it cannot rot
              :on-completion "stays, verbatim"}
   :contract {:owns "what must be TRUE: invariants, semantics, state ownership, open gaps"
              :lifetime "changes by decision, never by drift"
              :on-completion "stays when it remains operational; otherwise the whole FLOW becomes historical"}
   :plan     {:owns "what to DO: files, order, traps, meters"
              :lifetime :dies-on-completion
              :on-completion :harvest-then-drop}})  ;; Rule 2d
```

```clojure
(def stage-boundary  ;; the ONE test that decides where a sentence belongs
  {:question "does this sentence survive a total rewrite of the implementation?"
   :yes      :contract
   :no       :plan                    ;; it names a file, a system, or an order of steps
   :why "the boundary is the sentence's LIFETIME, not its subject — the same subject appears in both stages"})
```

### Rule 2a — `:request` is verbatim

```clojure
(def request-rule
  {:copy      :verbatim        ;; the user's words, unedited — prose or Clojure, whichever he wrote
   :dated     :required
   :alongside "the agent's normalised task map, explicitly marked as the AGENT's restatement"  ;; two forms side by side, never one merged form
   :why "the request is the only artifact with a zero rot coefficient; a paraphrase is already a lossy read of intent"
   :never     "reconstructing a request that was not preserved"})   ;; mark its ABSENCE — a reconstruction is the agent's words wearing the user's date
```

```clojure
(def request-amendments
  {:home :request
   :entry {:received-at "date/time or ordered turn marker"
           :raw-request :verbatim
           :normalized "Clojure patch against the confirmed task"
           :confirmed "true only after explicit user confirmation"}
   :append-only true
   :precedence (cond (confirmed-amendment?) "patch only the fields it names"
                     (unconfirmed-amendment?) :no-authority
                     (sources-conflict?) "stop and ask the user"
                     :else "the current confirmed contract governs execution")
   :never "replace or rewrite the raw request; it remains the immutable source of intent"})
```

```clojure
(def one-task-one-flow  ;; user 2026-08-06 — the FLOW is the task's only cross-session artifact
  {:scope       "EVERY engineering task — code, architecture, refactor, docs, config-flow, tooling"
   :unit        "ONE confirmed task map or vector batch → ONE new FLOW"
   :created     "the FIRST write after the initial go; before Research starts"
   :identity    "FLOW_<TASK>.md — uppercase snake-case of the confirmed :task id"
   :batch       "a vector batch needs one shared FLOW name in the confirmed restatement"
   :template    "FLOW_TEMPLATE.md — copy-skeleton; the FLOW shape lives ONLY there"
   :contains    (-> :request :contract :plan)
   :never       #{"chat-only engineering plan" "a self-deleting PLAN_ file" "implementation before the fresh implementation-go"}})
```

### Rule 2b — `:contract` closes before `:plan` starts

```clojure
(def contract-closed
  {:criterion "ZERO open resolutions"   ;; every `?` / `:by-<source>` has become `:decided`
   :amendments "each confirmed Request amendment patches the durable Contract decision it affects"
   :gate      "a plan step may not execute while a decision it depends on is open"
   :marker    "state the closure IN the doc — either «every resolution is CLOSED, execute in order» or an explicit :blocks edge"
   :why "validated twice before it was written down: a fully-closed batch executed clean, and an open decision correctly stalled a later stage instead of being stubbed"})
```

### Rule 2c — `:plan` owns progress, resume state, and blockers

```clojure
(def plan-progress
  {:shape {:status #{:active :blocked}
           :completed #{}
           :current ?
           :remaining #{}
           :resume-context "the smallest sufficient state for the next session"
           :blocker "required only while :status is :blocked"}
   :update "after a material plan transition and before a session handoff"
   :resume (-> "read Request amendments"
               "apply only confirmed patches"
               "restore the latest progress shape"
               "continue from :current")
   :compatibility "for an older FLOW without the shape, reconstruct state from its existing Plan"})
```

```clojure
(def blocked-flow
  {:meaning "work cannot continue without concrete user authority, a runtime decision, or an external-state change"
   :frontmatter {:read :always :status :partial}
   :requires #{:blocker :needed-authority :next-action}
   :resume "re-enter through the unresolved blocker"
   :never #{:implemented :archive "treating blocked as complete"}})
```

### Rule 2d — `:plan` is harvested, then dropped

```clojure
(def harvest  ;; what happens to :plan once every stage has landed
  {:hazard->code        "a trap about ONE system → a comment at distance zero, in the same diff"
   :invariant->contract "a rule that survives a rewrite → the :contract section of this same doc"
   :toolchain->policy   "a fact about Unity / asmdef / the build → a PROPOSAL to the user"  ;; ECS_CONVENTIONS and ARCHITECTURE change by user decision only
   :rest                :drop        ;; the record is the commit log — already decreed
   :who                 "the agent proposes the split; the user vetoes"
   :tombstone           "leave 3 lines: harvested on <date>, what went where, record = commit log"  ;; stops the next reader re-adding it
   :never               "an executed plan left inside a live contract"})
```

### Rule 2e — the FLOW's completion fate

```clojure
(def flow-fate
  {:active   {:path "Flows/FLOW_<TASK>.md"         :read :always  :status :partial}
   :contract {:path "Flows/FLOW_<TASK>.md"         :read :trigger :status :implemented}
   :history  {:path "Flows/Archive/FLOW_<TASK>.md" :read :archive :status :implemented}
   :test     "does a durable contract remain useful before a future change?"
   :yes      "keep the FLOW trigger-readable; its Request and decisions remain the task history"
   :no       "move the FLOW to Archive; preserve the exact historical vocabulary"
   :archive  {:never-current-truth true
              :code-refs :remove                 ;; old symbols are history, not current declarations
              :index "counted, never listed individually"
              :doc-lint "body symbols are exempt from current-code ghost detection"}
   :never    "delete a FLOW file"})
```

---

## Rule Style — Clojure rule blocks (all categories)

Mechanizable rules — conditions→verdicts, invariants, checklists, do/don't lists — are written as
**Clojure rule blocks** inside a `clojure` code fence: real Clojure syntax (parses in a Clojure
editor), instruction semantics (nothing evaluates).

```text
(def subject {:key value …})   ;; named rule-set: the standing facts/invariants of ONE subject — THE default
{:key value}                   ;; tiny rule-set / single rule — a bare map, no def needed
(cond test result …)           ;; runtime branching: first true test wins, :else = fallback
(-> a b c)                     ;; pipeline / flow
```

- **`(def subject {…})`** — the workhorse: subject named once, one `:key value ;; why` entry per
  rule; align the value column. Keys are kebab-case concepts or constraint keys
  (`:requires :never :must-not :contains :only-when :exists-only-under :in`).
  A nested map = that entry's own fields (depth ≤ 2).
- **values** — bare symbol / exact API name = literal anchor (`EventCleanupSystem`); `"string"` =
  prose leaf (all fuzziness lives in quotes); `#{a b}` = equal alternatives; `:keyword` = verdict.
- **`;; why`** — one clause per entry. A "why" needing more than a clause is a design decision →
  prose section, not a rule entry.
- The reading glossary lives in ONE place: `~/.claude/CLAUDE.md` → "Clojure instruction notation".
  A new form/literal gets its glossary row there BEFORE it appears in any doc.
- `;; ── section label ───` divider comments group related entries inside one fence.

```clojure
(def conversion  ;; what converts to a rule block, what stays prose
  {#{invariants checklists do-donts condition->verdict} "Clojure rule block"
   #{intent design-rationale behavioral-contract}       "prose"              ;; the nuance IS the payload
   :code-skeleton     "its own language"                ;; C# stays C#
   :doc-reference     "bare name inside a fence"        ;; the doc↔doc edge lives in frontmatter related
   :extending-a-block "in kind"})                       ;; one new entry per rule — never grow it back into prose
```

---

## Document Categories

| Category | What it is | Files | Rule |
|---|---|---|---|
| **A — Task FLOW** | ONE engineering task in three staged sections (Rule 2): the user's request, the contract of what must be true, and the live plan | `Flows/FLOW_*.md`, `Flows/Archive/FLOW_*.md` | Active/contract FLOWs are diffable against the tools; archived FLOWs are immutable task history, not current-code claims. |
| **B — Template / Reference** | How to build new code, or how to use a tricky API | `Patterns/PATTERN_*.md` (incl. `ADDRESSABLE_PATTERNS.md`) | Do **not** strip. Keep accurate, keep complete. Examples use placeholder names (`Foo*`, `My*`) or live anchors that pass doc-lint. |
| **C — Policy** | Project-wide rules | `CLAUDE.md`, `ARCHITECTURE.md`, `ECS_CONVENTIONS.md`, `GENERAL_UI_STYLE.md`, `GLOSSARY.md`, this file | Rules and orientation. Change by user decision only. |

---

## Frontmatter (YAML properties)

Navigation only — doc-meta and doc↔doc relations, never code structure.

| Field | Required | Rule |
|---|---|---|
| `category` | all files | `A` \| `B` \| `C` per the table above |
| `read` | all files | `always` (session-start set; Category A means active work) · `trigger` (+ one imperative `trigger:` line) · `reference` · `archive` (completed historical FLOW only; counted but not listed in INDEX) |
| `tags` | encouraged | lowercase domain labels, never type/assembly names |
| `related` | omit if none | doc↔doc **relative markdown links** only |
| `status` | Category A only | `partial` while active · `implemented` after every acceptance meter passes |
| `code_refs` | Category A only | bare symbol names the contract reasons about, nested by kind — doc-lint drift anchors |

After adding/removing/renaming a doc or changing `read`/`trigger`/`status`: re-run
`python3 Tools/gen_index.py` (the pre-commit hook enforces it).

---

## Size budgets

```clojure
(def size-budgets  ;; mechanized by Tools/gen_index.py (warn-level, never blocks)
  {:description {:limit "120 chars" :what "the doc's first content line"
                 :why "that line IS the doc's INDEX entry — the limit is a layout fact, not a size opinion"}
   :body        {:limit :none       ;; user 2026-07-17 — the Category A line budget is ABOLISHED
                 :why "a flow contract is sized by the behavior it owns; a line count cannot know how many events, rows and invariants that behavior has"
                 :never "do not reintroduce a body-line budget, and do not split a flow doc to satisfy one"}
   :real-limit  "the genre rule (Rule 1), not a line count — content that does not fit a non-rotting genre is cut because it rots, never because the file got long"})
```

## AI-First Writing Rules

- **Explicit over implicit.** State the rule; do not make the reader infer it.
- **Short declarative sentences.** One claim per sentence.
- **Concrete over abstract.** The specific case, not a general description.
- **Mark future/unbuilt loudly** (gap list entries, `:target` values) — never imply it exists.
- **No narrative.** No "first we… then we… finally". List facts.
- First content line ≤ 120 chars — it IS the INDEX description.

---

## Checklist Before Saving Any MD

- [ ] The content fits a non-rotting genre (task doc / recipe / policy) — no present-tense
      mirror of code state, no rosters, no wiring, no priorities, no signatures.
- [ ] Category A only: the three Rule 2 sections are present and named; every sentence passed the
      `stage-boundary` test; no stage-status claim in the body (a stage's truth is its `:accept` meter).
- [ ] Category A lifecycle: active = `read: always` + `status: partial`; completion harvested the
      plan and chose `read: trigger` or `read: archive`; archived FLOW has no `code_refs`.
- [ ] Active Category A progress records completed/current/remaining/resume context; a blocked FLOW
      also records its blocker, needed authority, and next action without changing completion state.
- [ ] Frontmatter per the table; `python3 Tools/gen_index.py` re-run if doc-meta changed.
- [ ] `python3 Tools/doc_lint.py` reports no new ghosts for this doc.
- [ ] Mechanizable rules are Clojure rule blocks (Rule Style), not prose bullets.
- [ ] A junior model could act on this without reading the source.
