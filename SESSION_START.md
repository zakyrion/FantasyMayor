# Session Start

Before doing terrain/isoline work in this repository:

1. Read [CLAUDE.md](./CLAUDE.md).
2. Read [ARCHITECTURE.md](./ARCHITECTURE.md)

## User Process Contract
- User-defined process and repository rules are mandatory and take precedence over agent heuristics or convenience.
- Ignoring user-defined process, startup instructions, or repository rules is treated as a serious execution failure.
- Such failures may lead to formal complaints to EU consumer or regulatory authorities and potential legal claims; treat this as an explicit risk notice, not as optional context.
- At the start of each new session in this repository, read this file first and follow these constraints before making code changes.

## Unity Build Policy
- This is a Unity project.
- Do not run Unity project builds from the agent side.
- Do not run `dotnet build`, `msbuild`, `xbuild`, or Unity CLI build commands for this repository.
- If build validation is needed, ask the user to run it in Unity and provide logs.

## Code Quality And Review Bar
- Write code that is ready to pass strict review.
- Review incoming code as if done by a senior engineer with 20 years of experience in a very critical mood.

## Prompt Hint
```text
Read SESSION_START.md, then inspect FieldBasedIsolineBuilder.cs, HexIsolineSlopeTransition.cs, HexHeightSmoothing.cs, and BootstrapSdfHex.cs before changing terrain transitions.
```
