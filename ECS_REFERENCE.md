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
  Components: HexIdComponent (PK), HexLevelComponent, HexTag, HexTypeComponent (terrain kind —
              one column set at end of generation; replaced the 4 data-less terrain tags)
  Discriminator: HexTag
  WRITES: MapGenerationSystem (creates rows, pipeline 100; sets levels),
          Mountain/River/Lake/Sea GenerationSubSystem (mutate HexLevelComponent via Set),
          AssignHexTypes (sets HexTypeComponent from the final level, via Set)
  READS (by table query With<HexTag> + With<HexIdComponent>):
          ForestHexResourceViewSubSystem, ForestSpawnSystem (square UV rect),
          HexInfoPanelSystem, HexInfoPanelHeaderSystem
  READS (by With<HexIdComponent> + With<HexLevelComponent>):
          MapGenerationSystem, Lake/Mountain/River/Sea GenerationSubSystem, TerrainViewDebugSystem
  READS (by terrain-type index With<HexTag> → AsMultiMap<HexTypeComponent>):
          Forest/Fish/ClayResourceGenerationSubSystem (Water bucket), TerrainViewGenerationSubSystem
          (Mount/Water/elevated buckets), WaterViewSubSystem (Water bucket)
  READS (HexTypeComponent per-entity): TerrainViewTextureSubSystem (base type; coastline is
          presentation-derived, NOT a hex type), HexInfoPanelHeaderSystem (header terrain name/icon)
  READS (⚠ BARE-KEY LEGACY — see audit list):
          TerrainViewSystem, TerrainViewGenerationSubSystem, TerrainViewTextureSubSystem,
          WaterViewSubSystem, TerrainViewDebugSystem (primary), CameraMovementSystem,
          ClayHexResourceViewSubSystem
  Lifecycle: created once per map generation; never destroyed (no teardown/regeneration flow yet).
  Note: APPROACH A example — single-valued cell-intrinsic data lives directly on this entity
        (terrain kind = HexTypeComponent, one column, group-queryable via AsMultiMap<HexTypeComponent>;
        it replaced the 4 data-less terrain tags). Resources do NOT (see Modeling Rule).

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
  WRITES: ForestHexResourceViewSubSystem (startup bulk) and ForestSpawnSystem (runtime, reactive),
          both via the shared ForestPlanter helper; FishHexResourceViewSubSystem (scaffold — empty)
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

ENTITY: City  (table — actor identity; one row per city, currently exactly one)
  Components: CityIdComponent (PK), CityTag
  Discriminator: CityTag
  WRITES: CitySpawnSystem (domain Actors, pipeline 900; allocates the id from CityIdAllocatorComponent,
          then attaches the Resource loadout seeded from CityConfigComponent)
  READS: none yet (future: OwnerFK resolution — EntityMap<CityIdComponent> once ownables carry the FK)
  Lifecycle: created once during MapCreation; never destroyed (no teardown/load flow yet).
  Note: first table under the new Assets/Domains/ root (domain Actors). CityIdComponent is the PK here
        and the same component is the FK carried by anything a city owns. CityIdAllocatorComponent is a
        full +1 allocator — the model allows N cities even though one exists today.

