---
category: C
read: reference
tags: [process, tooling, migration, lsp, graph, plan]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[CLAUDE](CLAUDE.md)"
  - "[DOC_STANDARD](DOC_STANDARD.md)"
status: partial
---

# AI Context Migration — Plan (FantasyMayor)

Repo-adapted execution plan for the AI-context migration spec (the verbatim source spec has been
removed once it had served its purpose; this doc is the surviving record): a gap analysis of the
spec's four epics against the infrastructure this repo **already has**, plus an adapted rollout.
Stance (ratified): **adapt to existing — do NOT rebuild greenfield.** The spec is generic;
much of Epics C and D already exist here in another form. We extend what's here and add only
the genuinely missing layer (LSP/Roslyn).

> **State (2026-06-30):** Epic A first step DONE (roslyn-mcp installed + wired). **graphify RETIRED**
> from the project — `roslyn-mcp` is now the code-structure/refs/symbols layer; `ecs-graph` + `di-graph`
> stay. This doc is itself the §C3 "research → plan artifact on disk" convention in action.

## Existing infrastructure (the baseline the spec lands on)

These already implement large parts of the spec's target architecture:

- **Code-structure tool:** `roslyn-mcp` (`mcp__roslyn__*`) — compiler-accurate LSP: definition,
  references, symbols, outline, call/type hierarchy. **Replaced `graphify`** (general code graph),
  which is RETIRED from the project (2026-06-30).
- **Graph tools (2, CLI):** `ecs-graph` (DefaultECS component↔system graph — `ecsg` navigate/search/
  bfs/update/rebuild; reads/writes, reactive consumers, Table-Rule PK/FK, system roles), `di-graph`
  (VContainer wiring — `dig` explain/resolve/consumers/installer/state/bfs/unresolved).
  Artifacts in `.ecs-graph/`, `.di-graph/`. **`ecs-graph` IS the spec's "custom in-house ECS graph tool".**
