---
category: C
read: trigger
trigger: перед будь-якою роботою, що створює або змінює код у Assets/, і перед зміною правила в ARCHITECTURE.md, рецепті Patterns/, скілі чи інструменті
tags: [architecture, rules, specification]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# FantasyMayor — Специфікація правил коду

Єдине джерело правил коду FantasyMayor: кожне правило зі стабільним ID і тим, хто його перевіряє.

**Що це.** Повний набір правил, яких має дотримуватись код. Запис входить сюди тоді й лише тоді, коли
його можна порушити зміною коду в `Assets/`.

**Напрям виведення.** ARCHITECTURE.md, рецепти `Patterns/`, скіли, fantasymayor-graph і
MarkerShapeAnalyzer виводяться ЗВІДСИ. Зміна правила починається тут і лише потім іде в похідні;
носій, що каже інакше, вважається застарілим.

**Як змінити правило.** `:says` правиться на місці, ID не чіпається. Нове правило дістає новий ID за
законом ID. Прибране правило лишає свій ID у `:retired-ids` назавжди.

**Як цитувати.** Помилка аналізатора і попередження графа називають ID. ID шукається в цьому файлі
текстом, ніколи за заголовком розділу — заголовок можна перейменувати, ID ні.

**Чого сьогодні не перевіряє ніщо.** Позначка `:enforcement` називає правила, чия перевірка слабша за
правило або ще не збудована.

**Звідки набір.** Каскад `Flows/RULES_SPECIFICATION`: CONTEXT.md тримає інвентар носіїв, CASCADE.md
`# s1` — якір кожного правила на носія. У цьому файлі якорів нема.

## Контракт набору

```clojure
(def spec-contract
  {:is "повний набір правил, яких має дотримуватись код FantasyMayor"
   :criterion "правило можна порушити ЗМІНОЮ КОДУ в Assets/: існує спостережувана ситуація коду, на яку правило відповідає «так» або «ні»"
   :out "усе, що порушується зміною документа, процесу, ассета, форми DI-реєстрації або поведінки інструмента — див. розділ меж набору"
   :single-source "похідні носії ВИВОДЯТЬСЯ звідси звіркою; цей файл — джерело для підтримки, а не для читання під час роботи"
   :carriers "агент читає носії, не цей файл: ARCHITECTURE.md, CLAUDE.md, проєктні скіли, рецепти Patterns/. Носій несе правило ПОВНІСТЮ, своїми словами і з числами закону — повтор тут норма, а не дефект; носій, що лише посилається сюди, не вчить нічого"
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
   :citation {:analyzer "діагностика MarkerShapeAnalyzer несе ID у тексті повідомлення у формі rule <prefix>/<slug>; номер діагностики лишається її тотожністю і в специфікацію не пишеться"
              :graph "попередження fantasymayor-graph закінчується позначкою [rule <prefix>/<slug>]"
              :agent "знахідка ревʼю називає ID так само"
              :direction "цитує завжди точка примусу; специфікація не знає ні номерів діагностик, ні імен перевірок"
              :lookup "ID знаходять у файлі текстовим пошуком; посилання на заголовок розділу заборонене"}})
```

Префікс називає предмет правила. Реєстр закритий: новий префікс заводиться тим самим записом, що й
перше правило під ним.

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
              :says "саме правило одним реченням, словами задачі"
              :governs "ключ конструкції з (def constructs) — те, чого правило стосується"
              :checked-by "рід перевірки, якому правило піддається — ключ із (def enforcers)"}
   :optional {:kind ":choice, коли умова правила — властивість ЗАДАЧІ, а не коду (def choice-rules); відсутність читається як :check"
              :enforcement ":none-today або :weaker-today (def enforcement-marks); відсутність читається як «перевірка збудована і міряє рівно те, що каже правило»"
              :note "факт, не видний із решти ключів"
              :numbers "іменовані числа правила (def numbers-placement)"
              :folders "закритий вміст рольових папок — лише в :folder/roles"
              :signatures "ознака примірника кожного рецепта — лише в :recipe/signatures"}
   :key-order "сталий: :id :says :governs :checked-by, далі необовʼязкові в порядку цього переліку"
   :where-enforcement-lives "відповідає (def enforcers) один раз для кожного роду перевірки, а не кожен запис правила: місце перевірки — властивість інструмента, не правила"
   :not-repeated {:carrier-anchors "ID записів інвентаря каскаду в документ не йдуть — вони вказують на текст носіїв, який похідна задача зніме, і згниють першими"
                  :contradiction-ids "ID розвʼязаних суперечностей лишаються в каскаді"
                  :diagnostic-numbers "номери діагностик не пишуться поруч із правилом — цитує точка примусу, не специфікація"
                  :baseline "виміри дня каскаду — кількість вузлів графа, систем, відхилень — не правило"
                  :skeletons "скелети C# з рецептів: у набір іде правило, яке скелет кодує"
                  :where-they-live "Flows/RULES_SPECIFICATION/CASCADE.md тримає провенанс кожного правила, CONTEXT.md — інвентар носіїв; обидва архівуються разом із задачею"}
   :never #{"^-метадані"
            "коментар усередині блоку — «чому» живе в :note"
            "символ із квадратними дужками"
            "проза поза рядком"
            "жива назва класу як правило"}})
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
            "коментар усередині блоку специфікації"}})
```

## Точки перевірки

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

## Позначки

Три необовʼязкові ключі запису означають не властивість правила, а спосіб його читати. Легенда кожного
стоїть тут один раз; сама позначка живе на правилі.

```clojure
(def choice-rules
  {:mark ":kind :choice"
   :criterion "умова правила — властивість ЗАДАЧІ (що код має робити), а не властивість коду; тому жоден інструмент не вирішує його з коду, і порушення видно як хибно обрану форму, а не як хибний рядок"
   :default ":check — умова правила є властивістю коду; відсутність :kind читається саме так"
   :why "правило, яке ніколи не спрацьовує, не відрізнити від мертвого; позначка каже читачеві, що це правило читають ПЕРЕД тим, як писати код"
   :count 25
   :rest 219})
```

```clojure
(def enforcement-marks
  {:values {:none-today "сьогодні це правило не перевіряє ніщо"
            :weaker-today "названий рід перевірки міряє менше, ніж каже правило"}
   :absent "перевірка збудована і міряє рівно те, що каже правило"
   :why "правило без збудованого примусу лишається правилом; позначка каже це вголос, замість тихо вдавати перевірку"
   :count 8})
```

```clojure
(def numbers-placement
  {:law "числа закону лягають ключем :numbers на те правило, яке їх називає; окремого блоку чисел у документі нема — число має рівно один дім"
   :numbers 10
   :rules 9
   :not-carried "базова лінія каскаду — кількість вузлів графа, систем, ролей, відхилень — вимір дня, а не правило; у документ не йде"})
