---
category: A
read: archive
status: implemented
tags:
  - boot
  - di
  - cascade
related:
  - "[FLOW](FLOW.md)"
---

# Task

```clojure
{:task :app-state-di-wiring
 :flow "Flows/APP_STATE_DI_WIRING/FLOW.md"
 :goal "a game state gathers its own systems through DI; Boot no longer wires systems into states by hand"
 :makes-today "Boot.Construct receives every first-order system by concrete type plus two DI collections, builds four states with new, builds GameModeMachine with new over a hand-filled dictionary, and itself runs the ConfigLoading and InstanceObjects startup steps before switching the machine to MainMenu"
 :result "Boot.Construct names no system; putting a system into a state = its installer registration line carrying WithParameter(AppState flags); every AppState value has one IAppState implementation registered through a VContainer installer; each state receives IReadOnlyList<IAppStateSystem> and keeps the systems whose flags contain its own value; GameModeMachine receives every IAppState through DI; Boot receives GameModeMachine through DI and only switches into the first state"
 :behaviour-must-hold "start → ConfigLoading loaders → InstanceObjects step → MainMenu → (Generate pressed) MapCreation pipeline + settle frames → Gameplay ticking — as it behaves today (FLOW # Acceptance, the owner's Unity check)"
 :decided [{:id :scope-first-order
            :value "only first-order systems implement IAppStateSystem; sub-systems and turn phases stay parts of their orchestrators, injected straight into them"}
           {:id :flags-multi-state
            :value "AppState stays [Flags]; a system belongs to every state whose value its flags contain; EventCleanupSystem is in every game state"}
           {:id :state-gets-every-kind
            :value "every state receives its filtered systems of every kind — IUpdatedSystem, ILateUpdatedSystem, IUniTaskSystem, IPrioritizedUniTaskSystem<MapGenerationStep>; what a state does with them is its own internal implementation, shaped by the owner afterwards"}
           {:id :startup-steps-are-states
            :value "ConfigLoading and InstanceObjects become IAppState states run by GameModeMachine; Boot only switches into the first state; every AppState value gets a state — Initialization and GameOver included"}
           {:id :value-by-with-parameter
            :value "a system receives its AppState through .WithParameter(AppState …) on its installer registration — the ConfigLoaderSystem precedent"}
           {:id :rules-after-code
            :value "code first; RULES_SPECIFICATION.md and its carriers are fixed at the end, as a later step of this task — not by s1, s2 or the code stage"}
           {:id :system-definition-from-architecture
            :value "what counts as a system is ARCHITECTURE.md (def systems) :system/definition — the raw request says «Бери з правил в Achitecture»"}]
 :out-of-scope #{"sub-systems (26) and turn phases (3) — they stay injected into their orchestrators, no IAppStateSystem, no AppState parameter"
                 "RULES_SPECIFICATION.md, ARCHITECTURE.md, Patterns/ recipes, INDEX.md, skills (including .claude/skills/fantasymayor-graph scripts) — rules and tools are fixed in a later step"
                 "the internal behaviour of each state beyond receiving and keeping its filtered systems — tick order, settle frames, transitions, entry effects are the owner's later shaping, except what the behaviour-must-hold line above requires"
                 "Unity-side assets: scenes, prefabs, .asset, addressable entries, .meta files (CLAUDE.md § 3)"
                 "builds of the Unity project or its csproj/sln (CLAUDE.md § 3)"
                 "reading .unity scenes or scene-serialized assets (CLAUDE.md § 3) — Boot's scene wiring stays an owner-verified fact"
                 "renaming or removing InitializationAppState, MapGenerationStep, FirstUIStep — not asked"
                 "ShowHexesUISystem's own shape (see the fact :show-hexes-ui-is-not-a-system and question Q2)"}}
```

# Reads

