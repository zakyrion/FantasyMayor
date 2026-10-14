---
category: A
read: archive
status: implemented
tags:
  - boot
  - structure
---

Code structure of the app flow: contracts in Core, states without game knowledge, transitions as systems.

# Subject

```clojure
{:structure "Boot's state contracts move into the Core assembly; the eight app states lose game knowledge; every mode transition is a system; the machine ticks a state only after its entry completes"
 :from "Flows/APP_STATES_OUT_OF_BOOT/ALGO_APP_FLOW.md (agreed 2026-09-26)"
 :touches #{"Assets/Scripts/Core/ — receives AppState, IAppStateSystem, GameState, IEventTag, IAppState, AppStateRequestedEvent; Core.asmdef + UniTask"
            "Assets/Modules/Boot/Core/ — emptied (see # Decisions :boot-core)"
            "Assets/Modules/Boot/Implementation/ — GameModeMachine, the eight states, Boot.cs, InitializationSystem, two new transition systems, one settle system; asmdef"
            "Assets/Scripts/EcsExtensions/ — usings only (IUniTaskSystem, IUpdatedSystem, AppStateSystemRegistration, bases …)"
            "Assets/Scripts/Installers/World/WorldInstaller.cs + Installers.World.asmdef"
            "Assets/Presentation/UI/GeneratorMenu/ — HexesUI (three buttons), new MainMenu request system; UIInstaller"
            "Assets/Presentation/HexIcons/ — new Gameplay seed step; HexIconsInstaller"
            "Assets/Modules/Turn/ — new Gameplay seed step; TurnInstaller"
            "Assets/Scripts/EcsExtensions/SystemPriorities.cs — new RuntimeTick constants"
            "every .cs that names a moved type — usings; every .asmdef that references Boot.Core — references"}}
```

# Structure

