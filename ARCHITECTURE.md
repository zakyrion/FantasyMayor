---
category: C
read: trigger
trigger: before any engineering task — request routing puts it into :read
tags: [architecture, ecs, conventions]
related:
  - "[DOC_STANDARD](DOC_STANDARD.md)"
---

# FantasyMayor — Architecture Reference

Every code law of FantasyMayor in full: systems, state, memory, entities, events, views, configs, threads, naming.

Every map key that is a namespaced keyword IS a rule id. A compile error, a graph warning or a review
finding cites that exact text, so the id is searchable here. The value states the rule in full and names
the live types it lands on; `;;` carries the enforcement point and the reason a rule is not worth arguing
with. Placement of new files and the choice of a `Patterns/` recipe are decided by the project skills
`fantasymayor-placement` and `fantasymayor-pattern-choice`, not here.

## Stack

- Engine: Unity · ECS: `Friflo.Engine.ECS` 3.6 (DoD style, not Unity DOTS) · DI: `VContainer` · Async: `UniTask`
- Assets: `Addressables` · Input: `InputSystem` · Rendering: URP · UI: `UI Toolkit` (UXML/USS) + `Unity App UI` (`com.unity.dt.app-ui`)
- Storage registry: `EntityStorages` — `World` (the game `EntityStore`), `Singletons` (single-instance components), configs by type
- Shared kernel of ECS types: `Assets/Scripts/EcsExtensions/` · primitives: `Assets/Scripts/Core/`

## Systems

```clojure
(def systems  ;; what the engine drives, how it is cut, what it caches
  {:system/definition
   "a system is a non-abstract class the engine drives: directly or transitively UpdatedSystem, LateUpdatedSystem, IUpdatedSystem, ILateUpdatedSystem, IUniTaskSystem, IPrioritizedUniTaskSystem, ConfigLoaderSystem<T> or EventCleanupSystem — nothing outside that list is a system"

   :system/subsystem-is-not-a-system
   "a member of a DI-collected family is a plain object with IDisposable owned by a system: the state ban and the managed-collection ban do not bind it"  ;; the graph's sub_system role is family membership, not systemhood

   :system/cadence
   "cadence comes from the stage the system serves, never from its interface: one-shot — a startup step, a config loader, a map-creation stage or subsystem; repeated — per-frame, reactive on every event, a turn phase on every turn"

   :system/query-caches-in-constructor
   "a system keeps its Archetype, ArchetypeQuery and ComponentIndex in readonly fields resolved once in the constructor"  ;; checked by /arch-check

   :system/driven-by
   "a per-frame system is driven either by a work table — the declared archetype it processes — or by a tick anchor, a singleton archetype whose presence switches the tick on; an ArchetypeQuery object only for a genuinely cross-archetype set"

   :system/base-choice
   (cond (one-shot-startup-step? system) IUniTaskSystem
         (ordered-pipeline? system)      "IPrioritizedUniTaskSystem<TStep> with the stage in the type argument; when the family already has an abstract base, that base"
         (after-every-update? system)    LateUpdatedSystem
         :else                           UpdatedSystem)

   :system/split-when
   #{"the system creates and destroys the same kind of content"
     "it diffs world state every frame to find out what changed"
     "it holds more than two unrelated query families"
     "it runs in several game states for different reasons"}

   :system/split-into
   {:startup-bulk   "a one-shot map-creation stage or subsystem"
    :runtime-duty   "one reactive system per responsibility"
    :shared-compute "a stateless helper in Helpers/"}

   ;; a one-element set needs no snapshot; a one-line tag swap needs no subsystem family
   :system/simplest-structure
   "code takes the simplest structure that solves the task: a pattern appears only when cardinality or real complexity demands it, never because a document or a comment mentions it"})

(def system-role  ;; the first true branch wins; these are exactly the roles fmgraph.py systems --role takes
  {:system/role-order
   (cond (sweeps-events-and-deletes? class)                           :cleanup
         (event-anchor-in-the-base-call? class)                       :reactive
         (and (update-loop? class) (holds-an-event-archetype? class))  "the role marker decides — see Markers"
         (update-loop? class)                                         :per_frame
         (map-creation-stage-in-the-type-argument? class)              :pipeline_stage
         (derives-from-turn-phase-base? class)                         :turn_phase
         (non-generic-unitask-contract? class)                         :startup_step
         (abstract-ancestor-someone-collects? class)                   :sub_system
         :else                                                         :no-role)})

(def system-order  ;; SystemPriorities is the one holder of execution order
  {:system/priority-source
   "Priority returns a named const int from SystemPriorities — never a class-local constant and never a literal"  ;; the graph accepts any resolvable const int, so it measures weaker than the rule

   :system/priority-space
   "each nested class of SystemPriorities is its own order space: values compare only inside one space, every member of a space carries a distinct value, and subsystem priorities compare only inside their own orchestrator"

   :system/priority-is-order-only
   "priority sets execution order and nothing else — never a rule that a consumer must sit above or below its producer"})
```

