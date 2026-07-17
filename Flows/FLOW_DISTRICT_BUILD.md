---
category: A
read: trigger
trigger: "before touching district build, the District table, or district open conditions (Domains.Actions.BuildDistrictAction, Domains.Economy.District, Domains.Economy.DistrictOpenCondition, Presentation.Districts)"
tags: [flow, district, open-condition, cross-domain, ecs]
related:
  - "[PATTERN_TRANSACTION_ENTITY](../Patterns/PATTERN_TRANSACTION_ENTITY.md)"
  - "[PATTERN_EVENT](../Patterns/PATTERN_EVENT.md)"
  - "[PATTERN_ORCHESTRATOR_SUBSYSTEM](../Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md)"
  - "[PATTERN_REACTIVE_SYSTEM](../Patterns/PATTERN_REACTIVE_SYSTEM.md)"
status: partial
code_refs:
  systems:    [BuildDistrictActionSystem, BuildDistrictActionCancelSystem, BuildDistrictTurnTickSystem, BuildDistrictCompletionSystem, DistrictViewSpawnSystem, DistrictBuildProgressViewSpawnSystem, DistrictBuildProgressViewDespawnSystem, HexInfoPanelDistrictSystem, DistrictBuildListUISubSystem, DistrictOpenConditionEvaluatorSystem, DistrictOpenConditionEvaluatorBootstrapSystem, DistrictSingleOpenConditionEvaluatorSubSystem, DistrictExistConditionSpawnSubSystem]
  events:     [DistrictBuildUIRequestedEvent, DistrictBuildConfirmedEvent, BuildDistrictCompleteEvent, DistrictBuiltEvent, BuildDistrictCancelEvent]
  components: [DistrictIdComponent, DistrictTypeComponent, DistrictTypeFKComponent, DistrictIdAllocatorComponent, BuildDistrictTurnsComponent, ActorTypeComponent, DistrictOpenStateComponent, DistrictOpenConditionKindComponent, DistrictExistConditionComponent, HexIdFKComponent]
  tags:       [DistrictTag, BuildDistrictInProgressTag, DistrictOpenConditionTag, DistrictBuildProgressViewTag]
---

# FLOW — District Build

One row per district from confirm to built; the buildable set is derived from that same table.

This doc owns ONE behavior: the player builds a district, and the districts he MAY build are derived
from the District table. It states the TARGET contract plus the plan closing `:now` → `:target`.
Live wiring is the ecs-graph; this doc is diffable against it. Code comments link here, never retell it.

```clojure
(def participants  ;; {assembly role-in-this-flow}
  {Presentation.UI.MainHud.HexInfoPanel  "open request; in-progress block (type, turns-left, cancel)"
   Presentation.UI.DistrictBuild         "projection + commands: lists Buildable districts, raises confirm; owns NO transaction state"
   Flows.DistrictBuild                   "UI-navigation event home; leaf assembly the UI references"
   Domains.Actions.BuildDistrictAction   "verb owner: spends, opens the District row, counts turns down, closes or reverts it"
   Domains.Economy.District              "fact owner: THE District table + its stage column + the table-changed pulse"
   Domains.Economy.DistrictOpenCondition "derives DistrictOpenState from the District table, per kind"
   Presentation.Districts                "world view: progress prefab while Planned, district prefab once Built"})
```

## The unification decision (2026-07-17)

```clojure
(def unification  ;; user 2026-07-17 — supersedes the verb/fact split of 2026-07-10
  {:problem   "ONE real fact «district of type T on hex H» lived in TWO rows — the verb row and the fact row both carried hex + type"
   :symptom   "two near-identical hex-keyed view spawners; a third copy («a planned tag») was proposed and REJECTED"
   :decision  "the District row exists from CONFIRM; the build stage is a STATE COLUMN on it"
   :verb-row  "shrinks to pure verb state + an FK into the District PK space — it copies NO district attribute"
   :bans      #{"a second tag for the planned stage" "a mirror row in another domain" "a hex/type copy on the verb row"}
   :why-dag   "Domains.Economy must not reference Domains.Actions; an evaluator reading only Economy rows needs no such edge"
   :tag-law   "1 tag per entity holds: the stage is a …StateComponent column, never a toggled tag"})
```

