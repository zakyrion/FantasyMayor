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

    public [Name]ConfigLoaderSystem(IAddressable addressable, World world) : base(addressable, world) { }

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

            World.Set([Name]ConfigComponent.FromConfig(configBox.Value));   // FLATTEN variant
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
// ...in LoadConfigsAsync: _config = await LoadConfigAsync<...>(...); World.Set(new [Name]ConfigComponent(_config.Value)); MarkAsLoaded();
protected override void OnDispose() => DisposeBox(ref _config);
```

## Rules

- **Flatten → release the box** in `finally`. **Wrap → RETAIN the box**, release in `OnDispose` — disposing it
  while the component holds the SO reference would dangle it. See [PATTERN_CONFIG](PATTERN_CONFIG.md).
- **Validate, then `MarkAsLoaded()`.** A loader that publishes garbage and continues hides the bug — fail loud
  (`../ARCHITECTURE.md`). `LoadConfigAsync` already throws on a failed load — keep it.
- **Publish a world component** (`World.Set`), never `CreateEntity` for the config itself.
- A loader MAY build derived runtime world components (e.g. a grid from the config); it MUST NOT do per-frame
  or gameplay logic.
- `cancellationToken.IsCancellationRequested` is the only quiet `return`.
- **Wiring:** register in the module installer as `IUniTaskSystem<ConfigLoadStep>`; `Boot` runs all loaders
  sequentially at startup. Addressable ownership: `Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md`.
```
