---
category: B
read: trigger
trigger: "before adding a config to the game or building objects from a config at startup"
tags: [pattern, config, systems]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_CONFIG](PATTERN_CONFIG.md)"
---

# Pattern — Config Loader System

One installer registration of `ConfigLoaderSystem<T>` loads a config; derived objects are `InstanceObjects` systems.

## Loading a config — one registration

```csharp
builder.RegisterAppStateSystem<ConfigLoaderSystem<[ConfigName]>>(Lifetime.Singleton, AppState.ConfigLoading)
    .WithParameter("address", ConfigAddresses.[CONFIG_NAME]);
```

`ConfigLoaderSystem<T>` loads the asset, throws when the load fails, calls `Validate()` when the SO implements
`IValidatableConfig`, and puts the SO into `EntityStorages`. It never releases the asset. No subclass, no
per-config loader file.

## Building objects from configs — an InstanceObjects system

```csharp
[UsedImplicitly]
public sealed class [Name]SpawnSystem : IUniTaskSystem
{
    private readonly EntityStorages _storages;

    public [Name]SpawnSystem(AppState appState, EntityStorages storages)
    {
        AppState = appState;
        _storages = storages;
    }

    public AppState AppState { get; }

    public UniTask Execute(CancellationToken cancellationToken)
    {
        var config = _storages.Get<[ConfigName]>();
        _storages.Singletons.Set(new [Name]Component { Value = new [RuntimeObject](config.Field) });

        return UniTask.CompletedTask;
    }

    public void Dispose()
    {
    }
}
```

```csharp
builder.RegisterAppStateSystem<[Name]SpawnSystem>(Lifetime.Singleton, AppState.InstanceObjects);
```

## Rules

```clojure
(def config-loader-rules
  {:loader       "ConfigLoaderSystem<T> only — registered per config type in the feature's installer"
   :address      "a const in ConfigAddresses (Ecs.Extensions), passed via WithParameter(\"address\", …) — never a string literal"  ;; SCREAMING_SNAKE_CASE named by the config type; value = the addressable entry name
   :runner       "the ConfigLoading state, then the InstanceObjects state, awaits on entry every IUniTaskSystem whose AppState flags contain its Mode"  ;; one after another, in registration order; Boot only switches the states (ARCHITECTURE → :wiring/state-by-mode)
   :registration "RegisterAppStateSystem<T>(Lifetime, AppState flags) — the one registration of a first-order system; it exposes IAppStateSystem and hands over the flags"
   :validation   "inside the SO (IValidatableConfig) — never in a system"
   :instance-step {:may      "build runtime singleton components or objects derived from configs"  ;; e.g. VertexGridSpawnSystem → VertexGridComponent
                   :must-not "load assets or run per-frame / gameplay logic"}
   :cancellation "ThrowIfCancellationRequested after every await"   ;; never a quiet return
   :never        #{"a …ConfigComponent" "a per-config loader subclass" "releasing a config"}})
```
