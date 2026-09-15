---
category: A
read: reference
status: partial
tags: [tools, patterns, cascade]
related:
  - "[FLOW](FLOW.md)"
---

Контекст для s1: два інструменти графа, ознаки 15 рецептів, закон тегів, маркери, сторож, споживачі, перевірка.

# Task

```clojure
{:task :pattern-markers-and-unified-graph
 :flow "Flows/ECS_GRAPH_PATTERN_INSTANCES/FLOW.md"
 :context-written-at "2026-09-15"
 :next-stage :s1
 :previous-cascade :none                     ;; Flows/Archive/ — поза межами (:off-limits плану)
 :mode {:path :cascade
        :gates "власник бачить лише CASCADE.md # s2 і # Contra; воріт після CONTEXT і після s1 нема"
        :isolation "кожна стадія — окремий субагент із чистим контекстом; нічого з розмови не доходить — лише цей файл і файли, які він називає"
        :questions "s1 нікого не питає: вибір, який не вирішується з цього файлу, лишається відкритим рішенням для s2 з варіантами й confidence"}
 :goal "реалізації кожного з 15 рецептів Patterns/ знаходить ОДИН інструмент проєкту (злиття ecs-graph і di-graph) одним запитом pattern <RECIPE>; роль класу чи сутності береться з бази там, де база вирішує однозначно, а де не вирішує — з явного маркера: атрибута на класі або label-тегу в оголошенні архетипу; маркер, що не збігається з формою класу, ламає компіляцію Unity через аналізатор Roslyn"
 :result "агент одним запитом бачить реалізації будь-якого рецепта; роль сутності чи системи або виводиться з бази, або стоїть явним маркером, який перевіряє компіляція; реактивні системи не стоять під per_frame; закон тегів — один головний тег на архетип плюс label-теги"
 :s1-makes "алгоритм і дані: розпізнавання 15 рецептів, закон тегів (головний + label), маркери, перевірка форми аналізатором, злиття двох графів в один — без імен методів і класів майбутнього коду"
 :decided [{:id :recipes-scope :value "усі 15 рецептів, включно з view, config, addressables"}
           {:id :query-shape :value "запит за назвою рецепта — <інструмент> pattern <RECIPE>; поки що, форму переглянуть після використання"}
           {:id :marker-reader :supersedes :signature-home
            :value "ознака рецепта — база або явний маркер у коді гри; маркери читає наявний Python-інструмент на tree-sitter; правило «рецепт → ознака» живе в коді інструмента"}
           {:id :families :value "ребро inherits у графі інструмента І звірка roslyn — обидва"}
           {:id :scope-tags :refined-by :main-and-label-tags :value "роль сутності несе окремий тег в оголошенні архетипу"}
           {:id :main-and-label-tags
            :main-tag "рівно один на архетип — мітка унікальності типу сутності; системи фільтрують за архетипом, тобто за ним"
            :label-tags "додаткові теги в оголошенні архетипу — маркери ролі чи належності; системи за ними не фільтрують"
            :analysis "пост-фільтрація: спершу всі архетипи (головний тег, компоненти, теги), далі їхні label-теги"
            :direction "код усюди більше спирається на архетипи й фільтрацію за архетипами"
            :open "доля EventTag і UITag — вирішує s2 і показує в # Contra"}
           {:id :markers-hybrid :value "роль з бази, де база вирішує однозначно; атрибут — лише там, де не вирішує"}
           {:id :drift-guard :value "маркер, що не збігається з формою класу, — помилка компіляції: аналізатор Roslyn у компіляції Unity"}
           {:id :merge-tools :value "ecs-graph і di-graph зливаються в один інструмент: один граф, один CLI"}
           {:id :one-task :value "маркери в коді, перегляд закону тегів, аналізатор і злиття інструментів — одна задача"}
           {:id :tool-build-allowed :value "заборона збірки — лише для Unity-проєкту; аналізатор збирати можна; формулювання CLAUDE.md § 3 уточнюється в цій задачі"}
           {:id :tool-home :value "злитий інструмент живе в .claude/skills/ проєкту; назва :by-naming-policy, пропозиція fantasymayor-graph, остаточно в s2"}
           {:id :old-tools :value "старі ~/.claude/skills/ecs-graph і ~/.claude/skills/di-graph видаляються повністю"}
           {:id :amendment-1
            :value "transaction entity маркується label-тегом; пара view ↔ система доповнюється атрибутами; ролі в systems і в instances скіла fantasymayor-pattern-choice виправляються"}
           {:id :parallel-with-review :value "Flows/DOC_AGENT_REVIEW/ лишається активним; ця задача йде паралельно"}
           {:id :baseline-commit :value "база ecs-graph закомічена в ~/.claude af626ef (гілка main)"}]
 :disproven "реактивну систему НЕ можна відділити від per_frame за наявністю ребра reacts_to — не входити знову (FLOW # Disproven)"
 :where #{"ARCHITECTURE.md → Entities: tag-law, table-rule, archetype-law — головний тег + label-теги (правка ARCHITECTURE.md = ask у hook; схвалення і є дозвіл)"
          "Assets/ — типи атрибутів-маркерів; атрибути на класах, де база не вирішує; label-теги в холдерах архетипів"
          "проєкт аналізатора Roslyn поза Assets/ (netstandard2.0, Microsoft.CodeAnalysis.CSharp) + зібрана DLL"
          ".claude/skills/<назва>/ — злитий інструмент: один граф, один CLI, pattern <RECIPE>, виправлені ролі, inherits, хост → родина, реєстрації DI, підписки view, ін'єкції IAddressable, каталоги, маркери"
          "споживачі — див. # Consumers"}
 :out-of-scope #{"Unity-side: імпорт DLL аналізатора, .meta, мітка RoslynAnalyzer, Plugin Inspector — власник"
                 "збірка Unity-проєкту: Unity build, dotnet build / msbuild його csproj або sln"
                 "генерування чи ручне написання .meta"
                 "читання .unity сцен і серіалізованих ассетів"
                 "Flows/Archive/ — ні читати, ні правити"
                 "коментарі в .cs, що посилаються на ECS_CONVENTIONS.md чи на скасоване правило payload-less — окрема непідтверджена задача в Flows/DOC_AGENT_REVIEW/FLOW.md (12 файлів .cs)"
                 "виправлення відхилень коду від рецептів, які інструмент покаже: view, що сама піднімає ECS-подію; 7 DeleteEntity «attribute by hand»; попередження key-role про HexIdFKComponent"
                 "зміна поведінки систем: маркери й label-теги не змінюють логіку; єдина допустима зміна — склад тегів в оголошеннях архетипів"
                 "інфраструктура автоматичних тестів (заборона проєкту)"
                 "форма запиту, окрема від pattern <RECIPE> (за роллю тощо) — пізніше"
                 "нові рецепти й зміни процедур рецептів, окрім формулювань закону тегів і посилань на інструмент"
                 "скіли unity-asset-graph і arch-check"
                 "керовані файли sdd-flow (.sdd-flow/manifest.json → managedFiles) — їх перезаписує update"
                 "зміст Flows/DOC_AGENT_REVIEW/FLOW.md — підтверджена історія іншої задачі"
                 "коміти — лише на слово власника (CLAUDE.md § 4)"}}
```

# Reads

Кожен файл, який s1 може читати. Шляхи від кореня репозиторію `/Users/serhiikharsun/Documents/HomeProjects/FantasyMayor`,
якщо не починаються з `~/.claude`.

