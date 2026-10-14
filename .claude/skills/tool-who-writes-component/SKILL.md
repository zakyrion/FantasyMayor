---
name: tool-who-writes-component
description: "Who writes, reads or removes an ECS component; who raises an event and who reacts to it; what an archetype holds; which tables key on a component — ask fmgraph (explain / neighbors / tables / spaces), never grep AddComponent or EventReader: the answer lives in generic arguments roslyn cannot classify."
user-invocable: false
---

# Who writes this component

```clojure
{:tool-skill "tool-who-writes-component"
 :question "who writes / reads / removes this component, who raises this event and who reacts to it, what this archetype holds"
 :order "run fmgraph: explain <node> for every facet and edge; neighbors <node> --rel writes,reads,removes,emits,reacts_to,polls,consumes to narrow; tables / spaces for ComponentIndex keys and PK/FK"
 :because "writes, reads and event flow sit in generic arguments (AddComponent<T>, EventReader<TEvent>, Events.Raise(new T)) and in archetype holders — fmgraph extracts them deterministically, roslyn and grep only see text"
 :invoke "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py neighbors <node> --rel emits,reacts_to,consumes"
 :example {:call "fmgraph.py neighbors NextTurnEvent --rel emits,reacts_to,consumes"
           :returned "<-emits- TurnPanelView via Events.Raise"}
 :blind-spots #{"an AddComponent(local) whose type is inferred is marked INFERRED; an ambiguous name is a warning and no edge — read check for them"
                "who CALLS a method, or who derives from a type — tool-who-calls"
                "the DI side (who injects, where it runs) — tool-di-wiring"}
 :fallback "a targeted read of the file fmgraph's source_location names; a grep only when fmgraph has no node, with that reason in the finding"}
```
