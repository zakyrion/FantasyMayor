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
  :fact "0.3.0 дав канонічне місце обом локальним патчам: шаблон FLOW має секцію Progress {:status :completed :current :remaining :resume-context}; done-contract має гілку (blocked-by-external-authority?) → record-blocker, keep-flow-active. Не покрито каноном: :status :blocked як значення і поля :blocker/:needed-authority/:next-action"
  :verified-by "прочитав .sdd-flow/templates/FLOW.md і FLOW_CONTRACT.md # Done"
  :consequence "flow-progress майже повністю перекритий; blocked-outcome — частково"}

 {:finding :project-shape-vs-0.3.0
  :at "2026-09-14"
  :fact "Проєктна форма FLOW застаріла відносно 0.3.0: project.md :flow-home Flows/FLOW_<TASK>.md і :sections FLOW_TEMPLATE.md; DOC_STANDARD Rule 2 вимагає секцій «# 1 · Request / # 2 · Contract / # 3 · Plan»; gen_index.py перевіряє FLOW лише за префіксом Flows/FLOW_. Шаблон 0.3.0 має інші секції і папку на задачу. Цей FLOW у папці проходить lint тільки тому, що gen_index його не впізнає як FLOW. Також project.md :never-preload називав GAMEPLAY_FOUNDATION.md, якого немає (прибрано на кроці 1)"
  :verified-by "прочитав project.md, DOC_STANDARD.md заголовки, gen_index.py:195-223; ls GAMEPLAY_FOUNDATION.md"
  :consequence "окремий крок ревʼю: DOC_STANDARD + FLOW_TEMPLATE + gen_index + project.md # Shape"}

 {:finding :flow-template-is-canon-copy
  :at "2026-09-14"
  :fact "FLOW_TEMPLATE.md = секції канонового .sdd-flow/templates/FLOW.md, перегруповані в три стадії DOC_STANDARD Rule 2 (# 1 · Request / # 2 · Contract / # 3 · Plan) + фронтматер; власного змісту понад це немає. RESEARCH_TEMPLATE.md (115 рядків) — така сама копія канонового templates/RESEARCH.md (83). Від FLOW_TEMPLATE залежать: DOC_STANDARD.md:117 (:template), project.md # Shape :sections, lint gen_index.py (заголовки трьох стадій для Flows/FLOW_* і Flows/Archive/FLOW_*); від RESEARCH_TEMPLATE — DOC_STANDARD.md:35, project.md :research-document, CLOJURE_GUIDE.md:535"
  :verified-by "прочитав FLOW_TEMPLATE.md цілком; grep посилань по репо; порівняв з .sdd-flow/templates/"
  :consequence "видалення шаблону тягне рішення про Rule 2 (три стадії) і lint gen_index; архівні FLOW мають старі заголовки"}]
