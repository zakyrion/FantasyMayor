---
category: A
read: trigger
trigger: "before touching EntityStore registration, world/singleton component access, or any proposal to add a second ECS store"
tags: [ecs, storage, di, plan]
status: partial
related:
  - "[ECS_CONVENTIONS](ECS_CONVENTIONS.md)"
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[DOC_STANDARD](DOC_STANDARD.md)"
code_refs:
  types: [EntityStorages, SingletonComponents, SingletonArchetypeDefinition, SingletonArchetypes, SingletonTag, WorldInstaller]
---

# PLAN — Entity Storages

Multi-session program: split singleton state into its own store, reached through one DI-registered storage alias.

Structure follows `DOC_STANDARD.md` → Rule 2 (three stages). This is a `PLAN_` doc: it owns no lasting
cross-domain behavioral contract, so when its plan is harvested the **whole file self-deletes** and its
surviving invariants land in `ECS_CONVENTIONS.md` (Rule 2d).

---

# 1 · Request

```clojure
(def request  ;; Rule 2a
  {:preserved :no
   :why "this doc predates the verbatim-request rule (2026-08-06); the 2026-08-05 session was cleared and only the user's ANSWERS survived — they are the decision log below"
   :never "do not reconstruct the request from the answers"})
```

```clojure
(def storages-decision  ;; user, 2026-08-05 — the answers that opened this program
  {:storage-kind    :second-entity-store   ;; Q1 — a real second Friflo EntityStore holding ONE singleton row; NOT a POCO cache, NOT a facade over the game store
   :di-shape        :alias-registry        ;; Q2 — one DI-registered alias type owning EVERY store in the project; two bare store registrations would collide by type
   :call-shape      :named-storage-access  ;; Q3 — call sites name the storage they mean (storages.Singletons …), never a bare injected store
   :rollout         :staged                ;; Q4 — stage by stage, each stage compiles and plays on its own
   :order           (-> :b-separate-storage :a-declared-archetype)  ;; (б) first, (а) after — the archetype is declared on a row that already lives in its own store
   :s5-state-fields :out-of-scope})        ;; user: to be looked at separately — depends on whether Friflo has a pre/post-Update seam at all
```

```clojure
(def decisions-closed  ;; user, 2026-08-05
  {:d1 {:q "SingletonStorage as a wrapper type, or a bare second store plus extension methods?"
        :decided :wrapper
        :why "hides the row (nothing can query or populate the singleton store by accident) and absorbs a :d2 restructure without touching call sites"
        :risk-noted "the ecs-graph extractor keys on archetype declarations in holder classes, so a hidden store might drop out of the graph"}
   :d3 {:q "Where do config components (loaded once at ConfigLoadStep) live?"
        :decided :singletons                ;; NOT a third storage — the registry stays at two members
        :why "config components are written once and read by type; they join against nothing, which is exactly the singleton access shape. They are also the bulk of the world-scoped components, so keeping them here is what actually buys the per-store 256 headroom"
        :later "if configs ever need their own lifecycle (hot reload), splitting them out stays cheap — the :d1 wrapper hides which store backs them"}
   :d4 {:q "Storage naming"
        :decided "World + Singletons"
        :why "the 'world' overload dissolves inside this program: :s2 turns the injected store parameter into storages.World, and :s3 removes the World* accessor prefix and the named \"world\" row outright"}})
```

---

# 2 · Contract

## Why the program exists

Three defects follow from world state riding the game store's singleton row. They are the reason for
the split, not a description of today's code:

- **Archetype migration on first write.** The singleton row is born carrying only its unique-entity
  marker, so the first write of each world-scoped component ADDS a column and migrates the row.
- **Structural-change trap.** That add is a structural change, and the guard is store-wide — a world
  write executed inside any query loop of the game store throws. With a separate store the game
  store's guard cannot see the write at all: **the trap disappears rather than being avoided**.
- **Name lookup per read.** Every read resolves the singleton by its unique-entity name first.

