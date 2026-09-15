---
category: C
read: always
tags: [contract, process, rules]
related:
  - "[INDEX](INDEX.md)"
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# CLAUDE.md

How to work in FantasyMayor. The lifecycle is sdd-flow's and is not restated here; this file
routes a request into it and holds what is FantasyMayor's own: how a task statement is filled,
the bans, the commit rule, the project-scoped notation.

<!-- BEGIN SDD-FLOW: pointer -->
The sdd-flow canon is installed at `.sdd-flow/` (`FLOW_CONTRACT.md` + `references/`), this project's declarations at `.sdd-flow/project.md`; invoke it via `/sdd-flow:start | resume | close`, `/sdd-research`, `/sdd-project-init`.
<!-- END SDD-FLOW: pointer -->

## 1. Session start

```clojure
(-> (:step-1 "read INDEX.md")
    (:step-2 (cond (one-active-flow?)      "/sdd-flow:resume on it"
                   (several-active-flows?) "name them and ask which one"
                   :else                   "wait for the request")))
```

## 2. Every request — route through sdd-flow

```clojure
(def request-routing
  {:first "invoke the framework skill that fits — never handle a request outside it"
   :skills {sdd-clojure-flow            "any request: normalization, deliverable kind, gates, FLOW"
            /sdd-flow:resume            "an active FLOW exists"
            sdd-deep-research           "a question with no fast right answer — only on the owner's word"
            sdd-cascade                 "a confirmed map carries :path :cascade"
            fantasymayor-pattern-choice "a statement for work that creates or changes code — run before the statement is shown"
            fantasymayor-placement      "an implementation map that names new files, types, asmdef references or installers — run before the map is shown"}
   :statement (-> (:read   (-> "read INDEX.md"
                               "match the task statement against each doc's trigger; every doc whose trigger holds goes into :read"
                               (cond (engineering-task?) "add ARCHITECTURE.md, whatever the triggers say" ;; canon definition, .sdd-flow/FLOW_CONTRACT.md
                                     :else               "only the trigger matches — :read may be empty")))
                  (:skills "the result of every fantasymayor-* skill whose :skills entry holds for this statement; the recipes fantasymayor-pattern-choice returns go into :read as well")
                  (:tools  "entries of .sdd-flow/project.md # Tools whose :prefer-when holds for this task")
                  (:accept (cond (changes-code? task) "code-verification + entries of .sdd-flow/project.md # Meters whose :when holds"
                                 :else                "entries of .sdd-flow/project.md # Meters whose :when holds for this task")))
   :show "all four together with the task statement — the owner confirms or corrects them"
   :read-when "the docs in :read are read once the statement is confirmed"})

(def code-verification
  (-> (:step-1 "mcp__roslyn__get_diagnostics — solutionPath FantasyMayor.sln, scoped to the changed files; target: clean")
      (:step-2 "/arch-check on the changed scope; target: no violation the change introduced")
      (:step-3 (when (ecs-changed?)
                 (:then "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py tags; target: exactly one main tag per archetype, label tags shown apart, 0 deviations, no runtime tag writes")))
      (:step-4 "the owner's Unity check; target: compiles and behaves — the final authority")))
```

## 3. Project bans

```clojure
(def bans
  {:never #{"builds of the Unity project — Unity builds, dotnet build / msbuild / xbuild of its csproj or sln, Unity CLI builds; Tools/MarkerShapeAnalyzer is outside the ban"
            "generating or hand-writing Unity .meta files — stop and ask"
            "reading .unity scenes or scene-serialized assets without the owner's allowance in the current task"
            "editing .md through vault_patch / vault_write"
            "hand-editing .fantasymayor-graph/ artifacts"
            "editing between INDEX.md's BEGIN/END GENERATED markers"
            "recreating module / domain / presentation .md files — module knowledge is code comments + tools"
            "proposing automated test infrastructure"
            "a silent skip on a missing prerequisite — throw instead"}
   :must {:md-edit "plain Read / Edit / Write"
          :md-move-delete "vault_move / vault_delete — they keep vault links intact"
          :canvas-read "Tools/read_canvas.sh; full vault_read only to change layout"}
   :ask-first #{"any edit of ARCHITECTURE.md — the graph-gate hook asks; approving it IS the permission"
                "editing Flows / Patterns / the INDEX agent zone / a canvas after a code change"
                "Unity-side authoring: prefabs, .asset, scenes, addressable entries"}})
```

## 4. Commit

```clojure
(def commit
  {:when "only on the owner's ask"
   :message "[FM-<n>] <what changed>"   ;; <n> = the number in the current branch name Tasks/FM-<n>-…
   :trailer "the Co-Authored-By line the session prescribes"})
```

## 5. Project-scoped notation

```clojure
(def notation-ecs-ext  ;; 2026-07-17 — project-scoped notation extension (ECS); universal forms stay global
  {:entity-shape "(def <Archetype> {:archetype … :tag … :labels … :pk … :fk … :kind … :state … :data …}) — one map = one entity; :tag is the main tag, :labels the label tags; keys anchor to tag-law / key-role-law (ARCHITECTURE.md)"
   :set-cardinality "the FIELD decides the #{} reading: singular-valued key (:home, :tag) → global 'one of'; collection-valued key (:data, :fk, :labels) → ALL members, unordered, no duplicates (= ECS composition)"
   :tag-never-set "a #{} under :tag is not alternative syntax — it DISPLAYS a Tag Law violation (2 main tags) — label tags go under :labels"})
```
