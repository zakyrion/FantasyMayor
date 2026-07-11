---
category: A
read: trigger
trigger: "before changing any district-build event, system, transaction state — or any doc that retells this flow"
tags: [flow, district, cross-domain, ecs]
related:
  - "[PATTERN_TRANSACTION_ENTITY](../Patterns/PATTERN_TRANSACTION_ENTITY.md)"
  - "[PATTERN_EVENT](../Patterns/PATTERN_EVENT.md)"
  - "[PATTERN_VIEW_SYSTEM](../Patterns/PATTERN_VIEW_SYSTEM.md)"
status: partial
code_refs:
  systems:          [DistrictBuildUISystem, DistrictBuildUISpawnSystem, BuildDistrictActionSystem, BuildDistrictTurnTickSystem, BuildDistrictCompletionSystem, DistrictViewSpawnSystem]
  events:           [DistrictBuildUIRequestedEvent, DistrictBuildConfirmedEvent, BuildDistrictCompleteEvent, DistrictBuiltEvent]
  components:       [DistrictBuildSelectionComponent, BuildDistrictTurnsLeftComponent, ActorTypeComponent]
  tags:             [BuildDistrictInProgressTag]
---

# FLOW — District Build

The cross-domain contract of the district-build transaction: one player gesture (open → pick →
confirm / dismiss) spanning `Presentation.UI.MainHud.HexInfoPanel` (the open request), `Presentation.UI.DistrictBuild`,
`Flows.DistrictBuild` (the event home), `Domains.Actions.BuildDistrictAction`, `Domains.Economy`,
`Presentation.Districts`.

## Purpose

A FLOW doc owns ONE cross-domain behavior end-to-end: its event vocabulary, state ownership, and
ordering invariants, stated as a CONTRACT — including target rules the code does not meet yet.
Code comments link here and never retell the other side's half (retold halves are what rots). Live
wiring is the ecs-graph (`/ecs-graph`); this doc is diffable against it — a mismatch is either
drift to fix or a deliberate contract change. Present-tense narrative about what the code does
today does NOT live here (that rots — the tools own it); the Roadmap below states TARGET only.

```clojure
(def participants  ;; {assembly role-in-this-flow}
  {Presentation.UI.MainHud.HexInfoPanel "emits the open request (DistrictBuildUIRequestedEvent) from the selected-hex context panel"
   Presentation.UI.DistrictBuild        "projection + commands: renders the transaction, raises the confirm pulse, owns NO transaction state"
   Flows.DistrictBuild                  "flow event-vocabulary home: owns the cross-subfeature UI-navigation event; leaf assembly the UI references"
   Domains.Actions.BuildDistrictAction  "verb owner: creates the in-progress build entity on confirm (no draft); a completion consumer writes the Economy District fact"
   Domains.Economy                      "vocabulary (build configs, costs, open conditions) + TARGET home of the built-district fact"
   Presentation.Districts               "world view: spawns the district prefab for every built district"})
```

## Event vocabulary (the contract)

| Event                           | Home                | Payload                                                                    | Producer → Consumer                                                          | Semantics                                                                  |
| ------------------------------- | ------------------- | -------------------------------------------------------------------------- | ---------------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| `DistrictBuildUIRequestedEvent` | Flows.DistrictBuild | —                                                                          | `HexInfoPanelView` → `DistrictBuildUISystem`, `DistrictBuildListUISubSystem` | player asked to open the overlay for the selected hex                      |
| `DistrictBuildConfirmedEvent`   | Domains.Actions     | `HexCoord` + `DistrictType` + `ActorType` (payer)                          | `DistrictBuildUISystem` → `BuildDistrictActionSystem`                        | player committed: create the in-progress build entity directly (no draft)  |
| `BuildDistrictCompleteEvent`    | Domains.Actions     | payload-less (doorbell)                                                    | `BuildDistrictTurnTickSystem` → `BuildDistrictCompletionSystem`              | ≥1 build countdown sits at 0; LEVEL-TRIGGERED — re-raised every turn until consumed |
| `DistrictBuiltEvent`            | Domains.Actions     | payload-less                                                               | `BuildDistrictCompletionSystem` → `DistrictViewSpawnSystem`                  | a district EXISTS as a fact (`DistrictTag`); raised at build completion     |