```

## Конструкції, про які говорять правила

Словник адресатів: на що показує `:governs` кожного правила. Девʼять записів не називає жоден
`:governs` — вони стоять тут тому, що їх називають ТЕКСТИ правил: на якому стеку стоїть код, чим
міряється якір, як інструмент упізнає вид вузла, що таке ребро графа, що таке діагностика; без них
правила ролі й маркера не прочитати.

```clojure
(def constructs
  {:stack        {:type Friflo.Engine.ECS :holds "рушій Unity, ECS Friflo.Engine.ECS 3.6 у стилі DoD (не Unity DOTS), DI VContainer, async UniTask, Addressables, InputSystem, URP, UI Toolkit плюс Unity App UI"}

   :entity-store {:type EntityStore :holds "сховище сутностей Friflo — власник архетипів, індексів і всіх структурних змін"}
   :storages     {:type EntityStorages :holds "реєстр сховищ: World (ігровий світ), Singletons (однопримірниковий стан), конфіги за типом"}
   :game-state   {:type GameState :holds "стан гри, у якому працює система Update-контракту"}
   :app-state    {:type AppState :holds "прапорець кроку застосунку, за яким запускаються разові системи: ConfigLoading, InstanceObjects, MainMenu"}

   :node-kind    {:type IComponent :holds "вид конструкції, про який говорять правила форми: компонент, тег, подія, конфіг, view, система, архетип, інсталер; як саме інструмент упізнає вид — його справа, не правило"}
   :node-identity {:type Assembly :holds "один вузол на оголошення (partial ділять його); неоголошений тип — вузол з declared false і видом за суфіксом; однойменні типи в різних просторах імен беруть ідентифікатор з простором; неоднозначне імʼя — попередження, не ребро; вузли вкладеного типу належать вкладеному типу"}
   :scan-roots   {:type Assets :holds "корені сканування коду: Assets/Domains, Assets/Presentation, Assets/Modules, Assets/Scripts, Assets/Flows"}
   :graph-edge   {:type ComponentIndex :holds "ребра, які читач виводить із коду: writes (AddComponent, Singletons.Set), reads (GetComponent, HasComponent, Singletons.Get, привʼязка до архетипу таблиці), removes, emits (CreateEvent), reacts_to і polls за роллю, disposes, fk_of за законом суфікса і реальним пошуком в індексі, inherits, hosts, registers, exposes, injects, runs_in, subscribes"}

   :component    {:type IComponent :holds "проста struct runtime-значень — колонка рядка таблиці"}
   :tag          {:type ITag :holds "порожня struct, чия присутність і є інформацією"}
   :main-tag     {:type Tags.Get :holds "перший аргумент типу в Tags.Get оголошення архетипу — дискримінатор таблиці; головний і label теги розділені маркером TagLabel на кожній структурі"}
   :tag-order    {:type Tags.Get :holds "порядок аргументів у Tags.Get: закон вимагає головний тег першим, інструмент порядку не перевіряє — головність вирішує лише маркер"}
   :label-marker {:type TagLabelAttribute :holds "атрибут лише на структурі, не множинний, не успадковується; без аргументу роль None"}
   :label-role   {:type TagLabelRole :holds "перелічення ролей мітки: None (членство), Transaction (transaction-сутність)"}

   :archetype    {:type Archetype :holds "оголошена форма рядка: набір колонок плюс набір тегів; у вузол графа головний тег записується лише коли він рівно один, label-теги — ті, що мають маркер"}
   :holder       {:type Archetypes :holds "static-тримач <Assembly>Archetypes, чиї члени повертають живий Archetype"}
   :singleton-manifest {:type SingletonArchetypeDefinition :holds "оголошення архетипу Singleton — єдиний дім однопримірникових компонентів"}

   :event        {:type EventTag :holds "однокадрова сутність: EventTag як головний тег плюс EventFrameComponent плюс компонент події"}
   :event-archetype {:type EventArchetypes :holds "архетип події, розвʼязаний EventArchetypes.Of<T>(store); кожен споживач бачить подію рівно раз у кадрі після підняття, незалежно від пріоритету"}
   :cleanup      {:type EventCleanupSystem :holds "єдина глобальна система прибирання: Priority int.MaxValue, видаляє дозрілі події в кінці того кадру"}

   :component-index {:type ComponentIndex :holds "індекс, ключований типом компонента через усі таблиці, що його несуть: роль несе тип ключа, таблицю несе тег; таблиця графа — кожне поле ComponentIndex з роллю за суфіксом ключа і єдиним архетипом, що ключ несе"}
   :indexed-key  {:type IIndexedComponent<TValue> :holds "контракт ключа індексу: GetIndexedValue() повертає значення ключа"}

   :system       {:type IUpdatedSystem :holds "клас, який веде рушій: прямо чи транзитивно UpdatedSystem, LateUpdatedSystem, IUpdatedSystem, ILateUpdatedSystem, IUniTaskSystem, IPrioritizedUniTaskSystem, ConfigLoaderSystem<T> або EventCleanupSystem; роль дістають лише оголошені неабстрактні класи видів other, installer, config, view"}
   :system-role  {:type SystemRoleKind :holds "роль системи: cleanup, reactive, per_frame, pipeline_stage, turn_phase, startup_step, sub_system; маркер знає лише PerFrame і Reactive; докази ролі — Update-цикл у спадковості, обхід EventTag із місцем DeleteEntity, аргумент стадії, предок TurnPhaseSubSystem, негенеричний IUniTaskSystem, абстрактний предок, якого хтось збирає; клас без ролі все одно polls архетипи подій, які тримає; форма per-frame = контракт циклу і не подієвий якір, форма reactive = контракт циклу, не табличний якір, і подієвий якір або утримуваний архетип події"}
   :role-marker  {:type SystemRoleAttribute :holds "атрибут лише на класі, не множинний, не успадковується"}
   :anchor       {:type UpdatedSystem :holds "якір міряється лише всередині base(...) класу, чия ПРЯМА база UpdatedSystem або LateUpdatedSystem; аргумент EventArchetypes.Of або AnyComponents над типами подій робить якір подієвим, інакше якір табличний; виклик EventArchetypes.Of поза base(...), включно з this(...), — утримання; подія, на яку клас якориться в base(...), дає ребро reacts_to, утримуваний архетип — reacts_to лише під reactive-маркером, інакше polls"}
   :cadence      {:type UpdatedSystem :holds "каденція виконання: repeated — UpdatedSystem, LateUpdatedSystem, IUpdatedSystem, ILateUpdatedSystem, EventCleanupSystem або база фази ходу; one-shot — IUniTaskSystem включно з ConfigLoaderSystem<T>, стадія конвеєра створення мапи, підсистема разового оркестратора; async — робота в async UniTask"}
   :update-base  {:type UpdatedSystem :holds "база, що вже робить знімок якірного архетипу для своїх нащадків, тому структурні зміни в Update безпечні"}
   :one-shot-base {:type IUniTaskSystem :holds "контракт разового кроку: Execute(token) плюс AppState, на якому Boot його запускає"}
   :pipeline-base {:type IPrioritizedUniTaskSystem<MapGenerationStep> :holds "контракт упорядкованого конвеєра: Update(token) плюс Priority, менший раніше; аргумент типу називає стадію, за якою DI збирає сімейство"}
   :turn-phase-base {:type TurnPhaseSubSystem :holds "public abstract class : IPrioritizedUniTaskSystem<TurnPhaseStep> — база фази ходу, що працює кожного ходу"}
   :subsystem    {:type IDisposable :holds "член сімейства, зібраного DI: простий обʼєкт із IsEnabled, Priority і Run, яким володіє оркестратор — не система"}
   :priority     {:type SystemPriorities :holds "вираз Priority, розвʼязаний проти кожної const int за шляхом вкладеності; вкладені класи — WorldInit, RuntimeTick та інші — простори порядку; EventCleanup дорівнює int.MaxValue"}

   :state-marker {:type StateAllowedAttribute :holds "атрибут на полі, конструктор приймає необовʼязкову причину"}
   :frame-box    {:type FrameBox<T> :holds "змінна struct значення на обмежену кількість кадрів: OneFrame, TwoFrames, ForFrames; Exist залежить від Time.frameCount, Value кидає на застарілому"}
   :structural-change {:type Entity :holds "структурна зміна — AddComponent, RemoveComponent, AddTag, RemoveTag; народження архетипом нею не є; тег, доданий за життя, інструмент бачить як AddComponent типу з іменем на Tag, а AddComponent події одразу після CreateEvent записом не рахується"}

   :view         {:type MonoBehaviour :holds "шар view — усе, що оголошено в папці Views/; партнер view-системи — MonoBehaviour у тій папці; рецепт PATTERN_VIEW_SYSTEM маркера ViewSubscriber не згадує"}
   :view-marker  {:type ViewSubscriberAttribute :holds "атрибут на класі, множинний, не успадковується; підпискою рахується += , ліворуч якого символ події — += над числом чи рядком підпискою не є"}

   :config       {:type ScriptableObject :holds "авторський ассет даних, що живе в EntityStorages за своїм типом"}
   :validatable  {:type IValidatableConfig :holds "контракт валідації авторських даних: Validate() кидає на кожне порушення"}
   :config-loader {:type ConfigLoaderSystem<T> :holds "єдиний loader конфігів: вантажить ассет, кидає при невдачі, валідує, кладе в сховище"}
   :config-address {:type ConfigAddresses :holds "тримач const-адрес addressable у SCREAMING_SNAKE_CASE"}
   :addressable  {:type IAddressable :holds "контракт завантаження: LoadAndInstanceAsync для GameObject, LoadAndInstanceAsync<T> для компонента, LoadAsync<T> для класу, що не GameObject; скасований токен дає OperationCanceledException після звільнення; контракт у Core/, реалізація в Implementation/"}
   :box          {:type Box<T> :holds "readonly struct над класом дескриптора: Value кидає без Exist, Dispose ідемпотентний і звільняє рівно раз, копії ділять дескриптор; Result<T> несе Box і Status, Status має Unknow, Failed, Success"}

   :allocator    {:type Allocator :holds "вибір часу життя native-памʼяті: Temp, TempJob, Persistent"}
   :native-container {:type NativeList<int> :holds "контейнер Unity.Collections — дозволена памʼять повторного прогону"}
   :managed-collection {:type System.Collections.Generic :holds "керовані колекції й масиви: List, Dictionary, HashSet, Queue, Stack, LinkedList, Sorted-сімейство та їхні інтерфейси"}
   :status-monitor {:type StatusMonitor :holds "джерело токена скасування для довгої async-роботи"}
   :query-helpers {:type QueryResultExtensions :holds "TryGetFirst(out entity) — пошук одного рядка на запиті, архетипі чи множині Entities"}

   :assembly     {:type asmdef :holds "збірка Unity — межа видимості типів і вузол графа залежностей"}
   :layer        {:type Assets :holds "шари: domain у Assets/Domains/, presentation у Assets/Presentation/, module у Assets/Modules/, shared kernel у Assets/Scripts/Core/ і Assets/Scripts/EcsExtensions/, app-root у Assets/Scripts/Installers/"}
   :role-folder  {:type Views :holds "рольові папки: Components/, Tags/, Events/, Configs/, Data/, Systems/, Helpers/, Views/, Archetypes/, Prefabs/, Textures/"}

   :diagnostic   {:type MarkerShapeAnalyzer :holds "діагностики категорії FantasyMayor.Markers, severity Error, увімкнені за замовчуванням; тип без маркера перевірок не реєструє, зайвий маркер аналізатор не перевіряє"}
   :marker-vocabulary {:type MarkerVocabulary :holds "словник аналізатора за метаданими: SystemRoleAttribute, ViewSubscriberAttribute, TagLabelAttribute, IUpdatedSystem, ILateUpdatedSystem, UpdatedSystem, LateUpdatedSystem, EventArchetypes, Friflo ComponentTypes і ITag, UnityEngine MonoBehaviour"}
   :recipe-signature {:type RECIPE_SIGNATURES :holds "ознака рецепта — джерело правди у recipes.py, читання у recipe-signatures.md; поліморфний каталог знаходиться трійкою: абстрактна SO-база з іменем на Config, неабстрактний SO-контейнер з масивом цієї бази, компонент виду, названий базою без суфікса Config, який несе архетип"}})
```

## Як зібрати правила однієї конструкції

```clojure
(def gather-by-construct
  {:why "розділ групує правила за своїм предметом, а :governs називає конструкцію — тому правила однієї конструкції лежать у кількох розділах: правила системи стоять у шести"
   :procedure (-> (:step-1 "узяти ключ конструкції з (def constructs)")
                  (:step-2 "знайти в цьому файлі рядки з ':governs :<ключ>' — це всі правила конструкції разом з їхніми ID")
                  (:step-3 "додати правила, які :says знайдених називають поіменно — суміжна конструкція живе в тому ж рядку")
                  (:step-4 "читати зібране як один набір; порядок файлу тут нічого не означає"))
   :who "агент перед зміною коду, що зачіпає конструкцію"
   :not-a-section "зрізу за :governs у документі нема: він виводиться пошуком, бо інакше правило дістає другий дім і починає гнити"})
