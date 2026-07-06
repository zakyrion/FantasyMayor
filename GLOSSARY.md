---
category: A
read: trigger
trigger: "when a domain term (any language) needs its canonical code name before searching roslyn / ecs-graph / di-graph"
tags: [glossary, vocabulary, navigation]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[INDEX](INDEX.md)"
status: partial
---

# GLOSSARY — domain vocabulary → code anchors

Map from human vocabulary (game-design terms, Ukrainian/English synonyms, abbreviations) to the
**canonical code names** to start a tool search from. One s-expr line per CONCEPT — this file grows
by concepts (~1 line per new mechanic), never by classes; the anchors feed `roslyn` / `ecsg.py` /
`dig.py`, which always return the current truth. Maintained by the docs-curator at milestone syncs.

Format: `(term "alias | alias | …" :domain <owner> → [Name1 Name2 …])` — one canonical action
rule per line (subject `term` with its aliases, `:domain` narrows, the verdict is the anchor
vector); anchors are bare type names, 2–4 per concept, verified against the graph before saving.

```lisp
(term "action points | AP | очки дій"        :domain Actors/Mayor              → [MayorAPComponent MayorAPRestoreComponent])
(term "AP restore per turn | відновлення ОД" :domain Actions                   → [MayorAPRestoreSubSystem])
(term "mayor | мер"                          :domain Actors/Mayor              → [MayorIdComponent MayorConfig MayorSpawnSystem])
(term "city | місто"                         :domain Actors/City               → [CityIdComponent CityConfig CitySpawnSystem])
(term "city center | центр міста"            :domain Economy/DistrictBuildOutcome → [SpawnCityCenterOutcomeConfig SpawnCityCenterOutcomeSubSystem])
(term "district | район"                     :domain Economy/District          → [DistrictIdAllocatorComponent])
(term "district build cost | вартість будівництва" :domain Economy/DistrictBuildCost → [DistrictBuildCostResourcePriceComponent])
(term "district open condition | умова відкриття району" :domain Economy/DistrictOpenCondition → [DistrictExistConditionComponent DistrictOpenConditionEvaluatorSystem])
(term "district build UI | вікно будівництва" :domain Presentation.UI/DistrictBuild → [DistrictBuildUIViewComponent])
(term "turn | хід"                           :domain Modules/Turn              → [NextTurnEvent TurnCompletedEvent TurnPhaseStep])
(term "end turn button | кнопка кінця ходу"  :domain Presentation.UI/EndTurn   → [EndTurnViewComponent])
(term "resource (inventory) | ресурс"        :domain Economy/Resource          → [ResourceComponent])
(term "resource bar | панель ресурсів"       :domain Presentation.UI/ResourceBar → [ResourceBarViewComponent])
(term "hex resource (natural) | природний ресурс" :domain Map/HexResources     → [HexResourceComponent])
(term "hex icon overlay | іконки на гексах"  :domain Presentation/HexIcons     → [HexIconsVisibilityComponent HexIconsConfigComponent])
(term "hex info panel | панель інформації гекса" :domain Presentation.UI/HexInfoPanel → [HexInfoPanelViewComponent])
(term "hex selection | вибір гекса"          :domain Presentation.UI           → [SelectedHexChangedEvent HexSelectedComponent])
```

Extending: add a line when a NEW concept enters the game (not a new class of an existing concept);
drop a line when the concept is removed. An anchor that stops resolving in the graph = drift — fix
the anchor, keep the term.