> **Not ECS — local C# events.** Close/dismiss and district-selection are **view→system C# events** now, not
> ECS pulses: the chrome `DistrictBuildUIView` raises `Closed`/`Confirmed` (the orchestrator subscribes),
> `DistrictBuildListUIView` raises `SelectionChanged` and `DistrictBuildPriceUIView` raises `PayerChanged`
> (each section subsystem subscribes to its own view). Only the four events above cross a frame (or
> assembly) boundary as ECS pulses. Traceability = the direct C# subscriptions, all visible at the subscriber.

```clojure
(def payload-verdicts  ;; deviations from PATTERN_EVENT the contract removed
  {DistrictBuiltEvent {:siblings :dropped}})  ;; was [HexId DistrictType], now payload-less — consumer reconciles off DistrictTag
```

## State ownership

```clojure
(def state-ownership  ;; {state {:now … :target …}} — the split below is the flow's core defect
  {:selected-hex    {:now "Presentation `HexSelectedComponent` — pre-confirm only; read into the committed entity's `HexIdComponent` at confirm, never owned by Actions before that"
                     :target :same}
   :chosen-district {:now    DistrictBuildSelectionComponent  ;; component in Presentation.UI.DistrictBuild.Components (own entity, tagged DistrictBuildSelectionTag); written by DistrictBuildListUISubSystem, read by the UI subsystems + DistrictBuildUISystem (confirm payload)
                     :target :same}  ;; no draft to migrate onto — selection staying in Presentation IS the target
   :payer           {:now    "view-local (DistrictBuildPriceUIView.SelectedOwner) → carried in the confirm pulse; at confirm BuildDistrictActionSystem spends the payer's resource stacks (ResourceLedger) + the Mayor's AP pool (MayorAPComponent), then stamps ActorTypeComponent on the in-progress entity"  ;; R2 landed — resources + AP spent at confirm
                     :target :same}
   :built-district  {:now    "Economy District fact (DistrictTag + DistrictId + HexId + DistrictType), written at completion by BuildDistrictCompletionSystem; the in-progress verb entity (BuildDistrictInProgressTag) is transient"  ;; Presentation.Districts renders the FACT
                     :target :same}})  ;; S1 landed the fact; turns (S2) only delay when the fact is written
```

## Ordering invariants

```clojure
(def ordering-invariants
  {:confirm-vs-dismiss  "view.OnConfirmClicked raises the C# Confirmed event → DistrictBuildUISystem builds then hides; OnCloseClicked/OnScrimClicked raise the C# Closed event → hide only"  ;; no draft, so dismiss never has anything to discard
   :list-before-others  "DistrictBuildListUISubSystem populates FIRST (Priority order)"  ;; it default-writes the selection the other sections read — reorder = throw on a missing selection
   :completion-pulse    {:producer "BuildDistrictTurnTickSystem піднімає BuildDistrictCompleteEvent ЩОХОДУ, поки хоч один каунтдаун стоїть на 0 (level-triggered) — ніколи raise-once"
                         :consumer "BuildDistrictCompletionSystem реконсилить ВЕСЬ in-progress-набір зі стану (TurnsLeft<=0), ніколи не довіряє одиничній доставці пульсу"
                         :why "pulse = doorbell для трасованості (матеріальний producer→consumer ланцюжок); згублений у вікні EventCleanup pulse = +1 хід латентності, ніколи не втрата коректності. Ослаблення БУДЬ-ЯКОЇ з двох половин ламає механіку мовчки"}})
```

## Target contract

The committed entity in `Domains.Actions` is created directly on confirm — no draft, no promote
step. The UI is a projection that reads current Presentation selection state (`HexSelectedComponent`
+ `DistrictBuildSelectionComponent`) and raises command pulses; a completed transaction writes the
built-district FACT into `Domains.Economy`; `Presentation.Districts` renders facts, never verbs.
`DistrictBuiltEvent` fires when the fact is written — at construction completion (R1, landed).

