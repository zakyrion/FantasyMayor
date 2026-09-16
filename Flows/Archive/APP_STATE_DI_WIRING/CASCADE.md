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
  - "[CONTEXT](CONTEXT.md)"
---

# Subject

```clojure
{:task :app-state-di-wiring
 :flow "Flows/APP_STATE_DI_WIRING/FLOW.md"
 :context "Flows/APP_STATE_DI_WIRING/CONTEXT.md"
 :subject "the DI wiring of game states — AppState-flagged first-order systems, IAppState states, GameModeMachine, Boot — and the DI wiring of every orchestrator's sub-systems and turn phases under one sub-system contract"
 :previous-cascade :none
 :stage :s2
 :amended ["2026-09-16 — after the s1 gate: decisions :c1-boot-drives-startup :c2-mainmenu-exit-as-is :c3-update-is-enough :c4-registration-helper :delete-generic-unitask-system :scope-lift-for-interface-replacement"
           "2026-09-16 — second amendment: decisions :c5-subsystem-interface-with-type (supersedes :scope-lift-for-interface-replacement) :c6-drop-t :c7-boot-startup-code-deleted"
           "2026-09-16 — third amendment: decisions :subsystem-single-contract :gate-s1-round-4 :evaluator-family-untouched; where two of them disagree the newer one is followed — :gate-s1-round-4 keeps two per-frame hosts although :subsystem-single-contract says orchestrators exist only for UniTask-based systems, and :evaluator-family-untouched keeps the evaluator family out of the contract although :gate-s1-round-4 lists DistrictOpenConditionEvaluatorTableChangedSystem among the update hosts whose parts go async"
           "2026-09-16 — fourth amendment, the third s1 gate: decisions :systems-and-subsystems-are-two-types :c13-async-parts-in-update-host :c14-unhandled-entry-throws :c16-delete-isystem-t :s1-approved — c-15 void; c-13, c-14, c-16 resolved; pipeline stages leave the sub-system contract"]
 :gates {:after-s1 "approved 2026-09-16 — decision :s1-approved" :after-s2 ?}}
```

# s1

```clojure
(def AppStateDiWiring
  {:makes     "every game state holds exactly the first-order systems flagged for it and every orchestrator of the sub-system contract holds exactly the sub-systems that name it, all of one awaited run shape, both gathered by the container from installer registrations; Boot runs no system itself and drives the states — walking Initialization to MainMenu by entry completion, then applying what each state requests — without naming a single system"
   :criterion "a state keeps a first-order system exactly when the system's AppState flags contain the state's own mode — (flags & mode) != 0 — and keeps it in the list of its most specific kind; an orchestrator keeps a sub-system or turn phase exactly when the orchestrator type the sub-system names equals the orchestrator's own type; a first-order system reaches a state only through a registration exposing it as IAppStateSystem with its flags, a sub-system reaches an orchestrator only through a registration exposing it as the one sub-system contract — a prioritized awaited run with no type argument plus the orchestrator type — and the two contracts never meet: no system implements the sub-system contract, no sub-system implements IAppStateSystem or IUniTaskSystem, neither contract derives from the other (decision :systems-and-subsystems-are-two-types); the district-open evaluator family alone stays collected by its abstract base, as today"

   :data {:system-flags        {:type AppState
                                :holds "the set of game states one first-order system belongs to — one bit per state; EventCleanupSystem carries all eight"
                                :from "the AppState flags the system's registration passes, under the one registration rule every first-order registration follows (WorldInstaller and the 12 module installers that register first-order systems)"}
          :first-order-systems {:type IReadOnlyList<IAppStateSystem>
                                :holds "every first-order system — 18 reactive, 7 per-frame, 1 cleanup, 14 pipeline stages, 27 startup-step registrations, ShowHexesUISystem — each carrying its flags, in registration order across installers; 11 of them are orchestrators already holding their kept parts (7 pipeline stages and 2 per-frame systems of the contract, DistrictOpenConditionEvaluatorBootstrapSystem and DistrictOpenConditionEvaluatorTableChangedSystem of the evaluator family)"
                                :from "the container's collection of every registration exposed as IAppStateSystem — the registration rule exposes it on every first-order registration"}
          :orchestrator-type   {:type System.Type
                                :holds "the type of the orchestrator system one sub-system or turn phase belongs to — named by the sub-system itself, never by the orchestrator or the installer"
                                :from "the sub-system's own declaration (decision :c5-subsystem-interface-with-type); whether a family's base or each concrete class declares it is s2's — the value is the same for a whole family, since after :evaluator-family-untouched no family on the contract serves two orchestrators"}
          :all-sub-systems     {:type IReadOnlyList
                                :type-args ["the one sub-system contract of decisions :c5-subsystem-interface-with-type and :subsystem-single-contract — IPrioritizedUniTaskSystem without a type argument, plus the orchestrator type; it derives from no system contract — not IUniTaskSystem, not IAppStateSystem — and carries no AppState (decision :systems-and-subsystems-are-two-types)"]
                                :holds "every sub-system and turn phase on the contract — 27 registrations over 9 families — each naming its orchestrator, carrying no AppState, in registration order across installers"
                                :from "the container's collection of every registration exposed as the sub-system contract; one container holds every installer's registrations (CONTEXT fact on the single LifetimeScope)"}
          :orchestrator-own-type {:type System.Type
                                  :holds "the concrete type of one orchestrator of the contract — the value its sub-systems must name"
                                  :from "the orchestrator itself; all 9 orchestrators of the contract are sealed and non-generic, so the runtime type and the declared type are one type"}
          :orchestrator-parts  {:type IReadOnlyList
                                :type-args ["the one sub-system contract — the kept element is run, ordered and switched through the contract itself, never through a family base (contra :c-8, resolved by the single contract)"]
                                :holds "the kept sub-systems of one orchestrator, in ascending Priority — as every orchestrator orders them today — each still switchable by its IsEnabled"
                                :from "the orchestrator's filter over :all-sub-systems by :orchestrator-type"}
          :evaluator-parts     {:type IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem>
                                :holds "DistrictSingleOpenConditionEvaluatorSubSystem and DistrictExistConditionEvaluatorSubSystem, in ascending Priority, each run synchronously — exactly as today"
                                :from "the container's collection by abstract base, registered .As<Concrete, DistrictOpenConditionEvaluatorSubSystem>() in EconomyInstaller, handed to DistrictOpenConditionEvaluatorBootstrapSystem, DistrictOpenConditionEvaluatorSystem and DistrictOpenConditionEvaluatorTableChangedSystem (decision :evaluator-family-untouched)"}
          :section-repopulate  {:type System.Action
                                :holds "the district-build host's re-run of every section part, which the list section invokes after a row click writes the selection"
                                :from "DistrictBuildUISystem hands it to each of its parts at construction, today and after (decision :c13-async-parts-in-update-host — no change); the contract does not carry it"}
          :run-cancellation    {:type CancellationToken
                                :holds "the one input of the one run shape — whether the run should stop"
                                :from "the host's own run: a pipeline-stage host passes the state's entry token it was awaited with; TurnProcessorSystem passes the token it passes its phases today; DistrictBuildUISystem has no token today — the token it passes is chosen in s2"}
          :main-hud-root       {:type GameObject
                                :holds "the shared Main UI instance under the main canvas, in whose children every MainHudSpawn part finds its window view"
                                :from "the MainHudComponent singleton's RootBox, written by MainHudSpawnSystem after it loads the Main UI and before it runs its parts — today the same object is handed to each part as its run argument"}
          :open-conditions-catalogue {:type DistrictOpenConditionsConfig
                                      :holds "the authored district-open conditions, one entry per condition, each of one concrete condition kind"
                                      :from "EntityStorages — loaded and validated (null entries throw) by the ConfigLoading state; today the host walks it and hands each entry to the parts one by one"}
          :outcomes-catalogue  {:type DistrictBuildOutcomesConfig
                                :holds "the authored district-build outcomes, one entry per outcome, each of one concrete outcome kind"
                                :from "EntityStorages — loaded and validated (null entries throw) by the ConfigLoading state; today the host walks it and hands each entry to the parts one by one"}
          :state-update-systems      {:type IReadOnlyList<IUpdatedSystem>
                                      :holds "the kept systems of one state that tick in the frame loop"
                                      :from "the state's filter over :first-order-systems"}
          :state-late-update-systems {:type IReadOnlyList<ILateUpdatedSystem>
                                      :holds "the kept systems of one state that tick in the late frame loop"
                                      :from "the state's filter over :first-order-systems"}
          :state-entry-steps         {:type IReadOnlyList<IUniTaskSystem>
                                      :holds "the kept one-shot steps of one state that are not pipeline stages — config loaders for ConfigLoading, VertexGridSpawnSystem for InstanceObjects, ShowHexesUISystem for MainMenu"
                                      :from "the state's filter over :first-order-systems; ShowHexesUISystem is a one-shot system since the generic one-shot contract is deleted (decision :delete-generic-unitask-system)"}
          :state-pipeline            {:type IReadOnlyList
                                      :type-args ["the system-side one-shot kind with a Priority — a system contract deriving from IUniTaskSystem, never the sub-system contract (decision :systems-and-subsystems-are-two-types); its name is born in s2"]
                                      :holds "the kept world-building pipeline stages of one state — one-shot systems with a Priority of their own"
                                      :from "the state's filter over :first-order-systems; a stage goes only into this list, by the most-specific-kind rule (decision :gate-s1-round-4, contra :c-7 closed)"}
          :game-states         {:type IReadOnlyList<IAppState>
                                :holds "one state per AppState value — eight: Initialization, ConfigLoading, InstanceObjects, MainMenu, MapCreation, MapLoading, Gameplay, GameOver"
                                :from "the container's collection of every registration exposed as IAppState, registered in WorldInstaller"}
          :states-by-mode      {:type IReadOnlyDictionary
                                :type-args [AppState IAppState]  ;; a comma inside the generic form is whitespace to the reader — the key and value types stand here
                                :holds "each state under its own mode"
                                :from "GameModeMachine, built from :game-states at its construction"}
          :machine             {:type GameModeMachine
                                :holds "the states by mode, the current state, and whether its entry completed — it switches only when Boot tells it to"
                                :from "the container, registered in WorldInstaller; injected into Boot"}
          :startup-order       {:type IReadOnlyList<AppState>
                                :holds "Initialization, ConfigLoading, InstanceObjects, MainMenu — the modes Boot walks before the player acts"
                                :from "Boot — decisions :first-state-and-transitions and :c1-boot-drives-startup"}
          :current-state       {:type IAppState
                                :holds "the one state that is active — entering, or ticking once its entry completed"
                                :from "the machine, set on every switch Boot orders"}
          :entry-completed     {:type bool
                                :holds "whether the current state's entry has finished — ticking is suspended until it is, and Boot's startup walk advances on it"
                                :from "the machine: cleared on every switch, set when the entry finishes uncancelled; exposed for Boot to read (decision :c1-boot-drives-startup)"}
          :requested-mode      {:type Nullable<AppState>
                                :holds "the mode the current state asks to be switched to after its tick, or nothing"
                                :from "the current state, after its tick; read and applied by Boot, never by the machine"}}

   :flow (-> ;; ── registration and composition time: installers, container build, Boot injection ──
             (:step-1 "every first-order system is registered under one registration rule: the registration always exposes the system as IAppStateSystem, next to the interfaces of its kind (interfaces only, no concrete type), and hands it its AppState flags; the system keeps its flags as its AppState — ShowHexesUISystem among them, as a one-shot system flagged MainMenu; the 14 pipeline stages are one-shot systems carrying a Priority of their own — a system-side property, never the sub-system contract"
                      {:reads #{} :writes #{:system-flags} :world :write
                       :state "the container builder holds 68 first-order registrations, every one exposed as IAppStateSystem with its flags: 25 reactive and per-frame Gameplay, EventCleanupSystem all eight states, 14 pipeline MapCreation, 26 loaders ConfigLoading, VertexGridSpawnSystem InstanceObjects, ShowHexesUISystem MainMenu"})

             (:step-2 "every sub-system and turn phase outside the evaluator family is registered exposed as the one sub-system contract, in the installer that registers it today, with no AppState and never as IAppStateSystem; each names the type of its orchestrator; each has the one run shape — awaited, the cancellation token its only input, no result — so the 21 parts whose run was synchronous (a game state, a GameObject or a config in, a bool out) now run awaited and read from the world what was handed to them; the two evaluator sub-systems keep their registration by abstract base and their synchronous run, untouched"
                      {:reads #{} :writes #{:orchestrator-type} :world :write
                       :state "the container builder holds 27 contract registrations in 7 installers, each naming one of 9 orchestrators, plus the 2 untouched evaluator registrations; no marker type groups any registration any more (MapGenerationStep and TurnPhaseStep are gone with the type argument, FirstUIStep with the generic one-shot contract)"})

             (:step-3 "one state per AppState value and GameModeMachine are registered in WorldInstaller, the states as IAppState, the machine as itself"
                      {:reads #{} :writes #{} :world :write
                       :state "the container builder holds eight IAppState registrations and one GameModeMachine registration"})

             (:step-4 (cond
                        (:flow-1 "the container is built; for every orchestrator of the contract it constructs, it hands over the one list of every sub-system; for every sub-system of that list whose orchestrator type equals the orchestrator's own type") (:conclusion-1 "the orchestrator keeps it — and back here")
                        (:flow-2 "otherwise — the sub-system names another orchestrator")                                                                                                                                              (:conclusion-2 "the orchestrator passes it by; when the list is walked, it orders what it kept by Priority; each of the three evaluator hosts receives the evaluator family by its abstract base and orders it, as today"))
                      {:reads #{:orchestrator-type :all-sub-systems :orchestrator-own-type}
                       :writes #{:all-sub-systems :orchestrator-parts :evaluator-parts :section-repopulate} :world :read
                       :state "each of the 9 orchestrators of the contract exists holding exactly its family, in ascending Priority, each sub-system constructed once and kept by the one orchestrator it names; the 3 evaluator hosts hold the evaluator family as today; the orchestrators come into existence while the container constructs the first-order systems of step-5 — DistrictOpenConditionEvaluatorSystem, a turn phase of the contract that hosts the evaluator family, while it constructs TurnProcessorSystem, and it consumes the evaluator list, not the list it belongs to"})

             (:step-5 "the container gathers every IAppStateSystem registration into one list in registration order, constructing each system with its flags"
                      {:reads #{:system-flags} :writes #{:first-order-systems} :world :read
                       :state "every first-order system exists once, with its AppState, inside one ordered list; the orchestrators among them hold their parts"})

             (:step-6 (cond
                        (:flow-1 "for every state, for every system of :first-order-systems whose flags contain the state's mode") (:conclusion-1 "put the system into the list of its most specific kind — a one-shot system with a Priority into the pipeline, any other one-shot system into the entry steps, an updated system into the updated list, a late-updated system into the late list, a system that is both updated and late-updated into both — and back here")
                        (:flow-2 "otherwise — the system's flags miss the mode")                                                     (:conclusion-2 "skip it; when the list is walked, the state's lists are complete — some of them empty"))
                      {:reads #{:first-order-systems}
                       :writes #{:state-update-systems :state-late-update-systems :state-entry-steps :state-pipeline}
                       :state "each of the eight states holds its filtered systems split by kind, no one-shot system in two lists; updated, late-updated and pipeline lists stand in ascending Priority, entry steps in registration order"})

             (:step-7 "GameModeMachine receives :game-states and files each state under its mode"
                      {:reads #{:game-states} :writes #{:states-by-mode}
                       :state "the machine can find the state of any of the eight modes; no state is current"})

             (:step-8 "Boot receives the machine through its injection"
                      {:reads #{:machine} :writes #{} :world :read
                       :state "Boot holds the machine and names no system"})

             ;; ── runtime: Unity frame loop — Boot decides every switch, the machine only carries it out ──
             (:step-9 (cond
                        (:flow-1 "Boot starts, runs no system of its own — its one-shot startup code over ConfigLoading and InstanceObjects is gone (decision :c7-boot-startup-code-deleted) — and orders the switch into the first mode of :startup-order; while the current mode is not the last of :startup-order") (:conclusion-1 "each frame Boot reads whether the current state's entry completed; when it has, Boot orders the switch into the next mode of :startup-order — and back here")
                        (:flow-2 "otherwise — the current mode is MainMenu")                                                                                                                                                                                                                         (:conclusion-2 "the startup walk is over; from now on Boot switches only on what the current state requests"))
                      {:reads #{:startup-order :states-by-mode :entry-completed} :writes #{:current-state :entry-completed} :world :read
                       :state "configs are loaded by the ConfigLoading state, the vertex grid is spawned by the InstanceObjects state, the generator UI is loaded by the MainMenu state; MainMenu is the current state"})

             (:step-10 "the current state runs what it keeps: on entry it awaits its entry steps, then its pipeline stages, one by one, each after a cancellation check; after entry, every frame it ticks its updated systems and every late frame its late-updated systems, in Priority order — EventCleanupSystem last among the updated ones. An orchestrator of the contract runs its kept parts in their Priority order, passing by a disabled one: a pipeline-stage host awaits each part with its own entry token — a Generation, HexResources or HexResourcesView part reads nothing it was not already reading (the game state it was handed was a default nobody read), a MainHudSpawn part reads the Main UI root the host wrote into the world just before, a DistrictOpenConditionSpawn or DistrictBuildOutcomeSpawn part walks the catalogue itself and spawns an entity for every entry of the kind it handles, and the host throws when an entry was left unhandled (decision :c14-unhandled-entry-throws); a per-frame host starts its parts' awaited run from its synchronous tick — TurnProcessorSystem as today, DistrictBuildUISystem on open and on the list section's re-populate, its sections reading their views from the world and ignoring the root they were handed; a UniTask started on the main thread runs synchronously until its first real await, so the sections complete inside the tick, and a part that ever needs to wait is kept by the host and its status checked on each tick (decision :c13-async-parts-in-update-host). An evaluator host runs the evaluator family synchronously, as today"
                       {:reads #{:current-state :entry-completed :state-entry-steps :state-pipeline :state-update-systems :state-late-update-systems :orchestrator-parts :evaluator-parts :run-cancellation :main-hud-root :open-conditions-catalogue :outcomes-catalogue :section-repopulate}
                        :writes #{:entry-completed :requested-mode :main-hud-root} :world :write
                        :state "the ECS world holds this frame's result of the state's systems and their parts — the terrain, resources, views, Main UI windows, condition and outcome entities after MapCreation's entry; the state has, or has not, a requested mode"})

             (:step-11 (cond
                         (:flow-1 "from MainMenu on, after the current state's tick, when :requested-mode holds a mode") (:conclusion-1 "Boot orders the machine to switch into that mode: the old state exits, the new state's entry starts, ticking is suspended until it completes")
                         (:flow-2 "otherwise")                                                                           (:conclusion-2 "the current state stays — the machine never switches on its own"))
                       {:reads #{:requested-mode :states-by-mode} :writes #{:current-state :entry-completed}
                        :state "MainMenu has handed over to MapCreation on a ripe generate request, and MapCreation to Gameplay after its settle frames — or the current state stays"})

             (:step-12 "when Boot is destroyed, the machine cancels an entry in flight and exits the current state"
                       {:reads #{:current-state} :writes #{:current-state :entry-completed} :world :read
                        :state "no state is current; no entry is running"}))

   :exits #{"a first-order system registered without an AppState parameter — the container falls back to resolving AppState, which is not registered, and the build throws (CONTEXT # Occasions, VContainer ResolveOrParameter fact); the registration rule of step-1 always passes the flags, so the occasion remains only for a registration that bypasses the rule"
            "a switch into a mode with no state filed — the machine's lookup throws KeyNotFoundException (CONTEXT fact on GameModeMachine.Switch)"
            "a state's entry is cancelled by a later switch or by disposal — the awaited system sequence stops at its next cancellation check and the machine swallows the cancellation (CONTEXT # Occasions, facts on GameModeMachine and Boot.Start)"
            "a catalogue entry of a condition or outcome kind no part handles — the host throws, as it does today (fact on the spawn hosts in :facts-beyond-context; decision :c14-unhandled-entry-throws — the mechanism is s2's, since no part returns whether it handled an entry)"}

   :numbers {:app-state-bits        "Initialization 1<<0, ConfigLoading 1<<1, InstanceObjects 1<<2, MainMenu 1<<3, MapCreation 1<<4, MapLoading 1<<5, Gameplay 1<<6, GameOver 1<<7"
             :every-state-flags     "the OR of the eight bits = 255 — EventCleanupSystem's flags"
             :membership            "(system flags & state mode) != 0"
             :orchestrator-membership "sub-system orchestrator type = orchestrator's own type — equality, not assignability; exact because all 9 orchestrators of the contract are sealed and non-generic"
             :event-cleanup-priority "int.MaxValue — SystemPriorities.RuntimeTick.EventCleanup; the greatest updated-system priority, so cleanup ticks last in every state's updated list"
             :settle-frames         "3 — MapCreation ticks this many frames after its pipeline before requesting Gameplay"
             :target-frame-rate     "60 — set by Boot at start"
             :states                "8 — one per AppState value"
             :first-order-classes   "43 — 42 of the census plus ShowHexesUISystem"
             :first-order-registrations "68 — 67 of the census plus ShowHexesUISystem"
             :by-state-after        "Initialization 1 (cleanup); ConfigLoading 26 loaders + cleanup; InstanceObjects VertexGridSpawnSystem + cleanup; MainMenu ShowHexesUISystem (entry step) + cleanup; MapCreation 14 stages + cleanup; MapLoading 1 (cleanup); Gameplay 23 updated + 2 late + cleanup; GameOver 1 (cleanup)"
             :sub-system-registrations "29 — 27 on the contract in 7 installers (TerrainGeneratorInstaller 4, HexResourcesInstaller 3, TerrainViewInstaller 3, HexResourcesViewInstaller 3, UIInstaller 8, EconomyInstaller 4, ActionsInstaller 2) + 2 evaluator sub-systems in EconomyInstaller, untouched"
             :orchestrators         "12 — 9 on the contract (7 pipeline stages: GenerationSystem, HexResourcesSystem, TerrainViewSystem, HexResourcesViewSystem, MainHudSpawnSystem, DistrictOpenConditionSpawnSystem, DistrictBuildOutcomeSpawnSystem; 2 per-frame: DistrictBuildUISystem, TurnProcessorSystem) + 3 evaluator hosts untouched (DistrictOpenConditionEvaluatorBootstrapSystem pipeline stage, DistrictOpenConditionEvaluatorSystem turn phase, DistrictOpenConditionEvaluatorTableChangedSystem reactive)"
             :families              "10 abstract bases — 9 on the contract, 1 untouched"
             :async-move            "7 families, 21 member registrations, 7 hosts go from a synchronous to the awaited run: GenerationSubSystem 4, HexResourcesSubSystem 3, HexResourcesViewSubSystem 3 (void Update(GameState)), MainHudSpawnSubSystem 4 (void Prepare(GameObject)), DistrictBuildUISubSystem 4 (void Populate(GameObject)), DistrictOpenConditionSpawnSubSystem 2 and DistrictBuildOutcomeSpawnSubSystem 1 (bool TrySpawn(config)); hosts: 6 pipeline stages + DistrictBuildUISystem; by installer: UIInstaller 8, TerrainGeneratorInstaller 4, HexResourcesInstaller 3, HexResourcesViewInstaller 3, EconomyInstaller 3. Already awaited: ViewSubSystem 3, TurnPhaseSubSystem 3. Stays synchronous: the evaluator family 2"
             :family-census
             {GenerationSubSystem                     {:members [MountainGenerationSubSystem RiverGenerationSubSystem LakeGenerationSubSystem SeaGenerationSubSystem] :orchestrators [GenerationSystem] :host-kind :pipeline-stage :run-today "void Update(GameState) via ISystem<GameState>; the host passes default(GameState), no member reads it" :run-after :contract :reads-instead :nothing :lifetime Singleton :installer TerrainGeneratorInstaller :visibility :internal :disposable true}
              HexResourcesSubSystem                   {:members [ForestResourceGenerationSubSystem ClayResourceGenerationSubSystem FishResourceGenerationSubSystem] :orchestrators [HexResourcesSystem] :host-kind :pipeline-stage :run-today "void Update(GameState) via ISystem<GameState>; default(GameState), unread" :run-after :contract :reads-instead :nothing :lifetime Singleton :installer HexResourcesInstaller :visibility :internal :disposable true}
              ViewSubSystem                           {:members [TerrainViewGenerationSubSystem TerrainViewTextureSubSystem WaterViewSubSystem] :orchestrators [TerrainViewSystem] :host-kind :pipeline-stage :run-today "UniTask Update(CancellationToken) via IUniTaskSystem<GameState>; awaited by the host" :run-after :contract :reads-instead :nothing :lifetime Singleton :installer TerrainViewInstaller :visibility :internal :disposable "declares IDisposable itself (decision :gate-s1-round-4); all 3 members override Dispose"}
              HexResourcesViewSubSystem               {:members [ForestHexResourceViewSubSystem ClayHexResourceViewSubSystem FishHexResourceViewSubSystem] :orchestrators [HexResourcesViewSystem] :host-kind :pipeline-stage :run-today "void Update(GameState) via ISystem<GameState>; default(GameState), unread" :run-after :contract :reads-instead :nothing :lifetime Singleton :installer HexResourcesViewInstaller :visibility :internal :disposable true}
              MainHudSpawnSubSystem                   {:members [HexInfoPanelSpawnSubSystem TurnPanelSpawnSubSystem ContextTabsSpawnSubSystem ResourceBarSpawnSubSystem] :orchestrators [MainHudSpawnSystem] :host-kind :pipeline-stage :run-today "void Prepare(GameObject) — the loaded Main UI instance, searched for the window view in its children" :run-after :contract :reads-instead :main-hud-root :lifetime Singleton :installer UIInstaller :visibility :internal :disposable "false today — see contra :c-11"}
              DistrictBuildUISubSystem                {:members [DistrictBuildListUISubSystem DistrictBuildHexResourcesUISubSystem DistrictBuildPriceUISubSystem DistrictBuildActionsUISubSystem] :orchestrators [DistrictBuildUISystem] :host-kind :per-frame :run-today "void Populate(GameObject) — the overlay root, read by no member; the host also hands each a Repopulate callback at construction, invoked only by the list section" :run-after :contract :reads-instead "nothing for the root; :section-repopulate stays handed over at construction" :lifetime Singleton :installer UIInstaller :visibility :public :disposable true}
              DistrictOpenConditionSpawnSubSystem     {:members [DistrictExistConditionSpawnSubSystem DistrictSingleOpenConditionSpawnSubSystem] :orchestrators [DistrictOpenConditionSpawnSystem] :host-kind :pipeline-stage :run-today "bool TrySpawn(DistrictOpenConditionConfig) — one catalogue entry in, whether its concrete kind is this part's out" :run-after :contract :reads-instead :open-conditions-catalogue :lifetime Scoped :installer EconomyInstaller :visibility :internal :disposable true}
              DistrictOpenConditionEvaluatorSubSystem {:members [DistrictSingleOpenConditionEvaluatorSubSystem DistrictExistConditionEvaluatorSubSystem] :orchestrators [DistrictOpenConditionEvaluatorBootstrapSystem DistrictOpenConditionEvaluatorSystem DistrictOpenConditionEvaluatorTableChangedSystem] :host-kind "pipeline-stage + turn-phase + reactive" :run-today "void Evaluate()" :run-after :untouched :reads-instead :untouched :lifetime Scoped :installer EconomyInstaller :visibility :public :disposable true}
              DistrictBuildOutcomeSpawnSubSystem      {:members [SpawnCityCenterOutcomeSubSystem] :orchestrators [DistrictBuildOutcomeSpawnSystem] :host-kind :pipeline-stage :run-today "bool TrySpawn(DistrictBuildOutcomeConfig) — one catalogue entry in, whether its concrete kind is this part's out" :run-after :contract :reads-instead :outcomes-catalogue :lifetime Scoped :installer EconomyInstaller :visibility :internal :disposable true}
              TurnPhaseSubSystem                      {:members [MayorAPRestoreSubSystem BuildDistrictTurnTickSystem DistrictOpenConditionEvaluatorSystem] :orchestrators [TurnProcessorSystem] :host-kind :per-frame :run-today "UniTask Update(CancellationToken) via IPrioritizedUniTaskSystem<TurnPhaseStep>; awaited by TurnPhaseRunner inside a run TurnProcessorSystem starts with Forget" :run-after :contract :reads-instead :nothing :lifetime "Singleton 2 (ActionsInstaller), Scoped 1 (EconomyInstaller)" :installer "ActionsInstaller + EconomyInstaller — the orchestrator is in TurnInstaller" :visibility :public :disposable "declares IDisposable itself (decision :gate-s1-round-4); no member overrides Dispose"}}
             :common-to-all-10      "bool IsEnabled { get; set; } and int Priority; every orchestrator checks IsEnabled at run; 11 order by Priority once at construction, TurnProcessorSystem's runner orders on every run"
             :turn-phase-host-forced "DistrictOpenConditionEvaluatorSystem is changed only as a turn phase: its base TurnPhaseSubSystem moves onto the contract (no type argument, the orchestrator type TurnProcessorSystem, IDisposable declared by the base), its registration in EconomyInstaller exposes the contract instead of the base; its own evaluator collection and synchronous evaluation stay; it needs no AppState — it is not first-order"
             :marker-types-deleted  "MapGenerationStep (Boot.Core; T of 14 stage declarations, 8 installer files, Boot.cs, MapCreationState.cs; named in 5 orchestrator comments and MapCreationState's cref), TurnPhaseStep (Turn/Data; T of TurnPhaseSubSystem only), FirstUIStep (Boot.Core; T of ShowHexesUISystem only — deleted by :delete-generic-unitask-system), InitializationAppState (Boot.Core; used nowhere — deleted by :gate-s1-round-4); ISystem<T> (Ecs.Extensions; its only three users leave it — deleted by :c16-delete-isystem-t)"
             :marker-types-kept     "GameState — the input of IUpdatedSystem and ILateUpdatedSystem ticks, data not a marker"}

   :out-of-scope #{"sub-systems returning UniTask<CommandBuffer> — a separate later task (decision :subsystem-single-contract); the run shape here returns nothing"
                   "the district-open evaluator family and how its three hosts collect and run it (decision :evaluator-family-untouched) — the owner rewrites it himself"
                   "fantasymayor-graph readings of hosts, sub-systems, pipeline stages and runs_in — fixed in the rules step (decisions :fmgraph-runs-in-later, :gate-s1-round-4)"}})
```

