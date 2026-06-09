# Session Handoff

> Transient work-in-progress note for resuming the next session. Not a canonical doc — delete/replace once
> the in-flight task is finished. The session-start skill reads root MD files, so this surfaces on startup.

## Active work (2026-06-08) — HexIcons screen-space badges

**Status:** the **screen-space overlay** approach for per-hex resource-icon badges is **approved** by the
user. `HexIconsSpawnSystem` is rewritten for screen-space with a **one-shot** world→screen projection at
spawn (works: `VertexGrid` hex-center for real height + `Screen.height − y` flip before
`RuntimePanelUtils.ScreenToPanel`). Deliberate prototype limits: positions are computed once and **drift
when the camera moves**; no incircle sizing / overflow→grid yet; a temporary demo drops forest+clay on a
few hexes.

**Next task (planned, not started):** the **per-frame projection system** (the "real solution").
Full step-by-step plan lives in:
`Assets/Modules/HexIcons/HEXICONS.md` → section **"Next Step — per-frame projection (planned)"**.
Gist: separate per-frame DefaultEcs system; move containers via `translate` (NOT per-frame `left/top`);
gate on camera-moved; cull behind-camera/off-screen; restore the 2:3 incircle sizing scaled by camera
distance + overflow→grid; drop the demo.

**How to resume:** read `Assets/Modules/HexIcons/HEXICONS.md` first (esp. the Spawn invariants + the
"Next Step" section), then restate the per-frame task via the task template (hard gate) and enter plan mode
before any edits.

**Uncommitted at handoff** (branch `Tasks/FM-7-add-forest`): `HexIconsSpawnSystem.cs`, `HexIcons.asmdef`,
`Assets/Modules/HexIcons/HEXICONS.md`, and the user's `HexIconsView.prefab`. The user handles committing.
