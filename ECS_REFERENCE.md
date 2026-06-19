---
category: B
read: trigger
trigger: "before writing an ECS query, entity table/join, or adding an archetype"
tags: [ecs, reference, archetypes]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# ECS_REFERENCE.md

Central registry of ECS state in FantasyMayor: every unique entity (archetype), every world
component, every event. **Do not load into context by default** — use for targeted lookup via
Graphify or grep.

Grep hints:
- Unique entities: `grep "ENTITY:" ECS_REFERENCE.md`
- World components: `grep "WORLD:" ECS_REFERENCE.md`
- Events: `grep "EVENT:" ECS_REFERENCE.md`
- Component producers: `grep "WRITES:" ECS_REFERENCE.md`
- Component consumers: `grep "READS:" ECS_REFERENCE.md`
- Table Rule violations: `grep "BARE-KEY" ECS_REFERENCE.md`

Storage taxonomy (entity table / world component / event entity / singleton entity) and the rules
for choosing between them: `ARCHITECTURE.md` → "State Storage Taxonomy".

---

## Modeling Rule — Normalization: Columns on the Hex vs a Separate Table

How you attach data to a hex is a deliberate choice. Use the DB-normalization analogy as the mental
model (an analogy, not a literal equality):

- **Entity ≈ a table row.** Its identity is the **primary key**.
- **Component ≈ a column** on that row.
- **`HexIdComponent` (its `HexCoord`) ≈ a foreign key.** Parallel entity sets carry it to point back
  at the hex they belong to. It is the ONLY link between the parallel sets — there is no
  parent/child reference.

Two ways to model per-hex data:

- **Approach A — a column on the hex row** (a component/tag on the hex entity itself). Use when the
  attribute is **single-valued and intrinsic to the cell (1:1)**: terrain level, terrain type tag.
- **Approach B — a separate table with a foreign key** (a dedicated entity set sharing the
  `HexCoord` via `HexIdComponent`). Use when the relationship is **1:N / multi-valued**, the count
  is variable (many trees on one forest hex), or the data has a **different lifecycle** than the hex
  (resources are added/chopped at runtime; views spawn/despawn reactively).

**Consequence — several parallel tables, not one list.** A hex is described across multiple sets
joined by the foreign key `HexIdComponent.Coords`: Hex, HexResource, ResourceView, HexIconContainer.

**Common mistake:** do not look for a resource as a component on the terrain-hex row. It is NOT
there — it lives in a separate table (entity set) with the same `HexCoord` foreign key.

### Table Rule — key + discriminator

A query MUST name its table: the key component PLUS the table's discriminator component. A bare
`With<HexIdComponent>` query is a cross-table UNION — it matches all four hex-keyed tables, NOT
"all hexes". Normative rule + the `EntityMap` / `EntityMultiMap` / `EntitySet` materializations:
`ARCHITECTURE.md` → "Relational Modeling — Table Rule". Violations are flagged
`⚠ BARE-KEY LEGACY` below and collected in the audit list at the end of this file.

---

## Entity Registry

Every unique entity that exists at runtime. `Tables` hold N rows; `Singletons` hold exactly one.
Event entities live in the Event Registry below.

### Tables (N rows)