```clojure
{:s1-tally {:state-named 12
            :hollow-steps 0         ;; step-3 writes nothing in :data but mutates the container builder (:world :write) — its state is named
            :undeclared 0
            :orphan 0               ;; new keys: :evaluator-parts (written step-4, read step-10), :section-repopulate (written step-4, read step-10), :run-cancellation, :open-conditions-catalogue, :outcomes-catalogue (read step-10), :main-hud-root (written and read step-10)
            :untouched-input 0
            :method-names 0         ;; Boot, GameModeMachine, WorldInstaller and the systems, bases and installers of :numbers appear as existing types and actors; run members appear only in :run-today as today's signatures; the sub-system contract and the registration helper are stated as contracts, not named
            :steps 12
            :steps-changed [:step-1 :step-10]   ;; fourth amendment: stages leave the sub-system contract; the c-13 and c-14 answers
            :contra {:total 16 :resolved 16 :void #{:c-3 :c-15}}
            :families {:on-contract 9 :untouched 1 :going-async 7 :already-async 2 :staying-sync 1}
            :registrations {:first-order 68 :on-contract 27 :evaluator-untouched 2 :run-shape-changes 21 :exposure-changes 27}
            :orchestrators {:on-contract 9 :evaluator-hosts 3}}
 :facts-beyond-context
 [{:fact "IUniTaskSystem.cs holds both contracts: the generic one extends only IDisposable and has an Update(CancellationToken) run member; the non-generic one extends IDisposable and IAppStateSystem and has an Execute(CancellationToken) run member; IPrioritizedUniTaskSystem<in T> extends the generic one and adds Priority"
   :verified-by "read Assets/Scripts/EcsExtensions/IUniTaskSystem.cs and IPrioritizedUniTaskSystem.cs whole"}
  {:fact "users of the generic contract: 14 pipeline stages via IPrioritizedUniTaskSystem<MapGenerationStep>; TurnPhaseSubSystem (abstract, public) via IPrioritizedUniTaskSystem<TurnPhaseStep>, subclassed by MayorAPRestoreSubSystem, BuildDistrictTurnTickSystem, DistrictOpenConditionEvaluatorSystem; ViewSubSystem (abstract, internal) via IUniTaskSystem<GameState>, subclassed by TerrainViewGenerationSubSystem, TerrainViewTextureSubSystem, WaterViewSubSystem; ShowHexesUISystem via IUniTaskSystem<FirstUIStep>. Both abstract bases already declare their own Priority, IsEnabled, run member and virtual Dispose"
   :verified-by "grep 'IUniTaskSystem<|IPrioritizedUniTaskSystem<' and ': ViewSubSystem|: TurnPhaseSubSystem' over Assets/**/*.cs; read TurnPhaseSubSystem.cs and ViewSubSystem.cs whole"}
  {:fact "the type argument groups a DI collection in ONE place only: IReadOnlyList<IPrioritizedUniTaskSystem<MapGenerationStep>> in Boot.Construct and MapCreationState. Nobody resolves IPrioritizedUniTaskSystem<TurnPhaseStep> or IUniTaskSystem<GameState>: TerrainViewSystem collects IReadOnlyList<ViewSubSystem> and TurnProcessorSystem collects IReadOnlyList<TurnPhaseSubSystem> — by abstract base class, registered .As<Concrete, Base>(); TurnPhaseRunner orders by Priority and awaits the run member; TerrainViewSystem sorts by Priority and awaits it"
   :verified-by "grep 'TurnPhaseStep>|<GameState>>' over Assets; read TurnPhaseRunner.cs, TurnProcessorSystem.cs, TerrainViewSystem.cs whole; fmgraph.py consumers ViewSubSystem / TurnPhaseSubSystem (one ctor collection injector each)"}
  {:fact "ShowHexesUISystem's constructor is (IMainCanvasProvider, IAddressable) — no AppState parameter; it is Scoped, registered .As<ShowHexesUISystem>(); its run member loads the generator UI once and returns silently on cancellation or failed load; Show and Hide only toggle the instance's GameObject; HexesUI (the view) never hides itself — today only MainMenuState.Exit hides it"
   :verified-by "read ShowHexesUISystem.cs whole and Assets/Presentation/UI/GeneratorMenu/Views/HexesUI.cs whole"}
  {:fact "FirstUIStep is used only as ShowHexesUISystem's type argument and in its summary cref; InitializationAppState is used nowhere else; MapGenerationStep and TurnPhaseStep are empty tag structs"
   :verified-by "read Boot/Core/FirstUIStep.cs, MapGenerationStep.cs, InitializationAppState.cs, Turn/Data/TurnPhaseStep.cs; grep over Assets (InitializationAppState re-grepped at the third amendment: its own file only)"}
  {:fact "orchestrator census: 29 sub-systems and turn phases in 10 abstract families, collected by 12 orchestrators through a constructor IReadOnlyList<FamilyBase>, every registration .As<Concrete, FamilyBase>(); the families differ in run member — sync void Update(GameState) through ISystem<GameState> (3 families, 10 members), async UniTask Update(CancellationToken) (ViewSubSystem, TurnPhaseSubSystem), void Evaluate(), bool TrySpawn over two different config types, void Populate(GameObject), void Prepare(GameObject); all 10 declare IsEnabled and Priority; 9 are IDisposable, MainHudSpawnSubSystem is not; ViewSubSystem and TurnPhaseSubSystem are IDisposable only through the generic contracts this task deletes"
   :verified-by "fmgraph.py systems --role sub_system (26) and --role turn_phase (3); fmgraph.py pattern PATTERN_ORCHESTRATOR_SUBSYSTEM (29 instances, 10 contracts, 12 hosts, 0 deviations); read the 10 bases and 11 host files whole, DistrictBuildUISystem lines 1-80 and 190-200, TerrainViewSystem lines 1-90 and 190-200; grep of every installer for SubSystem registrations"}
  {:fact "orchestrator shapes: all 12 are sealed and non-generic (3 public: DistrictBuildUISystem, TurnProcessorSystem, DistrictOpenConditionEvaluatorTableChangedSystem; 9 internal); lifetimes: Singleton GenerationSystem, HexResourcesSystem, TerrainViewSystem, HexResourcesViewSystem, MainHudSpawnSystem, DistrictBuildUISystem, TurnProcessorSystem; Scoped the 5 Economy hosts; every orchestrator lives in the same assembly as its family, except TurnProcessorSystem (Turn) whose phases live in Domains.Actions and Domains.Economy — both reference Turn directly"
   :verified-by "read the host declarations; python3 Tools/asmdef_reach.py assembly-of for every host and one member per family; can Domains.Actions TurnProcessorSystem and can Domains.Economy TurnProcessorSystem — YES, direct reference"}
  {:fact "one family, three orchestrators: DistrictOpenConditionEvaluatorSubSystem is collected by DistrictOpenConditionEvaluatorBootstrapSystem (pipeline stage, turn 1), DistrictOpenConditionEvaluatorSystem (turn phase, every turn) and DistrictOpenConditionEvaluatorTableChangedSystem (reactive, on DistrictTableChangedEvent); DistrictOpenConditionEvaluatorSystem is at once a member of TurnPhaseSubSystem and an orchestrator"
   :verified-by "read the three host files whole and EconomyInstaller registrations lines 57-69"}
  {:fact "VContainer 1.18.0: shared (Singleton, Scoped) instances are created through one Lazy<object> per registration (ScopedContainer sharedInstances GetOrAdd → lazy.Value); the build-time circular-dependency check walks a constructor parameter's registration only when TypeAnalyzer's cache holds its implementation type, and a collection registration's implementation type is the array type of the service"
   :verified-by "read Runtime/Container.cs lines 60-82 and 150-185, Runtime/Registry.cs lines 40-85, Runtime/Internal/TypeAnalyzer.cs lines 368-465; NOT run — that a re-entrant Lazy<object> throws InvalidOperationException is the default .NET Lazy<T> mode, not measured in Unity"}
  {:fact "fmgraph derives a hosts edge only from a constructor collection whose element is an ABSTRACT class («an interface element is no family», di_facts.py connect_hosts); role evidence reads pipeline_member as ancestry IPrioritizedUniTaskSystem with args (MapGenerationStep), turn_phase_member as TurnPhaseSubSystem ancestry, startup_step as non-generic IUniTaskSystem ancestry, family_member through hosts edges (roles.py lines 80-100); PATTERN_ORCHESTRATOR_SUBSYSTEM lists contracts and hosts from hosts edges (recipes.py lines 162-169)"
   :verified-by "read .claude/skills/fantasymayor-graph/scripts/di_facts.py lines 96-105, roles.py lines 75-100, recipes.py lines 40-56 and 155-175 (scripts read, artifacts not)"}
  {:fact "the synchronous run arguments carry little: every Generation, HexResources and HexResourcesView member receives a GameState that its host builds as default(GameState) with the comment «deltaTime is irrelevant», and no member reads it; no DistrictBuildUI section reads the GameObject root it is handed — each reads its own view from a singleton (the list section: DistrictBuildListUIViewComponent); every MainHudSpawn member uses its GameObject only to find its window view in the children, and that object is the RootBox of the MainHudComponent singleton MainHudSpawnSystem writes on the line before it runs the members"
   :verified-by "read GenerationSystem.cs, HexResourcesSystem.cs, HexResourcesViewSystem.cs, MainHudSpawnSystem.cs, DistrictBuildUISystem.cs, the 7 sync family bases and DistrictBuildListUISubSystem.cs lines 20-115 whole; grep -w state over the 10 Update(GameState) members (only doc comments), grep -w mainUi over the 4 MainHudSpawn members, grep root over the 4 DistrictBuildUI members (signature only)"}
  {:fact "the spawn hosts today: DistrictOpenConditionSpawnSystem and DistrictBuildOutcomeSpawnSystem walk the catalogue array in index order, throw on a null entry, hand each entry to the enabled parts in Priority order until one returns true, and throw «no subsystem handles … type» when none does; each part returns false unless the entry is of its concrete config kind, else creates one entity and returns true — so entities are created in catalogue order, interleaved across kinds. DistrictOpenConditionsConfig and DistrictBuildOutcomesConfig implement IValidatableConfig, whose Validate throws on a null array or null entry, and ConfigLoaderSystem calls Validate on load"
   :verified-by "read both spawn hosts and both family bases whole; read DistrictExistConditionSpawnSubSystem.cs and SpawnCityCenterOutcomeSubSystem.cs lines 20-40; grep TrySpawn over Assets/Domains/Economy (3 overrides); read DistrictOpenConditionsConfig.cs lines 11-35; grep IValidatableConfig in ConfigLoaderSystem.cs (lines 42-43)"}
  {:fact "DistrictBuildUISystem (UpdatedSystem, per-frame, Singleton) runs its parts synchronously from two places: its tick — when the DistrictBuildUIRequestedEvent pulse is ripe and a hex is selected, it creates the selection entity, runs every enabled section in Priority order, then shows the view — and the Repopulate callback it sets on every section at construction, which only DistrictBuildListUISubSystem invokes, from the view's SelectionChanged C# event, outside the frame tick; the list section default-selects the first buildable district only while the request pulse is ripe, and EventCleanupSystem deletes the pulse at the end of that frame's tick; the host keeps instance fields (_view, _chromeHooked) and has no cancellation token"
   :verified-by "read DistrictBuildUISystem.cs whole (207 lines) and DistrictBuildListUISubSystem.cs lines 20-115; grep Repopulate over Assets/Presentation/UI/DistrictBuild"}
  {:fact "precedent of a per-frame host with awaited parts: TurnProcessorSystem (IUpdatedSystem) starts its phases' run with Forget from its tick when a NextTurnEvent pulse is ripe and the TurnProcessorComponent singleton is Idle, sets it Running, the run sets it Completed, and a later tick resets it Idle and raises TurnCompletedEvent — a new pulse is ignored while Running; the token is StatusMonitor.Token (a MonoBehaviour's)"
   :verified-by "read TurnProcessorSystem.cs whole and TurnPhaseRunner.cs whole; grep StatusMonitor over Assets"}
  {:fact "the async hosts today run their parts inside their own awaited run: GenerationSystem, HexResourcesSystem and HexResourcesViewSystem call the parts synchronously and return a completed task; MainHudSpawnSystem calls them after awaiting the Main UI load and writing MainHudComponent; TerrainViewSystem awaits each part, checking cancellation before each; DistrictOpenConditionEvaluatorSystem (a turn phase) and DistrictOpenConditionEvaluatorBootstrapSystem call the evaluators synchronously inside their awaited run; DistrictOpenConditionEvaluatorTableChangedSystem is an UpdatedSystem calling them from its tick"
   :verified-by "read the host files named whole; grep class and Update over DistrictOpenConditionEvaluatorBootstrapSystem.cs and DistrictOpenConditionEvaluatorTableChangedSystem.cs"}
  {:fact "ISystem<in T> (Ecs.Extensions) is implemented only by GenerationSubSystem, HexResourcesSubSystem and HexResourcesViewSubSystem"
   :verified-by "grep 'ISystem<' over Assets/**/*.cs, excluding IUpdatedSystem and ILateUpdatedSystem"}
  {:fact "DistrictOpenConditionEvaluatorSystem is registered Scoped .As<DistrictOpenConditionEvaluatorSystem, TurnPhaseSubSystem>() at EconomyInstaller.cs:60-61 and its constructor receives only IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem>; the two evaluator sub-systems are registered .As<Concrete, DistrictOpenConditionEvaluatorSubSystem>() at EconomyInstaller.cs:63-67 — so with the family untouched no element of the contract list consumes the contract list"
   :verified-by "read DistrictOpenConditionEvaluatorSystem.cs whole; grep SubSystem|EvaluatorSystem in EconomyInstaller.cs and ActionsInstaller.cs"}]

 :contra [{:id :c-1
           :kills "Boot-controlled switching — decision :first-state-and-transitions"
           :case "step-9: Boot must learn that ConfigLoading's entry completed; today GameModeMachine fires the entry with Forget and keeps its entering flag private, and the only transition channel is RequestedMode, which the machine itself applies after a tick"
           :fails "the decision gives Boot the switching, while the existing contract gives each state the choice of the next mode (RequestedMode) and the machine the act of switching; s1 splits it — Boot walks :startup-order by entry completion, then applies the states' requested modes — which leaves two sources of 'what is next' and a machine that must expose entry completion"
           :fix [{:id :fix-a :confidence 55 :is "as s1 states: Boot owns :startup-order and advances on entry completion; from MainMenu on Boot applies the state's RequestedMode after the tick; the machine stops self-switching and exposes whether entry completed" :cost "GameModeMachine loses its Tick-then-switch; one polled or awaited entry-completion signal is added"}
                  {:id :fix-b :confidence 30 :is "every state, startup ones included, sets RequestedMode (a startup state at the end of its entry); Boot only reads it after each tick and switches — Boot 'controls' by being the one who applies it, :startup-order disappears" :cost "each startup state names its successor, contrary to 'Boot owns the order'; one idle frame per chained startup state (entry → next tick → switch)"}
                  {:id :fix-c :confidence 15 :is "Boot owns a full transition table (mode → next mode) and every state only reports that it finished; RequestedMode is retired" :cost "MainMenu's 'Generate pressed' and MapCreation's settle end become 'finished' signals; changes IAppState for every state, beyond the minimal transitions decided"}]
           :resolved {:by :owner :at "2026-09-16" :choice :fix-a :decision :c1-boot-drives-startup
                      :folded-into "steps 9 and 11, :entry-completed, :requested-mode, :machine"}}

          {:id :c-2
           :kills ":behaviour-must-hold — MainMenu → MapCreation"
           :case "frame N+1 after Generate is pressed: MainMenu ticks its updated list [EventCleanupSystem], cleanup deletes the now-ripe TerrainGenerationGenerateEventComponent, then MainMenu looks for a ripe request and finds none"
           :fails "decision :cleanup-runs-last orders cleanup last among SYSTEMS by priority; MainMenu's own ripe-request check is not a system and has no priority, so the priority guarantees nothing about it — the menu never leaves if the check runs after the updated list"
           :fix [{:id :fix-a :confidence 60 :is "a state reads its own transition condition before it ticks its updated systems (MainMenu: check the ripe request, then tick) — written into step-10 as the order inside a tick" :cost "one ordering sentence in s1; a constraint the owner's later state shapes must keep"}
                  {:id :fix-b :confidence 25 :is "the request check becomes a first-order updated system of MainMenu with a priority below int.MaxValue that writes the requested mode for the state to read" :cost "a new system and a channel from system to state — beyond the minimal task"}
                  {:id :fix-c :confidence 15 :is "EventCleanupSystem's flags exclude MainMenu" :cost "contradicts :flags-multi-state (every game state)"}]
           :resolved {:by :owner :at "2026-09-16" :choice :none :decision :c2-mainmenu-exit-as-is
                      :means "the owner runs the game and decides; s1 keeps no order inside a tick — the risk stays visible here until the owner's run"}}

          {:id :c-3
           :kills "MainMenu showing and hiding the generator UI — decision :show-hexes-ui-main-menu"
           :case "MainMenu keeps ShowHexesUISystem through IReadOnlyList<IUniTaskSystem<FirstUIStep>> and on exit must hide it; Show and Hide are members of the concrete class only, and the registration exposes interfaces only (:interfaces-only-registrations)"
           :fails "IUniTaskSystem<FirstUIStep> carries Update(CancellationToken) and Dispose — no Show, no Hide; the state can load the UI on entry but can neither show it after entry nor hide it on exit without casting to the concrete class"
           :fix [{:id :fix-a :confidence 45 :is "a first-UI kind interface in Boot.Core or Ecs.Extensions that carries show and hide next to the load, implemented by ShowHexesUISystem" :cost "one new interface; the fifth kind in step-6 gets its real type"}
                  {:id :fix-b :confidence 35 :is "ShowHexesUISystem becomes a startup step (IUniTaskSystem, AppState.MainMenu) whose run loads and shows; hiding moves to the UI itself or stays a known gap" :cost "loses Hide on exit — the generator UI stays visible over MapCreation and Gameplay unless something else hides it"}
                  {:id :fix-c :confidence 20 :is "MainMenu casts its kept first-UI step to ShowHexesUISystem" :cost "a runtime cast on a concrete type — the coupling the interfaces-only decision removes"}]
           :resolved {:by :owner :at "2026-09-16" :choice "void — the one run method is enough" :decisions #{:c3-update-is-enough :delete-generic-unitask-system}
                      :folded-into "step-1 and :state-entry-steps: ShowHexesUISystem is a one-shot system flagged MainMenu, run on MainMenu's entry; the first-UI kind is gone"
                      :residual "fact: HexesUI never hides itself and today only MainMenu's exit hides it — with no hide on exit the generator UI may stay over MapCreation and Gameplay; recorded for the owner's Unity check, not reopened"}}

          {:id :c-4
           :kills "fail-loud wiring — a system silently in no state"
           :case "a new reactive system registered .As<IUpdatedSystem>().WithParameter(AppState.Gameplay) but without .As<IAppStateSystem>(); or a class implementing IAppStateSystem and none of the five kinds"
           :fails "VContainer fills IReadOnlyList<IAppStateSystem> only from registrations exposed as that exact service type, so the first system is absent from every state; the second passes the flags filter and lands in no kind list — both never run, and nothing throws (CLAUDE.md bans a silent skip)"
           :fix [{:id :fix-a :confidence 50 :is "a state throws when a kept system implements none of the kinds it distributes; the missing-exposure case stays covered by review and the fmgraph rules step" :cost "catches the second case only"}
                  {:id :fix-b :confidence 35 :is "every kind interface extends IAppStateSystem, so a kind system cannot compile without flags; plus the kind check of fix-a" :cost "touches IUpdatedSystem, ILateUpdatedSystem, IUniTaskSystem<T> — subsystems and turn phases implementing them would be forced to carry AppState, contrary to :scope-first-order"}
                  {:id :fix-c :confidence 15 :is "one registration extension that always exposes IAppStateSystem and takes the flags" :cost "a new helper every installer uses; the WithParameter precedent changes shape"}]
           :resolved {:by :owner :at "2026-09-16" :choice :fix-c :decision :c4-registration-helper
                      :folded-into "step-1 as the one registration rule; the first exit narrows to a registration that bypasses it"
                      :residual "the second case — a kept system of no kind — is not caught by the rule; with four kinds and step-6's most-specific placement it can still land nowhere silently (nothing in the census does)"}}

          {:id :c-5
           :kills ":scope-first-order — sub-systems and turn phases carrying AppState once the generic one-shot contract is replaced by the non-generic one"
           :case "decision :delete-generic-unitask-system moves IPrioritizedUniTaskSystem<T>, ViewSubSystem and ShowHexesUISystem onto the non-generic IUniTaskSystem, which extends IAppStateSystem; TurnPhaseSubSystem derives from IPrioritizedUniTaskSystem<TurnPhaseStep>, so the three turn phases and the three terrain-view sub-systems must each expose an AppState and implement the non-generic run member (Execute) in place of their Update"
           :fails "an orchestrator's part now claims to belong to game states: it needs an AppState value (the enum has no None, so a constant 0 or a fake flag), an AppState constructor parameter on either base would throw at container build because their registrations pass none, and nothing in the type stops a later registration from exposing a sub-system as IAppStateSystem and dropping it into a state's list — exactly what :scope-first-order forbids"
           :fix [{:id :fix-a :confidence 30 :is "literal replacement: IPrioritizedUniTaskSystem<T> and ViewSubSystem derive from IUniTaskSystem; the two abstract bases return a constant AppState (no constructor parameter) and rename their run member" :cost "six sub-systems carry a meaningless AppState; the no-state-leak guarantee rests on registrations alone"}
                  {:id :fix-b :confidence 45 :is "the two abstract bases stop implementing any shared one-shot contract; only IPrioritizedUniTaskSystem (first-order pipeline stages) and ShowHexesUISystem move onto IUniTaskSystem" :cost "sub-systems are touched to REMOVE the generic interface rather than to replace it; TurnPhaseSubSystem and ViewSubSystem lose the IDisposable they inherited and must declare it"}
                  {:id :fix-c :confidence 25 :is "split the non-generic contract: an AppState-free one-shot contract carries the run member and Dispose, and IUniTaskSystem extends it with IAppStateSystem; sub-systems take the AppState-free one, first-order one-shots take IUniTaskSystem" :cost "a new interface — the owner said 'replaced by IUniTaskSystem', not by a new contract"}]
           :resolved {:by :owner :at "2026-09-16" :choice "a variant of fix-c: sub-systems and turn phases get their own contract, separate from IUniTaskSystem and IAppStateSystem, carrying the type of their orchestrator; narrowed by :subsystem-single-contract to one kind — IPrioritizedUniTaskSystem without T plus the orchestrator type — and by :evaluator-family-untouched to every family but the evaluators"
                      :decisions #{:c5-subsystem-interface-with-type :subsystem-single-contract :evaluator-family-untouched}
                      :folded-into "criterion; :orchestrator-type, :all-sub-systems, :orchestrator-own-type, :orchestrator-parts, :evaluator-parts; steps 2, 4, 10; :numbers :sub-system-registrations :orchestrators :families :async-move :family-census :orchestrator-membership"
                      :closed "the contract derives from no system contract — decision :systems-and-subsystems-are-two-types"}}

          {:id :c-6
           :kills "the type argument of IPrioritizedUniTaskSystem<T> — does it survive"
           :case "after the change a state filters its pipeline from :first-order-systems by kind, and Boot no longer resolves IReadOnlyList<IPrioritizedUniTaskSystem<MapGenerationStep>>; the only other closed form, <TurnPhaseStep>, is resolved by nobody"
           :fails "the type argument no longer groups any DI collection: MapGenerationStep duplicates the AppState.MapCreation flag, TurnPhaseStep groups nothing"
           :fix [{:id :fix-a :confidence 55 :is "keep IPrioritizedUniTaskSystem<T> with T" :cost "a redundant tag next to the flags"}
                  {:id :fix-b :confidence 45 :is "drop T — IPrioritizedUniTaskSystem becomes non-generic; MapGenerationStep and TurnPhaseStep lose their last use" :cost "14 stage declarations and their registrations change their exposed type; two empty tag structs become dead"}]
           :resolved {:by :owner :at "2026-09-16" :choice :fix-b :decision :c6-drop-t
                      :folded-into "step-2 state; :numbers :marker-types-deleted and :marker-types-kept"
                      :residual "IPrioritizedUniTaskSystem without T is the sub-system contract alone; pipeline stages leave it for a system-side kind with a Priority (decision :systems-and-subsystems-are-two-types, :state-pipeline)"}}

          {:id :c-7
           :kills "a pipeline stage running twice in MapCreation"
           :case "a pipeline stage implements both IUniTaskSystem (through its prioritized system kind) and that kind; a rule 'into the list of every kind it implements' would place it in both :state-entry-steps and :state-pipeline, and MapCreation's entry awaits both lists"
           :fails "every map-generation stage would run twice per entry — double terrain view, double spawns"
           :fix [{:id :fix-a :confidence 60 :is "as step-6 states: a one-shot system goes to the list of its most specific kind — prioritized into the pipeline, otherwise into the entry steps" :cost "one placement rule the state's filter must keep; the order of type tests matters"}
                  {:id :fix-b :confidence 25 :is "one list of one-shot systems per state: those with a Priority sorted by it, the rest in registration order before them" :cost "merges two kinds into one list; mixes two orderings in one list"}
                  {:id :fix-c :confidence 15 :is "keep 'every kind' and let each state decide which one-shot list it runs" :cost "every state must know about the overlap"}]
           :resolved {:by :owner :at "2026-09-16" :choice :fix-a :decisions #{:c7-boot-startup-code-deleted :gate-s1-round-4}
                      :closed "the owner confirmed the most-specific-kind rule at round 4: «так»"}}

          {:id :c-8
           :kills "the orchestrator running what it kept — :c5-subsystem-interface-with-type"
           :case "GenerationSystem receives the list of all sub-systems typed as the sub-system contract and keeps 4; to run them it needed void Update(GameState); the spawn hosts needed bool TrySpawn over a config; DistrictBuildUISystem needs Populate and must set Repopulate on each; TerrainViewSystem needs an awaited run"
           :fails "while the families had six different run members, a kept element typed as the contract could not be run, ordered or handed a callback without reaching its family base"
           :fix [{:id :fix-a :confidence 55 :is "the contract carries the orchestrator type plus IsEnabled and Priority; the orchestrator converts each kept element to its family base, throwing on a mismatch" :cost "one checked conversion per kept element per orchestrator"}
                  {:id :fix-b :confidence 30 :is "each family base implements the contract; every orchestrator keeps receiving IReadOnlyList<FamilyBase> and filters it by the orchestrator type" :cost "the orchestrator does not receive the list of ALL sub-systems as the owner said"}
                  {:id :fix-c :confidence 15 :is "a contract with a type argument per family run shape" :cost "reintroduces a grouping type argument right after :c6-drop-t"}]
           :resolved {:by :owner :at "2026-09-16" :choice "none of the three — the single contract: every kept part shares one awaited run shape, Priority and IsEnabled, so the orchestrator runs, orders and switches it through the contract alone"
                      :decisions #{:subsystem-single-contract :gate-s1-round-4}
                      :folded-into ":orchestrator-parts type-args; step-2; step-10; :numbers :async-move"
                      :not-yet-true "DistrictBuildUISubSystem: the Repopulate callback the host hands each section at construction is family-only data the contract does not carry — the kept part cannot be handed it through the contract (kept as is by decision :c13-async-parts-in-update-host; how the host reaches it is s2's); every other family on the contract needs nothing beyond the run shape once its run reads from the world (:main-hud-root, the two catalogues)"}}

          {:id :c-9
           :kills "turn-1 and same-turn district-open evaluation — one family serving three orchestrators"
           :case "DistrictSingleOpenConditionEvaluatorSubSystem and DistrictExistConditionEvaluatorSubSystem are run by DistrictOpenConditionEvaluatorBootstrapSystem, DistrictOpenConditionEvaluatorSystem and DistrictOpenConditionEvaluatorTableChangedSystem"
           :fails "one orchestrator type names one orchestrator: whichever of the three it names, the other two keep zero sub-systems and run nothing"
           :fix [{:id :fix-a :confidence 40 :is "a sub-system names the set of orchestrator types it belongs to" :cost "the Type property becomes a collection"}
                  {:id :fix-b :confidence 25 :is "the orchestrator type names a shared host type; the filter uses assignability" :cost "equality becomes assignability for every orchestrator"}
                  {:id :fix-c :confidence 25 :is "the evaluator family stays outside the contract, collected by its abstract base by all three hosts as today" :cost "two collection mechanisms side by side"}
                  {:id :fix-d :confidence 10 :is "one of the three hosts owns the family" :cost "changes three domain systems"}]
           :resolved {:by :owner :at "2026-09-16" :choice "owner rewrites later, family untouched — in effect fix-c" :decision :evaluator-family-untouched
                      :folded-into "criterion; :evaluator-parts; steps 2, 4, 10; :numbers :orchestrators :sub-system-registrations"}}

          {:id :c-10
           :kills "the game's start — a nested orchestrator consuming the list it belongs to"
           :case "DistrictOpenConditionEvaluatorSystem is a turn phase, so an element of :all-sub-systems, and was to receive :all-sub-systems as an orchestrator — a re-entrant shared-instance creation"
           :fails "the first resolve re-enters one Lazy<object> and throws (read, not run) — the game does not start"
           :fix [{:id :fix-a :confidence 50 :is "turn phases get a contract of their own" :cost "two contracts and two collections"}
                  {:id :fix-b :confidence 30 :is "orchestrators receive the sub-system list after the container is built" :cost "all orchestrators move composition out of construction"}
                  {:id :fix-c :confidence 20 :is "DistrictOpenConditionEvaluatorSystem keeps its evaluator family by abstract base" :cost "the cycle rule is kept by a case, not by a structure"}]
           :resolved {:by :owner :at "2026-09-16" :choice "owner rewrites later, family untouched — in effect fix-c" :decision :evaluator-family-untouched
                      :folded-into "step-4 state; fact on DistrictOpenConditionEvaluatorSystem's constructor and registration"
                      :residual "the cycle is avoided by the untouched family, not by a structure: a future part of the contract that itself receives the contract list would re-enter — none does after this task"}}

          {:id :c-11
           :kills "release of the terrain view — disposal of sub-systems whose IDisposable came from the deleted generic contracts"
           :case "ViewSubSystem is IDisposable only through IUniTaskSystem<GameState> and all three of its members override Dispose; TurnPhaseSubSystem is IDisposable only through IPrioritizedUniTaskSystem<TurnPhaseStep>"
           :fails "VContainer tracks a shared instance for disposal only when it is IDisposable — the three terrain-view sub-systems would silently stop being disposed"
           :fix [{:id :fix-a :confidence 70 :is "ViewSubSystem and TurnPhaseSubSystem declare IDisposable themselves" :cost "two base declarations"}
                  {:id :fix-b :confidence 30 :is "the sub-system contract extends IDisposable" :cost "MainHudSpawnSubSystem and its 4 members must gain an empty Dispose"}]
           :resolved {:by :owner :at "2026-09-16" :choice :fix-a :decision :gate-s1-round-4
                      :folded-into ":numbers :family-census rows ViewSubSystem and TurnPhaseSubSystem"
                      :closed "the contract derives from no one-shot contract (decision :systems-and-subsystems-are-two-types), so each family declares IDisposable itself where it has it; MainHudSpawnSubSystem stays non-disposable"}}

          {:id :c-12
           :kills "the fantasymayor-graph readings of orchestrators, sub-systems and pipeline stages"
           :case "fmgraph draws a hosts edge only from a constructor collection of an ABSTRACT class, marks pipeline_stage by IPrioritizedUniTaskSystem with the argument MapGenerationStep, and startup_step by non-generic IUniTaskSystem ancestry"
           :fails "after the change the 9 orchestrators of the contract receive a collection of an interface — no hosts edge, so PATTERN_ORCHESTRATOR_SUBSYSTEM loses 9 of its hosts and 27 of its instances; the 14 stages lose pipeline_stage evidence"
           :fix [{:id :fix-a :confidence 70 :is "extend :fmgraph-runs-in-later to these readings — repaired in the rules step" :cost "graph answers about orchestrators and stages are knowingly wrong between the code stage and the rules step"}
                  {:id :fix-b :confidence 30 :is "lift the off-limits on the graph scripts inside the code stage" :cost "contradicts :rules-after-code"}]
           :resolved {:by :owner :at "2026-09-16" :choice :fix-a :decision :gate-s1-round-4
                      :residual "the evaluator family keeps its abstract-base collection, so its hosts edges survive — the graph will show 1 family where the code has 10"}}

          {:id :c-13
           :kills "the district-build overlay — a synchronous per-frame host running awaited parts (decision :gate-s1-round-4 :update-hosts)"
           :case "DistrictBuildUISystem's tick sees a ripe DistrictBuildUIRequestedEvent: it creates the selection entity, must run its 4 sections in Priority order, then show the view; later a row click in the list section fires the view's SelectionChanged C# event outside any tick and re-runs all 4 sections through the Repopulate callback"
           :fails "an UpdatedSystem's tick returns nothing and cannot await: the sections' runs can only be started and let go. If a section's run yields past the frame, (1) the view is shown before its sections are filled, (2) the list section's default selection is read on a later frame, after EventCleanupSystem has deleted the ripe request, so no default is written and every section reads the birth value Unknown as the selection (what each section then does is not traced), (3) a click during an unfinished run starts a second concurrent populate over the same views, (4) sections run out of Priority order across frames, (5) there is no token — a run in flight when Gameplay exits keeps writing views. The contract also cannot carry the Repopulate callback (contra :c-8 :not-yet-true). Today no section awaits anything, so every run would complete synchronously — the failure is latent, not present"
           :fix [{:id :fix-a :confidence 40 :is "the TurnProcessorSystem precedent: the host starts ONE sequential awaited run of its kept sections per open or re-populate, keeps an in-flight status in a singleton (idle, running, completed), shows the view when the run completes, ignores or marks-for-once-more an open or re-populate that arrives while running, passes a token; the default selection is taken from the ripe request on the tick itself (written before the run starts) instead of by the list section inside its run" :cost "a status singleton and a pending re-populate mark; the list section's default-selection read moves to the host's open; the overlay shows at least as late as the run completes"}
                  {:id :fix-b :confidence 35 :is "the host starts each section's run in Priority order and requires it to have completed synchronously — throws when a returned run is still pending; order, ripeness and the click path stay as today" :cost "an invariant the contract's type does not show: an awaited run that must never truly await; a section that ever loads something fails loud at the first open"}
                  {:id :fix-c :confidence 25 :is "re-populate becomes data: the list section raises a one-frame request event after writing the selection, and the host's next tick runs the sections — the callback leaves the family and every run starts from the tick; combined with fix-a or fix-b for how the tick runs them" :cost "a new event type and one frame of delay between a click and the refreshed sections; the family then needs nothing beyond the contract (closes :c-8 :not-yet-true)"}]
           :resolved {:by :owner :at "2026-09-16" :choice "none of the three — no change" :decision :c13-async-parts-in-update-host
                      :means "a main-thread UniTask runs synchronously until its first real await, and no section awaits today, so every run completes inside the tick in Priority order; if a part ever needs to wait, the host keeps the started UniTask and checks its status on each tick; the Repopulate callback stays handed over at construction"
                      :folded-into "step-10; :section-repopulate; :run-cancellation"}}

          {:id :c-14
           :kills "fail-loud spawning of district-open conditions and build outcomes — the bool of the synchronous run"
           :case "DistrictOpenConditionsConfig holds an entry of a new condition kind that no spawn part handles; or DistrictBuildOutcomesConfig an outcome kind nobody handles"
           :fails "today the host hands the entry to each part, gets false from all, and throws naming the type; with the one run shape a part receives no entry and returns nothing, so an unhandled kind spawns no entity and nothing throws (CLAUDE.md bans a silent skip). Also: entities are created grouped by part priority instead of in catalogue order — whatever reads entity or index order (the list section default-selects the first buildable district) may see a different first entity; not measured"
           :fix [{:id :fix-a :confidence 40 :is "each part walks the catalogue and spawns the entries of its own kind; after all parts ran, the host counts the entities its parts spawned against the catalogue length and throws on a shortfall (null entries are already rejected by the config's own validation at load)" :cost "the host must know each part's archetype or a common kind column to count; the message loses the name of the unhandled type unless the host re-scans the catalogue"}
                  {:id :fix-b :confidence 30 :is "each part marks which catalogue indices it handled into a run record the host reads through the world; the host throws on an unmarked index, naming its type" :cost "a transient record in the world with one release place; the marks are a data channel standing in for the old return value"}
                  {:id :fix-c :confidence 20 :is "the host keeps walking the catalogue, writes the current entry into the world, and awaits all parts per entry; a part that spawns marks the entry handled" :cost "all parts run once per entry; the parameter is imitated through the world"}
                  {:id :fix-d :confidence 10 :is "drop the unhandled-kind check" :cost "a silent skip — contrary to CLAUDE.md § 3"}]
           :resolved {:by :owner :at "2026-09-16" :choice "throw — the mechanism is chosen in s2" :decision :c14-unhandled-entry-throws
                      :folded-into "step-10; :exits"
                      :residual "entity creation order grouped by part instead of catalogue order stays unmeasured"}}

          {:id :c-15
           :kills "the separation of the sub-system contract from IAppStateSystem — :c5-subsystem-interface-with-type against :subsystem-single-contract"
           :case "the single contract is IPrioritizedUniTaskSystem plus the orchestrator type; decision :delete-generic-unitask-system moved IPrioritizedUniTaskSystem onto the non-generic IUniTaskSystem, which extends IAppStateSystem; the 14 pipeline stages need AppState, the 27 parts must not carry it"
           :fails "if the prioritized contract keeps deriving from the non-generic one-shot contract as it is today, every sub-system and turn phase inherits an AppState it must implement (the enum has no None) and can be exposed into a state's list — the exact leak :c-5 was resolved against"
           :fix [{:id :fix-a :confidence 60 :is "the non-generic one-shot contract stops extending IAppStateSystem: it carries the awaited run and IDisposable; the prioritized contract extends it with Priority; the sub-system contract extends the prioritized one with the orchestrator type; every first-order class declares IAppStateSystem itself — as the 25 updated and late-updated systems already must for step-1 — including ConfigLoaderSystem, VertexGridSpawnSystem, ShowHexesUISystem and the 14 stages" :cost "IAppStateSystem declared on 17 more classes; a one-shot system without it cannot pass the registration rule, which is the check :c-4 wanted"}
                  {:id :fix-b :confidence 25 :is "the prioritized contract stands alone — Priority, the awaited run, IDisposable — not deriving from the one-shot contract; a pipeline stage implements both the one-shot contract (with AppState) and the prioritized one; a sub-system only the prioritized one plus the orchestrator type" :cost "two contracts declare the same run member; a stage satisfies both with one implementation; the most-specific-kind rule does the work it does today"}
                  {:id :fix-c :confidence 15 :is "sub-systems implement a constant AppState and the registration rule alone keeps them out of the states" :cost "c-5 fix-a, already rejected by the owner"}]
           :resolved {:by :owner :at "2026-09-16" :choice "void — every option mixed systems and sub-systems" :decision :systems-and-subsystems-are-two-types
                      :means "the sub-system contract IPrioritizedUniTaskSystem derives from no system contract and carries no AppState; pipeline stages are systems and do not implement it — their Priority is a system-side property"
                      :folded-into "criterion; :all-sub-systems; :state-pipeline; step-1"}}

          {:id :c-16
           :kills "nothing at runtime — a contract left without a user"
           :case "GenerationSubSystem, HexResourcesSubSystem and HexResourcesViewSubSystem, the only implementers of ISystem<in T>, move to the contract"
           :fails "ISystem<T> becomes dead code in Ecs.Extensions; the owner's :c6-drop-t answer deletes classes left without use, but ISystem<T> was never a type argument, so the decision does not name it"
           :fix [{:id :fix-a :confidence 65 :is "delete ISystem<T> with the move, in the spirit of «прибирай разом з непотрібними класами»" :cost "one file in Ecs.Extensions; any doc naming it goes stale until the rules step"}
                  {:id :fix-b :confidence 35 :is "keep it — not asked" :cost "a dead interface in the kernel"}]
           :resolved {:by :owner :at "2026-09-16" :choice :fix-a :decision :c16-delete-isystem-t
                      :folded-into ":numbers :marker-types-deleted"}}]
 :gate-after-s1 {:subject "the algorithm: is this how we compute, are these the structures, are these the conditions"
                 :open-contra #{}
                 :owner-verdict "approved 2026-09-16 with the four answers folded"}}
```

