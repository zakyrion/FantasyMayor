---
category: A
read: archive
status: implemented
tags:
  - architecture
  - rules
  - specification
  - cascade
related:
  - "[FLOW](FLOW.md)"
  - "[CONTEXT](CONTEXT.md)"
---

Набір правил коду FantasyMayor і структура майбутньої RULES_SPECIFICATION.md — стадії s1 і s2 каскаду.

# Subject

```clojure
{:task :rules-specification
 :flow "Flows/RULES_SPECIFICATION/FLOW.md"
 :context "Flows/RULES_SPECIFICATION/CONTEXT.md"
 :subject "RULES_SPECIFICATION.md — повна специфікація правил коду FantasyMayor"
 :previous-cascade :none
 :stage :s2
 :written-at "2026-09-16"
 :mode :auto
 :gates {:after-s1 :waived-by-owner :after-s2 :waived-by-owner}
 :adapted-s2 "s2 адаптований так само: замість декомпозиції класу — розкладка документа (розділи, імена def-блоків, публічна форма ID, форма запису правила); ключі методу (:in :out :writes :scratch :calls) не застосовні, замість них :does на розділ"
 :adapted "предмет каскаду — ДОКУМЕНТ, тому s1 не має :flow алгоритму: замість кроків тут набір правил і конструкції коду, про які вони говорять. Розкладка документа (розділи, def-імена, публічна форма ID) народжується в s2"}
```

# s1

```clojure
(def rules-specification
  {:makes "повний набір правил, яких має дотримуватись код FantasyMayor, разом із конструкціями коду, про які ці правила говорять"

   :criterion "запис входить у набір тоді й лише тоді, коли його можна порушити ЗМІНОЮ КОДУ в Assets/: існує спостережувана ситуація в коді, на яку правило відповідає «так» або «ні». Усе, що порушується зміною документа, процесу, ассета, форми DI-реєстрації або поведінки інструмента, лишається поза набором"

   :criterion-corollaries ["правило описує ситуацію коду, а не намір автора — інакше його не відрізнити від мертвого"
                           "правило, яке носій показує скелетом C#, входить як правило, а не як скелет: у набір іде те, що скелет кодує"
                           "жива назва класу — доказ у факті, ніколи не правило"
                           "визначення конструкції (що таке система, тег, подія, view) — не правило поведінки, а адресат правил: живе в :data"
                           "правило вибору форми (який рецепт, коли ділити систему) — правило коду: воно вирішує, якої форми набуде код"]

   :rule-shape {:id "стабільна тотожність правила — namespaced keyword; s2 дає публічну форму ID для помилки аналізатора і попередження графа, але не має права злити чи розділити правила"
                :says "одне речення — саме правило, словами задачі"
                :governs "конструкція з :data, якої правило стосується"
                :from "якорі носіїв — ID записів інвентаря CONTEXT.md # Inventory, плюс ID суперечності, коли правило її розвʼязує"
                :enforced-by "хто може це перевірити СЬОГОДНІ"
                :note "факт, не видний із решти ключів"
                :needs-owner "правило стоїть у наборі, але спирається на відкрите питання власника"
                :new-rule "правило, якого жоден носій не формулює — існує лише як розвʼязок суперечності"}

   :enforced-by-legend {:analyzer "MarkerShapeAnalyzer у компіляції Unity — помилка збірки FM1001-FM1004"
                        :graph "fantasymayor-graph — порушення або попередження в build і check"
                        :arch-check "скіл arch-check — детектор трьох заборон і часу життя алокатора"
                        :agent "агент читає код проти правила — pattern-choice, placement, asmdef_reach, ревʼю"
                        :none "сьогодні не перевіряє ніщо"}

   :groups [:systems-and-roles :system-state :collections-and-memory
            :tables-tags-archetypes :keys-and-indexes :components-writes-links
            :events :reactive-perframe-orchestrator :one-shot-pipeline-turn
            :transaction :view :configs-and-addressables :threading-and-structural
            :placement :naming :methods-fail-loud-comments :form-choice-and-signatures]

   :tally {:rules 243
           :inventory-entries 457
           :code-entries-covered 366
           :definitions-in-data 43
           :excluded 48
           :contradictions-resolved 20
           :auto-decided 24
           :needs-owner 3
           :new-rules 5}})
```

## Дані — конструкції коду, про які говорять правила

Кожен запис — конструкція, яку правило називає адресатом. `:anchors` — ID визначень з інвентаря CONTEXT.md, які цю конструкцію задають.

```clojure
{:stack        {:type Friflo.Engine.ECS :holds "рушій Unity, ECS Friflo.Engine.ECS 3.6 у стилі DoD (не Unity DOTS), DI VContainer, async UniTask, Addressables, InputSystem, URP, UI Toolkit плюс Unity App UI" :from "ARCHITECTURE Stack" :anchors [:arch-00]}

 :entity-store {:type EntityStore :holds "сховище сутностей Friflo — власник архетипів, індексів і всіх структурних змін" :from "ARCHITECTURE, усі рецепти систем"}
 :storages     {:type EntityStorages :holds "реєстр сховищ: World (ігровий світ), Singletons (однопримірниковий стан), конфіги за типом" :from "ARCHITECTURE view-boundary, PATTERN_CONFIG, рецепти систем"}
 :game-state   {:type GameState :holds "стан гри, у якому працює система Update-контракту" :from "PATTERN_PERFRAME_SYSTEM, PATTERN_REACTIVE_SYSTEM"}
 :app-state    {:type AppState :holds "прапорець кроку застосунку, за яким запускаються разові системи: ConfigLoading, InstanceObjects, MainMenu" :from "pattern-choice, PATTERN_CONFIG_LOADER"}

 :node-kind    {:type IComponent :holds "як інструмент упізнає вид конструкції: component — struct з IComponent або з іменем що закінчується на Component і не є подією; tag — struct з ITag; event — struct з іменем на Event чи EventComponent або тип, піднятий CreateEvent; data — будь-яка інша struct; config — неабстрактний клас, чия спадковість доходить до ScriptableObject; view — клас у папці Views/, чия спадковість доходить до MonoBehaviour; system — неабстрактний клас із роллю; archetype — член тримача або маніфест singleton; installer — клас з іменем на Installer або на IInstaller чи LifetimeScope" :from "graph-facts Nodes" :anchors [:gfx-02]}
 :node-identity {:type Assembly :holds "один вузол на оголошення (partial ділять його); неоголошений тип — вузол з declared false і видом за суфіксом; однойменні типи в різних просторах імен беруть ідентифікатор з простором; неоднозначне імʼя — попередження, не ребро; вузли вкладеного типу належать вкладеному типу" :from "graph-facts, MarkedTypeCheck" :anchors [:gfx-03 :msa-10]}
 :scan-roots   {:type Assets :holds "корені сканування коду: Assets/Domains, Assets/Presentation, Assets/Modules, Assets/Scripts, Assets/Flows" :from "graph-facts Scan roots" :anchors [:gfx-01]}
 :graph-edge   {:type ComponentIndex :holds "ребра, які читач виводить із коду: writes (AddComponent, Singletons.Set), reads (GetComponent, HasComponent, Singletons.Get, привʼязка до архетипу таблиці), removes, emits (CreateEvent), reacts_to і polls за роллю, disposes, fk_of за законом суфікса і реальним пошуком в індексі, inherits, hosts, registers, exposes, injects, runs_in, subscribes" :from "graph-facts Edges" :anchors [:gfx-07]}

 :component    {:type IComponent :holds "проста struct runtime-значень — колонка рядка таблиці" :from "PATTERN_COMPONENT, ARCHITECTURE key-role-law"}
 :tag          {:type ITag :holds "порожня struct, чия присутність і є інформацією" :from "PATTERN_TAG, ARCHITECTURE tag-law"}
 :main-tag     {:type Tags.Get :holds "перший аргумент типу в Tags.Get оголошення архетипу — дискримінатор таблиці; головний і label теги розділені маркером TagLabel на кожній структурі" :from "ARCHITECTURE tag-law, graph-facts facets" :anchors [:gfx-04]}
 :tag-order    {:type Tags.Get :holds "порядок аргументів у Tags.Get: закон вимагає головний тег першим, інструмент порядку не перевіряє — головність вирішує лише маркер" :from "tag_law.py" :anchors [:tgl-05]}
 :label-marker {:type TagLabelAttribute :holds "атрибут лише на структурі, не множинний, не успадковується; без аргументу роль None" :from "TagLabelAttribute.cs, tag_law.py" :anchors [:tgl-01]}
 :label-role   {:type TagLabelRole :holds "перелічення ролей мітки: None (членство), Transaction (transaction-сутність)" :from "TagLabelRole.cs" :anchors [:atr-05]}

 :archetype    {:type Archetype :holds "оголошена форма рядка: набір колонок плюс набір тегів; у вузол графа головний тег записується лише коли він рівно один, label-теги — ті, що мають маркер" :from "ARCHITECTURE archetype-law, ecs_facts.py" :anchors [:ecs-01]}
 :holder       {:type Archetypes :holds "static-тримач <Assembly>Archetypes, чиї члени повертають живий Archetype" :from "ARCHITECTURE archetype-law :home"}
 :singleton-manifest {:type SingletonArchetypeDefinition :holds "оголошення архетипу Singleton — єдиний дім однопримірникових компонентів" :from "EcsExtensions, ecs_facts.py"}

 :event        {:type EventTag :holds "однокадрова сутність: EventTag як головний тег плюс EventFrameComponent плюс компонент події" :from "ARCHITECTURE event-lifecycle" :anchors [:arch-72]}
 :event-archetype {:type EventArchetypes :holds "архетип події, розвʼязаний EventArchetypes.Of<T>(store); кожен споживач бачить подію рівно раз у кадрі після підняття, незалежно від пріоритету" :from "ARCHITECTURE, PATTERN_EVENT" :anchors [:arch-75 :evt-02]}
 :cleanup      {:type EventCleanupSystem :holds "єдина глобальна система прибирання: Priority int.MaxValue, видаляє дозрілі події в кінці того кадру" :from "ARCHITECTURE event-lifecycle, PATTERN_CLEANUP_SYSTEM" :anchors [:arch-76]}

 :component-index {:type ComponentIndex :holds "індекс, ключований типом компонента через усі таблиці, що його несуть: роль несе тип ключа, таблицю несе тег; таблиця графа — кожне поле ComponentIndex з роллю за суфіксом ключа і єдиним архетипом, що ключ несе" :from "ARCHITECTURE component-index, graph-facts tables" :anchors [:arch-45 :gfx-08]}
 :indexed-key  {:type IIndexedComponent<TValue> :holds "контракт ключа індексу: GetIndexedValue() повертає значення ключа" :from "ARCHITECTURE component-index :key-equality"}

 :system       {:type IUpdatedSystem :holds "клас, який веде рушій: прямо чи транзитивно UpdatedSystem, LateUpdatedSystem, IUpdatedSystem, ILateUpdatedSystem, IUniTaskSystem, IPrioritizedUniTaskSystem, ConfigLoaderSystem<T> або EventCleanupSystem; роль дістають лише оголошені неабстрактні класи видів other, installer, config, view" :from "arch-check Rule 2, roles.py" :anchors [:ach-06 :rol-01]}
 :system-role  {:type SystemRoleKind :holds "роль системи: cleanup, reactive, per_frame, pipeline_stage, turn_phase, startup_step, sub_system; маркер знає лише PerFrame і Reactive; докази ролі — Update-цикл у спадковості, обхід EventTag із місцем DeleteEntity, аргумент стадії, предок TurnPhaseSubSystem, негенеричний IUniTaskSystem, абстрактний предок, якого хтось збирає; клас без ролі все одно polls архетипи подій, які тримає; форма per-frame = контракт циклу і не подієвий якір, форма reactive = контракт циклу, не табличний якір, і подієвий якір або утримуваний архетип події" :from "SKILL role-decision, roles.py, MarkedTypeCheck, SystemRoleKind.cs" :anchors [:rol-02 :rol-03 :rol-05 :msa-11 :atr-02]}
 :role-marker  {:type SystemRoleAttribute :holds "атрибут лише на класі, не множинний, не успадковується" :from "SystemRoleAttribute.cs"}
 :anchor       {:type UpdatedSystem :holds "якір міряється лише всередині base(...) класу, чия ПРЯМА база UpdatedSystem або LateUpdatedSystem; аргумент EventArchetypes.Of або AnyComponents над типами подій робить якір подієвим, інакше якір табличний; виклик EventArchetypes.Of поза base(...), включно з this(...), — утримання; подія, на яку клас якориться в base(...), дає ребро reacts_to, утримуваний архетип — reacts_to лише під reactive-маркером, інакше polls" :from "graph-facts facets, MarkedTypeCheck, ecs_facts.py, SKILL" :anchors [:gfx-05 :msa-07 :msa-08 :ecs-02 :gsk-07]}
 :cadence      {:type UpdatedSystem :holds "каденція виконання: repeated — UpdatedSystem, LateUpdatedSystem, IUpdatedSystem, ILateUpdatedSystem, EventCleanupSystem або база фази ходу; one-shot — IUniTaskSystem включно з ConfigLoaderSystem<T>, стадія конвеєра створення мапи, підсистема разового оркестратора; async — робота в async UniTask" :from "arch-check System cadence" :anchors [:ach-04]}
 :update-base  {:type UpdatedSystem :holds "база, що вже робить знімок якірного архетипу для своїх нащадків, тому структурні зміни в Update безпечні" :from "ARCHITECTURE structural-change :base" :anchors [:arch-87]}
 :one-shot-base {:type IUniTaskSystem :holds "контракт разового кроку: Execute(token) плюс AppState, на якому Boot його запускає" :from "pattern-choice system-bases"}
 :pipeline-base {:type IPrioritizedUniTaskSystem<MapGenerationStep> :holds "контракт упорядкованого конвеєра: Update(token) плюс Priority, менший раніше; аргумент типу називає стадію, за якою DI збирає сімейство" :from "pattern-choice, PATTERN_PIPELINE_STAGE, roles.py"}
 :turn-phase-base {:type TurnPhaseSubSystem :holds "public abstract class : IPrioritizedUniTaskSystem<TurnPhaseStep> — база фази ходу, що працює кожного ходу" :from "Assets/Modules/Turn/Systems/TurnPhaseSubSystem.cs"}
 :subsystem    {:type IDisposable :holds "член сімейства, зібраного DI: простий обʼєкт із IsEnabled, Priority і Run, яким володіє оркестратор — не система" :from "PATTERN_ORCHESTRATOR_SUBSYSTEM, PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM"}
 :priority     {:type SystemPriorities :holds "вираз Priority, розвʼязаний проти кожної const int за шляхом вкладеності; вкладені класи — WorldInit, RuntimeTick та інші — простори порядку; EventCleanup дорівнює int.MaxValue" :from "SystemPriorities.cs, graph-facts facets" :anchors [:gfx-06]}

 :state-marker {:type StateAllowedAttribute :holds "атрибут на полі, конструктор приймає необовʼязкову причину" :from "Assets/Scripts/Core/StateAllowedAttribute.cs"}
 :frame-box    {:type FrameBox<T> :holds "змінна struct значення на обмежену кількість кадрів: OneFrame, TwoFrames, ForFrames; Exist залежить від Time.frameCount, Value кидає на застарілому" :from "Assets/Scripts/Core/FrameBox.cs, pattern-choice"}
 :structural-change {:type Entity :holds "структурна зміна — AddComponent, RemoveComponent, AddTag, RemoveTag; народження архетипом нею не є; тег, доданий за життя, інструмент бачить як AddComponent типу з іменем на Tag, а AddComponent події одразу після CreateEvent записом не рахується" :from "ARCHITECTURE structural-change, ecs_facts.py" :anchors [:arch-82 :ecs-03]}

 :view         {:type MonoBehaviour :holds "шар view — усе, що оголошено в папці Views/; партнер view-системи — MonoBehaviour у тій папці; рецепт PATTERN_VIEW_SYSTEM маркера ViewSubscriber не згадує" :from "placement role-folders, graph-facts, рішення :view-definition" :anchors [:vws-11]}
 :view-marker  {:type ViewSubscriberAttribute :holds "атрибут на класі, множинний, не успадковується; підпискою рахується += , ліворуч якого символ події — += над числом чи рядком підпискою не є" :from "ViewSubscriberAttribute.cs, MarkedTypeCheck" :anchors [:msa-09]}

 :config       {:type ScriptableObject :holds "авторський ассет даних, що живе в EntityStorages за своїм типом" :from "PATTERN_CONFIG"}
 :validatable  {:type IValidatableConfig :holds "контракт валідації авторських даних: Validate() кидає на кожне порушення" :from "EcsExtensions/IValidatableConfig.cs"}
 :config-loader {:type ConfigLoaderSystem<T> :holds "єдиний loader конфігів: вантажить ассет, кидає при невдачі, валідує, кладе в сховище" :from "PATTERN_CONFIG_LOADER"}
 :config-address {:type ConfigAddresses :holds "тримач const-адрес addressable у SCREAMING_SNAKE_CASE" :from "EcsExtensions/ConfigAddresses.cs"}
 :addressable  {:type IAddressable :holds "контракт завантаження: LoadAndInstanceAsync для GameObject, LoadAndInstanceAsync<T> для компонента, LoadAsync<T> для класу, що не GameObject; скасований токен дає OperationCanceledException після звільнення; контракт у Core/, реалізація в Implementation/" :from "ADDRESSABLE_PATTERNS Files і API" :anchors [:adr-01 :adr-03]}
 :box          {:type Box<T> :holds "readonly struct над класом дескриптора: Value кидає без Exist, Dispose ідемпотентний і звільняє рівно раз, копії ділять дескриптор; Result<T> несе Box і Status, Status має Unknow, Failed, Success" :from "Assets/Scripts/Core/Box.cs, Result.cs, Status.cs"}

 :allocator    {:type Allocator :holds "вибір часу життя native-памʼяті: Temp, TempJob, Persistent" :from "ARCHITECTURE native-allocator"}
 :native-container {:type NativeList<int> :holds "контейнер Unity.Collections — дозволена памʼять повторного прогону" :from "ARCHITECTURE system-collections"}
 :managed-collection {:type System.Collections.Generic :holds "керовані колекції й масиви: List, Dictionary, HashSet, Queue, Stack, LinkedList, Sorted-сімейство та їхні інтерфейси" :from "arch-check Rule 1"}
 :status-monitor {:type StatusMonitor :holds "джерело токена скасування для довгої async-роботи" :from "pattern-choice kernel-tree"}
 :query-helpers {:type QueryResultExtensions :holds "TryGetFirst(out entity) — пошук одного рядка на запиті, архетипі чи множині Entities" :from "pattern-choice kernel-tree"}

 :assembly     {:type asmdef :holds "збірка Unity — межа видимості типів і вузол графа залежностей" :from "placement layers і depends"}
 :layer        {:type Assets :holds "шари: domain у Assets/Domains/, presentation у Assets/Presentation/, module у Assets/Modules/, shared kernel у Assets/Scripts/Core/ і Assets/Scripts/EcsExtensions/, app-root у Assets/Scripts/Installers/" :from "placement layers"}
 :role-folder  {:type Views :holds "рольові папки: Components/, Tags/, Events/, Configs/, Data/, Systems/, Helpers/, Views/, Archetypes/, Prefabs/, Textures/" :from "placement folder-layout і role-folders"}

 :diagnostic   {:type MarkerShapeAnalyzer :holds "діагностики FM1001-FM1004 категорії FantasyMayor.Markers, severity Error, увімкнені за замовчуванням; тип без маркера перевірок не реєструє, зайвий маркер аналізатор не перевіряє" :from "MarkerShapeAnalyzer.cs" :anchors [:msa-05 :msa-06]}
 :marker-vocabulary {:type MarkerVocabulary :holds "словник аналізатора за метаданими: SystemRoleAttribute, ViewSubscriberAttribute, TagLabelAttribute, IUpdatedSystem, ILateUpdatedSystem, UpdatedSystem, LateUpdatedSystem, EventArchetypes, Friflo ComponentTypes і ITag, UnityEngine MonoBehaviour" :from "MarkerVocabulary.cs" :anchors [:msa-12]}
 :recipe-signature {:type RECIPE_SIGNATURES :holds "ознака рецепта — джерело правди у recipes.py, читання у recipe-signatures.md; поліморфний каталог знаходиться трійкою: абстрактна SO-база з іменем на Config, неабстрактний SO-контейнер з масивом цієї бази, компонент виду, названий базою без суфікса Config, який несе архетип" :from "recipes.py, recipe-signatures.md" :anchors [:rcp-01 :rcp-03]}}
```

## Правила — система, роль, маркер, порядок

```clojure
{:group :systems-and-roles
 :subject "що таке система, як вирішується її роль, коли потрібен маркер, звідки береться порядок виконання і коли система ділиться"
 :rules
 [{:id :system/definition :says "система — неабстрактний клас, який веде рушій: прямо чи транзитивно UpdatedSystem, LateUpdatedSystem, IUpdatedSystem, ILateUpdatedSystem, IUniTaskSystem, IPrioritizedUniTaskSystem, ConfigLoaderSystem<T> або EventCleanupSystem" :governs :system :from [:ach-06 :rol-01 :x-system-definition] :enforced-by :arch-check}
  {:id :system/subsystem-is-not-a-system :says "член сімейства, зібраного DI, — простий обʼєкт із IDisposable, яким володіє система: заборона стану й заборона керованих колекцій його не вʼяжуть" :governs :subsystem :from [:orc-02 :orc-07 :ror-04 :ror-10 :x-system-definition] :enforced-by :arch-check :note "роль sub_system у графі — ярлик членства в сімействі, не системність"}
  {:id :system/cadence :says "каденція читається зі стадії, яку система обслуговує, а не з інтерфейсу: разова — крок старту, loader конфіга, стадія чи підсистема створення мапи; повторна — per-frame, reactive на кожну подію, фаза ходу на кожен хід" :governs :cadence :from [:ach-04 :arch-17 :arch-18 :x-cadence-turn-phase] :enforced-by :arch-check}
  {:id :system/role-order :says "роль неабстрактного класу бере першу істинну гілку: обхід подій з видаленням → cleanup; подієвий якір у base(...) → reactive; Update-цикл і утримуваний архетип події → маркер; Update-цикл → per_frame; аргумент стадії створення мапи → pipeline_stage; предок TurnPhaseSubSystem → turn_phase; негенеричний IUniTaskSystem → startup_step; абстрактний предок, якого хтось збирає → sub_system; інакше ролі нема" :governs :system-role :from [:gsk-01 :rol-02 :rol-03] :enforced-by :graph}
  {:id :system/marker-required :says "маркер ролі обовʼязковий рівно в одній формі, якої порядок не вирішує: Update-клас тримає архетип події поза base(...)" :governs :role-marker :from [:gsk-02 :rol-04 :atr-01 :x-role-marker] :enforced-by :graph :note "сьогодні граф лише попереджає; помилку компіляції ставить GRAPH_STANDARD після каскаду"}
  {:id :system/marker-forbidden :says "маркер ролі на класі, чию роль вирішує форма, заборонений" :governs :role-marker :from [:rol-04 :gsk-06 :msa-06 :x-role-marker] :enforced-by :graph :note "рішення власника: зайвий маркер — помилка, не попередження"}
  {:id :system/marker-value :says "значення маркера мусить збігатися з формою класу: PerFrame вимагає контракту циклу без подієвого якоря, Reactive вимагає контракту циклу без табличного якоря плюс подієвий якір або утримуваний архетип події" :governs :role-marker :from [:msa-01 :msa-02 :msa-11 :x-role-marker] :enforced-by :analyzer}
  {:id :system/marker-not-inherited :says "жоден маркер не успадковується — кожен конкретний клас чи структура несе свій" :governs :role-marker :from [:atr-01 :atr-03 :atr-04 :atr-06] :enforced-by :analyzer}
  {:id :system/marker-vocabulary-parity :says "копія перелічення ролей в аналізаторі мусить збігатися з перелічуванням у EcsExtensions — аргумент атрибута приходить як базовий int" :governs :marker-vocabulary :from [:msa-13] :enforced-by :none}
  {:id :system/base-choice :says "базу обирає спосіб запуску: разовий крок старту — IUniTaskSystem; упорядкований конвеєр — IPrioritizedUniTaskSystem<T> зі стадією в аргументі, а коли сімейство має абстрактну базу — її; робота після кожного Update — LateUpdatedSystem; інакше UpdatedSystem" :governs :system :from [:pch-08 :pch-10 :pch-11 :pch-12] :enforced-by :agent}
  {:id :system/priority-source :says "Priority повертає іменовану const int із тримача пріоритетів проєкту — ніколи локальну константу класу й ніколи літерал" :governs :priority :from [:pch-05 :ecs-07 :x-priority] :enforced-by :graph :note "інструмент сьогодні приймає будь-яку розвʼязну const int, отже перевіряє слабше за правило"}
  {:id :system/priority-space :says "вкладений клас тримача пріоритетів — окремий простір порядку: значення порівнюються лише всередині простору, кожен член простору має власне значення, а пріоритети підсистем порівнюються лише всередині свого оркестратора" :governs :priority :from [:pch-05 :orc-09 :gfx-06] :enforced-by :agent}
  {:id :system/priority-is-order-only :says "пріоритет задає лише порядок виконання — ніколи правило, що споживач стоїть вище чи нижче виробника" :governs :priority :from [:arch-77 :rea-06 :evt-08] :enforced-by :agent}
  {:id :system/query-caches-in-constructor :says "система тримає свій архетип, запит та індекс у readonly-полі, розвʼязаному раз у конструкторі" :governs :system :from [:arch-55 :arch-28 :rea-02] :enforced-by :arch-check}
  {:id :system/driven-by :says "per-frame систему веде або робоча таблиця — оголошений архетип, який вона обробляє, — або якір тіку, singleton-архетип, присутність якого тік вмикає; обʼєкт запиту — лише для справді міжархетипної множини" :governs :archetype :from [:pfr-03 :pch-12] :enforced-by :agent}
  {:id :system/split-when :says "система ділиться, коли вона створює і знищує той самий вид вмісту, щокадру порівнює стан світу, тримає більше двох непов'язаних сімейств запитів або працює в кількох станах гри з різних причин" :governs :system :from [:arch-01 :pfr-07 :rea-08] :enforced-by :agent}
  {:id :system/split-into :says "стартовий обсяг іде в разову стадію чи підсистему створення мапи, runtime-обовʼязок — в одну reactive-систему на відповідальність, спільне обчислення — у stateless helper" :governs :system :from [:arch-02] :enforced-by :agent}
  {:id :system/simplest-structure :says "код набуває найпростішої структури, що розвʼязує задачу: патерн зʼявляється лише коли його вимагають кардинальність або справжня складність, ніколи тому, що його згадує документ чи коментар" :governs :system :from [:arch-88] :enforced-by :agent}]}
```

