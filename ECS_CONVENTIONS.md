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
| **Entity table** | N rows of the same shape (hexes, resources, views, icon containers) | the table's declared `Archetype` (Table Rule); keyed joins via `ComponentIndex<TComponent,TValue>` | the ecs-graph (`/ecs-graph`) |
| **World component** | exactly ONE instance, and NO consumer needs it in an entity query | `store.GetWorldComponent<T>()` / `store.SetWorldComponent(value)` | the ecs-graph (`/ecs-graph`) |
| **One-frame event entity** | a signal that something changed; consumed by every Reactive System exactly once, the frame AFTER it is raised | `store.CreateEvent(payload)`; `EventCleanupSystem` deletes it once ripe (Event Lifecycle below) | the ecs-graph (`/ecs-graph`) |
| **Singleton entity** | exactly ONE instance, but it MUST appear in entity queries (a per-frame system anchors on it, or reactive filters watch it) | its own declared archetype + tag, read via `TryGetFirst` | the ecs-graph (`/ecs-graph`) |

World component contract:
- A world component is a component on the ONE `UniqueEntity("world")` singleton row, reached only
  through `WorldComponentExtensions`. Mechanically it sits on an entity, but it is **not a table**:
  it carries no identity tag and no archetype filter names it, so nothing can query it. If its
  change must drive consumers, raise an explicit one-frame event entity alongside the write.
- All loaded configs are world components (`Patterns/PATTERN_CONFIG.md`). Runtime singletons follow the same
  storage: `CameraComponent`, `VertexGridComponent`, `TerrainTextureComponent`,
  `HexIconsViewComponent`, `HexIconsVisibilityComponent`.
- A world component carrying a **reference type** (`VertexGrid`, `Texture2D`) is set ONCE at
  creation; the payload object is then mutated in place by its writers. `SetWorldComponent` is never
  re-called after a mutation — readers always see the live object via `GetWorldComponent`.
- **Presence is not a runtime state.** `HasWorldComponent<T>()` survives for exactly one job: the
  fail-loud precondition check that a config was loaded before its consumer runs (throw, never skip).
  A runtime "not active" state is a **sentinel value** on an always-present component
  (`TurnProcessorStatus.Idle`, an empty `RootBox`), never an absent one — see Birth Completeness below.
- Singleton entities are the exception, not the default. Current ones exist because systems anchor
  per-frame ticks on them or query them: `TerrainViewComponent`, `HexSelectedComponent`,
  `WaterViewComponent`, `HexSelectionViewComponent`, `HexInfoPanelViewComponent`,
  `PlayerInputComponent`. When adding new single-instance state, default to a world component;
  create a singleton entity only when an entity-query consumer exists from day one. A singleton
  entity is still an entity — the Tag Law applies (it carries its tag, its archetype names it).

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
- The consumer is an `UpdatedSystem` driven by that event's own archetype
  (`EventArchetypes.Of<TheEvent>(store)`) → zero cost while no pulse exists. It gates on `IsRipe`
  and ignores the pulse entity itself; the pulse carries no payload to read.
- A consumer must never assume a same-frame reaction: an event is delivered the frame AFTER it is
  raised (Event Lifecycle below), so a chain of pulses spans a frame per link.
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
- An entity is created BY its archetype, never by a bare `CreateEntity()` — see Declared Archetypes
  below. Assign the row's values into a local entity variable; do not chain writes off the creating call.
- Prefer instance-based design; use `static` only when a type is truly stateless utility
  infrastructure (e.g. `ForestGroundPainter`).

## Statelessness And Collections (the two hard bans)

These two bans are machine-checked by `/arch-check`. They apply to every system.

