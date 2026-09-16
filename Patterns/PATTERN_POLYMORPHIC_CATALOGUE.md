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
---

# Pattern — Polymorphic Config Catalogue → Entity Table

A **heterogeneous** set of authored rules/effects — many *kinds*, each with its own parameters — that you (1) author as
a polymorphic `ScriptableObject` catalogue and (2) materialize once into an **entity table** (one row per authored
entry), so runtime systems query/join them like any other table instead of unpacking a config blob every check.

This is a **composition of three patterns**, not a new mechanism: [PATTERN_CONFIG](PATTERN_CONFIG.md) (the wrap-SO
variant) + [PATTERN_CONFIG_LOADER](PATTERN_CONFIG_LOADER.md) + [PATTERN_ORCHESTRATOR_SUBSYSTEM](PATTERN_ORCHESTRATOR_SUBSYSTEM.md).
What it adds on top — and what no single pattern states — is the **materialization into an entity table with per-kind
discriminators**, the **routing vs non-routing** orchestrator split, and the **dual-host** reuse of one subsystem family.

**Canonical implementation:** `Economy.DistrictOpenCondition` (`Assets/Domains/Economy/DistrictOpenCondition/`) —
explore it via fantasymayor-graph pattern PATTERN_POLYMORPHIC_CATALOGUE / code headers; this recipe is the generic procedure.

## When to use / when NOT

```clojure
;; ── USE when ALL hold ───────────────────────────────────────────────────────
(def use-when
  {:authored-kinds "several, differing in shape"                  ;; different parameters, or some parameter-less — all sharing one KEY (an FK such as DistrictTypeFKComponent — the reference side of an enum key space carries the FK suffix, never the owner's own column)
   :runtime        "queries/joins entries BY KEY"                 ;; reacts, looks up per turn, combines with another table — not a read-once blob
   :new-kind       "plugs in WITHOUT editing the orchestrator"})  ;; Open-Closed

;; ── do NOT use when ─────────────────────────────────────────────────────────
(cond
  (one-config-one-shape?)       PATTERN_CONFIG                  ;; read the SO via storages.Get<T> — never spawn a queryable row for a singleton config
  (homogeneous-read-once-list?) PATTERN_CONFIG                  ;; read the SO's array directly — cheaper than a table when never queried by key
  (behavior-on-the-config?)     :NO)                        ;; SO configs stay pure data (IValidatableConfig.Validate is the only method) — the type-switch lives in the subsystems
```

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

### 2 — Loading the container SO

One [`ConfigLoaderSystem<FoosConfig>` registration](PATTERN_CONFIG_LOADER.md) at `AppState.ConfigLoading` puts the
container SO into `EntityStorages`; the orchestrator reads the concrete subclasses later, at `MapGenerationStep`.
`FoosConfig` implements `IValidatableConfig` and checks all entries non-null — **fail loud**: a missing/blank
catalogue stops the game at boot, not later rule-blind.

```csharp
public sealed class FoosConfig : ScriptableObject, IValidatableConfig
{
    // ... _items as above
    public void Validate() { /* throw on a null array or a null entry */ }
}
```

### 3 — Spawn orchestrator + Try-pattern subsystem family (routing)

A [pipeline stage](PATTERN_PIPELINE_STAGE.md) orchestrator at `MapGenerationStep` reads the container SO, iterates
`Items`, and routes each entry to the **first** subsystem that handles its concrete type. **Fail loud** on a null
entry or a config type no subsystem matches (a new kind without its subsystem must not pass silently).

