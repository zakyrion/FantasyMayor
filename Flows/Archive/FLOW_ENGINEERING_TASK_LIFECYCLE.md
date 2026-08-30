---
category: A
read: archive
tags: [flow, process, task-lifecycle, sdd]
related:
  - "[DOC_STANDARD](../../DOC_STANDARD.md)"
  - "[CLAUDE](../../CLAUDE.md)"
status: implemented
---

# FLOW — Engineering Task Lifecycle

Every confirmed engineering task derives from the user's specification through one persistent FLOW document.

This document is the first task executed under the lifecycle it introduces. It owns the decision to make
one persistent FLOW the single cross-session artifact for every engineering task; the project policies own
the resulting standing rules.

---

# 1 · Request

## User specification and decision log — 2026-08-06

```clojure
{:task :sdd-comparison
 :problem "порівняти мій існуючий flow з sdd підходом та такими frameworks як: OpenSpec та Spec Kit"
 :goal "Знайти відмінності в підходах. Розібрати які сильні та слабкі сторони мають всі підходи"
 :expectation "Зрозуміти для себе чи потрібні нам якісь зміни в flow, чи можливо треба щось додати, чи змінити"}
```

> Якось в мене менше кроків в основному циклі, це ок чи ні, на твою думку?
> Щодо твоїх пропозицій:
> 1 -  Думаю є сенс завжди мати flow документ.
> 2 - ок
> 3 - ок
> 4 - ок
>
> Брати фреймворк я точно не хочу, я краще доповню свій підхід.

> я маю на увазі обидва.
> Файл може бути як джерело старих змін, і як cross session doc якщо мені потрібно перервати роботу.
> А весь наш flow має бути привʼязано до того що я задаю початкову специфікацію, а далі за ланцюжком

> Так, це те що я думаю мені потрібно. Спробуємо потім на реальних задачах

## Agent restatement — confirmed by the user

```clojure
{:task :implement-single-artifact-sdd
 :goal "зробити один постійний FLOW обов’язковим артефактом кожної engineering task"
 :decided {:task-unit "одна підтверджена map або vector batch"
           :first-write "після першого go створити FLOW і дослівно зберегти request разом з agent restatement"
           :research "факти та рішення одразу записуються у FLOW"
           :implementation "лише після consistency-check і fresh go"
           :completion "acceptance → harvest plan → tombstone → trigger або archive"
           :history "FLOW ніколи не видаляється"
           :migration "правило діє вперед; старі документи не мігрувати"}
 :result "власний single-artifact SDD без OpenSpec або Spec Kit"}
```

---

# 2 · Contract

```clojure
(def engineering-task-lifecycle
  {:source       "the user's initial specification — prose or Clojure"
   :unit         "ONE confirmed task map or vector batch → ONE new FLOW"
   :first-gate   "confirmation authorizes FLOW creation, Research and Plan — never implementation"
   :first-write  "create Flows/FLOW_<TASK>.md; preserve the request verbatim beside the agent restatement"
   :research     "verified facts and every user decision land in the same FLOW"
   :plan         "implementation map and consistency check land in the same FLOW"
   :second-gate  "fresh go authorizes only the confirmed implementation map"
   :execute      "implementation progress and acceptance evidence return to the same FLOW"
   :completion   "harvest durable knowledge, drop the executed plan, leave a tombstone"
   :history      "the FLOW file is never deleted"})
```

```clojure
(def flow-fate
  {:active    {:path "Flows/FLOW_<TASK>.md" :read :always :status :partial}
   :contract  {:path "Flows/FLOW_<TASK>.md" :read :trigger :status :implemented}
   :history   {:path "Flows/Archive/FLOW_<TASK>.md" :read :archive :status :implemented}
   :selection "a lasting contract stays trigger-readable; pure task history moves to Archive"
   :current-truth "code, derived graphs, policies and non-archived contracts — never archived symbol claims"})
```

```clojure
(def stage-exits
  {:request  "verbatim input and dated decisions preserved"
   :contract "zero open resolutions before implementation planning closes"
   :plan     "every step maps to the result or an acceptance meter"
   :execute  "result met, every present meter at target, diagnostic/runtime duties complete"
   :archive  "plan harvested and dropped; file retained"})
```

```clojure
(def migration
  {:policy :forward-only
   :existing-flow FLOW_DISTRICT_BUILD
   :change :none
   :frameworks :none})
```

```clojure
(def research-findings
  {:existing-base "DOC_STANDARD Rule 2 already separated Request, Contract and Plan"
   :replaced #{"multi-session-only persistence" "chat-only plans" "self-deleting PLAN files"}
   :index "read: always already provides dynamic session-start discovery without a second registry"
   :migration "FLOW_DISTRICT_BUILD already conforms to the surviving three-stage shape; unchanged"
   :pre-existing-drift "GLOSSARY.md declares category A while DOC_STANDARD classifies it as Category C; outside this task's scope"})
```

---

# 3 · Plan

```clojure
(def plan
  {:state :harvested-and-dropped
   :on "2026-08-06"
   :accept {:dogfood "active FLOW appeared in INDEX's read-at-start set"
            :policy "CLAUDE.md and AGENTS.md carry the same lifecycle and gate semantics"
            :standard "DOC_STANDARD.md defines one retained FLOW per engineering task"
            :tooling "py_compile clean; INDEX lint clean; doc-lint 0 ghosts across 33 current docs; git diff --check clean"
            :resume "session-start handles one active FLOW and surfaces several as a conflict"
            :fate "INDEX has 26 docs: 2 always, 21 trigger, 2 reference, 1 archive; this FLOW is absent from startup context"}
   :harvest {:policy #{CLAUDE.md AGENTS.md DOC_STANDARD.md}
             :tooling #{Tools/gen_index.py Tools/doc_lint.py}
             :session-start "/Users/serhiikharsun/.codex/skills/fantasymayor-session-start/SKILL.md"}
   :dropped "the executed implementation sequence"
   :record "working-tree diff; commit not requested"
   :never "re-add the executed plan"})
```
