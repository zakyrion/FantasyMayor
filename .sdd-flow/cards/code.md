# Card

```clojure
{:card :code
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
(def data-flow-law  ;; why numbers become invisible fields when this is missing
  {:out-is-return    "everything a method passes on is returned as a value and lands in a variable named by the :as of that key"
   :many-outs        "several results of one step = a record named in the words of the task, born in s2 (^:new on the symbol); record name plus field name read as a phrase of the task"
   :writes-is-noun   "a field of the owner is written only by the method that declared it in :writes; the class holds as fields only the nouns of the run — so what could have changed behind a call is visible in the head of the class"
   :scratch-is-local "a structure seen by one method and its :calls is a local of that method; a field for it is an s2 error"
   :helper-fills     "a helper writes into the caller's scratch only through :in and :writes of the same key"
   :case {:is     "a method whose :out is prose — three values — becomes three fields with other names, and the reader guesses"
          :should ":out :run-numbers; :run-numbers {:shape ^:new RunNumbers :fields {OrderedCells :ordered-area, ReachRadius :reach-circle, SeedCount :how-many-seeds}} → var numbers = ComputeRunNumbers(order, cells)"}})
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

```clojure
(def spine
  {:is    "the :flow of the entry point, where every step carries {:calls Method :out :key}"
   :reads "every step = one line of code: var <:as of the step's :out> = <Method>(<:as of every key of its :in that arrives as a parameter>); :out :none = a call without a variable"
   :record-in "when a method takes a field of a record, the line writes <as>.<Field>; unpacking a record into locals is not a step"
   :never "a step hidden inside another's argument — Write(run.Build()) is two steps"
   :not-steps "guards from :exits and lifetime lines (acquire, release, construct the owner) — not steps, and outside the meter"
   :meter {:steps-visible "lines of the entry point's body without guards and without lifetime lines = spine steps"}
   :skeleton "S2.md # s2"})
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
(def story  ;; what readable code is — the subject of every rule below
  {:sequence "the file reads top to bottom in the order of the matter: the entry point; each of its steps in call order, and right after a step its helpers"
   :plot     "the entry point is the table of contents: each step one line with the name of its result; no step hidden inside another's argument"
   :variable "every variable is a noun of the task; in the same paragraph it is visible where it came from and where it went"
   :name     "a name says WHAT this is in the words of the task, not how it was computed and not where it is computed"
   :scale    "a method is a paragraph of about forty lines; it splits only into heterogeneous steps"
   :why      "a why stands where the code would lie without it — and nowhere else"})
```

```clojure
(def story-test  ;; the only meter of the story is reading; form meters do not replace it
  {:how      "retell the file aloud top to bottom without scrolling back"
   :fails-if #{"you had to scroll to understand what a call yields or where a field came from"
               "there is a variable or field you cannot name with one word of the task"
               "there is a method after which you must guess which fields it changed"
               "there is a name you had to translate in your head into a word of the task"
               "there is a paragraph longer than about forty lines, or one split into methods without heterogeneous steps"}
   :who      "a reader who has not seen the artifact; or an agent forced to answer every :fails-if on every line"
   :home     "the read-back stage"})
```

```clojure
(def translation  ;; the rules of the story when s2 becomes code
  {:status :hypothesis
   :why "every rule below was derived from a single run; a rule becomes a rule when the same signal appears in two runs in a row (def cascade-calibration)"
   :language :neutral ;; the rules name no language; the project's adapter carries what its language adds
   :rules [order returned-results names paragraph entry-point primitive guards pair-is-a-type lifetime comments]})
```

```clojure
(def order  ;; rule 1 — the order of the file is the order of the matter
  {:rule   "the entry point first; then its steps in call order, and right after a step its helpers in order of first call; a helper with several callers stands after the FIRST"   ;; the reader scrolls in depth, not by levels
   :fields "by lifetime and in order of appearance in the story: run nouns (:lives :run), then borrowed (:external); constants in the order of the formula"
   :alphabet {:allowed "a class-API whose methods are independent entry points: a util, a provider, a model, a config"
              :never   "a class whose methods make up one algorithm"}
   :lineage "the order of exposition is for the reader, not the compiler — literate programming applied to the code itself where the language permits any method order"
   :meter  {:call-order "method order = order of first call in depth; 0 exceptions for an algorithmic class"}})