```clojure
[;; ── завдання ──────────────────────────────────────────────────────────────
 {:ref "Flows/ECS_GRAPH_PATTERN_INSTANCES/FLOW.md" :read :whole :is :number-source
  :why "сирий запит, контракт, мапа # Plan, знахідки з provenance, рішення, # Disproven; поправки до знахідок — у # Facts нижче"}
 {:ref "CODE_STORY_RULES_PROPOSAL.md" :read :section :is :example
  :why "чинний документ читабельності для каскаду; s1 — лише розділи про s1; s2 і стадія коду — цілком"}

 ;; ── інструмент ecs-graph (джерело злиття) ───────────────────────────────
 {:ref "~/.claude/skills/ecs-graph/scripts/build_graph.py" :read :whole :is :integration-point
  :why "екстракція і curate(); символи: ROOTS 34, ECS_CALLS 40-43, BASE_ROLE 47-55, CONFIG_BASES 56, classify_struct 223-230, system_role 233-237, class_kind 240-250, Extractor.extract_file 312-367, _bases 369-376, _collect_component_index_fields 505-539, _collect_singleton_manifest 541-562, _collect_holder_methods 564-607, _register_archetype 609-620, _handle_invocation 654-763 (reacts_to 754-760), _expand_typed_call_templates 781-822, _resolve_holder_calls 824-859, finalize 861-880, curate 909-1103 (закон тегів 993-1024), guess_kind 1106-1119, main 1138-1168, check_graph 1171-1205"}
 {:ref "~/.claude/skills/ecs-graph/scripts/ecsg.py" :read :whole :is :integration-point
  :why "CLI: _auto_refresh 35-56, Graph.resolve 81-97, cmd_stats, cmd_explain, cmd_neighbors, cmd_systems 175-189, cmd_search, cmd_tags_audit 207-261, cmd_spaces, cmd_tables, cmd_bfs, main 351-395"}
 {:ref "~/.claude/skills/ecs-graph/SKILL.md" :read :whole :is :integration-point :why "опис можливостей, схема graph.json (schema 3), правила чесності EXTRACTED / INFERRED / AMBIGUOUS"}
 {:ref "~/.claude/skills/ecs-graph/references/ecs-patterns.md" :read :whole :is :type-source :why "мапа «патерн ECS → вузол / ребро»"}

 ;; ── інструмент di-graph (джерело злиття) ────────────────────────────────
 {:ref "~/.claude/skills/di-graph/scripts/build_di_graph.py" :read :whole :is :integration-point
  :why "clean_type 65-88 (зберігає generic-аргументи), namespace_of 147-153 (обрізає простір імен), collection_element 165-179, Extractor._declare 283-315, _registration 325-370, _as_targets 372-382, _lifetime 384-389, _boot_compose 391-434, finalize 447-466, _merge_update 512-535, check_graph 538-565, curate 568-601, main 604-653"}
 {:ref "~/.claude/skills/di-graph/scripts/dig.py" :read :whole :is :integration-point
  :why "CLI: _auto_refresh 39-64 (інкрементний --update), cmd_explain, cmd_resolve 154-166, cmd_consumers, cmd_installer, cmd_state 193-200, cmd_search, cmd_bfs, cmd_unresolved 240-254"}
 {:ref "~/.claude/skills/di-graph/SKILL.md" :read :whole :is :integration-point :why "можливості, схема (schema 1), --update / --force"}
 {:ref "~/.claude/skills/di-graph/references/di-patterns.md" :read :whole :is :type-source :why "форми реєстрації, ін'єкції, Boot runs_in"}

 ;; ── рецепти й правила ────────────────────────────────────────────────────
 {:ref "Patterns/PATTERN_COMPONENT.md" :read :whole :is :type-source :why "рецепт 1"}
 {:ref "Patterns/PATTERN_TAG.md" :read :whole :is :integration-point :why "рецепт 2; рядки 34, 37, 40 — закон тегів і посилання на інструмент"}
 {:ref "Patterns/PATTERN_EVENT.md" :read :whole :is :type-source :why "рецепт 3; рядок 59 — посилання на інструмент"}
 {:ref "Patterns/PATTERN_CONFIG.md" :read :whole :is :type-source :why "рецепт 4"}
 {:ref "Patterns/PATTERN_CONFIG_LOADER.md" :read :whole :is :type-source :why "рецепт 5"}
 {:ref "Patterns/PATTERN_PIPELINE_STAGE.md" :read :whole :is :type-source :why "рецепт 6"}
 {:ref "Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md" :read :whole :is :type-source :why "рецепт 7"}
 {:ref "Patterns/PATTERN_REACTIVE_SYSTEM.md" :read :whole :is :type-source :why "рецепт 8"}
 {:ref "Patterns/PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM.md" :read :whole :is :type-source :why "рецепт 9; «reactive shell» — BuildDistrictActionSystem"}
 {:ref "Patterns/PATTERN_PERFRAME_SYSTEM.md" :read :whole :is :type-source :why "рецепт 10"}
 {:ref "Patterns/PATTERN_CLEANUP_SYSTEM.md" :read :whole :is :type-source :why "рецепт 11"}
 {:ref "Patterns/PATTERN_VIEW_SYSTEM.md" :read :whole :is :type-source :why "рецепт 12"}
 {:ref "Patterns/PATTERN_TRANSACTION_ENTITY.md" :read :whole :is :type-source :why "рецепт 13; рядок 91 — «identity tag never changes»"}
 {:ref "Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md" :read :whole :is :type-source :why "рецепт 14; рядки 27, 244 — посилання на інструменти; 154, 160, 226, 228, 270, 271 — закон тегів"}
 {:ref "Patterns/ADDRESSABLE_PATTERNS.md" :read :whole :is :type-source :why "рецепт 15"}
 {:ref "ARCHITECTURE.md" :read :section :is :integration-point
  :why "## Systems (18-83: reactive, stateless-systems) і ## Entities (85-150: table-rule 87-94, tag-law 96-103, key-role-law, archetype-law 121-132 з :arity-cap); ## Events 152-161"}
 {:ref ".claude/skills/fantasymayor-pattern-choice/SKILL.md" :read :whole :is :integration-point
  :why "recipe-tree (дерево вибору рецепта — словник ролей), instances 113-121 і tag-law-before 125-130 змінюються"}
 {:ref ".claude/skills/fantasymayor-placement/SKILL.md" :read :whole :is :type-source :why "де живуть нові типи атрибутів, label-теги, інсталери; перевірка залежностей збірок"}

 ;; ── shared kernel: бази, маркери, події ─────────────────────────────────
 {:ref "Assets/Scripts/Core/StateAllowedAttribute.cs" :read :whole :is :example :why "єдиний атрибут-маркер проєкту; його читає arch-check лексично"}
 {:ref "Assets/Scripts/Core/Core.asmdef" :read :whole :is :type-source :why "збірка без посилань — дім атрибутів-кандидат"}
 {:ref "Assets/Scripts/EcsExtensions/Ecs.Extensions.asmdef" :read :whole :is :type-source :why "другий дім-кандидат"}
 {:ref "Assets/Scripts/EcsExtensions/UpdatedSystem.cs" :read :whole :is :type-source :why "два конструктори бази: (EntityStore, Archetype) і (ArchetypeQuery) — якір реактивності стоїть в аргументі base(...)"}
 {:ref "Assets/Scripts/EcsExtensions/LateUpdatedSystem.cs" :read :whole :is :type-source :why "та сама форма для LateUpdate"}
 {:ref "Assets/Scripts/EcsExtensions/IUpdatedSystem.cs" :read :whole :is :type-source :why "пряма реалізація — опитувачі"}
 {:ref "Assets/Scripts/EcsExtensions/ILateUpdatedSystem.cs" :read :whole :is :type-source :why "HexIconsContainerPositionSystem реалізує напряму"}
 {:ref "Assets/Scripts/EcsExtensions/ISystem.cs" :read :whole :is :type-source :why "контракт GenerationSubSystem, HexResourcesSubSystem, HexResourcesViewSubSystem"}
 {:ref "Assets/Scripts/EcsExtensions/IUniTaskSystem.cs" :read :whole :is :type-source :why "IUniTaskSystem<T> і IUniTaskSystem (кроки старту з AppState)"}
 {:ref "Assets/Scripts/EcsExtensions/IPrioritizedUniTaskSystem.cs" :read :whole :is :type-source :why "база етапів конвеєра і TurnPhaseSubSystem"}
 {:ref "Assets/Scripts/EcsExtensions/EventArchetypes.cs" :read :whole :is :type-source :why "архетип події: EventFrameComponent + T, Tags.Get<EventTag>()"}
 {:ref "Assets/Scripts/EcsExtensions/EcsEventExtensions.cs" :read :whole :is :integration-point :why "CreateEvent складає теги сам (рядок 24) — дубль складу архетипу події"}
 {:ref "Assets/Scripts/EcsExtensions/EventCleanupSystem.cs" :read :whole :is :example :why "рецепт cleanup; єдиний фільтр за тегом поперек архетипів у shared kernel"}
 {:ref "Assets/Scripts/EcsExtensions/EventTag.cs" :read :whole :is :type-source :why "доля в новому законі — відкрита"}
 {:ref "Assets/Scripts/EcsExtensions/ConfigLoaderSystem.cs" :read :whole :is :example :why "рецепт config loader; рядок 13 — коментар із Register<ConfigLoaderSystem<X>>"}
 {:ref "Assets/Modules/Turn/Systems/TurnPhaseSubSystem.cs" :read :whole :is :type-source :why "абстрактна база фаз ходу"}
 {:ref "Assets/Presentation/UI/Tags/UITag.cs" :read :whole :is :type-source :why "спільний тег 4 архетипів — доля відкрита"}

 ;; ── холдери архетипів ────────────────────────────────────────────────────
 {:ref "Assets/Domains/Map/Archetypes/MapArchetypes.cs" :read :whole :is :number-source :why "Hex, HexResource"}
 {:ref "Assets/Domains/Economy/Archetypes/EconomyArchetypes.cs" :read :whole :is :number-source :why "District, OpenConditionSingle, OpenConditionExist (один тег на двох), BuildOutcome, generic Resource<TOwnerFK, TResourceTag>"}
 {:ref "Assets/Domains/Actors/Archetypes/ActorsArchetypes.cs" :read :whole :is :number-source :why "City, Mayor — блоковий return, не =>"}
 {:ref "Assets/Domains/Actions/Archetypes/ActionsArchetypes.cs" :read :whole :is :number-source :why "BuildDistrictInProgress — єдиний кандидат transaction entity"}
 {:ref "Assets/Presentation/Archetypes/PresentationArchetypes.cs" :read :whole :is :number-source :why "8 архетипів; 5 з них мають імена класів view"}
 {:ref "Assets/Presentation/UI/Archetypes/PresentationUIArchetypes.cs" :read :whole :is :number-source :why "4 панелі під UITag; псевдонім EcsTags"}
 {:ref "Assets/Modules/UserInput/Archetypes/UserInputArchetypes.cs" :read :whole :is :number-source :why "PlayerInput; псевдонім EcsTags"}
 {:ref "Assets/Scripts/Installers/World/SingletonArchetypes.cs" :read :whole :is :number-source :why "маніфест singleton: componentTypes.Add<T>() + Tags.Get<SingletonTag>()"}

 ;; ── класи, чию роль база не вирішує ──────────────────────────────────────
 {:ref "Assets/Modules/Turn/Systems/TurnProcessorSystem.cs" :read :whole :is :example :why "IUpdatedSystem; архетип події в полі; хост IReadOnlyList<TurnPhaseSubSystem>; заголовок: навмисно щокадровий"}
 {:ref "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictCompletionSystem.cs" :read :whole :is :example :why "IUpdatedSystem; архетип події в полі; заголовок: event-gated reconcile, пакетна обробка"}
 {:ref "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildUISystem.cs" :read :whole :is :example :why "UpdatedSystem на UI-архетипі + архетип події в полі + хост родини + підписки view"}
 {:ref "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildListUISubSystem.cs" :read :whole :is :example :why "підсистема: архетип події в полі + підписка SelectionChanged"}
 {:ref "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildPriceUISubSystem.cs" :read :whole :is :example :why "підписка PayerChanged"}
 {:ref "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelDistrictSystem.cs" :read :whole :is :example :why "реактивна через base(Query().AnyComponents(три події)) + IsRipe; підписка Cancelled"}
 {:ref "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionSystem.cs" :read :whole :is :example :why "reactive shell, названий рецептом reactive orchestrator"}
 {:ref "Assets/Domains/Economy/DistrictOpenCondition/Systems/DistrictOpenConditionEvaluatorTableChangedSystem.cs" :read :whole :is :example :why "реактивний оркестратор: base(Of<DistrictTableChangedEvent>) + IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem>"}
 {:ref "Assets/Modules/Boot/Implementation/States/MainMenuState.cs" :read :whole :is :example :why "стан гри з архетипом події в полі — не система"}

 ;; ── view ↔ система ───────────────────────────────────────────────────────
 {:ref "Assets/Presentation/UI/MainHud/HexInfoPanel/Views/HexInfoPanelView.cs" :read :whole :is :example :why "event Action Cancelled (120); CreateEvent з view (255)"}
 {:ref "Assets/Presentation/UI/DistrictBuild/Views/DistrictBuildUIView.cs" :read :whole :is :example :why "event Action Confirmed, Closed (36-37)"}
 {:ref "Assets/Presentation/UI/DistrictBuild/Views/DistrictBuildPriceUIView.cs" :read :whole :is :example :why "event Action<ActorType> PayerChanged (29)"}
 {:ref "Assets/Presentation/UI/DistrictBuild/Views/DistrictBuildListUIView.cs" :read :whole :is :example :why "event Action<DistrictType> SelectionChanged (20)"}

 ;; ── каталоги ─────────────────────────────────────────────────────────────
 {:ref "Assets/Domains/Economy/DistrictOpenCondition/Configs/DistrictOpenConditionsConfig.cs" :read :whole :is :example :why "контейнер [SerializeField] DistrictOpenConditionConfig[] (15)"}
 {:ref "Assets/Domains/Economy/DistrictOpenCondition/Configs/DistrictOpenConditionConfig.cs" :read :whole :is :example :why "абстрактна база SO"}
 {:ref "Assets/Domains/Economy/DistrictBuildOutcome/Configs/DistrictBuildOutcomesConfig.cs" :read :whole :is :example :why "контейнер [SerializeField] DistrictBuildOutcomeConfig[] (15)"}
 {:ref "Assets/Domains/Economy/DistrictBuildOutcome/Configs/DistrictBuildOutcomeConfig.cs" :read :whole :is :example :why "абстрактна база SO"}
 {:ref "Assets/Domains/Map/HexResources/Configs/HexResourcesConfig.cs" :read :whole :is :example :why "майже-каталог: масив struct-записів, без …KindComponent"}
 {:ref "Assets/Domains/Map/HexResources/Data/HexResourcesConfigEntry.cs" :read :whole :is :example :why "struct { HexResourceType Type; ResourceConfig Config }"}
 {:ref "Assets/Presentation/Terrain/Configs/IsolineConfig.cs" :read :whole :is :example :why "варіант PATTERN_CONFIG «два ассети однієї форми»"}

 ;; ── DI ───────────────────────────────────────────────────────────────────
 {:ref "Assets/Presentation/Terrain/Installer/TerrainViewInstaller.cs" :read :whole :is :example :why "ConfigLoaderSystem<X> з WithParameter(AppState.ConfigLoading) і VertexGridSpawnSystem з WithParameter(AppState.InstanceObjects)"}
 {:ref "Assets/Domains/Economy/Installer/EconomyInstaller.cs" :read :whole :is :example :why "підсистеми .As<База>(), подвійний хост"}
 {:ref "Assets/Modules/Boot/Implementation/Boot.cs" :read :whole :is :example :why "ручна композиція станів гри — runs_in"}

 ;; ── споживачі (зміст для s2 і коду; s1 бере звідси лише перелік) ─────────
 {:ref ".claude/hooks/graph-gate.py" :read :whole :is :integration-point :why "GRAPH_DIR_RE 22, GRAPH_EXES 25, повідомлення 35-37, 110-111"}
 {:ref ".claude/settings.json" :read :whole :is :integration-point :why "реєстрація hook за шляхом файла (рядок 9)"}
 {:ref ".gitignore" :read :section :is :integration-point :why "рядки 9-13 — дві теки артефактів"}
 {:ref "~/.claude/CLAUDE.md" :read :section :is :integration-point :why "рядки 9-15 — розділи ecs-graph і di-graph"}
 {:ref ".sdd-flow/project.md" :read :whole :is :integration-point :why "# Tools 32-44 і 72, # Meters 109-115, :code-verification 119, # Ceremonies 137-139"}
 {:ref "CLAUDE.md" :read :whole :is :integration-point :why "§ 2 code-verification 51-56; § 3 заборона збірки 63 і артефакти 67; § 5 notation-ecs-ext 91-96 (:tag-never-set)"}
 {:ref "GLOSSARY.md" :read :section :is :integration-point :why "рядки 4 (trigger у frontmatter) і 15-16"}
 {:ref "DOC_STANDARD.md" :read :section :is :integration-point :why "рядки 18 і 44-50 (tool-owns); розділ Frontmatter для правок документів"}
 {:ref "INDEX.md" :read :section :is :integration-point :why "рядок 81 — зона агента (ask-first); рядок 36 — генерована частина з frontmatter GLOSSARY"}
 {:ref "Tools/doc_lint.py" :read :section :is :integration-point :why "SKIP_DIRS 31-32 з двома теками артефактів; ROLE_SUFFIXES, GENERIC_VOCAB 34-49 — що вважається привидом"}
 {:ref "Tools/asmdef_reach.py" :read :section :is :integration-point :why "docstring рядок 6 називає обидва інструменти"}
 {:ref "~/.claude/projects/-Users-serhiikharsun-Documents-HomeProjects-FantasyMayor/memory/" :read :section :is :integration-point :why "рядки з # Consumers"}]
```

