---
category: B
read: trigger
trigger: "before creating a ScriptableObject config and its runtime component"
tags: [pattern, config]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_CONFIG_LOADER](PATTERN_CONFIG_LOADER.md)"
---

# Pattern — Config (ScriptableObject + Component)

Authored data lives in a `ScriptableObject`, loaded via Addressables, and published as a **world component**
that is the runtime source of truth. The SO is an authoring artifact only; runtime systems read the component
— except a catalogue component that deliberately wraps the SO reference (see below).

## 1. The ScriptableObject

```csharp
using UnityEngine;

namespace Domains.[Domain].[Feature].Configs
{
    [CreateAssetMenu(fileName = "[ConfigName]", menuName = "FantasyMayor/[Group]/[ConfigName]")]
    public sealed class [ConfigName] : ScriptableObject
    {
        [SerializeField] private [FieldType] _field;
        public [FieldType] Field => _field;
    }
}
```

**Container (catalogue) variant** — one SO holding an array of sub-config SOs (e.g. a roster of districts):

```csharp
[SerializeField] private [ItemConfig][] _items;
public [ItemConfig][] Items => _items;
```

## 2. The component (runtime source of truth)

Two shapes — pick by what the SO holds:

- **Flatten (default for scalar / tunable data):** copy values out; the loader releases the SO after copying.
  ```csharp
  public struct [Name]ConfigComponent
  {
      public [FieldType] Field;
      public static [Name]ConfigComponent FromConfig([ConfigName] c) =>
          new [Name]ConfigComponent { Field = c.Field };
  }
  ```
- **Wrap the SO reference (catalogues / engine references — prefabs, sprites, sub-config lists):** the
  component holds the live SO reference; the loader keeps the addressable handle alive for its lifetime.
  ```csharp
  public readonly struct [Name]ConfigComponent
  {
      public readonly [ConfigName] Value;
      public [Name]ConfigComponent([ConfigName] value) => Value = value;
  }
  ```

## Rules

- **The SO is authoring-only.** Runtime reads the component (`world.Get<[Name]ConfigComponent>()`), never the
  asset — unless the component is the wrap variant, whose whole job is to carry the SO reference.
- **Flatten vs wrap:** flatten pure values (no asset lifetime to manage); **wrap** when entries carry engine
  references or a sub-config list consumed lazily — a flattened copy would lose them. Wrapping is the norm for
  catalogue configs; record the wrapped component in `ecs-graph`.
- **Authored validity** (one entry per type, no null entries, non-empty lists) is validated at load — see
  [PATTERN_CONFIG_LOADER](PATTERN_CONFIG_LOADER.md), not in the SO.
- The config component is a **world component** (`world.Set`), never an entity. Storage taxonomy:
  `../ARCHITECTURE.md`.
```
