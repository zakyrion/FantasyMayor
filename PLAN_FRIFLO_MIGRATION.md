---
category: C
read: trigger
trigger: "when executing the FM-13 DefaultEcs→Friflo migration — the executor's sole entry point; delete this doc when :s8 lands"
tags: [migration, ecs, friflo]
related:
  - "[ECS_CONVENTIONS](ECS_CONVENTIONS.md)"
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[DOC_STANDARD](DOC_STANDARD.md)"
---

# PLAN_FRIFLO_MIGRATION.md

Closed-spec plan: DefaultEcs 0.17.2 → Friflo.Engine.ECS 3.6.0. Executor = Sonnet 5; one phase per session.

<!-- doc-lint: off — this doc's anchors are Friflo DLL types (never declared in Assets/*.cs) plus
     ^:new symbols that exist only after their phase lands; verified against Friflo 3.6.0 docs 2026-07-18.
     The doc self-deletes at :s8, so no ghost survives the program. -->

## Program

```clojure
(def program
  {:ticket   "FM-13"
   :branch   "Tasks/FM-13-friflo — already created, working tree clean at start"
   :executor "Sonnet 5. This doc is the CLOSED spec: every decision below is user-confirmed 2026-07-18.
              Do NOT re-research the Friflo API — the API Map is verified against Friflo 3.6.0 docs.
              Do NOT reopen any entry of `decided` — reopening = process violation."
   :phases   [:s0-package :s1-core :s2-components :s3-domains :s4-presentation
              :s5-purge :s5b-archetypes :s6-gate :s7-graphs :s8-docs]
   :status   {:s0 :done :s1 :done :s2 :done :s3 :done :s4 :done :s5 :done
              :s5b :current                    ;; user 2026-08-04 — inserted BEFORE the gate, ahead of :s6
              :s6 :todo :s7 :todo :s8 :todo}   ;; executor flips an entry to :done only after its :accept reads green
   :session-rule "ONE phase per session. Open the session by restating the phase's task maps, ask
                  open questions if any, WAIT for explicit GO (HARD GATE unchanged), then execute."})
```

## Closed decisions — do NOT reopen

```clojure
(def decided  ;; user-confirmed 2026-07-18
  {:package          "Friflo.Engine.ECS 3.6.0 via NuGetForUnity; Friflo.Engine.ECS.Boost REJECTED (unsafe bounds-check elision, pays only on huge result sets)"
   :systems-layer    "OURS stays: UniTask runners + orchestrators + SystemPriorities + VContainer DI; Friflo SystemRoot/QuerySystem NOT adopted"
   :fk-model         "key-sharing via IIndexedComponent; ILinkComponent REJECTED — cascade delete fights pulse+reconcile, logical ids match the config space"
   :event-lifetime   "full-frame + exactly-once via frame stamp (Event Lifecycle spec below); the priority-lattice ordering comments dissolve"
   :threading        "Law 1 final form: store I/O is MAIN-THREAD ONLY; turn pipeline runs INLINE; heavy compute may run off-thread but hops to main BEFORE any store write"
   :world-components "one UniqueEntity(\"world\") singleton entity carries all former world components (spec below)"
   :folder-rename    {:from "Assets/Scripts/DefaultECSExtensions" :to "Assets/Scripts/EcsExtensions"
                      :asmdef-name {:from "DefaultECS.Extensions" :to "Ecs.Extensions"}
                      :namespace   {:from "DefaultECSExtensions"  :to "EcsExtensions"}}

   ;; ── :s5b-archetypes — user-confirmed 2026-08-04 ──────────────────────────
   :archetype-model  "DECLARED archetypes, not just one-shot CreateEntity overloads: every entity kind
                      is a NAMED archetype in code, used both to create the row and to build its filter"
   :archetype-home   "one static ArchetypeHolder per domain — <Domain>Archetypes"
   :holder-shape     "the holder RESOLVES archetypes, it does not describe them. Each member returns the live
                      Archetype: `public static Archetype Hex(EntityStore store) => store.GetArchetype(…)`.
                      ComponentTypes/Tags are the method BODY, never public surface — user 2026-08-04:
                      «список тегів і компонентів це не архетип». The holder stores NOTHING, so it never
                      binds to an EntityStore; the store arrives as a parameter."
   :archetype-owner  "each SYSTEM caches the Archetype it uses in its own field, resolved once in the
                      constructor — same idiom as the stored ArchetypeQuery fields already in place"
   :birth-completeness "an entity is born with EVERY column it will ever carry, defaults included
                        (precedent: HexTypeComponent moves from a late AddComponent to a birth column
                        with its default value) — a column's PRESENCE stops being a predicate; where
                        presence WAS the predicate it becomes a value/sentinel check"
   :composition-change "different composition = a DIFFERENT entity: the old one is deleted and a new one
                        is created in its archetype; PK/FK carry over. No optional columns."
   :filter-mode      "BY NEED, not dogma: exact archetype where the target IS one archetype; plain
                      tag/component filter where the query is genuinely cross-archetype (e.g.
                      EventCleanupSystem sweeps every event by EventTag). Cross-entity queries are rare —
                      each one is reviewed WITH the user, never decided unilaterally by the executor"
   :runtime-writes   "UNCHANGED: AddComponent-upsert stays the write law for a value into an existing
                      column — :s5b changes composition-at-birth, not the write path"})
```

