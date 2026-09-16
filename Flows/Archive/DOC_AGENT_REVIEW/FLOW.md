---
category: A
read: archive
status: closed-by-owner
tags:
  - docs
  - process
  - sdd-flow
  - review
related:
  - "[CLAUDE](../../../CLAUDE.md)"
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
 :off-limits #{"Assets/" "видалені документи — не відновлювати" ".sdd-flow/ canon-файли (байт-в-байт з пакетом)"}
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
    (:step-3-skills-split (-> (:a "скіл fantasymayor-placement: Layers, depends, Placement, Folder layout, DI composition — з виправленнями відповідей")
                              (:b "скіл fantasymayor-pattern-choice: дерева Shared kernel + Pattern Recipes, turn phase, «no recipe», tag-law перед оголошенням, реалізації через ecsg / roslyn")
                              (:c "ARCHITECTURE.md: прибрати перенесені розділи; виправити tag-law, archetype-law, system-collections, native-allocator, reactive / event-lifecycle, deviations; перший рядок")
                              (:d "CLAUDE.md § 2: поле :skills і процедура перевірки коду")
                              (:e "project.md # Meters: прибрати метри, що переїхали")
                              (:f "PATTERN_EVENT, PATTERN_REACTIVE_SYSTEM, arch-check, пам'ять")
                              (:g "doc_lint, gen_index, унікальність (def …), grep «payload-less»")))
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
  :consequence "видалення шаблону тягне рішення про Rule 2 (три стадії) і lint gen_index; архівні FLOW мають старі заголовки"}

 {:finding :architecture-duplicates
  :at "2026-09-14"
  :fact "ARCHITECTURE.md дублює: банер PERMISSION-GATED (13-15) = CLAUDE.md bans :ask-first + graph-gate hook; абзац Style/notation (21-25) = DOC_STANDARD Rule Style; таблиця Pattern Recipes (198-218) = тригери Patterns/ в INDEX.md, і вже розійшлась — нема PATTERN_VIEW_SYSTEM.md і ADDRESSABLE_PATTERNS.md; :naming/:type-name (131-134) = ECS_CONVENTIONS → Naming & Construction"
  :verified-by "прочитав ARCHITECTURE.md цілком; ls Patterns/ проти таблиці; grep заголовків ECS_CONVENTIONS"
  :consequence "routing CLAUDE.md § 2 уже бере патерни з тригерів INDEX — таблиця стала другим пікером"}

 {:finding :architecture-dead-refs
  :at "2026-09-14"
  :fact "Посилання на неіснуюче: GENERAL_UI_STYLE.md §15 (32), Assets/Modules/Boot/BOOT.md (179, module MD скасовані), :flow-contract \"Flows/FLOW_<NAME>.md\" (171, стара форма; те саме в PATTERN_TRANSACTION_ENTITY.md:94). Правило «one behavior = one contract doc» спиралось на долю FLOW :contract (read: trigger після завершення) з видаленого Rule 2e — у каноні FLOW архівується цілком"
  :verified-by "ls GENERAL_UI_STYLE.md, ls Assets/Modules/Boot/BOOT.md — нема; порівняв з каноном flow-document / done-contract"
  :consequence "правило про контракт-документ поведінки не має дому"}

 {:finding :architecture-rosters-and-history
  :at "2026-09-14"
  :fact "Проти власного «Policy only — no living rosters»: shared-kernel перелічує ~15 типів Ecs.Extensions (60-61); абзац Turn pipeline (137-143) описує поточну реалізацію (TurnProcessorSystem опитує, фази inline); історія/причини: «Why this shape» DDD-абзац (50-55), дати в коментарях (tag-law 2026-07-08/07-15, key-role-law, cross-domain), «module MDs are abolished» (16-17). Типи в таблиці ролей і Turn-абзаці існують (WorldInstaller єдиний LifetimeScope, TurnProcessorSystem, TurnPhaseSubSystem, ViewSubSystem, HexResourcesViewSubSystem, UITag, AppState, GameModeMachine); Friflo 3.6.0 — так; doc_lint 0 привидів"
  :verified-by "grep по Assets/*.cs; ls Assets/Packages; ls Assets/Scripts; doc_lint --scope ARCHITECTURE"
  :consequence "225 рядків читаються в кожній інженерній задачі; ростери й наратив — кандидати на видалення"}

 {:finding :feature-folders-predate-layers
  :at "2026-09-14"
  :fact "Feature folder layout (ARCHITECTURE.md:78-91) не відповідає дереву. Asmdef: один на домен (Domains/Actions|Actors|Economy|Kernel|Map), Presentation і Presentation/UI, у Modules — на модуль або пару Core/Implementation. Рівень асемблі несе Archetypes/ (Domains/Actions|Actors|Economy|Map, Presentation, Presentation/UI, Modules/UserInput) — у блоці його нема. Installer/: у Actions/Actors/Economy на рівні домену, у Map — на рівні фічі (Generation, HexResources, Pathfinding), у Presentation — на рівні під-області й UI. Спільні Components/ на рівні домену (Actions, Actors). Дві назви однієї ролі: Utils/ (Map/Generation, Map/Hex) і Helpers/. Папки поза блоком: Textures/ (Presentation), вкладені підобласті з кодом без ролі (Terrain/Isolines, Terrain/Smooth, Terrain/CurveBuilders), вкладені вікна UI/MainHud/<Panel>/, Modules Core/Implementation/States. Placement :di-feature \"<Feature>/Installer/\" розходиться з доменним Installer/"
  :verified-by "find Assets/Domains Presentation Modules -type d; find -name *.asmdef; ls вибраних папок"
  :consequence "блок треба переписати під шари; кілька розбіжностей — рішення власника (Installer рівень, Utils vs Helpers)"}

 {:finding :architecture-step-3-validation
  :at "2026-09-14"
  :fact "Усі 15 пунктів :architecture-apply-set застосовані; усі класи з shared-kernel і boot-flow існують; 15 файлів рецептів існують; стек збігається з Packages/manifest.json і Assets/Packages. Розбіжності з репо: (1) :domain->domain коментар «Map/Economy (leaves) → Actors → Actions» — насправді листки Map і Kernel, Economy → Map+Kernel, Actors → Economy+Kernel, Actions → усі чотири; Kernel не названий; (2) :domain->module «e.g. AxialSystem, CurveBuilders» — CurveBuilders жоден домен не посилає, посилають AxialSystem, Turn, Boot.Core, Addressables.Core; (3) напрям module → domain/presentation (Boot.Implementation, UserInput) у depends не описаний; (4) «три шари» без Assets/Scripts (Core, EcsExtensions, Installers) — place не має куди класти новий shared-kernel код; «config providers» — такого модуля нема; (5) folder-layout дає рівень асемблі тільки доменам: Presentation/Archetypes, Presentation/UI/Archetypes, Presentation/UI/Tags, Installer у Presentation на рівні області, Modules/UserInput/Archetypes не описані; place не каже, куди інсталер презентації чи модуля. Суперечності: (6) System Taxonomy (п. 9, не чіпали) досі каже storages.Add<T> — проти :storage-target (a); (7) role-invariants :naming/:type-name дублює ECS_CONVENTIONS → Naming & Construction, рішення нема. Нотація: (8) :assembly-level #{…} означає всі три папки, а за каноном множина в не-колекційному полі читається «одне з». Не записано як задачу: (9) рефакторинг репо під нові правила (Map/Hex/Utils, Map/Generation/Utils → Helpers; Installer у Map/Generation|HexResources|Pathfinding; Presentation/Terrain/Isolines|Smooth|CurveBuilders → Helpers)"
  :verified-by "прочитав ARCHITECTURE.md цілком і git diff; grep декларацій кожного класу по Assets; references усіх .asmdef у Domains/Presentation/Modules/Scripts; find role-папок по шарах; Boot.cs і AppState.cs; doc_lint --scope ARCHITECTURE.md = 0 ghosts; gen_index LINT clean"
  :consequence "крок 3 не готовий до прийняття: (1)-(5) — неправда або прогалини в тексті, (6)-(7) — рішення власника, (9) — окрема задача рефакторингу"}

 {:finding :architecture-merge-validation
  :at "2026-09-15"
  :fact "Вірне: 17 класів shared kernel існують і :use збігається з сигнатурами; 15 рецептів існують; усі «ARCHITECTURE → <розділ>» у Patterns/ і CODE_STORY_RULES ведуть на наявні розділи; RemoveComponent/RemoveTag і component-change observers — 0; presentation не пише доменні таблиці (ecs-graph); FK-назви за правилом; команди tag-law-check працюють; doc_lint --scope 0/0. ПРОГАЛИНИ ПРОТИ РЕПО: (1) asmdef Assets/Flows/DistrictBuild (з FM-10; одна подія DistrictBuildUIRequestedEvent, яку і шле, і споживає Presentation.UI) — поза шарами; (2) Assets/Scripts/Utils (3 класи), Spawner (2), Extentions (1) — без asmdef, 0 використань, у шарах їх нема; (3) depends мовчить про shared-kernel → module (Ecs.Extensions → Boot.Core, Addressables.Core), presentation → module, module → module; module → layer реально лише Boot.Implementation і UserInput; (4) :tiers не можна застосувати — нема правила віднесення домену до ярусу, asmdef_reach відповідає на can/path/refs, яруси не перевіряє; (5) <Module>/Installer/ і <Module>/Archetypes/ для пари Core+Implementation лежать поза обома asmdef → Assembly-CSharp, недосяжна для Installers.World; реально інсталер Addressable — окремий asmdef у app-root; (6) WorldInstaller сам реєструє EventCleanupSystem, HexSelectionSystem, CameraMovementSystem, ConfigLoader<CameraMovementConfig>, MainCanvasProvider і створює PlayerInput голим world.CreateEntity; порядок InstallModules (UI другим, Actors перед Economy) проти :install-order, а Boot.ExecuteStartupStep виконує startup-системи в порядку реєстрації; у shared kernel нема місця для інсталера; (7) код у корені асемблі або в папці без ролі: 5 з 10 модулів (Addressable, Boot + States/, MainCanvas, CurveBuilders, AxialSystem) і Domains/Map/Pathfinding — нема в списку рефакторингу. СУПЕРЕЧНОСТІ: (8) role-folders :where дозволяє Installer/ у фічі, place :installer забороняє; Archetypes/ нема в role-folders; :shared-role суперечить обов'язковим Archetypes/ + Installer/; (9) :reactive :event :never coordinates і :consumer :never reading data — проти PATTERN_EVENT.md:48 :payload-tolerated; 3 з 12 подій несуть дані з коментарем-посиланням на цей допуск; «payload» означає і сам компонент події, і дані; (10) :tag-law :entity «identity tag = discriminator» проти :category-tag (UITag на 4 архетипах, ідентичність у …ViewComponent); UITag — жива назва в правилі; (11) recipe-tree :step-3 кладе рецепти в :read постановки, а CLAUDE.md § 2 :read-when читає :read (разом з ARCHITECTURE.md) лише після підтвердження; (12) у recipe-tree вкладені cond без :else: turn phase (TurnPhaseSubSystem, 3 реалізації) і startup-крок не з конфігу не дають листка; ORCHESTRATOR_SUBSYSTEM досяжний лише для map creation, хоча його тригер загальний; shared-kernel веде turn phase на IPrioritizedUniTaskSystem замість TurnPhaseSubSystem; (13) EventArchetypes і SingletonArchetypes поза правилом «<Assembly>Archetypes у Archetypes/»; <Assembly> не визначено (EconomyArchetypes проти PresentationUIArchetypes). НОТАЦІЯ: (14) native-allocator :where #{main-thread inside-a-job} — :where кон'юнктивне в каноні, читається «обидва»; (15) правило про Views — прозою поза формою. ЗОВНІШНЄ: Temp на кадр головного потоку і на job, TempJob 4 кадри, Friflo arity 5, TryGetEntityById, IIndexedComponent — підтверджено; ThreadPool-leak, Overflow counters, bucket-cap 100 — у документації не знайдено; документація: ручне звільнення Temp «has no effect» — причину :dispose не підтверджено. КОД (документ правий): TurnPhaseSubSystem.IsEnabled { get; set; } — змінний стан у базі системи"
  :verified-by "прочитав ARCHITECTURE.md цілком; граф references усіх asmdef (Python); grep декларацій і сигнатур класів shared kernel; WorldInstaller.cs, Boot.cs, GameplayState/MapCreationState OrderBy; інвентар папок з .cs проти role-folders; ecsg.py systems/explain/tags; поля подій (Python); PATTERN_EVENT.md; CLAUDE.md § 2; Friflo.Engine.ECS.xml 3.6.0; context7 Unity Collections 2.6 allocator-overview; doc_lint"
  :consequence "крок 3 (злиття) не готовий до прийняття: (8) (10) (13)-(15) — правки формулювань; (1)-(7), (9), (11), (12) — рішення власника; відхилення коду — у список рефакторингу"}]
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
  :reason "людський підручник, зміст якого несе канон нотації"}

 {:decision :architecture-stack-and-why
  :status :confirmed
  :at "2026-09-14"
  :value "з ARCHITECTURE.md: рядок ECS storage прибрати зі Stack (не стек і застарілий за наміром власника); абзац «Why this shape» видалити; посилання «see GENERAL_UI_STYLE.md §15» видалити"
  :verified-by "власник: «ECS storage … це вже не відповідає дійсності. Всі конфіги та сінглтони будуть звичайними обʼєктами. Більше того це точно не відноситься до технологічного стеку»; «Абсолютно не впевнений що ШІ потрібні пояснення як: **Why this shape.** …»; «GENERAL_UI_STYLE.md §15 це теж застаріло … це не пряма інструкція прочитати, а лише нагадування яке буде проігноровано»"
  :reason "не стек; пояснення не керує агентом; посилання-нагадування на видалений документ"}

 {:decision :storage-target
  :status :confirmed
  :at "2026-09-14"
  :value "(a) з ARCHITECTURE.md прибрати всі згадки сховища (Stack ECS storage, DI :storage-registry/:storage-members, EntityStorages/SingletonComponents у shared kernel); правило запишеться задачею рефакторингу; значення «звичайних обʼєктів» у цьому ревʼю не з'ясовується"
  :verified-by "власник: «1 - ігноруй 2 - а»"
  :reason "документ не описує ні застарілий стан, ні ще не збудовану ціль"}

 {:decision :folder-layout-rules
  :status :confirmed
  :at "2026-09-14"
  :value {:installer "у доменах — лише на рівні домену; Map/Generation|HexResources|Pathfinding/Installer — відхилення"
          :helpers "одна назва Helpers/; Map/Generation/Utils і Map/Hex/Utils перейменувати"
          :roleless-subfolders "Terrain/Isolines, Terrain/Smooth, Terrain/CurveBuilders — це Helpers/, яких ще не було; кодових підпапок без ролі не буває"}
  :verified-by "власник: «1 - а 2 - Helpers але тоді Utils для map переназвемо 3 - це Helpers яких просто до цього ще не було створено»"
  :reason "Feature folder layout переписується під шари Domain / Presentation / Module"}

 {:decision :system-taxonomy-to-flow
  :status :confirmed
  :at "2026-09-14"
  :value "таблицю System Taxonomy прибрати; замість неї — Clojure-алгоритм вибору ролі системи (форма — на обговоренні)"
  :verified-by "власник: «System Taxonomy не вважаю цей блок потрібним. Він не записаний в clojure формі і замість нього має бути clojure flow, бо ця таблиця є предком, а також ерзац алгоритмом»"
  :reason "таблиця перелічує ролі, а агенту потрібен порядок рішення"}

 {:decision :turn-pipeline-and-recipes
  :status :confirmed
  :at "2026-09-14"
  :value "абзац Turn pipeline видалити з ARCHITECTURE.md; Pattern Recipes переписати як Clojure flow з явними тригерами активації; алгоритм system-role (cond) — відкинуто"
  :verified-by "власник: «Turn pipeline - це частина ігрової механіки а не загальний опис архітектури. Pattern Recipes - треба розписати як clojure flow зі зрозумілими трігерами для активації. те що ти запропонував - не бачу як це щось змінює на краще»"
  :reason "ігрова механіка — не архітектура; вибір рецепта має бути процесом"}

 {:decision :architecture-apply-set
  :status :confirmed
  :at "2026-09-14"
  :value {:1-frontmatter :keep-trigger
          :2-header "видалити банер PERMISSION-GATED і абзац Style/notation"
          :3-stack "без ECS storage і GENERAL_UI_STYLE; UI Toolkit + Unity App UI лишається"
          :4-layers "без «Why this shape»"
          :5-shared-kernel "ростер прибрати; лишити тригери: умова → конкретний клас, який читати"
          :6-placement ":di-domain"
          :7-folders "folder-layout + role-folders"
          :8-di "без :storage-registry / :storage-members"
          :9-system-taxonomy "поки не чіпати — власник думає, чим замінити"
          :10-turn-pipeline :delete
          :11-laws "прибрати дати, переформулювати"
          :12-flow-contract "прибрати — врегульовано sdd-flow"
          :13-boot "прибрати посилання на BOOT.md"
          :14-code-shape :keep
          :15-pattern-recipes "Clojure flow з when-тригерами, +VIEW_SYSTEM, POLYMORPHIC_CATALOGUE, ADDRESSABLE_PATTERNS"}
  :verified-by "власник, відповідь по пунктах 1-15"
  :reason "підсумок кроку 3; власник перечитає і повернеться"}

 {:decision :architecture-reread-verdicts
  :status :confirmed
  :supersedes :architecture-apply-set
  :at "2026-09-14"
  :value {:rules-in-comments "Layers, Placement, Tag Law — правило, яке несе ;; коментар, переїжджає у значення форми; переписати зараз"
          :yaml-style "Shared kernel і Pattern Recipes — рядки умова→файл, а не Clojure; форма — на обговоренні"
          :role-invariants :delete
          :cross-domain-and-boot "кандидати на s1 + s2 артефакти; доля в ARCHITECTURE.md — ?"}
  :verified-by "власник: «Layers - треба переписати зараз важлива інформація по суті зберігається як clojure коментарі»; «Shared kernel … Pattern Recipes - написано в стилі YAML а не clojure»; «Role invariants - … можна видалити»; «Cross-domain behavior - теж кандидат на s1 + s2 артефакт як і Boot flow»"
  :reason "було: п. 5 і 15 застосовано у формі when-рядків, п. 11 — з правилами в коментарях; нове: власник перечитав і відкинув цю форму"}

 {:decision :architecture-draft-2-verdicts
  :status :confirmed
  :supersedes :architecture-reread-verdicts
  :at "2026-09-14"
  :value {:no-live-names "у правилах ARCHITECTURE.md — жодних переліків реальних модулів / доменів / класів там, де зміна коду мала б тягнути зміну документа (:module :holds, :tiers)"
          :presentation "перелік того, що тримає, — тавтологія; потрібен :never"
          :why-fields "видалити; натомість — інструкція, як перевіряти теги через ecs-graph"
          :module->domain "поки дозволено всім модулям; власник перевірить — очікує зворотного напряму"
          :installers "кожен інсталер — у власній папці Installer/, у всіх шарах; рівень для presentation / module — ?"
          :cross-domain-and-boot :delete
          :shared-kernel-and-recipes "дерево cond"}
  :verified-by "власник: «presentation якась тафтологія … немає блоку :never»; «:module :holds містить перелік модулів … зміна в коді має відобразитися на зміні в документі»; «:tiers … знову ж посилання на реальні назви»; «1 - пояснення можеш видалити, але інструкцію як шукати по тегах через ecs-graph tools - потрібно написати 2 - давай зараз дозволимо всім модулям … 3 - для всіх інсталерів має бути окрема папка … 4 - видаляй 5 - зроби деревом»"
  :reason "було: чернетка 1 з реальними назвами й відкритими питаннями 1-5; нове: відповіді власника"}

 {:decision :architecture-draft-3-go
  :status :confirmed
  :supersedes :architecture-draft-2-verdicts
  :at "2026-09-14"
  :value {:folder-layout "переписати: правила у значення, рівень асемблі для всіх шарів"
          :di-composition "переписати: правила у значення, без переліку живих назв"
          :system-taxonomy :delete
          :known-deviations "{:record … :never …} + Extentions як «не виправляти»"
          :no-live-names-rule "записати в DOC_STANDARD на його кроці"
          :index-description :update
          :installer-level "рівень асемблі в усіх шарах: <Domain>/Installer/, Presentation/Installer/, Presentation/UI/Installer/, <Module>/Installer/"
          :shared-kernel "блок лишається; для кожного класу — тригер активації і як використовувати, деревом"
          :presentation-never "«пряма зміна таблиці домену — лише командний імпульс» записати"
          :language "англійська — мова документа; чернетки обговорення були українською"}
  :verified-by "власник: «1 - так 2 - так 3 - видаляй 4 - ок 5 - так 6 - так go»; AskUserQuestion: «Рівень асемблі», «прибирати не треба, треба для кожного з класів написати правильний тригер активації і як використовувати», «Записати»"
  :reason "було: відкриті питання чернетки 2; нове: відповіді й go"}

 {:decision :merge-ecs-conventions
  :status :confirmed
  :at "2026-09-14"
  :value "ECS_CONVENTIONS.md зливається в один документ з ARCHITECTURE.md; кожне правило — один раз; живі назви, приклади, історія — не переносяться"
  :verified-by "власник: «Поясни який сенс в поділу? Цей документ для кодинг агента, тобто не бачу сенса в тому чи він буде завантажений 1 командою чи 2» → «зливаємо в 1 документ»"
  :reason "обидва документи завжди читає один агент; поділ породив дублі (Tag Law, key-role-law, архетипи в папках), що вже розійшлися; вибіркове перенесення за критерієм «закон дизайну / механіка коду» відкинуто"}

 {:decision :merge-answers
  :status :confirmed
  :at "2026-09-14"
  :value {:name ARCHITECTURE.md
          :code-comments "12 посилань на ECS_CONVENTIONS у коментарях Assets/ поки не чіпати"
          :allocation-rule (cond (runs-once-or-a-few-times? system) "дозволено звичайні System.Collections.Generic"
                                 (runs-repeatedly? system)          "zero-allocation на NativeCollections — кожен кадр, циклічно, регулярно, постійно")}
  :verified-by "власник: «1 - так»; «системам що працюють 1 чи буквально кілька разів тепер дозволено використовувати звичайні System.Collections. А для систем що будуть працювати: що кадру, циклічно, регулярно, постійно - для них правило на zero-allocation з використанням NativeCollections буде актуальним»; «3 - покищо посрати на коментарі»"
  :reason "було: Ban 2 — жодних System.Collections.Generic у будь-якій системі + zero-allocation-визначення 2026-09-07; нове: власник звузив бан до систем, що працюють повторювано"}

 {:decision :merge-go
  :status :confirmed
  :at "2026-09-14"
  :value {:async-allocator "для кожної async-системи тип алокації обирається окремо; Allocator.Temp — ніколи (прив'язаний до потоку, а async може виконуватись поза головним); TempJob — лише якщо робота вкладається в його 4 кадри"
          :arch-check-and-memory "оновити тим самим проходом"
          :merge :go}
  :verified-by "власник: «Для кожної async системи ми будемо окремо вибирати тип Allocation. Бо він привʼязаний до головного потоку, а ці системи можуть виконуватися й поза-ним, тому для таких Temp точно не підходить, можливо навіть Jobs не буде підходити бо він лише на 4 фрейми 1 - тим самим проходом 2 - go»"
  :reason "go на злиття за структурою з відповіді 2026-09-14"}

 {:decision :skills-split-answers
  :status :confirmed
  :at "2026-09-15"
  :value {:order "спершу відповіді на 15 питань валідації, потім розбиття"
          :home ".claude/skills/ у репо"
          :names #{fantasymayor-pattern-choice fantasymayor-placement}
          :call-point "окреме поле для скілів у Clojure-постановці (назва — :by-proposal)"
          :verification "усі перевірки коду (tag-law-check, arch-check, roslyn diagnostics) — процедурою в CLAUDE.md; з project.md # Meters прибираються"
          :tag-law-before "перша половина tag-law-check — у скіл вибору патерна"}
  :supersedes :statement-fields
  :verified-by "власник: «1 - ок, задавай свої 15 запитань 2 - в проєкті бо вони валідні лише для нього 3 - ок 4 - для скілів може бути окреме поле в clojure нотації 5 - перенось це в процес в Claude.md бо там в нас clojure flow 6 - а»"
  :reason "було (:statement-fields): :accept з project.md # Meters; нове: перевірка коду — процес у CLAUDE.md, скіли — окреме поле постановки"}

 {:decision :merge-validation-answers
  :status :confirmed
  :at "2026-09-15"
  :value {:wording-1-5 "застосувати: Archetypes/ і Installer/ — лише рівень асемблі; tag-law — один тег на архетип, для категорійного тегу дискримінатор у …ViewComponent, без назви UITag; shared kernel і app-root звільнені від правила holder-а, <Assembly> = назва asmdef без крапок і без Domains.; native-allocator :where — дві гілки; Views — гілка system-collections"
          :flows-asmdef "подію DistrictBuildUIRequestedEvent → Presentation.UI, asmdef Flows.DistrictBuild видалити — рефакторинг"
          :dead-scripts "Scripts/Utils, Spawner, Extentions — рефакторинг на видалення; запис про Extentions з deviations прибрати"
          :depends {:module "в ідеалі без залежностей, pure-логіка; може залежати від інших модулів"
                    :domain->module "скільки завгодно модулів"
                    :shared-kernel "ні від чого, крім зовнішніх бібліотек і рушія"}
          :tiers :needs-explanation
          :pair-module-installer "Assets/Scripts/Installers/<Module>/ з власним asmdef"
          :root-registrations "core-логіка рівня рушія (камера, ввід, UI-корінь, очистка подій) реєструється коренем і створюється першою"
          :event-values "значення в подіях дозволені"
          :recipe-holes "turn phase → ORCHESTRATOR_SUBSYSTEM; решта непокритого — явне «no recipe»; наявні реалізації патерна — не списком у документі, а скілом або скриптом із коду"
          :module-role-folders "рольові папки всередині Core/ і Implementation/; решта — рефакторинг"
          :skills-field :skills
          :allocator-lifetime "Temp — алокація на 1 кадр, TempJob — на 4 кадри; довше 4 кадрів — Persistent або System.Collections; загальна рекомендація, не помилка"
          :turn-phase-is-enabled "у рефакторинг"}
  :verified-by "власник, відповіді по пунктах 1-18; ліміт 100 записів на значення ключа ComponentIndex — context7 friflo/ecs-wiki docs/component-index.md"
  :reason "відповіді на :architecture-merge-validation; 9 — чекає пояснення; 13 знято скілом вибору патерна"}

 {:decision :merge-validation-follow-ups
  :status :confirmed
  :at "2026-09-15"
  :value {:tiers "лишити substrate / agents / verbs; віднесення — питання «чи знає домен конкретного власника?», «чи описує дію власника?»; перевірка напряму — asmdef_reach refs; циклів не буває"
          :presentation->module "скільки завгодно модулів"
          :module->shared-kernel :allowed
          :shared-kernel->module "рефакторинг: IUniTaskSystem : IAppStateSystem (Boot.Core), ConfigLoaderSystem → IAddressable (Addressables.Core)"
          :boot "Boot.Implementation — окремий виняток поза будь-яким правилом залежностей"
          :player-input "голий world.CreateEntity замість UserInputArchetypes.PlayerInput — рефакторинг"
          :install-order "правило «спершу залежності» лишається; порядок InstallModules — рефакторинг"
          :event-values "значення — повноцінні дані; споживач діє на них напряму; reconcile — один із варіантів"
          :patterns-and-memory "PATTERN_EVENT, PATTERN_REACTIVE_SYSTEM і пам'ять оновити тим самим проходом"
          :pattern-instances "розширити наявний інструмент (ecs-graph) під пошук реалізацій патерна"
          :allocator "порушення алокатора — помилки, не рекомендації"}
  :verified-by "власник: «9 - а, нам цикли не потрібні 19 - так 20 - а 21 - так 22 - це окреме виключення яке поза будь-яким правилом залежностей. 23 - додавай 24 - а 25 - б 26 - так, онови 27 - отже треба розширити наявний інструмент під нові вимоги 28 - це помилки а не рекомендації»"
  :reason "уточнення, відкриті відповідями 8, 11, 12, 14, 17"}

 {:decision :skills-split-go
  :status :confirmed
  :at "2026-09-15"
  :value {:allocator-errors "усе — помилки: Temp довше кадру, TempJob довше 4 кадрів, Temp в async; arch-check ловить те, що видно статично"
          :ecs-graph-extension "окрема задача одразу після цієї; поки скіл вибору патерна кличе ecsg.py systems --role і roslyn"
          :code-verification ["roslyn get_diagnostics на змінених файлах" "/arch-check на зміненій області" "ecsg.py tags — tag law" "перевірка власника в Unity — остаточна"]
          :graph-stats "ecsg.py stats і dig.py stats лишаються в project.md # Meters"
          :map :go}
  :verified-by "власник: «29 - а 30 - а 31 - мені подобається go»"
  :reason "go на мапу :architecture-skills-split"}

 {:decision :skills-split-noticed
  :status :confirmed
  :at "2026-09-15"
  :value {:doc-standard-genre "DOC_STANDARD Rule 1 — жанр :project-skill для .claude/skills/fantasymayor-*/SKILL.md"
          :turn-phase-is-enabled "лишається в рефакторингу, попри IsEnabled на підсистемах у PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM"
          :structural-in-update "PATTERN_REACTIVE_SYSTEM і PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM: структурні зміни в Update безпечні (база знімає id), заборонені лише у власному переборі системи; те саме уточнення в :use UpdatedSystem скіла pattern-choice"
          :global-claude-md "опис arch-check у ~/.claude/CLAUDE.md — під помилки часу життя алокатора"}
  :verified-by "власник: «1 - так 2 - лишаємо 3 - виправ 4 - онови»"
  :reason "чотири речі, помічені під час виконання :skills-split-go"}

 {:decision :code-comments-delete
  :status :confirmed
  :supersedes :merge-answers
  :at "2026-09-15"
  :value "коментарі в Assets/*.cs, що згадують ECS_CONVENTIONS (12) або payload-less (8), видалити скрізь; архів не чіпати; вузол ECONOMY_ACTORS.canvas — поза цим (canvas — :ask-first, дозволу не було)"
  :verified-by "власник: «1 - видаляй ці коментарі скрізь 2 - так»; grep по Assets/*.cs 2026-09-15 — 12 і 8 файлів"
  :reason "було (:merge-answers :code-comments): коментарі поки не чіпати; нове: власник помітив, що видалений документ «ще звідкись лізе», і наказав видалити"}

 {:decision :comment-granularity
  :status :open
  :at "2026-09-15"
  :value ?}

 {:decision :evaluator-command-buffer
  :status :open
  :at "2026-09-15"
  :value ?
  :verified-by "власник: «3 - command buffer instead» — відповідь на питання, чия передумова спростована (# Disproven)"}

 {:decision :review-stays-active
  :status :confirmed
  :at "2026-09-15"
  :value "DOC_AGENT_REVIEW лишається активним паралельно із задачею розширення ecs-graph"
  :verified-by "власник: «9 - поки що залиш»"
  :reason "власник не закриває ревʼю"}]
