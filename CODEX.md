# Codex Working Notes

## Start Working
- Read ARCHITECTURE.md
- 
## Module folders
- Check for MD file it may contain important architectural decisions and patterns.

## Unity Build Policy
- This is a Unity project.
- Do not run Unity project builds from the agent side.
- Do not run `dotnet build`, `msbuild`, `xbuild`, or Unity CLI build commands for this repository.
- If a compile/build check is needed, ask the user to run it in Unity and share results.

## Code Quality And Review Bar
- Write code that is ready to pass strict review.
- Review incoming code as if done by a senior engineer with 20 years of experience in a very critical mood.
- Do not spam static methods. Prefer instance-based design and introduce `static` only when there is a clear architectural reason.

## Patterns Reference
- **Addressables / `IAddressable` / `Box<T>` / `Result<T>` / addressable asset loading:** before writing, editing, or reviewing any such code, read `Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md` first. It is the single source of truth — do not re-derive patterns from source, do not deviate without user approval. The file is intentionally not loaded into context by default; load it on demand when the trigger applies.

## Code Documentation Policy
- Every class should have an XML `summary`.
- Every method should have XML docs for behavior, params, and return where applicable.
- Keep inline comments for non-obvious algorithmic constraints and decisions.