## Entity model (target)

<!-- doc-lint: off -->

```clojure
(def District  ;; Domains.Economy.District — the ONE row per district
  {:tag DistrictTag :pk DistrictIdComponent :fk HexIdFKComponent :data DistrictTypeComponent
   :state ^:new DistrictBuildStateComponent})   ;; #{Planned Built}; PK allocated at CONFIRM (was: at completion)

(def BuildDistrictAction  ;; Domains.Actions — pure verb, no district attributes
  {:tag BuildDistrictInProgressTag
   :fk  ^:new DistrictIdFKComponent             ;; → District PK; REPLACES the HexIdFK + DistrictTypeFK copy
   :data [BuildDistrictTurnsComponent ActorTypeComponent]})

(def DistrictOpenCondition  ;; unchanged by this flow
  {:tag DistrictOpenConditionTag :fk DistrictTypeFKComponent   ;; the GATED type
   :kind DistrictOpenConditionKindComponent :state DistrictOpenStateComponent
   :data DistrictExistConditionComponent})                     ;; Exist kind only: the REQUIRED type
```

<!-- doc-lint: on -->

```clojure
(def lifecycle
  {:confirm  (-> "spend AP + resources (all-or-nothing)" "allocate PK" "District row := Planned" "verb row := countdown + payer")
   :complete (-> "District row := Built" "dispose the verb row")
   :cancel   (-> "refund per the same-turn rule" "dispose BOTH rows")
   :invariant "a District row has exactly 2 exits: Built (terminal) or disposed (cancel) — nothing else removes it"})
```

## Event vocabulary (the contract)

```clojure
(def events
  {DistrictBuildUIRequestedEvent {:home Flows.DistrictBuild :payload :none :semantics "player asked to open the overlay"}
   DistrictBuildConfirmedEvent   {:home Domains.Actions :payload "HexCoord + DistrictType + ActorType" :semantics :command}
   BuildDistrictCancelEvent      {:home Domains.Actions :payload "HexCoord" :semantics :command}
   BuildDistrictCompleteEvent    {:home Domains.Actions :payload :none
                                  :semantics "doorbell: ≥1 countdown sits at 0; LEVEL-TRIGGERED — re-raised every turn until consumed"}})

(def command-vs-fact  ;; the rule the S3 fold enforces
  {:command {:consumer "the verb owner ONLY" :never "a view may not react to a command"}
   :fact    {:consumer "anyone deriving state — views, projections, evaluators"
             :why "a view on a command depends on system ORDER inside one tick; a view on a fact does not"}})
```

<!-- doc-lint: off -->

```clojure
(def target-event  ;; ^:new — DistrictBuiltEvent folds into it (S3)
  {^:new DistrictTableChangedEvent
     {:home Domains.Economy.District :payload ^:new DistrictTableChange   ;; #{Planned Built Removed}
      :shape "struct : IEquatable<> — the value IS the multimap key; filter via EntityMultiMap keyed by value"  ;; user 2026-07-17
      :raised-by "Domains.Actions (legal: Actions → Economy) — the owner DECLARES the signal, the mutator pulses it"
      :semantics "FACT: a District row entered this stage"
      :deviation "PATTERN_EVENT prescribes payload-less; the field is a DECIDED filter key, not a payload copy"
      :why-not-a-copy "Removed has no row left to read the stage from — the column cannot express it"
      :note "reconcile stays global + idempotent; a repeat pass in one frame is a no-op, never a correctness case"}})
```

<!-- doc-lint: on -->

> **Not ECS — local C# events.** Close/dismiss, district-selection, payer-switch and cancel-click are
> view→system C# events (`PATTERN_VIEW_SYSTEM`). Only the events above cross a frame or assembly boundary.