```clojure
{:AppFlow/entry {:does "the order the translator works in — each step leaves the project compiling in principle"
                 :flow (-> (:step-1 {:calls MoveContractsToCore :out :core-contracts})
                           (:step-2 {:calls RewireReferences :out :compiling-tree})
                           (:step-3 {:calls MachineWaitsForEntry :out :machine})
                           (:step-4 {:calls StripStates :out :states})
                           (:step-5 {:calls AddTransitionSystems :out :transitions})
                           (:step-6 {:calls AddGameplaySeeds :out :seeds})
                           (:step-7 {:calls WireMenuButtons :out :menu})
                           (:step-8 {:calls Register :out :registrations}))}

 :AppFlow/methods
 {MoveContractsToCore {:does "git mv each type file WITH its .meta into Assets/Scripts/Core/ and set namespace Core"
                       :in #{:boot-core-types :kernel-types :boot-impl-types}
                       :out :core-contracts
                       :note "moves: Modules/Boot/Core/AppState.cs, Modules/Boot/Core/IAppStateSystem.cs, Scripts/EcsExtensions/GameState.cs, Scripts/EcsExtensions/IEventTag.cs, Modules/Boot/Implementation/IAppState.cs, Modules/Boot/Implementation/Events/AppStateRequestedEvent.cs → all to Assets/Scripts/Core/ root (the Core layout keeps its files at the root) — EXCEPT AppStateRequestedEvent, which goes to Assets/Scripts/Core/Events/ with namespace Core.Events (rule event/declare: an event lives in an Events/ folder; amendment 2026-09-26, decision 18). The new Events/ folder gets its .meta from Unity, never from you. IAppState's summary is rewritten: no RequestedMode (gone from the interface); a state requests nothing — systems raise AppStateRequestedEvent; the machine ticks it only after EnterAsync completes. Core.asmdef gains the reference \"UniTask\" (IAppState returns UniTask)."}
  RewireReferences {:does "make every file and assembly see the moved types from Core"
                    :in #{:core-contracts}
                    :out :compiling-tree
                    :flow (-> (:step-1 "every .cs with `using Modules.Boot.Core;` → `using Core;`, deduplicated when `using Core;` is already there")
                              (:step-2 "every .cs that names GameState, IEventTag, IAppState or AppStateRequestedEvent without `using Core;` gains it; `using Modules.Boot.Implementation.Events;` is removed where it only served AppStateRequestedEvent")
                              (:step-3 "asmdefs: remove the Boot.Core reference (by name, or GUID:a15c10a9d6c504c279af3d692db5acbc in Presentation.UI.asmdef); add \"Core\" to Installers.World.asmdef and Flows.DistrictBuild.asmdef, which lack it")
                              (:step-4 "Boot.Implementation.asmdef keeps only Core, Ecs.Extensions, UniTask, VContainer — the owner already removed Presentation and Presentation.UI by hand; remove Domains.Map, UserInput, Turn, Domains.Actions, Domains.Economy only after StripStates leaves no file using them")
                              (:step-5 "git rm -r Assets/Modules/Boot/Core and Assets/Modules/Boot/Core.meta once nothing lives there — decision 16")
                              (:step-6 "crefs: `Modules.Boot.Core.AppState.MainMenu` in ShowHexesUISystem.cs → `Core.AppState.MainMenu` — the using line and this cref are the ONLY edits allowed in that file"))
                    :exits #{"a cref or fully-qualified name left on Modules.Boot.Core — roslyn get_diagnostics shows it; fix, never suppress"}}
  MachineWaitsForEntry {:does "GameModeMachine ticks and late-ticks the current state only after its EnterAsync completed"
                        :in #{:machine-state}
                        :out :machine
                        :writes #{:entry-completed}
                        :note "Switch sets :entry-completed false before starting the entry; the private EnterAsync sets it true after `await state.EnterAsync(token)` returns for THAT state (a cancelled entry throws and never sets it); Tick/LateTick return without ticking while it is false. Update the class summary: remove the stale IsEntryCompleted mention, say the machine withholds ticks until entry completes."}
  StripStates {:does "each of the eight states only filters its systems, runs its entry, ticks, late-ticks — no game knowledge, no transition"
               :in #{:states-now}
               :out :states
               :note "remove every `RequestedMode` property and `_requestedMode` field (the interface no longer has it); ConfigLoadingState and InstanceObjectsState: drop the Raise after entry and the EntityStorages ctor parameter; MainMenuState: drop the EventReader<TerrainGenerationGenerateEventComponent> and RequestMapCreationOnGenerateRequest; MapCreationState: drop SettleFrames, _settledFrames, CountSettleFrame; GameplayState: drop SeedStartOfPlay, EntityStorages and the Turn using. ConfigLoadingState keeps RunAllAsync (loaders run together); every other state keeps RunEntryAsync. Rewrite each summary to what the state now does. Keep eight classes."}
  AddTransitionSystems {:does "one system per transition in Boot.Implementation/Systems, each raising AppStateRequestedEvent"
                        :in #{:storages}
                        :out :transitions
                        :note "shape of each = InitializationSystem (IUpdatedSystem, AppState from ctor, EntityStorages, Priority from SystemPriorities.RuntimeTick, Update raises). ConfigLoadingCompletedSystem: flag ConfigLoading, raises {Requested AppState.InstanceObjects}. InstanceObjectsCompletedSystem: flag InstanceObjects, raises {Requested AppState.MainMenu}. MapCreationSettleSystem: flag MapCreation, counts ticks in a [StateAllowed(\"ticks since MapCreation entry completed; reset when the request is raised\")] int field; at :settle-ticks raises {Requested AppState.Gameplay} and resets the count to 0. Each carries the same :deviation/in-code comment InitializationSystem carries (per-frame, no work table, no tick anchor) adapted to its transition."
                        :numbers {:settle-ticks "3 — const in MapCreationSettleSystem, moved from MapCreationState with its comment"}}
  AddGameplaySeeds {:does "the two start-of-play writes GameplayState did move into one-shot steps of the layers that own the data"
                    :in #{:storages}
                    :out :seeds
                    :note "HexIconsVisibilitySeedSystem in Assets/Presentation/HexIcons/Systems/: IUniTaskSystem, flag Gameplay; Execute: storages.Singletons.Set(new HexIconsVisibilityComponent(true)); storages.Events.Raise(new HexIconsVisibilityChangedEvent()); return UniTask.CompletedTask; Dispose empty. TurnCountSeedSystem in Assets/Modules/Turn/Systems/: IUniTaskSystem, flag Gameplay; Execute: storages.Singletons.Set(new TurnCountComponent(1)). Carry GameplayState's two comments over to them. cancellationToken.ThrowIfCancellationRequested() first in each Execute (fail loud)."}
  WireMenuButtons {:does "all three generator-menu buttons raise the generate event, and a MainMenu reactive system turns it into the MapCreation request"
                   :in #{:storages :generate-reader}
                   :out :menu
                   :note "HexesUI.Start: GenerateButton, SecondButton and GenerateMeshButton (ids from TerrainGenerator.uxml) each `.clicked += GenerateHexes` — nothing else in HexesUI changes (its view-boundary deviation is a separate task). New MapCreationRequestSystem in Assets/Presentation/UI/GeneratorMenu/Systems/ by Patterns/PATTERN_REACTIVE_SYSTEM.md, batch shape: IUpdatedSystem, readonly EventReader<TerrainGenerationGenerateEventComponent>, Update: `if (!_generateRequests.DrainBatch()) return;` then storages.Events.Raise(new AppStateRequestedEvent { Requested = AppState.MapCreation }). No role marker."}
  Register {:does "every new system reaches its state through RegisterAppStateSystem in its layer's installer"
            :in #{:transitions :seeds :menu}
            :out :registrations
            :note "WorldInstaller (beside InitializationSystem): ConfigLoadingCompletedSystem → AppState.ConfigLoading, InstanceObjectsCompletedSystem → AppState.InstanceObjects, MapCreationSettleSystem → AppState.MapCreation, all Lifetime.Singleton. UIInstaller: MapCreationRequestSystem → AppState.MainMenu. HexIconsInstaller: HexIconsVisibilitySeedSystem → AppState.Gameplay. TurnInstaller: TurnCountSeedSystem → AppState.Gameplay. SystemPriorities.RuntimeTick gains ConfigLoadingCompleted = 3, InstanceObjectsCompleted = 4, MapCreationSettle = 5, MapCreationRequest = 6 (distinct; each state has its own systems, so order among them is moot). State registrations in WorldInstaller stay as they are."}}

 :AppFlow/data
 {:boot-core-types {:type "AppState, IAppStateSystem" :as "AppState, IAppStateSystem" :lives :external :holds "the files in Assets/Modules/Boot/Core/"}
  :kernel-types {:type "GameState, IEventTag" :as "GameState, IEventTag" :lives :external :holds "the files in Assets/Scripts/EcsExtensions/"}
  :boot-impl-types {:type "IAppState, AppStateRequestedEvent" :as "IAppState, AppStateRequestedEvent" :lives :external :holds "the files in Assets/Modules/Boot/Implementation/"}
  :core-contracts {:type "the six types in namespace Core" :as "Core" :lives :run :holds "the contracts every layer sees through the Core assembly"}
  :compiling-tree {:type "usings and asmdef references" :as "—" :lives :run :holds "no file or assembly names Boot.Core any more"}
  :machine-state {:type GameModeMachine :as GameModeMachine :lives :external :holds "the machine as it is today"}
  :entry-completed {:type bool :as _isEntryCompleted :lives GameModeMachine :holds "the current state's entry finished"}
  :machine {:type GameModeMachine :as GameModeMachine :lives :run :holds "a machine that withholds ticks during entry"}
  :states-now {:type IAppState :as "the eight *State classes" :lives :external :holds "the states as they are today, with the owner's hand edit in GameplayState"}
  :states {:type IAppState :as "the eight *State classes" :lives :run :holds "states that only run their systems"}
  :storages {:type EntityStorages :as _storages :lives :external :holds "injected by DI into every new system"}
  :generate-reader {:type "EventReader<TerrainGenerationGenerateEventComponent>" :as _generateRequests :lives MapCreationRequestSystem :holds "injected Transient by DI"}
  :transitions {:type IUpdatedSystem :as "ConfigLoadingCompletedSystem, InstanceObjectsCompletedSystem, MapCreationSettleSystem" :lives :run :holds "the three new transition systems"}
  :seeds {:type IUniTaskSystem :as "HexIconsVisibilitySeedSystem, TurnCountSeedSystem" :lives :run :holds "the two start-of-play steps"}
  :menu {:type IUpdatedSystem :as MapCreationRequestSystem :lives :run :holds "the MainMenu → MapCreation request"}
  :registrations {:type "RegisterAppStateSystem lines" :as "—" :lives :run :holds "six new registration lines"}}}
```

