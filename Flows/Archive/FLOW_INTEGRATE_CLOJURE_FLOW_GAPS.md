---
category: A
read: archive
tags: [flow, process, clojure, validation]
related:
  - "[CLAUDE](../../CLAUDE.md)"
  - "[AGENTS](../../AGENTS.md)"
  - "[DOC_STANDARD](../../DOC_STANDARD.md)"
status: implemented
---

# FLOW — Integrate Clojure Flow Gaps

Add the missing Clojure-first semantics to FantasyMayor without weakening its stronger task lifecycle.

---

# 1 · Request

## User specification — 2026-08-06

> Тоді треба додати ці промахи в поточний flow проєкту

> Так

## Agent restatement — confirmed by the user

```clojure
{:task :integrate-clojure-flow-gaps
 :goal "додати до standing flow FantasyMayor сильні сторони нового Clojure-first flow без втрати сильніших project lifecycle rules"
 :where #{CLAUDE.md
          AGENTS.md
          DOC_STANDARD.md
          Tools/doc_lint.py
          "/Users/serhiikharsun/.claude/CLAUDE.md"
          "/Users/serhiikharsun/.codex/AGENTS.md"
          "/Users/serhiikharsun/.codex/skills/fantasymayor-session-start/SKILL.md"}
 :off-limits #{"FantasyMayor runtime code"
               "SDDClojureFlow package"
               "existing archived FLOW history"
               "послаблення two-go contract"
               "заміна Request / Contract / Plan lifecycle"}
 :decided {:preserve #{:flow-after-research-go
                       :zero-open-resolutions
                       :request-contract-plan-lifetimes
                       :harvest-drop-tombstone
                       :contract-vs-history-fate
                       :accept-when-present}
           :add #{:always-prose-to-clojure-ir
                  :conditional-forms
                  :amendment-schema
                  :source-precedence
                  :progress-resume-schema
                  :blocked-outcome}}
 :do [(:step-1 "додати when, and, or та :then спочатку до canonical Claude glossary, потім синхронізувати Codex copy")
      (:step-2 "зробити prose → Clojure IR обов’язковою внутрішньою normalization для всіх запитів")
      (:step-3 "додати amendment contract: raw, normalized, confirmed та source precedence")
      (:step-4 "стандартизувати progress: completed, current, remaining, resume-context")
      (:step-5 "додати explicit blocked outcome без помилкового completion")
      (:step-6 "навчити session-start відновлювати нові progress, amendment і blocker fields")
      (:step-7 "додати до doc-lint Clojure syntax validation для актуальних project Markdown docs")]
 :skip #{"встановлення portable skill у FantasyMayor"
         "перенесення portable first-write timing"
         "always-archive behavior"
         "обов’язковий :accept"}
 :accept [{:meter "canonical glossary parity"
           :target "Claude canon та Codex copy семантично ідентичні"}
          {:meter "conditional notation"
           :target "when, and, or, :then, cond та :else канонізовані до використання"}
          {:meter "project contracts"
           :target "CLAUDE.md та AGENTS.md мають однакові normalization, amendment, resume і blocked semantics"}
          {:meter Tools/doc_lint.py
           :target "усі актуальні Clojure fences синтаксично валідні"}
          {:meter Tools/gen_index.py
           :target "INDEX lint clean"}
          {:meter "existing lifecycle"
           :target "two-go, first FLOW before Research, zero-open-resolution та flow-fate не послаблені"}]
 :result "FantasyMayor standing flow використовує новий Clojure-first layer поверх сильнішого project lifecycle"}
```

---

# 2 · Contract

```clojure
(def contract-state
  {:resolutions :closed
   :research-go :active
   :implementation-go :not-yet-requested
   :source-precedence [:verbatim-request
                       :confirmed-agent-restatement
                       :later-confirmed-amendments]
   :never #{"implementation during Research or Plan"
            "weaken the existing two-gate lifecycle"
            "edit the portable package"}})
```

