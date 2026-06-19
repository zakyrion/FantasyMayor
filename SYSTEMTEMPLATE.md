---
category: B
read: trigger
trigger: "before creating or editing an ECS system or subsystem"
tags: [template, ecs, systems]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[DOC_STANDARD](DOC_STANDARD.md)"
---

# FantasyMayor — System Template Catalog

How to create a new system. Pick the role first, then follow that role's template and rules.
The role taxonomy is ratified in `ARCHITECTURE.md` → "System Taxonomy"; this file carries the
concrete skeletons. Reference implementations named per template are the source of truth when this
file and code disagree.

---

## Step 0 — Pick the role

1. **Loads a config / prepares module data at startup?** → Config Loader. Go to `CONFIGTEMPLATE.md`
   (canonical; do not duplicate it here).
2. **Builds world/view content once during map creation?** → Pipeline Stage (Template 3).
   Several independently ordered parts? → Orchestrator + SubSystems (Template 4).
3. **Responds to a state change** (something appeared / vanished / toggled)? → Reactive System
   (Template 1). **This is the DEFAULT for runtime logic.**
4. **Genuinely must run every frame** (camera tracking, projection, input polling)? → Per-frame
   System (Template 2). Write down WHY it cannot be reactive. If the reason is "I need to notice
   when X changes" — it IS reactive; emit a pulse where X changes instead of diffing every frame.

---

## Template 1 — Reactive System (event-driven, pulse + reconcile)

CONDITION:
- Runtime logic that responds to a change: spawn/despawn views, rebuild UI, refresh a panel block.
- Reference implementations: `ForestSpawnSystem`, `ForestDespawnSystem`, `HexIconsVisibilitySystem`,
  `HexInfoPanelHeaderSystem`.

SKELETON:

```csharp
[UsedImplicitly]
public sealed class [Name]System : UpdatedSystem
{
    // Must run BEFORE EventCleanupSystem (int.MaxValue) so the pulse is consumed the same frame.
    private const int ExecutionPriority = [N];

    private readonly World _world;
    // Query caches: declarative, allowed. Name the table (key + discriminator) — Table Rule.
    private readonly EntityMultiMap<[Key]Component> _rowsByKey;

    public override int Priority => ExecutionPriority;

    public [Name]System(World world)
        : base(world.GetEntities().With<[Something]Event>().AsSet()) // the pulse IS the base set
    {
        _world = world;
        _rowsByKey = world.GetEntities()
            .With<[Key]Component>()
            .With<[Discriminator]Component>()
            .AsMultiMap<[Key]Component>();
    }

    // The pulse entity itself is ignored — reconciliation is global over current state.
    protected override void Update(GameState state, in Entity pulse)
    {
        // 1. Guard prerequisites (fail loud on the critical ones).
        // 2. Build the CURRENT desired state from world data.
        // 3. Diff against what exists; act only on the difference.
    }

    public override void Dispose()
    {
        _rowsByKey.Dispose();
        base.Dispose();
    }
}
```

Emitting the pulse (from a system, a game state, or a view):

```csharp
var pulse = world.CreateEntity();
pulse.Set(new [Something]Event());
pulse.Set(new EventTag()); // EventCleanupSystem disposes it at end of tick
```

RULES:
- The event is a **payload-less pulse** — an empty struct with the `…Event` suffix, in `Events/`.
  No coordinates, no lists inside the event. If the consumer needs data, it reads world state.
- On the pulse, **reconcile**: rebuild the desired set from current state and diff. Never process
  the event as a per-entity delta. This makes the system **idempotent** — a second pulse the same
  frame is a no-op, and missed/coalesced pulses are repaired by the next one.
- Base set = `With<TheEvent>` → the system costs nothing while no pulse exists.
- If persistent truth backs the pulse (e.g. a visibility flag), the truth lives in a world
  component or entity — the event only says "re-read it" (`HexIconsVisibilityChangedEvent` model).
- One-frame events DO NOT survive the async `MapCreation` pipeline. Startup bulk = Template 3/4,
  never an event.
- Emitters may be deferred: a dormant reactive scaffold (consumer wired, no emitter yet) is a valid
  intermediate state — mark it DORMANT in `ECS_REFERENCE.md`.
- Don't destroy entities while iterating the map/set that indexes them — snapshot into a
  `NativeList<Entity>` first, then destroy (`ForestDespawnSystem` model).

WARNINGS (decomposition smells — split instead of growing this system):
- It both **creates and destroys** the same content → two reactive systems, one event each.
- It needs a per-frame tick "just to check" → it stopped being reactive; find the change source
  and emit a pulse there.

