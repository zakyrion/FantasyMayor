# Tool skills

The project's generated tool skills — one per kind of question. Written by sdd-tool-skills, owned by the project;
`update` and `uninstall` of sdd-flow never touch these files.

```clojure
[{:skill "tool-who-calls"
  :question "who calls, references, implements or derives from a C# symbol"
  :tools [roslyn]
  :paths [".claude/skills/tool-who-calls/SKILL.md"]
  :verified-at "2026-09-23"
  :verified-by "mcp__roslyn__find_references EventLog → 9 refs (EventReader._log, EntityStorages.Events, ctor, 5 crefs)"
  :status :active}

 {:skill "tool-who-writes-component"
  :question "who writes / reads / removes a component, who raises and who reacts to an event, what an archetype holds"
  :tools [fantasymayor-graph]
  :paths [".claude/skills/tool-who-writes-component/SKILL.md"]
  :verified-at "2026-09-23"
  :verified-by "fmgraph.py neighbors NextTurnEvent --rel emits,reacts_to,consumes → <-emits- TurnPanelView via Events.Raise"
  :status :active}

 {:skill "tool-di-wiring"
  :question "what a type is registered as, who injects it, what fills a collection, which systems a game state runs"
  :tools [fantasymayor-graph]
  :paths [".claude/skills/tool-di-wiring/SKILL.md"]
  :verified-at "2026-09-23"
  :verified-by "fmgraph.py state GameplayState → 25 systems via AppState.Gameplay (after the RegisterAppStateSystem reading landed the same day)"
  :status :active}

 {:skill "tool-code-name-of-term"
  :question "which classes stand behind a game-design word the owner used"
  :tools [GLOSSARY.md roslyn]
  :paths [".claude/skills/tool-code-name-of-term/SKILL.md"]
  :verified-at "2026-09-23"
  :verified-by "GLOSSARY.md «turn | хід» → [NextTurnEvent TurnCompletedEvent TurnPhaseStep]"
  :status :active}

 {:skill "tool-recipe-instances"
  :question "which recipe a type follows, its live instances and deviations, the systems of one role"
  :tools [fantasymayor-graph]
  :paths [".claude/skills/tool-recipe-instances/SKILL.md"]
  :verified-at "2026-09-23"
  :verified-by "fmgraph.py systems --role reactive → 18 systems (501 HexSelectionViewSystem …)"
  :status :active}

 {:skill "tool-can-assembly-see"
  :question "can an assembly compile against a type, where the type lives, how two assemblies connect"
  :tools [asmdef_reach]
  :paths [".claude/skills/tool-can-assembly-see/SKILL.md"]
  :verified-at "2026-09-23"
  :verified-by "asmdef_reach.py can Presentation EventReader → YES, direct reference (Ecs.Extensions)"
  :status :active}

 {:skill "tool-where-asset-used"
  :question "where an asset is used, who loads it from code, why it ships"
  :tools [roslyn unity-mcp unity-asset-graph]
  :paths [".claude/skills/tool-where-asset-used/SKILL.md"]
  :verified-at "2026-09-23"
  :verified-by "find_references ConfigAddresses.CITY_CONFIG → ActorsInstaller.cs:17; mcp__unity__find_assets CityConfig → Assets/Addressables/Configs/Economy/CityConfig.asset; uag.py why HexIconsView.prefab → addressables → HexIconsConfig.asset → prefab"
  :status :active}]
```