## State ownership

```clojure
(def state-ownership  ;; only the rows this flow CHANGES; selection/payer/selected-hex stay as they are
  {:district-fact {:now    "Economy District row written at COMPLETION; hex + type duplicated on the verb row meanwhile"
                   :target "written at CONFIRM, carrying the stage column; the verb row holds NO district attribute"}
   :build-stage   {:now    "implicit — «planned» = a verb row existing in another domain; unreadable from Economy"
                   :target "explicit — the stage column on the District row; the single source for every consumer"}
   :open-state    {:now    "DistrictOpenStateComponent, SingleOpen only — Exist rows are spawned and never evaluated"
                   :target "every kind evaluated; re-derived on every District-table change, not only per turn"}})
```

## Ordering invariants

```clojure
(def ordering-invariants
  {:list-before-others "DistrictBuildListUISubSystem populates FIRST — it default-writes the selection the other sections read"
   :completion-pulse   {:producer "raise EVERY turn while any countdown sits at 0 (level-triggered)"
                        :consumer "reconcile the WHOLE in-progress set off state; never trust single delivery"
                        :why "this is just the decreed reactive contract (ECS_CONVENTIONS → Decomposition Rules): the pulse says «something changed», the system works off CURRENT state. Level-triggering makes the pair robust to a coalesced pulse."
                        :not-the-reason "«a lost pulse costs a turn of latency» — VERIFIED FALSE 2026-07-17. A turn phase raises its pulse from a SwitchToMainThread continuation, which resumes at PlayerLoopTiming.Update — BEFORE MonoBehaviour.Update, where Boot ticks the ECS loop. The pulse therefore lands OUTSIDE the tick and cannot be cleaned up before its consumer runs; a turn-phase pulse cannot be lost. Do NOT re-derive the old justification from this pairing, and do not treat pulse-loss as a hazard this flow defends against."}
   :dispose-before-reconcile "a system disposing rows runs BEFORE the views reconciling against them, same tick"
   :panel-above-producers    "HexInfoPanelDistrictSystem runs ABOVE every producer it reacts to — below them the pulse dies in the same frame"
   :evaluator-vs-ui          "NOT a same-tick ordering: the build list projects DistrictOpenState on the window-open pulse only, never every tick, while the evaluator runs on a turn boundary or a table change — earlier frames. They cannot compete in one tick. Do not invent a priority coupling between them."})
```

## Open-condition contract

```clojure
(def open-conditions
  {:derived-not-authored "DistrictOpenStateComponent is DERIVED from the District table; no system may author it as input"
   :hosts             "ONE DI-collected subsystem family, three hosts — evaluation logic is never duplicated per host"
   :kind-coverage     {:law "every DistrictOpenConditionKind member has exactly ONE evaluator subsystem"
                       :why "a spawned row with no evaluator is stuck at its seeded state forever — silently unbuildable"}
   :change-only-write "Set() the state only on an actual value change — the family is idempotent by contract"
   :threading         "the family Sets an EXISTING state column only — legal off-thread (ECS_CONVENTIONS Law 1); the turn host deliberately does NOT hop, because a hop costs a frame and buys nothing here"})

(def kind-semantics  ;; user 2026-07-17 — what each kind counts
  {SingleOpen {:closed-when "≥1 District row of the gated type exists in ANY stage" :counts #{:Planned :Built}
               :why "a build already under way must not be offered a second time"}
   Exist      {:buildable-when "≥1 District row of the REQUIRED type is Built" :counts #{:Built}
               :why "a prerequisite only counts once it actually stands"}})   ;; user: «лише якщо він збудований»
```

## Gap list (`:now` ≠ `:target`)

