---
category: C
read: trigger
trigger: "before touching EntityStore registration, world/singleton component access, or any proposal to add a second ECS store"
tags: [ecs, storage, di, plan]
related:
  - "[ECS_CONVENTIONS](ECS_CONVENTIONS.md)"
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# PLAN — Entity Storages

Multi-session program: split singleton state into its own store, reached through one DI-registered storage alias.

> **Status: SPEC ONLY — no code written.** Every symbol this doc proposes is UNBUILT. Names carry a
> `^:new` marker. Decisions still open are listed in **Open Decisions**; do not implement past an open one.

---

## 1. Decision record

```clojure
(def storages-decision  ;; user, 2026-08-05 — answers that opened this program
  {:storage-kind    :second-entity-store   ;; Q1=1 — a real second Friflo EntityStore, holding ONE singleton row; NOT a POCO cache, NOT a facade over the game store
   :di-shape        :alias-registry        ;; Q2 — one DI-registered alias type owning EVERY store in the project; two bare EntityStore registrations would collide by type
   :call-shape      :named-storage-access  ;; Q3 — call sites name the storage they mean (storages.Singletons …), never a bare injected store
   :rollout         :staged                ;; Q4 — stage by stage, each stage compiles and plays on its own
   :order           (-> :b-separate-storage :a-declared-archetype)  ;; (б) first, (а) after — the archetype is declared on a row that already lives in its own store
   :s5-state-fields :out-of-scope})        ;; user: to be looked at separately — depends on whether Friflo has a pre/post-Update seam at all
```

## 2. What this buys

Three defects die with the split, all of them consequences of world state riding the game store's
singleton row:

- **Archetype migration on first write.** The singleton row is born carrying only its unique-entity
  marker, so the first write of each world-scoped component ADDS a column and migrates the row.
- **Structural-change trap.** That add is a structural change, and the guard is store-wide — a world
  write executed inside any query loop of the game store throws. With a separate store the game
  store's guard cannot see the write at all: **the trap disappears rather than being avoided**.
- **Name lookup per read.** Every read resolves the singleton by its unique-entity name first.

Plus the ceiling already recorded as an open direction in `ECS_CONVENTIONS.md`: **the 256-component /
256-tag limit is PER store.** Everything currently sits in one store. Moving singleton state out is the
first real headroom this project gains, which is why the alias registry is built to hold N stores even
though `split-law` allows only two today.

## 3. Hard constraint — what may and may not be split

This is the load-bearing section. **A store is a closed world**: an entity in store A cannot be
queried, indexed, or FK-resolved from store B, and `ComponentIndex` is per-store.

```clojure
(def split-law  ;; measured 2026-08-05; re-measure with Tools/asmdef_reach.py + ecsg.py before acting
  {:safe-split    #{:singletons}   ;; singleton state is read/written by name only — nothing joins against it
   :blocked-split #{:per-domain}   ;; :storage.Economy / :storage.Map CANNOT be separate stores today
   :evidence      "12 cross-assembly archetype-read edges: Presentation reads Domains.Map (10 files) and Domains.Economy (3); Presentation.UI reads Domains.Map, Domains.Economy, Domains.Actors, Presentation; Domains.Actions reads Domains.Actors and Domains.Economy; UserInput reads Domains.Map and Presentation"
   :events        :one-store       ;; the event archetype holder is used from 5 assemblies — events stay in the shared game store, never per domain
   :fk-cost       "a per-domain split would force every cross-domain FK to resolve BY VALUE across stores instead of by row — that is a different program, not a later stage of this one"
   :verdict       "the alias registry is built to hold many stores; today it holds exactly TWO"})
```

**Consequence for naming.** `Singletons` and `World` are the only members that may exist after this
program. `Economy` / `Map` members are a **gap entry**, not a roadmap item — they unblock only if the
cross-assembly reads above are removed first.

## 4. Target shape

```clojure
(def target  ;; every name here is ^:new
  {:alias-type    ^:new EntityStorages        ;; DI singleton; the ONLY thing systems inject to reach a store
   :members       {:World      "the game store — every entity row, every event, every table and index"
                   :Singletons "the singleton store — exactly ONE row, world-scoped state"}
   :registration  "created and registered in WorldInstaller.Configure, before any installer that writes state"
   :old-accessor  WorldComponentExtensions    ;; retargeted onto the singleton storage, then renamed — see stage S3
   :injection     "systems take EntityStorages, not EntityStore"
   :never         "a system holding a raw EntityStore field it resolved from the alias and passed around as 'the' store"})
```

