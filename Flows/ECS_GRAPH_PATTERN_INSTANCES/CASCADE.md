---
category: A
read: reference
status: partial
tags: [tools, patterns, cascade]
related:
  - "[FLOW](FLOW.md)"
  - "[CONTEXT](CONTEXT.md)"
---

Каскад: s1 і s2 — закон тегів «головний + label», маркери, сторож Roslyn, злитий граф з pattern <RECIPE>, споживачі.

# Subject

```clojure
{:task :pattern-markers-and-unified-graph
 :flow "Flows/ECS_GRAPH_PATTERN_INSTANCES/FLOW.md"
 :context "Flows/ECS_GRAPH_PATTERN_INSTANCES/CONTEXT.md"
 :subject "закон тегів «головний + label» і маркери в коді гри; аналізатор Roslyn — сторож маркерів у компіляції Unity; злитий інструмент графа (ECS + DI) з pattern <RECIPE> для 15 рецептів; перехід споживачів і видалення двох старих інструментів"
 :previous-cascade :none
 :stage :s2
 :written-at "2026-09-15"
 :mode :auto                       ;; власник зняв ворота після s1 і читає лише s2 та # Contra
 :gates {:after-s1 :waived :after-s2 ?}
 :shared-rules [tag-law-main-label marker-shapes role-decision recipe-signatures]
 :parts [tag-law-and-markers drift-guard unified-graph graph-queries tool-cutover]
 :parts-are "межі поставок з :where CONTEXT.md — різні мови й середовища (C# гри, аналізатор netstandard2.0, Python-інструмент, документи); це не розбиття одного алгоритму на методи"
 :delivery-order (-> tag-law-and-markers drift-guard unified-graph graph-queries tool-cutover)}
```

# s1

