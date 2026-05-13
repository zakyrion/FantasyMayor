# CODEX.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# FantasyMayor: Project Context & Architectural Decisions

## Start Working
- Read ARCHITECTURE.md

## Stack
Engine: Unity | ECS: DefaultEcs (DoD, NOT Unity DOTS) | DI: VContainer | Async: UniTask | Assets: Addressables

## User Process Contract
- User-defined process and repository rules are mandatory and override agent-default workflows.
- Do not substitute personal heuristics, convenience patterns, or alternate execution flow when the user has already defined the process.
- Ignoring these instructions is considered a serious execution failure and must be treated as carrying complaint and legal escalation risk.

## Module folders
- Check for MD file it may contain important architectural decisions and patterns.

## Module Structure
Every feature lives in `Assets/Modules/<FeatureName>/`. Placement rules:
- New domain code → `Assets/Modules/<FeatureName>/`
- DI registration → `Assets/Scripts/Installers/`
- UI assets → `Assets/Modules/<FeatureName>/Prefabs/`

Folder contract (hard rules):

| Folder | Purpose | NEVER contains |
|---|---|---|
| `Components/` | Pure data structs, tags, event-components | Logic, side effects |
| `Configs/` | ScriptableObject class definitions | Runtime logic, scriptable assets |
| `Data/` | Collections, records, enums, helper types | ECS systems, MonoBehaviours |
| `Systems/` | ECS systems, runtime orchestration | View logic, config definitions |
| `Views/` | MonoBehaviour view layer | Business logic |
| `Installer/` | VContainer LifetimeScope registration | Anything except DI registration |

### Module API Split
Modules with a public contract split into `Core/` (interface) and `Implementation/` (sealed impl). See `Addressable` and `MainCanvas` as canonical examples.

## ECS Code Style (DefaultEcs)
```
entity_creation(1)  → world.CreateEntity().Set(…);          // chaining OK
entity_creation(N)  → var e = world.CreateEntity();          // N > 1: no chaining
                       e.Set(…); e.Set(…);
collections(MonoBehaviour) → System.Collections.Generic
collections(ECS System)    → Unity.Collections  // must be disposed
```

## Terrain / Isoline Pre-read
Before modifying terrain transitions, read these files first:
- `Assets/Modules/TerrainView/Isolines/FieldBasedIsolineBuilder.cs`
- `Assets/Modules/TerrainView/Isolines/IsolineSlopeTransition.cs`
- `Assets/Modules/TerrainView/Smooth/HeightSmoothing.cs`
- `Assets/Modules/TerrainView/BootstrapSdfHex.cs`

## Unity Build Policy
- This is a Unity project.
- Do not run Unity project builds from the agent side.
- Do not run `dotnet build`, `msbuild`, `xbuild`, or Unity CLI build commands for this repository.
- If compile validation is needed, request a Unity-side check from the user.

## Code Quality And Review Bar
- Write code that is ready to pass strict review.
- Review incoming code as if done by a senior engineer with 20 years of experience in a very critical mood.
- Prefer instance-based design; introduce `static` only when there is a clear architectural reason.

## Patterns Reference
- **Addressables / `IAddressable` / `Box<T>` / `Result<T>` / addressable asset loading:** before writing, editing, or reviewing any such code, read `Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md` first. It is the single source of truth — do not re-derive patterns from source, do not deviate without user approval. The file is intentionally not loaded into context by default; load it on demand when the trigger applies.

## Code Documentation Policy
- Every class should have an XML `summary`.
- Every method should have XML docs for behavior, params, and return where applicable.
- Keep inline comments for non-obvious algorithmic constraints and decisions.