---

## Template 2 — Per-frame System (Update / LateUpdate)

CONDITION:
- Logic that is genuinely continuous: camera movement, per-frame projection, input polling,
  selection watching. You must be able to say why a pulse cannot replace the tick.
- Reference implementations: `CameraMovementSystem`, `HexIconsContainerPositionSystem` (LateUpdate),
  `HexInfoPanelSystem` (selection watcher).

SKELETON:

```csharp
[UsedImplicitly]
public sealed class [Name]System : UpdatedSystem // or LateUpdatedSystem
{
    private const int ExecutionPriority = [N];

    private readonly World _world;

    public override int Priority => ExecutionPriority;

    public [Name]System(World world)
        : base(world.GetEntities().With<[Anchor]Component>().AsSet()) // tick anchor or work set
    {
        _world = world;
    }

    protected override void Update(GameState state, in Entity entity)
    {
        // Per-frame work. All inputs from world state / world components — no cross-frame fields.
    }
}
```

RULES:
- The base set is either the **work set** (the table the system processes — Table Rule applies) or
  a **tick anchor** (a singleton whose presence gates the tick, e.g. `PlayerInputComponent`).
- Use `LateUpdatedSystem` when the system must observe the frame's final state (run after camera
  movement, after gameplay writes). Order against other systems via `Priority`.
- **Stateless**: no cross-frame instance fields. Where the data belongs instead:
  - persistent state → a component on an entity, or a world component;
  - state valid only this frame → the `PreUpdate` + `FrameBox` pattern below;
  - a genuinely unavoidable instance field → `[StateAllowed("reason")]`, reviewed exception.
- A per-frame system that scans/diffs collections each tick to detect changes is a god-system
  smell — decompose per `ARCHITECTURE.md` → "Decomposition Rules".

### PreUpdate / PostUpdate + FrameBox (per-frame shared inputs)

`AEntitySetSystem<GameState>` (the base of `UpdatedSystem` / `LateUpdatedSystem`) provides two
frame hooks around the per-entity loop:
- **`PreUpdate(GameState)`** — runs ONCE before the per-entity `Update` calls. Resolve everything
  the loop shares here: world components, view/panel references, derived per-frame values. Guard
  every prerequisite **fail-loud** (by the time the system ticks, absence is a real error).
- **`PostUpdate(GameState)`** — runs ONCE after the loop. Use it to apply a batched result the loop
  accumulated or to release per-frame scratch. No current system needs it — prefer `PreUpdate`-only
  until a real batching case appears.

The shared inputs travel from `PreUpdate` to `Update` in a **`FrameBox<T>`** (`Core`) — NOT in plain
instance fields. The box is frame-stamped: a read in a later frame **throws** instead of silently
returning stale references, and `Dispose` drops the payload so held references (Camera, grid, panel)
never dangle on the singleton system. Reference implementation: `HexIconsContainerPositionSystem`.

The pattern applies to **both per-frame bases** — `UpdatedSystem` and `LateUpdatedSystem` (their
shared base `AEntitySetSystem<GameState>` provides the hooks). It is **forbidden in UniTask
systems** (`IUniTaskSystem<T>` / `IPrioritizedUniTaskSystem<T>`: Pipeline Stages, Pipeline
SubSystems, Config Loaders): an awaited `Update` spans frame boundaries, so the frame-stamped box
goes stale mid-`await` and throws — and a one-shot stage has no per-frame loop to broadcast into.
In async code pass data as locals/parameters across the awaits instead.

```csharp
[UsedImplicitly]
public sealed class [Name]System : UpdatedSystem // or LateUpdatedSystem
{
    private const int ExecutionPriority = [N];

    private readonly World _world;

    // The frame's shared inputs, broadcast from PreUpdate to the per-entity Update. Frame-stamped:
    // stale reads throw; Dispose drops the held references. The ONE sanctioned stateful field shape.
    [StateAllowed("Per-frame inputs; frame-stamped FrameBox, filled in PreUpdate, valid one frame.")]
    private FrameBox<FrameInputs> _inputs;

    public override int Priority => ExecutionPriority;

    public [Name]System(World world)
        : base(world.GetEntities().With<[Key]Component>().With<[Discriminator]Component>().AsSet())
    {
        _world = world;
    }

    protected override void PreUpdate(GameState state)
    {
        if (!_world.Has<[Shared]Component>())
            throw new InvalidOperationException("[Name]System: [Shared]Component is missing.");

        var shared = _world.Get<[Shared]Component>();
        // ...resolve + derive everything the loop needs, fail-loud on every miss...

        _inputs = FrameBox<FrameInputs>.OneFrame(new FrameInputs(/* resolved values */));
    }

    protected override void Update(GameState state, in Entity entity)
    {
        var inputs = _inputs.Value; // throws if stale — never reads a previous frame's references
        // ...per-entity work using inputs...
    }

    public override void Dispose()
    {
        _inputs.Dispose(); // drop held references so they don't dangle between map runs
        base.Dispose();
    }

    // Private nested readonly struct: an internal transport for one tick, not a reusable type.
    private readonly struct FrameInputs
    {
        // readonly fields + constructor only
    }
}
```