```
ENTITY: Hex  (table — one row per map cell)
  Components: HexIdComponent (PK), HexLevelComponent, HexTag
              (+ Hex{Plain,Mount,Water,Bedhill}Tag added by SyncHexTags at the end of generation)
  Discriminator: HexTag
  WRITES: TerrainGenerationSystem (creates rows, pipeline 100; sets levels),
          Mountain/River/Lake/Sea GenerationSubSystem (mutate HexLevelComponent via Set),
          SyncHexTags (adds the type tag)
  READS (by table query With<HexTag> + With<HexIdComponent>):
          ForestResourceViewSubSystem, ForestSpawnSystem (square UV rect),
          HexInfoPanelSystem, HexInfoPanelHeaderSystem
  READS (by With<HexIdComponent> + With<HexLevelComponent>):
          TerrainGenerationSystem, Lake/Mountain/River/Sea GenerationSubSystem, TerrainViewDebugSystem
  READS (⚠ BARE-KEY LEGACY — see audit list):
          TerrainViewSystem, TerrainViewGenerationSubSystem, TerrainViewTextureSubSystem,
          WaterViewSubSystem, TerrainViewDebugSystem (primary), CameraMovementSystem,
          ClayResourceViewSubSystem
  Lifecycle: created once per map generation; never destroyed (no teardown/regeneration flow yet).
  Note: APPROACH A example — single-valued cell-intrinsic data lives directly on this entity.
        Resources do NOT (see Modeling Rule).

ENTITY: HexResource  (table — logical resource layer)
  Components: HexIdComponent (FK), HexResourcesComponent (carries ResourceType)
  Discriminator: HexResourcesComponent
  WRITES: HexResources generation subsystems (create rows, pipeline 200)
  READS: HexResourcesViewSubSystem base (EntitySet; per-type filter via GetTargetResourceEntities),
         ForestSpawnSystem / ForestDespawnSystem (EntityMultiMap<HexResourcesComponent> — the
         Forest bucket by discriminator value),
         HexIconsVisibilitySystem (EntitySet; per-hex match by Coords),
         HexInfoPanelResourcesSystem (EntitySet; selected-hex match)
  Lifecycle: created during generation; nothing destroys rows yet (future chop/deplete mechanics).
  Note: DEDICATED entity, NOT a component on the hex row. One per (hex, ResourceType): a hex with
        several resource kinds has SEVERAL rows sharing one HexCoord — the 1:N case that forces a
        separate table. HexResourcesComponent implements IEquatable (keys on its Type enum) so it
        can serve as a MultiMap key.

ENTITY: ResourceView  (table — visual layer)
  Components: HexIdComponent (FK), ForestViewComponent | FishViewComponent
  Discriminator: ForestViewComponent / FishViewComponent
  WRITES: ForestResourceViewSubSystem (startup bulk) and ForestSpawnSystem (runtime, reactive),
          both via the shared ForestPlanter helper; FishResourceViewSubSystem (scaffold — empty)
  DESTROYS: ForestDespawnSystem (reactive reconcile — destroys the GameObject and disposes the row)
  READS: ForestSpawnSystem / ForestDespawnSystem (EntityMultiMap<HexIdComponent> — views by hex)
  Lifecycle: forest rows created at startup (one-shot) and at runtime on a pulse; destroyed at
        runtime on a pulse. Dormant today — no emitter raises the pulses yet.
  Note: one row per spawned visual instance — a single forest hex spawns MANY tree rows (second 1:N
        normalization: resource→views). ForestViewComponent holds { ResourceType, ForestView } —
        the MonoBehaviour reference kept so the GameObject can be destroyed on despawn. No
        ground-splat data is stored: forest ground paint is append-only into the terrain texture and
        never rebuilt from entities. Clay creates NO view rows — it mutates VertexGrid + the terrain
        texture directly (ClayViewComponent is UNUSED). Fish is scaffold.

ENTITY: HexIconContainer  (table — UI overlay layer)
  Components: HexIdComponent (FK), HexIconContainerComponent
  Discriminator: HexIconContainerComponent
  WRITES: HexIconsSpawnSystem (creates one EMPTY container per hex, pipeline 700);
          HexIconsVisibilitySystem (clears/rebuilds the container's icons on the visibility event)
  READS: HexIconsContainerPositionSystem (base set — per-frame panel-space projection, LateUpdate),
         HexIconsVisibilitySystem (EntitySet — all containers, on event)
  Lifecycle: created once per map; containers persist, their icon CONTENT is cleared/refilled.
  Note: exactly one row per hex. Holds a managed UI Toolkit VisualElement (the per-hex icon
        container) inside the screen-space overlay. See HexIcons/HEXICONS.md.
```

### Singletons (exactly one row; justified by entity-query consumers)

