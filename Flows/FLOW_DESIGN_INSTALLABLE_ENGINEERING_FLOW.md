---
category: A
read: trigger
trigger: "read before changing or implementing the standalone SDD Clojure Flow framework"
tags: [flow, process, portability, skill, framework]
related:
  - "[DOC_STANDARD](../DOC_STANDARD.md)"
  - "[Engineering Task Lifecycle](Archive/FLOW_ENGINEERING_TASK_LIFECYCLE.md)"
status: implemented
---

# FLOW — Design Installable Engineering Flow

Design a portable engineering-flow package that becomes active after one project installation.

---

# 1 · Request

## User specification and decisions — 2026-08-06

> Так, давай спробуємо щоб я міг це застосовувати і на інших проєктах

> 1 - Codex та Claude Code
> 2 - Було б прикольно мати щось як в Open Spec, коли я його заінсталив і він вже працює

## Agent restatement — confirmed by the user

```clojure
{:task :design-installable-engineering-flow
 :goal "спроєктувати переносимий engineering-flow package, який після встановлення автоматично працює в проєкті"
 :where #{CLAUDE.md
          AGENTS.md
          DOC_STANDARD.md
          Flows/Archive/FLOW_ENGINEERING_TASK_LIFECYCLE.md
          Flows/FLOW_DESIGN_INSTALLABLE_ENGINEERING_FLOW.md
          "/Users/serhiikharsun/.codex/skills/.system/skill-creator/"
          "локальний OpenSpec CLI — read-only аналіз installation UX"}
 :off-limits #{"runtime code"
               "встановлення нового framework"
               "зміна global agent configuration"
               "реалізація package до fresh implementation-go"}
 :decided {:agents #{Codex ClaudeCode}
           :experience (-> install project-bootstrap automatic-enforcement)
           :core #{specification hard-gate research plan fresh-go execute acceptance history}
           :artifact "один persistent FLOW на engineering task"
           :project-adapters #{"AGENTS.md для Codex" "CLAUDE.md для Claude Code"}
           :installation "одна команда на проєкт"
           :activation "після bootstrap flow стає standing process — не потребує ручного виклику на кожну задачу"
           :name :by-naming-policy}
 :research #{"package structure"
             "canonical core без дублювання semantics"
             "install/update/uninstall lifecycle"
             "безпечне доповнення наявних AGENTS.md та CLAUDE.md"
             "project configuration і FLOW storage override"
             "що лишити skill, а що зробити installer/framework layer"}
 :skip #{"implementation package" "міграція інших проєктів" "CLI installation"}
 :result "закритий дизайн installable framework і нова implementation task map"}
```

---

# 2 · Contract

```clojure
(def research-findings
  {:shared-standard "Codex and Claude Code both consume Agent Skills SKILL.md"
   :codex-skill-path ".agents/skills/<name>/SKILL.md"
   :claude-skill-path ".claude/skills/<name>/SKILL.md"
   :automatic "both agents may invoke a skill implicitly from its description"
   :standing-context "Codex loads AGENTS.md; Claude Code loads CLAUDE.md at session start"
   :plugin-boundary "Codex and Claude Code use different plugin manifests and marketplaces"
   :conclusion "one vendor-neutral CLI must install one canonical contract plus two thin agent adapters"})
```

```clojure
(def research-sources
  {:verified-on "2026-08-06"
   :codex "OpenAI Codex manual — Build skills, AGENTS.md and Package plugins"
   :claude "Claude Code official docs — Skills, Plugins and CLAUDE.md memory"
   :openspec "local OpenSpec CLI 1.7.0 and its installed init/update generators"})
```

```clojure
(def portable-vs-project
  {:portable #{"two confirmation gates"
               "Research → Plan → Execute"
               "one persistent FLOW per engineering task"
               "verbatim Request / durable Contract / disposable Plan"
               "batched decisions before implementation"
               "acceptance-backed completion"
               "active resume and permanent history"}
   :project #{"ECS and DI graph tools"
              "Unity validation policy"
              "Obsidian INDEX metadata"
              "FantasyMayor document ownership"
              "project-specific task template fields and verification commands"}})
```

```clojure
(def document-language
  {:primary :clojure-instruction-notation
   :input #{:clojure :prose}
   :canonical-ir :clojure
   :order [CLOJURE_NOTATION.md EXAMPLES.md FLOW_CONTRACT.md FLOW.md ADAPTERS]
   :rule "no form may be used before its semantics are defined in CLOJURE_NOTATION.md"
   :normative-content :clojure-only
   :structural-exceptions #{"YAML frontmatter required by an agent"
                            "package.json and config.json"
                            "CLI flags, filesystem paths and minimal Markdown navigation"}})
```

```clojure
(def input-normalization
  {:source-of-truth :verbatim-user-input
   :normalization :mandatory
   :pipeline (-> raw-user-input
                 preserve-verbatim
                 normalize-to-clojure
                 validate-meaning
                 detect-open-fields
                 classify-task
                 apply-flow)
   :dispatch (cond
               (clojure-form?) (:then "preserve exactly" "validate notation" "read as instruction")
               (prose?) (:then "preserve prose verbatim" "convert intent to Clojure IR" "continue from normalized meaning")
               :else (:then "mark input unresolved" "ask the user"))})
```