```clojure
[{:ref "Assets/Modules/Boot/Implementation/Boot.cs"
  :why "the hand wiring the change removes: Start (startup steps + first Switch), ExecuteStartupStep (flag filter precedent at line 55), Update/LateUpdate gate, OnDestroy dispose, Construct (lines 88-151)"
  :read #{:whole}
  :is #{:number-source :integration-point}}
 {:ref "Assets/Modules/Boot/Implementation/GameModeMachine.cs"
  :why "the machine: constructor over IReadOnlyDictionary<AppState, IAppState>, Switch, Tick-then-RequestedMode, entry suspension, Dispose"
  :read #{:whole}
  :is #{:integration-point :type-source}}
 {:ref "Assets/Modules/Boot/Implementation/IAppState.cs"
  :why "the state contract: Mode, RequestedMode, EnterAsync, Tick, LateTick, Exit"
  :read #{:whole}
  :is #{:type-source}}
 {:ref "Assets/Modules/Boot/Implementation/States/MainMenuState.cs"
  :why "today's MainMenu: EntityStore + ShowHexesUISystem, ripe-generate-request transition to MapCreation"
  :read #{:whole}
  :is #{:number-source :integration-point}}
 {:ref "Assets/Modules/Boot/Implementation/States/MapCreationState.cs"
  :why "today's MapCreation: pipeline sorted by Priority run in EnterAsync, IUpdatedSystem params sorted by Priority, SettleFrames = 3, transition to Gameplay"
  :read #{:whole}
  :is #{:number-source :integration-point}}
 {:ref "Assets/Modules/Boot/Implementation/States/GameplayState.cs"
  :why "today's Gameplay: EntityStorages, update and late-update lists sorted by Priority, EnterAsync seeds HexIconsVisibilityComponent, HexIconsVisibilityChangedEvent, TurnCountComponent(1)"
  :read #{:whole}
  :is #{:number-source :integration-point}}
 {:ref "Assets/Modules/Boot/Implementation/States/MapLoadingState.cs"
  :why "placeholder state with no systems and no transition"
  :read #{:whole}
  :is #{:integration-point}}
 {:ref "Assets/Modules/Boot/Core/AppState.cs"
  :why "the [Flags] enum, 8 single-bit values; no All member"
  :read #{:whole}
  :is #{:type-source}}
 {:ref "Assets/Modules/Boot/Core/IAppStateSystem.cs"
  :why "the contract: only AppState AppState { get; }"
  :read #{:whole}
  :is #{:type-source}}
 {:ref "Assets/Modules/Boot/Core/InitializationAppState.cs, MapGenerationStep.cs, FirstUIStep.cs"
  :why "three empty structs in Boot.Core used as type tags"
  :read #{:whole}
  :is #{:type-source}}
 {:ref "Assets/Modules/Boot/Implementation/Boot.Implementation.asmdef, Assets/Modules/Boot/Core/Boot.Core.asmdef"
  :why "Boot's assembly boundaries"
  :read #{:whole}
  :is #{:integration-point}}
 {:ref "Assets/Scripts/EcsExtensions/IUniTaskSystem.cs, IUpdatedSystem.cs, ILateUpdatedSystem.cs, IPrioritizedUniTaskSystem.cs, ISystem.cs, UpdatedSystem.cs, LateUpdatedSystem.cs, ConfigLoaderSystem.cs, EventCleanupSystem.cs, SystemPriorities.cs, GameState.cs, Ecs.Extensions.asmdef"
  :why "the system kinds a state receives, their constructors and Priority; which already carry AppState"
  :read #{:whole}
  :is #{:type-source :number-source}}
 {:ref "Assets/Presentation/Terrain/Systems/VertexGridSpawnSystem.cs"
  :why "the one non-config startup step; AppState(appState, storages) constructor precedent"
  :read #{:whole}
  :is #{:example}}
 {:ref "Assets/Presentation/UI/GeneratorMenu/Systems/ShowHexesUISystem.cs"
  :why "MainMenuState's dependency; IUniTaskSystem<FirstUIStep>, not a system by ARCHITECTURE's definition"
  :read #{:whole}
  :is #{:integration-point}}
 {:ref "Assets/Scripts/Installers/World/WorldInstaller.cs"
  :why "the root LifetimeScope: Configure registers EventCleanup/HexSelection/CameraMovement and a config loader (lines 64-70), then InstallModules calls 13 IInstaller in order (lines 77-89); the natural home to register states and GameModeMachine (Installers.World sees Boot.Implementation)"
  :read #{:whole}
  :is #{:integration-point :example}}
 {:ref "Assets/Presentation/UI/Installer/UIInstaller.cs, Assets/Domains/Map/Generation/Installer/TerrainGeneratorInstaller.cs, Assets/Presentation/Terrain/Installer/TerrainViewInstaller.cs, Assets/Domains/Map/HexResources/Installer/HexResourcesInstaller.cs, Assets/Presentation/HexResources/Installer/HexResourcesViewInstaller.cs, Assets/Presentation/Districts/Installer/DistrictsInstaller.cs, Assets/Presentation/HexIcons/Installer/HexIconsInstaller.cs, Assets/Modules/Turn/Installer/TurnInstaller.cs, Assets/Domains/Actors/Installer/ActorsInstaller.cs, Assets/Domains/Economy/Installer/EconomyInstaller.cs, Assets/Domains/Actions/Installer/ActionsInstaller.cs"
  :why "every registration line of a first-order system (census below); several comments there say «Boot wires … by hand» and become false after the change"
  :read #{:whole}
  :is #{:integration-point}}
 {:ref "Assets/Modules/Turn/Turn.asmdef (the asmdef owning Assets/Modules/Turn)"
  :why "Turn does not reference Boot.Core directly — see fact :turn-assembly-blind-to-appstate"
  :read #{:whole}
  :is #{:integration-point}}
 {:ref "Library/PackageCache/jp.hadashikick.vcontainer@7ec84530fae8/Runtime/Registry.cs, Runtime/Internal/InjectParameter.cs, Runtime/Internal/InstanceProviders/CollectionInstanceProvider.cs, Runtime/IObjectResolverExtensions.cs (ResolveOrParameter), Runtime/RegistrationBuilder.cs (As, WithParameter)"
  :why "VContainer 1.18.0 source: collection injection, typed-parameter matching, concrete-type visibility after As"
  :read #{:section}
  :is #{:type-source}}
 {:ref "ARCHITECTURE.md ## Systems (def systems) (def system-role), ## One-shot, pipeline, turn phase, ## System state"
  :why "the definition of a system (which classes are first-order), roles, the no-instance-state ban a new AppState property must respect"
  :read #{:section}
  :is #{:type-source}}
 {:ref "Patterns/PATTERN_PERFRAME_SYSTEM.md, PATTERN_REACTIVE_SYSTEM.md, PATTERN_PIPELINE_STAGE.md, PATTERN_CONFIG_LOADER.md, PATTERN_CLEANUP_SYSTEM.md"
  :why "the recipes' :wiring lines describe today's wiring (Boot.Construct by hand; pipeline auto-collect; WithParameter(AppState.ConfigLoading/InstanceObjects)); they are NOT edited in this task (:rules-after-code)"
  :read #{:section}
  :is #{:example}}]
```

# Search

```clojure
[{:for "the census of first-order systems by role"
  :where "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py systems --role <reactive|per_frame|cleanup|pipeline_stage|startup_step>"
  :settles "which classes receive AppState; result recorded in (def census) below"}
 {:for "installer + lifetime of one system; what fills a collection"
  :where "fmgraph.py explain <System>; fmgraph.py resolve <Interface>; fmgraph.py consumers <Base>"
  :settles "registration line, Lifetime, which orchestrator collects a sub-system family"}
 {:for "who else resolves a system by concrete type"
  :where "fmgraph.py explain <System> — incoming injects edges; mcp__roslyn__find_references on the class"
  :settles "whether a registration may drop its .As<Concrete>() (today only Boot, and MainMenuState via Boot, resolve concretes)"}
 {:for "whether an assembly compiles against AppState / IAppStateSystem / IAppState"
  :where "python3 Tools/asmdef_reach.py can <Assembly> <Type>"
  :settles "asmdef edits the code needs"}]
```

# Facts