## Правила — стан системи

```clojure
{:group :system-state
 :subject "заборона стану екземпляра, що ним є і що ним не є, і три виходи з заборони"
 :rules
 [{:id :state/no-instance-state :says "система не тримає змінного стану екземпляра" :governs :system :from [:arch-11 :ach-01 :pfr-02] :enforced-by :arch-check}
  {:id :state/is-state :says "стан — будь-яке переприсвоюване поле, readonly-поле змінного контейнера чи буфера, чий вміст змінюється між кадрами, і settable авто-властивість екземпляра" :governs :system :from [:arch-13 :ach-07 :ach-08] :enforced-by :arch-check}
  {:id :state/is-not-state :says "не стан — readonly незмінні залежності й дескриптори сховища, кеші запитів, розвʼязані раз у конструкторі, const і static readonly" :governs :system :from [:arch-12 :ach-08] :enforced-by :arch-check}
  {:id :state/mutable-static :says "змінне static-поле в системі — стан, бо перелік не-стану закритий і містить лише const і static readonly" :governs :system :from [:arch-12 :ach-08 :x-static-field] :enforced-by :none :note "детектор сьогодні не позначає жодного static-поля — прогалина інструмента, не дозвіл"}
  {:id :state/default-home :says "прапорець статусу, маркер роботи в процесі, лічильник прогресу, біт запущено чи завершено і дескриптор оброблюваного живуть на сутності або в однопримірниковому стані поза системою" :governs :system :from [:arch-14 :pfr-05] :enforced-by :agent}
  {:id :state/escape-order :says "вихід із заборони береться по порядку: спершу перенести стан у компонент чи однопримірниковий стан, потім коробка кадрів, і лише потім позначене поле" :governs :state-marker :from [:arch-15 :pfr-05] :enforced-by :agent}
  {:id :state/frame-box-use :says "значення на обмежену кількість кадрів живе в коробці кадрів: Value читається лише після Exist, коробка звільняється при знятті, а в async-системі коробка заборонена — await перетинає кадри й вона застаріває" :governs :frame-box :from [:pch-19 :pfr-06] :enforced-by :agent}
  {:id :state/frame-box-field :says "поле коробки кадрів у системі — стан і несе позначення з причиною: сама коробка — змінна struct у переприсвоюваному полі" :governs :frame-box :from [:pfr-06 :x-framebox-field] :enforced-by :arch-check}
  {:id :state/allowed-reason :says "позначення дозволеного стану завжди несе причину, хоча тип і дозволяє викликати його без аргументу" :governs :state-marker :from [:arch-15 :pch-18 :x-stateallowed-reason] :enforced-by :none}
  {:id :state/lifetime-flags :says "сторож підписки, сторож повторного входу, сторож звільнення і поле дескриптора ассета в системі — стан: кожне таке поле несе позначення з причиною" :governs :system :from [:vws-08 :pst-04 :adr-15 :x-stateless-vs-lifetime-flags] :enforced-by :arch-check :needs-owner :oq-stateless-vs-lifetime-flags}
  {:id :state/preupdate-cache :says "кеш кадру, розвʼязаний у PreUpdate, — стан: або позначене поле з причиною, або рефакторинг, і вибір робить власник" :governs :system :from [:ach-09 :pfr-06] :enforced-by :arch-check}
  {:id :state/injected-collection :says "поле списку, зібраного DI, у системі несе позначення дозволеного стану" :governs :subsystem :from [:orc-04 :ror-03 :cat-14 :orc-07] :enforced-by :arch-check}]}
```

## Правила — колекції й памʼять

```clojure
{:group :collections-and-memory
 :subject "де керовані колекції дозволені, що таке нульове виділення на повторному шляху і як обирається native-алокатор"
 :rules
 [{:id :alloc/view-exempt :says "код шару view може використовувати керовані колекції — view не система" :governs :view :from [:arch-16] :enforced-by :arch-check}
  {:id :alloc/one-shot-exempt :says "система разової каденції може використовувати керовані колекції" :governs :cadence :from [:arch-17 :ach-02] :enforced-by :arch-check}
  {:id :alloc/repeated-zero :says "система повторної каденції працює з нульовим виділенням: памʼять, узята на початку прогону, звільняється в його кінці, а прогін тримається на native-контейнерах, структурах і спанах" :governs :cadence :from [:arch-18 :ach-03] :enforced-by :arch-check}
  {:id :alloc/banned-generic-types :says "у повторній системі заборонені і директива імпорту керованих колекцій, і повні імена їхніх типів — списки, словники, множини, черги, стеки, впорядковані сімейства та їхні інтерфейси; масив платформи, спани й негенеричні колекції ця заборона не стосується" :governs :managed-collection :from [:ach-05] :enforced-by :arch-check}
  {:id :alloc/managed-element-exception :says "елементи керованого типу — обʼєкти сцени чи посилання на view — живуть у керованій колекції, виділеній раз при побудові власника й утримуваній весь його час життя" :governs :managed-collection :from [:arch-19 :x-generic-in-repeated-orchestrator] :enforced-by :arch-check :note "виняток знімає лише каденцію виділення; поле лишається станом і несе позначення"}
  {:id :alloc/enum-not-a-native-key :says "перелічення не може бути ключем native-множини чи native-словника — ключем іде базовий цілий тип, а значенням перелічення придатне" :governs :native-container :from [:arch-20] :enforced-by :agent}
  {:id :alloc/by-lifetime :says "алокатор обирає час життя: у межах одного кадру — тимчасовий кадровий, у межах чотирьох кадрів — тимчасовий задачний, довше — постійний або керована колекція, яку звільняє один названий власник" :governs :allocator :from [:arch-21 :ach-03 :pst-10] :enforced-by :arch-check}
  {:id :alloc/no-temp-in-async :says "async-система ніколи не бере кадровий тимчасовий алокатор, і така памʼять ніколи не лежить у полі й не живе через await" :governs :allocator :from [:arch-22 :ach-10 :pst-10 :pst-11] :enforced-by :arch-check}
  {:id :alloc/temp-scope :says "кадровий тимчасовий алокатор живе або в блоці кадру на головному потоці, або в блоці задачі всередині job — більше ніде, зокрема не в коді, відданому пулу потоків" :governs :allocator :from [:arch-23 :pst-11] :enforced-by :arch-check}
  {:id :alloc/breach-is-an-error :says "порушення часу життя алокатора — помилка, не рекомендація" :governs :allocator :from [:arch-24 :ach-03] :enforced-by :arch-check}
  {:id :alloc/no-per-run-garbage :says "на повторному шляху не створюється новий масив і не викликаються матеріалізатори послідовностей — жодної щойно виділеної керованої колекції на прогін" :governs :cadence :from [:ach-11] :enforced-by :arch-check}
  {:id :alloc/no-handed-out-snapshot :says "система не будує й не віддає керований знімок: дані йдуть у споживача по одному значенню або посиланням, бо інакше час життя не названо" :governs :system :from [:ach-12 :ach-16 :pfr-08 :vws-04 :vws-10] :enforced-by :arch-check}
  {:id :alloc/named-owner :says "кожне виділення має названого власника, що його звільняє, і інваріант — детерміноване звільнення, а не тип контейнера" :governs :allocator :from [:ach-13 :ach-17] :enforced-by :arch-check}
  {:id :alloc/not-by-type :says "не порушення: спани, перелічення сутностей архетипу, запиту чи індексу, читання колекції, виставленої конфігом або компонентом, native-контейнери й позначені поля — керований буфер, виділений раз і детерміновано звільнений, проходить" :governs :managed-collection :from [:ach-14 :ach-15] :enforced-by :arch-check}
  {:id :alloc/binds-the-path :says "заборона виділення на прогін вʼяже повторний ШЛЯХ виконання — цикл, тіло Update, будь-який helper чи підсистема цього шляху, — а не лише клас системи" :governs :cadence :from [:ach-11 :x-system-definition] :enforced-by :arch-check}]}
```

## Правила — таблиці, теги, архетипи, народження

```clojure
{:group :tables-tags-archetypes
 :subject "як оголошується таблиця сутностей, закон головного й label тегів, закон архетипу і повнота народження"
 :rules
 [{:id :table/is :says "таблиця — ключовий компонент плюс дискримінатор, разом в одному оголошеному архетипі" :governs :archetype :from [:arch-25] :enforced-by :graph}
  {:id :table/filter :says "фільтр запиту — архетип таблиці, ніколи голий ключовий компонент: голий ключ дає обʼєднання всіх таблиць цього простору ключів, тому фільтр не-події називає рівно один тег" :governs :archetype :from [:arch-26 :tag-07 :cat-07 :tgl-03 :gfx-09] :enforced-by :graph}
  {:id :table/sweep :says "обхід таблиці — ітерація її архетипу, без обʼєкта запиту" :governs :archetype :from [:arch-27] :enforced-by :agent}
  {:id :table/keyed-join :says "зʼєднання за ключем — індекс над ключовою колонкою, оголошений раз у конструкторі" :governs :component-index :from [:arch-28] :enforced-by :agent}
  {:id :table/cross-archetype :says "запит через кілька архетипів дозволений, лише коли фільтр справді охоплює кілька архетипів і його погоджено з власником; обхід усіх подій за спільним тегом події — єдиний свідомий виняток" :governs :archetype :from [:arch-29 :cln-07] :enforced-by :agent}
  {:id :table/join-at-use :says "зʼєднання — пошук за значенням ключа в точці використання, ніколи збережене посилання на сутність з рядка однієї таблиці в рядок іншої" :governs :component-index :from [:arch-30 :cmp-07] :enforced-by :graph}
  {:id :table/index-only-hot :says "індекс заводиться лише для гарячого зʼєднання — щокадру або багато разів за хід; для пошуку з частотою кліку архетип сканується" :governs :component-index :from [:arch-31] :enforced-by :agent}
  {:id :tag/is :says "тег — порожня struct з контрактом тегу: даних не несе, його присутність і є інформацією, а щойно потрібне значення — це компонент" :governs :tag :from [:tag-01 :tag-10 :sig-02] :enforced-by :graph}
  {:id :tag/one-main-tag :says "кожне оголошення архетипу має рівно один головний тег, записаний першим" :governs :main-tag :from [:arch-32 :arch-33 :tag-02 :tgl-02 :gfx-04 :gfx-09] :enforced-by :graph}
  {:id :tag/main-tag-unique :says "один головний тег називає один архетип — спільний головний тег двох архетипів заборонений, і єдиний виняток — тег події" :governs :main-tag :from [:arch-33 :tgl-02 :gfx-09] :enforced-by :graph}
  {:id :tag/label-marker :says "label-тег — структура тегу з маркером мітки, з роллю або без; міткою може бути лише структура з контрактом тегу" :governs :label-marker :from [:arch-34 :tag-06 :gsk-04 :atr-04 :msa-04] :enforced-by :analyzer}
  {:id :tag/label-count :says "поруч із головним тегом стоїть від нуля до чотирьох міток — оголошення архетипу бере не більше пʼяти аргументів типу" :governs :label-marker :from [:arch-35 :tag-05] :enforced-by :graph}
  {:id :tag/label-not-a-filter :says "label-тег ніколи не фільтр запиту" :governs :label-marker :from [:arch-35 :tag-06 :tgl-03 :gfx-09] :enforced-by :graph}
  {:id :tag/event-tag :says "кожна подія несе тег події як головний тег, і тег події не стоїть ніде, крім архетипу події" :governs :event :from [:arch-36 :tag-05 :tgl-02 :cln-06 :gfx-09] :enforced-by :graph}
  {:id :tag/added-by-archetype-only :says "тег додається лише оголошенням архетипу таблиці — ніколи живій сутності, бо це виносить рядок з його архетипу" :governs :structural-change :from [:tag-04 :tgl-04 :arch-63 :gfx-09 :x-live-tag-write] :enforced-by :graph}
  {:id :tag/composed-by-declaration :says "набір тегів складається лише в оголошенні архетипу — ніколи додаванням до вже складеного набору" :governs :archetype :from [:tgl-04 :gfx-09] :enforced-by :graph}
  {:id :tag/state-column :says "стан — колонка над перелічуванням із суфіксом стану, ніколи перемикаваний тег, і пишеться вона лише при зміні" :governs :component :from [:arch-37 :tag-08] :enforced-by :graph}
  {:id :tag/kind-column :says "вид — колонка над перелічуванням із суфіксом виду, ніколи другий головний тег, і пишеться вона раз при народженні" :governs :component :from [:arch-38 :tag-09 :cat-11] :enforced-by :graph}
  {:id :tag/transaction-role :says "роль мітки transaction позначає transaction-сутність; мітка без ролі означає членство" :governs :label-role :from [:arch-34 :trx-13 :sig-04] :enforced-by :graph}
  {:id :archetype/holder :says "архетипи оголошує один static-тримач на збірку, названий іменем збірки без крапок і без доменного префікса; shared kernel і app-root — виняток" :governs :holder :from [:arch-51 :plc-26] :enforced-by :agent}
  {:id :archetype/reach :says "архетип називає лише компоненти, які його збірка може бачити" :governs :assembly :from [:arch-52] :enforced-by :agent}
  {:id :archetype/shape :says "кожен член тримача повертає живий архетип у тілі методу, а сховище приходить параметром" :governs :holder :from [:arch-53 :gfx-02] :enforced-by :graph}
  {:id :archetype/holder-stateless :says "тримач архетипів нічого не зберігає" :governs :holder :from [:arch-54] :enforced-by :agent}
  {:id :archetype/birth :says "сутність народжується викликом створення на архетипі — ніколи голим створенням на сховищі в місці виклику" :governs :archetype :from [:arch-56] :enforced-by :graph}
  {:id :archetype/bulk :says "масове народження — виклик створення багатьох на архетипі, а коли кількість відома, перед ним резервується місткість" :governs :archetype :from [:arch-57] :enforced-by :agent}
  {:id :archetype/assign-then-write :says "новий рядок присвоюється локальній змінній, і лише потім у нього пишуть — ніколи ланцюжок записів просто з виклику створення" :governs :archetype :from [:arch-58] :enforced-by :agent}
  {:id :archetype/arity-cap :says "набір колонок і набір тегів в оголошенні архетипу беруть не більше пʼяти аргументів типу кожен" :governs :archetype :from [:arch-59] :enforced-by :graph}
  {:id :archetype/no-orphan-column :says "запис компонента, якого не називає жоден оголошений архетип, — порушення" :governs :archetype :from [:ecs-09] :enforced-by :graph}
  {:id :birth/completeness :says "сутність народжується з кожною колонкою, яку коли-небудь матиме" :governs :archetype :from [:arch-60 :cmp-14] :enforced-by :graph}
  {:id :birth/sentinel :says "присутність не предикат, а значення-сторож — невідомо, спокій, порожня коробка" :governs :component :from [:arch-61] :enforced-by :agent}
  {:id :birth/fixed-composition :says "склад рядка фіксований при народженні: необовʼязкових колонок нема, а зміна складу — видалити рядок і створити новий у його архетипі, перенісши значення ключів" :governs :archetype :from [:arch-62 :tag-08 :trx-13] :enforced-by :agent}
  {:id :birth/no-late-structural :says "після народження не буває ні видалення компонента, ні видалення тегу, ні пізнього додавання колонки, якої архетип не називає, ні додавання тегу" :governs :structural-change :from [:arch-63 :x-live-tag-write] :enforced-by :graph}
  {:id :singleton/manifest :says "коли однопримірникові компоненти взагалі вживаються, їх оголошує рівно один архетип-маніфест: використаний, але не оголошений компонент — порушення, оголошений, але не використаний — теж" :governs :singleton-manifest :from [:ecs-08] :enforced-by :graph :needs-owner :oq-singleton-manifest}
  {:id :singleton/read :says "однопримірниковий компонент має оголошений архетип, але споживачі читають і пишуть його лише через реєстр однопримірникового стану, ніколи запитом" :governs :storages :from [:cmp-13 :ldr-03] :enforced-by :agent}]}
```

## Правила — ключі, індекси, ролі колонок

```clojure
{:group :keys-and-indexes
 :subject "закон ролей ключа, індекс за компонентом і межі self-index"
 :rules
 [{:id :key/pk :says "первинний ключ — колонка з суфіксом ідентичності, власна тотожність рядка; її власник — рівно одна таблиця в парі зі своїм тегом, а унікальність тримає контракт, не рушій" :governs :indexed-key :from [:arch-40 :ecs-05] :enforced-by :graph}
  {:id :key/fk :says "зовнішній ключ — колонка з суфіксом посилання: посилання з рядка іншої таблиці в простір ключів власника, ніколи тип первинного ключа власника, звʼязок один до багатьох" :governs :indexed-key :from [:arch-41 :cmp-06 :ecs-05] :enforced-by :graph}
  {:id :key/data :says "колонка даних — значення атрибута, і вона ніколи не ключує індекси двох різних таблиць" :governs :component :from [:arch-42 :ecs-05] :enforced-by :graph}
  {:id :key/enum-space :says "простір-перелічення — простір ключів без таблиці первинного ключа: сторона власника несе колонку даних, сторона посилання — колонку з суфіксом посилання" :governs :indexed-key :from [:arch-43 :x-catalogue-key-naming] :enforced-by :graph}
  {:id :key/self-index :says "self-index дозволений, коли кожен рядок із цією колонкою даних належить одній таблиці, а значення пошуку може прийти ззовні; колонка стану чи виду — законний його ключ; той самий тип даних, індексований у двох таблицях, розділяється на пару первинного і зовнішнього ключа" :governs :component-index :from [:arch-44 :arch-39 :cat-07] :enforced-by :agent}
  {:id :key/pk-single-carrier :says "первинний ключ, який несуть кілька архетипів, — порушення: чужі носії мусять перейти на колонку посилання" :governs :indexed-key :from [:ecs-05] :enforced-by :graph}
  {:id :key/fk-value-parity :says "зовнішній ключ, який шукають в індексі, мусить нести те саме значення, що й ключ власника — розбіжність типу значення є порушенням" :governs :component-index :from [:ecs-06 :cmp-06] :enforced-by :graph}
  {:id :key/fk-home :says "колонка посилання лежить у папці фічі власника поруч із первинним ключем — один простір, одна пара типів, визначена раз" :governs :role-folder :from [:cmp-08] :enforced-by :agent}
  {:id :key/pk-compare-only :says "первинний ключ, який лише порівнюють і ніколи не індексують, лишається простим компонентом із рівністю" :governs :component :from [:cmp-05] :enforced-by :agent}
  {:id :index/maintained-by-write :says "індекс підтримує виклик запису: запис перекладає рядок, видалення сутності прибирає його" :governs :component-index :from [:arch-46] :enforced-by :agent}
  {:id :index/pk-uniqueness :says "дубль значення первинного ключа повертає обидва рядки без помилки, тому унікальність перевіряється в місці виділення ключа і там же кидає" :governs :component-index :from [:arch-47 :ecs-05] :enforced-by :graph}
  {:id :index/bucket-cap :says "на однакове значення ключа припадає не більше ста сутностей — вставка й видалення лінійні по дублях" :governs :component-index :from [:arch-48 :cmp-14] :enforced-by :agent}
  {:id :index/one-fk-per-space :says "сутність тримає один компонент на тип, тому має не більше одного зовнішнього ключа в простір; два посилання вимагають власної пари типів і є рішенням дизайну" :governs :component-index :from [:arch-49 :cmp-14] :enforced-by :graph}
  {:id :index/key-equality :says "компонент-ключ оголошує контракт індексованого компонента і повертає значення ключа своїм методом: перелічення працює прямо, struct-ключ сам реалізує рівність і хеш" :governs :indexed-key :from [:arch-50 :cmp-03 :cmp-04 :cmp-14] :enforced-by :graph}]}
```

## Правила — компоненти, записи, посилання

```clojure
{:group :components-writes-links
 :subject "форма компонента, єдиний шлях запису і форма стійкого посилання"
 :rules
 [{:id :component/is :says "компонент — проста struct runtime-значень без поведінки й методів, крім рівності, коли він ключ таблиці; у папці компонентів немає ні логіки, ні побічних ефектів" :governs :component :from [:cmp-01 :plc-31] :enforced-by :graph}
  {:id :component/declare :says "компонент оголошується структурою з контрактом компонента — невидимий рушію компонент мовчки нічого не робить при народженні" :governs :component :from [:cmp-02 :cmp-10 :sig-01] :enforced-by :graph}
  {:id :write/upsert :says "запис колонки — виклик додавання компонента зі значенням, тобто upsert; за повнотою народження це завжди простий запис значення" :governs :component :from [:arch-66 :cmp-09] :enforced-by :graph}
  {:id :write/no-ref-mutation :says "ніколи мутація через посилання у сховище компонентів і ніколи читання компонента як дескриптора для мутації — індекс перекладає рядок лише на виклик запису" :governs :component-index :from [:arch-67 :cmp-09] :enforced-by :agent}
  {:id :write/change-only :says "спершу порівняти, писати при відмінності — перезапис того самого значення даремно перекладає індексовані рядки" :governs :component :from [:arch-68 :arch-37 :cat-06] :enforced-by :agent}
  {:id :link/persistent-key :says "стійке посилання — доменний ключ: колонка ідентичності власника, на яку посилається колонка посилання цього простору, ніколи збережений дескриптор сутності" :governs :indexed-key :from [:arch-69] :enforced-by :graph}
  {:id :link/runtime-only :says "лише runtime-посилання — не серіалізоване, звʼязане часом життя, зазвичай шар view — може тримати пряме посилання або дескриптор сутності" :governs :view :from [:arch-70] :enforced-by :agent}
  {:id :link/miss-throws :says "пошук за ключем, що промахнувся, кидає виняток" :governs :component-index :from [:arch-71] :enforced-by :agent}]}
```

## Правила — події

```clojure
{:group :events
 :subject "життєвий цикл однокадрової події: підняття, дозрівання, споживання, прибирання"
 :rules
 [{:id :event/is :says "подія — однокадрова struct, піднята на власній сутності; її поля — звичайні дані, значення, потрібні споживачу" :governs :event :from [:arch-04 :evt-01 :evt-03] :enforced-by :graph}
  {:id :event/declare :says "подія оголошується структурою з контрактом компонента, з суфіксом події, у папці подій своєї фічі" :governs :event :from [:evt-03 :evt-07 :plc-33 :sig-03] :enforced-by :graph}
  {:id :event/raise :says "подія піднімається одним викликом створення події на сховищі, який штампує кадр і складає архетип події; імпульс, зібраний руками, губить тег або штамп, ніколи не дозріває і тече" :governs :event :from [:arch-73 :evt-04 :pch-16 :cln-02 :cln-05] :enforced-by :graph}
  {:id :event/ripe :says "споживач діє лише поки подія дозріла — у кадрі після народження; перевірка дозрілості стоїть першим рядком обробки, інакше система відпрацює двічі" :governs :event :from [:arch-74 :rea-05 :pch-17] :enforced-by :agent}
  {:id :event/anchor :says "споживач якориться на архетипі самої події і тримає його — доки події нема, він коштує нуль; значення береться з компонента події на сутності імпульсу" :governs :event-archetype :from [:arch-05 :rea-04 :pch-17 :ror-07] :enforced-by :graph}
  {:id :event/reaction :says "реакція — або діяти прямо на значеннях події, або reconcile: зібрати поточну множину зі стану світу, порівняти й діяти на різниці, ідемпотентно" :governs :event :from [:arch-06 :rea-03 :ror-08] :enforced-by :agent}
  {:id :event/no-same-frame :says "ніколи не розраховувати на реакцію в тому ж кадрі — ланцюг подій коштує кадр на ланку, і видимість не залежить від пріоритету" :governs :event :from [:arch-07 :evt-08] :enforced-by :agent}
  {:id :event/dormant-consumer :says "емітер може зʼявитися пізніше: споживача можна зібрати першим як сплячий каркас" :governs :event :from [:arch-08] :enforced-by :agent}
  {:id :event/no-change-observers :says "ніколи спостерігач додавання компонента, видалення компонента, зміни тегів чи будь-який спостерігач зміни значення — натомість подія поруч із записом; прийняти спостерігача можна лише рішенням для всього проєкту" :governs :entity-store :from [:arch-10] :enforced-by :agent}
  {:id :event/cleanup :says "прибирає події одна глобальна система: працює останньою в тіку, видаляє кожну дозрілу сутність із тегом події й не має нащадків — власного прибирання для окремої події не буває" :governs :cleanup :from [:cln-01 :cln-03 :cln-04] :enforced-by :graph}
  {:id :event/no-tag-on-persistent-row :says "стійка сутність даних ніколи не несе тег події" :governs :event :from [:cln-06] :enforced-by :graph}
  {:id :event/one-way :says "ніколи петля заповнення - команда - заповнення на однокадрових подіях: дані течуть в один бік" :governs :event :from [:evt-09] :enforced-by :agent}
  {:id :event/lossy-producer :says "виробник, що не контролює вікно кадру — фаза ходу чи async-робота, — дзвонить за рівнем: перепіднімає подію кожен хід або тік, поки умова тримається, а споживач звіряється зі станом і не довіряє одній доставці" :governs :event :from [:evt-11] :enforced-by :agent}
  {:id :event/startup-bulk :says "стартова масова робота — стадія конвеєра, ніколи подія: однокадрові події не переживають async-конвеєр створення мапи" :governs :pipeline-base :from [:evt-06] :enforced-by :agent}
  {:id :event/suffix :says "тип події має суфікс події; довший легасі-суфікс із словом компонент закритий — його несе рівно один живий тип і жоден новий" :governs :event :from [:arch-91 :x-event-suffix] :enforced-by :graph}]}
```

