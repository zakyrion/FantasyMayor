---
category: A
read: always
status: partial
tags: [tools, ecs-graph, patterns]
related:
  - "[DOC_AGENT_REVIEW](../DOC_AGENT_REVIEW/FLOW.md)"
---

# Request

```clojure
{:source :user
 :received-at "2026-09-15"
 :raw-request ["ECS_CONVENTIONS - ще звідкись лізе хоча файл видалено\nтреба розширити ecs-graph для додаткового пошуку по патернах"
               "1 - видаляй ці коментарі скрізь\n2 - так\n3 - command buffer instead\n4 - все\n5 - не зрозумів запитання\n6 - в коді інстурмента\n7 - обоє\n8 - спершу коміть, без різниці в яку гілку\n9 - поки що залиш"
               "5 - поки що за назвою а там подивимося."]}
```

# Confirmed contract

```clojure
{:task :ecs-graph-pattern-instances
 :goal "ecs-graph відповідає «що вже реалізує рецепт X» — крок instances скіла fantasymayor-pattern-choice без читання коду"
 :path :direct                                   ;; 65 — розширення наявного CLI
 :where #{"~/.claude/skills/ecs-graph/ — build_graph.py, ecsg.py, SKILL.md, references/"
          ".claude/skills/fantasymayor-pattern-choice/SKILL.md → instances"}
 :off-limits #{"Assets/" "артефакти графа руками"}   ;; 2026-09-15: голий Assets/ — невалідний символ Clojure; взято в лапки, зміст не змінено
 :decided #{"розширюємо наявний інструмент, не новий"
            "реактивна система відділяється від per_frame за ребром reacts_to"
            "жодного списку реалізацій у документі"
            "усі 15 рецептів Patterns/"
            "запит за назвою рецепта: ecsg.py pattern <RECIPE> — поки що"
            "зв'язок рецепт → ознака в коді — у коді інструмента"
            "родини: ребро inherits в ecs-graph і roslyn find_implementations — обидва"}
 :do ?
 :skip ?
 :read #{ARCHITECTURE.md "~/.claude/skills/ecs-graph/SKILL.md" "Patterns/ — усі 15 рецептів"}
 :skills {fantasymayor-pattern-choice "частина — Python CLI: recipe-tree → :no-recipe, kernel-tree → :none; його instances — те, що змінюється"
          fantasymayor-placement      "не застосовний — нових файлів, asmdef чи інсталерів FantasyMayor нема"}
 :tools #{ecs-graph "roslyn find_implementations — еталон родин для звірки"}
 :accept [{:meter "build_graph.py --check" :target "нових попереджень нема"}
          {:meter "ecsg.py stats" :target "curated: true, привидів нема"}
          {:meter "ecsg.py pattern <RECIPE> проти roslyn find_implementations на базі кожної родини" :target "ті самі члени"}
          {:meter "ecsg.py systems --role per_frame" :target "жодної системи з ребром reacts_to"}]
 :result "один запит ecsg.py повертає реалізації рецепта; реактивні системи не стоять під per_frame"}
```

# Plan

```clojure
{:task :pattern-markers-and-unified-graph
 :path :cascade
 :mode {:owner-sees "лише CASCADE.md # s2 і # Contra"
        :no-owner-gate #{:context :s1}
        :isolation :isolation-in-auto}
 :goal "реалізації 15 рецептів знаходить один інструмент — з бази, а де база не вирішує, з явного маркера: атрибута на класі або label-тегу в архетипі; маркер, що бреше, ламає компіляцію Unity"
 :where #{"ARCHITECTURE.md → Entities: tag-law, table-rule — головний тег + label-теги (:ask-first — схвалення в hook і є дозвіл)"
          "Assets/ — типи атрибутів-маркерів, атрибути на класах, де база не вирішує; label-теги в холдерах архетипів"
          "проєкт аналізатора Roslyn поза Assets/ (netstandard2.0, Microsoft.CodeAnalysis.CSharp 4.3) + зібрана DLL"
          ".claude/skills/<:by-naming-policy>/ — злиття ecs-graph і di-graph: один граф, один CLI, pattern <RECIPE>, виправлені ролі, inherits, хост → родина, реєстрації DI, підписки view, ін'єкції IAddressable, каталоги, маркери"
          "споживачі: .claude/hooks/graph-gate.py, .gitignore, ~/.claude/CLAUDE.md, .sdd-flow/project.md # Tools і # Meters, CLAUDE.md § 2 code-verification і § 3 заборона збірки, скіл fantasymayor-pattern-choice, Patterns/PATTERN_TAG.md, пам'ять; старі ~/.claude/skills/ecs-graph і di-graph — видалити повністю"}
 :off-limits #{"Unity-side: імпорт DLL, .meta, мітка RoslynAnalyzer — власник"
               "збірка Unity-проєкту"
               "Flows/Archive/"}
 :decided #{":scope-tags" ":main-and-label-tags" ":markers-hybrid" ":marker-reader" ":drift-guard"
            ":merge-tools" ":one-task" ":tool-build-allowed" ":tool-home" ":recipes-scope" ":query-shape" ":families"}
 :do (-> (:context "CONTEXT.md з цього FLOW: знахідки, рішення, файли й символи для читання, поза межами, перевірка")
         (:s1 "алгоритм і дані: розпізнавання 15 рецептів, закон тегів, маркери, перевірка форми аналізатором, злиття графів")
         (:s2 "декомпозиція: типи атрибутів, аналізатор, модулі інструмента, зміни споживачів — показ власнику з # Contra")
         (:code "після слова власника на s2")
         (:read-back "кожен змінений файл цілком")
         (:close "/sdd-flow:close на слово власника"))
 :accept [{:meter "<інструмент> pattern <RECIPE> для кожного з 15 рецептів проти roslyn find_implementations і grep" :target "ті самі реалізації; для рецептів без механічної ознаки — явна відповідь"}
          {:meter "<інструмент> systems --role per_frame" :target "жодної реактивної системи"}
          {:meter "<інструмент> tags" :target "рівно один головний тег на архетип"}
          {:meter "<інструмент> stats" :target "curated: true, привидів нема"}
          {:meter "CLAUDE.md § 2 code-verification на змінених .cs" :target "roslyn чисто, arch-check без нових порушень"}
          {:meter "перевірка власника в Unity" :target "компілюється; маркер, що не збігається з формою, дає помилку компіляції"}
          {:meter "grep ecs-graph / di-graph / ecsg.py / dig.py поза Flows/Archive і цим FLOW" :target "0"}
          {:meter "doc_lint + gen_index" :target "привиди не зросли, LINT clean"}]
 :result "агент одним запитом бачить реалізації будь-якого рецепта; роль сутності чи системи або виводиться з бази, або стоїть явним маркером, який перевіряє компіляція"}
```

