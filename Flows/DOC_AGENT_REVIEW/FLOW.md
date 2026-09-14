---
category: A
read: always
status: partial
tags: [docs, process, sdd-flow, review]
related:
  - "[CLAUDE](../../CLAUDE.md)"
---

# Request

```clojure
{:source :user
 :received-at "2026-09-14"
 :raw-request ["оновився мій sdd-framework потрібно підтягнути його нову версію 0.3.0 і потім зробимо велике ревʼю документів та того як працює агент"
               "1 - спочатку оновлюємося потім працюємо над документацією\n2 - давай flow тільки на 2-гу задачу\n3 - це будемо вирішувати в другій задачі\n4 - видаляємо агентів\n5 - онови й глобальні скіли\n6 - тут комітимо\n7 - немає коментарів чи пропозицій"
               "переходимо до ревʼю"
               "ми будемо рухатися крок за кроком та виправляти документи починаючи з CLAUDE.MD"
               "1 - як в 0.3.0\n2 - будемо обговорювати і я буду казати що мені подобається і що ні.\n3 - що за осі?"
               "моя думка така. Зараз Claude.md зберігає багато чого, якісь розуми, посилання, частини pipeline-ів тощо. Наприклад Working Contract: Research → Plan → Execute який по суті дублює те що є в sdd-framework\nБільше того в цьому документі є багато посилань на інші документи, але ніде немає конкретних інструкцій чи читати їх, чи ні. Тобто наш головний файл Claude.md не описує процеси та процедури розробки"]}
```

# Confirmed contract

```clojure
{:task :doc-agent-review
 :goal "ревʼю документів проєкту і того, як працює агент, після переходу на sdd-flow 0.3.0"
 :path :direct                          ;; документи, не алгоритмічний код
 :mode "крок за кроком: один документ = один крок; обговорення — власник каже, що подобається і що ні; правки після його слова"
 :where {:step-1 CLAUDE.md :next "називає власник після прийняття кроку"}
 :off-limits #{Assets/ "видалені документи — не відновлювати" ".sdd-flow/ canon-файли (байт-в-байт з пакетом)"}
 :decided #{"FLOW тільки на цю задачу, у формі папки 0.3.0"
            "ARCHITECTURE.md змінюється тільки з дозволу власника"
            "долю flow-progress / blocked-outcome вирішуємо тут"}
 :axes [:alignment-0.3.0 :truth-vs-repo :contradictions-duplicates :context-cost :agent-clarity]
 :skip ?
 :result "кожен документ кроку приведений до погодженої форми; кожна знахідка має рішення"}
```

# Plan

```clojure
(-> (:step-1 "CLAUDE.md — знахідки → обговорення → цільова форма → правки")
    (:step-2 ?)
    (:close "/sdd-flow:close на слово власника"))
```

# Findings