## Правила — reactive, per-frame, оркестратор і підсистеми

```clojure
{:group :reactive-perframe-orchestrator
 :subject "три форми runtime-логіки і сімейство підсистем, зібране DI"
 :rules
 [{:id :reactive/default :says "reactive — типовий вибір для runtime-логіки і єдиний реактивний механізм проєкту" :governs :system :from [:arch-03 :rea-01] :enforced-by :agent}
  {:id :reactive/shape :says "reactive-система — запечатаний нащадок Update-бази, чиї залежності, індекси й архетипи розвʼязані в конструкторі, чий якір у базовому виклику — архетип події, а тіло Update починається з перевірки дозрілості, далі йдуть сторожі передумов, що кидають, і дія лише на різниці" :governs :system :from [:rea-02 :sig-05] :enforced-by :agent}
  {:id :perframe/only-when :says "per-frame береться лише коли логіка справді неперервна і не може бути реактивною — рух камери, проєкція щокадру, опитування вводу, стеження за виділенням; треба вміти сказати, чому імпульс не замінить тік" :governs :system :from [:arch-09 :pfr-01] :enforced-by :agent}
  {:id :perframe/shape :says "per-frame система — запечатаний нащадок Update-бази, чий базовий виклик бере світ і робочу таблицю з тримача, а тіло Update не лишає полів між кадрами" :governs :system :from [:pfr-02 :sig-06] :enforced-by :agent}
  {:id :perframe/late-update :says "пізня Update-база береться, коли треба бачити остаточний стан кадру — після камери й ігрових записів" :governs :system :from [:pfr-04 :pch-11] :enforced-by :agent}
  {:id :orchestrator/when :says "сімейство оркестратор плюс підсистеми береться, коли є одна база з реалізацією на фічу, коли стадія має незалежно впорядковані частини або коли обробка однієї події завелика для одного тіла Update; вміщується в одне тіло — це звичайна система" :governs :subsystem :from [:orc-01 :ror-01 :ror-02 :pch-04 :pst-12] :enforced-by :agent}
  {:id :orchestrator/base-shape :says "база підсистеми — абстрактний клас із контрактом звільнення: захищене readonly-сховище з конструктора, вимикач, абстрактний пріоритет, абстрактна операція сімейства і віртуальне звільнення" :governs :subsystem :from [:orc-02] :enforced-by :agent}
  {:id :orchestrator/concrete-shape :says "конкретна підсистема запечатана, бере реєстр сховищ, а не голе сховище світу, і розвʼязує власні архетипи та індекси у конструкторі" :governs :subsystem :from [:orc-03] :enforced-by :agent}
  {:id :orchestrator/dispatch :says "оркестратор упорядковує сімейство раз у конструкторі за пріоритетом і на прогоні викликає лише ввімкнені, по порядку" :governs :subsystem :from [:orc-04 :ror-03] :enforced-by :agent}
  {:id :orchestrator/no-domain-logic :says "оркестратор не містить доменної логіки — сортує, пропускає вимкнені, запускає; уся робота живе в підсистемах" :governs :subsystem :from [:orc-05 :ror-10] :enforced-by :agent}
  {:id :orchestrator/query-caches :says "спільні запити й Try-helper живуть у базі сімейства, власні — у кожній підсистемі; кеші запитів належать сховищу, тому звільняти в підсистемі нема чого, а її звільнення прибирає лише те, що вона сама виділила" :governs :subsystem :from [:orc-08 :ror-04 :x-subsystem-query-caches] :enforced-by :agent}
  {:id :orchestrator/routing :says "маршрутизація сімейства — спроба обробки на кожній підсистемі, перший збіг виграє, жодного збігу — кинути" :governs :subsystem :from [:orc-10 :cat-09] :enforced-by :agent}
  {:id :orchestrator/empty-family :says "поки підсистем нема, оркестратор лишається порожньою оболонкою з якорем і без інжекції списку: контейнер кидає на порожню колекцію, тому список і цикл зʼявляються з першою підсистемою" :governs :subsystem :from [:ror-05] :enforced-by :agent}
  {:id :orchestrator/ripe-once :says "дозрілість перевіряє оркестратор один раз перед розсиланням — підсистема не перевіряє її повторно" :governs :subsystem :from [:ror-09] :enforced-by :agent}
  {:id :structural/update-is-safe :says "структурні зміни в тілі Update безпечні, бо база вже зняла знімок якірного архетипу; у переліченні, яке система чи підсистема відкриває сама, вони заборонені" :governs :update-base :from [:rea-07 :ror-12 :arch-87] :enforced-by :agent}]}
```

## Правила — разові системи, конвеєр створення мапи, фаза ходу

```clojure
{:group :one-shot-pipeline-turn
 :subject "форми, що працюють раз або по стадіях, і їхні передумови"
 :rules
 [{:id :oneshot/startup-step :says "разовий крок старту реалізує негенеричний async-контракт: виконання з токеном плюс прапорець кроку застосунку, на якому його запускають" :governs :one-shot-base :from [:pch-08] :enforced-by :agent}
  {:id :pipeline/stage :says "стадія конвеєра створення мапи — разове async-будування світу: реалізує впорядкований async-контракт зі стадією створення мапи в аргументі типу і працює раз" :governs :pipeline-base :from [:pst-01 :sig-08 :pch-10] :enforced-by :graph}
  {:id :pipeline/shape :says "стадія запечатана й внутрішня, тримає readonly реєстр сховищ, сховище і архетипи таблиць, на вході перевіряє свою передумову і кидає на її відсутність, кидає на скасування, народжує рядки через архетип і звільняє власні дескриптори у своєму звільненні" :governs :pipeline-base :from [:pst-02] :enforced-by :agent}
  {:id :pipeline/order :says "стадії йдуть послідовно за зростанням пріоритету з очікуванням: стадія може покладатися на все, що зробили менші пріоритети, а відсутню передумову кидає" :governs :pipeline-base :from [:pst-03] :enforced-by :agent}
  {:id :pipeline/re-entry :says "сторож повторного входу зʼявляється лише там, де регенерація може повторно увійти в стадію — інакше стадія знищує й створює заново" :governs :pipeline-base :from [:pst-04] :enforced-by :agent}
  {:id :pipeline/addressable-handle :says "дескриптор завантаженого ассета стадія утримує і звільняє у своєму звільненні" :governs :box :from [:pst-05] :enforced-by :agent}
  {:id :pipeline/singleton-view :says "одиничний view публікується для споживачів як компонент із суфіксом view" :governs :singleton-manifest :from [:pst-07] :enforced-by :agent}
  {:id :pipeline/singleton-non-queried :says "одиничні дані, які ніхто не запитує, живуть однопримірниковим компонентом" :governs :singleton-manifest :from [:pst-08] :enforced-by :agent}
  {:id :turn-phase/base :says "фаза ходу наслідує базу фаз ходу, аргумент типу якої називає стадію ходу, і має повторну каденцію — кожен хід" :governs :turn-phase-base :from [:sig-12 :x-cadence-turn-phase] :enforced-by :graph}]}
```

## Правила — transaction-сутність

```clojure
{:group :transaction
 :subject "багатокрокова поведінка, що охоплює кілька піддоменів, і її єдиний дім"
 :rules
 [{:id :transaction/when :says "transaction-сутність береться, коли піддоменів два або більше І поведінка багатокрокова — між відкривальним і фіксувальним імпульсом результат ще може змінитися, отже є сесійний стан, яким хтось має володіти; два піддомени й один імпульс — це reactive-система" :governs :archetype :from [:trx-01] :enforced-by :agent}
  {:id :transaction/degenerate :says "коли між відкриттям і фіксацією нема чим володіти — ресурс не витрачено, прев'ю на мапі нема, вибір є чистим станом інтерфейсу, — фіксація робиться прямо на підтверджувальному імпульсі, без стадій чернетки" :governs :archetype :from [:trx-02] :enforced-by :agent}
  {:id :transaction/one-home :says "багатокрокова поведінка між піддоменами має рівно один дім — transaction-сутність у домені-дієслові, який володіє і сутністю, і кожним записом у неї; ніколи підкладковий домен і ніколи presentation" :governs :archetype :from [:trx-03 :trx-08] :enforced-by :graph}
  {:id :transaction/one-archetype :says "одна архетипна форма на всю сесію з усіма колонками, включно з колонкою стадії: головний тег не змінюється, поруч стоїть мітка ролі transaction, а стадія з іншим складом означає видалити рядок і створити новий у її архетипі з перенесенням ключів" :governs :archetype :from [:trx-13 :sig-04 :x-transaction-tag-lifecycle] :enforced-by :graph}
  {:id :transaction/lifecycle :says "відкриття робить reactive-система домену-дієслова, створюючи рядок архетипом і пишучи ідентифікацію, сесійний стан і стадію; зміну несе командна подія зі значеннями команди, яку споживає лише reactive-система того ж домену; фіксація — запис колонки стадії; завершення — ФАКТ-рядок у таблиці підкладкового домену плюс подія, і споживачі читають таблицю фактів" :governs :archetype :from [:trx-04 :trx-12 :trx-14] :enforced-by :agent}
  {:id :transaction/single-entity-state :says "увесь стан транзакції живе на ОДНІЙ сутності: ні копії в однопримірниковому компоненті, ні другого дому, ні одного логічного стану, продубльованого по піддоменах і синхронізованого подіями" :governs :archetype :from [:trx-09 :trx-17] :enforced-by :agent}
  {:id :transaction/no-substrate-state :says "ніколи не класти стан у підкладковий домен заради того, щоб його прочитав інший шар; чистий стан вибору в інтерфейсі лишається в presentation" :governs :layer :from [:trx-10 :trx-17] :enforced-by :agent}
  {:id :transaction/ui-projection :says "інтерфейс — проєкція: його системи читають transaction-сутність прямо і звіряються ідемпотентно, ввід гравця йде командними імпульсами, ніколи записами, view живить систему локальною подією і сам імпульсу не піднімає, інтерфейс стану транзакції не тримає, а показує факти або живу сутність, ніколи копію-знімок" :governs :view :from [:trx-07 :trx-11 :trx-15 :trx-17] :enforced-by :agent}]}
```

## Правила — view і межа view

```clojure
{:group :view
 :subject "що таке шар view, що йому заборонено і як він спілкується зі своєю системою"
 :rules
 [{:id :view/layer :says "шар view — усе, що оголошено в папці view; партнер view-системи, тобто тип, який піднімає локальну подію і якого називає маркер підписника, додатково є нащадком базового класу обʼєкта сцени" :governs :view :from [:plc-38 :gfx-02 :vws-11 :x-view-definition] :enforced-by :graph :needs-owner :oq-view-in-views-folder}
  {:id :view/boundary :says "view ніколи не створює сутність, ніколи не піднімає ECS-подію і ніколи не отримує реєстр сховищ чи сховище сутностей — ні полем, ні параметром конструктора, ні параметром методу інʼєкції або конструювання" :governs :view :from [:arch-64 :vws-07 :rcp-02 :x-view-boundary-receives] :enforced-by :graph :note "правило без помилки компіляції — рішення :view-boundary; живих відхилень сьогодні дванадцять"}
  {:id :view/inward :says "усередину: view піднімає локальну подію мови, а керівна система чи підсистема підписується на неї прямо — view не знає сховища" :governs :view :from [:arch-65 :vws-03 :vws-01] :enforced-by :graph}
  {:id :view/outward :says "назовні: система штовхає у view по ОДНОМУ значенню — ніколи побудований у системі список чи масив" :governs :view :from [:vws-04 :pfr-08 :ach-16 :vws-10] :enforced-by :arch-check}
  {:id :view/ecs-only-across-a-boundary :says "ECS-імпульс між view і системою доречний, лише коли сигнал перетинає межу кадру або межу збірки, якої прямий виклик не досягає, і піднімає його СИСТЕМА" :governs :event :from [:vws-05 :evt-05] :enforced-by :agent}
  {:id :view/subscriber-marker :says "клас, що власним додаванням обробника підписується на подію view, несе маркер підписника з типом цього view; маркер без справжньої підписки, як і підписка без маркера, — порушення, а названий тип мусить бути нащадком базового класу обʼєкта сцени" :governs :view-marker :from [:gsk-03 :vpr-01 :vpr-02 :msa-03 :atr-03 :gsk-06] :enforced-by :analyzer}
  {:id :view/subscribe-once :says "система підписується на view один раз під сторожем — view живе довше — і відписується у своєму звільненні" :governs :view :from [:vws-08] :enforced-by :agent}
  {:id :view/handler-synchronous :says "обробник події view працює синхронно в колбеку інтерфейсу на головному потоці: спершу запис у сховище, потім штовхання назад у view — ніколи відкладання запису на наступний тік прапорцем" :governs :view :from [:vws-09 :vws-10] :enforced-by :agent}
  {:id :view/shape :says "view — запечатаний нащадок базового класу обʼєкта сцени з публічними подіями мови, піднятими на взаємодії, і методами встановлення значень, що привʼязують елементи інтерфейсу" :governs :view :from [:vws-07] :enforced-by :agent}
  {:id :view/subscription-visible :says "кожна підписка видна у підписника — пошук по імені події дає всіх слухачів" :governs :view :from [:vws-06] :enforced-by :agent}
  {:id :view/no-business-logic :says "у папці view немає бізнес-логіки, і сама папка існує лише в presentation" :governs :role-folder :from [:plc-38] :enforced-by :agent}]}
```

## Правила — конфіги й addressables

```clojure
{:group :configs-and-addressables
 :subject "авторські дані, їхнє завантаження і володіння дескриптором завантаженого ассета"
 :rules
 [{:id :config/is :says "конфіг — ассет авторських даних, що живе в реєстрі сховищ за своїм типом; читання кидає, коли конфіг не завантажено" :governs :config :from [:cfg-01 :cfg-06] :enforced-by :agent}
  {:id :config/shape :says "тип конфіга — запечатаний нащадок базового ассета даних із атрибутом створення ассета, приватними серіалізованими полями і властивостями лише для читання" :governs :config :from [:cfg-02 :plc-34] :enforced-by :agent}
  {:id :config/validate :says "тип конфіга реалізує контракт валідації тоді, коли авторські дані можуть бути хибними; валідація кидає на кожне порушення авторингу, викликається одразу після завантаження і живе в ассеті, ніколи в системі, а споживач перевіряє ще раз" :governs :validatable :from [:cfg-03 :cfg-10 :pch-15 :arch-101 :ldr-07 :x-validatable-config] :enforced-by :agent}
  {:id :config/container :says "контейнер-каталог — ассет даних із масивом під-конфігів того самого абстрактного типу" :governs :config :from [:cfg-04 :cat-03] :enforced-by :graph}
  {:id :config/key-is-the-type :says "ключ сховища конфігів — тип: один збережений примірник на тип, а два ассети однієї форми вимагають абстрактної бази й окремого запечатаного нащадка на кожен" :governs :config :from [:cfg-05] :enforced-by :agent}
  {:id :config/read-the-asset :says "читається сам ассет — без копії, без сплющення, без сторожа присутності й тихого пропуску навколо читання" :governs :config :from [:cfg-07] :enforced-by :agent}
  {:id :config/lifetime :says "конфіг завантажується раз на кроці завантаження конфігів і ніколи не звільняється: loader тримає лише значення дескриптора і сам дескриптор не звільняє" :governs :config-loader :from [:cfg-08 :ldr-02 :adr-16 :x-config-box-lifetime] :enforced-by :agent}
  {:id :config/immutable :says "конфіг ніколи не мутується, а його масиви спільні — лише читання, без захисного клонування" :governs :config :from [:cfg-09] :enforced-by :agent}
  {:id :config/derived-objects :says "обʼєкти, збудовані з конфіга, належать системі кроку побудови примірників: цей крок може будувати runtime-однопримірникові компоненти й обʼєкти, але не може вантажити ассети й не виконує ігрової чи покадрової логіки" :governs :app-state :from [:cfg-11 :ldr-08] :enforced-by :agent}
  {:id :config/off-thread-read :says "читати поля конфіга поза головним потоком дозволено — це прості дані" :governs :config :from [:cfg-12 :arch-79] :enforced-by :agent}
  {:id :config/loader :says "loader конфіга — лише узагальнена системa завантаження: вона вантажить ассет, кидає при невдачі, валідує, кладе в сховище і ніколи не звільняє; ніколи рукописний loader, нащадок на конфіг, компонент-конфіг чи таблиця сутностей для запитів конфігів" :governs :config-loader :from [:ldr-02 :ldr-04 :ldr-10 :cfg-06 :adr-19] :enforced-by :graph}
  {:id :config/address-constant :says "адреса addressable — константа у тримачі адрес, у верхньому регістрі через підкреслення, названа за типом конфіга, значення якої дорівнює імені запису в Unity; передається константа, ніколи рядковий літерал і ніколи імʼя типу" :governs :config-address :from [:pch-14 :ldr-05 :adr-18 :adr-19] :enforced-by :agent}
  {:id :config/instance-objects-step :says "система похідних обʼєктів — запечатаний разовий крок, що бере свій прапорець стану в конструкторі, читає конфіг і пише похідний однопримірниковий компонент" :governs :one-shot-base :from [:ldr-03] :enforced-by :agent}
  {:id :addressable/box-decides-release :says "звільнення вирішує коробка дескриптора, не статус: якщо всередині щось є, рівно один власник звільняє її, інакше витік" :governs :box :from [:adr-04] :enforced-by :agent}
  {:id :addressable/empty-box :says "невдалий статус означає порожню коробку, а звільнення порожньої чи типової коробки нічого не робить — безумовне звільнення у блоці завершення безпечне" :governs :box :from [:adr-05] :enforced-by :agent}
  {:id :addressable/instance-release :says "звільнення завантаженого примірника саме знищує обʼєкт сцени — ніколи знищувати його руками" :governs :addressable :from [:adr-06] :enforced-by :agent}
  {:id :addressable/no-prefab-loadasync :says "prefab вантажиться лише викликом завантаження з інстанціюванням; узагальнене завантаження обʼєкта сцени захищено і повертає невдачу" :governs :addressable :from [:adr-07] :enforced-by :agent}
  {:id :addressable/cancel-throws :says "скасування — виняток, а не статус: коробка до викликача не доходить, після власних подальших очікувань кидається на токен, а утримувані коробки звільняються у блоці завершення" :governs :addressable :from [:adr-08] :enforced-by :agent}
  {:id :addressable/one-owner :says "у коробки рівно один власник, а звільнене поле-коробка замінюється на порожню" :governs :box :from [:adr-09] :enforced-by :agent}
  {:id :addressable/ctor-injection :says "контракт завантаження інжектується лише через конструктор, а рідний API пакета поза його реалізацією не чіпають" :governs :addressable :from [:adr-10] :enforced-by :agent}
  {:id :addressable/box-field-disposable :says "клас, що зберігає поле-коробку, реалізує контракт звільнення або інакше маршрутизує звільнення" :governs :box :from [:adr-11] :enforced-by :agent}
  {:id :addressable/load-failure :says "невдале завантаження не-сценового ассета кидає виняток з адресою; успішний дескриптор утримується, а при знятті звільняється й скидається в порожній" :governs :addressable :from [:adr-12] :enforced-by :agent}
  {:id :addressable/missing-component :says "prefab без потрібного компонента звільняє коробку і кидає; типізований компонент повертається обгорнутою коробкою з каскадним звільненням" :governs :box :from [:adr-13] :enforced-by :agent}
  {:id :addressable/sequential-rollback :says "послідовні завантаження йдуть з відкатом: локальні порожні коробки, завантаження в захищеному блоці, фіксація в поля, скидання локальних, а в блоці завершення — звільнення кожної локальної" :governs :box :from [:adr-14] :enforced-by :agent}
  {:id :addressable/field-release :says "звільнення полів іде під сторожем звільненості через helper, що звільняє коробку і скидає її в порожню" :governs :box :from [:adr-15] :enforced-by :agent}
  {:id :addressable/never :says "ніколи адреса з імені типу, ніколи ручне знищення завантаженого обʼєкта, ніколи збережене значення при викинутій коробці, ніколи одна коробка двом власникам, ніколи читання значення без перевірки присутності, ніколи тихий вихід на скасуванні, ніколи звільнення в гілці перед киданням замість блоку завершення і ніколи поле-коробка без контракту звільнення" :governs :box :from [:adr-17] :enforced-by :agent}]}
```

## Правила — потік і структурні зміни

```clojure
{:group :threading-and-structural
 :subject "головний потік як межа сховища і безпечна ідіома структурної зміни"
 :rules
 [{:id :thread/main-only :says "кожен виклик сховища — лише головний потік: створення, запис, читання, видалення, пошук в індексі, підняття події" :governs :entity-store :from [:arch-78 :evt-10 :pst-11] :enforced-by :agent}
  {:id :thread/off-thread-plain-data :says "поза головним потоком — лише обчислення над простими даними й native-контейнерами, і читання полів конфіга сюди входить" :governs :entity-store :from [:arch-79 :cfg-12 :pst-11] :enforced-by :agent}
  {:id :thread/bridge :says "перед першим викликом сховища робиться перехід на головний потік, а подальші обчислення повертаються в пул; перехід на головний потік продовжується на наступному Update, тобто коштує близько кадру" :governs :entity-store :from [:arch-80 :pst-11] :enforced-by :agent}
  {:id :thread/no-off-thread-write :says "ніколи запис у сховище поза головним потоком — індексована колонка перекладає рядок на запис і псує індекс" :governs :entity-store :from [:arch-81] :enforced-by :agent}
  {:id :structural/enumeration-throws :says "структурна зміна всередині перелічення кидає — так само, як додавання колонки, яку архетип уже має, і як зміна непов'язаної сутності; сторож стоїть на все сховище" :governs :structural-change :from [:arch-83] :enforced-by :agent}
  {:id :structural/delete :says "видалення сутності не структурна зміна, але воно змінює перелічувану множину" :governs :structural-change :from [:arch-84] :enforced-by :agent}
  {:id :structural/idiom :says "ідіома безпечної зміни: зібрати ідентифікатори сутностей у native-список, закрити перелічення, знову взяти кожну сутність за ідентифікатором зі сховища і створювати й писати в другому проході" :governs :structural-change :from [:arch-85 :rea-07 :ror-12] :enforced-by :agent}
  {:id :structural/no-entity-snapshot :says "ніколи знімок значень сутностей — дескриптор сутності не є некерованим типом" :governs :structural-change :from [:arch-86] :enforced-by :agent}]}
```

## Правила — розміщення: шари, залежності, папки

