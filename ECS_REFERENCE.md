# ECS_REFERENCE.md

Reference for ECS and DoD architecture in FantasyMayor.
**Do not load into context by default** — use for targeted lookup via Graphify or grep.

Grep hints:
- Component consumers: `grep "READS:" ECS_REFERENCE.md`
- Component producers: `grep "WRITES:" ECS_REFERENCE.md`
- Entity archetypes: `grep "ARCHETYPE:" ECS_REFERENCE.md`
- System queries: `grep "QUERY:" ECS_REFERENCE.md`

---

## Modeling Rule — Normalization: Columns on the Hex vs a Separate Table

How you attach data to a hex is a deliberate choice. Use the DB-normalization analogy as the mental model
(an analogy, not a literal equality):

- **Entity ≈ a table row.** Its identity is the **primary key**.
- **Component ≈ a column** on that row.
- **`HexIdComponent` (its `HexCoord`) ≈ a foreign key.** Parallel entity sets carry it to point back at the
  hex they belong to. It is the ONLY link between the parallel sets — there is no parent/child reference.

Two ways to model per-hex data:

- **Approach A — a column on the hex row** (a component/tag on the hex entity itself). Use when the
  attribute is **single-valued and intrinsic to the cell (1:1)**: terrain level, terrain type tag. The
  identity is the hex itself. Terrain hexes use this.
- **Approach B — a separate table with a foreign key** (a dedicated entity set that shares the `HexCoord`
  via `HexIdComponent`). Use when the relationship is **1:N / multi-valued**:
  - more than one instance of the same kind of data can sit on one hex (you cannot put two columns of the
    same type on one row), or
  - the count is variable/many (many trees on one forest hex), or
  - the data has a **different lifecycle** than the hex (a resource is added/chopped at runtime; views
    spawn/despawn reactively).

  This is normalization: a repeating group is pulled out into its own entity set instead of being crammed
  onto the hex row.

**Consequence — several parallel tables, not one list.** A hex is described across multiple sets joined by
the foreign key `HexIdComponent.Coords`: the terrain-hex set, the logical-resource set (`HexResource`), the
visual set (`ResourceView`), and the UI-overlay set (`HexIconContainer`).

**Common mistake:** do not look for a resource as a column (component) on the terrain-hex row. It is NOT
there — it lives in a separate table (entity set) with the same `HexCoord` foreign key.

---

## Entity Archetypes

Each archetype is a stable component combination that exists at runtime.