```

# Decisions

```clojure
[{:decision :flow-shape
  :status :confirmed
  :at "2026-09-14"
  :value "Flows/DOC_AGENT_REVIEW/FLOW.md — папка 0.3.0, секції шаблону 0.3.0, фронтматер проєкту (read: always, status: partial), щоб INDEX бачив активну роботу"
  :verified-by "відповідь власника «1 - як в 0.3.0»"
  :reason "власник обрав форму 0.3.0; фронтматер лишено, бо gen_index інакше не побачить FLOW"}

 {:decision :step-cycle
  :status :confirmed
  :at "2026-09-14"
  :value "обговорення: агент приносить знахідки й пропозиції, власник каже що подобається і що ні; правки лише після цього"
  :verified-by "відповідь власника «2 - будемо обговорювати…»"
  :reason "власник веде ревʼю"}

 {:decision :claude-md-role
  :status :confirmed
  :at "2026-09-14"
  :value "(A) інструкція з роботи: канон — один вказівник, декларації — .sdd-flow/project.md, історія — архів/пам'ять/коміти; CLAUDE.md = процедури «коли X → кроки, перед кроком N прочитай Y»"
  :verified-by "відповідь власника «А»"
  :reason "головний файл має описувати процес розробки, а не зберігати причини й копії"}

 {:decision :request-routing
  :status :confirmed
  :at "2026-09-14"
  :value (-> "запустити потрібний скіл фреймворку"
             (cond (engineering-task?) "до постановки задачі дописати обов'язкове читання ARCHITECTURE.md, ECS_CONVENTIONS.md і потрібних за тригером документів з INDEX.md"
                   :else "без обов'язкових документів; підтягувати через INDEX.md за потреби"))
  :verified-by "відповідь власника на питання 1"
  :reason "інженерна задача спирається на політику й конвенції; неінженерна не платить за них контекстом"}

 {:decision :request-routing-docs
  :status :confirmed
  :supersedes :request-routing
  :at "2026-09-14"
  :value (-> "прочитати INDEX.md"
             "звірити постановку з тригерами INDEX → відповідні документи в :read"
             (cond (engineering-task?) "плюс ARCHITECTURE.md і ECS_CONVENTIONS.md завжди"
                   :else "тільки збіги тригерів")
             "прочитати :read після підтвердження постановки")
  :verified-by "власник: «мені не подобається це формулювання, бо воно не дає процесу. А процес такий що INDEX.md читається і в залежності від постановки задач читається відповідна документація»"
  :reason "було: неінженерна гілка «підтягувати за потреби» — не процес; нове: INDEX читається завжди, вибір документів — за постановкою"}

 {:decision :statement-fields
  :status :confirmed
  :at "2026-09-14"
  :value "§3/§4 чернетки (engineering/doc procedure) видалені; постановка задачі несе :read (INDEX-тригери + ARCHITECTURE/ECS_CONVENTIONS для інженерної), :tools (project.md # Tools за :prefer-when), :accept (project.md # Meters за :when); усі три показуються разом з постановкою"
  :verified-by "власник: «так, показувати :read разом із постановкою» і «так, беремо варіант з трьома полями»; перед тим — «схоже на спробу перевизначити локально крок планування і написання коду»"
  :reason "стадії — канонові; проєктне — лише чим наповнити постановку"}

 {:decision :architecture-trigger
  :status :confirmed
  :at "2026-09-14"
  :value "ARCHITECTURE.md read: always → trigger; project.md :read-always без нього"
  :verified-by "власник: «ок, змінимо і даю свій дозвіл»"
  :reason "читається через :read інженерної постановки"}

 {:decision :session-start-command
  :status :confirmed
  :at "2026-09-14"
  :value "/fantasymayor-session-start видаляється повністю; ceremony session-start у project.md теж"
  :verified-by "власник: «не бачу сенсу в цій команді, sdd-flow має повністю це замінити»"
  :reason "старт і resume несе sdd-flow"}

 {:decision :code-rules
  :status :confirmed
  :at "2026-09-14"
  :value "унікальні правила коду (Code Quality, Code Documentation, коментар на відстані нуль) лишаються в CLAUDE.md без змін; перенесення — не зараз"
  :verified-by "власник: «поки що сюди не ліземо»"
  :reason "вирішується на кроці ECS_CONVENTIONS, якщо до нього дійде"}

 {:decision :code-rules-out-of-claude-md
  :status :confirmed
  :supersedes :code-rules
  :at "2026-09-14"
  :value "§5 Code rules прибирається з CLAUDE.md; в ARCHITECTURE.md (розділ Code shape перед Pattern Recipes) переходять: найпростіша структура / патерн лише коли вимагає, коментар на відстані нуль, коментарі лише де неочевидно + без XML-doc; видаляються: strict-review/senior-формулювання (не наказує дії), instance/static (дубль ECS_CONVENTIONS.md:142), заглушка (канон: ?, emergent-decision)"
  :verified-by "власник: «## 5. Code rules - не бачу сенсу в цьому блоці тут. Це або буде частиною Architecture або ніде не буде»; вибір (a) — «а, flow-close теж видаляємо»"
  :reason "було: лишити без змін («поки що сюди не ліземо»); нове: CLAUDE.md не дім для правил коду"}

 {:decision :flow-close-command
  :status :confirmed
  :at "2026-09-14"
  :value "/flow-close видаляється повністю; ceremony flow-close у project.md теж; FLOW_TEMPLATE.md :close → /sdd-flow:close"
  :verified-by "власник: «а, flow-close теж видаляємо»"
  :reason "закриття несе /sdd-flow:close; проєктні правила долі FLOW (Rule 2d/2e) уже живуть у DOC_STANDARD.md, а project.md # Shape :lifecycle на нього вказує"}

 {:decision :no-local-patches
  :status :confirmed
  :at "2026-09-14"
  :value "flow-progress і blocked-outcome видаляються; дозволені лише проєктні поля детальнішої постановки (:read :tools :accept)"
  :verified-by "власник: «ніяких локальних патчів, ми можемо використовувати тільки щось для більш детальної постановки задачі»"
  :reason "0.3.0 покриває прогрес і блокер"}

 {:decision :commit-rule
  :status :confirmed
  :at "2026-09-14"
  :value "коміт тільки на прохання; повідомлення «[FM-<n>] …», n — з назви гілки Tasks/FM-<n>-…"
  :verified-by "власник: «так, це правило коміту для цього проєкту, запиши»; git log — префікс [FM-n] у всіх задачних комітах"
  :reason "правило жило тільки в історії"}

 {:decision :bans-home
  :status :confirmed
  :at "2026-09-14"
  :value "проєктні заборони лишаються в CLAUDE.md"
  :verified-by "відповідь власника «заборони проєктні, тому лишаються тут»"
  :reason "діють у кожній сесії"}

 {:decision :link-rule
  :status :confirmed
  :at "2026-09-14"
  :value "кожне посилання в CLAUDE.md — тільки у формі «перед X — прочитай Y»; решта документів доходить через INDEX.md"
  :verified-by "відповідь власника «так»"
  :reason "посилання без умови читання не керує агентом"}

 {:decision :task-template
  :status :confirmed
  :at "2026-09-14"
  :value "шаблон задачі (проза + Clojure-альтернатива + мапінг полів) видаляється з CLAUDE.md"
  :verified-by "відповідь власника «шаблон задачі непотрібен бо він є в sdd-flow»"
  :reason "нормалізацію й форму постановки несе канон (entry-contract, sdd-clojure-flow)"}

 {:decision :flow-template-delete
  :status :confirmed
  :at "2026-09-14"
  :value "видалити FLOW_TEMPLATE.md і RESEARCH_TEMPLATE.md; з DOC_STANDARD.md прибрати Rule 2 (2a-2e) і все про форму/життєвий цикл FLOW — канон це sdd-flow; gen_index.py: перевірку заголовків трьох стадій видалити назавжди, архів не перевіряти на форму, FLOW-папки 0.3.0 визнавати; project.md # Shape — на канон"
  :verified-by "власник: «FLOW_TEMPLATE - це заміна sdd flow-у, то треба напевне видалити»; «1 - видаляй 2 - видаляй все, в нас канон це sdd-flow 3 - не перевіряй історію і якщо в нових flow цього рядку не буде то ніколи його не первіряй, можеш взагалі видалити цю перевірку назаважди»"
  :reason "шаблони й Rule 2 — локальна перебудова канону"}

 {:decision :clojure-guide-delete
  :status :confirmed
  :at "2026-09-14"
  :value "CLOJURE_GUIDE.md видалено повністю; згадки в пам'яті позначені як видалені; архів не чіпається"
  :verified-by "власник: «CLOJURE_GUIDE - можна теж видаляти. Все що було потрібно вже давно переїхало в sdd-flow та й решті я сам навчився»"
  :reason "людський підручник, зміст якого несе канон нотації"}]