```clojure
(def normalization-visibility
  {:display (cond
              (engineering-task?) :show-and-confirm
              (ambiguity?) :show-relevant-fields-and-ask
              (user-requested?) :show
              :else :internal-only)
   :persist (cond
              (engineering-task?) (:then "store verbatim request" "store normalized restatement")
              :else (:then "use normalized form internally" "do not persist by default"))
   :never "skip normalization because the IR will not be shown"})
```

```clojure
(def normalization-integrity
  {:unknown-value ?
   :agent-proposal :by-source
   :multiple-deliverables :vector-batch
   :sequence '->
   :branching #{cond when}
   :condition-composition #{and or}
   :action-block :then
   :mutation-suffix '!
   :never #{"invent a missing field"
            "silently resolve ambiguity"
            "replace the verbatim source with its normalization"
            "lose a constraint expressed in prose"}})
```

```clojure
(def conditional-actions
  {:when "(when <condition> (:then <action> ...)) — execute only when the condition is true"
   :and "(and <condition-1> <condition-2> ...) — every condition must be true"
   :or "(or <condition-1> <condition-2> ...) — at least one condition must be true"
   :then "(:then <action-1> <action-2> ...) — ordered action block"
   :cond "(cond <condition> (:then ...) ... :else (:then ...)) — first true branch wins"
   :else ":else — final fallback branch"})
```

```clojure
(def conditional-rules
  {:conditions "predicates and facts only — never mutations"
   :then-order "actions execute from left to right"
   :when "has no alternative branch"
   :cond "executes exactly one branch"
   :and-or "compose conditions, never action sequences"
   :never #{"an ! action inside a condition" "more than one :else" ":else before the final branch"}})
```

```clojure
(def conditional-examples
  {:single (when "remaining turn count reaches 0"
             (:then (build-district!)
                    (delete-build-action!)))

   :branch (cond
             "cancellation happened in the same turn"
             (:then (refund-ap!)
                    (refund-all-resources!)
                    (delete-build-action!))

             :else
             (:then (refund-resources-proportionally! "remaining turns")
                    (delete-build-action!)))

   :composition (when
                  (and "district is still building"
                       (or "payer requested cancellation"
                           "owner requested cancellation"))
                  (:then (cancel-district-build!)))})
```

```clojure
(def example-normalization-findings
  {:duplicate-task-id "the second :add-turns map needs its own task id"
   :str-wrapper "(str \"...\") is not notation; a quoted string already owns abstract prose"
   :code-anchor "literal component/type names are bare Symbols, not descriptive keywords"})
```

```clojure
(def proposed-product
  {:name SDDClojureFlow
   :cli sdd-flow
   :package "sdd-clojure-flow"         ;; npm registry name was free on 2026-08-06
   :source "/Users/serhiikharsun/Documents/CoWork/SDDClojureFlow"
   :project-home ".sdd-flow/"
   :flow-home "Flows/"
   :agents #{Codex ClaudeCode}
   :commands #{init update doctor uninstall}
   :install-example "sdd-flow init . --tools codex,claude"
   :implementation "dependency-light Node.js CLI"
   :distribution "local package install in v1; npm publication is a later task"})
```

```clojure
(def installed-shape
  {:notation ".sdd-flow/references/CLOJURE_NOTATION.md"
   :examples ".sdd-flow/references/EXAMPLES.md"
   :flow-contract ".sdd-flow/FLOW_CONTRACT.md"
   :config ".sdd-flow/config.json"
   :template ".sdd-flow/templates/FLOW.md"
   :codex ".agents/skills/sdd-clojure-flow/SKILL.md"
   :claude ".claude/skills/sdd-clojure-flow/SKILL.md"
   :claude-commands ".claude/commands/sdd-flow/{start,resume,close}.md"
   :activation "agent-native skills and commands; root AGENTS.md / CLAUDE.md stay untouched"
   :history #{"Flows/FLOW_<TASK>.md" "Flows/Archive/FLOW_<TASK>.md"}})
```

```clojure
(def ownership
  {:managed #{".sdd-flow/references/CLOJURE_NOTATION.md"
              ".sdd-flow/references/EXAMPLES.md"
              ".sdd-flow/FLOW_CONTRACT.md"
              ".sdd-flow/templates/FLOW.md"
              "both thin SKILL.md adapters"
              "Claude command adapters"
              ".sdd-flow/manifest.json"}
   :user-owned #{"AGENTS.md and CLAUDE.md"
                 ".sdd-flow/config.json overrides"
                 "every FLOW document"
                 "all project code and docs"}
   :update "regenerate each configured adapter independently after checksum preflight"
   :uninstall "remove matching managed artifacts; preserve modified files and every FLOW"
   :never #{"edit an existing agent document" "delete task history" "infer resolution of conflicting project rules"}})
```

