---
category: B
read: trigger
trigger: "when starting a deep-research pass (sdd-deep-research) — copy the skeleton below → Flows/RESEARCH_<TOPIC>.md"
tags: [research, template, process]
related:
  - "[DOC_STANDARD](DOC_STANDARD.md)"
  - "[FLOW_TEMPLATE](FLOW_TEMPLATE.md)"
---

# RESEARCH_TEMPLATE

Copy-skeleton for a deep-research document: conditions, prior belief, trade-off map, disconfirmation, verdict.

```clojure
(def template-usage
  {:copy "everything below the cut line → Flows/RESEARCH_<TOPIC>.md"
   :topic-id "uppercase snake-case of the researched question"
   :frontmatter {:category "A" :read "trigger" :status "partial while the pass runs · implemented once delivered" :related "the FLOW that links it, when not standalone"}
   :engine "the sdd-deep-research skill — only the user switches it on (canon: deep-research)"
   :linked-from "the Findings section of the active FLOW; standalone = the document is the whole deliverable"
   :fate "moves to Flows/Archive/ together with the FLOW that links it (canon: done-contract)"
   :fill-rule "? stays ? until settled — a blank regime or an unverified option stays visibly blank"})
```

---

# Question

```clojure
{:question "what is being researched, stated plainly"
 :why-no-fast-answer "what makes this a trade-off space rather than a lookup"
 :opened-at "YYYY-MM-DD"
 :flow "Flows/FLOW_<TASK>.md, or :standalone"}
```

# Our conditions

```clojure
;; written BEFORE anything is read — an option wins only inside a regime (canon: deep-research)
{:scale ?
 :platform ?
 :budget ?
 :team ?
 :deadline ?
 :why "with our own regime left blank the map cannot be applied"}
```

# Prior belief

```clojure
;; the agent's guess goes on record before the search, specific enough to be provable wrong (canon: disconfirmation)
[{:hunch "the agent's own guess, specific enough to be provable wrong"
  :confidence 60
  :grounded-in "agent knowledge only — nothing read for this question yet"
  :at "YYYY-MM-DD"
  :outcome ?
  :deviation "when the search turns this into a different question, what changed and why"}]
```

# Options

```clojure
;; evidence weighed per canon evidence-weight: measured > ran-it > asserts > agent knowledge
[{:option "the candidate solution"
  :forces "what is in tension — the pull this option balances"
  :applies-when "the regime where it wins"
  :known-uses "where it has actually run"
  :evidence "what the source itself carries"
  :weakened-by "the named reason the evidence is thin: one source, no measurement, another context, agent inference"
  :confidence 70
  :buys "the size of what it gains"
  :cost-to-build "what it takes to make it from nothing"
  :cost-to-adopt "what it takes to take the ready-made one"
  :reversibility #{:two-way :one-way}}]
```

# Disconfirmation

```clojure
;; search for what would KILL the leading option, never for what would confirm it
{:target "the option currently leading"
 :searched-for "what would kill it"
 :came-back "what the search actually returned"
 :outcome #{:survived :weakened :killed}}
```

# Verdict

```clojure
{:recommends ?
 :because "the option and our conditions, joined"
 :evidence-bar "a one-way door demands strong evidence; a two-way door tolerates thin"
 :no-verdict "allowed — when nothing is verified the map itself is the deliverable"}
```

# Sources

```clojure
[{:source "what was read"
  :established "what this one actually settles — not what it is about"
  :kind #{:documentation :measured-study :confirmed-answer :practitioner-report :agent-knowledge}
  :at "YYYY-MM-DD"}]
```

# Search log

```clojure
;; saturation = rounds in a row that added no new option and no new evidence
[{:round 1
  :queries ["what was actually asked"]
  :new-options 0
  :new-evidence 0
  :at "YYYY-MM-DD"}]
```