```clojure
(def gaps
  {:g1 {:what "hex + type duplicated across the verb row and the fact row" :cost "every consumer picks a source; a third copy was already being proposed"}
   :g2 {:what "the build stage is unreadable from Domains.Economy"          :cost "SingleOpen cannot count in-progress builds without a DAG violation or a mirror"}
   :g3 {:what "Exist-kind conditions are spawned with no evaluator"         :cost "every Exist-gated district is permanently unbuildable; the seeded Closed state never flips"}
   :g4 {:what "the turn host's comment declares «every world write goes back on the main thread» — stricter than ECS_CONVENTIONS Law 1, which allows an off-thread write to an EXISTING component"
        :cost "the commented-out hop below it reads as a missing guard; it is a deliberate optimisation (a hop costs a frame). The comment is rot — it already misled one reader into filing a phantom bug."}
   :g5 {:what "views react to COMMAND pulses and lean on same-tick system order" :cost "a priority change silently breaks the view; the dependency is invisible at the call site"}
   :g6 {:what "SingleOpen's header claims the District table is «pure scaffold — nothing spawns it yet»" :cost "false since the build flow landed; a stale premise misleads the next reader"}})
```

## Plan

Every `^:new` name is a PROPOSAL pending veto (naming policy: self-sufficient without the namespace).
`:resolve` legend: `?` = user's call · `:by-code` · `:by-policy` · `:by-naming-policy` · `:decided`.
Every `:resolve` is CLOSED — no step waits on a decision; execute in order.

```clojure
(def plan-preconditions  ;; checked 2026-07-17 — do not re-derive
  {:asmdef "NO new asmdef reference is needed: Presentation and Presentation.UI already reference Domains.Economy, and Domains.Actions already references it too — every cross-assembly edge this plan uses exists"
   :dag    "Domains.Economy gains NO reference to Domains.Actions in any step; if a step seems to need one, the step is wrong"
   :unity  "one user-side step exists (a new folder's .meta, see :add-table-changed-event); everything else is plain code"})
```

<!-- doc-lint: off -->

