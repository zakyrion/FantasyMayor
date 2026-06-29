---
category: C
read: always
tags: [docs, conventions]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[INDEX](INDEX.md)"
---

# DOC_STANDARD.md

Single source of truth for how to write Markdown docs in this project.
Read this before creating or editing any `.md` file in a module or repo root.

---

## Rule 0 — Documentation is written for the AI agent, not for a human

Every `.md` file in this project — module docs, templates, policy files, and **this file itself** — is
written **primarily for an AI agent that will act on it**, not for a human reader. No human is the
primary audience; optimize for the model that executes against the doc.

Concretely: write for the **least-capable agent likely to read it** — explicit, literal, no implied
reasoning chains, nothing left "obvious from context". A doc succeeds when an agent can act on it
correctly **without opening the source**. This target is sharper than a vague "write for AI", and it is
what every rule below serves.

---

## Why This Standard Exists

This project recovers everything structural from the code via tools — read them instead of writing it
down:

- **`roslyn-mcp`** (LSP, `mcp__roslyn__*`) — types, fields, method signatures, references, who
  calls/implements whom, call/type hierarchy, file outline.
- **`ecs-graph`** — DoD/ECS relationships (component writers/readers, reactive consumers, archetypes,
  Table-Rule PK/FK, system roles/priorities).
- **`di-graph`** — VContainer wiring (registered-as + Lifetime + installer, collection resolution, GameMode).

If a tool can already answer it, a Markdown file must **not** repeat it.
Duplicated structure goes stale the moment code changes, and stale docs are worse
than no docs.

So every Markdown file holds **only** what those tools cannot extract from code:

- **Intent** — why this module/type exists, what problem it solves
- **Non-obvious invariants** — constraints that look different than they are
- **Design decisions** — why it is built this way (rationale)
- **How to use it correctly** — usage rules for a non-trivial public API
- **Behavioral contract** — the semantics *behind* a signature, which the tools cannot see:
  side-effects, ownership/disposal, aliasing (e.g. a getter returns a **live** internal
  collection, not a copy), required call order, idempotency, re-entrancy, "safe to call again".
  This is the layer whose absence forces a reader back into the source — capture it.
- **Current state** — what is implemented vs scaffold
- **Entity archetypes** — runtime component compositions (DoD entities are not
  classes, so `roslyn-mcp` sees no class — see the ecs-graph (`/ecs-graph`))

Rule of thumb: **if you could get the answer from `roslyn-mcp` / `ecs-graph` / `di-graph`, delete it from the MD.**

Corollary (do not over-strip): the tools give the **skeleton** — names, signatures, edges. They do
**not** give the **semantics**. A method's *signature* is forbidden here (roslyn has it); that same
method's *contract* (what it mutates, what it returns by reference, when it is legal to call) is
**required** here, because no tool can express it. When in doubt, keep the semantics, drop the shape.

---

## Document Categories

Every Markdown file falls into exactly one category. The rules differ per category.

| Category | What it is | Files | Rule |
|---|---|---|---|
| **A — Navigation** | Per-module reference | `Assets/Modules/*/*.md` | Follow the navigation structure below. Strip anything the tools (`roslyn-mcp`/`ecs-graph`/`di-graph`) cover. |
| **B — Template / Reference** | How to build new code, or how to use a tricky API | `SYSTEMTEMPLATE.md`, `CONFIGTEMPLATE.md`, `ADDRESSABLE_PATTERNS.md` | Do **not** strip. These encode procedure/convention. Keep accurate, keep complete. |
| **C — Policy** | Project-wide rules | `CLAUDE.md`, `ARCHITECTURE.md`, this file | Rules and orientation. Keep current. |

When unsure which category a new file is: if it describes one module, it is A.
If it describes *how to write code that follows a convention*, it is B.

---

## Frontmatter (YAML properties)

Every `.md` file starts with a YAML frontmatter block. It exists for **navigation only** —
Obsidian properties/Dataview/backlinks for the human, and stable `grep` queries for the agent.
It carries **doc-meta and doc↔doc relations only**. It is NOT a place to restate code structure.

```yaml
---
category: A                              # A | B | C — required, all files
read: reference                          # always | trigger | reference — required, all files
trigger: "before editing an ECS system"  # required IFF read: trigger — one line, when to read it
tags: [terrain, ecs]                     # domain tags, lowercase, no '#'; optional but encouraged
related:                                 # doc↔doc links only; markdown links, relative paths
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[TERRAIN_VIEW](../TerrainView/TERRAIN_VIEW.md)"
status: implemented                      # Category A ONLY — see enum below
---
```

Field rules:

- **`category`** — `A`, `B`, or `C` from the table above. Required in every file. The only field that is
  not derivable elsewhere in machine-readable form.
