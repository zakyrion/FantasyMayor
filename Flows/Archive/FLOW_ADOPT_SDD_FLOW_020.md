---
category: A
read: archive
status: implemented
tags: [process, sdd, framework, adoption]
related:
  - "[CLAUDE](../../CLAUDE.md)"
  - "[DOC_STANDARD](../../DOC_STANDARD.md)"
---

# FLOW_ADOPT_SDD_FLOW_020

Adopt sdd-flow 0.2.0: stop hand-maintaining a copy of the canon and consume it by version instead.

# 1 · Request

## User request — 2026-08-30 (verbatim)

> оновився sdd-flow до версії 0.2.0, підхопи його, там також зʼявився скіл для міграції

> підтягни його з гіт репозиторію, а не з локальної папки

> запушив

> 1 - а
> 2 - і для claude і для codex
> 3 - a
> 4 - ми відійшли від Агентів взагалі, тому все що стосується їх, непотрібне за визначенням.
> 5 - sdd-flow має інкорпорувати в себе відмінності
> 6 - ні
> 7 -/Users/serhiikharsun/Documents/CoWork/SDDClojureFlow/docs/SEPARATION_GUIDE.md прочитай ще й цей гайд

> :agents "для codex залиш все що йому потрібно, але раніше в нас були власні агенти для пошуку, оркестрації, тощо і я від них свідомо відмовився"

> go

## Agent restatement — confirmed by the user 2026-08-30

```clojure
{:task :adopt-sdd-flow-020
 :kind :mutation
 :lane :research-go                      ;; SEPARATION_GUIDE кроки 4-5 (правки CLAUDE.md) — окремий свіжий go
 :source {:repo "github.com/zakyrion/sdd-flow" :ref cd95d2f :version "0.2.0"
          :run-from "клон у scratchpad, не з ~/Documents/CoWork/SDDClojureFlow"}
 :case :origin                           ;; проєкт несе ручну копію канону
 :do (-> (:s1 "init . --tools claude,codex")
         (:s2 "diff . --file CLAUDE.md — чотири кошики за іменами визначень")
         (:s3 "прочитати дрейф по-визначенню (проєктний осад vs правило) + survey слотів")
         (:s4 "звіт: слот за слотом, з джерелом кожного факту → стоп"))
 :writes #{".sdd-flow/" ".claude/skills/sdd-*" ".claude/commands/sdd-*"
           ".agents/skills/sdd-*" "Flows/FLOW_ADOPT_SDD_FLOW_020.md"}
 :never-touches #{CLAUDE.md AGENTS.md INDEX.md ".sdd-flow/project.md"}
 :exclusive false                        ;; відмінності інкорпоруються в adapter, а не в ексклюзивність
 :promote :off
 :result "звіт: 4 кошики поіменно + карта слотів adapter'а з джерелом кожного факту + перелік рішень для кроків 4-5"
 :accept [{:meter "sdd-flow doctor ." :target "0 missing / modified / invalid managed files"}
          {:meter "sdd-flow diff . --file CLAUDE.md" :target "кожне визначення віднесене до кошика"}]}
```

## Amendments (append-only)

```clojure
[{:received-at "2026-08-30"
  :raw-request ":agents \"для codex залиш все що йому потрібно, але раніше в нас були власні агенти для пошуку, оркестрації, тощо і я від них свідомо відмовився\""
  :normalized {:agents {:codex :keep-everything-it-needs
                        :own-subagents :abandoned}
               :survey-consequence {:tools-slot "adapter НЕ декларує скаутів — roslyn / ecs-graph / di-graph / doc_lint"
                                    :finding "розділ «Discovery Scouts» у CLAUDE.md і .claude/agents/*.md — мертвий текст; матеріал для кроків 4-5"}}
  :confirmed true}]
```

```clojure
[{:received-at "2026-08-30"
  :raw-request "Ми маємо в результаті отримати sdd-flow який я зможу оновлювати з гіта і не перегенерювати все з 0 кожен апдейт."
  :normalized {:result+ "sdd-flow, який оновлюється з гіта командою update і не потребує ручної респеціалізації на кожен реліз"
               :accept+ [{:meter "sdd-flow update . після git-оновлення пакета" :target "керовані файли перегенеровані, doctor чистий, жодного ручного пасу"}
                         {:meter "sdd-flow diff ." :target "0 drifted, 0 local-only поза заявленими :deliberate-patches"}]}
  :confirmed true}]
```

# 2 · Contract

## Findings

