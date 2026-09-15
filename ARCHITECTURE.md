---
category: C
read: trigger
trigger: before any engineering task — request routing puts it into :read
tags: [architecture, ecs, conventions]
related:
  - "[DOC_STANDARD](DOC_STANDARD.md)"
---

# FantasyMayor — Architecture Reference

Runtime laws of FantasyMayor code: systems, entities, events, threading, code shape.

## Stack
- Engine: Unity · ECS: `Friflo.Engine.ECS` 3.6 (DoD style, not Unity DOTS) · DI: `VContainer` · Async: `UniTask`
- Assets: `Addressables` · Input: `InputSystem` · Rendering: URP · UI: `UI Toolkit` (UXML/USS) + `Unity App UI` (`com.unity.dt.app-ui`)

## Systems
```clojure
(def decomposition
  {:split-when #{"the system both creates and destroys the same kind of content"
                 "it diffs world state every frame to find out what changed"
                 "it holds more than two unrelated query families"
                 "it runs in several game states for different reasons"}
   :split-into {:startup-bulk   "a one-shot pipeline stage or subsystem inside map creation"
                :runtime-duty   "one reactive system per responsibility"
                :shared-compute "a stateless helper in Helpers/"}})

(def reactive
  {:is         "the default for runtime logic and the only reactive mechanism in the project"
   :event      {:is "a one-frame event; its component is ordinary data — the values the consumer needs"}
   :consumer   {:anchors-on "the event's own archetype — zero cost while no event exists" :gate "IsRipe"}
   :reaction   #{"act on the event component's values directly"
                 "reconcile: build the current set from world state, diff it, act on the difference"}
   :reconcile-gives "a second or coalesced event finds nothing to do; a missed event is repaired by the next one"
   :timing     "never assume a same-frame reaction — a chain of events costs one frame per link"
   :emitters   "may land later: the consumer can be built first as a dormant scaffold"
   :per-frame  {:only-when "the logic is genuinely continuous and cannot be reactive"}
   :component-change-observers {:never #{"store.OnComponentAdded" "store.OnComponentRemoved" "store.OnTagsChanged" "any value-change observer"}
                                :instead "raise an event next to the write"
                                :adopting-one "a design decision for the whole project, never a local one"}})

(def stateless-systems
  {:rule         "a system holds no mutable per-instance state"
   :not-state    #{"readonly DI dependencies and store handles"
                   "query caches — Archetype, ArchetypeQuery, ComponentIndex — resolved once in the constructor"
                   "const and static readonly values"}
   :state        #{"any reassignable field"
                   "a readonly field whose contents change across frames: collections, arrays, StringBuilder, native buffers held between ticks"}
   :default-home "a status flag, an in-flight marker, a progress counter, a running or completed bit, a handle to what is being processed — domain state on an entity, or single-instance state outside the system"
   :escape       {:order [:component FrameBox StateAllowedAttribute]
                  :component "move the state onto an entity or into single-instance state — always tried first"
                  FrameBox "state valid only for a bounded number of frames"
                  StateAllowedAttribute "a reviewed exception, always with a reason"}
   :checked-by   "/arch-check"})

(def system-collections
  (cond
    (monobehaviour-view? code)
    {:collections "System.Collections.Generic allowed — a view is not a system"}

    (runs-once-or-a-few-times? system)
    {:examples    #{"startup step" "config loader" "map-creation pipeline stage or subsystem"}
     :collections "System.Collections.Generic allowed"}

    (runs-repeatedly? system)
    {:examples    #{"per-frame system" "reactive system — every event" "turn phase — every turn"}
     :rule        "zero-allocation: memory claimed at the start of a run is released at its end; nothing is left for the GC"
     :collections "Unity.Collections native containers, structs, spans"
     :never       "a managed collection or array built per run"
     :except      "elements of a managed type (GameObject, view reference) — a System.Collections.Generic collection allocated once and held"
     :enum-key    "an enum cannot key a NativeHashSet or NativeParallelHashMap — key on its underlying int; as a value an enum is fine"}))

(def native-allocator
  {:lifetime (cond (lives-within-one-frame? memory)   Allocator.Temp
                   (lives-within-four-frames? memory) Allocator.TempJob
                   :else                              "Allocator.Persistent, or a System.Collections.Generic collection — released by one named owner")
   :async    {:never "Allocator.Temp — it is bound to its thread, and an async system may continue off the main thread"}
   :temp-on  (cond (main-thread?)  "the frame's Temp block, rewound at the end of the frame"
                   (inside-a-job?) "the job's Temp block, rewound at the end of the job"
                   :else           :never)
   :breach   "an error, never a recommendation — /arch-check reports what it can see statically"})
```