Спершу чотири спільні правила — їх читають кілька частин, тому вони стоять один раз. Далі п'ять алгоритмів,
по одному на поставку, кожен повний: `:makes`, `:criterion`, `:data`, `:flow`, `:exits`, `:numbers`.
Після них — рішення, які s1 закрив (з confidence для # Contra стадії s2), що лишено s2 як структуру, зведення
детекторів, власна contra і прогалини CONTEXT.md.

```clojure
(def tag-law-main-label  ;; перегляд tag-law (ARCHITECTURE.md → Entities); читають міграція тегів у коді й аудит інструмента
  {:main  {:per-archetype "рівно один"
           :is "мітка унікальності типу сутності — системи фільтрують за архетипом, тобто за ним"
           :shared-between-archetypes :never
           :exempt {:tag EventTag
                    :rule "EventTag — головний тег кожного архетипу події; ідентичність події несе її компонент, як сьогоднішній :event-archetype :exempt"
                    :recognized-by "склад архетипу містить EventFrameComponent і EventTag"}}
   :label {:is "маркер ролі чи належності в оголошенні архетипу"
           :recognized-by "label-маркер на struct тегу — роль тегу глобальна і не залежить від архетипу, в якому він стоїть"
           :per-archetype "0-4 поруч із головним — Tags.Get бере до 5 type-аргументів"
           :filter :never                      ;; жоден AllTags / AnyTags / WithoutAnyTags не бере label-тег
           :role {:transaction "роль рецепта PATTERN_TRANSACTION_ENTITY"
                  :none "належність без рецепта — UITag, DistrictOpenConditionTag"}}
   :runtime-tag-write :never                   ;; склад фіксується при народженні, як сьогодні
   :order-in-tags-get "рушію байдужий (Tags — множина); головний першим — лише для читача"
   :deviations #{"архетип без головного тегу"
                 "архетип з двома і більше головними тегами"
                 "головний тег, не EventTag, на двох і більше архетипах"
                 "EventTag на архетипі без EventFrameComponent"
                 "label-тег у крос-архетипному фільтрі запиту"
                 "крос-архетипний фільтр з кількістю тегів, відмінною від 1 (набори за EventTag звільнені, як сьогодні)"
                 "AddComponent тегу під час життя сутності"
                 "склад тегів архетипу зібрано поза Tags.Get — не прочитано"}})
```

```clojure
(def marker-shapes  ;; що стверджує кожен маркер і яка форма типу це підтверджує; аналізатор — помилка компіляції, інструмент — попередження
  {:predicates {update-loop?           "тип не abstract і транзитивно реалізує IUpdatedSystem або ILateUpdatedSystem"
                anchored-on-event?     "пряма база — UpdatedSystem або LateUpdatedSystem, і аргумент base(...) — виклик EventArchetypes.Of<E> або Query().AnyComponents(ComponentTypes.Get<…>) лише з типів подій"
                anchored-on-table?     "аргумент base(...) є, але не подієвий: член холдера архетипів або запит не лише з подій"
                holds-event-archetype? "у тілі типу поза аргументом base(...) є виклик EventArchetypes.Of<E>"
                event-type?            "struct з суфіксом Event або EventComponent — закон іменування проєкту"
                view-type?             "V транзитивно походить від MonoBehaviour"
                subscribes-to?         "у тілі типу є додавання обробника += до події, оголошеної у V або в її предку"
                tag-struct?            "тип — struct, що реалізує ITag"}
   :role-per-frame  {:claims "per-frame система, чию роль база не вирішує"
                     :shape (and (update-loop?) (not (anchored-on-event?)))}
   :role-reactive   {:claims "реактивна система, чию роль база не вирішує"
                     :shape (and (update-loop?) (not (anchored-on-table?)) (or (anchored-on-event?) (holds-event-archetype?)))}
   :view-subscriber {:claims "клас підписується на C#-події view V — аргумент typeof(V)"
                     :shape (and (view-type? V) (subscribes-to? V))}
   :label           {:claims "тег — label; для ролі — яка роль"
                     :shape (tag-struct?)}
   :redundant {:is "маркер ролі там, де role-decision дає роль і без маркера"
               :analyzer :not-checked          ;; :od-analyzer-scope — лише розбіжність форми
               :tool "попередження"}})
```

```clojure
(def role-decision  ;; роль не-abstract класу; згори вниз, перша істинна гілка перемагає
  (cond
    (and (update-loop?) (sweeps-events?))         :cleanup        ;; крос-архетипний набір за EventTag і DeleteEntity в тому ж класі — EventCleanupSystem
    (anchored-on-event?)                          :reactive       ;; база вирішує: 16 з EventArchetypes.Of<E> + HexInfoPanelDistrictSystem з AnyComponents
    (and (update-loop?) (holds-event-archetype?)) (cond (role-marker?) :marker-role
                                                        :else          :undecided)   ;; опитувачі: база не вирішує
    (update-loop?)                                :per_frame      ;; подієвого архетипу нема ні в базі, ні в тілі
    (pipeline-member?)                            :pipeline_stage ;; предок IPrioritizedUniTaskSystem з аргументом MapGenerationStep
    (turn-phase-member?)                          :turn_phase     ;; предок TurnPhaseSubSystem
    (startup-step?)                               :startup_step   ;; предок — неgeneric IUniTaskSystem; AppState — з реєстрації
    (family-member?)                              :sub_system     ;; предок — abstract клас, який збирає хост
    :else                                         :none))
```

```clojure
(def recipe-signatures  ;; правило «рецепт → ознака» живе в коді інструмента (:marker-reader); :expected — еталон meter після маркерів і міграції тегів
  {PATTERN_COMPONENT {:decided-by :kind
                      :signature "struct, що реалізує IComponent, або з суфіксом Component; не подія"
                      :expected 60}
   PATTERN_TAG {:decided-by :kind
                :signature "struct : ITag; вивід ділить головні й label за tag-law-main-label"
                :expected "30 = 23 + 6 головних + 1 label ролі transaction; label-типів 3"}
   PATTERN_EVENT {:decided-by :kind
                  :signature "struct з суфіксом Event або EventComponent, або тип у EventArchetypes.Of<T> чи CreateEvent"
                  :expected 13}
   PATTERN_CONFIG {:decided-by :base
                   :signature "не-abstract клас, чий родовід сягає ScriptableObject або SerializedScriptableObject; abstract SO-бази — окремим рядком"
                   :expected "31 пряма конкретна + конкретні нащадки 4 abstract SO-баз — перерахунок roslyn на стадії коду (c-6)"}
   PATTERN_CONFIG_LOADER {:decided-by :di
                          :signature "реєстрація ConfigLoaderSystem з аргументом X, .As<IUniTaskSystem>() і AppState.ConfigLoading у WithParameter; окремо — реєстрація IUniTaskSystem з AppState.InstanceObjects"
                          :expected "26 + 1"}
   PATTERN_PIPELINE_STAGE {:decided-by :base
                           :signature "не-abstract клас, предок IPrioritizedUniTaskSystem з аргументом MapGenerationStep"
                           :expected 14}
   PATTERN_ORCHESTRATOR_SUBSYSTEM {:decided-by :lexical
                                   :signature "контракт — abstract клас, елемент колекційного параметра конструктора (IReadOnlyList) хоча б одного хоста; члени — не-abstract нащадки; хости — власники параметра"
                                   :expected "10 контрактів / 29 членів (26 + 3 фази ходу) / 12 хостів (c-7)"}
   PATTERN_REACTIVE_SYSTEM {:decided-by #{:base :marker}
                            :signature "роль :reactive з role-decision"
                            :expected "18 = 17 за базою + 1 за маркером"}
   PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM {:decided-by :lexical
                                         :signature "роль :reactive і ребро hosts до родини"
                                         :expected 1}
   PATTERN_PERFRAME_SYSTEM {:decided-by #{:base :marker}
                            :signature "роль :per_frame з role-decision"
                            :expected "7 = 5 за базою + 2 за маркером"
                            :deviations "клас з роллю :undecided — «маркер потрібен»"}
   PATTERN_CLEANUP_SYSTEM {:decided-by :lexical
                           :signature "роль :cleanup з role-decision"
                           :expected 1}
   PATTERN_VIEW_SYSTEM {:decided-by :marker
                        :signature "клас з view-маркером typeof(V), підтверджений лексичною підпискою на подію V; view — клас у теці Views/ з родоводом до MonoBehaviour"
                        :expected "4 підписники / 5 підписок / 4 view"
                        :deviations #{"view з ребром emits — сама піднімає ECS-подію (HexInfoPanelView)"
                                      "підписка на ім'я події view без маркера"}}
   PATTERN_TRANSACTION_ENTITY {:decided-by :label-tag
                               :signature "архетип, серед label-тегів якого є тег з роллю transaction"
                               :expected 1}
   PATTERN_POLYMORPHIC_CATALOGUE {:decided-by :lexical
                                  :signature "abstract SO-база B + не-abstract SO-контейнер з полем [SerializeField] типу масив B + компонент з ім'ям B, де суфікс Config замінено на KindComponent, і цей компонент несе хоча б один архетип"
                                  :expected 2}
   ADDRESSABLE_PATTERNS {:decided-by :lexical
                         :signature "параметр конструктора типу IAddressable"
                         :expected 7}})
```

```clojure
(def tag-law-and-markers
  {:makes     "архетипи з одним унікальним головним тегом і label-тегами; маркер ролі на трьох опитувачах і view-маркер на чотирьох підписниках"
   :criterion "маркер стоїть лише там, де role-decision без маркера дає :undecided або де пару view ↔ система лексично не видно; склад тегів підкоряється tag-law-main-label; поведінка систем не змінюється — змінюється лише склад тегів в оголошеннях архетипів"

   :data {:holder-declarations {:type Archetype
                                :holds "39 архетипів: члени 7 доменних холдерів (GetArchetype з ComponentTypes.Get і Tags.Get), замикання EventArchetypes.Of<T>, маніфест SingletonArchetypes"
                                :from "9 файлів :holders-today"}
          :tag-types {:type ITag
                      :holds "23 struct : ITag сьогодні"
                      :from "теки Tags/ фіч і shared kernel (EventTag, SingletonTag)"}
          :tag-carriers {:type dict
                         :holds "тег → архетипи, що його несуть"
                         :from "склад Tags.Get у :holder-declarations; звірка з ecsg.py tags"}
          :birth-sites {:type Entity
                        :holds "місця, що складають теги змінюваних архетипів поза холдером (CreateEntity з Tags.Get)"
                        :from "grep Tags.Get< кожного змінюваного тегу поза холдерами; сьогодні відоме лише EcsEventExtensions.CreateEvent — для подій, які не змінюються"}
          :tag-roles {:type dict
                      :holds "тег → головний або label (з роллю чи без); архетип → чи має унікальний головний"
                      :from "tag-law-main-label над :tag-carriers"}
          :marker-types {:type Attribute
                         :holds "три види маркерів: роль системи (per-frame | reactive), view-підписник (Type view), label (роль transaction або без ролі)"
                         :from "нові типи в shared kernel; дім і форма — s2"}
          :new-main-tags {:type ITag
                          :holds "6 головних тегів — панелі HexInfoPanel, TurnPanel, ResourceBar, DistrictBuildUI; архетипи OpenConditionSingle, OpenConditionExist"
                          :from "архетипи без унікального головного; імена й теки Tags/ — s2"}
          :transaction-label {:type ITag
                              :holds "label-тег ролі transaction"
                              :from ":amendment-1; дім — s2 (:od-label-home)"}
          :pollers {:type Type
                    :holds "TurnProcessorSystem → per-frame; BuildDistrictCompletionSystem → reactive; DistrictBuildUISystem → per-frame"
                    :from ":pollers, рішення :od-pollers"}
          :subscribers {:type Type
                        :holds "HexInfoPanelDistrictSystem → HexInfoPanelView; DistrictBuildUISystem → DistrictBuildUIView; DistrictBuildListUISubSystem → DistrictBuildListUIView; DistrictBuildPriceUISubSystem → DistrictBuildPriceUIView"
                        :from ":view-subscriptions"}
          :assembly-reach {:type AssemblyDefinitionAsset
                           :holds "прямі references збірок, що отримують маркер або новий тег"
                           :from "python3 Tools/asmdef_reach.py refs | can"}}

   :flow (-> (:step-1 "зняти склад тегів усіх архетипів і місця, що складають його поза холдерами"
                      {:reads #{:holder-declarations :tag-types} :writes #{:tag-carriers :birth-sites}
                       :state "для кожного тегу відомі архетипи-носії; відомі всі місця народження поза холдерами"
                       :world :read})

             (:step-2 (cond
                        (:flow-1 "EventTag на архетипах подій")   (:conclusion-1 "головний — виняток закону")
                        (:flow-2 "тег несе рівно один архетип")   (:conclusion-2 "головний")
                        (:flow-3 "тег несуть кілька архетипів")   (:conclusion-3 "label без ролі; кожен носій отримує власний головний"))
                      {:reads #{:tag-carriers} :writes #{:tag-roles}
                       :state "кожен тег — головний або label; названо 6 архетипів без унікального головного і архетип рецепта transaction — BuildDistrictInProgress"})

             (:step-3 "оголосити типи маркерів"
                      {:reads #{} :writes #{:marker-types}
                       :state "три види маркерів існують у shared kernel"
                       :world :write})

             (:step-4 "оголосити нові теги"
                      {:reads #{:tag-roles} :writes #{:new-main-tags :transaction-label :tag-types}
                       :state "6 головних тегів у теках Tags/ своїх фіч і label-тег ролі transaction оголошено; типів тегів 30"
                       :world :write})

             (:step-5 "позначити label-теги маркером"
                      {:reads #{:tag-roles :marker-types :transaction-label} :writes #{:tag-types}
                       :state "UITag, DistrictOpenConditionTag і тег ролі transaction несуть label-маркер"
                       :world :write})

             (:step-6 "переписати склад тегів в оголошеннях архетипів разом з місцями народження"
                      {:reads #{:tag-roles :new-main-tags :transaction-label :birth-sites} :writes #{:holder-declarations :birth-sites}
                       :state "4 панелі UI і 2 архетипи умов відкриття несуть власний головний + label; BuildDistrictInProgress — BuildDistrictInProgressTag + label ролі transaction; жодне місце народження не складає старого складу"
                       :world :write})

             (:step-7 "поставити маркер ролі на опитувачів"
                      {:reads #{:pollers :marker-types} :writes #{:pollers}
                       :state "3 опитувачі несуть роль; role-decision більше не дає :undecided"
                       :world :write})

             (:step-8 "поставити view-маркер на підписників"
                      {:reads #{:subscribers :marker-types} :writes #{:subscribers}
                       :state "4 підписники називають свою view через typeof"
                       :world :write})

             (:step-9 (cond
                        (:flow-1 "змінена збірка бачить типи маркерів і тегів прямим посиланням") (:conclusion-1 "нічого")
                        (:flow-2 "не бачить, а правила залежностей дозволяють")                  (:conclusion-2 "додати посилання в asmdef"))
                      {:reads #{:pollers :subscribers :tag-types :new-main-tags :assembly-reach} :writes #{:assembly-reach}
                       :state "кожна змінена збірка компілюється за своїми посиланнями"
                       :world :write}))

   :exits #{"маркер потрібен у збірці, що не посилається на дім маркерів, і правила залежностей посилання забороняють — стоп і питання власнику (:core-reach: Boot.Implementation і Installers.World на Core не посилаються; :placement-rules)"
            "label-тегів більше, ніж вміщає Tags.Get поруч із головним — склад пішов би через Tags.Add, якого розбір холдерів не читає (:friflo-arity); сьогодні потрібно не більше 1 label на архетип"}

   :numbers {:archetypes 39
             :event-archetypes 13
             :tag-types-before 23
             :tag-types-after "30 = 23 + 6 головних + 1 label ролі transaction"
             :label-types "3 — UITag, DistrictOpenConditionTag, тег ролі transaction"
             :label-placements "7 = 4 UITag + 2 DistrictOpenConditionTag + 1 transaction"
             :holders-changed "3 — PresentationUIArchetypes (4 оголошення), EconomyArchetypes (2), ActionsArchetypes (1)"
             :tags-get-arity "головний + до 4 label — Tags.Get бере до 5 type-аргументів"
             :role-markers 3
             :view-markers 4}})
```

```clojure
(def drift-guard
  {:makes     "помилка компіляції Unity на кожному маркері, що не збігається з формою свого типу"
   :criterion "кожен маркер міряється marker-shapes над усіма оголошеннями свого типу; розбіжність — Diagnostic з DiagnosticSeverity.Error на атрибуті з текстом інваріанту; збіг — тиша; надлишковий маркер аналізатор не перевіряє"

   :data {:compilation {:type Compilation
                        :holds "збірка Unity в межах дії аналізатора"
                        :from "компілятор Unity (Roslyn 4.10) з DLL, позначеною RoslynAnalyzer"}
          :marker-types {:type ImmutableArray<INamedTypeSymbol>
                         :holds "типи трьох маркерів"
                         :from ":compilation за metadata-іменами, які знає аналізатор"}
          :shape-anchors {:type ImmutableArray<INamedTypeSymbol>
                          :holds "IUpdatedSystem, ILateUpdatedSystem, UpdatedSystem, LateUpdatedSystem, EventArchetypes, ITag, MonoBehaviour; якір, якого збірка не бачить, означає «цієї форми тут нема»"
                          :from ":compilation"}
          :marked-type {:type INamedTypeSymbol
                        :holds "тип вихідного коду з хоча б одним маркером"
                        :from ":compilation"}
          :claims {:type ImmutableArray<AttributeData>
                   :holds "маркери типу з аргументами: роль, typeof(V), роль label"
                   :from ":marked-type"}
          :declarations {:type ImmutableArray<SyntaxReference>
                         :holds "усі оголошення типу — partial дає кілька"
                         :from ":marked-type"}
          :loop-contract {:type bool :holds "update-loop?" :from ":marked-type і :shape-anchors"}
          :anchor-on-event {:type bool :holds "anchored-on-event?" :from "аргумент base(...) у :declarations, розв'язаний семантичною моделлю"}
          :anchor-on-table {:type bool :holds "anchored-on-table?" :from "аргумент base(...) у :declarations"}
          :held-event-archetypes {:type ImmutableArray<IMethodSymbol>
                                  :holds "замикання EventArchetypes.Of<E> у тілі типу поза base(...)"
                                  :from ":declarations"}
          :subscribed-events {:type ImmutableArray<IEventSymbol>
                              :holds "події, до яких тип додає обробник +="
                              :from ":declarations"}
          :tag-struct {:type bool :holds "tag-struct?" :from ":marked-type"}
          :descriptors {:type ImmutableArray<DiagnosticDescriptor>
                        :holds "по одному на вид розбіжності — роль per-frame, роль reactive, view-підписник, label; severity Error, увімкнено за замовчуванням"
                        :from "аналізатор"}
          :mismatches {:type ImmutableArray<Diagnostic>
                       :holds "розбіжності з місцем атрибута і текстом інваріанту"
                       :from "звірка тверджень з формою"}
          :analyzer-dll {:type FileInfo
                         :holds "DLL netstandard2.0, зібрана проти Microsoft.CodeAnalysis.CSharp 4.3"
                         :from "dotnet build лише проєкту аналізатора поза Assets/"}}

   :flow (-> (:step-1 (cond
                        (:flow-1 "збірка не бачить жодного типу маркера") (:conclusion-1 "аналізатор нічого не реєструє — маркер тут не написати")
                        (:flow-2 "інакше")                                (:conclusion-2 "розв'язати якорі форм"))
                      {:reads #{:compilation} :writes #{:marker-types :shape-anchors}
                       :state "аналізатор знає, які атрибути — маркери, і якими типами міряти форму"
                       :world :read})

             (:step-2 "зібрати твердження типу"
                      {:reads #{:compilation :marker-types} :writes #{:marked-type :claims :declarations}
                       :state "тип з маркером, його маркери з аргументами і всі його оголошення"
                       :world :read})

             (:step-3 "виміряти форму типу за всіма оголошеннями"
                      {:reads #{:marked-type :declarations :shape-anchors}
                       :writes #{:loop-contract :anchor-on-event :anchor-on-table :held-event-archetypes :subscribed-events :tag-struct}
                       :state "фактична форма типу: цикл Update, якір бази, утримані подієві архетипи, підписки, struct-тег"})

             (:step-4 "звірити кожне твердження з формою за marker-shapes"
                      {:reads #{:claims :loop-contract :anchor-on-event :anchor-on-table :held-event-archetypes :subscribed-events :tag-struct :descriptors}
                       :writes #{:mismatches}
                       :state "кожен маркер або підтверджено, або названо його розбіжність"})

             (:step-5 "віддати розбіжності компілятору"
                      {:reads #{:mismatches} :writes #{}
                       :state "збірка з розбіжністю не компілюється; без розбіжностей компілюється як раніше"
                       :world :write})

             (:step-6 "зібрати DLL аналізатора"
                      {:reads #{:descriptors} :writes #{:analyzer-dll}
                       :state "DLL netstandard2.0 зібрана без помилок і чекає імпорту власником"
                       :world :write}))

   :exits #{"DLL не імпортована, не позначена RoslynAnalyzer або лежить поза текою збірки з типами маркерів — перевірки нема (:unity-analyzer-docs: корінь Assets не покриває asmdef-збірки; імпорт — Unity-side власника)"
            "збірка не бачить жодного типу маркера — перевіряти нічого (:core-reach: Boot.Implementation і Installers.World на Core не посилаються)"
            "Microsoft.CodeAnalysis не відновлюється без мережі — DLL не збирається (:analyzer-build-env: у ~/.nuget/packages пакета нема)"}

   :numbers {:severity "Error"
             :diagnostic-kinds 4
             :roslyn-in-unity "4.10"
             :roslyn-built-against "4.3"
             :target-framework "netstandard2.0"
             :scope "збірка з типами маркерів + збірки, що прямо на неї посилаються (:od-analyzer-placement)"
             :markers-in-code "10 = 3 ролі + 4 view + 3 label"}})
```

```clojure
(def unified-graph
  {:makes     "один граф фактів ECS, DI, родоводу й маркерів з екземплярами 15 рецептів, зібраний одним детермінованим проходом"
   :criterion "екземпляр рецепта — те, що ознака з recipe-signatures механічно знаходить у фактах; роль — з бази, де база вирішує однозначно, інакше з маркера (role-decision); нічого не вигадується: невирішене стає попередженням або AMBIGUOUS, а не мовчанням"

   :data {:project-root {:type Path :holds "тека над Assets/" :from "CWD або --path, вгору до першої теки з Assets/"}
          :scan-roots {:type list
                       :holds "Assets/Domains, Assets/Presentation, Assets/Modules, Assets/Scripts, Assets/Flows"
                       :from ":od-roots"}
          :source-files {:type list :holds "Path кожного .cs під :scan-roots, відсортовані" :from "файлова система"}
          :declarations {:type dict
                         :holds "оголошення типу → вид (class | struct | interface | enum), ім'я, ланцюг вкладеності, повний простір імен, abstract, атрибути з аргументами (typeof, доступ до члена), бази з generic-аргументами як написано, параметри конструкторів і [Inject]-методів (тип, елемент колекції, аргументи), поля (тип, масив, атрибути), оголошення event Action, Priority, тека файла, source_location"
                         :from "tree-sitter над :source-files"}
          :usings {:type dict :holds "файл → простори імен з using і охоплюючий простір" :from "tree-sitter"}
          :holders {:type dict
                    :holds "(холдер, член) → компоненти, теги, type-параметри; маніфест singleton"
                    :from "класи з суфіксом Archetypes: GetArchetype(ComponentTypes.Get, Tags.Get) через => або перший return; componentTypes.Add<T>() і Tags.Get у маніфесті"}
          :ecs-sites {:type list
                      :holds "сирі ECS-факти з місцем і класом-власником: AddComponent, GetComponent, HasComponent, RemoveComponent, RemoveTag, Singletons.Get | Has | Set, CreateEvent; виклики холдерів і EventArchetypes.Of<T> з позицією — аргумент base(...) чи тіло; AllTags, AnyTags, WithoutAnyTags і AnyComponents(ComponentTypes.Get) з типами; DeleteEntity; поля ComponentIndex і доступи _field[key]; const int і Priority; виклики generic-методів з type-аргументами"
                      :from "tree-sitter"}
          :di-sites {:type list
                     :holds "Register і RegisterInstance з ланцюжком As, Lifetime, WithParameter(AppState.X) і інсталером; Boot.Construct — new …State(…) з параметрами"
                     :from "tree-sitter"}
          :subscription-sites {:type list
                               :holds "a.E += H — ім'я події E, текст отримувача a, клас-власник, місце"
                               :from "tree-sitter"}
          :nodes {:type dict
                  :holds "id → вузол: kind (component | tag | event | config | system | view | archetype | installer | interface | other), name, повний namespace, source_location, abstract, declared; система — role, priority, decided_by; архетип — components, main_tag, label_tags; DI-грані — lifetime, installer, contract, state"
                  :from "оголошення і розв'язані посилання за :od-node-ids"}
          :edges {:type list
                  :holds "{src dst rel via args app_state collection source_location confidence}; rel — has, writes, reads, removes, emits, reacts_to, polls, disposes, fk_of, references, inherits, hosts, registers, exposes, injects, runs_in, subscribes"
                  :from "розв'язані сирі факти і звірка"}
          :ancestry {:type dict :holds "id типу → транзитивні предки з generic-аргументами, як написано на ребрах inherits" :from "ребра inherits"}
          :markers {:type dict
                    :holds "id класу чи тегу → маркери: роль (per-frame | reactive), view (id view), label (transaction | без ролі)"
                    :from "атрибути в :declarations"}
          :ecs-tables {:type dict
                       :holds "tables з роллю PK | FK | IDX і архетипом, index_usages, sets, dispose_sites, late_writes, indexed_components, component_field_types, singleton_used — як schema 3"
                       :from "ECS-звірка"}
          :tag-audit {:type list :holds "відхилення tag-law-main-label з архетипом чи місцем" :from "аудит тегів"}
          :recipe-signatures {:type dict :holds "15 рецептів → ознака і джерело рішення" :from "(def recipe-signatures) у коді інструмента"}
          :recipe-instances {:type dict
                             :holds "рецепт → екземпляри (id, source_location, decided_by), відхилення, пояснення, коли екземплярів 0"
                             :from "застосування :recipe-signatures"}
          :warnings {:type list
                     :holds "parse error, AMBIGUOUS-посилання, зіткнення імен, :undecided, надлишкові маркери, підписки без маркера, DeleteEntity без одного кандидата, key-role, singleton-маніфест, непізнані форми реєстрації, склад через Tags.Add"
                     :from "усі кроки"}
          :graph-doc {:type dict
                      :holds "meta {schema, roots, files, generated, curated} + nodes + edges + ecs-tables + tag_audit + recipes + warnings"
                      :from "фінальна збірка"}
          :graph-file {:type Path :holds "<project>/<тека артефактів>/graph.json — одна тека; назва — s2" :from ":project-root"}
          :legacy-artifact-dirs {:type Path :holds "дві теки артефактів старих інструментів у корені проєкту" :from ":project-root"}}

   :flow (-> (:step-1 "знайти корінь і перелічити .cs під коренями"
                      {:reads #{:scan-roots} :writes #{:project-root :source-files}
                       :state "перелік 359 файлів гри під 5 коренями"
                       :world :read})

             (:step-2 "розібрати кожен файл і зняти оголошення"
                      {:reads #{:source-files} :writes #{:declarations :usings :holders :warnings}
                       :state "усі оголошення з базами, атрибутами, конструкторами, полями й подіями; холдери архетипів прочитані; файл, що не розбирається, — попередження, скан триває"
                       :world :read})

             (:step-3 "зняти сирі факти використання"
                      {:reads #{:source-files} :writes #{:ecs-sites :di-sites :subscription-sites}
                       :state "усі місця ECS-викликів, реєстрацій DI і підписок з позицією та класом-власником"
                       :world :read})

             (:step-4 "дати кожному оголошенню один id і розв'язати посилання в сирих фактах"
                      {:reads #{:declarations :usings :ecs-sites :di-sites :subscription-sites}
                       :writes #{:nodes :ecs-sites :di-sites :subscription-sites :warnings}
                       :state "вузол на оголошення (тип — ім'я з ланцюгом вкладеності; зіткнення — повний простір імен); кожне посилання — id за ім'ям і using, або AMBIGUOUS з попередженням"})

             (:step-5 "прочитати маркери"
                      {:reads #{:declarations :nodes} :writes #{:markers :warnings}
                       :state "кожен маркер прив'язаний до вузла класу чи тегу; аргумент typeof(V) — до вузла V або AMBIGUOUS"})

             (:step-6 "побудувати родовід"
                      {:reads #{:declarations :nodes} :writes #{:edges :ancestry :nodes}
                       :state "ребра inherits з аргументами; транзитивні предки кожного типу; config — за родоводом до ScriptableObject; view — тека Views/ і родовід до MonoBehaviour; abstract позначено"})

             (:step-7 "розгорнути архетипи"
                      {:reads #{:holders :ecs-sites :nodes :markers} :writes #{:nodes :edges :warnings}
                       :state "архетипи з конкретних холдерів і з замикань generic-холдерів — напряму й через шаблони-ретранслятори; у кожного components, main_tag і label_tags за label-маркером; ребра has; прив'язки власник → архетип з позицією base(...) чи тіло"})

             (:step-8 "звірити ECS-факти"
                      {:reads #{:ecs-sites :nodes :edges} :writes #{:edges :ecs-tables :nodes :warnings}
                       :state "writes, reads, removes, emits; disposes з прив'язок; таблиці з архетипами; key-role і два fk_of; references; пріоритети; маніфест singleton — як сьогоднішня schema 3"})

             (:step-9 "звірити DI-факти"
                      {:reads #{:di-sites :nodes} :writes #{:edges :nodes :warnings}
                       :state "registers з args, Lifetime, інсталером і app_state; exposes з args; injects з конструкторів зареєстрованих типів і [Inject]-методів, collection позначено; runs_in з Boot; contract і state — грані вузлів; Boot runs_in і RegisterInstance — EXTRACTED"})

             (:step-10 "з'єднати хости з родинами"
                       {:reads #{:declarations :nodes} :writes #{:edges}
                        :state "ребро hosts від кожного класу, чий конструктор бере колекцію abstract класу, до цього класу; член родини може мати кількох хостів"})

             (:step-11 "звести пари view ↔ підписник"
                       {:reads #{:markers :subscription-sites :declarations :ancestry :nodes} :writes #{:edges :warnings}
                        :state "ребро subscribes від кожного класу з view-маркером до V, підтверджене лексичною підпискою на подію V чи її предка; підписка на ім'я події view без маркера — попередження; повтор імені події — AMBIGUOUS"})

             (:step-12 "перевірити закон тегів"
                       {:reads #{:nodes :edges :markers :ecs-tables} :writes #{:tag-audit :warnings}
                        :state "кожне відхилення tag-law-main-label назване з архетипом чи місцем"})

             (:step-13 "вирішити роль кожного не-abstract класу за role-decision"
                       {:reads #{:nodes :edges :ancestry :markers :ecs-tables} :writes #{:nodes :edges :warnings}
                        :state "кожна система має role і decided_by (base | marker | lexical); reacts_to — лише від якоря бази або від маркера reactive; утриманий подієвий архетип в іншому класі — polls; :undecided і маркер там, де база вирішує, — попередження"})

             (:step-14 "знайти екземпляри 15 рецептів"
                       {:reads #{:recipe-signatures :nodes :edges :ancestry :markers :ecs-tables :tag-audit} :writes #{:recipe-instances}
                        :state "для кожного рецепта — екземпляри з decided_by, відхилення і пояснення порожнечі"})

             (:step-15 "записати граф і прибрати старі теки артефактів"
                       {:reads #{:nodes :edges :ecs-tables :tag-audit :recipe-instances :warnings :scan-roots :source-files :project-root}
                        :writes #{:graph-doc :graph-file :legacy-artifact-dirs}
                        :state "один граф на диску з curated true; старих тек артефактів нема"
                        :world :write}))

   :exits #{"над CWD нема Assets/ — вихід з помилкою (build_graph.py:1123-1128, build_di_graph.py:485-490)"
            "файл .cs не розбирається — попередження parse error, скан триває (build_graph.py:1156-1159)"
            "виклик generic-холдера з іншою кількістю type-аргументів — архетип не резолвиться, попередження (build_graph.py:842-847)"
            "DeleteEntity у класі з кількома прив'язаними архетипами — disposes не приписується, попередження «attribute by hand» (7 сьогодні, :ecs-graph-today)"
            "голе ім'я типу відповідає кільком оголошенням — посилання AMBIGUOUS з попередженням (:node-name-collisions)"
            "клас циклу Update тримає подієвий архетип без маркера — роль :undecided з попередженням «маркер потрібен» (:pollers — 3 до маркерів)"
            "ім'я події view повторюється — зв'язок підписки AMBIGUOUS (:view-subscriptions: tree-sitter не знає типу змінної view)"
            "склад тегів архетипу зібрано через Tags.Add — не прочитано, попередження (:friflo-arity)"
            "форма реєстрації поза відомими — RegisterFactory, RegisterComponentInHierarchy, RegisterEntryPoint, AsImplementedInterfaces, RegisterInstance не з ідентифікатора — попередження (di-patterns.md: «flag if they appear»)"}

   :numbers {:files "359 = 358 + 1 з Assets/Flows"
             :build-seconds "менше 1; сьогодні кожен екстрактор 0.20-0.24, розбір один замість двох"
             :schema "ecs 3, di 1 — злитий граф має власну версію; зміна версії = застарілість"
             :ecs-before "вузлів 419, ребер 567, попереджень 8 (7 DeleteEntity + 1 key-role)"
             :di-before "вузлів 272, ребер 329 — registers 101, exposes 73, injects 126, runs_in 29; попереджень 0"
             :reacts-to-before "21 — на кожен виклик EventArchetypes.Of, 5 з них не реактивні"
             :reacts-to-after "20 = 16 якорів EventArchetypes.Of + 3 події AnyComponents у HexInfoPanelDistrictSystem + 1 маркер BuildDistrictCompletionSystem"
             :polls-after "4 — TurnProcessorSystem, DistrictBuildUISystem, DistrictBuildListUISubSystem, MainMenuState"
             :roles-before "per_frame 28, pipeline_stage 15, sub_system 30, turn_phase 3"
             :roles-after "reactive 18, per_frame 7, cleanup 1, pipeline_stage 14, turn_phase 3, sub_system 26, startup_step — 2 класи (ConfigLoaderSystem, VertexGridSpawnSystem) з 27 реєстраціями, none — ShowHexesUISystem"
             :registers-after "101 — ребра з args, 26 реєстрацій ConfigLoaderSystem на одному вузлі"
             :injects-after "126 + параметри конструктора ConfigLoaderSystem, яких сьогодні нема через generic-ідентичність — точне число фіксує стадія коду"
             :warnings-after "не більше бази 8; нових 0"}})
```

```clojure
(def graph-queries
  {:makes     "відповідь одного CLI зі свіжого графа: pattern <RECIPE> і запити обох сьогоднішніх інструментів"
   :criterion "відповідь ніколи не йде зі застарілого графа; pattern <RECIPE> ніколи не повертає порожнечу без пояснення"

   :data {:request {:type list
                    :holds "argv — pattern <RECIPE>, tags, systems [--role], explain, neighbors, search, tables, spaces, bfs, resolve, consumers, installer, state, unresolved, stats, check"
                    :from "агент"}
          :project-root {:type Path :holds "тека над Assets/" :from "CWD або --path"}
          :graph-file {:type Path :holds "graph.json злитого графа" :from ":project-root"}
          :builder-source {:type Path :holds "скрипт збірки інструмента" :from "тека скіла"}
          :freshness {:type bool
                      :holds "граф є, schema поточна, жоден .cs під коренями з meta не новіший, кількість файлів та сама, скрипт збірки не новіший"
                      :from "mtime і meta"}
          :graph-doc {:type dict :holds "граф у пам'яті" :from ":graph-file"}
          :adjacency {:type dict :holds "id → вихідні й вхідні ребра" :from ":graph-doc"}
          :answer {:type list :holds "рядки stdout; помилка — stderr і ненульовий код" :from "команда над :graph-doc"}}

   :flow (-> (:step-1 "знайти корінь і виміряти свіжість графа"
                      {:reads #{:request :graph-file :builder-source} :writes #{:project-root :freshness}
                       :state "відомо, чи граф свіжий; відсутній граф — застарілий"
                       :world :read})

             (:step-2 (cond
                        (:flow-1 "граф свіжий")     (:conclusion-1 "нічого")
                        (:flow-2 "граф застарілий") (:conclusion-2 "повна збірка unified-graph; збірка впала — її помилка в stderr, ненульовий код, відповіді нема"))
                      {:reads #{:freshness :project-root} :writes #{:graph-file}
                       :state "на диску свіжий граф, або запит завершено помилкою"
                       :world :write})

             (:step-3 "завантажити граф і обчислити вхідні ребра"
                      {:reads #{:graph-file} :writes #{:graph-doc :adjacency}
                       :state "граф у пам'яті з вихідними й вхідними ребрами"
                       :world :read})

             (:step-4 (cond
                        (:flow-1 "pattern з назвою поза 15") (:conclusion-1 "перелік 15 назв і ненульовий код")
                        (:flow-2 "pattern <RECIPE>")         (:conclusion-2 "екземпляри з source_location і decided_by, відхилення; при 0 — рядок, чого бракує ознаці")
                        (:flow-3 "tags")                     (:conclusion-3 "архетипи з головним і label-тегами окремо, відхилення tag-law-main-label, підсумок")
                        (:flow-4 "check")                    (:conclusion-4 "висячі ребра, власники таблиць і наборів, архетипи без складу, екземпляри рецептів без вузла; curated; попередження")
                        (:flow-5 "інша команда")             (:conclusion-5 "як сьогодні над злитими фактами; explain показує ECS- і DI-грані вузла разом; resolve приймає ім'я з аргументами і фільтрує ребра за args"))
                      {:reads #{:request :graph-doc :adjacency} :writes #{:answer}
                       :state "відповідь надрукована"
                       :world :write}))

   :exits #{"над CWD нема Assets/ — вихід з помилкою (ecsg.py:27-32, dig.py:31-36)"
            "назва рецепта поза 15 — перелік назв і ненульовий код (:query-shape — назва приходить рядком; :recipes-scope — набір закритий)"
            "збірка під час самолікування впала — помилка без відповіді зі старого графа (ecsg.py:35-56 сьогодні fail-open; CLAUDE.md § 3 забороняє тихий пропуск)"}

   :numbers {:recipes 15
             :commands-ecs "stats, systems, explain, neighbors, search, tables, spaces, tags, bfs, --check"
             :commands-di "stats, explain, resolve, consumers, installer, state, search, bfs, unresolved, --check"
             :commands-shared "stats, explain, search, bfs, check — зливаються в одну; назви — s2"}})
```

```clojure
(def tool-cutover
  {:makes     "проєкт, глобальна конфігурація й пам'ять знають лише злитий інструмент і закон «головний + label»; старих інструментів нема"
   :criterion "старі скіли видаляються лише після того, як метри злитого інструмента дали цілі; після переходу grep назв старих інструментів в області :od-grep-scope = 0"

   :data {:tool-home {:type Path
                      :holds ".claude/skills/<назва>/ — SKILL.md (можливості, схема, правила чесності), скрипти збірки й запитів, references з мапою «рецепт → ознака»"
                      :from ":tool-home; назва — s2"}
          :meter-readings {:type dict
                           :holds "метр → показ: stats, check, pattern для 15 рецептів проти еталонів, systems --role per_frame, tags, grep, doc_lint, gen_index"
                           :from "запуски метрів"}
          :gate-and-tooling {:type Path
                             :holds ".claude/hooks/graph-gate.py — GRAPH_DIR_RE, GRAPH_EXES, повідомлення; .gitignore 9-13; Tools/doc_lint.py SKIP_DIRS; Tools/asmdef_reach.py docstring"
                             :from "# Consumers"}
          :tool-docs {:type Path
                      :holds "~/.claude/CLAUDE.md 9-15; .sdd-flow/project.md — # Tools, :prefer-when GLOSSARY, # Meters, :code-verification, # Ceremonies; CLAUDE.md § 3 — заборона збірки лише для Unity-проєкту, тека артефактів; fantasymayor-pattern-choice SKILL.md 3 і instances; PATTERN_EVENT 59; PATTERN_COMPONENT 78; PATTERN_POLYMORPHIC_CATALOGUE 27, 244; GLOSSARY.md 4, 15-16; DOC_STANDARD.md 18, 46-47; INDEX.md 81 — зона агента, ask-first"
                      :from "# Consumers"}
          :tag-law-docs {:type Path
                         :holds "ARCHITECTURE.md 87-103, 121-132 — ask у hook; PATTERN_TAG 13-15, 34, 37, 40; PATTERN_POLYMORPHIC_CATALOGUE 154, 160, 226, 228, 270, 271; PATTERN_TRANSACTION_ENTITY 91; CLAUDE.md § 2 :step-3 (55) і § 5 notation-ecs-ext (91-96); fantasymayor-pattern-choice tag-law-before 123-130"
                         :from ":tag-law-wording, # Consumers"}
          :memory-files {:type Path
                         :holds "MEMORY.md 35, 36, 52, 59; два файли пам'яті про інструменти → один; 6 файлів з історичними згадками"
                         :from "# Consumers"}
          :old-skills {:type Path :holds "дві теки старих скілів у ~/.claude/skills/" :from ":old-tools"}
          :grep-scope {:type list
                       :holds "репозиторій поза Flows/Archive/, Flows/ECS_GRAPH_PATTERN_INSTANCES/, .sdd-flow/, Flows/DOC_AGENT_REVIEW/FLOW.md, Library, .git і текою артефактів; ~/.claude/CLAUDE.md; ~/.claude/skills; тека пам'яті"
                       :from ":od-grep-scope"}}

   :flow (-> (:step-1 "встановити злитий інструмент у скіли проєкту"
                      {:reads #{} :writes #{:tool-home}
                       :state "скіл з SKILL.md, скриптами й references на місці"
                       :world :write})

             (:step-2 (cond
                        (:flow-1 "кожен метр злитого інструмента дав ціль") (:conclusion-1 "перехід триває")
                        (:flow-2 "інакше")                                   (:conclusion-2 "перехід стоїть, старі скіли лишаються"))
                      {:reads #{:tool-home} :writes #{:meter-readings}
                       :state "метри злитого інструмента прочитані, рішення про перехід прийнято"
                       :world :read})

             (:step-3 "переписати сторож і службові скрипти"
                      {:reads #{:meter-readings} :writes #{:gate-and-tooling}
                       :state "hook, .gitignore, doc_lint і asmdef_reach знають лише нову теку артефактів і нові скрипти"
                       :world :write})

             (:step-4 "переписати посилання на інструмент у документах і скілах"
                      {:reads #{:tool-home} :writes #{:tool-docs}
                       :state "документи й скіли називають один інструмент; заборона збірки уточнена до Unity-проєкту"
                       :world :write})

             (:step-5 "переписати закон тегів у документах"
                      {:reads #{} :writes #{:tag-law-docs}
                       :state "закон тегів скрізь — один унікальний головний + label-теги; «рівно один тег» ніде не лишився"
                       :world :write})

             (:step-6 "переписати пам'ять"
                      {:reads #{:tool-home} :writes #{:memory-files}
                       :state "пам'ять знає один інструмент; історичні згадки переформульовано"
                       :world :write})

             (:step-7 "видалити старі скіли"
                      {:reads #{:meter-readings} :writes #{:old-skills}
                       :state "старих скілів нема; коміт у ~/.claude — лише на слово власника"
                       :world :write})

             (:step-8 (cond
                        (:flow-1 "grep = 0, привидів не більше 3, gen_index без LINT") (:conclusion-1 "перехід підтверджено")
                        (:flow-2 "інакше")                                             (:conclusion-2 "назад до кроку, чий файл дав збіг"))
                      {:reads #{:grep-scope :gate-and-tooling :tool-docs :tag-law-docs :memory-files :old-skills} :writes #{:meter-readings}
                       :state "перехід підтверджено метрами"
                       :world :read}))

   :exits #{"метр злитого інструмента не дав цілі — старі скіли лишаються, перехід стоїть (# Consumers: «видалити після переносу»)"
            "власник відхилив ask hook на правку ARCHITECTURE.md — закон тегів там не змінено, задача стоїть на воротах (:graph-gate-hook)"
            "зона агента INDEX.md після зміни коду — ask-first (:category-a-docs, CLAUDE.md § 3)"}

   :numbers {:consumer-rows "31: 2 з :change :none, settings.json — лише за зміни шляху hook"
             :memory-files 9
             :grep-target 0
             :doc-lint-ghosts "не більше 3"
             :clojure-errors 0}})
```

```clojure
(def settled-in-s1  ;; кожне рішення s2 переносить у # Contra для власника; :confidence — 0-100, наскільки варіант правильний
  [{:id :od-main-vs-label :confidence 60
    :chosen "label-маркер — атрибут на struct тегу; роль тегу глобальна"
    :why "рушій не бачить маркера — Tags лишається множиною struct : ITag; tree-sitter і семантична модель читають атрибут однаково; позиція в Tags.Get рушію байдужа, тож нічого не несе"
    :rejected ["інтерфейс-нащадок ITag — 25: виявлення схеми Friflo через непрямий ITag не перевірено"
               "перший type-аргумент Tags.Get — 10: перестановка мовчки ламає закон"
               "іменна конвенція — 5: компілятор її не бачить"]}
   {:id :od-uniqueness :confidence 55
    :chosen "рівно один головний тег на архетип, і він не спільний з іншим архетипом; єдиний виняток — EventTag на архетипах подій"
    :why "«мітка унікальності типу сутності» і «системи фільтрують за архетипом, тобто за ним» читаються як 1:1 між головним тегом і архетипом"
    :rejected ["спільний головний тег-категорія дозволено — 45: менше змін, але «мітка унікальності» тоді неточна (c-1)"]}
   {:id :od-event-tag :confidence 65
    :chosen "EventTag лишається головним тегом архетипів подій — виняток закону"
    :why "EventCleanupSystem фільтрує за EventTag поперек архетипів, а за label фільтрувати заборонено; склад тегів події задано двічі (EventArchetypes.Of і CreateEvent) — варіант без зміни складу не ризикує розсинхроном; виняток уже стоїть у tag-law :event-archetype"
    :rejected ["EventTag — label, архетипи подій без головного — 25: суперечить :filter :never" "власний головний тег на кожну подію — 10: 13 типів і зміна двох місць складу"]}
   {:id :od-ui-tag :confidence 50
    :chosen "UITag і DistrictOpenConditionTag стають label без ролі; кожна з 4 панелей і кожен з 2 архетипів умов відкриття отримує власний головний тег"
    :why "за :od-uniqueness спільний головний неможливий; жоден запит не фільтрує за UITag чи DistrictOpenConditionTag (:tag-filters-across-archetypes), тож label їм нічого не ламає; коментар холдера уже зве UITag «category tag»"
    :rejected ["лишити головними тегами-категоріями — 35: несумісне з :od-uniqueness" "прибрати UITag без заміни — 15: губиться належність панелей"]}
   {:id :od-pollers :confidence 65
    :chosen "TurnProcessorSystem — per-frame (75); BuildDistrictCompletionSystem — reactive (65); DistrictBuildUISystem — per-frame (60)"
    :why "TurnProcessorSystem: заголовок «Deliberately a per-frame system … must tick every frame to poll the in-flight task»; BuildDistrictCompletionSystem: «Event-gated reconcile», без стиглої події нічого не робить, форма reactive (цикл, без якоря-таблиці, утримує Of) збігається; DistrictBuildUISystem: якір base(...) — таблиця DistrictBuildUI, щокадрово чіпляє view, маркер reactive дав би розбіжність форми (c-8)"
    :rejected ["TurnProcessorSystem reactive — 25" "BuildDistrictCompletionSystem per-frame — 35" "DistrictBuildUISystem reactive orchestrator — 40: потребує зміни поведінки"]}
   {:id :od-shell :confidence 65
    :chosen "BuildDistrictActionSystem — екземпляр рецепта 8; pattern PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM чесно показує 1; маркера нема"
    :why "BuildDistrictActionSystem уже має повну логіку (витрата ціни, рядки District і in-progress) і не бере списку підсистем; маркер «reactive orchestrator» без списку не збігся б із формою; рядок «Live shell instance» у PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM застарів — його правка поза межами, показати власнику"
    :rejected ["маркер на shell — 35: маркер суперечив би формі"]}
   {:id :od-view-marker :confidence 70
    :chosen "атрибут на підписнику з typeof(view)"
    :why "рецепт 12: view нічого не знає про систему, підписка живе в підписника; аналізатор звіряє typeof(V) з типом-власником події, яку розв'язує семантична модель; tree-sitter читає аргумент typeof"
    :rejected ["атрибут на view з typeof(системи) — 10: view дізнається про систему" "без атрибута, за іменем події — 20: суперечить :amendment-1"]}
   {:id :od-view-pairs :confidence 55
    :chosen "екземпляр рецепта 12 — лише пара з C#-підпискою (4 підписники)"
    :why "маркери за :amendment-1 доповнюють саме пари з подіями; драйвери лише-виходу окремої ознаки не мають і в meter «4 підписники» не входять"
    :rejected ["плюс драйвери view без подій — 45: окремий запит пізніше"]}
   {:id :od-analyzer-scope :confidence 65
    :chosen "лише розбіжність форми для трьох видів маркерів — роль, view, label; надлишковий маркер — попередження інструмента, не помилка компіляції"
    :why "рішення :drift-guard називає саме розбіжність маркера з формою; менше правил в аналізаторі — менше розходжень між двома реалізаціями marker-shapes"
    :rejected ["плюс надлишковий маркер і розміщення label-тегів як помилка — 35"]}
   {:id :od-analyzer-placement :confidence 75
    :chosen "DLL — у теці збірки з типами маркерів; конкретна тека — разом з :od-marker-home у s2"
    :why "маркер компілюється лише через пряме посилання на збірку свого типу, а DLL у теці цієї збірки діє на неї і на збірки, що на неї посилаються, — отже на кожну збірку, яка взагалі може написати маркер (:unity-analyzer-docs, :core-reach)"
    :rejected ["окрема тека з власним asmdef — 20: зайве посилання в кожній збірці" "корінь Assets — 5: asmdef-збірки не покриває"]}
   {:id :od-node-ids :confidence 65
    :chosen "вузол на оголошення: тип — ім'я з ланцюгом вкладеності; архетип — Холдер.Член, для замикання generic-холдера аргументи в id; generic-замикання типів — args на ребрах; зіткнення голих імен — повний простір імен і розв'язок посилань за using, інакше AMBIGUOUS"
    :why "код звертається до архетипу саме як до члена холдера; args на ребрах дають одне правило ідентичності для ECS і DI (exposes до IPrioritizedUniTaskSystem з args MapGenerationStep; 26 реєстрацій ConfigLoaderSystem — 26 ребер одного вузла); ланцюг вкладеності прибирає зіткнення TerrainView з класом констант"
    :rejected ["ідентифікатор з префіксом виду — 35: не прибирає зіткнення клас-клас і не збігається з тим, як код називає архетип"]}
   {:id :od-roots :confidence 75
    :chosen "5 коренів — додати Assets/Flows"
    :why "DistrictBuildUIRequestedEvent — ігровий код у власній asmdef; без кореня граф знає подію лише з використань, без source_location; Editor, TutorialInfo і Plugins лишаються поза"
    :rejected ["4 корені — 25"]}
   {:id :od-refresh :confidence 80
    :chosen "повна перебудова при застарілості; застарілість — нема графа, інша schema, новіший .cs, інша кількість файлів або новіший скрипт збірки; збірка впала — помилка, без відповіді зі старого графа"
    :why "обидва екстрактори по 0.2 с; інкремент лишає привидів після перейменування (:di-graph-today); тихий пропуск заборонено CLAUDE.md § 3"
    :rejected ["інкремент за маніфестом — 15" "fail-open зі старим графом — 5 (c-10)"]}
   {:id :od-grep-scope :confidence 75
    :chosen "поза Flows/Archive/, текою цієї задачі, .sdd-flow/ і Flows/DOC_AGENT_REVIEW/FLOW.md"
    :why "керований канон перезаписує update; чужий FLOW — підтверджена історія (FLOW_CONTRACT flow-document :never «silently rewrite a confirmed decision»); архів і ця тека — історія задач"
    :rejected ["правити й канон, і чужий FLOW — 25"]}
   {:id :od-consumers-scope :confidence 80
    :chosen "споживачі поза :where плану — у межах задачі; s2 називає їх власнику; ask-first для INDEX і ARCHITECTURE лишається"
    :why "meter grep = 0 без них не пройде; закон тегів у PATTERN_* і CLAUDE.md § 5 інакше суперечить коду"
    :rejected ["лишити — 20"]}
   {:id :s1-role-values :confidence 75
    :chosen "маркер ролі має закритий набір per-frame | reactive"
    :why "база сьогодні не вирішує лише цих ролей (:pollers); cleanup, pipeline, фази ходу, кроки старту база вирішує завжди"
    :rejected ["маркер на кожну з ролей рецептів — 25: маркери там, де база вирішує, суперечать :markers-hybrid"]}
   {:id :s1-label-role-in-marker :confidence 60
    :chosen "label-маркер несе роль (transaction) або не несе нічого (належність)"
    :why "інструмент не зашиває імена ігрових тегів у правило «рецепт → ознака»"
    :rejected ["ім'я тегу ролі в коді інструмента — 40"]}
   {:id :s1-reacts-vs-polls :confidence 80
    :chosen "reacts_to — лише від якоря base(...) або від маркера reactive; утриманий у полі подієвий архетип — ребро polls"
    :why "FLOW # Disproven: наявність reacts_to не відділяє реактивні від per_frame, бо сьогодні ребро стоїть на кожному виклику EventArchetypes.Of (21, з них 5 не реактивні)"
    :rejected ["ребро reacts_to на кожен виклик — 20: повертає спростоване"]}
   {:id :s1-config-transitive :confidence 70
    :chosen "config — не-abstract клас з родоводом до ScriptableObject; abstract SO-бази — окремо"
    :why "sealed-нащадки IsolineConfig та abstract баз каталогів — справжні типи ассетів, які реєструє ConfigLoaderSystem; сьогодні вони kind other"
    :rejected ["лише пряма база — 30 (c-6)"]}
   {:id :s1-view-kind :confidence 70
    :chosen "view — клас у теці Views/ з родоводом до MonoBehaviour"
    :why ":view-kind-imprecise: підрядок MonoBehaviour у базах дає Boot, DisposedMono, StatusMonitor, SpawnerAuthoringBase і пропускає 3 view, що походять від DisposedMono"
    :rejected ["лише родовід до MonoBehaviour — 20" "лише тека Views/ — 10"]}
   {:id :s1-orchestrator-contract :confidence 65
    :chosen "контракт рецепта 7 — abstract клас, який збирає колекційний параметр конструктора; 10 / 29 / 12; родина інтерфейсу конвеєра — рецепт 6"
    :why "рецепт 7 описує abstract базу + нащадків + хоста-систему; 14 етапів конвеєра збирає MapCreationState, не система"
    :rejected ["будь-який елемент колекційного параметра — 35 (c-7)"]}
   {:id :s1-catalogue-kind-link :confidence 60
    :chosen "компонент виду каталогу — ім'я abstract SO-бази з суфіксом Config, заміненим на KindComponent, який несе хоча б один архетип"
    :why "рецепт 14 сам іменує FooConfig ↔ FooKindComponent; обидва живі каталоги так названі (:catalogue-today)"
    :rejected ["зв'язок через архетип, який пише підсистема спавну родини — 40: непрямий ланцюг через DI і виклики"]}
   {:id :s1-recipes-in-graph :confidence 60
    :chosen "екземпляри рецептів обчислює збірка і зберігає в графі"
    :why "це похідні факти, як disposes і references; check лінтує їх, stats показує лічильники, запит — лише читання"
    :rejected ["обчислювати при кожному запиті — 40"]}
   {:id :s1-legacy-dirs :confidence 60
    :chosen "збірка злитого графа один раз прибирає дві теки артефактів старих інструментів"
    :why "теки похідні й у .gitignore; hook не пускає агента в них shell-командою; збірка вже прибирає свої застарілі артефакти (raw.json, manifest.json)"
    :rejected ["власник видаляє вручну — 40"]}
   {:id :s1-merged-node-facets :confidence 70
    :chosen "DI-види стають гранями вузла оголошення: contract — похідне від As-цілі чи елемента колекції, state — від Boot, lifetime і installer — з реєстрації"
    :why "один вузол на оголошення — explain показує ECS і DI разом; два набори видів одного типу — те, що злиття має прибрати"
    :rejected ["окремі DI-вузли поруч з ECS-вузлами — 30"]}])
```

```clojure
(def left-for-s2  ;; структура, не алгоритм; позначено :structural
  [{:id :od-marker-home :structural true :lean "Assets/Scripts/Core/ поруч зі StateAllowedAttribute — 55; Assets/Scripts/EcsExtensions/ — 45"
    :constraint-from-s1 "дім видно кожній збірці з маркером; DLL аналізатора — у тій самій теці (:od-analyzer-placement)"}
   {:id :od-label-home :structural true :lean "shared kernel Assets/Scripts/EcsExtensions/ — 55; Domains.Actions Tags/ — 25; Domains.Kernel — 20"}
   {:id :od-analyzer-deps :structural true :lean "PackageReference Microsoft.CodeAnalysis.CSharp 4.3 з nuget.org — 65; HintPath на DLL ApiUpdater 4.4 з редактора — 35"}
   {:id :od-tool-name :structural true :lean "fantasymayor-graph — 60"}
   {:id :marker-type-shape :structural true :what "один тип атрибута ролі з переліком чи окремий тип на роль; імена трьох видів маркерів; AllowMultiple для view-маркера"}
   {:id :analyzer-project :structural true :what "тека й назва проєкту аналізатора поза Assets/ (кандидат Tools/<назва>/); id діагностик"}
   {:id :dll-import-path :structural true :what "хто кладе зібрану DLL у теку Assets — агент копією без .meta чи власник при імпорті (прогалина CONTEXT)"}
   {:id :analyzer-registration :structural true :what "дія над символом чи над синтаксисом кожного оголошення з накопиченням (c-4)"}
   {:id :new-tag-names :structural true :what "імена 6 головних тегів і label-тегу ролі transaction, їхні теки Tags/"}
   {:id :cli-merge :structural true :what "назви спільних команд двох CLI, остаточні назви rel і ролей, тека артефактів"}
   {:id :build-pass-shape :structural true :what "кроки 2-3 unified-graph — один прохід по файлу чи два"}
   {:id :skill-layout :structural true :what "scripts/ і references/ злитого скіла, текст SKILL.md"}])
```

```clojure
{:s1-tally {:parts 5
            :steps {tag-law-and-markers 9 drift-guard 6 unified-graph 15 graph-queries 4 tool-cutover 8 :total 42}
            :state-named "42 / 42"
            :hollow-steps 0
            :undeclared 0
            :orphan 0
            :untouched-input 0
            :name-vs-state 0
            :guard-occasion "20 умов виходу, без приводу в CONTEXT.md — 0"
            :method-names 0}   ;; імена наявних API (GetArchetype, CreateEvent, AnyComponents, WithParameter) — якорі ознак, не декомпозиція; нових класів і методів не названо

 :contra [{:id :c-1
           :kills "tag-law-and-markers кроки 2, 4, 6 — 6 нових головних тегів і 7 змінених оголошень"
           :case "UITag на HexInfoPanel, TurnPanel, ResourceBar, DistrictBuildUI; DistrictOpenConditionTag на OpenConditionSingle і OpenConditionExist"
           :fails "унікальність головного тегу читає «мітку унікальності» строго; якщо власник мав на увазі «рівно один на архетип, спільний дозволено», зміна складу — зайвий обсяг у 3 холдерах"
           :fix [{:id :fix-a :confidence 55 :is "унікальний головний тег; UITag і DistrictOpenConditionTag — label" :cost "6 нових тегів, 7 оголошень, типів тегів 30"}
                 {:id :fix-b :confidence 45 :is "спільний головний тег-категорія дозволено; аудит знімає перевірку спільності" :cost "«мітка унікальності» неточна; кроки 4 і 6 скорочуються до label ролі transaction"}]}

          {:id :c-2
           :kills "поведінку: рядок народжується в іншому архетипі, ніж той, що ітерують системи"
           :case "EcsEventExtensions.CreateEvent складає Tags.Get<EventTag>() окремо від EventArchetypes.Of<T> (:event-tags-duplicated); якщо так само складено теги панелі UI, панель народиться без нового головного тегу"
           :fails "CONTEXT.md перелічує лише одне таке місце — для подій; для 7 змінюваних архетипів перелік не встановлено"
           :fix [{:id :fix-a :confidence 80 :is "крок 1 шукає Tags.Get< кожного змінюваного тегу поза холдерами; крок 6 змінює їх разом" :cost "склад тегів у змінній grep не ловить"}
                 {:id :fix-b :confidence 20 :is "аналізатор забороняє Tags.Get поза холдерами" :cost "розширює :od-analyzer-scope за рішення :drift-guard"}]}

          {:id :c-3
           :kills "anchored-on-event? для AnyComponents — в аналізаторі й в інструменті"
           :case "тип події без суфікса Event чи EventComponent у ComponentTypes.Get аргументу base(...)"
           :fails "подієвість визначено іменем; інструмент ще знає використання в EventArchetypes.Of<T> і CreateEvent, аналізатор у межах однієї збірки — ні"
           :fix [{:id :fix-a :confidence 70 :is "суфікс — закон іменування проєкту; обидва міряють однаково" :cost "подія, названа не за законом, дає розбіжність форми"}
                 {:id :fix-b :confidence 30 :is "подія — тип, що хоч раз стоїть у EventArchetypes.Of<T> або CreateEvent" :cost "аналізатор не бачить використань в інших збірках — два правила розходяться"}]}

          {:id :c-4
           :kills "drift-guard крок 3 — форма за всіма оголошеннями типу"
           :case "partial-клас: view-маркер на одній частині, += у другій"
           :fails "вимір по одному синтаксичному оголошенню дає хибну розбіжність; вимір по символу потребує семантичної моделі кожного дерева, а виклик Compilation.GetSemanticModel в аналізаторі — попередження RS1030 і вартість у кожній компіляції Unity"
           :fix [{:id :fix-a :confidence 55 :is "дія над синтаксисом кожного оголошення з готовою семантичною моделлю, накопичення за символом, звірка в кінці компіляції" :cost "потокобезпечне накопичення на час компіляції"}
                 {:id :fix-b :confidence 45 :is "дія над символом і семантична модель дерев його оголошень" :cost "RS1030; простіше"}]}

          {:id :c-5
           :kills "звичний запит resolve за замкненим ім'ям — di SKILL.md, instances скіла fantasymayor-pattern-choice"
           :case "вузла «IPrioritizedUniTaskSystem з аргументом MapGenerationStep» більше нема — є вузол IPrioritizedUniTaskSystem і ребра exposes з args"
           :fails "запит за замкненим ім'ям нічого не знайде, якщо розв'язувач не розбирає аргументи"
           :fix [{:id :fix-a :confidence 75 :is "розв'язувач приймає ім'я з аргументами і фільтрує ребра за args (graph-queries крок 4)" :cost "розбір аргументів у CLI"}
                 {:id :fix-b :confidence 25 :is "вузол на кожне замикання і ребро до відкритого оголошення" :cost "дві ідентичності одного типу — те, що злиття мало прибрати"}]}

          {:id :c-6
           :kills "meter pattern PATTERN_CONFIG проти еталону «config 35»"
           :case "InnerIsolineConfig, OuterIsolineConfig і нащадки ResourceConfig, DistrictOpenConditionConfig, DistrictBuildOutcomeConfig сьогодні — kind other"
           :fails "транзитивний родовід міняє число, а еталону для нового числа в CONTEXT.md нема"
           :fix [{:id :fix-a :confidence 70 :is "еталон — mcp__roslyn__get_type_hierarchy Descendants на 4 abstract SO-базах + 31 пряма конкретна" :cost "один вимір на стадії коду"}
                 {:id :fix-b :confidence 30 :is "лишити пряму базу" :cost "sealed-нащадки, які ConfigLoaderSystem реально вантажить, не видно як config"}]}

          {:id :c-7
           :kills "meter pattern PATTERN_ORCHESTRATOR_SUBSYSTEM проти «11 контрактів / 26 + 3 / 12 хостів»"
           :case "IPrioritizedUniTaskSystem з аргументом MapGenerationStep — інтерфейс; 14 членів збирає MapCreationState, не система"
           :fails "еталон CONTEXT.md рахує родину конвеєра в контракти, але не в члени; s1 віддає її рецепту 6"
           :fix [{:id :fix-a :confidence 65 :is "10 abstract контрактів / 29 членів / 12 хостів" :cost "еталон meter переписується"}
                 {:id :fix-b :confidence 35 :is "контракт — будь-який елемент колекційного параметра, і інтерфейс теж" :cost "14 етапів двічі — у рецептах 6 і 7; хости поза системами (MapCreationState, Boot)"}]}

          {:id :c-8
           :kills "маркер reactive на DistrictBuildUISystem, якщо власник обере для нього reactive orchestrator"
           :case "якір base(...) — PresentationUIArchetypes.DistrictBuildUI, тобто таблиця"
           :fails ":role-reactive вимагає not anchored-on-table — такий маркер дасть помилку компіляції; зробити клас реактивним — змінити поведінку, що поза межами"
           :fix [{:id :fix-a :confidence 60 :is "маркер per-frame; хост родини видно ребром hosts" :cost "рецепт 9 цього класу не показує"}
                 {:id :fix-b :confidence 40 :is "окрема задача: відкриття оверлея — на реактивній системі" :cost "зміна поведінки, нова задача"}]}

          {:id :c-9
           :kills "tool-cutover крок 5 — рядок PATTERN_TRANSACTION_ENTITY про label ролі transaction"
           :case "рецепт 13 сьогодні не каже ставити label; без рядка наступна транзакційна сутність не матиме маркера і pattern її не знайде"
           :fails "«нові рецепти й зміни процедур рецептів» поза межами, окрім формулювань закону тегів"
           :fix [{:id :fix-a :confidence 60 :is "рядок про label ролі — частина формулювання закону тегів у рецепті" :cost "межа тлумачення — показати власнику"}
                 {:id :fix-b :confidence 40 :is "рецепт не чіпати; label лише на BuildDistrictInProgress" :cost "майбутні транзакції невидимі для pattern"}]}

          {:id :c-10
           :kills "кожен запит, поки збірка графа зламана"
           :case "виняток у скрипті збірки після правки інструмента (parse error окремого .cs — лише попередження)"
           :fails "сьогодні ecsg.py fail-open віддає старий граф; s1 обрав помилку без відповіді — жоден запит не пройде до виправлення"
           :fix [{:id :fix-a :confidence 65 :is "помилка й ненульовий код" :cost "агент без графа до виправлення скрипта"}
                 {:id :fix-b :confidence 35 :is "відповідь зі старого графа з гучним попередженням у stderr і позначкою stale" :cost "застарілі факти можуть піти в рішення"}]}]

 :gaps-in-context [{:gap "еталон «orchestrator — 11 контрактів / 26 + 3 / 12 хостів» змішує родину конвеєра (рецепт 6) з рецептом 7" :in-s1 ":s1-orchestrator-contract, c-7"}
                   {:gap "еталон «config 35» — за прямою базою; числа для транзитивного правила нема" :in-s1 ":s1-config-transitive, c-6"}
                   {:gap "не встановлено, хто кладе зібрану DLL у теку Assets: :where каже «+ зібрана DLL», :out-of-scope віддає імпорт і .meta власнику" :in-s1 "лишено s2 — :dll-import-path"}
                   {:gap "місця народження поза холдерами для 7 змінюваних архетипів не перелічені — відоме лише CreateEvent для подій" :in-s1 "tag-law-and-markers крок 1, c-2"}
                   {:gap "не встановлено, чи зіштовхуються голі імена верхньорівневих типів у різних просторах імен" :in-s1 ":od-node-ids — попередження і AMBIGUOUS"}
                   {:gap "база injects під правилом однієї ідентичності не виміряна — ConfigLoaderSystem сьогодні без injects" :in-s1 "unified-graph :numbers :injects-after — фіксує стадія коду"}]

 :gate-after-s1 {:subject "алгоритм: чи так рахуємо, чи ті структури, чи ті умови"
                 :owner-verdict :waived-auto}}
```

# s2

Стадія s2, 2026-09-15, авто-режим. Для кожної з п'яти поставок s1 — структура коду. Частини 2-4 — класи й модулі:
методи з `:does :in :out :writes :scratch :flow :calls :exits :ends-with`, дані з `:from-s1` або `:shape`, хребет
точки входу і зведення метрів. Частини 1 і 5 — правки файлів без алгоритму в коді: точний перелік правок, а
покриття даних s1 зведено за тим, куди лягає кожна структура. Кожен вибір, який s1 лишив структурі, закрито тут;
перелік усіх рішень s1 і s2 з confidence для власника стоїть нагорі # Contra.

Нових тегів у коді ще нема, тож блоки, що називають їх іменами з ролевим суфіксом, обгорнуто перемикачем
doc-lint (база привидів — 3); коли код з'явиться, імена стануть кодом, і read-back перемикачі знімає.

## Частина 1 · tag-law-and-markers — C# гри

```clojure
(def placement-markers  ;; fantasymayor-placement над новими файлами без ролевого суфікса; теги — у new-tags нижче
  {:skills
   {fantasymayor-placement
    [{:file "Assets/Scripts/EcsExtensions/SystemRoleAttribute.cs" :assembly Ecs.Extensions :role-folder :none :installer :none}
     {:file "Assets/Scripts/EcsExtensions/SystemRoleKind.cs" :assembly Ecs.Extensions :role-folder :none :installer :none}
     {:file "Assets/Scripts/EcsExtensions/ViewSubscriberAttribute.cs" :assembly Ecs.Extensions :role-folder :none :installer :none}
     {:file "Assets/Scripts/EcsExtensions/TagLabelAttribute.cs" :assembly Ecs.Extensions :role-folder :none :installer :none}
     {:file "Assets/Scripts/EcsExtensions/TagLabelRole.cs" :assembly Ecs.Extensions :role-folder :none :installer :none}
     {:file "Tools/MarkerShapeAnalyzer/" :assembly :none :role-folder :none :installer :none
      :why "поза Assets/ — Unity його не компілює; це не шар гри, правила depends на нього не діють"}]}
   :why-shared-kernel "маркери потрібні трьом шарам (Domains, Presentation, Modules) і не знають жодного — place :shared-kernel; EcsExtensions пласка, без рольових тек (:placement-rules)"
   :references {:checked "python3 Tools/asmdef_reach.py refs для Turn, Domains.Actions, Domains.Economy, Presentation.UI — кожна прямо посилається на Ecs.Extensions (2026-09-15)"
                :new 0}})
```

```clojure
(def marker-home  ;; :od-marker-home разом з :od-analyzer-placement — один дім і для типів маркерів, і для DLL
  {:chosen "Assets/Scripts/EcsExtensions/ — збірка Ecs.Extensions"
   :confidence 70
   :why ["кожна збірка, де може стояти система, прямо посилається на Ecs.Extensions — там UpdatedSystem, IUpdatedSystem, EventArchetypes; область DLL у цій теці — сама збірка і 10 прямих споживачів: Boot.Implementation, Domains.Actions, Domains.Actors, Domains.Economy, Domains.Map, Installers.World, Presentation, Presentation.UI, Turn, UserInput"
         "усі 6 класів, що отримують маркер, уже мають using EcsExtensions"
         "s1-вихід «маркер у збірці, що не бачить дому маркерів» зникає — з Core його давали Boot.Implementation і Installers.World (:core-reach)"
         "якорі форм аналізатора живуть у тій самій збірці"]
   :rejected {:is "Assets/Scripts/Core/ поруч зі StateAllowedAttribute" :confidence 30
              :why "область DLL — 12 прямих споживачів Core, серед них Addressables.Core, Addressables.Implementations, AxialSystem без систем; Boot.Implementation і Installers.World поза областю"}})
```

```clojure
(def marker-types  ;; :marker-types s1 крок 3; файл на тип у Assets/Scripts/EcsExtensions/, namespace EcsExtensions
  {SystemRoleAttribute
   {:shape "public sealed class : Attribute, [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]"
    :fields {Role SystemRoleKind}
    :ctor "SystemRoleAttribute(SystemRoleKind role)"
    :comment "The role a system's base does not decide: an Update-loop class that holds an event archetype outside base(...). MarkerShapeAnalyzer fails the compilation when the class's shape contradicts the role."
    :use "[SystemRole(SystemRoleKind.PerFrame)]"}

   SystemRoleKind
   {:shape "public enum { PerFrame, Reactive }"
    :from-s1 "marker-shapes :role-per-frame і :role-reactive; :s1-role-values — закритий набір"}

   ViewSubscriberAttribute
   {:shape "public sealed class : Attribute, [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]"
    :fields {View System.Type}
    :ctor "ViewSubscriberAttribute(Type view)"
    :comment "The class adds handlers to C# events of this view; the view knows nothing of its subscriber."
    :use "[ViewSubscriber(typeof(HexInfoPanelView))]"}

   TagLabelAttribute
   {:shape "public sealed class : Attribute, [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]"
    :fields {Role TagLabelRole}
    :ctor ["TagLabelAttribute() — Role = TagLabelRole.None" "TagLabelAttribute(TagLabelRole role)"]
    :comment "A label tag: a role or membership marker beside an archetype's main tag. Never a query filter."
    :use ["[TagLabel]" "[TagLabel(TagLabelRole.Transaction)]"]}

   TagLabelRole
   {:shape "public enum { None, Transaction }"
    :from-s1 "tag-law-main-label :label :role — transaction або без ролі"}})
```

```clojure
(def marker-type-shape  ;; :marker-type-shape
  {:chosen "один тип атрибута ролі з переліком SystemRoleKind і AllowMultiple = false; ViewSubscriber — AllowMultiple = true; TagLabel — два конструктори"
   :confidence 55
   :why ["форма опитувача задовольняє і per-frame, і reactive — дві ролі на одному класі мусить забороняти хтось; AllowMultiple = false робить це компілятором (CS0579) без п'ятої діагностики"
         "перелік названо SystemRoleKind, а не SystemRole: [SystemRole(SystemRole.PerFrame)] ставить поруч атрибут і перелік з одним ім'ям, а збирати Unity-проєкт для перевірки розв'язку заборонено"
         "клас може слухати дві view — тому AllowMultiple = true у ViewSubscriber"]
   :rejected [{:is "два типи [PerFrameRole] і [ReactiveRole]" :confidence 35 :cost "п'ята діагностика «дві ролі»"}
              {:is "перелік SystemRole з тим самим ім'ям, що атрибут" :confidence 10}]})
```

```clojure
(def new-tags  ;; :new-main-tags і :transaction-label s1 крок 4; порожній struct : ITag, файл на тег, коментар — одне речення, що тег розрізняє
  [{:type HexInfoPanelTag :file "Assets/Presentation/UI/MainHud/HexInfoPanel/Tags/HexInfoPanelTag.cs"
    :namespace Presentation.UI.MainHud.HexInfoPanel.Tags :main-of PresentationUIArchetypes.HexInfoPanel :new-folder true}
   {:type TurnPanelTag :file "Assets/Presentation/UI/MainHud/TurnPanel/Tags/TurnPanelTag.cs"
    :namespace Presentation.UI.MainHud.TurnPanel.Tags :main-of PresentationUIArchetypes.TurnPanel :new-folder true}
   {:type ResourceBarTag :file "Assets/Presentation/UI/MainHud/ResourceBar/Tags/ResourceBarTag.cs"
    :namespace Presentation.UI.MainHud.ResourceBar.Tags :main-of PresentationUIArchetypes.ResourceBar :new-folder true}
   {:type DistrictBuildUITag :file "Assets/Presentation/UI/DistrictBuild/Tags/DistrictBuildUITag.cs"
    :namespace Presentation.UI.DistrictBuild.Tags :main-of PresentationUIArchetypes.DistrictBuildUI :new-folder false}
   {:type DistrictSingleOpenConditionTag :file "Assets/Domains/Economy/DistrictOpenCondition/Tags/DistrictSingleOpenConditionTag.cs"
    :namespace Domains.Economy.DistrictOpenCondition.Tags :main-of EconomyArchetypes.OpenConditionSingle :new-folder false
    :name-from "ім'я фічі й порядок слів, як у DistrictSingleOpenConditionEvaluatorSubSystem"}
   {:type DistrictExistOpenConditionTag :file "Assets/Domains/Economy/DistrictOpenCondition/Tags/DistrictExistOpenConditionTag.cs"
    :namespace Domains.Economy.DistrictOpenCondition.Tags :main-of EconomyArchetypes.OpenConditionExist :new-folder false}
   {:type TransactionTag :file "Assets/Scripts/EcsExtensions/TransactionTag.cs"
    :namespace EcsExtensions :label-of ActionsArchetypes.BuildDistrictInProgress :attribute "[TagLabel(TagLabelRole.Transaction)]"
    :home "shared kernel — :od-label-home: транзакційна сутність може жити в будь-якому домені дієслів, і тег не знає жодного"}])
```

```clojure
(def tag-law-and-markers-edits  ;; порядок — кроки s1; «рядок N» — номер у файлі на 2026-09-15; маркер стає окремим рядком над class, під наявним [UsedImplicitly]
  [{:s1-steps [1 2] :kind :verify
    :tag-carriers "UITag — 4 архетипи, DistrictOpenConditionTag — 2, EventTag — 13 подій, решта 20 тегів — по одному (:tags-today)"
    :birth-sites "0 поза холдерами: grep UITag, DistrictOpenConditionTag, BuildDistrictInProgressTag по п'яти коренях дає лише холдери й коментарі (2026-09-15); стадія коду повторює grep перед кроком 6"
    :tag-roles "EventTag — головний-виняток; UITag і DistrictOpenConditionTag — label без ролі; решта 20 — головні; архетипів без унікального головного — 6; архетип рецепта транзакції — BuildDistrictInProgress"}

   {:s1-step 3 :kind :create :files "5 файлів marker-types"}

   {:s1-step 4 :kind :create :files "7 файлів new-tags; три нові теки Tags/ — .meta теки й файлів створює Unity при імпорті, не агент"
    :shadowing "простори імен …Tags уже є в Presentation.UI і в DistrictOpenCondition — нового затінення Friflo Tags нема, псевдонім EcsTags у холдері лишається"}

   {:s1-step 5 :kind :edit :file "Assets/Presentation/UI/Tags/UITag.cs"
    :do ["додати using EcsExtensions;" "над struct — [TagLabel]"]}

   {:s1-step 5 :kind :edit :file "Assets/Domains/Economy/DistrictOpenCondition/Tags/DistrictOpenConditionTag.cs"
    :do ["додати using EcsExtensions;" "над struct — [TagLabel]"
         "коментар «Discriminator for the district-open-condition table (key: DistrictTypeFKComponent).» → «Family label of the two district-open-condition archetypes (key: DistrictTypeFKComponent); each archetype's own main tag discriminates it.»"]}

   {:s1-step 6 :kind :edit :file "Assets/Presentation/UI/Archetypes/PresentationUIArchetypes.cs"
    :do ["using Presentation.UI.MainHud.HexInfoPanel.Tags, using Presentation.UI.MainHud.ResourceBar.Tags, using Presentation.UI.MainHud.TurnPanel.Tags — в алфавітному порядку наявних using"
         "рядок 23: EcsTags.Get<UITag>() → EcsTags.Get<HexInfoPanelTag, UITag>()"
         "рядок 26: EcsTags.Get<UITag>() → EcsTags.Get<TurnPanelTag, UITag>()"
         "рядок 29: EcsTags.Get<UITag>() → EcsTags.Get<ResourceBarTag, UITag>()"
         "рядок 32: EcsTags.Get<UITag>() → EcsTags.Get<DistrictBuildUITag, UITag>()"
         "коментар класу, рядки 17-18: речення про спільний category tag → «Each HUD panel carries its own main tag; <see cref=\"UITag\" /> stands beside it as the label of HUD membership and is never a filter.»"]}

   {:s1-step 6 :kind :edit :file "Assets/Domains/Economy/Archetypes/EconomyArchetypes.cs"
    :do ["рядок 37: Tags.Get<DistrictOpenConditionTag>() → Tags.Get<DistrictSingleOpenConditionTag, DistrictOpenConditionTag>()"
         "рядок 44: Tags.Get<DistrictOpenConditionTag>() → Tags.Get<DistrictExistOpenConditionTag, DistrictOpenConditionTag>()"
         "коментар OpenConditionSingle, рядки 29-30: «Open conditions form TWO archetypes under one tag:» → «Open conditions form TWO archetypes, each under its own main tag beside the family label <see cref=\"DistrictOpenConditionTag\" />:»"]}

   {:s1-step 6 :kind :edit :file "Assets/Domains/Actions/Archetypes/ActionsArchetypes.cs"
    :do ["додати using EcsExtensions;"
         "рядок 23: Tags.Get<BuildDistrictInProgressTag>() → Tags.Get<BuildDistrictInProgressTag, TransactionTag>()"]}

   {:s1-step 6 :kind :verify :birth-sites "місць народження поза холдерами нема — правок 0"}

   {:s1-step 7 :kind :edit :file "Assets/Modules/Turn/Systems/TurnProcessorSystem.cs" :line 24 :do "[SystemRole(SystemRoleKind.PerFrame)]"}
   {:s1-step 7 :kind :edit :file "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictCompletionSystem.cs" :line 32 :do "[SystemRole(SystemRoleKind.Reactive)]"}
   {:s1-step 7 :kind :edit :file "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildUISystem.cs" :line 32 :do "[SystemRole(SystemRoleKind.PerFrame)]"}

   {:s1-step 8 :kind :edit :file "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelDistrictSystem.cs" :line 38 :do "[ViewSubscriber(typeof(HexInfoPanelView))]"}
   {:s1-step 8 :kind :edit :file "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildUISystem.cs" :line 32 :do "[ViewSubscriber(typeof(DistrictBuildUIView))] — під маркером ролі"}
   {:s1-step 8 :kind :edit :file "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildListUISubSystem.cs" :line 24
    :do ["[ViewSubscriber(typeof(DistrictBuildListUIView))]" "додати using Presentation.UI.DistrictBuild.Views;"]}
   {:s1-step 8 :kind :edit :file "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildPriceUISubSystem.cs" :line 30 :do "[ViewSubscriber(typeof(DistrictBuildPriceUIView))]"}

   {:s1-step 9 :kind :verify :assembly-reach "Turn, Domains.Actions, Domains.Economy, Presentation.UI прямо посилаються на Ecs.Extensions; тег ролі transaction живе в самій Ecs.Extensions — asmdef-правок 0"}])
```

```clojure
{:part tag-law-and-markers
 :s1-coverage {:holder-declarations "правки кроку 6 у трьох холдерах"
               :tag-types "7 нових типів і 2 з [TagLabel] — типів тегів 30"
               :tag-carriers "verify кроків 1-2"
               :birth-sites "verify кроків 1 і 6 — 0"
               :tag-roles "verify кроку 2 і :main-of / :label-of у new-tags"
               :marker-types "marker-types"
               :new-main-tags "new-tags, перші 6"
               :transaction-label "new-tags, сьомий"
               :pollers "правки кроку 7"
               :subscribers "правки кроку 8"
               :assembly-reach "verify кроку 9"
               :tally "11 / 11"}
 :numbers-check {:role-markers 3 :view-markers 4 :label-markers "3 — UITag, DistrictOpenConditionTag, тег ролі transaction" :holders-changed 3 :asmdef-edits 0}
 :verification "CLAUDE.md § 2 code-verification на змінених .cs; аудит тегів — уже новим інструментом, після частини 4 (c-4 у # Contra)"}
```

## Частина 2 · drift-guard — аналізатор Roslyn

```clojure
(def analyzer-project  ;; :analyzer-project, :od-analyzer-deps, :diagnostic-ids, :dll-import-path
  {:folder "Tools/MarkerShapeAnalyzer/"
   :files ["MarkerShapeAnalyzer.csproj" "MarkerShapeAnalyzer.cs" "MarkerVocabulary.cs" "MarkedTypeCheck.cs" "TypeShape.cs"]
   :namespace FantasyMayor.Analyzers
   :csproj {:sdk "Microsoft.NET.Sdk"
            :TargetFramework "netstandard2.0"
            :LangVersion "9.0"
            :AssemblyName "MarkerShapeAnalyzer"
            :PackageReference "Microsoft.CodeAnalysis.CSharp 4.3.0, PrivateAssets all"}
   :language-limit "у netstandard2.0 нема IsExternalInit — записів C# 9 нема; MarkerVocabulary і TypeShape — sealed class з readonly полями"
   :build "dotnet build Tools/MarkerShapeAnalyzer/MarkerShapeAnalyzer.csproj -c Release — перша збірка тягне Microsoft.CodeAnalysis.CSharp 4.3.0 з nuget.org; без мережі — s1-вихід, DLL нема"
   :dll "Tools/MarkerShapeAnalyzer/bin/Release/netstandard2.0/MarkerShapeAnalyzer.dll"
   :dll-to-assets {:who :owner
                   :do "скопіювати DLL у Assets/Scripts/EcsExtensions/, у Plugin Inspector зняти всі платформи й поставити мітку RoslynAnalyzer — одним імпортом"
                   :why "Unity-side за :out-of-scope; .meta генерує Unity; DLL, яку агент поклав би без налаштувань, до кроку власника імпортувалась би звичайним плагіном з посиланням на Microsoft.CodeAnalysis"}
   :diagnostics [{:id "FM1001" :descriptor PerFrameRoleMismatch
                  :message "{0} claims SystemRole(PerFrame), but a per-frame system is a non-abstract IUpdatedSystem or ILateUpdatedSystem whose base(...) is not anchored on an event archetype"}
                 {:id "FM1002" :descriptor ReactiveRoleMismatch
                  :message "{0} claims SystemRole(Reactive), but a reactive system is a non-abstract IUpdatedSystem or ILateUpdatedSystem, not anchored on a table in base(...), that anchors on or holds an event archetype"}
                 {:id "FM1003" :descriptor ViewSubscriberMismatch
                  :message "{0} claims ViewSubscriber({1}), but {1} must derive from MonoBehaviour and {0} must add a handler with += to an event declared by {1} or its base"}
                 {:id "FM1004" :descriptor TagLabelMismatch
                  :message "{0} claims TagLabel, but only a struct implementing ITag can be a label tag"}]
   :descriptor-common {:category "FantasyMayor.Markers" :severity "DiagnosticSeverity.Error" :enabled-by-default true}
   :gitignore ["після рядка 49 *.csproj — !/Tools/MarkerShapeAnalyzer/MarkerShapeAnalyzer.csproj" "/Tools/MarkerShapeAnalyzer/bin/" "/Tools/MarkerShapeAnalyzer/obj/"]
   :first-edit "CLAUDE.md § 3 рядок 63 — заборона збірки уточнюється ДО dotnet build аналізатора (tool-cutover-edits, запис :build-ban)"})
```

```clojure
(def analyzer-registration  ;; :analyzer-registration — c-4 s1
  {:chosen "RegisterSymbolStartAction(NamedType): тип без маркера — одна перевірка атрибутів і нічого далі; тип з маркером — RegisterSyntaxNodeAction на вузли його оголошень (кожна дія має готову SemanticModel) і RegisterSymbolEndAction для звірки"
   :confidence 70
   :why "область SymbolStart віддає вузли всіх часткових оголошень типу — partial закрито без Compilation.GetSemanticModel (RS1030) і без діагностик кінця компіляції"
   :rejected [{:is "дія над синтаксисом по всій компіляції з накопиченням і звіркою в CompilationEnd" :confidence 20}
              {:is "дія над символом і GetSemanticModel дерев його оголошень" :confidence 10}]})
```

```clojure
(def MarkerShapeAnalyzer-methods  ;; MarkerShapeAnalyzer.cs — [DiagnosticAnalyzer(LanguageNames.CSharp)] public sealed class : DiagnosticAnalyzer; поля — чотири static readonly дескриптори, SupportedDiagnostics повертає їх
  {Initialize
   {:does "підключає перевірку маркерів до кожної компіляції в області DLL"
    :in #{}
    :out :none
    :how "три рядки реєстрації: ConfigureGeneratedCodeAnalysis(None), EnableConcurrentExecution(), RegisterCompilationStartAction(StartCompilation)"
    :ends-with "кроків хребта нема — лише реєстрація"}

   StartCompilation
   {:does "назвати маркери й якорі форм цієї збірки і стежити за кожним іменованим типом"
    :in #{:compilation}
    :out :none
    :flow (-> (:step-1 "знайти типи маркерів і якорі форм" {:calls MarkerVocabulary.Resolve :out :vocabulary}))
    :ends-with "context.RegisterSymbolStartAction(typeStart => StartType(typeStart, vocabulary), SymbolKind.NamedType) — рядок реєстрації"
    :note "s1-вихід «збірка не бачить жодного типу маркера» гілкою не стає: за домом EcsExtensions кожна збірка в області DLL маркери бачить; якби DLL лягла деінде, поля маркерів — null, і CollectClaims нічого не знаходить"}

   StartType
   {:does "почати перевірку типу, що несе маркер"
    :in #{:marked-type :vocabulary}
    :out :none
    :flow (-> (:step-1 "маркери типу з аргументами" {:calls CollectClaims :out :claims}))
    :exits #{"claims порожні — тип без маркера: return, нічого не реєструючи (маркерів у коді 10 на понад тисячу типів — s1 :markers-in-code)"}
    :ends-with "var check = new MarkedTypeCheck(type, claims, vocabulary) — рядок часу життя; далі реєстрація: check.SightBaseAnchor на BaseConstructorInitializer, check.SightHeldEventArchetype на InvocationExpression, check.SightSubscription на AddAssignmentExpression, RegisterSymbolEndAction(check.ReportMismatches)"}

   CollectClaims
   {:does "вибирає атрибути типу, чий клас — один із трьох маркерів"
    :in #{:marked-type :vocabulary}
    :out :claims
    :how "type.GetAttributes() з AttributeClass, рівним за SymbolEqualityComparer.Default полю SystemRole, ViewSubscriber або TagLabel словника"}})
```

```clojure
(def MarkerVocabulary-methods  ;; MarkerVocabulary.cs — sealed class, readonly поля за :fields у даних
  {Resolve
   {:does "знаходить у збірці типи маркерів і якорі форм за metadata-іменами"
    :in #{:compilation}
    :out :vocabulary
    :how "static, compilation.GetTypeByMetadataName для кожного імені з :numbers, тип, якого збірка не бачить, лишається null"
    :numbers {:marker-names "EcsExtensions.SystemRoleAttribute, EcsExtensions.ViewSubscriberAttribute, EcsExtensions.TagLabelAttribute"
              :anchor-names "EcsExtensions.IUpdatedSystem, EcsExtensions.ILateUpdatedSystem, EcsExtensions.UpdatedSystem, EcsExtensions.LateUpdatedSystem, EcsExtensions.EventArchetypes, Friflo.Engine.ECS.ComponentTypes, Friflo.Engine.ECS.ITag, UnityEngine.MonoBehaviour"}}})
```

```clojure
(def MarkedTypeCheck-methods  ;; MarkedTypeCheck.cs — internal sealed class, один на тип з маркером; порядок файла — порядок його життя: спостереження приходять, звіт у кінці
  {MarkedTypeCheck
   {:does "тримає тип, його маркери і словник від SymbolStart до SymbolEnd"
    :in #{:marked-type :claims :vocabulary}
    :out :none
    :how "конструктор, readonly _type, _claims, _vocabulary, три порожні мішки спостережень"}

   SightBaseAnchor
   {:does "записує, на що спирається base(...) конструктора типу — на подієвий архетип чи на таблицю"
    :in #{:declarations :marked-type :vocabulary}
    :out :none
    :writes #{:base-anchor-sightings}
    :scratch #{:base-anchor}
    :flow (-> (:step-1 "ініціалізатор належить конструктору саме цього типу, а пряма база типу — UpdatedSystem або LateUpdatedSystem; інакше return")
              (:step-2 (cond
                         (:flow-1 "хоч один аргумент — виклик EventArchetypes.Of<E>") (:conclusion-1 "Event")
                         (:flow-2 "хоч один аргумент — AnyComponents(ComponentTypes.Get<…>), і кожен type-аргумент Get — struct з суфіксом Event або EventComponent") (:conclusion-2 "Event")
                         (:flow-3 "інакше") (:conclusion-3 "Table")))
              (:step-3 "додати вид у _baseAnchors"))
    :calls #{IsEventArchetypeCall}
    :note "return на кроці 1 має два приводи: вузол вкладеного типу теж приходить у дію зовнішнього (ContainingType ≠ _type); base(...) іншої бази — не якір системи, marker-shapes anchored-on-event? вимагає пряму базу UpdatedSystem або LateUpdatedSystem. Перевірка AnyComponents одна — інлайн"}

   IsEventArchetypeCall
   {:does "чи виклик — EventArchetypes.Of<E>"
    :in #{:vocabulary}
    :out bool
    :how "semanticModel.GetSymbolInfo(invocation).Symbol як IMethodSymbol з Name Of і ContainingType, рівним полю EventArchetypes словника"
    :note "два викликачі — SightBaseAnchor і SightHeldEventArchetype"}

   SightHeldEventArchetype
   {:does "записує подієвий архетип, який тип тримає поза base(...)"
    :in #{:declarations :marked-type :vocabulary}
    :out :none
    :writes #{:held-event-sightings}
    :flow (-> (:step-1 "виклик належить цьому типу і не стоїть усередині BaseConstructorInitializer; інакше return")
              (:step-2 "IsEventArchetypeCall — додати IMethodSymbol у _heldEventArchetypes"))
    :calls #{IsEventArchetypeCall}
    :note "return: вкладений тип; виклик в аргументі base(...) — якір, його пише SightBaseAnchor (holds-event-archetype? — «поза аргументом base(...)»)"}

   SightSubscription
   {:does "записує подію, до якої тип додає обробник +="
    :in #{:declarations :marked-type}
    :out :none
    :writes #{:subscription-sightings}
    :flow (-> (:step-1 "присвоєння належить цьому типу; інакше return")
              (:step-2 "символ лівої частини — IEventSymbol: додати в _subscribedEvents; інакше return"))
    :note "return на кроці 2: += над числом чи рядком — не підписка"}

   ReportMismatches
   {:does "звіряє маркери типу з його формою і віддає розбіжності компілятору"
    :in #{:marked-type :claims}
    :out :none
    :flow (-> (:step-1 "виміряти форму за всіма оголошеннями" {:calls MeasureShape :out :shape})
              (:step-2 "звірити кожен маркер з формою" {:calls FindMismatches :out :mismatches})
              (:step-3 "віддати розбіжності компілятору" {:calls context.ReportDiagnostic :out :none}))
    :ends-with "foreach (var mismatch in mismatches) context.ReportDiagnostic(mismatch) — один рядок"}

   MeasureShape
   {:does "складає фактичну форму типу з символу й зібраних спостережень"
    :in #{:marked-type :vocabulary :base-anchor-sightings :held-event-sightings :subscription-sightings}
    :out :shape
    :how "LoopContract — не IsAbstract і AllInterfaces містить IUpdatedSystem або ILateUpdatedSystem, EventAnchored — у _baseAnchors є Event, TableAnchored — є Table, HeldEventArchetypes і SubscribedEvents — знімки мішків, TagStruct — TypeKind.Struct і AllInterfaces містить ITag"}

   FindMismatches
   {:does "для кожного маркера перевіряє форму, яку він стверджує"
    :in #{:claims :shape :descriptors}
    :out :mismatches
    :flow (-> (:step-1 (cond
                         (:flow-1 "SystemRole з PerFrame") (:conclusion-1 "збіг — LoopContract і не EventAnchored; інакше FM1001")
                         (:flow-2 "SystemRole з Reactive") (:conclusion-2 "збіг — LoopContract, не TableAnchored, і EventAnchored або HeldEventArchetypes не порожні; інакше FM1002")
                         (:flow-3 "ViewSubscriber з typeof(V)") (:conclusion-3 "збіг — DerivesFrom(V, MonoBehaviour) і хоч одна подія з SubscribedEvents має ContainingType, від якого походить V; інакше FM1003")
                         (:flow-4 "TagLabel") (:conclusion-4 "збіг — TagStruct; інакше FM1004")))
              (:step-2 "розбіжність — Diagnostic.Create(дескриптор, місце атрибута, ім'я типу, ім'я V)"))
    :calls #{DerivesFrom}
    :note "місце — claim.ApplicationSyntaxReference.GetSyntax().GetLocation(): тип прийшов із SymbolStart над вихідним кодом, тож атрибут має синтаксис; роль — (SystemRoleKind)claim.ConstructorArguments[0].Value, V — ConstructorArguments[0].Value як INamedTypeSymbol"}

   DerivesFrom
   {:does "чи тип — це предок або походить від нього"
    :out bool
    :how "іти по BaseType від типу до null, порівнюючи SymbolEqualityComparer.Default"
    :note "два виклики з кроку 1 FindMismatches: V від MonoBehaviour; V від ContainingType події, бо подія може бути оголошена в предку V"}})
```

```clojure
(def drift-guard-data
  {:compilation            {:from-s1 :compilation :as context.Compilation :lives :external :note "дає Roslyn на старті компіляції"}
   :vocabulary             {:shape ^:new MarkerVocabulary :as vocabulary :lives StartCompilation
                            :fields {SystemRole :marker-types ViewSubscriber :marker-types TagLabel :marker-types
                                     UpdatedSystemInterface :shape-anchors LateUpdatedSystemInterface :shape-anchors
                                     UpdatedSystemBase :shape-anchors LateUpdatedSystemBase :shape-anchors
                                     EventArchetypes :shape-anchors ComponentTypes :shape-anchors
                                     TagInterface :shape-anchors MonoBehaviour :shape-anchors}
                            :holds "три типи маркерів і вісім якорів форм однієї збірки"
                            :note "захоплює лямбда StartType; кожен MarkedTypeCheck тримає її readonly"}
   :marker-types           {:from-s1 :marker-types :as SystemRole :lives StartCompilation
                            :note "поля SystemRole, ViewSubscriber, TagLabel словника; s1 :type ImmutableArray<INamedTypeSymbol> стає іменованими полями — масив за позицією був би ненародженим типом"}
   :shape-anchors          {:from-s1 :shape-anchors :as UpdatedSystemInterface :lives StartCompilation
                            :note "вісім полів словника, так само замість масиву; якір, якого збірка не бачить, — null"}
   :marked-type            {:from-s1 :marked-type :as _type :lives :external :note "context.Symbol у StartType; MarkedTypeCheck тримає readonly"}
   :claims                 {:from-s1 :claims :as claims :lives StartType :note "передано в MarkedTypeCheck — readonly _claims"}
   :declarations           {:from-s1 :declarations :as context.Node :lives :external
                            :note "у масив не збирається: область SymbolStart віддає вузли всіх часткових оголошень у зворотні виклики"}
   :base-anchor            {:shape ^:new BaseAnchor :as anchor :lives SightBaseAnchor :holds "enum { Event, Table } — вид одного base(...)"}
   :base-anchor-sightings  {:shape ^:new ConcurrentBag<BaseAnchor> :as _baseAnchors :lives :run
                            :holds "вид кожного base(...) у конструкторах типу"
                            :note "пише лише SightBaseAnchor, але багато разів і з різних дерев, читає MeasureShape в іншому зворотному виклику — між викликами хоста лишається тільки поле; ConcurrentBag — бо дії одного символу можуть іти паралельно"}
   :held-event-sightings   {:shape ^:new ConcurrentBag<IMethodSymbol> :as _heldEventArchetypes :lives :run :holds "виклики EventArchetypes.Of поза base(...)" :note "те саме, що _baseAnchors"}
   :subscription-sightings {:shape ^:new ConcurrentBag<IEventSymbol> :as _subscribedEvents :lives :run :holds "події, до яких тип додає обробник" :note "те саме, що _baseAnchors"}
   :shape                  {:shape ^:new TypeShape :as shape :lives ReportMismatches
                            :fields {LoopContract :loop-contract EventAnchored :anchor-on-event TableAnchored :anchor-on-table
                                     HeldEventArchetypes :held-event-archetypes SubscribedEvents :subscribed-events TagStruct :tag-struct}
                            :holds "фактична форма типу — те, що міряє marker-shapes"}
   :loop-contract          {:from-s1 :loop-contract :as LoopContract :lives ReportMismatches :note "поле TypeShape"}
   :anchor-on-event        {:from-s1 :anchor-on-event :as EventAnchored :lives ReportMismatches :note "поле TypeShape"}
   :anchor-on-table        {:from-s1 :anchor-on-table :as TableAnchored :lives ReportMismatches :note "поле TypeShape"}
   :held-event-archetypes  {:from-s1 :held-event-archetypes :as HeldEventArchetypes :lives ReportMismatches :note "поле TypeShape, знімок мішка"}
   :subscribed-events      {:from-s1 :subscribed-events :as SubscribedEvents :lives ReportMismatches :note "поле TypeShape, знімок мішка"}
   :tag-struct             {:from-s1 :tag-struct :as TagStruct :lives ReportMismatches :note "поле TypeShape"}
   :descriptors            {:from-s1 :descriptors :as SupportedDiagnostics :lives :external
                            :note "static readonly PerFrameRoleMismatch, ReactiveRoleMismatch, ViewSubscriberMismatch, TagLabelMismatch — живуть з типом аналізатора"}
   :mismatches             {:from-s1 :mismatches :as mismatches :lives ReportMismatches}
   :analyzer-dll           {:from-s1 :analyzer-dll :as MarkerShapeAnalyzer.dll :lives :external :note "вихід dotnet build — analyzer-project :dll, не значення коду"}})
```

```clojure
{:part drift-guard
 :spine-reads {StartCompilation "var vocabulary = MarkerVocabulary.Resolve(context.Compilation); context.RegisterSymbolStartAction(…)"
               StartType "var claims = CollectClaims(type, vocabulary); if (claims.IsEmpty) return; var check = new MarkedTypeCheck(type, claims, vocabulary); реєстрація чотирьох дій"
               ReportMismatches "var shape = MeasureShape(); var mismatches = FindMismatches(shape); foreach (var mismatch in mismatches) context.ReportDiagnostic(mismatch)"}
 :steps-visible {StartCompilation 1 StartType 1 ReportMismatches 3}
 :data-coverage {:s1-coverage "15 / 15"
                 :undeclared 0      ;; bool предикатів IsEventArchetypeCall і DerivesFrom — механіка, не іменник задачі
                 :orphan 0          ;; :analyzer-dll названо в analyzer-project — артефакт збірки, не значення методу
                 :one-step-fields "0 серед полів хребта; 3 мішки спостережень пише по одному зворотному виклику — :note, кожен читає інший виклик хоста"
                 :unborn 0}
 :how-semicolons 0
 :max-params "усі ≤ 3; дії хоста беруть один context"
 :reader-clean true}
```

## Частина 3 · unified-graph — збірка злитого графа

```clojure
(def graph-tool-layout  ;; :od-tool-name, :skill-layout, :build-pass-shape
  {:home ".claude/skills/fantasymayor-graph/"
   :scripts [{:module fmgraph.py         :holds "CLI — main, parse_request, refresh_graph, answer_request" :part graph-queries}
             {:module build.py           :holds "build_graph — хребет збірки; list_source_files, remove_legacy_artifact_dirs; SCAN_ROOTS, LEGACY_ARTIFACT_DIRS" :part unified-graph}
             {:module graph_store.py     :holds "ARTIFACT_DIR, SCHEMA, find_project_root, is_graph_fresh, write_graph, load_graph, LoadedGraph" :part #{unified-graph graph-queries}}
             {:module graph_draft.py     :holds "GraphDraft, Edge — граф під час збірки" :part unified-graph}
             {:module source_reading.py  :holds "read_sources, read_file, читачі вузлів і примітиви tree-sitter" :part unified-graph}
             {:module type_facts.py      :holds "know_types, declare_nodes, read_markers, build_ancestry, TypeFacts" :part unified-graph}
             {:module ecs_facts.py       :holds "expand_archetypes, register_archetype, reconcile_ecs_facts і його фази" :part unified-graph}
             {:module di_facts.py        :holds "reconcile_di_facts і його фази, connect_hosts" :part unified-graph}
             {:module view_pairs.py      :holds "pair_view_subscribers" :part unified-graph}
             {:module tag_law.py         :holds "audit_tag_law" :part unified-graph}
             {:module roles.py           :holds "decide_roles, collect_role_evidence, decide_role, connect_event_edges" :part unified-graph}
             {:module recipes.py         :holds "RECIPE_SIGNATURES, RECIPE_NAMES, find_recipe_instances і ознаки" :part unified-graph}
             {:module queries.py         :holds "COMMANDS і answer_* — функція на команду" :part graph-queries}]
   :executables #{fmgraph.py}
   :artifacts "<project>/.fantasymayor-graph/graph.json"
   :why-modules "кожен модуль — різнорідний крок хребта (різні іменники, різна механіка); два сьогоднішні білдери разом 1866 рядків — одним файлом абзаци не читались би"
   :pass-shape {:chosen "один розбір і один обхід дерева на файл" :confidence 60
                :why "сайт знає клас-власника через найближче охоплююче оголошення, тож порядок обходу неважливий; сьогодні два білдери розбирають кожен файл двічі"
                :rejected {:is "один розбір, два обходи — оголошення, потім сайти" :confidence 40}}})
```

```clojure
(def unified-graph-methods
  {build_graph
   {:module build.py
    :does "збирає злитий граф одним детермінованим проходом і кладе його на диск"
    :in #{:project-root}
    :out :graph-doc
    :flow (-> (:step-1 "перелічити .cs під коренями" {:calls list_source_files :out :source-files})
              (:step-2 "розібрати файли: оголошення й сирі факти" {:calls read_sources :out :source-facts})
              (:step-3 "знати кожен тип: вузол, маркери, родовід" {:calls know_types :out :type-facts})
              (:step-4 "розгорнути архетипи" {:calls expand_archetypes :out :none})
              (:step-5 "звірити ECS-факти" {:calls reconcile_ecs_facts :out :none})
              (:step-6 "звірити DI-факти" {:calls reconcile_di_facts :out :none})
              (:step-7 "з'єднати хости з родинами" {:calls connect_hosts :out :none})
              (:step-8 "звести пари view ↔ підписник" {:calls pair_view_subscribers :out :none})
              (:step-9 "перевірити закон тегів" {:calls audit_tag_law :out :none})
              (:step-10 "вирішити ролі" {:calls decide_roles :out :none})
              (:step-11 "знайти екземпляри рецептів" {:calls find_recipe_instances :out :none})
              (:step-12 "записати граф" {:calls write_graph :out :graph-doc})
              (:step-13 "прибрати старі теки артефактів" {:calls remove_legacy_artifact_dirs :out :none}))
    :ends-with "return graph_doc"
    :note "draft = GraphDraft(sources.parse_warnings) — рядок часу життя між кроками 2 і 3; крок 13 живе до tool-cutover кроку 7"}

   list_source_files
   {:module build.py
    :does "перелічує .cs під коренями сканування, відсортовані"
    :in #{:project-root :scan-roots}
    :out :source-files
    :how "rglob *.cs під кожним наявним коренем, sorted"
    :numbers {:files "359 = 358 + 1 з Assets/Flows"}}

   read_sources
   {:module source_reading.py
    :does "розбирає кожен файл один раз і складає оголошення й сирі факти всього коду"
    :in #{:project-root :source-files}
    :out :source-facts
    :scratch #{:parser :file-facts}
    :flow (-> (:step-1 "один Parser(LANG) на прохід")
              (:step-2 (cond
                         (:flow-1 "для кожного файла") (:conclusion-1 "read_file → FileFacts, його списки дописати в SourceFacts")
                         (:flow-2 "розбір файла кинув виняток") (:conclusion-2 "parse_warnings: «parse error in <шлях>: <виняток>», далі наступний файл"))))
    :calls #{read_file}
    :ends-with "return SourceFacts"
    :note "except на файл — s1-вихід «файл .cs не розбирається — попередження, скан триває» (build_graph.py:1156-1159); попередження йде в граф і в check, тож тихого пропуску нема"}

   read_file
   {:module source_reading.py
    :does "читає один файл одним обходом дерева"
    :in #{:parser :project-root :source-files}
    :out :file-facts
    :flow (-> (:step-1 "parser.parse(байти файла), rel — шлях від кореня")
              (:step-2 (cond
                         (:flow-1 "class | struct | record | interface | enum declaration") (:conclusion-1 "read_type_declaration; клас з суфіксом Archetypes — ще read_holder_members")
                         (:flow-2 "using_directive") (:conclusion-2 "простір імен у usings файла; охоплюючий namespace — теж")
                         (:flow-3 "invocation_expression") (:conclusion-3 "read_ecs_call і read_registration — кожен бере своє або нічого")
                         (:flow-4 "object_creation_expression …State у методі Construct класу Boot") (:conclusion-4 "read_state_composition")
                         (:flow-5 "assignment_expression з += над a.E") (:conclusion-5 "read_subscription")
                         (:flow-6 "element_access_expression над ідентифікатором поля") (:conclusion-6 "index usage в ecs_sites"))))
    :calls #{read_type_declaration read_holder_members read_ecs_call read_registration read_state_composition read_subscription}
    :note "один обхід — :build-pass-shape; примітиви text, line_of, field, first_child_of_type, analyze_invocation, arg_exprs, nearest_enclosing, namespace_of переносяться з build_graph.py як є"}

   read_type_declaration
   {:module source_reading.py
    :does "знімає одне оголошення типу"
    :in #{:parser}
    :out :declarations
    :how "вид, ім'я з ланцюгом вкладеності, повний namespace, abstract, атрибути з аргументами (typeof і доступ до члена — текстом), бази як TypeUse, параметри конструкторів і [Inject]-методів як TypeUse, поля (TypeUse, атрибути), імена event-оголошень, Priority і const int, тека файла, source_location"
    :calls #{read_type_use}}

   read_type_use
   {:module source_reading.py
    :does "читає написаний тип: ім'я, generic-аргументи, елемент колекції, масив"
    :out :type-use
    :how "IReadOnlyList | IEnumerable | IList | List | ICollection | IReadOnlyCollection з одним аргументом — колекція з елементом, array_type — масив з елементом, generic_name — ім'я й аргументи як написано"
    :note "три викликачі — бази, параметри, поля; замінює clean_type і collection_element обох білдерів"}

   read_holder_members
   {:module source_reading.py
    :does "знімає члени холдера архетипів"
    :out :holders
    :how "метод, що повертає Archetype, — GetArchetype(ComponentTypes.Get<…>, Tags.Get<…> або EcsTags.Get<…>) з => або першого return, type-параметри методу, метод, що повертає SingletonArchetypeDefinition, — componentTypes.Add<T>() і Tags.Get у тілі"}

   read_ecs_call
   {:module source_reading.py
    :does "класифікує виклик ECS-API і пише сирий факт з місцем і класом-власником"
    :out :ecs-sites
    :how "AddComponent, GetComponent, HasComponent, RemoveComponent, RemoveTag, Singletons.Get | Has | Set, CreateEvent, DeleteEntity, AllTags | AnyTags | WithoutAnyTags з типами, AnyComponents(ComponentTypes.Get<…>) з типами, Tags.Add, виклик холдера і EventArchetypes.Of<T>, виклик з type-аргументами — ще typed call для шаблонів"
    ;; :from-code rb-20 — anchor base лише під base(...), this(...) — body; чи база системна, вирішує ecs_facts anchor_of (:rb-20-anchor)
    :note "anchor base — виклик усередині constructor_initializer, інакше body; виклик, чиї type-аргументи — параметри охоплюючого generic-методу, — ретранслятор, лише typed call (правило is_template_use з build_graph.py)"}

   read_registration
   {:module source_reading.py
    :does "знімає реєстрацію DI з повним ланцюжком"
    :out :di-sites
    :how "Register і RegisterInstance у класі-інсталері: type-аргументи як TypeUse, As<…> цілі ланцюжка, Lifetime, WithParameter(AppState.X), RegisterInstance — аргумент і чи він ідентифікатор, RegisterFactory, RegisterComponentInHierarchy, RegisterEntryPoint, AsImplementedInterfaces — сайт unknown-form"}

   read_state_composition
   {:module source_reading.py
    :does "знімає створення стану гри в Boot.Construct"
    :out :di-sites
    :how "ім'я стану, ідентифікатори в аргументах, мапа параметр Construct → TypeUse"}

   read_subscription
   {:module source_reading.py
    :does "знімає підписку a.E += H"
    :out :subscription-sites
    :how "ім'я події E, текст отримувача a, клас-власник, source_location"}

   know_types
   {:module type_facts.py
    :does "знає про кожен тип його вузол, маркери й родовід"
    :in #{:source-facts :draft}
    :out :type-facts
    :flow (-> (:step-1 "вузли й розв'язані посилання" {:calls declare_nodes :out :none})
              (:step-2 "маркери" {:calls read_markers :out :markers})
              (:step-3 "родовід" {:calls build_ancestry :out :ancestry}))
    :ends-with "return TypeFacts(markers, ancestry)"}

   declare_nodes
   {:module type_facts.py
    :does "дає кожному оголошенню один id і розв'язує посилання в сирих фактах"
    :in #{:declarations :usings :ecs-sites :di-sites :subscription-sites}
    :out :none
    :writes #{:nodes :ecs-sites :di-sites :subscription-sites :warnings}
    :flow (-> (:step-1 "ім'я з ланцюгом вкладеності → оголошення; ім'я кількох оголошень — id з повним простором імен")
              (:step-2 "вузол на оголошення: struct — tag | event | component | data за classify_struct, interface — interface, клас з суфіксом Installer або базою IInstaller | LifetimeScope — installer, решта — other; name, namespace, source_location, abstract, declared true")
              (:step-3 "кожне ім'я типу в сайтах → draft.resolve(ім'я, usings файла сайта)"))
    :note "kind config і view ставить build_ancestry, system — decide_roles; архетипи додає expand_archetypes"}

   read_markers
   {:module type_facts.py
    :does "прив'язує кожен маркер до вузла класу чи тегу"
    :in #{:declarations :nodes}
    :out :markers
    :writes #{:warnings :nodes}   ;; :from-code rb-19 — resolve додає недекларовану view, add_node — грань markers
    :how "атрибут SystemRole або SystemRoleAttribute — role з останнього сегмента аргументу (PerFrame → per_frame, Reactive → reactive), ViewSubscriber — views += draft.resolve(ім'я з typeof), TagLabel без аргументу — label none, з TagLabelRole.Transaction — label transaction"
    :note "typeof(V), що не розв'язується однозначно, — попередження AMBIGUOUS від resolve, view не додається"}

   build_ancestry
   {:module type_facts.py
    :does "будує родовід: ребра inherits, транзитивні предки, види config і view"
    :in #{:declarations :nodes}
    :out :ancestry
    :writes #{:edges :nodes}
    :flow (-> (:step-1 "кожна база оголошення — ребро inherits з args як написано до розв'язаного вузла; база поза кодом гри (MonoBehaviour, ScriptableObject) — вузол declared false")
              (:step-2 "транзитивні предки кожного типу обходом inherits з множиною відвіданих")
              (:step-3 "не-abstract клас з предком ScriptableObject | SerializedScriptableObject — kind config; клас у теці Views/ з предком MonoBehaviour — kind view"))
    :numbers {:config-bases "ScriptableObject, SerializedScriptableObject" :view-folder "Views/"}
    :note "множина відвіданих — механіка обходу: спільного предка не проходимо двічі; AMBIGUOUS-база ребром не стає"}

   expand_archetypes
   {:module ecs_facts.py
    :does "розгортає архетипи з холдерів і позначає, як власники до них прив'язані"
    :in #{:holders :ecs-sites :markers}
    :out :none
    :writes #{:nodes :edges :warnings}
    :scratch #{:holder-templates :archetype-declaration}
    :flow (-> (:step-1 "конкретний член холдера і маніфест singleton — register_archetype з id Холдер.Член")
              (:step-2 "generic-член — шаблон; typed call, що не ретранслятор, — насіння; черга підставляє аргументи крізь методи-ретранслятори до холдера; замикання — register_archetype з id Холдер.Член<A,B>")
              (:step-3 "кількість type-аргументів ≠ параметрам шаблону — попередження «expected N type arg(s), got M — unresolved (forwarding template?)»")
              (:step-4 "кожен виклик холдера, EventArchetypes.Of<E> і AnyComponents лише з подій — на вузлі власника грані base_anchor, anchor_events, held_events; неподієвий архетип — ще ребро reads власник → архетип"))
    :calls #{register_archetype}   ;; :from-code rb-14, rb-20 — ще walk_templates (крок 2) і anchor_of (крок 4)
    :note "подієвий архетип — склад EventFrameComponent і EventTag; черга з лічильником 10000, як сьогодні, — вихід без гарантованого прогресу"}

   register_archetype
   {:module ecs_facts.py
    :does "додає вузол архетипу з компонентами, головним і label-тегами та ребра has"
    :in #{:archetype-declaration :markers}
    :out :none
    :writes #{:nodes :edges}
    :how "components — type-аргументи ComponentTypes.Get, label_tags — теги з label-маркером, main_tag — єдиний тег без label-маркера або null, коли таких не рівно один, ребро has до кожного компонента й тегу з via Холдер.Член"
    :note "три викликачі — конкретний член, замикання шаблону, маніфест singleton"}

   reconcile_ecs_facts
   {:module ecs_facts.py
    :does "звіряє сирі ECS-факти з вузлами — як schema 3 сьогодні"
    :in #{:ecs-sites :nodes :edges}
    :out :none
    :writes #{:edges :nodes :ecs-tables :warnings}
    :flow (-> (:step-1 "доступи до компонентів" {:calls connect_component_access :out :none})
              (:step-2 "видалення сутностей" {:calls attribute_disposals :out :none})
              (:step-3 "таблиці ComponentIndex" {:calls attach_tables :out :none})
              (:step-4 "закон ролей ключів" {:calls apply_key_role_law :out :none})
              (:step-5 "пріоритети" {:calls resolve_priorities :out :none})
              (:step-6 "маніфест singleton і записи без архетипу" {:calls check_singleton_manifest :out :none}))}

   connect_component_access
   {:module ecs_facts.py
    :does "ребра writes, reads, removes, emits і записи sets, late_writes, tags_add_sites"
    :in #{:ecs-sites}
    :out :none
    :writes #{:edges :ecs-tables}
    :how "AddComponent — writes, тег ще в late_writes, подія одразу після CreateEvent — лише вузол, GetComponent, HasComponent, Singletons.Get, Singletons.Has — reads, Singletons.Set — writes, RemoveComponent і RemoveTag — removes, CreateEvent — emits, AllTags, AnyTags, WithoutAnyTags — sets, Tags.Add — tags_add_sites"}

   attribute_disposals
   {:module ecs_facts.py
    :does "приписує кожен DeleteEntity архетипу власника"
    :in #{:ecs-sites :edges :ecs-tables}
    :out :none
    :writes #{:edges :warnings}
    :flow (-> (:step-1 (cond
                         (:flow-1 "власник має sets з EventTag") (:conclusion-1 "пропуск — зрілі події видаляє прибирач подій")
                         (:flow-2 "рівно одне ребро reads власника до архетипу") (:conclusion-2 "ребро disposes INFERRED")
                         (:flow-3 "інакше") (:conclusion-3 "попередження «N candidate archetypes via its own bindings — attribute by hand»"))))
    :numbers {:dispose-warnings-today 7}}

   attach_tables
   {:module ecs_facts.py
    :does "таблиця ComponentIndex — архетип, що несе ключ, і роль PK | FK | IDX"
    :in #{:ecs-sites :nodes}
    :out :none
    :writes #{:ecs-tables :nodes :warnings}   ;; :from-code rb-19 — resolve ключа ComponentIndex
    :how "поля ComponentIndex<TComponent,TValue> і доступи _field[key] з ecs_sites, архетип — єдиний, що несе ключ, роль — за суфіксом ключа, indexed_components і component_field_types з оголошень struct"}

   apply_key_role_law
   {:module ecs_facts.py
    :does "ролі компонентів, два сигнали fk_of і ребра references"
    :in #{:nodes :edges :ecs-tables}
    :out :none
    :writes #{:nodes :edges :warnings}
    :how "роль pk | fk | data за суфіксом, PK на кількох архетипах — попередження key-role, fk_of за суфіксом і fk_of за збігом TValue у ComponentIndex — окремі via, references архетип → архетип з has і fk_of — порт curate() проходу 4"}

   resolve_priorities
   {:module ecs_facts.py
    :does "Priority системи з const int за шляхом вкладеності"
    :in #{:declarations :nodes}
    :out :none
    :writes #{:nodes :warnings}
    :how "таблиця const за шляхом SystemPriorities.RuntimeTick.X, вираз Priority — точний шлях або єдиний суфікс, нерозв'язаний — попередження"}

   check_singleton_manifest
   {:module ecs_facts.py
    :does "маніфест singleton проти вжитих Singletons і записи в компонент без архетипу"
    :in #{:nodes :edges :ecs-tables}
    :out :none
    :writes #{:warnings}
    :how "вжиті, але не оголошені, і оголошені, але не вжиті, — попередження, writes компонента, якого не несе жоден архетип, — попередження orphaned column"}

   reconcile_di_facts
   {:module di_facts.py
    :does "звіряє реєстрації, ін'єкції й склад станів гри"
    :in #{:di-sites :declarations :nodes}
    :out :none
    :writes #{:edges :nodes :warnings}
    :flow (-> (:step-1 "реєстрації" {:calls connect_registrations :out :none})
              (:step-2 "ін'єкції" {:calls connect_injections :out :none})
              (:step-3 "стани гри" {:calls connect_boot_states :out :none}))}

   connect_registrations
   {:module di_facts.py
    :does "ребра registers і exposes з args, Lifetime, інсталером і app_state"
    :in #{:di-sites}
    :out :none
    :writes #{:edges :nodes :warnings}
    :how "Register<Impl> — registers інсталер → Impl з args замикання, via Register(Lifetime), app_state з WithParameter(AppState.X), exposes Impl → кожна As-ціль з її args, Register<Contract, Impl> — перша ціль контракт, RegisterInstance(ідентифікатор) — registers EXTRACTED до вузла з PascalCase ідентифікатора, unknown-form — попередження «форма реєстрації поза відомими»"
    :note "грані Impl: lifetime та installer — різні значення його registers через кому, відсортовано; contract true — на As-цілі. Попередження unknown-form — di-patterns.md «flag if they appear»"}

   connect_injections
   {:module di_facts.py
    :does "ребра injects з конструкторів зареєстрованих типів і з [Inject]-методів"
    :in #{:declarations :edges}
    :out :none
    :writes #{:edges :nodes :warnings}   ;; :from-code rb-19 — resolve типу параметра
    :how "конструктор — лише для типу з вхідним registers, [Inject]-метод — завжди, параметр-колекція — collection true і contract true на елементі, примітивні типи не беруться"}

   connect_boot_states
   {:module di_facts.py
    :does "ребра runs_in системи → стан гри з Boot.Construct"
    :in #{:di-sites}
    :out :none
    :writes #{:edges :nodes}
    :how "ідентифікатор в аргументах new …State — параметр Construct — його TypeUse чи елемент колекції, runs_in EXTRACTED з via Boot або Boot(collection), EntityStorages пропускається, state true на вузлі стану"}

   connect_hosts
   {:module di_facts.py
    :does "ребро hosts від кожного класу, чий конструктор бере колекцію abstract класу, до цього класу"
    :in #{:declarations :nodes}
    :out :none
    :writes #{:edges :nodes :warnings}   ;; :from-code rb-19 — resolve елемента колекції
    :how "параметр конструктора — колекція з елементом, що розв'язується у вузол класу з abstract true, — hosts via ctor, елемент-інтерфейс ребра не дає (:s1-orchestrator-contract)"
    :numbers {:hosts-expected 12}}

   pair_view_subscribers
   {:module view_pairs.py
    :does "з'єднує кожен клас з view-маркером з його view і називає підписки без маркера"
    :in #{:subscription-sites :declarations :markers :ancestry}
    :out :none
    :writes #{:edges :warnings}
    :scratch #{:view-events}
    :flow (-> (:step-1 "події кожної view — імена event, оголошені у view або в її предку; індекс ім'я події → view")
              (:step-2 "клас з маркером на V — його підписки з ім'ям події V → ребро subscribes клас → V via «E +=»; жодної — попередження «view-маркер без підписки»")
              (:step-3 (cond
                         (:flow-1 "підписка класу без маркера на V, ім'я події належить одній view") (:conclusion-1 "попередження «підписка на подію view без маркера»")
                         (:flow-2 "ім'я події належить двом і більше view") (:conclusion-2 "попередження AMBIGUOUS з іменами view")
                         (:flow-3 "інакше") (:conclusion-3 "не підписка на view"))))
    :numbers {:subscribers 4 :subscriptions 5 :views 4}}

   audit_tag_law
   {:module tag_law.py
    :does "називає кожне відхилення закону «головний + label» з архетипом чи місцем"
    :in #{:nodes :edges :markers :ecs-tables}
    :out :none
    :writes #{:tag-audit :warnings}
    :flow (-> (:step-1 "архетип: головних тегів 0 — «без головного тегу», 2+ — «кілька головних тегів [..]»; головні — теги ребер has без label-маркера")
              (:step-2 "головний тег, не EventTag, що є main_tag двох і більше архетипів, — «головний тег спільний [архетипи]»")
              (:step-3 "EventTag на архетипі без EventFrameComponent — «EventTag поза подією»")
              (:step-4 "sets без EventTag: label-тег у фільтрі — «label у фільтрі», тегів ≠ 1 — «фільтр з N тегами»")
              (:step-5 "late_writes тегів — «AddComponent тегу під час життя», tags_add_sites — «склад тегів через Tags.Add — не прочитано»"))
    :ends-with "кожне відхилення — запис у tag_audit {kind archetype_or_owner tags source_location} і рядок у warnings"
    :numbers {:deviations-expected 0}}

   decide_roles
   {:module roles.py
    :does "дає кожному не-abstract класу роль і джерело рішення за role-decision"
    :in #{:type-facts :nodes :edges :ecs-tables}
    :out :none
    :writes #{:nodes :edges :warnings}
    :flow (-> (:step-1 (cond
                         (:flow-1 "для кожного вузла класу з declared true і abstract false") (:conclusion-1 "кроки 2-5 — і далі")
                         (:flow-2 "класи скінчились") (:conclusion-2 "вихід")))
              (:step-2 "докази" {:calls collect_role_evidence :out :role-evidence})
              (:step-3 "рішення" {:calls decide_role :out :role-decision})
              (:step-4 "роль не none — kind system, role, decided_by на вузлі; ребра подій" {:calls connect_event_edges :out :none})
              (:step-5 "роль undecided — попередження «маркер потрібен»; маркер ролі, а рішення не з маркера, — «надлишковий маркер: роль вирішує база»"))
    :note "abstract класи роль не дістають — role-decision лише для не-abstract; decide_roles — абзац циклу, три кроки — виклики"}

   collect_role_evidence
   {:module roles.py
    :does "збирає для класу предикати role-decision"
    :in #{:type-facts :nodes :edges :ecs-tables}
    :out :role-evidence
    :how "update_loop — предок IUpdatedSystem або ILateUpdatedSystem, sweeps_events — клас власник set з EventTag і DeleteEntity-сайту, anchored_on_event — base_anchor event, holds_event_archetype — held_events не порожні, role_marker — роль з маркерів, pipeline_member — предок IPrioritizedUniTaskSystem з args MapGenerationStep, turn_phase_member — предок TurnPhaseSubSystem, startup_step — предок IUniTaskSystem без args, family_member — предок-abstract клас з вхідним hosts"}

   decide_role
   {:module roles.py
    :does "перша істинна гілка role-decision — роль і джерело рішення"
    :in #{:role-evidence}
    :out :role-decision
    :flow (-> (:step-1 (cond
                         (:flow-1 "update_loop і sweeps_events") (:conclusion-1 "cleanup, lexical")
                         (:flow-2 "anchored_on_event") (:conclusion-2 "reactive, base")
                         (:flow-3 "update_loop і holds_event_archetype") (:conclusion-3 "роль маркера з decided_by marker, без маркера — undecided")
                         (:flow-4 "update_loop") (:conclusion-4 "per_frame, base")
                         (:flow-5 "pipeline_member") (:conclusion-5 "pipeline_stage, base")
                         (:flow-6 "turn_phase_member") (:conclusion-6 "turn_phase, base")
                         (:flow-7 "startup_step") (:conclusion-7 "startup_step, base")
                         (:flow-8 "family_member") (:conclusion-8 "sub_system, lexical")
                         (:flow-9 "інакше") (:conclusion-9 "none"))))
    :note "чиста функція — без draft; порядок гілок — s1 role-decision дослівно"}

   connect_event_edges
   {:module roles.py
    :does "ребра reacts_to і polls від класу до подій"
    :in #{:role-decision :nodes}
    :out :none
    :writes #{:edges}
    :how "reactive з decided_by base — reacts_to до кожної anchor_events, reactive з marker — reacts_to до кожної held_events, інакше held_events — polls"
    :numbers {:reacts-to-expected "20 = 16 + 3 + 1" :polls-expected "4 — TurnProcessorSystem, DistrictBuildUISystem, DistrictBuildListUISubSystem, MainMenuState"}
    :note "MainMenuState і підсистема — не класи з роллю, але їхні held_events теж стають polls: цикл decide_roles проходить кожен не-abstract клас"}

   find_recipe_instances
   {:module recipes.py
    :does "для кожного з 15 рецептів знаходить екземпляри, відхилення і пояснення порожнечі"
    :in #{:recipe-signatures :source-facts :type-facts :nodes :edges :ecs-tables :tag-audit}
    :out :none
    :writes #{:recipe-instances :nodes :warnings}   ;; :from-code rb-19 — resolve поля [SerializeField] у find_polymorphic_catalogues
    :scratch #{:recipe-result}
    :flow (-> (:step-1 "для кожного рецепта в RECIPE_SIGNATURES — його ознака над графом → RecipeResult")
              (:step-2 "екземплярів 0 — empty_reason з ознаки: чого не знайдено")
              (:step-3 "RecipeResult у draft.recipe_instances під назвою рецепта"))
    :calls #{instances_of_kind instances_with_role find_tags find_configs find_config_loaders find_orchestrator_families find_reactive_orchestrators find_view_subscribers find_transaction_entities find_polymorphic_catalogues find_addressable_injections}
    :note "s1 крок 14 не читав :declarations, а ознака каталогу бере поле [SerializeField] типу масив B — вхід :source-facts додано; instances_of_kind і instances_with_role — примітиви: вид — рецепти 1 і 3, роль — рецепти 6, 8, 10, 11"}

   write_graph
   {:module graph_store.py
    :does "складає документ графа і пише його в теку артефактів"
    :in #{:project-root :source-files :draft :scan-roots}
    :out :graph-doc
    :writes #{:graph-file}
    :how "meta {schema SCHEMA, roots — наявні SCAN_ROOTS, files — кількість source_files, generated UTC, curated true} разом з draft.to_document(), mkdir теки, json.dumps indent 2 ensure_ascii False у graph.json"
    :numbers {:schema 1}}

   remove_legacy_artifact_dirs
   {:module build.py
    :does "прибирає дві теки артефактів старих інструментів, якщо вони є"
    :in #{:project-root :legacy-artifact-dirs}
    :out :none
    :writes #{:legacy-artifact-dirs}
    :how "shutil.rmtree кожної наявної теки з LEGACY_ARTIFACT_DIRS"
    :lifetime "тимчасовий: функція, константа і рядок хребта видаляються в tool-cutover кроці 7 разом зі старими скілами (:legacy-cleanup-lifetime у # Contra)"}})
```

```clojure
(def GraphDraft-methods  ;; graph_draft.py — граф під збіркою; клас-API примітивів, порядок — порядок першого вжитку в хребті
  {GraphDraft
   {:does "народжує порожній граф з попередженнями розбору"
    :in #{:warnings}
    :out :draft
    :how "nodes {}, name_index {}, edges [], edge_keys set(), ecs_tables {}, tag_audit [], recipe_instances {}, warnings — копія попереджень розбору"}

   add_node
   {:does "додає вузол або доповнює грані наявного"
    :in #{:nodes}
    :out :none
    :writes #{:nodes}
    :how "declared-вузол перекриває guessed kind, state не понижується до іншого виду, name_index[голе ім'я] += id для declared"}

   resolve
   {:does "ім'я типу з файла → id вузла"
    :in #{:nodes :usings}
    :out :node-id
    :flow (-> (:step-1 (cond
                         (:flow-1 "голе ім'я несе рівно одне оголошення") (:conclusion-1 "його id")
                         (:flow-2 "жодне") (:conclusion-2 "id = голе ім'я, вузол declared false з kind за суфіксом")
                         (:flow-3 "кілька — фільтр за просторами імен, видимими файлу, лишає одне") (:conclusion-3 "його id")
                         (:flow-4 "інакше") (:conclusion-4 "попередження «AMBIGUOUS <ім'я> [кандидати] @ місце», None"))))
    :note "None — посилання не стає ребром; неоднозначність видно в check"}

   add_edge
   {:does "додає ребро, якщо такого ще нема"
    :in #{:edge}
    :out :none
    :writes #{:edges}
    :how "ключ — (src dst rel via args app_state), для registers і exposes ще й source_location — кожна реєстрація окремий факт, як 26 ConfigLoaderSystem, ребро без src чи dst або петля не пишеться"}

   warn
   {:does "дописує попередження"
    :in #{:warnings}
    :out :none
    :writes #{:warnings}}

   to_document
   {:does "віддає розділи графа як словник документа"
    :in #{:draft}
    :out :graph-doc
    :how "nodes, edges як dict, ecs_tables, tag_audit, recipes, warnings — відсортовані без повторів"}})
```

```clojure
(def unified-graph-data
  {:project-root         {:from-s1 :project-root :as root :lives :external :note "дає CLI через find_project_root"}
   :scan-roots           {:from-s1 :scan-roots :as SCAN_ROOTS :lives :external :note "константа build.py: Assets/Domains, Assets/Presentation, Assets/Modules, Assets/Scripts, Assets/Flows"}
   :source-files         {:from-s1 :source-files :as source_files :lives build_graph}
   :source-facts         {:shape ^:new SourceFacts :as sources :lives build_graph
                          :fields {declarations :declarations usings :usings holders :holders ecs_sites :ecs-sites
                                   di_sites :di-sites subscription_sites :subscription-sites parse_warnings :warnings}
                          :holds "усе, що знято з коду за один прохід"}
   :declarations         {:from-s1 :declarations :as sources.declarations :lives build_graph}
   :usings               {:from-s1 :usings :as sources.usings :lives build_graph}
   :holders              {:from-s1 :holders :as sources.holders :lives build_graph}
   :ecs-sites            {:from-s1 :ecs-sites :as sources.ecs_sites :lives build_graph :grows "declare_nodes розв'язує імена на місці"}
   :di-sites             {:from-s1 :di-sites :as sources.di_sites :lives build_graph :grows "declare_nodes розв'язує імена на місці"}
   :subscription-sites   {:from-s1 :subscription-sites :as sources.subscription_sites :lives build_graph :grows "declare_nodes розв'язує імена на місці"}
   :parser               {:shape ^:new Parser :as parser :lives read_sources :holds "tree-sitter Parser(LANG) на прохід"}
   :file-facts           {:shape ^:new FileFacts :as file_facts :lives read_sources :holds "ті самі списки, що SourceFacts, для одного файла"}
   :type-use             {:shape ^:new TypeUse :as type_use :lives read_type_declaration :holds "name, args, element, is_collection, is_array — написаний тип бази, параметра чи поля"}
   :draft                {:shape ^:new GraphDraft :as draft :lives build_graph
                          :fields {nodes :nodes edges :edges ecs_tables :ecs-tables tag_audit :tag-audit
                                   recipe_instances :recipe-instances warnings :warnings}
                          :holds "граф під збіркою; кожен крок хребта пише свої розділи через :writes"
                          :note "локальна точки входу, не поле власника: helper-fills — кроки пишуть розділи переданого запису; ecs_tables, tag_audit і recipe_instances пише по одному кроку, nodes, edges і warnings — багато"}
   :nodes                {:from-s1 :nodes :as draft.nodes :lives build_graph
                          :grows "declare_nodes, build_ancestry, expand_archetypes, reconcile_ecs_facts, reconcile_di_facts, decide_roles"
                          :note "s2 додає грані власника base_anchor (event | table | null), anchor_events, held_events — позицію прив'язки s1 кроку 7, яку читають attribute_disposals і decide_roles, бо в s1 :edges позиції нема"}
   :node-id              {:shape ^:new str :as node_id :lives resolve :holds "id вузла або None"}
   :edge                 {:shape ^:new Edge :as edge :lives add_edge :holds "src dst rel via args app_state collection source_location confidence — форма s1 :edges"}
   :edges                {:from-s1 :edges :as draft.edges :lives build_graph
                          :grows "build_ancestry, expand_archetypes, reconcile_ecs_facts, reconcile_di_facts, connect_hosts, pair_view_subscribers, decide_roles"}
   :markers              {:from-s1 :markers :as types.markers :lives build_graph :holds "id → Markers {role views label}"}
   :ancestry             {:from-s1 :ancestry :as types.ancestry :lives build_graph :holds "id → [Ancestor {node args}]"}
   :type-facts           {:shape ^:new TypeFacts :as types :lives build_graph :fields {markers :markers ancestry :ancestry}
                          :holds "що відомо про типи понад їхні вузли"}
   :holder-templates     {:shape ^:new dict :as templates :lives expand_archetypes :holds "(клас, generic-метод) → type-параметри і ретрансляції"}   ;; :from-code rb-14 — живе в walk_templates
   :archetype-declaration {:shape ^:new ArchetypeDeclaration :as declaration :lives expand_archetypes :holds "id, components, tags, via, source_location одного архетипу"}
   :ecs-tables           {:from-s1 :ecs-tables :as draft.ecs_tables :lives build_graph :note "schema 3 плюс tags_add_sites для аудиту тегів"}
   :tag-audit            {:from-s1 :tag-audit :as draft.tag_audit :lives build_graph}
   :role-evidence        {:shape ^:new RoleEvidence :as evidence :lives decide_roles
                          :fields {update_loop :ancestry sweeps_events :ecs-tables anchored_on_event :nodes holds_event_archetype :nodes
                                   role_marker :markers pipeline_member :ancestry turn_phase_member :ancestry startup_step :ancestry family_member :edges}
                          :holds "предикати role-decision для одного класу"}
   :role-decision        {:shape ^:new RoleDecision :as decision :lives decide_roles :holds "role і decided_by — base | marker | lexical | none"}
   :view-events          {:shape ^:new dict :as view_events :lives pair_view_subscribers :holds "ім'я події → id view, що її оголошує сама чи через предка"}
   :recipe-signatures    {:from-s1 :recipe-signatures :as RECIPE_SIGNATURES :lives :external :note "константа recipes.py — recipe-signatures-in-code"}
   :recipe-result        {:shape ^:new RecipeResult :as result :lives find_recipe_instances
                          :holds "decided_by, instances [{id source_location decided_by}], groups {назва групи → id}, deviations, empty_reason"}
   :recipe-instances     {:from-s1 :recipe-instances :as draft.recipe_instances :lives build_graph}
   :warnings             {:from-s1 :warnings :as draft.warnings :lives build_graph :grows "майже кожен крок"}
   :graph-doc            {:from-s1 :graph-doc :as graph_doc :lives build_graph}
   :graph-file           {:from-s1 :graph-file :as graph_path :lives write_graph :note "<root>/.fantasymayor-graph/graph.json"}
   :legacy-artifact-dirs {:from-s1 :legacy-artifact-dirs :as LEGACY_ARTIFACT_DIRS :lives :external :note "тимчасова константа build.py — до tool-cutover кроку 7"}})
```

```clojure
(def recipe-signatures-in-code  ;; recipes.py RECIPE_SIGNATURES — рецепт → ознака, джерело рішення; RECIPE_NAMES — ключі в порядку Patterns/
  {PATTERN_COMPONENT                   {:finder "instances_of_kind — component" :decided-by :kind}
   PATTERN_TAG                         {:finder "find_tags — вид tag з позначкою main або label і роллю label" :decided-by :kind :deviations "draft.tag_audit"}
   PATTERN_EVENT                       {:finder "instances_of_kind — event" :decided-by :kind}
   PATTERN_CONFIG                      {:finder "find_configs — вид config; група abstract_bases — abstract класи з предком ScriptableObject | SerializedScriptableObject" :decided-by :base}
   PATTERN_CONFIG_LOADER               {:finder "find_config_loaders — ребра registers з app_state ConfigLoading, чий Impl має exposes до IUniTaskSystem; група instance_objects — registers з app_state InstanceObjects" :decided-by :di}
   PATTERN_PIPELINE_STAGE              {:finder "instances_with_role — pipeline_stage" :decided-by :base}
   PATTERN_ORCHESTRATOR_SUBSYSTEM      {:finder "find_orchestrator_families — групи contracts (dst ребер hosts), members (не-abstract нащадки контракту), hosts (src ребер hosts)" :decided-by :lexical}
   PATTERN_REACTIVE_SYSTEM             {:finder "instances_with_role — reactive" :decided-by #{:base :marker}}
   PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM {:finder "find_reactive_orchestrators — роль reactive і вихідне ребро hosts" :decided-by :lexical}
   PATTERN_PERFRAME_SYSTEM             {:finder "instances_with_role — per_frame" :decided-by #{:base :marker} :deviations "класи з роллю undecided — «маркер потрібен»"}
   PATTERN_CLEANUP_SYSTEM              {:finder "instances_with_role — cleanup" :decided-by :lexical}
   PATTERN_VIEW_SYSTEM                 {:finder "find_view_subscribers — src і dst ребер subscribes, групи subscribers і views" :decided-by :marker :deviations "view з вихідним emits; попередження «підписка на подію view без маркера»"}
   PATTERN_TRANSACTION_ENTITY          {:finder "find_transaction_entities — архетипи з label-тегом ролі transaction" :decided-by :label-tag}
   PATTERN_POLYMORPHIC_CATALOGUE       {:finder "find_polymorphic_catalogues — abstract SO-база B, не-abstract SO-клас з полем [SerializeField] масиву B, компонент з ім'ям B, де Config → KindComponent, з вхідним has" :decided-by :lexical}
   ADDRESSABLE_PATTERNS                {:finder "find_addressable_injections — src ребер injects via ctor до IAddressable" :decided-by :lexical}
   :empty-reason "кожна ознака несе рядок, чого вона шукала; при 0 екземплярів він іде в empty_reason — pattern ніколи не мовчить"
   :expected "s1 recipe-signatures :expected — еталон meter-readings частини 5"})
```

```clojure
{:part unified-graph
 :spine-reads "source_files = list_source_files(root); sources = read_sources(root, source_files); draft = GraphDraft(sources.parse_warnings); types = know_types(sources, draft); expand_archetypes(sources, types, draft); reconcile_ecs_facts(sources, draft); reconcile_di_facts(sources, draft); connect_hosts(sources, draft); pair_view_subscribers(sources, types, draft); audit_tag_law(types, draft); decide_roles(types, draft); find_recipe_instances(sources, types, draft); graph_doc = write_graph(root, source_files, draft); remove_legacy_artifact_dirs(root); return graph_doc"
 :steps-visible {build_graph "13; 12 після tool-cutover кроку 7"}
 :data-coverage {:s1-coverage "21 / 21"
                 :undeclared 0
                 :orphan 0
                 :one-step-fields 0      ;; полів власника нема; ecs_tables, tag_audit, recipe_instances — розділи локального запису draft
                 :unborn 0
                 :scratch-heavy "read_sources 2, expand_archetypes 2 — сигналу нема"}
 :how-semicolons 0
 :max-params "усі ≤ 3: find_recipe_instances(sources, types, draft), pair_view_subscribers(sources, types, draft), write_graph(root, source_files, draft), collect_role_evidence(class_id, types, draft), connect_event_edges(class_id, decision, draft)"
 :reader-clean true}
```

## Частина 4 · graph-queries — CLI

```clojure
(def fmgraph-methods  ;; fmgraph.py і graph_store.py
  {main
   {:module fmgraph.py
    :does "відповідає на один запит зі свіжого графа"
    :in #{:request}
    :out :none
    :flow (-> (:step-1 "прочитати запит" {:calls parse_request :out :request})
              (:step-2 "знайти корінь проєкту" {:calls find_project_root :out :project-root})
              (:step-3 "виміряти свіжість графа" {:calls is_graph_fresh :out :freshness})
              (:step-4 "перебудувати застарілий граф" {:calls refresh_graph :out :none})
              (:step-5 "завантажити граф" {:calls load_graph :out :loaded-graph})
              (:step-6 "відповісти" {:calls answer_request :out :none}))
    :note "команда build теж проходить хребет: застарілий граф самолікування збудує, а answer_build збудує ще раз — 0.2 с, зате без розгалуження в точці входу"}

   parse_request
   {:module fmgraph.py
    :does "argparse: команда, її аргументи і глобальний --path"
    :in #{:request}
    :out :request
    :how "підкоманди в порядку COMMANDS, pattern — choices=RECIPE_NAMES, systems і search — --role, neighbors і bfs — --rel, bfs — --depth і --in"
    :exits #{"назва рецепта поза 15 — argparse друкує перелік назв у stderr і виходить з кодом 2 (s1-вихід, :query-shape, :recipes-scope)"}}

   find_project_root
   {:module graph_store.py
    :does "перша тека з Assets/ від --path угору"
    :in #{:request}
    :out :project-root
    :exits #{"над --path нема Assets/ — sys.exit з повідомленням, код 1 (ecsg.py:27-32, dig.py:31-36)"}}

   is_graph_fresh
   {:module graph_store.py
    :does "чи граф можна читати без перебудови"
    :in #{:project-root :graph-file :builder-source}
    :out :freshness
    :how "true, лише коли graph.json є, meta.schema = SCHEMA, meta.files = кількість .cs під meta.roots, жоден .cs не новіший за graph.json і жоден scripts/*.py скіла не новіший"}

   refresh_graph
   {:module fmgraph.py
    :does "перебудовує граф, якщо він застарілий"
    :in #{:project-root :freshness}
    :out :none
    :writes #{:graph-file}
    :flow (-> (:step-1 (cond
                         (:flow-1 "граф свіжий") (:conclusion-1 "нічого")
                         (:flow-2 "граф застарілий") (:conclusion-2 "build_graph(root), один рядок stderr «fantasymayor-graph: stale -> rebuilt»"))))
    :calls #{build_graph}
    :note "except нема: виняток збірки — трейс у stderr і код 1, відповіді зі старого графа нема (s1-вихід, :od-refresh, c-10 s1)"}

   load_graph
   {:module graph_store.py
    :does "читає graph.json і рахує вхідні й вихідні ребра"
    :in #{:project-root :graph-file}
    :out :loaded-graph
    :scratch #{:graph-doc}
    :how "json.load у doc, outgoing і incoming — defaultdict(list) за src і dst, поля LoadedGraph з розділів doc"}

   answer_request
   {:module fmgraph.py
    :does "віддає запит обробнику його команди"
    :in #{:loaded-graph :request}
    :out :none
    :writes #{:answer}
    :how "COMMANDS[request.command](graph, request)"}

   find_node
   {:module graph_store.py
    :does "LoadedGraph: запит → id вузла"
    :in #{:loaded-graph}
    :out :node-id
    :flow (-> (:step-1 (cond
                         (:flow-1 "запит — id") (:conclusion-1 "він")
                         (:flow-2 "рівно одне точне ім'я без урахування регістру") (:conclusion-2 "його id")
                         (:flow-3 "рівно один вузол містить запит у імені") (:conclusion-3 "його id")
                         (:flow-4 "жодного") (:conclusion-4 "stderr «no node matches», sys.exit(1)")
                         (:flow-5 "кілька") (:conclusion-5 "stderr кандидати з видом, sys.exit(1)"))))
    :note "архетип має id і name Холдер.Член, тож explain ForestView бере клас, а не архетип PresentationArchetypes.ForestView; ненульовий код — fail-loud, сьогодні обидва CLI виходили з 0"}})
```

```clojure
(def commands  ;; queries.py COMMANDS — клас-API: незалежні точки входу, порядок файла = порядок таблиці; кожен обробник (graph, request) пише :answer у stdout
  {build      {:handler answer_build      :from :new         :prints "перебудувати зараз: файлів, вузлів, ребер, попереджень, секунд"}
   check      {:handler answer_check      :from "build_graph.py --check, build_di_graph.py --check"
               :prints "висячі ребра, власники таблиць і sets поза вузлами, архетипи без компонентів, екземпляри рецептів без вузла, curated, лічильники INFERRED і AMBIGUOUS, усі попередження; помилки цілісності — код 1"}
   stats      {:handler answer_stats      :from "ecsg.py stats + dig.py stats" :prints "вузли за kind, ребра за rel, системи за role, екземпляри за рецептом, попередження, curated"}
   pattern    {:handler answer_pattern    :from :new :prints "див. answer_pattern"}
   systems    {:handler answer_systems    :from "ecsg.py systems" :prints "системи за role і priority; --role фільтрує; ролі reactive, per_frame, cleanup, pipeline_stage, turn_phase, startup_step, sub_system"}
   tags       {:handler answer_tags       :from "ecsg.py tags" :prints "див. answer_tags"}
   explain    {:handler answer_explain    :from "ecsg.py explain + dig.py explain" :prints "див. answer_explain"}
   neighbors  {:handler answer_neighbors  :from "ecsg.py neighbors" :prints "ребра вузла, --rel фільтрує"}
   search     {:handler answer_search     :from "ecsg.py search + dig.py search" :prints "підрядок у name, kind або role; --role фільтрує"}
   bfs        {:handler answer_bfs        :from "ecsg.py bfs = dig.py bfs" :prints "обхід --depth, --rel, --in"}
   tables     {:handler answer_tables     :from "ecsg.py tables" :prints "таблиці ComponentIndex з роллю й архетипом"}
   spaces     {:handler answer_spaces     :from "ecsg.py spaces" :prints "простори ключів"}
   resolve    {:handler answer_resolve    :from "dig.py resolve" :prints "див. answer_resolve"}
   consumers  {:handler answer_consumers  :from "dig.py consumers" :prints "хто injects тип"}
   installer  {:handler answer_installer  :from "dig.py installer" :prints "що реєструє інсталер, Lifetime, As-цілі"}
   state      {:handler answer_state      :from "dig.py state" :prints "системи з runs_in у стан гри"}
   unresolved {:handler answer_unresolved :from "dig.py unresolved" :prints "injects без registers чи exposes"}
   :merged "спільні stats, explain, search, bfs, check — одна команда на пару; назви rel — s1 список плюс polls; назви ролей — s1 roles-after і undecided"})
```

```clojure
(def queries-methods  ;; обробники, що змінюються; решта — порт відповідної cmd_* без зміни логіки
  {answer_pattern
   {:does "показує екземпляри одного рецепта"
    :in #{:loaded-graph :request}
    :out :none
    :writes #{:answer}
    :flow (-> (:step-1 "RecipeResult рецепта з graph.recipe_instances")
              (:step-2 "рядок на екземпляр: ім'я, source_location, decided_by; групи — окремими блоками")
              (:step-3 "відхилення; для PATTERN_TAG — ще записи graph.tag_audit")
              (:step-4 (cond
                         (:flow-1 "екземплярів 0") (:conclusion-1 "empty_reason — чого бракує ознаці")
                         (:flow-2 "інакше") (:conclusion-2 "підсумок: N екземплярів, M відхилень"))))}

   answer_tags
   {:does "аудит тегів: головний і label-теги кожного архетипу та відхилення"
    :in #{:loaded-graph}
    :out :none
    :writes #{:answer}
    :how "архетипи за іменем з main_tag і label_tags окремими колонками, далі записи tag_audit за видом, підсумок «(N deviation(s) from one main tag + labels)»"}

   answer_explain
   {:does "вузол з усіма гранями ECS і DI та ребрами"
    :in #{:loaded-graph :adjacency :request}
    :out :none
    :writes #{:answer}
    :how "kind, namespace, source_location, role з decided_by, priority, base_anchor, main_tag і label_tags, lifetime, installer, contract, state, маркери, далі вихідні й вхідні ребра за rel з args і confidence, таблиці, де вузол — ключ або власник"}

   answer_resolve
   {:does "що наповнює IReadOnlyList<контракт>"
    :in #{:loaded-graph :adjacency :request}
    :out :none
    :writes #{:answer}
    :flow (-> (:step-1 "запит «Ім'я<A,B>» — ім'я й args; вузол — find_node(ім'я)")
              (:step-2 "вхідні exposes; args у запиті — лише ребра з тими самими args")
              (:step-3 "рядок на ребро: Impl, args реєстрації з тим самим source_location, lifetime, installer, місце"))
    :note "c-5 s1: вузла IPrioritizedUniTaskSystem<MapGenerationStep> нема — є вузол і ребра з args"}})
```

```clojure
(def graph-queries-data
  {:request       {:from-s1 :request :as request :lives main :note "argv на вході, argparse.Namespace після parse_request"}
   :project-root  {:from-s1 :project-root :as root :lives main}
   :graph-file    {:from-s1 :graph-file :as graph_path :lives :external :note "graph_store: root / ARTIFACT_DIR / graph.json"}
   :builder-source {:from-s1 :builder-source :as scripts_dir :lives :external :note "тека scripts/ скіла — найновіший *.py"}
   :freshness     {:from-s1 :freshness :as fresh :lives main}
   :loaded-graph  {:shape ^:new LoadedGraph :as graph :lives main
                   :fields {root :project-root nodes :graph-doc edges :graph-doc outgoing :adjacency incoming :adjacency
                            ecs_tables :graph-doc tag_audit :graph-doc recipe_instances :graph-doc warnings :graph-doc meta :graph-doc}
                   :holds "граф у пам'яті з обома напрямками ребер і find_node"}
   :graph-doc     {:from-s1 :graph-doc :as doc :lives load_graph}
   :adjacency     {:from-s1 :adjacency :as graph.outgoing :lives main :note "outgoing і incoming — поля LoadedGraph"}
   :node-id       {:shape ^:new str :as node_id :lives find_node :holds "id вузла"}
   :answer        {:from-s1 :answer :as stdout :lives :external :note "друк обробника; помилка — stderr і ненульовий код"}})
```

```clojure
{:part graph-queries
 :spine-reads "request = parse_request(sys.argv[1:]); root = find_project_root(Path(request.path)); fresh = is_graph_fresh(root); refresh_graph(root, fresh); graph = load_graph(root); answer_request(graph, request)"
 :steps-visible {main 6}
 :data-coverage {:s1-coverage "8 / 8" :undeclared 0 :orphan 0 :one-step-fields 0 :unborn 0}
 :how-semicolons 0
 :reader-clean true}
```

## Частина 5 · tool-cutover — скіл, споживачі, пам'ять, видалення

```clojure
(def skill-layout  ;; :tool-home, :skill-layout
  {:home ".claude/skills/fantasymayor-graph/"
   :SKILL.md {:frontmatter "лише name і description (DOC_STANDARD :project-skill)"
              :name fantasymayor-graph
              :description "Build and query ONE deterministic graph of FantasyMayor's code that roslyn-mcp cannot classify: ECS (Friflo.Engine.ECS) archetypes with main and label tags, component writers and readers, event producer→consumer, Table-Rule PK/FK, singletons; DI (VContainer) registrations, Lifetime, installers, injectors, collection fill, GameMode; system roles decided by base or by markers; and the live instances of every Patterns/ recipe via pattern <RECIPE>. Use WHENEVER a question is about ECS or DI relationships or recipe instances — prefer it over reading .cs or grepping generic calls."
              :sections ["# fantasymayor-graph — що це, один граф, один CLI, без LLM"
                         "## Deps — tree-sitter, tree-sitter-c-sharp, Python 3.10+"
                         "## Quick start — FMG=.claude/skills/fantasymayor-graph/scripts/fmgraph.py; build, check, pattern, tags"
                         "## Commands — 17 команд, рядок на команду з таблиці commands"
                         "## Roles and markers — role-decision, SystemRole, ViewSubscriber, TagLabel, MarkerShapeAnalyzer як сторож"
                         "## Schema — graph.json schema 1: meta, грані вузлів, поля й rel ребер, ecs_tables, tag_audit, recipes, warnings"
                         "## Honesty rules — EXTRACTED | INFERRED | AMBIGUOUS, попередження ніколи не мовчать, застарілий граф не відповідає"]}
   :scripts "13 модулів graph-tool-layout"
   :references [{:file "references/graph-facts.md" :holds "шаблон коду → вузол, грань, ребро: ECS, DI, родовід, маркери — злиття ecs-patterns.md і di-patterns.md"}
                {:file "references/recipe-signatures.md" :holds "15 рядків: рецепт, decided-by, ознака, що означає 0; перше речення — джерело правди RECIPE_SIGNATURES у scripts/recipes.py"}]
   :lint "doc_lint сканує .claude/skills — fences валідні, імена лише наявні; gen_index .claude пропускає"})
```

```clojure
(def meter-readings  ;; :meter-readings s1 крок 2 і 8 — що стадія коду запускає і з чим звіряє
  [{:meter "fmgraph.py pattern <RECIPE> для 15 рецептів" :target "s1 recipe-signatures :expected; config — 31 + нащадки 4 abstract SO-баз за mcp__roslyn__get_type_hierarchy Descendants (c-6 s1); orchestrator — 10 / 29 / 12 (c-7 s1)"}
   {:meter "fmgraph.py systems --role per_frame" :target "7: CameraMovementSystem, HexSelectionSystem, ResourceBarSystem, TurnPanelViewSystem, HexIconsContainerPositionSystem, TurnProcessorSystem, DistrictBuildUISystem — жодної реактивної"}
   {:meter "fmgraph.py tags" :target "0 відхилень; 30 типів тегів: 27 головних, 3 label"}
   {:meter "fmgraph.py check" :target "curated true, integrity clean, попереджень не більше 8: 7 DeleteEntity і 1 key-role; нових 0"}
   {:meter "fmgraph.py stats" :target "reacts_to 20, polls 4, registers 101"}
   {:meter "grep -rn -E 'ecs-graph|di-graph|ecsg\\.py|dig\\.py' в :grep-scope і окремо .sdd-flow/project.md" :target "0; ls ~/.claude/skills/ecs-graph ~/.claude/skills/di-graph — нема"}
   {:meter "python3 Tools/doc_lint.py --quiet; python3 Tools/gen_index.py" :target "привидів не більше 3, 0 помилок Clojure, gen_index без LINT"}])
```

```clojure
(def tool-cutover-edits  ;; :gate-and-tooling, :tool-docs, :tag-law-docs — «рядок N» на 2026-09-15; нові тексти англійською, як документи
  [{:id :build-ban :s1-step "4, перенесено на початок частини 2" :file "CLAUDE.md" :line 63
    :from "\"Unity builds, dotnet build, msbuild, xbuild, Unity CLI builds\""
    :to "\"builds of the Unity project — Unity builds, dotnet build / msbuild / xbuild of its csproj or sln, Unity CLI builds; Tools/MarkerShapeAnalyzer is outside the ban\""}

   {:s1-step 3 :file ".claude/hooks/graph-gate.py"
    :do ["docstring 5-8 → «The graph is DETERMINISTIC: fmgraph.py build extracts AND curates in ONE call (no LLM), so the main agent MAY run it directly. What stays denied is ad-hoc reaching into the .fantasymayor-graph/ artifacts — query and rebuild through the one CLI fmgraph.py, never poke the JSON by hand.»"
         "рядок 11: .ecs-graph/graph.json → .fantasymayor-graph/graph.json"
         "рядок 22: GRAPH_DIR_RE = re.compile(r\"\\.fantasymayor-graph\\b\"), коментар «matches .fantasymayor-graph anywhere in a path/command»"
         "рядки 23-25: коментар «The one graph CLI builds and queries» і GRAPH_EXES = {\"fmgraph.py\"}"
         "рядки 35-37: GRAPH_DIR_DENY → «Direct Bash access to .fantasymayor-graph/ (writing or reading the graph artifacts ad-hoc) is denied in the main session. Query and rebuild via fmgraph.py — one deterministic CLI.»"
         "рядок 79: /path/ecsg.py -> ecsg.py → /path/fmgraph.py -> fmgraph.py"
         "рядки 83-85 docstring bash_is_gated: «the two deterministic builders + the read-only query CLIs» → «the one graph CLI fmgraph.py»"
         "рядки 110-111 → «Writing the graph artifacts (.fantasymayor-graph/) by hand is denied — they are generated. Rebuild via fmgraph.py build.»"]
    :unchanged ".claude/settings.json — шлях hook той самий"}

   {:s1-step 3 :file ".gitignore"
    :do ["рядки 9-13 → «# fantasymayor-graph code graph (ECS + DI + recipes) — derived, regenerated on demand, never tracked» і «/.fantasymayor-graph/»"
         "після рядка 49 *.csproj — «!/Tools/MarkerShapeAnalyzer/MarkerShapeAnalyzer.csproj»"
         "у блок Local editor / OS noise — «/Tools/MarkerShapeAnalyzer/bin/», «/Tools/MarkerShapeAnalyzer/obj/», «__pycache__/»"]}

   {:s1-step 3 :file "Tools/doc_lint.py" :line 32 :do "\".ecs-graph\", \".di-graph\", → \".fantasymayor-graph\","}
   {:s1-step 3 :file "Tools/asmdef_reach.py" :line 6 :do "«questions roslyn / ecs-graph / di-graph do NOT:» → «questions roslyn / fantasymayor-graph do NOT:»"}

   {:s1-step 4 :file "~/.claude/CLAUDE.md"
    :do ["рядки 9-16 — розділи ecs-graph і di-graph з порожніми рядками по них — видалити"
         "рядок 49, приклад рядка :accept: {:meter ecs-graph :target \"tag deviations 0\"} → {:meter fantasymayor-graph :target \"tag deviations 0\"}"]
    :note "рядок 49 CONTEXT не називав — знайшов grep стадії s2 (c-2 у # Contra)"}

   {:s1-step 4 :file ".sdd-flow/project.md"
    :do ["рядки 32-44 — два записи # Tools → один: {:tool fantasymayor-graph :is \"derived code graph: ECS (Friflo.Engine.ECS) + DI (VContainer) + recipe instances\" :answers \"archetypes with main and label tags, which system writes/reads a component, event producer→consumer, Table-Rule PK/FK, singletons; what a type is registered as, Lifetime, installer, injectors, collection fill, GameMode; system roles; instances of the 15 Patterns/ recipes\" :prefer-when \"the question is about ECS or DI relationships or recipe instances rather than syntax\" :never-for \"hand-reading or hand-editing the graph artifacts — the graph-gate hook denies it\" :invoke \"python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py <command> (rebuild: build)\"}"
         "рядок 72: roslyn / ecs-graph / di-graph → roslyn / fantasymayor-graph"
         "рядки 109-115 — два meters → один: {:meter \"python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py check\" :target \"curated: true, integrity clean, no new warnings\" :when #{:ecs-changed :di-changed}}"
         "рядок 119: ecsg.py tags → fmgraph.py tags"
         "рядки 137-139 # Ceremonies graph-rebuild: :runs-at \"after changing an ECS archetype, DI wiring or a marker\", :owner \"fmgraph.py build — deterministic, one pass, no LLM\""]}

   {:s1-step 4 :file "CLAUDE.md"
    :do ["рядок 55: (:then \"python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py tags; target: exactly one main tag per archetype, label tags shown apart, 0 deviations, no runtime tag writes\")"
         "рядок 67: \"hand-editing .ecs-graph/ and .di-graph/ artifacts\" → \"hand-editing .fantasymayor-graph/ artifacts\""]}

   {:s1-step 4 :file "GLOSSARY.md" :do ["рядок 4 trigger: «before searching roslyn / ecs-graph / di-graph» → «before searching roslyn / fantasymayor-graph»" "рядки 15-16: «`roslyn` / `ecsg.py` / `dig.py`» → «`roslyn` / `fmgraph.py`»"]
    :then "python3 Tools/gen_index.py — генерований рядок INDEX.md:36 оновлюється сам"}

   {:s1-step 4 :file "DOC_STANDARD.md"
    :do ["рядок 18: (`roslyn` / `ecs-graph` / `di-graph`) → (`roslyn` / `fantasymayor-graph`)"
         "рядки 46-47 → один рядок tool-owns: #{writers readers reactive-consumers archetypes PK-FK priorities registered-as Lifetime installer injectors collections GameMode recipe-instances} fantasymayor-graph"]}

   {:s1-step 4 :file "INDEX.md" :line 83 :ask-first true
    :do "«`roslyn` / `ecsg.py` / `dig.py`» → «`roslyn` / `fmgraph.py`»"
    :note "зона агента — :ask-first; слово власника на s2 рахується питанням саме на цей рядок (:index-agent-zone у # Contra)"}

   {:s1-step 4 :file "Patterns/PATTERN_EVENT.md" :line 59 :do ":producer->consumer ecs-graph → :producer->consumer fantasymayor-graph"}
   {:s1-step 4 :file "Patterns/PATTERN_COMPONENT.md" :line 78 :do ":component-shape #{roslyn ecs-graph} → :component-shape #{roslyn fantasymayor-graph}"}

   {:s1-step 4 :file ".claude/skills/fantasymayor-pattern-choice/SKILL.md"
    :do ["рядок 3: «check the Tag Law before a new tag or archetype» → «check the tag law — one main tag plus label tags — before a new tag or archetype»"
         "рядок 115: :systems → \"python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py systems --role <role> — roles: reactive, per_frame, cleanup, pipeline_stage, turn_phase, startup_step, sub_system\""
         "рядок 116: :reactive → :recipe \"fmgraph.py pattern <RECIPE> — the recipe's live instances with decided_by base | marker | lexical and deviations; zero instances come with what the signature lacks\""
         "рядок 117: :families → \"fmgraph.py pattern PATTERN_ORCHESTRATOR_SUBSYSTEM, and mcp__roslyn__get_type_hierarchy direction Descendants on an abstract base; mcp__roslyn__find_implementations only on an interface\""
         "рядок 127: (:step-1 \"fmgraph.py explain <Tag> — which archetypes carry the tag, as main tag or as label\")"]}

   {:s1-step 5 :file "ARCHITECTURE.md" :ask-in-hook true
    :do ["рядок 88 table-rule :table: «(tag-law :discriminator)» → «(tag-law :main-tag)»"
         "рядки 96-103 tag-law → {:archetype {:requires \"exactly one main tag in every archetype declaration, written first in Tags.Get\" :never \"two main tags — that row cannot exist\"} :main-tag {:is \"the table discriminator — the identity of the entity type; systems filter by the archetype it names\" :unique \"one main tag names one archetype\" :exempt \"EventTag — the main tag of every event archetype\"} :label-tag {:is \"a role or membership marker beside the main tag — a struct marked [TagLabel]\" :role \"TagLabelRole.Transaction marks a transaction entity; no role = membership\" :count \"0-4 beside the main tag — Tags.Get takes at most 5 type arguments\" :never \"a query filter\"} :event-archetype {:exempt \"every event carries EventTag as its main tag; its archetype is named by the event component\"} :state — без змін :kind {:is \"…KindComponent wrapping an enum\" :never \"a second main tag\" :write \"set once at birth\"} :enum-columns — без змін :checked-by \"fantasymayor-graph tags; MarkerShapeAnalyzer for [TagLabel]\"}"
         "рядки 121-132 archetype-law — без змін: :arity-cap уже каже 5 type-аргументів"]
    :note "ask у hook — лише для головного агента; субагент коду hook не бачить (c-1 у # Contra)"}

   {:s1-step 5 :file "Patterns/PATTERN_TAG.md"
    :do ["рядки 13-15 → «A tag is an empty struct that marks an entity. It carries no data; its presence IS the information. TWO uses: the main tag — the table discriminator, the entity's identity (\"this row is an X\"), exactly one per archetype and unique to it; a label tag — a role or membership marker beside it, marked [TagLabel] (ARCHITECTURE → Entities, tag-law).»"
         "рядок 34: :per-entity {:exactly 1} → :per-entity {:main 1 :labels \"0-4\"} ;; Tag Law; EventTag is the main tag of every event; UITag and DistrictOpenConditionTag are labels"
         "після рядка 34 — :label \"[TagLabel] or [TagLabel(TagLabelRole.X)] on the struct; named in Tags.Get after the main tag; never a query filter\""
         "рядок 37: ;; a second tag breaks 1-entity-1-tag → ;; a second main tag breaks the tag law; a label never stands for a kind"
         "рядок 40: :producers+consumers ecs-graph → :producers+consumers fantasymayor-graph"]}

   {:s1-step 5 :file "Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md"
    :do ["рядок 27: «explore it via the ecs-graph / code headers» → «explore it via fantasymayor-graph pattern PATTERN_POLYMORPHIC_CATALOGUE / code headers»"
         "рядок 154: «Every row shares a **key** + THE discriminator tag (exactly one — Tag Law)» → «Every row shares a **key** + its archetype's main tag (exactly one — Tag Law), with the family label tag beside it when the family spans several archetypes»"
         "рядок 160 → «| Main tag | \"this row is a Foo\" — the row's ONLY main tag, named by the archetype | DistrictSingleOpenConditionTag |» і новий рядок таблиці після нього «| Family label tag | the family its archetypes share — [TagLabel], never a filter | DistrictOpenConditionTag |»"
         "рядок 226: ;; never a second tag → ;; never a second main tag"
         "рядок 228: :second-tag :NEVER ;; breaks 1-entity-1-tag (Tag Law) → :second-main-tag :NEVER ;; breaks one main tag per archetype (Tag Law)"
         "рядок 244: «(see [di-graph](../ARCHITECTURE.md) — collection resolution)» → «(see `fmgraph.py resolve <TBase>` — collection resolution)»"
         "рядок 270: «key(FK) + ONE discriminator tag + kind component» → «key(FK) + ONE main tag (+ the family label tag) + kind component»"
         "рядок 271: «never a second tag» → «never a second main tag»"]}

   {:s1-step 5 :file "Patterns/PATTERN_TRANSACTION_ENTITY.md" :line 91
    :do "«the identity tag never changes.» → «the main tag never changes, and the archetype carries TransactionTag — the label of role transaction — beside it.»"
    :from-s1 "c-9 s1 fix-a"}

   {:s1-step 5 :file "CLAUDE.md"
    :do ["рядок 93 :entity-shape: «{:archetype … :tag … :pk …}» → «{:archetype … :tag … :labels … :pk …}» і після «one map = one entity» — «; :tag is the main tag, :labels the label tags»"
         "рядок 94 :set-cardinality: «collection-valued key (:data, :fk)» → «collection-valued key (:data, :fk, :labels)»"
         "рядок 95 :tag-never-set: «DISPLAYS a Tag Law violation (2 identity tags)» → «DISPLAYS a Tag Law violation (2 main tags) — label tags go under :labels»"]}])
```

```clojure
(def memory-edits  ;; :memory-files — ~/.claude/projects/-Users-serhiikharsun-Documents-HomeProjects-FantasyMayor/memory/
  [{:s1-step 6 :file "project_fantasymayor_graph_tool.md" :kind :create
    :frontmatter "name project_fantasymayor_graph_tool, description «fantasymayor-graph — ONE project skill that builds and queries the ECS + DI + recipe-instance graph roslyn-mcp can't classify; use it instead of reading .cs», metadata node_type memory, type project"
    :body ["злиття двох графових скілів 2026-09 (задача ECS_GRAPH_PATTERN_INSTANCES): один граф, один CLI .claude/skills/fantasymayor-graph/scripts/fmgraph.py, артефакти .fantasymayor-graph/ (gitignored)"
           "ролі систем — з бази, де база вирішує, інакше з маркера [SystemRole]; view ↔ підписник — [ViewSubscriber]; транзакційна сутність — label-тег з [TagLabel(TagLabelRole.Transaction)]; маркер, що бреше, ламає компіляцію через MarkerShapeAnalyzer"
           "pattern <RECIPE> — екземпляри 15 рецептів; tags — один головний тег + label-теги; check — цілісність і попередження"
           "How to apply: ECS, DI чи «хто вже реалізує рецепт» — питати fmgraph.py, не читати .cs; самолікування перебудовує застарілий граф, зламана збірка — помилка без відповіді"]}
   {:s1-step 6 :file "project_ecs_graph_tool.md" :kind :delete :why "зміст злито в project_fantasymayor_graph_tool.md"}
   {:s1-step 6 :file "project_di_graph_tool.md" :kind :delete :why "зміст злито в project_fantasymayor_graph_tool.md"}
   {:s1-step 6 :file "MEMORY.md"
    :do ["рядки 35-36 → один рядок: «- [fantasymayor-graph skill (ECS + DI + recipes)](project_fantasymayor_graph_tool.md) — one graph, one CLI fmgraph.py: archetypes with main + label tags, reads/writes, reactive/per-frame roles from base or markers, DI registrations/injections/GameMode, pattern <RECIPE>; artifacts in .fantasymayor-graph/ (gitignored)»"
         "рядок 50: «(gen_index p1, build_graph, build_di_graph, doc_lint)» → «(gen_index p1, fmgraph.py build, doc_lint)»"
         "рядок 52: «BOTH graphs curate DETERMINISTICALLY in-build (no LLM); main agent runs build_graph.py + build_di_graph.py directly (di folded 2026-07-09)» → «the graph curates DETERMINISTICALLY in-build (no LLM); main agent runs fmgraph.py build directly (the two graph builds merged 2026-09)»"
         "рядок 59: «(di-graph curation went deterministic 2026-07-09)» → «(graph curation went deterministic 2026-07-09)»"]}
   {:s1-step 6 :file "feedback_delegate_graph_curation.md"
    :do ["рядок 3 description → «The graph curates DETERMINISTICALLY in its build (no LLM) — main agent runs fmgraph.py build directly. No docs-curator, no STEP-2 to delegate. graph-gate only bans ad-hoc artifact access.»"
         "рядок 10 → «The graph is DETERMINISTIC: fmgraph.py build extracts AND curates in ONE call, no LLM (the ECS and DI builds were separate until 2026-09 and each was deterministic since 2026-07-09). The main agent RUNS IT DIRECTLY.»"
         "рядок 12: «Why di-graph could go deterministic» → «Why the DI half could go deterministic»"
         "рядок 14: json.dump(open('.ecs-graph/graph.json','w')) → json.dump into the graph artifact"
         "рядок 16: «graph refresh (either graph) → just run its build script yourself (build_graph.py / build_di_graph.py, --force after a rename)» → «graph refresh → just run fmgraph.py build yourself»; посилання [[project_ecs_graph_tool]] і [[project_di_graph_tool]] → [[project_fantasymayor_graph_tool]]"]}
   {:s1-step 6 :file "feedback_docs_sync_needs_permission.md"
    :do ["рядок 3: «Doc/INDEX/canvas/ecs-graph sync» → «Doc/INDEX/canvas/graph sync»" "рядок 10: «`build_graph.py` / `build_di_graph.py`» → «`fmgraph.py build`»"]}
   {:s1-step 6 :file "project_dual_agent_contract.md" :line 58 :do "«{fantasymayor-session-start,di-graph,ecs-graph,migrate-to-codex}» → «{fantasymayor-session-start, the two graph skills of that time, migrate-to-codex}»"}
   {:s1-step 6 :file "project_entity_link_convention.md" :do ["рядок 15: «use the ecs-graph» → «use fantasymayor-graph»" "рядок 36: «`build_graph.py`» → «the graph build»"]}
   {:s1-step 6 :file "project_fm11_component_roles_program.md"
    :do ["рядок 28: «ecs-graph upgraded» → «the ECS graph tool (now fantasymayor-graph) upgraded»"
         "рядок 30: «new `ecsg.py tables`» → «new `tables`»"
         "рядок 50: «ecs-graph (di-graph untouched — no DI wiring changed)» → «the ECS graph (the DI graph untouched — no DI wiring changed)»"
         "рядок 51: «`ecsg.py spaces`» → «`spaces`»"]}
   {:s1-step 6 :file "project_tool_first_program.md"
    :do ["рядок 41: «narrowed to di-graph STEP-2 ONLY (and LATER, same-day 2026-07-09, di-graph curation» → «narrowed to the DI graph's STEP-2 ONLY (and LATER, same-day 2026-07-09, its curation»"
         "рядок 47: «Tools (ecs-graph/di-graph/roslyn/doc-lint)» → «Tools (the code graph/roslyn/doc-lint)»"]}
   {:s1-step 6 :kind :verify :do "grep назв у теці пам'яті — 0, [[project_ecs_graph_tool]] і [[project_di_graph_tool]] ніде не лишились"}])
```

```clojure
(def removal-edits  ;; :old-skills і тимчасовий код збірки — tool-cutover крок 7, лише коли meter-readings дали цілі
  [{:s1-step 7 :path "~/.claude/skills/ecs-graph/" :kind :delete :how "rm -rf теки цілком, включно з __pycache__"}
   {:s1-step 7 :path "~/.claude/skills/di-graph/" :kind :delete :how "rm -rf теки цілком"}
   {:s1-step 7 :path ".claude/skills/fantasymayor-graph/scripts/build.py" :kind :edit
    :do "видалити remove_legacy_artifact_dirs, LEGACY_ARTIFACT_DIRS і крок 13 хребта build_graph — теки прибрала перша збірка"}
   {:s1-step 7 :commit "~/.claude — лише на слово власника (CLAUDE.md § 4)"}])
```

```clojure
{:part tool-cutover
 :s1-coverage {:tool-home "skill-layout" :meter-readings "meter-readings" :gate-and-tooling "tool-cutover-edits: graph-gate.py, .gitignore, doc_lint.py, asmdef_reach.py"
               :tool-docs "tool-cutover-edits кроку 4" :tag-law-docs "tool-cutover-edits кроку 5" :memory-files "memory-edits"
               :old-skills "removal-edits" :grep-scope "meter-readings, grep з окремим .sdd-flow/project.md"
               :tally "8 / 8"}
 :consumer-rows "31 рядок # Consumers CONTEXT — усі в правках або з :change :none (PROJECT_ADAPTER.md:52, DOC_AGENT_REVIEW/FLOW.md), settings.json без змін; плюс знайдені на s2: ~/.claude/CLAUDE.md:49, MEMORY.md:50, feedback_docs_sync_needs_permission.md:10, вікілінки пам'яті"
 :delivery-order "частина 2 починається з :build-ban; далі — порядок s1"}
```

## Поправки власника на воротах s2

```clojure
(def owner-gate-amendments  ;; 2026-09-15 — слово власника на s2; стадія коду перекладає s2 РАЗОМ з цим блоком
  {:verdict :accepted
   :confirmed {:od-uniqueness "головний тег не спільний; EventTag — загальний виняток, бо на ньому працює прибирання подій (власник: PK — теж ідентичність, але покладаємось на тег)"
               :od-ui-tag "UITag і DistrictOpenConditionTag — label"
               :marker-type-shape "один SystemRoleAttribute з переліком SystemRoleKind — його важче забути"
               :od-view-pairs "рецепт view-system — лише пари з C#-підпискою"
               :di-facet-values "lifetime та installer — значення через кому"
               :legacy-cleanup-lifetime "код прибирання старих тек видаляється в кроці 7"}
   :from-owner
   [{:id :view-boundary
     :rule "MonoBehaviour-view не створює сутностей і подій і не отримує EntityStorages чи EntityStore — лише C#-подія, яку слухає її система"
     :why "власник: «стандартизуємо це як рішення. Що б View MonoBehaviour не створювали Entity»"
     :architecture {:file "ARCHITECTURE.md" :ask-in-hook true :who "головний агент (c-1 :fix-a)"
                    :do "у кінці ## Entities, після birth-completeness — (def view-boundary {:view \"a MonoBehaviour view never creates an entity or raises an event, and never receives EntityStorages or EntityStore\" :instead \"it raises a C# event; its driving system subscribes and writes the store (PATTERN_VIEW_SYSTEM)\" :checked-by \"fantasymayor-graph pattern PATTERN_VIEW_SYSTEM — deviations\"})"}
     :pattern {:file "Patterns/PATTERN_VIEW_SYSTEM.md" :line 75
               :do ":view … :never #{…} → той самий набір плюс коментар «;; ARCHITECTURE → Entities, view-boundary»"}
     :tool {:module "recipes.py — find_view_subscribers; source_reading.py — read_ecs_call"
            :do ["read_ecs_call: виклики CreateEntity і CreateEntities (архетипу чи store) — сирий факт creation з класом-власником, як CreateEvent"
                 "відхилення рецепта 12: клас kind view — власник CreateEvent, CreateEntity чи CreateEntities; або поле, параметр конструктора чи [Inject]/Construct-методу типу EntityStorages | EntityStore — рядок «view-boundary: <view> <що саме> @ місце»"]
            :expected-deviations "4 view: HexesUI, HexInfoPanelView, TurnPanelView, ContextTabsView — кожна тримає EntityStorages і створює подію (grep 2026-09-15); виправлення — поза задачею, рефакторинг"}
     :enforcement "відхилення в інструменті (попередження), не помилка компіляції — FM1005 лише на окреме слово власника, бо зламав би 4 наявні view"}]
   :code-stage {:who "субагент за цим артефактом" :architecture-edits "головний агент після звіту субагента — tag-law з tool-cutover-edits і view-boundary; hook питає власника"}})
```

## Потреби коду — :from-code

```clojure
(def from-code  ;; 2026-09-15, стадія коду: потреби, яких s2 не мав; поведінку, межі й рішення s2 не змінюють
  [{:id :analyzer-role-kind-copy :part drift-guard :from-code true
    :need "internal enum SystemRoleKind { PerFrame, Reactive } у MarkerVocabulary.cs — копія EcsExtensions.SystemRoleKind: аргумент атрибута приходить int-ом, а збірки гри аналізатор не бачить; без неї нотатка FindMismatches (SystemRoleKind)claim.ConstructorArguments[0].Value не компілюється"}
   {:id :analyzer-small-homes :part drift-guard :from-code true
    :need "BaseAnchor — enum у кінці MarkedTypeCheck.cs; кожен дескриптор має title і спільну const Category; MarkerVocabulary — приватний конструктор(Compilation), Resolve = new; TypeShape — конструктор на 6 полів (запис без IsExternalInit); csproj — RootNamespace FantasyMayor.Analyzers"}
   {:id :edge-record :part unified-graph :from-code true
    :need "Edge — frozen dataclass src dst rel via source_location args app_state collection confidence; to_document пише args, app_state, collection лише коли вони є"}
   {:id :resolve-nested :part unified-graph :from-code true
    :need "resolve: вкладений тип досяжний голим іменем лише коли верхньорівневого кандидата нема (TerrainView проти SystemPriorities.SubSystems.TerrainView); kind недекларованого вузла за суфіксом Tag | Event | Component | Installer | Config, інакше data — kind system ставить лише decide_roles; add_node: вгаданий kind не перекриває kind декларованого вузла"}
   {:id :source-reading-paragraphs :part unified-graph :from-code true
    :need "read_type_declaration поділено на read_attributes, read_fields, read_consts, read_event_names, read_parameters — абзац не вміщався; примітиви walk, nesting_chain, site_of (власник, простір імен, файл і місце сирого факту), body_expression, new_types_in_args, inferred_component_arg — порт з build_graph.py; види сайтів access | delete_entity | set | any_components | create_event | create_entity | tags_add | holder_call | typed_call | index_usage і register | register_instance | unknown_registration | state_composition"}
   {:id :declaration-id :part unified-graph :from-code true
    ;; converge 2026-09-15: resolve_type_use — тепер метод GraphDraft (rb-18)
    :need "declare_nodes ставить id на кожне оголошення (його читають read_markers і подальші кроки); resolve_type_use — колекція лишає своє generic-ім'я, елемент бере id; записи Markers, Ancestor, TypeFacts; грань вузла markers — її показує explain; константи CONFIG_BASES, VIEW_FOLDER, INSTALLER_BASES, TYPE_FIELDS_OF_SITES"}
   {:id :bind-owner :part unified-graph :from-code true
    ;; converge 2026-09-15: сигнатура — bind_owner(archetype_id, binding, draft) після rb-10; без охорони після rb-15
    :need "крок 4 expand_archetypes — помічник bind_owner(власник, архетип, прив'язка, draft) на кожну прив'язку; key_role — спільний для attach_tables і apply_key_role_law; dispose_sites пише attribute_disposals, index_usages, indexed_components і component_field_types — attach_tables"}
   {:id :events-for-every-class :part unified-graph :from-code true
    :need "decide_roles кличе connect_event_edges для кожного не-abstract класу, і з роллю none теж — так вимагає :note connect_event_edges і :polls-expected 4 (MainMenuState); CLASS_KINDS, UPDATE_LOOPS"}
   {:id :tag-audit-kinds :part unified-graph :from-code true
    :need "види відхилень no-main-tag, several-main-tags, shared-main-tag, event-tag-outside-event, label-in-filter, filter-tag-count, runtime-tag-write, tags-add; локальні помічники deviate і is_label"}
   {:id :recipe-signature-record :part unified-graph :from-code true
    :need "Signature {finder decided_by looks_for} — ознака рецепта з рядком для empty_reason; примітив instance; групи main і label у PATTERN_TAG, subscriptions у PATTERN_VIEW_SYSTEM; грані екземпляра tag, label_role, args, containers, kind_component, label_tags"}
   {:id :roots-from-files :part unified-graph :from-code true
    :need "write_graph бере meta.roots з шляхів source_files — імпорт SCAN_ROOTS з build.py замкнув би цикл імпортів; SCRIPTS_DIR у graph_store для is_graph_fresh"}
   {:id :query-helpers :part graph-queries :from-code true
    :need "counted і edge_details — спільний друк команд; INT_MAX"}   ;; rb-22, converge 2026-09-15: «answer_build імпортує build_graph локально» прибрано — імпорт угорі queries.py
   {:id :memory-line-16 :part tool-cutover :from-code true
    :need "feedback_delegate_graph_curation.md рядок 16: ще «ALLOWS … BOTH build scripts + the query CLIs; DENIES … .ecs-graph/ / .di-graph/» → fmgraph.py і .fantasymayor-graph/ — збіг meter grep, якого memory-edits не називав"}
   {:id :pattern-tag-wrap :part tool-cutover :from-code true
    :need "новий текст PATTERN_TAG рядків 13-15 розбито на рядки до 120 знаків — gen_index BUDGET; слова ті самі"}])
```

Converge 2026-09-15 — потреби, які народили знахідки read-back на слово власника (rb-13 … rb-24), і те, що
converge знайшов у коді без запису в s2. Поведінку й рішення s2 не змінюють, окрім rb-20 — його варіант власник обрав.

```clojure
(def from-code-converge  ;; 2026-09-15, прохід застосування знахідок read-back і converge
  [{:id :rb-4-type-argument-nodes :part unified-graph :from-code true :finding :rb-4
    :need "примітив type_argument_nodes(name_node) у розділі tree primitives source_reading.py — вузли між < і > generic-імені; кличуть read_type_use, read_registration двічі, analyze_invocation"}
   {:id :rb-13-template-use :part unified-graph :from-code true :finding :rb-13
    :need "read_template_use(invocation, type_args, src) — виявлення шаблонного вжитку, винесене з read_ecs_call; стоїть одразу за ним; повертає новий запис TemplateUse"
    :data {:template-use {:shape TemplateUse :as template :lives read_ecs_call
                          :fields {enclosing_method "ім'я охоплюючого generic-методу або порожньо" enclosing_params "його type-параметри"
                                   is_template_use "type-аргументи виклику — лише ці параметри: ретранслятор"}}}}
   {:id :rb-14-walk-templates :part unified-graph :from-code true :finding :rb-14
    :need "walk_templates(sources) — обхід шаблонів, винесений з expand_archetypes: повертає прив'язки, до яких дійшла черга; expand_archetypes бере прямі holder_call, потім walk_templates, у тому самому порядку; :holder-templates живе в walk_templates"}
   {:id :rb-15-unknown-forms :part unified-graph :from-code true :finding :rb-15
    :need "bind_owner без охорони — архетип і власник завжди є, відсутність падає KeyError; read_registration: Register без type-аргументів — сайт unknown_registration з формою «Register without type arguments», попередження дає connect_registrations; read_holder_members повертає запис HolderMembers, член Archetype поза формою GetArchetype(ComponentTypes.Get, Tags.Get) — рядок у unknown_forms; read_file кладе його у FileFacts.parse_warnings, read_sources — у SourceFacts.parse_warnings"
    :data {:holder-members {:shape HolderMembers :as holder_members :lives read_file
                            :fields {archetypes :holders unknown_forms :warnings}}}}
   {:id :rb-16-invocation :part unified-graph :from-code true :finding :rb-16
    :need "analyze_invocation повертає NamedTuple Invocation {method type_args receiver} або None; викликачі читають поля за іменем"}
   {:id :rb-17-constants :part unified-graph :from-code true :finding :rb-17
    :data {:type-declarations          {:as TYPE_DECLARATIONS :module source_reading.py :lives :external :holds "class, struct, record, interface, enum declaration" :from "read_file :flow-1"}
           :collection-generics        {:as COLLECTION_GENERICS :module source_reading.py :lives :external :holds "IReadOnlyList IEnumerable IList List ICollection IReadOnlyCollection" :from "read_type_use :how"}
           :unknown-registration-forms {:as UNKNOWN_REGISTRATION_FORMS :module source_reading.py :lives :external :holds "RegisterFactory RegisterComponentInHierarchy RegisterEntryPoint AsImplementedInterfaces" :from "read_registration :how"}
           :const-specials             {:as CONST_SPECIALS :module source_reading.py :lives :external :holds "int.MaxValue, int.MinValue з їхніми значеннями" :from "read_type_declaration :how — const int"}
           :event-frame                {:as EVENT_FRAME :module ecs_facts.py :lives :external :holds "EventFrameComponent" :from "expand_archetypes :note — склад подієвого архетипу"}
           :event-tag                  {:as EVENT_TAG :module ecs_facts.py :lives :external :holds "EventTag" :from "там само"
                                        :note "одна константа для трьох модулів: tag_law і roles імпортують EVENT_TAG, tag_law ще й EVENT_FRAME"}
           :template-queue-limit       {:as TEMPLATE_QUEUE_LIMIT :module ecs_facts.py :lives :external :holds "10000" :from "expand_archetypes :note — черга з лічильником"}
           :primitives                 {:as PRIMITIVES :module di_facts.py :lives :external :holds "int uint float double long ulong short byte bool char string object void decimal" :from "connect_injections :how — примітивні типи не беруться"}
           :view-boundary-types        {:as VIEW_BOUNDARY_TYPES :module recipes.py :lives :external :holds "EntityStorages, EntityStore" :from "owner-gate-amendments :view-boundary :tool"}}}
   {:id :rb-18-params :part unified-graph :from-code true :finding :rb-18
    :need "resolve_type_use(type_use, namespaces, location) — метод GraphDraft, кличе declare_nodes; tag_law: deviate(deviation, message) бере запис TagDeviation; конструктор TypeShape на 6 полів лишається — :analyzer-small-homes"
    :data {:tag-deviation {:shape TagDeviation :as deviation :lives audit_tag_law
                           :fields {kind "вид відхилення — :tag-audit-kinds" archetype_or_owner "архетип або власник фільтра" tags "теги відхилення" source_location "місце"}}}}
   {:id :rb-19-resolve-writes :part unified-graph :from-code true :finding :rb-19
    :need "draft.resolve дописує недекларований вузол і AMBIGUOUS-попередження — :writes доповнено на місці: read_markers +:nodes, attach_tables +:nodes :warnings, connect_injections +:warnings, connect_hosts +:nodes :warnings, find_recipe_instances +:nodes :warnings (через find_polymorphic_catalogues)"}
   {:id :rb-20-anchor :part #{unified-graph drift-guard} :from-code true :finding :rb-20
    :need "read_ecs_call: anchor base — лише під constructor_initializer з ключовим словом base, під this(...) — body; ecs_facts.anchor_of(site, draft) → base, коли власник має ребро inherits до SYSTEM_BASES, other-base — base(...) іншої бази, body — решта; кличуть bind_owner і фаза AnyComponents expand_archetypes; other-base не ставить ні base_anchor, ні anchor_events чи held_events, ребро reads неподієвого архетипу лишається; аналізатор SightHeldEventArchetype виключає лише BaseConstructorInitializer, як s2; обидві сторони = s1 marker-shapes anchored-on-event? і holds-event-archetype?"
    :data {:system-bases {:as SYSTEM_BASES :module ecs_facts.py :lives :external :holds "UpdatedSystem, LateUpdatedSystem" :from "s1 marker-shapes anchored-on-event? — пряма база"}}
    :latent "сьогодні систем з this(...) 0 і нащадків abstract-бази UpdatedSystem 0 — відбиток графа не змінився; димові прогони в scratchpad показали однакову міру з обох боків"}
   {:id :rb-21-prefixes :part unified-graph :from-code true :finding :rb-21
    :data {:key-role-warning             {:as KEY_ROLE_WARNING :module ecs_facts.py :lives :external :holds "key-role:" :note "префікс попереджень apply_key_role_law; фільтрує queries.answer_spaces"}
           :unmarked-subscription-warning {:as UNMARKED_SUBSCRIPTION_WARNING :module view_pairs.py :lives :external :holds "subscription to a view event without a marker:" :note "префікс попередження pair_view_subscribers; фільтрує recipes.find_view_subscribers"}}}
   {:id :rb-22-import :part graph-queries :from-code true :finding :rb-22
    :need "queries.py імпортує build_graph і KEY_ROLE_WARNING угорі модуля; у :query-helpers рядок про локальний імпорт прибрано"}
   {:id :analyzer-event-archetype-call :part drift-guard :from-code true
    :need "IsEventArchetypeCall(expression, semanticModel) — бере вираз аргументу, а не лише InvocationExpressionSyntax (SightBaseAnchor передає argument.Expression), і SemanticModel контексту дії; словник — поле _vocabulary; s2 :in #{:vocabulary} і :how через semanticModel.GetSymbolInfo — той самий зміст"}])
```

# Contra

Спершу — усі рішення s1 і s2, які власник може перевернути: вибране, confidence, найсильніша альтернатива.
Далі — справжні записи contra цієї стадії, потім вердикти s2 на contra s1.

```clojure
(def decisions-for-owner  ;; :stage — де рішення прийнято; :confidence — наскільки варіант правильний, 0-100
  [;; ── закон тегів і маркери ──────────────────────────────────────────────
   {:id :od-main-vs-label :stage :s1 :chosen "label — атрибут на struct тегу, роль тегу глобальна" :confidence 60 :alternative {:is "інтерфейс-нащадок ITag" :confidence 25}}
   {:id :od-uniqueness :stage :s1 :chosen "один головний тег на архетип, не спільний; виняток EventTag" :confidence 55 :alternative {:is "спільний головний тег-категорія дозволено" :confidence 45}}
   {:id :od-event-tag :stage :s1 :chosen "EventTag — головний тег архетипів подій" :confidence 65 :alternative {:is "EventTag — label, події без головного" :confidence 25}}
   {:id :od-ui-tag :stage :s1 :chosen "UITag і DistrictOpenConditionTag — label; 6 нових головних тегів" :confidence 50 :alternative {:is "лишити їх головними тегами-категоріями" :confidence 35}}
   {:id :new-tag-names :stage :s2 :chosen "імена в s2 new-tags: панель + Tag, District + Single | Exist + OpenCondition + Tag, Transaction + Tag" :confidence 60 :alternative {:is "імена членів холдера: OpenConditionSingle + Tag" :confidence 30}}
   {:id :od-label-home :stage :s2 :chosen "тег ролі transaction — shared kernel Assets/Scripts/EcsExtensions/" :confidence 60 :alternative {:is "Domains.Actions, BuildDistrictAction/Tags/" :confidence 25}}
   {:id :od-marker-home :stage :s2 :chosen "типи маркерів і DLL — Assets/Scripts/EcsExtensions/" :confidence 70 :alternative {:is "Assets/Scripts/Core/" :confidence 30}}
   {:id :marker-type-shape :stage :s2 :chosen "SystemRoleAttribute(SystemRoleKind), ViewSubscriberAttribute(Type) з AllowMultiple, TagLabelAttribute() і (TagLabelRole)" :confidence 55 :alternative {:is "два типи ролі PerFrameRole і ReactiveRole" :confidence 35}}
   {:id :od-pollers :stage :s1 :chosen "TurnProcessorSystem — per-frame 75; BuildDistrictCompletionSystem — reactive 65; DistrictBuildUISystem — per-frame 60" :confidence 65 :alternative {:is "DistrictBuildUISystem — reactive orchestrator (зміна поведінки)" :confidence 40}}
   {:id :od-shell :stage :s1 :chosen "BuildDistrictActionSystem — екземпляр рецепта 8; рецепт 9 показує 1" :confidence 65 :alternative {:is "маркер на shell" :confidence 35}}
   {:id :od-view-marker :stage :s1 :chosen "атрибут на підписнику з typeof(view)" :confidence 70 :alternative {:is "без атрибута, за іменем події" :confidence 20}}
   {:id :od-view-pairs :stage :s1 :chosen "рецепт 12 — лише пари з C#-підпискою" :confidence 55 :alternative {:is "плюс драйвери view без подій" :confidence 45}}
   {:id :s1-role-values :stage :s1 :chosen "маркер ролі — лише PerFrame | Reactive" :confidence 75 :alternative {:is "маркер на кожну роль рецептів" :confidence 25}}
   {:id :s1-label-role-in-marker :stage :s1 :chosen "роль label — у маркері, не ім'я тегу в коді інструмента" :confidence 60 :alternative {:is "ім'я тегу ролі в коді інструмента" :confidence 40}}
   ;; ── аналізатор ─────────────────────────────────────────────────────────
   {:id :od-analyzer-scope :stage :s1 :chosen "лише розбіжність форми трьох маркерів" :confidence 65 :alternative {:is "плюс надлишковий маркер і розміщення label як помилки" :confidence 35}}
   {:id :od-analyzer-placement :stage :s1 :chosen "DLL у теці збірки з типами маркерів" :confidence 75 :alternative {:is "окрема тека з власним asmdef" :confidence 20}}
   {:id :analyzer-project :stage :s2 :chosen "Tools/MarkerShapeAnalyzer/, namespace FantasyMayor.Analyzers, клас MarkerShapeAnalyzer" :confidence 60 :alternative {:is "Tools/Analyzers/ під майбутні аналізатори" :confidence 40}}
   {:id :od-analyzer-deps :stage :s2 :chosen "PackageReference Microsoft.CodeAnalysis.CSharp 4.3.0 з nuget.org" :confidence 65 :alternative {:is "HintPath на DLL ApiUpdater 4.4 з редактора" :confidence 35}}
   {:id :diagnostic-ids :stage :s2 :chosen "FM1001-FM1004, Error, категорія FantasyMayor.Markers" :confidence 60 :alternative {:is "FMMARK001-004" :confidence 40}}
   {:id :analyzer-registration :stage :s2 :chosen "SymbolStart + дії над вузлами оголошень + SymbolEnd" :confidence 70 :alternative {:is "дії по всій компіляції і звірка в CompilationEnd" :confidence 20}}
   {:id :dll-import-path :stage :s2 :chosen "агент збирає DLL у bin/, власник копіює в Assets/Scripts/EcsExtensions/ і налаштовує одним імпортом" :confidence 65 :alternative {:is "агент копіює DLL без .meta, власник налаштовує потім" :confidence 35}}
   {:id :build-ban-first :stage :s2 :chosen "уточнення заборони збірки в CLAUDE.md § 3 — першою правкою частини 2, до dotnet build" :confidence 80 :alternative {:is "лишити в tool-cutover — збірка аналізатора стоїть під старою забороною" :confidence 20}}
   ;; ── злитий граф ────────────────────────────────────────────────────────
   {:id :od-tool-name :stage :s2 :chosen "скіл fantasymayor-graph, CLI fmgraph.py, тека .fantasymayor-graph/" :confidence 60 :alternative {:is "fmgraph скрізь — коротша тека й скіл" :confidence 25}}
   {:id :cli-merge :stage :s2 :chosen "один виконуваний fmgraph.py з 17 підкомандами, build і check серед них" :confidence 70 :alternative {:is "окремий скрипт збірки поруч із CLI, як сьогодні" :confidence 30}}
   {:id :skill-layout :stage :s2 :chosen "13 модулів за кроками хребта; references graph-facts.md і recipe-signatures.md" :confidence 60 :alternative {:is "3 великі скрипти: збірка, сховище, запити" :confidence 40}}
   {:id :build-pass-shape :stage :s2 :chosen "один розбір і один обхід дерева на файл" :confidence 60 :alternative {:is "один розбір, два обходи" :confidence 40}}
   {:id :anchor-facets :stage :s2 :chosen "позиція прив'язки — грані вузла власника base_anchor, anchor_events, held_events" :confidence 60 :alternative {:is "окремий список прив'язок між кроками збірки" :confidence 40}}
   {:id :edge-dedup :stage :s2 :chosen "ключ ребра (src dst rel via args app_state), для registers і exposes ще source_location" :confidence 65 :alternative {:is "одне exposes на пару Impl-контракт" :confidence 35}}
   {:id :di-facet-values :stage :s2 :chosen "грані lifetime та installer — різні значення реєстрацій через кому" :confidence 55 :alternative {:is "Lifetime і інсталер лише на ребрах registers" :confidence 45}}
   {:id :recipe-reads-declarations :stage :s2 :chosen "ознаки рецептів читають і оголошення — поле [SerializeField] каталогу" :confidence 70 :alternative {:is "грань полів на вузлі config" :confidence 30}}
   {:id :unknown-node-exit :stage :s2 :chosen "find_node без збігу або з кількома — stderr і код 1" :confidence 70 :alternative {:is "код 0, як сьогодні" :confidence 30}}
   {:id :od-node-ids :stage :s1 :chosen "вузол на оголошення; архетип — Холдер.Член; args на ребрах" :confidence 65 :alternative {:is "id з префіксом виду" :confidence 35}}
   {:id :od-roots :stage :s1 :chosen "5 коренів з Assets/Flows" :confidence 75 :alternative {:is "4 корені" :confidence 25}}
   {:id :od-refresh :stage :s1 :chosen "повна перебудова при застарілості; збірка впала — помилка без відповіді" :confidence 80 :alternative {:is "відповідь зі старого графа з гучним stale" :confidence 35}}
   {:id :s1-reacts-vs-polls :stage :s1 :chosen "reacts_to — лише якір base(...) або маркер reactive; утриманий архетип — polls" :confidence 80 :alternative {:is "reacts_to на кожен EventArchetypes.Of" :confidence 20}}
   {:id :s1-config-transitive :stage :s1 :chosen "config — транзитивний родовід до ScriptableObject" :confidence 70 :alternative {:is "лише пряма база" :confidence 30}}
   {:id :s1-view-kind :stage :s1 :chosen "view — тека Views/ і родовід до MonoBehaviour" :confidence 70 :alternative {:is "лише родовід" :confidence 20}}
   {:id :s1-orchestrator-contract :stage :s1 :chosen "контракт — abstract клас у колекції конструктора; 10 / 29 / 12" :confidence 65 :alternative {:is "будь-який елемент колекції" :confidence 35}}
   {:id :s1-catalogue-kind-link :stage :s1 :chosen "FooConfig ↔ FooKindComponent за іменем" :confidence 60 :alternative {:is "ланцюг через підсистему спавну родини" :confidence 40}}
   {:id :s1-recipes-in-graph :stage :s1 :chosen "екземпляри рецептів рахує збірка і зберігає в графі" :confidence 60 :alternative {:is "рахувати при кожному запиті" :confidence 40}}
   {:id :s1-merged-node-facets :stage :s1 :chosen "DI-види — грані вузла оголошення" :confidence 70 :alternative {:is "окремі DI-вузли" :confidence 30}}
   ;; ── перехід ────────────────────────────────────────────────────────────
   {:id :s1-legacy-dirs :stage :s1 :chosen "старі теки артефактів прибирає перша збірка" :confidence 60 :alternative {:is "власник видаляє вручну" :confidence 40}}
   {:id :legacy-cleanup-lifetime :stage :s2 :chosen "код прибирання видаляється в кроці 7 разом зі старими скілами" :confidence 55 :alternative {:is "лишити назавжди як no-op" :confidence 45}}
   {:id :od-grep-scope :stage :s1 :chosen "поза Archive, текою задачі, .sdd-flow/ і DOC_AGENT_REVIEW/FLOW.md" :confidence 75 :alternative {:is "правити й канон, і чужий FLOW" :confidence 25}}
   {:id :od-consumers-scope :stage :s1 :chosen "споживачі поза планом — у межах задачі" :confidence 80 :alternative {:is "лишити" :confidence 20}}
   {:id :global-claude-md-example :stage :s2 :chosen "виправити приклад :accept у ~/.claude/CLAUDE.md:49" :confidence 65 :alternative {:is "виключити рядок з meter grep" :confidence 35}}
   {:id :index-agent-zone :stage :s2 :chosen "слово власника на s2 — дозвіл на рядок INDEX.md:83, показаний дослівно" :confidence 55 :alternative {:is "окреме питання на стадії коду" :confidence 45}}
   {:id :transaction-recipe-line :stage :s2 :chosen "PATTERN_TRANSACTION_ENTITY:91 називає label ролі transaction — формулювання закону тегів" :confidence 60 :alternative {:is "рецепт не чіпати" :confidence 40}}
   {:id :memory-merge :stage :s2 :chosen "дві пам'яті інструментів → одна project_fantasymayor_graph_tool.md" :confidence 75 :alternative {:is "переписати обидві" :confidence 25}}
   {:id :gitignore-lines :stage :s2 :chosen "виняток для csproj аналізатора, ігнор bin/, obj/ і __pycache__/" :confidence 80 :alternative {:is "аналізатор без csproj у git" :confidence 20}}])
```

```clojure
[{:id :c-1
  :kills "дозвіл власника на правку ARCHITECTURE.md"
  :case "стадія коду в авто-режимі йде субагентом; graph-gate.py рядки 100-102: agent_id або agent_type — hook виходить без рішення, ask не з'являється"
  :fails "CLAUDE.md § 3 :ask-first — «graph-gate hook asks; approving it IS the permission»; субагент правитиме закон тегів без питання"
  :fix [{:id :fix-a :confidence 70 :is "правку ARCHITECTURE.md робить головний агент після звіту субагента — hook питає власника" :cost "один крок стадії коду виходить із субагента"}
        {:id :fix-b :confidence 30 :is "слово власника на s2 рахується дозволом на текст tag-law, показаний у tool-cutover-edits" :cost "текст може змінитись на read-back — дозвіл тоді не на той текст"}]}

 {:id :c-2
  :kills "meter grep = 0 частини 5"
  :case "~/.claude/CLAUDE.md:49 — приклад {:accept {:meter ecs-graph …}} у глобальному глосарії; CONTEXT називав лише рядки 9-15"
  :fails "grep в області :od-grep-scope знаходить рядок, якого в переліку споживачів не було; це канонічна таблиця власника для всіх проєктів"
  :fix [{:id :fix-a :confidence 65 :is "замінити у прикладі назву інструмента — зміст рядка глосарія не змінюється" :cost "правка глобального файла власника"}
        {:id :fix-b :confidence 35 :is "виключити ~/.claude/CLAUDE.md:49 з meter" :cost "grep = 0 перестає бути правдою"}]}

 {:id :c-3
  :kills "meter grep над .sdd-flow/project.md"
  :case ":od-grep-scope виключає всю теку .sdd-flow/, а project.md — не керований файл і споживач (# Tools, # Meters, # Ceremonies)"
  :fails "grep може показати 0 при старих назвах у project.md"
  :fix [{:id :fix-a :confidence 80 :is "окремий grep по .sdd-flow/project.md — уже в meter-readings" :cost "одна команда"}
        {:id :fix-b :confidence 20 :is "виключати з області лише керовані файли з manifest.json" :cost "розбір manifest у meter"}]}

 {:id :c-4
  :kills "CLAUDE.md § 2 code-verification крок 3 на частині 1"
  :case "після правок холдерів UITag-архетипи несуть два теги; ecsg.py tags (старий аудит) дає 7 відхилень «carries 2 tags», поки частини 3-4 не готові"
  :fails "ціль «exactly one tag per archetype» старого аудиту червона на правильному коді"
  :fix [{:id :fix-a :confidence 70 :is "аудит тегів частини 1 — новим fmgraph.py tags після частини 4; для частини 1 — roslyn і arch-check" :cost "відкладений метр"}
        {:id :fix-b :confidence 30 :is "прогнати старий аудит і прийняти рівно 7 відомих відхилень" :cost "метр, що читається через виняток"}]}

 {:id :c-5
  :kills "SightBaseAnchor і SightHeldEventArchetype"
  :case "SymbolStart віддає в дії зовнішнього типу вузли вкладених типів; partial-клас з маркером в одному файлі й base(...) в іншому"
  :fails "без фільтра за ContainingType якір вкладеного типу пишеться зовнішньому; якщо ж дії SymbolStart у Roslyn 4.3 не доходять до частин в інших деревах, форма неповна — хибна розбіжність"
  :fix [{:id :fix-a :confidence 60 :is "фільтр за ContainingType уже в s2; перевірка власника в Unity — маркер на partial-класі в двох файлах" :cost "перевірка власника"}
        {:id :fix-b :confidence 40 :is "перейти на дії по всій компіляції з CompilationEnd" :cost "діагностики кінця компіляції, потокобезпечне накопичення"}]}

 {:id :c-6
  :kills "компіляція Unity між копіюванням DLL і налаштуванням імпорту"
  :case "DLL у Assets/Scripts/EcsExtensions/ з налаштуваннями за замовчуванням — плагін для всіх платформ з посиланням на Microsoft.CodeAnalysis"
  :fails "Unity не знаходить залежність і кидає помилки збірок, доки власник не зніме платформи й не поставить RoslynAnalyzer"
  :fix [{:id :fix-a :confidence 65 :is "власник копіює й налаштовує одним імпортом; звіт стадії коду дає точний шлях DLL і три дії" :cost "крок власника"}
        {:id :fix-b :confidence 35 :is "агент кладе DLL, власник одразу налаштовує" :cost "вікно з червоною компіляцією"}]}

 {:id :c-7
  :kills "подієвість у AnyComponents — аналізатор і інструмент (c-3 s1)"
  :case "подія, названа не за законом суфікса, у ComponentTypes.Get аргументу base(...)"
  :fails "обидві реалізації визначають подію іменем — такий клас дасть FM1001 або роль не reactive"
  :fix [{:id :fix-a :confidence 70 :is "суфікс Event | EventComponent — закон іменування; обидві сторони міряють однаково" :cost "подія не за законом — розбіжність форми"}
        {:id :fix-b :confidence 30 :is "подія — тип, що стоїть у EventArchetypes.Of або CreateEvent" :cost "аналізатор не бачить інших збірок — правила розходяться"}]}

 {:id :c-8
  :kills "перемикачі doc-lint у цьому файлі"
  :case "блоки new-tags, tag-law-and-markers-edits і tool-cutover-edits обгорнуто doc-lint off"
  :fails "справжня помилка в імені наявного типу всередині блоку привидом не стане"
  :fix [{:id :fix-a :confidence 70 :is "read-back знімає перемикачі, щойно нові теги є в коді, і doc_lint міряє блоки" :cost "одна правка read-back"}
        {:id :fix-b :confidence 30 :is "писати нові імена без суфікса, словами" :cost "перелік правок перестає бути точним"}]}]
```

```clojure
(def s1-contra-verdicts  ;; що s2 зробив з кожним записом contra s1
  [{:id :c-1 :verdict :fix-a :confidence 55 :note "унікальний головний тег; 6 нових тегів, 7 оголошень, типів тегів 30 — рішення :od-uniqueness"}
   {:id :c-2 :verdict :closed :note "grep 2026-09-15: місць народження поза холдерами 0; стадія коду повторює grep перед правкою холдерів"}
   {:id :c-3 :verdict :fix-a :confidence 70 :note "перенесено в c-7 цієї стадії"}
   {:id :c-4 :verdict :closed :note "SymbolStart закриває partial без GetSemanticModel і без CompilationEnd — :analyzer-registration; залишковий ризик — c-5"}
   {:id :c-5 :verdict :fix-a :confidence 75 :note "answer_resolve розбирає Ім'я<A,B> і фільтрує exposes за args"}
   {:id :c-6 :verdict :fix-a :confidence 70 :note "еталон config — roslyn get_type_hierarchy Descendants на 4 abstract SO-базах + 31; у meter-readings"}
   {:id :c-7 :verdict :fix-a :confidence 65 :note "10 контрактів / 29 членів / 12 хостів; у meter-readings"}
   {:id :c-8 :verdict :fix-a :confidence 60 :note "DistrictBuildUISystem — SystemRole(PerFrame); FM1002 на ньому неможливий"}
   {:id :c-9 :verdict :fix-a :confidence 60 :note "PATTERN_TRANSACTION_ENTITY:91 у tool-cutover-edits — :transaction-recipe-line"}
   {:id :c-10 :verdict :fix-a :confidence 65 :note "refresh_graph без except — помилка й код 1"}])
```

```clojure
{:gate-after-s2 {:subject "структура коду: декомпозиція, імена, час життя"
                 :owner-verdict :accepted
                 :at "2026-09-15"
                 :c-1 :fix-a
                 :amendments "## Поправки власника на воротах s2 — owner-gate-amendments"}}
```

# Read-back

Стадія read-back, 2026-09-15, субагент зі свіжим контекстом (рішення :isolation-in-auto). Прочитано цілком кожен
зачеплений файл коду, звірено з # s2, поправками власника і :from-code. Дванадцять знахідок виправлено в коді без
зміни поведінки — відбиток усіх команд графа до і після однаковий; тринадцять лишено на слово власника, кожна з
оцінкою виправлення. Вердикт власника ще не дано.

Оновлення 2026-09-15, прохід застосування (субагент, :isolation-in-auto): власник дав вердикт «читається» і наказав
застосувати всі знахідки, що чекали його слова. rb-13 … rb-24 виправлено за найвище оціненими варіантами — у кожної
`:done`; rb-25 лишено рішенням s2. Відбиток графа до і після однаковий, крім секунд збірки й часу генерації.

```clojure
(def read-back-run
  {:at "2026-09-15"
   :context :fresh
   :files-read {:game-csharp ["Assets/Scripts/EcsExtensions/ — SystemRoleAttribute.cs SystemRoleKind.cs ViewSubscriberAttribute.cs TagLabelAttribute.cs TagLabelRole.cs TransactionTag.cs"
                              "нові теги — HexInfoPanelTag.cs TurnPanelTag.cs ResourceBarTag.cs DistrictBuildUITag.cs DistrictSingleOpenConditionTag.cs DistrictExistOpenConditionTag.cs"
                              "label — UITag.cs DistrictOpenConditionTag.cs; холдери — PresentationUIArchetypes.cs EconomyArchetypes.cs ActionsArchetypes.cs"
                              "марковані — TurnProcessorSystem.cs BuildDistrictCompletionSystem.cs DistrictBuildUISystem.cs HexInfoPanelDistrictSystem.cs DistrictBuildListUISubSystem.cs DistrictBuildPriceUISubSystem.cs"]
                :analyzer ["Tools/MarkerShapeAnalyzer/ — MarkerShapeAnalyzer.csproj MarkerShapeAnalyzer.cs MarkerVocabulary.cs MarkedTypeCheck.cs TypeShape.cs"]
                :python ["fmgraph.py graph_store.py build.py source_reading.py graph_draft.py type_facts.py ecs_facts.py di_facts.py view_pairs.py tag_law.py roles.py recipes.py queries.py"]
                :docs-config [".claude/skills/fantasymayor-graph/SKILL.md, references/graph-facts.md, references/recipe-signatures.md"
                              ".claude/hooks/graph-gate.py .gitignore Tools/doc_lint.py Tools/asmdef_reach.py"
                              "CLAUDE.md GLOSSARY.md DOC_STANDARD.md .sdd-flow/project.md .claude/skills/fantasymayor-pattern-choice/SKILL.md"
                              "Patterns/ — COMPONENT EVENT TAG POLYMORPHIC_CATALOGUE TRANSACTION_ENTITY VIEW_SYSTEM; ARCHITECTURE.md — table-rule, tag-law, view-boundary"
                              "~/.claude/CLAUDE.md (git -C ~/.claude diff); пам'ять — MEMORY.md і 8 файлів memory-edits"]}
   :out-of-scope {:pre-existing-arch-check "стан _chromeHooked/_view (DistrictBuildUISystem), _hooked (DistrictBuildListUISubSystem, DistrictBuildPriceUISubSystem), _cancelHooked/_cancelHookedView (HexInfoPanelDistrictSystem); System.Collections.Generic у TurnProcessorSystem і DistrictBuildUISystem — було до задачі, не знахідки"
                  :ecs-conventions-comments "12 файлів .cs досі посилаються на ECS_CONVENTIONS — CONTEXT виніс це в DOC_AGENT_REVIEW :delete-stale-code-comments"
                  :doc-agent-review-diff "правки Flows/DOC_AGENT_REVIEW/FLOW.md датовані 14:54, до CONTEXT цієї задачі — не стадія коду"
                  :global-claude-md-other-edits "диф ~/.claude/CLAUDE.md несе й правки поза s2 — опис arch-check, 5 рядків глосарія, абзац Normalize every input; не цієї задачі"}})
```

```clojure
(def story-test-by-file  ;; :sequence :plot :variable :name :scale :why — відповіді після виправлень; номери знахідок — read-back-findings
  [{:group "C# гри — маркери, теги, холдери, марковані класи"
    :sequence "оголошення без алгоритму; маркер окремим рядком під [UsedImplicitly]; у холдерах головний тег першим у Tags.Get, label за ним"
    :plot :none
    :variable :none
    :name "кожен новий тип = символ s2 marker-types і new-tags; перелік SystemRoleKind, не SystemRole — :marker-type-shape"
    :scale "файл на тип, 9-21 рядок"
    :why "коментарі атрибутів = s2 :comment дослівно; тег — одне речення, що він розрізняє; два хибні коментарі UITag у DistrictBuildPriceUISubSystem виправлено (rb-11); два застарілі в незачеплених спавнерах — rb-23"
    :verdict "читається"}
   {:group "аналізатор Roslyn"
    :sequence "порядок життя: Initialize → StartCompilation → StartType → CollectClaims; MarkedTypeCheck: конструктор → SightBaseAnchor → IsEventArchetypeCall одразу після першого викликача → SightHeldEventArchetype → SightSubscription → ReportMismatches → MeasureShape → FindMismatches → DerivesFrom"
    :plot "StartCompilation 1 крок, StartType 1 крок + рядок часу життя + 4 реєстрації, ReportMismatches 3 рядки — дослівно spine-reads"
    :variable "vocabulary, claims, check, shape, mismatches, anchor, role, view — :as s2; perFrameShape, reactiveShape, subscribesToView — предикати marker-shapes"
    :name "SystemRoleKind-копія, BaseAnchor, Category, заголовки дескрипторів — :from-code; решта з s2"
    :scale "найдовший абзац FindMismatches — 38 рядків"
    :why "кожен return зворотного виклику має привід у коментарі — перший return SightSubscription дістав його на read-back (rb-12); чому ConcurrentBag — у коментарі класу"
    :verdict "читається"}
   {:group "fantasymayor-graph — 13 модулів Python"
    :sequence "алгоритмічні модулі — у порядку першого виклику після rb-1 і rb-2; graph_store, graph_draft, queries і таблиця шукачів recipes — клас-API незалежних точок входу"
    :plot "build_graph — 12 рядків = 12 кроків хребта; main — 6; know_types — 3; reconcile_ecs_facts — 6; reconcile_di_facts — 3; сайти читачів тепер повертаються, а не дописуються в параметр (rb-3)"
    :variable "root, source_files, sources, draft, types, graph, request, fresh — :as s2; ключі сайтів — :holds s2; кортеж called[0..2] з analyze_invocation — не іменований (rb-16)"
    :name "9 констант без :as у s2 (rb-17); примітив type_argument_nodes народився на read-back (rb-4)"
    :scale "read_ecs_call 78 і expand_archetypes 79 рядків — понад абзац (rb-13, rb-14); read_type_declaration 47, apply_key_role_law 48, audit_tag_law 52, declare_nodes 45 — одна справа з фазами під заголовками"
    :why "дописано: глобальний простір імен (rb-5), форма граматики const (rb-6), заголовок фази прив'язок і привід continue шаблонного обходу (rb-7); без приводу лишились три мовчазні охорони (rb-15)"
    :verdict "читається, з rb-13 … rb-22 на слово власника"}])
```

```clojure
(def read-back-meters  ;; до read-back → після виправлень
  {:call-order {:before 2 :after 0 :note "source_reading: read_type_use і read_attributes перед read_fields; ecs_facts: bind_owner перед register_archetype"}
   :steps-visible {build_graph "12 = 12" main "6 = 6" StartCompilation "1 = 1" StartType "1 = 1" ReportMismatches "3 = 3"}
   :names-from-artifact {:before 10 :after 9 :plus-read-back 1 :note "SCRIPTABLE_OBJECTS → CONFIG_BASES (rb-9); лишились константи rb-17; type_argument_nodes — rb-4"}
   :invented-at-translation {:from-code-entries 14 :born-at-read-back 1}
   :hidden-writes {:value 0 :note "полів :lives :run у Python нема; побічні записи draft.resolve поза :writes s2 — латентно, rb-19"}
   :one-step-fields {:value 3 :note "три мішки MarkedTypeCheck — rb-25, рішення s2"}
   :repeated-mechanics {:before 1 :after 0 :note "фільтр type_argument_list ×4 → type_argument_nodes"}
   :guards-without-occasion {:value 3 :note "rb-15"}
   :silent-returns {:analyzer 0 :python 3 :note "ті самі три охорони rb-15"}
   :unborn {:value 2 :note "пара src+rel, кортеж analyze_invocation — rb-16"}
   :one-release-place true
   :max-params {:before 8 :after 3 :note "лишились resolve_type_use 4, deviate 5, конструктор TypeShape 6 — rb-18"}
   :false-comments {:touched-before 2 :touched-after 0 :untouched 2 :note "rb-11 виправлено, rb-23 поза зачепленими файлами"}})
```

```clojure
(def read-back-findings
  [{:id :rb-1 :asks :sequence :verdict :fixed
    :finding "source_reading.py: read_type_use і read_attributes стояли перед read_fields, хоча перший їхній виклик — у read_fields"
    :done "read_fields перенесено одразу за read_type_declaration"}
   {:id :rb-2 :asks :sequence :verdict :fixed
    :finding "ecs_facts.py: bind_owner стояв перед register_archetype, а expand_archetypes кличе register_archetype першим"
    :done "функції переставлено"}
   {:id :rb-3 :asks :plot :verdict :fixed
    :finding "read_ecs_call, read_registration, read_state_composition, read_subscription дописували сайти в список-параметр — s2 дає їм :out, дані текли невидимо, 4 параметри"
    :done "кожен повертає list[dict], read_file робить file_facts.….extend(read_…(node, src, rel)); параметрів 3"}
   {:id :rb-4 :asks :repeated-mechanics :verdict :fixed
    :finding "фільтр дітей type_argument_list без < > , повторено 4 рази: read_type_use, read_registration двічі, analyze_invocation"
    :done "примітив type_argument_nodes у розділі tree primitives; converge вписує його в s2 :from-code"}
   {:id :rb-5 :asks :why :verdict :fixed
    :finding "read_file: usings.add(\"\") без причини"
    :done "коментар «the global namespace is seen from every file»"}
   {:id :rb-6 :asks :why :verdict :fixed
    :finding "read_consts читає значення двома формами без причини"
    :done "коментар про форму граматики; tree-sitter-c-sharp 0.23.5 кладе = і значення прямо під variable_declarator — перевірено розбором"}
   {:id :rb-7 :asks :why :verdict :fixed
    :finding "expand_archetypes: фаза прив'язок — єдина з чотирьох без заголовка; continue шаблонного обходу мовчить, чому хід обривається"
    :done "заголовок фази і коментар «a hop into neither a holder nor a template of the same arity reaches no archetype»"}
   {:id :rb-8 :asks :sequence :verdict :fixed
    :finding "build_ancestry: ancestor_names рахувався до охорони «оголошення не клас»"
    :done "обчислення після охорони"}
   {:id :rb-9 :asks :name :verdict :fixed
    :finding "recipes.py SCRIPTABLE_OBJECTS — друга назва набору type_facts CONFIG_BASES, чиє ім'я йде з s2 :numbers :config-bases"
    :done "from type_facts import CONFIG_BASES"}
   {:id :rb-10 :asks :variable :verdict :fixed
    :finding "bind_owner(owner_id, archetype_id, binding, draft): owner_id завжди binding[\"owner\"] — одне значення двома параметрами"
    :done "bind_owner(archetype_id, binding, draft)"}
   {:id :rb-11 :asks :why :verdict :fixed
    :finding "DistrictBuildPriceUISubSystem.cs рядки 42 і 134 називають сутність хрому «(UITag)» і «the UITag chrome entity» — UITag тепер label чотирьох HUD-архетипів"
    :done "DistrictBuildUITag в обох коментарях"}
   {:id :rb-12 :asks :why :verdict :fixed
    :finding "MarkedTypeCheck.SightSubscription: перший return без приводу, який мають обидва сусідні зворотні виклики"
    :done "коментар «A nested type's += belongs to that type.»"}
   {:id :rb-13 :asks :scale :verdict :fixed :was-kept-because "нова функція змінює структуру s2 — слово власника"
    :finding "read_ecs_call — 78 рядків; природний шов: виявлення шаблонного вжитку (enclosing_method, enclosing_params, is_template_use) і класифікація виклику"
    :fix [{:id :fix-a :confidence 55 :is "винести виявлення шаблонного вжитку в окрему функцію з записом-результатом, вписати в s2 :from-code"}
          {:id :fix-b :confidence 45 :is "лишити плаский розбір одного виклику"}]
    :done "read_template_use(invocation, type_args, src) → запис TemplateUse {enclosing_method enclosing_params is_template_use} одразу за read_ecs_call; read_ecs_call 78 → 74 рядки з двома рядками приводу якоря; s2 :from-code :rb-13-template-use"}
   {:id :rb-14 :asks :scale :verdict :fixed :was-kept-because "розбиття методу s2 — слово власника"
    :finding "expand_archetypes — 79 рядків, чотири різнорідні фази: конкретні члени, обхід шаблонів, прив'язки, AnyComponents"
    :fix [{:id :fix-a :confidence 60 :is "винести обхід шаблонів у функцію, що повертає прив'язки"}
          {:id :fix-b :confidence 40 :is "лишити одним абзацом із заголовками фаз"}]
    :done "walk_templates(sources) повертає прив'язки, до яких дійшов обхід; expand_archetypes бере прямі holder_call + walk_templates у тому самому порядку; 79 → 48 рядків; s2 :from-code :rb-14-walk-templates"}
   {:id :rb-15 :asks :why :verdict :fixed :was-kept-because "гілка → попередження чи її видалення змінює поведінку"
    :finding "три мовчазні охорони без приводу: bind_owner «not owner_id or archetype_id not in draft.nodes» — архетип завжди зареєстровано до прив'язки, власник сайту в типі завжди розв'язаний; read_registration «not type_args» — Register( без <…> у коді 0 (grep); read_holder_members «len(args) < 2» — оголошених членів 26, прочитаних 26"
    :fix [{:id :fix-a :confidence 65 :is "охорону bind_owner прибрати; дві інші — попередження форми поза відомими, як unknown_registration"}
          {:id :fix-b :confidence 35 :is "лишити як захист від майбутніх форм"}]
    :done "охорону bind_owner прибрано; Register без type-аргументів — сайт unknown_registration; член Archetype поза формою GetArchetype — рядок HolderMembers.unknown_forms → parse_warnings; сьогодні жодне не спрацювало — попереджень 9 до і після; s2 :from-code :rb-15-unknown-forms"}
   {:id :rb-16 :asks :variable :verdict :fixed :was-kept-because "новий тип — структура s2"
    :finding "ненароджені типи: пара src + rel у 9 сигнатурах читачів; кортеж (method, type_args, receiver) з analyze_invocation, викликачі читають called[0], called[1], called[2]"
    :fix [{:id :fix-a :confidence 60 :is "NamedTuple Invocation з полями method, type_args, receiver — розпаковка лишається тією самою"}
          {:id :fix-b :confidence 45 :is "ще й запис файла-джерела з src і rel"}
          {:id :fix-c :confidence 40 :is "лишити"}]
    :done "NamedTuple Invocation {method type_args receiver}; розпаковка method, type_args, receiver = called лишилась, індекси [0] [1] [2] — поля за іменем; пару src + rel fix-a не чіпав; s2 :from-code :rb-16-invocation"}
   {:id :rb-17 :asks :name :verdict :fixed :was-kept-because "імена даних s2 дає власник"
    :finding "9 констант без :as у s2, значення — з :how і :numbers: TYPE_DECLARATIONS, COLLECTION_GENERICS, UNKNOWN_REGISTRATION_FORMS, CONST_SPECIALS, EVENT_FRAME, EVENT_TAG (двічі — ecs_facts і tag_law; roles пише літерал \"EventTag\"), TEMPLATE_QUEUE_LIMIT, PRIMITIVES, VIEW_BOUNDARY_TYPES"
    :fix [{:id :fix-a :confidence 70 :is "converge вписує їх у s2 :from-code; один EVENT_TAG для трьох модулів"}
          {:id :fix-b :confidence 30 :is "перейменувати за назвами :numbers s2"}]
    :done "9 констант вписано в s2 :from-code :rb-17-constants; EVENT_TAG і EVENT_FRAME живуть лише в ecs_facts — tag_law і roles імпортують, літерал \"EventTag\" у roles прибрано"}
   {:id :rb-18 :asks :scale :verdict :fixed :was-kept-because "дві з трьох — :from-code, третя — вкладений помічник; зміна сигнатур — структура s2"
    :finding "понад 3 параметри: resolve_type_use 4, deviate 5 (перші чотири = запис tag_audit), конструктор TypeShape 6"
    :fix [{:id :fix-a :confidence 55 :is "resolve_type_use — метод GraphDraft; deviate бере запис відхилення"}
          {:id :fix-b :confidence 45 :is "лишити: TypeShape — конструктор запису без IsExternalInit"}]
    :done "resolve_type_use — метод GraphDraft (type_use, namespaces, location); deviate(TagDeviation, message) — запис відхилення tag_law; конструктор TypeShape на 6 полів лишився — fix-a його не називав; s2 :from-code :rb-18-params"}
   {:id :rb-19 :asks :variable :verdict :fixed :was-kept-because "код правильний, неповний облік у s2"
    :finding "draft.resolve додає недекларовані вузли й AMBIGUOUS-попередження, а :writes s2 у read_markers, attach_tables, connect_injections, connect_hosts, find_polymorphic_catalogues їх не називають; на кроці рецептів сьогодні 0 нових вузлів і 0 попереджень — виміряно прогоном у пам'яті"
    :fix [{:id :fix-a :confidence 70 :is "converge доповнює :writes s2"}
          {:id :fix-b :confidence 30 :is "find_polymorphic_catalogues звіряє голе ім'я поля з ім'ям бази до resolve"}]
    :done ":writes read_markers, attach_tables, connect_injections, connect_hosts, find_recipe_instances доповнено :nodes і :warnings — у s2 рядками з ;; :from-code rb-19 і записом :rb-19-resolve-writes"}
   {:id :rb-20 :asks :name :verdict :fixed :was-kept-because "зміна поведінки обох сторін"
    :finding "аналізатор і інструмент міряють «якір у base(...)» по-різному у двох латентних випадках: SightHeldEventArchetype виключає виклик під будь-яким ConstructorInitializerSyntax (s2 — BaseConstructorInitializer), а read_ecs_call ставить anchor base і для this(...); SightBaseAnchor вимагає пряму базу UpdatedSystem або LateUpdatedSystem, bind_owner ставить base_anchor за будь-якої бази; сьогодні систем з this(...) 0, abstract-нащадків UpdatedSystem 0"
    :fix [{:id :fix-a :confidence 60 :is "інструмент: anchor base лише в base(...) і лише за прямої бази UpdatedSystem або LateUpdatedSystem; аналізатор: фільтр BaseConstructorInitializer"}
          {:id :fix-b :confidence 40 :is "записати розбіжність у s2 як відому межу"}]
    :done "інструмент: anchor base лише під base(...), this(...) — body; anchor_of дає base лише за прямої бази UpdatedSystem | LateUpdatedSystem, base(...) іншої бази — ані якір, ані held (s1 marker-shapes); аналізатор: фільтр BaseConstructorInitializer, DLL перезібрано й покладено в Assets без .meta; димові прогони в scratchpad — this(...) held і інша база однаково з обох боків; s2 :from-code :rb-20-anchor"}
   {:id :rb-21 :asks :variable :verdict :fixed :was-kept-because "новий розділ чи константа — структура s2"
    :finding "recipes.find_view_subscribers вибирає відхилення з draft.warnings за префіксом тексту попередження view_pairs; queries.answer_spaces — за «key-role:»; читач шукача не бачить, звідки рядок"
    :fix [{:id :fix-a :confidence 50 :is "спільна константа префікса в модулі, що пише попередження"}
          {:id :fix-b :confidence 30 :is "view_pairs пише непозначені підписки в окремий розділ draft"}
          {:id :fix-c :confidence 20 :is "лишити"}]
    :done "KEY_ROLE_WARNING у ecs_facts і UNMARKED_SUBSCRIPTION_WARNING у view_pairs — модулі, що пишуть попередження; queries.answer_spaces і recipes.find_view_subscribers фільтрують за ними; s2 :from-code :rb-21-prefixes"}
   {:id :rb-22 :asks :why :verdict :fixed :was-kept-because ":from-code :query-helpers записав локальний імпорт — правка розійдеться із записом"
    :finding "queries.answer_build імпортує build_graph усередині функції; циклу імпорту нема, коментар пояснює перебудову, а не імпорт"
    :fix [{:id :fix-a :confidence 70 :is "імпорт угорі модуля і рядок :from-code прибрати на converge"}
          {:id :fix-b :confidence 30 :is "лишити"}]
    :done "from build import build_graph угорі queries.py; рядок :query-helpers про локальний імпорт прибрано з s2 :from-code"}
   {:id :rb-23 :asks :why :verdict :fixed :was-kept-because "файли поза зачепленими і поза переліком s2 — слово власника"
    :finding "DistrictSingleOpenConditionSpawnSubSystem.cs:13 «shared condition discriminator» і DistrictExistConditionSpawnSubSystem.cs:13 «the condition discriminator tag» — за новим законом дискримінує головний тег архетипу, DistrictOpenConditionTag — label родини"
    :fix [{:id :fix-a :confidence 75 :is "«its archetype's main tag beside the family label» в обох"}
          {:id :fix-b :confidence 25 :is "лишити до DOC_AGENT_REVIEW"}]
    :done "обидва коментарі спавнерів — «its archetype's main tag beside the family label»; кількість рядків та сама — source_location графа не зсунулись"}
   {:id :rb-24 :asks :name :verdict :fixed :was-kept-because "документ, а read-back виправляє лише код"
    :finding "пам'ять feedback_delegate_graph_curation.md рядок 14 — «go through the build scripts + read-only CLIs», хвіст двох інструментів у рядку, який memory-edits правили"
    :fix [{:id :fix-a :confidence 80 :is "«go through fmgraph.py»"}
          {:id :fix-b :confidence 20 :is "лишити як історію"}]
    :done "рядок 14 пам'яті — «go through fmgraph.py»"}
   {:id :rb-25 :asks :variable :verdict :kept-because :because "рішення s2 :note — між зворотними викликами Roslyn живе лише поле"
    :finding "_baseAnchors, _heldEventArchetypes, _subscribedEvents пише по одному зворотному виклику — лічильник one-step-fields 3"}])
```

```clojure
(def docs-config-check  ;; кожна правка — проти tool-cutover-edits, owner-gate-amendments і memory-edits
  [{:file ".claude/hooks/graph-gate.py" :matches true :note "докстрінг bash_is_gated — «the sanctioned graph tool» замість «one of …» за граматикою, зміст той самий"}
   {:file ".gitignore" :matches true}
   {:file "Tools/doc_lint.py, Tools/asmdef_reach.py" :matches true}
   {:file "CLAUDE.md" :matches true :note ":build-ban, крок 3 code-verification, hand-editing, notation-ecs-ext 93-95"}
   {:file "GLOSSARY.md, DOC_STANDARD.md, INDEX.md:83" :matches true}
   {:file ".sdd-flow/project.md" :matches true :note "# Tools, prefer-when GLOSSARY, # Meters, code-verification, # Ceremonies"}
   {:file ".claude/skills/fantasymayor-pattern-choice/SKILL.md" :matches true :note "рядки 3, 115-117, 127"}
   {:file "Patterns/PATTERN_EVENT, PATTERN_COMPONENT, PATTERN_TAG, PATTERN_POLYMORPHIC_CATALOGUE, PATTERN_TRANSACTION_ENTITY, PATTERN_VIEW_SYSTEM" :matches true :note "PATTERN_TAG 13-15 перенесено рядками — :from-code :pattern-tag-wrap"}
   {:file "ARCHITECTURE.md" :matches true :note "table-rule :main-tag, tag-law дослівно з s2, view-boundary дослівно з owner-gate-amendments; не редаговано на read-back"}
   {:file "~/.claude/CLAUDE.md" :matches true :note "розділи ecs-graph і di-graph видалено, приклад :accept — fantasymayor-graph"}
   {:file "пам'ять" :matches true :note "project_fantasymayor_graph_tool.md створено англійською — зміст :body s2; project_ecs_graph_tool.md і project_di_graph_tool.md видалено; MEMORY.md, feedback_*, project_* — за переліком; хвіст — rb-24"}
   {:file ".claude/skills/fantasymayor-graph/SKILL.md і references/" :matches true :note "розділи skill-layout; ролі systems включно з undecided — так і в коді"}
   {:removal "~/.claude/skills/ecs-graph і di-graph видалено (14 файлів у робочому дереві ~/.claude, коміт — на слово власника); remove_legacy_artifact_dirs і крок 13 прибрано з build.py"}
   {:stale-names {:grep "ecs-graph | di-graph | ecsg.py | dig.py | build_di_graph | build_graph.py поза Archive, текою задачі, .sdd-flow/ і DOC_AGENT_REVIEW/FLOW.md — 0; .sdd-flow/project.md — 0"
                  :old-tag-law "«exactly one tag», «1-entity-1-tag», «identity tags», «category tag» у документах і скілах — 0; у коді — rb-23"
                  :ecs-conventions "у документах 0; 12 коментарів .cs — поза задачею"}}])
```

```clojure
{:reconcile {:in-code-not-in-s2 ["9 констант — rb-17" "type_argument_nodes — народжено на read-back, rb-4"]
             :in-s2-not-in-code ["remove_legacy_artifact_dirs, LEGACY_ARTIFACT_DIRS, крок 13 build_graph — видалено кроком 7, як s2 і планував"
                                 "GraphDraft.add_node «state не понижується до іншого виду» — окремої гілки нема: state — грань true, add_node не пише None, пониження не трапляється"]
             :partial ["SightHeldEventArchetype — ConstructorInitializerSyntax замість BaseConstructorInitializer, rb-20"
                       "IsEventArchetypeCall бере (expression, semanticModel); словник — з поля, як :in s2"]
             :from-code-entries 14}
 :c-8 {:done "перемикачі doc-lint off/on біля new-tags, tag-law-and-markers-edits і tool-cutover-edits прибрано — нові теги є в коді"
       :doc-lint "3 привиди у 2 файлах — HexIdComponent у PATTERN_COMPONENT.md:42, PATTERN_TRANSACTION_ENTITY.md:51 і :72, не цієї задачі; 0 помилок Clojure"}
 :meters-after-fixes ["fmgraph.py build 0.28 с: 371 файл, 479 вузлів, 1255 ребер, 8 попереджень"
                      "відбиток 904 рядки — stats, check, tags, systems, tables, spaces, unresolved, pattern для 15 рецептів, explain на 9 вузлах, resolve — однаковий до і після"
                      "check — curated true, integrity clean, 8 попереджень, ті самі; tags — 0 відхилень"
                      "python3 -m py_compile усіх модулів — чисто"
                      "dotnet build MarkerShapeAnalyzer.csproj -c Release — 0 попереджень, 0 помилок; bin/ перезібрано після коментаря"
                      "mcp__roslyn__get_diagnostics DistrictBuildPriceUISubSystem.cs — 0 помилок"
                      "gen_index.py — LINT clean, INDEX.md без змін"]
 :owner-checks ["імпорт Tools/MarkerShapeAnalyzer/bin/Release/netstandard2.0/MarkerShapeAnalyzer.dll у Assets/Scripts/EcsExtensions/: зняти платформи, мітка RoslynAnalyzer"
                "компіляція Unity; маркер, що бреше, дає FM1001-FM1004; маркер на partial-класі у двох файлах (c-5)"]
 :findings {:total 25 :fixed 12 :kept 13 :needs-owner [:rb-13 :rb-14 :rb-15 :rb-16 :rb-17 :rb-18 :rb-19 :rb-20 :rb-21 :rb-22 :rb-23 :rb-24] :kept-by-s2 [:rb-25]}
 :findings-after-owner {:at "2026-09-15" :total 25 :fixed 24 :kept 1 :kept-by-s2 [:rb-25]
                        :pass "rb-13 … rb-24 за найвище оціненими варіантами, слово власника «Застосовуй все»"}
 :owner-verdict "reads"}   ;; власник 2026-09-15: «шикарно, все працює» — після імпорту DLL і компіляції Unity
```

# Converge

Прогін 2026-09-15 після застосування rb-13 … rb-24, субагент (:isolation-in-auto). Кожен метод, запис даних і
народжений тип s2 звірено з кодом: Python-модулі й аналізатор прочитано цілком, C# гри і документи частини 5 —
цілеспрямованим grep за текстом правок s2; символи коду без запису в s2 шукано розбором AST усіх 13 модулів.
Знайдене в s2, чого код уже не каже, і код без запису виправлено в s2 як :from-code (`from-code-converge`, рядки
`;; :from-code rb-19/rb-20/rb-14`). Лишилось одне :unrequested — ручна правка власника, яку агент не чіпає.

```clojure
[{:at "2026-09-15"
  :entries
  [;; ── частина 1 · tag-law-and-markers ─────────────────────────────────────
   {:s2 [SystemRoleAttribute SystemRoleKind ViewSubscriberAttribute TagLabelAttribute TagLabelRole] :verdict :present
    :code "Assets/Scripts/EcsExtensions/ — файл на тип" :note "Role і View — get-only властивості, форма C# для :fields; AttributeUsage і конструктори — як marker-types"}
   {:s2 [HexInfoPanelTag TurnPanelTag ResourceBarTag DistrictBuildUITag DistrictSingleOpenConditionTag DistrictExistOpenConditionTag TransactionTag] :verdict :present
    :code "шляхи й простори імен new-tags; TransactionTag з [TagLabel(TagLabelRole.Transaction)]"}
   {:s2 [placement-markers marker-home tag-law-and-markers-edits] :verdict :present
    :code "UITag і DistrictOpenConditionTag з [TagLabel]; холдери — Get<головний, label> у 7 рядках; маркери ролі на TurnProcessorSystem, BuildDistrictCompletionSystem, DistrictBuildUISystem; ViewSubscriber на 4 класах"}
   {:s2 "tag-law-and-markers-edits :numbers-check :role-markers 3" :verdict :unrequested
    :code "Assets/Modules/UserInput/Systems/CameraMovementSystem.cs:23 — [UsedImplicitly][SystemRole(SystemRoleKind.PerFrame)]"
    :note "ручна правка власника 20:25, після read-back; s2 такого маркера не знає; база LateUpdatedSystem вже вирішує per_frame — fmgraph check дає «redundant marker», аналізатор мовчить, бо форма збігається; агент не переписує ручних правок"
    :resolved-by :open}
   ;; ── частина 2 · drift-guard ──────────────────────────────────────────────
   {:s2 [Initialize StartCompilation StartType CollectClaims MarkerVocabulary.Resolve] :verdict :present
    :code "MarkerShapeAnalyzer.cs, MarkerVocabulary.cs" :note "приватний конструктор словника і Resolve = new — :analyzer-small-homes"}
   {:s2 [MarkedTypeCheck SightBaseAnchor SightSubscription ReportMismatches MeasureShape FindMismatches DerivesFrom] :verdict :present
    :code "MarkedTypeCheck.cs"}
   {:s2 SightHeldEventArchetype :verdict :present :code "MarkedTypeCheck.cs"
    :note "read-back мав :partial — ConstructorInitializerSyntax; після rb-20 фільтр BaseConstructorInitializer, як s2"}
   {:s2 IsEventArchetypeCall :verdict :partial :code "MarkedTypeCheck.cs — (ExpressionSyntax, SemanticModel)"
    :note "s2 :in #{:vocabulary}; код бере вираз і модель контексту, словник — з поля" :resolved-by "s2 amended :from-code — :analyzer-event-archetype-call"}
   {:s2 [compilation vocabulary marker-types shape-anchors marked-type claims declarations base-anchor base-anchor-sightings held-event-sightings subscription-sightings shape loop-contract anchor-on-event anchor-on-table held-event-archetypes subscribed-events tag-struct descriptors mismatches] :verdict :present
    :code "поля й локальні за :as drift-guard-data; BaseAnchor у кінці MarkedTypeCheck.cs, копія SystemRoleKind і Category — :from-code"}
   {:s2 analyzer-dll :verdict :present
    :code "Tools/MarkerShapeAnalyzer/bin/Release/netstandard2.0/MarkerShapeAnalyzer.dll → Assets/Scripts/EcsExtensions/MarkerShapeAnalyzer.dll"
    :note "перезібрано 2026-09-15 після rb-20 — 0 попереджень, 0 помилок; скопійовано лише DLL, .meta власника не чіпано"}
   ;; ── частина 3 · unified-graph ────────────────────────────────────────────
   {:s2 [build_graph list_source_files write_graph] :verdict :present :code "build.py, graph_store.py"
    :note "хребет 12 кроків — крок 13 і remove_legacy_artifact_dirs видалено кроком 7, як removal-edits; list_source_files :numbers 359 — сьогодні 371 файл, число коду, не структура"}
   {:s2 [remove_legacy_artifact_dirs legacy-artifact-dirs] :verdict :present :code "відсутні в build.py" :note "s2 сам призначив їм видалення в tool-cutover кроці 7"}
   {:s2 [read_sources read_file read_type_declaration read_type_use read_state_composition read_subscription] :verdict :present
    :code "source_reading.py" :note "абзаци read_type_declaration і примітиви — :source-reading-paragraphs; parse_warnings файла — :rb-15-unknown-forms"}
   {:s2 read_holder_members :verdict :partial :code "source_reading.py — повертає HolderMembers"
    :note "s2 :out :holders; після rb-15 — запис з archetypes і unknown_forms" :resolved-by "s2 amended :from-code — :rb-15-unknown-forms"}
   {:s2 read_ecs_call :verdict :contradicts :code "source_reading.py — anchor base лише під base(...)"
    :note "s2 :note «anchor base — виклик усередині constructor_initializer»; rb-20 fix-a, обраний власником, звузив до base(...)" :resolved-by "s2 amended :from-code — :rb-20-anchor і коментар у read_ecs_call"}
   {:s2 read_registration :verdict :present :code "source_reading.py" :note "Register без type-аргументів — теж unknown-form, :rb-15-unknown-forms"}
   {:s2 [know_types declare_nodes build_ancestry] :verdict :present :code "type_facts.py" :note "resolve_type_use тепер метод GraphDraft — :rb-18-params"}
   {:s2 read_markers :verdict :partial :code "type_facts.py" :note "пише й :nodes" :resolved-by "s2 amended :from-code — rb-19"}
   {:s2 expand_archetypes :verdict :partial :code "ecs_facts.py — фази: конкретні члени, прив'язки, AnyComponents"
    :note "обхід шаблонів — walk_templates; якір — anchor_of; base(...) іншої бази не дає граней" :resolved-by "s2 amended :from-code — :rb-14-walk-templates, :rb-20-anchor"}
   {:s2 [register_archetype reconcile_ecs_facts connect_component_access attribute_disposals apply_key_role_law resolve_priorities check_singleton_manifest] :verdict :present
    :code "ecs_facts.py" :note "attribute_disposals :numbers 7 — 7 попереджень DeleteEntity; KEY_ROLE_WARNING — :rb-21-prefixes"}
   {:s2 attach_tables :verdict :partial :code "ecs_facts.py" :note "resolve ключа пише :nodes :warnings" :resolved-by "s2 amended :from-code — rb-19"}
   {:s2 [reconcile_di_facts connect_registrations connect_boot_states] :verdict :present :code "di_facts.py"}
   {:s2 [connect_injections connect_hosts] :verdict :partial :code "di_facts.py" :note "resolve пише :warnings (і :nodes у connect_hosts); hosts 12"
    :resolved-by "s2 amended :from-code — rb-19"}
   {:s2 pair_view_subscribers :verdict :present :code "view_pairs.py" :note "підписників 4, підписок 5, view 4; префікс — :rb-21-prefixes"}
   {:s2 audit_tag_law :verdict :present :code "tag_law.py" :note "відхилень 0; TagDeviation — :rb-18-params"}
   {:s2 [decide_roles collect_role_evidence decide_role connect_event_edges] :verdict :present :code "roles.py" :note "reacts_to 20, polls 4; EVENT_TAG з ecs_facts"}
   {:s2 find_recipe_instances :verdict :partial :code "recipes.py" :note "resolve у find_polymorphic_catalogues пише :nodes :warnings; 15 ознак — recipe-signatures-in-code"
    :resolved-by "s2 amended :from-code — rb-19"}
   {:s2 recipe-signatures-in-code :verdict :present :code "recipes.py RECIPE_SIGNATURES — 15 рецептів, decided_by і ознаки як у мапі"}
   {:s2 [GraphDraft resolve add_edge warn to_document] :verdict :present :code "graph_draft.py" :note "resolve — ще :resolve-nested"}
   {:s2 add_node :verdict :partial :code "graph_draft.py"
    :note ":how «state не понижується до іншого виду» окремої гілки не має — state лише true, add_node не пише None; read-back записав це в :in-s2-not-in-code"
    :resolved-by "не виправлялось: твердження s2 тримається без гілки, поведінка та сама"}
   {:s2 [project-root scan-roots source-files source-facts declarations usings holders ecs-sites di-sites subscription-sites parser file-facts type-use draft nodes node-id edge edges markers ancestry type-facts archetype-declaration ecs-tables tag-audit role-evidence role-decision view-events recipe-signatures recipe-result recipe-instances warnings graph-doc graph-file] :verdict :present
    :code "локальні й поля за :as unified-graph-data"}
   {:s2 holder-templates :verdict :contradicts :code "ecs_facts.py walk_templates — templates"
    :note "s2 :lives expand_archetypes; після rb-14 — локальна walk_templates" :resolved-by "s2 amended :from-code — коментар у unified-graph-data і :rb-14-walk-templates"}
   ;; ── частина 4 · graph-queries ────────────────────────────────────────────
   {:s2 [main parse_request find_project_root is_graph_fresh refresh_graph load_graph answer_request find_node] :verdict :present :code "fmgraph.py, graph_store.py"}
   {:s2 [build check stats pattern systems tags explain neighbors search bfs tables spaces resolve consumers installer state unresolved] :verdict :present
    :code "queries.py COMMANDS — 17 обробників у порядку таблиці"}
   {:s2 [answer_pattern answer_tags answer_explain answer_resolve] :verdict :present :code "queries.py"}
   {:s2 "from-code :query-helpers «answer_build імпортує build_graph локально»" :verdict :contradicts :code "queries.py — імпорт угорі модуля після rb-22"
    :resolved-by "s2 amended :from-code — рядок прибрано, як rb-22 fix-a"}
   {:s2 "from-code :bind-owner «bind_owner(власник, архетип, прив'язка, draft)»" :verdict :contradicts :code "ecs_facts.py — bind_owner(archetype_id, binding, draft) після rb-10"
    :resolved-by "s2 amended :from-code — коментар у :bind-owner"}
   {:s2 [request project-root graph-file builder-source freshness loaded-graph graph-doc adjacency node-id answer] :verdict :present
    :code "за :as graph-queries-data" :note "builder-source — SCRIPTS_DIR, :roots-from-files"}
   ;; ── частина 5 · tool-cutover ─────────────────────────────────────────────
   {:s2 skill-layout :verdict :present :code ".claude/skills/fantasymayor-graph/ — SKILL.md, 13 модулів, references graph-facts.md і recipe-signatures.md"
    :note "у graph-facts.md рядок граней якоря і в SKILL.md рядок Honesty уточнено під rb-20 і rb-15"}
   {:s2 [tool-cutover-edits memory-edits removal-edits] :verdict :present
    :code "grep за текстом кожної правки — CLAUDE.md, graph-gate.py, .gitignore, doc_lint.py, asmdef_reach.py, project.md, GLOSSARY, DOC_STANDARD, INDEX.md:83, 6 Patterns/, pattern-choice SKILL.md, ARCHITECTURE.md tag-law і view-boundary, пам'ять; теки ecs-graph і di-graph і дві старі пам'яті відсутні"
    :note "feedback_delegate_graph_curation.md рядок 14 — rb-24"}
   {:s2 meter-readings :verdict :present :code "прогін 2026-09-15 — FLOW.md # Acceptance"
    :note "check дає 9 попереджень замість 8: дев'яте — redundant marker CameraMovementSystem з ручної правки власника"}
   ;; ── код без запису в s2 — розбір AST модулів і членів аналізатора ──────
   {:s2 "код без запису" :verdict :unrequested
    :code "TYPE_DECLARATIONS COLLECTION_GENERICS UNKNOWN_REGISTRATION_FORMS CONST_SPECIALS EVENT_FRAME EVENT_TAG TEMPLATE_QUEUE_LIMIT PRIMITIVES VIEW_BOUNDARY_TYPES type_argument_nodes; народжені цим проходом — Invocation TemplateUse HolderMembers read_template_use walk_templates anchor_of SYSTEM_BASES KEY_ROLE_WARNING UNMARKED_SUBSCRIPTION_WARNING TagDeviation GraphDraft.resolve_type_use"
    :resolved-by "s2 amended :from-code — from-code-converge"}]
  :found {:contradicts 4 :unrequested 22 :partial 9}   ;; unrequested: 9 констант і примітив, 11 народжених цим проходом, 1 маркер
  :contradicts 0
  :unrequested 1        ;; маркер CameraMovementSystem — ручна правка власника
  :resolved-by #{"s2 amended :from-code" :open}}]
```

# Calibration

Мірило самого каскаду за цей прогін (def cascade-calibration). Усі стадії — context, s1, s2, код, read-back і цей
прохід — вели субагенти з чистим контекстом в авто-режимі: рішення власника :isolation-in-auto, не канонічний
механізм stage-isolation; ворота CONTEXT і s1 власник зняв, бачив s2 з # Contra і read-back.

```clojure
{:at "2026-09-15"
 :run-shape {:isolation :isolation-in-auto :stages-as "субагенти, послідовно" :owner-gates-seen [:after-s2 :read-back]}
 :contra-noise "0 з 18 — contra s1 c-1 … c-10 і contra s2 c-1 … c-8; на воротах s2 власник прийняв c-1 :fix-a і не відкинув жодного запису"
 :invented-at-translation {:total 26
                           :code-stage 14                 ;; from-code, записані стадією коду
                           :translation-caught-late 3     ;; :rb-17-constants, :rb-19-resolve-writes, :analyzer-event-archetype-call — народжені перекладом, записані лише на converge
                           :born-at-read-back 1           ;; :rb-4-type-argument-nodes
                           :born-by-owner-approved-fixes 8} ;; rb-13 rb-14 rb-15 rb-16 rb-18 rb-20 rb-21 rb-22
 :names-lost {:at-read-back 1 :now 0 :note "SCRIPTABLE_OBJECTS — друга назва CONFIG_BASES, rb-9"}
 :read-back-findings {:total 25 :fixed 24 :kept 1 :fixed-at-read-back 12 :fixed-on-owner-word 12 :kept-by-s2 [:rb-25]}
 :converge {:contradicts 0 :unrequested 1 :found {:contradicts 4 :unrequested 22 :partial 9}
            :open "маркер SystemRole(PerFrame) на CameraMovementSystem — ручна правка власника після read-back; база вже вирішує роль"}
 :signals {:translation-constants "константи модулів народжуються перекладом без :as у s2 — 9 з 26; сигнал для правила «кожна константа — запис даних s2» чекає другого прогону"
           :late-accounting ":writes через спільний примітив (draft.resolve) s2 не обліковує — rb-19; той самий сигнал удруге — правило"}
 :owner-verdict "reads — власник 2026-09-15: «шикарно, все працює»"}
```
