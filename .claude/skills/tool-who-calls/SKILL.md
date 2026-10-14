---
name: tool-who-calls
description: "Who calls or references a C# symbol, what implements an interface, what derives from a type — ask roslyn (find_references / find_callers / find_implementations / get_type_hierarchy), never grep: grep misses overloads and partial names, and matches comments and XML-doc crefs. Blind to generic ECS/DI wiring — that is tool-who-writes-component / tool-di-wiring."
user-invocable: false
---

# Who calls this symbol

```clojure
{:tool-skill "tool-who-calls"
 :question "who calls, references, implements or derives from this C# symbol"
 :order "call roslyn over FantasyMayor.sln: mcp__roslyn__find_references (any use), find_callers (call sites of a method), find_implementations (an interface or abstract member), get_type_hierarchy (bases and descendants)"
 :because "roslyn resolves the symbol, so overloads, aliases and same-named members in other namespaces stay apart; grep matches comments, crefs and substrings"
 :invoke "mcp__roslyn__find_references {:solutionPath \"<repo>/FantasyMayor.sln\" :sourceFile \"<abs path of the declaring file>\" :symbolName \"<Name>\"}"
 :example {:call "find_references EventLog in Assets/Scripts/EcsExtensions/EventLog.cs"
           :returned "9 refs — EventReader._log, EntityStorages.Events, the constructor, 5 crefs in doc comments"}
 :blind-spots #{"a type named only inside generic arguments — AddComponent<T>, EventReader<TEvent>, Register<T>, IReadOnlyList<T> — roslyn lists the text reference but not the ECS/DI meaning: tool-who-writes-component, tool-di-wiring"
                "symbols just added in the same edit batch can be stale in the roslyn workspace — expected, not a finding"
                "Unity-side references (prefabs, scenes, .asset) — tool-where-asset-used"}
 :fallback "a targeted grep over Assets/ with the reason in the finding's :verified-by (e.g. the symbol lives outside FantasyMayor.sln)"}
```