```clojure
[{:finding :remote-carries-020
  :at "2026-08-30"
  :fact "origin/main репозиторію github.com/zakyrion/sdd-flow = cd95d2f = 0.2.0"
  :verified-by "git ls-remote + свіжий --depth 5 клон у scratchpad; package.json version = 0.2.0"
  :consequence "джерело канону — клон, не локальна тека"}

 {:finding :cli-already-020
  :at "2026-08-30"
  :fact "глобальний CLI sdd-flow@0.2.0 стоїть npm-симлінком на ~/Documents/CoWork/SDDClojureFlow"
  :verified-by "npm ls -g --depth=0; which sdd-flow"
  :consequence "команди запускаю з bin/ клону, щоб джерелом лишався гіт"}

 {:finding :never-installed-here
  :at "2026-08-30"
  :fact "у FantasyMayor немає .sdd-flow/ — фреймворк ніколи не інсталювався; канон живе ручною копією в CLAUDE.md"
  :verified-by "ls -d .sdd-flow (No such file); .claude/skills містить лише context7-mcp"
  :consequence "випадок :origin за класифікацією sdd-project-init"}

 {:finding :global-bundle-pre-020
  :at "2026-08-30"
  :fact "сесійні скіли anthropic-skills:sdd-clojure-flow / sdd-deep-research не містять sdd-project-init"
  :verified-by "список доступних скілів сесії; пошук по диску не знайшов бандл anthropic-skills"
  :consequence "скіл міграції приходить тільки проєктною інсталяцією (відповідь 3-a)"}

 {:finding :guide-names-this-project
  :at "2026-08-30"
  :fact "docs/SEPARATION_GUIDE.md містить пророблений кейс FantasyMayor: CLAUDE.md = 8 in sync, 10 drifted, 8 local-only; поіменно названі prior-art / deep-research / done-contract як осад і notation-ecs-ext як свідомо проєктний"
  :verified-by "прочитано за прямою вказівкою юзера (файл лише локальний, docs/ не в гіті)"
  :consequence "очікуваний результат s2 відомий наперед — розбіжність з ним сама є знахідкою"}

 {:finding :no-active-flow
  :at "2026-08-30"
  :fact "активних partial FLOW немає — FLOW_BACKPORT_SDD_014 має status: implemented"
  :verified-by "frontmatter Flows/FLOW_BACKPORT_SDD_014.md; Flows/Archive/ — 7 файлів"
  :consequence "цей FLOW стає єдиним активним"}]
```

```clojure
;; s1-s3 — встановлення, вимір копії, читання дрейфу
[{:finding :install-clean
  :at "2026-08-30"
  :fact "init поставив 23 managed files (.sdd-flow/ + .claude/skills/sdd-* + .claude/commands/sdd-* + .agents/skills/sdd-*); doctor чистий"
  :verified-by "node <clone>/bin/sdd-flow.js init . --tools claude,codex; doctor . → clean (23 managed files)"
  :consequence "жоден існуючий файл проєкту не перезаписаний; skills сесії підхопили sdd-project-init"}

 {:finding :copy-lives-in-two-files
  :at "2026-08-30"
  :fact "копія канону лежить у ДВОХ файлах: CLAUDE.md і AGENTS.md — обидва дають ідентичний вимір 8 in sync / 10 drifted / 8 local-only"
  :verified-by "sdd-flow diff . --file CLAUDE.md; --file AGENTS.md"
  :consequence "AGENTS.md — друга копія тієї ж копії (генерується Tools/gen_agents.py); юзер від Агентів відмовився"}

 {:finding :no-canon-elsewhere
  :at "2026-08-30"
  :fact "DOC_STANDARD / CLOJURE_GUIDE / FLOW_TEMPLATE / RESEARCH_TEMPLATE / ARCHITECTURE / ECS_CONVENTIONS несуть 0 in sync і 0 drifted — тільки власні проєктні визначення"
  :verified-by "sdd-flow diff . --file <кожен з шести>"
  :consequence "відділення копії торкається рівно двох документів"}

 {:finding :research-depth-is-canon-renamed
  :at "2026-08-30"
  :fact "local-only research-depth = канонічний (def research) під іншим іменем: :depth/:characterize/:hypotheses/:observability/:passes/:record збігаються, бракує лише :goal і :method"
  :verified-by "порівняння тіла (def research-depth) у CLAUDE.md з (def research) у .sdd-flow/FLOW_CONTRACT.md:236"
  :consequence "не кандидат на promote — це перейменована копія; diff порівнює за іменем і тому показав його як local-only"}

 {:finding :task-normalization-is-canon-renamed
  :at "2026-08-30"
  :fact "local-only task-normalization покривається канонічними (def normalization) у CLOJURE_NOTATION.md і (def entry-contract) у FLOW_CONTRACT.md"
  :verified-by "порівняння тіл трьох визначень"
  :consequence "теж перейменована копія, а не винахід проєкту"}

 {:finding :go-inherits-duplicated
  :at "2026-08-30"
  :fact "drifted deliverable-kind додає :go-inherits — канон несе те саме правило в (def go-contract) :kind-bound"
  :verified-by "порівняння тіл у CLAUDE.md і FLOW_CONTRACT.md"
  :consequence "дубль, а не осад — видаляється разом з копією"}

 {:finding :stale-claim-in-deep-research
  :at "2026-08-30"
  :fact "drifted deep-research стверджує «the globally installed plugin skill IS the engine» — після init скіли стоять проєктно в .claude/skills/"
  :verified-by "список скілів сесії до і після init"
  :consequence "твердження застаріло сьогодні ж — окремий аргумент проти копії"}]
```