## Scope snapshot (2026-07-18, informational — re-grep if in doubt)

```clojure
{:files-using-defaultecs 108
 :query-builds "91 AsSet/AsMap/AsMultiMap sites, 21 files hold keyed indices"
 :world-set-sites 26  :world-get-sites 82  :ref-get-reads 8  :create-entity-sites 40
 :event-types 14  :tags 23  :components ~155  ;; well under Friflo's 256-type limit
 :thread-hop-files ["Assets/Modules/Turn/Systems/TurnProcessorSystem.cs"          ;; goes INLINE (s3)
                    "Assets/Domains/Actions/Systems/MayorAPRestoreSubSystem.cs"   ;; goes INLINE (s3)
                    "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictTurnTickSystem.cs" ;; goes INLINE (s3)
                    "Assets/Presentation/Terrain/Systems/TerrainViewGenerationSubSystem.cs" ;; KEEPS off-thread compute; verify store writes happen after the hop to main
                    "Assets/Presentation/Terrain/Systems/TerrainViewTextureSubSystem.cs"]}  ;; same
```

## API Map — DefaultEcs → Friflo 3.6.0 (verified 2026-07-18)

| DefaultEcs | Friflo | Note |
|---|---|---|
| `new World()` | `new EntityStore()` | injected type changes everywhere: `World` → `EntityStore` |
| `world.CreateEntity()` | `store.CreateEntity()` | overloads accept components + `Tags.Get<T>()` in one call |
| `entity.Set(c)` | `entity.AddComponent(c)` | **upsert** — updates if present; this IS our Set() law |
| `ref readonly var c = ref entity.Get<T>()` | `entity.GetComponent<T>()` | read-only use; NEVER write through a ref |
| `entity.Has<T>()` | `entity.HasComponent<T>()` / `entity.Tags.Has<T>()` | components vs tags split |
| `entity.Remove<T>()` | `entity.RemoveComponent<T>()` / `entity.RemoveTag<T>()` | |
| `entity.Dispose()` | `entity.DeleteEntity()` | |
| `entity.IsAlive` | `!entity.IsNull` | Friflo detects stale/deleted handles |
| tag = empty struct | `struct XTag : ITag { }` | |
| data component | `struct X : IComponent { … }` | |
| keyed column (`IEquatable` key) | `struct X : IIndexedComponent<TValue> { public TValue V; public TValue GetIndexedValue() => V; }` | enum TValue expected to work (equatable); if compile refuses → underlying int |
| `world.GetEntities().With<A>().With<B>().AsSet()` | `store.Query<A,B>()` stored in a field | queries are built once and reused |
| `.With<SomeTag>()` (tag filter) | `.AllTags(Tags.Get<SomeTag>())` | also `AnyTags` / `WithoutAnyTags` |
| `EntityMap<K>` / `EntityMultiMap<K>` | `store.ComponentIndex<TComp,TValue>()[key]` | O(1); index auto-updates ONLY on `AddComponent` writes |
| iterate + dispose in loop | **FORBIDDEN**: `StructuralChangeException` | snapshot-before-iterate (existing idiom) or `store.GetCommandBuffer()` … `Playback()` |
| `world.Set(c)` (world component) | `WorldEntity.AddComponent(c)` | see World components spec |
| `world.Get<T>()` | `WorldEntity.GetComponent<T>()` | |
| `world.SubscribeComponentAdded/Removed` | `store.OnComponentAdded` / `store.OnComponentRemoved` | synchronous; `OnTagsChanged` for tags |
| `world.SubscribeEntityCreated/Disposed` | `store.OnEntityCreate` / `store.OnEntityDelete` | rarely used — verify each site's need survives |