## Markers

```clojure
(def markers  ;; MarkerShapeAnalyzer, category FantasyMayor.Markers, severity Error — a marker that contradicts the shape FAILS the Unity compile
  {:system/marker-required
   "the role marker is required in exactly one shape — the one form no ordering rule decides: an Update-loop class that holds an event archetype outside base(...). It carries [SystemRole(SystemRoleKind.Reactive)] or [SystemRole(SystemRoleKind.PerFrame)]"  ;; the graph only warns today; the rule wants a compile error

   :system/marker-forbidden
   "a role marker on a class whose own shape already decides its role is forbidden"  ;; a redundant marker is only a warning today; by the owner's decision it is an error

   :system/marker-value
   "the marker value must match the class shape: PerFrame demands the Update-loop contract with no event anchor; Reactive demands the Update-loop contract with no table anchor, plus either an event anchor or a held event archetype"  ;; the analyzer measures the value; the graph's role reader still allows any value on a table anchor

   :system/marker-not-inherited
   "no marker is inherited — every concrete class or struct carries its own"

   :system/marker-vocabulary-parity
   "the analyzer's own copy of the role enum must stay equal to SystemRoleKind in EcsExtensions — the attribute argument reaches the analyzer as a plain int"  ;; nothing checks this today

   :view/subscriber-marker
   "a class that subscribes to a view's C# event with its own += carries [ViewSubscriber(typeof(TheView))] naming that view; a marker without a real subscription and a subscription without a marker are both violations, and the named type must derive from MonoBehaviour"

   :tag/label-marker
   "a label tag is a tag struct carrying [TagLabel], with a role or without; only a struct implementing ITag can be a label"})
```

## System state

```clojure
(def system-state  ;; the stateless-system ban — /arch-check is the detector
  {:state/no-instance-state
   "a system holds no mutable instance state"

   :state/is-state
   #{"any reassignable field"
     "a readonly field of a mutable container or buffer whose contents change between frames — a collection, an array, a StringBuilder, a native buffer held between ticks"
     "a settable auto-property on the instance"}

   :state/is-not-state
   #{"readonly immutable dependencies and store handles"
     "query caches resolved once in the constructor — Archetype, ArchetypeQuery, ComponentIndex"
     "const and static readonly values"}

   :state/mutable-static
   "a mutable static field in a system IS state — the not-state list above is closed and holds only const and static readonly"  ;; the detector flags no static field at all: a tool gap, never a permission

   :state/default-home
   "a status flag, an in-flight marker, a progress counter, a started-or-finished bit and a handle to what is being processed live on an entity or in single-instance state outside the system"

   :state/lifetime-flags
   "a subscription guard, a re-entry guard, a disposal guard and an asset-handle field in a system are state: each such field carries [StateAllowed(«reason»)]"

   :state/preupdate-cache
   "a frame cache resolved in PreUpdate is state: either a marked field with a reason or a refactor, and the owner makes that choice"

   :state/injected-collection
   "a field holding a DI-collected list inside a system carries [StateAllowed(«reason»)]"

   :state/escape-order
   (-> (:step-1 "move the state onto an entity component or into single-instance state — always tried first")
       (:step-2 "FrameBox — a value valid for a bounded number of frames")
       (:step-3 "[StateAllowed(«reason»)] — a reviewed exception, always with a reason"))

   :state/frame-box-use
   "a bounded-frame value lives in FrameBox: Value is read only after Exist, the box is disposed on teardown, and in an async system a box is forbidden — an await crosses frames and the box goes stale"

   :state/allowed-reason
   "the state marker always carries a reason, even though the attribute allows a parameterless call"  ;; nothing checks this today

   :state/frame-box-field
   "a FrameBox field in a system is state and carries the marked exception with a reason — the box itself is a mutable struct in a reassignable field"})
```

## Collections and memory

