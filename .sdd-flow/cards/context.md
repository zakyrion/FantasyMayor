# Card

```clojure
{:card :context
 :generated-from "the sdd-cascade skill and FLOW_CONTRACT.md — edit those, never this file"
 :read-also [".sdd-flow/references/CLOJURE_NOTATION.md" ".sdd-flow/templates/CONTEXT.md"]
 :then "only the stage's input artifact and the files it names"}
```

# Rules

```clojure
(def stage-isolation
  {:rule "a stage begins in a fresh context and reads its input artifact and the files that artifact names; nothing from any conversation"
   :mechanism "a stage runner — a fresh agent launched for that one stage (def stage-runner); where the harness offers no such agent, the owner's hand: a new session or a cleared one"
   :handoff "a stage ends by writing its artifact and the next stage into # Progress of FLOW.md; the runner reports where it wrote and what waits at the gate — the launching session reads the artifact from the file"
   :why "a derivation colored by the reasoning that produced its input is not a derivation; context that is not in the artifact is context the next stage will not have"
   :test "the input artifact is complete when it names every file the stage may read, every decision it must honor, and what is out of scope"
   :never #{"reading the previous stage's conversation"
            "a stage that needs to be told something the artifact does not say"}})
```

```clojure
(def stage-runner
  {:is "a fresh agent launched from the session that carries the task, for exactly one stage: it reads the stage's input artifact and the files that artifact names, runs the stage, writes the stage's artifact and # Progress, and reports back"
   :launched-by "the owner's session in step mode; the curator in auto mode"
   :model "asked of the owner before every launch — never assumed; in auto mode asked for every stage up to the limit in one batch before the curator starts; skipped where the harness offers no choice"
   :prompt "the task folder, the stage, the mode, the chosen model, and the order: read the stage's card and the glossary (def cards), then only what the stage reads — never the launching session's reasoning"
   :report "the path of the artifact, the count of contra entries, the questions that wait at the gate, the counts sdd-flow lint leaves (def runner-inside :lint), and :missing — what the stage had to guess or could not find in its input artifact; the artifact itself is read from the file, never repeated in the report"
   :missing "the meter of (def stage-isolation :test): the launching session writes it into # Progress; calibration counts it as :context-gaps; a CONTEXT section that is short run after run is a template to fix"
   :parallel "only code slices run side by side (def slices); every other stage runs alone"
   :never #{"a runner that runs two stages"
            "a runner told what its input artifact does not say"
            "a report that stands in for the artifact"}})
```

```clojure
(def runner-inside  ;; what this skill does when it runs as a runner
  {:loads "its card, .sdd-flow/cards/<stage>.md, the glossary and the stage's template (def cards) — this whole file only where the card is absent"
   :reads "only what (def stages) lists for the stage — and nothing the prompt adds beyond the folder and the stage"
   :runs "the stage as this skill states it, contra included"
   :writes "the stage's artifact and # Progress of FLOW.md — the next stage, and :auto-decided entries into # Decisions when in auto mode; in a wave of more than one slice, its own files and nothing shared (def code-stage :single-writer)"
   :lint "before reporting: sdd-flow lint <task folder>; fix what is this stage's own; the counts that remain go into the report; where the command is absent the tallies are taken by eye and the report says so"
   :reports "the pointer (def stage-runner :report), :missing included — what this stage guessed or could not find in its input"
   :never "answering the launching session's questions in the report — the artifact answers"})
```

```clojure
(def auto-decided
  {:is "a decision a stage makes in auto mode where step mode would have asked the owner"
   :entry {:id :ad-name :auto-decided true :confidence 70
           :chosen "the option taken"
           :options [{:option "the option taken" :confidence 70} {:option "the other" :confidence 30}]
           :because "why the rating"
           :answers ^:optional :c-1}
   :rule "the highest-rated option wins; the entry stands in the artifact where the decision was made — # Gate of S1.md or S2.md"
   :collected "every entry into # Decisions of FLOW.md with :status :auto, so a resume and the narrative retell them"
   :owner "may veto any entry at the next gate — the veto amends the artifact as a dated decision (def decision-revisit)"
   :never "a decision taken silently, without an entry"})
```

