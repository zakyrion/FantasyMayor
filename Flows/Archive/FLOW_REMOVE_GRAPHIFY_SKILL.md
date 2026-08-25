---
category: A
read: archive
status: implemented
tags: [tooling, skills, cleanup]
---

# FLOW — Remove Graphify Skill

## 1 · Request

## User request — 2026-08-21 (verbatim)

> `graphify` - треба взагалі видалити зі скілів

## Agent restatement — confirmed by the user

```clojure
{:task :remove-graphify-skill
 :goal "повністю прибрати Graphify зі встановлених навичок"
 :where "/Users/serhiikharsun/.agents/skills/graphify/"
 :off-limits "проєктний graphify-out/"
 :decided "видалити всю глобальну папку навички разом із SKILL.md"
 :skip "відновлення або зміна graphify-out/"
 :result "Graphify більше не доступний як skill"}
```

## Amendments (append-only)

```clojure
[]
```

# 2 · Contract

```clojure
(def research-findings
  {:skill-directory "/Users/serhiikharsun/.agents/skills/graphify/"
   :contents #{SKILL.md .graphify_version}
   :codex-counterpart :absent
   :project-graphify-output :off-limits})
```

```clojure
(def decisions
  [{:decision :deletion-scope :status :confirmed
    :value "/Users/serhiikharsun/.agents/skills/graphify/"
    :reason "The confirmed task names this global skill directory."}
   {:decision :project-output :status :confirmed
    :value :preserve
    :reason "graphify-out/ is explicitly outside scope."}
   {:decision :result :status :confirmed
    :value "The skill directory is absent; a new session can no longer load Graphify."
    :reason "The current session already has the skill instructions in context."}])
```

Every resolution is CLOSED, execute in order after a fresh implementation-go.

# 3 · Plan

Harvested on 2026-08-21.
No durable fact needed a policy or code comment; the Contract preserves the task decision.
Record = commit log; no commit was requested.

## Acceptance

```clojure
[{:meter "filesystem" :target "/Users/serhiikharsun/.agents/skills/graphify/ is absent" :actual :absent :status :pass}
 {:meter "filesystem" :target "project graphify-out/ remains present and unchanged" :actual "present; .graphify_detect.json and .graphify_python remain" :status :pass}]
```

## Acceptance audit — 2026-08-21

```clojure
[{:meter "filesystem" :target "/Users/serhiikharsun/.agents/skills/graphify/ is absent" :actual :absent :status :pass}
 {:meter "filesystem" :target "project graphify-out/ remains present and unchanged" :actual "present; .graphify_detect.json and .graphify_python remain" :status :pass}]
```