```clojure
(def activation-boundary
  {:automatic "installation makes the lifecycle eligible for implicit skill invocation and explicit native commands"
   :deterministic #{"managed artifact generation" "version/checksum drift detection" "FLOW structure validation"}
   :not-guaranteed "an LLM instruction is not a mechanical security boundary"
   :v1 "instructions + doctor; no cross-vendor hooks"})
```

```clojure
(def adapter-strategy
  {:canonical "one lifecycle model and FLOW template in the CLI package"
   :codex {:delivery :skills :project-home ".agents/skills"}
   :claude {:delivery #{:skills :commands} :project-home ".claude"}
   :init "generate only the selected adapters"
   :update "version and reconcile every configured adapter independently"
   :root-agent-docs :untouched})
```

```clojure
(def design-closure
  {:resolutions :closed
   :implementation :new-task
   :gate :fresh-go})
```

```clojure
{:task :implement-sdd-clojure-flow
 :goal "створити локально installable SDDClojureFlow framework для Codex та Claude Code"
 :where "/Users/serhiikharsun/Documents/CoWork/SDDClojureFlow/"
 :off-limits #{"FantasyMayor runtime and process docs"
               "global Codex and Claude configuration"
               "real target projects outside disposable fixtures"
               "npm publication"}
 :decided {:product SDDClojureFlow
           :package "sdd-clojure-flow"
           :cli sdd-flow
           :runtime "Node.js, dependency-light"
           :commands #{init update doctor uninstall}
           :language {:primary :clojure-instruction-notation
                      :input #{:clojure :prose}
                      :canonical-ir :clojure
                      :prose-conversion :always}
           :documents [CLOJURE_NOTATION.md EXAMPLES.md FLOW_CONTRACT.md FLOW.md ADAPTERS]
           :core #{".sdd-flow/references/CLOJURE_NOTATION.md"
                   ".sdd-flow/references/EXAMPLES.md"
                   ".sdd-flow/FLOW_CONTRACT.md"
                   ".sdd-flow/config.json"
                   ".sdd-flow/manifest.json"
                   ".sdd-flow/templates/FLOW.md"}
           :codex ".agents/skills/sdd-clojure-flow/SKILL.md"
           :claude #{".claude/skills/sdd-clojure-flow/SKILL.md"
                     ".claude/commands/sdd-flow/{start,resume,close}.md"}
           :agent-docs :untouched
           :flows #{"Flows/FLOW_<TASK>.md" "Flows/Archive/FLOW_<TASK>.md"}
           :distribution "local npm install only"}
 :do [(:step-1 "initialize the standalone package")
      (:step-2 "write the canonical Clojure glossary")
      (:step-3 "write prose-normalization and conditional-action examples")
      (:step-4 "express the complete engineering flow only with canonized forms")
      (:step-5 "author the Clojure-first FLOW template")
      (:step-6 "generate and validate both native adapters")
      (:step-7 "implement safe idempotent CLI lifecycle")
      (:step-8 "verify against disposable project fixtures")]
 :skip #{"publish to npm" "install into FantasyMayor" "cross-vendor hooks" "migrate existing projects"}
 :accept [{:meter "Clojure reader fixtures" :target "every canonical normative form parses"}
          {:meter "notation coverage" :target "every used form is defined before first use"}
          {:meter "document-language validator" :target "no normative prose outside declared structural exceptions"}
          {:meter "prose normalization fixtures" :target "each input becomes equivalent Clojure IR without semantic loss"}
          {:meter "conditional examples" :target "when, and, or, :then, cond and :else covered"}
          {:meter "skill-creator quick_validate.py" :target "both generated skills valid"}
          {:meter "fixture init" :target "codex, claude and combined installs have the exact managed shape"}
          {:meter "fixture update" :target "idempotent; modified managed files fail loud without force"}
          {:meter "fixture doctor" :target "clean install passes; drift is reported"}
          {:meter "fixture uninstall" :target "managed artifacts removed; FLOWs and user files preserved"}
          {:meter "npm pack --dry-run" :target "only intended package files"}]
 :result "npm-installable local package; після install команда sdd-flow init . --tools codex,claude робить проєкт готовим"}
```

---

# 3 · Plan

```clojure
(def plan
  {:state :harvested-and-dropped
   :on "2026-08-06"
   :accept {:compatibility "one canonical lifecycle; Codex skill and Claude skill/command adapters"
            :installation "one CLI init selects and generates both adapters"
            :decisions "product, source path, distribution, ownership and CLI lifecycle closed"
            :implementation "fresh-gated task map recorded in Contract"
            :checks "INDEX lint clean; doc-lint 0 ghosts; git diff --check clean"}
   :harvest {:design "Contract section of this FLOW"
             :implementation-task :implement-sdd-clojure-flow}
   :dropped "the executed research sequence"
   :record "FLOW contract; no implementation or commit in this task"
   :never "execute the implementation map without its fresh go"})
```