```clojure
{:group :placement
 :subject "де живе новий код, на що йому можна посилатися і яку роль несе його папка"
 :rules
 [{:id :place/domain :says "домен — правила гри одного обмеженого контексту: таблиці сутностей і логіка, що їх змінює; він ніколи не залежить від presentation і ніколи не містить view, prefab чи текстур; одна збірка на домен, фіча — папка всередині нього, а нового домену без рішення власника не буває" :governs :layer :from [:plc-01 :plc-16 :plc-22] :enforced-by :agent}
  {:id :place/presentation :says "presentation показує доменний стан і приймає намір гравця: ніколи правило гри, ніколи стан, яким володіє домен, і ніколи пряма зміна доменної таблиці — лише командна подія; вигляд світу лежить в області збірки presentation, HUD — у папці вікна, а області йдуть підпапками збірки" :governs :layer :from [:plc-02 :plc-17 :plc-23] :enforced-by :agent}
  {:id :place/module :says "модуль — чиста логіка біля рушія, що не знає правил цієї гри і ніколи їх не містить; модуль з однією збіркою є однією фічею з рольовими папками прямо під коренем, парний модуль ділиться на збірку контракту і збірку реалізації з рольовими папками в кожній" :governs :layer :from [:plc-03 :plc-17 :plc-24 :plc-25] :enforced-by :agent}
  {:id :place/shared-kernel :says "shared kernel тримає примітиви й базові ECS-типи для всіх шарів — лише код, потрібний кільком шарам і не знаючий жодного з них" :governs :layer :from [:plc-04 :plc-18] :enforced-by :agent}
  {:id :dep/shared-kernel :says "shared kernel залежить лише від зовнішніх бібліотек і рушія, а посилатися на нього може будь-який шар" :governs :assembly :from [:plc-06 :plc-09] :enforced-by :agent}
  {:id :dep/module :says "модуль ідеально не має залежностей, може посилатися на інший модуль і на shared kernel, і ніколи — на домен чи presentation" :governs :assembly :from [:plc-07] :enforced-by :agent}
  {:id :dep/layer-to-module :says "домен і presentation посилаються на будь-яку кількість модулів, але лише на збірку контракту парного модуля або на модуль з однією збіркою — ніколи на збірку реалізації" :governs :assembly :from [:plc-08 :plc-41] :enforced-by :agent}
  {:id :dep/presentation-to-domain :says "presentation посилається на домен в один бік — на те, що показує; зворотного посилання не буває" :governs :assembly :from [:plc-10] :enforced-by :agent}
  {:id :dep/domain-tiers :says "домени мають яруси: описує, що робить власник, — дієслово; знає конкретного власника — агент; інакше підкладка; домен посилається лише на свій або попередній ярус" :governs :assembly :from [:plc-11] :enforced-by :agent}
  {:id :dep/no-cycles :says "циклів залежностей між збірками нема" :governs :assembly :from [:plc-12] :enforced-by :agent}
  {:id :dep/boot-exempt :says "збірка запуску, що складає стан гри, стоїть поза всіма правилами залежностей" :governs :assembly :from [:plc-13] :enforced-by :agent}
  {:id :place/namespace :says "простір імен іде за шляхом папки" :governs :role-folder :from [:plc-15 :cmp-02 :tag-03 :evt-03] :enforced-by :agent}
  {:id :folder/archetypes :says "папка архетипів лежить прямо під коренем збірки і лише тоді, коли збірка оголошує архетипи" :governs :role-folder :from [:plc-26] :enforced-by :agent}
  {:id :folder/shared-role :says "рольова папка живе всередині фічі, області чи збірки модуля; прямо під коренем домену чи presentation вона стоїть лише як спільна роль, вміст якої ділять кілька фіч або областей" :governs :role-folder :from [:plc-28 :plc-30] :enforced-by :agent}
  {:id :folder/no-code-outside-a-role :says "коду поза рольовою папкою не буває" :governs :role-folder :from [:plc-29] :enforced-by :agent}
  {:id :folder/roles :says "кожна рольова папка має закритий вміст і власну заборону" :governs :role-folder :from [:plc-31 :plc-32 :plc-33 :plc-34 :plc-35 :plc-36 :plc-37 :plc-38] :enforced-by :agent
   :folders {:components "чисті структури даних, ніколи логіка чи побічні ефекти"
             :tags "тег-компоненти"
             :events "однокадрові компоненти подій"
             :configs "класи ассетів даних, ніколи runtime-логіка й ніколи самі ассети"
             :data "колекції, записи, перелічення, ніколи ECS-системи чи обʼєкти сцени"
             :systems "ECS-системи й оркестрація, ніколи логіка view чи визначення конфігів"
             :helpers "stateless-обчислення, єдина назва цієї ролі, ніколи стан між кадрами чи володіння сутностями"
             :views "шар view, ніколи бізнес-логіка, лише в presentation"}}]}
```

## Правила — іменування

```clojure
{:group :naming
 :subject "суфікс ролі, самодостатність імені типу і межа доменного префікса"
 :rules
 [{:id :name/suffix :says "суфікс несе роль: дані — компонент, безполевий маркер — тег, однокадровий імпульс — подія, посилання між таблицями — імʼя ключа власника з позначкою посилання перед словом компонент" :governs :component :from [:arch-91 :cmp-11 :tag-10 :evt-07 :gfx-07] :enforced-by :graph}
  {:id :name/self-sufficient :says "імʼя типу зрозуміле без простору імен, імʼя фічі повторюється завжди і ніколи не лишається голої ролі, а розрізнення через псевдонім чи кваліфікатор простору імен не буває" :governs :assembly :from [:arch-92] :enforced-by :agent}
  {:id :name/prefix-exception :says "доменний префікс прибирається, і єдиний виняток — компонент ідентичності або дискримінатор таблиці, на які посилаються З ІНШИХ доменів" :governs :component :from [:arch-93 :cmp-12 :tag-11 :orc-11 :x-naming-prefix-exception] :enforced-by :agent :note "інсталер домену зберігає префікс, але його форма — предмет блоку DI і в набір не входить"}
  {:id :name/static :says "static-клас — лише для stateless-утиліти без полів" :governs :assembly :from [:arch-94] :enforced-by :agent}]}
```

## Правила — форма методів, гучна помилка, коментарі

```clojure
{:group :methods-fail-loud-comments
 :subject "як метод віддає результат, як код падає на відсутній передумові і де стоїть коментар"
 :rules
 [{:id :method/try :says "метод, що може не вдатися, повертає прапорець успіху і віддає вихід окремим параметром, а викликач розгалужується саме за прапорцем" :governs :query-helpers :from [:arch-95 :pch-06] :enforced-by :agent}
  {:id :method/return-value :says "певний один результат повертається значенням" :governs :query-helpers :from [:arch-96] :enforced-by :agent}
  {:id :method/no-inferred-success :says "ніколи виводити успіх з результату — ні з порожнього посилання, ні з нульової кількості, ні з нульової довжини" :governs :query-helpers :from [:arch-97] :enforced-by :agent}
  {:id :method/collection-by-ref :says "колекція, яку метод пише, передається за посиланням, ніколи за значенням" :governs :managed-collection :from [:arch-97] :enforced-by :agent}
  {:id :method/inline-helper :says "одноразовий приватний helper, що читає поля й розгортає лінійний потік викликача, лишається на місці: виноситься лише самодостатній за сигнатурою метод, який перевикористовується" :governs :system :from [:arch-98] :enforced-by :agent}
  {:id :method/try-get-first :says "пошук одного рядка йде спробним отриманням першого на запиті, архетипі чи множині сутностей" :governs :query-helpers :from [:pch-06] :enforced-by :agent}
  {:id :fail/throw :says "крок, що не може виконати свою роботу, кидає описовий виняток, називаючи те, чого бракує" :governs :system :from [:arch-99 :ntn-02 :pst-03] :enforced-by :agent}
  {:id :fail/no-silence :says "ніколи тихе повернення на відсутній передумові і ніколи попередження в лог із пропуском роботи" :governs :system :from [:arch-100 :ntn-02] :enforced-by :agent}
  {:id :fail/cancel :says "скасування перевіряється киданням на токен окремим рядком після кожного очікування, ніколи разом із перевіркою валідності і ніколи тихим поверненням" :governs :system :from [:arch-102 :pch-07 :ldr-09 :pst-06 :adr-08] :enforced-by :agent}
  {:id :fail/cleanup :says "те, чим володіє скасована робота, звільняється в блоці завершення, а часткова робота прибирається перед киданням" :governs :system :from [:arch-103 :adr-08] :enforced-by :agent}
  {:id :fail/exception-type :says "виняток за замовчуванням — виняток недопустимої операції з повідомленням" :governs :system :from [:arch-104 :cfg-02] :enforced-by :agent}
  {:id :fail/silence-boundary :says "межа гучної помилки: відсутність обробника кидає, коли через неї робота ЗАГУБИЛАСЯ — рядок не зʼявився, ассет не завантажився; тихий пропуск законний лише там, де нічого не просили й нічого не винні, і сам рецепт називає цю відсутність валідним станом" :governs :system :from [:cat-10 :x-fail-loud-vs-silent-no-op] :enforced-by :agent}
  {:id :async/token :says "довга async-задача бере токен у монітора стану застосунку" :governs :status-monitor :from [:pch-07] :enforced-by :agent}
  {:id :comment/on-the-thing :says "коментар — короткий, на самій речі, оновлюваний у тому ж диффі: намір, неочевидні інваріанти, контракти — і лише там, де логіка перестає бути простою й однозначною" :governs :system :from [:arch-89] :enforced-by :agent}
  {:id :comment/never :says "ніколи коментар про ІНШИЙ файл — лише посилання за імʼям або перенесення факту до власника — і ніколи шаблонна XML-документація" :governs :system :from [:arch-90] :enforced-by :agent}
  {:id :deviation/in-code :says "відоме відхилення записується коментарем у коді в місці відхилення" :governs :system :from [:arch-105] :enforced-by :agent}]}
```

## Правила — вибір форми, поліморфний каталог, ознаки рецептів

```clojure
{:group :form-choice-and-signatures
 :subject "яку форму набуває нова частина коду і за якою ознакою її примірник упізнається"
 :rules
 [{:id :choose/recipe-tree :says "форма береться за предметом частини: ECS-дані йдуть у рецепти даних, конфіг — у рецепти конфігів, поведінка — у рецепти поведінки, view — у рецепт пари view і системи, робота з ассетами — у рецепт addressables; коли жодна гілка не тримає, рецепта нема" :governs :system :from [:pch-01] :enforced-by :agent}
  {:id :choose/data-recipe :says "серед даних: несе значення — компонент, позначає ідентичність рядка — тег, сигналізує зміну — подія" :governs :component :from [:pch-02] :enforced-by :agent}
  {:id :choose/config-recipe :says "серед конфігів: багато видів різної форми зі спільним ключем у таблицю сутностей — поліморфний каталог, завантажується чи будується на старті — конфіг плюс loader, інакше просто конфіг" :governs :config :from [:pch-03] :enforced-by :agent}
  {:id :choose/behavior-recipe :says "серед поведінки: охоплює кілька піддоменів — transaction-сутність; будує світ при створенні мапи з незалежно впорядкованими частинами — оркестратор із підсистемами, просто будує світ — стадія конвеєра; фаза ходу — оркестратор із підсистемами; реагує на подію з незалежними частинами — reactive-оркестратор, просто реагує — reactive; неперервно щокадру — per-frame; прибирає події — cleanup" :governs :system :from [:pch-04] :enforced-by :agent}
  {:id :catalogue/when :says "поліморфний каталог береться, коли разом: кілька авторських видів різної форми зі спільним ключем, runtime запитує чи зʼєднує записи за ключем, а новий вид додається без правки оркестратора; один конфіг однієї форми і однорідний список на одне читання каталогом не є, і поведінки на конфігу не буває" :governs :config :from [:cat-01 :cat-02] :enforced-by :agent}
  {:id :catalogue/config-shape :says "абстрактний базовий конфіг тримає лише спільний ключ, конкретні види додають свої параметри або нічого, а контейнер тримає масив базових конфігів" :governs :config :from [:cat-03 :sig-10] :enforced-by :graph}
  {:id :catalogue/container :says "контейнер вантажиться одним loader-ом на кроці завантаження конфігів, реалізує валідацію і перевіряє, що записи не порожні — порожній каталог зупиняє гру на старті" :governs :config-loader :from [:cat-04] :enforced-by :agent}
  {:id :catalogue/spawn-orchestrator :says "оркестратор спавну — стадія конвеєра створення мапи: обходить записи і віддає кожен першій підсистемі, що обробляє його конкретний тип; порожній запис або тип без підсистеми кидає" :governs :pipeline-base :from [:cat-05 :cat-09] :enforced-by :agent}
  {:id :catalogue/row-shape :says "рядок каталогу народжується архетипом з усіма колонками виду: ключ-посилання простору субʼєкта, рівно один головний тег, мітка сімейства коли архетипів кілька, колонка виду над перелічуванням, записана раз, параметри виду і початковий стан; вибір іде self-index виду, ніколи другим головним тегом і ніколи вгадуванням за присутністю параметрів" :governs :archetype :from [:cat-06 :cat-11] :enforced-by :graph}
  {:id :catalogue/evaluator :says "необовʼязкове сімейство переоцінювачів читає свій зріз виду з self-index, зʼєднує цільову таблицю за ключем і звіряє колонку стану лише при зміні; запускається кожен ввімкнений безумовно, кожен сам запитує свої рядки, порожній зріз — валідний стан" :governs :subsystem :from [:cat-08 :cat-10] :enforced-by :agent}
  {:id :catalogue/dual-host :says "переоцінка для першого ходу і для кожного наступного живе двома тонкими хостами над тим самим сімейством: стадія конвеєра одразу після спавну і фаза ходу в хвості конвеєра ходу" :governs :subsystem :from [:cat-12] :enforced-by :agent}
  {:id :catalogue/extension :says "новий вид — це конкретний підклас конфіга, нове значення перелічування з компонентом параметрів, підсистема спавну і за потреби переоцінювач; оркестратор при цьому не змінюється" :governs :subsystem :from [:cat-15] :enforced-by :agent}
  {:id :catalogue/checklist :says "каталог повний, коли складені всі пʼять частин: абстрактна база з ключем і контейнер, завантаження з валідацією, оркестратор спавну з підсистемами, рядок таблиці з колонкою виду і за потреби сімейство переоцінювачів" :governs :config :from [:cat-16] :enforced-by :agent :note "рядок чеклиста про звільнення дескриптора loader-ом знято правилом часу життя конфіга"}
  {:id :recipe/signatures :says "примірник рецепта впізнається за ознакою, і ознака є правилом написання коду" :governs :recipe-signature :from [:sig-01 :sig-02 :sig-03 :sig-04 :sig-05 :sig-06 :sig-07 :sig-08 :sig-10 :sig-11 :sig-12] :enforced-by :graph
   :signatures {:component "оголошена struct виду компонента"
                :tag "оголошена struct з контрактом тегу, поділена на головні й мітки"
                :event "оголошена struct з суфіксом події"
                :transaction-entity "архетип несе мітку ролі transaction"
                :reactive "клас із роллю reactive — подієвий якір у базовому виклику або reactive-маркер"
                :per-frame "клас із роллю per_frame"
                :view-system "ребро підписки від класу з маркером підписника view"
                :pipeline-stage "клас реалізує впорядкований async-контракт зі стадією створення мапи"
                :polymorphic-catalogue "абстрактна база ассета з суфіксом конфіга плюс неабстрактний контейнер із масивом цієї бази плюс колонка виду, названа за базою і несена архетипом"
                :cleanup "Update-клас обходить тег події і видаляє"
                :turn-phase "нащадок бази фаз ходу"
                :startup-step "негенеричний async-контракт"}}]}
```

## Розвʼязання суперечностей

Двадцять записів CONTEXT.md # Overlaps and contradictions. Кожен розвʼязаний в авто-режимі: варіанти з оцінкою 0-100, обрано найвищий. `:wrong-carrier` — носій, чиє формулювання після цього вважається хибним і який звірятиме GRAPH_STANDARD.

```clojure
[{:id :x-view-definition :auto-decided true :confidence 70
  :resolved "шар view — усе, що оголошено в папці view; межа view і виняток керованих колекцій вʼяжуть кожен тип у цій папці, а партнер view-системи додатково є нащадком базового класу обʼєкта сцени"
  :rules [:view/layer :view/boundary :alloc/view-exempt :view/subscriber-marker]
  :because "рішення власника буквальне, а вимога нащадка обʼєкта сцени потрібна лише там, де її справді міряє аналізатор — у маркері підписника; так обидві реалізації лишаються читанням одного правила"
  :wrong-carrier "граф звужує шар до нащадків обʼєкта сцени, діагностика маркера розширює партнера на будь-який обʼєкт сцени поза папкою, ARCHITECTURE каже просто обʼєкт сцени"
  :options [{:option "шар = папка, партнер = обʼєкт сцени в папці" :confidence 70}
            {:option "view = лише обʼєкт сцени в папці" :confidence 25}
            {:option "view = будь-який обʼєкт сцени" :confidence 5}]}

 {:id :x-role-marker :auto-decided true :confidence 70
  :resolved "роль вирішує форма за фіксованим порядком гілок; маркер існує рівно для однієї форми, якої порядок не вирішує — Update-клас, що тримає архетип події поза базовим викликом, — і його значення мусить збігатися з формою: Reactive вимагає відсутності табличного якоря, PerFrame — відсутності подієвого; маркер там, де форма вирішує, заборонений"
  :rules [:system/role-order :system/marker-required :system/marker-forbidden :system/marker-value]
  :because "табличний якір робить каденцію покадровою за побудовою, тому заявка Reactive на ньому справді хибна; гілка прибирання стоїть першою й маркера не має, тож аналізатор її ніколи не бачить"
  :wrong-carrier "roles.py: гілка маркера дозволяє будь-яке значення на класі з табличним якорем — інструмент має звузити її до PerFrame"
  :options [{:option "форма вирішує, маркер лише добирає нерозвʼязану форму, значення обмежене якорем" :confidence 70}
            {:option "маркер завжди старший за форму" :confidence 20}
            {:option "прибрати гілку маркера і лишити per_frame" :confidence 10}]}

 {:id :x-priority :auto-decided true :confidence 80
  :resolved "Priority повертає іменовану const int із тримача пріоритетів проєкту; локальна константа класу й літерал заборонені; вкладений клас тримача — простір порядку, значення порівнюються лише всередині нього, а пріоритети підсистем — лише всередині свого оркестратора"
  :rules [:system/priority-source :system/priority-space]
  :because "живий код тримача не має жодної локальної константи виконання і шістдесят девʼять разів читає тримач — скелети відстали від коду"
  :wrong-carrier "скелети рецептів per-frame, стадії конвеєра, оркестратора з підсистемами і поліморфного каталогу"
  :options [{:option "лише тримач пріоритетів" :confidence 80} {:option "локальна константа лишається для стадій" :confidence 20}]}

 {:id :x-stateless-vs-lifetime-flags :auto-decided true :confidence 65
  :resolved "закон лишається як є: сторож підписки, сторож повторного входу, сторож звільнення і поле дескриптора ассета в системі — стан, і кожне таке поле несе позначення дозволеного стану з причиною"
  :rules [:state/lifetime-flags :state/no-instance-state :state/escape-order]
  :because "загальний виняток сховав би саме те, що прапорець називає; порядок виходу вже існує і цей випадок покриває"
  :wrong-carrier "жоден носій не хибний — хибний живий код: пʼять систем несуть такі поля без позначення"
  :needs-owner :oq-stateless-vs-lifetime-flags
  :options [{:option "закон як є, поля позначаються з причиною" :confidence 65}
            {:option "виняток для підписки й дескриптора ассета" :confidence 35}]}

 {:id :x-generic-in-repeated-orchestrator :auto-decided true :confidence 70
  :resolved "виняток елементів керованого типу покриває зібраний DI список у повторній системі: колекція виділяється раз при побудові власника і тримається; упорядкування в конструкторі — теж разове виділення; поле лишається станом і несе позначення"
  :rules [:alloc/managed-element-exception :state/injected-collection]
  :because "дві заборони про різне — одна про каденцію виділення, друга про змінність поля; перша знімається винятком, друга позначенням"
  :wrong-carrier "arch-check Rule 1: перелік заборонених типів не має пункту винятку, тому інструмент строгіший за закон"
  :options [{:option "виняток покриває, поле позначається" :confidence 70}
            {:option "оркестратор мусить обійтися без керованої колекції" :confidence 15}
            {:option "повторна система з таким полем — окремий клас винятку" :confidence 15}]}

 {:id :x-config-box-lifetime :auto-decided true :confidence 80
  :resolved "конфіг живе всю сесію: loader тримає лише значення дескриптора і сам дескриптор не звільняє"
  :rules [:config/lifetime]
  :because "три носії з чотирьох кажуть це прямо, і саме так поводиться єдина узагальнена система завантаження"
  :wrong-carrier "чеклист поліморфного каталогу — рядок про звільнення дескриптора в dispose"
  :options [{:option "конфіг не звільняється" :confidence 80} {:option "loader звільняє дескриптор" :confidence 20}]}

 {:id :x-subsystem-query-caches :auto-decided true :confidence 75
  :resolved "кеші запитів належать сховищу, тому підсистемі звільняти в них нічого; її звільнення прибирає лише те, що вона сама виділила — native-буфери й дескриптори ассетів"
  :rules [:orchestrator/query-caches]
  :because "архетип, запит і індекс не є ресурсом підсистеми — їх тримає сховище"
  :wrong-carrier "PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM: кожна підсистема нібито володіє своїми кешами і звільняє їх"
  :options [{:option "кеші належать сховищу" :confidence 75} {:option "кожна підсистема звільняє свої кеші" :confidence 25}]}

 {:id :x-validatable-config :auto-decided true :confidence 70
  :resolved "контракт валідації реалізується тоді, коли авторські дані можуть бути хибними; контейнер каталогу такий завжди — порожні й відсутні записи можливі, — тому він реалізує його завжди"
  :rules [:config/validate :catalogue/container]
  :because "три носії стають одним правилом, щойно «можуть бути хибними» читається як властивість даних, а не як смак автора"
  :wrong-carrier "pattern-choice: беззастережне «тип конфіга реалізує контракт валідації»"
  :options [{:option "за властивістю даних" :confidence 70} {:option "кожен конфіг валідується" :confidence 30}]}

 {:id :x-naming-prefix-exception :auto-decided true :confidence 65
  :resolved "доменний префікс дозволений лише компоненту ідентичності або дискримінатору таблиці, на які посилаються З ІНШИХ доменів; суто локальний дискримінатор префікса не носить"
  :rules [:name/prefix-exception]
  :because "сенс винятку — розрізнити однойменні ключі двох доменів у місці читання; без міждоменності розрізняти нема чого"
  :wrong-carrier "PATTERN_TAG: виняток для дискримінаторів без умови міждоменності"
  :options [{:option "лише міждоменні" :confidence 65} {:option "будь-який дискримінатор таблиці" :confidence 35}]}

 {:id :x-event-suffix :auto-decided true :confidence 75
  :resolved "правило одне: тип події має суфікс події; довший легасі-суфікс закритий — його несе рівно один живий тип і жоден новий, а інструменти читають обидва, щоб бачити той один"
  :rules [:event/suffix :event/declare]
  :because "інструмент мусить бачити легасі-тип, інакше він випаде з графа; закон від цього не змінюється"
  :wrong-carrier "жоден: закон і інструменти говорять про різне — закон про написання, інструмент про впізнавання"
  :options [{:option "один суфікс плюс закритий легасі-виняток" :confidence 75} {:option "обидва суфікси законні" :confidence 25}]}

 {:id :x-catalogue-key-naming :auto-decided true :confidence 70
  :resolved "ролі колонок стоять за суфіксом: ідентичність, посилання, дані; колонка виду сімейства каталогу — колонка даних, що працює як self-index, ніколи посилання, і носить суфікс виду"
  :rules [:key/enum-space :key/data :tag/kind-column :catalogue/row-shape]
  :because "закон ролей ключа і читач інструмента вже класифікують цю колонку як дані; посиланням її називає лише текст рецепта"
  :wrong-carrier "PATTERN_POLYMORPHIC_CATALOGUE: текст і скелет називають колонку виду ключем-посиланням; жива колонка виду району не має суфікса виду — це відхилення коду"
  :options [{:option "колонка виду — дані з self-index" :confidence 70} {:option "сторона посилання простору-перелічення може бути без суфікса" :confidence 30}]}

 {:id :x-transaction-tag-lifecycle :auto-decided true :confidence 85
  :resolved "у transaction-сутності один архетип на всю сесію, головний тег не змінюється, стадія — колонка стану, поруч стоїть мітка ролі transaction"
  :rules [:transaction/one-archetype :tag/state-column]
  :because "вступ рецепта суперечить і закону тегів, і власному розділу життєвого циклу того самого рецепта"
  :wrong-carrier "PATTERN_TRANSACTION_ENTITY, вступний рядок про життєвий цикл тегами"
  :options [{:option "стадія — колонка" :confidence 85} {:option "стадії позначаються тегами" :confidence 15}]}

 {:id :x-live-tag-write :auto-decided true :confidence 80
  :resolved "додавання тегу живій сутності записується прямо в заборону: тег зʼявляється лише в оголошенні архетипу, а після народження не буває ні видалення колонки чи тегу, ні пізнього додавання колонки, ні додавання тегу"
  :rules [:tag/added-by-archetype-only :birth/no-late-structural :tag/composed-by-declaration]
  :because "перелік заборон повноти народження неповний, хоча додавання тегу названо структурною зміною двома рядками вище"
  :wrong-carrier "ARCHITECTURE birth-completeness — неповний перелік заборонених операцій"
  :options [{:option "записати явно" :confidence 80} {:option "лишити як наслідок" :confidence 20}]}

 {:id :x-system-definition :auto-decided true :confidence 70
  :resolved "система — клас, який веде рушій за переліком базових контрактів; член сімейства підсистем — простий обʼєкт, яким володіє система, і заборони стану та керованих колекцій його не вʼяжуть; натомість заборона виділення на прогін вʼяже повторний ШЛЯХ виконання, у якому б класі він не лежав"
  :rules [:system/definition :system/subsystem-is-not-a-system :alloc/binds-the-path]
  :because "так знімається напруга без послаблення нульового виділення: стан — властивість класу, виділення — властивість шляху"
  :wrong-carrier "жоден носій не хибний — граф називає sub_system роллю, і це читалося як системність"
  :options [{:option "система за контрактом, виділення за шляхом" :confidence 70}
            {:option "заборони вʼяжуть і підсистеми" :confidence 30}]}

 {:id :x-cadence-turn-phase :auto-decided true :confidence 80
  :resolved "каденція читається зі стадії, яку система обслуговує, а не з інтерфейсу: упорядкований контракт зі стадією створення мапи — разова каденція, той самий контракт зі стадією ходу — повторна"
  :rules [:system/cadence :turn-phase/base :pipeline/stage]
  :because "база фаз ходу реалізує той самий упорядкований контракт і під описом за інтерфейсом підпадає під обидві каденції"
  :wrong-carrier "arch-check System cadence: каденція названа за інтерфейсом, а не за стадією"
  :options [{:option "за стадією" :confidence 80} {:option "за інтерфейсом" :confidence 20}]}

 {:id :x-stateallowed-reason :auto-decided true :confidence 75
  :resolved "позначення дозволеного стану завжди несе причину"
  :rules [:state/allowed-reason]
  :because "необовʼязковий параметр у типі — зручність компіляції, а не дозвіл; закон і pattern-choice кажуть однаково"
  :wrong-carrier "скелети рецептів оркестратора з підсистемами, reactive-оркестратора і поліморфного каталогу — голе позначення без причини"
  :options [{:option "причина обовʼязкова" :confidence 75} {:option "причина за смаком" :confidence 25}]}

 {:id :x-framebox-field :auto-decided true :confidence 70
  :resolved "поле коробки кадрів у системі — стан і несе позначення з причиною; місце коробки в порядку виходу означає перевагу ФОРМИ стану, а не звільнення від позначення"
  :rules [:state/frame-box-field :state/escape-order]
  :because "коробка є змінною структурою в переприсвоюваному полі, тобто рівно те, що закон називає станом; рецепт per-frame уже вимагає позначення"
  :wrong-carrier "жоден: закон описує порядок виходу, а не звільнення від позначення — читання було хибним"
  :options [{:option "поле коробки позначається" :confidence 70} {:option "коробка — самостійний вихід без позначення" :confidence 30}]}

 {:id :x-fail-loud-vs-silent-no-op :auto-decided true :confidence 65
  :resolved "відсутність обробника кидає, коли через неї робота ЗАГУБИЛАСЯ; тихий пропуск законний лише там, де нічого не просили й нічого не винні, і сам рецепт називає цю відсутність валідним станом"
  :rules [:fail/silence-boundary :fail/throw :catalogue/evaluator]
  :because "вид без підсистеми спавну означає рядок, якого не буде — це втрата; вид без переоцінювача означає, що переоцінювати нема чого"
  :wrong-carrier "жоден: асиметрію названо в самому рецепті каталогу, але без межі"
  :options [{:option "межа за втратою роботи" :confidence 65}
            {:option "кидати завжди" :confidence 20}
            {:option "тихий пропуск каталогу — відхилення" :confidence 15}]}

 {:id :x-static-field :auto-decided true :confidence 75
  :resolved "змінне static-поле в системі — стан: перелік не-стану закритий і містить лише константи й незмінні статичні значення"
  :rules [:state/mutable-static :state/is-not-state]
  :because "закон перелічує не-стан вичерпно, а детектор просто не вміє це побачити"
  :wrong-carrier "arch-check Rule 2: перелік непозначуваного виводить зі сканування будь-яке static-поле"
  :options [{:option "змінне static — стан" :confidence 75} {:option "static поза забороною" :confidence 25}]}

 {:id :x-view-boundary-receives :auto-decided true :confidence 80
  :resolved "view ніколи не отримує реєстр сховищ чи сховище сутностей жодним шляхом — ні полем, ні параметром конструктора, ні параметром методу інʼєкції або конструювання — і ніколи не створює сутність і не піднімає ECS-подію"
  :rules [:view/boundary]
  :because "чотири шляхи, які перевіряє інструмент, і є повним читанням правила; вужчі формулювання лишають дірки"
  :wrong-carrier "PATTERN_VIEW_SYSTEM: говорить лише про сховище сутностей і лише про інʼєкцію"
  :options [{:option "обидва типи, усі чотири шляхи" :confidence 80} {:option "заборонена лише інʼєкція" :confidence 20}]}]
```