## Roadmap — planned flows (backlog)

TARGET extensions of THIS district-build behavior, ordered by build-flow dependency. **R3–R5 below are
not built** (R1 and R2 have landed — see their `LANDED` markers). Every `^:new` symbol does not exist in code yet; new names are proposals (naming policy:
self-sufficient, repeat the feature name) pending the user's veto. doc-lint is suppressed for this
section because its vocabulary is deliberately future. Кожен пункт несе `contract-delta` (цільові
рядки, що задача додає до контракту вище) і `-open` мапу у формі `{:q … :resolve …}`. `:resolve` =
хто/що закриває питання перед тим, як пункт стане реальною clojure-задачею:

```clojure
(def resolve-legend
  {?                 "рішення користувача — справжня діра, агент не вигадує"
   :by-code          "агент закриває сам: читає код / roslyn / ecs-graph / di-graph"
   :by-policy        "агент пропонує за конвенцією (ECS_CONVENTIONS / Patterns), користувач вето"
   :by-naming-policy "агент пропонує ім'я за naming-policy, користувач вето"
   :decided          "вже вирішено — джерело в ;; поруч"})
```

<!-- doc-lint: off -->

```clojure
(def roadmap-deps  ;; build order — later items need earlier ones
  {:R1 "фундамент: in-progress-сутність, каунтдаун ходів, Economy-факт на завершенні (вбирає старі gap 2 + gap 5)"
   :R2 "на confirm разом з R1: списати ресурси; payload події += payer"
   :R3 "потребує R1 (є in-progress-сутність на гексі, поки будується)"
   :R4 "потребує R1 (показати turns-left) — і є точкою входу для R5"
   :R5 "потребує R1+R2+R4 (turns-left, повернення ресурсів, кнопка cancel)"})
```

### The five tasks (as stated)

Shared `:where` for всіх п'яти: `Presentation.UI.DistrictBuild`, `Flows.DistrictBuild`, `Domains.*`
(точні Domains-сайти — це per-task open-questions нижче). Дублікатний `:task :add-turns` розведено
на `:build-turns` / `:spend-resources`, щоб кожну можна було адресувати.

```clojure
[{:task :build-turns
  :goal "кожен район будується за різну кількість ходів"
  :flow (-> (:step-1 "confirm у DistrictBuildUIView")
            (:step-2 "DistrictBuildUISystem створює DistrictBuildConfirmedEvent")
            (:step-3 "BuildDistrictActionSystem handle → створює BuildDistrictActionTag entity")
            (:step-4 "+ ^:new TurnsComponent = ходи до завершення")
            (:step-5 "кожен хід TurnsComponent -= 1")
            (:step-6 "TurnsComponent == 0 → будуємо район, видаляємо BuildDistrictActionTag"))}

 {:task :spend-resources
  :goal "витратити ресурси на будівництво"
  :flow (-> (:step-1 "confirm у DistrictBuildUIView")
            (:step-2 "DistrictBuildUISystem створює DistrictBuildConfirmedEvent + set owner/payer")
            (:step-3 "BuildDistrictActionSystem handle → бере DistrictBuildCostConfig для DistrictType")
            (:step-4 "бере ресурси owner/payer і списує згідно DistrictBuildCostConfig")
            (:step-5 "додати HexIdComponent до BuildDistrictActionTag — де будується район"))}

 {:task :build-progress-view
  :goal "поки район будується — показувати view будівництва на гексі"
  :flow (-> (:step-1 "confirm у DistrictBuildUIView")
            (:step-2 "DistrictBuildUISystem створює DistrictBuildConfirmedEvent + set owner/payer")
            (:step-3 "^:new окрема ViewSystem handle → бере ^:new DistrictBuildProgressViewConfig для DistrictType")
            (:step-4 "Instantiate prefab для конкретного гексу"))}

 {:task :district-ui-block
  :goal "district-блок у HexInfoPanelView показує район що будується + ходи до завершення + скасувати"
  :architecture "ймовірно PATTERN_VIEW_SYSTEM"
  :flow (-> (:step-1 "confirm у DistrictBuildUIView")
            (:step-2 "BuildDistrictActionSystem створює BuildDistrictActionTag entity")
            (:step-3 "^:new окрема ViewSystem бере BuildDistrictActionTag, матчить гекс за HexIdComponent, малює UI у блоці"))}

 {:task :cancel-build
  :goal "скасувати будівництво через district-ui-block; той самий хід → повернути AP + ресурси; інший хід → ресурси пропорційно до ходів, що лишилися"
  :flow (-> (:step-1 "cancel у district-ui-block")
            (:step-2 "спавн ^:new BuildDistrictCancelEvent для конкретного HexIdComponent")
            (:step-3 "^:new BuildDistrictActionCancelSystem handle BuildDistrictCancelEvent")
            (:step-4 (cond
                       "той самий хід"  "повертаємо AP + ресурси, видаляємо BuildDistrictActionTag"
                       "інший хід"      "повертаємо ресурси пропорційно до turns-left, видаляємо BuildDistrictActionTag")))}]
```