Plus the ceiling recorded as an open direction in `ECS_CONVENTIONS.md`: **the 256-component /
256-tag limit is PER store.** Moving singleton state out is the first real headroom this project
gains, which is why the alias registry is built to hold N stores even though `split-law` allows two.

## Hard constraint — what may and may not be split

**A store is a closed world**: an entity in store A cannot be queried, indexed, or FK-resolved from
store B, and the component index is per-store.

```clojure
(def split-law  ;; measured 2026-08-05; re-measure with Tools/asmdef_reach.py + ecsg.py before acting
  {:safe-split    #{:singletons}   ;; singleton state is read/written by name only — nothing joins against it
   :blocked-split #{:per-domain}   ;; a per-domain Economy / Map store CANNOT exist today
   :evidence      "cross-assembly archetype reads, by assembly pair: Presentation → Domains.Map, Domains.Economy; Presentation.UI → Domains.Map, Domains.Economy, Domains.Actors, Presentation; Domains.Actions → Domains.Actors, Domains.Economy; UserInput → Domains.Map, Presentation"   ;; count deliberately unstated — it moves; the pairs are the gate
   :events        :one-store       ;; the event archetype holder is used from 5 assemblies — events stay in the shared game store, never per domain
   :fk-cost       "a per-domain split would force every cross-domain FK to resolve BY VALUE across stores instead of by row — that is a different program, not a later stage of this one"
   :verdict       "the alias registry is built to hold many stores; today it holds exactly TWO"})
```

**Consequence for naming.** `World` and `Singletons` are the only members that may exist after this
program. `Economy` / `Map` members are a **gap entry**, not a roadmap item.

## Target shape

```clojure
(def target
  {:alias-type    EntityStorages             ;; DI singleton; the ONLY thing systems inject to reach a store
   :members       {:World      "the game store — every entity row, every event, every table and index"
                   :Singletons "the singleton row — world-scoped runtime state AND every config component (:d3)"}
   :member-count  2                          ;; exactly two, by :d3 + split-law; a third member needs a decision, never a drive-by addition
   :wrapper       SingletonComponents        ;; the :d1 wrapper; shipped under THIS name, not the "SingletonStorage" the request proposed — do not "restore" the proposal's name
   :hides         #{"the second store" "the row"}   ;; nothing can query or populate the singleton store by accident
   :declaration   SingletonArchetypes        ;; the ONE place the row's complete composition is named
   :registration  "the registry is constructed before any installer that writes state"
   :injection     "systems take EntityStorages, never a bare store"
   :permitted     "a subsystem base may keep the store it was handed off the alias — that is a ctor parameter, not a second alias"
   :never         "re-registering a store in DI, or exposing one from any type other than the registry"})
```

```clojure
(def graph-visibility  ;; agent, verified 2026-08-06 — the :d1 :risk-noted did NOT materialize
  {:checked "ecsg.py search Singleton"
   :result  "the declared singleton archetype resolves as an archetype node; the extractor reads the declaration holder, so hiding the store cost the graph nothing"
   :so      "no build_graph.py fix is owed"})
```

```clojure
(def singleton-access-invariants  ;; what the wrapper must GUARANTEE — deliberately not a copy of its signatures
  {:birth-complete "the row is created from the declared archetype, carrying every column at once — no late column add, no migration"
   :write-guard    "writing an UNDECLARED component throws, naming the declaration site"   ;; fail-loud: the composition has exactly one home
   :read-guard     "reading before the first write throws — an uninitialized singleton is never silently a default value"
   :two-sets       "DECLARED composition and INITIALIZED-so-far are different sets; the presence check answers the second"})
```

## Contract closure (Rule 2b)

