---
name: fantasymayor-rules-conformance
description: Check that every carrier the agent actually reads — ARCHITECTURE.md, CLAUDE.md, the project skills, the Patterns/ recipes — still says what RULES_SPECIFICATION.md says, that no rule is missing from the carrier whose subject it is, and that the tools enforce what they claim. Read-only: it reports deviations by rule id and never edits a carrier or the specification. Use after a rule changes in the specification, after a carrier is edited, when a tool's check and a written rule may have drifted apart, or when the user invokes fantasymayor-rules-conformance.
---

# Rules conformance

`RULES_SPECIFICATION.md` is the maintenance source: a rule changes there first, and the carriers are updated
from it. The agent at work does not read it — it reads the carriers. So a carrier states the rule IN FULL;
repeating the specification is the point, not a defect. What this skill measures is whether the repetition
still says the same thing, and whether anything fell out of it on the way.

```clojure
(def model
  {:source RULES_SPECIFICATION.md
   :read-by-the-agent #{ARCHITECTURE.md CLAUDE.md "the project skills" "Patterns/ recipes"}
   :direction "specification → carriers; never a carrier back into the specification without the owner"
   :duplication :expected      ;; a carrier that only points at the specification teaches the agent nothing
   :defect #{"a carrier says something else" "a rule reaches no carrier at all" "a carrier keeps what the specification retired"}})
```

## Procedure

```clojure
(def conformance
  (-> (:step-1 "read RULES_SPECIFICATION.md — spec-contract, id-law, entry-shape, enforcers, gather-by-construct, and the rule blocks of the scope")
      (:step-2 "fix the scope (def scope) — a full run, one carrier, one id prefix, or the rules of one construct")
      (:step-3 "for each rule in scope, name the carriers whose subject it is (def carrier-scope) and find what each of them says about it")
      (:step-4 "classify every pair of rule and carrier (def verdicts); classify a carrier claim with no rule behind it too")
      (:step-5 "check coverage: a rule no agent-facing carrier states is a hole — the agent will never learn it")
      (:step-6 "report (def report) — findings only; this skill edits nothing")))
```

```clojure
(def scope
  {:full "every carrier, every rule — the run after a specification-wide change"
   :carrier "one carrier against the whole specification — the run after a carrier was edited"
   :prefix "one id prefix against every carrier — the run after a rule changed"
   :construct "the rules of one :governs key, gathered by the specification's own procedure"
   :states-itself "the report names the scope it ran; a finding outside the scope is still reported, never dropped"})
```

## Carriers

```clojure
(def carrier-scope
  [{:carrier ARCHITECTURE.md
    :carries "every code rule the agent must know while writing code — stated in full, in its own words, with the numbers of the law"
    :is-not "a pointer at the specification"
    :drift #{"says something else than the rule" "drops a clause of the rule — an exception, a condition, a number" "keeps a rule the specification retired"}}

   {:carrier CLAUDE.md
    :carries "the rules of how work runs here: routing into the lifecycle, the bans, the commit rule, the code-verification procedure"
    :drift #{"a verification step that names a command the tools no longer have" "a ban the specification contradicts"}}

   {:carrier ".claude/skills/fantasymayor-placement/SKILL.md, .claude/skills/fantasymayor-pattern-choice/SKILL.md"
    :carries "the rules its decision tree applies, stated in full at the branch that applies them"
    :drift #{"a branch that decides against the rule" "a condition of the rule missing from the branch" "a tree that sends the agent to a retired recipe or type"}}

   {:carrier "Patterns/PATTERN_*.md, Patterns/ADDRESSABLE_PATTERNS.md"
    :carries "the rules its block encodes, in the skeleton and in the recipe's own rules"
    :drift #{"a skeleton that encodes a retired rule" "prose contradicting the rule the skeleton shows" "a ghost type, a dead document, a renamed suffix"}}

   {:carrier ".claude/skills/fantasymayor-graph/"
    :is :enforcement-point
    :carries "a check for every rule marked :checked-by :graph"
    :drift #{"the check measures more or less than the rule" "no check at all" "a warning that does not name the rule id"}}

   {:carrier Tools/MarkerShapeAnalyzer
    :is :enforcement-point
    :carries "a diagnostic for every rule marked :checked-by :analyzer"
    :drift #{"the diagnostic measures something else" "no diagnostic" "a message without the rule id"}}

   {:carrier "~/.claude/skills/arch-check/SKILL.md"
    :is :enforcement-point
    :carries "a detection for every rule marked :checked-by :arch-check, over every root the rules bind"
    :drift #{"a ban the specification does not carry" "a rule it claims to catch and misses" "a scan root left out"}}])
```

## Verdicts

```clojure
(def verdicts
  {:aligned    "the carrier states the rule and says the same thing"
   :diverges   "the carrier says something else about the same subject — quote both sides"
   :incomplete "the carrier states the rule but drops a clause of it: an exception, a condition, a number"
   :missing    "a rule whose subject this carrier owns is absent from it"
   :uncarried  "no agent-facing carrier states the rule at all — the agent has no way to learn it"
   :extra      "the carrier holds a rule the specification does not carry — a candidate for a new rule OR for deletion; this skill never decides which"
   :stale      "the carrier names a type, file, document or rule that no longer exists"
   :uncited    "an enforcement point implements a rule but its message does not name the rule id — only for :is :enforcement-point carriers"
   :not-a-verdict "repeating the specification's wording — that is what a carrier is for"})
```

## Report

```clojure
(def report
  {:finding {:rule "the rule id, or :none for an :extra claim"
             :carrier "the carrier"
             :anchor "path:line of the carrier's text"
             :verdict "one of (def verdicts)"
             :spec-says "the rule's :says, verbatim"
             :carrier-says "the carrier's text, verbatim, or :absent"
             :proposal "the smallest change that would close the gap — for the owner, never applied here"}
   :tally {:scope "what the run covered"
           :rules-checked "how many rules of the scope were measured"
           :by-verdict "a count per verdict"
           :uncarried "the rules no carrier the agent reads states — the list, not only the count"}
   :target {:diverges 0 :incomplete 0 :stale 0 :uncarried 0}
   :home "the findings of the active FLOW; a run outside a task reports to the user"})
```

## Rules of the run

```clojure
(def run-rules
  {:read-only "this skill edits nothing — not a carrier, not the specification, not the graph artifacts"
   :evidence "every finding carries the carrier's anchor and both texts; a finding without a quote is a guess"
   :tools "fantasymayor-graph answers what a check actually measures; roslyn answers whether a named type still exists; doc_lint answers whether a document's symbol claims still resolve"
   :id-first "a finding is addressed by rule id — never by a section heading, which may be renamed"
   :never #{"counting a repetition as a defect"
            "asking a carrier to point at the specification instead of stating the rule"
            "deciding whether an :extra claim becomes a rule — that is the owner's"
            "editing a carrier to match the specification"
            "editing the specification to match a carrier"}})
```