```clojure
{:new-rules
 [{:id :system/cadence :because "каденцію за стадією не формулює жоден носій — вона народилась як розвʼязок :x-cadence-turn-phase"}
  {:id :state/mutable-static :because "жоден носій не каже про змінне static-поле в системі — правило виведене із закритого переліку не-стану (:x-static-field)"}
  {:id :alloc/binds-the-path :because "поширення заборони виділення на повторний ШЛЯХ, включно з підсистемами, — розвʼязок :x-system-definition"}
  {:id :fail/silence-boundary :because "межу між гучною помилкою і свідомим тихим пропуском не формулює жоден носій (:x-fail-loud-vs-silent-no-op)"}
  {:id :view/layer :because "двошарове визначення — шар за папкою, партнер за базовим класом — розвʼязок :x-view-definition, якого не несе жоден носій"}]}
```

## Відкриті питання власника

Три питання CONTEXT.md # Open questions. Кожне має обране читання, щоб набір лишався повним, але жодне не вважається закритим.

```clojure
[{:id :oq-stateless-vs-lifetime-flags :status :needs-owner :chosen-confidence 65
  :chosen "закон лишається як є; сторожі підписки, повторного входу, звільнення і поле дескриптора ассета — випадки позначеного стану з причиною"
  :rule-under-yes "правило :state/lifetime-flags у наборі; пʼять живих систем стають відхиленнями, які власник виправляє позначенням або переносом стану"
  :rule-under-no "правило :state/lifetime-flags замінюється винятком закону: підписка й дескриптор ассета не рахуються станом, і тоді :state/is-state дістає третій пункт переліку не-стану"
  :why-open "зачіпає живий код і те, що власник вважає станом"}

 {:id :oq-singleton-manifest :status :needs-owner :chosen-confidence 60
  :chosen "маніфест однопримірникових компонентів входить у набір як чинний закон"
  :rule-under-yes "правило :singleton/manifest у наборі: рівно один архетип-маніфест, використані компоненти дорівнюють оголошеним"
  :rule-under-no "правило виходить у виключені разом із блоком DI, з причиною «сховище під рефакторингом», а :singleton/read лишається"
  :why-open "власник назвав під рефакторингом лише реєстрацію DI, але сховище однопримірникового стану згадувалось як таке, що чекає змін"}

 {:id :oq-view-in-views-folder :status :needs-owner :chosen-confidence 60
  :chosen "межа view і виняток керованих колекцій вʼяжуть кожен тип у папці view, включно зі статичним класом-довідником"
  :rule-under-yes "правила :view/layer, :view/boundary і :alloc/view-exempt читають папку як шар — статичний довідник у ній теж не отримує сховища"
  :rule-under-no "шар звужується до нащадків базового класу обʼєкта сцени, і тоді один живий статичний клас у папці випадає з-під межі, а визначення знову розходиться з рішенням власника"
  :why-open "буквальне читання рішення власника дає наслідок, якого він міг не мати на увазі"}]
```

## Виходи — що свідомо поза набором

```clojure
{:exits
 [{:reason "форма реєстрації DI під рефакторингом — рішення власника :no-di-registration, межа :ad-di-boundary: виключено все, чий предмет — форма виклику реєстрації, час життя, ціль інтерфейсу, передані параметри, клас і розміщення інсталера, порядок установки модулів і ручне вписування систем у стан гри"
   :entries [:plc-05 :plc-19 :plc-27 :plc-40 :pch-09 :pch-13 :ldr-01 :ldr-06 :adr-02 :orc-06 :pfr-09 :pst-09 :rea-09 :ror-11 :cat-13 :gfx-10 :sig-09]
   :kept "правила про типи, константи й конструктори, які реєстрація лише споживає, лишаються в наборі: константа адреси, контракт валідації, вибір базового типу, форма конструктора, порожня колекція сімейства"}

  {:reason "процес агента, правило документа, нотація або поведінка інструмента — поза межами :bounds"
   :entries [:plc-00 :plc-14 :pch-00 :pch-20 :pch-21 :ach-18 :arch-106 :arch-107 :cmp-15 :tag-12 :evt-12 :adr-20 :gsk-05 :gsk-08 :gsk-09 :ecs-04 :sig-13 :sig-14 :sig-15 :trx-16 :ntn-01]
   :notes ["евристики впізнавання рецептів роду в (:sig-13 :sig-14 :sig-15) лишаються в коді інструмента — рішення :recipe-signatures"
           "приписування видалення сутності одному архетипу (:ecs-04) — межа висновку інструмента, не вимога до коду — рішення :ad-delete-entity"
           "вимога окремого документа на кожен transaction flow (:trx-16) прибрана рішенням :flow-contract-removed"]}

  {:reason "розміщення ассетів, а не коду — рішення :ad-assets"
   :entries [:plc-20 :plc-21 :plc-39]}

  {:reason "жива назва, історія або застарілий текст — доказ у факті, ніколи правило"
   :entries [:evt-13 :ach-19 :ror-06 :vws-02 :cat-17 :trx-05 :trx-06]}]

 :also-out #{"правила розповіді коду каскаду — рішення :code-story-rules-excluded"
             "скелети C# рецептів: у набір іде правило, яке скелет кодує, сам скелет лишається в носії — рішення :ad-skeletons"
             "примус: нові діагностики аналізатора, крок перевірки і скіл звірки — їх ставить GRAPH_STANDARD після каскаду"
             "розкладка документа: розділи, def-імена, публічна форма ID — робота s2"}}
```

## Числа

```clojure
{:law-numbers {:main-tags-per-archetype "рівно 1"
               :label-tags-per-archetype "0-4"
               :type-arguments-per-declaration "не більше 5 для набору колонок і для набору тегів"
               :entities-per-key-value "не більше 100 — вставка й видалення лінійні по дублях"
               :temp-allocator-lifetime "1 кадр"
               :tempjob-allocator-lifetime "4 кадри"
               :cleanup-priority "найбільше можливе ціле — прибирання йде останнім у тіку"
               :event-ripeness "1 кадр: споживач діє в кадрі після народження"
               :event-chain-cost "1 кадр на ланку"
               :pipeline-priority-band "приблизно 100-900 з кроком приблизно 100 — тепер значення простору порядку в тримачі, не локальна константа"}

 :baseline {:graph-nodes 479 :archetypes 39 :systems 71 :graph-warnings 8
            :roles-cleanup 1 :roles-per-frame 7 :roles-pipeline-stage 14 :roles-reactive 18
            :roles-startup-step 2 :roles-sub-system 26 :roles-turn-phase 3 :roles-undecided 0
            :role-markers 3 :view-subscriber-classes 4 :view-subscriptions 5 :unmarked-subscriptions 0
            :tag-label-structs 3 :tag-deviations 0 :transaction-instances 1
            :view-boundary-deviations 12 :files-in-view-folders 20 :view-nodes 19
            :local-priority-constants 0 :priority-holder-reads 69
            :legacy-event-suffix-types 1 :doc-lint-ghosts 3 :doc-lint-clojure-errors 0
            :analyzer-diagnostics "FM1001-FM1004, категорія маркерів, severity Error"}

 :inventory {:entries 457 :code 366 :definitions 43 :excluded 48
             :correction "FLOW.md # Progress називає 45 виключених; сума за видами дає 48 (17 DI, 21 процес і документ, 3 ассети, 7 застарілих) — s2 бере 48"}

 :set {:rules 243 :groups 17 :contradictions-resolved 20 :auto-decided 24 :needs-owner 3 :new-rules 5}}
```

## Покриття

```clojure
{:code-entries {:total 366 :covered-by-a-rule 366 :uncovered 0
                :method "кожен запис інвентаря з видом :code названий у :from щонайменше одного правила; дублікати з :same-as входять у :from того самого правила, що й оригінал"}
 :definitions {:total 43 :placed-in-data 43 :uncovered 0 :method ":anchors кожного запису :data"}
 :excluded {:total 48 :listed-with-reason 48}
 :contradictions {:total 20 :resolved 20}
 :s1-tally {:state-named :not-applicable :hollow-steps :not-applicable :undeclared 0 :orphan 0 :method-names 0
            :note "детектори кроків не застосовні: предмет каскаду — документ, і s1 не має :flow; :undeclared і :orphan пораховані між :governs правил і ключами :data"}}
```

## Рішення авто-режиму про самий артефакт

```clojure
{:id :ad-rule-ids-in-s1 :auto-decided true :confidence 65
 :question "CONTEXT.md :s1-must-not віддає ID правил стадії s2, а постановка стадії вимагає стабільного ID при кожному правилі"
 :chosen "s1 дає тотожність правила — namespaced keyword, що каже, ЯКЕ це правило; s2 дає публічну форму ID, на яку посилатимуться помилка аналізатора і попередження графа, але не має права злити або розділити правила цього набору"
 :options [{:option "тотожність у s1, публічна форма ID у s2" :confidence 65}
           {:option "жодних ID у s1 — лише порядкові позиції" :confidence 20}
           {:option "остаточні публічні ID уже в s1" :confidence 15}]
 :because "без тотожності набір правил не можна ні звірити з інвентарем, ні перерахувати; розкладка документа при цьому лишається недоторканою"}
```

# s2

Структура майбутньої RULES_SPECIFICATION.md: її розділи, імена def-блоків, публічна форма ID правила
і форма кожного запису. Текст документа пишеться наступною стадією як переклад цього s2 — у документі
не зʼявиться нічого, чого тут нема.

Ключі s2 з рецепта каскаду — `:in`, `:out`, `:writes`, `:scratch`, `:calls` — описують метод класу і до
документа не застосовні: у методу є вхід, вихід і мутація, у розділу документа — призначення і вміст.
Замість них тут `:does` на розділ, `:carries` — групи s1, які розділ несе, і `:rules` — скільки правил у
ньому лежить. Решта дисципліни та сама: нічого, чого не несе s1, і покриття замість data-coverage.

## Голова документа

```clojure
(def document
  {:file "RULES_SPECIFICATION.md — корінь репозиторію"
   :language "проза українською, дані — Clojure; :says кожного правила береться зі стадії s1 дослівно"
   :frontmatter {:category "C"
                 :read "trigger"
                 :trigger "перед будь-якою роботою, що створює або змінює код у Assets/, і перед зміною правила в ARCHITECTURE.md, рецепті Patterns/, скілі чи інструменті"
                 :tags "[architecture, rules, specification]"
                 :related "[ARCHITECTURE](ARCHITECTURE.md)"}
   :first-line "Єдине джерело правил коду FantasyMayor: кожне правило зі стабільним ID і тим, хто його перевіряє."
   :preamble ["що це: повний набір правил, яких має дотримуватись код; запис входить сюди тоді й лише тоді, коли його можна порушити зміною коду в Assets/"
              "напрям виведення: ARCHITECTURE.md, рецепти Patterns/, скіли, fantasymayor-graph і MarkerShapeAnalyzer виводяться ЗВІДСИ; зміна правила починається тут і лише потім іде в похідні, а носій, що каже інакше, вважається застарілим"
              "як змінити правило: :says правиться на місці, ID не чіпається; нове правило дістає новий ID за законом ID; прибране правило лишає свій ID у :retired-ids назавжди"
              "як цитувати: помилка аналізатора і попередження графа називають ID; ID шукається в цьому файлі текстом, ніколи за заголовком розділу — заголовок можна перейменувати, ID ні"
              "що чекає на власника: три читання позначені :needs-owner і зібрані окремим блоком; правило стоїть у наборі з обраним читанням, але жодне з трьох не закрите"
              "чого сьогодні не перевіряє ніщо: позначка :enforcement називає правила, чия перевірка слабша за правило або ще не збудована"
              "звідки набір: каскад Flows/RULES_SPECIFICATION — CONTEXT.md тримає інвентар носіїв, CASCADE.md # s1 — якір кожного правила на носія; у цьому файлі якорів нема"]
   :head-blocks [spec-contract id-law entry-shape parse-contract enforcers]})
```

```clojure
(def spec-contract
  {:is "повний набір правил, яких має дотримуватись код FantasyMayor"
   :criterion "правило можна порушити ЗМІНОЮ КОДУ в Assets/: існує спостережувана ситуація коду, на яку правило відповідає «так» або «ні»"
   :out "усе, що порушується зміною документа, процесу, ассета, форми DI-реєстрації або поведінки інструмента — див. розділ меж набору"
   :single-source "похідні носії ВИВОДЯТЬСЯ звідси звіркою; текст правила не дублюється на носіях"
   :change (-> (:step-1 "правити :says на місці, ID лишати")
               (:step-2 "нове правило — новий ID за (def id-law)")
               (:step-3 "прибране правило — ID у :retired-ids, ніколи не перевикористовується")
               (:step-4 "після зміни звірити похідні носії"))
   :never #{"правило без ID"
            "два правила з одним ID"
            "правило, сформульоване як намір автора, а не як ситуація коду"
            "жива назва класу як правило — назва є доказом у факті"}})
```

## Закон ID

```clojure
(def id-law
  {:form ":<prefix>/<slug> — namespaced keyword; <prefix> із реєстру нижче, <slug> — kebab-case з одного-трьох слів"
   :example :system/marker-value
   :prefix "префікс називає ПРЕДМЕТ правила — ніколи розділ і ніколи інструмент; один предмет може лежати у двох розділах, і це не помилка"
   :slug "slug каже, про що правило, словами задачі; він не повторює префікс і не називає інструмент"
   :unique "ID унікальний на весь файл"
   :mint (-> (:step-1 "узяти з реєстру префікс, чий предмет правило зачіпає")
             (:step-2 "коли жоден предмет не тримає — завести новий префікс і тим самим записом внести його в реєстр")
             (:step-3 "дати slug, якого в цьому префіксі ще нема"))
   :stable #{"перейменування розділу ID не змінює"
             "переформулювання :says ID не змінює"
             "зміна інструмента перевірки ID не змінює"
             "переміщення правила в інший розділ ID не змінює"}
   :split "коли правило ділиться, ID лишається на частині, що зберігає твердження; друга частина дістає новий ID"
   :retired-ids []
   :from-s1 "публічний ID дорівнює тотожності правила зі стадії s1 дослівно: 243 тотожності — 243 ID, жодного злиття й жодного поділу"
   :citation {:analyzer "діагностика MarkerShapeAnalyzer несе ID у тексті повідомлення у формі rule <prefix>/<slug>; номер FM лишається тотожністю діагностики і в специфікацію не пишеться"
              :graph "попередження fantasymayor-graph закінчується позначкою [rule <prefix>/<slug>]"
              :agent "знахідка ревʼю називає ID так само"
              :direction "цитує завжди точка примусу; специфікація не знає ні номерів діагностик, ні імен перевірок"
              :lookup "ID знаходять у файлі текстовим пошуком; посилання на заголовок розділу заборонене"}})
```

```clojure
(def id-prefixes
  {:system "система, її роль, маркер і порядок виконання"
   :state "стан екземпляра системи"
   :alloc "памʼять і колекції на шляху виконання"
   :table "таблиця сутностей і запит до неї"
   :tag "теги: головний, мітка, колонка замість тегу"
   :archetype "оголошення архетипу і його тримач"
   :birth "народження рядка і повнота його складу"
   :singleton "однопримірниковий стан"
   :key "ролі колонок: ідентичність, посилання, дані"
   :index "індекс над колонкою"
   :component "форма компонента"
   :write "запис колонки"
   :link "стійке посилання між рядками"
   :event "однокадрова подія"
   :reactive "reactive-форма"
   :perframe "покадрова форма"
   :orchestrator "оркестратор і його підсистеми"
   :structural "структурна зміна"
   :oneshot "разовий крок старту"
   :pipeline "стадія конвеєра створення мапи"
   :turn-phase "фаза ходу"
   :transaction "transaction-сутність"
   :view "шар view і його межа"
   :config "авторські дані та їх завантаження"
   :addressable "завантаження ассета і володіння коробкою"
   :thread "головний потік як межа сховища"
   :place "шар: домен, presentation, модуль, спільне ядро"
   :dep "посилання між збірками"
   :folder "рольова папка"
   :name "імена типів"
   :method "форма методу"
   :fail "гучна помилка і скасування"
   :async "async-робота і токен"
   :comment "коментар у коді"
   :deviation "записане відхилення"
   :choose "вибір рецепта"
   :catalogue "поліморфний каталог"
   :recipe "ознака примірника рецепта"})
```

## Форма запису правила

```clojure
(def entry-shape
  {:required {:id "стабільний ID — namespaced keyword за (def id-law)"
              :says "саме правило одним реченням, словами задачі; текст зі стадії s1 дослівно"
              :governs "ключ конструкції з (def constructs) — те, чого правило стосується"
              :checked-by "рід перевірки, якому правило піддається — ключ із (def enforcers)"}
   :optional {:kind ":choice, коли умова правила — властивість ЗАДАЧІ, а не коду (def choice-rules); відсутність читається як :check"
              :enforcement ":none-today або :weaker-today (def enforcement-marks); відсутність читається як «перевірка збудована і міряє рівно те, що каже правило»"
              :needs-owner "ID відкритого читання з (def open-readings); відсутність читається як «читання закрите»"
              :note "факт, не видний із решти ключів"
              :numbers "іменовані числа правила (def numbers-placement)"
              :folders "закритий вміст рольових папок — лише в :folder/roles"
              :signatures "ознака примірника кожного рецепта — лише в :recipe/signatures"}
   :key-order "сталий: :id :says :governs :checked-by, далі необовʼязкові в порядку цього переліку"
   :where-enforcement-lives "відповідає (def enforcers) один раз для кожного роду перевірки, а не кожен запис правила: місце перевірки — властивість інструмента, не правила"
   :not-repeated {:carrier-anchors "ID записів інвентаря (:from стадії s1) у документ не йдуть — вони вказують на текст носіїв, який похідна задача зніме, і згниють першими"
                  :contradiction-ids "ID розвʼязаних суперечностей (:x-…) лишаються в каскаді"
                  :diagnostic-numbers "номери FM не пишуться поруч із правилом — цитує точка примусу, не специфікація"
                  :baseline "виміри дня каскаду — кількість вузлів графа, систем, відхилень — не правило"
                  :skeletons "скелети C# з рецептів: у набір іде правило, яке скелет кодує"
                  :where-they-live "Flows/RULES_SPECIFICATION/CASCADE.md # s1 тримає :from кожного правила, CONTEXT.md — інвентар носіїв; обидва архівуються разом із задачею"}
   :never #{"^-метадані"
            "коментар ;; усередині блоку — «чому» живе в :note"
            "символ із квадратними дужками"
            "проза поза рядком"
            "жива назва класу як правило"}})
```

```clojure
(def enforcers
  {:analyzer {:is "MarkerShapeAnalyzer — помилка компіляції Unity, категорія FantasyMayor.Markers"
              :lives "Tools/MarkerShapeAnalyzer"
              :cites "ID у тексті повідомлення діагностики"}
   :graph {:is "fantasymayor-graph — порушення або попередження в build і check"
           :lives ".claude/skills/fantasymayor-graph"
           :invoke "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py check"
           :cites "ID у кінці рядка попередження"}
   :arch-check {:is "скіл arch-check — детектор заборони стану, керованих колекцій і часу життя алокатора"
                :lives "скіл arch-check"
                :invoke "/arch-check"
                :cites "ID у знахідці"}
   :agent {:is "агент читає код проти правила — pattern-choice, placement, asmdef_reach, ревʼю"
           :lives "скіли проєкту і ревʼю"
           :cites "ID у знахідці"}
   :none {:is "сьогодні не перевіряє ніщо"
          :lives "ніде"
          :always-with ":enforcement :none-today"}})
```

## Контракт парсера

```clojure
(def parse-contract
  {:fence "кожен блок clojure — валідні EDN-дані: або мапа, або список (def <простий-символ> <мапа-або-вектор>)"
   :rule-section "fence, чий символ починається на rules- , є розділом правил; його :rules — вектор записів"
   :entry "запис правила — мапа з ключами (def entry-shape)"
   :id-uniqueness "парсер падає на дублі ID"
   :tally "парсер перераховує правила, ID і розділи та звіряє з (def tally); розбіжність — помилка"
   :projection "з цього файлу виводяться проєкції: перелік ID для аналізатора і для графа, зріз правил за :governs, зріз за :checked-by"
   :today "канонічного парсера ще нема; сьогодні форму міряє читач Clojure у Tools/doc_lint.py, і це вужче за цей контракт"
   :never #{"^-метадані у будь-якому блоці"
            "символ із квадратними дужками"
            "проза поза рядком"
            "коментар ;; усередині блоку специфікації"}})
```

## Конструкції

```clojure
(def constructs-block
  {:def-name constructs
   :title "Конструкції, про які говорять правила"
   :does "словник адресатів: на що показує :governs кожного правила"
   :carries "усі 56 записів :data стадії s1"
   :entry-keys {:type "тип або якір мови одним символом — жодних квадратних дужок"
                :holds "що це таке, одним реченням"}
   :dropped #{":from — назва носія, звідки взято визначення"
              ":anchors — ID записів інвентаря"}
   :position "перед усіма розділами правил: кожен :governs показує сюди"
   :governed 47
   :vocabulary-only [:game-state :node-kind :node-identity :scan-roots :graph-edge :tag-order :anchor :diagnostic]
   :vocabulary-note "цих вісьмох не називає жодне :governs — вони стоять у словнику тому, що їх називають ТЕКСТИ правил: чим міряється якір, як інструмент упізнає вид вузла, що таке ребро графа, що таке діагностика; без них правила ролі й маркера не прочитати"})
```

## Розділи

```clojure
(def parts
  [{:part 1 :title "Рядок і його дані" :does "з чого складається рядок таблиці й що з ним можна робити" :sections [rules-tables rules-keys rules-components rules-events] :rules 71}
   {:part 2 :title "Системи і виконання" :does "хто виконує роботу, у якій формі й за якої каденції" :sections [rules-systems rules-system-state rules-memory rules-runtime-forms rules-one-shot rules-transaction] :rules 77}
   {:part 3 :title "Межі" :does "межі, за які код не виходить: view, авторські дані, потік, збірка" :sections [rules-view rules-configs rules-threading rules-placement] :rules 61}
   {:part 4 :title "Ремесло і вибір форми" :does "як написане окреме місце коду і яку форму взяти для нового" :sections [rules-naming rules-methods rules-form-choice] :rules 34}])
```

