---
category: C
read: trigger
trigger: "before authoring or reviewing any .md for DOC_STANDARD compliance (the docs-curator's charter; the main agent reads it only when it authors a doc itself)"
tags: [docs, conventions]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[INDEX](INDEX.md)"
---

# DOC_STANDARD.md

Single source of truth for how to write Markdown docs in this project.
Read this before creating or editing any `.md` file.

---

## Rule 0 — Documentation is written for the AI agent, not for a human

Every `.md` file in this project is written for an AI agent that will act on it. Write for the
**least-capable agent likely to read it**: explicit, literal, no implied reasoning chains. A doc
succeeds when an agent can act on it correctly **without opening the source**.

---

## Division of Labor — what belongs in a doc

The tools recover everything structural from code. A Markdown file must NOT repeat what a tool
answers — duplicated structure goes stale, and stale docs are worse than no docs.

| Structural fact | Owner (never the doc) |
|---|---|
| types, signatures, references, call/type hierarchy, outline | `roslyn-mcp` (`mcp__roslyn__*`) |
| component writers/readers, reactive consumers, archetypes, Table-Rule PK/FK, priorities | `ecs-graph` |
| registered-as + Lifetime + installer, injectors, collection resolution, GameMode | `di-graph` |

A doc holds ONLY what no tool can extract:

- **Intent** — why this module/type exists
- **Non-obvious invariants** — constraints that look different than they are
- **Design decisions** — why it is built this way
- **How to use it correctly** — usage rules for a non-trivial public API
- **Behavioral contract** — the semantics behind a signature: side-effects, ownership/disposal,
  aliasing (live vs copy), required call order, idempotency, re-entrancy
- **Current state** — implemented vs scaffold
- **Entity archetypes** — one-line naming only; the registry is the ecs-graph (`/ecs-graph`)

Rule of thumb: if a tool can answer it, delete it from the MD.
Corollary (do not over-strip): tools give the **skeleton** (names, signatures, edges), never the
**semantics**. A method's signature is forbidden here; that method's *contract* is required here.
When in doubt: keep the semantics, drop the shape.

---

## Document Categories

Every `.md` file falls into exactly one category.

| Category | What it is | Files | Rule |
|---|---|---|---|
| **A — Navigation** | Per-module reference | module MDs in `Assets/Modules/**`, `Assets/Domains/**`, `Assets/Presentation/**` | Follow the structure below. Strip anything the tools cover. |
| **B — Template / Reference** | How to build new code, or how to use a tricky API | `Patterns/PATTERN_*.md`, `ADDRESSABLE_PATTERNS.md` | Do **not** strip. Keep accurate, keep complete. |
| **C — Policy** | Project-wide rules | `CLAUDE.md`, `ARCHITECTURE.md`, `ECS_CONVENTIONS.md`, this file | Rules and orientation. Keep current. |

Unsure? One module → A. "How to write code that follows a convention" → B.

---

## Frontmatter (YAML properties)

Every `.md` starts with YAML frontmatter. It exists for **navigation** (Obsidian properties +
stable `grep` for the agent): doc-meta and doc↔doc relations only — never code structure.

```yaml
---
category: A                              # A | B | C
read: reference                          # always | trigger | reference
trigger: "before editing an ECS system"  # iff read: trigger
tags: [terrain, ecs]
related:
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
status: implemented                      # Category A only
code_refs:                               # Category A only — drift anchors
  systems:    [TerrainViewSystem]
  components: [TerrainViewComponent]
---
```

| Field | Required | Rule |
|---|---|---|
| `category` | all files | `A` \| `B` \| `C` per the table above |
| `read` | all files | `always` — session-start orientation docs only, keep this set tiny · `trigger` — read only when the named condition holds · `reference` — on demand; the default for Category A |
| `trigger` | iff `read: trigger`, forbidden otherwise | one imperative line naming the condition (e.g. `"before creating a config loader"`). `INDEX.md` lists the doc under "read on demand" with this text — it is what makes a reference doc discoverable |
| `tags` | encouraged | lowercase domain labels, no `#`. Describe the **domain**, never the code shape — no type/assembly names |
| `related` | omit if none | doc↔doc **markdown links with relative paths** ONLY. Never source files, never a code edge a tool already gives. This is the doc graph — no tool builds it |
| `status` | Category A only | `implemented` \| `partial` \| `scaffold` \| `stub`. The one-word machine index of `## Current State` (lets the agent grep "every scaffold module" without reading bodies). Keep the two in sync |
| `code_refs` | Category A only | drift anchors — rules below |