```clojure
(def memory-by-cadence  ;; the collection is chosen by cadence, the allocator by lifetime — /arch-check is the detector
  {:alloc/view-exempt
   "view-layer code may use managed collections — a view is not a system"

   :alloc/one-shot-exempt
   "a system of one-shot cadence may use managed collections — a startup step, a config loader, a map-creation stage or subsystem"

   :alloc/repeated-zero
   "a system of repeated cadence runs at zero allocation: memory claimed at the start of a run is released at its end, and the run stands on Unity.Collections native containers, structs and spans"

   :alloc/banned-generic-types
   "inside a repeated system both the System.Collections.Generic using directive and the full names of its types are banned — lists, dictionaries, sets, queues, stacks, the sorted family and their interfaces; the platform array, spans and the non-generic collections are outside this ban"

   :alloc/managed-element-exception
   "elements of a managed type — scene objects, view references — live in a managed collection allocated once when their owner is built and held for its whole lifetime"  ;; the exception lifts the allocation cadence ONLY: the field is still state and still carries the marked exception

   :alloc/no-per-run-garbage
   "no repeated path creates a new array or calls a sequence materializer — not one freshly allocated managed collection per run"

   :alloc/no-handed-out-snapshot
   "a system neither builds nor hands out a managed snapshot: data reaches the consumer one value at a time or by reference, because otherwise its lifetime has no name"

   :alloc/binds-the-path
   "the per-run allocation ban binds the repeated execution PATH — the loop, the body of Update, every helper and every subsystem on that path — not merely the system class"

   :alloc/not-by-type
   #{"spans"
     "enumerating the entities of an archetype, a query or an index"
     "reading a collection exposed by a config or a component"
     "native containers"
     "a marked field"
     "a managed buffer allocated once and released deterministically"}  ;; this list is what is NOT a violation — the invariant is the lifetime, not the container type

   :alloc/enum-not-a-native-key
   "an enum cannot key a native set or a native dictionary — key on its underlying integer type; as a value an enum is fine"})

(def native-allocator
  {:alloc/by-lifetime
   (cond (lives-within-one-frame? memory)    "Allocator.Temp — 1 frame"
         (lives-within-four-frames? memory)  "Allocator.TempJob — 4 frames"
         :else                               "Allocator.Persistent, or a managed collection released by one named owner")

   :alloc/no-temp-in-async
   "an async system never takes the frame-temporary allocator, and such memory never sits in a field and never lives across an await"  ;; Temp is bound to its thread, and an async system may resume off the main thread

   :alloc/temp-scope
   "the frame-temporary allocator lives either in the frame block on the main thread or in the job block inside a job — nowhere else, and in particular not in code handed to the thread pool"

   :alloc/named-owner
   "every allocation has a named owner that releases it; the invariant is deterministic release, not the type of the container"

   :alloc/breach-is-an-error
   "a breach of allocator lifetime is an error, never a recommendation"})
```

## Runtime forms

```clojure
(def reactive
  {:reactive/default
   "reactive is the default choice for runtime logic and the only reactive mechanism in the project"

   :reactive/shape
   (-> (:step-1 "a sealed subclass of an Update base")
       (:step-2 "dependencies, indexes and archetypes resolved in the constructor")
       (:step-3 "the anchor passed to base(...) is the event archetype — EventArchetypes.Of<TEvent>(store)")
       (:step-4 "the FIRST line of the Update body is the ripeness gate")
       (:step-5 "then the precondition guards, which throw")
       (:step-6 "then action on the difference only"))})

(def per-frame
  {:perframe/only-when
   "per-frame is taken only when the logic is genuinely continuous and cannot be reactive — camera movement, a per-frame projection, input polling, selection tracking; you must be able to say why a pulse cannot replace the tick"

   :perframe/shape
   "a per-frame system is a sealed subclass of an Update base whose base(...) takes the world and one work table from its archetype holder, and whose Update body leaves no field behind between frames"

   :perframe/late-update
   "the late Update base is taken when the system must see the frame's final state — after the camera and after the gameplay writes"})

(def orchestrator  ;; an orchestrator plus a DI-collected family of subsystems
  {:orchestrator/when
   "the family is taken when one base has one implementation per feature, when a stage has independently ordered parts, or when handling one event is too large for a single Update body; if it fits one body, it is an ordinary system"

   :orchestrator/base-shape
   "the subsystem base is an abstract class with the disposal contract: a protected readonly storage from the constructor, an on/off switch, an abstract priority, the abstract family operation and a virtual dispose"

   :orchestrator/concrete-shape
   "a concrete subsystem is sealed, takes EntityStorages rather than a bare world store, and resolves its own archetypes and indexes in its constructor"

   :orchestrator/dispatch
   "the orchestrator orders the family once in its constructor by priority and on a run calls only the enabled ones, in that order"

   :orchestrator/no-domain-logic
   "the orchestrator holds no domain logic — it sorts, skips the disabled and runs; all the work lives in the subsystems"

   :orchestrator/query-caches
   "shared queries and Try-helpers live in the family base, private ones in each subsystem; query caches belong to the store, so a subsystem has nothing to release there and its dispose clears only what it allocated itself"

   :orchestrator/routing
   "family routing is an attempted handling on each subsystem, first match wins, and no match throws"

   :orchestrator/empty-family
   "while there are no subsystems the orchestrator stays an empty shell with its anchor and no injected list — the container throws on an empty collection, so the list and the loop appear with the first subsystem"

   :orchestrator/ripe-once
   "the orchestrator checks ripeness once before dispatching — a subsystem never checks it again"

   ;; UpdatedSystem already snapshots for its subclasses — that is the whole reason
   :structural/update-is-safe
   "structural changes inside the Update body are safe because the base already snapshotted the anchor archetype; inside an enumeration the system or subsystem opens itself they are forbidden"})
```

