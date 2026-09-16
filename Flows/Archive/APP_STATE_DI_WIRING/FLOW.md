---
category: A
read: archive
status: implemented
tags:
  - boot
  - di
  - cascade
related:
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
---

# Request

```clojure
{:source :user
 :received-at "2026-09-16"
 :raw-request ["ось приблизний план структурних та архітектурних змін які я хочу отримати

(def app-state {
              :where \"AppState\"
              :description \"enum який показує до якого ігрового кроку чи стану належить власник\"
})

(def systems {
              :definition \"Бери з правил в Achitecture\"
              :implementation \"Кожна система має реалізовувати IAppStateSystem\"
              :IAppStateSystem \"Через VContainer installer кожна система має отримати своє значення app-state\"
})

(def GameModeMachine{
                     :where \"GameModeMachine.cs\"
                     :description \"Це state machine яка керує станами гри використовуючи для цього app-state та реазілації IAppState\"
})

(def IAppState {
                :where \"IAppState.cs\"
                :description \"Це інтерфейс який реалізовує кожен стан гри app-state\"
})

(def flow (->
           (:step-1 \"знайти всі системи\")
           (:step-2 \"кожна система має реалізовувати IAppStateSystem\")
           (:step-3 \"кожна система отримує свій app-state через VContainer installer\")
           (:step-4 \"для кожного значення app-state створюється свій стан гри який реалізовує IAppState\")
           (:step-5 \"кожна реалізація IAppState біндиться через VContainer installer\")
           (:step-6 \"кожна реалізація IAppState отримує список всіх реалізацій IAppStateSystem і фільтрує тільки ті які мають відповідний app-state і зберігає їх у внутрішньому списку\")
           (:step-7 \"GameModeMachine отримує через VContainer installer список всіх IAppState\")
           (:step-8 \"Boot отримує GameModeMachine через VContainer\")
           ))"
               "1 - системи першого порядку, підсистеми будуть частинами оркестраторів і напряму інʼєктяться в відповідну систему
2 - EventCleanupSystem має бути в усіх станах гри, саме для цього й є прапорці.
3 - ти закидай в кожен стан всі варіанти систем а я далі розберуся. Кожен IAppState матиме свою внутрішню реалізацію.
4 - Перше
5 - через WithParameter
6 - спочатку напишемо код потім в кінці виправимо правила

можеш починати перший крок каскаду окремим Opus саб агентом."
               "1 - дозволь
2 - перше. Перемиканнями між станами буде керувати Boot
3 - ShowHexesUISystem це тепер AppState.MainMenu
4 - EventCleanupSystem завжди має найбільший приорітет і тому відпрацьовує остання.
5 - Прибрати, лишити тільки інтерфейси — 60
6 - Виправити на кроці правил у кінці

s1 запусти в opus агенті"]}
```

# Confirmed contract

```clojure
{:task :app-state-di-wiring
 :goal "a game state gathers its own systems through DI; Boot no longer wires systems into states by hand"
 :path :cascade
 :where #{Assets/Modules/Boot
          Assets/Scripts/EcsExtensions
          Assets/Scripts/Installers
          "every domain / presentation / module installer that registers a first-order system"}
 :off-limits #{"Unity .meta files, scenes, prefabs, .asset — CLAUDE.md § 3"
               "RULES_SPECIFICATION.md, ARCHITECTURE.md, Patterns/ recipes, skills — rules are fixed in a later step, after the code"
               "sub-systems and turn phases — they stay injected into their orchestrator"}
 :pattern :no-recipe
 :decided "see # Decisions"
 :do (-> (:step-1 "find every first-order system")
         (:step-2 "every first-order system implements IAppStateSystem")
         (:step-3 "every first-order system receives its AppState through its VContainer installer registration, by WithParameter")
         (:step-4 "one IAppState implementation for every AppState value")
         (:step-5 "every IAppState implementation is bound through a VContainer installer")
         (:step-6 "every IAppState receives IReadOnlyList<IAppStateSystem>, keeps only the systems whose AppState flags contain its own value, stores them in internal lists")
         (:step-7 "GameModeMachine receives every IAppState through a VContainer installer")
         (:step-8 "Boot receives GameModeMachine through VContainer"))
 :result "Boot.Construct names no system; putting a system into a state = its installer registration line with WithParameter(AppState flags)"
 :read [ARCHITECTURE.md
        Patterns/PATTERN_PERFRAME_SYSTEM.md
        Patterns/PATTERN_REACTIVE_SYSTEM.md
        Patterns/PATTERN_PIPELINE_STAGE.md
        Patterns/PATTERN_CONFIG_LOADER.md
        Patterns/PATTERN_CLEANUP_SYSTEM.md]
 :tools #{roslyn fantasymayor-graph asmdef_reach}
 :accept [{:meter "mcp__roslyn__get_diagnostics, FantasyMayor.sln, changed files" :target "clean"}
          {:meter "/arch-check on the changed scope" :target "no violation the change introduced"}
          {:meter "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py check" :target "no new warning on a touched file"}
          {:meter "the owner's Unity check" :target "compiles; start → MainMenu → MapCreation → Gameplay behaves as before"}]}
```