```clojure
(def sections
  [{:def-name rules-tables :title "Таблиці, теги, архетипи, народження" :part 1 :carries :tables-tags-archetypes :rules 34
    :does "як оголошується таблиця сутностей, закон головного й label тегів, закон архетипу і повнота народження"}
   {:def-name rules-keys :title "Ключі, індекси, ролі колонок" :part 1 :carries :keys-and-indexes :rules 14
    :does "закон ролей ключа, індекс за компонентом і межі self-index"}
   {:def-name rules-components :title "Компоненти, записи, посилання" :part 1 :carries :components-writes-links :rules 8
    :does "форма компонента, єдиний шлях запису і форма стійкого посилання"}
   {:def-name rules-events :title "Події" :part 1 :carries :events :rules 15
    :does "життєвий цикл однокадрової події: підняття, дозрівання, споживання, прибирання"}
   {:def-name rules-systems :title "Системи, ролі, маркери, порядок" :part 2 :carries :systems-and-roles :rules 18
    :does "що таке система, як вирішується її роль, коли потрібен маркер, звідки береться порядок виконання і коли система ділиться"}
   {:def-name rules-system-state :title "Стан системи" :part 2 :carries :system-state :rules 12
    :does "заборона стану екземпляра, що ним є і що ним не є, і три виходи з заборони"}
   {:def-name rules-memory :title "Колекції й памʼять" :part 2 :carries :collections-and-memory :rules 15
    :does "де керовані колекції дозволені, що таке нульове виділення на повторному шляху і як обирається native-алокатор"}
   {:def-name rules-runtime-forms :title "Reactive, per-frame, оркестратор" :part 2 :carries :reactive-perframe-orchestrator :rules 15
    :does "три форми runtime-логіки і сімейство підсистем, зібране DI"}
   {:def-name rules-one-shot :title "Разові системи, конвеєр, фаза ходу" :part 2 :carries :one-shot-pipeline-turn :rules 9
    :does "форми, що працюють раз або по стадіях, і їхні передумови"}
   {:def-name rules-transaction :title "Transaction-сутність" :part 2 :carries :transaction :rules 8
    :does "багатокрокова поведінка, що охоплює кілька піддоменів, і її єдиний дім"}
   {:def-name rules-view :title "View і межа view" :part 3 :carries :view :rules 11
    :does "що таке шар view, що йому заборонено і як він спілкується зі своєю системою"}
   {:def-name rules-configs :title "Конфіги й addressables" :part 3 :carries :configs-and-addressables :rules 26
    :does "авторські дані, їхнє завантаження і володіння дескриптором завантаженого ассета"}
   {:def-name rules-threading :title "Потік і структурні зміни" :part 3 :carries :threading-and-structural :rules 8
    :does "головний потік як межа сховища і безпечна ідіома структурної зміни"}
   {:def-name rules-placement :title "Розміщення: шари, залежності, папки" :part 3 :carries :placement :rules 16
    :does "де живе новий код, на що йому можна посилатися і яку роль несе його папка"}
   {:def-name rules-naming :title "Іменування" :part 4 :carries :naming :rules 4
    :does "суфікс ролі, самодостатність імені типу і межа доменного префікса"}
   {:def-name rules-methods :title "Методи, гучна помилка, коментарі" :part 4 :carries :methods-fail-loud-comments :rules 16
    :does "як метод віддає результат, як код падає на відсутній передумові і де стоїть коментар"}
   {:def-name rules-form-choice :title "Вибір форми, каталог, ознаки рецептів" :part 4 :carries :form-choice-and-signatures :rules 14
    :does "яку форму набуває нова частина коду і за якою ознакою її примірник упізнається"}])
```

```clojure
(def section-shape
  {:heading "## <:title> — один заголовок другого рівня на розділ; частина дає заголовок того ж рівня з назвою частини"
   :prose "рівно один абзац перед блоком: переклад :does і нічого більше — жодного правила в прозі"
   :block "(def <:def-name> {:section <ключ :carries> :does \"<:does>\" :rules [ … ]})"
   :order "розділи йдуть у порядку (def sections); порядок читання не дорівнює порядку стадії s1 і ID від нього не залежать"
   :why-prose "документ у 243 правила ніхто не читає підряд; абзац на розділ дає картину, дані під ним — відповідь"})
```

## Позначки

```clojure
(def choice-rules
  {:mark ":kind :choice"
   :criterion "умова правила — властивість ЗАДАЧІ (що код має робити), а не властивість коду; тому жоден інструмент не вирішує його з коду, і порушення видно як хибно обрану форму, а не як хибний рядок"
   :default ":check — умова правила є властивістю коду; відсутність :kind читається саме так"
   :why "правило, яке ніколи не спрацьовує, не відрізнити від мертвого; позначка каже читачеві, що це правило читають ПЕРЕД тим, як писати код"
   :ids [:system/split-when :system/split-into :system/simplest-structure :system/base-choice :system/driven-by
         :table/index-only-hot :table/cross-archetype
         :event/reaction :event/dormant-consumer :event/startup-bulk
         :reactive/default :perframe/only-when :perframe/late-update :orchestrator/when
         :transaction/when :transaction/degenerate
         :view/ecs-only-across-a-boundary
         :config/validate
         :pipeline/re-entry
         :method/inline-helper
         :choose/recipe-tree :choose/data-recipe :choose/config-recipe :choose/behavior-recipe
         :catalogue/when]
   :count 25
   :rest 218})
```

```clojure
(def enforcement-marks
  {:values {:none-today "сьогодні це правило не перевіряє ніщо"
            :weaker-today "названий рід перевірки міряє менше, ніж каже правило"}
   :absent "перевірка збудована і міряє рівно те, що каже правило"
   :why "правило без збудованого примусу лишається правилом; позначка каже це вголос, замість тихо вдавати перевірку"
   :marked [{:rule :system/marker-vocabulary-parity :enforcement :none-today}
            {:rule :state/mutable-static :enforcement :none-today :note "детектор не позначає жодного static-поля — прогалина інструмента, не дозвіл"}
            {:rule :state/allowed-reason :enforcement :none-today}
            {:rule :system/marker-required :enforcement :weaker-today :note "граф сьогодні лише попереджає; правило вимагає помилки компіляції"}
            {:rule :system/marker-forbidden :enforcement :weaker-today :note "зайвий маркер сьогодні лише попередження; за рішенням власника це помилка"}
            {:rule :system/marker-value :enforcement :weaker-today :note "аналізатор міряє значення маркера; читач ролей у графі дозволяє на табличному якорі будь-яке значення"}
            {:rule :system/priority-source :enforcement :weaker-today :note "інструмент приймає будь-яку розвʼязну const int, отже перевіряє слабше за правило"}
            {:rule :view/boundary :enforcement :weaker-today :note "правило без помилки компіляції; граф називає відхилення"}]
   :count 8})
```

```clojure
(def notes-carried
  {:verbatim [:system/subsystem-is-not-a-system :alloc/managed-element-exception :name/prefix-exception :catalogue/checklist]
   :rewritten [:system/marker-required :system/marker-forbidden :system/priority-source :view/boundary :state/mutable-static]
   :rewrite-rule "переписані нотатки зберігають факт і втрачають дві речі, які гниють: назву майбутньої задачі й вимір дня каскаду (кількість живих відхилень)"
   :new [:system/marker-value]
   :new-source "факт узятий із # Contra стадії s1, запис :c-6 — розбіжність читача ролей із правилом"})
```

## Відкриті читання і межі набору

```clojure
(def open-readings-block
  {:def-name open-readings
   :title "Читання, що чекають на власника"
   :does "три місця, де набір стоїть на обраному читанні, а не на слові власника"
   :in-place "кожне з трьох правил стоїть у своєму розділі з ключем :needs-owner — набір лишається повним"
   :entry-keys {:id "ID читання"
                :rule "ID правила, яке на ньому стоїть"
                :chosen "обране читання і його оцінка"
                :under-yes "що стоїть у наборі, коли власник підтверджує"
                :under-no "що змінюється в наборі, коли власник відмовляє"
                :why-open "чому рішення не вважається закритим"}
   :entries [{:id :oq-stateless-vs-lifetime-flags :rule :state/lifetime-flags :chosen-confidence 65}
             {:id :oq-singleton-manifest :rule :singleton/manifest :chosen-confidence 60}
             {:id :oq-view-in-views-folder :rule :view/layer :chosen-confidence 60}]
   :count 3
   :never "тихо закрити читання, вписавши обране як остаточне"})
```

```clojure
(def out-of-scope-block
  {:def-name out-of-scope
   :title "Що свідомо не є правилом коду"
   :does "межа набору: чому запис носія міг не стати правилом"
   :criterion "запис входить у набір тоді й лише тоді, коли його можна порушити зміною коду в Assets/"
   :reasons ["форма реєстрації DI під рефакторингом: виклик реєстрації, час життя, ціль інтерфейсу, передані параметри, клас і розміщення інсталера, порядок установки модулів, ручне вписування систем у стан гри — правила про типи, константи й конструктори, які реєстрація лише споживає, лишаються в наборі"
             "процес агента, правило документа, нотація і поведінка інструмента — не правило коду; евристики впізнавання рецептів лишаються в коді інструмента"
             "розміщення ассетів, а не коду"
             "жива назва, історія або застарілий текст носія — доказ у факті, ніколи правило"
             "правила розповіді коду — вони живуть у каскаді"
             "скелет C# з рецепта: у набір іде правило, яке скелет кодує, сам скелет лишається в носії"
             "примус: нові діагностики, крок перевірки і скіл звірки — їх ставить окрема задача"]
   :dropped "інвентарні ID виключених записів (48 штук) у документ не йдуть — вони вказують на інвентар каскаду, а не на код"})
```

```clojure
(def numbers-placement
  {:law "числа закону лягають ключем :numbers на те правило, яке їх називає; окремого блоку чисел у документі нема — число має рівно один дім"
   :places [{:rule :tag/one-main-tag :numbers {:main-tags-per-archetype "рівно 1"}}
            {:rule :tag/label-count :numbers {:label-tags-per-archetype "0-4"}}
            {:rule :archetype/arity-cap :numbers {:type-arguments-per-declaration "не більше 5 для набору колонок і для набору тегів"}}
            {:rule :index/bucket-cap :numbers {:entities-per-key-value "не більше 100"}}
            {:rule :alloc/by-lifetime :numbers {:temp-allocator-lifetime "1 кадр" :tempjob-allocator-lifetime "4 кадри"}}
            {:rule :event/cleanup :numbers {:cleanup-priority "найбільше можливе ціле"}}
            {:rule :event/ripe :numbers {:event-ripeness "1 кадр: споживач діє в кадрі після народження"}}
            {:rule :event/no-same-frame :numbers {:event-chain-cost "1 кадр на ланку"}}
            {:rule :pipeline/order :numbers {:pipeline-priority-band "приблизно 100-900 з кроком приблизно 100"}}]
   :numbers 10
   :rules 9
   :not-carried "базова лінія стадії s1 — кількість вузлів графа, систем, ролей, відхилень — вимір дня каскаду, а не правило; у документ не йде"})
```

```clojure
(def tally-block
  {:def-name tally
   :title "Звірка"
   :does "числа, які парсер перераховує з самого файлу"
   :rules 244 :sections 17 :parts 4 :constructs 56 :prefixes 38
   :choice-rules 25 :enforcement-marks 8 :open-readings 3
   :checked-by {:agent 152 :graph 58 :arch-check 26 :analyzer 4 :none 3}
   :law "розбіжність між цим блоком і перерахунком — помилка файлу, не розбіжність тексту"})
```

## Рішення авто-режиму

```clojure
[{:id :ad-public-id-form :auto-decided true :confidence 70
  :chosen "публічний ID — тотожність стадії s1 дослівно, namespaced keyword; числового псевдоніма нема, номер FM лишається тотожністю діагностики і цитує ID, а не навпаки"
  :options [{:option "тотожність s1 дослівно, без псевдоніма" :confidence 70}
            {:option "тотожність плюс числовий псевдонім для діагностик" :confidence 20}
            {:option "нові ID і таблиця відповідності" :confidence 10}]
  :because "будь-яка інша форма створює другу тотожність, яку треба тримати чесною; ID, що дорівнює тотожності, не може розійтися сам із собою"
  :answers :c-1}

 {:id :ad-no-carrier-anchors :auto-decided true :confidence 65
  :chosen "документ не повторює ні :from правил, ні :anchors конструкцій, ні ID суперечностей: провенанс названо один раз у преамбулі й він лежить у каскаді"
  :options [{:option "не повторювати, провенанс у преамбулі" :confidence 65}
            {:option "лишити :from у кожному правилі" :confidence 35}]
  :because "якорі вказують на текст носіїв, який наступна задача зніме, і на інвентар, який архівується; 243 покажчики на майбутнє минуле згниють першими"}

 {:id :ad-checked-by :auto-decided true :confidence 70
  :chosen ":enforced-by стає :checked-by — рід перевірки, якому правило піддається; ДЕ живе перевірка, каже (def enforcers) один раз; сьогоднішній стан примусу несе окремий ключ :enforcement"
  :options [{:option ":checked-by плюс :enforcement, місце — у легенді" :confidence 70}
            {:option "лишити :enforced-by як факт про сьогодні" :confidence 30}]
  :because "інструмент змінюється частіше за правило; розділивши рід перевірки і її сьогоднішній стан, гниє лише другий, і він позначений"
  :answers :c-3}

 {:id :ad-choice-kind :auto-decided true :confidence 60
  :chosen "25 правил несуть :kind :choice за критерієм «умова — властивість задачі»; решта 218 читаються як :check"
  :options [{:option "позначити 25 правил вибору" :confidence 60}
            {:option "лишити як є — :checked-by :agent уже їх називає" :confidence 30}
            {:option "вивести правила вибору в окремий документ" :confidence 10}]
  :because ":agent стоїть на 152 правилах, з яких більшість порушується рядком коду; без позначки правило вибору не відрізнити від невиконуваного"
  :answers :c-5
  :risk "класифікація — судження цієї стадії; read-back переганяє критерій по кожному з 25"}

 {:id :ad-sections-one-to-one :auto-decided true :confidence 70
  :chosen "17 розділів документа — один до одного з групами стадії s1, перевпорядковані у 4 частини читання"
  :options [{:option "17 розділів 1:1, порядок читання новий" :confidence 70}
            {:option "злити в 8-10 більших розділів" :confidence 30}]
  :because "1:1 робить покриття механічним (група = розділ, суми збігаються); порядок читання вільний саме тому, що ID не залежить від місця"}

 {:id :ad-prose-per-section :auto-decided true :confidence 60
  :chosen "один абзац прози перед кожним розділом — переклад :does; частина дістає свій абзац"
  :options [{:option "абзац на розділ" :confidence 60}
            {:option "чисті дані без прози" :confidence 25}
            {:option "розбити на кілька файлів" :confidence 15}]
  :because "перший контакт агента з набором має давати картину; абзац гниє повільніше за правило, бо не несе жодного"
  :answers :c-8}

 {:id :ad-read-trigger :auto-decided true :confidence 70
  :chosen "frontmatter: read trigger — перед роботою, що створює або змінює код, і перед зміною правила в похідному носії"
  :options [{:option "read trigger" :confidence 70} {:option "read always" :confidence 30}]
  :because "always клав би 243 правила в кожну сесію, зокрема в ті, що коду не чіпають"}

 {:id :ad-numbers-on-rules :auto-decided true :confidence 70
  :chosen "десять чисел закону стають ключем :numbers на девʼяти правилах; окремого блоку чисел і базової лінії в документі нема"
  :options [{:option ":numbers на правилі" :confidence 70}
            {:option "окремий блок чисел з покажчиком на правило" :confidence 30}]
  :because "окремий блок дав би те саме число у двох місцях одного файлу — рівно та хвороба, від якої задача починалася"}

 {:id :ad-no-comments-in-fences :auto-decided true :confidence 65
  :chosen "жодного коментаря ;; усередині блоків специфікації — «чому» живе в :note"
  :options [{:option "без коментарів, чому в :note" :confidence 65}
            {:option "коментарі дозволені, парсер їх відкидає" :confidence 35}]
  :because "власник вимагав прози лише в рядках; коментар — проза поза рядком, і жодна проєкція його не побачить"}

 {:id :ad-constructs-kept-whole :auto-decided true :confidence 65
  :chosen "словник несе всі 55 конструкцій, включно з вісьмома, яких не називає жоден :governs"
  :options [{:option "усі 55, вісім позначені як словникові" :confidence 65}
            {:option "лишити 47, на які показує :governs" :confidence 35}]
  :because "тексти правил ролі, маркера і межі view називають якір, вид вузла і діагностику словами — без цих визначень правило не прочитати"}

 {:id :ad-tally-block :auto-decided true :confidence 60
  :chosen "документ закінчується блоком звірки, який парсер перераховує з файлу"
  :options [{:option "блок звірки, що перераховується" :confidence 60}
            {:option "без чисел про сам файл" :confidence 40}]
  :because "число, яке перераховується, не гниє: розбіжність одразу стає помилкою, а не застарілим рядком"}

 {:id :ad-citation-one-way :auto-decided true :confidence 70
  :chosen "цитує точка примусу: діагностика й попередження називають ID; специфікація не знає номерів діагностик і не має заголовкових якорів"
  :options [{:option "цитування в один бік" :confidence 70}
            {:option "правило несе номер своєї діагностики" :confidence 30}]
  :because "двобічне посилання довелося б тримати чесним з обох кінців; односторонні посилання ламає лише перейменування ID, яке заборонене"}

 {:id :ad-open-readings-visible :auto-decided true :confidence 70
  :chosen "три відкриті читання лишаються на своїх правилах ключем :needs-owner і зібрані окремим блоком з :under-yes і :under-no"
  :options [{:option "позначка на правилі плюс окремий блок" :confidence 70}
            {:option "прибрати три правила до відповіді власника" :confidence 30}]
  :because "набір має лишитися повним і водночас не вдавати, ніби власник уже відповів"
  :answers :c-4}]
```

## Що s2 робить із # Contra стадії s1

```clojure
[{:entry :c-1 :takes :fix-a :adapted "тотожність стає публічним ID, але без числового псевдоніма: номер FM лишається тотожністю діагностики" :in :ad-public-id-form}
 {:entry :c-2 :takes :fix-b :confidence 70
  :why "fix-a заборонений рішенням :ad-rule-ids-in-s1 — s2 не має права ділити правила набору"
  :mitigation "точка примусу цитує ID; два інструменти, що перевіряють різні частини одного правила, називають один ID — це очікувано, а не дефект"
  :options [{:option "лишити як є, поділ заборонено" :confidence 70}
            {:option "ділити правила на під-пункти" :confidence 20}
            {:option "повернути питання в s1" :confidence 10}]}
 {:entry :c-3 :takes :fix-a :in :ad-checked-by}
 {:entry :c-4 :takes :fix-a :in :ad-open-readings-visible}
 {:entry :c-5 :takes :fix-a :adapted "критерій звужено до «умова — властивість задачі», список із 25 ID виписано явно" :in :ad-choice-kind}
 {:entry :c-6 :takes :fix-a :adapted "правило лишається; розбіжність читача ролей із ним позначена на :system/marker-value як :enforcement :weaker-today" :in :enforcement-marks}
 {:entry :c-7 :takes :fix-a :adapted "преамбула документа називає напрям виведення: носії виводяться звідси, і той, що каже інакше, застарілий"}
 {:entry :c-8 :takes :fix-a :in :ad-prose-per-section}]
```

## Покриття

```clojure
{:rules {:in-s1 243 :placed 243 :lost 0 :invented 0
         :method "групи стадії s1 і розділи документа співвідносяться один до одного; сума :rules по (def sections) дорівнює 243"
         :by-part {1 71 2 77 3 61 4 34}
         :by-section {:rules-tables 34 :rules-keys 14 :rules-components 8 :rules-events 15
                      :rules-systems 18 :rules-system-state 12 :rules-memory 15 :rules-runtime-forms 15
                      :rules-one-shot 9 :rules-transaction 8
                      :rules-view 11 :rules-configs 26 :rules-threading 8 :rules-placement 16
                      :rules-naming 4 :rules-methods 16 :rules-form-choice 14}
         :exactly-one-section "жоден ID не стоїть у двох розділах: розділ бере групу цілком"}
 :ids {:minted 0 :equal-to-s1 243 :duplicates 0 :prefixes 38
       :note "s2 не завело жодного нового ID і не прибрало жодного"}
 :constructs {:in-s1 56 :placed 56 :governed 47 :vocabulary-only 9
              :correction "покриття стадії s1 називає orphan 0; за ключем :governs вісім конструкцій не названі жодним правилом — вони стоять у словнику як мова правил, і це записано явно в (def constructs-block)"}
 :marks {:choice 25 :enforcement 8 :needs-owner 3 :numbers 9 :notes 10}
 :s1-blocks-carried {:data constructs :rules sections :open-questions open-readings :exits out-of-scope :law-numbers numbers-placement}
 :s1-blocks-not-carried {:contradictions "розвʼязані суперечності лишаються в каскаді — у документі стоїть тільки їхній результат, тобто саме правило"
                         :baseline "виміри дня каскаду"
                         :inventory-ids "ID інвентаря носіїв"
                         :s1-tally "числа про сам каскад"}
 :invented-by-s2 {:rules 0 :constructs 0
                  :structure #{"розділи й порядок читання" "публічна форма ID і реєстр префіксів" "ключі запису" "позначки :kind :enforcement :needs-owner" "контракт парсера" "блок звірки"}
                  :law "структура — робота s2; жодного нового правила, жодної нової конструкції, жодного нового числа"}}
```

## Рішення стадії коду

Що стадія «код» виявила і вирішила під час перекладу s2 у RULES_SPECIFICATION.md. Кожен запис — :from-code.

```clojure
[{:id :fc-marks-not-enumerated :from-code true :auto-decided true :confidence 70
  :chosen "легенди (def choice-rules), (def enforcement-marks) і (def numbers-placement) стоять у документі без переліків :ids, :marked і :places: позначка живе на правилі, легенда каже, як її читати"
  :options [{:option "легенда без переліку, позначка на правилі" :confidence 70}
            {:option "легенда з повним переліком ID" :confidence 30}]
  :because "перелік дав би кожній із 25+8+10 позначок другий дім у тому самому файлі — рівно та хвороба, від якої задача починалася; перерахунок лишається в (def tally)"
  :discovered "s2 виписував переліки як інструкцію стадії коду, кому ставити позначку; у самому документі вони стають дублем"}

 {:id :fc-notes-carried-is-not-content :from-code true :auto-decided true :confidence 75
  :chosen "(def notes-carried) у документ не йде: це провенанс s2 — які нотатки взяті дослівно, які переписані, яка нова"
  :options [{:option "не переносити — провенанс належить каскаду" :confidence 75}
            {:option "перенести як блок про нотатки" :confidence 25}]
  :because "запис каже не про код, а про роботу стадії; entry-shape :not-repeated уже забороняє провенанс у документі"}

 {:id :fc-id-law-without-s1-pointer :from-code true :auto-decided true :confidence 65
  :chosen ":from-s1 з (def id-law) у документ не йде, а :citation :analyzer каже «номер діагностики лишається її тотожністю» без літер FM"
  :options [{:option "прибрати покажчик на s1 і номер FM" :confidence 65}
            {:option "лишити обидва як факт про походження" :confidence 35}]
  :because "s1 архівується разом із задачею, а номер діагностики документ не знає за рішенням :ad-citation-one-way; звідки набір, каже преамбула"}

 {:id :fc-open-readings-lose-the-day-count :from-code true :auto-decided true :confidence 70
  :chosen ":under-yes і :under-no трьох відкритих читань зберігають наслідок і втрачають кількість живих відхилень дня каскаду"
  :options [{:option "наслідок без числа відхилень" :confidence 70}
            {:option "лишити числа як вимір дня" :confidence 30}]
  :because "правило :rewrite-rule з (def notes-carried) знімає вимір дня з нотаток; та сама причина тримає для відкритих читань, бо число застаріє першим"
  :discovered "s2 задав :entry-keys відкритих читань, але не сказав, що робити з вимірами дня в тексті s1"}]
```

# Contra

Чому цей набір правил не спрацює, і що з цим робити. Предмет — артефакт s1, не носії й не майбутній документ.

