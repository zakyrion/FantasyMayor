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
status: implemented
code_refs:
  systems:    [BuildDistrictActionSystem, BuildDistrictActionCancelSystem, BuildDistrictTurnTickSystem, BuildDistrictCompletionSystem, DistrictViewSpawnSystem, DistrictBuildProgressViewSpawnSystem, DistrictBuildProgressViewDespawnSystem, HexInfoPanelDistrictSystem, DistrictBuildListUISubSystem, DistrictOpenConditionEvaluatorSystem, DistrictOpenConditionEvaluatorBootstrapSystem, DistrictOpenConditionEvaluatorTableChangedSystem, DistrictSingleOpenConditionEvaluatorSubSystem, DistrictExistConditionEvaluatorSubSystem, DistrictExistConditionSpawnSubSystem]
  events:     [DistrictBuildUIRequestedEvent, DistrictBuildConfirmedEvent, BuildDistrictCompleteEvent, DistrictTableChangedEvent, BuildDistrictCancelEvent]
  components: [DistrictIdComponent, DistrictIdFKComponent, DistrictTypeComponent, DistrictTypeFKComponent, DistrictIdAllocatorComponent, DistrictBuildStateComponent, ActionIdComponent, BuildDistrictTurnsComponent, ActorTypeComponent, DistrictOpenStateComponent, DistrictOpenConditionKindComponent, DistrictExistConditionComponent, HexIdFKComponent]
  data:       [DistrictBuildState, DistrictTableChange]
  tags:       [DistrictTag, BuildDistrictInProgressTag, DistrictOpenConditionTag, DistrictBuildProgressViewTag]
---

# FLOW — District Build

One row per district from confirm to built; the buildable set is derived from that same table.

This doc owns ONE behavior: the player builds a district, and the districts he MAY build are derived
from the District table. Structure follows `DOC_STANDARD.md` → Rule 2 (three stages). Live wiring is
the ecs-graph; this doc is diffable against it. Code comments link here, never retell it.

---

# 1 · Request

```clojure
(def request  ;; Rule 2a
  {:preserved :no
   :why "this doc predates the verbatim-request rule (2026-08-06); the 2026-07-17 session was cleared and only the user's ANSWERS survived — they are the decision log below"
   :verbatim-fragment "«лише якщо він збудований»"   ;; user, on what the Exist kind counts
   :never "do not reconstruct the request from the answers — a reconstruction is the agent's words wearing the user's date"})
```

```clojure
(def decisions  ;; all user calls, 2026-07-17
  {:unified-row   "District row from confirm + a stage column; a separate planned-tag REJECTED as a third copy"
   :exist-counts  "Exist counts Built only; SingleOpen counts Planned + Built"
   :triggers      "re-evaluate on new turn, confirm, completion AND cancel"
   :fold          "the old completion-only pulse folds into the table-changed event; views leave command pulses"
   :event-payload "field ON the event + IEquatable + multimap-keyed; NOT a separate component reusing the stage column — Removed has no row to carry it"
   :doc-rewrite   "this contract rewritten from scratch; the R1–R5 build history dropped — the record is the commit log"})
```

---

# 2 · Contract

```clojure
(def participants  ;; {code-home role-in-this-flow} — most keys are namespaces, not assemblies; assembly-level statements are made explicitly (see :why-dag)
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
   :verb-row  "pure verb state + an FK into the District PK space — it copies NO district attribute"
   :bans      #{"a second tag for the planned stage" "a mirror row in another domain" "a hex/type copy on the verb row"}
   :why-dag   "Domains.Economy must not reference Domains.Actions; an evaluator reading only Economy rows needs no such edge"
   :tag-law   "1 tag per entity holds: the stage is a …StateComponent column, never a toggled tag"})
```

## Entity model

```clojure
(def District  ;; Domains.Economy.District — the ONE row per district
  {:tag DistrictTag :pk DistrictIdComponent :fk HexIdFKComponent :data DistrictTypeComponent
   :state DistrictBuildStateComponent})   ;; #{Planned Built}; PK allocated at CONFIRM

(def BuildDistrictAction  ;; Domains.Actions — pure verb, no district attributes
  {:tag BuildDistrictInProgressTag
   :pk  ActionIdComponent                       ;; the action's own identity
   :fk  DistrictIdFKComponent                   ;; → District PK; there is NO HexIdFK/DistrictTypeFK copy on this row
   :data [BuildDistrictTurnsComponent ActorTypeComponent]})

(def DistrictOpenCondition
  {:tag DistrictOpenConditionTag :fk DistrictTypeFKComponent   ;; the GATED type
   :kind DistrictOpenConditionKindComponent :state DistrictOpenStateComponent
   :data DistrictExistConditionComponent})                     ;; Exist kind only: the REQUIRED type
```