# Plan

```clojure
(-> (:step-1 "/sdd-cascade context — CONTEXT.md")
    (:step-2 "/sdd-cascade s1 — the algorithm; owner gate")
    (:step-3 "/sdd-cascade s2 — the structure; owner gate")
    (:step-4 "/sdd-cascade code — translation of s2; meters")
    (:step-5 "/sdd-cascade read-back — owner verdict")
    (:step-6 "rules: RULES_SPECIFICATION.md first, then carriers, then fantasymayor-rules-conformance (CLAUDE.md § 6)")
    (:step-7 "close"))
```

# Findings

```clojure
[{:finding :mechanism-exists
  :at "2026-09-16"
  :fact "AppState ([Flags], 8 values: Initialization ConfigLoading InstanceObjects MainMenu MapCreation MapLoading Gameplay GameOver), IAppStateSystem (only an AppState property), IAppState, GameModeMachine and four states (MainMenuState MapCreationState MapLoadingState GameplayState) already exist in Assets/Modules/Boot"
  :verified-by "read the sources in Assets/Modules/Boot"
  :consequence "the task is rewiring, not a new mechanism"}
 {:finding :boot-wires-by-hand
  :at "2026-09-16"
  :fact "Boot.Construct receives 29 concrete systems plus IReadOnlyList<IUniTaskSystem> and IReadOnlyList<IPrioritizedUniTaskSystem<MapGenerationStep>>, builds the states with new, and builds GameModeMachine with new over a Dictionary<AppState, IAppState>; Boot itself runs ConfigLoading and InstanceObjects before switching to MainMenu"
  :verified-by "read Assets/Modules/Boot/Implementation/Boot.cs"
  :consequence "Boot is the main code the change removes"}
 {:finding :appstate-today
  :at "2026-09-16"
  :fact "only IUniTaskSystem extends IAppStateSystem; ConfigLoaderSystem<T> already receives its AppState by .WithParameter(AppState.ConfigLoading) (WorldInstaller.cs:68)"
  :verified-by "grep over Assets for IAppStateSystem and AppState"
  :consequence "WithParameter is the existing precedent for step-3"}
 {:finding :system-census
  :at "2026-09-16"
  :fact "fmgraph systems: reactive 18, per-frame 7, cleanup 1 (EventCleanupSystem), pipeline-stage 14, startup-step 2 (ConfigLoaderSystem, VertexGridSpawnSystem), turn-phase 3, sub-system 26"
  :verified-by "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py systems --role <role>"
  :consequence "first-order = reactive, per-frame, cleanup, pipeline-stage, startup-step; turn-phase and sub-system stay under their orchestrators"}]
```

# Decisions

