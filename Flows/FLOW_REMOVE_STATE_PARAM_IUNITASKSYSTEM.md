---
category: A
read: always
status: partial
tags: [ecs-extensions, boot, refactor]
code_refs:
  interfaces: [IUniTaskSystem, IPrioritizedUniTaskSystem]
  bases: [ConfigLoaderSystem, UniTaskSequentialSystem, TurnPhaseSubSystem, ViewSubSystem]
  callers: [Boot, MapCreationState, MainMenuState, TurnPhaseRunner, TurnProcessorSystem]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
---

# FLOW — Remove state param from IUniTaskSystem.Update

## 1 · Request

## User request — 2026-09-06 (verbatim)

> `{:task :rewrite-method :target "IUniTaskSystem.Update" :goal "Remove T state from method signature" :extra "This method is used by many descendants of IUniTaskSystem, so we need to remove T state from the method signature to avoid breaking changes."}`

## Agent restatement — confirmed by the user

```clojure
{:task :remove-state-param-iunitasksystem
 :goal "IUniTaskSystem<in T>.Update(T state, CancellationToken) → Update(CancellationToken); IPrioritizedUniTaskSystem<in T> inherits the new shape unchanged otherwise"
 :where "IUniTaskSystem.cs, IPrioritizedUniTaskSystem.cs (verified unaffected), every implementer/base/caller listed in Findings below"
 :decided ["T stays on both interfaces as a phantom generic — only the Update parameter is dropped, so the 18 VContainer .As<Concrete, IUniTaskSystem<Step>>() collection-grouping registrations are untouched"
           "GameState/DeltaTime delivery is not redesigned — it was already always default(GameState), so nothing observable changes there"
           "TurnPhaseRunner.RunAsync's now-dead TurnPhaseStep step parameter is also stripped, cascading into its one caller TurnProcessorSystem.cs"]
 :off-limits ["removing T from the interface declarations"
              "designing a real per-frame DeltaTime channel"
              "changing what any implementer actually does besides dropping the unused parameter"]
 :skip "no behavioral change — every marker T (ConfigLoadStep/MapGenerationStep/TurnPhaseStep/FirstUIStep) is empty and every GameState use is already a hollow default, so no implementer loses data it was using"
 :result "IUniTaskSystem<T>/IPrioritizedUniTaskSystem<T> expose Update(CancellationToken cancellationToken); every implementer/base/caller updated to match; solution compiles clean per roslyn diagnostics"}
```

## Amendments (append-only)

```clojure
[]
```

# 2 · Contract

```clojure
(def research-findings
  {:target "EcsExtensions.IUniTaskSystem<in T> — T appears ONLY in Update(T state, CancellationToken)"
   :inheriting-interface "IPrioritizedUniTaskSystem<in T> : IUniTaskSystem<T> — adds only Priority, no Update redeclaration, verified unaffected"
   :t-concrete-types #{ConfigLoadStep MapGenerationStep TurnPhaseStep FirstUIStep GameState}
   :marker-types-are-empty "ConfigLoadStep, MapGenerationStep, TurnPhaseStep, FirstUIStep are verified zero-field structs — read every declaration file"
   :gamestate-is-hollow "GameState.DeltaTime is the only non-empty T, but its sole IUniTaskSystem construction site (TerrainViewSystem.cs:197, pre-edit) was `default(GameState)` — DeltaTime is never populated with a real value through this path"
   :no-body-usage "grepped every implementer's Update body for `state` — zero real usages found anywhere; ShowHexesUISystem.cs already documented its param as \"Unused boot phase marker\""
   :t-is-di-load-bearing "18 VContainer .As<Concrete, IUniTaskSystem<ConfigLoadStep>>() / IPrioritizedUniTaskSystem<MapGenerationStep> registrations key collection-injection grouping off T — confirmed via grep across all Installer*.cs; T is not vestigial from the DI angle, only from the data angle"
   :configloadersystem-collapses-scope "ConfigLoaderSystem (abstract, T=ConfigLoadStep) is the sole Update declaration for its whole family — the 18 concrete *ConfigLoaderSystem classes only implement LoadConfigsAsync and need no edit"
   :file-count "31 files carry an actual edit (interface + 4 bases/orchestrators + 20 concrete Update declarations/overrides + 6 caller-only files); IPrioritizedUniTaskSystem.cs is the one file verified as needing none"})
```