```

# Disproven

```clojure
[]
```

# Attempted

```clojure
[{:approach "правити секції цього FLOW python-скриптом через s.index('# Progress')"
  :confidence 90
  :dropped-because "перший збіг '# Progress' стояв усередині тексту знахідки :local-patches-have-canon-home — зріз видалив дві знахідки, усі рішення, Disproven і Attempted; пошкоджена версія пішла в коміт e48cf34"
  :problems "виявлено при наступному записі; зміст відновлено з записів сесії 2026-09-14; секції правити лише Edit-ом по унікальному тексту"
  :at "2026-09-14"}]
```

# Progress

```clojure
{:status :active
 :completed #{"step-1 CLAUDE.md — застосовано, прийнято власником («приймаю, комітимо») і закомічено 2026-09-14"}
 :current :step-3-architecture
 :step-2 "прийнято («добре») і закомічено 2026-09-14 разом з видаленням CLOJURE_GUIDE.md: FLOW_TEMPLATE.md і RESEARCH_TEMPLATE.md видалені; DOC_STANDARD Rule 2 (2a-2e) → один блок-вказівник на канон, Category A рядок і чекліст скорочені; gen_index.py — перевірка заголовків трьох стадій видалена назавжди, FLOW-папка визнається, архів не перевіряється на форму й биті посилання; project.md # Shape → канон; CLOJURE_GUIDE.md:535 → канонічний шаблон; відновлення FLOW після інциденту теж чекає коміту"
 :remaining #{"відкрито: ARCHITECTURE.md:171 і PATTERN_TRANSACTION_ENTITY.md:94 — :flow-contract Flows/FLOW_<NAME>.md (стара форма)" "відкрито: DOC_STANDARD згадує видалений GENERAL_UI_STYLE.md" "8 привидів HexIdComponent"}
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