```
ENTITY: HexSelected  (singleton)
  Components: HexSelectedComponent (carries HexCoord)
  WRITES: HexSelectionSystem (on click — creates/updates/disposes; raises SelectedHexChangedEvent on
          EVERY mutation)
  READS: HexSelectionViewSystem (selection highlight — per-frame poll), HexSelectionSystem (avoid
         re-selecting the same hex), and all SelectedHexChangedEvent reactors reconcile against it:
         HexInfoPanelSystem (show/hide) + the block systems Header/Resources/District (fill) +
         ContextTabsAvailabilitySystem (tab availability)
  Note: entity (not a world component) because consumers query its PRESENCE — the info panel hides
        when the set is empty. The paired SelectedHexChangedEvent is the canonical "selection changed"
        pulse; the truth lives HERE.

ENTITY: PlayerInput  (singleton)
  Components: PlayerInputComponent
  WRITES: UserInput installer (creates at composition)
  READS: CameraMovementSystem, HexSelectionSystem (tick anchor + input binding)

ENTITY: TerrainViewSingleton  (singleton)
  Components: TerrainViewComponent (ObjectRef → the TerrainView MonoBehaviour)
  WRITES: TerrainViewSystem (creates after the prefab loads; destroys + recreates on re-run)
  READS: TerrainViewSystem, ClayResourceViewSubSystem (EntitySet — mesh re-apply after depression)
  Note: stays an entity while EntitySet consumers exist.

ENTITY: WaterViewSingleton  (singleton)
  Components: WaterViewComponent
  WRITES: WaterViewSubSystem (creates; re-reads its own row on later runs)
  READS: WaterViewSubSystem

ENTITY: HexSelectionViewSingleton  (singleton)
  Components: HexSelectionViewComponent
  WRITES: HexSelectionViewLoadingSystem (pipeline 500, async prefab load)
  READS: HexSelectionViewSystem (base/anchor set)

ENTITY: HexInfoPanelView  (singleton)
  Components: HexInfoPanelViewComponent (View → the HexInfoPanelView MonoBehaviour)
  WRITES: HexInfoPanelSpawnSubSystem (run by MainUISpawnSystem at pipeline 800 — GetComponentInChildren off
          the shared UI/MainUI instance; the orchestrator owns the single addressable handle)
  READS: HexInfoPanelSystem (resolves the view to show/hide), HexInfoPanelHeaderSystem /
         HexInfoPanelResourcesSystem / HexInfoPanelDistrictPlaceholderSystem (resolve the view to fill
         their block) — all four anchored on the SelectedHexChangedEvent pulse (EntitySet)
  Note: the panel starts empty (HexInfoPanelSpawnSubSystem); on the SelectedHexChangedEvent pulse
        HexInfoPanelSystem reconciles show/hide and the block systems fill from the current
        HexSelectedComponent. No intermediary refresh event.

ENTITY: EndTurnView  (singleton)
  Components: EndTurnViewComponent (View → the EndTurnView MonoBehaviour)
  WRITES: EndTurnSpawnSubSystem (run by MainUISpawnSystem at pipeline 800 — GetComponentInChildren off the
          shared UI/MainUI instance; the orchestrator owns the single addressable handle)
  READS: EndTurnSystem (base/anchor set — per-frame Gameplay state mirror; also reads TurnCountComponent)
  Note: the cluster starts hidden; EndTurnSystem reveals it in Gameplay, reflects TurnProcessorComponent
        presence as the Processing look, and pushes TurnCountComponent.Value into "Хід N". The
        click→NextTurnEvent emit lives in EndTurnView (module MainUI).
```

---

## World Component Registry

World components are NOT entities: stored via `world.Set<T>()`, read via `world.Get<T>()` (guard
with `world.Has<T>()`), invisible to `With<T>` / `WhenAdded<T>` / `WhenChanged<T>`. Contract and
decision rule: `ARCHITECTURE.md` → "State Storage Taxonomy".

### Runtime world components (9)