```
ARCHETYPE: Hex
  Components: HexIdComponent, HexLevelComponent, HexTag
               (+ Hex{Plain,Mount,Water,Bedhill}Tag added later by SyncHexTags in the pipeline)
  Owner: TerrainGenerationSystem (creates), HexesCore (defines)
  Note: One entity per hex cell. Core data entity — read by almost every terrain system.
        APPROACH A example: single-valued, cell-intrinsic data (HexTag + type tag) lives as
        components/tags directly ON this entity. Resources do NOT — see the Modeling Rule above.

ARCHETYPE: PlayerInput
  Components: PlayerInputComponent
  Owner: UserInput module

ARCHETYPE: SelectedHex
  Components: SelectedHexComponent
  Owner: HexSelectionSystem (creates/updates), one entity max
  Note: Recreated on each selection change.

ARCHETYPE: VertexGridSingleton
  Components: VertexGridComponent
  Owner: TerrainViewConfigLoaderSystem (creates)
  Note: Singleton. Shared fine vertex grid used by all terrain view subsystems.

ARCHETYPE: TerrainViewSingleton
  Components: TerrainViewComponent
  Owner: TerrainViewSystem (creates after the prefab loads)
  Note: TerrainViewConfigComponent is NOT here anymore — configs are world components (see below).

ARCHETYPE: HexSelectionViewSingleton
  Components: HexSelectionViewComponent
  Owner: HexSelectionViewLoadingSystem

ARCHETYPE: WaterViewSingleton
  Components: WaterViewComponent
  Owner: WaterViewSubSystem
  Note: WaterViewConfigComponent is NOT here anymore — configs are world components (see below).

// --- Resource entities (APPROACH B: parallel tables, joined to the hex by HexCoord FK) ---
// Two normalized layers sit beside the terrain hex, never on it: the logical-resource layer
// (HexResource) and the visual layer (ResourceView). Both carry HexIdComponent as the foreign
// key back to the hex. See the Modeling Rule above.

ARCHETYPE: HexResource
  Components: HexIdComponent, HexResourcesComponent
  Owner: HexResources module
  Note: LOGICAL-RESOURCE layer. DEDICATED entity, NOT a component on the terrain hex entity —
        a separate row that shares the hex's HexCoord (the foreign key). One per (hex, ResourceType),
        so a hex carrying several resource kinds has SEVERAL HexResource entities with the same
        HexCoord — exactly the 1:N case that forces a separate table. Lifecycle is independent of
        the hex (a resource can be added/chopped at runtime).

ARCHETYPE: ResourceView
  Components: HexIdComponent, {Forest|Fish}ViewComponent
  Owner: ForestViewSyncSystem (Forest, reactive); FishResourceViewSubSystem (Fish, scaffold)
  Note: VISUAL layer — one row per spawned visual instance, sharing the hex's HexCoord (FK).
        A single HexResource can spawn MANY ResourceView entities (one forest hex → several tree
        instances), so multiple ResourceView rows share one HexCoord — a second 1:N normalization,
        this time resource→views. Forest entities are created/destroyed REACTIVELY by
        ForestViewSyncSystem as forest HexResource entities appear/vanish. Fish scaffold.
        Clay creates NO view entities (the visual table is empty for clay): it mutates the shared
        VertexGrid (depression) and the terrain texture (clay gradient) directly — see
        HexResourcesView/HEXRESOURCESVIEW.md.

ARCHETYPE: HexIconContainer
  Components: HexIdComponent, HexIconContainerComponent
  Owner: HexIconsSpawnSystem (creates one EMPTY container per hex, eagerly);
         HexIconsContainerPositionSystem (positions); HexIconsVisibilitySystem (fills/clears icons on event)
  Note: UI OVERLAY layer — APPROACH B parallel table joined to the hex by HexCoord FK, exactly one row per
        hex. Holds a managed UI Toolkit VisualElement (the per-hex icon container) inside the screen-space
        overlay. HexIconsContainerPositionSystem iterates this set every frame (Gameplay) and projects each
        hex's vertex-grid center to panel space. See HexIcons/HEXICONS.md.

ARCHETYPE: HexIconsVisibilityChangedEvent (one-frame event)
  Components: HexIconsVisibilityChangedEvent, EventTag
  Owner: GameplayState.EnterAsync (creates it; later a UI toggle); EventCleanupSystem disposes it
  Note: payload-less "re-render icons" signal. Consumed by HexIconsVisibilitySystem, which reads the
        HexIconsVisibilityComponent WORLD component for the actual show/hide state (variant B).

// --- Config components are WORLD components, NOT entities (there is NO ConfigSingleton archetype) ---
// Stored via world.Set<TConfigComponent>(), read via world.Get<T>() (guard with world.Has<T>()).
// A world component is not an entity: it never appears in world.GetEntities() and cannot be matched
// by With<T> / WhenAdded<T> / WhenChanged<T>. See ARCHITECTURE.md "Config Component Storage" and
// CONFIGTEMPLATE.md. The 17 world config components:
//   TerrainGenerationConfigComponent, TerrainViewConfigComponent, TerrainTextureConfigComponent,
//   InnerIsolineConfigComponent, OuterIsolineConfigComponent, HeightSmoothingConfigComponent,
//   HydraulicErosionConfigComponent, WindErosionConfigComponent, WaterViewConfigComponent,
//   CameraMovementConfigComponent, LakeConfigComponent, MountainConfigComponent,
//   RiverConfigComponent, SeaConfigComponent, HexResourcesConfigComponent,
//   HexResourcesViewConfigComponent, ClayViewConfigComponent
//
// Non-config world components (same world.Set / world.Get storage, not entities):
//   CameraComponent (module Cameras) — the active scene camera, set by WorldInstaller.
//   HexIconsViewComponent (module HexIcons) — the screen-space icon overlay view, set by HexIconsSpawnSystem.
//   HexIconsVisibilityComponent (module HexIcons) — mutable bool, "are icons shown"; set by GameplayState
//     (and later UI), read by HexIconsVisibilitySystem.
```

---

## Component Registry

Each component with all known readers and writers.