```clojure
[{:fact "AppState is [Flags] with 8 single-bit values: Initialization 1<<0, ConfigLoading 1<<1, InstanceObjects 1<<2, MainMenu 1<<3, MapCreation 1<<4, MapLoading 1<<5, Gameplay 1<<6, GameOver 1<<7; there is no None and no All member"
  :at "2026-09-16"
  :verified-by "read Assets/Modules/Boot/Core/AppState.cs"
  :consequence "«in every state» is written as the OR of the eight values; a state's Mode is one bit; membership test precedent is (system.AppState & step) != 0 — Boot.cs:55"}
 {:fact "IAppStateSystem declares only AppState AppState { get; }; IUniTaskSystem (non-generic) extends IDisposable and IAppStateSystem and adds UniTask Execute(CancellationToken); it is the only type that extends IAppStateSystem today"
  :at "2026-09-16"
  :verified-by "read Boot/Core/IAppStateSystem.cs and EcsExtensions/IUniTaskSystem.cs; grep over Assets for IAppStateSystem"
  :consequence "the 27 startup-step registrations already carry AppState; the 40 other first-order classes do not"}
 {:fact "IUpdatedSystem and ILateUpdatedSystem each declare int Priority and void Update(GameState); UpdatedSystem and LateUpdatedSystem are abstract, implement them, and have protected constructors (EntityStore, Archetype) and (ArchetypeQuery); IPrioritizedUniTaskSystem<in T> : IUniTaskSystem<T> adds int Priority; IUniTaskSystem<in T> : IDisposable declares UniTask Update(CancellationToken); none of these carries AppState"
  :at "2026-09-16"
  :verified-by "read the files in Assets/Scripts/EcsExtensions"
  :consequence "AppState reaches reactive, per-frame, cleanup and pipeline-stage systems only through a new constructor parameter on each concrete class or on a base; a base's protected constructors are called by subclasses, so a base change touches every subclass constructor"}
 {:fact "ConfigLoaderSystem<T>(AppState appState, string address, IAddressable addressable, EntityStorages storages) and VertexGridSpawnSystem(AppState appState, EntityStorages storages) are the only constructors in Assets with an AppState-typed parameter; the registration is .As<IUniTaskSystem>().WithParameter(AppState.ConfigLoading).WithParameter(\"address\", ConfigAddresses.X) — first used at WorldInstaller.cs:66-69 — and .As<IUniTaskSystem>().WithParameter(AppState.InstanceObjects) at TerrainViewInstaller.cs:59-61"
  :at "2026-09-16"
  :verified-by "grep for AppState over Assets/**/*.cs; read both classes and installers"
  :consequence "the WithParameter precedent exists; no constructor has two AppState parameters"}
 {:fact "VContainer 1.18.0 WithParameter<TParam>(value) creates TypedParameter(typeof(TParam), value) whose Match is parameterType == Type (exact type, no name); WithParameter(string, value) matches by parameter name; ResolveOrParameter checks the registration's parameters first for every constructor, [Inject] method, field and property parameter and falls back to the container; a parameter nobody matches is ignored"
  :at "2026-09-16"
  :verified-by "read Runtime/Internal/InjectParameter.cs lines 5-60, Runtime/RegistrationBuilder.cs lines 89-119, Runtime/IObjectResolverExtensions.cs lines 41-66 in Library/PackageCache/jp.hadashikick.vcontainer@7ec84530fae8; version from Packages/manifest.json"
  :consequence "WithParameter(AppState.MapCreation | AppState.Gameplay) is typed AppState and fills the one AppState parameter; it collides only with a second AppState-typed parameter in the same constructor or [Inject] member, and none exists"}
 {:fact "VContainer collection injection: every registration exposed .As<T>() is added to one CollectionInstanceProvider for T that answers IEnumerable<T> and IReadOnlyList<T>, in registration order; each element is resolved with its own lifetime (Singleton from its registering container, others from the current scope); one registration with .As<A, B>() is one Registration in both collections, so Singleton/Scoped give the same instance through both; resolving IReadOnlyList<T> with zero registrations yields an empty array (TryFallbackToSingleElementCollection); Add throws «Conflict implementation type» only for two Singleton registrations of the same implementation type in one collection"
  :at "2026-09-16"
  :verified-by "read Runtime/Registry.cs (Build, AddToBuildBuffer, TryFallbackToSingleElementCollection) and Runtime/Internal/InstanceProviders/CollectionInstanceProvider.cs"
  :consequence "IReadOnlyList<IAppStateSystem> can gather every first-order system across all 13 installers, order preserved; ConfigLoaderSystem<X> closed types differ, so 26 of them do not conflict"}
 {:fact "VContainer: once a registration has any .As<…>(), its implementation type is guarded with a null entry and is NOT resolvable as itself unless the concrete type is listed in .As or .AsSelf() is called"
  :at "2026-09-16"
  :verified-by "read Runtime/Registry.cs lines 27-39 and TryGet lines 112-115"
  :consequence "every current registration lists the concrete type (.As<Concrete>() or .As<Concrete, Interface>()); a registration that drops it breaks whoever still resolves the concrete type"}
 {:fact "there is exactly one LifetimeScope in C#: WorldInstaller : LifetimeScope (Installers.World); its Awake clears parentReference (always root); Configure registers EntityStorages as instance, IMainCanvasProvider Scoped, EventCleanupSystem, HexSelectionSystem, ConfigLoaderSystem<CameraMovementConfig>, CameraMovementSystem, then InstallModules calls, in order, AddressableInstaller, UIInstaller, PathfindingInstaller, TerrainGeneratorInstaller, TerrainViewInstaller, HexResourcesInstaller, HexResourcesViewInstaller, DistrictsInstaller, HexIconsInstaller, TurnInstaller, ActorsInstaller, EconomyInstaller, ActionsInstaller — all plain IInstaller into the same builder"
  :at "2026-09-16"
  :verified-by "grep for LifetimeScope and IInstaller over Assets/**/*.cs; read WorldInstaller.cs whole; fmgraph.py search Installer (15 installer nodes incl. the IInstaller interface)"
  :consequence "no parent/child scope question: one container holds every registration, so one IReadOnlyList<IAppStateSystem> sees all systems; Scoped in the root scope is one instance per container"}
 {:fact "Boot is a scene MonoBehaviour injected through [Inject] Construct; no C# registers it (no RegisterComponent / InjectGameObject in Assets); the graph marks it scene_object true; the only remaining mechanism in the code is the root LifetimeScope's serialized autoInjectGameObjects list, which VContainer injects at the end of Build (LifetimeScope.cs:229 AutoInjectAll) during Awake — before Boot.Start"
  :at "2026-09-16"
  :verified-by "fmgraph.py explain Boot; grep over Assets; read VContainer Runtime/Unity/LifetimeScope.cs lines 135-229, 359; the scene itself NOT read (CLAUDE.md § 3 ban)"
  :consequence "Boot keeps its [Inject] method; the scene wiring is an owner-verified fact, unchanged by this task"}
 {:fact "Boot.Construct today receives IReadOnlyList<IUniTaskSystem> startupSystems, IReadOnlyList<IPrioritizedUniTaskSystem<MapGenerationStep>> generationPipeline, 27 concrete types (ShowHexesUISystem + 25 reactive/per-frame systems + EventCleanupSystem) and EntityStorages; builds MainMenuState(storages.World, showHexesUI), MapCreationState(generationPipeline, eventCleanup), GameplayState(storages, 24 IUpdatedSystem, 2 ILateUpdatedSystem {cameraMovement, hexIconsContainerPosition}), MapLoadingState(); and GameModeMachine over a dictionary with MainMenu, MapCreation, MapLoading, Gameplay only"
  :at "2026-09-16"
  :verified-by "read Boot.cs lines 88-151; fmgraph.py explain Boot (29 injects edges)"
  :consequence "every first-order system is ALREADY constructed at Boot's injection, before ConfigLoading runs — resolving them all into states at injection adds no new eagerness; Initialization, ConfigLoading, InstanceObjects, GameOver have no state today"}
 {:fact "Boot.Start: Application.targetFrameRate = 60; awaits ExecuteStartupStep(ConfigLoading) then ExecuteStartupStep(InstanceObjects) — each runs every IUniTaskSystem whose flags intersect the step, sequentially, in registration order, ThrowIfCancellationRequested before each, token CancellationToken.None; then sets _startupCompleted and Switch(MainMenu). Update/LateUpdate return while !_startupCompleted, else call machine Tick/LateTick with new GameState(Time.deltaTime); OnDestroy disposes the machine"
  :at "2026-09-16"
  :verified-by "read Boot.cs lines 38-82"
  :consequence "ConfigLoading must finish before InstanceObjects (VertexGridSpawnSystem reads TerrainViewConfig from storages) and both before MainMenu; startup steps have no Priority — their order is registration order"}
 {:fact "GameModeMachine(IReadOnlyDictionary<AppState, IAppState>) throws on null; Switch cancels and disposes the previous entry token, calls Exit on the current state, looks up _states[mode] (KeyNotFoundException on a missing mode), sets _entering and fires EnterAsync(token).Forget(); Tick and LateTick do nothing while _entering or no current; Tick calls current.Tick then, if RequestedMode has a value, Switch(it); EnterAsync swallows OperationCanceledException and clears _entering only when not cancelled; Dispose cancels, Exits current, nulls it; the machine is IDisposable and is not registered in DI today"
  :at "2026-09-16"
  :verified-by "read GameModeMachine.cs whole"
  :consequence "a transition requested inside EnterAsync is only acted on at the first Tick after entry completes — one frame per chained state (ConfigLoading → InstanceObjects → MainMenu); VContainer cannot inject a dictionary by collection — it injects IReadOnlyList<IAppState>; if the machine becomes a container-created IDisposable, the container disposes it on scope destroy in addition to Boot.OnDestroy (Dispose is safe to call twice: second call finds null token and null current)"}
 {:fact "IAppState (Boot.Implementation): AppState Mode; AppState? RequestedMode; UniTask EnterAsync(CancellationToken); void Tick(GameState); void LateTick(GameState); void Exit(). MainMenuState: deps EntityStore world + ShowHexesUISystem; EnterAsync resets request, awaits ui.Update(token), ui.Show(); Tick sets RequestedMode = MapCreation when a ripe TerrainGenerationGenerateEventComponent exists; Exit ui.Hide(). MapCreationState: const SettleFrames = 3; ctor sorts pipeline and params IUpdatedSystem by Priority; EnterAsync resets request and counter, runs each stage with ThrowIfCancellationRequested + await stage.Update(token); Tick updates systems, counts, requests Gameplay at 3. GameplayState: deps EntityStorages + update/late lists sorted by Priority; EnterAsync sets HexIconsVisibilityComponent(true), raises HexIconsVisibilityChangedEvent, sets TurnCountComponent(1); RequestedMode null. MapLoadingState: all no-ops, RequestedMode null"
  :at "2026-09-16"
  :verified-by "read IAppState.cs and the four files in Implementation/States whole"
  :consequence "states depend on non-system data (EntityStore / EntityStorages / ShowHexesUISystem) beyond the filtered systems; Initialization, ConfigLoading, InstanceObjects and GameOver states do not exist yet"}
 {:fact "ShowHexesUISystem implements IUniTaskSystem<FirstUIStep> (not the non-generic IUniTaskSystem), is registered Lifetime.Scoped .As<ShowHexesUISystem>() at UIInstaller.cs:35-36, has no role in fmgraph (kind other), and its only consumer is Boot → MainMenuState, which calls Update(token), Show(), Hide()"
  :at "2026-09-16"
  :verified-by "read ShowHexesUISystem.cs and UIInstaller.cs; fmgraph.py explain ShowHexesUISystem; fmgraph.py state MainMenuState"
  :consequence ":show-hexes-ui-is-not-a-system — ARCHITECTURE (def systems) lists IUniTaskSystem and IPrioritizedUniTaskSystem but not IUniTaskSystem<T>, so it is not a first-order system; MainMenuState then has zero first-order systems and still needs it (question Q2)"}
 {:fact "InitializationAppState and FirstUIStep and MapGenerationStep are empty structs in Boot.Core; InitializationAppState is used nowhere outside its file; FirstUIStep only as ShowHexesUISystem's IUniTaskSystem<FirstUIStep> type argument (no DI registration exposes that interface); MapGenerationStep is the type argument of the 14 pipeline stages' IPrioritizedUniTaskSystem<MapGenerationStep> — the DI collection-grouping tag MapCreationState consumes"
  :at "2026-09-16"
  :verified-by "grep over Assets/**/*.cs for each name; read the three files"
  :consequence "pipeline stages will carry two groupings of the same fact — the MapGenerationStep type tag and the AppState.MapCreation flag; removing either is not asked"}
 {:fact "turn phases (MayorAPRestoreSubSystem, BuildDistrictTurnTickSystem, DistrictOpenConditionEvaluatorSystem) derive from TurnPhaseSubSystem : IPrioritizedUniTaskSystem<TurnPhaseStep> and are collected by TurnProcessorSystem (ctor IReadOnlyList<TurnPhaseSubSystem>, TurnProcessorSystem.cs:36); the 26 sub-systems are collected by GenerationSystem, HexResourcesSystem, TerrainViewSystem, HexResourcesViewSystem, MainHudSpawnSystem, DistrictBuildUISystem, DistrictOpenConditionSpawnSystem, DistrictBuildOutcomeSpawnSystem, and the DistrictOpenConditionEvaluatorSubSystem family by three hosts (DistrictOpenConditionEvaluatorBootstrapSystem, DistrictOpenConditionEvaluatorSystem, DistrictOpenConditionEvaluatorTableChangedSystem)"
  :at "2026-09-16"
  :verified-by "fmgraph.py systems --role turn_phase / sub_system; fmgraph.py consumers <base> for the 10 bases; grep TurnPhaseSubSystem.cs"
  :consequence "all 29 are reached only through orchestrators — out of scope; TurnPhaseStep ≠ MapGenerationStep, so turn phases never enter the map-creation pipeline collection"}
 {:fact "assemblies that define or register first-order systems — Ecs.Extensions, Installers.World, Presentation.UI, Domains.Map, Presentation, Domains.Actors, Domains.Economy, Domains.Actions, UserInput, Boot.Implementation — each compile against AppState and IAppStateSystem (direct Boot.Core reference); Installers.World compiles against IAppState, GameModeMachine and the state classes (direct Boot.Implementation reference); Boot.Implementation cannot reference Installers.World"
  :at "2026-09-16"
  :verified-by "python3 Tools/asmdef_reach.py assembly-of <Type> and can <Assembly> <Type> for every pair"
  :consequence "states and GameModeMachine can be registered from Installers.World (WorldInstaller); no reference cycle is needed"}
 {:fact ":turn-assembly-blind-to-appstate — the Turn assembly (TurnInstaller, TurnProcessorSystem, TurnCountSystem) does NOT compile against AppState or IAppStateSystem: Boot.Core is reachable only transitively via Turn → Ecs.Extensions → Boot.Core; asmdef_reach says «add 'Boot.Core' to Turn.asmdef references»"
  :at "2026-09-16"
  :verified-by "python3 Tools/asmdef_reach.py can Turn AppState; can Turn IAppStateSystem"
  :consequence "the code stage must add Boot.Core to Turn.asmdef references — an asmdef edit outside :where literally, but inside the Turn module (question Q4)"}
 {:fact "fmgraph derives runs_in edges (and fmgraph.py state) ONLY from `new …State(…)` inside Boot.Construct (source_reading.py read_state_composition), and reads a registration's app_state as the text after the first «AppState.» of the FIRST argument of WithParameter (source_reading.py lines 449-458) — a flags expression is kept as raw text such as «MapCreation | AppState.Gameplay»; recipes.py find_config_loaders keys on app_state == \"ConfigLoading\" exactly"
  :at "2026-09-16"
  :verified-by "read .claude/skills/fantasymayor-graph/scripts/source_reading.py lines 440-478 and recipes.py lines 128-137; fmgraph.py state GameplayState/MapCreationState/MainMenuState"
  :consequence "after the change the graph loses every runs_in edge silently (no warning, so fmgraph check stays green); config loaders keep ConfigLoading alone so their recipe detection holds; the graph scripts are skills — off-limits here, a note for the later rules step"}
 {:fact "fmgraph.py check baseline 2026-09-16: curated=True, warnings=32, integrity clean; warnings already standing on files this task will likely touch: WorldInstaller.cs:61 [archetype/birth], BuildDistrictActionSystem.cs:99 [index/pk-uniqueness], HexSelectionViewLoadingSystem.cs:29 and TerrainViewSystem.cs:42 [link/persistent-key], DeleteEntity attribution on BuildDistrictActionCancelSystem.cs:97-98, HexSelectionSystem.cs:103, DistrictBuildProgressViewDespawnSystem.cs:85, TerrainViewSystem.cs:99, DistrictBuildUISystem.cs:184"
  :at "2026-09-16"
  :verified-by "ran python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py check"
  :consequence "these are pre-existing — the meter «no new warning on a touched file» is measured against this list"}
 {:fact "installer comments that describe today's hand wiring: WorldInstaller.cs:63, UIInstaller.cs:19-23 and 71, TerrainViewInstaller.cs:67, HexResourcesViewInstaller.cs:28-29, DistrictsInstaller.cs:10-12 and 27-28, HexIconsInstaller.cs:27 and 31, ActionsInstaller.cs:12-17; Boot.cs summary lines 26-31 and 84-87; ConfigLoaderSystem.cs:13"
  :at "2026-09-16"
  :verified-by "read each installer whole"
  :consequence "each becomes a false comment after the change (meter :false-comments 0) — the code stage rewrites or removes them"}
 {:fact "EcsEventExtensions.IsRipe(e) = EventFrameComponent.Frame < Time.frameCount; EventCleanupSystem.Update deletes every EventTag entity that is ripe; TerrainGenerationGenerateEventComponent is raised only by HexesUI.cs:39 and read only by MainMenuState (EventArchetypes.Of, ripe check); MainMenuState's comment says the event is cleaned up «once MapCreation starts ticking»"
  :at "2026-09-16"
  :verified-by "read EcsEventExtensions.cs, EventCleanupSystem.cs, MainMenuState.cs; grep TerrainGenerationGenerateEventComponent over Assets"
  :consequence "with EventCleanupSystem now in MainMenu (:flags-multi-state), a MainMenu tick that runs cleanup before its own ripe check would delete the request and MainMenu never leaves — order inside MainMenu matters for :behaviour-must-hold (question Q3)"}]
```