Friflo constraints the executor must respect:

```clojure
(def friflo-constraints
  {:index-write   "an indexed component's value changes ONLY via AddComponent — a ref write silently corrupts the index (this is why the Set-only law survives verbatim)"
   :index-dup-cap "≤100 entities per identical key value (insert/remove O(N) over duplicates); our buckets are units-to-tens — respect it in NEW designs"
   :type-cap      "256 component types, 256 tag types per store — we sit at ~155/23"
   :iteration     "NO structural change (AddComponent/AddTag/DeleteEntity/…) while enumerating a query — throws StructuralChangeException; snapshot or CommandBuffer"
   :thread        "store is NOT thread-safe; CommandBuffer may record on any thread but Playback() on main only"})
```

## Event Lifecycle spec (replaces the one-frame + priority-lattice model)

```clojure
(def event-lifecycle  ;; delivery contract: EVERY consumer sees EVERY event EXACTLY once, priority-independent
  {:archetype    [EventTag ^:new EventFrameComponent "payload component(s)"]
   :stamp        "producer stamps Time.frameCount at creation via the CreateEvent helper"
   :ripe-rule    "a consumer processes an event only when stamp < Time.frameCount — i.e. every event is consumed by ALL consumers exactly one frame after birth"
   :cleanup      "EventCleanupSystem (priority int.MaxValue, unchanged slot) deletes events with stamp < Time.frameCount at END of frame — after every consumer had its full frame"
   :consequence  "SystemPriorities loses ALL 'must sit above/below producer X' comments; priorities remain only as deterministic order, event visibility no longer depends on them"
   :idempotency  "consumers stay reconcile-style (already idempotent); a consumer must not assume same-frame reaction anywhere"})
```

```csharp
// EcsExtensions/EventFrameComponent.cs
public struct EventFrameComponent : IComponent { public int Frame; }

// EcsExtensions/EcsEventExtensions.cs — stateless static helpers (allowed: field-less)
public static class EcsEventExtensions
{
    public static Entity CreateEvent<T>(this EntityStore store, in T payload) where T : struct, IComponent
    {
        var e = store.CreateEntity(new EventFrameComponent { Frame = Time.frameCount }, Tags.Get<EventTag>());
        e.AddComponent(payload);
        return e;
    }

    // consumer-side gate: true exactly one full frame, the frame after birth
    public static bool IsRipe(in Entity eventEntity) =>
        eventEntity.GetComponent<EventFrameComponent>().Frame < Time.frameCount;
}

// EventCleanupSystem core: snapshot ids first (no structural change during enumeration), then delete ripe ones.
```

## World components spec (49 former `world.Set` values)

```clojure
(def world-components
  {:home       "ONE singleton entity created in WorldInstaller: store.CreateEntity(new UniqueEntity(\"world\"))"
   :access     "stateless extensions in EcsExtensions: GetWorldComponent<T>(this EntityStore) / SetWorldComponent<T>(this EntityStore, in T)"
   :lookup     "store.GetUniqueEntity(\"world\") inside the extensions — indexed, O(1)-class"
   :write-rule "SetWorldComponent uses AddComponent (upsert) — Set-only law applies to the world row too"
   :porting    "world.Set(x) → store.SetWorldComponent(x); world.Get<T>() → store.GetWorldComponent<T>() — mechanical, 26 + 82 sites"})
```

## Porting laws (apply in every phase)