# s2

```clojure
(def AppStateDiWiring-placement  ;; every new file, type and asmdef reference — python3 Tools/asmdef_reach.py, 2026-09-16
  {:law "a system and a sub-system share no interface: system side IAppStateSystem, IUpdatedSystem, ILateUpdatedSystem, IUniTaskSystem, IPipelineStageSystem; sub-system side IPrioritizedUniTaskSystem — no derivation between the two sides (decision :systems-and-subsystems-are-two-types)"
   :new-types [{:type ^:new IPipelineStageSystem :file "Assets/Scripts/EcsExtensions/IPipelineStageSystem.cs" :assembly Ecs.Extensions
                :is "the system kind of a map-creation stage — IUniTaskSystem plus int Priority (contra :c-17)"}
               {:type ^:new AppStateSystemRegistration :file "Assets/Scripts/EcsExtensions/AppStateSystemRegistration.cs" :assembly Ecs.Extensions
                :is "static class, the one registration rule of every first-order system (decision :c4-registration-helper)"}
               {:type ^:new OrchestratorSubSystems :file "Assets/Scripts/EcsExtensions/OrchestratorSubSystems.cs" :assembly Ecs.Extensions
                :is "static class, stateless: an orchestrator's selection of its parts and their awaited run — replaces TurnPhaseRunner and seven hand-written loops"}
               {:type ^:new AppStateSystems :file "Assets/Modules/Boot/Implementation/States/AppStateSystems.cs" :assembly Boot.Implementation
                :is "sealed class, one state's kept systems split by kind, with the filter and the three runs every state repeats"}
               {:type ^:new InitializationState :file "Assets/Modules/Boot/Implementation/States/InitializationState.cs" :assembly Boot.Implementation}
               {:type ^:new ConfigLoadingState :file "Assets/Modules/Boot/Implementation/States/ConfigLoadingState.cs" :assembly Boot.Implementation}
               {:type ^:new InstanceObjectsState :file "Assets/Modules/Boot/Implementation/States/InstanceObjectsState.cs" :assembly Boot.Implementation}
               {:type ^:new GameOverState :file "Assets/Modules/Boot/Implementation/States/GameOverState.cs" :assembly Boot.Implementation}]
   :new-references [{:from Ecs.Extensions :to VContainer :why "AppStateSystemRegistration extends IContainerBuilder" :decided false :contra :c-18}
                    {:from Turn :to Boot.Core :why "TurnProcessorSystem and TurnCountSystem name AppState" :decided :turn-asmdef-reference}]
   :reach [{:asks "can every installer assembly (Installers.World Presentation.UI Domains.Map Presentation Domains.Actors Domains.Economy Domains.Actions Turn) see a type of Ecs.Extensions" :answer "YES, direct reference — probed with ConfigLoaderSystem"}
           {:asks "can Installers.World see the state classes and GameModeMachine" :answer "YES — Boot.Implementation is a direct reference"}
           {:asks "can Boot.Implementation see EntityStorages, TerrainGenerationGenerateEventComponent, HexIconsVisibilityComponent" :answer "YES, direct references"}
           {:asks "can Presentation.UI see StatusMonitor" :answer "YES — Core, direct"}
           {:asks "can Turn see AppState" :answer "NO today — becomes YES with the decided Boot.Core reference"}
           {:asks "does any installer assembly already reference VContainer so the helper's IContainerBuilder is visible at the call site" :answer "yes — every installer compiles against IContainerBuilder today"}]
   :meta-files "new .cs files get their .meta from Unity on import; a deleted .cs takes its .meta with it — no .meta is written by hand (CLAUDE.md § 3)"
   :unchanged #{"IAppStateSystem" "AppState" "IAppState — members unchanged, its summary is reworded" "ConfigLoaderSystem<T> and VertexGridSpawnSystem — they already carry AppState" "the evaluator family and its two hosts beyond the renames of the table"}})
```