```
WORLD: CameraComponent  (module Cameras)
  Fields: Camera (live scene camera) + ReferenceFieldOfView (authored startup FOV = neutral 1x zoom baseline)
  WRITES: WorldInstaller (world.Set at startup; ReferenceFieldOfView snapshotted from the camera)
  READS: CameraMovementSystem, HexSelectionSystem, HexIconsContainerPositionSystem (zoom baseline)

WORLD: VertexGridComponent  (module TerrainView)
  WRITES: TerrainViewConfigLoaderSystem (world.Set ONCE at ConfigLoadStep)
  READS: TerrainViewSystem, TerrainViewGenerationSubSystem, TerrainViewTextureSubSystem,
         HexSelectionViewSystem, HexResourcesViewSubSystem (TryGetVertexGrid Try-pattern),
         ForestSpawnSystem, HexIconsContainerPositionSystem
  Note: carries the shared VertexGrid (a REFERENCE type). The grid object is mutated IN PLACE by the
        generation pipeline (isolines/erosion) and Clay; world.Set is never re-called after a
        mutation — readers always see the live grid. Not reactive. Direct readers throw on a missing
        component (fail-loud); the Try-method reports the miss as a bool.

WORLD: TerrainTextureComponent  (module TerrainView)
  WRITES: TerrainViewTextureSubSystem (world.Set once in the view pipeline)
  READS: TerrainViewSystem (applies the Texture2D to the mesh material),
         ClayResourceViewSubSystem (clay gradient paint target),
         ForestResourceViewSubSystem / ForestSpawnSystem (forest ground paint target, append-only)
  Note: carries the generated terrain Texture2D (a REFERENCE type) — created readable
        (Apply(false)) precisely so it can be repainted later. Painters mutate pixels in place
        (SetPixels32 + Apply); the material keeps reflecting mutations without re-applying.
        PERSISTENT — never disposed.

WORLD: HexIconsViewComponent  (module HexIcons)
  WRITES: HexIconsSpawnSystem (the screen-space icon overlay view)
  READS: HexIconsContainerPositionSystem, HexIconsVisibilitySystem

WORLD: HexIconsVisibilityComponent  (module HexIcons)
  WRITES: GameplayState.EnterAsync (initial IsVisible = true); later a UI toggle
  READS: HexIconsVisibilitySystem (on a HexIconsVisibilityChangedEvent)
  Note: mutable bool "are per-hex icons shown" — the single source of truth. The paired event is a
        payload-less pulse; the state lives HERE, not in the event.

WORLD: TurnProcessorComponent  (module Turn)
  Fields: Status (Running | Completed)
  WRITES: TurnProcessorSystem (Set Running on a NextTurnEvent pulse; the background run Sets Completed
          AFTER SwitchToMainThread — pool computes, main thread writes)
  READS: TurnProcessorSystem (polls Status each frame; removes the component once Completed)
  Note: present ONLY while a turn is processed — its PRESENCE is the "turn in progress" gate, removed
        on completion. Doubles as the re-entry guard (further NextTurnEvent pulses ignored while set).
        SKELETON — zero phases today, so a started turn completes immediately.

WORLD: TurnCountComponent  (module Turn)
  Fields: Value (current turn number; immutable readonly struct — advance via world.Set)
  WRITES: GameplayState.EnterAsync (seed Value = 1 on Gameplay enter),
          TurnCountSystem (Set Value + 1 on each TurnCompletedEvent)
  READS: EndTurnSystem (module MainUI — pushes Value into the "Хід N" label; throws if unseeded)
  Note: the current-turn counter. First Mayor Phase = turn 1; TurnCountSystem (Priority 1010, above the
        processor's 1000) increments on the TurnCompletedEvent pulse the same frame it is emitted.

WORLD: ContextTabsViewComponent  (module MainUI, window ContextTabs)
  Fields: View (→ the ContextTabsView MonoBehaviour)
  WRITES: ContextTabsSpawnSubSystem (world.Set at pipeline 800 — GetComponentInChildren off the shared
          UI/MainUI instance)
  READS: ContextTabSelectionSystem (restyle on the tab-changed pulse),
         ContextTabsAvailabilitySystem (enable/disable on the SelectedHexChangedEvent pulse)
  Note: world singleton (this window has NO singleton entity) — same view-reference role as the
        entity-based EndTurnView/HexInfoPanelView, but stored on the world.

WORLD: ActiveContextTabComponent  (module MainUI, window ContextTabs)
  Fields: Value (ContextTab enum: Overview | Buildings | Actions; Unknown=0 sentinel)
  WRITES: ContextTabsSpawnSubSystem (seed Overview on spawn), ContextTabsView (World.Set on tab click)
  READS: ContextTabSelectionSystem (reconciles the active highlight on the tab-changed pulse)
  Note: which context tab is active. The view writes it on click then raises the payload-less
        ContextTabChangedEvent; the selection system reconciles the highlight from HERE.
```

### Config world components (20)

All written ONCE by their module's Config Loader at `ConfigLoadStep` (`CONFIGTEMPLATE.md`).
Reader lists name the consuming systems.