## One-shot, pipeline, turn phase

```clojure
(def one-shot-forms
  {:oneshot/startup-step
   "a one-shot startup step implements the non-generic async contract IUniTaskSystem: Execute(token) plus the AppState flag Boot runs it on"

   :pipeline/stage
   "a map-creation stage is one-shot async world building: it implements IPrioritizedUniTaskSystem<MapGenerationStep> and runs once"

   :pipeline/shape
   "a stage is sealed and internal, holds a readonly EntityStorages, the store and its table archetypes, checks its own precondition on entry and throws when it is missing, throws on cancellation, gives birth to rows through an archetype, and releases its own handles in its dispose"

   :pipeline/order
   "stages run sequentially by ascending priority with an await: a stage may rely on everything the lower priorities did, and throws on a missing precondition"  ;; the map-creation band in SystemPriorities.WorldInit runs about 100 to 900 in steps of about 100

   :pipeline/re-entry
   "a re-entry guard appears only where regeneration can re-enter the stage — otherwise the stage destroys and creates anew"

   :pipeline/addressable-handle
   "a stage holds the handle of a loaded asset and releases it in its own dispose"

   :pipeline/singleton-view
   "a single view is published to its consumers as a component with the view suffix"

   :pipeline/singleton-non-queried
   "single data nobody queries lives as a single-instance component"

   :turn-phase/base
   "a turn phase derives from the turn-phase base TurnPhaseSubSystem, whose type argument TurnPhaseStep names the turn stage, and its cadence is repeated — every turn"})
```

## Entities

```clojure
(def table-rule  ;; a table is a key column plus a discriminator, declared as one archetype
  {:table/is
   "a table is a key component plus a discriminator — the main tag — together in one declared archetype"

   :table/filter
   "a query filter is the table's archetype, never a bare key component: a bare key gives the union of every table in that key space, so a non-event filter names exactly one tag"

   :table/sweep
   "sweeping a table is iterating its archetype itself — no query object"

   :table/keyed-join
   "a keyed join is a ComponentIndex over the key column, declared once in the constructor"

   :table/cross-archetype
   "a query across several archetypes is allowed only when the filter genuinely spans several archetypes and the owner has agreed to it; sweeping every event by the shared event tag is the one deliberate exception"

   :table/join-at-use
   "a join is a lookup by key value at the point of use — never a stored Entity reference from one table's row into another table's row"

   :table/index-only-hot
   "an index is created only for a hot join — read every frame or many times per turn; a click-frequency lookup scans the archetype instead"})

(def tag-law
  {:tag/is
   "a tag is an empty struct with ITag: it carries no data, its presence IS the information, and the moment a value is needed it is a component"

   :tag/one-main-tag
   "every archetype declaration carries exactly 1 main tag, written first in Tags.Get"  ;; the main tag is the table discriminator — the identity of the entity type; two main tags mean that row cannot exist

   :tag/main-tag-unique
   "one main tag names one archetype — two archetypes sharing a main tag are forbidden, and the event tag is the only exception"

   :tag/label-count
   "0-4 label tags stand beside the main tag — the main tag plus its labels must fit the type-argument cap of an archetype declaration"

   :tag/label-not-a-filter
   "a label tag is never a query filter"

   :tag/event-tag
   "every event carries EventTag as its main tag, and EventTag stands nowhere but an event archetype"

   :tag/added-by-archetype-only
   "a tag is added only by a table's archetype declaration — never to a live entity, because that carries the row out of its archetype"

   :tag/composed-by-declaration
   "the tag set is composed only inside the archetype declaration — never by adding to an already composed set"

   :tag/state-column
   "state is a column over an enum with the state suffix (…StateComponent), never a toggled tag, and it is written only on change"

   :tag/kind-column
   "kind is a column over an enum with the kind suffix (…KindComponent), never a second main tag, and it is written once at birth"

   :tag/transaction-role
   "the label role Transaction marks a transaction entity; a label with no role means membership"})
```