```clojure
[{:decision :scope-first-order
  :status :confirmed
  :at "2026-09-16"
  :value "only first-order systems implement IAppStateSystem; sub-systems stay parts of orchestrators and are injected straight into their orchestrator"
  :verified-by "owner's answer 1"
  :reason "a sub-system is run by its orchestrator, never by a state"}
 {:decision :flags-multi-state
  :status :confirmed
  :at "2026-09-16"
  :value "AppState stays [Flags]; a system belongs to every state whose value its flags contain; EventCleanupSystem is in every game state"
  :verified-by "owner's answer 2"
  :reason "the flags exist exactly for this"}
 {:decision :state-gets-every-kind
  :status :confirmed
  :at "2026-09-16"
  :value "every state receives its filtered systems of every kind (IUpdatedSystem, ILateUpdatedSystem, IUniTaskSystem, IPrioritizedUniTaskSystem<MapGenerationStep>); what a state does with them is its own internal implementation, which the owner will shape afterwards"
  :verified-by "owner's answer 3"
  :reason "owner decides each state's behaviour later"}
 {:decision :startup-steps-are-states
  :status :confirmed
  :at "2026-09-16"
  :value "ConfigLoading and InstanceObjects become IAppState states run by GameModeMachine; Boot only switches into the first state; every AppState value gets a state (Initialization and GameOver included, per step-4)"
  :verified-by "owner's answer 4 (option: startup steps become states) + step-4 of the raw request"
  :reason "one mechanism for every step of the game"}
 {:decision :value-by-with-parameter
  :status :confirmed
  :at "2026-09-16"
  :value "a system receives its AppState through .WithParameter(AppState …) on its installer registration"
  :verified-by "owner's answer 5"
  :reason "existing precedent in ConfigLoaderSystem registrations"}
 {:decision :rules-after-code
  :status :confirmed
  :at "2026-09-16"
  :value "code first; RULES_SPECIFICATION.md and its carriers are fixed at the end, as a step of this task"
  :verified-by "owner's answer 6"
  :reason "owner's order"}
 {:decision :context-stage-by-subagent
  :status :confirmed
  :at "2026-09-16"
  :value "the context stage runs in a separate Opus subagent whose only input is this FLOW.md and the files it names"
  :verified-by "owner's instruction"
  :reason "owner's word; canon names the owner's hand as the isolation mechanism — a fresh subagent reading only the artifact is accepted by the owner as that fresh context"}
 {:decision :turn-asmdef-reference
  :status :confirmed
  :at "2026-09-16"
  :value "the code stage adds a Boot.Core reference to Assets/Modules/Turn/Turn.asmdef so TurnInstaller, TurnProcessorSystem and TurnCountSystem see AppState"
  :verified-by "findings gate, owner's answer 1: «дозволь»; blocker reported in CONTEXT.md by asmdef_reach"
  :reason "Turn cannot name AppState otherwise"}
 {:decision :first-state-and-transitions
  :status :confirmed
  :at "2026-09-16"
  :value "Boot switches into Initialization first; the minimal transitions Initialization → ConfigLoading → InstanceObjects → MainMenu are part of this task; switching between states is controlled by Boot"
  :verified-by "findings gate, owner's answer 2: «перше. Перемиканнями між станами буде керувати Boot»"
  :reason "Boot owns the order of the game's steps"}
 {:decision :show-hexes-ui-main-menu
  :status :confirmed
  :at "2026-09-16"
  :value "ShowHexesUISystem becomes a first-order system with AppState.MainMenu"
  :verified-by "findings gate, owner's answer 3"
  :reason "owner's word"}
 {:decision :cleanup-runs-last
  :status :confirmed
  :at "2026-09-16"
  :value "EventCleanupSystem always has the highest priority value and therefore runs last; it keeps its flag in every state"
  :verified-by "findings gate, owner's answer 4: «EventCleanupSystem завжди має найбільший приорітет і тому відпрацьовує остання»"
  :reason "the MainMenu exit event is consumed before cleanup deletes it, by priority order"}
 {:decision :interfaces-only-registrations
  :status :confirmed
  :at "2026-09-16"
  :value "drop .As<Concrete>() from first-order system registrations; register interfaces only"
  :verified-by "findings gate, owner's answer 5 (option rated 60)"
  :reason "once Boot stops resolving concrete types nobody needs them"}
 {:decision :fmgraph-runs-in-later
  :status :confirmed
  :at "2026-09-16"
  :value "fmgraph's runs_in / state extraction (today read from new …State(…) in Boot.Construct) is fixed in the rules step at the end; until then its state edges are knowingly empty"
  :verified-by "findings gate, owner's answer 6"
  :reason "graph scripts are off-limits in the code stages"}
 {:decision :c1-boot-drives-startup
  :status :confirmed
  :at "2026-09-16"
  :value "Boot owns the startup order Initialization → ConfigLoading → InstanceObjects → MainMenu and advances when the current state's entry completes; from MainMenu on Boot applies the state's RequestedMode; GameModeMachine no longer switches by itself and exposes entry completion"
  :verified-by "gate after s1, owner's answer 1 to contra c-1 (fix-a, rated 55)"
  :reason "switching is Boot's"}
 {:decision :c2-mainmenu-exit-as-is
  :status :confirmed
  :at "2026-09-16"
  :value "no change for the MainMenu exit check against cleanup; the owner runs the game, sees whether it fails, and decides then"
  :verified-by "gate after s1, owner's answer 2 to contra c-2"
  :reason "empirical check by the owner"}
 {:decision :c3-update-is-enough
  :status :confirmed
  :at "2026-09-16"
  :value "ShowHexesUISystem needs no Show/Hide contract; its one run method is enough"
  :verified-by "gate after s1, owner's answer 3: «методу Update більш ніж досить»"
  :reason "contra c-3 was void"}
 {:decision :c4-registration-helper
  :status :confirmed
  :at "2026-09-16"
  :value "one registration helper that always exposes IAppStateSystem and passes the AppState flags; every first-order registration goes through it"
  :verified-by "gate after s1, owner's answer 4 to contra c-4 (fix-c)"
  :reason "a system can never silently miss every state"}
 {:decision :delete-generic-unitask-system
  :status :confirmed
  :at "2026-09-16"
  :value "IUniTaskSystem<T> is deleted; every use (IPrioritizedUniTaskSystem<T> base, ViewSubSystem, ShowHexesUISystem) moves to the non-generic IUniTaskSystem; ShowHexesUISystem is flagged AppState.MainMenu and run by MainMenu on entry; FirstUIStep is deleted"
  :verified-by "owner at the s1 gate: «IUniTaskSystem<T> ... взагалі має бути видалено», then answers 1-3 to the follow-up"
  :reason "owner's word"}
 {:decision :scope-lift-for-interface-replacement
  :status :confirmed
  :supersedes :scope-first-order
  :at "2026-09-16"
  :value "sub-systems and turn phases may be touched ONLY to replace IUniTaskSystem<T> by IUniTaskSystem; otherwise :scope-first-order stands"
  :verified-by "owner's answer 4 to the follow-up (rated 70)"
  :reason "the interface replacement reaches IPrioritizedUniTaskSystem<T>, TurnPhaseSubSystem and ViewSubSystem"}
 {:decision :c5-subsystem-interface-with-type
  :status :confirmed
  :supersedes :scope-lift-for-interface-replacement
  :at "2026-09-16"
  :value "sub-systems (and turn phases) get their own interface, separate from IUniTaskSystem / IAppStateSystem; it carries a Type property in which every sub-system names the type of its orchestrator system; the orchestrator receives the list of all sub-systems through DI and keeps those whose Type is its own — the same filtering a state does by AppState; all orchestrators are in scope for this"
  :verified-by "gate after s1 (amended), owner's answer 1 to contra c-5: «для підсистем ми можемо зробити свій окремий інтерфейс … Type property де кожна з підсистем сама вкаже тип своєї системи … така ж фільтрація всередині системи-оркестратора»"
  :reason "sub-systems never carry AppState, yet keep DI collection"}
 {:decision :c6-drop-t
  :status :confirmed
  :at "2026-09-16"
  :value "IPrioritizedUniTaskSystem loses its generic T; the classes left without use are deleted — MapGenerationStep, TurnPhaseStep (and any other marker type whose only use was that T)"
  :verified-by "gate after s1 (amended), owner's answer 2: «прибирай разом з непотрібними класами»"
  :reason "T groups no collection after the change"}
 {:decision :c7-boot-startup-code-deleted
  :status :confirmed
  :at "2026-09-16"
  :value "the one-shot startup code in Boot.Start (ExecuteStartupStep over ConfigLoading and InstanceObjects) is deleted; everything runs only through IAppState states; the owner's answer did not address the double-listing of a pipeline stage, so s1's most-specific-kind rule stands until the owner says otherwise"
  :verified-by "owner: «one-shot треба видалити і залишити лише IAppState», clarified: «Ти писав про Boot, де зараз є окремий one-shot стан в методі Start для ConfigLoading та InstanceObjects - цей код буде видалено»"
  :reason "one mechanism: states"}
 {:decision :subsystem-single-contract
  :status :confirmed
  :at "2026-09-16"
  :value "orchestrators exist only for UniTask-based systems; there is exactly one kind of sub-system — IPrioritizedUniTaskSystem (plus the orchestrator Type of :c5-subsystem-interface-with-type); in this cascade the contract is unified and collection goes through Type; returning UniTask<CommandBuffer> from sub-systems is a SEPARATE later task"
  :verified-by "owner: «оркестратори не існують для систем які не UniTask based … є лише 1 тип підсистем … IPrioritizedUniTaskSystem які будуть поверати UniTask<CommandBuffer>», then «1 - більше нічого, 5 - окремою задачею»"
  :reason "one sub-system shape"}
 {:decision :gate-s1-round-4
  :status :confirmed
  :at "2026-09-16"
  :value {:update-hosts "DistrictBuildUISystem and DistrictOpenConditionEvaluatorTableChangedSystem stay UpdatedSystem; their sub-systems become UniTask-based (IPrioritizedUniTaskSystem with Type)"
          :sync-parts "the synchronous parts of UniTask stages (Update(GameState), Prepare, TrySpawn families) move to async IPrioritizedUniTaskSystem in this cascade"
          :turn-phase-orchestrator "DistrictOpenConditionEvaluatorSystem (a turn phase that is itself an orchestrator): the owner rewrites it later"
          :idisposable "ViewSubSystem and TurnPhaseSubSystem declare IDisposable themselves"
          :fmgraph "graph readings are fixed in the rules step at the end"
          :most-specific-kind "a system goes only into the list of its most specific kind"
          :initialization-app-state "InitializationAppState is deleted"
          :open "the three-host evaluator family — re-asked in plain words"}
  :verified-by "owner's answers 1-8 at the s1 gate: «1 - самі залишаються Update системами але їх підсистеми стають UniTask based 2 - так 3 - не розумію запитання 4 - потім перепишу 5 - так 6 - так 7 - так 8 - видали»"
  :reason "owner's word"}
 {:decision :evaluator-family-untouched
  :status :confirmed
  :at "2026-09-16"
  :value "the DistrictOpenConditionEvaluator sub-system family (DistrictExistConditionEvaluatorSubSystem, DistrictSingleOpenConditionEvaluatorSubSystem) and how its three hosts (DistrictOpenConditionEvaluatorBootstrapSystem, DistrictOpenConditionEvaluatorSystem, DistrictOpenConditionEvaluatorTableChangedSystem) collect and run it stay exactly as today — no Type, no new contract; the owner rewrites it himself later. The hosts still receive only what the rest of this task forces on every system of their kind (AppState flags through the registration rule for first-order hosts; loss of T on IPrioritizedUniTaskSystem) — nothing more"
  :verified-by "owner: «нічого не робимо, потім перепишу це сам» in reply to the three-host / self-reference question"
  :reason "owner keeps this family for his own rewrite"}
 {:decision :systems-and-subsystems-are-two-types
  :status :confirmed
  :at "2026-09-16"
  :value "a system and a sub-system are two separate kinds of entity with NO shared interface and no inheritance between their contracts. System side: IAppStateSystem and the system kinds (IUpdatedSystem, ILateUpdatedSystem, the non-generic IUniTaskSystem : IAppStateSystem) — pipeline stages, config loaders and ShowHexesUISystem are systems. Sub-system side: IPrioritizedUniTaskSystem (no T) plus the orchestrator Type — never IUniTaskSystem, never IAppStateSystem, never AppState. c-15 is void: its options all mixed the two"
  :verified-by "owner at the third s1 gate: «ти робиш зараз якусь дику рекурсію і змішуєш системи та підсистеми. Це треба розносити в 2 різні типи сутностей»"
  :reason "owner has stated the separation repeatedly; every decision above is read under it"}
 {:decision :c13-async-parts-in-update-host
  :status :confirmed
  :at "2026-09-16"
  :value "no change: a UniTask started on the main thread runs synchronously until its first real await; if a part ever needs to wait, the host starts the UniTask and checks its status on each Update"
  :verified-by "owner at the third s1 gate, answer 2"
  :reason "owner: «0 проблем … Це ж елементарно»"}
 {:decision :c14-unhandled-entry-throws
  :status :confirmed
  :at "2026-09-16"
  :value "a catalogue entry that no spawn part handles throws an exception (mechanism chosen in s2)"
  :verified-by "owner at the third s1 gate, answer 3: «ну кидай якийсь вийняток»"
  :reason "fail loud"}
 {:decision :c16-delete-isystem-t
  :status :confirmed
  :at "2026-09-16"
  :value "ISystem<T> is deleted"
  :verified-by "owner at the third s1 gate, answer 4"
  :reason "no users left"}
 {:decision :s1-approved
  :status :confirmed
  :at "2026-09-16"
  :value "s1 approved on condition that the answers above are folded in; s2 opens"
  :verified-by "owner: «запускай s2»"
  :reason "gate after s1 passed"}
 {:decision :s2-approved
  :status :confirmed
  :at "2026-09-16"
  :value "s2 approved with the contra choices c-17 fix-a (IPipelineStageSystem), c-18 fix-a (VContainer into Ecs.Extensions), c-19 fix-a (IAppStateSystem only), c-20 fix-a (checked cast for Repopulate), c-21 fix-a (rows == entries), c-22 fix-b (GameModeMachine.Stop called by Boot.OnDestroy, no IDisposable), c-23 fix-a (accept, owner checks in Unity); the code stage runs as parallel Sonnet subagents, each owning a disjoint set of folders"
  :verified-by "owner at the s2 gate: «1a 2a 3a 4a 5a 6b 7a / наріж задачі для sonnet саб агентів щоб вони могли паралельно писати код не заважаючи один одному»"
  :reason "gate after s2 passed"}
 {:decision :close-by-owner
  :status :confirmed
  :at "2026-09-16"
  :value "the task closes after the code stage: everything is committed as is; read-back, converge and the rules step (RULES_SPECIFICATION + carriers + fmgraph runs_in readings) are NOT run; the three meter findings (Temp in async GenerationSystem, label-tag filter in DistrictOpenConditionSpawnSystem, [StateAllowed] without reasons) are left to the owner, who continues by hand"
  :verified-by "owner: «коміть все що є і можеш закривати задачу. Далі я хочу сам ручками попрацювати і зробити так як я це бачу. … все працює, гра абсолютно стабільна»"
  :reason "owner takes the code over"}
 {:decision :s1-stage-by-subagent
  :status :confirmed
  :at "2026-09-16"
  :value "the s1 stage runs in a separate Opus subagent reading only FLOW.md, CONTEXT.md and the files CONTEXT.md names"
  :verified-by "owner's instruction: «s1 запусти в opus агенті»"
  :reason "owner's word, same as the context stage"}]
```

