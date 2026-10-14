---
category: A
read: archive
status: implemented
tags:
  - boot
  - asmdef
  - clean-architecture
---

App states carry no game knowledge; Boot's assemblies depend inward only, by Clean Architecture's layers.

# Request

```clojure
{:source :user
 :received-at "2026-09-26"
 :raw-request ["ShowHexesUISystem має створювати HexesUI та підписуватися на події натискання на кнопки. Після якої має відбутися перехід до AppState.MapCreation"
               "ок, давай зробимо інакше. В нас є правила як має все бути влаштовано. Не треба переписувати чи змінювати ShowHexesUISystem \nВидали це і зроби так як це має бути для нашої архітектури. \nБільше того я бачу що в нас є проблема в тому що всі реалізації IAppState лежать в Boot.Implementation а так не повинно бути. Через це й купа залежностей."
               "1 — «B (25) Тільки мою FLOW»"
               "2 — «IAppState має лежати в Core\nAppStateSystems в Implementation\nAppStateRequestedEvent в Core\nIEventTag - теж в Core\nВикористовуємо концепцію layers з Чистої Архітектури дядька Боба»"
               "3 — «B (45) Стани без знань про гру»"
               "4 — «A (65) Cascade»"
               "5 — «C (20) Усе в Core»"
               "6 — «A (70) Розширений»"
               "7 — «A (80) Step»"]}
```

# Confirmed contract

```clojure
{:task :app-states-out-of-boot
 :goal "стани застосунку без знань про гру; залежності Boot за шарами Clean Architecture"
 :path :cascade
 :where #{"Assets/Modules/Boot/" "Assets/Scripts/Core/" "Assets/Scripts/EcsExtensions/" "Assets/Scripts/Installers/World/" "носії правила :dep/boot-exempt"}
 :off-limits #{ShowHexesUISystem}
 :decided {:layers "Clean Architecture (Uncle Bob) — залежності лише всередину"
           :contracts {IAppState "Core" AppStateRequestedEvent "Core" IEventTag "Core"}
           :impl {AppStateSystems "Boot.Implementation"}
           :states "лише виконують свої системи; ініціалізація початку гри (GameplayState) і переходи — системи шарів, яким належать"
           :rule-change ":dep/boot-exempt — AGENTS.md § 6, спершу RULES_SPECIFICATION"
           :buttons "усі три кнопки меню генератора ведуть до MapCreation"
           :fail-loud "відсутня передумова — throw"}
 :never #{"змінювати ShowHexesUISystem" "генерувати .meta — нові asmdef створює власник у Unity" "перезаписувати ручні правки власника"}
 :accept [{:meter "roslyn get_diagnostics" :target "clean"}
          {:meter "/arch-check" :target "нових порушень немає"}
          {:meter "fmgraph.py check" :target "нових warnings немає"}
          {:meter "asmdef_reach" :target "циклів немає, кожне нове посилання — всередину"}
          {:meter "fantasymayor-rules-conformance" :target "no :diverges / :incomplete / :uncarried"}
          {:meter "Unity (власник)" :target "компілюється; boot доходить до MainMenu і далі до MapCreation"}]
 :result "стани не знають гри; Boot.Implementation не залежить від доменів і presentation; кнопка меню генератора → MapCreation"}
```

# Plan

```clojure
(-> (:step-1 "structure gate — STRUCTURE_APP_FLOW.md")
    (:step-2 "rule change in this session, AGENTS.md § 6: RULES_SPECIFICATION :dep/boot-exempt → the composer Installers.World only; new rule — a state carries no game knowledge, transitions are systems; :wiring/state-by-mode — ticks only after entry; then carriers (ARCHITECTURE.md with the owner's permission, fantasymayor-placement :exempt), then fantasymayor-rules-conformance")
    (:step-3 "translation — fresh agent from STRUCTURE_APP_FLOW.md")
    (:step-4 "review — sdd-code-review over the acceptance rows"))
```

# Findings