```clojure
(def lifecycle
  {:confirm  (-> "spend AP + resources (all-or-nothing)" "allocate PK" "District row := Planned" "verb row := countdown + payer")
   :complete (-> "District row := Built" "dispose the verb row")
   :cancel   (-> "refund per the same-turn rule" "dispose BOTH rows")
   :invariant "a District row has exactly 2 exits: Built (terminal) or disposed (cancel) — nothing else removes it"})
```

## Event vocabulary

```clojure
(def events
  {DistrictBuildUIRequestedEvent {:home Flows.DistrictBuild :payload :none :semantics "player asked to open the overlay"}
   DistrictBuildConfirmedEvent   {:home Domains.Actions :payload "HexCoord + DistrictType + ActorType" :semantics :command}
   BuildDistrictCancelEvent      {:home Domains.Actions :payload "HexCoord" :semantics :command}
   BuildDistrictCompleteEvent    {:home Domains.Actions :payload :none
                                  :semantics "doorbell: ≥1 countdown sits at 0; LEVEL-TRIGGERED — re-raised every turn until consumed"}
   DistrictTableChangedEvent     {:home Domains.Economy.District :payload DistrictTableChange   ;; #{Planned Built Removed}
                                  :semantics "FACT: a District row entered this stage"
                                  :shape "struct : IEquatable<T> + IComponent — the value is a DECIDED payload, not an index key: no consumer slices this event by value, and it is backed by no component index"
                                  :consumed-by "anchoring on the event archetype, then reconciling globally off current state"
                                  :raised-by "Domains.Actions (legal: Actions → Economy) — the owner DECLARES the signal, the mutator pulses it"
                                  :deviation "PATTERN_EVENT prescribes payload-less; the field is a DECIDED filter key, not a payload copy"
                                  :why-not-a-copy "Removed has no row left to read the stage from — the column cannot express it"
                                  :note "reconcile stays global + idempotent; a repeat pass in one frame is a no-op, never a correctness case"}})
```

```clojure
(def command-vs-fact
  {:command {:consumer "the verb owner ONLY" :never "a view may not react to a command"}
   :fact    {:consumer "anyone deriving state — views, projections, evaluators"
             :why "a view on a command depends on system ORDER inside one tick; a view on a fact does not"}})
```

> **Not ECS — local C# events.** Close/dismiss, district-selection, payer-switch and cancel-click are
> view→system C# events (`PATTERN_VIEW_SYSTEM`). Only the events above cross a frame or assembly boundary.

## State ownership

```clojure
(def state-ownership  ;; only the rows this flow owns; selection / payer / selected-hex belong elsewhere
  {:district-fact "written at CONFIRM and carrying the stage column; the verb row holds NO district attribute"
   :build-stage   "explicit — DistrictBuildStateComponent on the District row; the single source for every consumer"
   :open-state    "every kind evaluated; re-derived on every District-table change, not only per turn"})
```

## Ordering invariants

```clojure
(def ordering-invariants
  {:list-before-others "DistrictBuildListUISubSystem populates FIRST — it default-writes the selection the other sections read"
   :completion-pulse   {:producer "raise EVERY turn while any countdown sits at 0 (level-triggered)"
                        :consumer "reconcile the WHOLE in-progress set off state; never trust single delivery"
                        :why "this is just the decreed reactive contract (ECS_CONVENTIONS → Decomposition Rules): the pulse says «something changed», the system works off CURRENT state. Level-triggering makes the pair robust to a coalesced pulse."
                        :not-the-reason "«a lost pulse costs a turn of latency» — FALSE, and doubly so since 2026-08-05: under the Event Lifecycle (ECS_CONVENTIONS) EVERY consumer sees EVERY pulse exactly once, one frame after it is raised, whatever the priorities. A pulse cannot be lost or out-raced. Do NOT re-derive the old timing justification from this pairing, and do not treat pulse-loss as a hazard this flow defends against."}
   :delete-before-reconcile "a system deleting rows runs BEFORE the views reconciling against them, same tick"  ;; system ORDER inside one tick — unrelated to event delivery, which is priority-independent
   :panel-above-producers    :DISSOLVED  ;; 2026-08-05 — was a same-tick visibility constraint; event delivery no longer depends on priority, so no consumer needs to sit above its producer
   :evaluator-vs-ui          "NOT a same-tick ordering: the build list projects DistrictOpenState on the window-open pulse only, never every tick, while the evaluator runs on a turn boundary or a table change — earlier frames. They cannot compete in one tick. Do not invent a priority coupling between them."})
```