```clojure
(def census  ;; first-order systems — fmgraph roles reactive, per_frame, cleanup, pipeline_stage, startup_step; «today» = where it effectively runs now
  {:counts {:reactive 18 :per_frame 7 :cleanup 1 :pipeline_stage 14 :startup_step-classes 2 :startup_step-registrations 27
            :classes 42 :registrations 67 :out-of-scope {:turn_phase 3 :sub_system 26}}
   :verified-by "fmgraph.py systems --role <role>; fmgraph.py explain <System> (source, lifetime, installer); fmgraph.py resolve IUniTaskSystem / IPrioritizedUniTaskSystem; line numbers from reading each installer whole; «today» from Boot.cs Construct/Start and the state classes"
   :cleanup [{:class EventCleanupSystem :file "Assets/Scripts/EcsExtensions/EventCleanupSystem.cs" :base IUpdatedSystem :ctor "(EntityStorages)" :installer "WorldInstaller.cs:64" :lifetime Singleton :as "As<EventCleanupSystem>()" :today "MapCreation (settle ticks) + Gameplay (last, Priority int.MaxValue)"}]
   :per_frame [{:class CameraMovementSystem :file "Assets/Modules/UserInput/Systems/CameraMovementSystem.cs" :base LateUpdatedSystem :installer "WorldInstaller.cs:70" :lifetime Singleton :as "As<CameraMovementSystem>()" :today "Gameplay (late)"}
               {:class HexSelectionSystem :file "Assets/Modules/UserInput/Systems/HexSelectionSystem.cs" :base UpdatedSystem :installer "WorldInstaller.cs:65" :lifetime Singleton :as "As<HexSelectionSystem>()" :today "Gameplay"}
               {:class ResourceBarSystem :file "Assets/Presentation/UI/MainHud/ResourceBar/Systems/ResourceBarSystem.cs" :base UpdatedSystem :installer "UIInstaller.cs:78" :lifetime Singleton :as "As<ResourceBarSystem>()" :today "Gameplay"}
               {:class DistrictBuildUISystem :file "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildUISystem.cs" :base "UpdatedSystem, IDisposable" :ctor "(EntityStorages, IReadOnlyList<DistrictBuildUISubSystem>)" :installer "UIInstaller.cs:82" :lifetime Singleton :as "As<DistrictBuildUISystem>()" :today "Gameplay"}
               {:class HexIconsContainerPositionSystem :file "Assets/Presentation/HexIcons/Systems/HexIconsContainerPositionSystem.cs" :base "ILateUpdatedSystem, IDisposable" :installer "HexIconsInstaller.cs:28" :lifetime Singleton :as "As<HexIconsContainerPositionSystem>()" :today "Gameplay (late)"}
               {:class TurnProcessorSystem :file "Assets/Modules/Turn/Systems/TurnProcessorSystem.cs" :base IUpdatedSystem :ctor "(EntityStorages, IReadOnlyList<TurnPhaseSubSystem>)" :installer "TurnInstaller.cs:21" :lifetime Singleton :as "As<TurnProcessorSystem>()" :today "Gameplay" :asmdef "Turn — no Boot.Core"}
               {:class TurnPanelViewSystem :file "Assets/Presentation/UI/MainHud/TurnPanel/Systems/TurnPanelViewSystem.cs" :base UpdatedSystem :installer "UIInstaller.cs:93" :lifetime Singleton :as "As<TurnPanelViewSystem>()" :today "Gameplay"}]
   :reactive [{:class HexSelectionViewSystem :file "Assets/Presentation/Terrain/Systems/HexSelectionViewSystem.cs" :base UpdatedSystem :installer "TerrainViewInstaller.cs:68" :lifetime Singleton :as "As<HexSelectionViewSystem>()" :today "Gameplay"}
              {:class HexInfoPanelSystem :file "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelSystem.cs" :base UpdatedSystem :installer "UIInstaller.cs:72" :lifetime Singleton :as "As<HexInfoPanelSystem>()" :today "Gameplay"}
              {:class HexInfoPanelHeaderSystem :file "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelHeaderSystem.cs" :base UpdatedSystem :installer "UIInstaller.cs:74" :lifetime Singleton :as "As<HexInfoPanelHeaderSystem>()" :today "Gameplay"}
              {:class ContextTabSelectionSystem :file "Assets/Presentation/UI/MainHud/ContextTabs/Systems/ContextTabSelectionSystem.cs" :base UpdatedSystem :installer "UIInstaller.cs:95" :lifetime Singleton :as "As<ContextTabSelectionSystem>()" :today "Gameplay"}
              {:class ContextTabsAvailabilitySystem :file "Assets/Presentation/UI/MainHud/ContextTabs/Systems/ContextTabsAvailabilitySystem.cs" :base UpdatedSystem :installer "UIInstaller.cs:97" :lifetime Singleton :as "As<ContextTabsAvailabilitySystem>()" :today "Gameplay"}
              {:class HexInfoPanelResourcesSystem :file "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelResourcesSystem.cs" :base UpdatedSystem :installer "UIInstaller.cs:76" :lifetime Singleton :as "As<HexInfoPanelResourcesSystem>()" :today "Gameplay"}
              {:class ForestSpawnSystem :file "Assets/Presentation/HexResources/Systems/ForestSpawnSystem.cs" :base UpdatedSystem :installer "HexResourcesViewInstaller.cs:30" :lifetime Singleton :as "As<ForestSpawnSystem>()" :today "Gameplay"}
              {:class BuildDistrictActionSystem :file "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionSystem.cs" :base UpdatedSystem :installer "ActionsInstaller.cs:28" :lifetime Singleton :as "As<BuildDistrictActionSystem>()" :today "Gameplay"}
              {:class DistrictBuildProgressViewSpawnSystem :file "Assets/Presentation/Districts/Systems/DistrictBuildProgressViewSpawnSystem.cs" :base UpdatedSystem :installer "DistrictsInstaller.cs:32" :lifetime Singleton :as "As<DistrictBuildProgressViewSpawnSystem>()" :today "Gameplay"}
              {:class BuildDistrictActionCancelSystem :file "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionCancelSystem.cs" :base UpdatedSystem :installer "ActionsInstaller.cs:31" :lifetime Singleton :as "As<BuildDistrictActionCancelSystem>()" :today "Gameplay"}
              {:class BuildDistrictCompletionSystem :file "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictCompletionSystem.cs" :base IUpdatedSystem :marker "SystemRole Reactive" :installer "ActionsInstaller.cs:34" :lifetime Singleton :as "As<BuildDistrictCompletionSystem>()" :today "Gameplay"}
              {:class DistrictBuildProgressViewDespawnSystem :file "Assets/Presentation/Districts/Systems/DistrictBuildProgressViewDespawnSystem.cs" :base UpdatedSystem :installer "DistrictsInstaller.cs:35" :lifetime Singleton :as "As<DistrictBuildProgressViewDespawnSystem>()" :today "Gameplay"}
              {:class DistrictViewSpawnSystem :file "Assets/Presentation/Districts/Systems/DistrictViewSpawnSystem.cs" :base UpdatedSystem :installer "DistrictsInstaller.cs:29" :lifetime Singleton :as "As<DistrictViewSpawnSystem>()" :today "Gameplay"}
              {:class ForestDespawnSystem :file "Assets/Presentation/HexResources/Systems/ForestDespawnSystem.cs" :base UpdatedSystem :installer "HexResourcesViewInstaller.cs:32" :lifetime Singleton :as "As<ForestDespawnSystem>()" :today "Gameplay"}
              {:class DistrictOpenConditionEvaluatorTableChangedSystem :file "Assets/Domains/Economy/DistrictOpenCondition/Systems/DistrictOpenConditionEvaluatorTableChangedSystem.cs" :base UpdatedSystem :ctor "(storages, IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> …)" :installer "EconomyInstaller.cs:69" :lifetime Scoped :as "As<DistrictOpenConditionEvaluatorTableChangedSystem>()" :today "Gameplay"}
              {:class HexIconsVisibilitySystem :file "Assets/Presentation/HexIcons/Systems/HexIconsVisibilitySystem.cs" :base UpdatedSystem :installer "HexIconsInstaller.cs:32" :lifetime Singleton :as "As<HexIconsVisibilitySystem>()" :today "Gameplay"}
              {:class TurnCountSystem :file "Assets/Modules/Turn/Systems/TurnCountSystem.cs" :base UpdatedSystem :installer "TurnInstaller.cs:24" :lifetime Singleton :as "As<TurnCountSystem>()" :today "Gameplay" :asmdef "Turn — no Boot.Core"}
              {:class HexInfoPanelDistrictSystem :file "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelDistrictSystem.cs" :base "UpdatedSystem, IDisposable" :installer "UIInstaller.cs:80" :lifetime Singleton :as "As<HexInfoPanelDistrictSystem>()" :today "Gameplay"}]
   :ctor-note "every reactive and per-frame class above except DistrictBuildUISystem, TurnProcessorSystem and DistrictOpenConditionEvaluatorTableChangedSystem has the constructor (EntityStorages storages) — none has an AppState parameter"
   :pipeline_stage [{:class GenerationSystem :file "Assets/Domains/Map/Generation/Systems/GenerationSystem.cs" :installer "TerrainGeneratorInstaller.cs:18" :lifetime Singleton :priority 100}
                    {:class HexResourcesSystem :file "Assets/Domains/Map/HexResources/Systems/HexResourcesSystem.cs" :installer "HexResourcesInstaller.cs:19" :lifetime Singleton :priority 200}
                    {:class TerrainViewSystem :file "Assets/Presentation/Terrain/Systems/TerrainViewSystem.cs" :installer "TerrainViewInstaller.cs:63" :lifetime Singleton :priority 300}
                    {:class HexResourcesViewSystem :file "Assets/Presentation/HexResources/Systems/HexResourcesViewSystem.cs" :installer "HexResourcesViewInstaller.cs:25" :lifetime Singleton :priority 400}
                    {:class HexSelectionViewLoadingSystem :file "Assets/Presentation/Terrain/Systems/HexSelectionViewLoadingSystem.cs" :installer "TerrainViewInstaller.cs:65" :lifetime Singleton :priority 500}
                    {:class TerrainViewDebugSystem :file "Assets/Presentation/Terrain/Systems/TerrainViewDebugSystem.cs" :installer "TerrainViewInstaller.cs:70" :lifetime Singleton :priority 600}
                    {:class HexIconsSpawnSystem :file "Assets/Presentation/HexIcons/Systems/HexIconsSpawnSystem.cs" :installer "HexIconsInstaller.cs:24" :lifetime Singleton :priority 700}
                    {:class MainHudSpawnSystem :file "Assets/Presentation/UI/MainHud/Systems/MainHudSpawnSystem.cs" :installer "UIInstaller.cs:55" :lifetime Singleton :priority 800}
                    {:class DistrictBuildUISpawnSystem :file "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildUISpawnSystem.cs" :installer "UIInstaller.cs:68" :lifetime Singleton :priority 810}
                    {:class CitySpawnSystem :file "Assets/Domains/Actors/City/Systems/CitySpawnSystem.cs" :installer "ActorsInstaller.cs:26" :lifetime Singleton :priority 900}
                    {:class MayorSpawnSystem :file "Assets/Domains/Actors/Mayor/Systems/MayorSpawnSystem.cs" :installer "ActorsInstaller.cs:29" :lifetime Singleton :priority 910}
                    {:class DistrictOpenConditionSpawnSystem :file "Assets/Domains/Economy/DistrictOpenCondition/Systems/DistrictOpenConditionSpawnSystem.cs" :installer "EconomyInstaller.cs:48" :lifetime Scoped :priority 920}
                    {:class DistrictOpenConditionEvaluatorBootstrapSystem :file "Assets/Domains/Economy/DistrictOpenCondition/Systems/DistrictOpenConditionEvaluatorBootstrapSystem.cs" :installer "EconomyInstaller.cs:57" :lifetime Scoped :priority 925}
                    {:class DistrictBuildOutcomeSpawnSystem :file "Assets/Domains/Economy/DistrictBuildOutcome/Systems/DistrictBuildOutcomeSpawnSystem.cs" :installer "EconomyInstaller.cs:37" :lifetime Scoped :priority 930}]
   :pipeline-note "all 14 are internal sealed, implement IPrioritizedUniTaskSystem<MapGenerationStep>, registered .As<Concrete, IPrioritizedUniTaskSystem<MapGenerationStep>>(), priorities from SystemPriorities.WorldInit; today they run only in MapCreationState.EnterAsync, sorted ascending by Priority, awaited one by one"
   :startup_step [{:class "ConfigLoaderSystem<T>" :file "Assets/Scripts/EcsExtensions/ConfigLoaderSystem.cs" :registrations 26 :lifetime Singleton :as "As<IUniTaskSystem>().WithParameter(AppState.ConfigLoading).WithParameter(\"address\", …)"
                   :lines ["WorldInstaller.cs:66 CameraMovementConfig"
                           "UIInstaller.cs:38 HexTerrainIconConfig" "UIInstaller.cs:43 DistrictIconConfig" "UIInstaller.cs:48 InventoryResourceIconConfig"
                           "TerrainGeneratorInstaller.cs:14 TerrainGenerationConfig"
                           "TerrainViewInstaller.cs:19 InnerIsolineConfig" "TerrainViewInstaller.cs:24 OuterIsolineConfig" "TerrainViewInstaller.cs:29 HeightSmoothingConfig" "TerrainViewInstaller.cs:34 WindErosionConfig" "TerrainViewInstaller.cs:39 HydraulicErosionConfig" "TerrainViewInstaller.cs:44 TerrainViewConfig" "TerrainViewInstaller.cs:49 TerrainTextureConfig" "TerrainViewInstaller.cs:54 WaterViewConfig"
                           "HexResourcesInstaller.cs:14 HexResourcesConfig"
                           "HexResourcesViewInstaller.cs:15 HexResourcesViewConfig" "HexResourcesViewInstaller.cs:20 ClayViewConfig"
                           "DistrictsInstaller.cs:17 DistrictViewsConfig" "DistrictsInstaller.cs:22 DistrictBuildProgressViewsConfig"
                           "HexIconsInstaller.cs:14 HexIconsConfig" "HexIconsInstaller.cs:19 HexResourceIconConfig"
                           "ActorsInstaller.cs:16 CityConfig" "ActorsInstaller.cs:21 MayorConfig"
                           "EconomyInstaller.cs:22 DistrictBuildsConfig" "EconomyInstaller.cs:27 DistrictBuildCostsConfig" "EconomyInstaller.cs:32 DistrictBuildOutcomesConfig" "EconomyInstaller.cs:43 DistrictOpenConditionsConfig"]
                   :today "ConfigLoading, run by Boot.Start in registration order"}
                  {:class VertexGridSpawnSystem :file "Assets/Presentation/Terrain/Systems/VertexGridSpawnSystem.cs" :registrations 1 :installer "TerrainViewInstaller.cs:59" :lifetime Singleton :as "As<IUniTaskSystem>().WithParameter(AppState.InstanceObjects)" :today "InstanceObjects, run by Boot.Start"}]
   :not-a-system [{:class ShowHexesUISystem :why "IUniTaskSystem<FirstUIStep> is outside ARCHITECTURE's system list; fmgraph gives it no role" :installer "UIInstaller.cs:35" :lifetime Scoped :today "MainMenu, as MainMenuState's direct dependency"}]
   :by-state-today {:Initialization [] :ConfigLoading "26 ConfigLoaderSystem<T>" :InstanceObjects [VertexGridSpawnSystem]
                    :MainMenu "no first-order system (ShowHexesUISystem only)" :MapCreation "14 pipeline stages + EventCleanupSystem"
                    :MapLoading [] :Gameplay "18 reactive + 7 per-frame (2 of them late) + EventCleanupSystem" :GameOver []}})
```