```
COMPONENT: HexIdComponent
  Module: HexesCore
  WRITES: TerrainGenerationSystem (initial creation)
  READS: LakeGenerationSubSystem, MountainGenerationSubSystem, RiverGenerationSubSystem,
         SeaGenerationSubSystem, TerrainGenerationSystem, TerrainViewSystem,
         TerrainViewGenerationSubSystem, TerrainViewTextureSubSystem,
         WaterViewSubSystem, TerrainViewDebugSystem, CameraMovementSystem,
         HexResourcesViewSubSystem
  Note: Present on every hex entity. Acts as identity marker.

COMPONENT: HexLevelComponent
  Module: HexesCore / TerrainGenerator
  WRITES: TerrainGenerationSystem (sets levels during generation)
  READS: LakeGenerationSubSystem, MountainGenerationSubSystem, RiverGenerationSubSystem,
         SeaGenerationSubSystem, TerrainGenerationSystem, TerrainViewDebugSystem
  Note: Carries terrain height/level data per hex.

COMPONENT: SelectedHexComponent
  Module: UserInput
  WRITES: HexSelectionSystem (on click — sets HexCoord)
  READS: HexSelectionViewSystem, HexSelectionSystem
  Note: Singleton entity. HexSelectionSystem also reads it to avoid re-selecting same hex.

COMPONENT: VertexGridComponent
  Module: TerrainView
  WRITES: TerrainViewConfigLoaderSystem (creates singleton)
  READS: TerrainViewSystem, TerrainViewGenerationSubSystem,
         TerrainViewTextureSubSystem, HexSelectionViewSystem
  Note: Singleton. Contains the shared VertexGrid struct.

COMPONENT: TerrainViewConfigComponent
  Module: TerrainView
  WRITES: TerrainViewConfigLoaderSystem
  READS: TerrainViewSystem, TerrainViewGenerationSubSystem,
         TerrainViewTextureSubSystem, WaterViewSubSystem,
         TerrainViewDebugSystem, CameraMovementSystem, HexSelectionSystem
  Note: Cross-module read — UserInput reads CellSize for raycast plane size.

COMPONENT: TerrainTextureComponent
  Module: TerrainView
  WRITES: TerrainViewTextureSubSystem (creates, publishes)
  READS: TerrainViewSystem (applies Texture2D to material, keeps entity alive),
         ForestViewSyncSystem / ForestGroundPainter (mutate the Texture2D pixels at runtime)
  Lifetime: PERSISTENT — kept alive past world-init; the same readable Texture2D instance
            stays on the material so reactive systems can repaint it (SetPixels32 + Apply)

COMPONENT: TerrainViewComponent
  Module: TerrainView
  WRITES: TerrainViewSystem (creates, holds ObjectRef to MonoBehaviour)
  READS: TerrainViewSystem

COMPONENT: HexSelectionViewComponent
  Module: TerrainView
  WRITES: HexSelectionViewLoadingSystem
  READS: HexSelectionViewSystem

COMPONENT: InnerIsolineConfigComponent
  Module: TerrainView
  WRITES: TerrainViewConfigLoaderSystem
  READS: TerrainViewGenerationSubSystem

COMPONENT: OuterIsolineConfigComponent
  Module: TerrainView
  WRITES: TerrainViewConfigLoaderSystem
  READS: TerrainViewGenerationSubSystem

COMPONENT: HeightSmoothingConfigComponent
  Module: TerrainView
  WRITES: TerrainViewConfigLoaderSystem
  READS: TerrainViewGenerationSubSystem

COMPONENT: HydraulicErosionConfigComponent
  Module: TerrainView
  WRITES: TerrainViewConfigLoaderSystem
  READS: TerrainViewGenerationSubSystem

COMPONENT: WindErosionConfigComponent
  Module: TerrainView
  WRITES: TerrainViewConfigLoaderSystem
  READS: TerrainViewGenerationSubSystem

COMPONENT: WaterViewConfigComponent
  Module: TerrainView
  WRITES: TerrainViewConfigLoaderSystem
  READS: WaterViewSubSystem

COMPONENT: WaterViewComponent
  Module: TerrainView
  WRITES: WaterViewSubSystem
  READS: WaterViewSubSystem

COMPONENT: CameraComponent
  Module: Cameras
  Storage: WORLD component (world.Set / world.Get), NOT an entity
  WRITES: WorldInstaller (world.Set at startup)
  READS: CameraMovementSystem, HexSelectionSystem, HexIconsSpawnSystem (all via world.Get)

COMPONENT: PlayerInputComponent
  Module: UserInput
  WRITES: UserInput installer
  READS: CameraMovementSystem, HexSelectionSystem

COMPONENT: CameraMovementConfigComponent
  Module: UserInput
  WRITES: UserInput config loader
  READS: CameraMovementSystem

COMPONENT: TerrainGenerationConfigComponent
  Module: TerrainGenerator
  WRITES: TerrainGenerator config loader
  READS: TerrainGenerationSystem, LakeGenerationSubSystem,
         RiverGenerationSubSystem, SeaGenerationSubSystem

COMPONENT: LakeConfigComponent
  Module: TerrainGenerator
  WRITES: TerrainGenerator config loader
  READS: LakeGenerationSubSystem

COMPONENT: MountainConfigComponent
  Module: TerrainGenerator
  WRITES: TerrainGenerator config loader
  READS: MountainGenerationSubSystem

COMPONENT: RiverConfigComponent
  Module: TerrainGenerator
  WRITES: TerrainGenerator config loader
  READS: RiverGenerationSubSystem

COMPONENT: SeaConfigComponent
  Module: TerrainGenerator
  WRITES: TerrainGenerator config loader
  READS: SeaGenerationSubSystem

COMPONENT: HexResourcesComponent
  Module: HexResources
  WRITES: HexResources generation subsystems
  READS: HexResourcesViewSubSystem
  Note: Lives on a DEDICATED resource entity (HexIdComponent + HexResourcesComponent),
        not on the terrain hex entity.

COMPONENT: HexResourcesConfigComponent
  Module: HexResources
  WRITES: HexResourcesConfigLoaderSystem
  READS: HexResourcesSubSystem

COMPONENT: HexResourcesViewConfigComponent
  Module: HexResourcesView
  WRITES: HexResourcesViewConfigLoaderSystem
  READS: HexResourcesViewSubSystem

COMPONENT: ClayViewConfigComponent
  Module: HexResourcesView
  WRITES: ClayViewConfigLoaderSystem (flattened from ClayViewConfig SO; SO not retained)
  READS: ClayResourceViewSubSystem
  Note: Dedicated clay config singleton — clay is prefab-less and does NOT live in
        HexResourcesViewConfig (whose entries require a prefab). Carries the footprint
        (radius/depth/aspect/pear/noise) and the center/rim clay colors.

COMPONENT: ForestViewComponent / ClayViewComponent / FishViewComponent
  Module: HexResourcesView
  WRITES: ForestViewComponent → ForestViewSyncSystem (on view entity creation);
          FishViewComponent → FishResourceViewSubSystem (scaffold)
  READS: (held for later show/hide of the view MonoBehaviour; ForestViewSyncSystem disposes
          forest view entities when their hex stops being a forest)
  Note: ForestViewComponent holds only { ResourceType, ForestView } — the view MonoBehaviour reference,
        kept so the GameObject can be destroyed when the hex stops being a forest. No ground-splat data is
        stored: forest ground paint is append-only and never rebuilt from the entities. Lives on
        ResourceView entities, never on HexResource entities. ClayViewComponent is currently UNUSED — clay
        deforms VertexGrid + paints the texture directly and creates no view entity. Fish is scaffold.

COMPONENT: HexIconContainerComponent
  Module: HexIcons
  WRITES: HexIconsSpawnSystem (on container entity creation, one per hex)
  READS: HexIconsContainerPositionSystem (per-frame, to position the container);
         HexIconsVisibilitySystem (on visibility event, to clear/rebuild the container's icons)
  Note: Holds a managed UI Toolkit VisualElement (the per-hex icon container). Lives on HexIconContainer
        entities alongside the HexIdComponent FK, never on the terrain hex entity.

COMPONENT: HexIconsVisibilityComponent (WORLD component, not an entity)
  Module: HexIcons
  WRITES: GameplayState.EnterAsync (initial IsVisible = true); later a UI toggle
  READS: HexIconsVisibilitySystem (on a HexIconsVisibilityChangedEvent)
  Note: Mutable bool "are per-hex icons shown" — single source of truth (variant B). Stored via world.Set.

COMPONENT: EventTag
  Module: Scripts/DefaultECSExtensions
  WRITES: any system that emits an event entity (alongside the event component)
  READS: EventCleanupSystem (destroys entity after 1 frame)
  Lifetime: EVENT — 1 frame
```