```clojure
(def contract-closure
  {:status :CLOSED          ;; 2026-08-06
   :d2 {:q "Birth Completeness for the singleton row: can every world-scoped column be named in ONE archetype?"
        :was-feared "the GENERIC builders cap at 5 type arguments, and the singleton carries far more columns than that"
        :answer :yes
        :how "the cap belongs to the generic overloads only — the composition is accumulated at RUNTIME into a component-types value and handed over once as a definition; that path has no arity limit"
        :consequence "step (а) IS «one declared archetype» after all; no alternative shape was needed"}
   :rule "every resolution is closed — the remaining stage executes in order"})
```

## Gaps

```clojure
(def gaps
  {:per-domain-storages {:state :blocked
                         :why "see split-law; unblocks only after the cross-assembly archetype reads it enumerates are removed"}
   :stateful-systems    {:state :out-of-scope
                         :what "35 mutable fields across 18 systems"
                         :why "waits on whether Friflo offers a pre/post-Update seam at all"}})
```

---

# 3 · Plan

Each stage is its own task map and its own `go`. A stage ends compiling and playable.
**A stage carries no status claim** — its truth is its `:accept` meter, run on demand (Rule 2).

```clojure
[{:stage :s1-alias
  :do    "introduce EntityStorages holding ONLY the existing store as World; construct and register it in WorldInstaller.Configure; nothing else changes"
  :skip  "the singleton store — :s1 does not create a second store"
  :accept {:meter "python3 ~/.claude/skills/di-graph/scripts/dig.py explain EntityStorages"
           :target "registered as a singleton by WorldInstaller"}}

 {:stage :s2-migrate-injectors
  :do    "systems inject EntityStorages and read .World; the bare store registration is removed at the END of this stage"
  :risk  "split by assembly, one assembly per commit, Domains before Presentation"
  :accept {:meter "python3 ~/.claude/skills/di-graph/scripts/dig.py consumers EntityStore"
           :target "no node matches"}}

 {:stage :s3-singleton-components
  :do    "add SingletonComponents — its own store plus one hidden row; move every world-scoped write and read onto storages.Singletons; delete the World* accessor extension class and the named \"world\" row on the game store with it"
  :scope "runtime singleton state AND config components move together — one stage, per :d3"
  :note  "the accessor move is what makes call sites say WHICH storage they mean (:call-shape)"
  :accept [{:meter "grep -rl WorldComponentExtensions Assets" :target "0 files"}
           {:meter "grep -rl GetUniqueEntity Assets --include='*.cs'"
            :target "0 files — the named row the game store resolved singletons through is gone, so no singleton state is left on it"}]}

 {:stage :s4-declared-archetype        ;; = step (а)
  :do    "declare the singleton row's archetype in a holder, born complete with every world-scoped column"
  :requires :s3
  :note  "a static type-diff is NOT available as a meter — most writes infer their type argument (Singletons.Set(new …)), so the declared-vs-written check only exists at runtime, where the wrapper's write guard performs it"
  :accept [{:meter "python3 ~/.claude/skills/ecs-graph/scripts/ecsg.py search Singleton"
            :target "the declared singleton archetype resolves as an archetype node"}
           {:meter "playtest into Gameplay"
            :target "no «is not declared in SingletonArchetypes.Singleton» throw — the write guard proves every written column was declared at birth"}]}

 {:stage :s5-docs
  :do    "ECS_CONVENTIONS → State Storage Taxonomy rewritten for two stores; ARCHITECTURE → Stack / DI composition updated (frozen — propose to the user)"
  :note  "this stage is what clears the accessor-rename fallout the :s3 landing left in the decreed docs"
  :accept {:meter "python3 Tools/doc_lint.py --quiet" :target "0 ghosts"}}]
```

```clojure
(def on-program-completion  ;; Rule 2c + 2d — what happens to THIS file
  {:harvest-to ECS_CONVENTIONS   ;; the surviving invariants: nothing but the registry creates a store; systems inject the alias, never a bare store; the 256-limit is per store
   :then       :self-delete      ;; a PLAN_ doc leaves no behavioral contract behind
   :record     "the commit log"})
```