```
WORLD: TerrainGenerationConfigComponent  (TerrainGenerator)
  WRITES: TerrainGenerationConfigLoaderSystem
  READS: TerrainGenerationSystem, Lake/River/Sea GenerationSubSystem

WORLD: LakeConfigComponent / MountainConfigComponent / RiverConfigComponent / SeaConfigComponent  (TerrainGenerator)
  WRITES: TerrainGenerator config loaders
  READS: the matching GenerationSubSystem

WORLD: TerrainViewConfigComponent  (TerrainView)
  WRITES: TerrainViewConfigLoaderSystem
  READS: TerrainViewSystem, TerrainViewGenerationSubSystem, TerrainViewTextureSubSystem,
         WaterViewSubSystem, TerrainViewDebugSystem, ClayResourceViewSubSystem,
         ForestResourceViewSubSystem, ForestSpawnSystem (CellSize),
         CameraMovementSystem, HexSelectionSystem (cross-module — see Cross-Module Reads)

WORLD: TerrainTextureConfigComponent / InnerIsolineConfigComponent / OuterIsolineConfigComponent /
       HeightSmoothingConfigComponent / HydraulicErosionConfigComponent / WindErosionConfigComponent  (TerrainView)
  WRITES: TerrainViewConfigLoaderSystem
  READS: TerrainViewTextureSubSystem (texture config); TerrainViewGenerationSubSystem (the rest)

WORLD: WaterViewConfigComponent  (TerrainView)
  WRITES: TerrainViewConfigLoaderSystem
  READS: WaterViewSubSystem

WORLD: CameraMovementConfigComponent  (UserInput)
  WRITES: UserInput config loader
  READS: CameraMovementSystem

WORLD: HexResourcesConfigComponent  (HexResources)
  WRITES: HexResourcesConfigLoaderSystem
  READS: HexResourcesSubSystem

WORLD: HexResourcesViewConfigComponent  (HexResourcesView)
  WRITES: HexResourcesViewConfigLoaderSystem
  READS: HexResourcesViewSubSystem base (prefab lookup), ForestResourceViewSubSystem, ForestSpawnSystem

WORLD: ClayViewConfigComponent  (HexResourcesView)
  WRITES: ClayViewConfigLoaderSystem (flattened from ClayViewConfig SO; SO not retained)
  READS: ClayResourceViewSubSystem
  Note: dedicated clay config — clay is prefab-less and does NOT live in HexResourcesViewConfig
        (whose entries require a prefab). Carries the footprint and the center/rim clay colors.

WORLD: HexIconsConfigComponent  (HexIcons)
  WRITES: HexIconsConfigLoaderSystem
  READS: HexIconsContainerPositionSystem, HexIconsVisibilitySystem

WORLD: HexResourceIconConfigComponent  (HexIcons)
  WRITES: HexIconsConfigLoaderSystem
  READS: HexIconsVisibilitySystem (icon rebuild), HexInfoPanelResourcesSystem (cross-module —
         resource rows in the info panel)

WORLD: HexTerrainIconConfigComponent  (HexesUI)
  WRITES: HexTerrainIconConfigLoaderSystem
  READS: HexInfoPanelHeaderSystem (terrain icon + name in the panel header)
```

---

## Event Registry

One-frame event entities: a marker component + `EventTag`, disposed by `EventCleanupSystem`
(`Priority = int.MaxValue`, runs LAST in every active state). Contract: events are **payload-less
pulses**; the consumer reconciles against current world state (idempotent) — see `ARCHITECTURE.md`
→ "Decomposition Rules". Events DO NOT survive the async `MapCreation` pipeline — never use them
for startup orchestration.

