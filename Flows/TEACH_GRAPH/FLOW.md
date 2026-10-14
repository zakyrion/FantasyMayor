---
category: A
read: always
status: partial
tags:
  - tools
  - fmgraph
related:
  - "[BUILD_UML](../BUILD_UML/FLOW.md)"
---

Teach fmgraph the facts this session kept grepping for, and stop subagents grepping C# by default.

# Request

```clojure
{:source :user
 :received-at "2026-09-26"
 :raw-request ["Ок, давай вчити граф. І для субагентів теж додай фікс"]}
```

# Confirmed contract

```clojure
{:task :teach-graph
 :kind :mutation
 :path :direct
 :goal "вчити граф + фікс для субагентів"
 :do [{:graph "members <Type> / member <Name> — properties, fields, methods of every type"}
      {:graph "explain on an enum lists its values"}
      {:subagents "PreToolUse hook: grep/rg/Grep over C# in Assets is denied unless the command carries # why: …; the message lists fmgraph / roslyn"}]
 :where [".claude/skills/fantasymayor-graph/" ".claude/hooks/"]
 :off-limits #{"code in Assets/"}
 :skip #{"stale cref warnings — decision 17"}
 :accept [{:meter "fmgraph.py build" :target "< 1 s"}
          {:meter "fmgraph.py check" :target "exit 0, no new warnings"}
          {:meter "the three grep questions of the session asked through the graph" :target "each answered"}
          {:meter "hook probe: grep over Assets/*.cs without a reason" :target "denied with the hint; with # why: allowed"}]
 :result "the graph answers member and enum questions; a bare grep over C# is stopped for every agent"}
```

# Plan

```clojure
(-> (:step-1 "source_reading: every member of a type — property, field, method, event, enum value — with its kind and line")
    (:step-2 "type_facts: members facet on the node; queries: members <Type>, member <Name>; explain shows enum values")
    (:step-3 "graph-gate.py: deny grep/rg/Grep over C# under Assets without # why:")
    (:step-4 "SKILL.md, graph-facts.md, tool-who-calls / tool-di-wiring hints where they name grep")
    (:step-5 "verify: build time, check, the three questions, the hook probe"))
```

# Findings

```clojure
[{:finding :session-greps
  :at "2026-09-26"
  :fact "of 40 grep over C# in this session (main 9, Opus 17 + 14), the graph lacked: members of a type (RequestedMode on 8 states, Priority on interfaces, IsEntryCompleted), enum values (AppState flags); everything else — registrations, singleton readers, OrchestratorType, event emitters, SystemPriorities — the graph already answered"
  :verified-by "the session transcript and both subagent transcripts parsed by script; each grep checked against fmgraph commands"
  :consequence "two new facts; the rest is a tool-order problem the hook addresses"}]
```

# Decisions

```clojure
[{:id 16
  :asked "Як саме фіксити субагентів?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "хук: grep по C# в Assets без # why: блокується" :confidence 45}
            {:id :b :option "хук + правило в AGENTS.md" :confidence 40}
            {:id :c :option "тільки правило" :confidence 15}]
  :chosen :a
  :value "hook only"
  :verified-by "«A (45) Хук»"}
 {:id 17
  :asked "Як граф повинен поводитись із застарілими cref?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "попередження в check без rule id" :confidence 70}
            {:id :b :option "не додавати" :confidence 30}]
  :chosen :b
  :value "no cref check"
  :verified-by "«B (30) Не додавати»"}
 {:id 18
  :asked "Постановку приймаєш — починати?"
  :status :confirmed
  :at "2026-09-26"
  :options [{:id :a :option "так, GO" :confidence 80}
            {:id :b :option "спершу зміни" :confidence 20}]
  :chosen :a
  :value "GO"
  :verified-by "«A (80) Так, GO»"}]
```

# Disproven

```clojure
[]
```

# Attempted

```clojure
[]
```

# Progress

```clojure
{:status :complete
 :completed #{"statement confirmed"
              "step 1-2 — members facet (value, const, field, property, event, indexer, method; partial parts add up), enum flag; commands members <Type>, member <Name>; explain lists enum values"
              "step 3 — graph-gate.py: grep/rg/ag over C# in Assets without # why: denied for EVERY agent (the check stands before the subagent exemption); the Grep tool over it denied; matcher gains Grep"
              "step 4 — SKILL.md commands + schema + the hook rule; graph-facts.md facets"
              "step 5 — verified"}
 :not-done #{"generated tool-* skills unchanged — the hook message carries the tool order; changing them goes through sdd-tool-skills"
             "arch-check (global skill) discovers with rg over the scope — its rg now needs # why: arch-check scan; the skill text was not edited"}
 :resume-context "done; archive through vault_move once Obsidian is up"}
```

# Acceptance

```clojure
[{:meter "fmgraph.py build" :target "< 1 s" :actual "0.34 s" :status :met}
 {:meter "fmgraph.py check" :target "exit 0, no new warnings" :actual "exit 0, 32 warnings as before" :status :met}
 {:meter "three grep questions through the graph" :target "each answered"
  :actual "member RequestedMode → 7 states; member IsEntryCompleted → 0 (the symbol is gone); members IPipelineStageSystem → Priority; explain AppState → 8 values" :status :met}
 {:meter "hook probe" :target "bare grep denied with the hint; # why: allowed"
  :actual "11 synthetic cases pass; a live Haiku subagent's grep over Assets/*.cs was blocked with the hint" :status :met}]
```
