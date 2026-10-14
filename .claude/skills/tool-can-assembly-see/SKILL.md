---
name: tool-can-assembly-see
description: "Can one assembly compile against a type, which assembly defines a type, by what reference path two assemblies connect — ask Tools/asmdef_reach.py (can / assembly-of / path / refs), never read .asmdef files by hand; run it before proposing code that uses a type across an asmdef boundary."
user-invocable: false
---

# Can this assembly see that type

```clojure
{:tool-skill "tool-can-assembly-see"
 :question "will this assembly compile against that type, and where does the type live"
 :order "run Tools/asmdef_reach.py: can <Assembly> <Type>; assembly-of <Type>; path <AsmA> <AsmB>; refs <Assembly>"
 :because "asmdef references resolve by name and GUID and only DIRECT references compile — neither roslyn nor fmgraph answers it, and reading the JSON by hand misses GUID refs"
 :invoke "python3 Tools/asmdef_reach.py can <Assembly> <Type>"
 :example {:call "asmdef_reach.py can Presentation EventReader"
           :returned "YES — Presentation compiles against EventReader (in Ecs.Extensions) — direct reference"}
 :blind-spots #{"whether a new reference is ALLOWED by the layering rules — that is fantasymayor-placement"
                "package assemblies outside Assets/ are not parsed"}
 :fallback "read the one .asmdef the tool names, with the reason in the finding"}
```