```clojure
(def porting-laws
  {:writes           "component writes ONLY via AddComponent — never through a ref (Friflo indexed components make ref-writes corrupting, not just illegal)"
   :reads            "GetComponent<T>(); the 8 `ref … Get<>` read sites become plain reads"
   :queries          "every AsSet/AsMap/AsMultiMap becomes a stored ArchetypeQuery or ComponentIndex field built in the constructor — never rebuilt per frame"
   :structural       "snapshot-before-iterate stays the default idiom; CommandBuffer only where a snapshot is genuinely awkward. CORRECTION found at :s1: Friflo's Entity struct carries a store reference — it is NOT unmanaged, so it cannot go in NativeList<Entity>/NativeArray<Entity> (unlike DefaultEcs's Entity). Snapshot entity.Id (int) instead and re-fetch via store.TryGetEntityById(id, out entity). Affects every existing snapshot-before-dispose system in :s3/:s4 (ForestDespawnSystem, BuildDistrictCompletionSystem, similar)."
   :batch-override   "one system (BuildDistrictCompletionSystem) uses DefaultEcs's batch Update(state, ReadOnlySpan<Entity>) override, not per-entity — the new UpdatedSystem base (built at :s1-runners) only supports per-entity dispatch (query.Entities). When :s3-domains ports this file, implement IUpdatedSystem directly instead of extending UpdatedSystem, same precedent as TurnProcessorSystem."
   :fail-loud        "unchanged: throw on missing prerequisites; a failed index lookup that must succeed → throw, never skip"
   :zero-alloc       "unchanged: no managed collections as system state; Unity.Collections for scratch; stored queries/indices are fine (Friflo-owned)"
   :meta-files       "NEVER hand-write .meta; folder/file renames move the existing .meta via git mv; metas for NEW files are generated by the user opening Unity"
   :roslyn           "run mcp__roslyn__get_diagnostics scoped to edited files per phase; CS0246 on symbols created in the SAME batch = expected staleness, name it once, move on; Unity is the authority"
   :architecture-md  "FROZEN — the executor NEVER edits it; s8 produces a proposed diff in chat for the user"})
```

## Phases

### :s0-package — install Friflo alongside DefaultEcs

```clojure
[{:task :s0-install
  :where "Assets/packages.config"
  :do    "add <package id=\"Friflo.Engine.ECS\" version=\"3.6.0\" manuallyInstalled=\"true\" /> — DefaultEcs entry STAYS until :s5"
  :result "user-side (Unity): open editor → NuGetForUnity restores → Assets/Packages/Friflo.Engine.ECS.3.6.0 + metas appear"
  :accept {:meter "ls Assets/Packages/" :target "Friflo.Engine.ECS.3.6.0 directory present (after the user's Unity pass)"}}]
```

### :s1-core — EcsExtensions layer + world bootstrap

```clojure
[{:task :s1-rename
  :where "Assets/Scripts/DefaultECSExtensions/ → Assets/Scripts/EcsExtensions/"
  :do    "git mv folder + its .meta; rename asmdef DefaultECS.Extensions → Ecs.Extensions (file + name field + every referencing asmdef); namespace DefaultECSExtensions → EcsExtensions in all declarations and usings repo-wide"
  :accept {:meter "grep -r DefaultECSExtensions Assets --include=*.cs --include=*.asmdef | wc -l" :target "0"}}

 {:task :s1-event-lifecycle
  :where "Assets/Scripts/EcsExtensions/"
  :do    "create EventFrameComponent + EcsEventExtensions per the Event Lifecycle spec; rewrite EventCleanupSystem: snapshot ripe event entities, DeleteEntity after enumeration"
  :listen :s1-rename}

 {:task :s1-world-access
  :where "Assets/Scripts/EcsExtensions/"
  :do    "create GetWorldComponent/SetWorldComponent extensions per the World components spec"}

 {:task :s1-runners
  :where "Assets/Scripts/EcsExtensions/"
  :do    "port runner/interface files (IUniTaskSystem family) off DefaultEcs types; most are ECS-agnostic — touch only what references World/Entity"}

 {:task :s1-priorities
  :where "Assets/Scripts/EcsExtensions/SystemPriorities.cs"
  :do    "strip every 'must sit above/below X to see the pulse' comment (obsolete under Event Lifecycle); keep the numeric order as deterministic-order documentation; TurnProcessor stays 1000"}

 {:task :s1-installer
  :where "Assets/Scripts/Installers/World/WorldInstaller.cs"
  :do    "new EntityStore() + RegisterInstance; create the UniqueEntity(\"world\") singleton BEFORE any world-component write; port the PlayerInput entity creation"
  :listen :s1-world-access}]
```

### :s2-components — all struct declarations

