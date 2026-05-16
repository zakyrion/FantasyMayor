# FantasyMayor — Architecture Reference

## Stack
- Engine: Unity
- ECS: `DefaultEcs` with a DoD style, not Unity DOTS
- DI: `VContainer`
- Async: `UniTask`
- Asset loading: Unity `Addressables`
- Input: Unity `InputSystem`
- Rendering: Universal Render Pipeline

## Repository Map

```text
FantasyMayor/
├─ Assets/
│  ├─ Modules/                # Domain and feature modules
│  ├─ Scripts/                # Shared runtime primitives and app-root installers
│  ├─ Addressables/           # Authored addressable content and config assets
│  ├─ AddressableAssetsData/  # Addressables editor configuration
│  ├─ Scenes/                 # Unity scenes and sub-scenes
│  ├─ Prefabs/                # Global prefabs
│  ├─ Resources/              # Legacy/global resources
│  ├─ Materials/
│  ├─ Shader/
│  ├─ Plugins/                # Odin, Easy Save, Dreamteck, Animancer
│  ├─ Packages/               # Vendored package payloads under Assets
│  ├─ Settings/
│  ├─ Editor/
│  ├─ TextMesh Pro/
│  ├─ Schemes/
│  └─ TutorialInfo/
├─ Packages/                  # Unity package manifest and lock file
├─ CLAUDE.md
├─ SYSTEMTEMPLATE.md
└─ CONFIGTEMPLATE.md
```

## Source Of Truth
| What | Where |
|---|---|
| Feature and domain runtime code | `Assets/Modules/*` |
| Shared runtime primitives | `Assets/Scripts/Core` |
| Shared ECS base systems | `Assets/Scripts/DefaultECSExtensions` |
| App-root DI composition | `Assets/Scripts/Installers/*` |
| Authored config assets | `Assets/Addressables/Configs/*` |
| Scenes | `Assets/Scenes/*` |
| Package dependencies | `Packages/manifest.json` |

## Current Scene And Content Layout
- Main scenes: `Assets/Scenes/SampleScene.unity`, `Assets/Scenes/TerrainMask.unity`
- Sub-scene: `Assets/Scenes/SampleScene/GameplaySubScene.unity`
- Addressable config roots:
  - `Assets/Addressables/Configs/TerrainGenerationConfig`
  - `Assets/Addressables/Configs/TerrainViewConfigs`
  - `Assets/Addressables/Configs/UserInput`
- Additional addressable content: `Assets/Addressables/Textures`
- Addressables editor data is stored in `Assets/AddressableAssetsData/*`

## Shared Runtime Assemblies

| Assembly | Path | Responsibility |
|---|---|---|
| `Core` | `Assets/Scripts/Core` | Shared utility primitives such as `Box<T>`, `Result<T>`, disposal helpers, status primitives |
| `DefaultECS.Extensions` | `Assets/Scripts/DefaultECSExtensions` | ECS base system types such as `UpdatedSystem`, `LateUpdatedSystem`, `ConfigLoaderSystem`, `IUniTaskSystem<T>` |
| `Installers` | `Assets/Scripts/Installers/Addressable` | App-root DI registration for addressable services |
| `Installers.World` | `Assets/Scripts/Installers/World` | App-root world composition, input wiring, event cleanup |

## Current Modules

| Module | Assemblies | Current responsibility |
|---|---|---|
| `Addressable` | `Addressables.Core`, `Addressables.Implementations` | Public addressable loading contract and implementation |
| `AxialSystem` | `AxialSystem` | Axial hex math, coordinates, generic axial grid primitives |
| `Boot` | `Boot.Core`, `Boot.Implementation` | Boot pipeline step markers and boot MonoBehaviour orchestration |
| `Configs` | no dedicated asmdef | Generic config provider abstractions used by runtime code |
| `CurveBuilders` | `CurveBuilders` | Shared curve builder contract |
| `HexesCore` | `Hexes.Core` | Hex domain data, tags, grid and utility operations |
| `HexesUI` | `Hexes.UI` | UI Toolkit based controls and first-step UI flow |
| `MainCanvas` | `MainCanvas.Core`, `MainCanvas.Implementation`, `MainCanvas.Installer` | Root canvas abstraction, provider implementation, DI registration |
| `Pathfinding` | `Pathfinding`, `Pathfinding.Installer` | Hex pathfinding utility and its DI registration |
| `TerrainGenerator` | `Terrain.Generator` | Terrain generation configs, event trigger, generation systems for mountains, rivers, lakes, sea |
| `TerrainView` | `Terrain.View` | Terrain mesh, textures, water view, isolines, smoothing, runtime view systems |
| `UserInput` | `UserInput` | Camera and player input ECS bridge plus camera movement config flow |