```clojure
[{:finding :rules-changed
  :at "2026-09-26"
  :fact "RULES_SPECIFICATION: :dep/boot-exempt переписано (виняток — лише композитор Installers.World), :wiring/state-by-mode (+ тік лише після входу, :weaker-today), :wiring/state-switch (просить лише система); нові :wiring/state-knows-no-game, :place/app-state-contracts. Носії: ARCHITECTURE.md (def wiring), fantasymayor-placement :exempt + :app-state-contracts; приклад member RequestedMode у fantasymayor-graph SKILL → member Mode"
  :verified-by "fantasymayor-rules-conformance, scope — ці п'ять ідентифікаторів проти ARCHITECTURE, AGENTS, placement, pattern-choice, Patterns/, fmgraph: aligned 5, diverges 0, incomplete 0, stale 0, uncarried 0; PATTERN_CONFIG `using EcsExtensions` — для IValidatableConfig, не застаріває"
  :consequence "процедура § 6 закрита; переклад може починатися"}
 {:finding :algorithm-document
  :at "2026-09-26"
  :fact "алгоритм — Flows/APP_STATES_OUT_OF_BOOT/ALGO_APP_FLOW.md (s1 + contra c-1, c-2); структура — STRUCTURE_APP_FLOW.md"
  :verified-by "sdd-algorithm-sketch і sdd-code-structure, lint clean"
  :consequence "алгоритмічний і структурний гейти пройдені"}
 {:finding :translation-report
  :at "2026-09-26"
  :fact "перекладач (Sonnet, свіжий агент): 6 типів перенесено в Core через git mv з .meta, Boot.Core — git rm, 6 нових систем, 8 станів, GameModeMachine, 4 інсталери, SystemPriorities, HexesUI, ShowHexesUISystem (using + cref), ~73 файли — using, 12 asmdef; :added — прибрано зайвий using EcsExtensions (CS8019); :missing — Lifetime.Singleton для трьох нових реєстрацій, формулювання коментарів"
  :verified-by "звіт перекладача; # Added у STRUCTURE_APP_FLOW.md"
  :consequence "обидва :missing прийнято в review"}
 {:finding :boot-exempt-rule
  :at "2026-09-26"
  :fact "RULES_SPECIFICATION :dep/boot-exempt (до зміни): «збірка запуску, що складає стан гри, стоїть поза всіма правилами залежностей»; placement-скіл ніс його як :exempt Boot.Implementation"
  :verified-by "grep RULES_SPECIFICATION.md:669, fantasymayor-placement SKILL.md:57"
  :consequence "зміна правила — перший крок за AGENTS.md § 6"}
 {:finding :state-foreign-knowledge
  :at "2026-09-26"
  :fact "чуже знання мали лише GameplayState (HexIcons з Presentation, TurnCountComponent з Turn — SeedStartOfPlay) і MainMenuState (TerrainGenerationGenerateEventComponent з Domains.Map); решта станів — тільки ядро"
  :verified-by "Read usings/bodies усіх станів у Assets/Modules/Boot/Implementation/States"
  :consequence "саме ці два знання переходять у системи своїх шарів"}
 {:finding :contract-deps
  :at "2026-09-26"
  :fact "IAppState потребує AppState (Boot.Core), GameState (Ecs.Extensions), UniTask; AppStateRequestedEvent : IEventTag (Ecs.Extensions); Ecs.Extensions → Boot.Core; Core і Boot.Core — 0 посилань; на Boot.Implementation посилається лише Installers.World; стани реєструє WorldInstaller.cs:84-91"
  :verified-by "asmdef_reach assembly-of / refs; fmgraph resolve IAppState"
  :consequence "перенесення в Core вимагає, щоб усе, що бачать ці контракти, теж було в Core або за ним"}
 {:finding :transition-broken-today
  :at "2026-09-26"
  :fact "Boot перемикає стан лише за AppStateRequestedEvent; RequestedMode у MainMenuState/MapCreationState ніхто не читав"
  :verified-by "Read Boot.cs:28-40, IAppState.cs (RequestedMode в інтерфейсі вже не було)"
  :consequence "MainMenu → MapCreation і MapCreation → Gameplay до зміни не працювали"}
 {:finding :owner-hand-edits
  :at "2026-09-26"
  :fact "власник прибрав з Boot.Implementation.asmdef посилання Presentation.UI і Presentation, а з GameplayState — using HexIcons (SeedStartOfPlay ще їх вживав)"
  :verified-by "git diff at survey time"
  :consequence "поточний стан; не повертати"}
 {:finding :core-contract-closure
  :at "2026-09-26"
  :fact "IAppState у своєму підписі тримає AppState (Boot.Core) і GameState (Ecs.Extensions); AppStateRequestedEvent тримає AppState. Core — shared kernel з 0 посилань; Ecs.Extensions → Core уже є, тож Core → Ecs.Extensions дало б цикл, а Core → Boot.Core порушує :dep/shared-kernel"
  :verified-by "Read IAppState.cs, GameState.cs, AppState.cs; asmdef_reach refs Core / Ecs.Extensions"
  :consequence "у Core переїхали й AppState, IAppStateSystem, GameState — рішення 8"}
 {:finding :core-sees-friflo-not-unitask
  :at "2026-09-26"
  :fact "Friflo.Engine.ECS — precompiled DLL з autoReferenced, тож IEventTag : IComponent компілюється в Core; UniTask — пакет з asmdef, Core його не мав"
  :verified-by "find Assets/Packages/Friflo.Engine.ECS.3.6.0/lib/*.dll; Ecs.Extensions.asmdef (overrideReferences false); Core.asmdef без references"
  :consequence "Core.asmdef: + UniTask"}
 {:finding :move-footprint
  :at "2026-09-26"
  :fact "простір імен іде за папкою: GameState вживають ~45 файлів, Modules.Boot.Core імпортують 76, IEventTag називають 17; інструменти (fmgraph type_facts, tag_law) ключують на голе імʼя IEventTag"
  :verified-by "mcp__roslyn__find_references GameState; grep-лічильники імпортів (інструмент не рахує імпорти простору імен)"
  :consequence "механічна зміна using у ~100 файлах"}
 {:finding :all-transitions-dead-but-entry
  :at "2026-09-26"
  :fact "MapCreationState просив Gameplay через RequestedMode після 3 settle-кадрів — ніхто не читав; живі переходи були: InitializationSystem → ConfigLoading, ConfigLoadingState і InstanceObjectsState — Raise після входу"
  :verified-by "Read MapCreationState.cs:55-62, Boot.cs, fmgraph explain AppStateRequestedEvent"
  :consequence "рішення 10 — усі переходи через системи"}
 {:finding :states-near-identical
  :at "2026-09-26"
  :fact "Initialization, MapLoading, GameOver — однакові: Filter(Mode), RunEntryAsync, Tick, LateTick"
  :verified-by "Read усіх восьми станів"
  :consequence "вісім класів лишились (структура); узагальнення — не в цій задачі"}
 {:finding :hexes-ui-reach
  :at "2026-09-26"
  :fact "HexesUI живе в Box ShowHexesUISystem; інша система не має до нього доступу без зміни ShowHexesUISystem. HexesUI сам raise-ить TerrainGenerationGenerateEventComponent (deviation view-boundary)"
  :verified-by "Read ShowHexesUISystem.cs, HexesUI.cs; fmgraph pattern PATTERN_VIEW_SYSTEM"
  :consequence "перехід з меню — реактивна система; відхилення — окрема задача (рішення 14)"}
 {:trap "Unity .meta"
  :where "кожен перенесений або новий файл"
  :avoid "git mv .cs разом з .meta; новий .cs — без .meta, Unity згенерує"
  :verified-by "AGENTS.md § 3 bans"}
 {:finding :src-dependency-rule
  :at "2026-09-26"
  :source "Robert C. Martin, «The Clean Architecture», blog.cleancoder.com, 2012-08-13, § The Dependency Rule; книга «Clean Architecture» (2017), гл. 22"
  :fact "«Source code dependencies can only point inwards. Nothing in an inner circle can know anything at all about something in an outer circle»; кількість кіл довільна, правило — ні"
  :verified-by "WebFetch blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html (первинне)"
  :applies "так: Core — найглибше коло; стани зі знаннями гри робили Boot-модуль залежним від зовнішніх кіл"
  :consequence "обґрунтовує рішення 3 і 5/8"}
 {:finding :src-crossing-boundaries
  :at "2026-09-26"
  :source "там само, § Crossing boundaries; книга гл. 22"
  :fact "межа перетинається через Dependency Inversion: внутрішнє коло оголошує інтерфейс, зовнішнє реалізує; через межу ходять прості структури даних"
  :verified-by "WebFetch того самого допису (первинне)"
  :applies "так: IAppState у Core, реалізації — у Boot.Implementation; AppStateRequestedEvent — проста структура"
  :consequence "рішення 2/5"}
 {:finding :src-main-component
  :at "2026-09-26"
  :source "Robert C. Martin, «Clean Architecture» (2017), гл. 26 «The Main Component» — O'Reilly, ch26"
  :fact "Main створює, координує й наглядає за рештою, отримує залежності від DI і роздає їх; найзовнішній плагін, поза діаграмою кіл"
  :verified-by "WebSearch (O'Reilly ch26 — існування розділу); зміст — вторинні нотатки github.com/serodriguez68/clean-architecture part-5-2 з цитатами"
  :weakened-by "зміст розділу — лише у вторинних нотатках"
  :applies "так: Main — WorldInstaller (Installers.World)"
  :consequence "рішення 12 — виняток лише в композитора"}
 {:finding :src-component-coupling
  :at "2026-09-26"
  :source "Robert C. Martin, «Clean Architecture» (2017), гл. 14 «Component Coupling» — ADP, SDP, SAP"
  :fact "ADP: граф без циклів; SDP: залежності — у бік стабільності; SAP: стабільний компонент абстрактний"
  :verified-by "WebSearch — en.wikipedia.org/wiki/Acyclic_dependencies_principle і вторинні конспекти гл. 14"
  :weakened-by "вторинні джерела"
  :applies "так: Core тримає абстракції, AppStateSystems — конкретний — у Boot.Implementation"
  :consequence "рішення 2"}
 {:finding :no-ready-solution
  :at "2026-09-26"
  :fact "готового зовнішнього рішення не треба: машина станів уже є; задача — переселення й зняття знань"
  :verified-by "огляд коду цього FLOW"
  :consequence "cascade лишився"}
 {:finding :flow-truncated-and-rebuilt
  :at "2026-09-26"
  :fact "скрипт запису приймання взяв першу згадку «# Acceptance» (у тексті плану) і вирізав Findings..Progress; секції відновлено з записів цієї сесії"
  :verified-by "sdd-flow lint (reader/parse) спіймав; копія пошкодженого файлу — у scratchpad сесії"
  :consequence "зміст рішень той самий; порядок записів — за номером"}]
```

