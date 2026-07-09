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
  systems:          [DistrictBuildUISystem, DistrictBuildUISpawnSystem, BuildDistrictActionSystem, DistrictViewSpawnSystem]
  events:           [DistrictBuildUIRequestedEvent, DistrictBuildConfirmedEvent, DistrictBuiltEvent]
  components:       [DistrictBuildSelectionComponent]
  tags:             [BuildDistrictActionTag]
---

# FLOW — District Build

The cross-domain contract of the district-build transaction: one player gesture (open → pick →
confirm / dismiss) spanning `Presentation.UI.HexInfoPanel` (the open request), `Presentation.UI.DistrictBuild`,
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
  {Presentation.UI.HexInfoPanel        "emits the open request (DistrictBuildUIRequestedEvent) from the selected-hex context panel"
   Presentation.UI.DistrictBuild       "projection + commands: renders the transaction, raises the confirm pulse, owns NO transaction state"
   Flows.DistrictBuild                 "flow event-vocabulary home: owns the cross-subfeature UI-navigation event; leaf assembly the UI references"
   Domains.Actions.BuildDistrictAction "verb owner: creates the committed build entity directly on confirm — no draft"
   Domains.Economy                     "vocabulary (build configs, costs, open conditions) + TARGET home of the built-district fact"
   Presentation.Districts              "world view: spawns the district prefab for every built district"})
```

## Event vocabulary (the contract)

| Event                           | Home                | Payload                                                                    | Producer → Consumer                                                          | Semantics                                                                  |
| ------------------------------- | ------------------- | -------------------------------------------------------------------------- | ---------------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| `DistrictBuildUIRequestedEvent` | Flows.DistrictBuild | —                                                                          | `HexInfoPanelView` → `DistrictBuildUISystem`, `DistrictBuildListUISubSystem` | player asked to open the overlay for the selected hex                      |
| `DistrictBuildConfirmedEvent`   | Domains.Actions     | `HexCoord` + `DistrictType`                                                | `DistrictBuildUISystem` → `BuildDistrictActionSystem`                        | player committed: create the committed build entity directly (no draft)    |
| `DistrictBuiltEvent`            | Domains.Actions     | `HexIdComponent` + `DistrictTypeComponent` siblings (target: payload-less) | `BuildDistrictActionSystem` → `DistrictViewSpawnSystem`                      | a district EXISTS as a fact; fires at construction completion (Roadmap R1) |

> **Not ECS — local C# events.** Close/dismiss and district-selection are **view→system C# events** now, not
> ECS pulses: the chrome `DistrictBuildUIView` raises `Closed`/`Confirmed` (the orchestrator subscribes),
> `DistrictBuildListUIView` raises `SelectionChanged` and `DistrictBuildPriceUIView` raises `PayerChanged`
> (each section subsystem subscribes to its own view). Only the three events above cross a frame/assembly
> boundary as ECS pulses. Traceability = the direct C# subscriptions, all visible at the subscriber.

```clojure
(def payload-verdicts  ;; deviations from PATTERN_EVENT that the contract removes
  {DistrictBuiltEvent {:siblings [HexIdComponent DistrictTypeComponent] :verdict :dead}})  ;; the sole consumer reconciles from world state and ignores them
```

## State ownership

```clojure
(def state-ownership  ;; {state {:now … :target …}} — the split below is the flow's core defect
  {:selected-hex    {:now "Presentation `HexSelectedComponent` — pre-confirm only; read into the committed entity's `HexIdComponent` at confirm, never owned by Actions before that"
                     :target :same}
   :chosen-district {:now    DistrictBuildSelectionComponent  ;; component in Presentation.UI.DistrictBuild.Components (own entity, tagged DistrictBuildSelectionTag); written by DistrictBuildListUISubSystem, read by the UI subsystems + DistrictBuildUISystem (confirm payload)
                     :target :same}  ;; no draft to migrate onto — selection staying in Presentation IS the target
   :payer           {:now    "view-local (DistrictBuildPriceUIView.SelectedOwner)"  ;; render-only today
                     :target "committed-entity component once resource-spend lands (Roadmap R2)"}
   :built-district  {:now    "the committed verb entity itself (BuildDistrictActionTag), created directly at confirm"  ;; no domain fact exists; Presentation.Districts renders the VERB
                     :target "fact entity in Domains.Economy, written at build completion (Roadmap R1)"}})
```

## Ordering invariants

```clojure
(def ordering-invariants
  {:confirm-vs-dismiss  "view.OnConfirmClicked raises the C# Confirmed event → DistrictBuildUISystem builds then hides; OnCloseClicked/OnScrimClicked raise the C# Closed event → hide only"  ;; no draft, so dismiss never has anything to discard
   :list-before-others  "DistrictBuildListUISubSystem populates FIRST (Priority order)"})  ;; it default-writes the selection the other sections read — reorder = throw on a missing selection