# Occasions

```clojure
[{:exit "a state whose value no system's flags contain — its filtered lists are empty"
  :occasion "census :by-state-today — Initialization, MapLoading and GameOver have no system; MainMenu has no first-order system; VContainer yields an empty IReadOnlyList when nothing matches (fact on collection injection)"}
 {:exit "a state receives a kind of system it has nothing to do with (e.g. Gameplay receives no pipeline stage, MapCreation no late-updated system)"
  :occasion "census — per-frame/reactive carry Gameplay only, pipeline stages MapCreation only, startup steps ConfigLoading/InstanceObjects only; under :state-gets-every-kind every list exists, many are empty"}
 {:exit "a first-order system registered without an AppState parameter"
  :occasion "today 40 of 42 first-order classes have no AppState parameter and 40 of 67 registrations no WithParameter(AppState…) (census :ctor-note, :startup_step); VContainer resolves an unmatched AppState enum parameter from the container, where AppState is not registered — resolution throws (fact on ResolveOrParameter fallback)"}
 {:exit "a mode switched to with no registered state"
  :occasion "GameModeMachine.Switch indexes _states[mode] and throws KeyNotFoundException (fact on GameModeMachine); today 4 of 8 values have a state"}
 {:exit "a system whose flags carry several values runs in several states"
  :occasion "decision :flags-multi-state — EventCleanupSystem in every state; today it already runs in MapCreation and Gameplay (Boot.cs:127, 138)"}
 {:exit "cancellation of a state's entry"
  :occasion "GameModeMachine.Switch cancels the previous entry token; MapCreationState and Boot.ExecuteStartupStep call ThrowIfCancellationRequested before each awaited system (facts on GameModeMachine and Boot.Start)"}]
```