### Нотаційні правки твого оригіналу (для тебе, не для агента)

> Людська примітка — дозволена явно. Повний розбір before→after: `CLOJURE_GUIDE.md` §11.

Вихідний батч мав дві нотаційні шорсткості. Що я змінив і чому:

1. **Дубль `:task :add-turns` (×2) → унікальні `:build-turns` / `:spend-resources`.**
   Сусідні мапи посилаються одна на одну за `:task`-id (напр. `:listen :build-turns`). Два
   однакові id роблять таке посилання неоднозначним — конкретну задачу неможливо адресувати.
2. **`:TurnsComponent` → `^:new TurnsComponent`.** `:keyword` = самозначуща мітка/вердикт
   (`:tile-busy`). Тип, який треба СТВОРИТИ — це код-якір: голий символ `TurnsComponent` +
   мета-тег `^:new` («створити, ще не існує»). Так агент знає, що це новий клас, а не ярлик.

Дрібниця на майбутнє (свідомо НЕ правив): усе в межах `(:step-1 "текст…")` — це проза, тож
символи й `^:new` усередині рядка декоративні. Хочеш, щоб якір/тег «працювали» — винось їх за
лапки. Для чорнового плану рядок-крок читабельніший, тому лишив як є.

### R1 — build over turns  (вбирає старі gap 2 + gap 5-ticking) — LANDED 2026-07-10 (S1→S2→S3)

Confirm creates an IN-PROGRESS verb entity in `Domains.Actions`, stamped with a per-DistrictType turn
countdown. A turn-phase hops to the main thread (`SwitchToMainThread`), decrements the countdown each
turn, and — while any countdown sits at 0 — raises the `BuildDistrictCompleteEvent` pulse; the
EVENT-GATED completion system consumes the pulse and reconciles the in-progress set into the
built-district FACT in `Domains.Economy`, while the world-view reconciles off the FACT.
**Event-gated (decided 2026-07-10; supersedes the same-day «Option B / per-frame, no event» draft):**
the pulse is kept for traceability — a material producer→consumer chain to log and trace («pulse як
stacktrace») — and made safe by the two-sided discipline decreed in `ordering-invariants
:completion-pulse` (level-triggered producer + state-reconciling consumer). Known and accepted:
`TurnsToBuild=0` completes on the NEXT turn, not instantly (pulse піднімається лише в turn-фазі) —
покрокова гра, latency в межах ходу нічого не вирішує. `DistrictViewSpawnSystem` reconciles off
`DistrictTag`, not the verb entity. `BuildDistrictActionTag` is dropped (replaced by
`BuildDistrictInProgressTag`).