```

## Target contract

The committed entity in `Domains.Actions` is created directly on confirm — no draft, no promote
step. The UI is a projection that reads current Presentation selection state (`HexSelectedComponent`
+ `DistrictBuildSelectionComponent`) and raises command pulses; a completed transaction writes the
built-district FACT into `Domains.Economy`; `Presentation.Districts` renders facts, never verbs.
`DistrictBuiltEvent` fires when the fact is written — at construction completion once turn-ticking
exists (Roadmap R1).

## Roadmap — planned flows (backlog)

TARGET extensions of THIS district-build behavior, ordered by build-flow dependency. **Nothing below
is built.** Every `^:new` symbol does not exist in code yet; new names are proposals (naming policy:
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

### R1 — build over turns  (вбирає старі gap 2 + gap 5-ticking)

At confirm the committed entity is stamped with a per-DistrictType turn countdown instead of
completing instantly. A turn-scoped system decrements it each turn; at zero it writes the
built-district FACT into `Domains.Economy` (new archetype: `DistrictTag` + `HexIdComponent` +
`DistrictTypeComponent` — the three types already exist, only the ENTITY is new), fires the (now
payload-less) `DistrictBuiltEvent`, and removes `BuildDistrictActionTag`. `DistrictViewSpawnSystem`
then reconciles off the FACT, not the verb entity.

```clojure
(def R1-contract  ;; цільові рядки, що R1 додає до контракту
  {:ownership {:build-turns-left {:now nil :target "^:new каунтдаун-компонент на committed-сутності (BuildDistrictActionTag)"}
               :built-district   {:target "Economy-факт замість verb-сутності; DistrictViewSpawnSystem reconcile off факту"}}
   :ordering  {:tick             "декремент раз на хід у Turn-пайплайні"
               :complete-at-zero "при 0, у ТОЙ САМИЙ хід: write Economy-факт → payload-less DistrictBuiltEvent → remove BuildDistrictActionTag"}
   :event     {DistrictBuiltEvent "стає payload-less; fires at completion, не at confirm"}})

(def R1-open
  {:turns-source     {:q "ходи на DistrictType — поле в DistrictBuildCostConfig чи окремий ^:new config?" :resolve ?}
   :decrement-owner  {:q "TurnPhaseSubSystem (Turn-пайплайн) чи reactive-on-turn-pulse?"                  :resolve :by-code}
   :completion-owner {:q "хто пише факт на 0 — BuildDistrictActionSystem чи ^:new completion-система?"    :resolve :by-policy}
   :component-name   {:q "TurnsComponent → BuildDistrictTurnsLeftComponent?"                              :resolve :by-naming-policy}
   :archetype        {:q "Economy-архетип DistrictTag+HexIdComponent+DistrictTypeComponent (+build_graph.py)" :resolve :decided}})  ;; старий gap 2
```

### R2 — spend resources

At confirm, `BuildDistrictActionSystem` resolves `DistrictBuildCostConfig` for the `DistrictType`,
reads the payer's resource pool, and spends it. The confirm event gains an owner/payer FK in its
payload. Spend must validate (fail-loud), not silently no-op on shortage.

```clojure
(def R2-contract  ;; цільові рядки, що R2 додає до контракту
  {:event     {DistrictBuildConfirmedEvent "payload += owner/payer FK (зараз лише HexCoord + DistrictType)"}
   :ownership {:payer {:now "view-local (DistrictBuildPriceUIView.SelectedOwner)" :target "у payload події → списання в BuildDistrictActionSystem"}}
   :ordering  {:spend-at-confirm "списання ресурсів на confirm, у BuildDistrictActionSystem, ДО стампу каунтдауна (R1)"
               :fail-loud        "нестача ресурсів — throw, не silent no-op"}})

(def R2-open
  {:payer-model    {:q "хто платить (Economy-actor) і де баланс ресурсів (компонент у Domains.Economy)?" :resolve :by-code}
   :payload-change {:q "DistrictBuildConfirmedEvent += owner/payer FK"                                    :resolve :decided}   ;; contract-delta вище
   :affordability  {:q "gate нестачі: UI вимикає confirm і/або система throw?"                            :resolve ?}          ;; UX-рішення
   :ap-spend       {:q "чи витрачається AP на confirm і де AP-баланс?"                                    :resolve ?}          ;; залежить від AP-моделі
   :hex-stamp      {:q "HexIdComponent на BuildDistrictActionTag — вже ставиться на confirm?"             :resolve :by-code}})
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
  {:participants {Presentation.UI.HexInfoPanel "district-блок = проєкція in-progress-білду (тип + turns-left + cancel), не лише кнопка build"}
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
