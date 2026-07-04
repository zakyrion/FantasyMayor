---
category: B
read: trigger
trigger: "before creating a polymorphic ScriptableObject config catalogue that materializes into an entity table (many kinds keyed by a shared FK), or a per-kind polymorphic system family over such a table"
tags: [ecs, config, polymorphism, orchestrator, table-rule]
related:
  - "[PATTERN_CONFIG](PATTERN_CONFIG.md)"
  - "[PATTERN_CONFIG_LOADER](PATTERN_CONFIG_LOADER.md)"
  - "[PATTERN_ORCHESTRATOR_SUBSYSTEM](PATTERN_ORCHESTRATOR_SUBSYSTEM.md)"
  - "[PATTERN_PIPELINE_STAGE](PATTERN_PIPELINE_STAGE.md)"
  - "[PATTERN_TAG](PATTERN_TAG.md)"
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[ECS_CONVENTIONS](../ECS_CONVENTIONS.md)"
---

# Pattern — Polymorphic Config Catalogue → Entity Table

A **heterogeneous** set of authored rules/effects — many *kinds*, each with its own parameters — that you (1) author as
a polymorphic `ScriptableObject` catalogue and (2) materialize once into an **entity table** (one row per authored
entry), so runtime systems query/join them like any other table instead of unpacking a config blob every check.

This is a **composition of three patterns**, not a new mechanism: [PATTERN_CONFIG](PATTERN_CONFIG.md) (the wrap-SO
variant) + [PATTERN_CONFIG_LOADER](PATTERN_CONFIG_LOADER.md) + [PATTERN_ORCHESTRATOR_SUBSYSTEM](PATTERN_ORCHESTRATOR_SUBSYSTEM.md).
What it adds on top — and what no single pattern states — is the **materialization into an entity table with per-kind
discriminators**, the **routing vs non-routing** orchestrator split, and the **dual-host** reuse of one subsystem family.

**Canonical implementation:** `Economy.DistrictOpenCondition` (`Assets/Domains/Economy/DistrictOpenCondition/`). Read
its `DISTRICT_OPEN_CONDITION.md` for the live example; this recipe is the generic procedure.

## When to use

- Authored data comes in **several kinds that differ in shape** (different parameters, or some parameter-less), all
  sharing one **key** (a FK such as `DistrictTypeComponent`).
- Runtime code must **query or join** those entries by key — react to them, look them up per turn, combine with another
  table — rather than read them once.
- A new kind should plug in **without editing** the orchestrator (Open-Closed).

## When NOT to use

- **One** config, one shape → plain [PATTERN_CONFIG](PATTERN_CONFIG.md) (flatten or wrap into a world component). Do
  not spawn an entity for a singleton config.
- Homogeneous list read once at startup and never queried by key → a flattened world component is cheaper than a table.
- You need behavior *on* the config → no. SO configs stay **pure data**; the type-switch lives in the subsystems.

---

## The five parts

### 1 — Polymorphic config catalogue (authored)

- **Abstract base config** `FooConfig : ScriptableObject` — holds only the shared **key** (`FooKey Key`, e.g. a
  `DistrictType`). No abstract behavior; SO stays pure data.
- **Concrete kinds** `BarFooConfig : FooConfig`, `BazFooConfig : FooConfig` — each adds its own parameters (or none).
- **Container SO** `FoosConfig : ScriptableObject` — holds `FooConfig[] Items`.

```csharp
public abstract class FooConfig : ScriptableObject
{
    [SerializeField] private FooKey _key;
    public FooKey Key => _key;                     // the shared FK; the "gated"/target subject
}

public sealed class BarFooConfig : FooConfig       // data-carrying kind
{
    [SerializeField] private FooKey _requiredThing;
    public FooKey RequiredThing => _requiredThing;
}

public sealed class BazFooConfig : FooConfig { }   // parameter-less kind

public sealed class FoosConfig : ScriptableObject
{
    [SerializeField] private FooConfig[] _items;
    public FooConfig[] Items => _items;
}
```

### 2 — Loader → world component (live SO reference)

One [config loader](PATTERN_CONFIG_LOADER.md) at `ConfigLoadStep`. It publishes a world component that **wraps the
live SO reference** (not a flattened copy — the orchestrator reads the concrete subclasses later, at
`MapGenerationStep`) and **retains the addressable `Box`** for the catalogue's lifetime, releasing it in `OnDispose`.
Validate all entries non-null and **fail loud** — a missing/blank catalogue stops the game at boot, not later
rule-blind.