# Build

```clojure
{:language "C# 9 (Unity), Friflo.Engine.ECS 3.6 (precompiled DLL, auto-referenced), VContainer, UniTask (package asmdef \"UniTask\")"
 :runtime "Unity Editor — the owner runs it; NEVER build the project (no dotnet build, msbuild, Unity CLI build)"
 :modules "asmdef assemblies; a type compiles only through a DIRECT reference (python3 Tools/asmdef_reach.py can <Assembly> <Type>); namespace follows the folder"
 :tests {:runner :none :command :none :style "no automated tests in this project — never add any"}
 :check ["mcp__roslyn__get_diagnostics solutionPath FantasyMayor.sln on the changed files — target clean; CS0246 on a type just moved in the same batch is workspace staleness, name it once, do not chase"
         "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py build && … check — no new warning on a touched file"
         "/arch-check on the changed scope"]
 :conventions "ARCHITECTURE.md; Patterns/PATTERN_REACTIVE_SYSTEM.md for MapCreationRequestSystem; fail loud — throw, never a silent return"}
```

# Traps

```clojure
[{:trap "Unity .meta"
  :where "every moved or new file"
  :avoid "move with `git mv X.cs` AND `git mv X.cs.meta`; a new .cs gets NO .meta from you — Unity writes it on import; never create or edit a .meta"
  :verified-by "AGENTS.md § 3 bans"}
 {:trap "asmdef reference by GUID"
  :where "Assets/Presentation/UI/Presentation.UI.asmdef"
  :avoid "it references Boot.Core as GUID:a15c10a9d6c504c279af3d692db5acbc — a name search misses it"
  :verified-by "grep of the Boot.Core.asmdef.meta guid over all asmdefs"}
 {:trap "non-transitive references"
  :where "Installers.World, Flows.DistrictBuild"
  :avoid "they see Boot.Core/Ecs.Extensions today but not Core; add \"Core\" to both"
  :verified-by "asmdef_reach refs per assembly using a moved type"}
 {:trap "owner's hand edits"
  :where "Boot.Implementation.asmdef, GameplayState.cs (uncommitted)"
  :avoid "build on them; never restore what the owner removed"
  :verified-by "git diff at survey time"}
 {:trap "WithParameter matches AppState by type"
  :where "AppStateSystemRegistration.RegisterAppStateSystem — `.WithParameter(appState)`"
  :avoid "a system must not take a second AppState constructor parameter — hard-code the requested mode inside each transition system"
  :verified-by "Read AppStateSystemRegistration.cs"}
 {:trap "ShowHexesUISystem is off-limits"
  :where "Assets/Presentation/UI/GeneratorMenu/Systems/ShowHexesUISystem.cs"
  :avoid "only its `using` line and one cref change (RewireReferences step-5); nothing else"
  :verified-by "owner, decision 1 and the gate answer on the forced edit"}]
```