```clojure
(def AppStateSystemRegistration-methods
  {RegisterAppStateSystem
   {:does "registers one first-order system so that it can reach a state: exposed as IAppStateSystem, handed its AppState flags"
    :in #{:system-flags}
    :out :system-registration
    :how "builder.Register<TSystem>(lifetime).As<IAppStateSystem>().WithParameter(appState) — an extension method on IContainerBuilder, TSystem constrained to IAppStateSystem, the RegistrationBuilder returned so a config loader chains WithParameter(\"address\", …)"
    :note "no kind interface and no concrete type is exposed — nothing resolves either once Boot stops composing states (contra :c-19); the constraint makes a system without AppState a compile error, the helper makes a registration without IAppStateSystem impossible"}})

(def AppStateSystemRegistration-data
  {:system-flags        {:from-s1 :system-flags :as appState :lives :external
                         :note "passed by each installer line; every system keeps it as a get-only AppState property set in its constructor"}
   :system-registration {:shape ^:new RegistrationBuilder :as registration :lives :external
                         :holds "VContainer's builder of the one registration — returned for chaining, owned by the container builder"}})
```

```clojure
(def AppStateSystems-methods
  {Filter
   {:does "keeps the first-order systems whose flags contain the state's mode and files each into the array of its most specific kind"
    :in #{:state-mode :first-order-systems}
    :out :state-systems
    :scratch #{:updated-buffer :late-updated-buffer :entry-step-buffer :pipeline-stage-buffer}
    :flow (-> (:step-1 (cond
                         (:flow-1 "for every system whose flags contain the mode")  (:conclusion-1 "an IPipelineStageSystem goes to the pipeline-stage buffer, any other IUniTaskSystem to the entry-step buffer; independently an IUpdatedSystem goes to the updated buffer and an ILateUpdatedSystem to the late-updated buffer, so a system of both kinds lands in both; a system that landed in no buffer throws — and back here")
                         (:flow-2 "otherwise — the flags miss the mode")            (:conclusion-2 "pass it by; when the list is walked, go on")))
              (:step-2 "sort the updated, late-updated and pipeline-stage buffers ascending by Priority, keep the entry steps in registration order, and build the record from the four buffers as arrays"))
    :exits #{"a kept system implementing no kind a state runs — throws InvalidOperationException naming the system type and the mode (s1 contra :c-4 residual: the registration rule does not catch it)"}
    :numbers {:membership "(system.AppState & mode) != 0"}
    :note "a static factory called once by each state's constructor; List<T> buffers and OrderBy are allowed — composition is one-shot and a state is not a system; four buffers stay in one method because the traversal is one homogeneous pass"}

   RunEntryAsync
   {:does "awaits every entry step in registration order, then every pipeline stage in Priority order"
    :in #{:state-entry-steps :state-pipeline :entry-token}
    :out :none
    :flow (-> (:step-1 "for each entry step: cancellationToken.ThrowIfCancellationRequested(), then await Execute(cancellationToken)")
              (:step-2 "for each pipeline stage: cancellationToken.ThrowIfCancellationRequested(), then await Execute(cancellationToken)"))
    :exits #{"cancellation of the entry — throws OperationCanceledException, which the machine swallows (s1 :exits)"}
    :note "two loops of the same shape stay inline — two uses, under the bar of three"}

   Tick
   {:does "ticks every kept updated system in Priority order"
    :in #{:state-update-systems :game-state}
    :out :none
    :how "an index loop over the UpdatedSystems array calling Update(gameState) — no enumerator per frame"}

   LateTick
   {:does "ticks every kept late-updated system in Priority order"
    :in #{:state-late-update-systems :game-state}
    :out :none
    :how "an index loop over the LateUpdatedSystems array calling Update(gameState)"}})

(def AppStateSystems-data
  {:state-systems             {:shape ^:new AppStateSystems :as _systems :lives :run
                               :fields {UpdatedSystems :state-update-systems LateUpdatedSystems :state-late-update-systems EntrySteps :state-entry-steps PipelineStages :state-pipeline}
                               :holds "one state's kept systems split by kind — a sealed class with four readonly arrays, built by Filter"
                               :note "a readonly field of every state class, written once by its constructor and read by EnterAsync, Tick and LateTick"}
   :first-order-systems       {:from-s1 :first-order-systems :as allSystems :lives :external
                               :note "each state constructor's IReadOnlyList<IAppStateSystem> parameter, handed to Filter and not kept"}
   :state-mode                {:shape ^:new AppState :as mode :lives Filter :holds "the one mode of the state that filters — its Mode"}
   :state-update-systems      {:from-s1 :state-update-systems :as UpdatedSystems :lives :run :note "IUpdatedSystem[] in code"}
   :state-late-update-systems {:from-s1 :state-late-update-systems :as LateUpdatedSystems :lives :run :note "ILateUpdatedSystem[] in code"}
   :state-entry-steps         {:from-s1 :state-entry-steps :as EntrySteps :lives :run :note "IUniTaskSystem[] in code"}
   :state-pipeline            {:from-s1 :state-pipeline :as PipelineStages :lives :run :note "IPipelineStageSystem[] in code — the system kind of contra :c-17"}
   :updated-buffer            {:shape ^:new List<IUpdatedSystem> :as updatedSystems :lives Filter :holds "the kept updated systems, unsorted"}
   :late-updated-buffer       {:shape ^:new List<ILateUpdatedSystem> :as lateUpdatedSystems :lives Filter :holds "the kept late-updated systems, unsorted"}
   :entry-step-buffer         {:shape ^:new List<IUniTaskSystem> :as entrySteps :lives Filter :holds "the kept one-shot systems that are not pipeline stages, in registration order"}
   :pipeline-stage-buffer     {:shape ^:new List<IPipelineStageSystem> :as pipelineStages :lives Filter :holds "the kept pipeline stages, unsorted"}})
```