# Findings

```clojure
[{:hunch "рецепти даних (component, tag, event), config і view уже впізнаються за видом вузла; системні рецепти — за базою + ребром reacts_to + ін'єкцією IReadOnlyList<Base>; бракує ребер inherits, розбору реєстрацій DI (config loader) і підписок view.Event += (view↔system); transaction entity і polymorphic catalogue однією лексичною ознакою не впізнаються"
  :confidence 60
  :grounded-in "знання агента + прочитані рецепти і build_graph.py classify / BASE_ROLE; код реалізацій ще не звірявся"
  :at "2026-09-15"
  :outcome :partly-killed
  :deviation "частина про reacts_to хибна (:reacts-to-is-not-reactive); рецепти, що живуть у реєстраціях DI, перетинаються з di-graph (:di-shaped-recipes) — цього в гіпотезі не було"}

 {:finding :reacts-to-is-not-reactive
  :at "2026-09-15"
  :fact "ребро reacts_to ставиться на КОЖЕН виклик EventArchetypes.Of<T>, де б він не стояв (build_graph.py:753-760). 21 ребро: 16 — системи, чий base(...) конструктора бере архетип події (ознака рецепта reactive); 5 — інше: TurnProcessorSystem і BuildDistrictCompletionSystem (IUpdatedSystem напряму, архетип події в полі, ручний цикл IsRipe щокадру), DistrictBuildUISystem (UpdatedSystem на власному UI-архетипі + поле з архетипом події), DistrictBuildListUISubSystem (підсистема), MainMenuState (стан гри)"
  :verified-by "grep EventArchetypes.Of по Assets/*.cs з перевіркою, чи виклик усередині : base(...); ecsg.py neighbors --rel reacts_to для кожної per_frame системи; прочитав build_graph.py:740-765"
  :consequence "правило «reactive = є ребро reacts_to» назве реактивним і TurnProcessorSystem, який навмисно опитує щокадру; ознака рецепта — якір у base(...)"}

 {:finding :role-conflations
  :at "2026-09-15"
  :fact "ролі systems зараз: per_frame 28 = 16 реактивних + 3 опитувачі подій + 6 справді щокадрових + EventCleanupSystem + дві бази UpdatedSystem / LateUpdatedSystem; роль cleanup не отримує ніхто — BASE_ROLE шукає нащадків EventCleanupSystem, а їх нема; sub_system 30 змішує стартові кроки IUniTaskSystem (ConfigLoaderSystem, ShowHexesUISystem, VertexGridSpawnSystem) з підсистемами родин; pipeline_stage бере будь-який IPrioritizedUniTaskSystem<T> — туди потрапила база TurnPhaseSubSystem (<TurnPhaseStep>; <MapGenerationStep> — 31 реалізація); абстрактні бази (UpdatedSystem, LateUpdatedSystem, TurnPhaseSubSystem, ViewSubSystem, ConfigLoaderSystem) стоять рядками систем"
  :verified-by "ecsg.py systems; grep баз класів і IPrioritizedUniTaskSystem<…> по Assets/*.cs; build_graph.py:47-55, 233-250"
  :consequence "роль виводиться з імені прямої бази підрядком; і systems --role, і майбутній pattern стоять на цій класифікації"}

 {:finding :families-not-in-graph
  :at "2026-09-15"
  :fact "абстрактні бази родин у коді гри мають різні контракти: IDisposable (DistrictBuildUISubSystem), ISystem<GameState> (GenerationSubSystem, HexResourcesSubSystem, HexResourcesViewSubSystem), IUniTaskSystem<GameState> (ViewSubSystem), без бази (MainHudSpawnSubSystem), IPrioritizedUniTaskSystem<TurnPhaseStep> (TurnPhaseSubSystem) та ін.; 12 класів-хостів беруть IReadOnlyList<База> у конструкторі. build_graph бере прямі бази (_bases), але не зберігає їх — ні ребра inherits, ні ребра від хоста до родини; конкретна підсистема отримує роль лише коли ім'я її бази містить SubSystem або відому базу"
  :verified-by "grep abstract class і IReadOnlyList<…> у конструкторах по Assets/*.cs (без Plugins); build_graph.py:326-376"
  :consequence "потрібні два ребра: inherits (клас → пряма база, з обходом транзитивно) і ребро хост → база родини через IReadOnlyList<База>"}

 {:finding :di-shaped-recipes
  :at "2026-09-15"
  :fact "реалізації PATTERN_CONFIG_LOADER — це реєстрації, а не класи: 27 Register<ConfigLoaderSystem<X>> в інсталерах і крок InstanceObjects через .WithParameter(AppState.InstanceObjects) (1 — VertexGridSpawnSystem); реєстрацію «як база» мають і родини підсистем. build_graph реєстрацій не розбирає (ECS_CALLS); di-graph їх уже розбирає (registered AS, Lifetime, installer)"
  :verified-by "grep Register<ConfigLoaderSystem< і AppState.InstanceObjects по Assets/*.cs; build_graph.py:40-43; опис di-graph у ~/.claude/CLAUDE.md"
  :consequence "для цих рецептів або ecs-graph розбирає Register<> сам — два екстрактори одного факту, — або pattern читає факти di-graph"}

 {:finding :view-system-subscriptions
  :at "2026-09-15"
  :fact "5 підписок view.Event += Handler у 3 системах і підсистемах UI проти 5 оголошень public event Action… у 4 view; MonoBehaviour уже вузли kind=view (17). tree-sitter не знає типу змінної view — зв'язати можна лише за іменем події, оголошеної у view-класі; сьогодні імена унікальні"
  :verified-by "grep «.X += » поза Views/ і «public event Action» по Assets/*.cs; ecsg.py stats"
  :consequence "пара view ↔ система впізнається за іменем події, з позначкою AMBIGUOUS, коли ім'я не унікальне"}

 {:finding :addressable-injections
  :at "2026-09-15"
  :fact "IAddressable інжектують конструктором 7 класів (ConfigLoaderSystem, ShowHexesUISystem, MainHudSpawnSystem, DistrictBuildUISpawnSystem, TerrainViewSystem, WaterViewSubSystem, HexSelectionViewLoadingSystem); не витягується"
  :verified-by "grep «IAddressable <ім'я>» по Assets/*.cs"
  :consequence "ознака — тип параметра конструктора, лексична"}

 {:finding :catalogue-signature
  :at "2026-09-15"
  :fact "ознака «абстрактна база ScriptableObject + ScriptableObject-контейнер із [SerializeField] База[] + …KindComponent» дає рівно 2 каталоги — DistrictOpenCondition і DistrictBuildOutcome; дві інші абстрактні бази SO без контейнера (ResourceConfig, IsolineConfig) — варіант PATTERN_CONFIG «два ассети однієї форми», ознака їх відсікає"
  :verified-by "grep abstract class …: ScriptableObject, [SerializeField] private …Config[], struct …KindComponent, використання ResourceConfig / IsolineConfig по Assets/*.cs"
  :consequence "каталог впізнається механічно, якщо граф знає inherits і типи полів конфігів"}

 {:finding :transaction-entity-no-signature
  :at "2026-09-15"
  :fact "єдиний живий кандидат — архетип BuildDistrictInProgress у ActionsArchetypes, і рецепт сам називає його виродженим випадком (без стадій). Лексичної ознаки, окремої від «архетип у холдері домену дієслова», нема; ярус домену (substrate / agents / verbs) — правило скіла placement, інструмент ярусів не знає"
  :verified-by "PATTERN_TRANSACTION_ENTITY.md; grep методів ActionsArchetypes"
  :consequence "або проксі за простором імен домену, або pattern чесно каже «механічно не впізнається»"}

 {:finding :data-recipes-already-kinds
  :at "2026-09-15"
  :fact "COMPONENT, TAG, EVENT, CONFIG і VIEW уже вузли свого виду: component 60, tag 23, event 13, config 35, view 17; подія впізнається лише за суфіксом Event / EventComponent"
  :verified-by "ecsg.py stats; build_graph.py:223-250"
  :consequence "для них pattern — фільтр за видом, нової екстракції не треба"}

 {:finding :markers-known-uses
  :at "2026-09-15"
  :fact "явні маркери ролей у коді, які читає інструмент, — усталена практика: jMolecules (Java — стереотипи анотаціями чи інтерфейсами; читають ArchUnit, IDE-перегляд за стереотипами, документація Spring Modulith); Structurizr.Annotations (.NET — атрибути «компонент» і «використовує компонент», щоб зробити витяг компонентів явним або доповнити його); Entitas (ECS — атрибути [Unique], [Event] на компонентах читає генератор коду, через reflection або Roslyn)"
  :verified-by "прочитав поза проєктом: odrotbohm.de/2025/11/jmolecules-2.0-stereotypical, результати пошуку Structurizr.Annotations (nuget, dotnet-extensions), вікі та issue #962 Entitas; сторінку документації Structurizr відкрити не вдалося (404), PDF jMolecules не прочитався"
  :evidence "документація інструментів і практики — стверджують, ніхто не міряв"
  :consequence "напрям власника має відомі аналоги, зокрема в ECS"}

 {:finding :markers-without-annotation
  :at "2026-09-15"
  :fact "jMolecules 2.0 дає стереотип і БЕЗ анотації на класі — правилом у JSON: «кожен тип, що реалізує X або позначений Y, — стереотип Z»; маркером може бути і однозначна база"
  :verified-by "прочитав odrotbohm.de/2025/11/jmolecules-2.0-stereotypical"
  :evidence "один джерельний допис автора бібліотеки"
  :consequence "гібрид можливий: роль з бази там, де база вирішує однозначно; атрибут — лише там, де не вирішує (реактивна проти опитувача на тій самій UpdatedSystem, transaction entity, пара view ↔ система)"}

 {:finding :markers-need-a-guard
  :at "2026-09-15"
  :fact "у prior art маркер ходить у парі з виконуваною перевіркою: правила ArchUnit над поняттями jMolecules, «правило як сенсор на кожен коміт, а не домовленість»; жодне джерело не міряло, як часто маркери розходяться з кодом"
  :verified-by "результати пошуку: dzone (jMolecules governance), loiane.com 2026-07 ArchUnit, contextmapper.org"
  :evidence "стверджують практики; вимірів нема"
  :consequence "маркер без перевірки форми — нове джерело неправди; у проєкті без тестів роль сенсора може грати lint інструмента або аналізатор Roslyn у компіляції Unity"}

 {:finding :tree-sitter-reads-attributes
  :at "2026-09-15"
  :fact "tree-sitter-c-sharp розбирає атрибути повністю, разом з аргументами typeof(...); поточна збірка графа — 358 файлів за 0.23 с. Маркери синтаксичні: щоб їх ПРОЧИТАТИ, Roslyn не потрібен; Roslyn додає розв'язання типів — транзитивні бази, generic-аргументи, однакові імена в різних просторах імен"
  :verified-by "запустив tree-sitter на прикладі [ReactiveSystem(typeof(FooEvent))] class X : UpdatedSystem; build_graph.py — час 0.30 с"
  :evidence "виміряно тут"
  :consequence "«Roslyn збирає маркери» — не єдиний шлях; наявний екстрактор читає їх без збірки"}

 {:finding :roslyn-options
  :at "2026-09-15"
  :fact "три шляхи Roslyn: (a) окремий CLI на MSBuildWorkspace — design-time build проєктів без емісії; у проєкті 62 csproj від Unity; є звіти про хвилини на великих рішеннях (Roslyn.sln ~4 хв; 300 проєктів — години); dotnet SDK 8 і 10 встановлені; сам CLI треба компілювати. (b) аналізатор або генератор у компіляції Unity — netstandard2.0, Microsoft.CodeAnalysis.CSharp 4.3, мітка ассета RoslynAnalyzer; документація описує лише AddSource і діагностики, запис файлів на диск не описаний; може падати помилкою компіляції, коли маркер не збігається з формою. (c) roslyn MCP — лише для агента, скрипт його не викличе"
  :verified-by "docs.unity3d.com 6000.0 create-source-generator; результати пошуку: RoslynIndexer, Steve Gordon, dotnet/roslyn #23823 і #14325; ls *.csproj; dotnet --list-sdks"
  :evidence "документація Unity — стверджує; швидкість MSBuildWorkspace — чужі звіти, тут не міряно"
  :consequence "Roslyn дорожчий за tree-sitter у кожному варіанті; його сильна сторона — перевірка форми, а не читання маркерів"}

 {:finding :syntactic-graph-cost-quality
  :at "2026-09-15"
  :fact "граф знань на tree-sitter для агента (Codebase-Memory, arXiv 2603.27277, 31 репозиторій): ~10× менше токенів, 2.1× менше викликів, якість відповіді 83% проти 92% у агента, що читає файли; автори домальовують гібридне розв'язання типів, щоб закрити сліпі плями"
  :verified-by "прочитав arxiv.org/html/2603.27277v1"
  :evidence "виміряно в статті; інша мова й інші питання, ніж тут"
  :consequence "синтаксичний граф дешевий, але втрачає точність там, де потрібні типи; явні маркери закривають саме цю втрату"}

 {:finding :inference-is-never-exact
  :at "2026-09-15"
  :fact "дослідження автоматичного пошуку патернів: правила й анотації вимагають ручної роботи експерта; класифікатори на ML дають ~80-87% точності й повноти на патернах GoF"
  :verified-by "результати пошуку: ResearchGate survey DPD, arXiv 2512.07193, ACM attention-based DPD — зведення пошуковика, статті повністю не читав"
  :evidence "вторинне зведення"
  :consequence "для 15 локальних рецептів, де потрібна точна відповідь, чистий вивід не підходить — ще один голос за маркери"}

 {:finding :tag-label-unknown
  :at "2026-09-15"
  :fact "терміна TagLabel нема ні в C#/ECS-джерелах, ні в репо, ні у Friflo: у Friflo [TagName] і [ComponentKey] — імена для JSON; у Unity «label» — мітка ассета (наприклад RoslynAnalyzer)"
  :verified-by "grep по Assets, ~/.claude; Friflo.Engine.ECS.xml 3.6.0; пошук «TagLabel attribute C# ECS»"
  :evidence "пошук нічого не дав"
  :consequence "значення TagLabel — питання власнику"}

 {:finding :context-corrections
  :at "2026-09-15"
  :fact "стадія CONTEXT уточнила знахідки проходу 1: реєстрацій ConfigLoaderSystem 26, не 27; класів-підписників view 4, не 3; справді щокадрових систем 5, не 6 — HexInfoPanelDistrictSystem реактивна через запит AnyComponents за типами подій; у ResourceConfig є контейнер (масив записів-структур), ознака каталогу його все одно відсікає; find_implementations повертає 0 на абстрактних класах — еталон родин лише get_type_hierarchy Descendants (та сама вада в :families скіла pattern-choice)"
  :verified-by "Flows/ECS_GRAPH_PATTERN_INSTANCES/CONTEXT.md # Facts — субагент стадії context, кожен факт зі своїм :verified-by"
  :consequence "числа й еталон звірки беруться з CONTEXT.md; попередні знахідки лишаються як записані"}]
```