```csharp
public readonly struct FoosConfigComponent { public readonly FoosConfig Value; /* ctor */ }
```

### 3 — Spawn orchestrator + Try-pattern subsystem family (routing)

A [pipeline stage](PATTERN_PIPELINE_STAGE.md) orchestrator at `MapGenerationStep` reads the world component, iterates
`Items`, and routes each entry to the **first** subsystem that handles its concrete type. **Fail loud** on a null
entry or a config type no subsystem matches (a new kind without its subsystem must not pass silently).

```csharp
internal abstract class FooSpawnSubSystem : IDisposable
{
    protected readonly World World;
    public bool IsEnabled { get; } = true;
    public int  Priority  { get; }
    protected FooSpawnSubSystem(World world) => World = world;
    public abstract bool TrySpawn(FooConfig config);   // true = "I handled it"
    public void Dispose() { }
}

internal sealed class FooSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
{
    public int Priority => 920;                        // domain-spawn cluster
    [StateAllowed] private readonly IReadOnlyList<FooSpawnSubSystem> _subSystems;
    private readonly World _world;
    // ctor: (World world, IReadOnlyList<FooSpawnSubSystem> subSystems)

    public UniTask Update(MapGenerationStep state, CancellationToken ct)
    {
        var items = _world.Get<FoosConfigComponent>().Value.Items;
        foreach (var item in items)
        {
            if (item == null)      throw new /* fail loud: null catalogue entry */;
            if (!TrySpawn(item))   throw new /* fail loud: no subsystem handles item.GetType() */;
        }
        return UniTask.CompletedTask;
    }

    private bool TrySpawn(FooConfig item)
    {
        foreach (var s in _subSystems)                 // ordered by Priority
            if (s.IsEnabled && s.TrySpawn(item)) return true;
        return false;
    }
}

internal sealed class BarFooSpawnSubSystem : FooSpawnSubSystem
{
    public override bool TrySpawn(FooConfig config)
    {
        if (config is not BarFooConfig bar) return false;   // not my kind → let the next try
        World.CreateEntity()
             .Set(new FooKeyComponent(bar.Key))             // FK key
             .Set(new FooTag())                             // discriminator: "this is a Foo row"
             .Set(new BarFooComponent(bar.RequiredThing));  // per-kind payload (doubles as kind discriminator)
        return true;
    }
}
```

### 4 — The entity table (one row per authored entry)

Every row shares a **key** + a **discriminator tag**; each **kind** adds its own marker:

| Slot | Role | Example |
|---|---|---|
| Key component (FK) | which subject this entry is about | `DistrictTypeComponent` |
| Discriminator tag | "this row is a Foo" (marks the whole table) | `DistrictOpenConditionTag` |
| Per-kind marker | which kind this row is | payload **component** or marker **tag** (see rule below) |
| Output tag (optional) | result the evaluator writes | `DistrictCanBeBuildTag` |

**Query by the table, never by the bare key** (Table Rule, [ECS_CONVENTIONS](../ECS_CONVENTIONS.md)):
`With<FooTag>().With<BarFooComponent>().With<FooKeyComponent>()`.

### 5 — (Optional) Evaluator family — the non-routing loop variant

When rows must be **re-checked over time** (each turn) and reconciled into an output tag, add a **second** family. It
uses the SAME orchestrator+subsystem shape but a **different loop discipline** (below). Each evaluator self-queries its
own kind-slice (`AsSet`), joins the target table by key (`AsMultiMap` / `AsMap`), and **idempotently** Sets/Removes the
output tag. Omit this whole part if the table is a static lookup joined on demand (then it is spawn-only).

```csharp
internal sealed class BarFooEvaluatorSubSystem : FooEvaluatorSubSystem
{
    private readonly EntitySet _rows;                              // With<FooTag>().With<BarFooComponent>().With<FooKeyComponent>().AsSet()
    private readonly EntityMultiMap<FooKeyComponent> _targetsByKey; // With<TargetTag>().With<FooKeyComponent>().AsMultiMap(...)

    public override void Evaluate()
    {
        foreach (ref readonly var row in _rows.GetEntities())
        {
            var key = row.Get<FooKeyComponent>();
            var satisfied = /* join: */ !_targetsByKey.TryGetEntities(key, out _);
            if (satisfied) row.Set<OutputTag>();     // idempotent
            else           row.Remove<OutputTag>();  // idempotent
        }
    }
}
```