## Open-condition contract

```clojure
(def open-conditions
  {:derived-not-authored "DistrictOpenStateComponent is DERIVED from the District table; no system may author it as input"
   :hosts             "ONE DI-collected subsystem family, three hosts — evaluation logic is never duplicated per host"
   :kind-coverage     {:law "every DistrictOpenConditionKind member has exactly ONE evaluator subsystem"
                       :why "a spawned row with no evaluator is stuck at its seeded state forever — silently unbuildable"}
   :change-only-write "write the state only on an actual value change — the family is idempotent by contract"
   :threading         "MAIN THREAD, no hop: the turn pipeline runs INLINE on the main thread, so this family — like every store reader/writer — has no thread to come back from (ECS_CONVENTIONS → Law 1). A future parallel variant would go through a CommandBuffer, recorded off-thread and played back on main (ECS_CONVENTIONS → Open Directions), which is NOT in force today."})

(def hosts  ;; the three that run the one family
  {DistrictOpenConditionEvaluatorBootstrapSystem   "world-init — covers turn 1 (no Preview phase runs before it)"
   DistrictOpenConditionEvaluatorSystem            "turn phase — covers future kinds depending on non-District state (population, tech)"
   DistrictOpenConditionEvaluatorTableChangedSystem "reactive — covers District-table changes within a turn; per-turn re-derivation is too late, confirm and cancel must re-gate the list inside the same turn"})

(def kind-semantics  ;; user 2026-07-17 — what each kind counts
  {SingleOpen {:closed-when "≥1 District row of the gated type exists in ANY stage" :counts #{:Planned :Built}
               :why "a build already under way must not be offered a second time"}
   Exist      {:buildable-when "≥1 District row of the REQUIRED type is Built" :counts #{:Built}
               :why "a prerequisite only counts once it actually stands"}})   ;; user: «лише якщо він збудований»
```

## Gaps

```clojure
(def gaps
  {:open :none})   ;; 2026-08-06 — the last entry (:g4) was retracted, see phantom-guard; a new gap is filed here when one is found
```

```clojure
(def phantom-guard  ;; dated record — kept ONLY because each entry stops a future reader from re-filing a non-gap
  {:g4      {:was "the evaluator's «missing» main-thread hop, filed as a bug 2026-07-17"
             :verdict :RETRACTED   ;; the family writes an EXISTING column, which the law permitted off-thread; a hop costs a frame
             :root-cause "the file's own comment declared a stricter rule than the decreed one — the agent trusted the comment over ECS_CONVENTIONS"
             :moot-since "2026-08-05 — FM-13 made the turn pipeline inline and main-thread-only; both the permission and the hop are gone"}
   :g7      :never-existed   ;; the FM-13 plan named a :g7 here; no such gap was ever filed — the list ran :g1…:g6
   :chimera "the district-row chimera (a row matching two identity tags at once) is GONE — eliminated by the FM-13 engine migration, not by a fix inside this flow; stop looking for one"
   :g1-g3-g5-g6 "closed 2026-07-17 by the plan; what each was and why is the commit log"})
```

---

# 3 · Plan

```clojure
(def plan  ;; Rule 2c tombstone
  {:state     :harvested-and-dropped
   :on        "2026-08-06"
   :executed  "2026-07-17 — five tasks, every :accept meter verified (ecs-graph, di-graph, user playtest)"
   :harvest   {:to-contract   "the evaluator host set, the level-triggered pulse rule, the kind semantics — all above"
               :to-guard      "the :g4 retraction and the :g7/chimera phantoms — see phantom-guard"
               :proposed-out  #{"the stale-query trap: a query filter naming a column the row no longer carries matches NOTHING — an empty set, a silent no-op, no compile error and no throw"
                                "the asmdef fact (Unity's non-SDK csproj does not expose transitive references for compilation, so a Boot-composed system needs a DIRECT asmdef reference)"}}
   :dropped   "the task bodies, their pre-FM-13 API vocabulary, the S2–S4 atomicity band, the measured blast radius"
   :record    "the commit log"
   :never     "re-adding an executed plan to this file"})
```