# Decisions

```clojure
[{:decision :recipes-scope
  :status :confirmed
  :at "2026-09-15"
  :value "усі 15 рецептів, включно з view, config, addressables"
  :offered [{:option "лише системні" :confidence 55} {:option "плюс даних" :confidence 35} {:option "усі 15" :confidence 10}]
  :verified-by "власник: «4 - все»"
  :reason "власник обрав найширше покриття"}

 {:decision :query-shape
  :status :confirmed
  :at "2026-09-15"
  :value "за назвою рецепта — ecsg.py pattern <RECIPE>; поки що, форму переглянемо після використання"
  :offered [{:option "за назвою рецепта" :confidence 80} {:option "за роллю системи" :confidence 20}]
  :verified-by "власник: «5 - поки що за назвою а там подивимося.»"
  :reason "роль мають лише системи; компоненти, конфіги й view за роллю не знайти"}

 {:decision :signature-home
  :status :confirmed
  :at "2026-09-15"
  :value "зв'язок рецепт → ознака в коді живе в коді інструмента"
  :offered [{:option "у коді інструмента" :confidence 65} {:option "у фронтматері рецепта" :confidence 35}]
  :verified-by "власник: «6 - в коді інстурмента»"
  :reason "не зачіпає gen_index і doc_lint"}

 {:decision :families
  :status :confirmed
  :at "2026-09-15"
  :value "ребро inherits в ecs-graph і roslyn find_implementations лишається"
  :offered [{:option "inherits в ecs-graph" :confidence 60} {:option "лишити roslyn" :confidence 40}]
  :verified-by "власник: «7 - обоє»"
  :reason "граф відповідає сам, roslyn — звірка"}

 {:decision :baseline-commit
  :status :confirmed
  :at "2026-09-15"
  :value "незакомічена робоча версія ecs-graph закомічена як база: ~/.claude af626ef, гілка main, лише skills/ecs-graph"
  :offered [{:option "спершу закомітити" :confidence 70} {:option "поверх без коміту" :confidence 30}]
  :verified-by "власник: «8 - спершу коміть, без різниці в яку гілку»; git log ~/.claude"
  :reason "розширення починається з чистої бази"}

 {:decision :parallel-with-review
  :status :confirmed
  :at "2026-09-15"
  :value "DOC_AGENT_REVIEW лишається активним; ця задача йде паралельно"
  :offered [{:option "закрити ревʼю" :confidence 55} {:option "лишити паралельно" :confidence 45}]
  :verified-by "власник: «9 - поки що залиш»"
  :reason "власник не закриває ревʼю"}

 {:decision :scope-tags
  :status :confirmed
  :at "2026-09-15"
  :value "TagLabel — окремі ECS-теги: один головний тег — мітка унікальності (дискримінатор), окремі scope-теги — мітка належності до системи чи ролі; Tag Law «1 сутність = 1 тег» переглядається"
  :offered [{:option "атрибут-мітка з назвою ролі" :confidence 60} {:option "тег Friflo як маркер ролі" :confidence 30} {:option "мітка ассета Unity" :confidence 10}]
  :verified-by "власник: «1 - це окремі теги. Зараз в нас вже є правило 1 сутність = 1 тег, але ми можемо його переглянути. Буде головний main tag … і окремі scope теги, як мітка належності до системи чи ролі»"
  :reason "роль сутності (transaction entity тощо) механічно не виводиться — її несе тег в оголошенні архетипу"}

 {:decision :markers-hybrid
  :status :confirmed
  :at "2026-09-15"
  :value "роль береться з бази, де база вирішує однозначно; атрибут — лише там, де не вирішує"
  :offered [{:option "гібрид" :confidence 65} {:option "атрибут на кожному класі" :confidence 35}]
  :verified-by "власник: «2 - гібрид.»"
  :reason "менше правок і менше місць, де маркер може збрехати"}

 {:decision :marker-reader
  :status :confirmed
  :supersedes :signature-home
  :at "2026-09-15"
  :value "маркери читає наявний Python-інструмент на tree-sitter"
  :offered [{:option "Python на tree-sitter" :confidence 60} {:option "аналізатор Roslyn у Unity" :confidence 20} {:option "окремий Roslyn CLI" :confidence 20}]
  :verified-by "власник: «3 - наявний Python-інструмент»; tree-sitter розбирає атрибути з typeof — виміряно (:tree-sitter-reads-attributes)"
  :reason "було: ознака лише в коді інструмента; нове: ознака — база або явний маркер у коді гри, читає той самий інструмент"}

 {:decision :drift-guard
  :status :confirmed
  :at "2026-09-15"
  :value "маркер, що не збігається з формою класу, — помилка компіляції: аналізатор Roslyn у компіляції Unity"
  :offered [{:option "попередження в --check" :confidence 60} {:option "помилка компіляції" :confidence 30} {:option "без сторожа" :confidence 10}]
  :verified-by "власник: «4 - помилка компіляції»"
  :reason "сторож спрацьовує в Unity на кожній компіляції, а не лише коли хтось запустив інструмент"}

 {:decision :merge-tools
  :status :confirmed
  :at "2026-09-15"
  :value "ecs-graph і di-graph зливаються в один інструмент: один граф, один CLI"
  :offered [{:option "злити в один" :confidence 70} {:option "CLI поверх двох графів" :confidence 30}]
  :verified-by "власник: «5 - так злити»"
  :reason "рецепти, що живуть у DI, і рецепти ECS відповідає один інструмент"}

 {:decision :one-task
  :status :confirmed
  :at "2026-09-15"
  :value "маркери в коді, перегляд Tag Law, аналізатор і злиття інструментів — одна задача"
  :offered [{:option "дві задачі" :confidence 65} {:option "одна задача" :confidence 35}]
  :verified-by "власник: «6 - одна задача»"
  :reason "власник веде як одне ціле"}

 {:decision :path-cascade-auto
  :status :confirmed
  :supersedes :path-direct-in-contract
  :at "2026-09-15"
  :value "шлях :cascade в авто-режимі: власник бачить лише s2 (CASCADE.md # s2 і # Contra) як підсумок рішень; ворота CONTEXT і s1 без його слова"
  :offered [{:option ":cascade" :confidence 70} {:option ":direct" :confidence 30}]
  :verified-by "власник: «1 - :cascade але в auto режимі, тобто я хочу побачити лише s2 артефакт як підсумок того що ти вирішив зробити»"
  :reason "було: :direct 65 у постановці; нове: п'ять частин — Tag Law, маркери, аналізатор, злиття інструментів, споживачі — мінімальною зміною не робляться"}

 {:decision :isolation-in-auto
  :status :confirmed
  :at "2026-09-15"
  :value "кожну стадію (context → s1 → s2) веде окремий субагент на Opus із чистим контекстом, послідовно; головний агент показує власнику s2 з файлу"
  :offered [{:option "власник очищає контекст сам" :confidence 60} {:option "усе в одній сесії" :confidence 25} {:option "субагенти" :confidence 15}]
  :verified-by "власник: «1 - go, через суб агентів на Opus 5 Extra запусти їх послідовно і результат з файлу потім виведеш сюди»"
  :reason "власник обрав субагентів попри канон (stage-isolation :mechanism) і project.md :subagents :abandoned — рішення лише для цього каскаду; рівень effort субагенту інструмент Agent не задає"}

 {:decision :main-and-label-tags
  :status :confirmed
  :at "2026-09-15"
  :value {:main-tag "рівно один на архетип — мітка унікальності типу сутності; системи фільтрують за архетипом, тобто за ним"
          :label-tags "додаткові теги в оголошенні архетипу — маркери ролі чи належності; системи за ними не фільтрують"
          :analysis "пост-фільтрація: спершу всі архетипи — головний тег, компоненти, теги; далі їхні додаткові label-теги"
          :direction "код усюди більше спирається на архетипи й фільтрацію за архетипами"
          :open "EventTag і UITag у новому законі — вирішує s2 і показує в # Contra"}
  :verified-by "власник: «2 - системи все одно будуть фільтрувати по головному тегу, але задля маркерів можемо додавати й решту тегів … кожен тип entity буде мати лише 1 головний тег, то аналізом коду можна легко проводити пост фільтрацію … Ми взагалі в усьому коду будемо більше спиратися на архетипи та фільтрацію за архетипами.»"
  :reason "роль сутності несе label-тег у холдері архетипу; tree-sitter його бачить, бо архетипи оголошені"}

 {:decision :tool-build-allowed
  :status :confirmed
  :at "2026-09-15"
  :value "заборона збірки — лише для Unity-проєкту; інструменти (аналізатор Roslyn) збирати можна; формулювання CLAUDE.md § 3 уточнюється в цій задачі"
  :offered [{:option "виняток для проєкту аналізатора" :confidence 55} {:option "DLL збирає власник" :confidence 45}]
  :verified-by "власник: «3 - білд заборонено лише для Unity проєкту, для тулів білд дозволено»"
  :reason "текст бану «dotnet build, msbuild» читався як заборона будь-якої збірки"}

 {:decision :tool-home
  :status :confirmed
  :at "2026-09-15"
  :value "злитий інструмент живе в .claude/skills/ проєкту; назва — :by-naming-policy, пропозиція fantasymayor-graph, остаточно в s2"
  :offered [{:option "у проєкті" :confidence 65} {:option "у ~/.claude/skills" :confidence 35}]
  :verified-by "власник: «4 - в проєкті»"
  :reason "інструмент знає рецепти й маркери саме FantasyMayor"}

 {:decision :s2-accepted
  :status :confirmed
  :at "2026-09-15"
  :value {:s2 "прийнято разом з CASCADE.md → «Поправки власника на воротах s2»; це й дозвіл на показані правки Patterns/ та рядок INDEX.md:83"
          :od-uniqueness "головний тег не спільний; EventTag — загальний виняток для прибирання подій"
          :od-ui-tag "UITag і DistrictOpenConditionTag — label"
          :marker-type-shape "один атрибут з переліком"
          :od-view-pairs "view-system — лише C#-підписка"
          :di-facet-values "через кому"
          :legacy-cleanup-lifetime "ок"
          :code-stage "код пише субагент за артефактом s2; правки ARCHITECTURE.md — головний агент, hook питає власника (c-1 :fix-a)"}
  :verified-by "власник: «1 - в нас по суті є ще й PK … бачу EventTag як загальний вийняток, бо він існує для того щоб відпрацьовувала система видалення подій 2 - погоджуюся що це будуть label 3 - 1 атрибут з enum значенням … 4 - давай лише за C# підпискою і стандартизуємо це як рішення … 5 - через кому 6 - ок … s2 - примаю код пише теж саб агент по s2 артефакту»"
  :reason "ворота s2"}

 {:decision :view-boundary
  :status :confirmed
  :at "2026-09-15"
  :value "MonoBehaviour-view не створює сутностей і подій і не отримує EntityStorages — закон у ARCHITECTURE.md → Entities (view-boundary), відхилення показує pattern PATTERN_VIEW_SYSTEM; помилка компіляції — ні, лише на окреме слово"
  :verified-by "власник: «стандартизуємо це як рішення. Що б View MonoBehaviour не створювали Entity»; grep 2026-09-15 — 4 view вже порушують: HexesUI, HexInfoPanelView, TurnPanelView, ContextTabsView"
  :reason "правило жило лише в рецепті PATTERN_VIEW_SYSTEM; виправлення чотирьох view — рефакторинг поза задачею"}

 {:decision :read-back-verdict
  :status :confirmed
  :at "2026-09-15"
  :value {:unity "DLL аналізатора імпортовано після зняття платформи Editor і мітки RoslynAnalyzer; проєкт компілюється, сторож працює"
          :read-back "прийнято; усі 12 знахідок, що чекали власника (rb-13 … rb-24), застосувати за найвище оціненими варіантами"
          :commit "після застосування — коміт усього"}
  :verified-by "власник: «шикарно, все працює. Застосовуй все і потім все комітимо»; перед тим — помилка імпорту DLL «Unable to resolve reference 'Microsoft.CodeAnalysis'», .meta: Editor enabled 1 і без мітки (c-6)"
  :reason "вердикт на read-back і перевірка власника в Unity"}]
```

