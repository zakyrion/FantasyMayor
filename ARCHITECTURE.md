# FantasyMayor — Architecture Reference

## Stack
- Engine: Unity
- ECS: `DefaultEcs` (DoD approach, NOT Unity DOTS)
- DI: `VContainer`
- Async: `UniTask`
- Assets: `Addressables`

---

## Repository Map

```text
FantasyMayor/
├─ Assets/
│  ├─ Modules/        # Domain modules (feature code)
│  ├─ Scripts/        # Core primitives + DI installers
│  ├─ Scenes/
│  ├─ Resources/
│  ├─ Addressables/
│  ├─ Prefabs/
│  └─ Plugins/        # Odin, EasySave, Dreamteck, Animancer
└─ Packages/
```

**Source of truth:**
| What | Where |
|---|---|
| Domain logic | `Assets/Modules/*` |
| Base abstractions | `Assets/Scripts/Core` |
| Content / data | `Assets/Scenes`, `Assets/Addressables` |

---

## Module Structure

Every feature lives in `Assets/Modules/<FeatureName>/`:

```text
<FeatureName>/
├─ Components/   # Pure data structs, tags, event-components
├─ Configs/      # ScriptableObject class definitions (not assets)
├─ Data/         # Collections, records, enums, helper types
├─ Systems/      # ECS systems, runtime orchestration
├─ Views/        # MonoBehaviour view layer
├─ Prefabs/      # Module-scoped prefabs, uxml, uss
└─ Installer/    # VContainer LifetimeScope registration
```

**Folder contract (hard rules):**

| Folder | NEVER contains |
|---|---|
| `Components/` | Logic, side effects |
| `Configs/` | Runtime logic, scriptable assets |
| `Data/` | ECS systems, MonoBehaviours |
| `Systems/` | View logic, config definitions |
| `Views/` | Business logic |
| `Installer/` | Anything except DI registration |

---

## Existing Modules

| Module | Assembly | Purpose |
|---|---|---|
| `AxialSystem` | `AxialSystem` | Hex grid math (`HexCoord`, `AxialMath`) |
| `HexesCore` | `Hexes.Core` | Hex domain: components, events, spawn |
| `TerrainView` | `Terrain.View` | 3D terrain mesh from `HexGrid` |
| `HexesUI` | `Hexes.UI` | UI layer for hex generation |
| `MainCanvas` | `MainCanvas.Core` / `.Implementation` | Root canvas abstraction |
| `Addressable` | `Addressables.Core` / `.Implementations` | Addressables wrapper |
| `CurveBuilders` | `CurveBuilders` | Shared curve builder contracts |

---

## Coding Rules

### Rule: New feature placement
```
New domain code  → Assets/Modules/<FeatureName>/
DI registration  → Assets/Scripts/Installers/
UI assets        → Assets/Modules/<FeatureName>/Prefabs/
```

### Rule: Module API split
> Separate contract from implementation — like `MainCanvas` and `Addressable` modules.

---

## ECS Code Style (DefaultEcs)

entity_creation(1)  -> world.CreateEntity().Set(...);
entity_creation(N)  -> var entity = world.CreateEntity();
when N > 1           entity.Set(...); entity.Set(...);
entity_creation(_)  -> FORBID chaining;

method_creation(static_class)     -> static method;
method_creation(instance_class)   -> instance method;

collection_creation(MonoBehaviour)         -> use Generic.Collections;
collection_creation(DefaultECSSystem)      -> use Unity.Collections;
than dispose 