```

## Частина 1 — Рядок і його дані

З чого складається рядок таблиці й що з ним можна робити. Чотири розділи: таблиці й теги, ролі
колонок, форма компонента і запису, життя однокадрової події.

## Таблиці, теги, архетипи, народження

```clojure
(def rules-tables
  {:section :tables-tags-archetypes
   :does "як оголошується таблиця сутностей, закон головного й label тегів, закон архетипу і повнота народження"
   :rules
   [{:id :table/is :says "таблиця — ключовий компонент плюс дискримінатор, разом в одному оголошеному архетипі" :governs :archetype :checked-by :graph}
    {:id :table/filter :says "фільтр запиту — архетип таблиці, ніколи голий ключовий компонент: голий ключ дає обʼєднання всіх таблиць цього простору ключів, тому фільтр не-події називає рівно один тег" :governs :archetype :checked-by :graph}
    {:id :table/sweep :says "обхід таблиці — ітерація її архетипу, без обʼєкта запиту" :governs :archetype :checked-by :agent}
    {:id :table/keyed-join :says "зʼєднання за ключем — індекс над ключовою колонкою, оголошений раз у конструкторі" :governs :component-index :checked-by :agent}
    {:id :table/cross-archetype :says "запит через кілька архетипів дозволений, лише коли фільтр справді охоплює кілька архетипів і його погоджено з власником; обхід усіх подій за спільним тегом події — єдиний свідомий виняток" :governs :archetype :checked-by :agent :kind :choice}
    {:id :table/join-at-use :says "зʼєднання — пошук за значенням ключа в точці використання, ніколи збережене посилання на сутність з рядка однієї таблиці в рядок іншої" :governs :component-index :checked-by :graph}
    {:id :table/index-only-hot :says "індекс заводиться лише для гарячого зʼєднання — щокадру або багато разів за хід; для пошуку з частотою кліку архетип сканується" :governs :component-index :checked-by :agent :kind :choice}
    {:id :tag/is :says "тег — порожня struct з контрактом тегу: даних не несе, його присутність і є інформацією, а щойно потрібне значення — це компонент" :governs :tag :checked-by :graph}
    {:id :tag/one-main-tag :says "кожне оголошення архетипу має рівно один головний тег, записаний першим" :governs :main-tag :checked-by :graph :numbers {:main-tags-per-archetype "рівно 1"}}
    {:id :tag/main-tag-unique :says "один головний тег називає один архетип — спільний головний тег двох архетипів заборонений, і єдиний виняток — тег події" :governs :main-tag :checked-by :graph}
    {:id :tag/label-marker :says "label-тег — структура тегу з маркером мітки, з роллю або без; міткою може бути лише структура з контрактом тегу" :governs :label-marker :checked-by :analyzer}
    {:id :tag/label-count :says "поруч із головним тегом стоїть від нуля до чотирьох міток — оголошення архетипу бере не більше пʼяти аргументів типу" :governs :label-marker :checked-by :graph :numbers {:label-tags-per-archetype "0-4"}}
    {:id :tag/label-not-a-filter :says "label-тег ніколи не фільтр запиту" :governs :label-marker :checked-by :graph}
    {:id :tag/event-tag :says "кожна подія несе тег події як головний тег, і тег події не стоїть ніде, крім архетипу події" :governs :event :checked-by :graph}
    {:id :tag/added-by-archetype-only :says "тег додається лише оголошенням архетипу таблиці — ніколи живій сутності, бо це виносить рядок з його архетипу" :governs :structural-change :checked-by :graph}
    {:id :tag/composed-by-declaration :says "набір тегів складається лише в оголошенні архетипу — ніколи додаванням до вже складеного набору" :governs :archetype :checked-by :graph}
    {:id :tag/state-column :says "стан — колонка над перелічуванням із суфіксом стану, ніколи перемикаваний тег, і пишеться вона лише при зміні" :governs :component :checked-by :graph}
    {:id :tag/kind-column :says "вид — колонка над перелічуванням із суфіксом виду, ніколи другий головний тег, і пишеться вона раз при народженні" :governs :component :checked-by :graph}
    {:id :tag/transaction-role :says "роль мітки transaction позначає transaction-сутність; мітка без ролі означає членство" :governs :label-role :checked-by :graph}
    {:id :archetype/holder :says "архетипи оголошує один static-тримач на збірку, названий іменем збірки без крапок і без доменного префікса; shared kernel і app-root — виняток" :governs :holder :checked-by :agent}
    {:id :archetype/reach :says "архетип називає лише компоненти, які його збірка може бачити" :governs :assembly :checked-by :agent}
    {:id :archetype/shape :says "кожен член тримача повертає живий архетип у тілі методу, а сховище приходить параметром" :governs :holder :checked-by :graph}
    {:id :archetype/holder-stateless :says "тримач архетипів нічого не зберігає" :governs :holder :checked-by :agent}
    {:id :archetype/birth :says "сутність народжується викликом створення на архетипі — ніколи голим створенням на сховищі в місці виклику" :governs :archetype :checked-by :graph}
    {:id :archetype/bulk :says "масове народження — виклик створення багатьох на архетипі, а коли кількість відома, перед ним резервується місткість" :governs :archetype :checked-by :agent}
    {:id :archetype/assign-then-write :says "новий рядок присвоюється локальній змінній, і лише потім у нього пишуть — ніколи ланцюжок записів просто з виклику створення" :governs :archetype :checked-by :agent}
    {:id :archetype/arity-cap :says "набір колонок і набір тегів в оголошенні архетипу беруть не більше пʼяти аргументів типу кожен" :governs :archetype :checked-by :graph :numbers {:type-arguments-per-declaration "не більше 5 для набору колонок і для набору тегів"}}
    {:id :archetype/no-orphan-column :says "запис компонента, якого не називає жоден оголошений архетип, — порушення" :governs :archetype :checked-by :graph}
    {:id :birth/completeness :says "сутність народжується з кожною колонкою, яку коли-небудь матиме" :governs :archetype :checked-by :graph :enforcement :weaker-today}
    {:id :birth/sentinel :says "присутність не предикат, а значення-сторож — невідомо, спокій, порожня коробка" :governs :component :checked-by :agent}
    {:id :birth/fixed-composition :says "склад рядка фіксований при народженні: необовʼязкових колонок нема, а зміна складу — видалити рядок і створити новий у його архетипі, перенісши значення ключів" :governs :archetype :checked-by :agent}
    {:id :birth/no-late-structural :says "після народження не буває ні видалення компонента, ні видалення тегу, ні пізнього додавання колонки, якої архетип не називає, ні додавання тегу" :governs :structural-change :checked-by :graph}
    {:id :singleton/manifest :says "коли однопримірникові компоненти взагалі вживаються, їх оголошує рівно один архетип-маніфест: використаний, але не оголошений компонент — порушення, оголошений, але не використаний — теж" :governs :singleton-manifest :checked-by :graph}
    {:id :singleton/read :says "однопримірниковий компонент має оголошений архетип, але споживачі читають і пишуть його лише через реєстр однопримірникового стану, ніколи запитом" :governs :storages :checked-by :agent}]})
```

## Ключі, індекси, ролі колонок

```clojure
(def rules-keys
  {:section :keys-and-indexes
   :does "закон ролей ключа, індекс за компонентом і межі self-index"
   :rules
   [{:id :key/pk :says "первинний ключ — колонка з суфіксом ідентичності, власна тотожність рядка; її власник — рівно одна таблиця в парі зі своїм тегом, а унікальність тримає контракт, не рушій" :governs :indexed-key :checked-by :graph}
    {:id :key/fk :says "зовнішній ключ — колонка з суфіксом посилання: посилання з рядка іншої таблиці в простір ключів власника, ніколи тип первинного ключа власника, звʼязок один до багатьох" :governs :indexed-key :checked-by :graph}
    {:id :key/data :says "колонка даних — значення атрибута, і вона ніколи не ключує індекси двох різних таблиць" :governs :component :checked-by :graph}
    {:id :key/enum-space :says "простір-перелічення — простір ключів без таблиці первинного ключа: сторона власника несе колонку даних, сторона посилання — колонку з суфіксом посилання" :governs :indexed-key :checked-by :graph}
    {:id :key/self-index :says "self-index дозволений, коли кожен рядок із цією колонкою даних належить одній таблиці, а значення пошуку може прийти ззовні; колонка стану чи виду — законний його ключ; той самий тип даних, індексований у двох таблицях, розділяється на пару первинного і зовнішнього ключа" :governs :component-index :checked-by :agent}
    {:id :key/pk-single-carrier :says "первинний ключ, який несуть кілька архетипів, — порушення: чужі носії мусять перейти на колонку посилання" :governs :indexed-key :checked-by :graph}
    {:id :key/fk-value-parity :says "зовнішній ключ, який шукають в індексі, мусить нести те саме значення, що й ключ власника — розбіжність типу значення є порушенням" :governs :component-index :checked-by :graph}
    {:id :key/fk-home :says "колонка посилання лежить у папці фічі власника поруч із первинним ключем — один простір, одна пара типів, визначена раз" :governs :role-folder :checked-by :agent}
    {:id :key/pk-compare-only :says "первинний ключ, який лише порівнюють і ніколи не індексують, лишається простим компонентом із рівністю" :governs :component :checked-by :agent}
    {:id :index/maintained-by-write :says "індекс підтримує виклик запису: запис перекладає рядок, видалення сутності прибирає його" :governs :component-index :checked-by :agent}
    {:id :index/pk-uniqueness :says "дубль значення первинного ключа повертає обидва рядки без помилки, тому унікальність перевіряється в місці виділення ключа і там же кидає" :governs :component-index :checked-by :graph}
    {:id :index/bucket-cap :says "на однакове значення ключа припадає не більше ста сутностей — вставка й видалення лінійні по дублях" :governs :component-index :checked-by :agent :numbers {:entities-per-key-value "не більше 100"}}
    {:id :index/one-fk-per-space :says "сутність тримає один компонент на тип, тому має не більше одного зовнішнього ключа в простір; два посилання вимагають власної пари типів і є рішенням дизайну" :governs :component-index :checked-by :graph}
    {:id :index/key-equality :says "компонент-ключ оголошує контракт індексованого компонента і повертає значення ключа своїм методом: перелічення працює прямо, struct-ключ сам реалізує рівність і хеш" :governs :indexed-key :checked-by :graph}]})