```clojure
[{:task :s2-plain
  :where "Assets/Domains/ Assets/Modules/ Assets/Presentation/ (component/tag/event declarations only)"
  :do    "data & config & event-payload structs get : IComponent; tag structs get : ITag; EventTag itself : ITag"}

 {:task :s2-indexed
  :where "the keyed columns (authoritative list = ecsg.py tables, snapshot below)"
  :do    "each becomes IIndexedComponent<TValue> with GetIndexedValue() returning the key field; drop now-redundant IEquatable ceremony where present"
  :decided "known set 2026-07-18: CityIdFKComponent DistrictIdComponent DistrictIdFKComponent DistrictOpenConditionKindComponent DistrictOpenStateComponent DistrictTypeComponent HexIdFKComponent HexResourceComponent HexTypeComponent MayorIdFKComponent"}

 {:task :s2-event-frame
  :do    "every event-producing site will add EventFrameComponent via CreateEvent — declaration side needs nothing beyond :s1; verify all 14 event payload structs are IComponent"}]
```

### :s3-domains — systems in Assets/Domains + Assets/Modules

```clojure
[{:task :s3-port
  :where "Assets/Domains/ Assets/Modules/"
  :do    "apply the API Map file-by-file: injected World→EntityStore; queries→stored ArchetypeQuery/ComponentIndex; event production→store.CreateEvent; event consumption→IsRipe gate; world.Get/Set→World-component extensions"
  :pattern "PATTERN_REACTIVE_SYSTEM / PATTERN_CLEANUP_SYSTEM stay the recipes — only the API inside changes"}

 {:task :s3-turn-inline
  :where "Assets/Modules/Turn/Systems/TurnProcessorSystem.cs, Assets/Domains/Actions/Systems/MayorAPRestoreSubSystem.cs, Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictTurnTickSystem.cs"
  :do    "remove RunOnThreadPool/SwitchToMainThread — phases run inline on main thread; RunTurnAsync().Forget() stays"
  :accept {:meter "grep -rn 'RunOnThreadPool\\|SwitchToMainThread' Assets/Modules Assets/Domains" :target "0 hits"}}]
```

### :s4-presentation — systems in Assets/Presentation + Assets/Scripts leftovers

```clojure
[{:task :s4-port
  :where "Assets/Presentation/ Assets/Scripts/"
  :do    "same API-Map pass; terrain subsystems KEEP off-thread compute but every store write sits after the hop to main (Law 1)"
  :accept [{:meter "grep -rl 'using DefaultEcs' Assets --include=*.cs | wc -l" :target "0"}
           {:meter "mcp__roslyn__get_diagnostics scoped to Assembly-CSharp + module projects" :target "no errors beyond known same-batch staleness"}]}]
```

### :s5-purge — remove DefaultEcs completely

```clojure
[{:task :s5-purge
  :where "Assets/packages.config + repo-wide"
  :do    "delete the DefaultEcs package entry; user-side Unity pass removes Assets/Packages/DefaultEcs.0.17.2; purge per the remove-completely rule: reword passing mentions in comments, no deferred tails"
  :accept {:meter "grep -ri defaultecs Assets Tools --include=*.cs --include=*.asmdef --include=*.config | wc -l" :target "0"}}]
```

### :s5b-archetypes — declared archetypes for birth AND filtering

Sits BEFORE the gate deliberately: it rewrites every creation site, so one `:s6` playtest covers the
purge and the archetype model together instead of gating twice.

```clojure
(def s5b-scope  ;; measured 2026-08-04, informational — re-grep if in doubt
  {:tags 22                    ;; EventTag and UITag are CATEGORY tags (Tag Law) — 14 event types under
                               ;; EventTag, several panels under UITag; each is its own archetype
   :birth-sites 26             ;; bare CreateEntity() + AddComponent chain
   :one-shot-already 2         ;; WorldInstaller (world singleton, PlayerInput)
   :filter-sites {:query 91 :tag-filter 75 :component-index 64}
   :store {:instances 1 :where WorldInstaller}})   ;; one store for the whole app run
```

