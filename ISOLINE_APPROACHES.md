# Isoline Approaches (Current)

This document was cleaned up to keep only the active approach.

## Active Approach
1. Build isolines with field-based active-region logic.
2. Fill the interior plateau from a seed inside the inner isoline.
3. Build slope transition by interpolating across the band between inner and outer isolines.
4. Apply post-process shaping in this order:
- wind erosion
- hydraulic erosion (pipe model)
- optional radius blur

## Key Invariants
- Center plateau must remain flat where required by design.
- Isoline-to-isoline transition correctness is higher priority than visual polish.
- Height edits only affect `Y`; no `X/Z` displacement.

## Source Files
- `Assets/Modules/HexesView/Systems/FieldBasedIsolineBuilder.cs`
- `Assets/Modules/HexesView/Systems/IsolineBuilderUtility.cs`
- `Assets/Modules/HexesView/Systems/HexIsolineSlopeTransition.cs`
- `Assets/Scripts/HexHeightSmoothing.cs`
- `Assets/Scripts/BootstrapSdfHex.cs`

## Status
- This approach is accepted as the current final terrain-generator baseline.
- Next work is river channels, then macro landscape direction, then gameplay mechanics.