```clojure
(def archetype-law  ;; one static <Assembly>Archetypes holder per assembly that declares archetypes
  {:archetype/holder
   "archetypes are declared by one static holder per assembly, named after the assembly without dots and without the domain prefix; the shared kernel and the app-root are the exception"

   :archetype/reach
   "an archetype names only components its own assembly can see"

   :archetype/shape
   "each member of the holder returns the live Archetype in the method body — store.GetArchetype(ComponentTypes.Get<…>(), Tags.Get<…>()) — and the store arrives as a parameter"

   :archetype/holder-stateless
   "the archetype holder stores nothing"  ;; the system keeps the Archetype it uses in a readonly field resolved once in its constructor

   :archetype/birth
   "an entity is born by a create call ON the archetype — archetype.CreateEntity() — never by a bare store.CreateEntity() at the call site"

   :archetype/bulk
   "bulk birth is archetype.CreateEntities(n), and when the count is known EnsureCapacity(n) comes first"

   :archetype/assign-then-write
   "the new row is assigned to a local entity variable and only then written into — never a chain of writes straight off the creating call"

   :archetype/arity-cap
   "the component set and the tag set of an archetype declaration take at most 5 type arguments each"

   :archetype/no-orphan-column
   "writing a component no declared archetype names is a violation"})

(def birth-completeness
  {:birth/completeness
   "an entity is born carrying every column it will ever hold"

   :birth/sentinel
   "presence is not a predicate but a sentinel value — Unknown, Idle, an empty box"

   :birth/fixed-composition
   "the composition of a row is fixed at birth: there are no optional columns, and changing the composition means deleting the row and creating a new one in its archetype, carrying the key values over"

   :birth/no-late-structural
   "after birth there is no RemoveComponent, no RemoveTag, no late AddComponent of a column the archetype does not name, and no tag addition"})

(def singleton-state  ;; EntityStorages.Singletons — its own store, one row
  {:singleton/manifest
   "wherever single-instance components are used at all, exactly one manifest archetype declares them — SingletonArchetypes.Singleton; a component used but not declared is a violation, and one declared but not used is a violation too"

   :singleton/read
   "a single-instance component has a declared archetype, but consumers read and write it only through the single-instance registry EntityStorages.Singletons — never by a query"})
```

## Keys and indexes

```clojure
(def key-role-law  ;; the suffix carries the role: …IdComponent is identity, …FKComponent is a reference, …Component is an attribute
  {:key/pk
   "a primary key is a column with the identity suffix — the row's own identity; its owner is exactly one table paired with its tag, and uniqueness is held by contract, not by the engine"

   :key/fk
   "a foreign key is a column with the reference suffix: a reference from another table's row into the owner's key space, never the owner's own primary-key type, and the relation is one to many"

   :key/data
   "a data column is an attribute value, and it never keys the indexes of two different tables"

   :key/enum-space
   "an enum space is a key space with no primary-key table: the owner side carries the data column, the referencing side a column with the reference suffix"

   :key/self-index
   "a self-index is allowed when every row carrying that data column belongs to one table while the lookup value may come from outside; a state or kind column is a legal key for it; the same data type indexed across two tables is split into a primary-key and foreign-key pair"

   :key/pk-single-carrier
   "a primary key carried by several archetypes is a violation: the foreign carriers must move to a reference column"

   :key/fk-value-parity
   "a foreign key looked up in an index must carry the same value as the owner's key — a mismatch in the value type is a violation"

   :key/fk-home
   "a reference column lives in the owner's feature folder beside the primary key — one space, one pair of types, defined once"

   :key/pk-compare-only
   "a primary key that is only compared and never indexed stays a plain component with equality"})

(def component-index  ;; ComponentIndex — keyed on the component type across every table that carries it
  {:index/maintained-by-write
   "the index is maintained by the write call: a write re-files the row, deleting the entity drops it"

   :index/pk-uniqueness
   "a duplicate primary-key value returns both rows without an error, so uniqueness is checked at the site that allocates the key, and it throws there"

   :index/bucket-cap
   "at most 100 entities fall on one identical key value — insert and remove are linear over the duplicates"

   :index/one-fk-per-space
   "an entity holds one component per type, so it holds at most one foreign key into a key space; two references need their own pair of types and are a design decision"

   :index/key-equality
   "a key component declares IIndexedComponent<TValue> and returns the key from GetIndexedValue(): an enum works directly, a struct key implements equality and hashing itself"})
```

## Components, writes, links

```clojure
(def components-and-writes
  {:component/is
   "a component is a plain struct of runtime values with no behaviour and no methods, except equality when it is a table key; the Components/ folder holds no logic and no side effects"

   :component/declare
   "a component is declared as a struct with IComponent — a component the engine cannot see silently does nothing at birth"

   :write/upsert
   "writing a column is entity.AddComponent(value) — the upsert; under birth completeness it is always a plain value write"

   :write/no-ref-mutation
   "never mutate through a ref into component storage and never read a component as a handle to mutate — GetComponent is a read, and the index re-files the row only on the write call"

   :write/change-only
   "compare first, write on difference — rewriting the same value re-files indexed rows for nothing"

   :link/persistent-key
   "a persistent link is a domain key: the owner's identity column, referenced by that space's reference column — never a stored Entity handle"

   :link/runtime-only
   "a runtime-only link — not serialized, coupled by lifetime, usually view-layer — may hold a direct reference or an Entity handle"

   :link/miss-throws
   "a key lookup that misses throws"})
```