```clojure
(def R1-model  ;; механіка end-to-end (built)
  {:in-progress-entity   ;; verb, Domains.Actions.BuildDistrictAction — created at confirm, disposed at completion
     [BuildDistrictInProgressTag ActionIdComponent HexIdComponent DistrictTypeComponent
      BuildDistrictTurnsLeftComponent ActorTypeComponent]     ;; TurnsLeft=S2, ActorType=S3 (payer — captured at confirm, not spent → R2)
   :fact-entity          ;; fact, Domains.Economy.District — created at completion
     [DistrictTag DistrictIdComponent HexIdComponent DistrictTypeComponent] ;; DistrictId PK allocated (DistrictIdAllocatorComponent)
   :systems  {BuildDistrictActionSystem     "confirm → create in-progress; stamp TurnsLeft = DistrictBuildCostConfig.TurnsToBuild"
              BuildDistrictTurnTickSystem   "TurnPhaseSubSystem (Upkeep 510): SwitchToMainThread → TurnsLeft -= 1 щоходу (floor 0); поки left<=0 — raise BuildDistrictCompleteEvent (level-triggered)"
              BuildDistrictCompletionSystem "EVENT-GATED (601): на BuildDistrictCompleteEvent pulse реконсилить in-progress-набір — TurnsLeft<=0 → write fact + dispose + raise DistrictBuiltEvent"
              DistrictViewSpawnSystem       "reconcile off DistrictTag (fact) на DistrictBuiltEvent (602)"}
   :pulse-discipline "completion-pulse = doorbell, не payload: рівно та двостороння дисципліна, що декретована в ordering-invariants :completion-pulse — тут не переказується"
   :events   {BuildDistrictCompleteEvent "payload-less doorbell; TurnTick → Completion; level-triggered (щоходу до consume)"
              DistrictBuiltEvent         "payload-less; raised at COMPLETION (main-thread, 601); siblings dropped (reconcile off DistrictTag)"}})

(def R1-decomposition  ;; три впорядкованих під-задачі — не «одна дія» (як виконувалось; S2 записано ЯК ЗБУДОВАНО, не як чорновий Option B)
  {:S1 "verb→fact split: confirm → in-progress entity → completion → Economy fact; view off DistrictTag; build INSTANT. Drops BuildDistrictActionTag."
   :S2 "turn countdown: + BuildDistrictTurnsLeftComponent (=TurnsToBuild at confirm); BuildDistrictTurnTickSystem (turn phase, main-thread hop) decrements value і level-triggered піднімає BuildDistrictCompleteEvent; completion — event-gated reconcile по TurnsLeft<=0."
   :S3 "payer capture: DistrictBuildConfirmedEvent += ActorType; read view.SelectedOwner at confirm; stamp ActorTypeComponent. Captured, not spent (→R2)."})

(def R1-open  ;; усі закриті 2026-07-10
  {:turns-source     {:a "DistrictBuildCostConfig.TurnsToBuild [Range 0..100] — поле вже існує"          :resolve :decided}
   :decrement-owner  {:a "BuildDistrictTurnTickSystem : TurnPhaseSubSystem — SwitchToMainThread, value-write + level-triggered pulse" :resolve :decided}
   :completion-owner {:a "BuildDistrictCompletionSystem — event-gated reconcile по TurnsLeft<=0 (pulse = doorbell)" :resolve :decided}
   :completion-model {:a "event-gated > per-frame: трасованість ланцюжка подій; безпека = level-triggered + reconcile (див. ordering-invariants :completion-pulse)" :resolve :decided}
   :component-name   {:a "BuildDistrictTurnsLeftComponent (policy > input TurnsComponent)"               :resolve :decided}
   :archetype        {:a "fact DistrictTag+DistrictId(PK)+HexId+DistrictType в Economy.District"         :resolve :decided}})
```

### R2 — spend resources — LANDED 2026-07-12

At confirm, `BuildDistrictActionSystem` resolves the whole `DistrictBuildCostConfig` for the
`DistrictType`, then spends ALL-OR-NOTHING before creating the in-progress entity: the payer's
resource stacks are charged `DistrictPrices` via `ResourceLedger`, and the Mayor's `MayorAPComponent`
pool is charged `ApPrice` (AP is always Mayor-paid, whatever the resource payer). Affordability is
checked across the WHOLE price first; a shortfall throws (fail-loud) — the confirm is UI-GATED, so the
throw is an invariant net, never a normal path. The confirm pulse already carried the payer
(`ActorType`, from R1/S3), so no payload change was needed.