```clojure
[{:task :fix-evaluator-comment   ;; S1 — was «restore the commented-out hop»; RETRACTED 2026-07-17
  :closes :g4  :where DistrictOpenConditionEvaluatorSystem
  :retracted "the missing hop was filed as a bug (:g4) and is NOT one: the family Sets an EXISTING state column, which Law 1 permits off-thread, and a hop would cost a frame"   ;; user 2026-07-17
  :root-cause "the file's own comment declares a stricter rule than the decreed one — the agent trusted the comment instead of ECS_CONVENTIONS"
  :do "delete the commented-out SwitchToMainThread line and rewrite the comment to state the REAL reason: this family writes existing components only, so it stays on the pool by design"
  :skip "the hop itself — restoring it would be a regression"
  :accept {:meter "code read" :target "no commented-out code; the comment cites Law 1's structural-vs-value line, not a blanket write rule"}}

 {:task :unify-district-row     ;; S2 — the core; needs nothing
  :closes #{:g1 :g2}  :where [Domains.Economy.District Domains.Actions.BuildDistrictAction]
  :decided "row from confirm; stage as a column"   ;; user 2026-07-17
  :add [{:file ^:new DistrictBuildState :where "Economy/District/Data/" :shape "enum #{Unknown Planned Built}"}   ;; Unknown=0, per the DistrictOpenState precedent
        {:file ^:new DistrictBuildStateComponent :pattern PATTERN_COMPONENT :shape "stage column; IEquatable — a self-index key"}
        {:file ^:new DistrictIdFKComponent :pattern PATTERN_COMPONENT :shape "FK into the District PK space; IEquatable — a multimap key"}]
  :trap "EVERY query over the verb row is invalidated: it loses HexIdFKComponent + DistrictTypeFKComponent, so any With<> chain naming them matches NOTHING. A stale chain does not fail — it yields an EMPTY set and the system silently no-ops. Re-read every verb-row query before assuming it is unchanged."
  :change {BuildDistrictActionSystem (-> "spend (rule unchanged)" "allocate the PK here — move the DistrictIdAllocatorComponent seed off the completion system"
                                         "create the District row := Planned" "create the verb row := tag + DistrictIdFK + turns + payer"
                                         "verb row NO LONGER carries HexIdFKComponent or DistrictTypeFKComponent")
           BuildDistrictCompletionSystem (-> "REWRITE the base query — its current chain names HexIdFKComponent + DistrictTypeFKComponent, which the verb row no longer has (see :trap); filter on the tag + turns + the new FK instead"
                                             "reconcile off TurnsLeft<=0 (rule unchanged)"
                                             "resolve the District row via the FK → Set stage Built"
                                             "dispose the verb row" "NO LONGER creates the fact row — it already exists")
           BuildDistrictActionCancelSystem (-> "REWRITE the hex-keyed multimap — it currently indexes the VERB row by HexIdFKComponent, which is gone (see :trap); index the DISTRICT table by hex instead"
                                               "resolve the District row by hex (the pulse payload) → its PK"
                                               "resolve the verb row by the FK = that PK"
                                               "read DistrictType from the District ROW (was: the verb row)" "refund (rule unchanged)" "dispose BOTH rows")
           BuildDistrictTurnTickSystem "query + logic unchanged — it names only the tag and the turns column, both of which the verb row keeps"}
  :skip "the refund maths, the AP rule, the level-triggered pulse discipline — all landed, none in scope"
  :accept [{:meter ecs-graph :target "the BuildDistrictAction archetype carries no HexIdFKComponent and no DistrictTypeFKComponent"}
           {:meter "playtest" :target "a confirmed build still COMPLETES — the silent-empty-set trap above is the likeliest way this step regresses"}]}

 {:task :add-table-changed-event   ;; S3 — needs :unify-district-row
  :where Domains.Economy.District   ;; closes nothing on its own — see :enables below
  :add [{:file ^:new DistrictTableChange :where "Economy/District/Data/" :shape "enum #{Unknown Planned Built Removed}"}
        {:file ^:new DistrictTableChangedEvent :where "Economy/District/Events/" :pattern PATTERN_EVENT}]  ;; shape + rationale: target-event above
  :user-side "Events/ does not exist under Economy/District/ — a NEW folder needs a Unity-generated .meta. Agents never hand-write .meta: create the folder Unity-side, or ask. Everything else in this step is plain code."
  :raise {BuildDistrictActionSystem "{Planned} after the row is created"
          BuildDistrictCompletionSystem "{Built} once per pass, after the stage writes"
          BuildDistrictActionCancelSystem "{Removed} after the rows are disposed"}
  :delete DistrictBuiltEvent   ;; folded; consumers migrate in :rewire-presentation
  :enables :g5   ;; the pulse this step adds is what lets :rewire-presentation actually close :g5
  :accept {:meter ecs-graph :target "DistrictBuiltEvent absent; DistrictTableChangedEvent has 3 producers"}}

 {:task :rewire-presentation   ;; S4 — needs :add-table-changed-event
  :closes :g5  :where [Presentation.Districts Presentation.UI.MainHud.HexInfoPanel]
  :change {DistrictBuildProgressViewSpawnSystem
             {:trigger "{Planned}"   ;; was DistrictBuildConfirmedEvent — a COMMAND
              :reconcile "District rows staged Planned with no progress view (keyed by hex)"
              :gain "kills the same-tick order-dependency its header declares (it ran right after BuildDistrictActionSystem)"}
           DistrictViewSpawnSystem
             {:trigger "{Built}" :reconcile "District rows staged Built with no district view"}
           DistrictBuildProgressViewDespawnSystem
             {:trigger "#{Built Removed}" :reconcile "progress-view rows whose hex has no Planned District row — covers completion AND cancel with one rule"}
           HexInfoPanelDistrictSystem
             {:trigger "WithEither: SelectedHexChangedEvent | TurnCompletedEvent | DistrictTableChangedEvent"   ;; 4 disjuncts → 3: confirm + cancel fold in
              :data "district type from the District ROW; turns-left from the verb row via the FK"
              :block-rule "in-progress block ⟺ the selected hex's District row is staged Planned"}}
  :skip "the panel's other block states, the icon config, the cancel C# event — all landed"
  :accept {:meter ecs-graph :target "no Presentation system reacts to DistrictBuildConfirmedEvent or BuildDistrictCancelEvent"}}

 {:task :evaluate-open-conditions   ;; S5 — needs :unify-district-row + :add-table-changed-event
  :closes #{:g3 :g6}  :where Domains.Economy.DistrictOpenCondition
  :change {DistrictSingleOpenConditionEvaluatorSubSystem
             {:query :unchanged   ;; its multimap over DistrictTag + DistrictTypeComponent ALREADY spans every row
              :why "after :unify-district-row that map holds Planned AND Built rows — the in-progress case arrives for FREE"
              :do "correct the stale header (:g6): the table is no longer scaffold, and the query now counts in-progress builds"
              :dissolves "the proposed in-progress set on this subsystem — no BuildDistrictInProgressTag read, no Economy→Actions edge"}}
  :add [{:file ^:new DistrictExistConditionEvaluatorSubSystem :name :by-naming-policy
         :base DistrictOpenConditionEvaluatorSubSystem :pattern PATTERN_ORCHESTRATOR_SUBSYSTEM
         :do (-> "slice the condition table by kind Exist" "per row read DistrictExistConditionComponent.RequiredDistrict"
                 "look that type up in the District multimap, keep only rows staged Built"   ;; user: «лише якщо він збудований»
                 "target := Buildable when ≥1 Built row exists, else Closed" "change-only Set()")
         :register "the family is DI-collected — add it to the DistrictOpenCondition installer, or BOTH hosts silently skip it"}
        {:file ^:new DistrictOpenConditionEvaluatorTableChangedSystem :name :by-naming-policy   ;; 3rd host, sibling of …EvaluatorSystem / …EvaluatorBootstrapSystem
         :role reactive-system :pattern PATTERN_REACTIVE_SYSTEM
         :trigger "DistrictTableChangedEvent — any stage; every transition can flip a condition"
         :do "run the same DI-collected family; NO evaluation logic of its own"
         :why "per-turn re-derivation is too late: confirm and cancel must re-gate the list within the same turn"
         :wire "a new reactive system is THREE wiring steps, none optional — miss one and it never runs, silently: (1) a SystemPriorities constant; (2) installer registration, CONCRETE + .As<TheSystem>() per ARCHITECTURE di-composition; (3) manual composition into the Gameplay state in Boot. Verify with dig.py after."
         :unlike "the sibling subsystem below needs only its installer line — the family is DI-collected; a HOST is not"}]
  :hosts-after {DistrictOpenConditionEvaluatorBootstrapSystem "world-init — covers turn 1 (no Preview phase runs before it)"
                DistrictOpenConditionEvaluatorSystem "turn phase — covers future kinds depending on non-District state (population, tech)"
                ^:new DistrictOpenConditionEvaluatorTableChangedSystem "reactive — covers District-table changes within a turn"}
  :accept [{:meter ecs-graph :target "DistrictExistConditionComponent has ≥1 reader"}
           {:meter "playtest" :target "building the last SingleOpen district drops it from the list on CONFIRM, not on completion; cancel returns it"}]}]
```

<!-- doc-lint: on -->

## Decision log

```clojure
(def decisions  ;; all user calls, 2026-07-17
  {:unified-row   "District row from confirm + a stage column; a separate planned-tag REJECTED as a third copy"
   :exist-counts  "Exist counts Built only; SingleOpen counts Planned + Built"
   :triggers      "re-evaluate on new turn, confirm, completion AND cancel"
   :fold          "DistrictBuiltEvent folds into the table-changed pulse; views leave command pulses"
   :event-payload "field ON the event + IEquatable + multimap-keyed; NOT a separate component reusing the stage column — Removed has no row to carry it"
   :doc-rewrite   "this contract rewritten from scratch; the R1–R5 build history dropped — the record is the commit log"})
```