```clojure
[{:task :a1-holders
  :status :done                                 ;; 2026-08-04 — 8 holders, one per owning assembly, roslyn clean
  :goal  "the archetype becomes a NAMED thing in code — one source for both birth and filter"
  :where "one per domain under Assets/Domains/<D>/, Assets/Presentation/<S>/, Assets/Modules/<M>/"
  :do    "static <Domain>Archetypes holder per `holder-shape`: static readonly ComponentTypes + Tags
          per archetype, plus field-less accessors over store.GetArchetype(types, tags)"
  :decided {:static-exception "allowed — the holder has NO state fields, so it stays a stateless
                               field-less helper (the project's only static exemption)"
            :naming :by-naming-policy}   ;; self-sufficient without namespace: HexArchetypes, DistrictArchetypes
  :result "every entity kind has exactly one declared archetype"}

 {:task :a2-birth
  :status :done                                 ;; 2026-08-04 — 25 sites/22 files ported; every AddTag<> call
                                                 ;; eliminated repo-wide (0 remaining); roslyn clean
  :listen :a1-holders
  :where "the 26 bare-CreateEntity sites"
  :do    "the ARCHETYPE creates the entity, then values are written into its columns"
  :decided {:birth-call "archetype.CreateEntity() — the entity is born BY its archetype, already carrying
                         every column at default; the following AddComponent calls are value upserts into
                         existing columns, so none of them migrates the entity.
                         REJECTED 2026-08-04: store.CreateEntity(values…, tags) — it lands in the right
                         archetype but never uses the Archetype object, which is the point of the phase."
            :defaults "birth-completeness comes free: CreateEntity() gives every column default(T), and the
                       two sentinels we need (HexType.Unknown, DistrictType.Unknown) are already 0"
            :archetype-cannot-carry-values
                  "VERIFIED against the 3.6.0 assembly 2026-08-04: Archetype offers CreateEntity() and
                   CreateEntities(int) — BOTH produce rows with default values only. Friflo has no
                   value-carrying creation on Archetype, so 'the archetype creates the entity' and 'no
                   AddComponent' cannot both hold. Values must come from the creating call instead.
                   (The package XML docs omit ArchetypeExtensions/CreateEntities — an earlier entry here
                   claimed CreateEntities did not exist; that was wrong, the DLL has it.)"
            :bulk "Archetype.CreateEntities(int) for default-valued bulk rows; EnsureCapacity(n) first when
                   the count is known."
            :arity-cap "ComponentTypes.Get/Tags.Get cap at 5 type args — our widest archetype carries 4"}
  :decided "apply `birth-completeness` — a column added later today becomes a birth column with its default"
  :accept {:meter "grep -rn 'CreateEntity()' Assets --include=*.cs" :target "0 outside the holders"}}

 {:task :a3-events
  :status :done                                 ;; 2026-08-04 — landed with :a1; CreateEvent is now a single creating call
  :listen :a1-holders
  :where EcsEventExtensions "+ the CreateEvent call sites"
  :do    "each event type gets its archetype [EventTag EventFrameComponent payload]; CreateEvent<T>
          creates directly in it — the payload is no longer a second AddComponent"
  :decided "Event Lifecycle (frame stamp, IsRipe gate, cleanup slot) is NOT reopened — only birth changes"
  :skip   "EventCleanupSystem keeps its EventTag sweep — that filter IS cross-archetype by design"}

 {:task :a4-filters
  :status :done                                 ;; 2026-08-04 — TryGetFirst(Archetype) overload added;
                                                 ;; UpdatedSystem/LateUpdatedSystem gained an
                                                 ;; (EntityStore, Archetype) constructor alongside the
                                                 ;; existing ArchetypeQuery one; every exact-archetype
                                                 ;; site (driving queries + lookup fields, ~50 files)
                                                 ;; converted to the matching holder call; the one
                                                 ;; genuinely cross-archetype driving query
                                                 ;; (HexInfoPanelDistrictSystem, AnyComponents over 3
                                                 ;; event types) stayed on ArchetypeQuery by design;
                                                 ;; roslyn clean (0 errors)
  :listen :a1-holders
  :where "the Query<> / tag-filter sites"
  :do    "per `filter-mode`: exact archetype where the target is one archetype, plain tag/component
          filter where the query genuinely spans archetypes"
  :decided {:exact-archetype-mechanism
                  "VERIFIED against the 3.6.0 assembly 2026-08-04 (roslyn get_symbol_info against the
                   real metadata — Archetype.Entities has no XML doc comment, so it is invisible to a
                   docs-only search): Archetype.Entities and ArchetypeQuery.Entities both return the SAME
                   QueryEntities type. So an exact-archetype filter site needs NO query at all — resolve
                   the Archetype via the SAME holder method already used for that archetype's creation
                   (one field, one holder call, composition written once), and iterate/Count/TryGetFirst
                   on it exactly like an ArchetypeQuery today. ArchetypeQuery stays reserved for
                   genuinely cross-archetype filters (fluent AllTags/AnyTags/HasValue/ValueInRange,
                   multi-archetype Archetypes span) — unchanged by this decision."
            :tryfirst-gap
                  "QueryResultExtensions.TryGetFirst (Assets/Scripts/EcsExtensions/QueryResultExtensions.cs)
                   currently overloads ArchetypeQuery and the ComponentIndex<T,V> indexer's Entities type
                   only — needs a third overload (Archetype, or QueryEntities directly) before
                   exact-archetype sites can drop ArchetypeQuery."}
  :must-not "decide a cross-archetype case alone — surface it to the user and decide together"
  :skip   "ComponentIndex lookups — keyed access, not an archetype filter; unchanged"}

 {:task :a5-transitions
  :goal  "the consequence of `composition-change`"
  :do    "where composition changes mid-life today (first-time AddComponent of a new column, or
          RemoveComponent), delete the old entity and create a new one in its archetype; PK/FK carry over"
  :decided "Table Rule unchanged"}]
```