```clojure
(def artifact-syntax
  {:rule   "both artifacts are Clojure data; the reader takes them without error; nothing is evaluated"
   :bans   {"(def <Class>/methods …)" "def requires a simple symbol"
            "^:tag on a keyword"      "a keyword carries no metadata"
            "^:tag on a string"       "a string carries no metadata — it fails the same way"
            "Type[] as a symbol"      "brackets break a symbol; write the array type in :holds or as the language's generic form"}
   :allows {"List<Item>"             "one symbol: < and > are symbol characters"
            "{Update {…}}"           "a symbol as a map key"
            "{:optional true}"       "instead of ^:optional on a string"}
   :check  "the framework's Clojure reader — doctor runs it over every managed document; a task artifact is read by the agent under the same rules, and a fence the reader rejects is not an artifact"
   :meter  {:reader-clean {:target true}}})
```

```clojure
(def cascade
  {:goal      "the story is written BEFORE the code; the translation into code invents nothing"
   :artifacts [s1 s2]
   :s1        "the statement of the task and the algorithm that solves it: what is made, by which rule, with which structures, and what each step changes in them"
   :s2        "the compressed pseudocode of the future class: decomposition into methods, lifetime of structures, direction of every value"
   :code      "a translation of s2 — and only a translation"
   :law       "s1 knows no method or class names — only data types; decomposition is born for the first time in s2; nothing exists in the code that does not exist in s2"
   :s1-is-not "a less detailed copy of s2: if future methods show in s1, it is already too late to discuss the algorithm"
   :back-to-s2 "a need the code discovers and s2 lacks — a helper, a scratch, a field, a type — is written into s2 marked :from-code (def from-code), never invented silently"
   :existing-code {:is :number-source :is-not :subject :read "whole, without a cap on how many files are opened"}
   :findings  "CONTEXT.md, and the # Findings of FLOW.md behind it — the only place a guard's occasion counts as proven"
   :value-comes-from #{"decomposition DERIVED from the states of the data"
                       "data flow visible before the code"
                       "the algorithm stated before the code"}
   :value-does-not-come-from #{"volume of text" "number of levels"}
   :meter     {:invented-at-translation 0}})
```

```clojure
(def from-code  ;; the one form of the mark — a rule can count it
  {:form ":from-code \"what the code discovered, and why s2 lacked it\" — a key on the entry's value map, for a method and a data entry alike"
   :new "an entry born in the code carries the key from birth"
   :amended "an entry s2 already held, amended from the code, gains the key saying what was amended; a later amendment extends the same string"
   :counts ":invented-at-translation is the number of entries that carry the key"
   :never #{"the mark as prose inside :note"
            "^:from-code as metadata — a reader of entries meets a wrapped map"
            "a need that reaches the code and not s2"}})
```

```clojure
(def context-stage
  {:writes CONTEXT.md
   :from "FLOW.md: the confirmed contract, the findings, the decisions"
   :is "everything s1 needs and nothing it does not: the task restated, the code to read with paths and symbols, what to search for and where, facts with provenance, decisions to honor, what is out of scope, how the result is verified"
   :tools "the project's knowledge tools first (adapter), targeted reads second, prior art as the contract allows (def prior-art)"
   :existing-code {:is :number-source :is-not :subject :read "whole, without a cap on how many files are opened"}
   :kind ":artifact-kind — :class, or a free keyword; a non-class kind adapts the class keys of s1 and s2 and records the adaptation in Subject :adapted; a kind's own key set enters canon after its second run"
   :build "how the project builds, in # Build: the language, the runtime, how files import each other, the test runner with its exact command and style, where the conventions are stated — a code runner must never have to guess the module system from a neighbouring file"
   :gotchas "traps of the codebase and its libraries the next stage would fall into — each with :where, :avoid and :verified-by; a fact that only says how things are belongs in # Facts; empty is valid"
   :living "when Flows/Specs/<Subject>.md exists, # Reads names it as :spec-source and the s2 stage writes a delta against it (def delta)"
   :gate "the lifecycle's findings gate — the owner reads CONTEXT.md and answers before s1 exists"
   :complete-when (and (names-every-file-the-next-stage-may-read?)
                       (names-artifact-kind?)
                       (names-build-facts?)
                       (states-out-of-scope?)
                       (ends-with-verification?))
   :never #{"a raw dump"
            "a link the next stage would have to follow on its own initiative"
            "a fact without :verified-by"}})
```
