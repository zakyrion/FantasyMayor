# HexesUI

UI Toolkit screen for hex terrain generation, loaded and shown by the `MainMenu` game state
(`Boot.Implementation`). It is no longer a boot phase.

## Non-Obvious Invariants
- **USS `picking-mode` is not supported in this Unity version.** Raycast-transparency is applied
  from C# in `HexesUI.Start()` via `EnablePicking(false)` (Unity.AppUI). The `raycast-transparent`
  USS class is only a semantic tag that marks which elements to disable in code — setting it in
  USS alone does nothing.
- Layout: every panel is raycast-transparent except the right panel, which is the only
  interactive area (holds the buttons). This lets clicks pass through the rest of the overlay
  to the map underneath.
- `ShowHexesUISystem.Update` is idempotent — calling it again does not spawn a second UI. The
  `MainMenu` state calls it on entry, then toggles visibility via `Show()` / `Hide()` (Hide on the
  transition to `MapCreation`).
- `ShowHexesUISystem` owns the loaded prefab instance and its addressable handle; both are
  released in `Dispose` (follow `ADDRESSABLE_PATTERNS.md`).
- The Generate button dispatches terrain generation by creating an entity with
  `TerrainGenerationGenerateEventComponent + EventMarkerComponent`.

## Current State
One screen with three buttons: Generate, Second Step, Generate Mesh.
**Only Generate is wired to logic.** Second Step and Generate Mesh are placeholders.