# Questions for the findings gate

```clojure
[(:question-1 {:id :Q1-first-state
               :ask "which state does Boot switch into first — Initialization (enum order, no systems) or ConfigLoading (what Boot runs first today)? and who moves Initialization → ConfigLoading → InstanceObjects → MainMenu"
               :facts "Boot.Start runs ConfigLoading, InstanceObjects, then Switch(MainMenu); Initialization has no system and no user; a transition happens only through RequestedMode after a Tick (fact on GameModeMachine); :behaviour-must-hold needs this chain to work, while :state-gets-every-kind leaves state behaviour to the owner"})
 (:question-2 {:id :Q2-show-hexes-ui
               :ask "ShowHexesUISystem is not a system by ARCHITECTURE's definition and MainMenuState needs it (Update/Show/Hide) — does MainMenuState keep receiving it as a plain constructor dependency, or does it become a first-order system with AppState.MainMenu?"
               :facts "fact :show-hexes-ui-is-not-a-system; census :not-a-system"})
 (:question-3 {:id :Q3-cleanup-in-main-menu
               :ask "EventCleanupSystem in every state puts it into MainMenu, whose only exit is a ripe TerrainGenerationGenerateEventComponent that cleanup deletes when ripe — must MainMenu check its request before its systems tick, or should cleanup's flags exclude some states?"
               :facts "fact on IsRipe / EventCleanupSystem / MainMenuState; decision :flags-multi-state"})
 (:question-4 {:id :Q4-turn-asmdef
               :ask "may the code stage add Boot.Core to the Turn assembly's asmdef references? TurnInstaller, TurnProcessorSystem and TurnCountSystem cannot see AppState otherwise"
               :facts "fact :turn-assembly-blind-to-appstate"})
 (:question-5 {:id :Q5-concrete-as
               :ask "once Boot stops resolving concretes, do registrations keep .As<Concrete>() (harmless, keeps the concrete resolvable) or expose only interfaces?"
               :facts "fact on VContainer concrete-type visibility; only Boot (and MainMenuState via Boot) resolves concretes today"})
 (:question-6 {:id :Q6-graph-runs-in
               :ask "accepted that fmgraph loses its runs_in edges and `state` answers after the change (no warning — the scripts are off-limits here), to be repaired in the later rules/tools step?"
               :facts "fact on fmgraph runs_in derivation"})]
```

# Verification

```clojure
{:meters [{:meter "mcp__roslyn__get_diagnostics — solutionPath FantasyMayor.sln, scoped to the changed files" :target "clean"}
          {:meter "/arch-check on the changed scope" :target "no violation the change introduced (an AppState get-only property set in the constructor is readonly immutable data — not state per ARCHITECTURE ## System state)"}
          {:meter "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py check" :target "no warning standing on a touched file beyond the 2026-09-16 baseline listed in # Facts; integrity clean"}
          {:meter "the owner's Unity check" :target "compiles; start → MainMenu → MapCreation → Gameplay behaves as before"}]
 :owner-check "compilation and runtime behaviour in Unity; Boot's scene injection (autoInjectGameObjects on the WorldInstaller object) is scene-serialized and verified only by the owner"}
```

# Complete

```clojure
{:names-every-file-the-next-stage-may-read true
 :states-out-of-scope true
 :ends-with-verification true
 :links-to-follow-on-own-initiative 0}
```