## Entities
```clojure
(def table-rule
  {:table      "a key component + a discriminator (tag-law :discriminator), together in one declared archetype"
   :filter     {:requires "the table's archetype" :never "a bare key component — a union of every table sharing that key space"}
   :sweep      "iterate the table's archetype itself — no query object"
   :keyed-join "a ComponentIndex over the key column, declared once in the constructor"
   :cross-archetype-query {:only-when "the filter genuinely spans several archetypes" :requires "reviewed with the owner, never introduced unilaterally"}
   :join       {:is "a lookup by key value at the point of use" :never "a stored Entity reference from one table's row to another table's row"}
   :index      {:only-when "a hot join — read every frame or many times per turn" :never "an index for a click-frequency lookup — scan the archetype"}})

(def tag-law
  {:archetype       {:requires "exactly one tag in every archetype declaration" :never "two identity tags — that row cannot exist"}
   :discriminator   (cond (category-tag? tag) "the tag is a kind marker shared by several tables; the …ViewComponent discriminates the table"
                          :else               "the tag is the table discriminator")
   :event-archetype {:exempt "every event carries EventTag; its archetype is named by the event component"}
   :state           {:is "…StateComponent wrapping an enum" :never "a toggled tag" :write "flipped through AddComponent, change-only"}
   :kind            {:is "…KindComponent wrapping an enum" :never "a second tag" :write "set once at birth"}
   :enum-columns    "a state or kind column is a legal self-index key: IIndexedComponent<TEnum> over the family's own rows, read as index[value]"})

(def key-role-law
  {:pk         {:type "…IdComponent" :is "the row's own identity" :owner "exactly one table, paired with its tag" :index "unique by contract — the engine does not enforce it"}
   :fk         {:type "…FKComponent" :is "a reference from another table's row into the owner's key space" :never "the owner's PK type" :index "1:N"}
   :data       {:type "…Component" :is "an attribute value" :never "keying the indexes of two different tables"}
   :enum-space {:is "a key space with no PK table" :owner-side :data :referencing-side :fk}
   :self-index {:allowed-when "every row carrying the Data component belongs to one table; the lookup value may come from outside"
                :never "the same Data type indexed across two tables — split it into a PK + FK pair"}})

(def component-index
  {:keyed-on         "the component type, across every table that carries it — the key type carries the role, the tag carries the table"
   :maintained-by    "the write call: AddComponent re-files the row, deleting the entity drops it"
   :pk-uniqueness    "a duplicate key returns both rows without an error — check at the allocation site and throw there"
   :bucket-cap       "at most 100 entities per identical key value — insert and remove are O(N) over the duplicates"
   :one-fk-per-space "an entity holds one component per type, so at most one FK into a key space; two references need their own pair of FK types — a design decision"
   :key-equality     "the component declares IIndexedComponent<TValue> and returns the key from GetIndexedValue(); an enum works directly, a struct key implements IEquatable"})

(def archetype-law
  {:home      {:is "one static <Assembly>Archetypes holder per assembly that declares archetypes"
               :assembly "the asmdef name without dots and without the Domains. prefix"
               :exempt ["shared kernel" "app-root"]}
   :reach     "an archetype names only components its assembly can reference"
   :shape     "each member returns the live Archetype: store.GetArchetype(ComponentTypes.Get<…>(), Tags.Get<…>()) in the method body; the store arrives as a parameter"
   :state     "the holder stores nothing"
   :owner     "a system keeps the Archetype it uses in a readonly field, resolved once in the constructor"
   :birth     {:is "archetype.CreateEntity() — the row is born with every column at default" :never "a bare store.CreateEntity() at a call site"}
   :bulk      "archetype.CreateEntities(n); EnsureCapacity(n) first when the count is known"
   :values    "assign the new row to a local entity variable, then write — never chain writes off the creating call"
   :arity-cap "ComponentTypes.Get and Tags.Get take at most 5 type arguments"})

(def birth-completeness
  {:rule        "an entity is born carrying every column it will ever hold"
   :presence    {:never "a predicate" :instead "a sentinel value — Unknown, Idle, an empty box"}
   :composition {:never "an optional column" :change "delete the row and create a new one in its archetype, carrying the PK/FK value over"}
   :never       #{"RemoveComponent" "RemoveTag" "a late AddComponent of a column the archetype does not name — it migrates the row out of its archetype"}})

(def component-writes
  {:write       "entity.AddComponent(value) — the upsert; under birth completeness always a plain value write"
   :never       #{"mutating through a ref into component storage — an index re-files only on the write call"
                  "treating GetComponent<T>() as a handle to mutate"}
   :change-only "compare first, write on difference — rewriting the same value re-files indexed rows for nothing"})

(def links
  {:persistent   {:is "a stable domain key: the owner's …IdComponent, referenced as that space's …FKComponent" :never "a stored Entity handle"}
   :runtime-only {:is "non-serialized, lifetime-coupled, usually view-layer" :may "hold a direct reference or an Entity handle"}
   :resolution   "a key lookup that misses throws"})
```