Pattern rules:
- The payload is a **private nested `readonly struct`** (e.g. `FramePose` in the reference) — it is
  a one-tick transport, not a public type.
- `FrameBox<T>.OneFrame(...)` for strictly-this-frame data; `TwoFrames`/`ForFrames` exist for the
  rare cross-frame handoff — justify any budget above one frame in the `[StateAllowed]` reason.
- The `FrameBox` field still carries `[StateAllowed("...")]` — `/arch-check` skips it, and the
  reason documents why the field is sanctioned.
- All prerequisite guards live in `PreUpdate`, not per entity — one throw per frame, and the
  per-entity `Update` stays branch-light.
- `Dispose` MUST call `box.Dispose()` — it clears the held references (a destroyed `Camera`/grid
  must not survive on the system instance between map runs).

---

## Template 3 — Pipeline Stage (one-shot async, world-init)

CONDITION:
- One-shot world/view construction during map creation, ordered against other stages.
- Reference implementations: `TerrainGenerationSystem`, `HexIconsSpawnSystem`,
  `MainUISpawnSystem` (an orchestrator stage that fans out into `MainUISpawnSubSystem`s).

SKELETON:

```csharp
[UsedImplicitly]
internal sealed class [Name]System : IPrioritizedUniTaskSystem<TerrainGenerationStep>
{
    private const int ExecutionPriority = [N]; // current stages: 100..800, spaced by 100

    private readonly World _world;

    public int Priority => ExecutionPriority;

    public [Name]System(World world)
    {
        _world = world;
    }

    public async UniTask Update(TerrainGenerationStep state, CancellationToken cancellationToken)
    {
        // Fail-loud guard on prerequisites produced by earlier stages.
        if (!_world.Has<[Prerequisite]Component>())
            throw new InvalidOperationException("[Name]System: [Prerequisite]Component is missing.");

        // Cancellation on its own line, separate from error conditions.
        if (cancellationToken.IsCancellationRequested)
            return;

        // Build content: create entities, world.Set runtime world components, load prefabs.
    }

    public void Dispose()
    {
        // Release owned handles (addressables, views), dispose query caches.
    }
}
```

RULES:
- Stages run **sequentially in ascending `Priority`**, awaited by the `MapCreation` state. A stage
  may rely on everything produced by lower-priority stages — and must fail loud if it is missing.
- Idempotency: if a stage can be re-entered (state re-entry, regeneration), guard with an
  `_isLoaded` flag or destroy-and-recreate (`TerrainViewSystem` model). Loading stages that own an
  addressable handle keep it and release in `Dispose` (`ADDRESSABLE_PATTERNS.md`).
- `cancellationToken.IsCancellationRequested` is the only quiet `return` — own line, never combined
  with validity checks.
- A stage that creates a singleton view entity publishes it for consumers
  (e.g. `HexInfoPanelViewComponent`); a stage that creates single-instance non-queried state uses a
  world component instead (`TerrainTextureComponent` model).

---

## Template 4 — Orchestrator + SubSystems

CONDITION:
- A pipeline stage with several independently ordered parts, or a family of implementations behind
  one abstraction (DoD polymorphism: one base, one implementation per resource/feature).
- Reference implementations: `HexResourcesViewSystem` + `HexResourcesViewSubSystem` (sync children),
  `TerrainViewSystem` + `ViewSubSystem` (async children).

ORCHESTRATOR SKELETON:

```csharp
[UsedImplicitly]
internal sealed class [Name]System : IPrioritizedUniTaskSystem<TerrainGenerationStep>
{
    private const int ExecutionPriority = [N];

    private readonly IReadOnlyList<[Name]SubSystem> _subSystems;

    public int Priority => ExecutionPriority;

    public [Name]System(IReadOnlyList<[Name]SubSystem> subSystems)
    {
        _subSystems = subSystems
            .OrderBy(system => system.Priority)
            .ToArray();
    }

    public UniTask Update(TerrainGenerationStep state, CancellationToken cancellationToken)
    {
        var gameState = default(GameState); // one-shot: deltaTime is irrelevant

        foreach (var subSystem in _subSystems)
        {
            if (!subSystem.IsEnabled)
                continue;

            subSystem.Update(gameState);
        }

        return UniTask.CompletedTask;
    }

    public void Dispose() { }
}
```

SUBSYSTEM SKELETON (sync; for async children model `ViewSubSystem` and await each `Update`):

```csharp
[UsedImplicitly]
internal sealed class [Feature]SubSystem : [Name]SubSystem
{
    private const int ExecutionPriority = [N];

    public override int Priority => ExecutionPriority;

    public [Feature]SubSystem(World world) : base(world) { }

    public override void Update(GameState state)
    {
        // One-shot step logic. Fail loud on missing prerequisites.
    }

    public override void Dispose()
    {
        // Dispose own query caches, then base.
        base.Dispose();
    }
}
```

RULES:
- The orchestrator **contains no domain logic**: sort children by `Priority`, skip
  `IsEnabled == false`, run, done. All real work lives in the subsystems.
- The abstract subsystem base owns the SHARED queries and `Try…` helpers
  (`HexResourcesViewSubSystem.TryGetVertexGrid` model); each subsystem owns and disposes its OWN
  extra queries.
- Children are registered in the module installer as the base type
  (`.As<[Feature]SubSystem, [Name]SubSystem>()`) so DI collects the family into the orchestrator's
  `IReadOnlyList`.
- Shared computation used by both a subsystem and a reactive system goes to a stateless helper in
  `Helpers/` (`ForestPlanter` model) — never duplicated, never stateful.
- Subsystem priorities order children WITHIN the orchestrator only — they are unrelated to pipeline
  stage priorities.

---

## Global rules (every template)

Canon for these bans and conventions lives in `ARCHITECTURE.md` (Statelessness And Collections, Table
Rule, Component Writes, Collector, Fail Loud); this section is the applied per-template form, and
`/arch-check` enforces the two bans.

**Statelessness (machine-checked by `/arch-check`):**
- No mutable per-instance fields. `readonly` handles (DI deps, `World`) and query caches
  (`EntitySet`, `EntityMap`, `EntityMultiMap`) are fine — they are declarative.
- A `readonly` collection whose CONTENTS change across frames IS state. Forbidden — state lives on
  entities or world components, not inside systems.
- Escape hatches in order: move the state out → `FrameBox<T>` for frame-bounded caches →
  `[StateAllowed("reason")]` as a last, reviewed resort.

**Collections (machine-checked by `/arch-check`):**
- No `System.Collections.Generic` in systems. `Unity.Collections` + explicit dispose:
  `Allocator.Temp` within a frame, `Allocator.Persistent` across `await` / thread-pool boundaries.
- Exception: managed element types (`GameObject`, views) stay `System.Collections.Generic` — mark
  with a short comment.

**Queries:**
- Every set names its table: key + discriminator (Table Rule, `ARCHITECTURE.md`). No bare
  `With<HexIdComponent>`.
- Writes go through `entity.Set<T>(value)` — never `ref Get` mutation (breaks reactive filters and
  maintained maps).
- Dispose EVERY `EntitySet` / `EntityMap` / `EntityMultiMap` the system constructed.

**Methods:**
- Output-producing methods follow the `Try…` + `bool` + `ref`/`out` Collector convention
  (`ARCHITECTURE.md`).
- Fail loud: missing prerequisites throw with a message naming what is missing. No
  warn-and-skip, no silent `return` (cancellation excepted).

**Responsibility warnings — split the system when:**
- it both creates and destroys the same content;
- it diffs world state every frame to find "what changed";
- it serves more than two unrelated query families;
- it runs in multiple game states for different reasons.
The split recipe (one-shot startup + reactive pair + shared helper) is in `ARCHITECTURE.md` →
"Decomposition Rules".

**Registration & wiring:**
- Register per-frame and reactive systems with their **concrete** type in the module installer
  (`builder.Register<[Name]System>(Lifetime.Singleton).As<[Name]System>()`); pipeline stages as
  `IPrioritizedUniTaskSystem<TerrainGenerationStep>`; subsystems as their family base type.
- Per-frame and reactive systems must then be wired into a game state by hand in `Boot.Construct` —
  decide WHICH state (almost always `Gameplay`) and add the system to that state's array. A system
  not wired into a state never runs.
