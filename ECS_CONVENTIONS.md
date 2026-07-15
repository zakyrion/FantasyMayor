---
category: C
read: trigger
trigger: "before writing or editing any ECS system, component, event, config, or query"
tags: [ecs, conventions, policy]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# FantasyMayor — ECS & Runtime Conventions

The ECS/runtime rulebook: where state lives, how systems are decomposed, and the write / collection /
error conventions every system must follow. Layering, taxonomy, and the recipe index live in
`ARCHITECTURE.md`; the per-block skeletons live in `Patterns/PATTERN_*.md`.

## State Storage Taxonomy

Four storages. Pick by answering: how many instances, and does anything need to FIND it via an
entity query?

**Apply via:** a loaded config is always a world component — the config-loader procedure that does it
is `Patterns/PATTERN_CONFIG_LOADER.md`.

| Storage | Use when | Access | Registry |
|---|---|---|---|
| **Entity table** | N rows of the same shape (hexes, resources, views, icon containers) | query = key + discriminator (Table Rule); `EntitySet` / `EntityMap` / `EntityMultiMap` | the ecs-graph (`/ecs-graph`) |
| **World component** | exactly ONE instance, and NO consumer needs it in an entity query | `world.Set` / `world.Get`, guarded by `world.Has` | the ecs-graph (`/ecs-graph`) |
| **One-frame event entity** | a signal that something changed; consumed by a Reactive System this same frame | marker component + `EventTag`; `EventCleanupSystem` disposes at end of tick | the ecs-graph (`/ecs-graph`) |
| **Singleton entity** | exactly ONE instance, but it MUST appear in entity queries (a per-frame system anchors on it, or reactive filters watch it) | `With<TheComponent>` set with `Count`-guard | the ecs-graph (`/ecs-graph`) |

World component contract:
- A world component is **not an entity**: it never appears in `world.GetEntities()` and cannot be
  matched by `With<T>` / `WhenAdded<T>` / `WhenChanged<T>`. If its change must drive reactive
  consumers, raise an explicit one-frame event entity alongside the `world.Set`.
- All loaded configs are world components (`Patterns/PATTERN_CONFIG.md`). Runtime singletons follow the same
  storage: `CameraComponent`, `VertexGridComponent`, `TerrainTextureComponent`,
  `HexIconsViewComponent`, `HexIconsVisibilityComponent`.
- A world component carrying a **reference type** (`VertexGrid`, `Texture2D`) is set ONCE at
  creation; the payload object is then mutated in place by its writers. `world.Set` is never
  re-called after a mutation — readers always see the live object via `world.Get`.
- Singleton entities are the exception, not the default. Current ones exist because systems anchor
  per-frame ticks on them or query them: `TerrainViewComponent`, `HexSelectedComponent`,
  `WaterViewComponent`, `HexSelectionViewComponent`, `HexInfoPanelViewComponent`,
  `PlayerInputComponent`. When adding new single-instance state, default to a world component;
  create a singleton entity only when an entity-query consumer exists from day one. A singleton
  entity is still an entity — the Tag Law applies (it carries its tag, its queries filter on it).

## Decomposition Rules

The project moved from per-frame "god-systems" to decomposed role-pure systems. Apply these rules
when designing or reviewing any system.

**Apply via:** the reactive-split skeleton and the point-of-use "split when" checklist are in
`Patterns/PATTERN_REACTIVE_SYSTEM.md`.

**God-system smell — split it when a system:**
- both **creates and destroys** the same kind of content;
- **diffs world state every frame** to find out "what changed" (multi-filter scans, sorts,
  per-frame set comparisons);
- holds **more than two unrelated query families** (it serves several masters);
- runs in **multiple game states** for different reasons.

**The split recipe** (worked example: the retired per-frame `ForestViewSyncSystem`, split → 3 systems):
1. The startup bulk becomes a **Pipeline SubSystem** (one-shot, runs once inside `MapCreation`):
   `ForestHexResourceViewSubSystem` plants every forest hex and paints ground once.
2. Each runtime responsibility becomes its own **Reactive System** in `Gameplay`:
   `ForestSpawnSystem` (on `ForestHexAppearedEvent`) and `ForestDespawnSystem`
   (on `ForestHexRemovedEvent`).
3. Shared computation moves to a stateless **helper** (`Helpers/`): `ForestPlanter`.

