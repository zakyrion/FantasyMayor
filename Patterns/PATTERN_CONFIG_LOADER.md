---
category: B
read: trigger
trigger: "before creating a config loader system"
tags: [pattern, config, systems]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_CONFIG](PATTERN_CONFIG.md)"
---

# Pattern — Config Loader System

A one-shot system that runs at `ConfigLoadStep`: loads config SO(s) from Addressables, validates them, and
publishes the world component(s). Subclass `ConfigLoaderSystem`. One approach.

## Skeleton

```csharp
[UsedImplicitly]
internal sealed class [Name]ConfigLoaderSystem : ConfigLoaderSystem
{
    private const string [Name]ConfigKey = "[addressable key]";

    public [Name]ConfigLoaderSystem(IAddressable addressable, EntityStore world) : base(addressable, world) { }

    protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
    {
        var configBox = Box<[ConfigName]>.Empty();
        try
        {
            configBox = await LoadConfigAsync<[ConfigName]>([Name]ConfigKey, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            // Validate BEFORE publishing — throw on any authoring violation (null entries, dup keys, empty).
            // e.g. throw new InvalidOperationException("[Name]ConfigLoaderSystem: entry N is null.");

            World.SetWorldComponent([Name]ConfigComponent.FromConfig(configBox.Value));   // FLATTEN variant
            MarkAsLoaded();
        }
        finally
        {
            DisposeBox(ref configBox);   // FLATTEN: release the SO after copying
        }
    }
}
```

**WRAP variant** (component holds the live SO reference): do NOT dispose the box in `finally` — retain it in a
field and release in `OnDispose`, because the published component points at the SO.

```csharp
private Box<[ConfigName]> _config = Box<[ConfigName]>.Empty();
// ...in LoadConfigsAsync: _config = await LoadConfigAsync<...>(...); World.SetWorldComponent(new [Name]ConfigComponent(_config.Value)); MarkAsLoaded();
protected override void OnDispose() => DisposeBox(ref _config);
```

## Rules

```clojure
(def config-loader-rules
  {:box-flatten  "release in finally"
   :box-wrap     "RETAIN in a field, release in OnDispose"   ;; disposing while the component holds the SO reference would dangle it (PATTERN_CONFIG)
   :order        (-> validate MarkAsLoaded)                  ;; publishing garbage and continuing hides the bug — fail loud; LoadConfigAsync already throws on failed load, keep it
   :publish      "SetWorldComponent"                         ;; never an entity for the config itself
   :loader       {:may      "build derived runtime world components"  ;; e.g. a grid from the config
                  :must-not "per-frame or gameplay logic"}
   :quiet-return "cancellationToken.IsCancellationRequested ONLY"
   :wiring       "installer .As<IUniTaskSystem<ConfigLoadStep>>"})  ;; Boot runs all loaders sequentially at startup; addressable ownership: ADDRESSABLE_PATTERNS.md
```