```

## Компоненти, записи, посилання

```clojure
(def rules-components
  {:section :components-writes-links
   :does "форма компонента, єдиний шлях запису і форма стійкого посилання"
   :rules
   [{:id :component/is :says "компонент — проста struct runtime-значень без поведінки й методів, крім рівності, коли він ключ таблиці; у папці компонентів немає ні логіки, ні побічних ефектів" :governs :component :checked-by :graph}
    {:id :component/declare :says "компонент оголошується структурою з контрактом компонента — невидимий рушію компонент мовчки нічого не робить при народженні" :governs :component :checked-by :graph}
    {:id :write/upsert :says "запис колонки — виклик додавання компонента зі значенням, тобто upsert; за повнотою народження це завжди простий запис значення" :governs :component :checked-by :graph}
    {:id :write/no-ref-mutation :says "ніколи мутація через посилання у сховище компонентів і ніколи читання компонента як дескриптора для мутації — індекс перекладає рядок лише на виклик запису" :governs :component-index :checked-by :agent}
    {:id :write/change-only :says "спершу порівняти, писати при відмінності — перезапис того самого значення даремно перекладає індексовані рядки" :governs :component :checked-by :agent}
    {:id :link/persistent-key :says "стійке посилання — доменний ключ: колонка ідентичності власника, на яку посилається колонка посилання цього простору, ніколи збережений дескриптор сутності" :governs :indexed-key :checked-by :graph}
    {:id :link/runtime-only :says "лише runtime-посилання — не серіалізоване, звʼязане часом життя, зазвичай шар view — може тримати пряме посилання або дескриптор сутності" :governs :view :checked-by :agent}
    {:id :link/miss-throws :says "пошук за ключем, що промахнувся, кидає виняток" :governs :component-index :checked-by :agent}]})
```

## Події

```clojure
(def rules-events
  {:section :events
   :does "життєвий цикл однокадрової події: підняття, дозрівання, споживання, прибирання"
   :rules
   [{:id :event/is :says "подія — однокадрова struct, піднята на власній сутності; її поля — звичайні дані, значення, потрібні споживачу" :governs :event :checked-by :graph}
    {:id :event/declare :says "подія оголошується структурою з контрактом компонента, з суфіксом події, у папці подій своєї фічі" :governs :event :checked-by :graph}
    {:id :event/raise :says "подія піднімається одним викликом створення події на сховищі, який штампує кадр і складає архетип події; імпульс, зібраний руками, губить тег або штамп, ніколи не дозріває і тече" :governs :event :checked-by :graph}
    {:id :event/ripe :says "споживач діє лише поки подія дозріла — у кадрі після народження; перевірка дозрілості стоїть першим рядком обробки, інакше система відпрацює двічі" :governs :event :checked-by :agent :numbers {:event-ripeness "1 кадр: споживач діє в кадрі після народження"}}
    {:id :event/anchor :says "споживач якориться на архетипі самої події і тримає його — доки події нема, він коштує нуль; значення береться з компонента події на сутності імпульсу" :governs :event-archetype :checked-by :graph}
    {:id :event/reaction :says "реакція — або діяти прямо на значеннях події, або reconcile: зібрати поточну множину зі стану світу, порівняти й діяти на різниці, ідемпотентно" :governs :event :checked-by :agent :kind :choice}
    {:id :event/no-same-frame :says "ніколи не розраховувати на реакцію в тому ж кадрі — ланцюг подій коштує кадр на ланку, і видимість не залежить від пріоритету" :governs :event :checked-by :agent :numbers {:event-chain-cost "1 кадр на ланку"}}
    {:id :event/dormant-consumer :says "емітер може зʼявитися пізніше: споживача можна зібрати першим як сплячий каркас" :governs :event :checked-by :agent :kind :choice}
    {:id :event/no-change-observers :says "ніколи спостерігач додавання компонента, видалення компонента, зміни тегів чи будь-який спостерігач зміни значення — натомість подія поруч із записом; прийняти спостерігача можна лише рішенням для всього проєкту" :governs :entity-store :checked-by :agent}
    {:id :event/cleanup :says "прибирає події одна глобальна система: працює останньою в тіку, видаляє кожну дозрілу сутність із тегом події й не має нащадків — власного прибирання для окремої події не буває" :governs :cleanup :checked-by :graph :numbers {:cleanup-priority "найбільше можливе ціле"}}
    {:id :event/no-tag-on-persistent-row :says "стійка сутність даних ніколи не несе тег події" :governs :event :checked-by :graph}
    {:id :event/one-way :says "ніколи петля заповнення - команда - заповнення на однокадрових подіях: дані течуть в один бік" :governs :event :checked-by :agent}
    {:id :event/lossy-producer :says "виробник, що не контролює вікно кадру — фаза ходу чи async-робота, — дзвонить за рівнем: перепіднімає подію кожен хід або тік, поки умова тримається, а споживач звіряється зі станом і не довіряє одній доставці" :governs :event :checked-by :agent}
    {:id :event/startup-bulk :says "стартова масова робота — стадія конвеєра, ніколи подія: однокадрові події не переживають async-конвеєр створення мапи" :governs :pipeline-base :checked-by :agent :kind :choice}
    {:id :event/suffix :says "тип події має суфікс події; довший легасі-суфікс із словом компонент закритий — його несе рівно один живий тип і жоден новий" :governs :event :checked-by :graph}]})
```

## Частина 2 — Системи і виконання

Хто виконує роботу, у якій формі й за якої каденції. Шість розділів: що таке система і як
вирішується її роль, заборона стану, памʼять, три runtime-форми, разові форми, transaction-сутність.

## Системи, ролі, маркери, порядок

```clojure
(def rules-systems
  {:section :systems-and-roles
   :does "що таке система, як вирішується її роль, коли потрібен маркер, звідки береться порядок виконання і коли система ділиться"
   :rules
   [{:id :system/definition :says "система — неабстрактний клас, який веде рушій: прямо чи транзитивно UpdatedSystem, LateUpdatedSystem, IUpdatedSystem, ILateUpdatedSystem, IUniTaskSystem, IPrioritizedUniTaskSystem, ConfigLoaderSystem<T> або EventCleanupSystem" :governs :system :checked-by :arch-check}
    {:id :system/subsystem-is-not-a-system :says "член сімейства, зібраного DI, — простий обʼєкт із IDisposable, яким володіє система: заборона стану й заборона керованих колекцій його не вʼяжуть" :governs :subsystem :checked-by :arch-check :note "роль sub_system у графі — ярлик членства в сімействі, не системність"}
    {:id :system/cadence :says "каденція читається зі стадії, яку система обслуговує, а не з інтерфейсу: разова — крок старту, loader конфіга, стадія чи підсистема створення мапи; повторна — per-frame, reactive на кожну подію, фаза ходу на кожен хід" :governs :cadence :checked-by :arch-check}
    {:id :system/role-order :says "роль неабстрактного класу бере першу істинну гілку: обхід подій з видаленням → cleanup; подієвий якір у base(...) → reactive; Update-цикл і утримуваний архетип події → маркер; Update-цикл → per_frame; аргумент стадії створення мапи → pipeline_stage; предок TurnPhaseSubSystem → turn_phase; негенеричний IUniTaskSystem → startup_step; абстрактний предок, якого хтось збирає → sub_system; інакше ролі нема" :governs :system-role :checked-by :graph}
    {:id :system/marker-required :says "маркер ролі обовʼязковий рівно в одній формі, якої порядок не вирішує: Update-клас тримає архетип події поза base(...)" :governs :role-marker :checked-by :analyzer :note "граф сьогодні лише попереджає; правило вимагає помилки компіляції"}
    {:id :system/marker-forbidden :says "маркер ролі на класі, чию роль вирішує форма, заборонений" :governs :role-marker :checked-by :analyzer :note "зайвий маркер сьогодні лише попередження; за рішенням власника це помилка"}
    {:id :system/marker-value :says "значення маркера мусить збігатися з формою класу: PerFrame вимагає контракту циклу без подієвого якоря, Reactive вимагає контракту циклу без табличного якоря плюс подієвий якір або утримуваний архетип події" :governs :role-marker :checked-by :analyzer :note "аналізатор міряє значення маркера; читач ролей у графі дозволяє на табличному якорі будь-яке значення"}
    {:id :system/marker-not-inherited :says "жоден маркер не успадковується — кожен конкретний клас чи структура несе свій" :governs :role-marker :checked-by :analyzer}
    {:id :system/marker-vocabulary-parity :says "копія перелічення ролей в аналізаторі мусить збігатися з перелічуванням у EcsExtensions — аргумент атрибута приходить як базовий int" :governs :marker-vocabulary :checked-by :none :enforcement :none-today}
    {:id :system/base-choice :says "базу обирає спосіб запуску: разовий крок старту — IUniTaskSystem; упорядкований конвеєр — IPrioritizedUniTaskSystem<T> зі стадією в аргументі, а коли сімейство має абстрактну базу — її; робота після кожного Update — LateUpdatedSystem; інакше UpdatedSystem" :governs :system :checked-by :agent :kind :choice}
    {:id :system/priority-source :says "Priority повертає іменовану const int із тримача пріоритетів проєкту — ніколи локальну константу класу й ніколи літерал" :governs :priority :checked-by :graph :note "інструмент приймає будь-яку розвʼязну const int, отже перевіряє слабше за правило"}
    {:id :system/priority-space :says "вкладений клас тримача пріоритетів — окремий простір порядку: значення порівнюються лише всередині простору, кожен член простору має власне значення, а пріоритети підсистем порівнюються лише всередині свого оркестратора" :governs :priority :checked-by :agent}
    {:id :system/priority-is-order-only :says "пріоритет задає лише порядок виконання — ніколи правило, що споживач стоїть вище чи нижче виробника" :governs :priority :checked-by :agent}
    {:id :system/query-caches-in-constructor :says "система тримає свій архетип, запит та індекс у readonly-полі, розвʼязаному раз у конструкторі" :governs :system :checked-by :arch-check}
    {:id :system/driven-by :says "per-frame систему веде або робоча таблиця — оголошений архетип, який вона обробляє, — або якір тіку, singleton-архетип, присутність якого тік вмикає; обʼєкт запиту — лише для справді міжархетипної множини" :governs :archetype :checked-by :agent :kind :choice}
    {:id :system/split-when :says "система ділиться, коли вона створює і знищує той самий вид вмісту, щокадру порівнює стан світу, тримає більше двох непов'язаних сімейств запитів або працює в кількох станах гри з різних причин" :governs :system :checked-by :agent :kind :choice}
    {:id :system/split-into :says "стартовий обсяг іде в разову стадію чи підсистему створення мапи, runtime-обовʼязок — в одну reactive-систему на відповідальність, спільне обчислення — у stateless helper" :governs :system :checked-by :agent :kind :choice}
    {:id :system/simplest-structure :says "код набуває найпростішої структури, що розвʼязує задачу: патерн зʼявляється лише коли його вимагають кардинальність або справжня складність, ніколи тому, що його згадує документ чи коментар" :governs :system :checked-by :agent :kind :choice}]})