# Disproven

```clojure
[{:hypothesis "реактивну систему можна відділити від per_frame за наявністю ребра reacts_to (було в :decided постановки)"
  :refuted-by "reacts_to ставиться на кожен виклик EventArchetypes.Of<T>: 5 з 21 ребра — не реактивні системи рецепта, серед них TurnProcessorSystem, що навмисно опитує щокадру"
  :at "2026-09-15"
  :details "# Findings → :reacts-to-is-not-reactive"}]
```

# Attempted

```clojure
[]
```

# Progress

```clojure
{:status :active
 :completed #{"постановка підтверджена 2026-09-15; база ecs-graph закомічена af626ef"
              "CONTEXT.md, CASCADE.md # s1, # s2 — субагенти 2026-09-15; s2 прийнято власником"}
 :current "знахідки read-back застосовано, converge і calibration записано; чекає коміту і /sdd-flow:close"
 :stage :ready-to-close
 :next-invocation "після фіксів і метрів: коміти в репо ([FM-14]) і в ~/.claude на слово власника «потім все комітимо»; закриття — /sdd-flow:close на слово власника"
 :refactoring-later #{"4 view порушують view-boundary: HexesUI, HexInfoPanelView, TurnPanelView, ContextTabsView — EntityStorages + CreateEvent у view"}
 :prior-art-guess-outcome {:outcome :confirmed-with-deviation
                           :deviation "маркер не обов'язково атрибут — jMolecules дає правило за базою (:markers-without-annotation); маркери читає tree-sitter без Roslyn (:tree-sitter-reads-attributes)"}
 :prior-art-guess {:hunch "явні маркери точніші за вивід, і інструменти, що їх читають, існують (jMolecules + ArchUnit, Structurizr-анотації); ціна — дрейф: маркер бреше, коли код змінився, тому prior art поєднує маркер із правилом, яке перевіряє, що позначений клас справді має форму; Roslyn розв'язує типи й наслідування, яких не бачить tree-sitter, але на Unity-проєкті потребує згенерованих csproj і в рази повільніший за 0.2 с"
                   :confidence 55
                   :grounded-in "знання агента — до пошуку"
                   :at "2026-09-15"}
 :remaining #{"ворота знахідок проходу 2" "план і мапа реалізації → go"}
 :resume-context "Дослідження проходу 1: для кожного з 15 рецептів — ознака реалізації в коді, чи бачить її build_graph.py зараз, звірка з roslyn/grep. Код ecs-graph: ~/.claude/skills/ecs-graph/scripts (git ~/.claude)."}
```

