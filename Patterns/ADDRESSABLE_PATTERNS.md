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
}

namespace Core;
enum Status { Unknow = 0, Failed, Success, Cancelled }

struct Result<T> {
    Box<T> Box;            // valid only when Status == Success
    Status Status;
    static Result<T> Success(T value, Action<T> dispose = null);
    static Result<T> Cancelled();
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
1. `Status.Success` → exactly one `result.Box.Dispose()`. Leak otherwise (handle + instance for `LoadAndInstanceAsync`).
2. `Status != Success` → do nothing; impl already cleaned up. Box is `default`.
3. Release of `LoadAndInstanceAsync` destroys the `GameObject`. Never `Object.Destroy` it yourself.
4. `LoadAsync<GameObject>` is guarded → `Fail`. Use `LoadAndInstanceAsync` for prefabs.
5. After every await re-check **both** `token.IsCancellationRequested` and `result.Status`. Dispose if cancelled post-success.
6. One owner per `Box<T>`. Disposed box → set field to `Box<T>.Empty()`.
7. Always inject `IAddressable` via constructor. Never touch `UnityEngine.AddressableAssets.Addressables` outside the impl.
8. Any class that stores a `Box<T>` field must implement `IDisposable` (or otherwise route disposal).

## Patterns

### Load non-GameObject asset
```csharp
var r = await _addressable.LoadAsync<MyConfig>("MyConfigAddress", token);
if (token.IsCancellationRequested || r.Status != Status.Success) return;
_box = r.Box;
// use _box.Value
// teardown: _box.Dispose(); _box = Box<MyConfig>.Empty();
```

### Load + instantiate prefab, expose typed component
```csharp
var r = await _addressable.LoadAndInstanceAsync("UI/MyPanel", token, parent);
if (token.IsCancellationRequested || r.Status != Status.Success) return Box<MyPanel>.Empty();
var c = r.Box.Value.GetComponent<MyPanel>();
if (c == null) { r.Box.Dispose(); return Box<MyPanel>.Empty(); }
return Box<MyPanel>.Wrap(c, _ => r.Box.Dispose()); // disposal cascades to instance + handle
```

### Sequential loads with rollback
```csharp
var a = await Load<A>(KEY_A, t); if (t.IsCancellationRequested || !a.Exist) return;
var b = await Load<B>(KEY_B, t); if (t.IsCancellationRequested || !b.Exist) { a.Dispose(); return; }
var c = await Load<C>(KEY_C, t); if (t.IsCancellationRequested || !c.Exist) { a.Dispose(); b.Dispose(); return; }
_a = a; _b = b; _c = c; // commit to fields

async UniTask<Box<T>> Load<T>(string addr, CancellationToken t) where T : class {
    var r = await _addressable.LoadAsync<T>(addr, t);
    if (t.IsCancellationRequested || r.Status == Status.Cancelled) return Box<T>.Empty();
    if (r.Status != Status.Success || !r.Box.Exist) {
        Debug.LogError($"Failed to load asset by address '{addr}'.");
        return Box<T>.Empty();
    }
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

### ECS handoff
Loader system retains `Box<T>` ownership; component carries `Value` only.
Config components are stored as **world components** (see `Patterns/PATTERN_CONFIG.md`), not on an entity:
```csharp
_world.Set(new MyConfigComponent { Value = _config.Value });
```

## Anti-patterns
| Wrong | Why | Right |
|---|---|---|
| `LoadAsync<T>(nameof(T), token)` | `nameof(T)` is the literal `"T"`, not the type name | explicit address const |
| `LoadAsync<GameObject>(...)` | guarded → `Fail` | `LoadAndInstanceAsync` |
| `Object.Destroy(result.Box.Value)` | release already destroys → double-destroy | just `Box.Dispose()` |
| Dispose on `Failed` / `Cancelled` | nothing to release; ownership confusion | branch on `Success` |
| Store `Box.Value`, drop `Box` | leaks handle (and instance) | store the `Box<T>` |
| Same `Box<T>` passed to two owners | both call Dispose; ambiguous lifetime | one owner; re-wrap with separate release |
| Read `Value` without `Exist` / `Status` check | throws | check first |
| Skip token re-check after await | caller-side cancellation missed | check token AND status |
| `Box<T>` field with no `IDisposable` | leak across scope teardown | implement disposal (see pattern) |

## Conventions
- Address keys: `private const string FOO_BAR_CONFIG = "FooBarConfig";` (SCREAMING_SNAKE_CASE field, value = addressable address).
- Loaders: `Modules/<Feature>/Systems/<Feature>ConfigLoaderSystem.cs`. ECS components: `Modules/<Feature>/Components/<Name>ConfigComponent { public <Name>Config Value; }`.
- If a situation does not match any pattern above, stop and ask before inventing a new one.