## Events
```clojure
(def event-lifecycle
  {:archetype  "EventTag + EventFrameComponent + the event component, resolved by EventArchetypes.Of<T>(store)"
   :raise      "store.CreateEvent(new TEvent { … }) — stamps the frame inside the creating call"
   :ripe       "a consumer acts only while IsRipe(entity) — the frame after birth"
   :delivery   "every consumer exactly once, independent of system priority"
   :cleanup    "EventCleanupSystem, Priority int.MaxValue, deletes ripe events at the end of that frame"
   :priorities {:order "execution only" :never "a rule that a consumer must sit above or below its producer"}})
```

## Threading
```clojure
(def store-thread
  {:main-thread-only "every store call — create, AddComponent, GetComponent, delete, index lookups, raising events"
   :off-thread       "computation over plain data and native containers only; reading ScriptableObject config fields counts as plain data"
   :bridge           "await UniTask.SwitchToMainThread() before the first store call; SwitchToThreadPool for more computation"
   :bridge-cost      "a hop to the main thread resumes at the next Update — about one frame"
   :never            "an off-thread store write — an indexed column re-files its row on write, so it corrupts the index"})

(def structural-change
  {:is      #{AddComponent RemoveComponent AddTag RemoveTag}
   :is-not  "birth by an archetype — archetype.CreateEntity() migrates nothing"
   :throws  "inside an enumeration — also for an AddComponent into a column the archetype already names, and for an unrelated entity; the guard is store-wide"
   :delete  "not a structural change, but it mutates the set being enumerated"
   :idiom   (-> "collect entity.Id into a NativeList<int>"
                "close the enumeration"
                "re-fetch each id via store.TryGetEntityById"
                "spawn and write in that second pass")
   :never   "snapshotting Entity values — an Entity is not unmanaged"
   :base    "UpdatedSystem already snapshots for its subclasses"})
```

## Code shape

```clojure
(def code-shape
  {:structure "the simplest structure that solves the task"
   :pattern-only-when "cardinality or real complexity demands it — never because a doc or a code comment mentions it"
   :examples #{"a one-element set needs no snapshot"
               "a one-line tag swap needs no subsystem family"}
   :comments {:home "a short comment ON the thing itself (class header / method), updated in the same diff — intent, non-obvious invariants, contracts"
              :only-where "the logic stops being simple and unambiguous"
              :never #{"a comment about ANOTHER file — link by name only, or move the fact to its owner"
                       "boilerplate XML documentation"}}})

(def naming
  {:suffix            {:data "…Component" :tag "…Tag" :event "…Event" :fk "…FKComponent — the owner key's name with FK before Component"}
   :type-name         {:rule "clear without the namespace — C# Framework Design Guidelines"
                       :repeat "the feature name, always: <Feature>CostConfig, never a bare Config"
                       :strip "only a pure domain prefix that adds no clarity"
                       :never "disambiguation through a using alias or a namespace qualifier"}
   :prefix-exceptions #{"an FK/PK identity component or a table discriminator referenced across domains" "a DI installer — <Domain>Installer"}
   :static            {:only-when "the type is a stateless utility with no fields"}})

(def output-methods
  {:may-fail   {:shape "bool Try…(in inputs, out or ref outputs)" :caller "branches on the bool"}
   :sure-single-output "return the value"
   :never      #{"inferring success from the output — null, Count == 0, Length == 0"
                 "passing a collection the method writes by value — pass it by ref"}
   :inline     "a single-use private helper that reads fields and unfolds the caller's linear flow stays inline; extract only a signature-complete method that is reused or self-contained"})

(def fail-loud
  {:rule      "a step that cannot do its job throws a descriptive exception naming what is missing"
   :never     #{"a silent return or return UniTask.CompletedTask on a missing prerequisite" "Debug.LogWarning and skip"}
   :validate  "at the source (IValidatableConfig.Validate) and again at the consumer"
   :cancel    {:signal "cancellationToken.ThrowIfCancellationRequested() on its own line" :never "a quiet return"}
   :cleanup   #{"release what cancelled work owns in try/finally" "clean up partial work before throwing"}
   :exception "InvalidOperationException with a message by default"})
```

## Known deviations
```clojure
(def deviations
  {:record "a code comment at the deviation site"
   :never  "a list of deviations in this document"})
```