```

# Disproven

```clojure
[{:hypothesis "DistrictOpenConditionEvaluatorSystem пише в store поза головним потоком (виведено з його коментаря «legal off-thread under Law 1»)"
  :refuted-by "TurnProcessorSystem.RunTurnAsync викликає TurnPhaseRunner.RunAsync без перемикання потоку; у Modules/Turn і Domains/Economy/DistrictOpenCondition нема SwitchToThreadPool / SwitchToMainThread / Task.Run — фази й Evaluate() виконуються на головному потоці; коментар хибний, код store-thread не порушує"
  :at "2026-09-15"
  :details "# Decisions → :evaluator-command-buffer"}]
```

# Attempted

```clojure
[{:approach "правити секції цього FLOW python-скриптом через s.index('# Progress')"
  :confidence 90
  :dropped-because "перший збіг '# Progress' стояв усередині тексту знахідки :local-patches-have-canon-home — зріз видалив дві знахідки, усі рішення, Disproven і Attempted; пошкоджена версія пішла в коміт e48cf34"
  :problems "виявлено при наступному записі; зміст відновлено з записів сесії 2026-09-14; секції правити лише Edit-ом по унікальному тексту"
  :at "2026-09-14"}

 {:approach "замінити таблицю System Taxonomy на (def system-role (cond …)) — ознака задачі → роль → базовий тип → рецепт, плюс скорочені role-invariants"
  :confidence 60
  :dropped-because "власник: «не бачу як це щось змінює на краще»"
  :problems "той самий перелік ролей у формі cond; дублював таблицю Pattern Recipes замість процесу вибору рецепта"
  :at "2026-09-14"}]