# Search

```clojure
[{:for "поточні члени кожної родини: нащадки абстрактної бази"
  :where "mcp__roslyn__get_type_hierarchy direction Descendants, solutionPath FantasyMayor.sln, на кожній з 11 баз # Facts :families-today"
  :settles "еталон для ребра inherits і для meter pattern <RECIPE>; find_implementations на абстрактному класі повертає 0"}
 {:for "усі реалізації інтерфейсної бази"
  :where "mcp__roslyn__find_implementations на IUpdatedSystem, ILateUpdatedSystem, IUniTaskSystem, IPrioritizedUniTaskSystem"
  :settles "транзитивний перелік систем для розпізнавання ролі"}
 {:for "аргумент base(...) кожного нащадка UpdatedSystem і LateUpdatedSystem"
  :where "grep -rn --include='*.cs' ': base(' Assets/Domains Assets/Presentation Assets/Modules Assets/Scripts"
  :settles "яка база вирішує роль лексично: EventArchetypes.Of, Query().AnyComponents(подій), холдер архетипу"}
 {:for "архетип події в полі поза base(...)"
  :where "grep -rn --include='*.cs' 'EventArchetypes.Of<' тих самих коренів"
  :settles "класи, де потрібен маркер (опитувачі)"}
 {:for "реєстрації DI з параметром стану застосунку"
  :where "grep -rn --include='*.cs' 'WithParameter(AppState.' Assets; python3 ~/.claude/skills/di-graph/scripts/dig.py resolve IUniTaskSystem"
  :settles "екземпляри config loader і кроку InstanceObjects — di-graph WithParameter не бачить"}
 {:for "підписки на події view і оголошення подій"
  :where "grep -rn --include='*.cs' -E 'public event (System\\.)?Action' і -E '\\.[A-Z][A-Za-z]+ \\+= [A-Z_][A-Za-z]+;' поза Views/"
  :settles "пари view ↔ система"}
 {:for "сьогоднішні числа графів (базова лінія)"
  :where "python3 ~/.claude/skills/ecs-graph/scripts/ecsg.py stats | systems | tags; python3 ~/.claude/skills/di-graph/scripts/dig.py stats | resolve <База>"
  :settles "числа для :numbers і для звірки після злиття; артефакти графа читати лише через CLI"}
 {:for "видимість типів атрибутів між збірками"
  :where "python3 Tools/asmdef_reach.py refs <Assembly> | can <Assembly> <Type>"
  :settles "дім атрибутів і область дії DLL аналізатора (стадія s2)"}
 {:for "вимоги Unity до аналізатора (лише якщо s2 потребує деталей понад # Facts)"
  :where "context7: бібліотека /websites/unity3d_6000_0_manual, запити про Roslyn analyzer scope і RoslynAnalyzer label"
  :settles "розміщення DLL і область застосування; веб-пошук заборонено — вікно дослідження закрите"}]
```

# Facts