```

```clojure
(def returned-results  ;; rule 2 — a step's result is returned, not hidden
  {:rule    "a step gives its noun as a value; several values = a record named from the task; a field only for a noun several steps write"   ;; the data flow is visible at the call site
   :not     "returning through reference parameters: they show the flow but rule the signature — a record does the same with one name"
   :scratch "a local of the method, allocated where first needed; the temporary is never a field"
   :meter   {:hidden-writes 0         ;; a :lives :run field written by a method without :writes on it
             :one-step-fields 0}})
```

```clojure
(def names  ;; rule 3 — every variable is a noun of the task
  {:rule    "the name of a type, method, field, parameter and structure is a word from s1/s2 through :as or :fields; the translation invents no synonyms"
   :concept "a concept identifier names a noun or a number of the TASK; mechanics names (i, direction, neighbour, best, current) are free, as long as they are words of the matter"
   :numbers "a number carries the name from :numbers, not the name of the computation that yields it and not the place where it is computed"
   :phrase  "record plus field read as a phrase of the task: numbers.ReachRadius says what it is; numbers.MidRad does not"
   :predicate "a predicate's name pays off only when it is more precise than the expression; a foreign word from another subsystem — never"
   :meter   {:names-from-artifact "every concept identifier = the :as or :fields of some s2 key; 0 without a source"}})
```

```clojure
(def paragraph  ;; rule 4 — a paragraph of human scale
  {:rule    "a method is one paragraph of about forty lines: one :does, the steps visible as lines; it splits ONLY into heterogeneous steps"
   :split-when #{"the :flow steps are heterogeneous — different nouns, different mechanics"
                 "a mechanic repeats three or more times (def primitive)"
                 "the paragraph no longer fits in about forty lines AND has a natural seam"}
   :never-split "a homogeneous loop into three methods; a helper called once that is the same deed"
   :inline-with-heading "a phase inside a long paragraph is marked by a heading comment: find the ring → position in the ring → walk the sides"
   :meter   {:story-test "(def story-test)"}})
```

```clojure
(def entry-point  ;; rule 5 — the entry point is the table of contents
  {:contains #{:guards :spine-steps :lifetime-lines}
   :rule     "guards at the entry, then one line per spine step; no branching on the substance of the task"
   :never    "a step hidden inside another's argument; copying fields between steps"
   :meter    {:steps-visible "(def spine)"}})
```

```clojure
(def primitive  ;; rule 6 — a repeated mechanic disappears into a primitive
  {:rule     "a mechanic repeated three or more times becomes a primitive that disappears from the call site"
   :mechanic "the same loop or expression of two or more lines, repeated verbatim or with names substituted; a single call is not a mechanic"
   :bar      "fewer than three — inline; a primitive without a name from the task is worse than the repetition"
   :meter    {:repeated-mechanics 0}})
```

```clojure
(def guards  ;; rule 7 — a guard with an occasion, or nothing
  {:rule       "a branch that CONTEXT.md proved impossible does not exist in the code; if it must exist, it throws with the text of the invariant"
   :must-exist "a branch must exist when the compiler demands it or when a loop exits without guaranteed progress"
   :silent     "a silent return is allowed for cancellation and for the entry point's guards, each of which stands in :exits with an occasion"
   :continue   "a continue inside a loop has an occasion in the s2 :note and a comment in the code — otherwise the reader guesses"
   :not-found  "a not-found state, if it exists in the task, is a property of the type, not a flag"
   :meter      {:guards-without-occasion 0 :silent-returns "0 outside the entry point's :exits"}})