**`code_refs`** — a machine-readable index of the code symbols the doc is *about*. Its one purpose:
**drift detection** — every name must resolve in `ecs-graph` / `roslyn-mcp`; a name that stops
resolving is the signal the doc drifted (rename/removal → revisit the doc).
- **Bare symbol names only.** Never signatures, fields, priorities, or counts — *which* symbols, not
  *what they contain*.
- **Nested by kind.** Canonical keys: `systems`, `components`, `tags`, `events`, `world_components`,
  `interfaces`, `types`, `enums`, `configs`, `views`. Open set — add a key when a genuinely new kind appears.
- **Anchors, not a roster.** List only the symbols the prose reasons about. The bar: "if this is
  renamed or removed, the doc must be revisited" — not an exhaustive mirror of the assembly.
- **Omit entirely** when the doc has no anchorable symbols (pure design/setup notes). An empty index is noise.

Frontmatter must NOT contain anything the tools already give: assembly/package names, field counts,
priorities, signatures, folder trees. The two deliberate exceptions are `status` and `code_refs`
(names, never shape) — each buys a machine-checkable fact no tool query produces.

---

## Category A — Navigation MD Structure

Use these sections, in this order. **Omit any section with nothing real to say.** Do not pad —
a 12-line file that says only what matters is correct.

```markdown
# ModuleName

One sentence: what this module does.

## Purpose
Why it exists and its role in the game. Skip if the one-liner covers it.

## Trigger
REQUIRED if an ECS system in this module runs reactively. Name the event and the
system (the tools cannot see this — see "Triggers" below). Skip otherwise.

## How To Use Correctly
Only for a non-trivial public API that is easy to misuse — see the criteria below.
Style: Invariants → Patterns → Anti-patterns (model: ADDRESSABLE_PATTERNS.md).

## Public Contract & Gotchas
The behavioral contract of key public/cross-module types — semantics the tools cannot
see. One bullet per fact: side-effect, ownership, aliasing (live vs copy), call order,
idempotency, "safe to re-call". This is the section whose absence sends a reader into
the source. May merge with "Non-Obvious Invariants" when small.

## Non-Obvious Invariants
Constraints a reader cannot guess from the code shape. One bullet each.

## Design Decisions
Why it is built this way — only decisions that are non-obvious or were debated.

## Current State
What is implemented, what is scaffold, what is stubbed. Be blunt.
```

### Forbidden in Category A (the tools already have it)

- Type / component / field tables
- Method signatures
- Dependency lists (assemblies, packages)
- Folder / file trees
- Inheritance chains
- System priority numbers
- Code blocks restating a `struct` / `enum` definition

**One exception:** a code block whose content *is* the instruction (a correct-usage snippet in
"How To Use Correctly"). Reference: `ADDRESSABLE_PATTERNS.md`.

**Signature vs contract.** Forbidden = the *shape* (`Set(VertexCoord, HexVertex)`). Required = the
*contract* (`Set` mutates the owner cache as a side-effect; snapshot before iterating). Naming a
method to attach its contract is fine; restating its parameter list is not.

### When "How To Use Correctly" is warranted

Add it only if BOTH hold: (1) other modules call this public API; (2) it is easy to use wrong
(ownership, lifecycle, async, disposal, ordering, hidden preconditions). A pure-math utility or a
single-obvious-method module does not need it. Structure it as:
- **Invariants** — numbered hard rules ("Success → exactly one Dispose")
- **Patterns** — minimal correct-usage snippets
- **Anti-patterns** — a `Wrong | Why | Right` table

---

## AI-First Writing Rules