```clojure
[{:id :c-1
  :kills "звʼязок специфікації з примусом"
  :case "s1 дає правилу тотожність :system/marker-value, s2 дає публічний ID, аналізатор посилається на публічний; при першому ж перейменуванні тотожності два ID розходяться"
  :fails "стабільність ID, яку власник підтвердив, тримається на обіцянці s2, а не на чомусь перевірюваному"
  :fix [{:id :fix-a :confidence 60 :is "s2 бере тотожність s1 як канонічний ID і додає лише числовий псевдонім для діагностики" :cost "публічні ID стають довгими"}
        {:id :fix-b :confidence 40 :is "s2 будує таблицю відповідності тотожність-ID і робить її частиною документа" :cost "ще одна таблиця, яку треба тримати чесною"}]}

 {:id :c-2
  :kills "повноту набору"
  :case "366 записів інвентаря згорнуті в 243 правила; наприклад :view/subscriber-marker зʼїдає чотири записи — маркер, підписку, попередження інструмента і діагностику FM1003"
  :fails "злиття могло проковтнути розрізнення, яке потрібне саме точці примусу: аналізатор і граф перевіряють РІЗНІ частини цього правила"
  :fix [{:id :fix-a :confidence 55 :is "s2 розкладає кожне правило з кількома :from на під-пункти з власними ID саме там, де точки примусу різні" :cost "набір росте, частина під-пунктів ніколи не спрацює"}
        {:id :fix-b :confidence 30 :is "лишити як є: :from тримає всі якорі, і звірка з носієм усе одно йде через них" :cost "діагностика не зможе вказати на під-правило"}
        {:id :fix-c :confidence 15 :is "розділити зараз, у s1" :cost "s1 роздувається до майже інвентаря, і сенс згортання зникає"}]}

 {:id :c-3
  :kills "ключ :enforced-by"
  :case ":system/marker-forbidden сьогодні :graph, а рішення власника вже вимагає помилки компіляції — GRAPH_STANDARD додасть її одразу після каскаду"
  :fails "поле описує сьогоднішній стан інструментів і згниє першим, тобто саме те, від чого власник тікав"
  :fix [{:id :fix-a :confidence 65 :is "s2 замінює ключ на «хто МОЖЕ це перевірити» — властивість правила, а не інструмента" :cost "втрачається карта покриття примусом"}
        {:id :fix-b :confidence 35 :is "лишити як факт про сьогодні і перевіряти при кожній звірці" :cost "ще одна річ, яка гниє"}]}

 {:id :c-4
  :kills "три правила набору"
  :case "якщо власник вирішить, що сторож підписки не є станом, :state/lifetime-flags зникає, а :state/is-state дістає третій пункт переліку не-стану"
  :fails "набір поданий як повний, хоча три його місця стоять на обраному читанні, а не на слові власника"
  :fix [{:id :fix-a :confidence 70 :is "s2 тримає ці три правила окремим блоком, який власник закриває одним рішенням" :cost "документ має явно тимчасовий розділ"}
        {:id :fix-b :confidence 30 :is "прибрати їх із набору до відповіді власника" :cost "набір перестає бути повним, а живий код лишається без правила"}]}

 {:id :c-5
  :kills "критерій"
  :case "група вибору форми — :choose/behavior-recipe, :system/split-when — проходить критерій, бо вирішує, якої форми набуде код, але жодна ситуація коду не робить її хибною"
  :fails "правило, яке ніколи не спрацьовує, не відрізнити від мертвого — саме те, про що попереджає знахідка :silent-rule-risk"
  :fix [{:id :fix-a :confidence 55 :is "s2 позначає такі правила окремим видом — правило вибору, а не правило перевірки" :cost "два класи правил у одному документі"}
        {:id :fix-b :confidence 30 :is "лишити разом: :enforced-by :agent уже їх називає" :cost "читач вважатиме їх перевірюваними"}
        {:id :fix-c :confidence 15 :is "вивести правила вибору зі специфікації" :cost "суперечить :ad-selection-rules і лишає дерево рецептів без дому"}]}

 {:id :c-6
  :kills "розвʼязання :x-role-marker"
  :case "правило каже, що клас із табличним якорем може нести лише значення PerFrame; roles.py сьогодні дозволяє там будь-яке"
  :fails "розвʼязок вимагає зміни коду інструмента, а стадія s1 не має права його чіпати — специфікація виявиться правдою про майбутнє"
  :fix [{:id :fix-a :confidence 70 :is "правило лишається, а зміна інструмента йде в задачу GRAPH_STANDARD разом із примусом" :cost "до того граф і специфікація розходяться в одному місці"}
        {:id :fix-b :confidence 30 :is "записати поточну поведінку інструмента як правило" :cost "специфікація закріплює те, що аналізатор називає помилкою"}]}

 {:id :c-7
  :kills "єдиність джерела"
  :case "правило :tag/one-main-tag живе тепер і в наборі, і дослівно в законі тегів носія, і в аудиті інструмента"
  :fails "поки GRAPH_STANDARD не зніме текст з носіїв, джерел стає три, а не одне — рівно та хвороба, від якої задача починалася"
  :fix [{:id :fix-a :confidence 75 :is "нічого не робити в каскаді: зняття тексту з носіїв — предмет наступної задачі, і воно вже заплановане" :cost "проміжний стан із трьома джерелами живе стільки, скільки живе GRAPH_STANDARD"}
        {:id :fix-b :confidence 25 :is "правити носії тут" :cost "прямо заборонено межами задачі"}]}

 {:id :c-8
  :kills "читабельність"
  :case "243 правила плюс 48 виключених плюс 43 конструкції в даних — документ, який ніхто не прочитає підряд"
  :fails "носій, який читають лише пошуком, не дає агентові картини, а саме по неї він приходить"
  :fix [{:id :fix-a :confidence 60 :is "s2 дає кожній групі один абзац прози перед даними — картина зверху, дані знизу" :cost "проза теж гниє, хоч і повільніше"}
        {:id :fix-b :confidence 25 :is "лишити чисті дані: документ і не призначений для читання підряд" :cost "перший контакт агента з набором стає дорожчим"}
        {:id :fix-c :confidence 15 :is "розбити на кілька файлів" :cost "суперечить рішенню про одну специфікацію в одному файлі"}]}]
```

```clojure
{:gate-after-s1 {:subject "алгоритм: чи це той набір правил, ті конструкції і ті межі"
                 :owner-verdict :waived-by-owner
                 :auto "стадія розвʼязала кожну суперечність найвище оціненим варіантом; усе спірне зібрано в # Contra, у блоці розвʼязань і в блоці відкритих питань"}}
```

## Contra s2

Чому не спрацює сама СТРУКТУРА документа. Предмет — артефакт s2, не набір правил і не носії.

```clojure
[{:id :s2-c-1
  :kills "чесність позначки :kind"
  :case "правило :view/ecs-only-across-a-boundary позначене :choice, бо «доречність» — судження; але частину умови — межу збірки — граф цілком здатен виміряти"
  :fails "поділ на вибір і перевірку зробила ця стадія власним судженням по 243 записах; контестабельна класифікація стає постійним ключем у джерелі"
  :fix [{:id :fix-a :confidence 60 :is "критерій виписано в блоці, список із 25 ID явний; read-back переганяє критерій по кожному з них і по вибірці :check-правил" :cost "стадія read-back дорожчає на один прохід"}
        {:id :fix-b :confidence 25 :is "прибрати :kind і лишити :checked-by :agent" :cost "правило вибору знову не відрізнити від невиконуваного — те, про що попереджає знахідка про мертве правило"}
        {:id :fix-c :confidence 15 :is "питати власника про кожне спірне" :cost "25 питань там, де власник чекає підсумку"}]}

 {:id :s2-c-2
  :kills "простежуваність до носіїв"
  :case "GRAPH_STANDARD звіряє ARCHITECTURE.md і рецепти проти набору; щоб знати, ЯКИЙ абзац носія відповідає правилу :tag/one-main-tag, він мусить відкрити CASCADE.md # s1 цієї задачі"
  :fails "після архівації задачі звʼязок правило-носій живе в одному файлі, який жодне правило не називає; один git mv — і слід тримається лише на памʼяті преамбули"
  :fix [{:id :fix-a :confidence 60 :is "преамбула називає каскад як провенанс, а звірку носіїв робить задача, поки каскад ще не архівований" :cost "вікно між каскадом і звіркою стає обовʼязковим"}
        {:id :fix-b :confidence 30 :is "лишити :from у кожному правилі" :cost "243 покажчики на текст, який ця ж програма робіт зніме — вони стануть хибними раніше за все інше"}
        {:id :fix-c :confidence 10 :is "винести таблицю правило-носій окремим файлом поруч" :cost "другий документ, який треба тримати чесним — проти рішення про одне джерело"}]}

 {:id :s2-c-3
  :kills "контракт парсера"
  :case ":projection обіцяє перелік ID для аналізатора і зріз за :governs, але канонічного парсера ще нема; сьогодні форму міряє лише читач Clojure у Tools/doc_lint.py"
  :fails "документ, оголошений даними для машини, до появи парсера перевіряється як текст — і перше порушення форми знайде людина, а не інструмент"
  :fix [{:id :fix-a :confidence 65 :is "контракт записаний як вимога до файлу, а ключ :today чесно каже, що міряє лише doc_lint; парсер ставить наступна задача" :cost "проміжний стан, у якому обіцянка більша за перевірку"}
        {:id :fix-b :confidence 20 :is "прибрати контракт парсера з документа" :cost "форма блоків стає смаком автора, і перша ж проза поза рядком ламає майбутню проєкцію"}
        {:id :fix-c :confidence 15 :is "написати парсер у цьому каскаді" :cost "прямо поза межами задачі — предмет каскаду документ, не інструмент"}]}

 {:id :s2-c-4
  :kills "звірку документа зі стадією s1"
  :case "розділи документа йдуть у новому порядку читання — таблиці перед системами, — а стадія s1 починається з систем"
  :fails "converge, який порівнює за позицією, побачить розбіжність там, де її нема"
  :fix [{:id :fix-a :confidence 70 :is "звірка йде за ключем :carries розділу і за ID правила, ніколи за порядком" :cost "правило звірки треба назвати вголос, інакше наступна стадія зробить по-своєму"}
        {:id :fix-b :confidence 30 :is "лишити порядок стадії s1" :cost "документ відкривається найскладнішою групою — 18 правил про роль системи — перед тим, як сказати, що таке рядок таблиці"}]}

 {:id :s2-c-5
  :kills "ключ :enforcement"
  :case "вісім позначок описують сьогоднішній стан інструментів; щойно наступна задача поставить помилку компіляції на маркер, три з них стають хибними"
  :fails "це рівно те гниття, від якого тікав ключ :enforced-by — воно лише звузилося з 243 місць до восьми"
  :fix [{:id :fix-a :confidence 60 :is "значень рівно два, кожне — факт про правило, і обидва перевіряються при кожній звірці носіїв" :cost "звірка дістає ще один обовʼязковий пункт"}
        {:id :fix-b :confidence 25 :is "прибрати ключ: правило стоїть саме за себе" :cost "постановка вимагає позначати незбудований примус, і агент вважатиме перевіреним те, чого ніхто не перевіряє"}
        {:id :fix-c :confidence 15 :is "тримати стан примусу окремим файлом" :cost "другий документ поруч із єдиним джерелом"}]}

 {:id :s2-c-6
  :kills "ціну читання"
  :case "агент, що бере тригер перед зміною одного рядка в системі, дістає всі 243 правила, 55 конструкцій і чотири частини"
  :fails "єдине джерело коштує як ціле навіть тоді, коли потрібен один розділ; дешевого зрізу до появи парсера нема"
  :fix [{:id :fix-a :confidence 55 :is "тригер звужує привід, частини й заголовки дають дешеву навігацію всередині файлу" :cost "ціна лишається, просто платиться рідше"}
        {:id :fix-b :confidence 25 :is "розбити на чотири файли за частинами" :cost "проти рішення про одну специфікацію в одному файлі"}
        {:id :fix-c :confidence 20 :is "покласти на початок покажчик ID за розділами" :cost "243 ID у двох місцях одного файлу"}]}

 {:id :s2-c-7
  :kills "обіцянку «джерело, з якого виводять»"
  :case ":says кожного правила — українська проза в рядку; проєкція винесе ID, :governs, :checked-by і числа, але САМЕ правило лишиться текстом"
  :fails "інструмент не зможе вивести перевірку з правила — він і далі кодуватиме її руками, а специфікація лишиться контрактом для агента, а не для машини"
  :fix [{:id :fix-a :confidence 70 :is "прийняти межу вголос: машині йде тотожність, адресат, рід перевірки і числа; правило лишається для агента, а звірка «інструмент проти правила» — робота агента, не парсера" :cost "обіцянка автоматичного виведення звужується до звірки"}
        {:id :fix-b :confidence 20 :is "додати кожному правилу формальний предикат" :cost "s1 не несе жодного — це був би винахід на 243 записи"}
        {:id :fix-c :confidence 10 :is "лишити обіцянку як є" :cost "перша ж спроба згенерувати перевірку з файлу покаже, що її там нема"}]}]
```

```clojure
{:gate-after-s2 {:subject "структура документа: розділи, форма ID, форма запису, межі того, що документ несе"
                 :owner-verdict :waived-by-owner
                 :auto "кожен спірний вибір записаний у блоці рішень авто-режиму з оцінками; вісім записів # Contra стадії s1 розвʼязані явно, сім власних — у блоці вище"}}
```

# Read-back

Стадія прочитала RULES_SPECIFICATION.md цілком як чужий файл, не знаючи ні s1, ні s2, і лише потім
звірила прочитане з s2 і з рангами правил s1. Кожна знахідка має вердикт: `:fixed` — виправлено в
документі, `:kept-because` — лишено з названою причиною. Правил стадія не переписувала: що саме каже
правило — слово власника.

```clojure
[{:id :rb-1 :what "число `:constructs 55` у (def tally) не сходиться з файлом: словник несе 56 записів"
  :where "розділ «Звірка», (def tally)"
  :how-found "перерахунок ключів `{:type` у (def constructs): перший запис :stack стоїть на рядку відкриття блоку, і лічильник s2 його не побачив — та сама помилка пройшла в (def constructs-block) :carries «усі 55 записів» і в # Покриття :constructs {:in-s1 55}"
  :verdict :fixed
  :fix "(def tally) :constructs 55 → 56"}

 {:id :rb-2 :what "проза перед словником каже «Вісім записів не називає жоден :governs» — насправді девʼять"
  :where "розділ «Конструкції, про які говорять правила», абзац перед (def constructs)"
  :how-found "47 конструкцій названі якимось :governs, 56 у словнику; неназваних девʼять — вісім із :vocabulary-only s2 плюс :stack"
  :verdict :fixed
  :fix "«Вісім записів» → «Девʼять записів»; перелік того, навіщо вони стоять у словнику, дістав «на якому стеку стоїть код» — рівно факт, який несе :stack"}

 {:id :rb-3 :what "правила про одну конструкцію розкидані по розділах: :governs :system живе в шести розділах, :component у пʼяти, :subsystem, :view і :archetype — у чотирьох; десять із 47 названих конструкцій мають правила щонайменше в трьох розділах"
  :where "усі чотири частини; найгірший випадок — :system (rules-systems, rules-system-state, rules-memory, rules-runtime-forms, rules-methods, rules-form-choice)"
  :reading "чужак, який прийшов по «правила про систему», порядком читання їх не збере — лише текстовим пошуком за :governs; обіцяний зріз за :governs (parse-contract :projection) сьогодні не існує, і сам документ це визнає ключем :today"
  :kept-because "поділ на розділи — рішення :ad-sections-one-to-one (17 груп s1 один до одного), а покажчик ID за розділами відхилено в s2-c-6 fix-c; перекроїти розділи означає перекроїти s2, і це слово власника"
  :for-owner true}

 {:id :rb-4 :what "частина і розділ мають один рівень заголовка: чотири `## Частина N — …` стоять серед 17 заголовків розділів і семи заголовків головних блоків, усі `##`"
  :where "уся структура файлу"
  :reading "в огляді документа частина не відрізняється від розділу, тобто чотири частини читання, заради яких s2 перевпорядкував групи, у навігації не видно"
  :kept-because "(def section-shape) :heading в s2 приписує саме це — «частина дає заголовок того ж рівня»; рівні заголовків читають gen_index і doc_lint, і міняти їх у read-back без слова власника зарано"}

 {:id :rb-5 :what "до першого правила — 319 рядків контракту: (def spec-contract), (def id-law), (def id-prefixes), (def entry-shape), (def parse-contract), (def enforcers), три легенди позначок і 70-рядковий словник"
  :where "від голови файлу до розділу «Таблиці, теги, архетипи, народження»"
  :reading "для машини це правильний порядок — контракт перед даними; для читача перший контакт із НАБОРОМ настає на середині першої третини файлу"
  :kept-because "порядок голови задає (def document) :head-blocks; жоден із цих блоків не зайвий, а тригер у frontmatter звужує привід відкривати файл — s2-c-6 fix-a"}

 {:id :rb-6 :what "абзац прози перед кожним розділом дослівно дорівнює ключу :does того самого блоку — відрізняється лише велика перша літера; так 17 разів"
  :where "усі 17 розділів правил"
  :reading "одне речення має два доми в одному файлі — рівно та хвороба, через яку :fc-marks-not-enumerated відмовився переносити переліки позначок; читач, що прочитав абзац, у :does не дізнається нічого"
  :kept-because "(def section-shape) :prose приписує «переклад :does і нічого більше»; прибрати один із двох домів — це або зняти прозу (проти :ad-prose-per-section), або зняти :does із блоків (проти форми розділу), і обидва — рішення власника"
  :for-owner true}

 {:id :rb-7 :what "складені записи: один ID несе кілька правил, яких дотримуються й які порушують окремо"
  :where ":addressable/never (вісім заборон), :folder/roles (вісім папок у ключі :folders), :recipe/signatures (дванадцять ознак), :transaction/lifecycle (чотири стадії), :transaction/ui-projection (пʼять тверджень), :place/domain (чотири), :reactive/default (вибір плюс заборона «єдиний реактивний механізм»)"
  :why-it-matters "точка примусу цитує ID, а не його частину: знахідка на :addressable/never не скаже, ЯКА з восьми заборон упала"
  :kept-because "це розвʼязана суперечність :c-2, узята як fix-b: s2 не має права ділити правила набору, а read-back тим паче — поділ змінює те, що каже правило"}

 {:id :rb-8 :what "правило-підсумок дублює своїх сусідів: сім із восьми заборон :addressable/never уже стоять окремими ID у тому самому файлі"
  :where ":addressable/never проти :config/address-constant, :addressable/instance-release, :addressable/one-owner, :addressable/empty-box, :addressable/cancel-throws, :addressable/sequential-rollback, :addressable/box-field-disposable"
  :why-it-matters "одне порушення дає дві знахідки з різними ID; зміна одного з двох формулювань розводить їх мовчки"
  :kept-because "текст обох сторін — s1 дослівно; звести їх в один запис означає переписати правило"
  :for-owner true}

 {:id :rb-9 :what "словник конструкцій місцями переказує саме правило, а не адресата"
  :where ":system :holds повторює перелік восьми баз слово в слово з :system/definition :says; :tag проти :tag/is; :component проти :component/is; :subsystem проти :system/subsystem-is-not-a-system; :cleanup проти :event/cleanup"
  :why-it-matters "словник мав бути мовою правил; там, де він несе саме правило, факт знову має два доми, і перейменування бази доведеться правити двічі"
  :kept-because "і :holds, і :says узяті з s1 дослівно; розвести їх — це правка тексту правила"}

 {:id :rb-10 :what "три ID забороняють те саме: додати тег після народження"
  :where ":tag/added-by-archetype-only, :tag/composed-by-declaration, :birth/no-late-structural — усі три :checked-by :graph, усі три в rules-tables"
  :why-it-matters "один живий випадок дасть три знахідки, і читач не знатиме, яке з трьох правило-господар"
  :kept-because "злиття близнюків — та сама заборона, що й у :rb-7; предмет власника"}

 {:id :rb-11 :what "дві мови в одному наборі: шість правил називають живі типи C# у :says, решта 237 переказують їх словами задачі"
  :where ":system/definition, :system/role-order, :system/base-choice, :system/subsystem-is-not-a-system, :system/marker-vocabulary-parity, :state/preupdate-cache — проти «Update-база», «базовий клас обʼєкта сцени», «контракт тегу» скрізь інде"
  :why-it-matters "(def spec-contract) :never забороняє живу назву класу ЯК правило; тут назва не є правилом, але шість тверджень усе одно згниють від одного перейменування бази"
  :kept-because ":says кожного з шести — s1 дослівно, і без переліку баз правило :system/definition не має предмета"}

 {:id :rb-12 :what "переганяння критерію :kind :choice по всіх 25, як вимагав s2-c-1 fix-a: 21 тримає критерій чисто, чотири — гібриди, друга половина яких є властивістю КОДУ"
  :where ":system/split-when («тримає більше двох непов'язаних сімейств запитів» вимірне), :system/driven-by, :view/ecs-only-across-a-boundary (межу збірки граф міряє — s2 назвав це сам), :method/inline-helper («перевикористовується» вимірне)"
  :kept-because "поки запис складений (:rb-7), позначка на весь запис не може бути чистою; критерій прикладений до ГОЛОВНОЇ умови кожного з чотирьох, і вона всюди властивість задачі"}

 {:id :rb-13 :what "зворотний бік того ж критерію: два непозначені правила читаються як вибір"
  :where ":index/one-fk-per-space — власний текст каже «є рішенням дизайну»; :key/self-index — умова «значення пошуку може прийти ззовні» є властивістю задачі"
  :kept-because "поставити :kind — це змінити, як правило читають; 25 обрані стадією s2 явним списком, і додавати до нього в read-back означає правити її судження"
  :for-owner true}

 {:id :rb-14 :what "звірка позначок на несуперечність — порушень нема"
  :where "усі 243 записи"
  :checked ["усі три :checked-by :none несуть :enforcement :none-today, як вимагає (def enforcers) :none :always-with, і навпаки"
            "усі пʼять :weaker-today називають прогалину в :note"
            "усі три значення :needs-owner розвʼязуються в ID (def open-readings), і :rule кожного з трьох записів блоку вказує на живе правило"
            "порядок ключів (def entry-shape) :key-order витриманий у всіх 243 записах"]
  :kept-because "нема що виправляти"}

 {:id :rb-15 :what "блок відкритих читань стоїть за 433 рядки від першого правила, що на нього посилається, і втратив ключі :count і :never, які s2 дав (def open-readings-block)"
  :where ":singleton/manifest — рядок 352; (def open-readings) — рядок 785"
  :why-it-matters "позначка на правилі видна, легенда в (def entry-shape) є, отже читача не введено в оману — але ціна :under-yes/:under-no ходить через увесь файл; втрачені :count і :never переказані прозою над блоком, і серед чотирьох записів :from-code це рішення не назване"
  :kept-because "факт не втрачено — «Три читання» і «Тихо закрити читання … не можна» стоять у прозі; окремий :from-code для переносу двох ключів у прозу дописувати заднім числом у чужу стадію не буду"}

 {:id :rb-16 :what "ID відкритого читання (:oq-…) — єдина родина ідентифікаторів, форми якої документ не задає"
  :where "(def open-readings) :id проти (def id-law) і (def id-prefixes)"
  :why-it-matters "закон ID накриває тільки ID правил; реєстр префіксів не має :oq, і читач не знає, чи це той самий простір імен"
  :kept-because "це не ID правила, і блок визначає їх позицією; заводити другий закон ID у read-back — переписувати контракт файлу"}

 {:id :rb-17 :what "звірка документа з s2 і s1 — точна"
  :where "усі блоки файлу"
  :checked ["243 ID: усі з s1, жодного зайвого, дублів 0, 38 префіксів, усі в реєстрі й усі вжиті"
            ":says усіх 243 — байт у байт із s1; :governs усіх 243 — те саме; :checked-by усіх 243 дорівнює :enforced-by s1"
            "56 конструкцій перенесені всі, :holds збігається скрізь, окрім :diagnostic, який втратив «FM1001-FM1004» — це :fc-id-law-without-s1-pointer, прикладений і до словника; літер FM у файлі 0"
            "десять нотаток: пʼять переписані і одна нова — рівно ті, що називає (def notes-carried); жодної загубленої"
            "17 розділів × правила збігаються з # Покриття s2 по кожному розділу; суми частин 71+77+61+34 = 243"
            "суми (def tally) :checked-by {:agent 152 :graph 58 :arch-check 26 :analyzer 4 :none 3} дають 243 і збігаються з перерахунком"
            "усі 30 блоків — (def <простий-символ> <мапа-або-вектор>), дужки збалансовані, ^-метаданих 0, крапки з комою поза рядками 0, символів із квадратними дужками 0"]
  :kept-because "нема що виправляти"}

 {:id :rb-18 :what "чотири записи :from-code — справжні потреби, записані чесно"
  :where "# s2, блок «Рішення стадії коду»"
  :checked [":fc-marks-not-enumerated — перевірено: у документі нема ні :ids, ні :marked, ні :places; перерахунок справді лишився в (def tally)"
            ":fc-notes-carried-is-not-content — перевірено: (def notes-carried) у документі нема, а самі нотатки на місці"
            ":fc-id-law-without-s1-pointer — перевірено: :from-s1 знято, FM у файлі 0"
            ":fc-open-readings-lose-the-day-count — перевірено: «пʼять живих систем» і «один живий статичний клас» стали загальними формулюваннями"]
  :also "неназваним лишилося одне дрібне рішення тієї ж стадії: три рядки :law-numbers s1 втратили хвости (:entities-per-key-value, :cleanup-priority, :pipeline-priority-band); кожен хвіст живе в :says правила-власника — :index/bucket-cap, :event/cleanup, :system/priority-source"
  :kept-because "факт не втрачено жодного разу; дописувати пʼятий :from-code у чужу стадію заднім числом — робота converge, якщо він визнає це потрібним"}

 {:id :rb-19 :what "той самий факт цитування живе у двох блоках голови"
  :where "(def id-law) :citation {:analyzer :graph :agent} проти (def enforcers) :cites на кожному з чотирьох родів перевірки"
  :why-it-matters "де саме інструмент ставить ID, сказано двічі; розійдуться вони мовчки"
  :kept-because "обидва блоки прийшли з s2 такими; який із двох домів правильний — форма голови документа, тобто слово власника"}]