```clojure
[;; ── інструменти сьогодні ─────────────────────────────────────────────────
 {:id :ecs-graph-today
  :fact "ecs-graph: ~/.claude/skills/ecs-graph/ — SKILL.md 138 рядків, scripts/build_graph.py 1209, scripts/ecsg.py 399, references/ecs-patterns.md 63. Один повний прохід tree-sitter + curate() → <project>/.ecs-graph/graph.json (schema 3). ROOTS: Assets/Domains, Assets/Presentation, Assets/Modules, Assets/Scripts — 358 файлів за 0.20-0.24 с. Вузли 419: archetype 39, component 60, config 35, data 48, event 13, installer 14, interface 14, other 80, system 76, tag 23, view 17. Ребра 567: disposes 3, emits 14, fk_of 7, has 134, reacts_to 21, reads 294, references 9, writes 85. Таблиць 23, попереджень 8 (7 DeleteEntity «attribute by hand» + 1 key-role про HexIdFKComponent). Ролі систем: per_frame 28, pipeline_stage 15, sub_system 30, turn_phase 3. CLI: stats, systems [--role], explain, neighbors [--rel], search [--role], tables, spaces, tags, bfs [--depth --rel --in], глобальний --path. Збірка: без прапорців = повна; --check = lint цілісності; --force і --update — приховані застарілі синоніми. Самолікування: перед кожним запитом повна перебудова, якщо будь-який .cs новіший за graph.json або змінилась кількість файлів"
  :at "2026-09-15"
  :verified-by "прочитав усі чотири файли цілком; ecsg.py stats, systems, tags; build_graph.py --check; /usr/bin/time на build_graph.py; wc -l"
  :consequence "злиття бере модель повної перебудови як перевірену; схема й команди — вихідна точка нового CLI"}

 {:id :di-graph-today
  :fact "di-graph: ~/.claude/skills/di-graph/ — SKILL.md 116, scripts/build_di_graph.py 657, scripts/dig.py 301, references/di-patterns.md 65. Екстракція + curate() → .di-graph/raw.json, manifest.json і graph.json (schema 1). Ті самі 4 ROOTS, 358 файлів, повна збірка 0.23 с. Вузли 272: contract 25, installer 14, service 228, state 5. Ребра 329: exposes 73, injects 126, registers 101, runs_in 29. Попереджень 0. CLI: stats, explain, resolve, consumers, installer, state, search, bfs, unresolved. Збірка: повна / --force / --update (інкремент за mtime-маніфестом) / --check. Самолікування: dig.py запускає --update; перейменування лишає привидів, потрібен --force"
  :at "2026-09-15"
  :verified-by "прочитав усі чотири файли цілком; dig.py stats; /usr/bin/time на build_di_graph.py"
  :consequence "обидва екстрактори однаково дешеві — інкрементний режим не має виграшу, лише ризик привидів"}

 {:id :di-namespace-truncated
  :fact "di-graph зберігає простір імен лише останнім сегментом: namespace_of викликає clean_type, який для qualified_name бере останню частину; dig.py explain ShowHexesUISystem друкує namespace: Systems. ecs-graph зберігає повний простір імен"
  :at "2026-09-15"
  :verified-by "build_di_graph.py:65-88 і 147-153; dig.py explain ShowHexesUISystem; ecsg.py explain ForestView"
  :consequence "простір імен у злитому графі береться за правилом ecs-graph"}

 {:id :di-generic-identity
  :fact "ecs-graph зводить тип до голого імені (type_name, generic-аргументи відкидаються), di-graph зберігає generic-аргументи в ідентичності (IPrioritizedUniTaskSystem<MapGenerationStep> ≠ IPrioritizedUniTaskSystem<TurnPhaseStep>; ConfigLoaderSystem<CityConfig> — окремий вузол)"
  :at "2026-09-15"
  :verified-by "build_graph.py:80-96; build_di_graph.py:65-88; dig.py search ConfigLoader — 26 вузлів ConfigLoaderSystem<X> + оголошення"
  :consequence "злитий граф мусить мати одне правило ідентичності типу для обох наборів фактів"}

 {:id :node-name-collisions
  :fact "ключ вузла в ecs-graph — голе ім'я, і воно зіштовхується: 6 імен архетипів збігаються з іменами класів view — DistrictBuildProgressView, DistrictView, ForestView, HexSelectionView, TerrainView, WaterView; TerrainView ще й вкладений клас констант у SystemPriorities.cs:110. Вузол архетипу поглинає атрибути класу: ecsg.py explain ForestView → kind archetype, source_location Assets/Presentation/HexResources/Views/ForestView.cs:5; explain TerrainView → source_location SystemPriorities.cs:110"
  :at "2026-09-15"
  :verified-by "ecsg.py explain ForestView і TerrainView; grep оголошень class/struct/enum з іменами всіх 24 не-подієвих архетипів"
  :consequence "злитий граф потребує ідентифікаторів, що не зіштовхуються; без цього 6 view не видно як view"}

 {:id :view-kind-imprecise
  :fact "kind view (17) = клас, у базах якого є підрядок MonoBehaviour. Серед 17 — не view: Boot, DisposedMono, StatusMonitor, SpawnerAuthoringBase. У теках Views/ оголошено 19 класів view; 16 похідні від MonoBehaviour, 3 від DisposedMono (TerrainView, WaterView, HexSelectionView); 6 з 19 сховані зіткненням імен"
  :at "2026-09-15"
  :verified-by "ecsg.py search з фільтром [view]; grep class у */Views/*.cs"
  :consequence "ознака view для рецепта 12 — не «база MonoBehaviour»; кандидати: тека Views/, транзитивна база MonoBehaviour через inherits, маркер"}

 {:id :roots-miss-flows
  :fact "Assets/Flows/DistrictBuild/Events/DistrictBuildUIRequestedEvent.cs (asmdef Flows.DistrictBuild) лежить поза ROOTS: граф знає подію лише з використань, без source_location. Інші .cs поза ROOTS: Assets/Editor 1, Assets/TutorialInfo 2, Assets/Plugins 363 (сторонні)"
  :at "2026-09-15"
  :verified-by "find Assets -name '*.cs' поза чотирма коренями; ecsg.py explain DistrictBuildUIRequestedEvent"
  :consequence "корені злитого інструмента — відкрите рішення :od-roots"}

 {:id :tag-law-in-tools
  :fact "закон тегів у коді інструмента: curate() прохід 3 (build_graph.py:993-1024) — _STRUCTURAL_TAGS = {EventTag}; попередження, коли не-подієвий архетип без тегу або з >1 доменним тегом, коли крос-архетипний запит має >1 тег, і на кожен AddComponent тегу під час життя. ecsg.py tags (cmd_tags_audit 207-261): архетип події — без доменних тегів, інший — рівно 1; сьогодні 0 відхилень. CLAUDE.md § 2 :step-3 (рядок 55) задає ціль «exactly one tag per archetype and per query binding»"
  :at "2026-09-15"
  :verified-by "прочитав build_graph.py і ecsg.py; ecsg.py tags; CLAUDE.md:55"
  :consequence "під новим законом label-теги інакше дадуть хибні попередження; аудит рахує головний тег окремо від label"}

 {:id :holder-parsing
  :fact "_collect_holder_methods бере склад архетипу з GetArchetype(arg0, arg1): компоненти — type-аргументи arg0, теги — type-аргументи arg1, незалежно від отримувача (Tags.Get і EcsTags.Get однаково); тіло — => або перший return (ActorsArchetypes). Маніфест singleton — componentTypes.Add<T>() + Tags.Get. Додатковий тег у Tags.Get<A, B> потрапить у той самий список tags без розрізнення головного й label"
  :at "2026-09-15"
  :verified-by "build_graph.py:541-620; прочитав усі 8 холдерів"
  :consequence "розрізнення головного тегу й label-тегу треба задати правилом — :od-main-vs-label"}

 {:id :graph-gate-hook
  :fact ".claude/hooks/graph-gate.py: GRAPH_DIR_RE = r\"\\.(?:ecs|di)-graph\\b\"; GRAPH_EXES = {ecsg.py, dig.py, build_graph.py, build_di_graph.py}; Bash-сегмент, що згадує теку артефактів з іншим виконуваним файлом, — deny; Write/Edit у теку — deny; правка ARCHITECTURE.md — ask; субагенти звільнені; fail-open. Hook зареєстровано в .claude/settings.json:9 шляхом до файла"
  :at "2026-09-15"
  :verified-by "прочитав graph-gate.py цілком; grep graph-gate .claude/settings.json"
  :consequence "нова тека артефактів і нові імена скриптів мають потрапити в регулярку і в GRAPH_EXES; ask на ARCHITECTURE.md лишається"}

 {:id :roslyn-families
  :fact "roslyn MCP: find_implementations на абстрактному класі повертає 0 (MainHudSpawnSubSystem, UpdatedSystem); на інтерфейсі IUpdatedSystem — 25 транзитивних реалізацій, включно з нащадками UpdatedSystem; get_type_hierarchy direction Descendants на MainHudSpawnSubSystem повертає 4 нащадки"
  :at "2026-09-15"
  :verified-by "викликав mcp__roslyn__find_implementations і mcp__roslyn__get_type_hierarchy на FantasyMayor.sln"
  :consequence "еталон родин для meter — get_type_hierarchy Descendants на абстрактній базі, find_implementations — лише на інтерфейсі; instances :families у скілі pattern-choice зараз називає інструмент, що на абстрактних базах мовчить"}

 {:id :families-today
  :fact "11 контрактів родин (dig.py resolve): IPrioritizedUniTaskSystem<MapGenerationStep> 14, TurnPhaseSubSystem 3, GenerationSubSystem 4, HexResourcesSubSystem 3, HexResourcesViewSubSystem 3, ViewSubSystem 3, MainHudSpawnSubSystem 4, DistrictBuildUISubSystem 4, DistrictOpenConditionSpawnSubSystem 2, DistrictOpenConditionEvaluatorSubSystem 2, DistrictBuildOutcomeSpawnSubSystem 1. Контракти баз: IDisposable (DistrictBuildUISubSystem, DistrictOpenConditionSpawnSubSystem, DistrictOpenConditionEvaluatorSubSystem, DistrictBuildOutcomeSpawnSubSystem), ISystem<GameState> (GenerationSubSystem, HexResourcesSubSystem, HexResourcesViewSubSystem), IUniTaskSystem<GameState> (ViewSubSystem), без бази (MainHudSpawnSubSystem), IPrioritizedUniTaskSystem<TurnPhaseStep> (TurnPhaseSubSystem)"
  :at "2026-09-15"
  :verified-by "dig.py resolve на кожному контракті; grep abstract class по чотирьох коренях"
  :consequence "26 конкретних підсистем + 3 фази ходу + 14 етапів конвеєра — числа для звірки"}

 {:id :hosts-today
  :fact "12 хостів родин беруть IReadOnlyList<База> у конструкторі: DistrictBuildOutcomeSpawnSystem, DistrictOpenConditionEvaluatorSystem, DistrictOpenConditionEvaluatorBootstrapSystem, DistrictOpenConditionEvaluatorTableChangedSystem, DistrictOpenConditionSpawnSystem, HexResourcesSystem, GenerationSystem, MainHudSpawnSystem, DistrictBuildUISystem, HexResourcesViewSystem, TerrainViewSystem, TurnProcessorSystem. Поза системами — TurnPhaseRunner (helper), Boot.Construct (IReadOnlyList<IUniTaskSystem>, IReadOnlyList<IPrioritizedUniTaskSystem<MapGenerationStep>>), GameplayState, MapCreationState. Родину DistrictOpenConditionEvaluatorSubSystem інжектують три хости: етап конвеєра, фаза ходу, реактивна система"
  :at "2026-09-15"
  :verified-by "grep IReadOnlyList<…SubSystem|System|Step> у параметрах по чотирьох коренях"
  :consequence "ребро хост → родина — лексичне; один член родини може мати кількох хостів"}

 {:id :per-frame-composition
  :fact "ПОПРАВКА до FLOW :role-conflations: per_frame 28 = 16 реактивних з base(EventArchetypes.Of<E>) + HexInfoPanelDistrictSystem (реактивна: base(storages.World.Query().AnyComponents(ComponentTypes.Get<SelectedHexChangedEvent, TurnCompletedEvent, DistrictTableChangedEvent>())) і перший рядок IsRipe; ребра reacts_to не має) + 3 опитувачі (TurnProcessorSystem, BuildDistrictCompletionSystem, DistrictBuildUISystem) + 5 справді щокадрових (CameraMovementSystem, HexSelectionSystem, ResourceBarSystem, TurnPanelViewSystem, HexIconsContainerPositionSystem) + EventCleanupSystem + бази UpdatedSystem і LateUpdatedSystem. У FLOW стояло «6 справді щокадрових»"
  :at "2026-09-15"
  :verified-by "ecsg.py systems; прочитав заголовки й конструктори всіх 28; grep IsRipe( — 22 файли систем + MainMenuState + 2 файли shared kernel; ecsg.py neighbors HexInfoPanelDistrictSystem --rel reacts_to — порожньо"
  :consequence "ознака реактивної: аргумент base(...) — архетип події АБО запит AnyComponents лише з типів подій"}

 {:id :reactive-16
  :fact "16 систем з base(storages.World, EventArchetypes.Of<E>(storages.World)): DistrictOpenConditionEvaluatorTableChangedSystem, BuildDistrictActionCancelSystem, BuildDistrictActionSystem, HexInfoPanelSystem, HexInfoPanelResourcesSystem, HexInfoPanelHeaderSystem, ContextTabsAvailabilitySystem, ContextTabSelectionSystem, ForestDespawnSystem, ForestSpawnSystem, DistrictViewSpawnSystem, DistrictBuildProgressViewDespawnSystem, DistrictBuildProgressViewSpawnSystem, HexSelectionViewSystem, HexIconsVisibilitySystem, TurnCountSystem"
  :at "2026-09-15"
  :verified-by "grep -B3 EventArchetypes.Of< з перевіркою рядка : base("
  :consequence "база вирішує роль reactive для 16 без маркера"}

 {:id :pollers
  :fact "архетип події в полі, не в base(...): TurnProcessorSystem (IUpdatedSystem; Of<NextTurnEvent>; хост фаз ходу; заголовок «Deliberately a per-frame system … must tick every frame to poll the in-flight task»), BuildDistrictCompletionSystem (IUpdatedSystem; Of<BuildDistrictCompleteEvent>; заголовок «Event-gated reconcile … batch dispatch»), DistrictBuildUISystem (UpdatedSystem на PresentationUIArchetypes.DistrictBuildUI; Of<DistrictBuildUIRequestedEvent>; хост DistrictBuildUISubSystem; підписки view), DistrictBuildListUISubSystem (підсистема; той самий запит), MainMenuState (стан гри; Of<TerrainGenerationGenerateEventComponent>)"
  :at "2026-09-15"
  :verified-by "прочитав заголовки й конструктори п'яти файлів"
  :consequence "для трьох систем база роль не вирішує — тут маркер (:markers-hybrid); підсистема й стан гри — не системи рецептів 8 і 10"}

 {:id :reactive-orchestrator-today
  :fact "реактивний оркестратор за формою (base(Of<E>) + IReadOnlyList<База>) — лише DistrictOpenConditionEvaluatorTableChangedSystem. Рецепт 9 називає живим екземпляром «reactive shell» BuildDistrictActionSystem, у якого списку підсистем нема — за формою він звичайна реактивна система"
  :at "2026-09-15"
  :verified-by "grep обох ознак; PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM.md, розділ Incremental start"
  :consequence "shell механічно не впізнається як рецепт 9 — :od-shell"}

 {:id :pipeline-and-phases
  :fact "IPrioritizedUniTaskSystem<MapGenerationStep> реалізують 14 етапів: GenerationSystem 100, HexResourcesSystem 200, TerrainViewSystem 300, HexResourcesViewSystem 400, HexSelectionViewLoadingSystem 500, TerrainViewDebugSystem 600, HexIconsSpawnSystem 700, MainHudSpawnSystem 800, DistrictBuildUISpawnSystem 810, CitySpawnSystem 900, MayorSpawnSystem 910, DistrictOpenConditionSpawnSystem 920, DistrictOpenConditionEvaluatorBootstrapSystem 925, DistrictBuildOutcomeSpawnSystem 930. Роль pipeline_stage зараз також має абстрактна TurnPhaseSubSystem (IPrioritizedUniTaskSystem<TurnPhaseStep>) — BASE_ROLE шукає підрядок без generic-аргументу. Фази ходу: MayorAPRestoreSubSystem 500, BuildDistrictTurnTickSystem 510, DistrictOpenConditionEvaluatorSystem 1000"
  :at "2026-09-15"
  :verified-by "ecsg.py systems; dig.py resolve для обох контрактів"
  :consequence "база з generic-аргументом вирішує роль однозначно, якщо аргумент зберігається"}

 {:id :sub-system-mix
  :fact "sub_system 30 = 26 конкретних підсистем родин + ConfigLoaderSystem (generic IUniTaskSystem), ShowHexesUISystem (IUniTaskSystem<FirstUIStep>, Scoped у UIInstaller, runs_in MainMenuState), VertexGridSpawnSystem (IUniTaskSystem, крок InstanceObjects), абстрактна ViewSubSystem"
  :at "2026-09-15"
  :verified-by "ecsg.py systems; dig.py explain ShowHexesUISystem"
  :consequence "кроки старту й підсистеми родин — різні ролі; ShowHexesUISystem не підпадає під жоден з 15 рецептів"}

 {:id :cleanup-today
  :fact "рецепт cleanup має один екземпляр — EventCleanupSystem: IUpdatedSystem, storages.World.Query().AllTags(Tags.Get<EventTag>()), DeleteEntity зрілих подій, Priority SystemPriorities.RuntimeTick.EventCleanup. Роль cleanup не отримує ніхто: BASE_ROLE шукає клас, у базах якого є EventCleanupSystem"
  :at "2026-09-15"
  :verified-by "прочитав EventCleanupSystem.cs; build_graph.py:47-55; ecsg.py systems"
  :consequence "ознака cleanup — крос-архетипний запит за EventTag + видалення, а не база"}

 {:id :config-loader-count
  :fact "ПОПРАВКА до FLOW :di-shaped-recipes: реєстрацій Register<ConfigLoaderSystem<X>> — 26 (27-й збіг grep — коментар ConfigLoaderSystem.cs:13). Усі з .As<IUniTaskSystem>().WithParameter(AppState.ConfigLoading); крок InstanceObjects — 1 (VertexGridSpawnSystem, TerrainViewInstaller.cs:59-61). dig.py resolve IUniTaskSystem → 27 реалізацій = 26 ConfigLoaderSystem<X> + VertexGridSpawnSystem. WithParameter(AppState.X) di-graph не розбирає"
  :at "2026-09-15"
  :verified-by "grep Register<ConfigLoaderSystem< з виключенням ConfigLoaderSystem.cs; grep WithParameter(AppState.; dig.py resolve IUniTaskSystem"
  :consequence "екземпляри рецепта 5 — реєстрації; стан застосунку треба добувати з ланцюжка WithParameter"}

 {:id :view-subscriptions
  :fact "ПОПРАВКА до FLOW :view-system-subscriptions: 5 підписок у 4 класах (не в 3): HexInfoPanelDistrictSystem — view.Cancelled; DistrictBuildUISystem — view.Confirmed, view.Closed; DistrictBuildListUISubSystem — view.SelectionChanged; DistrictBuildPriceUISubSystem — view.PayerChanged. Оголошень public event Action… 5 у 4 view: HexInfoPanelView, DistrictBuildUIView (2), DistrictBuildPriceUIView, DistrictBuildListUIView. Системи, що лише штовхають значення у view через …ViewComponent без подій (ResourceBarSystem, TurnPanelViewSystem та ін.), цією ознакою не ловляться"
  :at "2026-09-15"
  :verified-by "grep обох шаблонів по чотирьох коренях"
  :consequence "пара view ↔ система за підписками — 4 класи; чи рахувати пари «лише вихід» — :od-view-pairs"}

 {:id :view-raises-pulse
  :fact "HexInfoPanelView.cs:255 — view сама піднімає ECS-подію: _storages.World.CreateEvent(new DistrictBuildUIRequestedEvent()); рецепт 12 каже :never «raise an ECS pulse to its own system»; подія живе в іншій збірці (Flows.DistrictBuild); ecs-graph показує ребро emits від HexInfoPanelView"
  :at "2026-09-15"
  :verified-by "ecsg.py explain DistrictBuildUIRequestedEvent; прочитав рядки 251-255 view"
  :consequence "інструмент мусить уміти показати таке відхилення; виправлення — поза межами"}

 {:id :addressable-injections
  :fact "IAddressable інжектують конструктором 7 класів: ShowHexesUISystem, MainHudSpawnSystem, DistrictBuildUISpawnSystem, TerrainViewSystem, WaterViewSubSystem, HexSelectionViewLoadingSystem, ConfigLoaderSystem"
  :at "2026-09-15"
  :verified-by "grep 'IAddressable <ім'я>[,)]' по чотирьох коренях; збігається з FLOW :addressable-injections"
  :consequence "ознака рецепта 15 — тип параметра конструктора; di-graph уже має ребро injects IAddressable"}

 {:id :catalogue-today
  :fact "ПОПРАВКА причини в FLOW :catalogue-signature: ознака «абстрактна база SO + SO-контейнер з [SerializeField] База[] + …KindComponent» дає 2 каталоги — DistrictOpenConditionsConfig → DistrictOpenConditionConfig[] з DistrictOpenConditionKindComponent, DistrictBuildOutcomesConfig → DistrictBuildOutcomeConfig[] з DistrictBuildOutcomeKindComponent. ResourceConfig контейнер МАЄ: HexResourcesConfig тримає HexResourcesConfigEntry[] (struct { HexResourceType Type; ResourceConfig Config }), підсистеми генерації приводять тип (ClayResourceGenerationSubSystem.cs:41) — але поле типізоване struct-записом, таблиці сутностей і …KindComponent нема, тож ознака його відсікає. IsolineConfig контейнера не має: дві sealed-підкласи зберігаються за типом"
  :at "2026-09-15"
  :verified-by "grep [SerializeField] …Config[], struct …KindComponent, класи-нащадки ResourceConfig і IsolineConfig; прочитав HexResourcesConfig.cs і HexResourcesConfigEntry.cs"
  :consequence "ознаці каталогу потрібні inherits, типи полів SO і вид …KindComponent; висновок FLOW про 2 каталоги стоїть"}

 {:id :transaction-candidate
  :fact "єдиний кандидат рецепта 13 — ActionsArchetypes.BuildDistrictInProgress (DistrictIdFKComponent, ActionIdComponent, BuildDistrictTurnsComponent, ActorTypeComponent; тег BuildDistrictInProgressTag); рецепт сам називає випадок виродженим"
  :at "2026-09-15"
  :verified-by "прочитав ActionsArchetypes.cs і PATTERN_TRANSACTION_ENTITY.md"
  :consequence "label-тег ролі транзакції стоїть в одному холдері"}

 {:id :data-kinds-today
  :fact "рецепти даних уже є видами вузлів: component 60, tag 23, event 13 (подія впізнається суфіксом Event / EventComponent або використанням у EventArchetypes.Of / CreateEvent), config 35 (база ScriptableObject / SerializedScriptableObject)"
  :at "2026-09-15"
  :verified-by "ecsg.py stats; build_graph.py:223-250"
  :consequence "pattern для рецептів 1-4 — фільтр за видом"}

 {:id :state-allowed-precedent
  :fact "єдиний атрибут-маркер проєкту — StateAllowedAttribute (Assets/Scripts/Core/, збірка Core, AttributeUsage Field, необов'язковий reason); 7 використань; skill arch-check читає [StateAllowed] лексично. Інші атрибути в корінних теках: SegmentSizeAttribute (Assets/Scripts/Extentions/, без asmdef, без використань). Лічильники атрибутів у коді гри: UsedImplicitly 75, CreateAssetMenu 39, Serializable 9, StateAllowed 7"
  :at "2026-09-15"
  :verified-by "grep ': Attribute' і '[Назва' по чотирьох коренях; ~/.claude/skills/arch-check/SKILL.md:90-101"
  :consequence "атрибут-маркер, який читає інструмент без Roslyn, має прецедент у проєкті"}

 {:id :tree-sitter-attributes
  :fact "tree-sitter-c-sharp розбирає атрибути з аргументами typeof(...) (FLOW :tree-sitter-reads-attributes); встановлено Python 3.13.5, tree-sitter 0.25.2, tree-sitter-c-sharp 0.23.5"
  :at "2026-09-15"
  :verified-by "FLOW # Findings (виміряно); python3 -c importlib.metadata.version"
  :consequence "читання маркерів не потребує Roslyn"}

 ;; ── теги ─────────────────────────────────────────────────────────────────
 {:id :tags-today
  :fact "39 архетипів, у кожному рівно 1 тег. 13 архетипів подій — EventTag. UITag несуть 4 архетипи (HexInfoPanel, TurnPanel, ResourceBar, DistrictBuildUI) — коментар холдера: «category tag under the Tag Law … identity rides on each panel's own view component». DistrictOpenConditionTag несуть 2 (OpenConditionSingle, OpenConditionExist — різний склад). SingletonTag — архетип Singleton. Решта 19 тегів — по одному архетипу. Generic Resource<TOwnerFK, TResourceTag> закривається двічі (CityResourceTag, MayorResourceTag)"
  :at "2026-09-15"
  :verified-by "ecsg.py tags; прочитав усі холдери"
  :consequence "під «головний тег = мітка унікальності» UITag і DistrictOpenConditionTag зараз не унікальні — :od-uniqueness"}

 {:id :holders-today
  :fact "холдери: MapArchetypes (Domains.Map), EconomyArchetypes (Domains.Economy), ActorsArchetypes (Domains.Actors), ActionsArchetypes (Domains.Actions), PresentationArchetypes (Presentation), PresentationUIArchetypes (Presentation.UI), UserInputArchetypes (UserInput) + generic EventArchetypes (Ecs.Extensions) + маніфест SingletonArchetypes (Installers.World, SingletonArchetypeDefinition). Presentation.UI і UserInput пишуть EcsTags.Get через псевдонім, бо простір імен …Tags затінює Friflo Tags"
  :at "2026-09-15"
  :verified-by "find Assets -name '*Archetypes.cs' поза Plugins; прочитав усі"
  :consequence "label-теги додаються в ці 9 місць; розбір має розуміти обидва написання"}

 {:id :friflo-arity
  :fact "Friflo.Engine.ECS 3.6.0: Tags.Get``1 … Get``5, ComponentTypes.Get``1 … Get``5, Tags.Add``1, EntityStoreBase.GetArchetype(ComponentTypes, Tags). Найбільший склад компонентів сьогодні — 4 (District, OpenConditionExist, Mayor, BuildDistrictInProgress); тегів — 1. ARCHITECTURE.md archetype-law :arity-cap — «at most 5 type arguments»"
  :at "2026-09-15"
  :verified-by "grep M:Friflo.Engine.ECS.Tags.Get і ComponentTypes.Get у Assets/Packages/Friflo.Engine.ECS.3.6.0/lib/netstandard2.1/Friflo.Engine.ECS.xml; ARCHITECTURE.md:132"
  :consequence "у Tags.Get поміщається головний тег + до 4 label-тегів; більше — лише через Tags.Add, який розбір холдерів не читає"}

 {:id :event-tags-duplicated
  :fact "рядок події народжує EcsEventExtensions.CreateEvent: store.CreateEntity(new EventFrameComponent { … }, payload, Tags.Get<EventTag>()) — склад тегів задано тут окремо від EventArchetypes.Of<T>"
  :at "2026-09-15"
  :verified-by "прочитав EcsEventExtensions.cs:18-25 і EventArchetypes.cs"
  :consequence "будь-яка зміна тегів архетипів подій мусить змінити обидва місця, інакше рядок народиться в іншому архетипі, ніж той, на якому стоять споживачі"}

 {:id :tag-filters-across-archetypes
  :fact "фільтр за тегом поперек архетипів стоїть у 2 місцях: EventCleanupSystem — Query().AllTags(Tags.Get<EventTag>()); HexIconsSpawnSystem.cs:76 — Query<HexIdPKComponent>().AllTags(Tags.Get<HexTag>()). Третій крос-архетипний запит — HexInfoPanelDistrictSystem, AnyComponents за типами подій, без тегів"
  :at "2026-09-15"
  :verified-by "grep AllTags|AnyTags|WithoutAnyTags|AnyComponents|Query< по чотирьох коренях"
  :consequence "якщо EventTag стане label-тегом, EventCleanupSystem фільтруватиме за label — суперечить :label-tags; AllTags бере надмножину, тож label-теги на Hex цей запит не зламають"}

 {:id :tag-law-wording
  :fact "формулювання «один тег» у документах: ARCHITECTURE.md:97 («exactly one tag in every archetype declaration», «two identity tags — that row cannot exist»), :102 (:kind «never a second tag»), :98-99 (:discriminator cond category-tag); PATTERN_TAG.md:34 ({:exactly 1}; «the only extras: structural EventTag on pulses, category UITag»), :37; PATTERN_POLYMORPHIC_CATALOGUE.md:154, 160 («the row's ONLY tag»), 226, 228 (:second-tag :NEVER), 270, 271; PATTERN_TRANSACTION_ENTITY.md:91 («the identity tag never changes»); CLAUDE.md:55 (§ 2 :step-3), CLAUDE.md:95 (§ 5 :tag-never-set «2 identity tags»); .claude/skills/fantasymayor-pattern-choice/SKILL.md:3 і 123-130"
  :at "2026-09-15"
  :verified-by "grep -i «tag law|one tag|only tag|second tag|identity tag|exactly 1» по Patterns, ARCHITECTURE.md, CLAUDE.md, .claude/skills"
  :consequence "перегляд закону зачіпає ці рядки; «kind — колонка, не другий тег» лишається правдою і під label-тегами"}

 ;; ── маркери: де жити атрибутам ──────────────────────────────────────────
 {:id :core-reach
  :fact "Core.asmdef не має посилань. Прямо посилаються на Core: Addressables.Core, Addressables.Implementations, AxialSystem, Domains.Actions, Domains.Actors, Domains.Economy, Domains.Map, Ecs.Extensions, Presentation, Presentation.UI, Turn, UserInput. Boot.Implementation (MainMenuState) і Installers.World (SingletonArchetypes) на Core не посилаються, але посилаються на Ecs.Extensions. Ecs.Extensions посилається на UniTask, Core, Addressables.Core, Boot.Core, Unity.Collections. Ігрових asmdef — 23"
  :at "2026-09-15"
  :verified-by "python3 Tools/asmdef_reach.py refs для Presentation.UI, Boot.Implementation, Installers.World, Ecs.Extensions, Turn, UserInput, Presentation; розбір JSON усіх *.asmdef поза Plugins і Packages"
  :consequence "атрибут у Core видно всім збіркам із системами, окрім Boot.Implementation і Installers.World"}

 {:id :placement-rules
  :fact "скіл fantasymayor-placement: shared kernel — Assets/Scripts/Core/ або Assets/Scripts/EcsExtensions/, :only-when кілька шарів потребують коду і він не знає жодного з них; теги — рольова тека Tags/; нове посилання між збірками — python3 Tools/asmdef_reach.py can <Assembly> <Type>; :never — код поза рольовою текою (Core і EcsExtensions пласкі, без рольових тек)"
  :at "2026-09-15"
  :verified-by "прочитав .claude/skills/fantasymayor-placement/SKILL.md цілком; ls Assets/Scripts/Core Assets/Scripts/EcsExtensions"
  :consequence "дім атрибутів і label-тегів вирішує s2 за цими правилами — :od-marker-home, :od-label-home"}

 ;; ── сторож: аналізатор Roslyn ────────────────────────────────────────────
 {:id :unity-compiler
  :fact "ProjectSettings/ProjectVersion.txt: 6000.5.1f1. Unity компілює Roslyn 4.10 (Unity.app/…/Scripting/DotNetSdk/sdk/8.0.318/Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll, ProductVersion 4.10.0-3.25064.8); власні аналізатори Unity (BuildPipeline/Unity.Analyzers.Common) — 4.10; ApiUpdater — 4.4. Assembly-CSharp.csproj: LangVersion 9.0, Analyzer Include на Unity.SourceGenerators. csproj у корені — 62"
  :at "2026-09-15"
  :verified-by "cat ProjectVersion.txt; читання рядка ProductVersion з DLL редактора; grep csproj"
  :consequence "аналізатор, зібраний проти Microsoft.CodeAnalysis.CSharp 4.3 (вимога документації), завантажиться компілятором 4.10"}

 {:id :unity-analyzer-docs
  :fact "документація Unity 6000.0: мітка ассета RoslynAnalyzer (чутлива до регістру) у Plugin Inspector, усі платформи вимкнені; аналізатор у корені Assets діє на ПЕРЕДВИЗНАЧЕНІ збірки (скрипти без власного asmdef); аналізатор у теці з asmdef — лише на цю збірку і збірки, що на неї посилаються. З FLOW :roslyn-options: netstandard2.0, Microsoft.CodeAnalysis.CSharp 4.3; діагностика може мати severity Error"
  :at "2026-09-15"
  :verified-by "context7 /websites/unity3d_6000_0_manual — install-existing-analyzer, create-roslyn-analyzer, analyzer-scope-and-diagnostics; FLOW # Findings"
  :consequence "увесь код гри — у власних asmdef, тож DLL у корені Assets його НЕ перевірить; DLL має лежати в теці збірки, на яку посилаються перевірювані збірки"}

 {:id :analyzer-build-env
  :fact "DLL з міткою RoslynAnalyzer в Assets нема (grep по .meta — 0). dotnet SDK: 8.0.412 і 10.0.300. ~/.nuget/packages містить 20 пакетів, серед них жодного Microsoft.CodeAnalysis — restore потребує мережі nuget.org. Assets/NuGet.config — NuGetForUnity (repositoryPath ./Packages), це Unity-side менеджер пакетів"
  :at "2026-09-15"
  :verified-by "grep -rl RoslynAnalyzer --include='*.meta' Assets; dotnet --list-sdks; ls ~/.nuget/packages; cat Assets/NuGet.config"
  :consequence "збірка аналізатора — перша в проєкті; джерело Microsoft.CodeAnalysis — :od-analyzer-deps"}

 ;; ── документи й перевірки ────────────────────────────────────────────────
 {:id :doc-lint-baseline
  :fact "python3 Tools/doc_lint.py --quiet до цієї стадії: 3 привиди у 2 файлах (HexIdComponent — PATTERN_COMPONENT.md:42, PATTERN_TRANSACTION_ENTITY.md:51 і :72), 0 помилок Clojure, 39 md. Привид = PascalCase-токен із суфіксом SubSystem|System|Component|Tag|Event|Config|Installer|View, якого нема в Assets/**.cs; рядки з маркерами історії (remov, retir, renam, видален, перейменован …) пропускаються"
  :at "2026-09-15"
  :verified-by "запустив doc_lint.py і doc_lint.py --quiet; прочитав Tools/doc_lint.py:28-60, 330-375"
  :consequence "артефакти s1 і s2 не вводять неіснуючих імен із ролевими суфіксами — майбутні імена писати без суфікса або словами"}

 {:id :category-a-docs
  :fact "gen_index: документ Category A у теці задачі, що не FLOW.md, потребує status; read: always обов'язковий лише для FLOW.md зі status partial; посилання в тілі поза fence перевіряються. Правка зони агента INDEX.md після зміни коду — :ask-first (CLAUDE.md § 3); рядок між BEGIN/END GENERATED — лише через gen_index"
  :at "2026-09-15"
  :verified-by "прочитав Tools/gen_index.py:90-130, 180-226; CLAUDE.md:63-78"
  :consequence "CONTEXT.md і CASCADE.md — read: reference, status: partial"}

 {:id :stale-comments-other-task
  :fact "12 файлів .cs досі посилаються на ECS_CONVENTIONS.md (серед них EcsEventExtensions.cs:9, EventCleanupSystem.cs:10); видалення цих коментарів — задача :delete-stale-code-comments у Flows/DOC_AGENT_REVIEW/FLOW.md, :confirmed false"
  :at "2026-09-15"
  :verified-by "grep -rl ECS_CONVENTIONS --include='*.cs' Assets; прочитав DOC_AGENT_REVIEW/FLOW.md:600-616"
  :consequence "торкаючись цих файлів, коментарі не чіпати"}]
```