## Decisions

```clojure
(def decisions
  [{:decision :source-of-canon :status :confirmed :at "2026-08-30" :value "гіт-клон github.com/zakyrion/sdd-flow@cd95d2f"
    :verified-by "пряма вказівка юзера «підтягни його з гіт репозиторію, а не з локальної папки»"
    :reason "локальна тека і ремоут розійшлись на один коміт; після пушу збігаються, але джерелом лишається гіт"}
   {:decision :tools :status :confirmed :at "2026-08-30" :value "claude,codex"
    :verified-by "відповідь юзера на п.2" :reason "обидві ноги"}
   {:decision :skills-home :status :confirmed :at "2026-08-30" :value :project-local
    :verified-by "відповідь юзера на п.3 (a)" :reason "детермінована версія в репозиторії; глобальний бандл до-0.2.0"}
   {:decision :own-subagents :status :confirmed :at "2026-08-30" :value :abandoned
    :verified-by "пряме слово юзера" :reason "свідома відмова від власних агентів пошуку/оркестрації"}
   {:decision :exclusive :status :confirmed :at "2026-08-30" :value false
    :verified-by "відповідь юзера на п.5 «sdd-flow має інкорпорувати в себе відмінності»"
    :reason "після відділення копії в проєкті лишається один фреймворк, а не два поруч"}
   {:decision :promote :status :confirmed :at "2026-08-30" :value :off
    :verified-by "відповідь юзера на п.6" :reason "напрямок «вгору» юзера не цікавить у цьому заході"}
   {:decision :local-only-fate :status :confirmed :at "2026-08-30" :value :deliberate-patches
    :verified-by "відповідь юзера на п.1 (a)"
    :reason "flow-progress і blocked-outcome лишаються в CLAUDE.md як заявлені локальні патчі — канонічного відповідника немає, promote вимкнено"
    :supersedes :local-only-fate}
   {:decision :agents-chain :status :confirmed :at "2026-08-30" :value :retire-whole-chain
    :verified-by "відповідь юзера на п.2 (a)"
    :reason "AGENTS.md + Tools/gen_agents.py + крок 1c session-start + гілка pre-commit прибираються одним пасом на кроках 4-5"}
   {:decision :pointer-home :status :confirmed :at "2026-08-30" :value CLAUDE.md
    :verified-by "відповідь юзера на п.3 (a)"
    :reason "один знімний блок BEGIN/END SDD-FLOW: pointer; знімається через sdd-flow unlink"}
   {:decision :skill-duplication :status :deferred :at "2026-08-30" :value ?
    :verified-by "пряме слово юзера «потім повернемося»"
    :reason "глобальний до-0.2.0 бандл і проєктні 0.2.0 скіли видно в сесії одночасно; винесено за межі цього FLOW"}
   {:decision :tooling-prune :status :confirmed :at "2026-08-30" :value "\".sdd-flow\" у PRUNE (gen_index.py) і SKIP_DIRS (doc_lint.py)"
    :verified-by "go юзера після того, як він назвав оновлюваність з гіта критерієм приймання"
    :reason "варіант з дописуванням frontmatter у керовані файли ламає sdd-flow update — той перезапише їх, а doctor до того звітуватиме modified"}
   {:decision :cli-source :status :closed-as-not-blocking :at "2026-08-30"
    :value "будь-який git-чекаут пакета; юзер преференцію не зафіксував"
    :verified-by "метр update прочитаний з чистого клону: git pull → update . → 23 файли перегенеровано, doctor чистий, adapter недоторканий"
    :reason "вибір інвокації (npx з гіта / глобальна інсталяція з гіта / симлінк + git pull) виявився робочою звичкою, а не умовою результату — три варіанти записані в :extension адаптера"}])
```