# Acceptance

```clojure
[{:meter "build_graph.py --check" :target "нових попереджень нема"
  :actual "2026-09-15, замінник fmgraph.py check: curated true, integrity clean, попереджень 8 — 7 DeleteEntity «attribute by hand» + 1 key-role HexIdFKComponent, ті самі, що в базі; нових 0"
  :actual-after-read-back-fixes "2026-09-15, після rb-13 … rb-24: curated true, integrity clean, попереджень 9 до і після проходу — ті самі 8 + «redundant marker: CameraMovementSystem claims SystemRole, but its role per_frame is decided by base» від ручного маркера власника на CameraMovementSystem.cs:23 (після read-back); правки задачі нових попереджень не дали"
  :status :blocked-by-owner
  :blocker "дев'яте попередження — ручна правка власника: прибрати маркер (роль вирішує база LateUpdatedSystem) або прийняти його"}
 {:meter "ecsg.py stats" :target "curated: true, привидів нема"
  :actual "2026-09-15, fmgraph.py stats: 371 файл, вузлів 479, ребер 1255; reacts_to 20, polls 4, registers 101, exposes 73, injects 129 (126 + 3 параметри ConfigLoaderSystem), runs_in 29, hosts 12, subscribes 5; ролі reactive 18, per_frame 7, cleanup 1, pipeline_stage 14, turn_phase 3, startup_step 2, sub_system 26; check — висячих ребер 0"
  :actual-after-read-back-fixes "2026-09-15: ті самі вузли 479, ребра 1255 і лічильники rel, ролей і рецептів; попереджень 9 (див. check); curated true; doc_lint — 3 привиди, не цієї задачі"
  :status :met}
 {:meter "ecsg.py pattern <RECIPE> проти roslyn find_implementations" :target "ті самі члени"
  :actual "2026-09-15, fmgraph.py pattern на 15 рецептах: component 60, tag 30 (27 головних + 3 label), event 13, config 39 = 31 + 8 нащадків 4 abstract SO-баз за roslyn get_type_hierarchy Descendants, config loader 26 + 1 instance_objects, pipeline 14, orchestrator 10 контрактів / 29 членів / 12 хостів — члени збіглися з roslyn Descendants на 10 abstract базах, reactive 18, reactive orchestrator 1, per_frame 7, cleanup 1, view 4 підписники / 5 підписок / 4 view + 12 відхилень view-boundary у 4 view (HexesUI, HexInfoPanelView, TurnPanelView, ContextTabsView), transaction 1, catalogue 2, addressable 7"
  :actual-after-read-back-fixes "2026-09-15: pattern на 15 рецептах — вивід до і після проходу однаковий рядок у рядок; документ графа в пам'яті — ідентичний"
  :status :met}
 {:meter "ecsg.py systems --role per_frame" :target "жодної системи з ребром reacts_to"
  :actual "2026-09-15, fmgraph.py systems --role per_frame: 7 — CameraMovementSystem, HexSelectionSystem, ResourceBarSystem, TurnPanelViewSystem, HexIconsContainerPositionSystem (base), TurnProcessorSystem, DistrictBuildUISystem (marker); жодної реактивної; tags — 0 відхилень"
  :actual-after-read-back-fixes "2026-09-15: 7, той самий перелік; CameraMovementSystem лишається (base) попри ручний маркер"
  :status :met}
 {:meter "fmgraph.py tags (# Plan :accept)" :target "рівно один головний тег на архетип"
  :actual "2026-09-15, після rb-13 … rb-24: 39 архетипів, у кожного один головний тег; 0 відхилень; типів тегів 30 — 27 головних, 3 label"
  :status :met}
 {:meter "CLAUDE.md § 2 code-verification на змінених .cs (# Plan :accept)" :target "roslyn чисто, arch-check без нових порушень"
  :actual "2026-09-15, прохід rb: mcp__roslyn__get_diagnostics — DistrictSingleOpenConditionSpawnSubSystem.cs 0, DistrictExistConditionSpawnSubSystem.cs 0, MarkedTypeCheck.cs (MarkerShapeAnalyzer.csproj) 0; /arch-check MarkedTypeCheck.cs — 0 (не система, без Generic і алокаторів); fmgraph.py tags — 0 відхилень; dotnet build MarkerShapeAnalyzer -c Release — 0 попереджень, 0 помилок"
  :status :met}
 {:meter "перевірка власника в Unity (# Plan :accept)" :target "компілюється; маркер, що не збігається з формою, дає помилку компіляції"
  :actual "2026-09-15, власник: DLL імпортовано (Editor знято, мітка RoslynAnalyzer), проєкт компілюється, сторож працює — «шикарно, все працює». Після rb-20 DLL перезібрано і замінено лише бінарник (.meta не чіпано); димовий прогін поза Unity — ті самі FM1001-FM1004, змінився тільки this(...)-випадок"
  :status :recheck-by-owner
  :blocker "Unity ще не компілював DLL після rb-20 — погляд на консоль після реімпорту"}
 {:meter "grep ecs-graph / di-graph / ecsg.py / dig.py поза Flows/Archive і цим FLOW (# Plan :accept)" :target "0"
  :actual "2026-09-15: 0 поза Flows/ і .sdd-flow/references (там приклад канону PROJECT_ADAPTER.md:52 — :od-grep-scope); .sdd-flow/project.md 0; ~/.claude/skills/ecs-graph і di-graph відсутні"
  :status :met}
 {:meter "doc_lint + gen_index (# Plan :accept)" :target "привиди не зросли, LINT clean"
  :actual "2026-09-15: doc_lint — 3 привиди у 2 файлах (HexIdComponent у PATTERN_COMPONENT і PATTERN_TRANSACTION_ENTITY, не цієї задачі), 0 помилок Clojure; gen_index — LINT clean, INDEX.md не змінився"
  :status :met}]
```