**Reactive = generic pulse + reconcile:**
- The event is a **payload-less one-frame pulse** ("something changed"), not a per-entity delta.
  No coordinates, no lists in the event.
- On the pulse, the system **reconciles against current world state**: build the current set, diff
  it, act on the difference. Work with state, not with transitivity.
- Reconciliation makes the system **idempotent**: a second pulse in the same frame finds nothing to
  do. Missed or coalesced pulses are harmless — the next pulse repairs everything.
- The consumer is an `UpdatedSystem` whose base set is `With<TheEvent>` → zero cost while no pulse
  exists. The pulse entity itself is ignored inside `Update`.
- Emitters may be deferred: build the reactive consumer as a dormant scaffold first, wire emitters
  when the gameplay mechanic lands.

## Naming & Construction

- **Component naming by role (suffix):**
  - a component **carrying data** → `…Component` (e.g. `HexIdComponent`, `HexIconsVisibilityComponent`)
  - a **tag / marker** component (empty, presence-only) → `…Tag` (e.g. `HexTag`, `EventTag`)
  - a **one-frame event** component → `…Event` (e.g. `ForestHexAppearedEvent`, `SelectedHexChangedEvent`)
  - a **foreign key** component (its ONLY payload is another key space's key value) → `…FKComponent`.
    The name derives mechanically from the owner key: insert `FK` before `Component`
    (the FK of `HexIdComponent` is the hex space's `…IdFKComponent`). Key-role law: Table Rule below.

  Pre-existing `…EventComponent` names (e.g. `TerrainGenerationGenerateEventComponent`) predate this rule;
  they stay until a deliberate rename, but new events use the `…Event` suffix.
- **Self-sufficient names (C# / .NET Framework Design Guidelines).** The simple type name must read clearly
  on its own, **without** leaning on the namespace to disambiguate — this is a C# codebase and follows the
  FDG, **not** Go's "avoid stutter". Two consequences:
  - **Repeat the feature name — required, not merely tolerated.** `DistrictBuildCostConfig`, never a bare
    `CostConfig` / `Config` that would collide with a sibling feature's type. Accept the length (and the
    stutter in the fully-qualified name) — clarity at the use site wins. **Never** push disambiguation onto
    a `using` alias or a namespace qualifier. Repeating the **feature** name is how a feature's types cohere
    (`DistrictOpenConditionConfig`, `DistrictOpenConditionEvaluatorSystem`).
  - **Strip only a pure domain prefix that adds no clarity** — exactly as .NET itself does (`System.IO.File`,
    not `IOFile`): under `Domains.Map.Generation.*` a system is `GenerationSystem`, **not**
    `MapGenerationSystem`; the reference model is `DistrictOpenConditionConfig` (`Domains.Economy.*`, carries
    no `Economy`). If stripping the domain would make the name ambiguous, keep enough to stay self-sufficient.
  **Exceptions** — a name may carry a token that collides with a domain when it is:
  (1) an **FK/PK identity component** or **Table-Rule discriminator** — a stable relational identifier
  referenced across domains (`DistrictTypeComponent`, `HexIdComponent`, `ActorTypeComponent`, and the shared
  action key space `ActionIdComponent`); or (2) a **DI installer** — the uniform `[Domain]Installer` name is
  a deliberate disambiguator across the ~13 sibling installers.
- Single-component entity creation may chain: `world.CreateEntity().Set(...)`
- Once more than one component is assigned, stop chaining and use a local entity variable
- Prefer instance-based design; use `static` only when a type is truly stateless utility
  infrastructure (e.g. `ForestGroundPainter`).

## Statelessness And Collections (the two hard bans)

These two bans are machine-checked by `/arch-check`. They apply to every system.

**Apply via:** the applied per-recipe form is in each `Patterns/PATTERN_*.md` (the recipe's Rules).

**Ban 1 — no stateful systems.** A system holds no mutable per-instance state. Instance fields must
be `readonly` handles: DI dependencies, `World`, query caches. What does NOT count as state:
- query caches (`EntitySet`, `EntityMap<T>`, `EntityMultiMap<T>`) — they are declarative,
  self-maintaining views of world state;
- `const` / `static readonly` configuration values.

What DOES count as state: any reassignable field, and any `readonly` field whose CONTENTS mutate
across frames (collections, arrays, `StringBuilder`, `Native*` buffers held between ticks).

Escape hatches, in order of preference:
1. Move the state where it belongs — onto an entity (a component) or into a world component.
2. `FrameBox<T>` (`Core`) for state valid only within a bounded number of frames (e.g. a per-frame
   cache resolved in `PreUpdate`): it is frame-stamped and fails loud on a stale read.
3. `[StateAllowed("reason")]` (`Core`) on the field — a deliberate, reviewed exception that
   `/arch-check` skips. Always pass the reason.

**The default home for "system state" is a component — reach for 2–3 only after ruling out 1.**
Most things that feel like per-system state are not: a status flag, an in-flight marker, a progress
counter, an "is this running" / "did this complete" bit, a handle to the thing currently being
processed — these are domain state that belongs ON AN ENTITY (a component) or in a WORLD component,
and moving them there breaks nothing. The system then just reads/writes that component and stays
stateless. A component is not limited to "intrinsic data" — a transient, single-instance lifecycle
flag is a perfectly valid world component. Do not assume a value must live in the system just because
only that system touches it today; the moment a value lives on a component, any future system can
observe it without auditing the writer. Worked example: `TurnProcessorComponent` (module `Turn`) is a
world component that holds the in-flight turn's status and doubles as the "turn in progress" signal;
the launching `TurnProcessorSystem` keeps zero mutable fields.

**Ban 2 — no `System.Collections.Generic` in systems.** Use `Unity.Collections`
(`NativeList`, `NativeHashSet`, `NativeParallelHashMap`, …) and dispose explicitly.
- `Allocator.Temp` for within-frame scratch; `Allocator.Persistent` (explicit dispose) when a
  container must outlive an `await` or cross a `RunOnThreadPool` boundary. Why Temp cannot do
  either: Threading And Native Memory below (Law 2).
- **Exception — managed elements:** a collection whose elements are managed types (`GameObject`,
  view references, `Entity`-wrapping records) stays `System.Collections.Generic`, because a
  `NativeContainer` only holds `unmanaged` types. Mark such cases with a short comment.
- Enums can't be `NativeHashSet`/`NativeParallelHashMap` **keys** (no `IEquatable<T>`) — key on the
  underlying `int`. As a **value** an enum is fine (`unmanaged`).
- In `MonoBehaviour` (view) code, `System.Collections.Generic` is fine.

## Component Writes And Reactivity (DefaultEcs)

Project rule: **always write components through `entity.Set<T>(value)`** — the publishing write path.
This is mandatory: any component can gain a reactive consumer without auditing every place that writes it.

- **Do not** mutate through `ref entity.Get<T>()`. In-place mutation does not publish a change, silently
  breaking `WhenChanged<T>` sets and `SubscribeComponentChanged<T>` callbacks. `ref Get` is read-only.
- `ref` + `entity.NotifyChanged<T>()` is an escape hatch, **not** an approved pattern. A forgotten
  `NotifyChanged` is a silent desync; it also cannot deliver the true old value (overwritten in place).
  Use `Set(value)`.
- Reactive consumers: `WhenChanged<T>()` (batched — call `Complete()` each tick) and
  `world.SubscribeComponentChanged<T>(...)` (immediate, gives old + new).
- DefaultEcs tracks the write **call**, not the value — no built-in value-diff. `With<T>` / `Without<T>`
  filters track component presence only.
- **Entity set-membership diffs are a separate case.** Reacting to which *entities* enter or leave a set
  uses `WhenAdded` / `WhenRemoved`, not `WhenChanged`. The project's chosen alternative for view upkeep
  is the pulse + reconcile pattern (Decomposition Rules above) — prefer it over `WhenAdded`/`WhenRemoved`
  buffers for new code.

## Threading And Native Memory (UniTask × DefaultEcs)

The Turn pipeline (`TurnPhaseSubSystem` chain) and any `UniTask` code may run off the main thread
(`RunOnThreadPool`). Two laws govern what such code may touch. Decided 2026-07-10 on the district
build-over-turns mechanic; worked producer example: `BuildDistrictTurnTickSystem`.

**Law 1 — DefaultEcs world access by thread.** The line is STRUCTURAL vs VALUE: anything that
changes which entities match which `EntitySet` (create, first-write of a component type, dispose,
pulse) synchronously rewrites set buffers the main thread iterates every frame — main thread only.
Rewriting the bytes of an already-present component touches no set membership.

```clojure
(def defaultecs-thread-law
  {:off-thread-allowed {:value-read  "entity.Get<T>() / world.Get<T>()"
                        :value-write "entity.Set<T>() of an EXISTING component — presence unchanged, no EntitySet buffer mutation"}
   :value-write-caveat "safe ONLY while the component has no WhenChanged<T> / SubscribeComponentChanged consumer — those buffer every Set call on the consumer's thread"
   :main-thread-only   ["world.CreateEntity()"
                        "entity.Set<T>() that ADDS a component (first write of that type — structural)"
                        "entity.Dispose() / Remove<T>()"
                        "raising event pulses (EventTag entities)"]
   :bridge             "await UniTask.SwitchToMainThread() before structural ops, back via SwitchToThreadPool"})
```

**Law 2 — `Allocator.Temp` is thread-bound.** Temp is a per-thread TLS stack: allocation is a
pointer bump (≈ cost of a local variable), reclamation is a WHOLESALE rewind of the thread's stack.
The rewind is driven by whoever owns the thread's lifecycle — and nobody owns a `ThreadPool` thread's.

```clojure
(def temp-allocator-law
  {:cost      "TLS stack bump; free = frame rewind (main) / job-end rewind (worker) — wholesale, not per-allocation"
   :dispose   "call Dispose() anyway — it clears the safety handle, costs nothing; project convention"
   :where     #{:main-thread :inside-a-job}   ;; only threads whose lifecycle Unity drives
   :never     "ThreadPool / any thread Unity does not manage — its TLS block is NEVER rewound (no frame, no job boundary) → silent leak"
   :lifetime  "does not survive the frame (main) / the job (worker) — never hold across an await"
   :overflow  "block exhausted (4 MB player / 16 MB editor main thread) is NOT a crash: block may grow 2×, then falls back to the slower linear allocator; visible as Overflow counters in the memory report"
   :off-thread-scratch "need a native buffer on ThreadPool? Allocator.Persistent + explicit Dispose, or move the work behind SwitchToMainThread"})
```

## Collector And Output Methods

- A method that produces a value or fills a collection MUST report success/validity through a `bool`
  return and be named `Try…` (`TryGet`, `TryCollect`, `TryPick`). The caller branches on that `bool`.
- The caller MUST NOT infer success by inspecting the output — `== null`, `.Count == 0`, `.Length == 0`
  as a success signal is forbidden. It couples the caller to the method's internals. Branch on the `bool`.
- Make data direction visible at the call site: pass a written output as `ref` (or `out`), a read-only
  input as `in`. Never pass a collection the method **writes** by value — a by-value `NativeHashSet` /
  `NativeList` still aliases the same native memory, so the mutation is invisible at the call site.
- A producer with exactly one output **and** guaranteed success may simply **return** the value. Use the
  `Try…` + `ref`/`out` + `bool` form when success is not guaranteed, or when there are multiple outputs.
- Reference example: `ForestPlanter.TryPickForestEntry` — input as a parameter, result via `out`,
  `bool` return.
- A single-use private helper that reads class fields and just unfolds the caller's linear flow does **not**
  earn extraction — inline it into the caller. Extract only when the method is signature-complete (inputs
  as parameters) and is either reused or a self-contained operation. `Update` (a system's orchestration
  root) may read the system's own fields directly.

## Relational Modeling — Table Rule

An entity "table" is defined by its query, and a query MUST name the table, not just the key.

- **Table = key component + discriminator component.** A bare `With<KeyComponent>` query is
  **forbidden** — it is a UNION of every table sharing that key space, not a table.
- **Tag Law (2026-07-08; strengthened 2026-07-15 FM-11) — universal and machine-checkable:**

  ```clojure
  (def tag-law
    {:entity {:requires "EXACTLY 1 tag — its identity / table discriminator"}  ;; the tag defines the entity's boundary; an entity without a tag does not exist
     :filter {:requires "exactly 1 tag in every With<> chain"}  ;; 2 identity tags describe a row that cannot exist — a DEAD filter
     :category-tag UITag                                        ;; a shared kind-marker satisfies the law (identity then rides on the *ViewComponent)
     :event-filter :exempt                                      ;; a reactive base set filters on the *Event component — the event IS the filter
     :state "…StateComponent wrapping an enum — NEVER a toggled tag"  ;; Set() re-indexes maintained maps, so a state flip moves the row between self-index slices automatically
     :kind  "…KindComponent wrapping an enum — NEVER a second tag"    ;; per-kind access = self-index AsMultiMap lookup by the enum value
     :why "tag = archetype identity → deterministic attribution; kind and state are COLUMNS of the row, not identities"})
  ```

- **Tag classes.** Only ONE class of tag exists — the identity/discriminator (plus the structural
  `EventTag` on pulses and the category `UITag`). What looked like other tag uses is data:
  a **runtime-toggled marker** is a state COLUMN → `…StateComponent { Value = enum }`, flipped via
  `Set()` (change-only writes: compare first, write on difference); a **subtype marker** inside a
  table family is a kind COLUMN → `…KindComponent { Value = enum }`, set once at spawn. Both are
  map keys of legal self-indexes (`AsMultiMap<…>` with the family tag as discriminator) — consumers
  read a slice with `TryGetEntities(value)` instead of scanning or tag-filtering.
- **Key-role law (2026-07-15, FM-11) — a key's role is visible in its TYPE.** The owner table's
  identity and other tables' references to it are DIFFERENT component types:

  ```clojure
  (def key-role-law
    {:pk   {:suffix "…IdComponent" :home "exactly ONE owner table, paired with its tag" :index "AsMap<PK>"}       ;; the row's own identity
     :fk   {:suffix "…FKComponent" :home "rows of OTHER tables pointing at the owner"   :index "AsMultiMap<FK>"}  ;; wraps the owner space's key VALUE; IEquatable required
     :data {:suffix "…Component"   :never "keying maps of two DIFFERENT tables"}                                  ;; attribute value; self-index allowed (exception below)
     :kind {:owner-side :data      :referencing-side :fk}   ;; enum key space with NO PK table (district types): the carrier's own value = Data, a catalogue/verb pointer = FK
     :why "a shared key TYPE re-creates the bare-key UNION at the type level; separate types make the compiler enforce the Table Rule"})
  ```

- **Secondary-index exception (self-index).** A Data component MAY key a map when the map's
  discriminator is a tag of the SAME table — a join/select over the table's own attribute
  (hexes grouped by `HexTypeComponent`; resource rows by `HexResourceComponent`). The lookup
  VALUE may arrive from outside (converted from an FK or another table's field) — the boundary
  holds because every indexed row belongs to one table. The same Data type keying maps of TWO
  different tables is the forbidden shared-key conflation — split it into PK + FK.

- The hex key space under the law (FM-11 target shape; FK types are created during the migration):

  <!-- doc-lint: off — FM-11 target-state names, created during the migration -->
  | Table | Discriminator | Key component | Role |
  |---|---|---|---|
  | Hex | `HexTag` | `HexIdComponent` | PK — one entity per coordinate |
  | HexResource | `HexResourceTag` | `HexIdFKComponent` | FK — N per coordinate (one per `ResourceType`) |
  | ForestView | `ForestViewTag` | `HexIdFKComponent` | FK — N per coordinate |
  | HexIconContainer | `HexIconContainerTag` | `HexIdFKComponent` | FK — one per coordinate |
  | DistrictView | `DistrictViewTag` | `HexIdFKComponent` | FK — one per built hex |

- One query definition has three materializations — pick by access pattern:

  ```csharp
  // PK table → unique index. TryGetEntity(key, out Entity). A duplicate PK value THROWS (fail-loud).
  EntityMap<HexIdComponent> hexByCoord =
      world.GetEntities().With<HexTag>().AsMap<HexIdComponent>();

  // FK 1:N table → non-unique index, keyed on the FK TYPE. TryGetEntities(key, out ReadOnlySpan<Entity>).
  EntityMultiMap<HexIdFKComponent> resourcesByCoord =
      world.GetEntities().With<HexResourceTag>().AsMultiMap<HexIdFKComponent>();

  // Join = construct the FK value from the PK value at the point of use.
  if (resourcesByCoord.TryGetEntities(new HexIdFKComponent { Coords = hexId.Coords }, out var rows)) { /* … */ }

  // The same table as a sweep set.
  EntitySet resources =
      world.GetEntities().With<HexIdFKComponent>().With<HexResourceComponent>().With<HexResourceTag>().AsSet();
  ```
  <!-- doc-lint: on -->

- **DefaultEcs mechanics behind the law (verified against 0.17.2 source):**
  - `AsMap<TKey>` / `AsMultiMap<TKey>` implicitly add `With<TKey>()` to the rule — the map alone is
    still a bare-key UNION of every table carrying `TKey`; the discriminator tag in the chain is what
    names the table (Tag Law). The key type carries the ROLE; the tag carries the TABLE.
  - Maps self-maintain via `ComponentChangedMessage<TKey>`: `entity.Set(newKey)` re-indexes the row,
    component removal / entity disposal removes the entry. This works ONLY because of the "always
    write through `entity.Set<T>(value)`" rule above — a single ref-mutation silently desyncs every
    maintained index.
  - `EntityMap` (PK index) THROWS `ArgumentException` on a duplicate key value — PK uniqueness is
    enforced fail-loud by the container itself.
  - DefaultEcs stores ONE component instance per type per entity ⇒ an entity carries AT MOST ONE FK
    into a given key space. A relationship that needs two references into the same space gets its own
    dedicated pair of FK types — a deliberate design decision, never an ad-hoc workaround.
- Query caches held as system fields (`EntitySet`, `EntityMap`, `EntityMultiMap`) are declarative.
  They do NOT count as forbidden system state under the stateless-systems ban.
- The map API is Try-pattern (`bool` return + `out` result) — the same contract as the Collector
  convention above. Branch on the `bool`.
- **Key equality:** a key component MUST implement `IEquatable<T>` + `GetHashCode`, or an
  `IEqualityComparer<T>` MUST be passed to the `AsMap` / `AsMultiMap` overload.
  `HexIdComponent` (delegates to `HexCoord`) and `HexTypeComponent` (keys on its enum)
  implement this — follow their shape for new key components.
- **Join = a lookup by key value at the point of use.** Never store an `Entity` reference from one
  table's row to another table's row.
- Maintained indexes are for hot joins (read every frame or many times per turn). A
  click-frequency query may linearly scan an `EntitySet` instead — do not build a map for it.
- Legacy bare-key queries were audited and fixed (2026-07-02; 8 sites — hex space got `HexTag`,
  actor-by-type lookups became id-PK + `ActorTypeComponent` table sets). The codebase holds ZERO
  bare-key queries — do not reintroduce the pattern; the ecs-graph's bare-key warnings catch regressions.

## Link Convention — Domain ID vs Entity Handle

- A **domain / persistent relationship** is expressed as a stable domain key, never as a stored
  `Entity` handle: the owner's identity is its `…IdComponent` (PK); a reference from another table
  is that space's `…FKComponent` wrapping the same value (key-role law, Table Rule above).
- A **runtime-only link** — non-serialized, lifetime-coupled, typically view-layer (a view
  component holding its `MonoBehaviour`) — may hold a direct reference or an `Entity` handle.
- Rationale: a stable ID survives save/load and map regeneration; a stale ID fails loud at
  resolution (`Try…` + throw), a stale `Entity` handle fails silent.

## Error Handling — Fail Loud

- A step that cannot do its job correctly MUST throw, not silently succeed. Missing or invalid
  prerequisites (absent / `!Exist` config, null prefab, missing required component) are errors —
  throw a descriptive exception naming exactly what is missing.
- **Forbidden:** silent `return` / `return UniTask.CompletedTask` on a missing prerequisite, and
  `Debug.LogWarning(...)` + skip. A "completed" step that did nothing hides the bug — the goal is a
  working game or a hard, visible failure, not a pile of warnings.
- Validate at the source (the config loader throws on `!Exist`) AND keep a defensive throw at the
  consumer as a second barrier — do not assume validation happened elsewhere.
- `cancellationToken.IsCancellationRequested` is the ONE legitimate quiet `return`. Keep it on its own
  line, separate from error conditions (never `if (cancelled || !valid) return;`).
- Clean up partial work before throwing (e.g. `Object.Destroy(instance)`).
- Exception type is not critical; `InvalidOperationException` with a message is the codebase default
  (see `TerrainGenerationConfigLoaderSystem`).