```

## Вердикт read-back

Документ читається — але читається як довідник, а не як розповідь, і саме таким його спроєктували.
Механічний бік бездоганний: 30 блоків валідні, 243 ID унікальні, кожен `:says` байт у байт із s1,
кожен `:governs` і `:checked-by` на місці, порядок ключів витриманий у всіх 243 записах, позначки не
суперечать одна одній, а чотири записи `:from-code` описують те, що стадія коду справді зробила.
Звірка знайшла рівно дві неправди — обидві числові, обидві про той самий пропущений `:stack`, і обидві
виправлені. Що лишається — це ціна форми, а не дефект перекладу: правила про одну конструкцію
розкидані по шести розділах, і зібрати їх можна лише пошуком, бо обіцяний зріз за `:governs` ще не
існує; частини не видно в заголовках; сімнадцять абзаців прози дослівно повторюють `:does` під собою;
складені записи на кшталт `:addressable/never` несуть по вісім правил під одним ID і на сім восьмих
дублюють сусідів. Жодна з цих знахідок не є помилкою стадії коду — усі чотири стоять у явних
рішеннях s2, які read-back не має права скасувати. Власникові варто показати першими три: чи
приймається пошук як єдиний спосіб зібрати правила однієї конструкції (`:rb-3`), чи лишаються
складені записи нероздільними, коли сім із восьми їхніх частин уже мають власні ID (`:rb-8`), і чи
потрібні обидва доми одного речення — проза розділу і `:does` під нею (`:rb-6`).

# Converge

Стадія прочитала RULES_SPECIFICATION.md цілком і класифікувала кожен запис `# s2` проти неї: голову
документа, закон ID і всі 38 префіксів, обовʼязкові й необовʼязкові ключі запису, 17 розділів із
їхніми лічильниками, три класи позначок, словник конструкцій, відкриті читання, межі набору і звірку.
Жодне число документа не взяте на віру — кожне перераховане скриптом по блоках самого файлу. Звірка
йде за ключем `:carries` розділу і за ID правила, ніколи за порядком: так приписав `s2-c-4 fix-a`.

```clojure
(def converge-method
  {:at "2026-09-16"
   :subject "RULES_SPECIFICATION.md проти # s2 цього каскаду"
   :unit "запис s2: блок голови, закон ID, префікс, ключ запису, розділ, клас позначок, словник, відкрите читання, межі набору, звірка"
   :compared-by "ключ :carries розділу і ID правила, ніколи порядок"
   :counted-by "скрипт по блоках файлу: fence, далі символ (def …), далі записи з ключем :id"
   :verdicts {:present "запис s2 стоїть у документі"
              :partial "стоїть, але у зміненій формі"
              :contradicts "документ каже інакше, ніж s2"
              :unrequested "у документі є те, чого s2 не знає"}
   :file {:lines 836 :fences 30 :rules 243 :sections 17 :parts 4 :prefixes 38 :constructs 56}})
```

## Голова документа

```clojure
(def converge-head
  [{:entry :document/frontmatter :verdict :present :evidence "пʼять ключів :frontmatter дослівно — category C, read trigger, текст тригера, tags, related"}
   {:entry :document/first-line :verdict :present :evidence "перший рядок під заголовком — рядок s2 дослівно"}
   {:entry :document/preamble :verdict :present :evidence "сім пунктів :preamble стали сімома абзацами з жирним заголовком, у тому ж порядку і з тим самим змістом"}
   {:entry :document/head-blocks :verdict :present :evidence "порядок spec-contract, id-law, id-prefixes, entry-shape, parse-contract, enforcers; id-prefixes стоїть там, куди його кладе id-law словами «реєстру нижче»"}
   {:entry :spec-contract :verdict :present :evidence "порядкова різниця з s2 — нуль рядків"}
   {:entry :id-law :verdict :partial :evidence "знято ключ :from-s1; :citation :analyzer каже «номер діагностики лишається її тотожністю» замість «номер FM»; літер FM у файлі нуль"
    :because :fc-id-law-without-s1-pointer :reason-holds true}
   {:entry :id-prefixes :verdict :present :evidence "38 ключів реєстру дослівно"}
   {:entry :entry-shape :verdict :partial :evidence "пʼять рядків переписані тим самим рішенням про покажчик на s1 — :says, :carrier-anchors, :contradiction-ids, :diagnostic-numbers, :where-they-live; з :never знято знак коментаря"
    :because :fc-id-law-without-s1-pointer :reason-holds true
    :note "рішення :from-code названо тільки для (def id-law), а застосоване ще тут, у (def numbers-placement) і в записі :diagnostic словника"}
   {:entry :parse-contract :verdict :partial :evidence "з :never знято знак коментаря, решта дослівно" :because :unnamed
    :note "єдина змістовна зміна — заборона тепер написана словами, а не символом; правило те саме"}
   {:entry :enforcers :verdict :present :evidence "порядкова різниця — нуль рядків; ключ :none :always-with витриманий: три правила з :checked-by :none несуть :enforcement :none-today, і навпаки"}])
```

## Закон ID і реєстр префіксів

```clojure
(def converge-id-law
  [{:entry :id-law/form :verdict :present :evidence "243 з 243 ID — namespaced keyword у формі :prefix/slug; жодного винятку"}
   {:entry :id-law/unique :verdict :present :evidence "унікальних ID 243, дублів 0"}
   {:entry :id-law/registry :verdict :present :evidence "38 префіксів у реєстрі, 38 ужито; незареєстрованих 0, порожніх 0"}
   {:entry :id-law/retired-ids :verdict :present :evidence "ключ :retired-ids [] на місці"}
   {:entry :id-law/citation :verdict :present :evidence "жодного номера діагностики і жодного посилання на заголовок у файлі; цитує лише точка примусу"}
   {:entry :id-prefixes/usage :verdict :present
    :evidence "system 18, alloc 15, event 15, addressable 13, config 13, state 12, tag 12, view 11, archetype 9, catalogue 9, key 9, orchestrator 9, transaction 8, dep 7, pipeline 7, table 7, fail 6, method 6, index 5, place 5, structural 5, birth 4, choose 4, folder 4, name 4, thread 4, link 3, perframe 3, write 3, comment 2, component 2, reactive 2, singleton 2, async 1, deviation 1, oneshot 1, recipe 1, turn-phase 1"
    :note "найрідші пʼять — по одному правилу; реєстр закритий і жоден префікс не стоїть порожнім"}])
```

## Форма запису

```clojure
(def converge-entry-shape
  [{:entry :entry-shape/required :verdict :present :evidence "243 з 243 записів несуть :id, :says, :governs і :checked-by"}
   {:entry :entry-shape/key-order :verdict :present :evidence "порушень сталого порядку ключів — 0 на 243 записах"}
   {:entry :entry-shape/kind :verdict :present :evidence "25 записів із :kind :choice; множина ID збігається з переліком s2 точно, симетрична різниця порожня"}
   {:entry :entry-shape/enforcement :verdict :present :evidence "8 позначок: три :none-today і пʼять :weaker-today, ті самі правила і ті самі тексти :note, що в (def enforcement-marks) :marked"}
   {:entry :entry-shape/needs-owner :verdict :present :evidence "три правила — :state/lifetime-flags, :singleton/manifest, :view/layer — несуть ID свого читання, і кожен ID розвʼязується в (def open-readings)"}
   {:entry :entry-shape/note :verdict :present :evidence "10 нотаток рівно на тих правилах, що їх називає (def notes-carried): чотири дослівні, пʼять переписаних, одна нова на :system/marker-value"}
   {:entry :entry-shape/numbers :verdict :present :evidence "9 правил, 10 чисел; правила і значення збігаються з (def numbers-placement) :places дослівно"}
   {:entry :entry-shape/folders :verdict :present :evidence "ключ :folders стоїть рівно на :folder/roles"}
   {:entry :entry-shape/signatures :verdict :present :evidence "ключ :signatures стоїть рівно на :recipe/signatures"}
   {:entry :entry-shape/no-other-keys :verdict :present :evidence "ключів поза переліком (def entry-shape) у записах правил — нуль"}
   {:entry :entry-shape/governs-resolves :verdict :present :evidence "243 з 243 значень :governs розвʼязуються у словнику; висячих 0; названих конструкцій 47, як і каже s2"}
   {:entry :entry-shape/checked-by :verdict :present :evidence "agent 152, graph 58, arch-check 26, analyzer 4, none 3 — сума 243, збіг із (def tally) і з s2"}])
```

## Розділи, частини, форма розділу

```clojure
(def converge-sections
  [{:entry :sections/count :verdict :present :evidence "17 блоків із символом на rules-, жодного зайвого і жодного зниклого"}
   {:entry :sections/carries :verdict :present :evidence "ключ :section кожного блоку дорівнює :carries того ж розділу в s2 — 17 з 17"}
   {:entry :sections/does :verdict :present :evidence "ключ :does кожного блоку — текст s2 дослівно, 17 з 17"}
   {:entry :sections/titles :verdict :present :evidence "17 заголовків другого рівня дорівнюють :title розділів s2"}
   {:entry :sections/order :verdict :present :evidence "порядок блоків у файлі дорівнює порядку (def sections)"}
   {:entry :sections/rule-counts :verdict :present
    :evidence "перерахунок: tables 34, keys 14, components 8, events 15, systems 18, system-state 12, memory 15, runtime-forms 15, one-shot 9, transaction 8, view 11, configs 26, threading 8, placement 16, naming 4, methods 16, form-choice 14; кожен збігається з :rules розділу s2 і з # Покриття :by-section; сума 243"}
   {:entry :parts :verdict :present :evidence "чотири заголовки частин із назвами s2; суми 71, 77, 61, 34 по розділах частини збігаються з (def parts)"}
   {:entry :section-shape/prose :verdict :present :evidence "17 з 17 абзаців перед блоком — переклад :does; знахідка :rb-6 про два доми одного речення лишається чинною і адресована власникові"}
   {:entry :section-shape/block :verdict :present :evidence "кожен блок — (def <імʼя> {:section … :does … :rules [ … ]}), як приписує (def section-shape)"}])
```

## Позначки

```clojure
(def converge-marks
  [{:entry :choice-rules :verdict :partial :evidence "легенда стоїть без ключа :ids; 25 позначених правил збігаються з переліком s2 точно" :because :fc-marks-not-enumerated :reason-holds true}
   {:entry :enforcement-marks :verdict :partial :evidence "легенда стоїть без ключа :marked; 8 позначених правил і їхні :note дослівні" :because :fc-marks-not-enumerated :reason-holds true}
   {:entry :numbers-placement :verdict :partial :evidence "легенда стоїть без ключа :places; 9 правил і 10 чисел дослівні; :not-carried переписано без слів «стадії s1»" :because :fc-marks-not-enumerated :reason-holds true}
   {:entry :notes-carried :verdict :present :evidence "блоку в документі нема — і не мало бути; усі 10 нотаток на правилах" :because :fc-notes-carried-is-not-content :reason-holds true}])
```

## Словник конструкцій

```clojure
(def converge-constructs
  [{:entry :constructs :verdict :contradicts
    :evidence "у файлі 56 записів з ключами :type і :holds; s2 каже «усі 55 записів» у (def constructs-block) :carries і :constructs 55 у # Покриття"
    :second-fact "неназваних жодним :governs у файлі девʼять; s2 :vocabulary-only перелічує вісім"
    :the-fact "зайвий проти s2 — запис :stack, який лічильник s2 не побачив, бо той стоїть на рядку відкриття блоку"
    :direction "документ правий, помилка арифметики — у s2; read-back :rb-1 і :rb-2 виправив файл, s2 лишився з 55"
    :fix {:where "s2 — окремий запис :from-code про перерахунок словника; або слово власника лишити s2 як історію стадії"
          :never "правити RULES_SPECIFICATION.md під число s2 — у файлі 56 живих записів"}}
   {:entry :constructs/entry-keys :verdict :present :evidence "усі 56 записів мають рівно :type і :holds; ключів :from і :anchors у файлі нема"}
   {:entry :constructs/position :verdict :present :evidence "словник стоїть перед усіма розділами правил"}
   {:entry :constructs/governed :verdict :present :evidence "названих якимось :governs конструкцій 47 — число s2"}])
```

## Відкриті читання, межі набору, звірка

```clojure
(def converge-tail
  [{:entry :open-readings :verdict :partial
    :evidence "три записи з ключами :id, :rule, :chosen, :chosen-confidence, :under-yes, :under-no, :why-open — усі, що дав s2; :under-yes і :under-no без вимірів дня каскаду"
    :moved-to-prose [:does :in-place :count :never]
    :because :fc-open-readings-lose-the-day-count
    :reason-holds true
    :note "перенос чотирьох ключів у прозу над блоком не названо жодним записом :from-code — це знахідка :rb-15, і converge її підтверджує"}
   {:entry :out-of-scope :verdict :partial
    :evidence ":criterion і всі сім :reasons дослівно"
    :moved-to-prose [:does]
    :dropped [:dropped]
    :because :unnamed
    :note "ключ :dropped («інвентарні ID виключених записів, 48 штук») знято зовсім; факт не втрачено — його несе (def entry-shape) :not-repeated :carrier-anchors"}
   {:entry :tally :verdict :contradicts
    :evidence ":constructs 56 у файлі проти 55 у (def tally-block); решта дев'ять чисел збігаються з перерахунком — rules 243, sections 17, parts 4, prefixes 38, choice-rules 25, enforcement-marks 8, open-readings 3, checked-by agent 152 graph 58 arch-check 26 analyzer 4 none 3"
    :the-fact "той самий пропущений :stack"
    :note "це друге місце одного факту, а не друга помилка"}
   {:entry :s2-wrappers :verdict :present
    :evidence "(def parts), (def sections), (def section-shape), (def constructs-block), (def open-readings-block), (def out-of-scope-block), (def tally-block) — інструкції s2 про структуру, а не вміст; реалізовані заголовками, прозою і формою блоків, у файл як блоки не йдуть"}])
```

## Записи :from-code стадії коду

```clojure
(def converge-from-code
  [{:id :fc-marks-not-enumerated :reason-holds true
    :checked "у файлі нема ні :ids, ні :marked, ні :places; при цьому 25, 8 і 9 позначок стоять на тих самих правилах, що їх називав s2, і перерахунок лишився в (def tally)"
    :verdict "причина тримає: перелік справді дав би позначці другий дім у тому самому файлі"}
   {:id :fc-notes-carried-is-not-content :reason-holds true
    :checked "(def notes-carried) у файлі нема; усі 10 нотаток на правилах, поділ на дослівні, переписані й нову збігається"
    :verdict "причина тримає: запис говорив про роботу стадії, не про код"}
   {:id :fc-id-law-without-s1-pointer :reason-holds true
    :checked "ключа :from-s1 нема, літер FM у файлі 0"
    :verdict "причина тримає, але текст запису вужчий за дію: рішення застосоване ще в (def entry-shape) пʼять разів, у (def numbers-placement) і в записі :diagnostic словника"
    :recommend "розширити текст запису на всі місця, де знято покажчик на стадію s1"}
   {:id :fc-open-readings-lose-the-day-count :reason-holds true
    :checked ":under-yes і :under-no трьох читань не несуть жодного виміру дня каскаду"
    :verdict "причина тримає"}
   {:unnamed true :what "ключі :count, :never, :in-place і :does блоку відкритих читань стали прозою" :found-by :rb-15}
   {:unnamed true :what "ключ :dropped блоку меж набору знято зовсім" :found-by :converge}
   {:unnamed true :what "знак коментаря знято з :never у (def entry-shape) і (def parse-contract) — заборона написана словами" :found-by :converge}])
```

## Те, чого s2 не знає

```clojure
(def converge-unrequested
  {:substantive 0
   :substantive-check "правил 243 — жодного поза набором s2; конструкцій 56 — усі з s1; чисел 10 — усі з :places; позначок 25, 8, 3, 9, 10 — усі з s2; ключів запису поза (def entry-shape) — нуль; блоків 30 — усі названі s2"
   :structure-without-an-s2-home
   [{:what "заголовок першого рівня «FantasyMayor — Специфікація правил коду»" :s2-gave ":first-line, заголовка не давав" :carries "нічого нового"}
    {:what "заголовок «Контракт набору» над (def spec-contract)" :s2-gave "блок без назви розділу" :carries "нічого нового"}
    {:what "заголовок «Точки перевірки» над (def enforcers)" :s2-gave "блок без назви розділу" :carries "нічого нового"}
    {:what "абзац перед (def id-prefixes)" :s2-gave "нічого" :carries "переказ id-law :prefix і :mint крок 2"}
    {:what "абзац під заголовком «Позначки»" :s2-gave "нічого" :carries "переказ entry-shape :optional"}
    {:what "друге речення абзацу кожної частини — перелік її розділів" :s2-gave "абзац на частину" :carries "навігацію"}]
   :reading {:chosen "змістовне: :unrequested рахує лише те, що додає правило, дане, число або ключ, якого s2 не знає — тоді 0"
             :chosen-confidence 70
             :other "буквальне: рахувати й прозу та заголовки, яких s2 не називає — тоді 6"
             :other-confidence 30
             :auto-decided true
             :because "стадія міряє дрейф набору, а не типографіку; жоден із шести записів не несе правила, дана чи числа, і кожен переказує ключ, що вже стоїть у файлі"}})
```

## Дрейф, який converge знайшов і не правив

```clojure
(def converge-drift
  [{:id :dr-1 :what "преамбула називає «CASCADE.md # s1 — якір кожного правила на носія», а (def entry-shape) :where-they-live той самий покажчик уже втратив і каже просто «CASCADE.md тримає провенанс»"
    :why-it-matters "один файл дає два читання того самого факту; рішення :fc-id-law-without-s1-pointer застосували до блоків і не застосували до прози"
    :cost "мале: обидва тексти правдиві сьогодні, розійдуться після архівації задачі"
    :fix "вирівняти прозу під блок або блок під прозу — слово власника"}
   {:id :dr-2 :what "числа 55 і «вісім» у s2 проти 56 і девʼяти у файлі"
    :why-it-matters "це обидва записи :contradicts цієї стадії"
    :fix "запис :from-code у s2; документ не чіпати"}
   {:id :dr-3 :what "три рішення стадії коду не мають запису :from-code"
    :why-it-matters "наступний каскад над цим файлом не побачить, що їх ухвалювали свідомо"
    :fix "дописати три записи в # s2, блок «Рішення стадії коду» — робота власника або окремого проходу, не converge"}])
```

## Метри

```clojure
(def converge-meters
  {:contradicts 2
   :contradicts-entries [:constructs :tally]
   :one-fact "обидва — той самий пропущений запис словника :stack"
   :target-met false
   :unrequested 0
   :unrequested-target-met true
   :classified {:present 39 :partial 8 :contradicts 2 :unrequested 0}
   :note "класифіковано 49 записів s2: 10 голови, 6 закону ID, 12 форми запису, 9 розділів, 4 позначок, 4 словника, 4 хвоста — останній із них про обгортки s2, які в документ як блоки не йдуть"})
```

## Вердикт converge

Документ виводиться зі свого s2. Кожне правило, кожен префікс, кожна позначка й кожне число, яке s2
назвав, стоїть у файлі рівно там і рівно так, як приписано: 243 ID у 17 розділах і 4 частинах, 38
префіксів усі вжиті й усі зареєстровані, порядок ключів витриманий у всіх 243 записах, 25 позначок
вибору, 8 позначок примусу, 3 відкриті читання, 9 правил із числами і 10 нотаток — усі на тих самих
правилах, що їх назвав s2, перевірено перерахунком, а не читанням чисел документа. Вісім записів
класифіковано як `:partial`, і жоден із них нічого не втратив: чотири — це переліки позначок і
покажчик на стадію s1, зняті названими рішеннями `:from-code`, причини яких тримають; решта — ключі,
що переїхали в прозу того самого розділу або відпали разом із провенансом, і три такі рішення
ухвалено без запису. Єдина справжня
розбіжність одна, і вона арифметична: стадія s2 порахувала 55 конструкцій і вісім несудимих, тоді як
у словнику 56 і девʼять, бо лічильник не побачив запис `:stack`. Її виправив read-back — у файлі, не
в s2, — тому метр `:contradicts` читає 2 при цілі 0, і закрити його може лише запис `:from-code` у
s2 або слово власника. `:unrequested` читає 0: у файлі нема жодного правила, дана, числа чи ключа,
яких s2 не знає; шість дрібниць структури — заголовок першого рівня, дві назви розділів голови, два
абзаци переказу і навігаційне речення частини — не несуть нічого, чого б не було поруч у блоці.

# Calibration

Міри цього прогону — каскаду не над класом, а над документом.

```clojure
(def calibration
  {:run "RULES_SPECIFICATION.md, 2026-09-15 — 2026-09-16, шість стадій, кожна окремим агентом із чистим контекстом, авто-режим"
   :contra-noise {:entries 15 :s1 8 :s2 7 :rejected-by-owner :unknown
                  :why "ворота після s1 і після s2 стоять :waived-by-owner; власник не бачив жодного запису # Contra, отже шум контри цим прогоном не виміряний"
                  :what-is-known "усі 8 записів # Contra стадії s1 розвʼязані явно в s2; з 7 записів # Contra s2 read-back переганяв два — :s2-c-1 (критерій :choice) і :s2-c-4 (звірка не за порядком), обидва витримали"}
   :invented-at-translation {:named 4 :unnamed 3 :total 7 :recorded-rate "4 з 7"
                             :named [:fc-marks-not-enumerated :fc-notes-carried-is-not-content :fc-id-law-without-s1-pointer :fc-open-readings-lose-the-day-count]
                             :unnamed ["ключі :count, :never, :in-place і :does блоку відкритих читань стали прозою"
                                       "ключ :dropped блоку меж набору знято зовсім"
                                       "знак коментаря знято з :never у двох блоках голови"]
                             :scope-drift "один із чотирьох названих — :fc-id-law-without-s1-pointer — застосовано ширше, ніж каже його текст"}
   :names-lost {:def-names 0 :rule-ids 0 :prefixes 0 :constructs 0 :section-titles 0 :marks 0
                :s2-content-keys 7
                :which [:from-s1 :ids :marked :places :count :never :dropped]
                :note "жодного втраченого ФАКТУ: чотири ключі зняті названим рішенням, два переїхали в прозу того ж розділу, один покритий іншим ключем того самого файлу"}
   :read-back {:findings 19 :fixed 2 :kept 17 :for-owner 3
               :fixed-what "обидві правки — числові й обидві про той самий пропущений :stack"
               :hit-rate "2 з 19 знахідок були неправдою файлу; решта 17 — ціна форми, приписаної s2"}
   :converge {:contradicts 2 :unrequested 0 :classified 49 :present 39 :partial 8}
   :owner-verdict :pending
   :owner-sees "підсумок фінального артефакту — так домовлено на початку каскаду"
   :document-vs-class
   "Рецепт каскаду писався для класу: ключі s2 — :in, :out, :writes, :scratch, :calls, а одиниця звірки converge — «метод, запис даних, народжений тип». Над документом жодне з цього не прикладається, і кожна стадія мусила винайти свою одиницю: s2 завів :does, :carries і :rules, converge — «блок голови, префікс, ключ запису, розділ, позначка, словник». Ціна винаходу видно в самому прогоні — ця стадія двічі зупинилась, не почавши писати, поки одиниця класифікації не була названа вголос. Каркас каскаду над документом тримає: дисципліна «нічого, чого не несе попередній рівень» дала 0 винайдених правил на 243 і 2 розбіжності на 49 класифікованих записів. Не тримає словник: назви ключів треба перекладати на кожній стадії, і саме там прогон гальмує."})
```

# Поправки власника після converge

```clojure
(def owner-amendments
  {:at "2026-09-16"
   :who "власник, після підсумку каскаду"
   :entries
   [{:id :oa-constructs-count
     :from-code "лічильник s2 не побачив запис :stack — він стоїть на рядку відкриття блоку; словник несе 56 конструкцій, з них 9 не називає жоден :governs"
     :owner "впиши 56 у s2"
     :changed "s2 :constructs 55 → 56 у трьох місцях цього артефакту; документ уже ніс 56 після read-back"
     :effect "метр converge :contradicts 2 → 0"}

    {:id :oa-split-never
     :owner "id для заборон розбивай"
     :changed "правило :addressable/never знято; дві заборони, яких не несло жодне сусіднє правило, дістали власні ID — :addressable/no-detached-value і :addressable/check-before-read; решта шість уже живуть поіменно: адреса з імені типу — :config/address-constant, ручне знищення — :addressable/instance-release, одна коробка двом власникам — :addressable/one-owner, тихий вихід на скасуванні — :addressable/cancel-throws, звільнення перед киданням замість блоку завершення — :addressable/cancel-throws і :addressable/sequential-rollback, поле-коробка без контракту — :addressable/box-field-disposable"
     :effect "правил 243 → 244; :checked-by :agent 152 → 153; кожну заборону тепер цитує власний ID"}

    {:id :oa-drop-duplicate-prose
     :owner "повтори прибирай"
     :changed "17 абзаців прози перед блоками правил знято — кожен дослівно повторював :does блоку під собою; заголовки розділів і проза частин лишились"
     :effect "одне речення — один дім; файл 836 → 777 рядків"}

    {:id :oa-gather-by-construct
     :owner "якщо це дійсно проблема то тоді можна написати інструкцію як це шукати і збирати в єдине ціле"
     :from-code "s2 не мав блоку про те, як зібрати правила однієї конструкції з кількох розділів — знахідка read-back :rb-3"
     :changed "у документ додано (def gather-by-construct): процедура з чотирьох кроків, пошук за ':governs :<ключ>', і пряма відмова робити зріз розділом"
     :effect "розкладка за предметом лишається; цілісність відновлюється процедурою, а не другим домом правила"}

    {:id :oa-close-open-readings
     :owner "погоджуюсь — на всі три обрані читання"
     :changed "з правил :state/lifetime-flags, :singleton/manifest і :view/layer знято ключ :needs-owner; блок (def open-readings) і його розділ видалені; з (def entry-shape) знято необовʼязковий ключ :needs-owner; з преамбули знято абзац про очікування власника; з (def tally) знято :open-readings"
     :effect "три читання закриті словом власника; у наборі не лишилось правила, що стоїть на здогадці"}]
   :meters {:rules 244 :ids-unique true :prefixes-registered true
            :doc-lint "0 помилок Clojure, 3 старі привиди в Patterns/"
            :gen-index "LINT clean"
            :contradicts 0 :unrequested 0}})
```