```clojure
;; s5 — запис адаптера і вказівника
[{:finding :adapter-written
  :at "2026-08-30"
  :fact "записано .sdd-flow/project.md (усі шість слотів + Canon copy + Extension) і один маркований рядок-вказівник у CLAUDE.md поза зонами SHARED"
  :verified-by "Write/Edit; gen_agents.py --check → 10 zone(s) in sync (вказівник не зачепив жодної SHARED-зони)"
  :consequence "diff тепер працює без --file: читає :files з адаптера і бачить обидві копії"}

 {:finding :gen-index-aborts-on-framework-docs
  :at "2026-08-30"
  :fact "python3 Tools/gen_index.py АВАРІЙНО зупиняється: 9 нових .md фреймворку не мають frontmatter category/read, а .sdd-flow відсутній у PRUNE (Tools/gen_index.py:33)"
  :verified-by "запуск gen_index.py — «INDEX generation aborted» зі списком 18 порушень"
  :consequence "поки не полагоджено, pre-commit блокує будь-який коміт; INDEX.md не перегенерований, новий FLOW у нього не потрапив"}

 {:finding :doc-lint-ghosts-from-framework
  :at "2026-08-30"
  :fact "doc_lint дає 10 привидів у .sdd-flow/references/EXAMPLES.md — приклади фреймворку названі під FantasyMayor (DistrictBuiltEvent, TurnsComponent), але таких символів у коді немає"
  :verified-by "python3 Tools/doc_lint.py — усі 10 рядків з одного файлу"
  :consequence "той самий корінь: SKIP_DIRS у Tools/doc_lint.py:31 не знає про .sdd-flow"}]
```

```clojure
;; кроки 4-5 гайда — відділення копії і зняття ланцюга Codex-доків
[{:finding :copy-separated
  :at "2026-08-30"
  :fact "CLAUDE.md більше не несе канону: 0 in sync, 0 drifted, 4 local-only — і всі чотири заявлені (flow-progress, blocked-outcome як свідомі патчі; notation-ecs-ext, openspec-policy як проєктні правила)"
  :verified-by "sdd-flow diff . після правки; файл 37671 → ~19000 символів"
  :consequence "другий метр поправки досягнутий; наступний реліз прийде через update, а не через ручний бекпорт"}

 {:finding :agents-chain-retired
  :at "2026-08-30"
  :fact "видалено AGENTS.md і Tools/gen_agents.py; крок 1c прибрано з обох session-start (Claude-команда і Codex-скіл у repo та ~/.codex); гілка gen_agents знята з pre-commit; три .claude/agents/*.md скаути видалені разом з розділом «Discovery Scouts»"
  :verified-by "grep по репозиторію і ~/.codex — живих посилань не лишилось, окрім датованої історії в Flows/Archive і в FLOW_BACKPORT_SDD_014"
  :consequence "копія канону більше не існує у другому файлі; Codex працює через .agents/skills/sdd-*"}

 {:finding :dangling-links-fixed
  :at "2026-08-30"
  :fact "два архівні FLOW мали related-посилання на видалений AGENTS.md — знято саме посилання, тіла документів недоторкані"
  :verified-by "gen_index.py: LINT clean після правки (до неї — 2 broken link)"
  :consequence "історія збережена, лінт зелений"}

 {:finding :backport-flow-now-obsolete
  :at "2026-08-30"
  :fact "FLOW_BACKPORT_SDD_014 (status: implemented, read: trigger) описував зони CLAUDE.md, яких більше немає; його trigger указував на структуру, що зникла"
  :verified-by "тригер у frontmatter проти поточного CLAUDE.md"
  :consequence "юзер наказав видалити його звідусіль — файл видалено 2026-08-30 разом з посиланнями; два архівні RESEARCH_* лишились: вони тримають not-adopted вердикти, на які спирається пам'ять"}]
```

## Drift analysis (SEPARATION_GUIDE крок 3 — по-визначенню)