```clojure
(def R2-model  ;; механіка end-to-end (built)
  {:spend    {:owner     BuildDistrictActionSystem   ;; on the DistrictBuildConfirmedEvent pulse, BEFORE CreateEntity
              :resources "payer's stacks (Mayor→MayorResourceTag / City→CityResourceTag, keyed by owner FK) charged DistrictPrices via ResourceLedger.Deduct (its first consumer)"
              :ap        "Mayor's MayorAPComponent -= ApPrice — always Mayor-paid, regardless of the resource payer"
              :atomicity "CanAfford(all resources) && ap>=ApPrice checked FIRST → then Deduct + AP write; never a partial charge"
              :fail-loud "shortfall → throw (InvalidOperationException); the UI gate prevents reaching it in normal play"}
   :ui-gate  {:owner     DistrictBuildPriceUISubSystem  ;; computes affordability in Render — the choke point for populate AND payer switch
              :verdict   "ap>=ApPrice && every resource have>=need for the selected payer"
              :push      "chrome DistrictBuildUIView.SetConfirmEnabled(affordable) — disables «ЗБУДУВАТИ» when unaffordable"
              :style     ".confirm:disabled (muted, no gold) in DistrictBuildAction.uss; SetEnabled(false) drives the pseudo-state"}
   :fix      "DistrictBuildPriceUISubSystem read the AP pool from MayorIdComponent (the id) instead of MayorAPComponent — corrected so both the AP row and the gate use the real pool"})

(def R2-open  ;; усі закриті 2026-07-12
  {:payer-model    {:a "payer = ActorType (Mayor|City); resource stacks resolved by owner tag + FK; ResourceLedger is the owner-blind spend channel" :resolve :by-code}
   :payload-change {:a "no change needed — DistrictBuildConfirmedEvent already carries ActorType Payer (R1/S3)"                                       :resolve :by-code}
   :affordability  {:a "UI-gate + throw-guard: price subsystem disables confirm when unaffordable; system throws as the invariant net"                :resolve :decided}   ;; user 2026-07-12
   :ap-spend       {:a "AP spent at confirm too; balance = MayorAPComponent on the Mayor (ApPrice authored in DistrictBuildCostConfig)"               :resolve :decided}   ;; user 2026-07-12
   :hex-stamp      {:a "HexIdComponent already stamped at confirm since R1 — unchanged"                                                               :resolve :by-code}})
```

### R3 — construction progress view

While a build is in progress, a world-space progress prefab sits on the hex. A reactive ViewSystem
in `Presentation.Districts` reacts to the in-progress entity, resolves a per-DistrictType progress
config, and instantiates the prefab at the hex; it is torn down at completion (R1) or cancel (R5),
after which `DistrictViewSpawnSystem` spawns the real district off the fact.

```clojure
(def R3-contract  ;; цільові рядки, що R3 додає до контракту
  {:participants {Presentation.Districts "+ progress-view поки будується — не лише готовий район off факту"}
   :ownership    {:progress-view {:now nil :target "^:new reactive ViewSystem тримає progress-prefab на гексі під час білду"}}
   :ordering     {:spawn   "на створення in-progress-сутності (BuildDistrictActionTag), config per DistrictType"
                  :despawn "на completion (R1) або cancel (R5) → далі DistrictViewSpawnSystem малює готовий район off факту"}})

(def R3-open
  {:home    {:q "окрема система в Presentation.Districts чи гілка DistrictViewSpawnSystem?"       :resolve :by-policy}
   :config  {:q "^:new DistrictBuildProgressViewConfig: prefab на DistrictType (PATTERN_CONFIG)" :resolve :decided}
   :trigger {:q "реагує на BuildDistrictActionTag; reconcile за HexIdComponent"                  :resolve :decided}
   :despawn {:q "хто прибирає progress-prefab на completion/cancel — ця ж система?"              :resolve :by-policy}})
```