**Apply via:** the applied per-recipe form is in each `Patterns/PATTERN_*.md` (the recipe's Rules).

**Ban 1 — no stateful systems.** A system holds no mutable per-instance state. Instance fields must
be `readonly` handles: DI dependencies, the `EntityStore`, query caches. What does NOT count as state:
- query caches (`Archetype`, `ArchetypeQuery`, `ComponentIndex<TComponent,TValue>`) — they are
  declarative, self-maintaining views of store state, resolved once in the constructor;
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

## Component Writes

Project rule: **always write components through `entity.AddComponent(value)`** — the upsert write path.
This is mandatory, and it is what keeps every maintained index truthful.

- **Do not** mutate through a `ref` into component storage. An `IIndexedComponent` type is backed by a
  `ComponentIndex`, and the index is updated by the WRITE CALL, not by the value: mutating the field in
  place leaves the row filed under its old key, so every keyed lookup silently returns wrong rows.
  `entity.GetComponent<T>()` is a read — treat its result as a value, never as a handle to mutate.
- `AddComponent` is an **upsert**: it updates the column when it exists, and adds it only when it does
  not. Under Birth Completeness (below) every column already exists at birth, so a runtime write is
  always a plain value write — it never migrates the entity between archetypes.
- **Change-only writes.** Compare first, write on difference. A write into an indexed column re-files
  the row; rewriting the same value each frame is pure churn.
- **There is no component reactivity, by design.** The engine offers no value-change observer, and the
  project adopts none: `store.OnComponentAdded` / `OnComponentRemoved` / `OnTagsChanged` are
  deliberately unused (0 call sites). The ONE reactive mechanism is the one-frame event pulse +
  reconcile (Decomposition Rules above). If a value change must drive a consumer, raise a pulse next
  to the write — adding an engine-level observer instead is a design decision for the whole project,
  never a local one.

## Declared Archetypes

Every entity kind is a NAMED archetype in code — one declaration serving both the row's birth and its
filter. A bare `store.CreateEntity()` at a call site is forbidden; only a holder resolves archetypes.

**Apply via:** the holder shape and a worked pair of archetypes live in `MapArchetypes`.

```clojure
(def archetype-law  ;; FM-13, 2026-08-05
  {:home      "one static <Assembly>Archetypes holder per ASSEMBLY (asmdef), not per feature — an archetype may only name components its own assembly can reference"
   :shape     "the holder RESOLVES, it does not describe: every member returns the live Archetype — `public static Archetype Hex(EntityStore store) => store.GetArchetype(ComponentTypes.Get<…>(), Tags.Get<…>())`. ComponentTypes/Tags are the method BODY, never public surface"
   :state     "the holder stores NOTHING, so it never binds to a store — the store arrives as a parameter (this is the project's static exemption: a field-less stateless helper)"
   :owner     "each system caches the Archetype it uses in its own readonly field, resolved once in the constructor — same idiom as a stored ComponentIndex"
   :birth     "archetype.CreateEntity() — the row is born BY its archetype with every column at default(T); the writes that follow are value upserts into columns that already exist"
   :bulk      "archetype.CreateEntities(n) for default-valued bulk rows; EnsureCapacity(n) first when the count is known"
   :arity-cap "ComponentTypes.Get / Tags.Get cap at 5 type arguments"
   :filter    "BY NEED, not dogma: where the target IS one archetype, iterate the archetype itself (archetype.Entities yields the same result type an ArchetypeQuery does, so no query is needed); ArchetypeQuery stays for a genuinely cross-archetype filter"
   :cross-archetype "a cross-archetype filter is reviewed WITH the user, never introduced unilaterally"})
```

**Birth Completeness — an entity is born carrying EVERY column it will ever hold**, defaults included.
Three binding consequences:
- A column's PRESENCE is never a predicate. Where presence used to carry meaning, the meaning moves
  into a **value**: a sentinel (`HexType.Unknown`, `DistrictType.Unknown`, `TurnProcessorStatus.Idle`)
  or an empty box.
- There are no optional columns. **A different composition is a different entity** — to change one,
  delete the row and create a new one in its archetype, carrying the PK/FK value over.
  `RemoveComponent` / `RemoveTag` appear nowhere in the codebase; do not reintroduce them.
- A late `AddComponent` of a column the archetype does not name is a bug, not an extension: it migrates
  the entity out of the archetype every filter names it by.

**Structural changes during iteration.** The engine's own definition governs here — this section restates
`Friflo.Engine.ECS` 3.6 (`StructuralChangeException`), it does not extend it:

```clojure
(def structural-change  ;; FM-13 review, 2026-08-05 — Friflo XML docs are the authority, this doc follows them
  {:is        #{AddComponent RemoveComponent AddTag RemoveTag}  ;; "A structural change is adding / removing components or tags" — these throw StructuralChangeException inside a query loop
   :is-not    archetype.CreateEntity                            ;; birth BY an archetype migrates nothing; the store-extension CreateEntity(components…, tags) overloads are documented as "without any structural change"
   :value-upsert-included "an AddComponent into a column the archetype ALREADY names still throws — the guard is on the call, not on whether the row moves"
   :guard     :store-wide                                       ;; the throw also fires for a structural call on an UNRELATED entity inside the dispatched work
   :delete    "not documented as a structural change, but it mutates the set being enumerated — snapshot anyway"})
```

So a row may be BORN inside an enumeration, but nothing may be written into it there. The idiom is
**snapshot-before-iterate**: collect `entity.Id` values into a `NativeList<int>`, close the enumeration,
then re-fetch each via `store.TryGetEntityById` and do the spawning and the writes in that second pass.
An `Entity` carries a store reference, so it is not `unmanaged` and cannot live in a native container —
snapshot ids, never entities. `UpdatedSystem` already does this for its subclasses.

Snapshotting the SOURCE ids (rather than buffering freshly-created entities plus their managed payload)
is what keeps the second pass allocation-free: `DistrictViewSpawnSystem` and `HexIconsSpawnSystem` are the
worked examples — one `NativeList<int>`, no managed side-buffer, archetype birth still at the call site.

## Event Lifecycle

An event is a payload-less one-frame pulse delivered to EVERY consumer exactly once, one full frame
after it is raised, independent of system priority.

```clojure
(def event-lifecycle  ;; FM-13, 2026-08-05
  {:archetype   "[EventTag, EventFrameComponent, payload] — resolved by EventArchetypes.Of<T>(store)"
   :raise       "store.CreateEvent(payload) — stamps Time.frameCount inside the creating call"
   :ripe-rule   "a consumer acts only while EcsEventExtensions.IsRipe(entity) — stamp < Time.frameCount, i.e. the frame AFTER birth"
   :cleanup     "EventCleanupSystem (Priority int.MaxValue) deletes ripe events at the end of that frame — after every consumer has had its full frame"
   :priorities  "SystemPriorities orders execution deterministically ONLY; event visibility no longer depends on it, so a 'must sit above/below the producer' rule cannot exist"
   :idempotency "consumers stay reconcile-style; never assume a same-frame reaction — a chain of pulses costs one frame per link"})
```

## Threading And Native Memory (UniTask × ECS)

Some `UniTask` code still runs off the main thread (`RunOnThreadPool`) for heavy computation. Two laws
govern what such code may touch.

**Law 1 — store access is MAIN-THREAD ONLY.** The `EntityStore` is not thread-safe, and the line is
simply store vs no-store: every read and every write happens on the main thread. Off-thread code
computes over plain data and hops back BEFORE it touches the store.

```clojure
(def store-thread-law  ;; FM-13, 2026-08-05
  {:main-thread-only   "every store call — CreateEntity, AddComponent, GetComponent, DeleteEntity, index lookups, raising pulses"
   :off-thread-allowed "computation over plain data and native containers only; worked examples: TerrainViewGenerationSubSystem, TerrainViewTextureSubSystem (mesh/texture math off-thread, every store write after the hop)"
   :bridge             "await UniTask.SwitchToMainThread() before the FIRST store call; back via SwitchToThreadPool for more compute"
   :bridge-cost        "a hop costs ~1 update/frame — SwitchToMainThread resumes at PlayerLoopTiming.Update, so a pool thread waits for the next frame"
   :turn-pipeline      "runs INLINE on the main thread — turn phases do not hop at all, so the phase-hop budget no longer exists"
   :why "there is no off-thread value-write loophole: an indexed column re-files its row on write, so an off-thread write corrupts the index rather than merely racing on bytes"})
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

An entity "table" is defined by its archetype, and every filter MUST name the table, not just the key.

- **Table = key component + discriminator tag, together in one declared archetype.** A filter on a bare
  key component is **forbidden** — it is a UNION of every table sharing that key space, not a table.
  Because a table's archetype is declared once in its holder and reused for both birth and filtering,
  the union cannot be expressed by accident.
- **Tag Law (2026-07-08; strengthened 2026-07-15 FM-11) — universal and machine-checkable:**

  ```clojure
  (def tag-law
    {:entity {:requires "EXACTLY 1 tag — its identity / table discriminator"}  ;; the tag defines the entity's boundary; an entity without a tag does not exist
     :filter {:requires "exactly 1 tag in every archetype declaration"}  ;; 2 identity tags describe a row that cannot exist — a DEAD filter
     :category-tag UITag                                        ;; a shared kind-marker satisfies the law (identity then rides on the *ViewComponent)
     :event-filter :exempt                                      ;; every event shares EventTag; its archetype is named by the payload component — the event IS the filter
     :state "…StateComponent wrapping an enum — NEVER a toggled tag"  ;; AddComponent re-files the row, so a state flip moves it between self-index slices automatically
     :kind  "…KindComponent wrapping an enum — NEVER a second tag"    ;; per-kind access = self-index ComponentIndex lookup by the enum value
     :why "tag = archetype identity → deterministic attribution; kind and state are COLUMNS of the row, not identities"})
  ```

- **Tag classes.** Only ONE class of tag exists — the identity/discriminator (plus the structural
  `EventTag` on pulses and the category `UITag`). What looked like other tag uses is data:
  a **runtime-toggled marker** is a state COLUMN → `…StateComponent { Value = enum }`, flipped via
  `AddComponent` (change-only writes: compare first, write on difference); a **subtype marker** inside a
  table family is a kind COLUMN → `…KindComponent { Value = enum }`, set once at birth. Both are legal
  self-index keys (`IIndexedComponent<TEnum>` over the family's own rows) — consumers read a slice with
  `index[value]` instead of scanning or tag-filtering.
- **Key-role law (2026-07-15, FM-11) — a key's role is visible in its TYPE.** The owner table's
  identity and other tables' references to it are DIFFERENT component types:

  ```clojure
  (def key-role-law
    {:pk   {:suffix "…IdComponent" :home "exactly ONE owner table, paired with its tag" :index "ComponentIndex<PK,TValue> — unique by contract, not enforced"}   ;; the row's own identity
     :fk   {:suffix "…FKComponent" :home "rows of OTHER tables pointing at the owner"   :index "ComponentIndex<FK,TValue> — 1:N"}                                ;; wraps the owner space's key VALUE
     :data {:suffix "…Component"   :never "keying indexes of two DIFFERENT tables"}                                                                              ;; attribute value; self-index allowed (exception below)
     :kind {:owner-side :data      :referencing-side :fk}   ;; enum key space with NO PK table (district types): the carrier's own value = Data, a catalogue/verb pointer = FK
     :why "a shared key TYPE re-creates the bare-key UNION at the type level; separate types make the compiler enforce the Table Rule"})
  ```

- **Secondary-index exception (self-index).** A Data component MAY be indexed when every row carrying
  it belongs to the SAME table — a join/select over the table's own attribute (hexes grouped by
  `HexTypeComponent`; resource rows by `HexResourceComponent`). The lookup VALUE may arrive from
  outside (converted from an FK or another table's field) — the boundary holds because every indexed
  row belongs to one table. The same Data type indexed across TWO different tables is the forbidden
  shared-key conflation — split it into PK + FK.

- The hex key space under the law:

  | Table | Discriminator | Key component | Role |
  |---|---|---|---|
  | Hex | `HexTag` | `HexIdComponent` | PK — one entity per coordinate |
  | HexResource | `HexResourceTag` | `HexIdFKComponent` | FK — N per coordinate (one per `ResourceType`) |
  | ForestView | `ForestViewTag` | `HexIdFKComponent` | FK — N per coordinate |
  | HexIconContainer | `HexIconContainerTag` | `HexIdFKComponent` | FK — one per coordinate |
  | DistrictView | `DistrictViewTag` | `HexIdFKComponent` | FK — one per built hex |

- One table has two access shapes — pick by access pattern:

  ```csharp
  // Sweep the whole table: the declared archetype IS the filter. No query object needed.
  private readonly Archetype _resources = MapArchetypes.HexResource(store);
  foreach (var row in _resources.Entities) { /* … */ }

  // Keyed join: an index over the key column. Declared once in the constructor, never rebuilt.
  private readonly ComponentIndex<HexResourceComponent, HexResourceType> _byResourceType =
      store.ComponentIndex<HexResourceComponent, HexResourceType>();
  foreach (var row in _byResourceType[HexResourceType.Forest]) { /* … */ }
  ```

- **Engine mechanics behind the law (verified against the 3.6.0 assembly):**
  - A `ComponentIndex<TComponent,TValue>` is keyed on the component TYPE and spans every table that
    carries it — the index alone is still a bare-key UNION. What names the table is the archetype the
    row was born in (Tag Law): the key type carries the ROLE, the tag carries the TABLE. So an index
    shared by two tables is a conflation, not a shortcut — split it into PK + FK types.
  - An index self-maintains on the WRITE CALL: `AddComponent` re-files the row, entity deletion drops
    the entry. This works ONLY because of the AddComponent-only rule above — one ref-mutation silently
    desyncs every index over that type.
  - **PK uniqueness is a contract, not an enforcement.** A `ComponentIndex` does not throw on a
    duplicate key — it simply returns both rows. A PK allocator that can repeat a value is a bug the
    engine will not catch for you; check at the allocation site and throw there.
  - **Index bucket cap: ≤100 entities per identical key value** — insert/remove is O(N) over the
    duplicates. Our buckets run units-to-tens; respect the cap in any NEW design.
  - The store holds ONE component instance per type per entity ⇒ an entity carries AT MOST ONE FK
    into a given key space. A relationship needing two references into the same space gets its own
    dedicated pair of FK types — a deliberate design decision, never an ad-hoc workaround.
- Query caches held as system fields (`Archetype`, `ArchetypeQuery`, `ComponentIndex`) are declarative.
  They do NOT count as forbidden system state under the stateless-systems ban.
- **Key equality:** an indexed component declares `IIndexedComponent<TValue>` and returns the key field
  from `GetIndexedValue()`; `TValue` must be equatable. An enum works directly (`HexResourceComponent`
  → `HexResourceType`); a struct key delegates to its own `IEquatable` implementation
  (`HexIdComponent` → `HexCoord`). Follow their shape for new key components.
- **Join = a lookup by key value at the point of use.** Never store an `Entity` reference from one
  table's row to another table's row.
- Indexes are for hot joins (read every frame or many times per turn). A click-frequency lookup may
  scan the archetype linearly instead — do not declare an index for it.
- Legacy bare-key queries were audited and fixed (2026-07-02; 8 sites — hex space got `HexTag`,
  actor-by-type lookups became id-PK + `ActorTypeComponent` tables). The codebase holds ZERO bare-key
  filters — do not reintroduce the pattern; the ecs-graph's warnings catch regressions.

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

## Open Directions — NOT rules

Recorded so the constraint is visible at the point of code, not carried in anyone's head. Nothing here
is in force; each needs its own decision before any code follows it.

```clojure
(def open-directions  ;; 2026-08-05, after FM-13
  {:multi-storage
     {:constraint "an EntityStore is capped at 256 component types and 256 tag types — the budget is finite and per-store"
      :now        "ONE store for the whole app run (created in WorldInstaller); ~155 component types, ~23 tag types"
      :direction  "if the budget runs out, the answer is more than one store — which raises questions this project has not answered: who owns which store, how a cross-store join is even expressed (an index never spans stores), and where the split falls"
      :status     :unanswered}

   :cumulative-command-buffer
     {:what     "work with entities from a parallel thread (or several) by recording changes into a CommandBuffer and syncing only the changes: recording is legal on any thread, Playback() only on main"
      :caveat   "until Playback() the store is UNCHANGED — a long chain of dependent changes must account for that: the second step does not see what the first recorded. Resource spending is the sharp case (a second spend re-reads the pre-playback balance and both succeed)"
      :now      "0 usages in Assets/**; Law 1 (main-thread-only) is what actually holds today"
      :status   :direction-only}})
```