```
EVENT: TerrainGenerationGenerateEventComponent
  Producer: HexesUI Generate button (later: save/load)
  Consumer: MainMenu game state — detects it and switches to MapCreation (which runs the pipeline)
  Lifetime: 1 frame (cleaned once MapCreation ticks EventCleanupSystem)
  Note: the ONLY world-init trigger. Naming predates the …Event suffix rule — kept until a
        deliberate rename.

EVENT: HexIconsVisibilityChangedEvent
  Producer: GameplayState.EnterAsync (writes HexIconsVisibilityComponent, then raises this);
            later a UI toggle
  Consumer: HexIconsVisibilitySystem — clears every HexIconContainer, rebuilds icons from each
            hex's resources if visible
  Lifetime: 1 frame
  Note: payload-less pulse — the truth is the HexIconsVisibilityComponent WORLD component.

EVENT: SelectedHexChangedEvent
  Producer: HexSelectionSystem (module UserInput) — raised on EVERY selection mutation: create,
            deselect (dispose), and re-select to another coord
  Consumers: HexInfoPanelSystem (550 — show/hide), HexInfoPanelHeaderSystem (560),
             HexInfoPanelResourcesSystem (561), HexInfoPanelDistrictPlaceholderSystem (562) — fill the
             panel blocks; ContextTabsAvailabilitySystem (560) — per-tab enabled state
  Lifetime: 1 frame
  Note: the canonical "selection changed" pulse — payload-less; every consumer reads the current
        HexSelectedComponent (present/value), reconciling its block. Replaced both the consumers' former
        per-frame polling AND the old HexInfoPanelRefreshEvent (which only re-broadcast this with a Coords
        payload).

EVENT: ContextTabChangedEvent
  Producer: ContextTabsView (module MainUI, window ContextTabs) — on a tab click, after it writes the
            new ActiveContextTabComponent
  Consumer: ContextTabSelectionSystem (560) — reconciles the active-tab highlight from
            ActiveContextTabComponent
  Lifetime: 1 frame
  Note: payload-less pulse — the active tab lives in the ActiveContextTabComponent world singleton.

EVENT: ForestHexAppearedEvent / ForestHexRemovedEvent
  Producer: NO emitter yet (future gameplay: planting / chopping)
  Consumers: ForestSpawnSystem (Appeared) / ForestDespawnSystem (Removed) — each reconciles forest
             ResourceView rows against current HexResource state (spawn missing / destroy orphaned;
             ground paint append-only, never reverted)
  Lifetime: 1 frame
  Note: DORMANT scaffold. The startup forest is built one-shot by ForestResourceViewSubSystem —
        these pulses cover runtime changes only.

EVENT: NextTurnEvent
  Producer: EndTurnView (module MainUI) — the End Turn button creates NextTurnEvent + EventTag on click
            (future: AI turn advance may also emit)
  Consumer: TurnProcessorSystem — starts a turn: Set TurnProcessorComponent, run phases off-thread
  Lifetime: 1 frame
  Note: Consumed by a PER-FRAME poller (queries With<NextTurnEvent>), NOT a WhenAdded reactive set.
        Ignored while a turn is already in progress (re-entry guard on TurnProcessorComponent).

EVENT: TurnCompletedEvent
  Producer: TurnProcessorSystem (module Turn) — raised when a turn resolves, the same frame it removes
            TurnProcessorComponent
  Consumer: TurnCountSystem (Priority 1010, > processor's 1000) — increments TurnCountComponent
  Lifetime: 1 frame
  Note: the reusable "a turn just finished" pulse — decouples turn-boundary reactors from the processor's
        completion check. Future turn-boundary systems subscribe here too.

EVENT: EventTag
  Producer: any system emitting an event (Set on the event entity alongside the event component)
  Consumer: EventCleanupSystem (disposes the entity at end of tick)
  Lifetime: 1 frame
  Note: the universal cleanup marker — every event entity above carries it.
```

---

## Bare-Key Legacy Audit

Bare `With<HexIdComponent>` queries violating the Table Rule (they match all four hex-keyed tables).
Written before the parallel tables existed; pending audit/fix. Do NOT copy this pattern.

```
⚠ BARE-KEY LEGACY (7):
  TerrainViewSystem            — hexSet
  TerrainViewGenerationSubSystem — hexSet
  TerrainViewTextureSubSystem  — hexSet
  WaterViewSubSystem           — hexSet
  TerrainViewDebugSystem       — primary set
  CameraMovementSystem         — hexIdSet
  ClayResourceViewSubSystem    — hexSet (square UV rect)
```

---

## Data Flow by Phase

