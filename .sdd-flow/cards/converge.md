# Card

```clojure
{:card :converge
 :generated-from "the sdd-cascade skill and FLOW_CONTRACT.md — edit those, never this file"
 :read-also [".sdd-flow/references/CLOJURE_NOTATION.md" ".sdd-flow/templates/S2.md"]
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
(def converge  ;; the drift meter — the product still derives from this level, or it does not
  {:runs   "at any time after code exists; also after every change to the subject's code that did not go through the cascade"
   :scope  "#{:slice :whole} — :slice classifies only the entries one slice carries, right after its runner reports; :whole is the run after the meters, at close, and at any later time"
   :reads  #{S2.md "the living S2 when it exists (def living-s2)" "every file s2 names"}
   :classifies "every s2 method, data entry and born type against the code"
   :verdict #{:present :partial :absent :contradicts :unrequested :deferred}
   :partial "a counterpart exists but lacks part of what the entry declares — a step of :flow, a field, a type, an exit"
   :absent "no counterpart in the code at all"
   :unrequested "code the s2 does not know — a method, a field, a type born in the code"
   :deferred "an entry marked ^:deferred with a confirmed amendment (def deferral) — accounted for, outside the tally"
   :accounted "every entry of the level appears in :entries once; an entry missing from the list is unaccounted, and the run is not clean — sdd-flow lint computes it against the effective s2"
   :level  "every entry that is not :present or :deferred names the level it returns to (def verdict :levels)"
   :clean  "every entry accounted; partial 0, absent 0, contradicts 0, unrequested 0; :deferred only where the mark stands"
   :writes "S2.md # Converge, append-only; a fix lands in s2 marked :from-code, or in the code — the owner chooses; a deferral lands in FLOW.md # Amendments first"
   :never  "editing s1 or s2 silently to match the code"
   :meter  {:partial 0 :absent 0 :contradicts 0 :unrequested 0}})
```

```clojure
(def verdict  ;; two verdicts, never one — the code derives from the level, and the level does what the task asked
  {:when "at close, after converge :whole and the acceptance check, before merge; written by the closing session, not a runner — it reads two records and names the level of every failure"
   :conformance "clean or drifted — read from the last converge :whole (def converge :clean)"
   :behavior "met, failed or pending — read from FLOW.md # Acceptance, every row at target, and the owner's check when CONTEXT.md # Verification names one"
   :independent "a faithful translation of a flawed algorithm is clean and fails; a translation patched to pass a meter is met and drifted — the record shows both so neither is read as the other"
   :failures "every failed meter, every read-back finding kept open and every converge entry that is not :present or :deferred, each with :level and :returns-to; a failure traced to a rated decision names it in :decision"
   :levels {:context "a fact or gotcha the code stood on was false — CONTEXT.md, then the chain from s1"
            :s1 "the algorithm computes the wrong thing — S1.md, then s2 and code"
            :s2 "the structure cannot carry the algorithm — S2.md, then code"
            :code "the translation slipped — the code alone, s2 unchanged"}
   :boundary "a behavioral failure with conformance clean is never fixed in the code; the level named is amended and the cascade re-runs from there"
   :writes "S2.md # Verdict; the calibration block and the ledger row read it"
   :never #{"a failure without a level"
            "one verdict standing in for the other"
            "a green that rests on a deferral"}})
```

```clojure
(def deferral
  {:is "an s2 entry the owner leaves out of this task's code — a scope amendment, never a verdict"
   :how "a row in FLOW.md # Amendments with its :amendment id, confirmed by the owner; the entry in S2.md carries ^:deferred on its value map and :deferred-by naming that row"
   :converge "a deferred entry is accounted for and classified :deferred — outside the tally that decides clean"
   :merge "a deferred entry does not enter the living S2 — the living S2 says what the code is; the archived S2.md and the amendment say what waits"
   :ledger "the row counts :deferred"
   :never #{"a deferral without a confirmed amendment"
            "an entry deferred by the agent"
            "a deferral that turns a red converge green"}})
```

```clojure
(def living-s2
  {:is "one S2 per subject — a class or a system — that lives outside the task folders and is the level the code regenerates from"
   :home "Flows/Specs/<Subject>.md; created by the first merge, never by hand"
   :born "the first cascade on a subject writes its S2 whole; at close, after a clean converge, it becomes the living S2"
   :delta "a cascade on a subject with a living S2 reads it at the s2 stage and writes its S2 as a delta: every method and data entry it adds, changes or removes carries ^:added, ^:changed or ^:removed on the entry's value map; an untouched entry is not repeated; the spine is written whole"
   :merge "a stage after a clean converge, at close: added entries are inserted in call order, changed entries replace their namesake, removed entries are deleted, deferred entries stay out (def deferral), the marks fall away; the task folder archives with the delta"
   :converge "measures the code against the living S2 once it exists — the standing drift meter of the subject; a task's converge before merge reads the delta over the living S2"
   :rot "a living S2 that converge cannot verify is the same as none; it exists only for subjects that went through the cascade — never a documentation effort"
   :never #{"a living S2 written by hand"
            "a merge before converge is clean"
            "a second living S2 for one subject"}})
```