```

```clojure
(def pair-is-a-type  ;; rule 8
  {:rule "two collections synchronous by index, or a foreign type into which two values are packed — an unborn type"   ;; only the reader's head holds the link between the halves
   :meter {:unborn 0}})
```

```clojure
(def lifetime  ;; rule 9 — owner and lifetime side by side, no ladder
  {:owner  "the system holds only read-only dependencies; run nouns live in an owner class for one call that knows nothing of the world — a snapshot in, a result out"
   :by-lives {:run       "the owner's constructor → the owner's release"
              :Method    "acquired in the first line where needed — released at the end of the paragraph"
              :EntryPoint "born in the method that returns it; released by the entry point that received it"
              :external  "not released here"}
   :one-place "every structure has exactly one place of release — :lives chooses it"
   :never  "release driving indentation — a ladder of nested cleanup blocks"
   :zero-allocation "deterministic lifetime, not a ban on types"
   :params "a helper that needs more than three parameters is either a paragraph of its caller, or its parameters are one record (def pair-is-a-type)"
   :meter  {:one-release-place true :max-params 3}})
```

```clojure
(def comments  ;; rule 10 — a comment says what the code does not
  {:only    "a why not visible from the code; a phase heading inside a long paragraph"
   :truth   "a comment is true for EVERY caller; an invariant another caller breaks stands in the wrong place"
   :meter   {:false-comments 0}})
```

```clojure
(def meters
  {;; ── the story — by eye ───────────────────────────────────────────
   :story-test              {:target :pass :on :code :how "(def story-test)"}
   :owner-verdict           {:target "reads" :on :code :means "the only meter that truly counts"}
   :call-order              {:target "0 exceptions" :on :code}
   :steps-visible           {:target "= spine steps" :on :entry-point}
   :names-from-artifact     {:target 0 :on :code :means "concept identifiers without :as or :fields in s2"}
   :invented-at-translation {:target 0 :on :code :means "s2 entries that carry :from-code (def from-code)"}
   ;; ── the artifacts — by eye ───────────────────────────────────────
   :state-named             {:target "every step leaves a named state" :on s1}
   :hollow-steps            {:target 0 :on s1 :means "steps without mutation and without a decision"}
   :s1-method-names         {:target 0 :on s1 :means "a method or class name in s1 — decomposition leaked from s2"}
   :s1-data-coverage        {:target "undeclared 0, orphan 0" :on s1}
   :data-coverage           {:target "undeclared 0, orphan 0, one-step-fields 0, unborn 0, s1-coverage complete" :on s2}
   :how-semicolons          {:target 0 :on s2 :means "steps in :how belong in :flow"}
   :reader-clean            {:target true :on #{s1 s2}}
   :context-gaps            {:target 0 :on :runner :means "what a stage had to guess — a CONTEXT section to fix when it repeats"}
   ;; ── the form — countable, language-neutral ───────────────────────
   :hidden-writes           {:target 0 :on :code}
   :one-step-fields         {:target 0 :on :code}
   :repeated-mechanics      {:target 0 :on :code}
   :guards-without-occasion {:target 0 :on :code}
   :silent-returns          {:target "0 outside the entry point's :exits" :on :code}
   :unborn                  {:target 0 :on :code}
   :one-release-place       {:target true :on :code}
   :max-params              {:target 3 :on :code}
   :false-comments          {:target 0 :on :code}
   ;; ── drift ────────────────────────────────────────────────────────
   :contradicts             {:target 0 :on :converge}
   :unrequested             {:target 0 :on :converge}
   :partial                 {:target 0 :on :converge}
   :absent                  {:target 0 :on :converge :means "an s2 entry with no counterpart in the code"}
   ;; ── the limit ────────────────────────────────────────────────────
   :warning "every countable meter green is zero information about the story; the story is caught only by the story-test and the owner"
   :language "the project's adapter adds the meters its language earns; canon carries none"
   :never   "breaking the structure by force for the sake of a meter"})
```