```clojure
;; кожне drifted визначення: що додав проєкт і чи це про проєкт, чи про правило
[{:definition agent-output      :adds "дата бекпорту, «shown for confirmation», крос-реф «below»" :verdict :cosmetic}
 {:definition evidence-weight   :adds "крос-реф «option-confidence» замість «(def option-confidence)»" :verdict :cosmetic}
 {:definition recurrence-guard  :adds "імена секцій без «#»" :verdict :cosmetic}
 {:definition go-contract       :adds ":research-go «create the task active FLOW»; :expires/:revoked-by прозою" :verdict :duplicate
  :covered-by "канон flow-document :first-write + go-contract :expires/:revoked-by/:after-revocation"}
 {:definition deliverable-kind  :adds ":go-inherits" :verdict :duplicate :covered-by "канон go-contract :kind-bound"}

 {:definition attempted         :adds "«(Contract stage — durable, survives the plan drop)»" :verdict :residue :slot :shape}
 {:definition disproven         :adds "«(Contract stage — durable, survives the plan drop)»" :verdict :residue :slot :shape}
 {:definition deep-research     :adds "«globally installed plugin skill» (застаріле) + «from RESEARCH_TEMPLATE.md»" :verdict :residue :slot :shape}
 {:definition done-contract     :adds "roslyn pre-check; Unity-side check; harvest + tombstone" :verdict :residue :slot #{:meters :ceremonies}}
 {:definition prior-art         :adds "«(roslyn / graphs / targeted reads)» замість «project-native knowledge tools»" :verdict :residue :slot :tools}]
```

```clojure
;; local-only — доля кожного при :promote :off
[{:definition research-depth     :verdict :canon-renamed :action "видалити; канон (def research)"}
 {:definition task-normalization :verdict :canon-renamed :action "видалити; канон (def normalization) + (def entry-contract)"}
 {:definition close-ritual       :verdict :project        :slot :ceremonies :action "адаптер вказує на /flow-close"}
 {:definition task-amendments    :verdict :mixed          :slot :shape :action "форма запису — у :shape; правило покрите flow-document :source-precedence"}
 {:definition flow-progress      :verdict :general        :action ?  ;; promote вимкнено → свідомий локальний патч або :shape
  :covered-by "канон flow-document :contains :progress/:resume-context — але не форму"}
 {:definition blocked-outcome    :verdict :general        :action ?  ;; канонічного відповідника немає
  :note "promote вимкнено → лишається свідомим локальним патчем"}
 {:definition openspec-policy    :verdict :project        :slot :extension}
 {:definition notation-ecs-ext   :verdict :project        :slot :extension :note "гайд прямо каже: лишається"}]
```

## Disproven (append-only)

```clojure
[]
```

## Attempted (append-only)

```clojure
[]
```

# 3 · Plan

```clojure
;; ТОМБСТОУН — план виконаний і знятий 2026-08-30 (DOC_STANDARD Rule 2d)
{:status :implemented
 :did "встановлено sdd-flow 0.2.0 з гіта; виміряно і знято ручну копію канону; написано адаптер; прибрано ланцюг AGENTS.md і власних агентів"
 :durable-facts-went-to #{".sdd-flow/project.md — усі шість слотів, свідомі патчі, retired-список"
                          "Tools/gen_index.py + Tools/doc_lint.py — .sdd-flow у PRUNE / SKIP_DIRS"
                          "пам'ять: sdd-flow-020-adoption, dual-agent-contract (RETIRED), scout-misfire (DELETED)"}}
```

## Acceptance

```clojure
;; аудит закриття — кожен метр прочитаний наживо 2026-08-30, жоден не з пам'яті
[{:meter "sdd-flow doctor ." :target "0 missing / modified / invalid"
  :actual "clean (23 managed files)" :status :passed}
 {:meter "sdd-flow diff ." :target "0 drifted, 0 local-only поза заявленими патчами"
  :actual "CLAUDE.md: 0 in sync, 0 drifted, 4 local-only — усі чотири заявлені в адаптері" :status :passed}
 {:meter "sdd-flow update . після git-оновлення пакета" :target "керовані файли перегенеровані, doctor чистий, без ручного пасу"
  :actual "git pull (Already up to date) → updated: 23 managed files → doctor clean; .sdd-flow/project.md з тим самим shasum" :status :passed}
 {:meter "python3 Tools/gen_index.py" :target "INDEX перегенеровано, лінт чистий"
  :actual "INDEX.md written: 33 docs; LINT: clean" :status :passed}
 {:meter "python3 Tools/doc_lint.py --quiet" :target "привиди не зросли"
  :actual "0 ghost(s) in 0 file(s)" :status :passed}
 {:meter "mcp__roslyn__get_diagnostics" :target "чисто на зміненому скоупі"
  :actual "не застосовний — жодного .cs не змінено" :status :not-applicable}
 {:meter "перевірка на боці Unity" :target "компілюється і поводиться"
  :actual "не застосовний — зміни лише в документах і в python-інструментах" :status :not-applicable}]
```