```

# Progress

```clojure
{:status :closed-by-owner
 :closed {:at "2026-09-15"
          :raw "Давай закриємо цей flow бо він вже настільки застарів що неможливо зрозуміти що треба робити."
          :outcome "кроки 1-3 прийняті й закомічені; :remaining, поправка про коментарі і відкриті рішення :comment-granularity / :evaluator-command-buffer не виконані й не переносяться — закрито словом власника, не за приймальними метрами"
          :carried "оновлення PATTERN_TRANSACTION_ENTITY.md до сучасного стандарту — окремою задачею"}
 :completed #{"step-1 CLAUDE.md — застосовано, прийнято власником («приймаю, комітимо») і закомічено 2026-09-14"
              "step-3 ARCHITECTURE.md — злиття з ECS_CONVENTIONS, валідація, розбиття на скіли fantasymayor-placement і fantasymayor-pattern-choice; прийнято власником («приймаю, комітимо») і закомічено 2026-09-15"}
 :current :none
 :step-3-merge "2026-09-14: застосовано за :merge-go, не закомічено — ECS_CONVENTIONS.md злитий в ARCHITECTURE.md (392 рядки: + Systems, Entities, Events, Threading, naming / output-methods / fail-loud у Code shape) і видалений через vault_delete; State Storage, живі переліки, приклади, історія, Open Directions не перенесені; посилання перенаправлені: 12 Patterns, CLAUDE.md, project.md, DOC_STANDARD, GAME_MECHANICS, CODE_STORY_RULES, скіл arch-check (+ класифікація cadence, Allocator.Temp в async), ~/.claude/CLAUDE.md, 12 файлів пам'яті; поза архівом і цим FLOW згадок 0"
 :step-3 "застосовано 2026-09-14 за :architecture-apply-set, не закомічено: ARCHITECTURE.md 225 → 212 рядків; System Taxonomy не чіпали (п. 9); додано перший рядок-опис для INDEX; власник перечитає і повернеться"
 :step-2 "прийнято («добре») і закомічено 2026-09-14 разом з видаленням CLOJURE_GUIDE.md: FLOW_TEMPLATE.md і RESEARCH_TEMPLATE.md видалені; DOC_STANDARD Rule 2 (2a-2e) → один блок-вказівник на канон, Category A рядок і чекліст скорочені; gen_index.py — перевірка заголовків трьох стадій видалена назавжди, FLOW-папка визнається, архів не перевіряється на форму й биті посилання; project.md # Shape → канон; CLOJURE_GUIDE.md:535 → канонічний шаблон; відновлення FLOW після інциденту теж чекає коміту"
 :step-3-validation "2026-09-14: провалідовано за :architecture-step-3-validation"
 :step-3-rewrite "2026-09-14: застосовано за :architecture-draft-3-go, не закомічено — ARCHITECTURE.md 212 → 219 рядків; видалено System Taxonomy + role-invariants, Cross-domain behavior, Boot flow; Layers/depends/Placement/Folder layout/DI/Tag Law/Deviations — правила у значеннях, 0 ;; коментарів; Shared kernel і Pattern Recipes — дерева cond; tag-law-check через ecsg.py; чекає прочитання власником"
 :step-3-merge-validation "2026-09-15: провалідовано за :architecture-merge-validation; відповіді — :merge-validation-answers, :merge-validation-follow-ups"
 :step-3-skills-split "2026-09-15: застосовано за :skills-split-go, не закомічено — нові скіли .claude/skills/fantasymayor-placement (Layers, depends, Placement, Folder layout, DI composition) і fantasymayor-pattern-choice (дерева рецептів і shared kernel, instances, tag-law-before); ARCHITECTURE.md 392 → 228 рядків — лише закони, виправлені за відповідями (tag-law, archetype-law, system-collections + Views, native-allocator за часом життя, reactive / event-lifecycle зі значеннями, deviations без Extentions); CLAUDE.md § 2 — поле :skills і (def code-verification); project.md # Meters без roslyn і Unity-перевірки + вказівник; PATTERN_EVENT, REACTIVE_SYSTEM, REACTIVE_ORCHESTRATOR_SYSTEM, TRANSACTION_ENTITY, PIPELINE_STAGE; CODE_STORY_RULES:576; arch-check; INDEX agent zone; пам'ять (reactive, zero-allocation, use-pattern-recipes, MEMORY.md). PATTERN_POLYMORPHIC_CATALOGUE не чіпав — його «payload» це параметри виду, не події"
 :remaining #{"12 коментарів у Assets/*.cs посилаються на видалений ECS_CONVENTIONS.md — власник: поки не чіпати"
              "arch-check: Scope resolution сканує лише Assets/Modules і Assets/Scripts (без Domains/Presentation); згадує .claude/SEARCH_POLICY.md і arch-scout"
              "рефакторинг репо під ARCHITECTURE.md: Map/Hex/Utils і Map/Generation/Utils → Helpers; Installer/ на рівень асемблі — Map/Generation|HexResources|Pathfinding, Presentation/Terrain|HexResources|Districts|HexIcons; Presentation/Terrain/Isolines|Smooth|CurveBuilders → Helpers; Domains/Actions/Systems на рівні домену (не спільна роль); Assets/Flows/DistrictBuild: подія → Presentation.UI, asmdef видалити; видалити Assets/Scripts/Utils, Spawner, Extentions; рольові папки всередині Core/ і Implementation/ у Addressable, Boot (+ States/), MainCanvas, у корені CurveBuilders і AxialSystem, у Domains/Map/Pathfinding; TurnPhaseSubSystem.IsEnabled — змінний стан у системі; shared kernel → модулі: IUniTaskSystem : IAppStateSystem (Boot.Core), ConfigLoaderSystem → IAddressable (Addressables.Core); UserInput → Domains.Map, Presentation; WorldInstaller: PlayerInput через UserInputArchetypes.PlayerInput, порядок InstallModules «спершу залежності»"
              "DOC_STANDARD: правило «жодних живих назв у правилах документа»"
              "PATTERN_CONFIG досі описує EntityStorages — чекає задачі рефакторингу сховища"
              "задача паралельно з цією (:review-stays-active): розширити ecs-graph — реалізації патерна, відрізнити реактивні системи від per-frame (ребра reacts_to); відповіді 2026-09-15: усі 15 рецептів, зв'язок рецепт → ознака в коді інструмента, родини — ребро inherits в ecs-graph і roslyn обидва, база закомічена ~/.claude af626ef; відкрито: форма запиту; свій FLOW — після підтвердження постановки"
              "відкрито: PATTERN_TRANSACTION_ENTITY.md:94 — :flow-contract Flows/FLOW_<NAME>.md (стара форма); в ARCHITECTURE.md прибрано" "відкрито: DOC_STANDARD згадує видалений GENERAL_UI_STYLE.md" "3 привиди HexIdComponent: PATTERN_COMPONENT.md:42, PATTERN_TRANSACTION_ENTITY.md:51 і :72"}
 :resume-context "Кроки 1-2 закомічені 2026-09-14, крок 3 — 2026-09-15. Крок 3 (ARCHITECTURE.md): злиття з ECS_CONVENTIONS, валідація, розбиття на два проєктні скіли (.claude/skills/fantasymayor-*) з місцями виклику в CLAUDE.md § 2 і процедурою code-verification. Наступний крок називає власник. Далі: окрема задача розширення ecs-graph; рефакторинг коду з :remaining — окремо."}
