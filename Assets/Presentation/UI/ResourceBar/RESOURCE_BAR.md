---
category: A
read: reference
tags: [ui, ecs]
related:
  - "[MAIN_UI](../MAIN_UI.md)"
  - "[GENERAL_UI_STYLE](../../../../GENERAL_UI_STYLE.md)"
status: partial
code_refs:
  systems:    [ResourceBarSystem, ResourceBarSpawnSubSystem]
  components: [InventoryResourceIconConfigComponent, ResourceBarViewComponent, CityIdComponent, MayorIdComponent, ResourceComponent, ActorTypeComponent]
  tags:       [CityResourceTag, MayorResourceTag, CityTag, MayorTag, UITag]
  enums:      [ActorType]
---

# ResourceBar

The **left-edge resource panel**: the two inventory pools (City / Mayor) as a vertical scroll list. The
window keeps the `ResourceBar` name even though it no longer lives in the top bar.

## Purpose
Glanceable, always-on readout of the two inventory pools — **City** (`Місто`) and **Mayor** (`Мер`) — pinned
down the **left edge** (`GENERAL_UI_STYLE.md` §4). A vertical scroll list: one **row per resource**
(`icon + City value + Mayor value`); a static owner header (`Місто` / `Мер`) sits above the scroll area. The
top bar no longer holds resources — it is now a thin empty placeholder strip.

## Content model
- **Rows = the authored config entries, in order.** `InventoryResourceIconConfig.Entries` IS the row set:
  only the `ResourceType`s the author lists get a row (the enum has 7 values; the author curates).
- **Each row = `icon + City value + Mayor value`** — the two owner values share the row width, role-colored
  (City blue, Mayor gold). The resource icon is the unified square plate shared with the terrain / hex-resource
  icons.
- Resources-only by design — the Mayor's Action Points / leadership stats are NOT shown here (AP lives in
  the turn sub-panel). City has no AP.

## Trigger
None — this window is **not** reactive; `ResourceBarSystem` runs **per-frame** in Gameplay
(role/priority: `ecsg.py explain ResourceBarSystem`).
**Per-frame justification** (override of Reactive-by-default, `ARCHITECTURE.md`): no `ResourcesChanged`
pulse exists yet, so the list reads the City/Mayor `Resource` stacks directly every frame. Cost is trivial
(2 owners × N rows). Replace with a reactive consumer once a resource-changed pulse lands.

## Non-Obvious Invariants
- **The icon config doubles as the row schema.** Adding/removing a row = editing the config asset, not the
  UXML. The runtime `Build` clears the editor-preview rows and rebuilds from the config.
- **The owner header (`Місто` / `Мер`) is static UXML**, authored above the `ScrollView`; the view builds only
  the scrolling rows. Header column labels align with each row's value columns via a leading spacer the width
  of the row icon.
- **Owner amounts are read via the Table Rule**, never a bare key: `With<CityIdComponent> + With<ResourceTag>
  → AsMultiMap<CityIdComponent>` (and the Mayor equivalent). The owner id is a PK on the actor row and the FK
  on each resource stack. The actor row itself is `With<CityIdComponent>().With<CityTag>().With<ActorTypeComponent>()`
  (and the Mayor equivalent) — `CityTag`/`MayorTag` still gate the actor row alongside `ActorTypeComponent`,
  which is read as the row's discriminator, not a replacement for the tags.
- **Lives on the shared `UI/MainUI` document.** `ResourceBarView` queries only its own `ResourcePanel`
  (left panel) + `TopBar` (thin strip) subtrees and toggles only those — never the document root (that blanks
  the whole Main UI). Neither is `raycast-transparent`, so the left panel blocks map clicks over itself and the
  top strip over the top edge (the Root stays click-through).
- **Spawns hidden, revealed in Gameplay** by `ResourceBarSystem` — `Show()`/`Hide()` toggle BOTH the left
  panel and the thin top strip together (the view temporarily owns the empty top strip until window-openers /
  system icons land there).
- `SetCityAmount` / `SetMayorAmount` are **no-ops for a `ResourceType` with no row** — a stack whose type the
  author omitted is silently ignored, by design.

## ECS bindings
- World component: `InventoryResourceIconConfigComponent` (loaded at `ConfigLoadStep`, addressable key
  `"InventoryResourceIconConfig"`; wraps the Box so sprites stay loaded).
- Singleton entity: `ResourceBarViewComponent` (+ `UITag`) — published by `ResourceBarSpawnSubSystem`.
- Reads: `CityIdComponent`/`MayorIdComponent`/`ActorTypeComponent` (Actors), `ResourceComponent`/`ResourceTag`
  (Economy). See the ecs-graph (`/ecs-graph`, cross-module reads).

## Current State
- **Code: implemented** — config + loader + component, view, spawn subsystem, per-frame system, markup
  (`ResourcePanel` + thin `TopBar` in the shared `HexInfoPanel.uxml`) + USS, DI + Boot wiring. Layout is the
  left-edge vertical list (`ResourceBarView` builds rows; public API `Build` / `SetCityAmount` /
  `SetMayorAmount` / `Show` / `Hide` unchanged by the relocation).
- **Requires Unity-side authoring (not in code):** the `InventoryResourceIconConfig.asset` (+ resource
  sprites) at addressable key `"InventoryResourceIconConfig"`, and a `ResourceBarView` MonoBehaviour added to
  the `UI/MainUI` prefab with the shared `PanelRenderer` assigned (else the spawn subsystem throws).
- Visual is a **first cut** of the §4 left panel; spacing/skin to be refined.