# Decisions

```clojure
[{:id 1 :asked "«Видали це» — що саме?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "ShowHexesUISystem, переписати за рецептом" :confidence 75}
            {:id :b :option "лише FLOW HEXES_UI_MAP_CREATION" :confidence 25}]
  :chosen :b :value "видалено лише попередню FLOW; ShowHexesUISystem не чіпаємо" :verified-by "«B (25) Тільки мою FLOW»"}
 {:id 2 :asked "Де лежать контракти станів?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "Ecs.Extensions" :confidence 70}
            {:id :b :option "нова збірка" :confidence 30}
            {:id :c :option "власна відповідь власника" :confidence 0}]
  :chosen :c :value "IAppState, AppStateRequestedEvent, IEventTag — Core; AppStateSystems — Implementation; шари Clean Architecture"
  :verified-by "«IAppState має лежати в Core / AppStateSystems в Implementation / AppStateRequestedEvent в Core / IEventTag - теж в Core / Використовуємо концепцію layers з Чистої Архітектури дядька Боба»"}
 {:id 3 :asked "Де живуть самі стани?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "у своїх шарах" :confidence 55}
            {:id :b :option "стани без знань про гру" :confidence 45}]
  :chosen :b :value "стани лише виконують свої системи; знання — у системах шарів" :verified-by "«B (45) Стани без знань про гру»"}
 {:id 4 :asked "Як ведемо задачу?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "cascade" :confidence 65} {:id :b :option "direct" :confidence 35}]
  :chosen :a :value "cascade" :verified-by "«A (65) Cascade»"}
 {:id 5 :asked "«Core» — яка саме збірка?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "IAppState/подія → Boot.Core, IEventTag → Core" :confidence 50}
            {:id :b :option "усе в Boot.Core" :confidence 30}
            {:id :c :option "усе в Core" :confidence 20}]
  :chosen :c :value "IAppState, AppStateRequestedEvent, IEventTag — у збірці Core (Assets/Scripts/Core)" :verified-by "«C (20) Усе в Core»"}
 {:id 6 :asked "Варіант cascade?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "розширений — з кроком джерел (Clean Architecture)" :confidence 70} {:id :b :option "базовий" :confidence 30}]
  :chosen :a :value "розширений" :verified-by "«A (70) Розширений»"}
 {:id 7 :asked "Режим cascade?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "step" :confidence 80} {:id :b :option "auto" :confidence 20}]
  :chosen :a :value "step" :verified-by "«A (80) Step»"}
 {:id 8 :asked "IAppState і подія в Core тягнуть за собою AppState і GameState. Як?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "у Core переїжджають також AppState, IAppStateSystem і GameState; Boot.Core спорожніє, Core → UniTask" :confidence 70}
            {:id :b :option "Core → Boot.Core; потребує зміни :dep/shared-kernel" :confidence 30}]
  :chosen :a :value "AppState, IAppStateSystem, GameState теж у Core; Boot.Core спорожніє; Core → UniTask" :verified-by "«A (70) Усе в Core»"}
 {:id 9 :asked "Перехід з меню генератора, коли ShowHexesUISystem не змінюється?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "реактивна система MainMenu у Presentation.UI/GeneratorMenu; HexesUI шле подію з трьох кнопок" :confidence 55}
            {:id :b :option "та сама система в Domains.Map/Generation" :confidence 25}
            {:id :c :option "у Presentation.UI, лише Generate" :confidence 20}]
  :chosen :a :value "реактивна система MainMenu у Presentation.UI/GeneratorMenu → MapCreation; HexesUI шле подію генерації з усіх трьох кнопок" :verified-by "«A (55) UI + три кнопки»"}
 {:id 10 :asked "Переходи між станами після зняття знань?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "перехід «після входу» — сам стан; решта — системи" :confidence 55}
            {:id :b :option "кожен перехід — система відповідного стану" :confidence 45}]
  :chosen :b :value "кожен перехід — система відповідного стану; стан лише виконує системи" :verified-by "«B (45) Усі через системи»"}
 {:id 11 :asked "Крок джерел: перевірити Clean Architecture в мережі?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "так — веб-пошук" :confidence 70} {:id :b :option "ні — з мого знання" :confidence 30}]
  :chosen :a :value "крок джерел із веб-пошуком Clean Architecture" :verified-by "«A (70) Так, веб-пошук»"}
 {:id 12 :asked "Кому належить виняток із правил залежностей після переселення?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "лише композиторові — Installers.World" :confidence 70}
            {:id :b :option "обом" :confidence 20}
            {:id :c :option "нікому" :confidence 10}]
  :chosen :a :value "виняток лише в композитора Installers.World; Boot.Implementation — звичайний модуль" :verified-by "«A (70) Лише Installers.World»"}
 {:id 13 :asked "Стан тікає, поки вхід триває — переходи «після входу» спрацюють передчасно. Як?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "машина не тікає стан, доки вхід не завершено" :confidence 75}
            {:id :b :option "singleton «вхід завершено» в кожній системі переходу" :confidence 25}]
  :chosen :a :value "машина не тікає стан, доки вхід не завершено" :verified-by "«A (75) Машина чекає кінця входу»"}
 {:id 14 :asked "HexesUI сам шле ECS-подію — відхилення межі view. Що робимо?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "прийняти в цій задачі" :confidence 70}
            {:id :b :option "окрема наступна задача — HexesUI на C#-події" :confidence 30}]
  :chosen :b :value "відхилення HexesUI — окрема наступна задача; у цій HexesUI шле подію з трьох кнопок" :verified-by "«B (30) Окрема задача»"}
 {:id 15 :asked "Дозволити змінити в ShowHexesUISystem лише рядок using і один cref?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "так — лише ці два рядки" :confidence 80}
            {:id :b :option "ні — старий простір імен у Core" :confidence 20}]
  :chosen :a :value "ShowHexesUISystem — лише рядок using і один cref" :verified-by "«A (80) Так, лише ці два рядки»"}
 {:id 16 :asked "Boot.Core після перенесення порожня. Що з нею?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "видалити (git rm з .meta) і прибрати посилання з 11 asmdef" :confidence 75}
            {:id :b :option "лишити порожньою" :confidence 25}]
  :chosen :a :value "Boot.Core видалити" :verified-by "«A (75) Видалити»"}
 {:id 17 :asked "Як пишемо код?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "один свіжий агент, Opus" :confidence 70}
            {:id :b :option "один свіжий агент, Sonnet" :confidence 20}
            {:id :c :option "паралельні частини" :confidence 10}]
  :chosen :b :value "один свіжий агент, Sonnet" :verified-by "«B (20) Свіжий агент, Sonnet»"}
 {:id 18 :asked "Подія запиту стану лежить у корені Core, а правило каже: подія живе в теці Events/. Що робимо?" :status :confirmed :at "2026-09-26"
  :chosen :a :value "AppStateRequestedEvent → Assets/Scripts/Core/Events/, простір Core.Events" :verified-by "«A (70) В Core/Events/»"
  :options [{:id :a :option "перенести в Assets/Scripts/Core/Events/ (простір Core.Events; ~10 файлів отримують using Core.Events)" :confidence 70}
            {:id :b :option "лишити в корені Core як свідоме відхилення" :confidence 30}]}
 {:id 19 :asked "Хто виправляє два пропуски в коді і перенесення з питання 18?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "той самий перекладач (Sonnet) з оновленої структури" :confidence 55}
            {:id :b :option "у сесії власника" :confidence 45}]
  :chosen :a :value "той самий перекладач, з оновленої структури" :verified-by "«A (55) Той самий перекладач»"}]
```