## Events

```clojure
(def event-lifecycle  ;; EventTag + EventFrameComponent + the event component, resolved by EventArchetypes.Of<T>(store)
  {:event/is
   "an event is a one-frame struct raised on its own entity; its fields are ordinary data — the values the consumer needs"

   :event/declare
   "an event is declared as a struct with IComponent, with the event suffix, in the Events/ folder of its feature"

   :event/suffix
   "an event type carries the event suffix; the longer legacy suffix ending in the word Component is closed — exactly one live type still carries it and no new type may"

   :event/raise
   "an event is raised by one call — store.CreateEvent(new TEvent { … }) — which stamps the frame and composes the event archetype; a pulse assembled by hand loses the tag or the stamp, never ripens, and leaks"

   :event/ripe
   "a consumer acts only while the event is ripe — the 1 frame after birth; the ripeness check is the first line of the handler, otherwise the system fires twice"

   :event/anchor
   "the consumer anchors on the event's own archetype and holds it — while no event exists it costs zero; the values come from the event component on the pulse entity"

   :event/reaction
   #{"act directly on the event's values"
     "reconcile: build the current set from world state, diff it, act on the difference, idempotently"}  ;; reconcile makes a second or coalesced event find nothing to do, and a missed event is repaired by the next one

   :event/no-same-frame
   "never count on a reaction in the same frame — a chain of events costs 1 frame per link, and visibility does not depend on priority"

   :event/dormant-consumer
   "the emitter may land later: the consumer can be built first as a dormant scaffold"

   :event/no-change-observers
   "never store.OnComponentAdded, store.OnComponentRemoved, store.OnTagsChanged or any other value-change observer — raise an event next to the write instead; adopting an observer is a decision for the whole project, never a local one"

   :event/cleanup
   "one global system cleans events up: EventCleanupSystem runs last in the tick at Priority int.MaxValue, deletes every ripe entity carrying EventTag, and has no subclasses — there is no per-event cleanup"

   :event/no-tag-on-persistent-row
   "a persistent data entity never carries EventTag"

   :event/one-way
   "never a fill - command - fill loop on one-frame events: data flows one way"

   :event/lossy-producer
   "a producer that does not control the frame window — a turn phase or async work — rings by level: it re-raises the event every turn or tick while the condition holds, and the consumer reconciles against state instead of trusting one delivery"

   :event/startup-bulk
   "startup bulk work is a pipeline stage, never an event: one-frame events do not survive the async map-creation pipeline"})
```

## Transaction entity

```clojure
(def transaction
  {:transaction/when
   "a transaction entity is taken when there are two or more subdomains AND the behaviour is multi-step — between the opening and the committing pulse the result can still change, so there is session state somebody must own; two subdomains and one pulse is a reactive system"

   :transaction/degenerate
   "when there is nothing to own between opening and commit — no resource spent, no preview on the map, the choice is pure interface state — the commit happens straight on the confirming pulse, with no draft stages"

   :transaction/one-home
   "multi-step behaviour across subdomains has exactly one home: a transaction entity in the verb domain, which owns both the entity and every write into it — never a substrate domain and never presentation"

   :transaction/one-archetype
   "one archetype shape for the whole session, with all its columns including the stage column: the main tag never changes, the Transaction label stands beside it, and a stage with a different composition means deleting the row and creating a new one in its archetype, carrying the keys over"

   :transaction/lifecycle
   (-> (:open   "a reactive system of the verb domain creates the row through its archetype and writes identification, session state and stage")
       (:change "a command event carrying the command's values, consumed only by a reactive system of the same domain")
       (:commit "a write of the stage column")
       (:finish "a FACT row in the substrate domain's table plus an event; consumers read the fact table"))

   :transaction/single-entity-state
   "all of the transaction's state lives on ONE entity: no copy in a single-instance component, no second home, and no one logical state duplicated across subdomains and synchronized by events"

   :transaction/no-substrate-state
   "never put state into a substrate domain so that another layer can read it; pure selection state in the interface stays in presentation"

   :transaction/ui-projection
   "the interface is a projection: its systems read the transaction entity directly and reconcile idempotently, player input travels as command pulses and never as writes, a view feeds its system a local event and raises no pulse itself, and the interface holds no transaction state — it shows facts or the live entity, never a snapshot copy"})
```

## View boundary