# Recipe signatures

Ознака кожного рецепта в коді сьогодні: чим вирішується роль (гібридне правило `:markers-hybrid`), що бачить
граф зараз, базові числа. `:decided-by` — `:kind` (вид вузла), `:base` (база чи контракт), `:lexical`
(аргумент base(...), тип параметра, тип поля), `:di` (реєстрація), `:marker` (атрибут), `:label-tag`
(тег в оголошенні архетипу). Правило «рецепт → ознака» живе в коді інструмента (`:marker-reader`).

```clojure
[{:recipe PATTERN_COMPONENT :decided-by :kind
  :signature "struct : IComponent або ім'я з суфіксом Component (не Event, не EventComponent)"
  :graph-now "вид component, 60; роль pk / fk / data за суфіксом"
  :gap "нема"}
 {:recipe PATTERN_TAG :decided-by :kind
  :signature "struct : ITag"
  :graph-now "вид tag, 23"
  :gap "головний чи label — не розрізняється (:od-main-vs-label)"}
 {:recipe PATTERN_EVENT :decided-by :kind
  :signature "struct з суфіксом Event / EventComponent, або тип у EventArchetypes.Of<T> / CreateEvent"
  :graph-now "вид event, 13; DistrictBuildUIRequestedEvent без source_location (поза ROOTS)"
  :gap "корені"}
 {:recipe PATTERN_CONFIG :decided-by :base
  :signature "class : ScriptableObject | SerializedScriptableObject"
  :graph-now "вид config, 35"
  :gap "транзитивна база не йде (через проміжну абстрактну SO — лише пряма база)"}
 {:recipe PATTERN_CONFIG_LOADER :decided-by :di
  :signature "реєстрація Register<ConfigLoaderSystem<X>>().As<IUniTaskSystem>().WithParameter(AppState.ConfigLoading) — 26; реєстрація IUniTaskSystem з WithParameter(AppState.InstanceObjects) — 1 (VertexGridSpawnSystem)"
  :graph-now "di-graph: registers + exposes IUniTaskSystem; WithParameter не розбирається; ecs-graph реєстрацій не бачить"
  :gap "стан застосунку з ланцюжка WithParameter"}
 {:recipe PATTERN_PIPELINE_STAGE :decided-by :base
  :signature "implements IPrioritizedUniTaskSystem<MapGenerationStep>"
  :graph-now "роль pipeline_stage 15 = 14 етапів + абстрактна TurnPhaseSubSystem (generic-аргумент загублено)"
  :gap "зберегти generic-аргумент бази; абстрактні бази — не екземпляри"}
 {:recipe PATTERN_ORCHESTRATOR_SUBSYSTEM :decided-by :lexical
  :signature "абстрактна база (IDisposable | ISystem<GameState> | IUniTaskSystem<GameState> | без бази | TurnPhaseSubSystem) + нащадки + хост, що бере IReadOnlyList<База> у конструкторі"
  :graph-now "ні inherits, ні ребра хост → родина; di-graph має injects collection:true і exposes .As<База>"
  :gap "ребро inherits (транзитивно) і ребро хост → родина"}
 {:recipe PATTERN_REACTIVE_SYSTEM :decided-by :lexical
  :signature "UpdatedSystem, чий base(...) бере EventArchetypes.Of<E> (16) або Query().AnyComponents лише з типів подій (HexInfoPanelDistrictSystem, 1); IsRipe першим рядком"
  :graph-now "роль per_frame; reacts_to ставиться на КОЖЕН виклик EventArchetypes.Of (21 ребро, 5 — не реактивні)"
  :gap "якір у base(...) замість будь-якого виклику; опитувачі — маркер"}
 {:recipe PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM :decided-by :lexical
  :signature "ознака рецепта 8 + хост IReadOnlyList<База>"
  :graph-now "не впізнається"
  :gap "форма дає 1 (DistrictOpenConditionEvaluatorTableChangedSystem); shell BuildDistrictActionSystem — :od-shell"}
 {:recipe PATTERN_PERFRAME_SYSTEM :decided-by :base
  :signature "UpdatedSystem | LateUpdatedSystem | IUpdatedSystem | ILateUpdatedSystem без якоря події в base(...) і без архетипу події в полях — 5"
  :graph-now "роль per_frame змішує 28 різних класів"
  :gap "3 опитувачі (архетип події в полі) — база не вирішує → маркер"}
 {:recipe PATTERN_CLEANUP_SYSTEM :decided-by :lexical
  :signature "IUpdatedSystem з Query().AllTags(Tags.Get<EventTag>()) і DeleteEntity — 1 (EventCleanupSystem)"
  :graph-now "роль cleanup не видається нікому; sets має запис з EventTag"
  :gap "ознака за запитом, не за базою"}
 {:recipe PATTERN_VIEW_SYSTEM :decided-by :marker
  :signature "view (клас у Views/, база MonoBehaviour або DisposedMono) оголошує event Action…; система чи підсистема підписується view.Подія += Handler — 5 підписок, 4 view, 4 підписники; tree-sitter не знає типу змінної view"
  :graph-now "вид view неточний (17 з 4 не-view; 6 view сховані зіткненням імен); підписок не видно"
  :gap "за :amendment-1 пара доповнюється атрибутами; форма атрибута — :od-view-marker"}
 {:recipe PATTERN_TRANSACTION_ENTITY :decided-by :label-tag
  :signature "label-тег ролі в оголошенні архетипу; кандидат — ActionsArchetypes.BuildDistrictInProgress"
  :graph-now "не впізнається — лексичної ознаки нема"
  :gap "тип label-тегу, його дім, розрізнення з головним"}
 {:recipe PATTERN_POLYMORPHIC_CATALOGUE :decided-by :lexical
  :signature "абстрактна база SO + SO-контейнер з полем [SerializeField] База[] + …KindComponent родини — 2"
  :graph-now "не впізнається: нема inherits і типів полів SO"
  :gap "inherits + типи полів конфігів"}
 {:recipe ADDRESSABLE_PATTERNS :decided-by :lexical
  :signature "параметр конструктора типу IAddressable — 7"
  :graph-now "di-graph: injects IAddressable; ecs-graph — нема"
  :gap "злиття фактів DI"}]
```