```clojure
(def InitializationState-methods  ;; the shape of ConfigLoadingState, InstanceObjectsState, MapLoadingState and GameOverState too — only Mode differs
  {InitializationState
   {:does "keeps the systems flagged Initialization"
    :in #{:first-order-systems}
    :out :none
    :writes #{:state-systems}
    :flow (-> (:step-1 "file the kept systems by kind" {:calls AppStateSystems.Filter :out :state-systems}))}

   EnterAsync
   {:does "runs the kept entry steps and pipeline stages"
    :in #{:state-systems :entry-token}
    :out :none
    :how "return _systems.RunEntryAsync(cancellationToken)"}

   Tick
   {:does "ticks the kept updated systems"
    :in #{:state-systems :game-state}
    :out :none
    :how "_systems.Tick(gameState)"}

   LateTick
   {:does "ticks the kept late-updated systems"
    :in #{:state-systems :game-state}
    :out :none
    :how "_systems.LateTick(gameState)"}})
;; Mode => AppState.Initialization, RequestedMode => null, Exit empty; the constructor takes IReadOnlyList<IAppStateSystem> allSystems — its keys are declared with their owners
```

```clojure
(def MainMenuState-methods
  {MainMenuState
   {:does "resolves the generate-request archetype and keeps the systems flagged MainMenu"
    :in #{:storages :first-order-systems}
    :out :none
    :writes #{:generate-requests :state-systems}
    :flow (-> (:step-1 "file the kept systems by kind" {:calls AppStateSystems.Filter :out :state-systems}))
    :note "_generateRequests = EventArchetypes.Of<TerrainGenerationGenerateEventComponent>(storages.World) is a query-cache lifetime line, as today; the constructor takes EntityStorages instead of a bare EntityStore — the container registers EntityStorages, not the World"}

   EnterAsync
   {:does "clears the request, then runs the kept entry steps — ShowHexesUISystem loads the generator UI"
    :in #{:state-systems :entry-token}
    :out :none
    :writes #{:requested-mode}
    :flow (-> (:step-1 "await the entry" {:calls AppStateSystems.RunEntryAsync :out :none}))
    :note "_requestedMode = null opens the entry as a lifetime line; Show after the load and Hide on Exit are gone with the first-UI kind (decision :c3-update-is-enough, s1 contra :c-3 residual); Exit becomes empty"}

   Tick
   {:does "requests MapCreation on a ripe generate request, then ticks the kept updated systems"
    :in #{:generate-requests :state-systems :game-state}
    :out :none
    :writes #{:requested-mode}
    :flow (-> (:step-1 "request MapCreation when a ripe request exists" {:calls RequestMapCreationOnRipeRequest :out :none})
              (:step-2 "tick the kept updated systems" {:calls AppStateSystems.Tick :out :none}))
    :note "the check keeps its place before anything else in the tick, where today's Tick has it; EventCleanupSystem now ticks in MainMenu and deletes the ripe request inside step-2 (s1 contra :c-2, decision :c2-mainmenu-exit-as-is — the owner's run decides)"}

   RequestMapCreationOnRipeRequest
   {:does "sets the request to MapCreation when a ripe generate request exists"
    :in #{:generate-requests}
    :out :none
    :writes #{:requested-mode}
    :how "if HasRipeRequest() then _requestedMode = AppState.MapCreation — HasRipeRequest unchanged"
    :calls #{HasRipeRequest}}

   LateTick
   {:does "ticks the kept late-updated systems"
    :in #{:state-systems :game-state}
    :out :none
    :how "_systems.LateTick(gameState)"}})

(def MainMenuState-data
  {:storages          {:shape ^:new EntityStorages :as storages :lives :external
                       :holds "the registry of the game world and its singletons — the container's instance"
                       :note "a constructor parameter of MainMenuState and GameplayState, kept as _storages by GameplayState and by the systems that already keep it"}
   :generate-requests {:shape ^:new Archetype :as _generateRequests :lives :run
                       :holds "the event archetype of TerrainGenerationGenerateEventComponent"
                       :note "a query cache resolved once in the constructor, as today"}
   :requested-mode    {:from-s1 :requested-mode :as _requestedMode :lives :run
                       :grows "cleared by EnterAsync, set by the tick that decides a transition"
                       :note "MainMenuState and MapCreationState keep it as _requestedMode behind RequestedMode => _requestedMode (MapCreationState's auto-property with a private setter becomes this field); the other six states return null; GameModeMachine forwards it, Boot reads it"}})
```

```clojure
(def MapCreationState-methods
  {MapCreationState
   {:does "keeps the systems flagged MapCreation"
    :in #{:first-order-systems}
    :out :none
    :writes #{:state-systems}
    :flow (-> (:step-1 "file the kept systems by kind" {:calls AppStateSystems.Filter :out :state-systems}))
    :note "the params IUpdatedSystem[] constructor and both OrderBy calls are gone — Filter sorts"}

   EnterAsync
   {:does "clears the request and the settle count, then awaits the pipeline stages"
    :in #{:state-systems :entry-token}
    :out :none
    :writes #{:requested-mode :settled-frames}
    :flow (-> (:step-1 "await the entry" {:calls AppStateSystems.RunEntryAsync :out :none}))
    :note "_requestedMode = null and _settledFrames = 0 open the entry as lifetime lines"}

   Tick
   {:does "ticks the kept updated systems, then counts one settle frame"
    :in #{:state-systems :game-state}
    :out :none
    :writes #{:settled-frames :requested-mode}
    :flow (-> (:step-1 "tick the kept updated systems" {:calls AppStateSystems.Tick :out :none})
              (:step-2 "count a settle frame, request Gameplay at the last" {:calls CountSettleFrame :out :none}))}

   CountSettleFrame
   {:does "counts one settle frame and requests Gameplay once the count reaches SettleFrames"
    :in #{:settled-frames}
    :out :none
    :writes #{:settled-frames :requested-mode}
    :how "_settledFrames++ then, when _settledFrames >= SettleFrames, _requestedMode = AppState.Gameplay"
    :numbers {:settle-frames "3"}}

   LateTick
   {:does "ticks the kept late-updated systems"
    :in #{:state-systems :game-state}
    :out :none
    :how "_systems.LateTick(gameState)"}})

(def MapCreationState-data
  {:settled-frames {:shape ^:new int :as _settledFrames :lives :run
                    :grows "0 on entry, +1 per tick"}})
```

```clojure
(def GameplayState-methods
  {GameplayState
   {:does "keeps the systems flagged Gameplay"
    :in #{:storages :first-order-systems}
    :out :none
    :writes #{:state-systems}
    :flow (-> (:step-1 "file the kept systems by kind" {:calls AppStateSystems.Filter :out :state-systems}))
    :note "_storages = storages is a lifetime line; the two list parameters and both OrderBy calls are gone"}

   EnterAsync
   {:does "runs the kept entry steps and pipeline stages, then seeds the start of play"
    :in #{:state-systems :entry-token :storages}
    :out :none
    :flow (-> (:step-1 "await the entry" {:calls AppStateSystems.RunEntryAsync :out :none})
              (:step-2 "seed the start of play" {:calls SeedStartOfPlay :out :none}))
    :note "async now; both lists are empty today, so the entry completes at once as before"}

   SeedStartOfPlay
   {:does "turns hex icons on, raises their changed event and sets turn 1 — today's EnterAsync body, comments kept"
    :in #{:storages}
    :out :none
    :how "Singletons.Set(new HexIconsVisibilityComponent(true)), World.CreateEvent(new HexIconsVisibilityChangedEvent()), Singletons.Set(new TurnCountComponent(1))"}

   Tick
   {:does "ticks the kept updated systems"
    :in #{:state-systems :game-state}
    :out :none
    :how "_systems.Tick(gameState)"}

   LateTick
   {:does "ticks the kept late-updated systems"
    :in #{:state-systems :game-state}
    :out :none
    :how "_systems.LateTick(gameState)"}})
```

