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
**canonical code names** to start a tool search from. One Clojure entry per CONCEPT — this file grows
by concepts (~1 entry per new mechanic), never by classes; the anchors feed `roslyn` / `ecsg.py` /
`dig.py`, which always return the current truth. Maintained by the main agent at milestone syncs (with the user's approval).

Format: one map — key = `"alias | alias | …"` (the human terms), value = `{:domain <owner> :anchors
[Name1 Name2 …]}`; anchors are bare type names, 2–4 per concept, verified against the graph before saving.

```clojure
(def glossary
  {"action points | AP | очки дій"                    {:domain Actors/Mayor                   :anchors [MayorAPComponent MayorAPRestoreComponent]}
   "AP restore per turn | відновлення ОД"             {:domain Actions                        :anchors [MayorAPRestoreSubSystem]}
   "mayor | мер"                                      {:domain Actors/Mayor                   :anchors [MayorIdComponent MayorConfig MayorSpawnSystem]}
   "city | місто"                                     {:domain Actors/City                    :anchors [CityIdComponent CityConfig CitySpawnSystem]}
   "city center | центр міста"                        {:domain Economy/DistrictBuildOutcome   :anchors [SpawnCityCenterOutcomeConfig SpawnCityCenterOutcomeSubSystem]}
   "district | район"                                 {:domain Economy/District               :anchors [DistrictIdAllocatorComponent]}
   "district build cost | вартість будівництва"       {:domain Economy/DistrictBuildCost      :anchors [DistrictBuildCostResourcePriceComponent]}
   "district open condition | умова відкриття району" {:domain Economy/DistrictOpenCondition  :anchors [DistrictExistConditionComponent DistrictOpenConditionEvaluatorSystem]}
   "district build UI | вікно будівництва"            {:domain Presentation.UI/DistrictBuild  :anchors [DistrictBuildUIViewComponent]}
   "turn | хід"                                       {:domain Modules/Turn                   :anchors [NextTurnEvent TurnCompletedEvent TurnPhaseStep]}
   "end turn button | кнопка кінця ходу"              {:domain Presentation.UI/EndTurn        :anchors [EndTurnViewComponent]}
   "resource (inventory) | ресурс"                    {:domain Economy/Resource               :anchors [ResourceComponent]}
   "resource bar | панель ресурсів"                   {:domain Presentation.UI/ResourceBar    :anchors [ResourceBarViewComponent]}
   "hex resource (natural) | природний ресурс"        {:domain Map/HexResources               :anchors [HexResourceComponent]}
   "hex icon overlay | іконки на гексах"              {:domain Presentation/HexIcons          :anchors [HexIconsVisibilityComponent HexIconsConfigComponent]}
   "hex info panel | панель інформації гекса"         {:domain Presentation.UI/HexInfoPanel   :anchors [HexInfoPanelViewComponent]}
   "hex selection | вибір гекса"                      {:domain Presentation.UI                :anchors [SelectedHexChangedEvent HexSelectedComponent]}})
```

Extending: add a line when a NEW concept enters the game (not a new class of an existing concept);
drop a line when the concept is removed. An anchor that stops resolving in the graph = drift — fix
the anchor, keep the term.
