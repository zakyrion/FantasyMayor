---
category: A
read: reference
tags: [ui, ecs]
related:
  - "[MAIN_UI](../MAIN_UI.md)"
  - "[GENERAL_UI_STYLE](../../../../GENERAL_UI_STYLE.md)"
  - "[ECS_REFERENCE](../../../../ECS_REFERENCE.md)"
status: partial
---

# ResourceBar

The top-bar **resource strip**: the `GENERAL_UI_STYLE.md` §4 top-bar CENTER only — the resource pool as an
owner×resource matrix. No window-openers, no system icons.

## Purpose
Glanceable, always-on readout of the two inventory pools — **City** (`Місто`) and **Mayor** (`Мер`) — at
the top edge. Rows are the two owners; columns are inventory resources (`icon + value`). This is the first
slice of the §4 top bar; the other two zones are intentionally not built.

## Content model
- **Columns = the authored config entries, in order.** `InventoryResourceIconConfig.Entries` IS the column
  set: only the `ResourceType`s the author lists get a column (the enum has 7 values; the author curates).
- **Rows = owners:** `Місто` (city, blue) and `Мер` (gold). Each cell = that owner's `Amount` for the
  column's `ResourceType`. The resource icon heads the column once.
- Resources-only by design — the Mayor's Action Points / leadership stats are NOT shown here (AP lives in
  the turn sub-panel). City has no AP.

## Trigger
None — this window is **not** reactive. `ResourceBarSystem` is a **Per-frame System** (Gameplay).
**Per-frame justification** (override of Reactive-by-default, `ARCHITECTURE.md`): no `ResourcesChanged`
pulse exists yet, so the strip reads the City/Mayor `Resource` stacks directly every frame. Cost is trivial
(2 owners × N columns). Replace with a reactive consumer once a resource-changed pulse lands.

## Non-Obvious Invariants
- **The icon config doubles as the column schema.** Adding/removing a column = editing the config asset, not
  the UXML. The runtime `Build` clears the editor-preview cells and rebuilds from the config.
- **Owner amounts are read via the Table Rule**, never a bare key: `With<CityIdComponent> + With<ResourceTag>
  → AsMultiMap<CityIdComponent>` (and the Mayor equivalent). The owner id is a PK on the actor row and the FK
  on each resource stack. The actor id itself comes from the actor table (`With<CityIdComponent> + CityTag`).
- **Lives on the shared `UI/MainUI` document.** `ResourceBarView` queries only its own `TopBar` subtree and
  toggles only that element — never the document root (that blanks the whole Main UI). The bar is NOT
  `raycast-transparent`, so it blocks clicks across the top strip (the Root stays click-through).
- **Spawns hidden, revealed in Gameplay** by `ResourceBarSystem` — the exact mirror of EndTurn's shell reveal.
- `SetCityAmount` / `SetMayorAmount` are **no-ops for a `ResourceType` with no column** — a stack whose type
  the author omitted is silently ignored, by design.

## ECS bindings
- World component: `InventoryResourceIconConfigComponent` (loaded at `ConfigLoadStep`, addressable key
  `"InventoryResourceIconConfig"`; wraps the Box so sprites stay loaded).
- Singleton entity: `ResourceBarViewComponent` (+ `UITag`) — published by `ResourceBarSpawnSubSystem`.
- Reads: `CityIdComponent`/`CityTag`/`MayorIdComponent`/`MayorTag` (Actors), `ResourceComponent`/`ResourceTag`
  (Economy). See `ECS_REFERENCE.md` → Cross-Module Reads.

## Current State
- **Code: implemented** — config + loader + component, view, spawn subsystem, per-frame system, markup
  (`TopBar` in the shared `HexInfoPanel.uxml`) + USS, DI + Boot wiring.
- **Requires Unity-side authoring (not in code):** the `InventoryResourceIconConfig.asset` (+ resource
  sprites) at addressable key `"InventoryResourceIconConfig"`, and a `ResourceBarView` MonoBehaviour added to
  the `UI/MainUI` prefab with the shared `PanelRenderer` assigned (else the spawn subsystem throws).
- Visual is a **first cut** of the §4 matrix; spacing/skin to be refined against
  `design-mockups/FantasyMayor-HUD.html`.