# Disproven

```clojure
[{:hypothesis "AppStateRequestedEvent можна перенести в Boot.Core без нових посилань"
  :refuted-by "IEventTag у Ecs.Extensions, Ecs.Extensions → Boot.Core — зворотне дало б цикл (asmdef_reach)"
  :at "2026-09-26"
  :details "попередня FLOW HEXES_UI_MAP_CREATION, видалена за рішенням 1"}]
```

# Attempted

```clojure
[{:approach "підключити кнопки через правку ShowHexesUISystem + перенесення лише події"
  :confidence 65
  :dropped-because "власник: ShowHexesUISystem не змінювати; корінь — стани в Boot.Implementation"
  :problems "три повторні відкриття питання 2 через неперевірені заздалегідь залежності"
  :at "2026-09-26"}]
```

# Progress

```clojure
{:status :complete
 :completed #{"task statement" "decisions 1-19" "survey" "sources" "algorithm" "structure" "rule change § 6" "translation" "review + re-review" "Editor compile" "owner's Unity check"}
 :current "closed 2026-09-26"
 :remaining #{}
 :stage :step-6
 :next-invocation "sdd-cascade — variant :extended, mode :step"
 :resume-context "code translated, uncommitted; review found: event outside Events/ (structure), leftover Boot/Core folder + Core.meta (code), EnterAsync sets completion without checking the state (code)"}
```