- **Worker subagents (= spec's workers):** read-only Haiku scouts in `.claude/agents/`
  (`discovery-scout` [formerly graphify-scout], `arch-scout`, `asset-scout`). They burn context and
  return distilled findings; the main agent keeps reasoning + edits.
- **Dispatcher enforcement (= spec's C1):** `.claude/hooks/search-gate.py` — a PreToolUse hook
  that **denies** main-session source discovery (`grep`/`glob`/`rg`/`find` over `Assets/**/*.cs`)
  and direct graph CLIs, routes them to `roslyn-mcp` / the scout, and **budgets** main-agent `.cs`
  reads (8/session). Subagents are exempt. Law: `.claude/SEARCH_POLICY.md`.
- **Doc standard (= spec's §2 + Epic D principle):** `DOC_STANDARD.md` already mandates
  "delete what the tools (`roslyn-mcp`/`ecs-graph`/`di-graph`) can recover; keep only intent /
  invariants / why / behavioral contract." `INDEX.md` is the generated doc map.
- **~~Missing~~ → now present:** the **LSP / Roslyn** layer (`roslyn-mcp`) — installed + wired
  (2026-06-30); supplies `definition` / `references` / `symbols` / `outline` / `callHierarchy`.

## Gap analysis (spec epic → existing → gap → adapted action)

| Epic | Spec wants | Already in repo | Gap | Adapted action | Status |
|---|---|---|---|---|---|
| **A — LSP/Roslyn** | symbol-precise nav (defs/refs/outline/callHierarchy), no full-file reads | **`roslyn-mcp`** (installed + wired 2026-06-30) | — (graphify retired; roslyn supersedes it) | §10 proof passed on `HexIdComponent`; available to main + scout | ✅ DONE |
| **B — graph→typed MCP** | typed, size-bounded JSON tools (`component_consumers`, `system_contract`, `execution_order`, `impact_of_change`); symbol-precise anchors; capped output | 2 graph **CLIs** (`ecs-graph`/`di-graph`; graphify retired) with rich semantics, parsed as **text** by scouts | output is unbounded text, fragile to parse; not typed/capped JSON; no `definition`-jump anchors | **wrap `ecs-graph`** as a thin FastMCP facade; **+AsSet extractor extension**; anchors join to Epic-A LSP jumps; cap+paginate; keep CLI as build/fallback | 🟢 done 2026-06-30 (ecs-graph + di-graph facades live, sets=65; needs CC reload) |
| **C — dispatcher** | main session never reads/greps; workers return ≤2–3k distilled; research→plan→execute split | hook denies main-session Assets/`.cs` discovery + budgets reads; scouts = workers; CLAUDE.md "Discovery Scouts = search front door" (done this session); session-start skill exists | **C1 done.** C2: no explicit ≤2–3k cap codified in scout charter. C3: research/plan/execute session split not formalized. C4: session-start still has main agent reading docs broadly. Open: main-session `.md` reads are still allowed (hook gates only `Assets/.cs`) | codify C2 cap in scout `.md` + SEARCH_POLICY; formalize C3 (this doc seeds the convention); slim session-start to "map + dispatch"; **decide** whether "no direct reads" extends to `.md` docs | 🟢 majority done |
| **D — docs why-only** | strip recoverable, keep why; machine-parsable frontmatter (`code_refs`, `components`); CLAUDE.md as map | DOC_STANDARD enforces why-only; CLAUDE.md + ARCHITECTURE trimmed this session; module MDs already contract-only | D1 vault audit not systematic; D4 frontmatter lacks `code_refs`/`components` anchors; D2/D3 strip+rewrite ongoing ad hoc | run D1 audit via scout; extend frontmatter schema with `code_refs`/`components`; finish strip/rewrite per DOC_STANDARD — **only after A/B** make recoverable docs provably redundant | 🟡 in progress |

## Adapted rollout order

Spec order is A→C→B→D. Adapted, because C is mostly already in place:

1. **Epic A (LSP)** — net-new, biggest single code-read win, low coupling. *Blocked on user
   actions (Unity project files + MCP install).*
2. **Epic C remainder** — cheap, mostly charter/doc edits, builds directly on this session's
   work (C2 cap, C3 session-split convention, C4 slim session-start, doc-read decision).
3. **Epic B (graph MCP)** — wrap `ecs-graph` first; B2 anchors become useful once Epic-A LSP
   exists to jump to them.
4. **Epic D (vault audit + frontmatter + strip)** — last; A/B must exist first so deletions are
   provably safe (only delete content the graph/LSP can reproduce). Run the audit via the scout.

## Epic A first step (spec §10) — ✅ DONE (2026-06-29)

Chosen server: **RoslynMcpServer (JoshuaRamirez)** — `dotnet tool install -g RoslynMcp.Server`
(v0.4.0; command `roslyn-mcp` in `~/.dotnet/tools`, 41 tools). It targets the **.NET 9 runtime**,
which is NOT installed here (only 8 + 10) → run it with **`DOTNET_ROLL_FORWARD=Major`** to use
.NET 10. No .NET 9 install needed.

Proof (all via the tool over stdio JSON-RPC, **zero full-file reads**, against `Domains.Map.csproj`):
- `search_symbols "HexIdComponent"` → `Assets/Domains/Map/Hex/Components/HexIdComponent.cs:6` (~0.5s).
- `find_references HexIdComponent` → **34 sites** across Mountain/Sea/Lake/River generation +
  Forest/Clay/Fish resource subsystems + `MapGenerationSystem` + `HexPathfindingUtility` — the
  component's real dependency graph, incl. generic `With/AsMap/Set<HexIdComponent>` sites
  (confirms Roslyn type-argument tracking = the spec's A acceptance criterion).
- `get_document_outline` → full struct outline (Equals/GetHashCode/Coords).

Caveat: references span only `Domains.Map` because `solutionPath` was a single `.csproj` and **no
`.sln` exists at root**. Cross-project references need a generated `.sln`.

## Remaining for Epic A

- **`.sln` for cross-project navigation:** only per-project `.csproj` exist at root, no `.sln`.
  A single-`.csproj` `solutionPath` limits `find_references` to that project. Generate a Unity
  `.sln` (or pass a `.sln` path) for whole-solution references.
- **MCP wiring → DONE:** registered in committed `.mcp.json` (server `roslyn`, bare `roslyn-mcp` +
  `DOTNET_ROLL_FORWARD=Major`); `~/.dotnet/tools` added to `~/.zprofile` PATH; Claude Code reloaded and
  the `mcp__roslyn__*` tools are live. Available to both the main session and the scout.
- **Resolved:** Unity `.csproj` present; runtime via `DOTNET_ROLL_FORWARD=Major`; server chosen
  (RoslynMcpServer) + installed (v0.4.0); .NET-9 install avoided; Serena `#1014` blocker moot.

## Open decisions (status as of 2026-06-29)

- **`DefaultEcs.Analyzer` in use? → DEFERRED to Epic B.** Revisit when working on the ecs
  graph CLI (look at output/search format, possibly extend it). A4 (generated partial-class
  sites in `find-references`) is cheaply confirmable later from `Packages/manifest.json` / a
  `.csproj`. Note: `DefaultEcs.Analyzer` (a Roslyn source generator) is distinct from the
  in-house `ecs-graph` CLI.
- **Extend "no direct reads" to `.md` docs? → UNDECIDED.** Keep current scope (hook gates only
  `Assets/**/*.cs`; the main agent may still read docs) until decided.
- **Worker ≤2–3k output cap as a hard rule? → NOT NOW.** Observe how the scouts behave first;
  do not codify the cap in the charter yet.

## Epic B — Plan (ratified 2026-06-30)

> **STATUS: ✅ IMPLEMENTED 2026-06-30.** All steps below done: `build_graph.py` extended (schema 2,
> `AsSet` + `[With]/[When*]` attrs), `ecs_mcp.py` FastMCP facade (10 typed tools) written + stdio-proven,
> graph re-built and `sets[]`=65 merged into the curated `graph.json`, registered in `.mcp.json` + scout
> charter + SEARCH_POLICY §1a + SKILL.md schema. **Goes live after a Claude Code reload + approving the
> `ecs-graph` MCP server.** The **di-graph facade** (`dig_mcp.py`, 11 typed tools) was added the same day
> by the same pattern — no extractor change needed (the `unresolved` gap-sniff was clean: only `World`-style
> benign cases). Known minor data nuance: 3 GameMode state nodes are mis-kinded `service` (the class
> `_declare` overrides Boot's `state` kind by file order) — cosmetic, all `runs_in` facts + queries work.

Scope this round: **wrap `ecs-graph` only** (di-graph reuses the pattern in a later round). The
deliverable of the *planning* step = this section (plan-only; implementation is a separate,
explicitly-confirmed step).

### Architecture
- New `~/.claude/skills/ecs-graph/scripts/ecs_mcp.py` — `from ecsg import Graph` (the existing loader
  already computes reverse edges + BFS + `resolve()`; `main()` is `__name__`-guarded, so the class
  imports clean). Add typed-return query functions on top of `Graph` — the CLI's `cmd_*` only `print()`;
  the facade **returns dicts**.
- Transport: **FastMCP** (Python MCP SDK) over stdio. Dependency: `pip install mcp` (confirm/install).
- `ecsg.py` / `build_graph.py` stay as the build + CLI fallback — the CLI is NOT removed.
- Register as a SECOND MCP server `ecs-graph` in the repo `.mcp.json`
  (`command: python3`, `args: [<abs>/ecs_mcp.py]`).

### Extractor extension (`build_graph.py`) — IN SCOPE
The fact store today records AsMap/AsMultiMap (→ `tables`), events (`emits`/`reacts_to`) and tags
(`kind:tag` + `reads`), but **NOT `AsSet`** (only the constituent fluent `With<T>` reads; `[With]`/
`[Without]` attributes on `AEntitySetSystem` are missed entirely). To deliver "which entities bind as
AsSet/AsMap/AsMultiMap per system":
- `_handle_invocation`: add `AsSet` → emit a **set binding** grouping the preceding `With/Without`
  chain → new top-level collection `sets: [{owner, components[], source_location}]` (schema 2,
  analogous to `tables`).
- Also parse `[With(typeof(X))]` / `[Without(...)]` **attributes** on `AEntitySetSystem` subclasses
  (attribute nodes, not invocations) so attribute-defined sets are captured.
- Bump `meta.schema` 1→2; needs `--force` re-build + a STEP-2 re-curation pass.

### Tool surface (extended — ratified)
| Tool | In | Out |
|---|---|---|
| `component_consumers` | component/tag | `{writers[], readers[], reactive[]}` |
| `system_contract` | system | `{role, priority, reads, writes, emits, reacts_to, bindings, tables, source_location}` |
| `system_bindings` | system | `{sets[], maps[], multimaps[]}` |
| `component_bindings` | component | systems that bind it + how (Set/Map/MultiMap) |
| `event_flow` | event | producer→consumer (`emits`→`reacts_to`) |
| `execution_order` | role?/phase? | systems by `priority` |
| `impact_of_change` | node, direction, depth | BFS dependents, paginated |
| `find_node` / `explain_node` | keyword/node | generic fallback (CLI parity) |

### Cross-cutting
- **Envelope:** every list tool → `{meta:{curated, generated, stale?}, total, returned, offset, truncated, items[]}`; `limit` default 50 / max 200; hard response-size cap.
- **Anchors (B2):** each item carries `source_location` (file:line) straight from the store — the direct
  input to roslyn `go_to_definition`/`find_references`. Document the graph→roslyn jump; do NOT call
  roslyn at build time.
- **Honesty:** `raw.json`-only → `meta.curated:false` on every response (mirrors the CLI).
- **Staleness:** the facade reads a static artifact (unlike always-fresh roslyn). Surface
  `meta.generated`; optionally compare manifest mtimes → `stale:true`. **B does NOT fix staleness** —
  that's the CLI's `--update`/`--force` job.

### Access policy (ratified) — codify in `SEARCH_POLICY.md` during implementation
Default: discovery is the **scout's** job, not the main agent's. The main agent may call the ecs-graph
MCP tools **only if (a)** it is already editing code and needs a quick lookup, **or (b)** information is
critically insufficient (fallback). Intent: kill (1) constant mindless grep/reads and (2) cyclic search
+ context bloat. Enforcement is **behavioral/documented** — the hook cannot detect "am I editing" / "is
info critically low"; the existing `.cs`-read budget stays. Optional later: a soft deny-by-default with
an explicit in-call override.

### Out of scope (this round)
~~di-graph facade~~ (DONE 2026-06-30, same pattern) · fixing staleness · changing the STEP-2 curation flow.

### Implementation steps (when approved — separate; plan-only now)
1. Confirm/install `mcp` (FastMCP).
2. Extend `build_graph.py`: `AsSet` + `[With]/[Without]` attrs → `sets[]`, schema 1→2; `--force`
   re-build + STEP-2 re-curation.
3. `ecs_mcp.py`: `from ecsg import Graph` + typed query functions (return dicts).
4. FastMCP wrapper: the 8 tools + envelope/pagination.
5. Smoke-test over stdio (as done for roslyn).
6. Register in `.mcp.json`; update `discovery-scout` charter + `SEARCH_POLICY.md` (scout uses typed MCP;
   main-agent two-condition rule).
7. Flip this doc's gap-table Epic B row → 🟢.

## Current State

Epic A first step DONE (roslyn-mcp installed + wired, §10 proof passed). **graphify RETIRED** from the
project (docs/hooks/scout-charter; scout renamed `graphify-scout`→`discovery-scout`). Out of scope for
the retirement (still mechanically present, follow-up needed): the graphify global skill, the
`post-commit`/`post-checkout` git hooks, `graphify-out/` artifacts, and 6 module-MD mentions.
**Epic B IMPLEMENTED 2026-06-30** (see §Epic B — Plan): FastMCP facade `ecs_mcp.py` over `ecs-graph`
(10 typed tools) + `AsSet`/`[With]` extractor extension (schema 2); graph re-built, `sets[]`=65 merged
into the curated `graph.json`; wired in `.mcp.json` + scout charter + SEARCH_POLICY §1a. The **di-graph
facade** (`dig_mcp.py`, 11 tools) landed the same day by the same pattern (gap-sniff clean; no extractor
change). Goes live after a Claude Code reload + approving the `ecs-graph` **and `di-graph`** servers. Epic C
is otherwise the most advanced. Update this doc's gap table + status as each epic lands.