# Consumers

Усе, що називає старі інструменти або старий закон тегів. `:in-plan` — чи назване в `:where` мапи # Plan;
`false` означає «знайдено на цій стадії», і рядок потрапляє в роботу через meter grep або через закон тегів
(`:od-consumers-scope`).

```clojure
[{:file ".claude/hooks/graph-gate.py" :lines "5-8, 11, 22, 25, 35-37, 79, 110-111" :in-plan true :change "теки артефактів і імена скриптів"}
 {:file ".claude/settings.json" :lines "9" :in-plan false :change "лише якщо змінюється шлях hook"}
 {:file ".gitignore" :lines "9-13" :in-plan true :change "дві теки → одна"}
 {:file "~/.claude/CLAUDE.md" :lines "9-15" :in-plan true :change "розділи ecs-graph і di-graph видаляються"}
 {:file ".sdd-flow/project.md" :lines "32-44 (# Tools), 72 (GLOSSARY :prefer-when), 109-115 (# Meters), 119 (:code-verification), 137-139 (# Ceremonies graph-rebuild)" :in-plan true :change "один інструмент, один meter stats"}
 {:file "CLAUDE.md" :lines "55 (§ 2 :step-3), 63 (§ 3 заборона збірки), 67 (§ 3 артефакти)" :in-plan true :change "новий аудит тегів; «лише Unity-проєкт»; нова тека"}
 {:file "CLAUDE.md" :lines "91-96 (§ 5 notation-ecs-ext :entity-shape, :tag-never-set)" :in-plan false :change "формулювання під label-теги"}
 {:file ".claude/skills/fantasymayor-pattern-choice/SKILL.md" :lines "3, 113-121 (instances), 123-130 (tag-law-before)" :in-plan true :change "pattern <RECIPE>, tags, еталон родин"}
 {:file "Patterns/PATTERN_TAG.md" :lines "13-15, 34, 37, 40" :in-plan true :change "закон тегів, посилання на інструмент"}
 {:file "Patterns/PATTERN_EVENT.md" :lines "59" :in-plan false :change "назва інструмента"}
 {:file "Patterns/PATTERN_COMPONENT.md" :lines "78" :in-plan false :change "назва інструмента"}
 {:file "Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md" :lines "27, 244 (інструменти); 154, 160, 226, 228, 270, 271 (закон тегів)" :in-plan false :change "назви і закон тегів"}
 {:file "Patterns/PATTERN_TRANSACTION_ENTITY.md" :lines "91" :in-plan false :change "«identity tag» → головний тег"}
 {:file "ARCHITECTURE.md" :lines "87-103 (table-rule, tag-law), 121-132 (archetype-law)" :in-plan true :change "головний + label-теги; ask у hook"}
 {:file "GLOSSARY.md" :lines "4 (frontmatter trigger → генерований рядок INDEX.md:36), 15-16" :in-plan false :change "назви інструментів; gen_index після правки frontmatter"}
 {:file "DOC_STANDARD.md" :lines "18, 46-47 (tool-owns)" :in-plan false :change "назви інструментів"}
 {:file "INDEX.md" :lines "81 (зона агента)" :in-plan false :change "назви CLI; :ask-first"}
 {:file "Tools/doc_lint.py" :lines "31-32 (SKIP_DIRS)" :in-plan false :change "нова тека артефактів"}
 {:file "Tools/asmdef_reach.py" :lines "6 (docstring)" :in-plan false :change "назви інструментів"}
 {:file ".sdd-flow/references/PROJECT_ADAPTER.md" :lines "52 (приклад {:tool ecs-graph …})" :in-plan false :change :none :why "керований файл sdd-flow — перезаписується update"}
 {:file "Flows/DOC_AGENT_REVIEW/FLOW.md" :lines "144-145, 361, 458, 467-469, 506, 555, 557, 604, 615" :in-plan false :change :none :why "підтверджена історія іншої задачі"}
 {:file "~/.claude/skills/ecs-graph/ і ~/.claude/skills/di-graph/" :lines "усе" :in-plan true :change "видалити після переносу; репозиторій ~/.claude, коміт — на слово власника"}
 ;; ── пам'ять: ~/.claude/projects/-Users-serhiikharsun-Documents-HomeProjects-FantasyMayor/memory/
 {:file "memory/MEMORY.md" :lines "35, 36, 52, 59" :in-plan true :change "індекс"}
 {:file "memory/project_ecs_graph_tool.md" :lines "2, 3, 11, 20, 22, 25, 26, 29, 33, 40-43, 45 — файл цілком про інструмент" :in-plan true :change "переписати або злити"}
 {:file "memory/project_di_graph_tool.md" :lines "3, 10, 15, 16, 27, 30-32 — файл цілком про інструмент" :in-plan true :change "переписати або злити"}
 {:file "memory/feedback_delegate_graph_curation.md" :lines "3, 10, 12, 14, 16" :in-plan true :change "один build"}
 {:file "memory/feedback_docs_sync_needs_permission.md" :lines "3, 10" :in-plan true :change "назви"}
 {:file "memory/project_dual_agent_contract.md" :lines "58" :in-plan true :change "історична згадка — переформулювати"}
 {:file "memory/project_entity_link_convention.md" :lines "15, 36" :in-plan true :change "назви"}
 {:file "memory/project_fm11_component_roles_program.md" :lines "28, 30, 50, 51" :in-plan true :change "історичні згадки — переформулювати"}
 {:file "memory/project_tool_first_program.md" :lines "41, 47" :in-plan true :change "історичні згадки — переформулювати"}]
```