---

## System Query Map

EntitySet queries per system. **Config components are world components** (read via `world.Get<T>()`,
guarded by `world.Has<T>()`), NOT EntitySet queries — every `With<*ConfigComponent>` line below is
historical shorthand and now means a `world.Get<*ConfigComponent>()` read. The cited line numbers are
indicative only.

```
SYSTEM: TerrainGenerationSystem
  QUERY: primary = With<HexIdComponent> + With<HexLevelComponent>  [TerrainGenerationSystem.cs:35]
  QUERY: config  = With<TerrainGenerationConfigComponent>          [TerrainGenerationSystem.cs:39]
  QUERY: hexSet  = With<HexIdComponent> + With<HexLevelComponent>  [TerrainGenerationSystem.cs:42]

SYSTEM: LakeGenerationSubSystem
  QUERY: terrainConfig = With<TerrainGenerationConfigComponent>    [L41]
  QUERY: lakeConfig    = With<LakeConfigComponent>                 [L44]
  QUERY: hexSet        = With<HexIdComponent> + With<HexLevelComponent> [L47]

SYSTEM: MountainGenerationSubSystem
  QUERY: config  = With<MountainConfigComponent>                   [L42]
  QUERY: hexSet  = With<HexIdComponent> + With<HexLevelComponent>  [L45]

SYSTEM: RiverGenerationSubSystem
  QUERY: terrainConfig = With<TerrainGenerationConfigComponent>    [L35]
  QUERY: riverConfig   = With<RiverConfigComponent>                [L38]
  QUERY: hexSet        = With<HexIdComponent> + With<HexLevelComponent> [L41]

SYSTEM: SeaGenerationSubSystem
  QUERY: terrainConfig = With<TerrainGenerationConfigComponent>    [L41]
  QUERY: seaConfig     = With<SeaConfigComponent>                  [L44]
  QUERY: hexSet        = With<HexIdComponent> + With<HexLevelComponent> [L47]

SYSTEM: TerrainViewSystem
  QUERY: primary    = With<ShowTerrainViewEventComponent>          [L50]
  QUERY: hexSet     = With<HexIdComponent>                         [L57]
  QUERY: configSet  = With<TerrainViewConfigComponent>             [L58]
  QUERY: vertexGrid = With<VertexGridComponent>                    [L59]
  QUERY: texture    = With<TerrainTextureComponent>                [L60]

SYSTEM: TerrainViewGenerationSubSystem
  QUERY: hexSet         = With<HexIdComponent>                     [L49]
  QUERY: config         = With<TerrainViewConfigComponent>         [L50]
  QUERY: vertexGrid     = With<VertexGridComponent>                [L51]
  QUERY: innerIsoline   = With<InnerIsolineConfigComponent>        [L52]
  QUERY: outerIsoline   = With<OuterIsolineConfigComponent>        [L53]
  QUERY: heightSmooth   = With<HeightSmoothingConfigComponent>     [L54]
  QUERY: hydraulic      = With<HydraulicErosionConfigComponent>    [L55]
  QUERY: wind           = With<WindErosionConfigComponent>         [L56]

SYSTEM: TerrainViewTextureSubSystem
  QUERY: hexSet        = With<HexIdComponent>                      [L54]
  QUERY: terrainConfig = With<TerrainViewConfigComponent>          [L55]
  QUERY: textureConfig = With<TerrainTextureConfigComponent>       [L56]
  QUERY: vertexGrid    = With<VertexGridComponent>                 [L57]

SYSTEM: WaterViewSubSystem
  QUERY: hexSet        = With<HexIdComponent>                      [L48]
  QUERY: terrainConfig = With<TerrainViewConfigComponent>          [L49]
  QUERY: waterConfig   = With<WaterViewConfigComponent>            [L50]

SYSTEM: TerrainViewDebugSystem
  QUERY: primary = With<HexIdComponent>                            [L25]
  QUERY: hexSet  = With<HexIdComponent> + With<HexLevelComponent>  [L29]
  QUERY: config  = With<TerrainViewConfigComponent>                [L34]

SYSTEM: HexSelectionViewSystem
  QUERY: primary      = With<HexSelectionViewComponent>            [L37]
  QUERY: selectedHex  = With<SelectedHexComponent>                 [L41]
  QUERY: vertexGrid   = With<VertexGridComponent>                  [L44]

SYSTEM: CameraMovementSystem
  QUERY: primary       = With<PlayerInputComponent>   (tick anchor)
  QUERY: playerInput   = With<PlayerInputComponent>   (input binding)
  QUERY: cameraConfig  = With<CameraMovementConfigComponent>
  QUERY: hexIdSet      = With<HexIdComponent>
  QUERY: terrainConfig = With<TerrainViewConfigComponent>
  READ:  CameraComponent via world.Get (world component, not a query)

SYSTEM: HexSelectionSystem
  QUERY: primary       = With<PlayerInputComponent>   (tick anchor)
  QUERY: playerInput   = With<PlayerInputComponent>   (input binding)
  QUERY: terrainConfig = With<TerrainViewConfigComponent>
  QUERY: selectedHex   = With<SelectedHexComponent>
  READ:  CameraComponent via world.Get (world component, not a query)

SYSTEM: HexResourcesSubSystem
  QUERY: config = With<HexResourcesConfigComponent>               [L21]

SYSTEM: HexResourcesViewSubSystem (base; Clay/Fish)
  QUERY: config       = With<HexResourcesViewConfigComponent>
  QUERY: resourceSet  = With<HexIdComponent> + With<HexResourcesComponent>
  QUERY: vertexGrid   = With<VertexGridComponent>

SYSTEM: ClayResourceViewSubSystem (extends base, one-shot)
  QUERY: clayConfig    = With<ClayViewConfigComponent>      (footprint + palette)
  QUERY: terrainConfig = With<TerrainViewConfigComponent>   (CellSize)
  QUERY: texture       = With<TerrainTextureComponent>      (clay paint target)
  QUERY: terrainView   = With<TerrainViewComponent>         (mesh re-apply)
  QUERY: hexSet        = With<HexIdComponent>               (square UV rect, like the texture bake)

SYSTEM: ForestViewSyncSystem (reactive, IUpdatedSystem)
  BASE:  texture      = With<TerrainTextureComponent>   (paint target + readiness gate)
  QUERY: resourceSet  = With<HexIdComponent> + With<HexResourcesComponent>
  QUERY: vertexGrid   = With<VertexGridComponent>
  QUERY: viewConfig   = With<HexResourcesViewConfigComponent>
  QUERY: terrainConfig= With<TerrainViewConfigComponent>
  QUERY: hexSet       = With<HexIdComponent>             (square UV rect, like the texture bake)

SYSTEM: HexIconsContainerPositionSystem (per-frame, ILateUpdatedSystem, Gameplay)
  BASE:  containers  = With<HexIdComponent> + With<HexIconContainerComponent>  (the set it positions each frame)
  QUERY: vertexGrid  = With<VertexGridComponent>         (PreUpdate, hex center for projection)
  Note: reads HexIconsViewComponent + CameraComponent via world.Get in PreUpdate. LateUpdate, priority 700 —
        after CameraMovementSystem (priority 0) so containers track the camera without a one-frame lag.

SYSTEM: HexIconsVisibilitySystem (event-driven, IUpdatedSystem, Gameplay, priority 800)
  BASE:  events     = With<HexIconsVisibilityChangedEvent>   (only fires the frame an event exists → zero idle cost)
  QUERY: containers = With<HexIdComponent> + With<HexIconContainerComponent>  (all containers — clear/rebuild)
  QUERY: resources  = With<HexIdComponent> + With<HexResourcesComponent>      (per hex, matched by Coords)
  Note: reads HexIconsVisibilityComponent + HexIconsViewComponent + HexIconsConfigComponent +
        HexResourceIconConfigComponent via world.Get (fail-loud). Sprite lookup is a linear scan of
        HexResourceIconConfig.Entries; missing/null sprite is a skip. No cached render state.

SYSTEM: EventCleanupSystem
  QUERY: events = With<EventTag>                                  [L22]
```