# Acceptance

```clojure
[{:meter "roslyn get_diagnostics" :target "clean" :actual "solution-wide, severity Error — 0 (review і re-review, 2026-09-26)" :status :met}
 {:meter "/arch-check" :target "нових порушень немає" :actual "перекладач: жодного stateful/Generic/alloc у нових системах; MapCreationSettleSystem._ticksSinceEntry несе [StateAllowed] — прочитано в review" :status :met}
 {:meter "fmgraph.py check" :target "нових warnings немає" :actual "re-review: integrity clean, 32 warnings, жодного на зміненому файлі; event/declare на AppStateRequestedEvent зник після перенесення в Core/Events (рішення 18; перший прохід — :failed :level :structure)" :status :met}
 {:meter "asmdef_reach" :target "циклів немає, посилання — всередину" :actual "Core → UniTask; Boot.Implementation → Core, Ecs.Extensions, UniTask, VContainer; Core не бачить Boot.Implementation; Presentation.UI бачить AppStateRequestedEvent" :status :met}
 {:meter "fantasymayor-rules-conformance" :target "clean" :actual "aligned 5, решта 0 (до перекладу; носії не називають старих домівок типів)" :status :met}
 {:meter "структура: Boot.Core видалено повністю" :target "папки й Core.meta немає" :actual "re-review: у Assets/Modules/Boot лише Implementation; Core.meta не відстежується (перший прохід — :failed :level :code)" :status :met}
 {:meter "структура: MachineWaitsForEntry — «true лише для ТОГО стану»" :target "вхід старого стану не відкриває тік нового" :actual "re-review: GameModeMachine.cs:84 — `if (state == _current) _isEntryCompleted = true;` (перший прохід — :failed :level :code)" :status :met}
 {:meter "Editor compile — mcp__unity__recompile → console_status (AGENTS.md § 2 step-4, дозволено власником 2026-09-26)" :target "compilationFailed false, жодної помилки від зміни" :actual "recompile → up_to_date (Unity вже скомпілював: Library/ScriptAssemblies/Core.dll 16:17 після останньої правки 16:06); console_status: compilationFailed false, consoleErrors 0; помилки в буфері (13:25–14:05 UTC) — проміжні, з часу перекладу; .meta нових файлів і Core/Events створив Unity" :status :met}
 {:meter "Unity (власник)" :target "boot → MainMenu → кнопка → MapCreation → Gameplay" :actual "власник: «перевірив, працює» (2026-09-26)" :status :met}]
```

# Amendments

```clojure
[]
```