```clojure
{:grep-used "grep -rn -E 'ecs-graph|di-graph|ecsg\\.py|dig\\.py|build_graph\\.py|build_di_graph\\.py' у репозиторії (--exclude-dir Archive, Library, .git і теки артефактів), у ~/.claude (без skills, projects, plugins, .git, *.jsonl) і в теці пам'яті"
 :global-other "у ~/.claude поза пам'яттю назви стоять лише в CLAUDE.md:9-15 (і в самих двох скілах); skills/unity-asset-graph має власний scripts/build_graph.py — збіг імені файла, назв інструментів там нема"
 :at "2026-09-15"
 :memory-rule "пам'ять feedback_remove_completely: вилучати скрізь за один прохід, історичні згадки переформулювати, перевірити grep = 0"}
```

# Open decisions

Вибір, який цей файл не закриває. s1 будує алгоритм так, щоб він працював за кожним варіантом, або обирає
варіант і пише це в свій артефакт; s2 фіксує і показує власнику в # Contra.

```clojure
[{:id :od-main-vs-label
  :question "як інструмент і аналізатор відрізняють головний тег архетипу від label-тегу"
  :options [{:is "тип label-тегу позначений: атрибут на struct тегу або окремий інтерфейс-нащадок ITag" :confidence 55}
            {:is "позиція: перший type-аргумент Tags.Get — головний" :confidence 30}
            {:is "іменна конвенція label-тегів" :confidence 15}]
  :grounded-in "агент; :holder-parsing, :friflo-arity; суфікс …Tag обов'язковий за naming і doc_lint"}
 {:id :od-uniqueness
  :question "«мітка унікальності»: головний тег належить рівно одному архетипу, чи в архетипі рівно один головний тег, а ділити його можна"
  :options [{:is "рівно один головний на архетип І не спільний між архетипами" :confidence 55}
            {:is "рівно один на архетип, спільний дозволено як категорія" :confidence 45}]
  :grounded-in "текст :main-and-label-tags читається двояко; :tags-today — UITag ×4, DistrictOpenConditionTag ×2"}
 {:id :od-event-tag
  :question "EventTag у новому законі (рішення за s2, показ у # Contra)"
  :options [{:is "EventTag — головний тег архетипів подій; їх ідентичність — компонент події (як виняток сьогодні)" :confidence 55}
            {:is "EventTag — label; архетипи подій без головного тегу, закон їх звільняє" :confidence 30}
            {:is "власний головний тег на кожну подію" :confidence 15}]
  :grounded-in ":tag-filters-across-archetypes (EventCleanupSystem фільтрує за EventTag), :event-tags-duplicated"}
 {:id :od-ui-tag
  :question "UITag і DistrictOpenConditionTag у новому законі (рішення за s2, показ у # Contra)"
  :options [{:is "UITag стає label; кожна панель отримує власний головний тег; OpenConditionSingle / OpenConditionExist — так само" :confidence 50}
            {:is "лишаються головними тегами-категоріями, ідентичність — компонент view чи склад" :confidence 35}
            {:is "UITag прибрати без заміни" :confidence 15}]
  :grounded-in ":tags-today; залежить від :od-uniqueness"}
 {:id :od-marker-home
  :question "де живуть типи атрибутів-маркерів"
  :options [{:is "Assets/Scripts/Core/ поруч зі StateAllowedAttribute (збірка Core)" :confidence 55}
            {:is "Assets/Scripts/EcsExtensions/ (збірка Ecs.Extensions — її бачать і Boot.Implementation, Installers.World)" :confidence 45}]
  :grounded-in ":core-reach, :placement-rules, :state-allowed-precedent"}
 {:id :od-label-home
  :question "де живе тип label-тегу ролі (перший — роль transaction entity)"
  :options [{:is "shared kernel Assets/Scripts/EcsExtensions/, як EventTag і SingletonTag" :confidence 55}
            {:is "Domains.Actions, рольова тека Tags/" :confidence 25}
            {:is "Domains.Kernel (збірка без посилань, лише Data/ActorType.cs)" :confidence 20}]
  :grounded-in ":placement-rules; asmdef_reach path Domains.Actions Domains.Kernel — посилання є"}
 {:id :od-pollers
  :question "який рецепт несе маркер кожного опитувача"
  :options [{:is "TurnProcessorSystem — per-frame (+ хост фаз ходу)" :confidence 75}
            {:is "TurnProcessorSystem — reactive" :confidence 25}
            {:is "BuildDistrictCompletionSystem — reactive у пакетній формі" :confidence 60}
            {:is "BuildDistrictCompletionSystem — per-frame" :confidence 40}
            {:is "DistrictBuildUISystem — per-frame + orchestrator" :confidence 55}
            {:is "DistrictBuildUISystem — reactive orchestrator" :confidence 45}]
  :grounded-in ":pollers — заголовки класів самі описують намір"}
 {:id :od-shell
  :question "як pattern PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM знаходить reactive shell без списку підсистем"
  :options [{:is "маркер на shell (BuildDistrictActionSystem)" :confidence 45}
            {:is "shell — екземпляр рецепта 8, поки нема списку; рецепт 9 чесно показує 1" :confidence 55}]
  :grounded-in ":reactive-orchestrator-today"}
 {:id :od-view-marker
  :question "форма маркера пари view ↔ система"
  :options [{:is "атрибут на підписнику з typeof(view)" :confidence 60}
            {:is "атрибут на view з typeof(системи)" :confidence 15}
            {:is "без атрибута: зв'язок за іменем події, AMBIGUOUS при повторі (гіпотеза FLOW)" :confidence 25}]
  :grounded-in ":amendment-1 («доповнюється атрибутами»), :view-subscriptions, :tree-sitter-attributes"}
 {:id :od-view-pairs
  :question "чи екземпляр рецепта 12 — лише пара з підпискою, чи й система, що лише штовхає значення у view"
  :options [{:is "лише пари з підпискою (4 підписники)" :confidence 55}
            {:is "плюс драйвери view без подій (читають …ViewComponent.View)" :confidence 45}]
  :grounded-in ":view-subscriptions; рецепт 12 описує обидва напрямки"}
 {:id :od-analyzer-scope
  :question "що перевіряє аналізатор"
  :options [{:is "лише маркери: кожен атрибут стоїть на класі тієї форми, яку він стверджує" :confidence 65}
            {:is "плюс надлишковий маркер там, де база вже вирішує, і розміщення label-тегів" :confidence 35}]
  :grounded-in ":drift-guard; :markers-need-a-guard (FLOW)"}
 {:id :od-analyzer-placement
  :question "куди власник кладе DLL аналізатора, щоб перевірка дійшла до збірок з маркерами"
  :options [{:is "у теку збірки з типами атрибутів (Core або Ecs.Extensions) — діє на неї і на збірки, що на неї посилаються" :confidence 65}
            {:is "окрема тека з власним asmdef, на який посилаються ігрові збірки" :confidence 25}
            {:is "корінь Assets" :confidence 10}]
  :grounded-in ":unity-analyzer-docs — корінь Assets діє лише на передвизначені збірки; :core-reach"}
 {:id :od-analyzer-deps
  :question "звідки проєкт аналізатора бере Microsoft.CodeAnalysis і де лежить його код"
  :options [{:is "PackageReference Microsoft.CodeAnalysis.CSharp 4.3 з nuget.org (dotnet restore, мережа)" :confidence 65}
            {:is "HintPath на DLL, що постачається з редактором Unity (ApiUpdater 4.4)" :confidence 35}]
  :home "поза Assets/; тека й назва — s2 (кандидат Tools/<назва>/)"
  :grounded-in ":analyzer-build-env, :unity-compiler"}
 {:id :od-node-ids
  :question "ідентичність вузлів злитого графа"
  :options [{:is "ідентифікатор з видом (архетип окремо від класу з тим самим іменем)" :confidence 60}
            {:is "архетип іменується через холдер (PresentationArchetypes.ForestView), класи — голим іменем" :confidence 40}]
  :grounded-in ":node-name-collisions, :di-generic-identity"}
 {:id :od-roots
  :question "корені сканування"
  :options [{:is "додати Assets/Flows (1 файл)" :confidence 70}
            {:is "лишити 4 корені" :confidence 30}]
  :grounded-in ":roots-miss-flows"}
 {:id :od-refresh
  :question "модель самолікування"
  :options [{:is "повна перебудова при застарілості (модель ecs-graph)" :confidence 80}
            {:is "інкремент за маніфестом (модель di-graph)" :confidence 20}]
  :grounded-in ":ecs-graph-today, :di-graph-today — обидві збірки 0.23 с"}
 {:id :od-grep-scope
  :question "область meter «grep назв старих інструментів = 0»"
  :options [{:is "поза Flows/Archive/, поза всією текою цієї задачі, поза .sdd-flow/ (керований канон) і поза Flows/DOC_AGENT_REVIEW/FLOW.md (чужа історія)" :confidence 70}
            {:is "буквально «поза Archive і цим FLOW» — правити й канон, і чужий FLOW" :confidence 30}]
  :grounded-in "# Consumers; FLOW_CONTRACT flow-document :never «silently rewrite a confirmed decision»; manifest.json managedFiles"}
 {:id :od-consumers-scope
  :question "споживачі, не названі в :where плану (GLOSSARY.md, DOC_STANDARD.md, зона агента INDEX.md, Tools/doc_lint.py, Tools/asmdef_reach.py, PATTERN_EVENT, PATTERN_COMPONENT, PATTERN_POLYMORPHIC_CATALOGUE, PATTERN_TRANSACTION_ENTITY, CLAUDE.md § 5)"
  :options [{:is "у межах задачі — їх вимагає meter grep і закон тегів; s2 називає їх власнику (ask-first для INDEX і ARCHITECTURE)" :confidence 75}
            {:is "лишити — meter grep не пройде" :confidence 25}]
  :grounded-in "# Consumers"}
 {:id :od-tool-name
  :question "назва скіла і скриптів злитого інструмента"
  :options [{:is "fantasymayor-graph (пропозиція :tool-home)" :confidence 60}
            {:is "інша назва за naming policy" :confidence 40}]
  :grounded-in ":tool-home; project-скіли fantasymayor-pattern-choice, fantasymayor-placement задають префікс"}]
```