```clojure
(def view-layer  ;; everything declared in a Views/ folder; Views/ exists only in presentation
  {:view/layer
   "the view layer is everything declared in the Views/ folder; the partner of a view system — the type that raises the local event and that the subscriber marker names — additionally derives from MonoBehaviour"

   :view/boundary
   "a view never creates an entity, never raises an ECS event and never receives EntityStorages or EntityStore — not as a field, not as a constructor parameter, not as a parameter of an injection or construction method"  ;; no compile error today; the graph names the deviation

   :view/inward
   "inward: the view raises a C# event and its driving system or subsystem subscribes to it directly — the view knows no store"

   :view/outward
   "outward: the system pushes ONE value at a time into the view — never a list or an array built inside the system"

   :view/ecs-only-across-a-boundary
   "an ECS pulse between a view and a system is right only when the signal crosses a frame boundary or an assembly boundary a direct call cannot reach, and the SYSTEM raises it"

   :view/subscribe-once
   "a system subscribes to a view once, under a guard — the view outlives the subscription — and unsubscribes in its dispose"

   :view/handler-synchronous
   "a view's event handler runs synchronously in the interface callback on the main thread: first the store write, then the push back into the view — never a write deferred to the next tick by a flag"

   :view/shape
   "a view is a sealed MonoBehaviour with public C# events raised on interaction and setter methods that bind interface elements"

   :view/subscription-visible
   "every subscription is visible in the subscriber — a search by the event's name finds all its listeners"

   :view/no-business-logic
   "the Views/ folder holds no business logic, and the folder itself exists only in presentation"})
```

## Configs and addressables

```clojure
(def configs  ;; a config is a ScriptableObject asset living in EntityStorages under its own type
  {:config/is
   "a config is an authored data asset that lives in the storage registry under its own type; a read throws when the config is not loaded"

   :config/shape
   "a config type is a sealed ScriptableObject with the create-asset attribute, private serialized fields and read-only properties"

   :config/validate
   "a config type implements IValidatableConfig when the authored data can be wrong; Validate throws on every authoring violation, is called immediately after loading and lives in the asset, never in a system, and the consumer checks again"

   :config/container
   "a catalogue container is a data asset holding an array of sub-configs of the same abstract type"

   :config/key-is-the-type
   "the key of the config storage is the type: one stored instance per type, and two assets of the same shape demand an abstract base and a separate sealed subclass for each"

   :config/read-the-asset
   "the asset itself is read — no copy, no flattening, no presence guard and no silent skip around the read"

   :config/lifetime
   "a config is loaded once at the config-loading step and never released: the loader keeps only the value of the handle and never disposes the handle itself"

   :config/immutable
   "a config is never mutated and its arrays are shared — read only, with no defensive cloning"

   :config/derived-objects
   "objects built from a config belong to the instance-objects step: that step may build runtime single-instance components and objects, but may not load assets and runs no gameplay or per-frame logic"

   :config/off-thread-read
   "reading config fields off the main thread is allowed — they are plain data"

   :config/loader
   "the config loader is only the generic loading system ConfigLoaderSystem<T>: it loads the asset, throws on failure, validates, puts it into the storage and never releases; never a hand-written loader, never a subclass per config, never a config component and never an entity table for querying configs"

   :config/address-constant
   "an addressable address is a constant in ConfigAddresses, SCREAMING_SNAKE_CASE, named after the config type, whose value equals the entry name authored in Unity; the constant is passed, never a string literal and never a type name"

   :config/instance-objects-step
   "the derived-objects system is a sealed one-shot step that takes its AppState flag in the constructor, reads the config and writes the derived single-instance component"})
```

```clojure
(def addressables  ;; IAddressable is injected by constructor; Box<T> owns the handle, Result<T> carries Box plus Status
  {:addressable/ctor-injection
   "the loading contract is injected through the constructor only, and the package's own API is not touched outside its implementation"

   :addressable/no-prefab-loadasync
   "a prefab is loaded only by the call that loads and instantiates; the generic load of a scene object is guarded and returns a failure"

   :addressable/instance-release
   "releasing a loaded instance destroys the scene object itself — never destroy it by hand"

   :addressable/load-failure
   "a failed load of a non-scene asset throws with the address in the message; a successful handle is held, and on teardown it is released and reset to empty"

   :addressable/missing-component
   "a prefab without the required component releases the box and throws; a typed component is returned in a wrapping box whose disposal cascades"

   :addressable/cancel-throws
   "cancellation is an exception, not a status: the box never reaches the caller, the token is thrown on after the method's own further awaits, and held boxes are released in the finally block"

   :addressable/box-decides-release
   "the box decides the release, not the status: if something is inside, exactly one owner releases it, otherwise it leaks"

   :addressable/empty-box
   "a failed status means an empty box, and releasing an empty or default box does nothing — an unconditional release in the finally block is safe"

   :addressable/one-owner
   "a box has exactly one owner, and a released box field is replaced with an empty one"

   :addressable/box-field-disposable
   "a class that stores a box field implements the disposal contract or otherwise routes the release"

   :addressable/field-release
   "field release runs under a disposed guard, through a helper that releases the box and resets it to empty"

   :addressable/sequential-rollback
   "sequential loads run with rollback: local empty boxes, the loads inside a guarded block, a commit into the fields, the locals reset, and in the finally block every local released"

   :addressable/no-detached-value
   "the value from a box is never stored apart from it — a released box leaves the stored value dangling"

   :addressable/check-before-read
   "the value is read only after checking that the box holds one"})
```