```clojure
(def GameModeMachine-methods
  {GameModeMachine
   {:does "files every state under its mode"
    :in #{:game-states}
    :out :none
    :writes #{:states-by-mode}
    :how "a Dictionary<AppState, IAppState> sized to the list, then Add(state.Mode, state) for every state — Add throws on a second state with the same mode"
    :note "the parameter changes from IReadOnlyDictionary<AppState, IAppState> to IReadOnlyList<IAppState>: VContainer injects a collection, not a dictionary (CONTEXT fact on GameModeMachine)"}

   Switch
   {:does "leaves the current state and starts the entry of the state filed under the mode"
    :in #{:states-by-mode}
    :out :none
    :writes #{:current-state :entry-completed :entry-cancellation}
    :flow (-> (:step-1 "cancel and release the previous entry's cancellation source")
              (:step-2 "Exit the current state, if any")
              (:step-3 "_current = _statesByMode[mode]")
              (:step-4 "mark an entry running, create a new cancellation source, EnterAsync(_current, token).Forget()"))
    :calls #{EnterAsync}
    :exits #{"a mode with no state filed — the lookup throws KeyNotFoundException (s1 :exits)"}
    :note "unchanged in body; only Boot calls it now (decision :c1-boot-drives-startup)"}

   EnterAsync
   {:does "awaits one state's entry and marks it completed unless it was cancelled"
    :in #{:current-state :entry-token}
    :out :none
    :writes #{:entry-completed}
    :how "unchanged — await state.EnterAsync(cancellationToken) inside a try that swallows OperationCanceledException, then clear the running mark only when the token was not cancelled"
    :exits #{"the entry is cancelled by a later switch or by disposal — swallowed (s1 :exits)"}}

   IsEntryCompleted
   {:does "tells whether a state is current and its entry has finished"
    :in #{:current-state :entry-completed}
    :out :entry-completed
    :how "_current != null && !_entering"}

   CurrentMode
   {:does "names the mode of the current state"
    :in #{:current-state}
    :out :current-mode
    :how "_current.Mode"}

   RequestedMode
   {:does "forwards the mode the current state requests"
    :in #{:current-state}
    :out :requested-mode
    :how "_current.RequestedMode"}

   Tick
   {:does "ticks the current state"
    :in #{:current-state :game-state}
    :out :none
    :how "_current.Tick(gameState) — no guard and no switch of its own, Boot guards and switches"}

   LateTick
   {:does "late-ticks the current state"
    :in #{:current-state :game-state}
    :out :none
    :how "_current.LateTick(gameState) — Boot guards"}

   Stop
   {:does "cancels an entry in flight and exits the current state"
    :in #{:current-state}
    :out :none
    :writes #{:current-state :entry-cancellation}
    :how "today's Dispose body, renamed; GameModeMachine no longer implements IDisposable, so the container does not track it — Boot.OnDestroy is the one release place (owner at the s2 gate: contra :c-22 fix-b)"
    :note "amended at the s2 gate 2026-09-16, before any code — not :from-code"}})

(def GameModeMachine-data
  {:game-states        {:from-s1 :game-states :as states :lives GameModeMachine :note "the constructor parameter IReadOnlyList<IAppState>, from the container"}
   :states-by-mode     {:from-s1 :states-by-mode :as _statesByMode :lives :run :note "readonly, filled once by the constructor"}
   :current-state      {:from-s1 :current-state :as _current :lives :run :grows "set by Switch, cleared by Dispose"}
   :entry-completed    {:from-s1 :entry-completed :as _entering :lives :run
                        :note "the field holds the inverse — an entry is running; set by Switch, cleared by EnterAsync; IsEntryCompleted exposes the s1 meaning"}
   :entry-cancellation {:shape ^:new CancellationTokenSource :as _enterCts :lives :run
                        :holds "the cancellation source of the entry in flight"
                        :grows "replaced by Switch, cancelled and released by Switch and by Dispose"}
   :entry-token        {:shape ^:new CancellationToken :as cancellationToken :lives EnterAsync
                        :holds "the token of one entry, passed down to the state, its AppStateSystems.RunEntryAsync and every system it awaits"}
   :game-state         {:shape ^:new GameState :as gameState :lives Tick
                        :holds "this frame's delta time — built by Boot as the tick argument, passed down through the machine, the state and its systems"}})
```

```clojure
(def Boot-methods
  {Construct
   {:does "receives the machine from the container"
    :in #{:machine}
    :out :none
    :writes #{:machine}
    :how "the [Inject] method, now with one parameter GameModeMachine machine — the scene's LifetimeScope injects Boot at the end of its build, before Start (CONTEXT fact on Boot's injection)"
    :note "the 29 parameters, every new …State(…) and the hand-filled dictionary are gone"}

   Start
   {:does "fixes the frame rate and switches into the first startup mode"
    :in #{:machine :startup-order}
    :out :none
    :flow (-> (:step-1 "switch into StartupOrder[0]" {:calls GameModeMachine.Switch :out :none}))
    :numbers {:target-frame-rate "60 — Application.targetFrameRate, a lifetime line before the switch"}
    :note "void, no longer async: ExecuteStartupStep and the startup code over ConfigLoading and InstanceObjects are deleted (decision :c7-boot-startup-code-deleted), with _startupSystems and _startupCompleted"}

   Update
   {:does "ticks the current state once its entry completed, then advances the mode"
    :in #{:machine}
    :out :none
    :flow (-> (:step-1 "tick the current state with new GameState(Time.deltaTime)" {:calls GameModeMachine.Tick :out :none})
              (:step-2 "switch into the next mode, if any" {:calls AdvanceMode :out :none}))
    :exits #{"the machine's IsEntryCompleted is false — no state yet, or its entry still runs; ticking is suspended while a state enters (CONTEXT fact on GameModeMachine)"}}

   AdvanceMode
   {:does "switches into the next startup mode while the walk lasts, otherwise into the mode the current state requests"
    :in #{:machine :startup-order}
    :out :none
    :scratch #{:next-mode}
    :flow (-> (:step-1 "nextMode = NextStartupMode(_machine.CurrentMode) ?? _machine.RequestedMode")
              (:step-2 (cond
                         (:flow-1 "nextMode has a value")  (:conclusion-1 "_machine.Switch(nextMode.Value)")
                         (:flow-2 "otherwise")             (:conclusion-2 "the current state stays"))))
    :calls #{NextStartupMode GameModeMachine.Switch}}

   NextStartupMode
   {:does "names the startup mode after the given one, or nothing once the given mode is the last of the walk or outside it"
    :in #{:startup-order :current-mode}
    :out :next-mode
    :how "Array.IndexOf(StartupOrder, currentMode) — an index from 0 to Length - 2 gives StartupOrder[index + 1], any other index gives null"}

   LateUpdate
   {:does "late-ticks the current state once its entry completed"
    :in #{:machine}
    :out :none
    :flow (-> (:step-1 "late-tick the current state with new GameState(Time.deltaTime)" {:calls GameModeMachine.LateTick :out :none}))
    :exits #{"the machine's IsEntryCompleted is false — the same occasion as Update"}}

   OnDestroy
   {:does "stops the machine: cancels an entry in flight and exits the current state"
    :in #{:machine}
    :out :none
    :flow (-> (:step-1 "stop the machine" {:calls GameModeMachine.Stop :out :none}))
    :note "kept, not deleted — contra :c-22 fix-b chosen by the owner at the s2 gate"}})

(def Boot-data
  {:machine       {:from-s1 :machine :as _machine :lives :external
                   :note "created by the container (Singleton, not IDisposable); released by Boot.OnDestroy calling _machine.Stop() — the one release place (contra :c-22 fix-b, owner 2026-09-16)"}
   :startup-order {:from-s1 :startup-order :as StartupOrder :lives :run
                   :holds "static readonly AppState[] — Initialization, ConfigLoading, InstanceObjects, MainMenu"
                   :note "a constant of the class, written by no method"}
   :current-mode  {:shape ^:new AppState :as currentMode :lives NextStartupMode
                   :holds "the mode of the current state, read from GameModeMachine.CurrentMode"}
   :next-mode     {:shape ^:new Nullable<AppState> :as nextMode :lives AdvanceMode
                   :holds "the mode to switch into after this frame's tick, or nothing"}})
```

```clojure
(def OrchestratorSubSystems-methods
  {SelectForOrchestrator
   {:does "keeps the sub-systems that name the given orchestrator, ascending by Priority"
    :in #{:orchestrator-own-type :all-sub-systems :orchestrator-type}
    :out :orchestrator-parts
    :how "allSubSystems.Where(part => part.OrchestratorType == orchestratorType).OrderBy(part => part.Priority).ToArray()"
    :exits #{"no sub-system names the orchestrator — throws InvalidOperationException naming it: every orchestrator of the contract has parts (s1 :family-census) and OrchestratorType is typed by hand in each family base, so an empty selection is a mistyped type and lost work"}
    :numbers {:orchestrator-membership "part.OrchestratorType == orchestratorType — equality, all 9 orchestrators are sealed and non-generic"}
    :note "called once, from an orchestrator's constructor; LINQ is allowed at composition"}

   RunAsync
   {:does "awaits every enabled part, in the order given"
    :in #{:orchestrator-parts :run-cancellation}
    :out :none
    :flow (-> (:step-1 (cond
                         (:flow-1 "for each part, after cancellationToken.ThrowIfCancellationRequested()")  (:conclusion-1 "a part with IsEnabled false is passed by with continue, an enabled part is awaited with await part.Update(cancellationToken) — and back here")
                         (:flow-2 "otherwise — the parts are walked")                                         (:conclusion-2 "return"))))
    :exits #{"cancellation — throws OperationCanceledException (s1 :exits); the pipeline host's entry swallows it in the machine, TurnProcessorSystem's token is StatusMonitor's"}
    :note "the continue skips a part switched off through IsEnabled, the switch every family base carries (s1 :common-to-all-10); an index loop over IReadOnlyList — no enumerator per run on TurnProcessorSystem's repeated path"}})

(def OrchestratorSubSystems-data
  {:orchestrator-type     {:from-s1 :orchestrator-type :as OrchestratorType :lives :external
                           :note "a get-only property of IPrioritizedUniTaskSystem, returned by each family base as typeof(<its orchestrator>)"}
   :all-sub-systems       {:from-s1 :all-sub-systems :as allSubSystems :lives :external
                           :note "an orchestrator constructor's IReadOnlyList<IPrioritizedUniTaskSystem> parameter, handed to SelectForOrchestrator and not kept"}
   :orchestrator-own-type {:from-s1 :orchestrator-own-type :as orchestratorType :lives SelectForOrchestrator
                           :note "passed as typeof(<the orchestrator class>) — never GetType()"}
   :orchestrator-parts    {:from-s1 :orchestrator-parts :as _subSystems :lives :run
                           :note "a readonly [StateAllowed] field of each orchestrator (ARCHITECTURE :state/injected-collection), written once by its constructor; TurnProcessorSystem keeps its name _phases"}
   :run-cancellation      {:from-s1 :run-cancellation :as cancellationToken :lives RunAsync
                           :note "a pipeline-stage host passes its Execute token; TurnProcessorSystem and DistrictBuildUISystem pass StatusMonitor.Token (ARCHITECTURE :async/token)"}})
```

```clojure
(def DistrictBuildUISystem-methods
  {DistrictBuildUISystem
   {:does "composes the overlay host: keeps its sections and hands each the re-populate callback"
    :in #{:system-flags :storages :all-sub-systems}
    :out :none
    :writes #{:orchestrator-parts :section-repopulate}
    :flow (-> (:step-1 "keep the sections that name this host" {:calls OrchestratorSubSystems.SelectForOrchestrator :out :orchestrator-parts})
              (:step-2 "hand every section the re-populate callback" {:calls HandRepopulateToSections :out :none}))
    :note "base(appState, storages.World, PresentationUIArchetypes.DistrictBuildUI(storages.World)) and the three archetype caches are lifetime lines; Update, HookChrome, Open and the confirm path are unchanged"}

   HandRepopulateToSections
   {:does "sets PopulateSections as the Repopulate of every kept section"
    :in #{:orchestrator-parts}
    :out :none
    :writes #{:section-repopulate}
    :how "for each part, part is DistrictBuildUISubSystem section gives section.Repopulate = PopulateSections, any other part throws InvalidOperationException naming its type"
    :exits #{"a kept part that is not a DistrictBuildUISubSystem — only that family names this host, so another type is a mistyped OrchestratorType (contra :c-20)"}}

   PopulateSections
   {:does "runs every enabled section in Priority order and requires the run to have finished inside the call"
    :in #{:orchestrator-parts}
    :out :none
    :scratch #{:section-run}
    :flow (-> (:step-1 "run = OrchestratorSubSystems.RunAsync(_subSystems, StatusMonitor.Token)")
              (:step-2 (cond
                         (:flow-1 "run.Status is Pending — a section awaited past the call")  (:conclusion-1 "throw InvalidOperationException: the host must then keep the run and poll its status each tick (decision :c13-async-parts-in-update-host)")
                         (:flow-2 "otherwise")                                                   (:conclusion-2 "run.GetAwaiter().GetResult() — rethrows a section's exception where today's synchronous call threw it"))))
    :calls #{OrchestratorSubSystems.RunAsync}
    :note "no section awaits today, so a main-thread run completes synchronously (s1 step-10); the overlay root is no longer read — no section read it; the Status check comes first because GetResult on a pending UniTask throws «Not yet completed» and returns its source to the pool (read in Library/PackageCache/com.cysharp.unitask@360e370345b9/Runtime/UniTaskCompletionSource.cs, GetResult)"}})

(def DistrictBuildUISystem-data
  {:section-repopulate {:from-s1 :section-repopulate :as Repopulate :lives :run
                        :note "the settable Action property of DistrictBuildUISubSystem, written once per section by this host's constructor, invoked only by DistrictBuildListUISubSystem"}
   :section-run        {:shape ^:new UniTask :as run :lives PopulateSections
                        :holds "the started run of all sections, finished or not"}})
```

```clojure
(def DistrictOpenConditionSpawnSystem-methods
  {DistrictOpenConditionSpawnSystem
   {:does "keeps the condition spawn parts"
    :in #{:system-flags :storages :all-sub-systems}
    :out :none
    :writes #{:orchestrator-parts :condition-rows}
    :flow (-> (:step-1 "keep the parts that name this host" {:calls OrchestratorSubSystems.SelectForOrchestrator :out :orchestrator-parts}))
    :note "AppState = appState, _storages = storages and _conditionRows = storages.World.Query().AllTags(Tags.Get<DistrictOpenConditionTag>()) are lifetime lines"}

   Execute
   {:does "has every part spawn the conditions of its kind, then throws when a catalogue entry got no row"
    :in #{:orchestrator-parts :run-cancellation}
    :out :none
    :flow (-> (:step-1 "run the parts" {:calls OrchestratorSubSystems.RunAsync :out :none})
              (:step-2 "throw on an unhandled condition" {:calls EnsureEveryConditionSpawned :out :none}))
    :note "async now; today's silent return on a cancelled token and the private TrySpawn router are gone — RunAsync throws on cancellation (ARCHITECTURE :fail/cancel)"}

   EnsureEveryConditionSpawned
   {:does "compares the condition rows with the catalogue entries and throws on a mismatch"
    :in #{:condition-rows :open-conditions-catalogue}
    :out :none
    :how "_conditionRows.Count != conditions.Length throws InvalidOperationException with both counts and the distinct concrete type names of the catalogue's entries"
    :exits #{"rows and entries differ — a condition kind no part handles (decision :c14-unhandled-entry-throws); a null entry already threw at load in the config's own Validate (s1 fact on the spawn hosts)"}
    :numbers {:every-condition-spawned "condition rows == catalogue entries"}}})

(def DistrictOpenConditionSpawnSystem-data
  {:open-conditions-catalogue {:from-s1 :open-conditions-catalogue :as conditions :lives EnsureEveryConditionSpawned
                               :note "_storages.Get<DistrictOpenConditionsConfig>().Conditions — read by the host for the check and by each part for its walk"}
   :condition-rows            {:shape ^:new ArchetypeQuery :as _conditionRows :lives :run
                               :holds "every entity carrying the DistrictOpenConditionTag label — both condition archetypes"
                               :note "a query cache resolved once in the constructor (ARCHITECTURE :system/query-caches-in-constructor)"}})

(def DistrictBuildOutcomeSpawnSystem-methods  ;; the same shape as DistrictOpenConditionSpawnSystem
  {Execute
   {:does "has every part spawn the outcomes of its kind, then throws when a catalogue entry got no row"
    :in #{:orchestrator-parts :run-cancellation}
    :out :none
    :flow (-> (:step-1 "run the parts" {:calls OrchestratorSubSystems.RunAsync :out :none})
              (:step-2 "throw on an unhandled outcome" {:calls EnsureEveryOutcomeSpawned :out :none}))}

   EnsureEveryOutcomeSpawned
   {:does "compares the outcome rows with the catalogue entries and throws on a mismatch"
    :in #{:outcome-rows :outcomes-catalogue}
    :out :none
    :how "_outcomeRows.Count != outcomes.Length throws InvalidOperationException with both counts and the distinct concrete type names of the catalogue's entries"
    :exits #{"rows and entries differ — an outcome kind no part handles (decision :c14-unhandled-entry-throws)"}
    :numbers {:every-outcome-spawned "outcome rows == catalogue entries"}}})

(def DistrictBuildOutcomeSpawnSystem-data
  {:outcomes-catalogue {:from-s1 :outcomes-catalogue :as outcomes :lives EnsureEveryOutcomeSpawned
                        :note "_storages.Get<DistrictBuildOutcomesConfig>().Outcomes"}
   :outcome-rows       {:shape ^:new Archetype :as _outcomeRows :lives :run
                        :holds "EconomyArchetypes.BuildOutcome(storages.World) — the one outcome archetype"
                        :note "resolved once in the constructor, beside the SelectForOrchestrator step"}})
```

