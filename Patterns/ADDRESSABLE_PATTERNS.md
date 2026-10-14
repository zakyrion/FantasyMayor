---
category: B
read: trigger
trigger: "before writing/editing/reviewing Addressables, IAddressable, Box<T> or Result<T> code"
tags: [addressables, patterns, ecs]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_CONFIG_LOADER](PATTERN_CONFIG_LOADER.md)"
---

# IAddressable Contract

Single source of truth for addressable loading. Read this; do not grep.

## Files
- `Assets/Modules/Addressable/Core/IAddressable.cs` — interface
- `Assets/Modules/Addressable/Implementation/Addressable.cs` — impl, VContainer-aware (`IObjectResolver.Instantiate`)
- `Assets/Scripts/Core/{Result,Box,Status}.cs` — return types
- `Assets/Scripts/Installers/Addressable/AddressableInstaller.cs` — app-root installer that registers `IAddressable` with `Lifetime.Scoped`

## API
```csharp
namespace Modules.Addressable.Core;
interface IAddressable {
    UniTask<Result<GameObject>> LoadAndInstanceAsync       (string asset, CancellationToken token, Transform root = null);
    UniTask<Result<T>>          LoadAndInstanceAsync<T>    (string asset, CancellationToken token, Transform root = null) where T : Component; // instantiates prefab, returns typed component
    UniTask<Result<T>>          LoadAsync<T>               (string asset, CancellationToken token) where T : class; // T != GameObject
    // all three: a cancelled token → OperationCanceledException, after releasing whatever was loaded
}

namespace Core;
enum Status { Unknow = 0, Failed, Success }

struct Result<T> {
    Box<T> Box;            // valid only when Status == Success
    Status Status;
    static Result<T> Success(T value, Action<T> dispose = null);
    static Result<T> Fail();
}

readonly struct Box<T> {
    T    Value;            // throws InvalidOperationException if !Exist
    bool Exist;            // false for default / empty / disposed
    void Dispose();        // idempotent; invokes the release Action<T> exactly once
    static Box<T> Wrap(T value, Action<T> dispose);
    static Box<T> Empty();
}
```

`Box<T>` is a struct wrapping a class `BoxHandle<T>` — copies share the handle; disposing any disposes all.

## Invariants
1. The `Box`, not the `Status`, decides release: `Exist` says something is inside, and then exactly one owner calls `Dispose()`. Leak otherwise (handle + instance for `LoadAndInstanceAsync`).
2. `Status != Success` → the `Box` is empty; `Dispose()` on an empty or `default` Box is a no-op, so an unconditional release in `finally` is safe.
3. Release of `LoadAndInstanceAsync` destroys the `GameObject`. Never `Object.Destroy` it yourself.
4. `LoadAsync<GameObject>` is guarded → `Fail`. Use `LoadAndInstanceAsync` for prefabs.
5. Cancellation is an exception, not a `Status`: a cancelled call throws `OperationCanceledException` and no `Box` reaches you. After your own later awaits call `token.ThrowIfCancellationRequested()`, and release boxes you still hold in `try/finally`.
6. One owner per `Box<T>`. Disposed box → set field to `Box<T>.Empty()`.
7. Always inject `IAddressable` via constructor. Never touch `UnityEngine.AddressableAssets.Addressables` outside the impl.
8. Any class that stores a `Box<T>` field must implement `IDisposable` (or otherwise route disposal).

## Patterns

### Load non-GameObject asset
```csharp
var r = await _addressable.LoadAsync<MyConfig>("MyConfigAddress", token);   // throws on cancel
if (r.Status != Status.Success)
    throw new InvalidOperationException("Failed to load MyConfig by address 'MyConfigAddress'.");
_box = r.Box;
// use _box.Value
// teardown: _box.Dispose(); _box = Box<MyConfig>.Empty();
```

### Load + instantiate prefab, expose typed component
```csharp
var r = await _addressable.LoadAndInstanceAsync("UI/MyPanel", token, parent);   // throws on cancel
if (r.Status != Status.Success)
    throw new InvalidOperationException("Failed to load prefab 'UI/MyPanel'.");
var c = r.Box.Value.GetComponent<MyPanel>();
if (c == null) { r.Box.Dispose(); throw new InvalidOperationException("Prefab 'UI/MyPanel' has no MyPanel component."); }
return Box<MyPanel>.Wrap(c, _ => r.Box.Dispose()); // disposal cascades to instance + handle
```

### Sequential loads with rollback
```csharp
var a = Box<A>.Empty(); var b = Box<B>.Empty(); var c = Box<C>.Empty();
try
{
    a = await Load<A>(KEY_A, t);
    b = await Load<B>(KEY_B, t);
    c = await Load<C>(KEY_C, t);
    _a = a; _b = b; _c = c;                                      // commit: the fields own the handles now
    a = Box<A>.Empty(); b = Box<B>.Empty(); c = Box<C>.Empty();  // so finally releases nothing
}
finally
{
    DisposeBox(ref a); DisposeBox(ref b); DisposeBox(ref c);     // cancel or failure: release what loaded before it
}

async UniTask<Box<T>> Load<T>(string addr, CancellationToken t) where T : class {
    var r = await _addressable.LoadAsync<T>(addr, t);            // throws on cancel
    if (r.Status != Status.Success || !r.Box.Exist)
        throw new InvalidOperationException($"Failed to load asset by address '{addr}'.");
    return r.Box;
}
```

### Field disposal
```csharp
public void Dispose() {
    if (_isDisposed) return;
    _isDisposed = true;
    DisposeBox(ref _config);
}
static void DisposeBox<T>(ref Box<T> b) {
    if (!b.Exist) return;
    b.Dispose();
    b = Box<T>.Empty();
}
```

### Config handoff
Configs are the one deliberate exception to invariant 1: `ConfigLoaderSystem<T>` keeps only `Box.Value` and never
disposes the `Box` — a config lives for the whole session (see `Patterns/PATTERN_CONFIG.md`):
```csharp
_storages.Add(result.Box.Value);
```

## Anti-patterns
| Wrong | Why | Right |
|---|---|---|
| `LoadAsync<T>(nameof(T), token)` | `nameof(T)` is the literal `"T"`, not the type name | explicit address const |
| `LoadAsync<GameObject>(...)` | guarded → `Fail` | `LoadAndInstanceAsync` |
| `Object.Destroy(result.Box.Value)` | release already destroys → double-destroy | just `Box.Dispose()` |
| Store `Box.Value`, drop `Box` | leaks handle (and instance) | store the `Box<T>` |
| Same `Box<T>` passed to two owners | both call Dispose; ambiguous lifetime | one owner; re-wrap with separate release |
| Read `Value` without `Exist` / `Status` check | throws | check first |
| `if (token.IsCancellationRequested) return;` | the caller sees a completed task and carries on over unfinished work | `token.ThrowIfCancellationRequested()` |
| Release in a branch right before the throw | every exit path needs its own copy; one gets missed | `try/finally` |
| `Box<T>` field with no `IDisposable` | leak across scope teardown | implement disposal (see pattern) |

## Conventions
- Address keys: `private const string FOO_BAR_CONFIG = "FooBarConfig";` (SCREAMING_SNAKE_CASE field, value = addressable address).
- Configs: no loader file and no component — one `ConfigLoaderSystem<[Name]Config>` registration in the feature installer, address passed via `WithParameter("address", ConfigAddresses.[NAME]_CONFIG)`; every config address lives in `ConfigAddresses` (`Patterns/PATTERN_CONFIG_LOADER.md`).
- If a situation does not match any pattern above, stop and ask before inventing a new one.