# Decisions

```clojure
[{:decision "contracts: IAppState, AppStateRequestedEvent, IEventTag in the Core assembly; with them AppState, IAppStateSystem, GameState; AppStateSystems stays in Boot.Implementation" :by "owner, decisions 2, 5, 8"}
 {:decision "states carry no game knowledge; every transition is a system" :by "owner, decisions 3, 10"}
 {:decision "all three generator-menu buttons → MapCreation, via a MainMenu reactive system in Presentation.UI" :by "owner, decisions 9, 1(C of the first round)"}
 {:decision "the machine never ticks a state before its entry completes" :by "owner, decision 13"}
 {:decision "HexesUI keeps its view-boundary deviation in this task" :by "owner, decision 14"}
 {:decision "ShowHexesUISystem: nothing but the forced using/cref lines" :by "owner, decision 1 + gate"}
 {:decision "fail loud — throw on a missing prerequisite, ThrowIfCancellationRequested, never a quiet return" :by "owner, decision 3 of the first round; project ban"}
 {:decision "AppStateRequestedEvent lives in Assets/Scripts/Core/Events/, namespace Core.Events — every file that raises or reads it has `using Core.Events;`" :by "owner, decision 18 — review found the root placement broke event/declare"}
 {:decision "Boot.Core is deleted: after the moves, git rm -r Assets/Modules/Boot/Core and Assets/Modules/Boot/Core.meta (its asmdef, .meta files and the empty Archetypes/Components/Tags folders); no asmdef references it" :by "owner, decision 16"}]
```

