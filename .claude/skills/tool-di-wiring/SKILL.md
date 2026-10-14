---
name: tool-di-wiring
description: "What a type is registered as (Lifetime, installer, As contracts, AppState), who injects it, what fills an IReadOnlyList<T>, which systems a game state runs — ask fmgraph (installer / consumers / resolve / state / unresolved), never read installers by hand: RegisterAppStateSystem and collection fill are generic wiring roslyn cannot classify."
user-invocable: false
---

# How this type is wired

```clojure
{:tool-skill "tool-di-wiring"
 :question "what is this type registered as, who injects it, what fills this collection, which systems run in this game state"
 :order "run fmgraph: installer <Installer> (what it registers, Lifetime, As, app_state); consumers <Type> (who injects it); resolve <Contract> (what fills IReadOnlyList<Contract>); state <GameState> (the systems whose AppState flags name its Mode); unresolved (injected, never registered)"
 :because "VContainer wiring lives in generic calls — Register<T>().As<C>(), RegisterAppStateSystem<T>(Lifetime, AppState.X), IReadOnlyList<T> injection — fmgraph reads them all in one pass"
 :invoke "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py state GameplayState"
 :example {:call "fmgraph.py state GameplayState"
           :returned "25 systems, each via AppState.Gameplay at its installer line (TurnCountSystem @ TurnInstaller.cs:24 …)"}
 :blind-spots #{"a registration form outside Register / RegisterAppStateSystem / RegisterInstance is a warning in check, not an edge"
                "a system's run ORDER inside a state is its Priority (fmgraph systems), not the runs_in edge"
                "MonoBehaviour [Inject] Construct methods on scene objects are read, but which scene holds the object is not — tool-where-asset-used"}
 :fallback "a targeted read of the installer fmgraph names; a grep only for a form fmgraph warns it does not follow, with that reason"}
```
