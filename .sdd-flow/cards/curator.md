# Card

```clojure
{:card :curator
 :generated-from "the sdd-cascade skill and FLOW_CONTRACT.md — edit those, never this file"
 :read-also [".sdd-flow/references/CLOJURE_NOTATION.md"]
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
(def stages
  [{:stage :context
    :writes CONTEXT.md
    :reads #{FLOW.md "the files and tools FLOW.md names"}
    :gate "the lifecycle's findings gate — the owner sees CONTEXT.md before s1 exists"}
   {:stage :s1
    :writes "S1.md — # s1, # Contra, # Gate"
    :reads #{CONTEXT.md "the files CONTEXT.md names"}
    :gate :after-s1}
   {:stage :s2
    :writes "S2.md — # s2, # Contra, # Slices, # Gate"
    :reads #{S1.md "Flows/Specs/<Subject>.md when it exists — then s2 is a delta (def delta)"}
    :gate :after-s2}
   {:stage :code
    :writes "the files CONTEXT.md names as touched"
    :reads #{S2.md "the files it writes, whole"}
    :shape "(def code-stage) — one runner, or one runner per slice"
    :gate "the project's meters (adapter) after every runner has reported, then read-back"}
   {:stage :read-back
    :writes "S2.md # Read-back"
    :reads #{S2.md "every touched file, whole, as a stranger"}
    :gate "the owner's verdict: reads, or not"}
   {:stage :converge
    :writes "S2.md # Converge"
    :reads #{S2.md "every file s2 names"}
    :gate :none
    :runs "at any time after code exists — the drift meter of the north star"}
   {:stage :verdict
    :writes "S2.md # Verdict"
    :reads #{"S2.md # Converge" "FLOW.md # Acceptance"}
    :gate :none
    :runs "at close, by the closing session — not a runner: it reads two records and names the level of every failure (def verdict)"}
   {:stage :merge
    :writes "Flows/Specs/<Subject>.md — the living S2"
    :reads #{S2.md "the living S2 when it exists"}
    :gate "converge :whole clean (def converge :clean) and # Verdict written"
    :runs "at close; on the first cascade of a subject the task's s2 becomes the living S2 whole (def merge)"}])
```

```clojure
(def cascade-mode
  {:axis [:step :auto]
   :named-at "the implementation go on a :path :cascade map; unnamed = :step"
   :step "one runner per stage, launched from the owner's session; every gate is the owner's word in that session before the next runner starts"
   :auto "a curator — itself a fresh agent — launches one runner per stage in order up to the limit, with no owner's gate between them; a disputed place is closed by an :auto-decided entry (def auto-decided); at the limit the curator returns the narrative (def auto-narrative)"
   :auto-to {:s2 "context → s1 → s2, then the narrative and the owner's choice of how the code is written — the default"
             :code "also code, read-back and converge; the code stage takes the highest-rated option among one runner and the slices s2 proposed"}
   :models "in auto mode the owner names the model for every stage up to the limit in one batch before the curator starts — the curator cannot ask mid-run"
   :escalate "an :auto-decided whose top rating is below 60, or whose top two ratings lie within 10 of each other, does not decide: the curator stops and the narrative arrives early with that entry as the open question; the owner may name other numbers when naming the mode"
   :owner-sees "in step mode every artifact at its gate; in auto mode the narrative and S2.md — and the code, read-back and converge when the limit is :code"
   :never #{"auto mode chosen by the agent"
            "a curator that runs a stage itself instead of launching a runner"}})
```