```clojure
(def DistrictExistConditionSpawnSubSystem-methods  ;; the shape of DistrictSingleOpenConditionSpawnSubSystem and SpawnCityCenterOutcomeSubSystem too
  {Update
   {:does "creates one Exist condition row for every catalogue entry of the Exist kind"
    :in #{:sub-system-storages :open-conditions-catalogue}
    :out :none
    :flow (-> (:step-1 (cond
                         (:flow-1 "for each entry of Storages.Get<DistrictOpenConditionsConfig>().Conditions that is a DistrictExistConditionConfig")  (:conclusion-1 "create the row exactly as today's TrySpawn body does — and back here")
                         (:flow-2 "otherwise — an entry of another kind")                                                                                 (:conclusion-2 "continue — another part spawns it"))))
    :ends-with #{"return UniTask.CompletedTask"}
    :note "the continue skips an entry of another part's kind; the Single part matches DistrictSingleOpenConditionConfig, SpawnCityCenterOutcomeSubSystem walks Storages.Get<DistrictBuildOutcomesConfig>().Outcomes for SpawnCityCenterOutcomeConfig; rows are now created grouped by part, not in catalogue order (contra :c-23)"}})

(def MainHudSpawnSubSystem-methods  ;; the base, plus the run of its 4 members
  {ReadMainHudRoot
   {:does "gives the shared Main UI instance the host wrote into the world"
    :in #{:sub-system-storages}
    :out :main-hud-root
    :how "Storages.Singletons.Get<MainHudComponent>().RootBox — its Value when it exists, otherwise InvalidOperationException"
    :exits #{"the root box does not exist — a member run before MainHudSpawnSystem wrote it; UIInstaller seeds an empty MainHudComponent at build (s1 fact on the MainHudSpawn members)"}}

   Update
   {:does "a member's run: reads the root, then resolves its window view as today's Prepare did"
    :in #{:sub-system-storages}
    :out :none
    :flow (-> (:step-1 "read the root" {:calls ReadMainHudRoot :out :main-hud-root})
              (:step-2 "today's Prepare body over mainUi"))
    :ends-with #{"return UniTask.CompletedTask"}
    :note "written in HexInfoPanelSpawnSubSystem, TurnPanelSpawnSubSystem, ContextTabsSpawnSubSystem and ResourceBarSpawnSubSystem"}})

(def MainHudSpawnSubSystem-data
  {:sub-system-storages {:shape ^:new EntityStorages :as Storages :lives :external
                         :holds "the container's EntityStorages kept by a family base as a protected readonly field"
                         :note "new on MainHudSpawnSubSystem, DistrictOpenConditionSpawnSubSystem and DistrictBuildOutcomeSpawnSubSystem, whose World field it replaces"}
   :main-hud-root       {:from-s1 :main-hud-root :as mainUi :lives Update
                         :note "the Main UI root GameObject, returned by ReadMainHudRoot"}})

(def DistrictOpenConditionEvaluatorBootstrapSystem-methods
  {Execute
   {:does "runs the evaluator family synchronously, as today"
    :in #{:evaluator-parts}
    :out :none
    :how "today's Update body under the name Execute — the family is not on the contract (decision :evaluator-family-untouched)"}})

(def DistrictOpenConditionEvaluatorBootstrapSystem-data
  {:evaluator-parts {:from-s1 :evaluator-parts :as _subSystems :lives :run
                     :note "unchanged — collected by abstract base, also kept by DistrictOpenConditionEvaluatorSystem and DistrictOpenConditionEvaluatorTableChangedSystem"}})
```

```clojure
(def AppStateDiWiring-edits  ;; the mechanical edits — one row per identical change; a row names its files, symbols and the exact change
  [;; ── contracts and kernel ───────────────────────────────────────────────
   {:kind :contract :file "Assets/Scripts/EcsExtensions/IPrioritizedUniTaskSystem.cs" :symbol IPrioritizedUniTaskSystem
    :change "the generic IPrioritizedUniTaskSystem<in T> : IUniTaskSystem<T> becomes the non-generic sub-system contract deriving from nothing: Type OrchestratorType { get; }, int Priority { get; }, bool IsEnabled { get; }, UniTask Update(CancellationToken cancellationToken); summary rewritten for sub-systems"}
   {:kind :contract :file "Assets/Scripts/EcsExtensions/IUniTaskSystem.cs" :symbol IUniTaskSystem
    :change "the generic IUniTaskSystem<in T> is deleted from the file; the non-generic IUniTaskSystem : IDisposable, IAppStateSystem with Execute stays"}
   {:kind :contract :file "Assets/Scripts/EcsExtensions/IUpdatedSystem.cs" :symbol IUpdatedSystem :change "declares : IAppStateSystem"}
   {:kind :contract :file "Assets/Scripts/EcsExtensions/ILateUpdatedSystem.cs" :symbol ILateUpdatedSystem :change "declares : IAppStateSystem"}
   {:kind :contract :files ["Assets/Scripts/EcsExtensions/UpdatedSystem.cs" "Assets/Scripts/EcsExtensions/LateUpdatedSystem.cs"] :symbols [UpdatedSystem LateUpdatedSystem]
    :change "both protected constructors gain a leading AppState appState and set public AppState AppState { get; }"}
   {:kind :asmdef :file "Assets/Scripts/EcsExtensions/Ecs.Extensions.asmdef" :change "references gain \"VContainer\" (contra :c-18)"}
   {:kind :asmdef :file "Assets/Modules/Turn/Turn.asmdef" :change "references gain \"Boot.Core\" (decision :turn-asmdef-reference)"}
   {:kind :new-file :files ["Assets/Scripts/EcsExtensions/IPipelineStageSystem.cs" "Assets/Scripts/EcsExtensions/AppStateSystemRegistration.cs" "Assets/Scripts/EcsExtensions/OrchestratorSubSystems.cs" "Assets/Modules/Boot/Implementation/States/AppStateSystems.cs"]
    :change "the defs above and AppStateDiWiring-placement; IPipelineStageSystem is public interface IPipelineStageSystem : IUniTaskSystem { int Priority { get; } }"}
   {:kind :new-file :files ["Assets/Modules/Boot/Implementation/States/InitializationState.cs" "Assets/Modules/Boot/Implementation/States/ConfigLoadingState.cs" "Assets/Modules/Boot/Implementation/States/InstanceObjectsState.cs" "Assets/Modules/Boot/Implementation/States/GameOverState.cs"]
    :change "public sealed class <Mode>State : IAppState in the shape of InitializationState-methods"}
   {:kind :rewritten :files ["Assets/Modules/Boot/Implementation/Boot.cs" "Assets/Modules/Boot/Implementation/GameModeMachine.cs" "Assets/Modules/Boot/Implementation/States/MainMenuState.cs" "Assets/Modules/Boot/Implementation/States/MapCreationState.cs" "Assets/Modules/Boot/Implementation/States/GameplayState.cs" "Assets/Modules/Boot/Implementation/States/MapLoadingState.cs"]
    :change "as their defs; MapLoadingState takes the InitializationState shape with Mode MapLoading; class summaries rewritten to the new wiring"}
   ;; ── deleted types ──────────────────────────────────────────────────────
   {:kind :deleted :files ["Assets/Scripts/EcsExtensions/ISystem.cs" "Assets/Modules/Boot/Core/FirstUIStep.cs" "Assets/Modules/Boot/Core/InitializationAppState.cs" "Assets/Modules/Boot/Core/MapGenerationStep.cs" "Assets/Modules/Turn/Data/TurnPhaseStep.cs" "Assets/Modules/Turn/Helpers/TurnPhaseRunner.cs"]
    :symbols [ISystem<T> FirstUIStep InitializationAppState MapGenerationStep TurnPhaseStep TurnPhaseRunner]
    :change "file deleted with its .meta; TurnPhaseRunner's run is OrchestratorSubSystems.RunAsync now"}
   ;; ── first-order systems: AppState on the system side ───────────────────
   {:kind :appstate-updated-subclass
    :symbols [HexSelectionSystem HexSelectionViewSystem ResourceBarSystem DistrictBuildUISystem TurnPanelViewSystem HexInfoPanelSystem HexInfoPanelHeaderSystem ContextTabSelectionSystem ContextTabsAvailabilitySystem HexInfoPanelResourcesSystem ForestSpawnSystem BuildDistrictActionSystem DistrictBuildProgressViewSpawnSystem BuildDistrictActionCancelSystem DistrictBuildProgressViewDespawnSystem DistrictViewSpawnSystem ForestDespawnSystem DistrictOpenConditionEvaluatorTableChangedSystem HexIconsVisibilitySystem TurnCountSystem HexInfoPanelDistrictSystem CameraMovementSystem]
    :files "the census :file of each (CONTEXT # census :per_frame and :reactive)"
    :change "the constructor gains a leading AppState appState and passes it first to base(appState, …)"}
   {:kind :appstate-direct-kind :symbols [EventCleanupSystem TurnProcessorSystem BuildDistrictCompletionSystem HexIconsContainerPositionSystem]
    :files ["Assets/Scripts/EcsExtensions/EventCleanupSystem.cs" "Assets/Modules/Turn/Systems/TurnProcessorSystem.cs" "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictCompletionSystem.cs" "Assets/Presentation/HexIcons/Systems/HexIconsContainerPositionSystem.cs"]
    :change "the constructor gains a leading AppState appState and sets public AppState AppState { get; }"}
   {:kind :pipeline-stage
    :symbols [GenerationSystem HexResourcesSystem TerrainViewSystem HexResourcesViewSystem HexSelectionViewLoadingSystem TerrainViewDebugSystem HexIconsSpawnSystem MainHudSpawnSystem DistrictBuildUISpawnSystem CitySpawnSystem MayorSpawnSystem DistrictOpenConditionSpawnSystem DistrictOpenConditionEvaluatorBootstrapSystem DistrictBuildOutcomeSpawnSystem]
    :files "the census :pipeline_stage :file of each"
    :change ": IPrioritizedUniTaskSystem<MapGenerationStep> becomes : IPipelineStageSystem; UniTask Update(CancellationToken) is renamed Execute; the constructor gains a leading AppState appState and sets public AppState AppState { get; }; a comment naming MapGenerationStep says «map-creation stage» instead"}
   {:kind :appstate-one-shot :symbol ShowHexesUISystem :file "Assets/Presentation/UI/GeneratorMenu/Systems/ShowHexesUISystem.cs"
    :change ": IUniTaskSystem<FirstUIStep> becomes : IUniTaskSystem; Update(CancellationToken) is renamed Execute; the constructor gains a leading AppState appState and sets public AppState AppState { get; }; the summary's FirstUIStep cref is reworded; Show and Hide stay, now without a caller (s1 contra :c-3 residual)"}
   ;; ── orchestrators: the contract list and the shared run ────────────────
   {:kind :host-select :symbols [GenerationSystem HexResourcesSystem HexResourcesViewSystem TerrainViewSystem MainHudSpawnSystem DistrictOpenConditionSpawnSystem DistrictBuildOutcomeSpawnSystem DistrictBuildUISystem TurnProcessorSystem]
    :change "the constructor's IReadOnlyList<FamilyBase> parameter becomes IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems, and the field is set by OrchestratorSubSystems.SelectForOrchestrator(typeof(<Host>), allSubSystems) in place of OrderBy(…).ToArray()"}
   {:kind :host-run :symbols [GenerationSystem HexResourcesSystem HexResourcesViewSystem]
    :change "Execute becomes async and awaits OrchestratorSubSystems.RunAsync(_subSystems, cancellationToken) in place of the default(GameState) loop — GenerationSystem between Generate(config) and AssignHexTypes(), its RunGenerationSubSystems deleted"}
   {:kind :host-run :symbol TerrainViewSystem :change "RunViewSubSystemsAsync is deleted; LoadAndSetupAsync awaits OrchestratorSubSystems.RunAsync(_subSystems, cancellationToken) in its place"}
   {:kind :host-run :symbol MainHudSpawnSystem :change "after the MainHudComponent write, the Prepare loop becomes await OrchestratorSubSystems.RunAsync(_subSystems, cancellationToken)"}
   {:kind :host-run :symbol TurnProcessorSystem :change "the _runner field is deleted; RunTurnAsync awaits OrchestratorSubSystems.RunAsync(_phases, token); _phases is selected once in the constructor, no longer ordered per turn"}
   ;; ── sub-system family bases: onto the contract ─────────────────────────
   {:kind :family-base :symbols [GenerationSubSystem HexResourcesSubSystem HexResourcesViewSubSystem]
    :files ["Assets/Domains/Map/Generation/Systems/GenerationSubSystem.cs" "Assets/Domains/Map/HexResources/Systems/HexResourcesSubSystem.cs" "Assets/Presentation/HexResources/Systems/HexResourcesViewSubSystem.cs"]
    :change ": ISystem<GameState> becomes : IPrioritizedUniTaskSystem, IDisposable; adds public Type OrchestratorType => typeof(GenerationSystem | HexResourcesSystem | HexResourcesViewSystem); public abstract void Update(GameState state) becomes public abstract UniTask Update(CancellationToken cancellationToken)"}
   {:kind :family-base :symbol ViewSubSystem :file "Assets/Presentation/Terrain/Systems/ViewSubSystem.cs"
    :change ": IUniTaskSystem<GameState> becomes : IPrioritizedUniTaskSystem, IDisposable; adds OrchestratorType => typeof(TerrainViewSystem)"}
   {:kind :family-base :symbol TurnPhaseSubSystem :file "Assets/Modules/Turn/Systems/TurnPhaseSubSystem.cs"
    :change ": IPrioritizedUniTaskSystem<TurnPhaseStep> becomes : IPrioritizedUniTaskSystem, IDisposable; adds OrchestratorType => typeof(TurnProcessorSystem); the summary's TurnPhaseRunner becomes TurnProcessorSystem"}
   {:kind :family-base :symbol MainHudSpawnSubSystem :file "Assets/Presentation/UI/MainHud/Systems/MainHudSpawnSubSystem.cs"
    :change "becomes : IPrioritizedUniTaskSystem; gains a protected constructor (EntityStorages storages) keeping Storages; adds OrchestratorType => typeof(MainHudSpawnSystem) and ReadMainHudRoot (MainHudSpawnSubSystem-methods); abstract void Prepare(GameObject) becomes abstract UniTask Update(CancellationToken)"}
   {:kind :family-base :symbol DistrictBuildUISubSystem :file "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildUISubSystem.cs"
    :change ": IDisposable becomes : IPrioritizedUniTaskSystem, IDisposable; adds OrchestratorType => typeof(DistrictBuildUISystem); abstract void Populate(GameObject root) becomes abstract UniTask Update(CancellationToken); Repopulate stays"}
   {:kind :family-base :symbols [DistrictOpenConditionSpawnSubSystem DistrictBuildOutcomeSpawnSubSystem]
    :files ["Assets/Domains/Economy/DistrictOpenCondition/Systems/DistrictOpenConditionSpawnSubSystem.cs" "Assets/Domains/Economy/DistrictBuildOutcome/Systems/DistrictBuildOutcomeSpawnSubSystem.cs"]
    :change ": IDisposable becomes : IPrioritizedUniTaskSystem, IDisposable; the constructor (EntityStore world) becomes (EntityStorages storages) and the World field becomes protected readonly EntityStorages Storages; adds OrchestratorType => typeof(DistrictOpenConditionSpawnSystem | DistrictBuildOutcomeSpawnSystem); abstract bool TrySpawn(config) becomes abstract UniTask Update(CancellationToken)"}
   ;; ── sub-system members: synchronous run becomes the awaited run (21) ───
   {:kind :member-sync-to-async :count 10
    :symbols [MountainGenerationSubSystem RiverGenerationSubSystem LakeGenerationSubSystem SeaGenerationSubSystem ForestResourceGenerationSubSystem ClayResourceGenerationSubSystem FishResourceGenerationSubSystem ForestHexResourceViewSubSystem ClayHexResourceViewSubSystem FishHexResourceViewSubSystem]
    :change "public override void Update(GameState state) becomes public override UniTask Update(CancellationToken cancellationToken); the body is unchanged, every return and the end return UniTask.CompletedTask"}
   {:kind :member-sync-to-async :count 4 :symbols [HexInfoPanelSpawnSubSystem TurnPanelSpawnSubSystem ContextTabsSpawnSubSystem ResourceBarSpawnSubSystem]
    :change "the constructor calls base(storages); Prepare(GameObject mainUi) becomes Update(CancellationToken) whose first line is var mainUi = ReadMainHudRoot(); the rest of the body unchanged, ending return UniTask.CompletedTask"}
   {:kind :member-sync-to-async :count 4 :symbols [DistrictBuildListUISubSystem DistrictBuildHexResourcesUISubSystem DistrictBuildPriceUISubSystem DistrictBuildActionsUISubSystem]
    :change "public override void Populate(GameObject root) becomes public override UniTask Update(CancellationToken cancellationToken); the body is unchanged, ending return UniTask.CompletedTask"}
   {:kind :member-sync-to-async :count 3 :symbols [DistrictExistConditionSpawnSubSystem DistrictSingleOpenConditionSpawnSubSystem SpawnCityCenterOutcomeSubSystem]
    :change "the constructor calls base(storages); TrySpawn(config) becomes Update(CancellationToken) walking the catalogue — DistrictExistConditionSpawnSubSystem-methods"}
   ;; ── registrations ──────────────────────────────────────────────────────
   {:kind :registration :count 26 :symbol "ConfigLoaderSystem<X>"
    :files "the 26 lines of CONTEXT # census :startup_step :lines"
    :change "builder.Register<ConfigLoaderSystem<X>>(Lifetime.Singleton).As<IUniTaskSystem>().WithParameter(AppState.ConfigLoading).WithParameter(\"address\", …) becomes builder.RegisterAppStateSystem<ConfigLoaderSystem<X>>(Lifetime.Singleton, AppState.ConfigLoading).WithParameter(\"address\", …)"}
   {:kind :registration :count 1 :symbol VertexGridSpawnSystem :file "Assets/Presentation/Terrain/Installer/TerrainViewInstaller.cs:59"
    :change "builder.RegisterAppStateSystem<VertexGridSpawnSystem>(Lifetime.Singleton, AppState.InstanceObjects)"}
   {:kind :registration :count 1 :symbol ShowHexesUISystem :file "Assets/Presentation/UI/Installer/UIInstaller.cs:35"
    :change "builder.RegisterAppStateSystem<ShowHexesUISystem>(Lifetime.Scoped, AppState.MainMenu)"}
   {:kind :registration :count 1 :symbol EventCleanupSystem :file "Assets/Scripts/Installers/World/WorldInstaller.cs:64"
    :change "builder.RegisterAppStateSystem<EventCleanupSystem>(Lifetime.Singleton, AppState.Initialization | AppState.ConfigLoading | AppState.InstanceObjects | AppState.MainMenu | AppState.MapCreation | AppState.MapLoading | AppState.Gameplay | AppState.GameOver)"}
   {:kind :registration :count 14 :symbols "the 14 pipeline stages"
    :files ["TerrainGeneratorInstaller.cs:18" "HexResourcesInstaller.cs:19" "TerrainViewInstaller.cs:63 65 70" "HexResourcesViewInstaller.cs:25" "HexIconsInstaller.cs:24" "UIInstaller.cs:55 68" "ActorsInstaller.cs:26 29" "EconomyInstaller.cs:37 48 57"]
    :change ".As<Stage, IPrioritizedUniTaskSystem<MapGenerationStep>>() becomes builder.RegisterAppStateSystem<Stage>(<the same Lifetime>, AppState.MapCreation)"}
   {:kind :registration :count 25 :symbols "the 25 reactive and per-frame systems but EventCleanupSystem"
    :files ["WorldInstaller.cs:65 70" "UIInstaller.cs:72 74 76 78 80 82 93 95 97" "TerrainViewInstaller.cs:68" "HexResourcesViewInstaller.cs:30 32" "DistrictsInstaller.cs:29 32 35" "HexIconsInstaller.cs:28 32" "TurnInstaller.cs:21 24" "ActionsInstaller.cs:28 31 34" "EconomyInstaller.cs:69"]
    :change ".As<System>() becomes builder.RegisterAppStateSystem<System>(<the same Lifetime>, AppState.Gameplay)"}
   {:kind :registration :count 27 :symbols "every sub-system and turn phase on the contract"
    :files ["TerrainGeneratorInstaller.cs:21 23 25 27" "HexResourcesInstaller.cs:22 24 26" "TerrainViewInstaller.cs:73 75 77" "HexResourcesViewInstaller.cs:35 37 39" "UIInstaller.cs:57 59 61 63 85 87 89 91" "EconomyInstaller.cs:40 51 54 60" "ActionsInstaller.cs:22 25"]
    :change ".As<Concrete, FamilyBase>() becomes .As<IPrioritizedUniTaskSystem>(), the Lifetime unchanged"}
   {:kind :registration :count 2 :file "Assets/Domains/Economy/Installer/EconomyInstaller.cs:63 66" :symbols [DistrictSingleOpenConditionEvaluatorSubSystem DistrictExistConditionEvaluatorSubSystem]
    :change "none — the evaluator family is untouched"}
   {:kind :registration :count 9 :file "Assets/Scripts/Installers/World/WorldInstaller.cs" :symbols [InitializationState ConfigLoadingState InstanceObjectsState MainMenuState MapCreationState MapLoadingState GameplayState GameOverState GameModeMachine]
    :change "after InstallModules(builder): builder.Register<XState>(Lifetime.Singleton).As<IAppState>() for each of the eight states, in AppState order, then builder.Register<GameModeMachine>(Lifetime.Singleton)"}
   ;; ── comments that become false ─────────────────────────────────────────
   {:kind :comment
    :files ["WorldInstaller.cs:63" "UIInstaller.cs:19-23 53-54 71 84" "TerrainViewInstaller.cs:67" "HexResourcesViewInstaller.cs:28-29" "DistrictsInstaller.cs:10-12 27-28" "HexIconsInstaller.cs:27 31" "TurnInstaller.cs:18-20" "ActionsInstaller.cs:9-17" "ConfigLoaderSystem.cs:12-13" "IAppState.cs:8-13" "DistrictBuildUISystem.cs:21-30 187-188" "DistrictBuildUISubSystem.cs:7-10" "MainHudSpawnSubSystem.cs:5-10" "MainHudSpawnSystem.cs:16-21" "the pipeline-stage comments naming MapGenerationStep or «Boot world-init orchestrator»"]
    :change "reworded to the new wiring — registration through RegisterAppStateSystem, states filtering by flags, orchestrators selecting by OrchestratorType; a comment says only what stays true for every caller"}])
```