```clojure
[{:finding :claude-md-restates-canon
  :at "2026-09-14"
  :fact "«Working Contract: Research → Plan → Execute» (CLAUDE.md:19-50) і HARD GATE повторюють канон: hard-gate, research, plan, execute, done-contract з .sdd-flow/FLOW_CONTRACT.md. HARD GATE сказано тричі: Working Contract, Engineering Task Template і сам канон"
  :verified-by "прочитав CLAUDE.md і .sdd-flow/FLOW_CONTRACT.md 0.3.0 поруч"
  :consequence "текст процесу в CLAUDE.md — друга копія канону; sdd-flow diff її не бачить, бо там немає (def …)"}

 {:finding :claude-md-restates-adapter
  :at "2026-09-14"
  :fact "Частини CLAUDE.md дублюють .sdd-flow/project.md: Start Working ↔ # Entry; Documentation Access ↔ # Tools (obsidian, read_canvas) + # Bans (vault_patch); Unity Build Policy ↔ # Bans + # Meters (roslyn get_diagnostics); Doc ownership ↔ :ask-first; graph refresh у Code Knowledge Policy ↔ # Ceremonies graph-rebuild"
  :verified-by "прочитав обидва файли повністю"
  :consequence "одне правило має два домівки — при зміні одна з них протухне"}

 {:finding :links-without-read-rule
  :at "2026-09-14"
  :fact "CLAUDE.md згадує ARCHITECTURE.md, DOC_STANDARD.md (4 рази), ECS_CONVENTIONS.md, Patterns/, INDEX.md, FLOW_CONTRACT.md, Tools/*.py — умову читання («прочитай перед X») має тільки INDEX.md; CODE_STORY_RULES_PROPOSAL.md і GLOSSARY.md не згадані зовсім, їхні тригери живуть лише в INDEX"
  :verified-by "grep посилань по CLAUDE.md; перевірив кожне на наявність умови читання"
  :consequence "підтверджує тезу власника: головний файл посилається, але не каже, коли читати"}

 {:finding :no-development-procedures
  :at "2026-09-14"
  :fact "CLAUDE.md не описує жодної процедури розробки кроками: як додати систему/компонент/подію, як змінити документ, порядок перевірки компіляції, як комітити (префікс [FM-xx] використовується в історії, ніде не записаний), як закрити задачу. Є політики і заборони, немає «коли X — зроби 1, 2, 3»"
  :verified-by "перелік заголовків CLAUDE.md; grep FM-/commit/branch; git log"
  :consequence "підтверджує тезу власника: файл описує рамки, а не процес"}

 {:finding :history-and-reasons
  :at "2026-09-14"
  :fact "Нечинний для дії текст: OpenSpec Policy (запис оцінки 2026-08-05), «module MDs abolished … rotted faster», пояснення «DELIBERATE LOCAL PATCHES … owner declined 2026-08-30», «(no curator agent)», абзац «The notation is defined ONCE», погроза «complaint and legal escalation risk» у User Process Contract"
  :verified-by "прочитав CLAUDE.md; класифікував кожен абзац: наказує дію чи пояснює історію"
  :consequence "платиться контекстом у кожній сесії, а поведінку не задає"}

 {:finding :glossary-two-homes
  :at "2026-09-14"
  :fact "CLAUDE.md:24 називає глосарій нотації .sdd-flow/references/CLOJURE_NOTATION.md, а CLAUDE.md:211 — ~/.claude/CLAUDE.md як «THE canonical glossary, defined ONCE»"
  :verified-by "grep CLOJURE_NOTATION / canonical glossary по CLAUDE.md"
  :consequence "пряма суперечність: два канонічні глосарії"}

 {:finding :local-patches-have-canon-home
  :at "2026-09-14"
  :fact "0.3.0 дав канонічне місце обом локальним патчам: шаблон FLOW має # Progress

```clojure
{:status :active
 :completed #{"step-1 CLAUDE.md — застосовано, прийнято власником («приймаю, комітимо») і закомічено 2026-09-14"}
 :current :step-2-choice
 :remaining #{"наступний документ — називає власник"}
 :resume-context "Крок 1 застосовано: CLAUDE.md 277 → 86 рядків (session start → request-routing з :read/:tools/:accept → bans → commit → notation-ecs-ext); ARCHITECTURE.md read: trigger + розділ Code shape; project.md # Entry/# Bans/# Ceremonies/# Canon copy оновлені; команди fantasymayor-session-start і flow-close видалені; FLOW_TEMPLATE :close → /sdd-flow:close. Помічено й додано в bans CLAUDE.md правило «silent skip → throw», що жило лише в project.md # Bans. Далі кандидати: форма FLOW (DOC_STANDARD + FLOW_TEMPLATE + gen_index + project.md # Shape), ECS_CONVENTIONS, 8 привидів HexIdComponent."}
```

# Acceptance

```clojure
[{:meter "python3 Tools/gen_index.py"
  :target "LINT: clean"
  :actual "LINT: clean (45 docs, 2 always) — після кроку 1"
  :status :pending}
 {:meter "python3 Tools/doc_lint.py --quiet"
  :target "0 Clojure syntax errors; ghosts не зросли"
  :actual "8 ghosts (HexIdComponent, до ревʼю), 0 syntax errors — після кроку 1"
  :status :pending}
 {:meter "власник"
  :target "кожен крок прийнятий словом"
  :actual "крок 1 прийнято 2026-09-14"
  :status :pending}]
```

# Amendments

```clojure
[]
```