# Occasions

```clojure
[{:exit "інструмент запущено поза проєктом — над CWD нема Assets/"
  :occasion "обидва білдери вже виходять з помилкою (build_graph.py:1123-1128, build_di_graph.py:485-490) — :ecs-graph-today, :di-graph-today"}
 {:exit "запит до графа, якого ще не зібрано"
  :occasion "ecsg.py:59-63 і dig.py:67-76 завершуються з підказкою зібрати — :ecs-graph-today"}
 {:exit "файл .cs не розбирається"
  :occasion "обидва білдери ловлять виняток на файл і пишуть попередження parse error, скан триває (build_graph.py:1156-1159)"}
 {:exit "виклик generic-холдера з іншою кількістю type-аргументів — архетип не резолвиться"
  :occasion "build_graph.py:842-847 пише попередження «unresolved (forwarding template?)» — :holder-parsing"}
 {:exit "DeleteEntity у системі з кількома архетипами — ребро disposes не приписується"
  :occasion "7 таких попереджень сьогодні — :ecs-graph-today"}
 {:exit "ім'я події view повторюється — зв'язок view ↔ підписник за іменем неоднозначний"
  :occasion "tree-sitter не знає типу змінної view — :view-subscriptions; сьогодні імена унікальні"}
 {:exit "аналізатор не діє — DLL не імпортована, не позначена RoslynAnalyzer або лежить не в тій теці"
  :occasion "імпорт і мітка — Unity-side власника; корінь Assets не покриває asmdef-збірки — :unity-analyzer-docs, :analyzer-build-env"}
 {:exit "label-тегів більше, ніж вміщає Tags.Get поруч із головним"
  :occasion "Tags.Get має до 5 type-аргументів — :friflo-arity"}]
```

# Verification

Meters `:accept` мапи # Plan, розписані конкретно. Базові числа — у # Facts.

```clojure
{:meters [{:meter "<інструмент> pattern <RECIPE> для кожного з 15 рецептів"
           :target "ті самі реалізації, що в еталоні; рецепт без механічної ознаки дає явну відповідь, не порожнечу"
           :reference {:families "mcp__roslyn__get_type_hierarchy direction Descendants на абстрактній базі; mcp__roslyn__find_implementations — лише на інтерфейсі"
                       :systems "grep base(...) і EventArchetypes.Of< — # Facts :reactive-16, :per-frame-composition, :pollers"
                       :di "grep Register<ConfigLoaderSystem< і WithParameter(AppState. — :config-loader-count"
                       :data "ecsg.py stats сьогодні — :data-kinds-today (з поправкою на корені й зіткнення імен)"
                       :expected "component 60, tag 23, event 13, config 35, config-loader 26 + 1, pipeline-stage 14, orchestrator — 11 контрактів / 26 + 3 члени / 12 хостів, reactive 17, reactive-orchestrator 1 (+ shell за :od-shell), per-frame 5 (+ опитувачі за маркерами), cleanup 1, view-system 4 підписники, transaction 1 (після label-тегу), catalogue 2, addressables 7"}}
          {:meter "<інструмент> systems --role per_frame"
           :target "жодної реактивної системи: ні 16 з :reactive-16, ні HexInfoPanelDistrictSystem"}
          {:meter "<інструмент> tags"
           :target "рівно один головний тег на архетип; label-теги показані окремо; відхилень 0"}
          {:meter "<інструмент> stats і <інструмент> build --check (або його відповідник)"
           :target "curated: true; integrity clean (0 висячих ребер); нових попереджень нема проти бази: 8 ecs (7 DeleteEntity + 1 key-role), 0 di"}
          {:meter "CLAUDE.md § 2 code-verification на змінених .cs"
           :target "mcp__roslyn__get_diagnostics чисто на змінених файлах (CS0246 на символах, щойно доданих у сусідніх файлах тієї ж правки, — відома застарілість workspace); /arch-check — без нових порушень; аудит тегів новим інструментом"}
          {:meter "збірка проєкту аналізатора (dotnet build саме проєкту аналізатора, не Unity)"
           :target "DLL netstandard2.0 зібрана без помилок"}
          {:meter "grep -rn -E 'ecs-graph|di-graph|ecsg\\.py|dig\\.py' в області :od-grep-scope: репозиторій, ~/.claude/CLAUDE.md, ~/.claude/skills, тека пам'яті"
           :target "0; ls ~/.claude/skills/ecs-graph ~/.claude/skills/di-graph — нема таких тек"}
          {:meter "python3 Tools/doc_lint.py --quiet і python3 Tools/gen_index.py"
           :target "привидів не більше 3 (база :doc-lint-baseline), 0 помилок Clojure; gen_index без LINT-помилок"}]
 :owner-check "перевірка власника в Unity: після імпорту DLL з міткою RoslynAnalyzer проєкт компілюється; маркер, навмисно поставлений на клас не тієї форми, дає помилку компіляції; поведінка гри не змінилась"}
```

# Complete

```clojure
{:names-every-file-the-next-stage-may-read true
 :states-out-of-scope true
 :ends-with-verification true
 :links-to-follow-on-own-initiative 0}
```