```

## Стан системи

```clojure
(def rules-system-state
  {:section :system-state
   :does "заборона стану екземпляра, що ним є і що ним не є, і три виходи з заборони"
   :rules
   [{:id :state/no-instance-state :says "система не тримає змінного стану екземпляра" :governs :system :checked-by :arch-check}
    {:id :state/is-state :says "стан — будь-яке переприсвоюване поле, readonly-поле змінного контейнера чи буфера, чий вміст змінюється між кадрами, і settable авто-властивість екземпляра" :governs :system :checked-by :arch-check}
    {:id :state/is-not-state :says "не стан — readonly незмінні залежності й дескриптори сховища, кеші запитів, розвʼязані раз у конструкторі, const і static readonly" :governs :system :checked-by :arch-check}
    {:id :state/mutable-static :says "змінне static-поле в системі — стан, бо перелік не-стану закритий і містить лише const і static readonly" :governs :system :checked-by :arch-check :note "детектор не позначає жодного static-поля — прогалина інструмента, не дозвіл"}
    {:id :state/default-home :says "прапорець статусу, маркер роботи в процесі, лічильник прогресу, біт запущено чи завершено і дескриптор оброблюваного живуть на сутності або в однопримірниковому стані поза системою" :governs :system :checked-by :agent}
    {:id :state/escape-order :says "вихід із заборони береться по порядку: спершу перенести стан у компонент чи однопримірниковий стан, потім коробка кадрів, і лише потім позначене поле" :governs :state-marker :checked-by :arch-check}
    {:id :state/frame-box-use :says "значення на обмежену кількість кадрів живе в коробці кадрів: Value читається лише після Exist, коробка звільняється при знятті, а в async-системі коробка заборонена — await перетинає кадри й вона застаріває" :governs :frame-box :checked-by :agent}
    {:id :state/frame-box-field :says "поле коробки кадрів у системі — стан і несе позначення з причиною: сама коробка — змінна struct у переприсвоюваному полі" :governs :frame-box :checked-by :arch-check}
    {:id :state/allowed-reason :says "позначення дозволеного стану завжди несе причину, хоча тип і дозволяє викликати його без аргументу" :governs :state-marker :checked-by :none :enforcement :none-today}
    {:id :state/lifetime-flags :says "сторож підписки, сторож повторного входу, сторож звільнення і поле дескриптора ассета в системі — стан: кожне таке поле несе позначення з причиною" :governs :system :checked-by :arch-check}
    {:id :state/preupdate-cache :says "кеш кадру, розвʼязаний у PreUpdate, — стан: або позначене поле з причиною, або рефакторинг, і вибір робить власник" :governs :system :checked-by :arch-check}
    {:id :state/injected-collection :says "поле списку, зібраного DI, у системі несе позначення дозволеного стану" :governs :subsystem :checked-by :arch-check}]})
```

## Колекції й памʼять

```clojure
(def rules-memory
  {:section :collections-and-memory
   :does "де керовані колекції дозволені, що таке нульове виділення на повторному шляху і як обирається native-алокатор"
   :rules
   [{:id :alloc/view-exempt :says "код шару view може використовувати керовані колекції — view не система" :governs :view :checked-by :arch-check}
    {:id :alloc/one-shot-exempt :says "система разової каденції може використовувати керовані колекції" :governs :cadence :checked-by :arch-check}
    {:id :alloc/repeated-zero :says "система повторної каденції працює з нульовим виділенням: памʼять, узята на початку прогону, звільняється в його кінці, а прогін тримається на native-контейнерах, структурах і спанах" :governs :cadence :checked-by :arch-check}
    {:id :alloc/banned-generic-types :says "у повторній системі заборонені і директива імпорту керованих колекцій, і повні імена їхніх типів — списки, словники, множини, черги, стеки, впорядковані сімейства та їхні інтерфейси; масив платформи, спани й негенеричні колекції ця заборона не стосується" :governs :managed-collection :checked-by :arch-check}
    {:id :alloc/managed-element-exception :says "елементи керованого типу — обʼєкти сцени чи посилання на view — живуть у керованій колекції, виділеній раз при побудові власника й утримуваній весь його час життя" :governs :managed-collection :checked-by :arch-check :note "виняток знімає лише каденцію виділення; поле лишається станом і несе позначення"}
    {:id :alloc/enum-not-a-native-key :says "перелічення не може бути ключем native-множини чи native-словника — ключем іде базовий цілий тип, а значенням перелічення придатне" :governs :native-container :checked-by :agent}
    {:id :alloc/by-lifetime :says "алокатор обирає час життя: у межах одного кадру — тимчасовий кадровий, у межах чотирьох кадрів — тимчасовий задачний, довше — постійний або керована колекція, яку звільняє один названий власник" :governs :allocator :checked-by :arch-check :numbers {:temp-allocator-lifetime "1 кадр" :tempjob-allocator-lifetime "4 кадри"}}
    {:id :alloc/no-temp-in-async :says "async-система ніколи не бере кадровий тимчасовий алокатор, і така памʼять ніколи не лежить у полі й не живе через await" :governs :allocator :checked-by :arch-check}
    {:id :alloc/temp-scope :says "кадровий тимчасовий алокатор живе або в блоці кадру на головному потоці, або в блоці задачі всередині job — більше ніде, зокрема не в коді, відданому пулу потоків" :governs :allocator :checked-by :arch-check :enforcement :weaker-today}
    {:id :alloc/breach-is-an-error :says "порушення часу життя алокатора — помилка, не рекомендація" :governs :allocator :checked-by :arch-check}
    {:id :alloc/no-per-run-garbage :says "на повторному шляху не створюється новий масив і не викликаються матеріалізатори послідовностей — жодної щойно виділеної керованої колекції на прогін" :governs :cadence :checked-by :arch-check}
    {:id :alloc/no-handed-out-snapshot :says "система не будує й не віддає керований знімок: дані йдуть у споживача по одному значенню або посиланням, бо інакше час життя не названо" :governs :system :checked-by :arch-check}
    {:id :alloc/named-owner :says "кожне виділення має названого власника, що його звільняє, і інваріант — детерміноване звільнення, а не тип контейнера" :governs :allocator :checked-by :arch-check}
    {:id :alloc/not-by-type :says "не порушення: спани, перелічення сутностей архетипу, запиту чи індексу, читання колекції, виставленої конфігом або компонентом, native-контейнери й позначені поля — керований буфер, виділений раз і детерміновано звільнений, проходить" :governs :managed-collection :checked-by :arch-check}
    {:id :alloc/binds-the-path :says "заборона виділення на прогін вʼяже повторний ШЛЯХ виконання — цикл, тіло Update, будь-який helper чи підсистема цього шляху, — а не лише клас системи" :governs :cadence :checked-by :arch-check}]})
```

## Reactive, per-frame, оркестратор

```clojure
(def rules-runtime-forms
  {:section :reactive-perframe-orchestrator
   :does "три форми runtime-логіки і сімейство підсистем, зібране DI"
   :rules
   [{:id :reactive/default :says "reactive — типовий вибір для runtime-логіки і єдиний реактивний механізм проєкту" :governs :system :checked-by :agent :kind :choice}
    {:id :reactive/shape :says "reactive-система — запечатаний нащадок Update-бази, чиї залежності, індекси й архетипи розвʼязані в конструкторі, чий якір у базовому виклику — архетип події, а тіло Update починається з перевірки дозрілості, далі йдуть сторожі передумов, що кидають, і дія лише на різниці" :governs :system :checked-by :agent}
    {:id :perframe/only-when :says "per-frame береться лише коли логіка справді неперервна і не може бути реактивною — рух камери, проєкція щокадру, опитування вводу, стеження за виділенням; треба вміти сказати, чому імпульс не замінить тік" :governs :system :checked-by :agent :kind :choice}
    {:id :perframe/shape :says "per-frame система — запечатаний нащадок Update-бази, чий базовий виклик бере світ і робочу таблицю з тримача, а тіло Update не лишає полів між кадрами" :governs :system :checked-by :agent}
    {:id :perframe/late-update :says "пізня Update-база береться, коли треба бачити остаточний стан кадру — після камери й ігрових записів" :governs :system :checked-by :agent :kind :choice}
    {:id :orchestrator/when :says "сімейство оркестратор плюс підсистеми береться, коли є одна база з реалізацією на фічу, коли стадія має незалежно впорядковані частини або коли обробка однієї події завелика для одного тіла Update; вміщується в одне тіло — це звичайна система" :governs :subsystem :checked-by :agent :kind :choice}
    {:id :orchestrator/base-shape :says "база підсистеми — абстрактний клас із контрактом звільнення: захищене readonly-сховище з конструктора, вимикач, абстрактний пріоритет, абстрактна операція сімейства і віртуальне звільнення" :governs :subsystem :checked-by :agent}
    {:id :orchestrator/concrete-shape :says "конкретна підсистема запечатана, бере реєстр сховищ, а не голе сховище світу, і розвʼязує власні архетипи та індекси у конструкторі" :governs :subsystem :checked-by :agent}
    {:id :orchestrator/dispatch :says "оркестратор упорядковує сімейство раз у конструкторі за пріоритетом і на прогоні викликає лише ввімкнені, по порядку" :governs :subsystem :checked-by :agent}
    {:id :orchestrator/no-domain-logic :says "оркестратор не містить доменної логіки — сортує, пропускає вимкнені, запускає; уся робота живе в підсистемах" :governs :subsystem :checked-by :agent}
    {:id :orchestrator/query-caches :says "спільні запити й Try-helper живуть у базі сімейства, власні — у кожній підсистемі; кеші запитів належать сховищу, тому звільняти в підсистемі нема чого, а її звільнення прибирає лише те, що вона сама виділила" :governs :subsystem :checked-by :agent}
    {:id :orchestrator/routing :says "маршрутизація сімейства — спроба обробки на кожній підсистемі, перший збіг виграє, жодного збігу — кинути" :governs :subsystem :checked-by :agent}
    {:id :orchestrator/empty-family :says "поки підсистем нема, оркестратор лишається порожньою оболонкою з якорем і без інжекції списку: контейнер кидає на порожню колекцію, тому список і цикл зʼявляються з першою підсистемою" :governs :subsystem :checked-by :agent}
    {:id :orchestrator/ripe-once :says "дозрілість перевіряє оркестратор один раз перед розсиланням — підсистема не перевіряє її повторно" :governs :subsystem :checked-by :agent}
    {:id :structural/update-is-safe :says "структурні зміни в тілі Update безпечні, бо база вже зняла знімок якірного архетипу; у переліченні, яке система чи підсистема відкриває сама, вони заборонені" :governs :update-base :checked-by :agent}]})