```clojure
(def curator
  {:is "a fresh agent launched from the owner's session with the task folder, the limit and the models per stage"
   :runs (-> (:stage-1 "launch the context runner; wait; read its report")
             (:stage-2 "launch the s1 runner; wait; read its report")
             (:stage-3 "launch the s2 runner; wait; read its report")
             (:limit (when (= :code auto-to)
                       (:then "launch the code stage as the highest-rated option of S2.md # Slices — slices in waves by :after; after every wave, write what its runners reported for the shared files (def code-stage :single-writer), then one converge runner with :scope :slice, one record per slice"
                              "run the project's meters once, after every runner has reported"
                              "launch one read-back runner over every touched file"
                              "launch the converge runner, :scope :whole"))))
   :bar "after every report: an :auto-decided under the bar (def cascade-mode :escalate) stops the curator; the narrative arrives early with that entry as the open question"
   :on-failure "a runner that reports no artifact stops the curator; the narrative names the stage that failed and nothing is retried silently"
   :collects "every :auto-decided from the artifacts into # Decisions of FLOW.md with :status :auto; every :missing into # Progress"
   :returns "the narrative (def auto-narrative) — told by the owner's session in prose, the curator's report is its source"
   :never #{"running a stage in its own context" "asking the owner mid-run" "a gate the limit does not waive"}})
```

```clojure
(def runner-launch  ;; what the launching session does
  {:ask "which model runs this stage — every launch, never assumed; skipped where the harness offers no choice"
   :prompt #{"the task folder" "the stage" "the mode" "the order: read .sdd-flow/cards/<stage>.md and the glossary, then only the stage's input artifact and the files it names — this whole file only where the card is absent"}
   :never-in-prompt "the launching session's findings, reasoning or answers — if the stage needs them, they belong in the artifact"
   :wait "the runner's report: the artifact's path, the count of contra entries, the questions at the gate"
   :then (:then (read-artifact-from-file!)
                (lint-the-task-folder!)
                (present-gate!))
   :lint "the launching session runs sdd-flow lint before it presents a gate and shows the counts beside the artifact; an :error is the owner's to weigh, never hidden"
   :progress "the launching session writes the runner's model, outcome and :missing into # Progress"})
```

```clojure
(def auto-narrative
  {:told "in the owner's session, in prose: the facts the context stage established, the algorithm s1 chose, the structure s2 chose, every :auto-decided with its rating, how the result will look in the code, and the question that ends it — how is the code written: one runner or the slices s2 proposed, and which models"
   :then "the owner's answer is the s2 gate: a veto amends S2.md, a yes launches the code stage as chosen"
   :never "a narrative that replaces reading the artifact — S2.md remains the subject of the gate"})
```

```clojure
(def code-stage
  {:how #{:one-runner :slices}
   :reads "S2.md and the files it writes, whole"
   :one-runner "one fresh agent translates all of S2.md"
   :slices "one fresh agent per slice, in waves by :after, each with the model the owner named; after every wave reports, one converge runner with :scope :slice — one record per slice of the wave; a slice that drifted is fixed before the next wave"
   :single-writer "in a wave of more than one slice a runner writes its own files and nothing shared — not FLOW.md, not S2.md; its :from-code needs and its :missing travel in its report, and the launching session, or the curator, writes them after the wave — one writer, no collision; a slice alone in its wave writes S2.md itself"
   :meters "the project's meters (adapter) run once, after every runner has reported"
   :then "read-back — one runner, every touched file"
   :from-code "a need the code discovers is written into S2.md in the one form (def from-code) — by the runner that found it, into its slice's entries only; in a parallel wave, through its report (:single-writer)"})
```

```clojure
(def slices  ;; the end of every s2
  {:proposes "the s2 stage: how the translation splits into parallel runners, and the one-runner option rated beside it"
   :entry {:slice :name :writes #{} :carries #{} :after #{} :confidence 60}
   :law #{"no two slices write the same file"
          "every slice's runner reads S2.md whole and writes only its files"
          "a shared type or helper is written by one slice; every slice that reads it names that slice in :after"
          "in a wave of more than one slice a runner writes its own files and nothing shared (def code-stage :single-writer)"}
   :waves "slices launch in waves: every slice whose :after have all reported launches together; a slice with an empty :after is in the first wave"
   :chosen "at the s2 gate by the owner; in auto mode to :code by the highest rating"
   :after "the launching session runs the project's meters over all slices together, then one read-back runner over every touched file — the story does not split"
   :empty "a class small enough for one paragraph proposes no slices: {:one-runner {:confidence 100}}"
   :skeleton "S2.md # Slices"})
```
