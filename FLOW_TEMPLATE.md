---
category: B
read: trigger
trigger: "when creating a new Category A FLOW — copy the skeleton below, then fill it"
tags: [flow, template, process]
related:
  - "[DOC_STANDARD](DOC_STANDARD.md)"
---

# FLOW_TEMPLATE

Copy-skeleton for a Category A FLOW: three Rule 2 stages + Decisions, Progress, Acceptance, Amendments shapes.

```clojure
(def template-usage
  {:copy "everything below the cut line → Flows/FLOW_<TASK>.md"
   :task-id "uppercase snake-case of the confirmed :task id"
   :frontmatter {:category "A" :read "always" :status "partial"}  ;; + code_refs when the contract reasons about code
   :shape-home "the FLOW shape lives ONLY here; DOC_STANDARD owns lifecycle and fate rules"
   :fill-rule "? stays ? until the user closes it — never invent"
   :close "the canon close-ritual block (CLAUDE.md) — /flow-close on the Claude side"})
```

---

# 1 · Request

## User request — YYYY-MM-DD (verbatim)

> the user's words, unedited — prose or Clojure, whichever they wrote

## Agent restatement — confirmed by the user

```clojure
{:task :task-id
 :goal "requested capability"
 :where ?
 :off-limits ?
 :decided ?
 :do ?
 :skip ?
 :accept ^:optional [{:meter "instrument" :target "required reading"}]
 :result "observable outcome"}
```

## Amendments (append-only)

```clojure
[{:received-at "YYYY-MM-DD"
  :raw-request "verbatim amendment"
  :normalized "Clojure patch against the confirmed task"
  :confirmed false}]
```

# 2 · Contract

## Findings

```clojure
;; distilled facts that replace assumptions — each carries its provenance (canon: provenance)
[{:finding :finding-id
  :at "YYYY-MM-DD"
  :fact "the verified fact, stated plainly"
  :verified-by "how this was established, in prose"
  :research-document "Flows/RESEARCH_<TOPIC>.md, when a deep-research pass produced it"
  :consequence "what it changes for this task"}]
```

## Decisions

```clojure
(def decisions  ;; the contract closes when NO decision is :open (Rule 2b); revisit = dated supersedes-entry (canon: decision-revisit)
  [{:decision :decision-id :status :confirmed :at "YYYY-MM-DD" :value ? :verified-by "how this was established" :reason "why"}
   {:decision :open-decision :status :open :at "YYYY-MM-DD" :value ?}
   {:decision :revisited-decision :status :confirmed :supersedes :decision-id :at "YYYY-MM-DD" :value ?
    :verified-by "the new detail that reopened it"
    :reason "what was checked before, what came out, and what is new now"}])
```

## Disproven (append-only)

```clojure
;; write the moment a hypothesis is refuted; reread before formulating any new one (canon: disproven)
[{:hypothesis "the refuted assumption, stated plainly"
  :refuted-by "the observation or experiment that killed it"
  :at "YYYY-MM-DD"
  :details "anchor to the diagnostic block holding the full story"}]
```

## Attempted (append-only)

```clojure
;; write the moment an approach is abandoned; reread before offering any approach (canon: attempted, recurrence-guard)
[{:approach "what was tried, stated plainly"
  :confidence 70
  :dropped-because "what made it unusable"
  :problems "what it cost — what broke, and what it took to find out"
  :at "YYYY-MM-DD"}]
```

# 3 · Plan

```clojure
{:status :active
 :completed #{}
 :current ?
 :remaining #{}
 :resume-context "the smallest sufficient state for the next session"}
```

```clojure
(-> (:step-1 "first implementation step")
    (:step-2 "verification")
    (:step-3 "close per close-ritual"))
```

## Acceptance

```clojure
[{:meter "instrument" :target "required reading" :actual ? :status :pending}]
```