# Parts

```clojure
;; one agent for the whole is proposed — the steps are ordered (moves before usings before systems), and parts would share the rewiring
[]
```

# Contra

```clojure
[{:id :c-1
  :kills "the off-limits file"
  :case "AppState leaves namespace Modules.Boot.Core"
  :fails "ShowHexesUISystem stops compiling unless its using line and one cref change"
  :fix [{:id :fix-a :confidence 80 :is "allow exactly those two lines" :cost "a mechanical touch of the off-limits file"}
        {:id :fix-b :confidence 20 :is "keep namespace Modules.Boot.Core for the moved types inside Core" :cost "breaks :place/namespace (namespace follows the folder)"}]
  :outcome "fix-a — owner, decision 15"}
 {:id :c-2
  :kills "an empty assembly"
  :case "Boot.Core holds no type after the move"
  :fails "an empty asmdef with 11 dangling references and empty Archetypes/Components/Tags folders"
  :fix [{:id :fix-a :confidence 75 :is "git rm the Boot.Core folder with its .meta files and remove its references" :cost "one more deletion; Unity reimports"}
        {:id :fix-b :confidence 25 :is "keep Boot.Core empty" :cost "a dead assembly every layer still references"}]
  :outcome "fix-a — owner, decision 16"}]
```

# Added

```clojure
[{:added "removed the now-redundant `using EcsExtensions;` from GameModeMachine.cs and all eight *State.cs files"
  :why "RewireReferences (step-1/step-2) covers adding or swapping using directives for the moved types; it does not name pruning a directive that became dead once GameState left EcsExtensions for Core. Left in place it is a CS8019 hidden diagnostic on every one of those nine files — removed for the same reason the structure asks new usings to be added: a correct import list."}
 {:added "dropped `using Core;` from Assets/Scripts/Core/Events/AppStateRequestedEvent.cs itself"
  :why "decision 18 moves the type into namespace Core.Events without saying the file's own usings change; once it lives in the Core.Events namespace, C# already sees Core's members (AppState) by nested-namespace lookup, so `using Core;` there is a CS8019 hidden diagnostic — removed."}
 {:added "dropped `using Core;` from Assets/Scripts/Installers/World/EventReaderAotDeclarations.cs, replaced by `using Core.Events;`"
  :why "decision 18 names the file's `using Core;` → `using Core.Events;` swap only implicitly (\"every file that raises or reads it gets using Core.Events; and drops using Core; only if it becomes unused\"); confirmed by grep that this file's only Core-namespace use was AppStateRequestedEvent, so the drop applies here — the other 6 raiser/reader files (MapCreationRequestSystem, Boot.cs, ConfigLoadingCompletedSystem, InitializationSystem, InstanceObjectsCompletedSystem, MapCreationSettleSystem) still use AppState from Core directly and keep both usings."}]
```