```clojure
(def decisions
  [{:decision :keep-generic-t :status :confirmed :at "2026-09-06"
    :value "T stays on IUniTaskSystem<in T> / IPrioritizedUniTaskSystem<in T>; only the Update parameter is dropped"
    :verified-by "user answer to AskUserQuestion, weighing against the 18 DI collection-grouping registrations found in Findings"
    :reason "removing T would break VContainer's IReadOnlyList<IUniTaskSystem<Step>> stage-grouping mechanism — a separate, larger, unrequested redesign"}
   {:decision :gamestate-out-of-scope :status :confirmed :at "2026-09-06"
    :value "GameState/DeltaTime delivery is not redesigned in this task"
    :verified-by "user answer to AskUserQuestion; grounded in the finding that DeltaTime was already always default(GameState)"
    :reason "nothing today reads real per-frame data through this path, so there is nothing to preserve or replace"}
   {:decision :strip-runasync-step-param :status :confirmed :at "2026-09-06"
    :value "TurnPhaseRunner.RunAsync drops its now-dead TurnPhaseStep step parameter; TurnProcessorSystem.cs:80 drops the matching `new TurnPhaseStep()` argument"
    :verified-by "user answer to AskUserQuestion, surfaced as an emergent decision because TurnProcessorSystem.cs sits outside the originally confirmed :where"
    :reason "leaving a parameter that is immediately ignored contradicts the task's own motivation (stop passing dead marker values through system plumbing)"}])
```

Every resolution is CLOSED, execute in order after a fresh implementation-go.

# 3 · Plan

```clojure
(-> (:step-1 "IUniTaskSystem.cs: Update(T state, CancellationToken) -> Update(CancellationToken); fix XML doc (drop <param name=\"state\">)")
    (:step-2 "4 bases/orchestrators: ConfigLoaderSystem.cs, UniTaskSequentialSystem.cs (signature + forwards to children), TurnPhaseSubSystem.cs, ViewSubSystem.cs")
    (:step-3 "12 concrete MapGenerationStep implementers: GenerationSystem, HexResourcesSystem, DistrictOpenConditionSpawnSystem, DistrictBuildOutcomeSpawnSystem, DistrictOpenConditionEvaluatorBootstrapSystem, MayorSpawnSystem, CitySpawnSystem, HexIconsSpawnSystem, HexResourcesViewSystem, TerrainViewDebugSystem, HexSelectionViewLoadingSystem, DistrictBuildUISpawnSystem, MainHudSpawnSystem")
    (:step-4 "TerrainViewSystem.cs: own Update(MapGenerationStep,...) signature + its internal ViewSubSystem orchestration loop (drop the dead `var state = default(GameState)` local and the forwarded argument)")
    (:step-5 "ShowHexesUISystem.cs: Update(FirstUIStep state,...) -> Update(cancellationToken); drop its own <param name=\"state\"> doc line")
    (:step-6 "3 TurnPhaseSubSystem overrides: DistrictOpenConditionEvaluatorSystem, BuildDistrictTurnTickSystem, MayorAPRestoreSubSystem")
    (:step-7 "3 ViewSubSystem overrides: TerrainViewTextureSubSystem, WaterViewSubSystem, TerrainViewGenerationSubSystem")
    (:step-8 "Caller sites: Boot.cs (drop `new ConfigLoadStep()`), MapCreationState.cs (drop `new MapGenerationStep()` + its now-dead `step` local), MainMenuState.cs (drop `new FirstUIStep()`)")
    (:step-9 "TurnPhaseRunner.cs: RunAsync drops its TurnPhaseStep step parameter and the forwarded argument; TurnProcessorSystem.cs:80 drops `new TurnPhaseStep()`")
    (:step-10 "mcp__roslyn__get_diagnostics scoped to every touched project — clean before declaring done")
    (:step-11 "hand off to the user for the Unity-side compile + playtest check (agent never builds)"))
```

## Progress

```clojure
{:status :active
 :completed #{:step-1 :step-2 :step-3 :step-4 :step-5 :step-6 :step-7 :step-8 :step-9 :step-10}
 :current :step-11
 :remaining #{}
 :resume-context "All 31 files edited: IUniTaskSystem<in T>/IPrioritizedUniTaskSystem<in T> keep T, Update(CancellationToken) everywhere. mcp__roslyn__get_diagnostics on FantasyMayor.sln returns 0 errors and 17 pre-existing warnings, all in Assets/Plugins/Dreamteck (unrelated third-party code, none in a touched file). Only step-11 remains: the owner's own Unity compile + playtest check — the agent never builds Unity."}
```

## Acceptance

```clojure
[{:meter "mcp__roslyn__get_diagnostics (solutionPath FantasyMayor.sln)" :target "0 new diagnostics" :actual "0 errors; 17 pre-existing warnings, all in Assets/Plugins/Dreamteck, none in a touched file" :status :pass}
 {:meter "the owner's Unity-side check" :target "compiles and behaves" :actual ? :status :pending}]
```