```clojure
{:spine-reads {"Boot.Start"                         "Application.targetFrameRate = 60; _machine.Switch(StartupOrder[0]);"
               "Boot.Update"                        "if (!_machine.IsEntryCompleted) return; _machine.Tick(new GameState(Time.deltaTime)); AdvanceMode();"
               "Boot.LateUpdate"                    "if (!_machine.IsEntryCompleted) return; _machine.LateTick(new GameState(Time.deltaTime));"
               "InitializationState ctor"           "_systems = AppStateSystems.Filter(Mode, allSystems);"
               "MainMenuState ctor"                 "_generateRequests = EventArchetypes.Of<TerrainGenerationGenerateEventComponent>(storages.World); _systems = AppStateSystems.Filter(Mode, allSystems);"
               "MainMenuState.EnterAsync"           "_requestedMode = null; await _systems.RunEntryAsync(cancellationToken);"
               "MainMenuState.Tick"                 "RequestMapCreationOnRipeRequest(); _systems.Tick(gameState);"
               "MapCreationState.EnterAsync"        "_requestedMode = null; _settledFrames = 0; await _systems.RunEntryAsync(cancellationToken);"
               "MapCreationState.Tick"              "_systems.Tick(gameState); CountSettleFrame();"
               "GameplayState.EnterAsync"           "await _systems.RunEntryAsync(cancellationToken); SeedStartOfPlay();"
               "DistrictBuildUISystem ctor"         "_subSystems = OrchestratorSubSystems.SelectForOrchestrator(typeof(DistrictBuildUISystem), allSubSystems); HandRepopulateToSections();"
               "DistrictOpenConditionSpawnSystem.Execute" "await OrchestratorSubSystems.RunAsync(_subSystems, cancellationToken); EnsureEveryConditionSpawned();"
               "DistrictBuildOutcomeSpawnSystem.Execute"  "await OrchestratorSubSystems.RunAsync(_subSystems, cancellationToken); EnsureEveryOutcomeSpawned();"
               "MainHudSpawnSubSystem member Update" "var mainUi = ReadMainHudRoot(); …today's Prepare body…; return UniTask.CompletedTask;"}
 :steps-visible "every spine above: body lines without guards and lifetime lines = its :flow steps"
 :data-coverage {:s1-coverage "complete — all 23 s1 keys have an entry (:from-s1): system-flags first-order-systems orchestrator-type all-sub-systems orchestrator-own-type orchestrator-parts evaluator-parts section-repopulate run-cancellation main-hud-root open-conditions-catalogue outcomes-catalogue state-update-systems state-late-update-systems state-entry-steps state-pipeline game-states states-by-mode machine startup-order current-state entry-completed requested-mode"
                 :undeclared 0
                 :orphan 0
                 :one-step-fields {:count 0
                                   :composition-fields [:state-systems :generate-requests :states-by-mode :orchestrator-parts :condition-rows :outcome-rows :evaluator-parts :section-repopulate]
                                   :why-not-counted "readonly fields written once by a constructor and read by the class's other entry points (EnterAsync, Tick, Execute) — dependencies and caches of the owner, not run nouns of one spine; the reader judges"}
                 :unborn 0
                 :scratch-heavy [{:method AppStateSystems.Filter :scratch 4 :kept "one homogeneous pass files every system — the buffers are its outputs"}]
                 :born-in-s2 [:system-registration :state-systems :state-mode :updated-buffer :late-updated-buffer :entry-step-buffer :pipeline-stage-buffer :storages :generate-requests :settled-frames :entry-cancellation :entry-token :game-state :current-mode :next-mode :section-run :condition-rows :outcome-rows :sub-system-storages]}
 :how-semicolons 0
 :reader-clean true}   ;; the sdd-flow Clojure reader (clojure-reader.js readAll) over every fence of CASCADE.md, 2026-09-16
```

# Contra

```clojure
[{:id :c-17
  :kills "MapCreation's stage order — the Priority of pipeline stages on the system side"
  :case "the 14 stages leave IPrioritizedUniTaskSystem, which is the sub-system contract alone (decision :systems-and-subsystems-are-two-types); MapCreation must still await them ascending by SystemPriorities.WorldInit, and registration order is not that order — UIInstaller registers MainHudSpawnSystem (800) before TerrainGeneratorInstaller registers GenerationSystem (100)"
  :fails "s1 names a system-side one-shot kind with a Priority but no existing type is one: IUniTaskSystem has no Priority, and borrowing the sub-system contract is void"
  :fix [{:id :fix-a :confidence 65 :is "a new system kind IPipelineStageSystem : IUniTaskSystem with int Priority, in Ecs.Extensions — as this s2 writes" :cost "one interface; 14 declarations change their base; ARCHITECTURE :system/base-choice and :pipeline/stage rename it in the rules step"}
        {:id :fix-b :confidence 25 :is "int Priority on IUniTaskSystem itself; config loaders, VertexGridSpawnSystem and ShowHexesUISystem return a priority from a new startup space; one sorted one-shot array per state" :cost "a Priority on ConfigLoaderSystem<T>, VertexGridSpawnSystem and ShowHexesUISystem and a new SystemPriorities space; s1's two lists — entry steps and pipeline — merge into one"}
        {:id :fix-c :confidence 10 :is "MapCreation awaits its stages in registration order and the installers are reordered to match" :cost "execution order hidden in installer order across 8 files; SystemPriorities.WorldInit becomes dead"}]}

 {:id :c-18
  :kills "the registration helper compiling where every installer can call it"
  :case "RegisterAppStateSystem extends IContainerBuilder; the one assembly all 12 installer assemblies reference directly is Ecs.Extensions (asmdef_reach can <assembly> ConfigLoaderSystem — YES for each), and asmdef_reach refs Ecs.Extensions lists no VContainer"
  :fails "the helper does not compile in Ecs.Extensions without a new asmdef reference no decision covers; Boot.Core, the other common reference, references nothing at all"
  :fix [{:id :fix-a :confidence 60 :is "add VContainer to Ecs.Extensions.asmdef — the shared kernel may depend on external libraries (fantasymayor-placement :depends :shared-kernel) — as this s2 writes" :cost "the ECS kernel learns the DI library"}
        {:id :fix-b :confidence 25 :is "add VContainer to Boot.Core.asmdef and place the helper beside IAppStateSystem" :cost "a Core contract assembly takes its first dependency"}
        {:id :fix-c :confidence 15 :is "a new small assembly for the helper, referenced by the 12 installer assemblies" :cost "a new asmdef whose .meta is Unity's, and 12 asmdef edits"}]}

 {:id :c-19
  :kills "s1 step-1 as written — the registration exposes a system «next to the interfaces of its kind»"
  :case "RegisterAppStateSystem exposes IAppStateSystem only"
  :fails "after Boot stops composing, nothing resolves a kind collection or a concrete first-order system — a state files by type tests over IReadOnlyList<IAppStateSystem>; exposing the kinds registers services nobody consumes, not exposing them narrows an approved s1 sentence"
  :fix [{:id :fix-a :confidence 65 :is "IAppStateSystem only — as this s2 writes; s1 step-1 is read as narrowed at this gate" :cost "the s1 sentence is amended"}
        {:id :fix-b :confidence 35 :is "the helper also exposes every kind TSystem implements, by type tests and As(Type)" :cost "four type tests in the helper and registrations with no consumer"}]}

 {:id :c-20
  :kills "s1's reading of the single contract — a kept part is run, ordered and switched through the contract, never through a family base"
  :case "DistrictBuildUISystem must set Repopulate on each of its 4 kept sections; IPrioritizedUniTaskSystem has no Repopulate, and decision :c13-async-parts-in-update-host keeps the callback as it is"
  :fails "the host reaches DistrictBuildUISubSystem by a checked conversion — the one place in the task where the contract alone is not enough"
  :fix [{:id :fix-a :confidence 55 :is "one checked conversion per section at construction, throwing on a part of another type — as this s2 writes" :cost "a family-base conversion inside one host"}
        {:id :fix-b :confidence 35 :is "s1 contra :c-13 fix-c: the list section raises a re-populate request event and the host re-runs its sections on its next tick; Repopulate leaves the family" :cost "a new event type and one frame between a click and refreshed sections; reopens what the owner closed as «no change»"}
        {:id :fix-c :confidence 10 :is "the host publishes its re-populate through a world singleton the list section reads" :cost "a managed callback stored in ECS data"}]}

 {:id :c-21
  :kills "the precision of the unhandled-entry throw — decision :c14-unhandled-entry-throws"
  :case "DistrictOpenConditionsConfig holds 5 entries, one of a new kind; the two parts create 4 rows"
  :fails "the host knows rows and entries but not which entry went unhandled: its message names both counts and every concrete type in the catalogue, where today's named the one unhandled type; and the check assumes no condition row exists before this stage — true while MapCreation is entered once per session (GameplayState requests no mode — CONTEXT fact on the states), false on any later regeneration"
  :fix [{:id :fix-a :confidence 55 :is "rows == entries over the family label query (DistrictOpenConditionTag) and the outcome archetype — as this s2 writes" :cost "a coarser message; a regeneration would throw until the check counts a delta"}
        {:id :fix-b :confidence 30 :is "each part records the catalogue indices it handled in a transient world record; the host names the type of the first unmarked index, then releases the record" :cost "a new component, a writer in every part and one release place"}
        {:id :fix-c :confidence 15 :is "fix-a with the count taken before the run and compared as a delta" :cost "one more line and a local; still no single type name"}]}

 {:id :c-22
  :kills "s1 step-12 — the machine is released «when Boot is destroyed»"
  :case "GameModeMachine becomes a Singleton registration and is IDisposable; VContainer disposes it when the LifetimeScope is destroyed, and Boot.OnDestroy disposes it too"
  :fails "two release places, in an order Unity decides — the one-release-place rule is broken even though a second Dispose is harmless"
  :fix [{:id :fix-a :confidence 60 :is "the container is the one release place; Boot.OnDestroy is deleted — as this s2 writes; step-12 reads «when the scope is destroyed»" :cost "Boot no longer chooses the moment states exit; the scope and Boot share one scene"}
        {:id :fix-b :confidence 40 :is "Boot releases: the machine drops IDisposable for a Stop() that Boot.OnDestroy calls" :cost "a renamed member; the container no longer tracks the machine"}]}

 {:id :c-23
  :kills ":behaviour-must-hold — the district-build list's default selection"
  :case "DistrictBuildListUISubSystem default-selects buildable[0] from the DistrictOpenStateComponent index"
  :fails "condition rows are now created grouped by part priority — every Exist row, then every Single row — instead of interleaved in catalogue order; whether the index, and so the first buildable district, follows creation order is not measured (s1 contra :c-14 residual)"
  :fix [{:id :fix-a :confidence 60 :is "accept; the owner's Unity check shows the default on the first open" :cost "the default district may change"}
        {:id :fix-b :confidence 25 :is "the list section orders buildable districts by DistrictType before taking the first" :cost "a behaviour change inside a section, beyond this task"}
        {:id :fix-c :confidence 15 :is "the host walks the catalogue and runs all parts per entry through a current-entry world record" :cost "s1 contra :c-14 fix-c — every part once per entry, a parameter imitated through the world"}]}]
```

```clojure
{:gate-after-s2 {:subject "the structure of the code: decomposition, names, lifetime"
                 :open-contra #{}
                 :resolved {:c-17 :fix-a :c-18 :fix-a :c-19 :fix-a :c-20 :fix-a :c-21 :fix-a :c-22 :fix-b :c-23 :fix-a}
                 :owner-verdict "approved 2026-09-16 — «1a 2a 3a 4a 5a 6b 7a», then the code stage is sliced for parallel Sonnet subagents; TurnPhaseRunner deletion stands as written (not objected)"}}
```
