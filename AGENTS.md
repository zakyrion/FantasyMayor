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
The precise gate semantics are `go-contract` / `done-contract` below. One confirmed task
map or vector batch owns one persistent Category A FLOW: the first `go` authorizes its
creation, Research and Plan write through it, and Execute closes it. The lifecycle governs
`:plan` and `:mutation` deliverables; a request whose deliverable kind is `:answer` takes
the answer lane instead (`deliverable-kind` / `answer-contract` below) — no gate, no FLOW.
1. **Research** — gather facts, do not accumulate source. **Tool-first + code**: ECS/DI
   structure via your installed `ecs-graph` / `di-graph` skills (never by grepping for
   `AddComponent<T>()` / `builder.Register<…>()` — generic-typed wiring is invisible to
   text search); targeted code reads (a file's header comment is its contract) — read the
   files you will edit yourself, once. In = distilled findings, not raw dumps. Any doc's
   claim about code is a HYPOTHESIS — verify names via `Tools/doc_lint.py` / a grep for
   the declaration before relying on it.
   Research depth scales with the change's semantic distance — `research-depth` block below.
   Prior art beyond the project is a standing research leg at EVERY depth (`prior-art`); a web
   search or a foreign repository passes the `outbound-gate` confirmation first. Research runs
   in two passes with a findings gate between them (`research-passes`): pass 1 returns findings
   + the pointed questions they opened — never a plan. Every recorded finding, decision,
   disproven hypothesis, and attempted approach carries its provenance — `:verified-by` + `:at`
   (`provenance` block) — so a guess and a measurement never read alike on the second pass.
2. **Plan** — restate the task via the Engineering Task Template, ask clarifying
   questions, and **wait for explicit confirmation** before any edit (the HARD GATE
   below). The implementation map is written only AFTER the user has seen the research
   findings and answered the questions they opened (the findings gate; pass 2 collapses
   when the answers open nothing). Every option offered to the user carries its
   confidence 0-100 (`option-confidence`), and an approach already in Attempted/Disproven
   is named as a return, never as a new idea (`recurrence-guard`).
   Surface ALL open decisions in ONE consolidated pass and BATCH the questions —
   a stated effect whose value-source is not given is an ask exactly like a missing
   field, never a licence to stub; do not drip questions across rounds. When new code
   will consume types across an asmdef boundary, verify the consuming `.asmdef`
   references (or run `Tools/asmdef_reach.py`) BEFORE asking for GO.
   After confirmation, the FIRST write is `Flows/FLOW_<TASK>.md`: preserve the user's
   request verbatim beside the agent restatement, then record Research, decisions and the
   implementation map in that same file. A vector batch needs one confirmed shared FLOW
   name. There are no chat-only engineering plans and no self-deleting `PLAN_` files.
3. **Execute** — edit under the standing invariants (ECS writes via `entity.AddComponent(value)` — the
   Friflo upsert path, never `ref`-mutation; instance-by-default, zero-allocation systems).
   Record acceptance evidence in the FLOW; on completion harvest durable facts, drop the
   executed plan, leave its tombstone, and retain the file under the fate rules in
   `DOC_STANDARD.md`.

<!-- BEGIN GENERATED: research-depth (Tools/gen_agents.py — never edit inside) -->
```clojure
(def research-depth  ;; 2026-08-25 — SDD 0.1.1 backport: research scales with the change's semantic distance
  {:depth {:port "inventory is enough — a port preserves semantics by construction"
           :replace "characterization + divergence hypotheses are mandatory"
           :new "hypotheses at the integration points"}
   :characterize {:only-when "the change deletes or replaces an existing mechanism"
                  :what "behavioral contract of the original: observable behavior over time, all use sites, invariants"
                  :source #{"git history" "live behavior"}
                  :gate "a replacement decision cannot be confirmed while the original's contract is missing"}
   :hypotheses "each contract clause → hypothesis 'the replacement may violate this' → a cheap check"
   :observability "the irreducibly empirical residue gets self-diagnosing guards, not predictions"
   :prior-art "an added leg of research at EVERY depth, :port included — see prior-art below"       ;; 2026-08-29 — SDD 0.1.4 backport
   :passes "two, with a findings gate between them — see research-passes below"                    ;; 2026-08-29 — SDD 0.1.4 backport
   :record "findings go into the Findings section of the active FLOW, each carrying provenance"    ;; 2026-08-29 — SDD 0.1.4 backport
   :never #{"confirm a replacement on an unverified 'the new thing already does what is needed'"
            "record a finding without saying how it was established"}})

;; ── research answers to sources, not to the model's priors (2026-08-29 — SDD 0.1.4 backport) ──

(def research-passes
  {:pass-1 (-> "sharpen the task with the user"
               "search"
               "return with the findings and the questions they opened")
   :gate "the findings gate — nothing of the plan is written before the user has seen the findings and answered"
   :pass-2 {:opened-by "the answers"
            :collapses "when the answers open nothing — planning starts at once"}
   :to-plan "only when the user agrees with what the second pass returned"
   :binds "every research phase at every depth; the deep-research skill inherits it"
   :why "a plan written on the first pass is a plan written before the user could correct the question"})

(def prior-art
  {:is "how this same problem is already solved outside this repository and this project"
   :scope :beyond-project
   :framing "what counts as an analogue is set by the task at hand — never by a fixed list; an engineering task need not rest on code"
   :relation "an added leg of the research method — it never replaces the project-native one (roslyn / graphs / targeted reads)"
   :depth-bound "every depth, :port included"
   :recorded-as "a finding like any other, carrying its provenance"
   :deep "when the question has no fast right answer, this becomes deep-research"
   :ungated #{"documentation for a library the task already names" "context7 queries"}
   :never "a silent step outside the project (outbound-gate)"})

(def outbound-gate
  {:applies-to #{:web-search :foreign-repository}
   :requires "explicit user confirmation before the agent reaches outside the project"
   :ask "name what is to be searched, in the terms of the task, and what the search is meant to settle; for a repository, name it and what is to be read there"
   :granularity "one confirmation per research pass, covering what was named in it"
   :window "after the second confirmed pass, ask whether the search may continue without a confirmation each time; the window closes with the research pass"
   :when (materially-different-from-what-was-confirmed? next-query)
   :then (:then (reformulate-with-user!)
                (wait-for-go!))
   :never #{"a silent outbound search"
            "reading another local project on the agent's own initiative"}})

(def deep-research
  {:is "a research pass for a question with no fast right answer — a pool of trade-offs rather than a lookup"
   :skill "sdd-deep-research — the globally installed plugin skill IS the engine; this canon only wires it in"
   :activated-by #{"the user, directly" "the agent asking permission the moment it sees the answer is not quick"}
   :rule "only the user switches it on"
   :standalone "runs without a FLOW; then the research document is the whole deliverable"
   :artifact "Flows/RESEARCH_<TOPIC>.md from RESEARCH_TEMPLATE.md, linked from the Findings of the active FLOW"
   :archive "moves to Flows/Archive/ together with the FLOW that links it"
   :map "option → forces → when it applies → known uses → evidence and what weakens it → confidence → what it buys → cost to build against cost to adopt → reversibility"
   :conditions "our own regime is written before anything is read — applicability is an axis of its own, not a shade of truth"
   :no-verdict "returning a map without a recommendation is a valid result"
   :never "manufacturing a recommendation the sources do not carry"})

(def evidence-weight
  {:attaches-to "every option offered and every finding a search produced"
   :confidence "the integer 0-100 of option-confidence — unchanged, and no second number beside it"
   :grounded-in "in prose, next to the number: where the belief came from — documentation, a confirmed answer, a study that measured it, a practitioner report, or the agent's own knowledge"
   :weakened-by "the named reason the evidence is thin: one source, no measurement, another context, agent inference"
   :order (-> "measured it" "ran it and reported the outcome" "asserts it" "agent knowledge")
   :agent-knowledge :lowest-weight                             ;; a statistical squeeze of text is not an observation
   :bar "reversibility sets how much evidence an option must carry — a one-way door demands strong, a two-way door tolerates thin"
   :never "collapsing the number and its ground into one figure"})

(def disconfirmation
  {:before-search "record the agent's own guess, specific enough to be provable wrong"
   :then "search for what would kill the leading option, never for what would confirm it"
   :deviation "when the search turns the question into a different one, write down what changed"
   :because "a belief recorded after the search always agrees with it, and a source that merely agrees is not a finding"
   :never "a vague hunch — it cannot be killed and buys nothing"})
```
<!-- END GENERATED: research-depth -->

## Start Working
- **Read `INDEX.md` first — and by default ONLY `INDEX.md`.** It is the generated doc map
  and the single key to every doc and canvas: read-priority (`always` / `trigger` /
  `reference`) plus a one-line description per file. Let INDEX drive all navigation — do
  **not** preload anything it does not send you to.
- Follow INDEX's read-priority: read the docs it marks `read: always` next; open
  `trigger` docs only when their condition holds, and `reference` docs on demand.
- The `always` set is dynamic: a Category A FLOW with `status: partial` is active work. Read it
  after the standing always-docs and reconstruct its current stage, findings with their provenance
  (its Findings section — see `provenance`), unresolved decisions, disproven hypotheses (its
  Disproven section — see `disproven`), attempted-and-dropped approaches (its Attempted section —
  see `attempted`), any linked `Flows/RESEARCH_*.md` document, and next plan item. If several
  active FLOWs exist, report the conflict and ask which one to resume.
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
- The confirmed statement's first write is its Category A FLOW. Creating or updating that
  FLOW is Research/Plan work; it never authorizes implementation. `DOC_STANDARD.md` owns
  the exact three-stage structure and active/contract/archive lifecycle.
- The gate holds `:plan` and `:mutation` deliverables. A request whose deliverable kind is
  `:answer` takes the answer lane instead — classification shown inline together with the
  answer, the answer itself is the artifact, no gate and no FLOW:

<!-- BEGIN GENERATED: deliverable-kind (Tools/gen_agents.py — never edit inside) -->
```clojure
(def deliverable-kind  ;; 2026-08-29 — SDD 0.1.2 backport: what kind of thing this request asks for
  {:axis [:answer :plan :mutation]
   :answer "how something works or should be done — the answer itself is the artifact"
   :plan "design or prepare future work — the plan document is the artifact, still no mutation"
   :mutation "change the world: files, code, configuration, external state"
   :source "the user's words in this request set the kind — nothing else"
   :escalation {:never "the agent promotes the kind on its own"
                :only "the user's explicit ask escalates answer → plan → mutation"}
   :go-inherits "a go inherits the kind of the confirmed map — a go on an answer or a plan never opens mutation"
   :ambiguous (:then (state-classification!)
                     (ask-user!))})

(def answer-contract  ;; 2026-08-29 — SDD 0.1.2 backport: the answer lane
  {:artifact "the answer itself — complete, direct, scoped to exactly what was asked"
   :premise "when the question carries a premise, verify it first and say plainly when it is wrong"
   :close "end by handing control back — the user decides what happens next, even when it looks obvious"
   :follow-up "a possible task may be offered in one line — never developed, never started"
   :never #{"unsolicited roadmaps, step plans, or migration guides"
            "designing or executing changes the question did not request"
            "silently escalating :answer into :plan or :mutation"
            "burying the asked answer under adjacent advice"}})
```
<!-- END GENERATED: deliverable-kind -->

- Use the template for engineering tasks by default: coding, architecture changes,
  refactors, documentation, config-flow work, and other repository changes. Not required
  for casual conversation or pure Q&A without repository changes.
- If one or more blocks are missing in an engineering task request, do not silently
  invent them — ask for each missing block. Every block should be present unless the user
  explicitly opts out — **except «Роби за шаблоном» (OPTIONAL)**: its absence is never an
  ask; pick a pattern yourself only when one clearly fits, never gate on it.

What a confirmation ("go") authorizes, and what "done" means:

<!-- BEGIN GENERATED: go-contract (Tools/gen_agents.py — never edit inside) -->
```clojure
(def go-contract
  {:authorizes "only the actions needed for the :result of the CURRENT confirmed task map"
   :kind-bound "a go inherits the deliverable kind of the confirmed map — a go on an answer or a plan never opens mutation"  ;; 2026-08-29 — SDD 0.1.2 backport
   :research-go "create the task's active FLOW, then research and plan through it — NEVER implementation"
   :implementation-go "implementation born from research = a NEW task map + a NEW go"
   :expires "on completion of the confirmed task, or when its scope materially changes"
   :revoked-by "an interrupt or a user question — answer only, zero actions until a fresh go"})
```
<!-- END GENERATED: go-contract -->

```clojure
(def done-contract
  {:result :required                    ;; the stated :result is met
   :accept :when-present                ;; every :accept meter reads its :target — "almost" does not exist
   :diagnostic :when-code-changed       ;; careful re-read of the edited scope; Codex has no C# analyzer
   :runtime :when-only-user-can-verify  ;; Unity-side check stays the user's authority
   :flow "acceptance recorded; durable facts harvested; executed plan replaced by its tombstone"
   :research-document "archived together with the FLOW that links it"  ;; 2026-08-29 — SDD 0.1.4 backport
   :commit :only-when-requested
   :never "a checked checkbox or the mere fact of editing files"})
```

<!-- BEGIN GENERATED: close-ritual (Tools/gen_agents.py — never edit inside) -->
```clojure
(def close-ritual  ;; 2026-08-07 — the explicit completion act; Claude Code: /flow-close command, Codex: follow this block manually
  {:trigger "user-invoked only — never auto-close"
   :steps (-> (check-all-acceptance!)     ;; read every :accept meter for real — never from memory
              (record-acceptance-audit!)  ;; :meter/:target/:actual/:status into the FLOW
              (harvest-plan!)             ;; DOC_STANDARD Rule 2d — agent proposes the split, user vetoes
              (choose-fate!)              ;; DOC_STANDARD Rule 2e — trigger | archive; a linked Flows/RESEARCH_* doc moves with its FLOW
              (regen-index!))             ;; gen_index pass 1 + doc_lint one-liner
   :blocked "any meter off target → FLOW stays partial; record per blocked-outcome"
   :never #{"archive with a failing meter" "close without the user's explicit ask"}})
```
<!-- END GENERATED: close-ritual -->

<!-- BEGIN GENERATED: task-template (Tools/gen_agents.py — never edit inside) -->
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
<!-- END GENERATED: task-template -->

<!-- BEGIN GENERATED: task-clojure-example (Tools/gen_agents.py — never edit inside) -->
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
<!-- END GENERATED: task-clojure-example -->

- The HARD GATE and the missing-block rules apply UNCHANGED to Clojure input: an absent
  key means "ask about that block, aiming the question at the specific map" — never "no
  constraints". The exceptions are `:pattern` and `:accept`: both optional, an absent one
  is never an ask — never gate on them.

<!-- BEGIN GENERATED: agent-output (Tools/gen_agents.py — never edit inside) -->
```clojure
(def agent-output  ;; 2026-08-25 — SDD 0.1.1 backport: artifact language vs answer language
  {:artifacts "Clojure only for artifacts: normalized task maps shown for confirmation, FLOW records, contract drafts — always framed by prose stating what the form means"
   :answers "prose for everything else: explanations, diagnoses, statuses, answers to questions"
   :forms "small: nesting ≤ 2, readable strings over invented keyword chains"
   :options "an offered set of options is never bare — every option carries its confidence (option-confidence below)"  ;; 2026-08-29 — SDD 0.1.4 backport
   :never #{"answer a question with a Clojure form"
            "reference a previously introduced label bare (:s2, :d-4) — restate its content in place"}})

(def option-confidence  ;; 2026-08-29 — SDD 0.1.4 backport: rated options
  {:when "the agent offers the user a choice — in any lane, at any gate"
   :carries "an integer 0-100 on every option in the set"
   :measures "how likely this option is the right decision"
   :is-not "certainty that a fact is true — the ground under a claim is stated by :verified-by instead"
   :estimated-before "the options reach the user, not after the user picks"
   :survives "the numbers are written into the FLOW with the option they rated, so an estimate can later be read against what actually happened"
   :never #{"an unrated set of options"
            "a number the agent would not defend"
            "collapsing likelihood and evidence quality into one figure"}})
```
<!-- END GENERATED: agent-output -->

<!-- BEGIN GENERATED: task-contracts (Tools/gen_agents.py — never edit inside) -->
```clojure
(def task-normalization
  {:input #{:prose :clojure}
   :before "classification and action"
   :raw-input :preserve-verbatim
   :canonical-ir :clojure
   :show (cond (engineering-task?) :always
               (ambiguous?) "the fields that need resolution"
               :else :may-stay-internal)
   :persist (cond (engineering-task?) "Request section of its FLOW"
                  :else :not-required)
   :never "invent or silently resolve a missing field"})

(def task-amendments
  {:home "Request section of the current FLOW"
   :entry {:received-at "date/time or ordered turn marker"
           :raw-request :verbatim
           :normalized "Clojure patch against the confirmed task"
           :confirmed "true only after the user confirms it"}
   :source-of-intent :raw-request
   :execution-source "the latest confirmed normalized contract consistent with the raw request"
   :patch "a confirmed amendment changes only the fields it names"
   :unconfirmed :no-authority
   :conflict "stop and ask; never silently merge contradictory sources"})

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

(def disproven  ;; 2026-08-25 — SDD 0.1.1 backport: refuted hypotheses are first-class
  {:home "the Disproven section of the active FLOW (Contract stage — durable, survives the plan drop)"
   :entry {:hypothesis "the refuted assumption, stated plainly"
           :refuted-by "the observation or experiment that killed it"
           :at "the date it was refuted"                       ;; 2026-08-29 — SDD 0.1.4 backport
           :details "anchor to the diagnostic block holding the full story"}
   :write "the moment a hypothesis is refuted — an index entry here, not only a line inside the diagnostic log"
   :read "before formulating any new hypothesis, reread this section"
   :why "a compacted or resumed session must not re-enter a dead end it already paid for"})

;; ── 2026-08-29 — SDD 0.1.4 backport: provenance, attempted approaches, decision hygiene ──

(def provenance
  {:attaches-to #{:findings :decisions :disproven :attempted}
   :verified-by "free prose: how this was established — ran it and watched, read the source, the documentation says so, read it outside the project, or nothing but the agent's hunch"
   :at "the date the claim was established or the decision was made"
   :why "a long session rewrites its own FLOW; without provenance a guess and a measurement read alike on the second pass, and the task walks in circles"
   :never "a recorded claim whose ground is left unstated"})

(def attempted
  {:home "the Attempted section of the active FLOW (Contract stage — durable, survives the plan drop)"
   :entry {:approach "what was tried, stated plainly"
           :confidence "the number it carried when it was offered"
           :dropped-because "what made it unusable"
           :problems "what it cost — what broke, and what it took to find out"
           :at "the date it was dropped"}
   :differs-from :disproven                                    ;; a refuted belief is not the same object as a tried-and-dropped approach
   :write "the moment an approach is abandoned"
   :read "before offering any approach (recurrence-guard)"
   :why "an R&D task that forgets its own attempts pays for each of them twice"})

(def recurrence-guard
  {:before "offering the user any approach or option"
   :read #{"Attempted" "Disproven"}
   :when (already-tried? approach)
   :then (:then (say-so-before-offering-it!)
                (state-what-changed-since!)
                (offer-it-as-a-return!))
   :never "presenting a tried approach as a new idea"})

(def decision-revisit
  {:trigger (or (new-details?)
                (new-request?)
                (outcome-contradicts-the-decision?))
   :form "a new dated entry naming the decision it supersedes, showing what was checked, what came out then, and what is new now"
   :leaves "the superseded entry exactly as it was written"
   :never #{"editing a confirmed decision in place"
            "re-deciding without saying what changed"}})

(def emergent-decision  ;; 2026-08-29 — SDD 0.1.2 backport: the late-discovered ?
  {:is "a choice that surfaces mid-execution and is absent from the confirmed task map"
   :examples #{"a runtime or SDK version" "a name" "a format default" "deleting whatever stands in the way"}
   :rule "a late-discovered ? — surface it and wait before acting, never resolve it silently"
   :batch "when ordering allows, collect emergent decisions and ask in one pass"
   :no-alternative "having no alternative is still a decision — state it before acting, not report it after"})
```
<!-- END GENERATED: task-contracts -->

- The universal notation is defined ONCE — the canonical glossary autoloads for you from
  `~/.codex/AGENTS.md` (Codex-native copy; canon home `~/.claude/CLAUDE.md`). The block
  below adds PROJECT-scoped readings only:

<!-- BEGIN GENERATED: notation-ecs-ext (Tools/gen_agents.py — never edit inside) -->
```clojure
(def notation-ecs-ext  ;; 2026-07-17 — project-scoped notation extension (ECS); universal forms stay global
  {:entity-shape "(def <Archetype> {:archetype … :tag … :pk … :fk … :kind … :state … :data …}) — one map = one entity; keys anchor to tag-law / key-role-law (ARCHITECTURE.md)"
   :set-cardinality "the FIELD decides the #{} reading: singular-valued key (:home, :tag) → global 'one of'; collection-valued key (:data, :fk) → ALL members, unordered, no duplicates (= ECS composition)"
   :tag-never-set "a #{} under :tag is not alternative syntax — it DISPLAYS a Tag Law violation (2 identity tags)"})
```
<!-- END GENERATED: notation-ecs-ext -->

## Code Knowledge Policy (tool-first — module MDs abolished 2026-07-09)
- **Module/domain/presentation MD files do not exist and must NEVER be recreated.**
  A stale doc poisons context worse than no doc.
- Knowledge lives in four non-rotting forms:
  1. **Derived** — ECS/DI relationships via the `ecs-graph` / `di-graph` skills; doc
     symbol claims are lint-checked by `Tools/doc_lint.py`.
  2. **Code comments at distance zero** — intent, non-obvious invariants, and contracts
     live in a short comment ON the thing itself, updated in the same diff. A comment
     about ANOTHER file is a rot seed — link by name only.
  3. **Dated records** — every engineering task has a dated Category A FLOW; its verbatim request and
     decision log preserve the change across sessions. The commit message remains the execution record.
  4. **Decreed rules** — `ARCHITECTURE.md` (policy), `ECS_CONVENTIONS.md` (point-of-code
     rules), `Patterns/` (recipes). They change only by the user's decision.
- If you add or change an ECS entity archetype, refresh the ecs-graph; after changing DI
  wiring, refresh the di-graph. Both builds are deterministic — run them yourself.
- `DOC_STANDARD.md` governs the surviving doc genres. `ECS_CONVENTIONS.md` — read before
  writing or editing any ECS system, component, event, config, or query.

## OpenSpec Policy

<!-- BEGIN GENERATED: openspec-policy (Tools/gen_agents.py — never edit inside) -->
```clojure
(def openspec-policy  ;; 2026-08-05 — removed from the active workflow of BOTH agents
  {:project-integration :removed        ;; skills (.claude/.codex) + openspec/ deleted
   :cli :available                      ;; global `openspec` CLI stays installed
   :future-use :explicit-only           ;; never triggers by itself — only by the user's direct ask
   :reconsider-only-when "довготривала capability spec із реальними delta requirements"})
```
<!-- END GENERATED: openspec-policy -->

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