## Threading and structural change

```clojure
(def store-thread  ;; the main thread is the store's boundary
  {:thread/main-only
   "every store call is main-thread only: create, write, read, delete, index lookup, raising an event"

   :thread/off-thread-plain-data
   "off the main thread only computation over plain data and native containers, and reading config fields counts as plain data"

   :thread/bridge
   "before the first store call switch to the main thread, and further computation goes back to the thread pool; the hop to the main thread resumes at the next Update, so it costs about one frame"  ;; await UniTask.SwitchToMainThread() before the first store call, UniTask.SwitchToThreadPool() for more computation

   :thread/no-off-thread-write
   "never a store write off the main thread — an indexed column re-files its row on write, so it corrupts the index"})

(def structural-change  ;; AddComponent, RemoveComponent, AddTag, RemoveTag — birth by an archetype is none of them
  {:structural/enumeration-throws
   "a structural change inside an enumeration throws — so does adding a column the archetype already names, and so does changing an unrelated entity; the guard is store-wide"

   :structural/delete
   "deleting an entity is not a structural change, but it does mutate the set being enumerated"

   :structural/idiom
   (-> (:step-1 "collect entity.Id into a NativeList<int>")
       (:step-2 "close the enumeration")
       (:step-3 "re-fetch each id via store.TryGetEntityById")
       (:step-4 "create and write in that second pass"))

   :structural/no-entity-snapshot
   "never a snapshot of Entity values — an Entity handle is not an unmanaged type"})
```

## Naming

```clojure
(def naming
  {:name/suffix
   {:data      "…Component — it carries values"
    :tag       "…Tag — a field-less marker"
    :event     "…Event — a one-frame pulse"
    :reference "the owner key's name with the reference mark before the word Component — …FKComponent"}

   :name/self-sufficient
   "a type name is clear WITHOUT its namespace, the feature name is always repeated and a bare role name is never left standing, and disambiguation through a using alias or a namespace qualifier does not happen"  ;; C# Framework Design Guidelines, not Go: <Feature>CostConfig, never a bare Config

   :name/prefix-exception
   "the domain prefix is stripped, and the only exception is an identity component or a table discriminator referenced FROM other domains"  ;; a domain installer keeps its prefix, but the form of an installer belongs to DI and is not a code rule

   :name/static
   "a static class is only for a stateless utility with no fields"})
```

## Methods, failure, comments

```clojure
(def output-methods  ;; QueryResultExtensions is the shared kernel's Try-helper
  {:method/try
   "a method that can fail returns a success flag and hands its output back through a separate parameter, and the caller branches on that flag"

   :method/return-value
   "one certain result is returned as the value"

   :method/no-inferred-success
   "never infer success from the result — not from a null reference, not from a zero count, not from a zero length"

   :method/collection-by-ref
   "a collection the method writes into is passed by ref, never by value"

   :method/try-get-first
   "looking up a single row goes through TryGetFirst on a query, an archetype or an Entities set"

   :method/inline-helper
   "a single-use private helper that reads fields and unfolds the caller's linear flow stays inline: only a method that is signature-complete and reused is extracted"})

(def fail-loud
  {:fail/throw
   "a step that cannot do its job throws a descriptive exception naming what is missing"

   :fail/no-silence
   "never a silent return on a missing precondition and never a log warning with the work skipped"

   :fail/silence-boundary
   "the boundary of loud failure: a missing handler throws when work was LOST through it — a row did not appear, an asset did not load; a silent skip is legal only where nothing was asked and nothing is owed, and the recipe itself names that absence a valid state"

   :fail/cancel
   "cancellation is observed by throwing on the token, on its own line after every await — never together with a validity check and never as a quiet return"

   :fail/cleanup
   "what cancelled work owns is released in the finally block, and partial work is cleaned up before throwing"

   :fail/exception-type
   "the default exception is InvalidOperationException with a message"

   :async/token
   "a long async task takes its token from the application status monitor StatusMonitor"})

(def comments
  {:comment/on-the-thing
   "a comment is short, sits ON the thing itself — the class header, the method — and is updated in the same diff: intent, non-obvious invariants, contracts, and only where the logic stops being simple and unambiguous"

   :comment/never
   #{"a comment about ANOTHER file — link by name only, or move the fact to its owner"
     "boilerplate XML documentation"}})
```

## Known deviations

```clojure
(def deviations
  {:deviation/in-code "a known deviation is recorded as a code comment at the deviation site"
   :never             "a list of deviations in this document"})
```