ENTITY: Mayor  (table — actor identity; exactly one, singleton actor)
  Components: MayorIdComponent (PK), MayorTag, MayorAPRestoreComponent (per-turn AP restore amount)
  Discriminator: MayorTag
  WRITES: MayorSpawnSystem (domain Actors, pipeline 910; id stays 1; seeds MayorAPRestoreComponent from
          MayorConfigComponent.StartActionPoints, attaches the config Resource loadout, and seeds the
          Mayor's ActionPoint Resource stack — the live AP pool — also from StartActionPoints)
  READS: MayorActionPointsRestoreSubSystem (domain Actions, turn phase; reads MayorAPRestoreComponent to
         reset the AP stack each turn). Future: OwnerFK resolution — EntityMap<MayorIdComponent>.
  Note: MayorAPRestoreComponent is NOT the live AP pool — the live pool is the Mayor's ActionPoint Resource
        stack (see ENTITY: Resource). The restore component only carries the per-turn refill amount.
  Lifecycle: created once during MapCreation; never destroyed.
  Note: single mayor — MayorIdComponent is always 1. Kept as a PK table (not a world component) so it is
        queryable as an OwnerFK target, the same shape as City.

ENTITY: Resource  (table — inventory resource stack)
  Components: <owner FK: CityIdComponent | MayorIdComponent>, ResourceComponent (Type + Amount), ResourceTag
  Discriminator: ResourceTag
  WRITES: CitySpawnSystem + MayorSpawnSystem (domain Actors, pipeline 900/910 — each attaches one row per
          ResourceType to its own actor at map creation, via Economy's generic ResourceLoadoutSpawner).
          Mayor amounts come from MayorConfigComponent and City amounts from CityConfigComponent
          (ResourceTypes the author omits start at 0). ResourceType.ActionPoint is EXCLUDED from the generic
          loadout (no City AP); MayorSpawnSystem seeds the Mayor's ActionPoint stack separately via
          ResourceLoadoutSpawner.SpawnResource (= StartActionPoints). MayorActionPointsRestoreSubSystem
          (domain Actions, turn phase) re-Sets the Mayor's ActionPoint stack to MayorAPRestoreComponent.Value
          at the start of each turn. Noble-owned stacks are NOT written yet (deferred reactive system — see Note).
  READS: DistrictBuildActionSystem (Presentation.UI — reads the Mayor's stacks; the ActionPoint stack is the
         live AP shown in the build overlay).
  Lifecycle: created once at map creation for City + Mayor; never destroyed yet. Query a given owner's
        stacks via With<OwnerFK> + With<ResourceTag> → AsMultiMap<OwnerFK> (NEVER bare — the owner id is a
        PK on the actor AND a FK here).
  Note: SoA ownership — the owner's id component IS the FK (no polymorphic OwnerId). Composite key
        (OwnerFK + ResourceComponent.Type); NO surrogate ResourceId. Actors writes these rows via Economy's
        owner-agnostic ResourceLoadoutSpawner (cross-domain Actors→Economy, see Cross-Module Reads). Noble
        loadouts are DEFERRED: Nobles emerge at runtime, so ResourceNobleSpawnSystem (reactive, on a
        payload-less NobleSpawnEvent — reconciles against the Noble table; domain Actors) ships when the
        Noble actor exists. Design in ECONOMY_ACTORS.canvas.

ENTITY: District  (table — district identity; SCAFFOLD — NO spawner yet, player-built at runtime)
  Components: DistrictIdComponent (PK), DistrictTag
  Discriminator: DistrictTag
  WRITES: none yet — SCAFFOLD. Types defined (domain Economy, data-only slice); no entity created.
          District creation is PLAYER-ACTION-DRIVEN (reactive), NOT a world-init spawn — deliberately
          unlike City/Mayor (City/MayorSpawnSystem). The build-flow lands in a later slice.
  READS: none yet.
  Lifecycle: not instantiated yet. Planned: one row per built district; the id is allocated from
        DistrictIdAllocatorComponent. Columns omitted this slice: HexIdComponent (FK → Hex),
        OwnerFK (CityId | MayorId | NobleId), Type, Price, Actions. Buildings carry DistrictId as a FK.
  Note: DistrictIdComponent is the PK here AND the FK carried by anything scoped to a district (Buildings).
        Query via With<DistrictIdComponent> + With<DistrictTag> (NEVER bare). See Economy/ECONOMY.md.
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
  READS: TerrainViewSystem, ClayHexResourceViewSubSystem (EntitySet — mesh re-apply after depression)
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
         HexInfoPanelResourcesSystem / HexInfoPanelDistrictSystem (resolve the view to fill
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
        click→NextTurnEvent emit lives in EndTurnView (Presentation.UI).

ENTITY: ResourceBarView  (singleton)
  Components: ResourceBarViewComponent (View → the ResourceBarView MonoBehaviour), UITag
  WRITES: ResourceBarSpawnSubSystem (run by MainUISpawnSystem at pipeline 800 — GetComponentInChildren off
          the shared UI/MainUI instance; builds the left-panel resource rows from InventoryResourceIconConfigComponent,
          then leaves the bar hidden)
  READS: ResourceBarSystem (base/anchor set — per-frame Gameplay; reveals the strip and fills City + Mayor
         inventory amounts each frame)
  Note: top-bar resource strip (GENERAL_UI_STYLE.md §4 center). Per-frame (not reactive) because no
        ResourcesChanged pulse exists yet — it reads the City/Mayor Resource stacks directly every frame.
        Reads cross-module Actors + Economy components (see Cross-Module Reads).

ENTITY: DistrictBuildActionView  (singleton)
  Components: DistrictBuildActionViewComponent (View → the DistrictBuildActionView MonoBehaviour), UITag
  WRITES: DistrictBuildActionSpawnSystem (pipeline 810 — instantiates the SEPARATE UI/DistrictBuildAction
          overlay document under IMainCanvasProvider.RootGO; owns its handle in DistrictBuildActionRootComponent)
  READS: DistrictBuildActionSystem (base/anchor set — per-frame Gameplay; shows on DistrictBuildRequestedEvent
         filled from HexSelectedComponent + DistrictsBuildConfigComponent + payer stacks, hides on
         DistrictBuildClosedEvent)
  Note: modal district-build overlay (separate UIDocument, higher sort order). Spawns hidden. Reads
        cross-module Actors + Economy components (see Cross-Module Reads).
```

---

## World Component Registry

World components are NOT entities: stored via `world.Set<T>()`, read via `world.Get<T>()` (guard
with `world.Has<T>()`), invisible to `With<T>` / `WhenAdded<T>` / `WhenChanged<T>`. Contract and
decision rule: `ARCHITECTURE.md` → "State Storage Taxonomy".

### Runtime world components (12)

```
WORLD: CameraComponent  (module Cameras)
  Fields: Camera (live scene camera)
  WRITES: WorldInstaller (world.Set at startup)
  READS: CameraMovementSystem, HexSelectionSystem, HexIconsContainerPositionSystem (projection)

WORLD: VertexGridComponent  (Presentation/Terrain)
  WRITES: TerrainViewConfigLoaderSystem (world.Set ONCE at ConfigLoadStep)
  READS: TerrainViewSystem, TerrainViewGenerationSubSystem, TerrainViewTextureSubSystem,
         HexSelectionViewSystem, HexResourcesViewSubSystem (TryGetVertexGrid Try-pattern),
         ForestSpawnSystem, HexIconsContainerPositionSystem
  Note: carries the shared VertexGrid (a REFERENCE type). The grid object is mutated IN PLACE by the
        generation pipeline (isolines/erosion) and Clay; world.Set is never re-called after a
        mutation — readers always see the live grid. Not reactive. Direct readers throw on a missing
        component (fail-loud); the Try-method reports the miss as a bool.

WORLD: TerrainTextureComponent  (Presentation/Terrain)
  WRITES: TerrainViewTextureSubSystem (world.Set once in the view pipeline)
  READS: TerrainViewSystem (applies the Texture2D to the mesh material),
         ClayHexResourceViewSubSystem (clay gradient paint target),
         ForestHexResourceViewSubSystem / ForestSpawnSystem (forest ground paint target, append-only)
  Note: carries the generated terrain Texture2D (a REFERENCE type) — created readable
        (Apply(false)) precisely so it can be repainted later. Painters mutate pixels in place
        (SetPixels32 + Apply); the material keeps reflecting mutations without re-applying.
        PERSISTENT — never disposed.

WORLD: HexIconsViewComponent  (Presentation/HexIcons)
  WRITES: HexIconsSpawnSystem (the screen-space icon overlay view)
  READS: HexIconsContainerPositionSystem, HexIconsVisibilitySystem

WORLD: HexIconsVisibilityComponent  (Presentation/HexIcons)
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
  READS: EndTurnSystem (Presentation.UI — pushes Value into the "Хід N" label; throws if unseeded)
  Note: the current-turn counter. First Mayor Phase = turn 1; TurnCountSystem (Priority 1010, above the
        processor's 1000) increments on the TurnCompletedEvent pulse the same frame it is emitted.

WORLD: ContextTabsViewComponent  (Presentation.UI, window ContextTabs)
  Fields: View (→ the ContextTabsView MonoBehaviour)
  WRITES: ContextTabsSpawnSubSystem (world.Set at pipeline 800 — GetComponentInChildren off the shared
          UI/MainUI instance)
  READS: ContextTabSelectionSystem (restyle on the tab-changed pulse),
         ContextTabsAvailabilitySystem (enable/disable on the SelectedHexChangedEvent pulse)
  Note: world singleton (this window has NO singleton entity) — same view-reference role as the
        entity-based EndTurnView/HexInfoPanelView, but stored on the world.

WORLD: ActiveContextTabComponent  (Presentation.UI, window ContextTabs)
  Fields: Value (ContextTab enum: Overview | Buildings | Actions; Unknown=0 sentinel)
  WRITES: ContextTabsSpawnSubSystem (seed Overview on spawn), ContextTabsView (World.Set on tab click)
  READS: ContextTabSelectionSystem (reconciles the active highlight on the tab-changed pulse)
  Note: which context tab is active. The view writes it on click then raises the payload-less
        ContextTabChangedEvent; the selection system reconciles the highlight from HERE.

WORLD: CityIdAllocatorComponent  (domain Actors)
  Fields: Next (the id the next City will take)
  WRITES: CitySpawnSystem (self-init Next = 1 on first run; advances by +1 per city via world.Set)
  READS: CitySpawnSystem (reads Next to allocate)
  Note: monotonic CityId source. Its PRESENCE doubles as the one-shot guard — CitySpawnSystem skips if
        it already exists (no duplicate actors on pipeline re-entry / future load). Persisted via
        save/load (contract only — ES3 wiring deferred).

WORLD: MayorIdAllocatorComponent  (domain Actors)
  Fields: Next (effectively constant 1 — single mayor)
  WRITES: MayorSpawnSystem (self-init Next = 1; advances by +1, but only one mayor is created)
  READS: MayorSpawnSystem
  Note: kept for symmetry with CityIdAllocatorComponent and the save/load contract; yields a constant 1.

WORLD: DistrictIdAllocatorComponent  (domain Economy)  [SCAFFOLD]
  Fields: Next (the id the next District will take)
  WRITES: none yet — SCAFFOLD. Defined (data-only slice) but NOT set on the world; the player-driven
          district build-flow (later slice) will self-init and advance it.
  READS: none yet.
  Note: monotonic DistrictId source, shaped like CityIdAllocatorComponent. Persisted via save/load
        (contract only — ES3 wiring deferred).
```

### Config world components (24)

All written ONCE by their module's Config Loader at `ConfigLoadStep` (`CONFIGTEMPLATE.md`).
Reader lists name the consuming systems.

```
WORLD: TerrainGenerationConfigComponent  (Map/Generation)
  WRITES: TerrainGenerationConfigLoaderSystem
  READS: MapGenerationSystem, Lake/River/Sea GenerationSubSystem

WORLD: LakeConfigComponent / MountainConfigComponent / RiverConfigComponent / SeaConfigComponent  (Map/Generation)
  WRITES: TerrainGenerator config loaders
  READS: the matching GenerationSubSystem

WORLD: TerrainViewConfigComponent  (Presentation/Terrain)
  WRITES: TerrainViewConfigLoaderSystem
  READS: TerrainViewSystem, TerrainViewGenerationSubSystem, TerrainViewTextureSubSystem,
         WaterViewSubSystem, TerrainViewDebugSystem, ClayHexResourceViewSubSystem,
         ForestHexResourceViewSubSystem, ForestSpawnSystem (CellSize),
         CameraMovementSystem, HexSelectionSystem (cross-module — see Cross-Module Reads)

WORLD: TerrainTextureConfigComponent / InnerIsolineConfigComponent / OuterIsolineConfigComponent /
       HeightSmoothingConfigComponent / HydraulicErosionConfigComponent / WindErosionConfigComponent  (Presentation/Terrain)
  WRITES: TerrainViewConfigLoaderSystem
  READS: TerrainViewTextureSubSystem (texture config); TerrainViewGenerationSubSystem (the rest)

WORLD: WaterViewConfigComponent  (Presentation/Terrain)
  WRITES: TerrainViewConfigLoaderSystem
  READS: WaterViewSubSystem

WORLD: CameraMovementConfigComponent  (UserInput)
  WRITES: UserInput config loader
  READS: CameraMovementSystem

WORLD: HexResourcesConfigComponent  (Map/HexResources)
  WRITES: HexResourcesConfigLoaderSystem
  READS: HexResourcesSubSystem

WORLD: HexResourcesViewConfigComponent  (Presentation/HexResources)
  WRITES: HexResourcesViewConfigLoaderSystem
  READS: HexResourcesViewSubSystem base (prefab lookup), ForestHexResourceViewSubSystem, ForestSpawnSystem

WORLD: ClayViewConfigComponent  (Presentation/HexResources)
  WRITES: ClayViewConfigLoaderSystem (flattened from ClayViewConfig SO; SO not retained)
  READS: ClayHexResourceViewSubSystem
  Note: dedicated clay config — clay is prefab-less and does NOT live in HexResourcesViewConfig
        (whose entries require a prefab). Carries the footprint and the center/rim clay colors.

WORLD: HexIconsConfigComponent  (Presentation/HexIcons)
  WRITES: HexIconsConfigLoaderSystem
  READS: HexIconsContainerPositionSystem, HexIconsVisibilitySystem

WORLD: HexResourceIconConfigComponent  (Presentation/HexIcons)
  WRITES: HexIconsConfigLoaderSystem
  READS: HexIconsVisibilitySystem (icon rebuild), HexInfoPanelResourcesSystem (cross-module —
         resource rows in the info panel)

WORLD: HexTerrainIconConfigComponent  (HexesUI)
  WRITES: HexTerrainIconConfigLoaderSystem
  READS: HexInfoPanelHeaderSystem (terrain icon + name in the panel header)

WORLD: InventoryResourceIconConfigComponent  (Presentation.UI, window ResourceBar)
  WRITES: InventoryResourceIconConfigLoaderSystem
  READS: ResourceBarSpawnSubSystem (builds the resource-strip columns from the entries at MapCreation)
  Note: wraps the InventoryResourceIconConfig Box so sprites stay loaded. The entry list also DEFINES the
        strip's columns + order (only listed ResourceTypes get a column). Keys on Economy ResourceType —
        distinct from Presentation's HexResourceIconConfigComponent (hex resources).

WORLD: MayorConfigComponent  (domain Actors)
  WRITES: MayorConfigLoaderSystem
  READS: MayorSpawnSystem (seeds the Mayor's starting resource loadout, MayorAPRestoreComponent, and the
         initial ActionPoint Resource stack at map creation)
  Note: flattened from the MayorConfig SO — Resources array copied out, SO released after load. Carries
        StartActionPoints, which at spawn seeds BOTH MayorAPRestoreComponent (per-turn refill) and the
        Mayor's initial ActionPoint Resource stack (live pool). AP spending mechanics are a later slice.
        Lives in Actors/Mayor — actor-intrinsic startup state. See Actors/ACTORS.md.

WORLD: CityConfigComponent  (domain Actors)
  WRITES: CityConfigLoaderSystem
  READS: CitySpawnSystem (seeds the City's starting resource loadout at map creation)
  Note: flattened from the CityConfig SO — Resources array copied out, SO released after load. Resources
        only — the City has no Action Points (unlike MayorConfigComponent). Lives in Actors/City —
        actor-intrinsic startup state. See Actors/ACTORS.md.

WORLD: DistrictsBuildConfigComponent  (domain Economy, District)
  WRITES: DistrictsBuildConfigLoaderSystem (RETAINS the addressable Box — releases it in OnDispose, unlike the
          copy-out actor loaders; the build window reads the catalogue throughout play)
  READS: DistrictBuildActionSystem (Presentation.UI — passes the catalogue reference to the overlay view)
  Note: carries a REFERENCE to the DistrictsBuildConfig SO (Value) — no flatten/copy (the SO holds the
        Districts + their Prices/requirements). Economy's first config. See ECONOMY.md.
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
             HexInfoPanelResourcesSystem (561), HexInfoPanelDistrictSystem (562 — district build-prompt /
             details / hidden) — fill the panel blocks; ContextTabsAvailabilitySystem (560) — per-tab enabled
  Lifetime: 1 frame
  Note: the canonical "selection changed" pulse — payload-less; every consumer reads the current
        HexSelectedComponent (present/value), reconciling its block. Replaced both the consumers' former
        per-frame polling AND the old HexInfoPanelRefreshEvent (which only re-broadcast this with a Coords
        payload).

EVENT: ContextTabChangedEvent
  Producer: ContextTabsView (Presentation.UI, window ContextTabs) — on a tab click, after it writes the
            new ActiveContextTabComponent
  Consumer: ContextTabSelectionSystem (560) — reconciles the active-tab highlight from
            ActiveContextTabComponent
  Lifetime: 1 frame
  Note: payload-less pulse — the active tab lives in the ActiveContextTabComponent world singleton.

EVENT: DistrictBuildRequestedEvent
  Producer: HexInfoPanelView (Presentation.UI, window HexInfoPanel) — the District build slot
            (DistrictBuildButton) creates DistrictBuildRequestedEvent + EventTag on click
  Consumer: DistrictBuildActionSystem (Presentation.UI, window DistrictBuild) — opens the modal overlay,
            filled from the current HexSelectedComponent + the DistrictsBuildConfigComponent catalogue
  Lifetime: 1 frame
  Note: payload-less pulse — "open the district-build window for the selected hex". Event type lives in
        Presentation.UI.DistrictBuild.Events (next to its window).

EVENT: DistrictBuildClosedEvent
  Producer: DistrictBuildActionView (Presentation.UI, window DistrictBuild) — the «X», the scrim, and the
            «ЗБУДУВАТИ» button each create DistrictBuildClosedEvent + EventTag on click
  Consumer: DistrictBuildActionSystem — hides the overlay
  Lifetime: 1 frame
  Note: payload-less pulse. Build itself is dormant for now — «ЗБУДУВАТИ» only closes.

EVENT: ForestHexAppearedEvent / ForestHexRemovedEvent
  Producer: NO emitter yet (future gameplay: planting / chopping)
  Consumers: ForestSpawnSystem (Appeared) / ForestDespawnSystem (Removed) — each reconciles forest
             ResourceView rows against current HexResource state (spawn missing / destroy orphaned;
             ground paint append-only, never reverted)
  Lifetime: 1 frame
  Note: DORMANT scaffold. The startup forest is built one-shot by ForestHexResourceViewSubSystem —
        these pulses cover runtime changes only.

EVENT: NextTurnEvent
  Producer: EndTurnView (Presentation.UI) — the End Turn button creates NextTurnEvent + EventTag on click
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
  ClayHexResourceViewSubSystem    — hexSet (square UV rect)
```

---

## Data Flow by Phase

```
ConfigLoadStep (one-time boot bootstrap, before the state machine)
  → all Config Loaders → world.Set config WORLD components (22)
  → TerrainViewConfigLoaderSystem additionally → world.Set VertexGridComponent (runtime WORLD component)

MainMenu state
  → ShowHexesUISystem → loads + shows the HexGeneratorUI (Generate button)
  → Generate click → TerrainGenerationGenerateEventComponent → state switch to MapCreation

World-init pipeline (run by the MapCreation state; IPrioritizedUniTaskSystem<MapGenerationStep>
stages awaited sequentially in ascending priority, then settle frames, then switch to Gameplay):

  → MapGenerationSystem (100)        → creates Hex rows (HexIdComponent + HexLevelComponent)
      → Mountain / River / Lake / Sea GenerationSubSystem → mutate HexLevelComponent
      → AssignHexTypes → sets HexTypeComponent from the final level
  → HexResourcesSystem (200)             → generation subsystems create HexResource rows
  → TerrainViewSystem (300, orchestrator, async):
      → TerrainViewGenerationSubSystem   → mutates VertexGrid in place
      → TerrainViewTextureSubSystem      → world.Set TerrainTextureComponent (runtime WORLD component)
      → WaterViewSubSystem               → creates WaterViewSingleton
  → HexResourcesViewSystem (400, orchestrator):
      → ClayHexResourceViewSubSystem (200)  → VertexGrid depression + clay texture gradient
      → FishHexResourceViewSubSystem (300)  → scaffold
      → ForestHexResourceViewSubSystem (400)→ plants ALL forest hexes (ResourceView rows) + paints
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
  → HexInfoPanelHeader/Resources/District (560–562, reactive on SelectedHexChangedEvent)
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

Layer model: `Presentation → Map` reads are expected and one-way (the render layer reads the world it
draws); reads that used to cross the old HexResourcesView/HexIcons/TerrainView module borders are now
**intra-Presentation** (same assembly) and no longer coupling points.

```
TerrainViewConfigComponent     (Presentation) read by UserInput (CameraMovementSystem, HexSelectionSystem)
                               → CellSize used for raycast plane and camera bounds
HexIdComponent                 (Map) read by Presentation (resource + icon views) and HexesUI (Presentation.UI)
                               → the universal hex foreign key (joins across all hex-keyed tables)
HexResourcesComponent          (Map) read by Presentation (icon rebuild), HexesUI (info panel rows)
VertexGridComponent            (Presentation) read within Presentation (planting heights, icon
                               projection, selection ring) — intra-layer now, not cross-module
TerrainTextureComponent        (Presentation) read within Presentation (clay + forest ground painting)
HexResourceIconConfigComponent (Presentation) read by HexesUI (HexInfoPanelResourcesSystem — shared icons)
HexSelectedComponent           read by HexesUI (HexInfoPanelSystem + Header/Resources/District block
                               systems + ContextTabsAvailabilitySystem) and Presentation (HexSelectionViewSystem)
ResourceComponent / ResourceType / ResourceLoadoutSpawner  (Economy) used by Actors (City/MayorSpawnSystem
                               attach per-owner resource stacks via the generic helper). Cross-domain
                               Actors→Economy (owner-keyed logic in Actors; Economy stays owner-agnostic).
MapGenerationConfig hex types (HexTerrainType)  read by Economy (DistrictBuildingConfig) — cross-domain
                               Economy→Map (districts gate on hex type).
CityIdComponent / CityTag / MayorIdComponent / MayorTag  (Actors) and ResourceComponent / ResourceTag
                               (Economy)  read by Presentation.UI (ResourceBarSystem) — fills the left-edge
                               resource panel with City + Mayor pools. Cross-layer Presentation.UI→{Actors,
                               Economy} (UI reads domain state; domains never depend on Presentation.UI).
```