# Disproven

```clojure
[]
```

# Attempted

```clojure
[]
```

# Progress

```clojure
{:status :complete
 :completed #{"normalization" "confirmed contract" "context" "s1" "s2" "code"}
 :current :none
 :remaining #{}
 :skipped-by-owner #{"read-back" "converge" "rules step" "fixes of the three meter findings"}
 :stage :closed
 :next-invocation :none
 :resume-context "closed 2026-09-16 by the owner; the code is the owner's from here; open leftovers are listed in # Acceptance and decision :close-by-owner"}
```

# Acceptance

```clojure
[{:meter "mcp__roslyn__get_diagnostics, FantasyMayor.sln, severity Error" :target "clean" :actual "0 errors solution-wide, 2026-09-16, after all six code slices" :status :met}
 {:meter "/arch-check, changed scope (git diff of 112 .cs)" :target "no introduced violation"
  :actual "1 introduced error: Allocator.Temp in async GenerationSystem.Execute → AssignHexTypes after await [rule alloc/no-temp-in-async]; [StateAllowed] without a reason on new orchestrator list fields (GenerationSystem, HexResourcesSystem, TurnProcessorSystem) and a missing marker on MainHudSpawnSystem._subSystems (pre-existing shape) [rule state/injected-collection]"
  :status :handed-to-owner}
 {:meter "fmgraph.py check vs a HEAD worktree" :target "no new warning on a touched file"
  :actual "1 new: tag law — DistrictOpenConditionSpawnSystem filters by label tag DistrictOpenConditionTag in AllTags (the rows==entries count of s2 contra c-21 fix-a) [rule tag/label-not-a-filter]; 32 others identical to baseline"
  :status :handed-to-owner}
 {:meter "owner's Unity check" :target "compiles; start → MainMenu → MapCreation → Gameplay as before" :actual "owner 2026-09-16: «все працює, гра абсолютно стабільна»" :status :met}]
```

