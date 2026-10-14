---
name: fantasymayor-placement
description: Check every FantasyMayor placement against the layering law before anyone sees it — a file or type created, moved or deleted, an asmdef reference added or removed, an installer or registration placed. Use BEFORE showing any option, task statement, implementation map or cascade structure that places, moves or deletes a type or file or touches an asmdef reference (AGENTS.md § 2; .sdd-flow/project.md # Ceremonies placement-check — the survey before options, the structure for every file), or when the user invokes fantasymayor-placement.
---

# Placement

Where each file of a FantasyMayor task lives, which assemblies it may reference, which installer registers it —
checked before an option, a statement or a structure reaches the owner. An option the law forbids is never offered.

## Frame

```clojure
(def frame  ;; Clean Architecture (R. C. Martin, 2017): ch. 22 the Dependency Rule, ch. 26 the Main component
  {:dependency-rule "source dependencies point inward only: an inner circle never names anything of an outer one"
   :circles "shared kernel (Assets/Scripts/Core, Assets/Scripts/EcsExtensions) → modules → domains → presentation; the composer sees them all"
   :crossing "a boundary is crossed by an interface the inner circle owns and the outer implements; plain structs cross it"
   :main "Installers.World — the app composer holding the root LifetimeScope — is the one assembly outside every dependency rule"
   :folders "the book is silent about folders inside an assembly; the folder law below is this project's own"})
```

## Procedure

```clojure
(def placement
  (-> (:step-1 "list every placement the option, statement, map or structure makes: a file or type created, moved or deleted; an asmdef reference added or removed; an installer or registration placed")
      (:step-2 "each new or moved file: its layer by layers, its path by place and folder-layout, its role folder by role-folders; namespace = folder path")
      (:step-3 "each new or moved type: its destination assembly must see every type the moved type itself names — python3 Tools/asmdef_reach.py can <DestinationAssembly> <Type> for each; a NO that only a new reference fixes is checked by step-4, and a reference that would close a cycle kills the option")
      (:step-4 "each new reference between assemblies: allowed by depends, and no cycle — python3 Tools/asmdef_reach.py path <To> <From> must print NOT reachable; a type compiles only through a direct reference")
      (:step-5 "each deleted file: when it is the last file of its folder, the folder and its .meta go too, and every `using` of the vanished namespace is removed — find them with mcp__roslyn__get_diagnostics (CS0234) after the delete, never leave them to Unity")
      (:step-6 "each new registration: its installer by place :installer and di-composition")
      (:gate "a placement the law forbids is dropped from the option set before the owner sees it; when every option is forbidden, say so and ask")
      (:result "{:skills {fantasymayor-placement [{:placement … :assembly … :role-folder … :installer … :verdict :allowed|:forbidden :rule <id>}]}} — shown with the statement, the map or the structure")))
```

## Layers

```clojure
(def layers
  {:domain        {:home  "Assets/Domains/<Domain>/"
                   :holds "the game rules of one bounded context: its entity tables and the logic that changes them"
                   :never #{"a dependency on presentation" "views, prefabs, textures"}}
   :presentation  {:home  ["Assets/Presentation/" "Assets/Presentation/UI/"]
                   :holds "the display of domain state and the intake of the player's intent"
                   :never #{"a game rule" "state a domain owns" "a direct change to a domain table — only a command event"}
                   :ui-selection "pure interface selection state stays in presentation — never put into a substrate domain so another layer can read it (transaction/no-substrate-state)"}
   :module        {:home  "Assets/Modules/<Module>/"
                   :holds "pure engine-facing logic that knows none of this game's rules"
                   :never "a game rule"}
   :shared-kernel {:home  ["Assets/Scripts/Core/" "Assets/Scripts/EcsExtensions/"]
                   :holds "primitives and ECS base types every layer builds on — only code several layers need that knows none of them"}
   :app-root      {:home  "Assets/Scripts/Installers/"
                   :holds "the DI composition of the whole application — the one place that sees every layer"}})

(def depends
  {:shared-kernel        {:only "external libraries and the engine"}
   :module               {:ideal "no dependencies — pure logic"
                          :may   ["another module" "the shared kernel"]
                          :never #{"a domain" "presentation"}}
   :layer->module        {:from  [:domain :presentation]
                          :count "any number of modules"
                          :only  "a Core/ contract assembly or a single-asmdef module"
                          :never "an Implementation/ assembly"}
   :layer->shared-kernel :allowed
   :presentation->domain "one way — onto what it renders"
   :domain->presentation :never
   :domain->domain       {:tiers     [:substrate :agents :verbs]
                          :classify  (cond (describes-what-an-owner-does? domain) :verbs
                                           (knows-a-concrete-owner? domain)       :agents
                                           :else                                  :substrate)
                          :rule      "a domain references only its own tier or a tier before it"}
   :cycles               :never
   :exempt               {Installers.World "the one assembly outside every dependency rule — the app composer holding the root LifetimeScope, Main in Clean Architecture ch. 26; Boot.Implementation (machine, states, transition systems) is an ordinary module: shared kernel and external libraries only"}
   :archetype-reach      "an archetype names only components its own assembly can see"
   :check                "python3 Tools/asmdef_reach.py refs <Assembly> — its direct references; path <AsmA> <AsmB> — the direction between two assemblies; can <Assembly> <Type> — compile visibility"
   :namespace            "follows the folder path"})
```

## Placement

```clojure
(def place
  {:domain-rule   {:to "Assets/Domains/<Domain>/<Feature>/" :owner "the domain that owns the rule" :new-domain "only on the owner's decision"}
   :world-view    "Assets/Presentation/<SubArea>/"
   :hud           "Assets/Presentation/UI/<Window>/"
   :infra         "Assets/Modules/<Module>/"
   :shared-kernel {:to #{"Assets/Scripts/Core/" "Assets/Scripts/EcsExtensions/"} :only-when "several layers need the code and it knows none of them"}
   :app-state-contracts "AppState, IAppStateSystem, IAppState, GameState, AppStateRequestedEvent and IEventTag live in the shared kernel Core (the event in Core/Events/); the machine, the states and AppStateSystems in Boot.Implementation"
   :event         "an event is a struct implementing IEventTag with the event suffix, in the Events/ folder of its own feature — in the shared kernel too"
   :fk-column     "a reference (FK) column lives in the owner feature's folder next to its primary key — one namespace, one pair of types, defined once"
   :installer     (cond (domain? code)                     "Assets/Domains/<Domain>/Installer/"
                        (presentation? code)               #{"Assets/Presentation/Installer/" "Assets/Presentation/UI/Installer/"}
                        (single-asmdef-module? code)       "Assets/Modules/<Module>/Installer/"
                        (core-implementation-module? code) "Assets/Scripts/Installers/<Module>/ — its own asmdef, referencing the module's Core/ and Implementation/"
                        (engine-level-core? code)          "no installer — the app-root registers it itself, first")
   :app-root      {:to "Assets/Scripts/Installers/" :only-when "the composition of the whole application"}
   :config-class  "the feature's Configs/ folder"
   :config-asset  "Assets/Addressables/Configs/"
   :ui-assets     {:to "<Area>/Prefabs/" :contains "prefabs, uxml, uss" :textures "<Area>/Textures/"}})
```

## Folder layout

```clojure
(def folder-layout
  {:domain        {:assembly "Assets/Domains/<Domain>/ — one asmdef per domain"
                   :feature  "Assets/Domains/<Domain>/<Feature>/"}
   :presentation  {:assembly #{"Assets/Presentation/" "Assets/Presentation/UI/"}
                   :area     #{"<Assembly>/<SubArea>/" "Assets/Presentation/UI/<Window>/<Panel>/"}}
   :single-module {:assembly     "Assets/Modules/<Module>/ — one asmdef; the module is one feature"
                   :role-folders "directly under the assembly root"}
   :pair-module   {:assembly     ["Assets/Modules/<Module>/Core/" "Assets/Modules/<Module>/Implementation/"]
                   :role-folders "inside each of the two assemblies"}
   :shared-kernel {:layout "flat — its files sit at the assembly root; its events alone go to Events/"
                   :why "a small assembly that changes for one reason (Clean Architecture ch. 13, CCP) — role folders would only scatter it"}
   "Archetypes/"  {:where "directly under an assembly root" :only-when "the assembly declares archetypes"}
   "Installer/"   {:where "directly under the root of a domain, a presentation assembly or a single-asmdef module"
                   :never #{"inside a feature or an area" "inside a pair module — its installer lives in the app-root"}}
   :shared-role   {:where "a role folder directly under a domain or presentation assembly root" :only-when "several features or areas share its contents"}
   :never         "code outside a role folder — except the shared kernel (:shared-kernel above)"})

(def role-folders
  {:where        "inside a feature, an area or a module assembly; directly under a domain or presentation assembly root only as a shared role"
   "Components/" {:contains "pure data structs"           :never "logic, side effects"}
   "Tags/"       {:contains "tag components"}
   "Events/"     {:contains "event types — structs implementing IEventTag"}
   "Configs/"    {:contains "ScriptableObject class defs"  :never "runtime logic, config assets"}
   "Data/"       {:contains "collections, records, enums"  :never "ECS systems, MonoBehaviours"}
   "Systems/"    {:contains "ECS systems + orchestration"  :never "view logic, config definitions"}
   "Helpers/"    {:contains "stateless computation — the only name for this role" :never "cross-frame state, entity ownership"}
   "Views/"      {:contains "MonoBehaviour view layer"     :never "business logic" :only-in :presentation}
   "Prefabs/"    {:contains "prefabs, uxml, uss"                                   :only-in :presentation}
   "Textures/"   {:contains "texture assets"                                       :only-in :presentation}})
```

## DI composition

```clojure
(def di-composition
  {:root              {:is WorldInstaller :only "the one LifetimeScope — the whole application is composed in its Configure()"}
   :root-first        "the root itself registers the engine-level core — camera, input, UI root, the one open EventReader<> registration — before any installer; EventReaderAotDeclarations (the closed-type AOT list, rule event/aot-reader) lives beside the root too"
   :installer         {:is "a plain class : VContainer.IInstaller" :mono-only-when "it owns [SerializeField] data"}
   :install-order     {:where "InstallModules, called from the root's Configure()" :rule "dependencies before dependents — startup systems run in registration order"}
   :new-installer     "one new <Name>Installer().Install(builder) line in InstallModules, after the installers it depends on; every other registration stays in its layer's installer"
   :first-order-system {:register "builder.RegisterAppStateSystem<TheSystem>(Lifetime, AppState flags) — exposes IAppStateSystem only, never .As<TheSystem>()" :collected-by "every app state whose Mode its flags contain (AppStateSystems.Filter); Boot names no system"}
   :subsystem         {:register "builder.Register<TheSubSystem>(Lifetime).As<IPrioritizedUniTaskSystem>()" :routed-by "its OrchestratorType"}
   :public-api-module {:shape "a Core/ contract assembly + an Implementation/ assembly" :consumers "reference Core/ only"}})
```