---

## Routing vs non-routing — the two orchestrator disciplines

Both host an `IReadOnlyList<TSubSystem>`; they differ in HOW they call it:

| | **Routing (spawn)** | **Non-routing (evaluator)** |
|---|---|---|
| Loop | per config entry, `TrySpawn` down the list, **first match wins** | run **every** enabled subsystem unconditionally |
| Input | one authored config → one owner | each subsystem self-queries its own rows |
| Unhandled kind | **fail loud** (no subsystem matched) | **silent no-op** (that kind simply has no rows touched) |
| Use for | materializing config → entities | periodic reconcile of existing rows |

The asymmetry is deliberate: a config kind with no spawn-subsystem is an authoring bug (loud); a kind with no
evaluator yet is just not-implemented-yet (quiet, expected).

## Per-kind discriminator rule

- **Data-carrying kind** → a payload **component** that doubles as the discriminator (present ⇒ this kind). e.g.
  `BarFooComponent { RequiredThing }`.
- **Parameter-less kind** → an empty marker **tag** (present ⇒ this kind). Without it, a parameter-less row is
  indistinguishable from any other. See [PATTERN_TAG](PATTERN_TAG.md).

Never overload the *key* as the kind discriminator — the key is the subject (1:N per key is expected), the discriminator
is the kind.

## Dual-host reuse (when the evaluator must run at two lifecycles)

If the evaluator must cover **turn 1** (before the turn pipeline first runs) AND **every later turn**, register **two
thin host wrappers** over the SAME DI-collected subsystem list — no duplicated evaluation logic:
- a **bootstrap** host `IPrioritizedUniTaskSystem<MapGenerationStep>` (priority right after the spawn stage) for turn 1;
- a **turn-phase** host `: TurnPhaseSubSystem` (tail of the turn pipeline) for every subsequent turn.

Both inject the same `IReadOnlyList<FooEvaluatorSubSystem>` and just loop `Evaluate()`.

## DI wiring (collection injection)

In the domain installer, `Singleton` everything; register each subsystem **as the abstract base** so VContainer fills
the `IReadOnlyList<TBase>` (see [di-graph](../ARCHITECTURE.md) — collection resolution):

```csharp
builder.Register<FooSpawnSystem>(Lifetime.Singleton).As<IPrioritizedUniTaskSystem<MapGenerationStep>>();
builder.Register<BarFooSpawnSubSystem>(Lifetime.Singleton).As<FooSpawnSubSystem>();
builder.Register<BazFooSpawnSubSystem>(Lifetime.Singleton).As<FooSpawnSubSystem>();
// evaluator family (if any): both hosts + each concrete .As<FooEvaluatorSubSystem>()
```

The collected list field carries `[StateAllowed]` (a DI-owned collection is the deliberate-state exception, not a
zero-alloc violation).

## Adding a new kind (the payoff)

Three pieces, orchestrator untouched:
1. a concrete `FooConfig` subclass (its parameters, or none);
2. its per-kind discriminator (payload component if it has params, else a marker tag);
3. a `FooSpawnSubSystem` (and a `FooEvaluatorSubSystem` if the family evaluates), each registered `.As<…Base>()`.

## Checklist

- [ ] Container SO holds `FooConfig[]`; abstract base carries only the shared key; concretes are pure data.
- [ ] Loader at `ConfigLoadStep` wraps the **live SO ref**, retains the `Box`, fails loud on null/empty, releases on dispose.
- [ ] Spawn orchestrator fails loud on a null entry / unhandled type; subsystems return `bool` from `TrySpawn` (Try-pattern).
- [ ] Every row = key (FK) + discriminator tag + per-kind marker; consumers query the table, never the bare key.
- [ ] Payload kinds → component discriminator; parameter-less kinds → marker tag.
- [ ] Evaluator (if present) is non-routing, self-queries its slice, joins by key via `AsMap`/`AsMultiMap`, Sets/Removes idempotently.
- [ ] Dual-host only if two lifecycles are needed; both reuse one subsystem list.
- [ ] All `Singleton`; subsystems `.As<AbstractBase>()`; collected field `[StateAllowed]`.