# Amendments

```clojure
[{:received-at "2026-09-16"
  :raw-request "IUniTaskSystem<FirstUIStep> - це не правильно, інтерфейс з IUniTaskSystem<T> взагалі має бути видалено
1 - перше
2 - поки що залишиться як є, потім я запущу гру, побачу як це впаде й прийму рішення про зміну
3 - запитання не вірне по суті, методу Update більш ніж досить.
4 - helper"
  :normalized {:gate :after-s1
               :c-1 "fix-a — Boot owns the startup order and advances when an entry completes; from MainMenu on it applies RequestedMode; the machine stops switching by itself and exposes entry completion"
               :c-2 "unchanged for now — owner runs the game, watches it fail or not, decides then"
               :c-3 "the question is void — Update is enough for ShowHexesUISystem; no Show/Hide contract"
               :c-4 "fix-c — one registration helper that always exposes IAppStateSystem"
               :scope-change "delete the generic IUniTaskSystem<T> entirely — touches IPrioritizedUniTaskSystem<T> (14 pipeline stages, turn phases via TurnPhaseSubSystem), ViewSubSystem (sub-systems), ShowHexesUISystem, FirstUIStep; sub-systems and turn phases were :off-limits"
               :open "resolved 2026-09-16 by the next amendment"}
  :confirmed true}
 {:received-at "2026-09-16"
  :raw-request "1 - IUniTaskSystem<T> замінюється на IUniTaskSystem
2 - перше
3 - так
4 - перше"
  :normalized {:replace "every use of the generic IUniTaskSystem<T> is replaced by the non-generic IUniTaskSystem; IUniTaskSystem<T> is deleted"
               :show-hexes-ui "ShowHexesUISystem implements IUniTaskSystem, flagged AppState.MainMenu; MainMenu runs it on entry"
               :first-ui-step "FirstUIStep is deleted"
               :scope "the ban on sub-systems and turn phases is lifted ONLY for this interface replacement, inside this task"}
  :confirmed true}
 {:received-at "2026-09-16"
  :raw-request "1 - для підсистем ми можемо зробити свій окремий інтерфейс який буде їх відрізняти одна від одної. Для того щоб зберегти інʼєкції ми можемо використати Type property де кожна з підсистем сама вкаже тип своєї системи. І далі буде така ж фільтрація всередині системи-оркестратора
2 - прибирай разом з непотрібними класами
3 - one-shot треба видалити і залишити лише IAppState
---
Ти писав про Boot, де зараз є окремий one-shot стан в методі Start для ConfigLoading та InstanceObjects - цей код буде видалено"
  :normalized "see decisions :c5-subsystem-interface-with-type :c6-drop-t :c7-boot-startup-code-deleted"
  :confirmed true}]
```