- **`read`** — **required in every file.** The agent's read-priority signal, and the data `INDEX.md` is
  generated from. One of:
  - `always` — read on every session start. Reserved for the few orientation docs (this file,
    `ARCHITECTURE.md`, `CLAUDE.md`, `INDEX.md`). Keep this set tiny.
  - `trigger` — read **only** when a specific condition holds. Requires a `trigger` field naming that
    condition (e.g. templates, `ADDRESSABLE_PATTERNS.md`, `GENERAL_UI_STYLE.md`).
  - `reference` — consult on demand, no fixed trigger. Default for per-module navigation docs (Category A):
    you read a module's doc when you go into that module.
- **`trigger`** — **required iff `read: trigger`, forbidden otherwise.** One line, the condition that should
  send a reader here (imperative, e.g. `"before creating or editing an ECS system"`). This is what makes an
  otherwise-orphan reference doc discoverable: `INDEX.md` lists it under "read on demand" with this text.
- **`tags`** — domain labels for filtering (e.g. `ecs`, `terrain`, `ui`, `hex`, `config`, `boot`,
  `pathfinding`, `input`, `camera`). Lowercase, no `#` prefix (Obsidian adds it in the properties UI).
  Describe the **domain**, never the code shape. Do not encode type/assembly names — that is the tools' job.
- **`related`** — cross-**document** links only, as **markdown** links with **relative paths**
  (consistent with `useMarkdownLinks: true` in `.obsidian/app.json`; renders in IDE and GitHub). Never
  link to source files here and never duplicate a code edge the tools already give — this is the doc
  graph, which no code tool builds. Omit the field if there are no real doc relations.
- **`status`** — **Category A only.** One of: `implemented`, `partial`, `scaffold`, `stub`.
  This is the **one** field that overlaps prose (`## Current State`). It is kept on purpose as a
  machine-readable enum so the agent can answer "list every scaffold module" with one `grep` instead of
  reading every body. Rule 0 exception, deliberate and scoped: the prose `## Current State` stays the
  authoritative detail; `status` is its one-word index. Keep the two in sync. Omit `status` in B/C files.

What frontmatter must NOT contain (same spirit as the forbidden list below): module/type/assembly names
as data, field counts, priorities, signatures, or anything the tools already give. If `grep`-ing the
frontmatter and asking `roslyn-mcp`/`ecs-graph` would return the same fact, it does not belong here.

---

## Category A — Navigation MD Structure

Use these sections, in this order. **Omit any section that has nothing real to say.**
Do not pad. A 12-line file that says only what matters is correct.

```markdown
# ModuleName

One sentence: what this module does.

## Purpose
Why it exists and its role in the game. Skip if the one-liner already covers it.

## Trigger
REQUIRED if an ECS system in this module runs reactively (e.g. `WhenAdded<SomeEvent>`).
Name the event and the system. The tools cannot see this — see "Triggers (reactive ECS)" below.
Skip for modules with no reactive/event-driven entry point.

## How To Use Correctly
If the module exposes a non-trivial public API that is easy to misuse.
Use the ADDRESSABLE_PATTERNS.md style: Invariants → Patterns → Anti-patterns.
Skip entirely for modules with no public API or an obvious one.

## Public Contract & Gotchas
The behavioral contract of this module's key public/cross-module types — the semantics the tools
cannot see. One bullet per fact: side-effect, ownership, aliasing (live vs copy), call order,
idempotency, "safe to re-call". Include it whenever correct use depends on something not visible in
the signature — even for module-internal types that *other* systems mutate. This is the section whose
absence sent the last reader into the source. May merge with "Non-Obvious Invariants" when small.

## Non-Obvious Invariants
Constraints a reader cannot guess from the code shape alone.
Things that look one way but are another. One bullet each.

## Design Decisions
Why it is built this way. Trade-offs that were chosen deliberately.
Only decisions that are non-obvious or were debated. Skip the trivial ones.

## Current State
What is implemented, what is scaffold, what is stubbed. Be blunt.
```

---

## Forbidden in Category A (the tools already have it)

Never put these in a navigation MD:

- Type / component / field tables that just list what the struct contains
- Method signatures
- Dependency lists (assemblies, packages)
- Folder / file trees
- Inheritance chains ("X extends Y extends Z")
- System priority numbers
- Code blocks that restate a `struct` or `enum` definition

**One exception:** a code block is allowed when the code block *itself is the
instruction* — e.g. a correct-usage snippet in a "How To Use Correctly" block.
`ADDRESSABLE_PATTERNS.md` is the reference for this exception.

**Signature vs contract — do not confuse them.** Forbidden = the *shape* (`Set(VertexCoord, HexVertex)`).
Required = the *contract* the tools cannot see (`Set` mutates the owner cache as a side-effect;
`GetOwnedVertexCoords` returns the **live** cache set, so snapshot before calling `Set`). Naming a method
to attach its contract is fine; restating its parameter list as a table is not.

