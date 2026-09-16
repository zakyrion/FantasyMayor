---
category: B
read: trigger
trigger: "before creating or reading a ScriptableObject config"
tags: [pattern, config]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_CONFIG_LOADER](PATTERN_CONFIG_LOADER.md)"
---

# Pattern — Config (ScriptableObject)

A config is a `ScriptableObject` kept in `EntityStorages` by type and read via `storages.Get<T>()`.

## 1. The ScriptableObject

```csharp
using System;
using EcsExtensions;
using UnityEngine;

namespace Domains.[Domain].[Feature].Configs
{
    [CreateAssetMenu(fileName = "[ConfigName]", menuName = "FantasyMayor/[Group]/[ConfigName]")]
    public sealed class [ConfigName] : ScriptableObject, IValidatableConfig
    {
        [SerializeField] private [FieldType] _field;
        public [FieldType] Field => _field;

        // Optional: implement IValidatableConfig only when the authored data can be wrong.
        public void Validate()
        {
            if (_field == null)
                throw new InvalidOperationException("[ConfigName]: Field is null.");
        }
    }
}
```

**Container (catalogue) variant** — one SO holding an array of sub-config SOs (e.g. a roster of districts):

```csharp
[SerializeField] private [ItemConfig][] _items;
public [ItemConfig][] Items => _items;
```

**Two assets of one shape** — the storage key is the type, so each stored instance needs its own type: an abstract
base with one sealed subclass per asset (`IsolineConfig` → `InnerIsolineConfig`, `OuterIsolineConfig`).

## 2. Reading it

```csharp
var config = _storages.Get<[ConfigName]>();   // throws when the config was not loaded
```

## Rules

```clojure
(def config-rules
  {:storage     "EntityStorages by type — storages.Add<T> at load, storages.Get<T> everywhere else"  ;; never a …ConfigComponent, never a queryable entity table
   :read        "the SO itself; no copy, no flatten"                    ;; Get<T> throws — no Has-guard and no silent skip around it
   :lifetime    "loaded once at AppState.ConfigLoading, never released"  ;; configs live for the whole session
   :mutation    :never                                                   ;; arrays are shared by every reader — read-only, no defensive clone
   :key         "one stored instance per type"                           ;; a second asset of the same shape = a sealed subclass
   :validation  "IValidatableConfig.Validate on the SO; the loader calls it before Add"  ;; one-entry-per-type, no nulls, non-empty — throw
   :derived     "objects built from a config belong to an AppState.InstanceObjects system"  ;; PATTERN_CONFIG_LOADER
   :off-thread  "reading SO fields inside RunOnThreadPool is allowed"})  ;; plain immutable managed data, not store access (ARCHITECTURE → Threading and structural change)
```