```clojure
(def s5b-off-limits
  {:runtime-writes "AddComponent-upsert into an existing column — the FM-13 write law, untouched"
   :s6-fixes       "the uncommitted snapshot-before-iterate fixes in the working tree — neither rewritten nor reverted"
   :ecs-graph      "teaching the extractor the archetype surface is :s7, not here"
   :architecture   "ARCHITECTURE.md stays FROZEN — :s8 produces the proposed diff"})
```

### :s6-gate — Unity compile + playtest parity (user-side)

```clojure
[{:task :s6-verify
  :result "user compiles in Unity and playtests; agent fixes what surfaces"
  :accept {:meter "playtest checklist" :target "map gen · hex select · district confirm/cancel/complete · forest spawn/despawn · turn end + AP restore · resource bar · open-condition re-gate · REPEATED district-row clicks across several turns (the ex-chimera repro) — all clean, no InvalidOperationException"}}]
```

### :s7-graphs — retarget the derived tooling

```clojure
[{:task :s7-ecs-graph
  :where "~/.claude/skills/ecs-graph/ (build/extract scripts)"
  :do    "extractor learns Friflo surface: CreateEntity(components, Tags.Get<>), AddComponent, Query<> + AllTags/WithoutAnyTags, ComponentIndex, IIndexedComponent (PK/FK/IDX), ITag/IComponent decls, store.On* subscriptions, CreateEvent helper; drop DefaultEcs patterns"
  :accept {:meter "ecsg.py stats after full rebuild" :target "curated true · archetypes ≈23 · tables ≈31 · events 14 · warnings ≤ baseline(5)"}}

 {:task :s7-di-graph
  :do    "DI wiring unchanged by migration; rebuild and verify only"
  :accept {:meter "dig.py stats" :target "curated true, no new unresolved"}}]
```

### :s8-docs — decreed docs catch up

```clojure
[{:task :s8-conventions
  :where "ECS_CONVENTIONS.md"
  :do    "laws reworded: Set-only → AddComponent-only (same law, new anchor); Law 1 = store I/O main-thread only; NEW Event Lifecycle law from the spec above; all DefaultEcs API anchors → Friflo"}

 {:task :s8-patterns
  :where "Patterns/*.md (15 files)"
  :do    "swap API inside skeletons to Friflo per the API Map; recipes' intent unchanged"}

 {:task :s8-architecture
  :where "ARCHITECTURE.md"
  :do    "executor produces a PROPOSED DIFF in chat (frozen doc — user applies/approves); content: ECS engine name, Table Rule wording over IIndexedComponent, event lifecycle policy"
  :off-limits "editing the file directly"}

 {:task :s8-flow
  :where "Flows/FLOW_DISTRICT_BUILD.md"
  :do    "close :g7 with a dated note: chimera class eliminated by FM-13 engine migration; drop DefaultEcs-specific wording"}

 {:task :s8-index
  :do    "python3 Tools/gen_index.py; python3 Tools/doc_lint.py --quiet"
  :accept {:meter "doc_lint ghosts" :target "≤ pre-migration baseline (3)"}}

 {:task :s8-self-delete
  :do    "this plan doc is deleted (record = commit history), per the FM-11 precedent"
  :only-when "user confirms :s6 playtest green and :s7/:s8 accepted"}]
```
