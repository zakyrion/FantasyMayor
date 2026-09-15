---
name: fantasymayor-placement
description: Place every new FantasyMayor file — layer, assembly, folder, role folder, installer — and check each new cross-assembly reference against the dependency rules. Use when a FantasyMayor implementation map names new files, types, asmdef references or installers (CLAUDE.md § 2 puts the result into the map's :skills), or when the user invokes fantasymayor-placement.
---

# Placement

Where each new file of a FantasyMayor task lives, which assemblies it may reference, and which installer registers it.

## Procedure

```clojure
(def placement
  (-> (:step-1 "list every new file, type, asmdef reference and registration the implementation map creates")
      (:step-2 "each new file: its layer by layers, its path by place and folder-layout, its role folder by role-folders")
      (:step-3 "each new reference between assemblies: allowed by depends; python3 Tools/asmdef_reach.py can <Assembly> <Type> — a type compiles only through a direct reference")
      (:step-4 "each new registration: its installer by place :installer and di-composition")
      (:result "{:skills {fantasymayor-placement [{:file … :assembly … :role-folder … :installer …}]}} — shown with the implementation map")))
```

## Layers

```clojure
(def layers
  {:domain        {:home  "Assets/Domains/<Domain>/"
                   :holds "the game rules of one bounded context: its entity tables and the logic that changes them"
                   :never #{"a dependency on presentation" "views, prefabs, textures"}}
   :presentation  {:home  ["Assets/Presentation/" "Assets/Presentation/UI/"]
                   :holds "the display of domain state and the intake of the player's intent"
                   :never #{"a game rule" "state a domain owns" "a direct change to a domain table — only a command event"}}
   :module        {:home  "Assets/Modules/<Module>/"
                   :holds "pure engine-facing logic that knows none of this game's rules"
                   :never "a game rule"}
   :shared-kernel {:home  ["Assets/Scripts/Core/" "Assets/Scripts/EcsExtensions/"]
                   :holds "primitives and ECS base types every layer builds on"}
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
   :exempt               {Boot.Implementation "outside every dependency rule — it composes the game states"}
   :check                "python3 Tools/asmdef_reach.py refs <Assembly> — its direct references; path <AsmA> <AsmB> — the direction between two assemblies"
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
   :installer     (cond (domain? code)                     "Assets/Domains/<Domain>/Installer/"
                        (presentation? code)               #{"Assets/Presentation/Installer/" "Assets/Presentation/UI/Installer/"}
                        (single-asmdef-module? code)       "Assets/Modules/<Module>/Installer/"
                        (core-implementation-module? code) "Assets/Scripts/Installers/<Module>/ — its own asmdef, referencing the module's Core/ and Implementation/"
                        (engine-level-core? code)          "no installer — the app-root registers it itself, first")
   :app-root      {:to "Assets/Scripts/Installers/" :only-when "the composition of the whole application"}
   :config-asset  "Assets/Addressables/Configs/"
   :ui-assets     {:to "<Area>/Prefabs/" :contains "prefabs, uxml, uss"}})
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
   "Archetypes/"  {:where "directly under an assembly root" :only-when "the assembly declares archetypes"}
   "Installer/"   {:where "directly under the root of a domain, a presentation assembly or a single-asmdef module"
                   :never #{"inside a feature or an area" "inside a pair module — its installer lives in the app-root"}}
   :shared-role   {:where "a role folder directly under a domain or presentation assembly root" :only-when "several features or areas share its contents"}
   :never         "code outside a role folder"})

(def role-folders
  {:where        "inside a feature, an area or a module assembly; directly under a domain or presentation assembly root only as a shared role"
   "Components/" {:contains "pure data structs"           :never "logic, side effects"}
   "Tags/"       {:contains "tag components"}
   "Events/"     {:contains "one-frame event components"}
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
   :root-first        "the root itself registers the engine-level core — camera, input, UI root, event cleanup — before any installer"
   :installer         {:is "a plain class : VContainer.IInstaller" :mono-only-when "it owns [SerializeField] data"}
   :install-order     {:where "InstallModules, called from the root's Configure()" :rule "dependencies before dependents — startup systems run in registration order"}
   :new-installer     "one new <Name>Installer().Install(builder) line in InstallModules, after the installers it depends on; every other registration stays in its layer's installer"
   :per-frame-system  {:register "as its concrete type: .As<TheSystem>()" :never "As<IUpdatedSystem>" :wired-by "Boot adds concrete systems to game states by hand"}
   :public-api-module {:shape "a Core/ contract assembly + an Implementation/ assembly" :consumers "reference Core/ only"}})
```