```clojure
(def research-findings
  {:canonical-glossary {:claude "/Users/serhiikharsun/.claude/CLAUDE.md"
                        :codex "/Users/serhiikharsun/.codex/AGENTS.md"
                        :current-parity "same universal forms; vendor-specific prelude and provenance wording differ intentionally"
                        :missing #{when and or :then :always-prose-normalization}}
   :project-contract {:claude-resume "CLAUDE.md Start Working already discovers active FLOWs"
                      :codex-resume "/Users/serhiikharsun/.codex/skills/fantasymayor-session-start/SKILL.md"
                      :lifetime-placement {:amendments :request
                                           :durable-decisions :contract
                                           :progress-blocker-acceptance :plan}}
   :clojure-corpus {:active-blocks 115
                    :documents 24
                    :reader-forms #{set metadata quote discard}
                    :runtime :absent
                    :validator "dependency-free reader inside Tools/doc_lint.py"
                    :archive "excluded because archived forms are immutable historical vocabulary"}
   :doc-lint {:baseline "0 ghosts; 35 current Markdown docs scanned; 1148 C# declarations"
              :existing-scope "current docs only; Flows/Archive excluded"
              :integration "add Clojure errors beside ghost results without weakening ghost detection or --scope"}
   :workspace {:preserve #{"Assets/Plugins/Easy Save 3/Resources/ES3/ES3Defaults.asset"
                           INDEX.md
                           Flows/Archive/FLOW_COMPLETE_CLOJURE_FIRST_DESIGN.md
                           Flows/FLOW_DESIGN_INSTALLABLE_ENGINEERING_FLOW.md}
               :never "stage, revert, or rewrite unrelated user changes"}})
```

```clojure
(def integration-contract
  {:normalization {:input #{:prose :clojure}
                   :canonical-ir :clojure
                   :always true
                   :display (cond
                              (engineering-task?) :show-and-confirm
                              (ambiguity?) :show-relevant-fields-and-ask
                              :else :internal-only)
                   :never #{"replace verbatim input" "invent a missing value"}}
   :amendment {:home :request
               :shape {:received-at "date"
                       :raw-request "verbatim"
                       :normalized "Clojure IR"
                       :confirmed "boolean"}
               :authority "only a confirmed amendment patches the operational contract"
               :conflict "raw request remains evidence; contradiction requires a user decision"}
   :progress {:home :plan
              :shape {:status #{:active :blocked}
                      :completed "set"
                      :current "one item or ?"
                      :remaining "set"
                      :resume-context "smallest sufficient state"
                      :blocker "required only while blocked"}}
   :blocked {:frontmatter {:read :always :status :partial}
             :requires "a concrete external authority, user decision, runtime check, or environment change"
             :then "record blocker and preserve the next action"
             :never #{"mark done" "archive" "invent a workaround outside scope"}}
   :lifecycle {:preserve #{:two-go :flow-before-research :zero-open-resolutions
                           :request-contract-plan :harvest-drop-tombstone
                           :contract-vs-history :accept-when-present}}})
```

---

# 3 · Plan

```clojure
(def plan
  {:state :harvested-and-dropped
   :on "2026-08-06"
   :accept {:canonical-parity "glossary rows and normalization paragraph are identical"
            :conditionals "when, and, or, :then, cond and :else are canonical before project use"
            :project-parity "CLAUDE.md and AGENTS.md lifecycle blocks are identical"
            :reader-negative "four invalid fixtures fail with line and column"
            :doc-lint "0 ghosts; 0 Clojure syntax errors; 34 current docs after archival; 1148 symbols"
            :index "active FLOW appeared in always; generated INDEX lint clean"
            :lifecycle "two-go, FLOW-before-Research, zero-open, harvest and flow-fate preserved"
            :skill "skill-creator quick_validate: valid"
            :diff "git diff --check clean"}
   :harvest {:universal #{"/Users/serhiikharsun/.claude/CLAUDE.md"
                           "/Users/serhiikharsun/.codex/AGENTS.md"}
             :project #{CLAUDE.md AGENTS.md DOC_STANDARD.md}
             :validator Tools/doc_lint.py
             :resume "/Users/serhiikharsun/.codex/skills/fantasymayor-session-start/SKILL.md"}
   :fate :history
   :dropped "the executed implementation sequence"
   :record "working-tree diff; commit not requested"
   :never "re-add the executed plan"})
```
