---
category: B
read: trigger
trigger: "before working with a config, config component, or loader flow"
tags: [template, config]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[CONFIGS](Assets/Modules/Configs/CONFIGS.md)"
---

# FantasyMayor — Config Template Catalog

How to create config-related classes: the authored `ScriptableObject`, its flattened ECS component,
and the Config Loader that connects them. This file is the canonical source of truth for the config
flow — `SYSTEMTEMPLATE.md` only points here.

The flow is always the same three pieces:

```
[Config].asset (authored SO, Addressables)
   → [Name]ConfigLoaderSystem (ConfigLoadStep, one-shot)
       → world.Set([Name]ConfigComponent)   ← WORLD component, the runtime source of truth
```

---

## 1 — Config (ScriptableObject)

CONDITION:
- Config data must be authored in the Unity editor and loaded via Addressables.

SKELETON:

```csharp
using UnityEngine;

namespace Modules.[ModuleName].Configs
{
    [CreateAssetMenu(fileName = "[ConfigName]", menuName = "FantasyMayor/[ModuleGroup]/[ConfigMenuName]")]
    public class [ConfigName] : ScriptableObject
    {
        [Header("[GroupName]")]
        public [FieldType] [fieldName];
        // All authored fields: engine resources (prefabs, sprites) and tunable values.
    }
}
```

RULES:
- The SO is an **authoring artifact only**. Runtime systems never hold or read the SO — they read
  the flattened component. The loader does not retain the SO after flattening (exception: a config
  whose entries carry engine references consumed lazily, e.g. prefab lists — then the component
  wraps the SO reference and records it in the ecs-graph (`/ecs-graph`)).
- Authored uniqueness constraints (e.g. "one entry per ResourceType") are validated at load — see
  block 3.

## 2 — Config Component (struct)

CONDITION:
- Runtime ECS representation of a loaded config; systems read flattened values.

SKELETON:

```csharp
using Modules.[ModuleName].Configs;

namespace Modules.[ModuleName].Components
{
    public struct [Name]ConfigComponent
    {
        public [FieldType] [FieldName];

        public static [Name]ConfigComponent FromConfig([ConfigName] config)
        {
            return new [Name]ConfigComponent
            {
                [FieldName] = config.[fieldName],
            };
        }
    }
}
```

RULES:
- Name: `[Name]ConfigComponent`. Flattening lives in a static `FromConfig` on the component.
- Copy VALUES out of the SO (flatten). Wrap the SO reference only for engine-resource configs that
  cannot be flattened (see rule in block 1).

## 3 — Config Loader System

CONDITION:
- Module initialization during `ConfigLoadStep`: load config(s) from Addressables, publish the
  flattened world component(s), prepare derived runtime data other systems require.

SKELETON:

```csharp
[UsedImplicitly]
public sealed class [Name]ConfigLoaderSystem : ConfigLoaderSystem
{
    private const string [Name]_CONFIG = "[addressable key]";

    public [Name]ConfigLoaderSystem(IAddressable addressable, World world) : base(addressable, world) { }

    protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
    {
        var configBox = Box<[ConfigName]>.Empty();

        try
        {
            configBox = await LoadConfigAsync<[ConfigName]>([Name]_CONFIG, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            World.Set([Name]ConfigComponent.FromConfig(configBox.Value));
            // Optional: derived runtime data other systems require (see RULES).
            MarkAsLoaded();
        }
        finally
        {
            DisposeBox(ref configBox);
        }
    }
}
```

RULES — what a loader can do:
- load one or many configs from Addressables (`LoadConfigAsync` throws on a failed load — keep it);
- `world.Set` the flattened config component(s);
- create derived runtime data required by other systems — including runtime WORLD components
  (reference: `TerrainViewConfigLoaderSystem` also builds the `VertexGrid` and
  `world.Set`s `VertexGridComponent`) and, rarely, initialization entities;
- **validate** the loaded data before `MarkAsLoaded()` — duplicate keys, missing prefabs, empty
  entry lists are errors: throw a descriptive exception (Fail Loud, `ARCHITECTURE.md`). A loader
  that "loads" garbage and continues hides the bug.

What a loader must NOT do:
- per-frame or gameplay logic; reacting to gameplay events;
- `world.CreateEntity().Set(...)` for the config component itself — the config is a WORLD
  component, never an entity.

STORAGE RULE (normative, see `ARCHITECTURE.md` → "State Storage Taxonomy"):
- The flattened config component is stored as a **world component** via `World.Set(...)`.
- Consumers read it with `World.Get<[Name]ConfigComponent>()`, guarded by `World.Has<...>()`;
  systems for which the config is a hard prerequisite throw on the miss (second barrier).
- A world component is not query-matchable (`With<T>` / `WhenAdded<T>` / `WhenChanged<T>` do not see
  it — full contract in `ARCHITECTURE.md`). So if a config must drive reactive systems at runtime (live
  re-tuning), `world.Set` the new value AND raise a one-frame pulse event; the reactive consumer
  re-reads the world component (`SYSTEMTEMPLATE.md` Template 1).
- Config components appear in the ecs-graph (`/ecs-graph`) as world components; refresh it when
  adding one.

WIRING:
- Register the loader in the module installer as `IUniTaskSystem<ConfigLoadStep>`; `Boot` runs all
  loaders sequentially at startup, before the state machine starts.

## 4 — IAddressable

CONDITION:
- Any code that touches `IAddressable`, `Box<T>`, `Result<T>`, or addressable asset ownership.

DO:
- Read `Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md` first. It is the single source of truth
  for load/ownership/disposal patterns — do not re-derive them.