---

## When You Need "How To Use Correctly"

Add this block only if **both** are true:

1. The module exposes a public API other modules call.
2. That API is easy to use wrong (ownership rules, lifecycle, async, disposal,
   ordering, hidden preconditions).

Examples that need it: `Addressable` (Box/Result ownership), anything with manual
disposal, anything with a strict call order.

Examples that do not: a pure-math utility, a tag-component-only module, a module
whose only entry point is a single obvious method.

Model the block on `ADDRESSABLE_PATTERNS.md`:
- **Invariants** — numbered, hard rules ("Success → exactly one Dispose")
- **Patterns** — minimal correct-usage snippets
- **Anti-patterns** — a `Wrong | Why | Right` table

---

## AI-First Writing Rules

The reader is an AI agent — assume the least-capable model likely to read it (Rule 0). Optimize for
zero ambiguity, not for brevity.

- **Explicit over implicit.** State the rule directly. Do not make the reader infer it.
- **Short declarative sentences.** One claim per sentence.
- **Concrete over abstract.** Show the specific case, not a general description.
- **Mark incomplete work loudly.** Write "SCAFFOLD — `Update()` is empty", not silence.
- **Name the gotcha.** If something is a common mistake, say "Common mistake:" and name it.
- **No narrative.** Skip "first we... then we... finally we." List facts.
- **Clarity is not verbosity.** "Lean" means no duplicated code structure. It does
  not mean dropping the explanation a junior model needs.

---

## Triggers (reactive ECS)

Systems that react to an event (`world.GetEntities().WhenAdded<SomeEvent>()...`) have a
trigger relationship that `roslyn-mcp` **cannot represent** — a reactive event set is a call site, not a
definition/reference edge, so a `find_references` on the system never reveals which event drives it.

Therefore every event-driven module MUST carry a `## Trigger` section naming the event and the
system, and pointing to the ecs-graph (`/ecs-graph`) for the full producer→consumer flow. The ecs-graph
DOES model it (`reacts_to` edges) — the `## Trigger` section makes it explicit in the doc too.

Example:
```markdown
## Trigger
`FooSystem` runs on `WhenAdded<FooEventComponent>`.
This reactive trigger is not visible to `roslyn-mcp` — full event flow is in the ecs-graph (`/ecs-graph`).
```

## Entity Archetypes (DoD)

Runtime entities are component compositions, not classes. They are created by
scattered `world.CreateEntity().Set(...)` calls, so `roslyn-mcp` cannot reconstruct
them (there is no class). They live in the ecs-graph (`/ecs-graph`), built on demand from the code.

- Do **not** duplicate full archetype definitions in module MDs.
- A module MD may name its key archetypes in one line and point to the ecs-graph (`/ecs-graph`).
- When you add or change an archetype in code, refresh the ecs-graph (`/ecs-graph`).

---

## Worked Example

Bad (duplicates what the tools have — type table, deps, signatures):

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

Behavioral-contract example (the layer that was missing — the tools list `Set`/`GetOwnedVertexCoords`
but not their semantics):

```markdown
## Public Contract & Gotchas
- `VertexGrid.GetOwnedVertexCoords(hex)` returns the **live** owner-cache set, not a copy.
- `VertexGrid.Set(coord, vertex)` mutates that owner cache as a side-effect — so iterating
  `GetOwnedVertexCoords` while calling `Set` throws "Collection was modified". Snapshot first.
- `TerrainView.ApplyHeightsFromVertexGrid(grid)` is safe to call again after the initial bake; it
  re-reads the whole grid, so post-bake grid edits (e.g. clay depressions) are picked up.
```

---

## Checklist Before Saving Any MD

- [ ] A YAML frontmatter block is present: `category` + `read` set; `trigger` set iff `read: trigger`;
      `status` set for Category A; `related` uses relative markdown links to docs only; no tool-derivable
      data in frontmatter. (Then regenerate `INDEX.md` — see below.)
- [ ] If a doc was added/removed/renamed or its `read`/`trigger`/`status` changed, `INDEX.md` was
      regenerated (`python3 Tools/gen_index.py`).
- [ ] Every line answers something the tools (`roslyn-mcp`/`ecs-graph`/`di-graph`) cannot.
- [ ] No type tables, signatures, dep lists, folder trees, priorities, inheritance.
- [ ] Key public/cross-module types carry their **behavioral contract** (side-effects, aliasing,
      ownership, call order) — the semantics behind the signature, not the signature itself.
- [ ] Scaffold / incomplete work is marked explicitly.
- [ ] If a system reacts to an event, a `## Trigger` section names the event + points to the ecs-graph (`/ecs-graph`).
- [ ] A junior model could act on this without reading the source.
- [ ] If it changed an archetype, the ecs-graph (`/ecs-graph`) was refreshed too.