---

## Data Flow by Boot Phase

```
ConfigLoadStep (one-time boot bootstrap)
  → TerrainViewConfigLoaderSystem  → creates VertexGridComponent singleton
  → [all config loaders]           → set config WORLD components (world.Set), not entities

MainMenu state (entered after bootstrap)
  → ShowHexesUISystem → loads + shows the HexGeneratorUI (Generate button)

World-init pipeline (run by the MapCreation game state, NOT a boot phase)
  Trigger: TerrainGenerationGenerateEventComponent (HexesUI Generate button) is detected by the MainMenu
  state, which switches to MapCreation. MapCreation.EnterAsync awaits every
  IPrioritizedUniTaskSystem<TerrainGenerationStep> stage sequentially, in ascending priority, then ticks
  its reactive systems for a few settle frames and switches to Gameplay:

  → TerrainGenerationSystem (100) → creates Hex entities (HexIdComponent + HexLevelComponent)
      → Mountain / River / Lake / Sea GenerationSubSystem → mutate HexLevelComponent
      → then SyncHexTags → adds Hex{Plain,Mount,Water,Bedhill}Tag from final level
  → HexResourcesSystem (200) → resource gen subsystems → create HexResource entities
  → TerrainViewSystem (300) async:
      → TerrainViewGenerationSubSystem → mutates VertexGrid in place
      → TerrainViewTextureSubSystem    → writes PERSISTENT TerrainTextureComponent
      → WaterViewSubSystem             → writes WaterViewComponent
  → HexResourcesViewSystem (400) → Clay/Fish view subsystems (scaffold) only
  → HexSelectionViewLoadingSystem (500) async → creates HexSelectionViewComponent singleton
  → TerrainViewDebugSystem (600) → debug rays per hex level
  → HexIconsSpawnSystem (700) → spawns the screen-space overlay + creates EMPTY HexIconContainer entities
      (one per hex, HexIdComponent FK + HexIconContainerComponent); no icons added here

Gameplay state entry (GameplayState.EnterAsync):
  → sets HexIconsVisibilityComponent (IsVisible = true) + raises a one-frame HexIconsVisibilityChangedEvent
      (+ EventTag) → consumed on the first Gameplay tick by HexIconsVisibilitySystem

Reactive (per-frame, after boot — NOT a pipeline stage):
  → ForestViewSyncSystem (IUpdatedSystem) → diffs forest HexResource entities; spawns/removes
      ResourceView (forest) entities + paints/erases green ground in TerrainTextureComponent
  → HexIconsVisibilitySystem (IUpdatedSystem, Gameplay, priority 800) → on a HexIconsVisibilityChangedEvent,
      clears every HexIconContainer and (if visible) rebuilds icons from each hex's resources via
      HexResourceIconConfig
  → HexIconsContainerPositionSystem (ILateUpdatedSystem, Gameplay, after CameraMovementSystem) → projects
      each HexIconContainer's vertex-grid center to panel space every frame so containers track the camera
```