## Module Layout Rules

Every new feature should still prefer `Assets/Modules/<FeatureName>/` and use this target layout:

```text
<FeatureName>/
├─ Components/   # Pure data structs, tags, event-components
├─ Configs/      # ScriptableObject class definitions
├─ Data/         # Collections, records, enums, helper types
├─ Systems/      # ECS systems and runtime orchestration
├─ Views/        # MonoBehaviour view layer
├─ Prefabs/      # Module-scoped prefabs, uxml, uss
└─ Installer/    # Feature-specific VContainer registration
```

Placement rules:
- New domain code goes to `Assets/Modules/<FeatureName>/`
- Feature-specific DI goes to `Assets/Modules/<FeatureName>/Installer/`
- Cross-cutting or app-root DI goes to `Assets/Scripts/Installers/`
- UI assets go to `Assets/Modules/<FeatureName>/Prefabs/`

Folder contract:
| Folder | Never contains |
|---|---|
| `Components/` | Logic or side effects |
| `Configs/` | Runtime logic or config assets |
| `Data/` | ECS systems or MonoBehaviours |
| `Systems/` | View logic or config definitions |
| `Views/` | Business logic |
| `Installer/` | Anything except DI registration |

## Current Structure Notes And Exceptions
- The repository is partially standardized, not fully uniform.
- `TerrainGenerator` and `TerrainView` are the closest matches to the full module template.
- `HexesUI` currently uses `Installer`, `Systems`, and `Prefabs`, but does not have the full standard folder set.
- `AxialSystem`, `CurveBuilders`, and `Configs` are flatter utility-style modules rather than full feature modules.
- `Boot`, `Addressable`, and `MainCanvas` use explicit contract and implementation splits.
- `Pathfinding` has a root runtime assembly plus a separate installer assembly.
- `UserInput` currently has `Components`, `Configs`, and `Systems`, but no module-local installer folder.
- `Assets/Scripts/Extentions` is a legacy typo-named folder and should be treated as existing structure, not a naming standard.

## Public Contract Split
- When a module exposes a reusable public API, prefer a `Core/` contract assembly plus an `Implementation/` assembly.
- Current canonical examples are `Addressable`, `MainCanvas`, and `Boot`.

## ECS And Runtime Conventions
- Single-component entity creation may chain: `world.CreateEntity().Set(...)`
- Once more than one component is assigned, stop chaining and use a local entity variable
- In `MonoBehaviour` code, prefer `System.Collections.Generic`
- In ECS systems, prefer `Unity.Collections` and dispose persistent allocations explicitly
- Prefer instance-based design; use `static` only when a type is truly stateless utility infrastructure

## Boot And System Flow
- Boot step markers currently live in `Boot.Core` as `ConfigLoadStep`, `FirstUIStep`, and `TerrainGenerationStep`
- Config initialization systems should derive from `ConfigLoaderSystem`
- Regular per-frame logic should derive from `UpdatedSystem`
- Late-frame logic should derive from `LateUpdatedSystem`
- Ordered async subsystem pipelines can use `IUniTaskSystem<T>` and `UniTaskSequentialSystem<T>`

## On-Demand References
- For `IAddressable`, `Box<T>`, `Result<T>`, or addressable ownership rules, read `Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md`
- For terrain transition work, pre-read:
  - `Assets/Modules/TerrainView/Isolines/FieldBasedIsolineBuilder.cs`
  - `Assets/Modules/TerrainView/Isolines/IsolineSlopeTransition.cs`
  - `Assets/Modules/TerrainView/Smooth/HeightSmoothing.cs`
- For water-view-specific setup details, read `Assets/Modules/TerrainView/WATER_VIEW_SETUP.md`