### R4 — HexInfoPanel in-progress block

The district block in `HexInfoPanelView` today offers the build action on the selected hex. When the
selected hex has an in-progress build (`BuildDistrictActionTag` whose `HexIdComponent` matches the
selection), the block instead shows the district being built, the turns-left (R1's countdown), and a
cancel control. Likely `PATTERN_VIEW_SYSTEM`: a ViewSystem reads the in-progress entity for the
selected hex and pushes to the view; the view raises a C# cancel event (not ECS), matching the rest
of the DistrictBuild UI.

```clojure
(def R4-contract  ;; цільові рядки, що R4 додає до контракту
  {:participants {Presentation.UI.MainHud.HexInfoPanel "district-блок = проєкція in-progress-білду (тип + turns-left + cancel), не лише кнопка build"}
   :ownership    {:in-progress-block {:now "лише build-дія на порожньому гексі" :target "^:new ViewSystem push-to-view: тип + turns-left + cancel для вибраного гексу"}}
   :ordering     {:reconcile "на зміні вибору (HexSelectedComponent) + на декременті ходів (R1) → push-to-view"}
   :event        {:cancel "C# event view→система (не ECS), як решта DistrictBuild UI → R5"}})

(def R4-open
  {:pattern        {:q "PATTERN_VIEW_SYSTEM — ViewSystem читає in-progress-сутність, push-to-view" :resolve :decided}
   :block-now      {:q "що district-блок HexInfoPanelView показує зараз на порожньому/забудованому гексі?" :resolve :by-code}
   :selection-link {:q "як блок дізнається вибраний гекс — HexSelectedComponent?"                  :resolve :by-code}
   :turns-refresh  {:q "turns-left оновлюється щоходу (R1) → push при зміні"                        :resolve :decided}
   :cancel-emit    {:q "кнопка cancel = C# event (не ECS)"                                          :resolve :decided}})
```

### R5 — cancel build (+ refund)

From R4's cancel control, a cancel pulse is raised for the hex. A new reactive system handles it: if
cancelled the SAME turn as confirm → full refund (AP + resources) and remove the entity; otherwise →
refund resources proportional to turns-left and remove the entity. It also tears down the progress
view (R3). AP is refunded ONLY on the same turn; resources always.

```clojure
(def R5-contract  ;; цільові рядки, що R5 додає до контракту
  {:event     {BuildDistrictCancelEvent "^:new, для конкретного HexIdComponent — виробник R4-view, споживач ^:new BuildDistrictActionCancelSystem"}
   :ownership {:confirm-turn {:now nil :target "^:new штамп ходу-confirm на сутності — для same-turn тесту"}}
   :ordering  {:refund  (cond "той самий хід" "AP + ресурси повністю"
                              "інший хід"     "ресурси пропорційно до turns-left; AP — ні")
               :cleanup "remove BuildDistrictActionTag + progress-view (R3)"}})

(def R5-open
  {:event          {:q "^:new BuildDistrictCancelEvent + ^:new BuildDistrictActionCancelSystem" :resolve :decided}
   :same-turn-test {:q "як визначити 'той самий хід' і де номер поточного ходу?"                 :resolve :by-code}   ;; знайду turn-лічильник
   :refund-partial {:q "формула пропорційного повернення + округлення (cost * turnsLeft / total?)" :resolve ?}        ;; геймдизайн
   :ap-rule        {:q "AP лише того ж ходу, ресурси завжди"                                     :resolve :decided}   ;; goal
   :view-cleanup   {:q "прибрати progress-view (R3) + tag на cancel"                             :resolve :decided}})
```

### Technical debt — independent of the flows above (former gap 3)

```clojure
(def dedup-debt
  {:problem "find-config-by-DistrictType дублюється: TryGetDistrict verbatim ×2 (Price/HexResources subsystems), TryGetCost — той самий shape, ResolvePrefab — третій варіант"
   :fix     "один stateless helper у Domains.Economy District Helpers/"})
```

<!-- doc-lint: on -->
