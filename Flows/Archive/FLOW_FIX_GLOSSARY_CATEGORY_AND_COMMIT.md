---
category: A
read: archive
tags: [flow, documentation, metadata, commit]
related:
  - "[GLOSSARY](../../GLOSSARY.md)"
  - "[DOC_STANDARD](../../DOC_STANDARD.md)"
status: implemented
---

# FLOW — Fix Glossary Category And Commit

Correct the GLOSSARY metadata drift and commit the task lifecycle without unrelated user changes.

---

# 1 · Request

## User request — 2026-08-06

> Коміть, і пофікси проблему з GLOSSARY.md
> А ще я бачу що можна з мого flow зробити переносимий скіл чи framework

## Agent restatement — confirmed by the user

```clojure
{:task :fix-glossary-category-and-commit
 :goal "усунути metadata drift у GLOSSARY.md і закомітити завершений engineering-task lifecycle"
 :where #{GLOSSARY.md CLAUDE.md AGENTS.md DOC_STANDARD.md INDEX.md
          Tools/gen_index.py Tools/doc_lint.py
          Flows/Archive/FLOW_ENGINEERING_TASK_LIFECYCLE.md}
 :off-limits #{"Assets/Plugins/Easy Save 3/Resources/ES3/ES3Defaults.asset"
               ARCHITECTURE.md
               "ECS/runtime"}
 :decided {:glossary-category :C
           :glossary-content :unchanged
           :commit "один commit: lifecycle + пов’язане виправлення GLOSSARY metadata"
           :global-skill "session-start skill змінений, але він поза git repository і в commit не ввійде"}
 :skip #{"переписування GLOSSARY" "створення portable skill у цій задачі"}
 :result "GLOSSARY має правильну Category C; lifecycle закомічено без стороннього ES3Defaults.asset"}
```

---

# 2 · Contract

```clojure
(def glossary-fix
  {:change "frontmatter category A → C"
   :status "remove the Category-A-only status field"
   :content :unchanged
   :reason "GLOSSARY is a project-wide policy/reference document under DOC_STANDARD Category C"
   :mechanical-after #{"regenerate INDEX" "run doc-lint"}})
```

```clojure
(def commit-boundary
  {:contains "engineering-task lifecycle changes, both task-history FLOWs, and GLOSSARY metadata fix"
   :excludes "the user's pre-existing ES3Defaults.asset change"
   :global-skill "outside the FantasyMayor repository; cannot be part of this commit"})
```

---

# 3 · Plan

```clojure
(def plan
  {:state :harvested-and-dropped
   :on "2026-08-06"
   :accept {:metadata "GLOSSARY category is C; Category-A-only status removed; body unchanged"
            :syntax "Python compilation clean"
            :index "generator LINT clean"
            :docs "doc-lint 0 ghosts across 34 current docs"
            :diff "git diff --check clean"
            :warnings "only the pre-existing GAMEPLAY_FOUNDATION budget warning remains"
            :commit-boundary "explicit staged path audit excludes ES3Defaults.asset"}
   :harvest {:glossary GLOSSARY.md
             :lifecycle #{CLAUDE.md AGENTS.md DOC_STANDARD.md}
             :history "this FLOW remains under Flows/Archive"}
   :dropped "the executed metadata, validation, closure and commit sequence"
   :record "one scoped repository commit requested; the global session-start skill remains outside it"
   :never "re-add the executed plan"})
```
