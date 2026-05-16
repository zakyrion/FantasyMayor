# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# FantasyMayor: Project Context & Architectural Decisions

## Start Working
- Read ARCHITECTURE.md
- `SYSTEMTEMPLATE.md` - general template for systems and subsystems; read it if you will work with systems
- `CONFIGTEMPLATE.md` - general template for configs, config components, and config loader systems; read it if you will work with config flows

## User Process Contract
- User-defined process and repository rules are mandatory and override agent-default workflows.
- Do not substitute personal heuristics, convenience patterns, or alternate execution flow when the user has already defined the process.
- Ignoring these instructions is considered a serious execution failure and must be treated as carrying complaint and legal escalation risk.

## Expected behavior
- You are my assistant. Your task is to implement my intent and propose your own options for solving problems.
- Our work is built on dialogue. I value discussing all important details in advance.
- Start work only when I explicitly instruct you to do so, or when you no longer have unresolved questions.
- Questions have higher priority than solving the task quickly.

## Module folders
- Check for MD file it may contain important architectural decisions and patterns.
- Architecture, stack, module layout, and ECS conventions are described in `ARCHITECTURE.md`.
- Do not duplicate or override architecture rules here. Treat `ARCHITECTURE.md` as the single source of truth for structure.

## Terrain / Isoline Pre-read
Before modifying terrain transitions, read these files first:
- `Assets/Modules/TerrainView/Isolines/FieldBasedIsolineBuilder.cs`
- `Assets/Modules/TerrainView/Isolines/IsolineSlopeTransition.cs`
- `Assets/Modules/TerrainView/Smooth/HeightSmoothing.cs`

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
- Add comments only where the logic stops being simple and unambiguous.
- Prefer short targeted comments for non-obvious algorithmic constraints, decisions, and invariants.
- Do not add boilerplate XML documentation by default.
