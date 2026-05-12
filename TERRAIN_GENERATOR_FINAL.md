# Terrain Generator Final Baseline (2026-04-21)

## Status
- This is the current final baseline of the terrain generator.
- Core generator systems are implemented and considered working together.
- Plateau center preservation is a required invariant and is currently respected.

## Implemented Systems
1. Field-based isoline generation:
- `Assets/Modules/HexesView/Systems/FieldBasedIsolineBuilder.cs`
- `Assets/Modules/HexesView/Systems/IsolineBuilderUtility.cs`
2. Interior plateau fill from inside seed:
- `HexIsolineSlopeTransition.FillInsideIsoline(...)`
3. Slope transition between inner/outer isolines:
- `HexIsolineSlopeTransition.ApplyTransitionBetweenIsoLines(...)`
4. Directional wind erosion with material transport:
- `HexHeightSmoothing.ApplyDirectionalWindErosion(...)`
5. Hydraulic erosion (pipe model, not droplets):
- `HexHeightSmoothing.ApplyHydraulicErosionPipeModel(...)`
6. Radius blur as final polish:
- `HexHeightSmoothing.ApplyRadiusBlur(...)`

## Current Terrain Processing Order
1. Build isolines and apply isoline transition.
2. Apply wind erosion.
3. Apply hydraulic erosion (pipe model).
4. Apply optional radius blur.

## Scope Freeze For This Milestone
- Terrain generator internals are considered complete for now.
- No more detail-level polishing is required in this milestone unless it fixes a clear bug.

## Non-Goals Right Now
- Shader-only fake rain erosion.
- Micro detail polishing before river and macro landscape decisions.