```

## Разові системи, конвеєр, фаза ходу

```clojure
(def rules-one-shot
  {:section :one-shot-pipeline-turn
   :does "форми, що працюють раз або по стадіях, і їхні передумови"
   :rules
   [{:id :oneshot/startup-step :says "разовий крок старту реалізує негенеричний async-контракт: виконання з токеном плюс прапорець кроку застосунку, на якому його запускають" :governs :one-shot-base :checked-by :agent}
    {:id :pipeline/stage :says "стадія конвеєра створення мапи — разове async-будування світу: реалізує впорядкований async-контракт зі стадією створення мапи в аргументі типу і працює раз" :governs :pipeline-base :checked-by :graph}
    {:id :pipeline/shape :says "стадія запечатана й внутрішня, тримає readonly реєстр сховищ, сховище і архетипи таблиць, на вході перевіряє свою передумову і кидає на її відсутність, кидає на скасування, народжує рядки через архетип і звільняє власні дескриптори у своєму звільненні" :governs :pipeline-base :checked-by :agent}
    {:id :pipeline/order :says "стадії йдуть послідовно за зростанням пріоритету з очікуванням: стадія може покладатися на все, що зробили менші пріоритети, а відсутню передумову кидає" :governs :pipeline-base :checked-by :agent :numbers {:pipeline-priority-band "приблизно 100-900 з кроком приблизно 100"}}
    {:id :pipeline/re-entry :says "сторож повторного входу зʼявляється лише там, де регенерація може повторно увійти в стадію — інакше стадія знищує й створює заново" :governs :pipeline-base :checked-by :agent :kind :choice}
    {:id :pipeline/addressable-handle :says "дескриптор завантаженого ассета стадія утримує і звільняє у своєму звільненні" :governs :box :checked-by :agent}
    {:id :pipeline/singleton-view :says "одиничний view публікується для споживачів як компонент із суфіксом view" :governs :singleton-manifest :checked-by :agent}
    {:id :pipeline/singleton-non-queried :says "одиничні дані, які ніхто не запитує, живуть однопримірниковим компонентом" :governs :singleton-manifest :checked-by :agent}
    {:id :turn-phase/base :says "фаза ходу наслідує базу фаз ходу, аргумент типу якої називає стадію ходу, і має повторну каденцію — кожен хід" :governs :turn-phase-base :checked-by :graph}]})
```

## Transaction-сутність

```clojure
(def rules-transaction
  {:section :transaction
   :does "багатокрокова поведінка, що охоплює кілька піддоменів, і її єдиний дім"
   :rules
   [{:id :transaction/when :says "transaction-сутність береться, коли піддоменів два або більше І поведінка багатокрокова — між відкривальним і фіксувальним імпульсом результат ще може змінитися, отже є сесійний стан, яким хтось має володіти; два піддомени й один імпульс — це reactive-система" :governs :archetype :checked-by :agent :kind :choice}
    {:id :transaction/degenerate :says "коли між відкриттям і фіксацією нема чим володіти — ресурс не витрачено, прев'ю на мапі нема, вибір є чистим станом інтерфейсу, — фіксація робиться прямо на підтверджувальному імпульсі, без стадій чернетки" :governs :archetype :checked-by :agent :kind :choice}
    {:id :transaction/one-home :says "багатокрокова поведінка між піддоменами має рівно один дім — transaction-сутність у домені-дієслові, який володіє і сутністю, і кожним записом у неї; ніколи підкладковий домен і ніколи presentation" :governs :archetype :checked-by :graph}
    {:id :transaction/one-archetype :says "одна архетипна форма на всю сесію з усіма колонками, включно з колонкою стадії: головний тег не змінюється, поруч стоїть мітка ролі transaction, а стадія з іншим складом означає видалити рядок і створити новий у її архетипі з перенесенням ключів" :governs :archetype :checked-by :graph}
    {:id :transaction/lifecycle :says "відкриття робить reactive-система домену-дієслова, створюючи рядок архетипом і пишучи ідентифікацію, сесійний стан і стадію; зміну несе командна подія зі значеннями команди, яку споживає лише reactive-система того ж домену; фіксація — запис колонки стадії; завершення — ФАКТ-рядок у таблиці підкладкового домену плюс подія, і споживачі читають таблицю фактів" :governs :archetype :checked-by :agent}
    {:id :transaction/single-entity-state :says "увесь стан транзакції живе на ОДНІЙ сутності: ні копії в однопримірниковому компоненті, ні другого дому, ні одного логічного стану, продубльованого по піддоменах і синхронізованого подіями" :governs :archetype :checked-by :agent}
    {:id :transaction/no-substrate-state :says "ніколи не класти стан у підкладковий домен заради того, щоб його прочитав інший шар; чистий стан вибору в інтерфейсі лишається в presentation" :governs :layer :checked-by :agent}
    {:id :transaction/ui-projection :says "інтерфейс — проєкція: його системи читають transaction-сутність прямо і звіряються ідемпотентно, ввід гравця йде командними імпульсами, ніколи записами, view живить систему локальною подією і сам імпульсу не піднімає, інтерфейс стану транзакції не тримає, а показує факти або живу сутність, ніколи копію-знімок" :governs :view :checked-by :agent}]})
```

## Частина 3 — Межі

Межі, за які код не виходить: view, авторські дані, потік, збірка. Чотири розділи: шар view і його
межа, конфіги й addressables, головний потік, розміщення коду.

## View і межа view

```clojure
(def rules-view
  {:section :view
   :does "що таке шар view, що йому заборонено і як він спілкується зі своєю системою"
   :rules
   [{:id :view/layer :says "шар view — усе, що оголошено в папці view; партнер view-системи, тобто тип, який піднімає локальну подію і якого називає маркер підписника, додатково є нащадком базового класу обʼєкта сцени" :governs :view :checked-by :graph}
    {:id :view/boundary :says "view ніколи не створює сутність, ніколи не піднімає ECS-подію і ніколи не отримує реєстр сховищ чи сховище сутностей — ні полем, ні параметром конструктора, ні параметром методу інʼєкції або конструювання" :governs :view :checked-by :graph :enforcement :weaker-today :note "правило без помилки компіляції; граф називає відхилення"}
    {:id :view/inward :says "усередину: view піднімає локальну подію мови, а керівна система чи підсистема підписується на неї прямо — view не знає сховища" :governs :view :checked-by :graph}
    {:id :view/outward :says "назовні: система штовхає у view по ОДНОМУ значенню — ніколи побудований у системі список чи масив" :governs :view :checked-by :arch-check}
    {:id :view/ecs-only-across-a-boundary :says "ECS-імпульс між view і системою доречний, лише коли сигнал перетинає межу кадру або межу збірки, якої прямий виклик не досягає, і піднімає його СИСТЕМА" :governs :event :checked-by :agent :kind :choice}
    {:id :view/subscriber-marker :says "клас, що власним додаванням обробника підписується на подію view, несе маркер підписника з типом цього view; маркер без справжньої підписки, як і підписка без маркера, — порушення, а названий тип мусить бути нащадком базового класу обʼєкта сцени" :governs :view-marker :checked-by :analyzer}
    {:id :view/subscribe-once :says "система підписується на view один раз під сторожем — view живе довше — і відписується у своєму звільненні" :governs :view :checked-by :agent}
    {:id :view/handler-synchronous :says "обробник події view працює синхронно в колбеку інтерфейсу на головному потоці: спершу запис у сховище, потім штовхання назад у view — ніколи відкладання запису на наступний тік прапорцем" :governs :view :checked-by :agent}
    {:id :view/shape :says "view — запечатаний нащадок базового класу обʼєкта сцени з публічними подіями мови, піднятими на взаємодії, і методами встановлення значень, що привʼязують елементи інтерфейсу" :governs :view :checked-by :agent}
    {:id :view/subscription-visible :says "кожна підписка видна у підписника — пошук по імені події дає всіх слухачів" :governs :view :checked-by :agent}
    {:id :view/no-business-logic :says "у папці view немає бізнес-логіки, і сама папка існує лише в presentation" :governs :role-folder :checked-by :agent}]})