```

# Acceptance

```clojure
[{:meter "python3 Tools/gen_index.py"
  :target "LINT: clean"
  :actual "LINT: clean (44 docs, 2 always) — при закритті 2026-09-15"
  :status :met}
 {:meter "python3 Tools/doc_lint.py --quiet"
  :target "0 Clojure syntax errors; ghosts не зросли"
  :actual "3 ghosts, 0 syntax errors, 41 md — при закритті 2026-09-15 (привиди старі, HexIdComponent)"
  :status :met}
 {:meter "grep «(def <name>» по ARCHITECTURE.md і двох SKILL.md"
  :target "кожне визначення рівно один раз"
  :actual "40 визначень, дублів 0 — 2026-09-15"
  :status :met}
 {:meter "grep -i «payload-less» поза Flows/Archive"
  :target "0"
  :actual "0 у документах і скілах; 3 рядки в пам'яті описують відставку правила як історію — 2026-09-15"
  :status :met}
 {:meter "власник"
  :target "кожен крок прийнятий словом"
  :actual "кроки 1-2 прийнято 2026-09-14, крок 3 — 2026-09-15; далі FLOW закрито власником як застарілий"
  :status :not-met}]
```

# Amendments

```clojure
[{:received-at "2026-09-15"
  :raw-request ["А якщо спробувати зробити з певної частини файлу Architecture окремий скіл під цей проєкт. Умовно навіщо постійно читати весь документ якщо можна порозносити по скілах багато блоків. Умовно для роботи над файловими каталогами проєкту свій скіл, для пошуку по проєкту - свій окремий, для визначення патерну - теж окремий скіл і так далі. Що скажеш на це?"
                "Але кожен скіл можна буде підтримувати окремо. \nОт прочитавши весь файл, скільки б скілів ти виділив з нього як корисні і що б залишив?\nБо наприклад: коли ми пишемо код в проєкті нам важливо підібрати патерн, і якщо це буде лише рекомендація в документі то агент це може або виконати, або ні. Але якщо це буде скіл то вірогідність що він буде виконаний набагато вища"
                "якщо tag-law-check настільки важливий мін має бути в Claude.md як крок до перевірки коду, як частина нашого процесу.\nЯ згоден з твоєю декомпозицією по скілах, давай рухатися в цьому напрямку"]
  :normalized {:task :architecture-skills-split
               :goal "рішення, що ухвалюються в певний момент, винести з ARCHITECTURE.md у проєктні скіли з фіксованим місцем виклику в CLAUDE.md § 2; закони лишити в документі; перевірку законів зробити кроком процесу в CLAUDE.md"
               :path :direct
               :decided #{"скіл вибору патерна: Shared kernel + Pattern Recipes"
                          "скіл розміщення: Layers, depends, Placement, Folder layout, DI composition"
                          "в ARCHITECTURE.md лишаються Stack, Systems, Entities, Events, Threading, Code shape, Known deviations"
                          "tag-law-check — крок перевірки коду в CLAUDE.md"
                          "кожне правило в одному місці; скіл = процедура + посилання на закон"}
               :open #{}}
  :confirmed true}                      ;; 2026-09-15: відкриті питання закриті рішеннями :skills-split-answers … :skills-split-go

 {:received-at "2026-09-15"
  :raw-request ["ECS_CONVENTIONS - ще звідкись лізе хоча файл видалено\nтреба розширити ecs-graph для додаткового пошуку по патернах"
                "1 - видаляй ці коментарі скрізь\n2 - так\n3 - command buffer instead\n4 - все\n5 - не зрозумів запитання\n6 - в коді інстурмента\n7 - обоє\n8 - спершу коміть, без різниці в яку гілку\n9 - поки що залиш"]
  :normalized {:task :delete-stale-code-comments
               :goal "у коді не лишається коментарів, що посилаються на видалений ECS_CONVENTIONS.md або на скасоване правило payload-less"
               :path :direct
               :where "Assets/*.cs — лише коментарі"   ;; розширення :off-limits Assets/ цього контракту
               :off-limits #{"код поза коментарями" Flows/Archive/ ECONOMY_ACTORS.canvas}
               :decided #{":code-comments-delete"}
               :open #{":comment-granularity" ":evaluator-command-buffer"}
               :result "grep ECS_CONVENTIONS і payload-less по Assets/*.cs = 0"}
  :confirmed false}                     ;; 2026-09-15: не підтверджена й не виконана — FLOW закрито власником
 ;; 2026-09-15: відповіді 4, 6, 7, 8 належать задачі ecs-graph — у її FLOW після підтвердження постановки; 8 виконано: база ecs-graph закомічена в ~/.claude af626ef (main)
 ]
```