```
ConfigLoadStep (one-time boot bootstrap, before the state machine)
  → all Config Loaders → world.Set config WORLD components (20)
  → TerrainViewConfigLoaderSystem additionally → world.Set VertexGridComponent (runtime WORLD component)

MainMenu state
  → ShowHexesUISystem → loads + shows the HexGeneratorUI (Generate button)
  → Generate click → TerrainGenerationGenerateEventComponent → state switch to MapCreation

World-init pipeline (run by the MapCreation state; IPrioritizedUniTaskSystem<TerrainGenerationStep>
stages awaited sequentially in ascending priority, then settle frames, then switch to Gameplay):

  → TerrainGenerationSystem (100)        → creates Hex rows (HexIdComponent + HexLevelComponent)
      → Mountain / River / Lake / Sea GenerationSubSystem → mutate HexLevelComponent
      → SyncHexTags → adds Hex{Plain,Mount,Water,Bedhill}Tag from the final level
  → HexResourcesSystem (200)             → generation subsystems create HexResource rows
  → TerrainViewSystem (300, orchestrator, async):
      → TerrainViewGenerationSubSystem   → mutates VertexGrid in place
      → TerrainViewTextureSubSystem      → world.Set TerrainTextureComponent (runtime WORLD component)
      → WaterViewSubSystem               → creates WaterViewSingleton
  → HexResourcesViewSystem (400, orchestrator):
      → ClayResourceViewSubSystem (200)  → VertexGrid depression + clay texture gradient
      → FishResourceViewSubSystem (300)  → scaffold
      → ForestResourceViewSubSystem (400)→ plants ALL forest hexes (ResourceView rows) + paints
                                            green ground once (append-only, on top of clay)
  → HexSelectionViewLoadingSystem (500)  → creates HexSelectionViewSingleton (async prefab load)
  → TerrainViewDebugSystem (600)         → debug rays per hex level
  → HexIconsSpawnSystem (700)            → world.Set HexIconsViewComponent + creates EMPTY
                                            HexIconContainer rows (one per hex)
  → MainUISpawnSystem (800)              → instantiates the Main UI root, then runs spawn subsystems:
                                            HexInfoPanelView + EndTurnView entity singletons (both hidden)
                                            + ContextTabs view/active-tab WORLD singletons (Overview seeded)

Gameplay state entry (GameplayState.EnterAsync):
  → world.Set HexIconsVisibilityComponent (IsVisible = true)
  → raises HexIconsVisibilityChangedEvent → consumed on the first Gameplay tick
  → world.Set TurnCountComponent (Value = 1) → the turn cluster shows "Хід 1" from the first tick

Gameplay state, per-frame (Update, ascending priority):
  → HexSelectionSystem (0) / HexSelectionViewSystem     → selection write (raises SelectedHexChangedEvent
                                                          on every mutation) + highlight (poll)
  → HexInfoPanelSystem (550, reactive on SelectedHexChangedEvent) → show/hide (ShowSelection/ShowEmpty)
  → HexInfoPanelHeader/Resources/DistrictPlaceholder (560–562, reactive on SelectedHexChangedEvent)
                                                          → fill panel blocks, reading HexSelectedComponent
  → ForestSpawnSystem (600) / ForestDespawnSystem (601, reactive) → reconcile forest views on a pulse (dormant)
  → HexIconsVisibilitySystem (800, reactive)           → clear/rebuild icon containers on the pulse
  → EndTurnSystem (560)                                → reveals the turn cluster; mirrors Processing;
                                                          pushes TurnCountComponent.Value into "Хід N"
  → ContextTabSelectionSystem (560, reactive on ContextTabChangedEvent) → restyles the active tab
  → ContextTabsAvailabilitySystem (560, reactive on SelectedHexChangedEvent) → reconciles per-tab enabled
  → TurnProcessorSystem (1000)                         → polls the in-flight turn; on completion removes
                                                          TurnProcessorComponent + raises TurnCompletedEvent
  → TurnCountSystem (1010, reactive)                   → increments TurnCountComponent on that pulse
  → EventCleanupSystem (int.MaxValue)                  → disposes all EventTag entities

Gameplay state, per-frame (LateUpdate):
  → CameraMovementSystem (0) → HexIconsContainerPositionSystem (700) — containers track the camera
    without a one-frame lag
```

---

## Cross-Module Component Reads

Components read outside their owner module — potential coupling points.

```
TerrainViewConfigComponent     read by UserInput (CameraMovementSystem, HexSelectionSystem)
                               → CellSize used for raycast plane and camera bounds
HexIdComponent                 read by HexResourcesView, HexIcons, HexesUI
                               → the universal hex foreign key (joins across all hex-keyed tables)
HexResourcesComponent          read by HexIcons (icon rebuild), HexesUI (info panel resource rows)
VertexGridComponent            read by HexResourcesView (planting heights), HexIcons (projection),
                               HexSelectionViewSystem (selection ring)
TerrainTextureComponent        read by HexResourcesView (clay + forest ground painting)
HexResourceIconConfigComponent read by HexesUI (HexInfoPanelResourcesSystem — shared icon set)
HexSelectedComponent           read by HexesUI (HexInfoPanelSystem + Header/Resources/District block
                               systems + ContextTabsAvailabilitySystem) and TerrainView (HexSelectionViewSystem)
```