```

## Конфіги й addressables

```clojure
(def rules-configs
  {:section :configs-and-addressables
   :does "авторські дані, їхнє завантаження і володіння дескриптором завантаженого ассета"
   :rules
   [{:id :config/is :says "конфіг — ассет авторських даних, що живе в реєстрі сховищ за своїм типом; читання кидає, коли конфіг не завантажено" :governs :config :checked-by :agent}
    {:id :config/shape :says "тип конфіга — запечатаний нащадок базового ассета даних із атрибутом створення ассета, приватними серіалізованими полями і властивостями лише для читання" :governs :config :checked-by :agent}
    {:id :config/validate :says "тип конфіга реалізує контракт валідації тоді, коли авторські дані можуть бути хибними; валідація кидає на кожне порушення авторингу, викликається одразу після завантаження і живе в ассеті, ніколи в системі, а споживач перевіряє ще раз" :governs :validatable :checked-by :agent :kind :choice}
    {:id :config/container :says "контейнер-каталог — ассет даних із масивом під-конфігів того самого абстрактного типу" :governs :config :checked-by :graph}
    {:id :config/key-is-the-type :says "ключ сховища конфігів — тип: один збережений примірник на тип, а два ассети однієї форми вимагають абстрактної бази й окремого запечатаного нащадка на кожен" :governs :config :checked-by :agent}
    {:id :config/read-the-asset :says "читається сам ассет — без копії, без сплющення, без сторожа присутності й тихого пропуску навколо читання" :governs :config :checked-by :agent}
    {:id :config/lifetime :says "конфіг завантажується раз на кроці завантаження конфігів і ніколи не звільняється: loader тримає лише значення дескриптора і сам дескриптор не звільняє" :governs :config-loader :checked-by :agent}
    {:id :config/immutable :says "конфіг ніколи не мутується, а його масиви спільні — лише читання, без захисного клонування" :governs :config :checked-by :agent}
    {:id :config/derived-objects :says "обʼєкти, збудовані з конфіга, належать системі кроку побудови примірників: цей крок може будувати runtime-однопримірникові компоненти й обʼєкти, але не може вантажити ассети й не виконує ігрової чи покадрової логіки" :governs :app-state :checked-by :agent}
    {:id :config/off-thread-read :says "читати поля конфіга поза головним потоком дозволено — це прості дані" :governs :config :checked-by :agent}
    {:id :config/loader :says "loader конфіга — лише узагальнена системa завантаження: вона вантажить ассет, кидає при невдачі, валідує, кладе в сховище і ніколи не звільняє; ніколи рукописний loader, нащадок на конфіг, компонент-конфіг чи таблиця сутностей для запитів конфігів" :governs :config-loader :checked-by :graph}
    {:id :config/address-constant :says "адреса addressable — константа у тримачі адрес, у верхньому регістрі через підкреслення, названа за типом конфіга, значення якої дорівнює імені запису в Unity; передається константа, ніколи рядковий літерал і ніколи імʼя типу" :governs :config-address :checked-by :agent}
    {:id :config/instance-objects-step :says "система похідних обʼєктів — запечатаний разовий крок, що бере свій прапорець стану в конструкторі, читає конфіг і пише похідний однопримірниковий компонент" :governs :one-shot-base :checked-by :agent}
    {:id :addressable/box-decides-release :says "звільнення вирішує коробка дескриптора, не статус: якщо всередині щось є, рівно один власник звільняє її, інакше витік" :governs :box :checked-by :agent}
    {:id :addressable/empty-box :says "невдалий статус означає порожню коробку, а звільнення порожньої чи типової коробки нічого не робить — безумовне звільнення у блоці завершення безпечне" :governs :box :checked-by :agent}
    {:id :addressable/instance-release :says "звільнення завантаженого примірника саме знищує обʼєкт сцени — ніколи знищувати його руками" :governs :addressable :checked-by :agent}
    {:id :addressable/no-prefab-loadasync :says "prefab вантажиться лише викликом завантаження з інстанціюванням; узагальнене завантаження обʼєкта сцени захищено і повертає невдачу" :governs :addressable :checked-by :agent}
    {:id :addressable/cancel-throws :says "скасування — виняток, а не статус: коробка до викликача не доходить, після власних подальших очікувань кидається на токен, а утримувані коробки звільняються у блоці завершення" :governs :addressable :checked-by :agent}
    {:id :addressable/one-owner :says "у коробки рівно один власник, а звільнене поле-коробка замінюється на порожню" :governs :box :checked-by :agent}
    {:id :addressable/ctor-injection :says "контракт завантаження інжектується лише через конструктор, а рідний API пакета поза його реалізацією не чіпають" :governs :addressable :checked-by :agent}
    {:id :addressable/box-field-disposable :says "клас, що зберігає поле-коробку, реалізує контракт звільнення або інакше маршрутизує звільнення" :governs :box :checked-by :agent}
    {:id :addressable/load-failure :says "невдале завантаження не-сценового ассета кидає виняток з адресою; успішний дескриптор утримується, а при знятті звільняється й скидається в порожній" :governs :addressable :checked-by :agent}
    {:id :addressable/missing-component :says "prefab без потрібного компонента звільняє коробку і кидає; типізований компонент повертається обгорнутою коробкою з каскадним звільненням" :governs :box :checked-by :agent}
    {:id :addressable/sequential-rollback :says "послідовні завантаження йдуть з відкатом: локальні порожні коробки, завантаження в захищеному блоці, фіксація в поля, скидання локальних, а в блоці завершення — звільнення кожної локальної" :governs :box :checked-by :agent}
    {:id :addressable/field-release :says "звільнення полів іде під сторожем звільненості через helper, що звільняє коробку і скидає її в порожню" :governs :box :checked-by :agent}
    {:id :addressable/no-detached-value :says "значення з коробки не зберігається окремо від неї — звільнена коробка лишає збережене значення висячим" :governs :box :checked-by :agent}
    {:id :addressable/check-before-read :says "значення читається лише після перевірки присутності в коробці" :governs :box :checked-by :agent}]})
```

## Потік і структурні зміни

```clojure
(def rules-threading
  {:section :threading-and-structural
   :does "головний потік як межа сховища і безпечна ідіома структурної зміни"
   :rules
   [{:id :thread/main-only :says "кожен виклик сховища — лише головний потік: створення, запис, читання, видалення, пошук в індексі, підняття події" :governs :entity-store :checked-by :agent}
    {:id :thread/off-thread-plain-data :says "поза головним потоком — лише обчислення над простими даними й native-контейнерами, і читання полів конфіга сюди входить" :governs :entity-store :checked-by :agent}
    {:id :thread/bridge :says "перед першим викликом сховища робиться перехід на головний потік, а подальші обчислення повертаються в пул; перехід на головний потік продовжується на наступному Update, тобто коштує близько кадру" :governs :entity-store :checked-by :agent}
    {:id :thread/no-off-thread-write :says "ніколи запис у сховище поза головним потоком — індексована колонка перекладає рядок на запис і псує індекс" :governs :entity-store :checked-by :agent}
    {:id :structural/enumeration-throws :says "структурна зміна всередині перелічення кидає — так само, як додавання колонки, яку архетип уже має, і як зміна непов'язаної сутності; сторож стоїть на все сховище" :governs :structural-change :checked-by :agent}
    {:id :structural/delete :says "видалення сутності не структурна зміна, але воно змінює перелічувану множину" :governs :structural-change :checked-by :agent}
    {:id :structural/idiom :says "ідіома безпечної зміни: зібрати ідентифікатори сутностей у native-список, закрити перелічення, знову взяти кожну сутність за ідентифікатором зі сховища і створювати й писати в другому проході" :governs :structural-change :checked-by :agent}
    {:id :structural/no-entity-snapshot :says "ніколи знімок значень сутностей — дескриптор сутності не є некерованим типом" :governs :structural-change :checked-by :agent}]})
```

## Розміщення: шари, залежності, папки

```clojure
(def rules-placement
  {:section :placement
   :does "де живе новий код, на що йому можна посилатися і яку роль несе його папка"
   :rules
   [{:id :place/domain :says "домен — правила гри одного обмеженого контексту: таблиці сутностей і логіка, що їх змінює; він ніколи не залежить від presentation і ніколи не містить view, prefab чи текстур; одна збірка на домен, фіча — папка всередині нього, а нового домену без рішення власника не буває" :governs :layer :checked-by :agent}
    {:id :place/presentation :says "presentation показує доменний стан і приймає намір гравця: ніколи правило гри, ніколи стан, яким володіє домен, і ніколи пряма зміна доменної таблиці — лише командна подія; вигляд світу лежить в області збірки presentation, HUD — у папці вікна, а області йдуть підпапками збірки" :governs :layer :checked-by :agent}
    {:id :place/module :says "модуль — чиста логіка біля рушія, що не знає правил цієї гри і ніколи їх не містить; модуль з однією збіркою є однією фічею з рольовими папками прямо під коренем, парний модуль ділиться на збірку контракту і збірку реалізації з рольовими папками в кожній" :governs :layer :checked-by :agent}
    {:id :place/shared-kernel :says "shared kernel тримає примітиви й базові ECS-типи для всіх шарів — лише код, потрібний кільком шарам і не знаючий жодного з них" :governs :layer :checked-by :agent}
    {:id :dep/shared-kernel :says "shared kernel залежить лише від зовнішніх бібліотек і рушія, а посилатися на нього може будь-який шар" :governs :assembly :checked-by :agent}
    {:id :dep/module :says "модуль ідеально не має залежностей, може посилатися на інший модуль і на shared kernel, і ніколи — на домен чи presentation" :governs :assembly :checked-by :agent}
    {:id :dep/layer-to-module :says "домен і presentation посилаються на будь-яку кількість модулів, але лише на збірку контракту парного модуля або на модуль з однією збіркою — ніколи на збірку реалізації" :governs :assembly :checked-by :agent}
    {:id :dep/presentation-to-domain :says "presentation посилається на домен в один бік — на те, що показує; зворотного посилання не буває" :governs :assembly :checked-by :agent}
    {:id :dep/domain-tiers :says "домени мають яруси: описує, що робить власник, — дієслово; знає конкретного власника — агент; інакше підкладка; домен посилається лише на свій або попередній ярус" :governs :assembly :checked-by :agent}
    {:id :dep/no-cycles :says "циклів залежностей між збірками нема" :governs :assembly :checked-by :agent}
    {:id :dep/boot-exempt :says "збірка запуску, що складає стан гри, стоїть поза всіма правилами залежностей" :governs :assembly :checked-by :agent}
    {:id :place/namespace :says "простір імен іде за шляхом папки" :governs :role-folder :checked-by :agent}
    {:id :folder/archetypes :says "папка архетипів лежить прямо під коренем збірки і лише тоді, коли збірка оголошує архетипи" :governs :role-folder :checked-by :agent}
    {:id :folder/shared-role :says "рольова папка живе всередині фічі, області чи збірки модуля; прямо під коренем домену чи presentation вона стоїть лише як спільна роль, вміст якої ділять кілька фіч або областей" :governs :role-folder :checked-by :agent}
    {:id :folder/no-code-outside-a-role :says "коду поза рольовою папкою не буває" :governs :role-folder :checked-by :agent}
    {:id :folder/roles :says "кожна рольова папка має закритий вміст і власну заборону" :governs :role-folder :checked-by :agent
     :folders {:components "чисті структури даних, ніколи логіка чи побічні ефекти"
               :tags "тег-компоненти"
               :events "однокадрові компоненти подій"
               :configs "класи ассетів даних, ніколи runtime-логіка й ніколи самі ассети"
               :data "колекції, записи, перелічення, ніколи ECS-системи чи обʼєкти сцени"
               :systems "ECS-системи й оркестрація, ніколи логіка view чи визначення конфігів"
               :helpers "stateless-обчислення, єдина назва цієї ролі, ніколи стан між кадрами чи володіння сутностями"
               :views "шар view, ніколи бізнес-логіка, лише в presentation"}}]})