- **Explicit over implicit.** State the rule; do not make the reader infer it.
- **Short declarative sentences.** One claim per sentence.
- **Concrete over abstract.** The specific case, not a general description.
- **Mark incomplete work loudly.** "SCAFFOLD — `Update()` is empty", not silence.
- **Name the gotcha.** Write "Common mistake:" and name it.
- **No narrative.** No "first we… then we… finally". List facts.
- **Clarity is not verbosity.** "Lean" = no duplicated code structure — NOT dropping the
  explanation a junior model needs.

---

## Triggers (reactive ECS)

A reactive event set is a call site, not a definition/reference edge — `roslyn-mcp` cannot show
which event drives a system. So every event-driven module MUST carry a `## Trigger` section naming
the event + the system, and pointing to the ecs-graph (`/ecs-graph`) for the full flow:

```markdown
## Trigger
`FooSystem` runs on `WhenAdded<FooEventComponent>`.
Not visible to `roslyn-mcp` — full event flow: the ecs-graph (`/ecs-graph`).
```

## Entity Archetypes (DoD)

Runtime entities are component compositions created by scattered `world.CreateEntity().Set(...)`
calls — no class exists for `roslyn-mcp` to see. The registry is the ecs-graph (`/ecs-graph`).
- Do **not** duplicate archetype definitions in module MDs; naming key archetypes in one line + a
  pointer is fine.
- When an archetype changes in code, refresh the ecs-graph.

---

## Worked Example

Bad (duplicates the tools — type table, deps, signatures):

```markdown
## ECS Components
| Component | Fields |
| HexResourcesComponent | ResourceType Type |
## Dependencies
Core, VContainer, Hexes.Core, AxialSystem ...
## HexResourcesSystem
public HexResourcesSystem(World world, IReadOnlyList<...> subsystems)
Priority: 200
```

Good (only what the tools cannot give):

```markdown
# HexResources

Generates logical resource data for Forest, Clay, Fish. Renders nothing.

## Non-Obvious Invariants
- Resources live on a DEDICATED entity (HexIdComponent + HexResourcesComponent),
  never as a tag on the hex entity itself.
- HexResourcesConfig requires unique ResourceType values — duplicates fail validation.

## Current State
All three generation subsystems are fully implemented. Visuals live in HexResourcesView.
```

Behavioral-contract example (the tools list `Set`/`GetOwnedVertexCoords` but not their semantics):

```markdown
## Public Contract & Gotchas
- `VertexGrid.GetOwnedVertexCoords(hex)` returns the **live** owner-cache set, not a copy.
- `VertexGrid.Set(coord, vertex)` mutates that owner cache as a side-effect — iterating
  `GetOwnedVertexCoords` while calling `Set` throws "Collection was modified". Snapshot first.
- `TerrainView.ApplyHeightsFromVertexGrid(grid)` is safe to call again after the initial bake;
  it re-reads the whole grid, so post-bake grid edits are picked up.
```

---

## Checklist Before Saving Any MD

- [ ] Frontmatter present: `category` + `read`; `trigger` iff `read: trigger`; `status` for
      Category A; `related` = relative markdown links to docs only; no tool-derivable data.
- [ ] If a doc was added/removed/renamed or its `read`/`trigger`/`status` changed:
      `python3 Tools/gen_index.py` was re-run. (The git pre-commit hook — `Tools/githooks/pre-commit`,
      enabled via `core.hooksPath` — runs it on every commit and blocks on lint issues.)
- [ ] (Category A) `code_refs` lists the symbols the prose reasons about, nested by kind, bare
      names only — and every name resolves (a non-resolving name is drift to fix, not to ship).
- [ ] Every line answers something the tools cannot.
- [ ] No type tables, signatures, dep lists, folder trees, priorities, inheritance.
- [ ] Key public/cross-module types carry their **behavioral contract**.
- [ ] Scaffold / incomplete work is marked explicitly.
- [ ] Event-driven system → a `## Trigger` section names the event + points to the ecs-graph.
- [ ] A junior model could act on this without reading the source.
- [ ] Archetype changed → the ecs-graph (`/ecs-graph`) was refreshed too.