```csharp
internal abstract class FooSpawnSubSystem : IDisposable
{
    protected readonly EntityStore World;
    public bool IsEnabled { get; set; } = true;        // the family's switch
    public abstract int Priority { get; }              // each concrete names its own SystemPriorities member
    protected FooSpawnSubSystem(EntityStore world) => World = world;
    public abstract bool TrySpawn(FooConfig config);   // true = "I handled it"
    public virtual void Dispose() { }
}

internal sealed class FooSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
{
    public int Priority => SystemPriorities.WorldInit.FooSpawn;   // the one holder of order — never a literal
    [StateAllowed("DI-collected composition, fixed at construction")] private readonly IReadOnlyList<FooSpawnSubSystem> _subSystems;
    private readonly EntityStorages _storages;
    // ctor: (EntityStorages storages, IReadOnlyList<FooSpawnSubSystem> subSystems)

    public UniTask Update(MapGenerationStep state, CancellationToken ct)
    {
        var items = _storages.Get<FoosConfig>().Items;
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
        // The archetype carries EVERY column of this kind — the row is born complete (Birth Completeness).
        var row = EconomyArchetypes.BarFoo(World).CreateEntity();
        row.AddComponent(new FooSubjectFKComponent(bar.Key));                 // FK into the subject's key space
        row.AddComponent(new FooKindComponent { Value = FooKind.Bar });       // kind column — the selector
        row.AddComponent(new BarFooComponent(bar.RequiredThing));             // per-kind payload (parameters only)
        row.AddComponent(new FooStateComponent { Value = FooState.Closed });  // initial state, if the family evaluates
        return true;
    }
}
```

### 4 — The entity table (one row per authored entry)

Every row shares a **key** + its archetype's main tag (exactly one — Tag Law), with the family label tag beside it when the family spans several archetypes; kind and evaluator
output are enum COLUMNS, never extra tags:

| Slot | Role | Shape |
|---|---|---|
| Key component (FK) | which subject this entry is about | the subject space's `…FKComponent` (key-role law) |
| Main tag | "this row is a Foo" — the row's ONLY main tag, named by the archetype | `DistrictSingleOpenConditionTag` |
| Family label tag | the family its archetypes share — [TagLabel], never a filter | `DistrictOpenConditionTag` |
| Kind component | which kind this row is | `FooKindComponent { FooKind Value }` — `IIndexedComponent<FooKind>`, written once at birth |
| Payload component (per kind) | the kind's parameters | plain component, named by that kind's archetype |
| State component (optional) | result the evaluator reconciles | `FooStateComponent { FooState Value }` — `IIndexedComponent<FooState>`, change-only writes |

**Filter by the table, never by the bare key** (Table Rule): the family sweep is that kind's declared
archetype; a KIND slice or a STATE slice is a legal self-index — `store.ComponentIndex<FooKindComponent,
FooKind>()[kind]` (same for state). The write CALL re-files the row, so a state flip moves it between
slices automatically.

### 5 — (Optional) Evaluator family — the non-routing loop variant

When rows must be **re-checked over time** (each turn) and reconciled into a STATE column, add a **second** family. It
uses the SAME orchestrator+subsystem shape but a **different loop discipline** (below). Each evaluator reads its
kind-slice from the kind self-index, joins the target table by key (a `ComponentIndex` on the FK), and reconciles the
state component with **change-only writes**. Omit this whole part if the table is a static lookup joined on demand
(then it is spawn-only and needs no state component).

```csharp
internal sealed class BarFooEvaluatorSubSystem : FooEvaluatorSubSystem
{
    private readonly ComponentIndex<FooKindComponent, FooKind> _rowsByKind;   // kind slice (self-index)
    private readonly ComponentIndex<FooSubjectFKComponent, FooSubjectKey> _targetsByKey;

    public override void Evaluate()
    {
        // An empty bucket is a valid catalogue state, not an error — the loop simply does nothing.
        foreach (var row in _rowsByKind[FooKind.Bar])
        {
            var key = row.GetComponent<FooSubjectFKComponent>().Value;
            var satisfied = /* join: */ HasTarget(key);
            var next = satisfied ? FooState.Open : FooState.Closed;
            if (row.GetComponent<FooStateComponent>().Value != next)   // change-only write: no churn, no re-index
                row.AddComponent(new FooStateComponent { Value = next });
        }
    }
}
```

---

## Routing vs non-routing — the two orchestrator disciplines

Both host an `IReadOnlyList<TSubSystem>`; they differ in HOW they call it:

```clojure
(def routing  ;; the :spawn discipline
  {:loop           "per config entry, TrySpawn down the list, first match wins"
   :input          "one authored config → one owner"
   :unhandled-kind :FAIL-LOUD                          ;; a kind with no spawn-subsystem is an AUTHORING BUG
   :use-for        "materializing config → entities"})

(def non-routing  ;; the :evaluator discipline
  {:loop           "run EVERY enabled subsystem unconditionally"
   :input          "each subsystem self-queries its own rows"
   :unhandled-kind :silent-no-op                       ;; no evaluator yet = not-implemented-yet — quiet, expected
   :use-for        "periodic reconcile of existing rows"})
```

The loud/quiet asymmetry is deliberate — see the `;;` notes above.

## Per-kind rule (2026-07-15 FM-11 — kind is a COLUMN)

```clojure
(def per-kind-rule
  {:kind                 "FooKindComponent { FooKind Value }"  ;; ONE enum component per family, every row carries it, written once at birth
   :selection            "self-index ComponentIndex<FooKindComponent, FooKind>[kind]"  ;; never a second main tag, never payload-presence sniffing
   :payload              {:only-when "the kind has parameters"} ;; part of that kind's archetype — presence is NOT the kind selector (Birth Completeness)
   :second-main-tag      :NEVER                                 ;; breaks one main tag per archetype (Tag Law)
   :key-as-discriminator :NEVER})                               ;; the key is the SUBJECT (1:N per key expected); the kind names the RULE
```

## Dual-host reuse (when the evaluator must run at two lifecycles)

If the evaluator must cover **turn 1** (before the turn pipeline first runs) AND **every later turn**, register **two
thin host wrappers** over the SAME DI-collected subsystem list — no duplicated evaluation logic:
- a **bootstrap** host `IPrioritizedUniTaskSystem<MapGenerationStep>` (priority right after the spawn stage) for turn 1;
- a **turn-phase** host `: TurnPhaseSubSystem` (tail of the turn pipeline) for every subsequent turn.

Both inject the same `IReadOnlyList<FooEvaluatorSubSystem>` and just loop `Evaluate()`.

## DI wiring (collection injection)

In the domain installer, `Singleton` everything; register each subsystem **as the abstract base** so VContainer fills
the `IReadOnlyList<TBase>` (see `fmgraph.py resolve <TBase>` — collection resolution):

```csharp
builder.Register<FooSpawnSystem>(Lifetime.Singleton).As<IPrioritizedUniTaskSystem<MapGenerationStep>>();
builder.Register<BarFooSpawnSubSystem>(Lifetime.Singleton).As<FooSpawnSubSystem>();
builder.Register<BazFooSpawnSubSystem>(Lifetime.Singleton).As<FooSpawnSubSystem>();
// evaluator family (if any): both hosts + each concrete .As<FooEvaluatorSubSystem>()
```

The collected list field carries `[StateAllowed("reason")]` (a DI-owned collection is the deliberate-state exception, not a
zero-alloc violation).

## Adding a new kind (the payoff)

Three pieces, orchestrator untouched:
1. a concrete `FooConfig` subclass (its parameters, or none);
2. a new `FooKind` enum value (+ a payload component if the kind has parameters);
3. a `FooSpawnSubSystem` (and a `FooEvaluatorSubSystem` if the family evaluates), each registered `.As<…Base>()`.

## Checklist

```clojure
(def checklist
  {:container-SO       "FooConfig[] items; base carries ONLY the shared key; concretes pure data"
   :loader             "ConfigLoaderSystem<FoosConfig>: keeps only Box.Value, fails loud on null/empty, NEVER disposes the Box"  ;; a config loads once at ConfigLoading and lives the whole session
   :spawn-orchestrator "fail loud on null entry / unhandled type; subsystems bool TrySpawn — Try-pattern"
   :row                "key(FK) + ONE main tag (+ the family label tag) + kind component (+ payload, + state); consumers query the TABLE, never the bare key"
   :kind               "enum FooKindComponent on every row — selection via the kind self-index, never a second main tag"
   :evaluator          {:if-present "non-routing, reads its kind-slice, joins by key through a ComponentIndex, reconciles the state component change-only"}
   :dual-host          {:only-when "two lifecycles needed"}  ;; both reuse ONE subsystem list
   :di                 "all Singleton; subsystems .As<AbstractBase>(); collected field [StateAllowed("reason")]"})
```