Skeleton (shape only — not final code):

```csharp
// Alias registry: every EntityStore in the project, created once, named. Nothing else creates a store.
public sealed class EntityStorages
{
    public EntityStore World { get; }
    public SingletonStorage Singletons { get; }
}

// Owns its own EntityStore plus the single row inside it, and hides both.
public sealed class SingletonStorage
{
    public T Get<T>() where T : struct, IComponent;
    public bool Has<T>() where T : struct, IComponent;
    public void Set<T>(in T component) where T : struct, IComponent;
}
```

## 5. Blast radius (measured 2026-08-05 — re-measure, do not trust these numbers later)

```clojure
(def blast-radius
  {:store-injectors    88   ;; dig.py consumers EntityStore
   :singleton-writes   63
   :singleton-reads    96
   :singleton-presence 71
   :note "reads/writes are mechanical one-line rewrites; the injector change is what must be staged"})
```

## 6. Staged plan

Each stage is its own task map and its own `go`. A stage ends compiling and playable.

```clojure
[{:stage :s1-alias
  :do    "introduce EntityStorages holding ONLY the existing store as World; register it in WorldInstaller; nothing else changes"
  :skip  "the singleton store — s1 does not create a second store"
  :accept {:meter dig.py :target "EntityStorages registered; EntityStore still resolvable"}}

 {:stage :s2-migrate-injectors
  :do    "systems inject EntityStorages and read .World; the bare EntityStore registration is removed at the END of this stage"
  :risk  "88 injectors — split by assembly, one assembly per commit, Domains before Presentation"
  :accept {:meter dig.py :target "consumers EntityStore = 0"}}

 {:stage :s3-singleton-storage
  :do    "add SingletonStorage with its own EntityStore + one row; retarget the world-component accessors onto it; rename them off the World* prefix"
  :note  "the accessor rename is what makes the call sites say WHICH storage they mean (:call-shape)"
  :accept {:meter grep :target "WorldComponentExtensions gone; 0 singleton components on the game store"}}

 {:stage :s4-declared-archetype        ;; = step (а)
  :do    "declare the singleton row's archetype in a holder, born complete with every world-scoped column"
  :requires :s3
  :blocked-by :d2
  :accept {:meter ecsg.py :target "the singleton row resolves to ONE declared archetype; 0 late column adds"}}

 {:stage :s5-docs
  :do    "ECS_CONVENTIONS → State Storage Taxonomy rewritten for two stores; ARCHITECTURE → Stack/DI composition updated"
  :accept {:meter doc_lint :target "0 ghosts"}}]
```

## 7. Open decisions — do not implement past these

```clojure
(def open
  {:d1 {:q "SingletonStorage as a wrapper type, or a bare second EntityStore plus extension methods?"
        :tradeoff "wrapper hides the row and makes s4 trivial; bare store keeps one concept but leaves the row addressable by anyone"
        :agent-proposal :wrapper}
   :d2 {:q "Birth Completeness for the singleton row: can every world-scoped column be named in ONE archetype?"
        :fact "ComponentTypes.Get / Tags.Get cap at 5 type arguments, and the singleton carries far more columns than that"
        :consequence "if the cap cannot be worked around, step (а) is NOT 'one declared archetype' but something else — resolve BEFORE s4"
        :agent-proposal :unresolved}
   :d3 {:q "Do config components (loaded once at ConfigLoadStep) belong in Singletons, or do they earn a third storage?"
        :agent-proposal :singletons}
   :d4 {:q "Storage naming: World / Singletons — confirm or veto"
        :agent-proposal "World + Singletons"}})
```

## 8. Gap list

- Per-domain storages (`Economy`, `Map`) — **blocked**, see `split-law`; unblocks only after the 12
  cross-assembly archetype reads are removed.
- The stateful-system audit (35 mutable fields across 18 systems) is **not** part of this program;
  it waits on whether Friflo offers a pre/post-Update seam at all.