```

## Частина 4 — Ремесло і вибір форми

Як написане окреме місце коду і яку форму взяти для нового. Три розділи: імена типів, форма методу й
гучна помилка, вибір рецепта і ознака його примірника.

## Іменування

```clojure
(def rules-naming
  {:section :naming
   :does "суфікс ролі, самодостатність імені типу і межа доменного префікса"
   :rules
   [{:id :name/suffix :says "суфікс несе роль: дані — компонент, безполевий маркер — тег, однокадровий імпульс — подія, посилання між таблицями — імʼя ключа власника з позначкою посилання перед словом компонент" :governs :component :checked-by :graph}
    {:id :name/self-sufficient :says "імʼя типу зрозуміле без простору імен, імʼя фічі повторюється завжди і ніколи не лишається голої ролі, а розрізнення через псевдонім чи кваліфікатор простору імен не буває" :governs :assembly :checked-by :agent}
    {:id :name/prefix-exception :says "доменний префікс прибирається, і єдиний виняток — компонент ідентичності або дискримінатор таблиці, на які посилаються З ІНШИХ доменів" :governs :component :checked-by :agent :note "інсталер домену зберігає префікс, але його форма — предмет блоку DI і в набір не входить"}
    {:id :name/static :says "static-клас — лише для stateless-утиліти без полів" :governs :assembly :checked-by :agent}]})
```

## Методи, гучна помилка, коментарі

```clojure
(def rules-methods
  {:section :methods-fail-loud-comments
   :does "як метод віддає результат, як код падає на відсутній передумові і де стоїть коментар"
   :rules
   [{:id :method/try :says "метод, що може не вдатися, повертає прапорець успіху і віддає вихід окремим параметром, а викликач розгалужується саме за прапорцем" :governs :query-helpers :checked-by :agent}
    {:id :method/return-value :says "певний один результат повертається значенням" :governs :query-helpers :checked-by :agent}
    {:id :method/no-inferred-success :says "ніколи виводити успіх з результату — ні з порожнього посилання, ні з нульової кількості, ні з нульової довжини" :governs :query-helpers :checked-by :agent}
    {:id :method/collection-by-ref :says "колекція, яку метод пише, передається за посиланням, ніколи за значенням" :governs :managed-collection :checked-by :agent}
    {:id :method/inline-helper :says "одноразовий приватний helper, що читає поля й розгортає лінійний потік викликача, лишається на місці: виноситься лише самодостатній за сигнатурою метод, який перевикористовується" :governs :system :checked-by :agent :kind :choice}
    {:id :method/try-get-first :says "пошук одного рядка йде спробним отриманням першого на запиті, архетипі чи множині сутностей" :governs :query-helpers :checked-by :agent}
    {:id :fail/throw :says "крок, що не може виконати свою роботу, кидає описовий виняток, називаючи те, чого бракує" :governs :system :checked-by :agent}
    {:id :fail/no-silence :says "ніколи тихе повернення на відсутній передумові і ніколи попередження в лог із пропуском роботи" :governs :system :checked-by :agent}
    {:id :fail/cancel :says "скасування перевіряється киданням на токен окремим рядком після кожного очікування, ніколи разом із перевіркою валідності і ніколи тихим поверненням" :governs :system :checked-by :agent}
    {:id :fail/cleanup :says "те, чим володіє скасована робота, звільняється в блоці завершення, а часткова робота прибирається перед киданням" :governs :system :checked-by :agent}
    {:id :fail/exception-type :says "виняток за замовчуванням — виняток недопустимої операції з повідомленням" :governs :system :checked-by :agent}
    {:id :fail/silence-boundary :says "межа гучної помилки: відсутність обробника кидає, коли через неї робота ЗАГУБИЛАСЯ — рядок не зʼявився, ассет не завантажився; тихий пропуск законний лише там, де нічого не просили й нічого не винні, і сам рецепт називає цю відсутність валідним станом" :governs :system :checked-by :agent}
    {:id :async/token :says "довга async-задача бере токен у монітора стану застосунку" :governs :status-monitor :checked-by :agent}
    {:id :comment/on-the-thing :says "коментар — короткий, на самій речі, оновлюваний у тому ж диффі: намір, неочевидні інваріанти, контракти — і лише там, де логіка перестає бути простою й однозначною" :governs :system :checked-by :agent}
    {:id :comment/never :says "ніколи коментар про ІНШИЙ файл — лише посилання за імʼям або перенесення факту до власника — і ніколи шаблонна XML-документація" :governs :system :checked-by :agent}
    {:id :deviation/in-code :says "відоме відхилення записується коментарем у коді в місці відхилення" :governs :system :checked-by :agent}]})
```

## Вибір форми, каталог, ознаки рецептів

```clojure
(def rules-form-choice
  {:section :form-choice-and-signatures
   :does "яку форму набуває нова частина коду і за якою ознакою її примірник упізнається"
   :rules
   [{:id :choose/recipe-tree :says "форма береться за предметом частини: ECS-дані йдуть у рецепти даних, конфіг — у рецепти конфігів, поведінка — у рецепти поведінки, view — у рецепт пари view і системи, робота з ассетами — у рецепт addressables; коли жодна гілка не тримає, рецепта нема" :governs :system :checked-by :agent :kind :choice}
    {:id :choose/data-recipe :says "серед даних: несе значення — компонент, позначає ідентичність рядка — тег, сигналізує зміну — подія" :governs :component :checked-by :agent :kind :choice}
    {:id :choose/config-recipe :says "серед конфігів: багато видів різної форми зі спільним ключем у таблицю сутностей — поліморфний каталог, завантажується чи будується на старті — конфіг плюс loader, інакше просто конфіг" :governs :config :checked-by :agent :kind :choice}
    {:id :choose/behavior-recipe :says "серед поведінки: охоплює кілька піддоменів — transaction-сутність; будує світ при створенні мапи з незалежно впорядкованими частинами — оркестратор із підсистемами, просто будує світ — стадія конвеєра; фаза ходу — оркестратор із підсистемами; реагує на подію з незалежними частинами — reactive-оркестратор, просто реагує — reactive; неперервно щокадру — per-frame; прибирає події — cleanup" :governs :system :checked-by :agent :kind :choice}
    {:id :catalogue/when :says "поліморфний каталог береться, коли разом: кілька авторських видів різної форми зі спільним ключем, runtime запитує чи зʼєднує записи за ключем, а новий вид додається без правки оркестратора; один конфіг однієї форми і однорідний список на одне читання каталогом не є, і поведінки на конфігу не буває" :governs :config :checked-by :agent :kind :choice}
    {:id :catalogue/config-shape :says "абстрактний базовий конфіг тримає лише спільний ключ, конкретні види додають свої параметри або нічого, а контейнер тримає масив базових конфігів" :governs :config :checked-by :graph}
    {:id :catalogue/container :says "контейнер вантажиться одним loader-ом на кроці завантаження конфігів, реалізує валідацію і перевіряє, що записи не порожні — порожній каталог зупиняє гру на старті" :governs :config-loader :checked-by :agent}
    {:id :catalogue/spawn-orchestrator :says "оркестратор спавну — стадія конвеєра створення мапи: обходить записи і віддає кожен першій підсистемі, що обробляє його конкретний тип; порожній запис або тип без підсистеми кидає" :governs :pipeline-base :checked-by :agent}
    {:id :catalogue/row-shape :says "рядок каталогу народжується архетипом з усіма колонками виду: ключ-посилання простору субʼєкта, рівно один головний тег, мітка сімейства коли архетипів кілька, колонка виду над перелічуванням, записана раз, параметри виду і початковий стан; вибір іде self-index виду, ніколи другим головним тегом і ніколи вгадуванням за присутністю параметрів" :governs :archetype :checked-by :graph :enforcement :weaker-today}
    {:id :catalogue/evaluator :says "необовʼязкове сімейство переоцінювачів читає свій зріз виду з self-index, зʼєднує цільову таблицю за ключем і звіряє колонку стану лише при зміні; запускається кожен ввімкнений безумовно, кожен сам запитує свої рядки, порожній зріз — валідний стан" :governs :subsystem :checked-by :agent}
    {:id :catalogue/dual-host :says "переоцінка для першого ходу і для кожного наступного живе двома тонкими хостами над тим самим сімейством: стадія конвеєра одразу після спавну і фаза ходу в хвості конвеєра ходу" :governs :subsystem :checked-by :agent}
    {:id :catalogue/extension :says "новий вид — це конкретний підклас конфіга, нове значення перелічування з компонентом параметрів, підсистема спавну і за потреби переоцінювач; оркестратор при цьому не змінюється" :governs :subsystem :checked-by :agent}
    {:id :catalogue/checklist :says "каталог повний, коли складені всі пʼять частин: абстрактна база з ключем і контейнер, завантаження з валідацією, оркестратор спавну з підсистемами, рядок таблиці з колонкою виду і за потреби сімейство переоцінювачів" :governs :config :checked-by :agent :note "рядок чеклиста про звільнення дескриптора loader-ом знято правилом часу життя конфіга"}
    {:id :recipe/signatures :says "примірник рецепта впізнається за ознакою, і ознака є правилом написання коду" :governs :recipe-signature :checked-by :graph :enforcement :weaker-today
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
                  :startup-step "негенеричний async-контракт"}}]})
```

## Що свідомо не є правилом коду

Межа набору: чому запис носія міг не стати правилом. Критерій один і той самий — запис входить у
набір тоді й лише тоді, коли його можна порушити зміною коду в `Assets/`.

```clojure
(def out-of-scope
  {:criterion "запис входить у набір тоді й лише тоді, коли його можна порушити зміною коду в Assets/"
   :reasons ["форма реєстрації DI під рефакторингом: виклик реєстрації, час життя, ціль інтерфейсу, передані параметри, клас і розміщення інсталера, порядок установки модулів, ручне вписування систем у стан гри — правила про типи, константи й конструктори, які реєстрація лише споживає, лишаються в наборі"
             "процес агента, правило документа, нотація і поведінка інструмента — не правило коду; евристики впізнавання рецептів лишаються в коді інструмента"
             "розміщення ассетів, а не коду"
             "жива назва, історія або застарілий текст носія — доказ у факті, ніколи правило"
             "правила розповіді коду — вони живуть у каскаді"
             "скелет C# з рецепта: у набір іде правило, яке скелет кодує, сам скелет лишається в носії"
             "примус: нові діагностики, крок перевірки і скіл звірки — їх ставить окрема задача"]})
```

## Звірка

```clojure
(def tally
  {:rules 244 :sections 17 :parts 4 :constructs 56 :prefixes 38
   :choice-rules 25 :enforcement-marks 7
   :checked-by {:agent 152 :graph 56 :arch-check 28 :analyzer 6 :none 2}
   :law "розбіжність між цим блоком і перерахунком — помилка файлу, не розбіжність тексту"})
```
