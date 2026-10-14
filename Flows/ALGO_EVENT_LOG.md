---
category: A
read: trigger
trigger: "before changing how events are raised, kept or read — the lifted algorithm of the event log"
status: implemented
tags:
  - events
  - ecs
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
---

# ALGO_EVENT_LOG.md

The algorithm of the event log, lifted from the code: one ring per event type, a cursor per reader.

## Subject

```clojure
{:lifted-from ["Assets/Scripts/EcsExtensions/EventLog.cs"
               "Assets/Scripts/EcsExtensions/EventRing.cs"
               "Assets/Scripts/EcsExtensions/EventRingSlot.cs"]
 :mode :clean
 :at "2026-09-18"
 :commit "2878d8c"
 :revised "2026-09-23 — global sequence removed (rule :event/sequence retired), cursor release added (rule :event/reader-release)"}
```

## s1

```clojure
(def EventLog
  {:makes     "every reader taken through the events of its type in the order they were raised, each event once"
   :criterion "a reader's next event is the oldest one of its type it has not taken yet; an event pushed out of its ring by a newer one is handed to nobody"

   :data {:rings   {:type EventRing
                    :holds "one ring per event type — its live slots from oldest to newest, how many of its events were ever raised, and the archetype its events are born in"
                    :from "born the first time that type is touched, as long as the capacity the type declares"}
          :slot    {:type EventRingSlot
                    :holds "the id of one living event entity"
                    :from "written when the event's entity is born"}
          :events  {:type EntityStore
                    :holds "every event alive in the log, one entity per event, carrying its payload"
                    :from "one store the log owns from birth"}
          :cursors {:type long
                    :holds "one offset per reader — how many events of its type that reader has already taken; a row a disposed reader released is marked closed and waits for the next reader"
                    :from "a row opened when a reader is opened — a released row first, a new one otherwise"}}

   :flow (-> (:step-1 "give the event type its ring — the one it already has, or a new one as long as the capacity it declares"
                      {:reads #{:rings} :writes #{:rings} :state "the type has a ring of its own: an empty circle of its capacity, and the archetype its events are born in"})

             (:step-2 "open a reader's own place in the queue of its type, at «nothing taken yet» — a row some disposed reader released, or a new one"
                      {:reads #{:rings} :writes #{:cursors} :state "the reader owns a row standing at offset zero"
                       :note "offset zero is not «the oldest present» — the first read pulls the row forward to whatever is still alive"})

             (:step-3 "make room for the event being raised — a full ring loses its oldest event, deleted and dropped from the circle"
                      {:reads #{:rings} :writes #{:rings :events} :state "the ring has one free place, and whoever had not taken the dropped event never will"})

             (:step-4 "bring the raised payload into the log as a living entity of its type, named by a slot"
                      {:reads #{} :writes #{:events :slot} :state "the event exists as an entity carrying the payload the caller handed in" :world :read})

             (:step-5 "place the slot as the newest of its ring and count the raise"
                      {:reads #{:slot} :writes #{:rings} :state "the event is the newest of its type, and the count of all ever raised of that type is the offset the next one will take"})

             (:step-6 "pull a reader that fell behind forward to the oldest event of its type still alive"
                      {:reads #{:rings :cursors} :writes #{:cursors} :state "the reader's offset stands on an event that is still alive, or past the newest one"})

             (:step-7 (cond
                        (:flow-1 "while the reader's offset stands short of all ever raised of its type")
                        (:conclusion-1 "the event at that offset is read off its entity, handed over, the offset moves one on — and back here for the next read")

                        (:flow-2 "otherwise")
                        (:conclusion-2 "nothing is handed over and the reader is told there is nothing left"))
                      {:reads #{:rings :events :cursors} :writes #{:cursors} :state "the reader holds one event and stands just past it, or holds nothing and stands at the end of its type" :world :write})

             (:step-8 "empty the whole log — every ring drops its oldest over and over until nothing of any type is alive"
                      {:reads #{:rings} :writes #{:rings :events} :state "no event of any type is alive, while the ever-raised counts stand, so every reader meets the end on its next read"})

             (:step-9 "a disposed reader hands its row back — marked closed, kept for the next reader; a second release or a read through it throws"
                      {:reads #{:cursors} :writes #{:cursors} :state "the row belongs to no reader until the next one opens"}))

   :exits #{"the store's schema does not know the event type as a component — the log throws, and that type is neither raised, opened nor read"
            "a reader released twice, or read after its release — the log throws"}

   :numbers {:capacity        "the ring capacity the type declares, and 128 when it declares none"
             :oldest-offset   "all ever raised of the type, minus how many of them are alive now"
             :place-in-circle "the head of the circle plus the offset's distance from the oldest, wrapped by the capacity"}})
```

## Flags

```clojure
[{:flag :f-1
  :kind :dangling-tail
  :where "EventLog.cs — ClearAllEvents"
  :what "nothing in the project calls it; its only other mention is the comment above the eviction helper"
  :touches "empty the whole log — every ring drops its oldest over and over until nothing of any type is alive"
  :status :by-rule
  :resolution "intended: rule :event/clear-all leaves the caller to whichever task needs it"}

 {:flag :f-2
  :kind :bug
  :where "EventLog.cs — _cursors, OpenCursor"
  :what "a cursor row is opened and never released, so the list grows for the life of the process with every reader ever built, while readers are Transient"
  :touches "open a reader's own place in the queue of its type, at «nothing taken yet»"
  :status :resolved
  :resolution "2026-09-23: EventReader is IDisposable, Dispose releases the row through EventLog.CloseCursor and OpenCursor reuses it (rule :event/reader-release); VContainer never disposes a Transient, so the owner disposes its reader — today no owner dies before exit (one root scope), so no owner calls it yet"}

 {:flag :f-3
  :kind :inaccuracy
  :where "EventLog.cs — TryRead, OpenCursor"
  :what "the doc says the cursor's next unread event «of its type», but the row carries no type — the type comes from whoever names it at the read, and only the reader's own habit keeps the pair together"
  :touches "open a reader's own place in the queue of its type, at «nothing taken yet»"
  :status :open}

 {:flag :f-4
  :kind :inaccuracy
  :where "EventLog.cs — Raise doc comment"
  :what "it promises «the next global sequence number», while each type counts its own raises and no number spans the types"
  :touches "place the slot as the newest of its ring and count the raise"
  :status :resolved
  :resolution "2026-09-23: comment says the type counts one more raise; rule :event/sequence retired"}

 {:flag :f-5
  :kind :inaccuracy
  :where "EventRingSlot.cs — type doc comment"
  :what "it says the entity id is «paired with the global sequence number», while the struct holds the id alone"
  :touches "bring the raised payload into the log as a living entity of its type, named by a slot"
  :status :resolved
  :resolution "2026-09-23: comment names the entity id alone"}

 {:flag :f-6
  :kind :unclear
  :where "EventLog.cs — constructor, the array sized by the schema's component count"
  :what "the array is sized once at birth and indexed later by a type's schema index; whether the schema can still learn component types after that cannot be told from this code"
  :touches "give the event type its ring — the one it already has, or a new one as long as the capacity it declares"
  :status :resolved
  :resolution "Friflo builds its schema once, with every component type registered at startup (Friflo docs, Native AOT registration) — sizing the array at birth is safe"}]
```