```clojure
(def delta  ;; s2 on a subject that already has a living S2
  {:reads "S1.md and Flows/Specs/<Subject>.md"
   :writes "only what changes: every method and data entry the task adds, changes or removes, with ^:added, ^:changed or ^:removed on the entry's value map; an untouched entry is not repeated"
   :spine "written whole — it is the table of contents of the class after the change"
   :coverage "s1-coverage and data-coverage tally the living S2 plus the delta, as the merge would leave them"
   :marks "on the value map, for methods and data alike — a keyword carries no metadata, and one placement is one pass for a reader"
   :never #{"a delta that repeats the living S2" "a mark on a key" "a changed entry without its whole new body"}})
```

```clojure
(def method-keys
  {:does      "WHAT the method does, in one phrase; a helper has a :does too. A method may cover several s1 steps or be a phase of one — :does sets the boundary, not the step count"
   :in        "#{data keys} — everything the method READS; a key with :lives Method or EntryPoint arrives as a parameter, a key with :lives :run or :external is read as a field"
   :out       "a data key — what it RETURNS as a value; several results = one record named in the words of the task; :none = nothing"
   :writes    "#{data keys} — which structures it CHANGES: a run noun (:lives :run) or the scratch of the caller (:lives Caller, passed as a parameter)"
   :scratch   "#{data keys} — structures (collections, records) that live only inside the method; in code they are locals, never fields"
   :how       "HOW — the mechanism of one deed; a semicolon in :how means a :flow belongs here"
   :flow      "the order of steps inside; in the entry point every step carries {:calls Method :out :key} (step-keys)"
   :calls     "#{methods} — helpers that are not steps; a call that IS a step stands in the step itself and is not repeated here"
   :exits     "guards at the entry — each with an occasion proven in CONTEXT.md"
   :ends-with "what it finishes with when done"
   :numbers   "{:number-name \"formula or threshold\"} — every number, each named from the task; constants of the domain's geometry need no name"
   :note      "a fact not visible from the other keys; mandatory for every continue inside a loop — those are the places where the reader guesses"})
```

```clojure
(def step-keys  ;; the payload of a step in the entry point's :flow
  {:calls "Method — a bare symbol; one per step"
   :out   "a data key — the same as the :out of the called method; :none = nothing"
   :echo  "the match between a step's :out and the method's :out is checked by eye; a mismatch is a signal"})
```

```clojure
(def data-keys
  {:key    "the structure's name as a word of the task"
   :from-s1 "the key of the structure in the :data of the s1"
   :type   "the real type, repeated from s1 beside :from-s1 — S2.md alone must translate into code; a structure born in s2 carries :shape instead"
   :shape  "only for structures born in s2: ^:new <Type> — a record s1 did not have"
   :fields "{FieldInCode :numbers-or-data-key …} — what the record consists of: the field name as a bare symbol of the language, the value the key whose number or structure the field carries"
   :as     "a bare symbol — the name of the field or variable in code"
   :lives  ":run = a noun WRITTEN by two or more spine steps — a field of the owner | EntryPoint = returned by one step, read by the next — a local of the entry point | Method = a local of that method | :external = borrowed; whoever gave it releases it"
   :holds  "what lies in it"
   :paired-with {:asks "with which structure it shares an index" :optional true}
   :grows  {:asks "how it changes over the run" :optional true}
   :note   {:asks "a fact not visible from the rest" :optional true}
   :type-home "the type is chosen in s1 and repeated in s2 — the code stage reads S2.md only"
   :unborn "a record whose :fields landed on a foreign type (a pair, a tuple), or a record with :paired-with — an unborn type (def pair-is-a-type)"
   :numbers-become "a number from :numbers that outlives its method becomes a field of a record (:fields) or a data entry with :as — and carries the same name"
   :machine "sdd-flow lint takes every tally here that is a count — the s1 family of its catalogue; untouched input, a step's name against its state and a guard's occasion are judgments of one sentence against another and stay by eye"
   :glossary "CLOJURE_NOTATION.md (def cascade-fields) and the :data-reference reading"})
```