# Amendments

```clojure
[{:received-at "2026-09-15"
  :raw-request "1 - ок, бачу проблему що певні речі з коду хер зрозумілі для статичного аналізу. Тоді треба вводити атрибути та TagLabel. Для того щоб маркувати компоненти, системи та інші класи щоб Roslyn міг збирати інформацію з прогону.\n2 - так\n3 - зводимо все в 1 тулу\n4 - ось тут нам допоможуть TagLabel-и\n5 - атрибутами доповнимо\n6 - шукай, це важливо. Можливо після цього пошуку я зміню певні рішення та думки\n7 - відкладемо це не потім"
  :normalized {:direction "явні маркери в коді — атрибути і TagLabel на компонентах, системах та інших класах, щоб Roslyn збирав те, чого статичний аналіз не виводить"
               :roles "виправити ролі в systems і instances скіла pattern-choice — так"
               :tool "усе зводиться в один інструмент"
               :transaction-entity "маркується TagLabel"
               :view-system "доповнюється атрибутами"
               :prior-art "пошук в інтернеті дозволено; власник може змінити рішення після нього"
               :off-limits-assets "уточнення відкладено"
               :open #{"що таке TagLabel — у репо, пакетах і Friflo такого нема (Friflo TagName / ComponentKey — імена для JSON)"
                       "який Roslyn: MCP, окремий CLI на Roslyn чи генератор / аналізатор у компіляції Unity"
                       "межа «однієї тули»: ecs-graph + di-graph + pattern?"
                       "Assets/ стає місцем змін — маркери в коді"}}
  :supersedes #{":signature-home — ознака живе в коді інструмента" ":decided — реактивна відділяється за reacts_to"}
  :confirmed true}                       ;; 2026-09-15: відкриті питання закриті другою поправкою і рішеннями

 {:received-at "2026-09-15"
  :raw-request "1 - це окремі теги. Зараз в нас вже є правило 1 сутність = 1 тег, але ми можемо його переглянути. Буде головний main tag який використовуватиметься як мітка унікальності і окремі scope теги, як мітка належності до системи чи ролі\n2 - гібрид. \n3 - наявний Python-інструмент\n4 - помилка компіляції\n5 - так злити \n6 - одна задача"
  :normalized {:task :pattern-markers-and-unified-graph
               :goal "реалізації 15 рецептів знаходить один інструмент — з бази, а де база не вирішує, з явного маркера; маркер, що бреше, ламає компіляцію Unity"
               :path :by-proposal                   ;; було :direct 65 — обсяг виріс, пропозиція :cascade
               :where #{"ARCHITECTURE.md → Entities (tag-law, table-rule)"
                        "Assets/ — типи атрибутів, атрибути на класах, scope-теги в холдерах архетипів"
                        "проєкт аналізатора Roslyn поза Assets/ + DLL у Unity"
                        "інструмент — злиття ecs-graph і di-graph"
                        "споживачі: graph-gate hook, ~/.claude/CLAUDE.md, .sdd-flow/project.md, скіл fantasymayor-pattern-choice, PATTERN_TAG, пам'ять"}
               :decided #{":scope-tags" ":markers-hybrid" ":marker-reader" ":drift-guard" ":merge-tools" ":one-task"}
               :open #{"шлях: :cascade чи :direct"
                       "форма перегляду Tag Law: скільки scope-тегів, чи фільтрує система за scope-тегом поперек архетипів, EventTag і UITag як scope-теги"
                       "збірка DLL аналізатора — dotnet build під забороною CLAUDE.md § 3"
                       "назва і дім злитого інструмента"}}
  :confirmed true}]                      ;; 2026-09-15: відкриті питання закриті рішеннями :path-cascade-auto … :tool-home, :isolation-in-auto; go на мапу # Plan
```