---

## Cross-Module Component Reads

Components read outside their owner module — potential coupling points.

```
TerrainViewConfigComponent   read by UserInput (CameraMovementSystem, HexSelectionSystem)
                             → CellSize used for raycast plane and camera bounds
HexIdComponent               read by HexResourcesView
                             → joins hex identity with resource data
VertexGridComponent          read by HexSelectionViewSystem (TerrainView)
                             → used to extract vertex rings for selection highlight
```

---

## Event Components

One-frame components used for async signalling between systems.

```
TerrainGenerationGenerateEventComponent
  Producer: HexesUI Generate button (later: save/load)
  Consumer: MainMenu game state — detects it and switches to MapCreation (which runs the pipeline)
  Lifetime: 1 frame (paired with EventTag; cleaned once MapCreation ticks EventCleanupSystem)
  Note: This is the ONLY world-init trigger. The old per-stage events
        (ShowTerrainViewEventComponent, HexResourcesGenerateEventComponent,
        ShowHexResourcesViewEventComponent) were removed — stages are now pipeline-driven.
  Naming: predates the …Event suffix rule (ARCHITECTURE.md) — kept until a deliberate rename.

HexIconsVisibilityChangedEvent
  Producer: GameplayState.EnterAsync (writes HexIconsVisibilityComponent, then raises this); later a UI toggle
  Consumer: HexIconsVisibilitySystem — clears every HexIconContainer, rebuilds from each hex's resources if visible
  Lifetime: 1 frame (paired with EventTag; cleaned by EventCleanupSystem)
  Note: payload-less signal (variant B) — the truth is HexIconsVisibilityComponent, not the event.

TerrainTextureComponent (NOT an event component — listed here only to correct the old assumption)
  Producer: TerrainViewTextureSubSystem
  Consumer: TerrainViewSystem (ApplyTexture, keeps entity alive); ForestViewSyncSystem paints into it (append-only)
  Lifetime: PERSISTENT — see the Component Registry entry above

EventTag
  Producer: any system emitting an event (Set on the event entity alongside the event component)
  Consumer: EventCleanupSystem (disposes the entity at end of tick, priority int.MaxValue)
  Lifetime: 1 frame
```
