---
category: A
read: archive
status: closed-by-owner
tags:
  - ecs
  - events
  - cascade
related:
  - "[FLOW](FLOW.md)"
  - "[CONTEXT](CONTEXT.md)"
---

# Subject

```clojure
{:task :event-log
 :flow "Flows/EVENT_LOG/FLOW.md"
 :context "Flows/EVENT_LOG/CONTEXT.md"
 :subject "журнал подій: сховище Events, кільце на тип події, курсори читачів, EventReader<TEvent>; перевід усіх нинішніх виробників і споживачів; зміна правил і перевірок"
 :previous-cascade :none
 :stage :s1
 :gates {:after-s1 ? :after-s2 ?}}
```

# s1

```clojure
(def EventLog
  {:makes "журнал подій: кожна подія — сутність в окремому сховищі Events, упорядкована в кільці свого типу з лімітом; кожен читач бере свої непрочитані події по одній, а облік прочитаного веде журнал"

   :criterion "читач отримує кожну подію свого типу рівно раз, у порядку підняття, поки її не витиснула новіша понад ліміт; видача = прочитано (auto-ack); читач, що тікає після виробника, бачить подію в тому ж тіку, перед ним — у наступному; нічого не видаляється за часом — лише витіснення найстарішої при записі понад ліміт"

   :data {:events-store   {:type EntityStore
                           :holds "лише сутності подій: значення події + штамп номера + власний головний тег типу; жодних рядків світу"
                           :from "новий EntityStore у конструкторі EntityStorages поруч із World і Singletons (рішення :store, :buffer-shape; факт: кілька EntityStore в одному процесі вже працюють)"}
          :event-payload  {:type IComponent
                           :holds "значення однієї події — struct нинішнього типу події (13 оголошених + запит стану); у 10 з 13 полів немає"
                           :from "виробник при піднятті; копія видається читачу"}
          :own-tag        {:type ITag
                           :holds "власний головний тег типу події; несе атрибут ліміту або не несе"
                           :from "оголошення події (рішення :capacity-per-event-tag); спільний EventTag прибирається"}
          :sequence-stamp {:type IComponent
                           :holds "struct з одним long — глобальний номер події на її сутності; замінює EventFrameComponent"
                           :from "журнал при піднятті (рішення :global-sequence)"}
          :global-sequence {:type long
                            :holds "останній виданий глобальний номер; 0 до першої події"
                            :from "журнал; живе весь процес"}
          :tag-schema     {:type EntitySchema
                           :holds "TagType.TagIndex кожного тегу і кількість тегів схеми; TagType несе System.Type тегу для читання атрибута"
                           :from "EntityStore.GetEntitySchema() Friflo — спільна схема всіх сховищ"}
          :kind-binding   {:type Array
                           :holds "int[] за ComponentType.StructIndex значення події → TagIndex власного тегу; -1, поки тип не зареєстровано"
                           :from "реєстрація журналу типу; звідки береться пара «значення ↔ тег» — contra :c-1"}
          :capacity-by-tag {:type Array
                            :holds "int[] за TagIndex: ліміт кільця типу"
                            :from "атрибут ліміту на власному тезі, інакше типовий 128 (рішення :default-capacity, :numbers)"}
          :ring-ids       {:type Array
                           :holds "за TagIndex — int[ліміт]: id живих сутностей подій цього типу по колу, від найстарішої"
                           :from "виділяє реєстрація; пише підняття (рішення :buffer-shape)"}
          :ring-head      {:type Array
                           :holds "int[] за TagIndex: слот найстарішої присутньої події"
                           :from "реєстрація (0); просуває витіснення"}
          :ring-count     {:type Array
                           :holds "int[] за TagIndex: скільки подій типу зараз у кільці, не більше ліміту"
                           :from "реєстрація (0); підняття +1, витіснення −1"}
          :ring-appended  {:type Array
                           :holds "long[] за TagIndex: скільки подій типу піднято за процес; зміщення найстарішої присутньої = appended − count"
                           :from "реєстрація (0); підняття +1"}
          :cursor-rows    {:type List<long>
                           :holds "за id курсора: зміщення в кільці свого типу наступної непрочитаної події; 0 = «без зміщення»"
                           :from "журнал додає рядок при народженні читача; пише лише читання (рішення :cursor-home, :reader-source)"}
          :reader         {:type EventReader<TEvent>
                           :holds "хендл без стану: id курсора, TagIndex свого типу, посилання на журнал; readonly-поле або параметр у власника"
                           :from "DI: одна відкрита generic-реєстрація Lifetime.Transient у VContainer 1.18.0 — кожна інʼєкція народжує новий читач (рішення :reader-source)"}
          :consumer-batch {:type bool
                           :holds "чи видав читач за цей прохід хоч одну подію — для споживача, що реагує раз на пачку"
                           :from "локаль проходу споживача (рішення :batch-reaction)"}
          :app-state-request {:type AppStateComponent
                              :holds "значення запиту на зміну стану: Requested (AppState); нинішня struct стає значенням події журналу, власний тег — нинішній AppStateTag; імена за :event/suffix вирішує s2"
                              :from "піднімає InitializationSystem (рішення :app-state-request-as-event)"}
          :pending-mode   {:type AppState?
                           :holds "режим, у який Boot перемкне машину після тіку; null — запиту нема"
                           :from "Boot; пише читання запитів стану замість обробника OnComponentChanged"}}

   :flow (-> (:step-1 "відкрити журнал: створити сховище Events і порожні таблиці кілець на кількість тегів схеми; глобальний номер 0; курсорів нема"
                      {:reads #{:tag-schema}
                       :writes #{:events-store :kind-binding :capacity-by-tag :ring-ids :ring-head :ring-count :ring-appended :global-sequence :cursor-rows}
                       :state "журнал порожній: жодного зареєстрованого типу, жодного курсора, жодної події"
                       :world :write})

             (:step-2 (cond
                        (:flow-1 "тип події вже має кільце (kind-binding за StructIndex значення ≠ -1)") (:conclusion-1 "нічого не робити")
                        (:flow-2 "інакше — перший дотик до типу: народження читача або перше підняття")
                        (:conclusion-2 "прочитати атрибут ліміту з власного тегу, без атрибута — 128; виділити кільце int[ліміт]; head 0, count 0, appended 0; звʼязати StructIndex значення з TagIndex тегу"))
                      {:reads #{:tag-schema :own-tag :kind-binding}
                       :writes #{:kind-binding :capacity-by-tag :ring-ids :ring-head :ring-count :ring-appended}
                       :state "тип події має порожнє кільце з відомим лімітом, і до нього веде звʼязок зі значенням події"})

             (:step-3 "народити читача: DI створює EventReader<TEvent> на кожну інʼєкцію; журнал реєструє тип (step-2), додає рядок курсора зі зміщенням 0 і віддає його індекс як id курсора"
                      {:reads #{:kind-binding}
                       :writes #{:cursor-rows :reader}
                       :state "у власника є читач, чий курсор «без зміщення»: перше читання дасть найстарішу присутню подію (рішення :first-read-position); усі читачі нинішніх класів народжуються при інʼєкції Boot, до першої події"})

             (:step-4 "підняти подію — з будь-якого місця на головному потоці: номер = глобальний + 1; (cond (:flow-1 \"кільце повне: count = ліміт\") (:conclusion-1 \"витіснити: видалити сутність ids[head], head = (head + 1) mod ліміт, count − 1\") (:flow-2 \"інакше\") (:conclusion-2 \"нічого не витісняти\")); створити в Events сутність одним викликом народження: значення + штамп номера + власний тег; ids[(head + count) mod ліміт] = id нової сутності; count + 1; appended + 1"
                      {:reads #{:event-payload :own-tag :kind-binding :capacity-by-tag :ring-ids :ring-head :ring-count :ring-appended :global-sequence}
                       :writes #{:events-store :sequence-stamp :global-sequence :ring-ids :ring-head :ring-count :ring-appended}
                       :state "подія — найновіша в кільці свого типу з глобальним номером; якщо ліміт був вичерпаний, найстаріша подія типу видалена і читач, що її не встиг, її втратив; виняток не кидається (рішення :retention-capacity)"
                       :world :write})

             (:step-5 "прочитати одну подію (TryRead): oldest = appended − count; (cond (:flow-1 \"курсор < oldest — його подію витіснено\") (:conclusion-1 \"курсор = oldest\") (:flow-2 \"інакше\") (:conclusion-2 \"курсор як є\")); (cond (:flow-3 \"курсор = appended\") (:conclusion-3 \"нічого не видати — false\") (:flow-4 \"інакше\") (:conclusion-4 \"слот = (head + курсор − oldest) mod ліміт; взяти сутність за ids[слот] з Events; скопіювати значення; курсор + 1; true\"))"
                      {:reads #{:reader :cursor-rows :capacity-by-tag :ring-ids :ring-head :ring-count :ring-appended :events-store}
                       :writes #{:cursor-rows :event-payload}
                       :state "споживач тримає копію значення наступної непрочитаної події, а курсор уже стоїть за нею — подія прочитана, навіть якщо обробка кине (рішення :commit-timing); або нових подій нема і курсор не нижче найстарішої присутньої"
                       :world :read})

             (:step-6 (cond
                        (:flow-1 "споживач реагує на кожну подію: поки TryRead дає подію")
                        (:conclusion-1 "діяти на її значенні або reconcile зі станом — і назад сюди")
                        (:flow-2 "споживач реагує раз на пачку: поки TryRead дає подію")
                        (:conclusion-2 "позначити пачку непорожньою — і назад сюди; після вичерпання, якщо пачка непорожня, відреагувати один раз")
                        (:flow-3 "читання вичерпано")
                        (:conclusion-3 "прохід закінчено; тиха умова виходу споживача перевіряється ПІСЛЯ вичерпання, щоб подія, яку сьогодні губить прибирання, не лежала в журналі до пізнішого проходу"))
                      {:reads #{:reader :event-payload}
                       :writes #{:consumer-batch}
                       :state "усі непрочитані події типу видані цьому читачу рівно раз, реакція виконана раз на подію або раз на пачку; кінець тіку нічого не видаляє — EventCleanupSystem немає"
                       :world :write})

             (:step-7 "Boot приймає запит стану: після тіку машини вичерпати свій читач запитів; останній запит пачки стає очікуваним режимом; якщо він ≠ поточному — перемкнути машину; очікуваний режим скинути"
                      {:reads #{:reader :app-state-request :pending-mode}
                       :writes #{:pending-mode}
                       :state "запит, піднятий у тіку, перемкнув стан у тому ж кадрі після тіку, як сьогодні; сутності стану в World і підписки OnComponentChanged немає"
                       :world :write}))

   :exits #{"читач типу без жодного виробника — TryRead ніколи нічого не дає; occasion: ForestHexAppearedEvent і ForestHexRemovedEvent мають споживачів і не мають виробника (CONTEXT # Occasions)"
            "подію записано, а її читач зараз не тікає — подія чекає в кільці до повернення стану або витісняється за лімітом; occasion: HexesUI не ховається, а читач TerrainGenerationGenerateEventComponent — лише MainMenuState; Gameplay-системи не тікають в інших станах (рішення :inactive-consumer)"}

   :numbers {:default-capacity "128 — розмір чанку Friflo НЕ підтверджено: Friflo.Engine.ECS.xml 3.6.0 має лише F:StructInfo.ChunkSize «Is a multiple of 64» без числа сутностей, а Chunk`1 — вид запиту над компонентами архетипу, не блок фіксованої ємності; тому гілка «інакше 128» рішення :default-capacity"
             :explicit-capacities ?
             :oldest-offset "appended − count"
             :slot "(head + (курсор − oldest-offset)) mod ліміт"
             :initial-cursor "0 — «без зміщення»; clamp до oldest-offset дає найстарішу присутню"
             :first-sequence "1; global-sequence = 0 до першої події"
             :event-types "14 = 13 оголошених (2 сплячі: ForestHexAppearedEvent, ForestHexRemovedEvent) + запит стану; 10 з 13 без полів"
             :raise-sites "15 = 14 сайтів CreateEvent + 1 підняття запиту стану"
             :reader-injections "25 = 16 споживачів з якорем у base(...) + 3 у HexInfoPanelDistrictSystem + 5 «є хоч одна» + 1 Boot"
             :store-bound "не більше Σ лімітів зареєстрованих типів сутностей у Events: 14 × 128 = 1792 при типовому ліміті"
             :cost "O(1) на підняття, витіснення і TryRead"
             :retired "EventCleanup = int.MaxValue; штамп кадру Time.frameCount; видимість «кадр після»"}})
```

```clojure
(def EventLog-migration  ;; що стається з кожною нинішньою родиною виробників і споживачів — на рівні даних
  [{:family :raise-sites
    :who #{HexSelectionSystem BuildDistrictActionSystem BuildDistrictActionCancelSystem BuildDistrictCompletionSystem TurnProcessorSystem GameplayState DistrictBuildUISystem HexInfoPanelDistrictSystem}
    :before "CreateEvent на World: EventFrameComponent{Frame} + значення + EventTag"
    :after "step-4 у сховище Events: значення + штамп номера + власний тег; колбеки DistrictBuildUISystem.OnConfirmed і HexInfoPanelDistrictSystem.OnCancelled і GameplayState.SeedStartOfPlay піднімають поза тіком — журнал це приймає"}
   {:family :view-producers
    :who #{HexesUI ContextTabsView TurnPanelView HexInfoPanelView}
    :after "піднімають у журнал так само, як сьогодні в World; відхилення :view/boundary лишається (рішення :views-raise-events-kept)"}
   {:family :lossy-producer
    :who #{BuildDistrictTurnTickSystem}
    :before "піднімає BuildDistrictCompleteEvent щоходу, поки хоч один лічильник ≤ 0"
    :after "піднімає один раз за хід, лише коли хоч один лічильник цього ходу вперше дійшов до нуля: TurnsLeft до декременту = 1, після = 0; BuildDistrictCompletionSystem лишається reconcile зі станом; коментарі «never optimize into raise-once» переписуються (рішення :lossy-producer)"
    :note "рядок з TurnsToBuild = 0 обслуговує BuildDistrictActionSystem: її подія (600) читається BuildDistrictCompletionSystem (603) у тому ж тіку, рядок зникає до фази ходу"}
   {:family :anchored-consumers
    :who #{HexSelectionViewSystem HexInfoPanelSystem HexInfoPanelHeaderSystem ContextTabsAvailabilitySystem HexInfoPanelResourcesSystem ContextTabSelectionSystem BuildDistrictActionSystem BuildDistrictActionCancelSystem TurnCountSystem HexIconsVisibilitySystem DistrictBuildProgressViewSpawnSystem DistrictBuildProgressViewDespawnSystem DistrictViewSpawnSystem DistrictOpenConditionEvaluatorTableChangedSystem ForestSpawnSystem ForestDespawnSystem}
    :before "якір EventArchetypes.Of<T> у base(...); Update(entity) на кожну сутність; IsRipe першим рядком"
    :after "якоря подій у base(...) нема; система тримає один читач свого типу і щотіку проходить step-6 гілкою «на кожну подію»; тіло реакції те саме, IsRipe зникає; тихі виходи (нема view, нема текстури) — після вичерпання; роль reactive = «тримає EventReader»; на якій базі стоїть клас без якоря — s2 (UpdatedSystem сьогодні вимагає якір)"}
   {:family :any-components-consumer
    :who #{HexInfoPanelDistrictSystem}
    :before "один запит AnyComponents над SelectedHexChangedEvent, TurnCompletedEvent, DistrictTableChangedEvent; reconcile на кожну сутність"
    :after "три читачі; кожен вичерпується; reconcile на кожну видану подію, як сьогодні на кожну сутність; обхід через запит зникає"}
   {:family :batch-consumers  ;; рішення :batch-reaction — п'ять нинішніх «є хоч одна дозріла»
    :who #{TurnProcessorSystem BuildDistrictCompletionSystem DistrictBuildUISystem DistrictBuildListUISubSystem MainMenuState}
    :after {TurnProcessorSystem "вичерпує читач NextTurnEvent щотіку в БУДЬ-ЯКОМУ статусі; лише в Idle непорожня пачка запускає один хід — події, що прийшли в тік Completed, губляться, як сьогодні (H1), кілька кліків — один хід (H2); лишається per_frame з маркером (рішення :per-frame-readers)"
            BuildDistrictCompletionSystem "вичерпує читач; непорожня пачка — один reconcile над усім in-progress; маркер Reactive знімається — роль вирішує читач"
            DistrictBuildUISystem "вичерпує читач DistrictBuildUIRequestedEvent у проході табличного якоря до тихого виходу; непорожня пачка — Open; лишається per_frame з маркером"
            DistrictBuildListUISubSystem "власний читач (H4); вичерпує його в populate; непорожня пачка — дефолтний вибір; Repopulate з кліку рядка знаходить читач уже вичерпаним і вибір не скидає"
            MainMenuState "читач TerrainGenerationGenerateEventComponent вичерпується перед тіком систем; непорожня пачка — _requestedMode = MapCreation; що цей запит ніхто не читає — паузована задача AppStateRunner, не цей каскад"}}
   {:family :app-state-request
    :who #{InitializationSystem Boot CoreArchetypes AppStateComponent AppStateTag}
    :before "Boot.Construct створює сутність AppStateArchetype у World і підписується на OnComponentChanged; InitializationSystem якориться на цьому архетипі й пише AppStateComponent на різниці; OnDestroy кидає без сутності"
    :after "InitializationSystem піднімає запит стану подією (step-4) — раз за перебування в Initialization, contra :c-3; Boot тримає читач запитів і проходить step-7; сутності стану в World, архетипу для неї, підписки й відписки нема (рішення :app-state-request-as-event; знімає відхилення :event/no-change-observers)"}
   {:family :cleanup-and-kernel
    :who #{EventCleanupSystem EventTag EventFrameComponent EventArchetypes EcsEventExtensions WorldInstaller}
    :after "EventCleanupSystem і її реєстрація з 8 прапорцями зникають (GameOverState і MapLoadingState лишаються без систем — допустимо); спільний EventTag і EventFrameComponent зникають; EventArchetypes зникає; CreateEvent/IsRipe замінює підняття в журнал і TryRead; WorldInstaller реєструє EventReader<TEvent> одним відкритим generic Transient; імена нових типів і методів — s2"}
   {:family :same-tick-delivery  ;; рішення :same-tick-delivery; пріоритети — SystemPriorities.RuntimeTick
    :ticks {SelectedHexChangedEvent "виробник 1 → усі шість споживачів 501-1030 у тому ж тіку"
            DistrictTableChangedEvent "Planned з 600 → 601, 604, 605, 610, 1030 у тому ж тіку; Removed з 602 і Built з 603 → 604, 605, 610, 1030 у тому ж тіку, 601 — у наступному; усі reconcile (H3)"
            BuildDistrictCompleteEvent "з 600 → 603 у тому ж тіку; з фази ходу всередині 1000 → 603 у наступному"
            TurnCompletedEvent "1000 → 1010 і 1030 у тому ж тіку"
            "події з view, колбеків і входу стану" "піднято поза тіком → читаються в першому тіку після підняття"
            AppStateComponent "InitializationSystem 2 → Boot після тіку машини, у тому ж кадрі"}}])
```

```clojure
(def EventLog-rule-surface  ;; CLAUDE.md § 6: спершу RULES_SPECIFICATION.md, потім carriers, потім перевірки з id у повідомленні, потім fantasymayor-rules-conformance
  {:order (-> (:step-1 "RULES_SPECIFICATION.md: змінити, зняти (id → :retired-ids у (def id-law)), народити; конструкції й реєстр префіксів")
              (:step-2 "carriers")
              (:step-3 "перевірки, що міряють ці правила")
              (:step-4 "fantasymayor-rules-conformance над змінами; target: no :diverges, no :incomplete, no :uncarried"))

   :retire #{:event/ripe :event/anchor :event/no-same-frame :event/cleanup :event/lossy-producer :orchestrator/ripe-once}

   :change {:event/is "подія — сутність у сховищі Events, що живе до витіснення, а не однокадрова"
            :event/raise "підняття у журнал: номер, кільце свого типу, витіснення найстарішої понад ліміт"
            :event/reaction "до вибору «на значеннях / reconcile» додається «на кожну подію або раз на вичерпану пачку»"
            :event/startup-bulk "вибір лишається; причина «однокадрові події не переживають конвеєр» замінюється"
            :event/one-way "«однокадрові події» → «події журналу»"
            :event/no-tag-on-persistent-row "теги подій стоять лише в сховищі Events"
            :tag/event-tag "кожна подія несе ВЛАСНИЙ головний тег свого типу; спільного тегу події нема"
            :tag/main-tag-unique "виняток «тег події» зникає"
            :table/cross-archetype "виняток «обхід усіх подій за спільним тегом» зникає"
            :system/definition "EventCleanupSystem зникає з переліку"
            :system/cadence "EventCleanupSystem зникає; «reactive на кожну подію» → «reactive на події свого читача»"
            :system/role-order "гілка cleanup зникає; reactive = клас циклу, що тримає EventReader; per_frame-маркер — виняток для такого класу; не-система з читачем — «споживач події» без ролі системи"
            :system/marker-required "маркер обов'язковий, коли клас циклу тримає EventReader, але є per_frame"
            :system/marker-forbidden "текст може лишитись; Reactive-маркер на власнику читача стає зайвим"
            :system/marker-value "PerFrame = контракт циклу + EventReader; значення Reactive втрачає законну форму — лишається лише PerFrame"
            :reactive/shape "без якоря в base(...) і без перевірки дозрілості: readonly EventReader, вичерпання, сторожі, дія"
            :choose/behavior-recipe "гілка «прибирає події — cleanup» зникає"}

   :born {:event/log-capacity "ліміт на власний тег типу: атрибут або типовий 128; нова подія понад ліміт витісняє найстарішу; переповнення не кидає"
          :event/sequence "глобальний монотонний номер кожної події між усіма типами"
          :event/reader "споживач тримає readonly EventReader<TEvent>, народжений DI як Transient, і читає while TryRead; курсор веде сховище; видача = прочитано"
          :event/read-position "новий курсор читає з найстарішої присутньої події; витиснене зміщення продовжує з найстарішої, що лишилась"
          :event/delivery "читач після виробника бачить подію в тому ж тіку, перед ним — у наступному"
          :event/drain-before-exit "тихий вихід споживача стоїть після вичерпання читача"
          :event/non-system-reader "читач дозволений стану, підсистемі й Boot; роль у графі — «споживач події»"
          :orchestrator/own-reader "підсистема, якій потрібна подія, тримає власний читач; оркестратор нічого їй не пересилає"
          :ids :by-id-law}

   :keep #{:event/declare :event/suffix :event/dormant-consumer :event/no-change-observers :thread/main-only :system/priority-is-order-only :view/boundary :view/ecs-only-across-a-boundary :state/no-instance-state :state/is-not-state :alloc/repeated-zero}
   :keep-note {:system/priority-is-order-only "доставка від пріоритету не залежить — лише тік, у якому її видно"
               :state/is-not-state "readonly EventReader — хендл сховища, курсор не в полі системи"}

   :specification-other {:id-prefixes {:event "«однокадрова подія» → «подія журналу»"}
                         :constructs {:event "EventTag → власний тег + штамп номера у сховищі Events"
                                      :event-archetype "знімається або стає кільцем типу"
                                      :cleanup "знімається"
                                      :anchor "подієвого якоря нема; якір лише табличний"
                                      :system-role "cleanup і утримуваний архетип зникають; доказ reactive — EventReader"
                                      :cadence "EventCleanupSystem зникає"
                                      :priority "EventCleanup = int.MaxValue зникає"
                                      :storages "додається Events"
                                      :marker-vocabulary "EventArchetypes зникає, EventReader додається"}}

   :carriers #{"ARCHITECTURE.md — Systems, Markers, Runtime forms, Entities, Events (правка лише з дозволу власника — graph-gate)"
               "Patterns/PATTERN_EVENT.md" "Patterns/PATTERN_REACTIVE_SYSTEM.md" "Patterns/PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM.md"
               "Patterns/PATTERN_PERFRAME_SYSTEM.md" "Patterns/PATTERN_TRANSACTION_ENTITY.md" "Patterns/PATTERN_VIEW_SYSTEM.md" "Patterns/PATTERN_COMPONENT.md"
               "Patterns/PATTERN_CLEANUP_SYSTEM.md — рецепт втрачає предмет; видалення лише через vault_delete"
               ".claude/skills/fantasymayor-pattern-choice/SKILL.md — role-marker, event-kernel"
               ".claude/skills/fantasymayor-graph/SKILL.md і references/graph-facts.md"
               ".claude/skills/fantasymayor-placement/SKILL.md — Events/ «one-frame»"
               "INDEX.md — лише через gen_index, не руками між маркерами"}

   :checks #{"fmgraph ecs_facts.py — вид event за власним тегом у Events, emits за підняттям у журнал, споживання за injects EventReader<T>"
             "fmgraph roles.py — без cleanup і audit_cleanup; reactive за EventReader; per_frame-маркер; роль «споживач події» для не-систем"
             "fmgraph source_reading.py — без сайтів EventArchetypes.Of і AnyComponents подій"
             "fmgraph tag_law.py — без правила EventTag + EventFrameComponent; власний головний тег події"
             "fmgraph recipes.py — сигнатури reactive / per_frame / event без якоря; cleanup знімається"
             "fmgraph di_facts.py — відкрита generic-реєстрація Register(typeof(X<>)) має бути видимою"
             "Tools/MarkerShapeAnalyzer — MarkedTypeCheck, TypeShape, FM1001/FM1002/FM1005/FM1006, MarkerVocabulary; перебудова dll у Assets/Scripts/EcsExtensions"
             "~/.claude/skills/arch-check/SKILL.md — EventCleanupSystem у визначенні системи й каденції"}})
```

```clojure
{:s1-tally {:state-named "7/7"
            :hollow-steps 0
            :undeclared 0
            :orphan 0
            :untouched-input 0
            :method-names "0 нових; EventReader<TEvent> і TryRead — імена рішень :consumer-shape і :reader-source; решта символів — нинішній код як джерело даних і точки інтеграції"}
 :contra [{:id :c-1
           :kills "step-2, step-4, step-5 — звʼязок значення події з кільцем"
           :case "EventReader<SelectedHexChangedEvent>: читач і підняття знають тип значення, а кільце й атрибут ліміту стоять на власному ТЕЗІ (рішення :capacity-per-event-tag); у 10 з 13 типів значення без полів"
           :fails "рішення не каже, звідки журнал знає, який тег належить значенню: без оголошеної пари kind-binding не заповнити, а два порожні struct на кожен безпольовий тип — подвоєння оголошень"
           :fix [{:id :fix-a :confidence 55 :is "значення події оголошує свій тег (generic-атрибут або інтерфейс на struct значення); TEvent — завжди значення; тег лише дискримінатор і носій ліміту" :cost "два struct на кожен тип, зокрема 10 безпольових"}
                 {:id :fix-b :confidence 25 :is "ключ кільця — сам тип значення (ComponentType.StructIndex), атрибут ліміту на значенні; власний тег лишається лише заради закону тегів" :cost "частково переглядає :capacity-per-event-tag — лише словом власника"}
                 {:id :fix-c :confidence 20 :is "безпольова подія — лише тег, TEvent = тег; подія з полями — тег + значення" :cost "дві форми читання й підняття проти однієї форми :consumer-shape"}]}
          {:id :c-2
           :kills "BuildDistrictCompletionSystem після рішення :lossy-producer"
           :case "BuildDistrictCompleteEvent витиснено до читання: понад ліміт подій типу між двома тіками 603, або стан Gameplay не тікає між фазою ходу і reconcile"
           :fails "лічильник пройшов нуль рівно раз, подію більше ніхто не підніме; рядок in-progress лишається з TurnsLeft < 0 назавжди — будівництво зависає без винятку"
           :fix [{:id :fix-a :confidence 70 :is "прийняти: виробник дає не більше однієї події за хід плюс одну на підтвердження, читач тікає щокадру в Gameplay, 128 не досяжні" :cost "нуль; ризик лише теоретичний"}
                 {:id :fix-b :confidence 30 :is "BuildDistrictCompletionSystem ще й читає TurnCompletedEvent і звіряє in-progress на кожному кінці ходу" :cost "другий читач і зайвий reconcile щоходу; повертає «дзвін за рівнем» в інше місце"}]}
          {:id :c-3
           :kills "step-7 — «запит піднято раз»"
           :case "InitializationSystem — система циклу стану Initialization; раз вона тримається тим, що Boot перемикає стан одразу після тіку, в якому прочитав запит"
           :fails "якщо перемикання не станеться (запит = поточному, або паузована задача поверне шлюз входу, що не тікає стан) — система піднімає запит щотіку і заповнює кільце до ліміту копіями"
           :fix [{:id :fix-a :confidence 55 :is "запит піднімає разовий крок входу стану Initialization (startup_step), а не система циклу" :cost "змінює вид InitializationSystem у модулі Boot — межа з паузованою задачею"}
                 {:id :fix-b :confidence 45 :is "лишити систему циклу; раз тримається негайним перемиканням Boot" :cost "раз залежить від порядку в Boot, а не від форми виробника"}]}
          {:id :c-4
           :kills "step-3 у плеєр-збірці IL2CPP (H9)"
           :case "OpenGenericInstanceProvider викликає MakeGenericType(EventReader<>, struct значення) у рантаймі; Standalone і Android — IL2CPP"
           :fails "закритий generic над value-type без статичної згадки може не мати AOT-коду — читач не народиться в плеєрі, хоча в Editor усе працює"
           :fix [{:id :fix-a :confidence 50 :is "прийняти: readonly-поле або параметр EventReader<X> у кожного власника — статична згадка закритого типу; перевіряє плеєр-збірка власника" :cost "ризик виявляється лише в збірці"}
                 {:id :fix-b :confidence 30 :is "закриті реєстрації на кожен тип події в інсталерах фіч" :cost "14 рядків реєстрації; одна відкрита реєстрація втрачається"}
                 {:id :fix-c :confidence 20 :is "не-generic фабрика в журналі, яку DI дає як Singleton" :cost "суперечить :reader-source — лише словом власника"}]}
          {:id :c-5
           :kills "«кожен ланцюжок працює як раніше» у латентних сценаріях (H5, H6)"
           :case "Generate натиснуто в Gameplay (HexesUI не ховається), або повторний вхід у Gameplay з тим самим контейнером"
           :fails "за :inactive-consumer подія лежить до витіснення: повернення в MainMenu одразу просить MapCreation; Gameplay-читачі нового світу читають події старого, BuildDistrictActionCancelSystem кине на старих Coords; сьогодні обидва шляхи недосяжні"
           :fix [{:id :fix-a :confidence 65 :is "прийняти як латентний ризик до появи «назад у меню»; записати в Findings" :cost "нуль зараз"}
                 {:id :fix-b :confidence 35 :is "при виході зі стану пересунути курсори його читачів на кінець кільця" :cost "суперечить :inactive-consumer — лише словом власника; стан мусить знати читачів своїх систем"}]}]
 :gate-after-s1 {:subject "the algorithm: is this how we compute, are these the structures, are these the conditions"
                 :owner-verdict ?}}
```

<!-- doc-lint: off — s1 amendments and s2 name types born by this cascade (the event marker interface and the app-state request event); they exist in code only after the code stage -->

# s1 amendments (gate 2026-09-16)

```clojure
(def EventLog-s1-amendments  ;; гейт s1, 2026-09-16 — ці поправки новіші за текст s1 вище і перемагають, де розходяться
  {:gate-after-s1 {:owner-verdict "схвалено з поправками" :decisions #{:event-is-one-component :capacity-all-default :initialization-request-kept :aot-closed-generics :clear-all-events :rule-ids-and-cleanup-recipe :completion-eviction-risk}}

   :data-changes
   {:events-store    "лише сутності подій, і кожна несе РІВНО ОДИН компонент — значення події; ні тегу, ні штампа на сутності (рішення :event-is-one-component)"
    :event-payload   {:type IEventTag :holds "struct значення події; тип реалізує інтерфейс-маркер події IEventTag, похідний від Friflo IComponent; цей тип і є «тегом» події — дискримінатором кільця"}
    :own-tag         :retired   ;; окремого ITag на подію нема; спільного тегу подій теж нема
    :kind-binding    :retired   ;; кільце ключується самим типом значення — звʼязку «значення ↔ тег» нема чого вести; c-1 закрито
    :tag-schema      {:type EntitySchema :holds "ComponentType.StructIndex типу значення і кількість типів компонентів схеми (Components.Length); атрибут ліміту читається з System.Type struct події"}
    :capacity-by-tag {:type Array :holds "ліміт кільця за StructIndex типу значення: атрибут ліміту на struct події, інакше 128; зараз атрибут не стоїть ніде — усі типи 128 (рішення :capacity-all-default)"}
    :ring-ids        {:type Array :holds "за StructIndex — по колу пари «id живої сутності події + її глобальний номер», від найстарішої"}
    :sequence-stamp  "не компонент на сутності (одна сутність — один компонент), а друга половина пари в кільці: номер стоїть поруч з id сутності"
    :ring-head       "за StructIndex типу значення, як і ring-count, ring-appended"
    :app-state-request "значення запиту — тип події з суфіксом події, що реалізує IEventTag; нинішні AppStateComponent, AppStateTag і архетип запиту зникають"
    :aot-declaration {:type Array :holds "одне оголошення наперед: закритий EventReader<TEvent> для кожного з 14 типів подій, щоб IL2CPP згенерував код; відкрита generic-реєстрація Transient лишається єдиним джерелом читачів (рішення :aot-closed-generics)"}}

   :flow-changes
   {:step-1 "таблиці кілець розмічаються на кількість типів КОМПОНЕНТІВ схеми, а не тегів"
    :step-2 "перший дотик до типу значення: ліміт з атрибута на struct події, інакше 128; звʼязок із тегом не ведеться"
    :step-4 "сутність народжується з одним компонентом — значенням; глобальний номер кладеться в кільце поруч з id сутності"
    :step-5 "без змін, крім ключа: кільце за StructIndex типу значення"
    :step-7 "запит стану — подія журналу свого типу; InitializationSystem піднімає її щотіку, поки стан Initialization; «раз» тримає Boot, що перемикає стан одразу після тіку (рішення :initialization-request-kept, c-3 варіант b)"
    :step-8 {:action "очистити журнал: для кожного кільця, поки воно не порожнє, витіснити найстарішу подію (видалити сутність, head + 1, count − 1); курсори не чіпаються — після очищення найстаріше зміщення = appended, тож перше читання кожного курсора піднімає його до кінця кільця: для читача журнал порожній; appended і глобальний номер не скидаються — зміщення лишаються монотонними"
             :reads #{:ring-ids :ring-head :ring-count}
             :writes #{:events-store :ring-head :ring-count}
             :state "у Events жодної сутності; кожен курсор при наступному читанні стоїть на кінці свого кільця"
             :world :write
             :note "хто і коли кличе очищення — не визначено (рішення :clear-all-events); каскад виклику не додає"}}

   :numbers-changes
   {:explicit-capacities "жодного — усі 14 типів мають 128"
    :store-bound "14 × 128 = 1792 сутностей у Events щонайбільше"
    :event-types "14 = 13 оголошених + запит стану"}

   :contra-resolved
   {:c-1 "закрито рішенням :event-is-one-component — кільце, ліміт і атрибут ключуються типом значення"
    :c-2 "варіант a: ризик прийнято, страховочної звірки на TurnCompletedEvent нема (рішення :completion-eviction-risk)"
    :c-3 "варіант b: запит лишається в системі циклу InitializationSystem (рішення :initialization-request-kept)"
    :c-4 "замість a/b/c: одне оголошення закритих EventReader<TEvent> для IL2CPP плюс відкрита реєстрація (рішення :aot-closed-generics)"
    :c-5 "замість a/b: API очищення всіх подій, step-8; викликача нема (рішення :clear-all-events)"}

   :rule-surface-changes
   {:tag/event-tag "подія — сутність у Events з одним компонентом типу з IEventTag; жодного тегу на події"
    :event/log-capacity "ліміт на ТИПІ ПОДІЇ (struct значення), атрибут або 128"
    :event/no-tag-on-persistent-row "лишається з новим текстом: компонент події (тип з IEventTag) ніколи не стоїть на сутності World"
    :cleanup-recipe "Patterns/PATTERN_CLEANUP_SYSTEM.md видаляється через vault_delete на етапі коду"}})
```

# s2

```clojure
(def EventLog-types  ;; кожен новий, змінений і прибраний тип; :file — ПРОПОЗИЦІЯ, fantasymayor-placement запускається перед кодом
  {IEventTag                  {:status :new :file "Assets/Scripts/EcsExtensions/IEventTag.cs" :kind :interface
                               :role "public interface IEventTag : IComponent — маркер типу події; struct події реалізує його замість IComponent"}
   EventCapacityAttribute     {:status :new :file "Assets/Scripts/EcsExtensions/EventCapacityAttribute.cs" :kind :attribute
                               :role "[AttributeUsage(Struct, AllowMultiple = false, Inherited = false)] sealed; int Capacity; const int DefaultCapacity = 128; конструктор кидає ArgumentOutOfRangeException на capacity < 1; зараз ніде не стоїть"}
   EventLog                   {:status :new :file "Assets/Scripts/EcsExtensions/EventLog.cs" :kind :class
                               :role "public sealed class — журнал подій: власне EntityStore Events, кільце на тип події, курсори читачів, глобальний номер; public Raise, public ClearAllEvents; internal OpenCursor, TryRead для EventReader; internal конструктор, як SingletonComponents"}
   EventRing                  {:status :new :file "Assets/Scripts/EcsExtensions/EventRing.cs" :kind :class
                               :role "internal sealed class — кільце одного типу події: архетип типу, слоти по колу, head, count, appended"}
   EventRingSlot              {:status :new :file "Assets/Scripts/EcsExtensions/EventRingSlot.cs" :kind :struct
                               :role "internal readonly struct — id живої сутності події плюс її глобальний номер"}
   EventReader<TEvent>        {:status :new :file "Assets/Scripts/EcsExtensions/EventReader.cs" :kind :class
                               :role "public sealed class, where TEvent : struct, IEventTag — хендл читача: журнал і id курсора; TryRead(out TEvent), DrainBatch()"}
   EventReaderAotDeclarations {:status :new :file "Assets/Scripts/Installers/World/EventReaderAotDeclarations.cs" :kind :static-class
                               :role "[Preserve] static class — одне оголошення закритих EventReader<TEvent> для 14 типів подій під IL2CPP (рішення :aot-closed-generics)"}
   AppStateRequestedEvent     {:status :new :file "Assets/Modules/Boot/Implementation/Events/AppStateRequestedEvent.cs" :kind :struct
                               :role "public struct : IEventTag { public AppState Requested; } — запит на зміну стану застосунку; namespace Modules.Boot.Implementation.Events"
                               :note "не в Boot.Core: Ecs.Extensions посилається на Boot.Core, тож Boot.Core не бачить IEventTag без циклу збірок"}

   EntityStorages             {:status :changed :file "Assets/Scripts/EcsExtensions/EntityStorages.cs" :role "+ public EventLog Events { get; } — народжується в конструкторі поруч із World і Singletons"}
   SystemPriorities           {:status :changed :file "Assets/Scripts/EcsExtensions/SystemPriorities.cs" :role "RuntimeTick.EventCleanup прибрано; summary RuntimeTick: споживач після виробника бачить подію в тому ж тіку, перед ним — у наступному; пріоритет — лише порядок"}
   SystemRoleKind             {:status :changed :file "Assets/Scripts/EcsExtensions/SystemRoleKind.cs" :role "лишається лише PerFrame — Reactive прибрано: reactive вирішує сам EventReader, законної форми для значення нема"}
   SystemRoleAttribute        {:status :changed :file "Assets/Scripts/EcsExtensions/SystemRoleAttribute.cs" :role "коментар: роль, яку база не вирішує, — клас циклу, що тримає EventReader і все ж тікає per-frame"}
   WorldInstaller             {:status :changed :file "Assets/Scripts/Installers/World/WorldInstaller.cs" :role "реєстрацію EventCleanupSystem прибрано; + builder.Register(typeof(EventReader<>), Lifetime.Transient) одразу після RegisterInstance(entityStorages)"}
   Installers.World.asmdef    {:status :changed :file "Assets/Scripts/Installers/World/Installers.World.asmdef" :role "+ посилання Flows.DistrictBuild — оголошенню AOT потрібен DistrictBuildUIRequestedEvent"}
   Flows.DistrictBuild.asmdef {:status :changed :file "Assets/Flows/DistrictBuild/Flows.DistrictBuild.asmdef" :role "+ посилання Ecs.Extensions — DistrictBuildUIRequestedEvent реалізує IEventTag"}
   Boot                       {:status :changed :file "Assets/Modules/Boot/Implementation/Boot.cs" :role "читач запитів стану замість сутності в World і OnComponentChanged"}
   InitializationSystem       {:status :changed :file "Assets/Modules/Boot/Implementation/Systems/InitializationSystem.cs" :role "IUpdatedSystem без якоря; щотіку піднімає AppStateRequestedEvent{ConfigLoading}"}
   MainMenuState              {:status :changed :file "Assets/Modules/Boot/Implementation/States/MainMenuState.cs" :role "читач TerrainGenerationGenerateEventComponent замість архетипу; EntityStorages з конструктора прибрано"}
   GameplayState              {:status :changed :file "Assets/Modules/Boot/Implementation/States/GameplayState.cs" :role "SeedStartOfPlay піднімає HexIconsVisibilityChangedEvent у журнал"}
   InitializationState        {:status :changed :file "Assets/Modules/Boot/Implementation/States/InitializationState.cs" :role "лише коментар класу: «today only cleanup» → «today only InitializationSystem» (contra :c-7)"}
   MapCreationState           {:status :changed :file "Assets/Modules/Boot/Implementation/States/MapCreationState.cs" :role "лише коментарі про прибирання подій; значення SettleFrames не змінюється (contra :c-7)"}

   EcsEventExtensions         {:status :removed :file "Assets/Scripts/EcsExtensions/EcsEventExtensions.cs" :role "CreateEvent і IsRipe removed — замінює EventLog.Raise і EventReader.TryRead"}
   EventArchetypes            {:status :removed :file "Assets/Scripts/EcsExtensions/EventArchetypes.cs" :role "removed — подієвого якоря нема"}
   EventCleanupSystem         {:status :removed :file "Assets/Scripts/EcsExtensions/EventCleanupSystem.cs" :role "removed — утримання лише витісненням"}
   EventFrameComponent        {:status :removed :file "Assets/Scripts/EcsExtensions/EventFrameComponent.cs" :role "removed — штамп кадру не потрібен"}
   EventTag                   {:status :removed :file "Assets/Scripts/EcsExtensions/EventTag.cs" :role "removed — подія несе один компонент"}
   AppStateComponent          {:status :removed :file "Assets/Modules/Boot/Core/Components/AppStateComponent.cs" :role "removed — значення запиту переходить в AppStateRequestedEvent"}
   AppStateTag                {:status :removed :file "Assets/Modules/Boot/Core/Tags/AppStateTag.cs" :role "removed"}
   CoreArchetypes             {:status :removed :file "Assets/Modules/Boot/Core/Archetypes/CoreArchetypes.cs" :role "removed — архетипу запиту в World нема; разом з порожніми теками Components, Tags, Archetypes"}
   :meta-files "кожен removed .cs іде разом зі своїм .meta (і .meta порожньої теки); нові файли дістають .meta від Unity — агент .meta не пише"})
```

```clojure
(def EventLog-methods  ;; клас-API: Raise, TryRead, OpenCursor, ClearAllEvents — незалежні точки входу; порядок у файлі: конструктор, Raise і його кроки, OpenCursor, TryRead і його кроки, ClearAllEvents, потім RingOf і CapacityOf (після першого виклику)
  {EventLog
   {:does "відкрити журнал: власне сховище подій, порожня таблиця кілець на кількість типів компонентів, курсорів нема, номер 0"
    :in #{:event-type-schema}
    :out :none
    :writes #{:events-store :rings :cursor-rows :global-sequence}
    :flow (-> (:step-1 "_store = new EntityStore()")
              (:step-2 "_schema = EntityStore.GetEntitySchema()")
              (:step-3 "_ringsByEventType = new EventRing[_schema.Components.Length]")
              (:step-4 "_cursors = new List<long>()"))
    :note "internal: народжує лише EntityStorages; _lastSequence = 0 за замовчуванням"}

   Raise<TEvent>
   {:does "підняти подію: вона стає найновішою в кільці свого типу з наступним глобальним номером"
    :in #{:event-payload}
    :out :none
    :writes #{:events-store :rings :global-sequence}
    :flow (-> (:step-1 "кільце типу — народжене при першому дотику" {:calls RingOf :out :ring})
              (:step-2 "кільце повне — витіснити найстарішу" {:calls EvictOldestWhenFull :out :none})
              (:step-3 "народити сутність з одним компонентом і номером" {:calls BirthEventEntity :out :ring-slot})
              (:step-4 "поставити слот у хвіст кільця" {:calls EventRing.Append :out :none}))
    :ends-with #{"count ≤ capacity; переповнення не кидає — найстаріша подія втрачена для того, хто її не прочитав (:retention-capacity)"}
    :note "public; where TEvent : struct, IEventTag; параметр in TEvent evt; головний потік (:thread/main-only) — підняття поза тіком (колбек view, вхід стану) приймається так само"}

   EvictOldestWhenFull
   {:does "звільнити місце під нову подію"
    :in #{:ring}
    :out :none
    :writes #{:events-store :ring}
    :flow (-> (:step-1 (cond (:flow-1 "ring.IsFull") (:conclusion-1 "витіснити найстарішу")
                             (:flow-2 "інакше") (:conclusion-2 "нічого"))))
    :calls #{EvictOldest}}

   EvictOldest
   {:does "видалити найстарішу подію кільця"
    :in #{:ring :events-store}
    :out :none
    :writes #{:events-store :ring}
    :how "_store.GetEntityById(ring.Oldest.EntityId).DeleteEntity(), потім ring.DropOldest()"
    :note "другий викликач — ClearAllEvents; стоїть після першого (Raise → EvictOldestWhenFull)"}

   BirthEventEntity<TEvent>
   {:does "народити сутність події в архетипі її типу й видати їй наступний глобальний номер"
    :in #{:ring :event-payload :global-sequence}
    :out :ring-slot
    :writes #{:events-store :global-sequence}
    :flow (-> (:step-1 "var entity = ring.Archetype.CreateEntity()")
              (:step-2 "entity.AddComponent(evt) — запис у колонку, яку архетип уже має (:archetype/assign-then-write); не структурна зміна")
              (:step-3 "_lastSequence += 1")
              (:step-4 "return new EventRingSlot(entity.Id, _lastSequence)"))}

   OpenCursor<TEvent>
   {:does "народити курсор читача зі зміщенням 0 і зареєструвати тип його подій"
    :in #{:cursor-rows}
    :out :cursor-id
    :writes #{:cursor-rows :rings}
    :flow (-> (:step-1 "торкнутися кільця типу" {:calls RingOf :out :none})
              (:step-2 "_cursors.Add(0)")
              (:step-3 "return _cursors.Count - 1"))
    :note "internal; результат RingOf відкидається навмисно: ліміт читається і кільце виділяється при народженні читача в DI, а не на першому читанні в тіку (:alloc/binds-the-path); зміщення 0 = «без зміщення» — перше читання дасть найстарішу присутню (:first-read-position)"}

   TryRead<TEvent>
   {:does "видати курсору наступну непрочитану подію його типу"
    :in #{:cursor-id :cursor-rows :rings :events-store}
    :out :event-payload
    :writes #{:cursor-rows}
    :flow (-> (:step-1 "кільце типу" {:calls RingOf :out :ring})
              (:step-2 "зміщення курсора, підняте до найстарішої присутньої" {:calls ClampedCursor :out :unread-offset})
              (:step-3 "копія події, курсор за нею" {:calls DeliverEventAt :out :event-payload}))
    :exits #{"unread-offset = ring.AppendedCount — непрочитаних нема: evt = default, return false; стоїть між step-2 і step-3, бо потребує зміщення; occasion: кожен тік кожного споживача закінчується тут — AppStateSystems.Tick викликає Update щокадру незалежно від подій (CONTEXT # Facts)"}
    :ends-with #{"return true"}
    :note "internal; сигнатура bool TryRead<TEvent>(int cursorId, out TEvent evt) (:method/try); нуль виділень"}

   ClampedCursor
   {:does "зміщення наступної непрочитаної події курсора, не нижче найстарішої присутньої"
    :in #{:ring :cursor-id :cursor-rows}
    :out :unread-offset
    :writes #{:cursor-rows}
    :flow (-> (:step-1 "var offset = _cursors[cursorId]")
              (:step-2 (cond (:flow-1 "offset < ring.OldestOffset — його подію витіснено або журнал очищено") (:conclusion-1 "offset = ring.OldestOffset; _cursors[cursorId] = offset")
                             (:flow-2 "інакше") (:conclusion-2 "як є")))
              (:step-3 "return offset"))
    :numbers {:oldest-offset "ring.OldestOffset = appended − count"}}

   DeliverEventAt<TEvent>
   {:does "видати копію події за зміщенням і поставити курсор за нею — видача = прочитано"
    :in #{:ring :cursor-id :unread-offset :events-store}
    :out :event-payload
    :writes #{:cursor-rows}
    :flow (-> (:step-1 "var slot = ring.SlotAt(offset)")
              (:step-2 "var evt = _store.GetEntityById(slot.EntityId).GetComponent<TEvent>()")
              (:step-3 "_cursors[cursorId] = offset + 1")
              (:step-4 "return evt"))
    :note "курсор рухається до обробки — виняток споживача подію не повертає (:commit-timing); сутність за id жива за побудовою кільця (лише живі id), тож перевірки IsNull нема (guards)"}

   ClearAllEvents
   {:does "видалити всі події з усіх кілець"
    :in #{:rings :events-store}
    :out :none
    :writes #{:events-store :rings}
    :flow (-> (:step-1 (cond (:flow-1 "для кожного кільця, що не null: поки !ring.IsEmpty") (:conclusion-1 "витіснити найстарішу — і назад сюди")
                             (:flow-2 "кільця обійдено") (:conclusion-2 "вихід"))
                       {:calls EvictOldest :out :none}))
    :note "public; курсори не чіпаються — після очищення OldestOffset = AppendedCount, і ClampedCursor підніме кожен курсор до кінця кільця на першому читанні: для читача журнал порожній; appended і глобальний номер не скидаються; викликача в каскаді нема (:clear-all-events)"}

   RingOf<TEvent>
   {:does "кільце типу події; при першому дотику народити його з лімітом типу"
    :in #{:event-type-schema :rings}
    :out :ring
    :writes #{:rings}
    :flow (-> (:step-1 "var eventType = _schema.GetComponentType<TEvent>()")
              (:step-2 "var ring = _ringsByEventType[eventType.StructIndex]; не null — return ring")
              (:step-3 "ring = new EventRing(_store.GetArchetype(ComponentTypes.Get<TEvent>(), default), CapacityOf<TEvent>())")
              (:step-4 "_ringsByEventType[eventType.StructIndex] = ring; return ring"))
    :calls #{CapacityOf}
    :exits #{"eventType == null — схема не знає TEvent: throw InvalidOperationException з іменем типу; occasion: розпізнавання компонента через похідний інтерфейс IEventTag у схемі Friflo не перевірене (contra :c-2)"}
    :numbers {:rings-length "_schema.Components.Length — StructIndex дорівнює індексу масиву, Components[0] завжди null"}
    :note "private; викликачі Raise, OpenCursor і TryRead — рівноправні точки входу клас-API, тому RingOf і CapacityOf стоять наприкінці файлу як спільні помічники (order :alphabet allowed)"}

   CapacityOf<TEvent>
   {:does "ліміт кільця типу: атрибут ліміту на struct події, інакше типовий"
    :in #{:capacity-attribute}
    :out :capacity
    :how "typeof(TEvent).GetCustomAttribute<EventCapacityAttribute>()?.Capacity ?? EventCapacityAttribute.DefaultCapacity"
    :numbers {:default-capacity "128 = EventCapacityAttribute.DefaultCapacity (рішення :capacity-all-default)"}
    :note "private static — без полів; відбиття лише при першому дотику до типу"}})
```

```clojure
(def EventRing-methods  ;; народжений запис: п'ять паралельних масивів s1 за одним індексом = ненароджений тип (pair-is-a-type)
  {EventRing
   {:does "порожнє кільце типу з відомим лімітом і архетипом"
    :in #{:event-archetype :capacity}
    :out :none
    :writes #{:ring-slots}
    :how "Archetype = archetype, _slots = new EventRingSlot[capacity]; head, count, appended = 0"}
   IsFull        {:does "кільце повне" :out :none :how "_count == _slots.Length"}
   IsEmpty       {:does "у кільці нема подій" :out :none :how "_count == 0"}
   OldestOffset  {:does "зміщення найстарішої присутньої події" :out :unread-offset :numbers {:oldest-offset "_appended − _count"}}
   AppendedCount {:does "скільки подій типу піднято за процес — зміщення, яке ще не народилось" :out :none :how "_appended"}
   Oldest        {:does "слот найстарішої присутньої події" :out :ring-slot :how "_slots[_head]"}
   DropOldest    {:does "забути найстарішу подію" :writes #{:ring-head :ring-count} :out :none
                  :numbers {:next-head "(_head + 1) mod _slots.Length"}
                  :note "сутність видаляє EventLog.EvictOldest до цього виклику — кільце сховища не знає"}
   Append        {:does "поставити слот найновішої події" :in #{:ring-slot} :writes #{:ring-slots :ring-count :ring-appended} :out :none
                  :numbers {:tail-slot "(_head + _count) mod _slots.Length"}
                  :note "викликається лише після EvictOldestWhenFull — повне кільце сюди не доходить"}
   SlotAt        {:does "слот події за зміщенням" :in #{:unread-offset} :out :ring-slot
                  :numbers {:slot "(_head + (offset − OldestOffset)) mod _slots.Length"}
                  :note "OldestOffset ≤ offset < AppendedCount гарантує TryRead"}})

(def EventReader-methods
  {EventReader
   {:does "народити читача з власним курсором у журналі"
    :in #{:storages}
    :out :none
    :writes #{:cursor-rows}
    :flow (-> (:step-1 "_log = storages.Events")
              (:step-2 "_cursorId = _log.OpenCursor<TEvent>()" {:calls EventLog.OpenCursor :out :cursor-id}))
    :note "єдиний конструктор public EventReader(EntityStorages storages) — VContainer будує його Transient на кожну інʼєкцію; поля лише readonly — хендл, не стан (:state/is-not-state)"}
   TryRead
   {:does "видати наступну непрочитану подію свого типу"
    :in #{:cursor-id}
    :out :event-payload
    :how "return _log.TryRead(_cursorId, out evt)"
    :note "public bool TryRead(out TEvent evt)"}
   DrainBatch
   {:does "вичерпати всі непрочитані події типу й сказати, чи пачка була непорожня"
    :in #{:cursor-id}
    :out :consumer-batch
    :flow (-> (:step-1 "var batchHeldEvents = false")
              (:step-2 (cond (:flow-1 "поки TryRead(out _)") (:conclusion-1 "batchHeldEvents = true — і назад сюди")
                             (:flow-2 "вичерпано") (:conclusion-2 "return batchHeldEvents"))))
    :note "public bool DrainBatch(); примітив п'яти споживачів «раз на пачку» (рішення :batch-reaction; primitive — 5 повторів); значення подій відкидаються — реакція раз зі стану (contra :c-8)"}})
```

```clojure
(def EventLog-data
  {:events-store      {:from-s1 :events-store :type EntityStore :as _store :lives :run
                       :holds "лише сутності подій, кожна рівно з одним компонентом типу з IEventTag"
                       :note "run власника = процес; створює й не звільняє ніхто, як World"}
   :event-type-schema {:from-s1 :tag-schema :type EntitySchema :as _schema :lives :external
                       :holds "схема компонентів Friflo: GetComponentType<TEvent>(), Components.Length"
                       :note "статична схема Friflo — ніхто не звільняє"}
   :rings             {:from-s1 :capacity-by-tag :shape ^:new EventRing :as _ringsByEventType :lives :run
                       :holds "EventRing[] за ComponentType.StructIndex; null — тип ще не торкався журналу"
                       :grows "елемент народжується при першому дотику до типу, далі лише мутує"
                       :note "поглинає s1 :ring-ids :ring-head :ring-count :ring-appended :capacity-by-tag"}
   :ring              {:shape ^:new EventRing :as ring :lives EntryPoint
                       :fields {Archetype :event-archetype, _slots :ring-slots, _head :ring-head, _count :ring-count, _appended :ring-appended}
                       :holds "кільце одного типу події; локаль Raise і TryRead, параметр їхніх кроків"}
   :ring-slots        {:from-s1 :ring-ids :type Array :as _slots :lives EventRing
                       :holds "EventRingSlot[capacity] по колу від _head; довжина = ліміт типу"}
   :ring-head         {:from-s1 :ring-head :type int :as _head :lives EventRing :holds "слот найстарішої присутньої події"}
   :ring-count        {:from-s1 :ring-count :type int :as _count :lives EventRing :holds "подій типу зараз у кільці, ≤ _slots.Length"}
   :ring-appended     {:from-s1 :ring-appended :type long :as _appended :lives EventRing :holds "подій типу піднято за процес; не скидається очищенням"}
   :capacity          {:from-s1 :capacity-by-tag :type int :as capacity :lives RingOf :holds "ліміт типу, далі живе як _slots.Length"}
   :capacity-attribute {:shape ^:new EventCapacityAttribute :as attribute :lives CapacityOf :holds "атрибут ліміту на struct події або null"}
   :event-archetype   {:shape ^:new Archetype :as Archetype :lives EventRing
                       :holds "архетип [TEvent] без тегів у сховищі Events — народження через archetype.CreateEntity() (:archetype/birth)"}
   :ring-slot         {:from-s1 :sequence-stamp :shape ^:new EventRingSlot :as slot :lives EntryPoint
                       :fields {EntityId :events-store, Sequence :global-sequence}
                       :holds "id живої сутності події плюс її глобальний номер; повертає BirthEventEntity у Raise, SlotAt у DeliverEventAt"}
   :global-sequence   {:from-s1 :global-sequence :type long :as _lastSequence :lives :run :holds "останній виданий глобальний номер; 0 до першої події"}
   :cursor-rows       {:from-s1 :cursor-rows :type List<long> :as _cursors :lives :run
                       :holds "за id курсора — зміщення наступної непрочитаної події в кільці свого типу"
                       :grows "+1 рядок на народження читача (DI, до першого тіку); не скорочується — Transient-читач живе до кінця процесу (f13)"}
   :cursor-id         {:from-s1 :reader :type int :as _cursorId :lives EventReader :holds "індекс рядка в _cursors; видає OpenCursor"}
   :unread-offset     {:shape ^:new long :as offset :lives TryRead :holds "зміщення наступної непрочитаної події після підняття до найстарішої"}
   :event-payload     {:from-s1 :event-payload :type IEventTag :as evt :lives :external
                       :holds "копія значення події; out-параметр — належить тому, хто читає або піднімає"}
   :storages          {:shape ^:new EventLog :as Events :lives :external
                       :holds "EntityStorages.Events — журнал, який бере EventReader і кожен виробник; реєстр живе весь процес"}
   :reader            {:from-s1 :reader :type EventReader<TEvent> :as _<eventsRead> :lives :external
                       :holds "readonly-поле власника — ім'я за таблицями споживачів нижче"
                       :note "дає DI (Transient), не звільняє ніхто; курсор живе до кінця процесу"}
   :consumer-batch    {:from-s1 :consumer-batch :type bool :as <noun>Requested :lives EntryPoint
                       :holds "чи пачка непорожня — результат DrainBatch у Update споживача «раз на пачку»"}
   :app-state-request {:from-s1 :app-state-request :shape ^:new AppStateRequestedEvent :as request :lives TryReadLastRequestedMode
                       :fields {Requested :pending-mode}
                       :holds "запит на режим застосунку"}
   :pending-mode      {:from-s1 :pending-mode :type AppState :as requestedMode :lives Update
                       :holds "останній запит пачки цього тіку"
                       :note "було AppState? у полі Boot._pendingMode; поле зникає — запит читається й застосовується в тому самому Update; bool + out замість nullable (:method/try)"}
   :aot-declaration   {:from-s1 :aot-declaration :type Array :as reader :lives DeclareClosedReaders
                       :holds "14 рядків new EventReader<X>(storages) з TryRead і DrainBatch у методі, якого ніхто не кличе"}})
```

```clojure
(def EventConsumer-per-event  ;; 16 класів родини :anchored-consumers — однакова зміна форми
  {:class-shape "sealed class : IUpdatedSystem (плюс IDisposable, де був); public AppState AppState { get; }; public int Priority => SystemPriorities.RuntimeTick.<та сама константа> (не override); конструктор (AppState appState, EntityStorages storages, EventReader<TEvent> <reader>, …решта параметрів як були) — base(...) нема; private readonly EventReader<TEvent> _<reader>"
   :methods
   {Update   {:does "видати реакції кожну непрочитану подію свого типу — по одній"
              :in #{:reader}
              :out :none
              :flow (-> (:step-1 (cond (:flow-1 "поки _<reader>.TryRead(out var evt)") (:conclusion-1 "реакція на цю подію — і назад сюди")
                                       (:flow-2 "непрочитаних нема") (:conclusion-2 "вихід"))
                                 {:calls <Reaction> :out :none}))
              :note "public void Update(GameState state); тихі виходи й кидки лишаються return і throw усередині реакції — цикл іде далі, вичерпання завжди повне (:event/drain-before-exit)"}
    <Reaction> {:does "колишнє тіло Update(state, entity) без першого рядка IsRipe; значення — з параметра, не з GetComponent на сутності"
                :in #{:event-payload}
                :out :none
                :note "private void <Reaction>(in TEvent evt) або без параметра, коли значення не читається; GameState не передається — жодне тіло його не читає"}}
   :structural "UpdatedSystem знімав id якірного архетипу подій; читач не перелічує сутностей, тож структурні зміни в реакції безпечні так само; перелічення, яке реакція відкриває сама, як і раніше знімає id (:structural/idiom)"
   :comments "summary класу: «anchored on the one-frame pulse», «ripe», EventCleanupSystem переписуються на «reads its EventReader»"
   :table
   [{:class HexSelectionViewSystem :file "Assets/Presentation/Terrain/Systems/HexSelectionViewSystem.cs" :event SelectedHexChangedEvent :reader selectedHexChanges :reaction SyncSelectionBorder :values false}
    {:class HexInfoPanelSystem :file "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelSystem.cs" :event SelectedHexChangedEvent :reader selectedHexChanges :reaction SwapContextPanel :values false}
    {:class HexInfoPanelHeaderSystem :file "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelHeaderSystem.cs" :event SelectedHexChangedEvent :reader selectedHexChanges :reaction FillHexHeader :values false}
    {:class ContextTabsAvailabilitySystem :file "Assets/Presentation/UI/MainHud/ContextTabs/Systems/ContextTabsAvailabilitySystem.cs" :event SelectedHexChangedEvent :reader selectedHexChanges :reaction ReconcileTabAvailability :values false}
    {:class HexInfoPanelResourcesSystem :file "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelResourcesSystem.cs" :event SelectedHexChangedEvent :reader selectedHexChanges :reaction FillResourcesBlock :values false}
    {:class ContextTabSelectionSystem :file "Assets/Presentation/UI/MainHud/ContextTabs/Systems/ContextTabSelectionSystem.cs" :event ContextTabChangedEvent :reader contextTabChanges :reaction ReconcileActiveTab :values false}
    {:class BuildDistrictActionSystem :file "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionSystem.cs" :event DistrictBuildConfirmedEvent :reader buildConfirmations :reaction BuildConfirmedDistrict :values true
     :also "два підняття в реакції → _storages.Events.Raise(new DistrictTableChangedEvent{Planned}) і, при TurnsToBuild == 0, Raise(new BuildDistrictCompleteEvent())"}
    {:class BuildDistrictActionCancelSystem :file "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionCancelSystem.cs" :event BuildDistrictCancelEvent :reader buildCancellations :reaction CancelDistrictBuild :values true
     :also "підняття DistrictTableChangedEvent{Removed} → _storages.Events.Raise"}
    {:class TurnCountSystem :file "Assets/Modules/Turn/Systems/TurnCountSystem.cs" :event TurnCompletedEvent :reader turnCompletions :reaction AdvanceTurnCount :values false}
    {:class HexIconsVisibilitySystem :file "Assets/Presentation/HexIcons/Systems/HexIconsVisibilitySystem.cs" :event HexIconsVisibilityChangedEvent :reader hexIconsVisibilityChanges :reaction RebuildHexIcons :values false}
    {:class DistrictBuildProgressViewSpawnSystem :file "Assets/Presentation/Districts/Systems/DistrictBuildProgressViewSpawnSystem.cs" :event DistrictTableChangedEvent :reader districtTableChanges :reaction SpawnMissingProgressViews :values false}
    {:class DistrictBuildProgressViewDespawnSystem :file "Assets/Presentation/Districts/Systems/DistrictBuildProgressViewDespawnSystem.cs" :event DistrictTableChangedEvent :reader districtTableChanges :reaction DespawnStaleProgressViews :values false}
    {:class DistrictViewSpawnSystem :file "Assets/Presentation/Districts/Systems/DistrictViewSpawnSystem.cs" :event DistrictTableChangedEvent :reader districtTableChanges :reaction SpawnMissingDistrictViews :values false}
    {:class DistrictOpenConditionEvaluatorTableChangedSystem :file "Assets/Domains/Economy/DistrictOpenCondition/Systems/DistrictOpenConditionEvaluatorTableChangedSystem.cs" :event DistrictTableChangedEvent :reader districtTableChanges :reaction EvaluateOpenConditions :values false
     :also "реєстрація Scoped лишається; параметр IReadOnlyList subSystems лишається"}
    {:class ForestSpawnSystem :file "Assets/Presentation/HexResources/Systems/ForestSpawnSystem.cs" :event ForestHexAppearedEvent :reader forestHexAppearances :reaction SpawnMissingForests :values false}
    {:class ForestDespawnSystem :file "Assets/Presentation/HexResources/Systems/ForestDespawnSystem.cs" :event ForestHexRemovedEvent :reader forestHexRemovals :reaction DespawnStaleForests :values false}]})
```

```clojure
(def EventConsumer-any-of-three  ;; HexInfoPanelDistrictSystem — Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelDistrictSystem.cs
  {:class-shape "sealed class : IUpdatedSystem, IDisposable; [ViewSubscriber(typeof(HexInfoPanelView))] лишається; конструктор (AppState appState, EntityStorages storages, EventReader<SelectedHexChangedEvent> selectedHexChanges, EventReader<TurnCompletedEvent> turnCompletions, EventReader<DistrictTableChangedEvent> districtTableChanges); запит AnyComponents зникає"
   :methods
   {Update {:does "звірити блок району на кожну видану подію трьох типів"
            :in #{:reader}
            :out :none
            :flow (-> (:step-1 (cond (:flow-1 "поки _selectedHexChanges.TryRead(out _)") (:conclusion-1 "звірити — і назад")) {:calls ReconcileDistrictBlock :out :none})
                      (:step-2 (cond (:flow-1 "поки _turnCompletions.TryRead(out _)") (:conclusion-1 "звірити — і назад")) {:calls ReconcileDistrictBlock :out :none})
                      (:step-3 (cond (:flow-1 "поки _districtTableChanges.TryRead(out _)") (:conclusion-1 "звірити — і назад")) {:calls ReconcileDistrictBlock :out :none}))
            :note "три цикли над трьома різними читачами; кожен — один рядок while з одним викликом, нижче планки примітиву (механіка від двох рядків); тихі виходи — усередині ReconcileDistrictBlock"}
    ReconcileDistrictBlock {:does "колишнє тіло Update(state, entity) без IsRipe" :out :none}
    OnCancelled {:does "як було" :note "_storages.World.CreateEvent → _storages.Events.Raise(new BuildDistrictCancelEvent { Coords = coords })"}}})

(def EventConsumer-batch  ;; п'ять споживачів «раз на пачку» (рішення :batch-reaction)
  [{:class TurnProcessorSystem :file "Assets/Modules/Turn/Systems/TurnProcessorSystem.cs"
    :shape "[SystemRole(PerFrame)] лишається (:per-frame-readers); поле _nextTurnPulses → private readonly EventReader<NextTurnEvent> _nextTurnRequests; конструктор + параметр EventReader<NextTurnEvent> nextTurnRequests; HasRipePulse прибрано"
    :flow (-> (:step-1 "var turnRequested = _nextTurnRequests.DrainBatch() — у БУДЬ-ЯКОМУ статусі, першим рядком" {:calls EventReader.DrainBatch :out :consumer-batch})
              (:step-2 (cond (:flow-1 "статус Idle") (:conclusion-1 "!turnRequested → return; інакше Running, RunTurnAsync, return")
                             (:flow-2 "статус Completed") (:conclusion-2 "Idle і _storages.Events.Raise(new TurnCompletedEvent())"))))
    :note "запит, прочитаний у тік Completed, губиться, як сьогодні (H1); кілька кліків — один хід (H2)"}
   {:class BuildDistrictCompletionSystem :file "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictCompletionSystem.cs"
    :shape "[SystemRole(Reactive)] прибрано — роль вирішує читач; _completePulses → private readonly EventReader<BuildDistrictCompleteEvent> _buildCompletions; + параметр конструктора"
    :flow (-> (:step-1 "!_buildCompletions.DrainBatch() → return" {:calls EventReader.DrainBatch :out :consumer-batch})
              (:step-2 "reconcile над усім in-progress як було; RaiseTableChanged → _storages.Events.Raise(new DistrictTableChangedEvent{Built})"))
    :comments "summary «DOORBELL … producer re-raises EVERY turn (level-triggered) … EventCleanup frame window» переписується: виробник піднімає раз; втрата лише витісненням, ризик прийнято (:completion-eviction-risk)"}
   {:class DistrictBuildUISystem :file "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildUISystem.cs"
    :shape "UpdatedSystem з табличним якорем на view-singleton і [SystemRole(PerFrame)] лишаються; _requestedSet → private readonly EventReader<DistrictBuildUIRequestedEvent> _overlayRequests; HasRipeRequest прибрано"
    :flow (-> (:step-1 "var overlayRequested = _overlayRequests.DrainBatch() — до тихого виходу" {:calls EventReader.DrainBatch :out :consumer-batch})
              (:step-2 "view == null → return")
              (:step-3 "HookChrome(view)")
              (:step-4 "overlayRequested → Open(view)"))
    :also "OnConfirmed: _storages.World.CreateEvent → _storages.Events.Raise(new DistrictBuildConfirmedEvent { … })"
    :note "Update(state, entity) викликається лише за наявності рядка якоря (contra :c-6)"}
   {:class DistrictBuildListUISubSystem :file "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildListUISubSystem.cs"
    :shape "_requestedSet → private readonly EventReader<DistrictBuildUIRequestedEvent> _overlayRequests — власний читач (:orchestrator/own-reader); конструктор (EntityStorages storages, EventReader<DistrictBuildUIRequestedEvent> overlayRequests) : base(storages.World); HasRipeRequest прибрано"
    :flow (-> (:step-1 "у Update(token), на місці HasRipeRequest(): _overlayRequests.DrainBatch() → дефолтний вибір" {:calls EventReader.DrainBatch :out :consumer-batch}))
    :note "Repopulate з кліку рядка знаходить читач уже вичерпаним — вибір не скидається (H4)"}
   {:class MainMenuState :file "Assets/Modules/Boot/Implementation/States/MainMenuState.cs"
    :shape "_generateRequests: Archetype → private readonly EventReader<TerrainGenerationGenerateEventComponent>; конструктор (IReadOnlyList<IAppStateSystem> allSystems, EventReader<TerrainGenerationGenerateEventComponent> generateRequests); EntityStorages і using Friflo прибрано; HasRipeRequest прибрано; RequestMapCreationOnRipeRequest → RequestMapCreationOnGenerateRequest"
    :flow (-> (:step-1 "RequestMapCreationOnGenerateRequest: _generateRequests.DrainBatch() → _requestedMode = MapCreation" {:calls EventReader.DrainBatch :out :consumer-batch})
              (:step-2 "_systems.Tick(state)"))
    :note "коментар «cleaned up by EventCleanupSystem once MapCreation starts ticking» прибрано; що RequestedMode ніхто не читає — паузована задача AppStateRunner"}])
```

```clojure
(def EventProducers  ;; кожен сайт CreateEvent → EntityStorages.Events.Raise; споживачі-виробники з таблиць вище не повторюються
  [{:class HexSelectionSystem :file "Assets/Modules/UserInput/Systems/HexSelectionSystem.cs" :site "RaiseSelectionChanged" :after "_storages.Events.Raise(new SelectedHexChangedEvent())" :comments "«One-frame pulse» → «event»"}
   {:class BuildDistrictTurnTickSystem :file "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictTurnTickSystem.cs" :site "Tick"
    :after "змінна raiseEvent → anyBuildFinishedThisTurn; умова turnsLeft <= 0 → turnsLeft == 0 (лічильник цього ходу вперше дійшов до нуля: до декременту 1, після 0); після циклу — _storages.Events.Raise(new BuildDistrictCompleteEvent())"
    :comments "коментар класу «LEVEL-TRIGGERED BY DESIGN … never optimize into raise-once» переписується: піднімає раз на хід, коли лічильник уперше дійшов до нуля; BuildDistrictCompletionSystem звіряється зі станом (рішення :lossy-producer)"
    :numbers {:finished-this-turn "TurnsLeft після декременту == 0"}}
   {:class GameplayState :file "Assets/Modules/Boot/Implementation/States/GameplayState.cs" :site "SeedStartOfPlay" :after "_storages.Events.Raise(new HexIconsVisibilityChangedEvent())" :comments "«one-frame event» → «event»"}
   {:class HexesUI :file "Assets/Presentation/UI/GeneratorMenu/Views/HexesUI.cs" :site "GenerateHexes" :after "_storages.Events.Raise(new TerrainGenerationGenerateEventComponent())" :note "відхилення :view/boundary лишається (:views-raise-events-kept)"}
   {:class ContextTabsView :file "Assets/Presentation/UI/MainHud/ContextTabs/Views/ContextTabsView.cs" :site "Emit" :after "_storages.Events.Raise(new ContextTabChangedEvent())" :note "те саме"}
   {:class TurnPanelView :file "Assets/Presentation/UI/MainHud/TurnPanel/Views/TurnPanelView.cs" :site "OnClicked" :after "_storages.Events.Raise(new NextTurnEvent())" :note "те саме"}
   {:class HexInfoPanelView :file "Assets/Presentation/UI/MainHud/HexInfoPanel/Views/HexInfoPanelView.cs" :site "OnBuildClicked" :after "_storages.Events.Raise(new DistrictBuildUIRequestedEvent())" :comments "«payload-less one-frame request … No consumer yet» → подія журналу, споживачі DistrictBuildUISystem і DistrictBuildListUISubSystem"}])

(def EventTypes  ;; 13 оголошених типів: IComponent → IEventTag; коментарі «one-frame», «EventTag», «Cleared by EventCleanupSystem» переписуються
  [{:type SelectedHexChangedEvent :file "Assets/Presentation/Terrain/Events/SelectedHexChangedEvent.cs"}
   {:type HexIconsVisibilityChangedEvent :file "Assets/Presentation/HexIcons/Events/HexIconsVisibilityChangedEvent.cs"}
   {:type ForestHexAppearedEvent :file "Assets/Presentation/HexResources/Events/ForestHexAppearedEvent.cs"}
   {:type ForestHexRemovedEvent :file "Assets/Presentation/HexResources/Events/ForestHexRemovedEvent.cs"}
   {:type ContextTabChangedEvent :file "Assets/Presentation/UI/MainHud/ContextTabs/Events/ContextTabChangedEvent.cs"}
   {:type TurnCompletedEvent :file "Assets/Modules/Turn/Events/TurnCompletedEvent.cs"}
   {:type NextTurnEvent :file "Assets/Modules/Turn/Events/NextTurnEvent.cs"}
   {:type DistrictBuildUIRequestedEvent :file "Assets/Flows/DistrictBuild/Events/DistrictBuildUIRequestedEvent.cs" :note "потребує Ecs.Extensions у Flows.DistrictBuild.asmdef"}
   {:type DistrictBuildConfirmedEvent :file "Assets/Domains/Actions/BuildDistrictAction/Events/DistrictBuildConfirmedEvent.cs"}
   {:type BuildDistrictCancelEvent :file "Assets/Domains/Actions/BuildDistrictAction/Events/BuildDistrictCancelEvent.cs"}
   {:type BuildDistrictCompleteEvent :file "Assets/Domains/Actions/BuildDistrictAction/Events/BuildDistrictCompleteEvent.cs" :note "internal — contra :c-1"}
   {:type DistrictTableChangedEvent :file "Assets/Domains/Economy/District/Events/DistrictTableChangedEvent.cs" :note "IEquatable<DistrictTableChangedEvent>, IEventTag; згадка AsMultiMap-фільтра в коментарі прибирається — фільтра немає"}
   {:type TerrainGenerationGenerateEventComponent :file "Assets/Domains/Map/Generation/Components/TerrainGenerationGenerateEventComponent.cs" :note "легасі-суфікс і тека Components/ лишаються (базове попередження event/declare)"}])
```

```clojure
(def AppStateRequest  ;; родина :app-state-request
  {Boot
   {:file "Assets/Modules/Boot/Implementation/Boot.cs"
    :fields "private GameModeMachine _machine; private EventReader<AppStateRequestedEvent> _appStateRequests; поля _entityStorages і _pendingMode прибрано"
    :methods
    {Construct {:does "прийняти машину і читач запитів стану"
                :out :none
                :how "[Inject] public void Construct(GameModeMachine machine, EventReader<AppStateRequestedEvent> appStateRequests) — сутності в World і підписки OnComponentChanged нема"}
     Update    {:does "тікнути стан, потім застосувати останній запит стану цього тіку"
                :in #{:reader}
                :out :none
                :flow (-> (:step-1 "тік поточного стану" {:calls GameModeMachine.Tick :out :none})
                          (:step-2 "останній запит пачки" {:calls TryReadLastRequestedMode :out :pending-mode})
                          (:step-3 "перемкнути, коли запит ≠ поточному" {:calls GameModeMachine.Switch :out :none}))
                :exits #{"TryReadLastRequestedMode = false — запиту нема; стоїть між step-2 і step-3; occasion: кожен тік, крім тіків Initialization, де InitializationSystem піднімає запит"
                         "requestedMode == _machine.CurrentMode — стоїть перед step-3; occasion: запит на поточний режим, як сьогодні"}
                :note "перемикання завжди після тіку, у тому ж кадрі (CONTEXT # Facts: Boot.Update)"}
     TryReadLastRequestedMode {:does "вичерпати читач запитів стану; останній запит пачки перемагає"
                               :in #{:reader}
                               :out :pending-mode
                               :scratch #{:app-state-request}
                               :flow (-> (:step-1 "requestedMode = default; found = false")
                                         (:step-2 (cond (:flow-1 "поки _appStateRequests.TryRead(out var request)") (:conclusion-1 "requestedMode = request.Requested; found = true; Debug.Log як сьогодні — і назад")
                                                        (:flow-2 "вичерпано") (:conclusion-2 "return found"))))
                               :note "private bool TryReadLastRequestedMode(out AppState requestedMode) (:method/try)"}
     OnDestroy {:does "зупинити машину" :out :none :how "_machine?.Stop() — перевірка сутності запиту й відписка зникають"}}}

   InitializationSystem
   {:file "Assets/Modules/Boot/Implementation/Systems/InitializationSystem.cs"
    :shape "sealed class : IUpdatedSystem; public AppState AppState { get; }; public int Priority => SystemPriorities.RuntimeTick.Initialization; конструктор (AppState appState, EntityStorages storages); private readonly EntityStorages _storages"
    :methods {Update {:does "попросити режим ConfigLoading"
                      :out :none
                      :how "_storages.Events.Raise(new AppStateRequestedEvent { Requested = AppState.ConfigLoading })"
                      :note "щотіку, поки стан Initialization; раз — бо Boot перемикає одразу після тіку (:initialization-request-kept); форма без робочої таблиці — contra :c-4"}}}

   AppStateRequestedEvent {:file "Assets/Modules/Boot/Implementation/Events/AppStateRequestedEvent.cs" :fields {Requested :pending-mode}
                           :comment "«the mode someone asks the app to switch to; a request only — the current mode is known by the machine alone» — перенесено з AppStateComponent"}})
```

```clojure
(def EventLog-wiring  ;; DI, AOT, реєстр
  {EntityStorages {:constructor "World = new EntityStore(); Singletons = new SingletonComponents(singletonArchetype); Events = new EventLog()"}
   WorldInstaller {:configure (-> (:step-1 "entityStorages і RegisterInstance як були")
                                  (:step-2 "builder.Register(typeof(EventReader<>), Lifetime.Transient) — одна відкрита реєстрація, коментар: кожна інʼєкція — новий читач з власним курсором; закриті типи для IL2CPP — EventReaderAotDeclarations")
                                  (:step-3 "RegisterAppStateSystem<EventCleanupSystem>(… усі 8 прапорців) — removed; решта як була"))}
   EventReaderAotDeclarations
   {:methods {DeclareClosedReaders
              {:does "статично згадати закритий EventReader<TEvent> кожного типу події, щоб IL2CPP згенерував його код"
               :in #{:storages}
               :out :none
               :scratch #{:aot-declaration}
               :flow (-> (:step-1 "на кожен із 14 типів: var r = new EventReader<X>(storages); r.TryRead(out _); r.DrainBatch()")
                         (:step-2 "throw new InvalidOperationException(\"AOT declaration only — never called\")"))
               :note "[Preserve] на класі й методі; кидок наприкінці, не на початку — інакше компілятор відкидає недосяжний код; новий тип події додається сюди (правило :event/aot-reader); ніхто не кличе"}}
    :types [SelectedHexChangedEvent HexIconsVisibilityChangedEvent ForestHexAppearedEvent ForestHexRemovedEvent ContextTabChangedEvent TurnCompletedEvent NextTurnEvent DistrictBuildUIRequestedEvent DistrictBuildConfirmedEvent BuildDistrictCancelEvent BuildDistrictCompleteEvent DistrictTableChangedEvent TerrainGenerationGenerateEventComponent AppStateRequestedEvent]}})
```

```clojure
(def EventLog-rule-edits  ;; CLAUDE.md § 6: RULES_SPECIFICATION.md першим → carriers → перевірки → fantasymayor-rules-conformance
  {:specification
   {:file "RULES_SPECIFICATION.md"
    :id-law {:retired-ids [:event/ripe :event/anchor :event/no-same-frame :event/cleanup :event/lossy-producer :orchestrator/ripe-once]}
    :id-prefixes {:event "подія журналу"}
    :change
    {:event/is "подія — сутність у сховищі Events рівно з одним компонентом, тип якого реалізує IEventTag; живе до витіснення новішою понад ліміт свого типу; її поля — звичайні дані"
     :event/declare "подія оголошується struct з IEventTag, з суфіксом події, у папці Events/ своєї фічі"
     :event/raise "подія піднімається одним викликом EntityStorages.Events.Raise(new TEvent { … }); AddComponent типу події деінде — порушення"
     :event/reaction "реакція — діяти на значеннях або reconcile; і на кожну видану подію, або раз на вичерпану пачку"
     :event/startup-bulk "стартова масова робота — стадія конвеєра, ніколи подія: подія не несе порядку роботи й не гарантує, що її прочитають до витіснення"
     :event/one-way "ніколи петля заповнення - команда - заповнення на подіях журналу; споживач ніколи не піднімає тип, який сам читає в тому ж циклі (contra :c-9)"
     :event/no-tag-on-persistent-row "компонент події — тип з IEventTag — ніколи не стоїть на сутності World чи Singletons"
     :tag/event-tag "подія не несе тегу: її дискримінатор — тип єдиного компонента з IEventTag у сховищі Events"
     :tag/main-tag-unique "один головний тег називає один архетип — спільний головний тег двох архетипів заборонений, без винятків"
     :table/cross-archetype "запит через кілька архетипів — лише коли фільтр справді охоплює кілька архетипів і погоджено з власником; винятку для подій нема"
     :system/definition "перелік без EventCleanupSystem"
     :system/cadence "повторна — per-frame, reactive на події свого читача, фаза ходу"
     :system/base-choice "reactive-споживач події — IUpdatedSystem напряму з readonly EventReader<TEvent>; UpdatedSystem лишається для per-frame з робочою таблицею чи якорем тіку"
     :system/role-order "Update-цикл і поле EventReader → reactive, або per_frame під маркером PerFrame; Update-цикл → per_frame; стадія → pipeline_stage; TurnPhaseSubSystem → turn_phase; негенеричний IUniTaskSystem → startup_step; абстрактний предок, якого збирають → sub_system; інакше ролі нема; не-система з EventReader — «споживач події» без ролі"
     :system/marker-required "маркер PerFrame обовʼязковий на класі циклу, що тримає EventReader, але тікає per-frame; без маркера такий клас reactive — перевіряє агент"
     :system/marker-forbidden "маркер ролі на класі без поля EventReader заборонений"
     :system/marker-value "PerFrame вимагає контракту циклу плюс поле EventReader; Reactive законної форми не має — значення прибрано з перелічення"
     :reactive/shape "sealed IUpdatedSystem: залежності в конструкторі, readonly EventReader<TEvent> з DI, Update — while (reader.TryRead(out var evt)) реакція; тихий вихід — усередині реакції або після вичерпання; сторожі, що кидають; дія на різниці"
     :orchestrator/empty-family "оболонка з читачем або якорем, без інжекції списку"
     :structural/update-is-safe "структурні зміни безпечні в тілі Update бази, що зняла знімок якоря, і в реакції на подію з читача — читач не перелічує сутностей; у переліченні, яке система відкриває сама, заборонені"
     :name/suffix "…Event — подія журналу"
     :choose/behavior-recipe "гілка «прибирає події — cleanup» прибрана"
     :state/is-not-state "до «store handles» додано readonly EventReader<TEvent> — курсор живе в EventLog"}
    :born
    {:event/log-capacity {:says "ліміт кільця — на типі події: [EventCapacity(n)] на struct, інакше 128; нова подія понад ліміт витісняє найстарішу; переповнення не кидає" :checked-by :agent}
     :event/sequence {:says "кожна подія дістає глобальний монотонний номер між усіма типами" :checked-by :agent}
     :event/reader {:says "споживач тримає readonly EventReader<TEvent>, народжений DI як Transient, і читає while TryRead; курсор веде журнал; видача = прочитано" :checked-by :graph}
     :event/read-position {:says "новий курсор читає з найстарішої присутньої події; витиснене зміщення продовжує з найстарішої, що лишилась" :checked-by :agent}
     :event/delivery {:says "читач після виробника бачить подію в тому ж тіку, перед ним — у наступному" :checked-by :agent}
     :event/drain-before-exit {:says "тихий вихід споживача стоїть після вичерпання читача або всередині реакції на подію" :checked-by :agent}
     :event/non-system-reader {:says "EventReader дозволений стану, підсистемі й Boot; у графі — ребро consumes без ролі системи" :checked-by :graph}
     :orchestrator/own-reader {:says "підсистема, якій потрібна подія, тримає власний читач; оркестратор нічого їй не пересилає" :checked-by :agent}
     :event/clear-all ^:new {:says "EventLog.ClearAllEvents видаляє всі події всіх кілець; курсори продовжують із порожнього журналу; хто кличе — рішення окремої задачі" :checked-by :agent}
     :event/aot-reader ^:new {:says "кожен тип події має закритий EventReader<TEvent> в EventReaderAotDeclarations — інакше IL2CPP-плеєр не народить читача" :checked-by :agent}}
    :constructs
    {:storages "реєстр: World, Singletons, Events (EventLog), конфіги"
     :event "сутність у Events з одним компонентом типу з IEventTag"
     :event-archetype :removed
     :event-log {:new true :type EventLog :holds "сховище Events, кільце на тип, курсори, глобальний номер"}
     :event-reader {:new true :type EventReader<TEvent> :holds "хендл курсора: TryRead, DrainBatch"}
     :cleanup :removed
     :system "перелік без EventCleanupSystem"
     :system-role "ролі без cleanup; доказ reactive — поле EventReader у класі циклу; маркер знає лише PerFrame"
     :anchor "якір лише табличний у base(...) UpdatedSystem чи LateUpdatedSystem; подієвого якоря нема"
     :cadence "без EventCleanupSystem"
     :priority "без EventCleanup"
     :graph-edge "emits за Events.Raise; reacts_to — reactive-читач; polls — per_frame-читач; consumes — не-система з читачем"
     :structural-change "без примітки про AddComponent після CreateEvent"
     :marker-vocabulary "SystemRoleAttribute, ViewSubscriberAttribute, TagLabelAttribute, IUpdatedSystem, ILateUpdatedSystem, EventReader`1, Friflo ITag, UnityEngine MonoBehaviour"}}

   :carriers
   {"ARCHITECTURE.md" {:ask-first "graph-gate — правка лише з дозволу власника"
                       :edits ["Stack: реєстр — World, Singletons, Events"
                               "Systems: :system/definition, :system/cadence, :system/base-choice, :system/role-order — як у специфікації"
                               "Markers: :system/marker-required, :system/marker-forbidden, :system/marker-value"
                               "System state: :state/is-not-state + readonly EventReader"
                               "Runtime forms: :reactive/shape, :orchestrator/empty-family, :structural/update-is-safe; :orchestrator/ripe-once removed; + :orchestrator/own-reader"
                               "Entities: :table/cross-archetype, :tag/main-tag-unique, :tag/event-tag"
                               "Events: заголовний коментар def — EventLog + EventReader; changed :event/is :event/declare :event/raise :event/reaction :event/startup-bulk :event/one-way :event/no-tag-on-persistent-row; removed :event/ripe :event/anchor :event/no-same-frame :event/cleanup :event/lossy-producer; + 9 born event-правил"
                               "Naming: :name/suffix event"]}
    "Patterns/PATTERN_EVENT.md" ["frontmatter trigger «before creating an ECS event»; заголовок «Event (log)»; related без PATTERN_CLEANUP_SYSTEM" "скелет: struct : IEventTag; [EventCapacity(n)] необовʼязковий; рядок у EventReaderAotDeclarations" "підняття: _storages.Events.Raise(new [Name]Event { … })" "rules: :visibility → delivery за тіком; :latency, :lossy-producer removed; + :capacity :aot-reader :clear-all"]
    "Patterns/PATTERN_REACTIVE_SYSTEM.md" ["скелет: sealed : IUpdatedSystem, readonly EventReader<[Name]Event>, Update — while TryRead → реакція" "варіант «раз на пачку»: if (!_reader.DrainBatch()) return; reconcile" "секція маркера: маркера нема; PerFrame лише на per-frame класі з читачем (PATTERN_PERFRAME_SYSTEM)" "rules: :driven-by EventReader, :ripe-gate removed, :structural-in-update — читач не перелічує"]
    "Patterns/PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM.md" ["скелет оркестратора на читачі без base(...)" "Reactive-маркер removed" "ripe-gate removed; підсистема з власним читачем (:orchestrator/own-reader)"]
    "Patterns/PATTERN_PERFRAME_SYSTEM.md" ["варіант з маркером: [SystemRole(PerFrame)] на класі циклу, що тримає EventReader і тікає щокадру" "rules :role-marker — те саме"]
    "Patterns/PATTERN_TRANSACTION_ENTITY.md" ["рядки 60-100: IsRipe removed, читач; CreateEvent → Events.Raise"]
    "Patterns/PATTERN_COMPONENT.md" ["рядок 70: «one-frame pulse» → «log event»"]
    "Patterns/PATTERN_TAG.md" ["рядок 35: «EventTag is the main tag of every event» removed — подія тегу не несе"]
    "Patterns/PATTERN_CLEANUP_SYSTEM.md" ["removed via vault_delete (рішення :rule-ids-and-cleanup-recipe)"]
    ".claude/skills/fantasymayor-pattern-choice/SKILL.md" ["гілка (cleans-up-events?) PATTERN_CLEANUP_SYSTEM removed" "def role-marker: :shape, :required, :forbidden, :value — за новими маркерними правилами" "def event-kernel: raising → EventLog / Events.Raise; consuming → EventReader, while TryRead або DrainBatch" "systems roles без cleanup; :marks без Reactive"]
    ".claude/skills/fantasymayor-graph/SKILL.md" ["опис: EventArchetypes.Of anchors → EventReader fields і Events.Raise" "дерево ролей і markers — як :system/role-order" "systems --role без cleanup і undecided" "ребра reacts_to, polls, consumes; facet event_readers замість base_anchor/anchor_events/held_events"]
    ".claude/skills/fantasymayor-graph/references/graph-facts.md" ["kind event: struct з IEventTag або суфіксом" "archetype без EventArchetypes.Of" "facets, edges emits/reacts_to/polls/consumes/disposes" "таблиця правил: tag/main-tag-unique, tag/event-tag, event/raise, event/no-tag-on-persistent-row оновлено; event/cleanup removed; system/marker-* за новим текстом"]
    ".claude/skills/fantasymayor-graph/references/recipe-signatures.md" ["PATTERN_REACTIVE_SYSTEM — role reactive (reader | marker? ні: reader)" "PATTERN_PERFRAME_SYSTEM — deviations без undecided" "рядок PATTERN_CLEANUP_SYSTEM removed"]
    ".claude/skills/fantasymayor-placement/SKILL.md" ["Events/ :contains «event types — structs implementing IEventTag»" ":root-first без «event cleanup»; + відкрита реєстрація EventReader<> і EventReaderAotDeclarations у корені"]
    "DOC_STANDARD.md" ["рядок 85: приклад якоря EventCleanupSystem removed → живий якір EventLog"]
    "INDEX.md" ["генерована зона — python3 Tools/gen_index.py після змін frontmatter рецептів" "агентна зона рядок 85 «per-frame / reactive / cleanup» — лише з дозволу власника (CLAUDE.md § 3 :ask-first)"]}})
```

```clojure
(def EventLog-rule-edits-from-code  ;; слайс :rules, знахідки поза буквальним переліком s2 — виправлені в межах узгодженості цього ж переліку
  [{:from-code "RULES_SPECIFICATION.md :recipe/signatures :signatures :cleanup видалено (рецепт видалено, гілка cleanup прибрана з :choose/behavior-recipe), :reactive переписано на «поле EventReader без маркера PerFrame» — s2 не називав цей запис явно, але :choose/behavior-recipe і видалення PATTERN_CLEANUP_SYSTEM.md роблять старий :cleanup запис привидом і :reactive запис хибним"}
   {:from-code ".claude/skills/fantasymayor-pattern-choice/SKILL.md def system-bases: додано гілку (reacts-to-an-event?) → IUpdatedSystem напряму — без неї дерево вело б reactive-споживача на UpdatedSystem, суперечачи новому :system/base-choice"}
   {:from-code "Flows/EVENT_LOG/CASCADE.md і CONTEXT.md не мали frontmatter (category/read/status) — python3 Tools/gen_index.py падав; додано мінімальний frontmatter за зразком архівних CASCADE/CONTEXT, без body-правок"}])
```

```clojure
(def EventLog-check-edits  ;; CLAUDE.md § 6 step-3 — повідомлення цитують id
  {"fmgraph type_facts.py" ["COMPONENT_CONTRACTS += IEventTag" "kind event: IEventTag у базах АБО суфікс …Event / …EventComponent"]
   "fmgraph source_reading.py" ["сайти EventArchetypes.Of, AnyComponents-подій і CreateEvent removed" "+ сайт raise_event: метод Raise на receiver, що закінчується на Events, тип — з new у аргументі" "read_registration: Register(typeof(X<>), Lifetime.L) — сайт register_open_generic замість unknown_registration"]
   "fmgraph ecs_facts.py" ["EVENT_FRAME, EVENT_TAG removed" "bind_owner і any_components без подієвих гілок — будь-яка привʼязка архетипу = reads" "connect_component_access: emits за raise_event via Events.Raise; попередження event/raise — AddComponent типу kind event поза класом EventLog" "attribute_disposals і audit_births: сайти в класі EventLog пропускаються — витіснення і народження подій не рядки таблиць"]
   "fmgraph roles.py" ["RoleEvidence: sweeps_events, anchored_on_event, table_anchored, holds_event_archetype removed; + event_readers — аргументи полів типу EventReader з sources.declarations fields" "decide_role: loop і читач → маркер PerFrame → per_frame decided_by marker, інакше reactive decided_by reader; loop → per_frame base; далі як було" "попередження: маркер на класі без читача → system/marker-forbidden; гілки undecided і marker-required removed" "connect_event_edges: reacts_to (reactive), polls (per_frame під маркером), consumes (клас без ролі системи або sub_system) via EventReader" "audit_cleanup removed; INT_MAX removed"]
   "fmgraph tag_law.py" ["перевірки EventTag без EventFrameComponent і event-without-event-tag removed" "виняток EVENT_TAG у shared-main-tag і фільтрах removed" "+ event/no-tag-on-persistent-row: тип kind event у компонентах будь-якого оголошеного архетипу"]
   "fmgraph di_facts.py" ["register_open_generic → ребро registers з args ('<>',) і lifetime; injects EventReader з args лишаються як були" ":note RegisterAppStateSystem<T> сьогодні не читається як реєстрація — тому роль читача береться з поля, не з injects"]
   "fmgraph recipes.py" ["PATTERN_CLEANUP_SYSTEM signature removed" "PATTERN_REACTIVE_SYSTEM decided_by «reader | marker» → «reader»" "PATTERN_PERFRAME_SYSTEM без deviations undecided" "view-boundary: create_event → raise_event"]
   "fmgraph queries.py" ["facets explain: event_readers замість base_anchor, anchor_events, held_events"]
   "Tools/MarkerShapeAnalyzer/TypeShape.cs" ["EventAnchored, TableAnchored, HeldEventArchetypes, Cleanup removed; + HoldsEventReader"]
   "Tools/MarkerShapeAnalyzer/MarkerVocabulary.cs" ["EventArchetypes, ComponentTypes, EventCleanupSystem, UpdatedSystemBase, LateUpdatedSystemBase removed; + EventReader = EcsExtensions.EventReader`1" "копія SystemRoleKind — лише PerFrame (:system/marker-vocabulary-parity)"]
   "Tools/MarkerShapeAnalyzer/MarkedTypeCheck.cs" ["SightBaseAnchor, IsEventArchetypeCall, SightHeldEventArchetype, BaseAnchor removed" "HoldsEventReader — будь-яке поле типу, чий OriginalDefinition = EventReader, читається з символу при SymbolStart" "MarkerDecidesRole = LoopContract && HoldsEventReader" "PerFrame: !LoopContract → FM1001; LoopContract && !HoldsEventReader → FM1006" "DecidedRole: «per-frame — runs the Update loop and holds no EventReader» | «no role at all»"]
   "Tools/MarkerShapeAnalyzer/MarkerShapeAnalyzer.cs" ["FM1002 і FM1005 retired — дескриптори removed з SupportedDiagnostics" "FM1001 текст: PerFrame demands the Update-loop contract plus an EventReader field — rule system/marker-value" "FM1006 текст: marker on a class holding no EventReader — rule system/marker-forbidden" "StartType: реєстрації BaseConstructorInitializer та InvocationExpression removed"]
   "Assets/Scripts/EcsExtensions/MarkerShapeAnalyzer.dll" ["перебудова Tools/MarkerShapeAnalyzer і копія dll — збірка аналізатора поза забороною"]
   "~/.claude/skills/arch-check/SKILL.md" ["визначення системи (рядок 70) і каденція (рядок 77) без EventCleanupSystem" "перелік not-state (рядок 131) + readonly EventReader<TEvent> — хендл, курсор в EventLog"]})
```

```clojure
(def EventLog-check-edits-from-code  ;; слайс :checks, знахідки поза буквальним переліком — виправлені в межах узгодженості цього ж переліку
  [{:from-code "queries.py імпортував INT_MAX з roles.py для показу Priority=int.MaxValue в systems; roles.py втратив INT_MAX разом з audit_cleanup — константу перенесено в queries.py напряму, інакше імпорт падає"}
   {:from-code "type_facts.py TYPE_FIELDS_OF_SITES отримав 'generic' поруч із 'type'/'types' — без цього register_open_generic (di_facts.py) носив би нерозв'язане ім'я типу замість id вузла графа"}
   {:from-code "ecs_facts.py expand_archetypes: сайт any_components (0 живих викликів AnyComponents у коді) спрощено до звичайних reads-ребер по кожному типу, замість event-специфічної інтерпретації — узгоджено з bind_owner, s2 не називав цей сайт окремо"}])
```

```clojure
{:spine-reads
 {EventLog.Raise "var ring = RingOf<TEvent>(); EvictOldestWhenFull(ring); var slot = BirthEventEntity(ring, evt); ring.Append(slot)"
  EventLog.TryRead "var ring = RingOf<TEvent>(); var offset = ClampedCursor(ring, cursorId); [exit: offset == ring.AppendedCount → false]; evt = DeliverEventAt<TEvent>(ring, cursorId, offset); return true"
  EventLog.ClearAllEvents "foreach ring ≠ null: while (!ring.IsEmpty) EvictOldest(ring)"
  EventLog.OpenCursor "RingOf<TEvent>(); _cursors.Add(0); return _cursors.Count - 1"
  EventReader.DrainBatch "var batchHeldEvents = false; while (TryRead(out _)) batchHeldEvents = true; return batchHeldEvents"
  Boot.Update "_machine.Tick(new GameState(Time.deltaTime)); [exit: !TryReadLastRequestedMode(out var requestedMode)]; [exit: requestedMode == _machine.CurrentMode]; _machine.Switch(requestedMode)"
  per-event-consumer.Update "while (_<reader>.TryRead(out var evt)) <Reaction>(evt)"
  InitializationSystem.Update "_storages.Events.Raise(new AppStateRequestedEvent { Requested = AppState.ConfigLoading })"}
 :steps-visible {Raise 4 TryRead 3 ClearAllEvents 1 OpenCursor 3 Boot.Update 3 per-event-consumer.Update 1 HexInfoPanelDistrictSystem.Update 3}
 :data-coverage {:undeclared 0
                 :orphan 0
                 :one-step-fields {:count 1 :entry :global-sequence :kept-because "власник EventLog живе весь процес: номер переживає виклик Raise, тож поле, а не локаль; пише один крок (BirthEventEntity), читає лише він — contra :c-5"}
                 :unborn 0   ;; п'ять паралельних масивів s1 народжені як EventRing, пара id + номер — як EventRingSlot
                 :s1-coverage {:complete true
                               :map {:events-store :events-store, :event-payload :event-payload, :sequence-stamp :ring-slot, :global-sequence :global-sequence, :tag-schema :event-type-schema, :capacity-by-tag #{:capacity :rings}, :ring-ids :ring-slots, :ring-head :ring-head, :ring-count :ring-count, :ring-appended :ring-appended, :cursor-rows :cursor-rows, :reader #{:reader :cursor-id}, :consumer-batch :consumer-batch, :app-state-request :app-state-request, :pending-mode :pending-mode, :aot-declaration :aot-declaration}
                               :retired-by-amendment #{:own-tag :kind-binding}}
                 :typed "complete — кожен запис несе :type або :shape"
                 :scratch-heavy 0}
 :how-semicolons 0
 :reader-clean true}
```

# Contra

```clojure
[{:id :c-1
  :kills "EventReaderAotDeclarations — рішення :aot-closed-generics «один клас»"
  :case "BuildDistrictCompleteEvent оголошений internal у Domains.Actions; клас у Installers.World не може написати EventReader<BuildDistrictCompleteEvent>; Installers.World ще й не посилається на Flows.DistrictBuild"
  :fails "один клас не бачить внутрішній тип іншої збірки — компіляція впаде"
  :fix [{:id :fix-a :confidence 60 :is "зробити BuildDistrictCompleteEvent public; додати Flows.DistrictBuild в Installers.World.asmdef" :cost "тип стає видимим поза Actions — інкапсуляція, яку сьогодні ніщо не використовує"}
        {:id :fix-b :confidence 25 :is "[assembly: InternalsVisibleTo(\"Installers.World\")] у Domains.Actions" :cost "новий файл AssemblyInfo і прихований звʼязок збірок"}
        {:id :fix-c :confidence 15 :is "друге оголошення AOT усередині Domains.Actions для його internal-подій" :cost "не «1 клас» — суперечить слову власника"}]}
 {:id :c-2
  :kills "RingOf і кожен читач — якщо схема Friflo не бачить IEventTag як компонент"
  :case "struct SelectedHexChangedEvent : IEventTag; схема будується скануванням збірок; чи зараховує вона struct, чий IComponent прийшов через похідний інтерфейс, у CONTEXT не перевірено"
  :fails "GetComponentType<TEvent>() дасть null або Archetype не складеться — жоден читач не народиться"
  :fix [{:id :fix-a :confidence 65 :is "лишити : IEventTag і сторож у RingOf, що кидає з іменем типу; перевіряє перший запуск власника в Unity" :cost "ризик виявиться лише в запуску, зате гучно"}
        {:id :fix-b :confidence 35 :is "кожна struct події пише обидва: : IComponent, IEventTag" :cost "подвійне оголошення на 14 типах; fmgraph і аналізатор мають приймати обидві форми"}]}
 {:id :c-3
  :kills "закон імен і тегів — читання IEventTag"
  :case ":name/suffix — «…Tag — безполевий маркер»; :tag/is — «тег — порожня struct з ITag»; IEventTag — інтерфейс компонента з даними"
  :fails "імʼя каже «тег», а тип — контракт компонента зі значеннями; агент, що читає закон суфіксів, помилиться; type_facts.check_struct_shape лишається осторонь лише тому, що це інтерфейс"
  :fix [{:id :fix-a :confidence 70 :is "лишити імʼя власника; у :name/suffix дописати виняток: IEventTag — контракт типу події, не тег" :cost "виняток у законі імен"}
        {:id :fix-b :confidence 30 :is "назвати IEventComponent" :cost "лише словом власника — він назвав «event tag»"}]}
 {:id :c-4
  :kills ":system/driven-by, :perframe/shape на InitializationSystem"
  :case "архетип запиту зникає з World; InitializationSystem лишається IUpdatedSystem без робочої таблиці й без якоря тіку"
  :fails "per-frame система без таблиці — форма, яку закон не називає; граф дасть per_frame за базою, правило — порушення"
  :fix [{:id :fix-a :confidence 65 :is "записати відхилення коментарем на класі (:deviation/in-code): виробник запиту стану без таблиці до завершення AppStateRunner" :cost "одне записане відхилення"}
        {:id :fix-b :confidence 20 :is "дописати в :system/driven-by виняток «виробник без таблиці»" :cost "розширює закон заради одного класу"}
        {:id :fix-c :confidence 15 :is "якір на архетипі PlayerInput з World" :cost "фальшивий якір — не предмет системи"}]}
 {:id :c-5
  :kills ":global-sequence — поле, яке пишуть і ніхто не читає"
  :case "EventRingSlot.Sequence заповнюється в BirthEventEntity; TryRead видає лише TEvent"
  :fails "мертві дані; one-step-field у лічильнику покриття"
  :fix [{:id :fix-a :confidence 70 :is "лишити — рішення :global-sequence («закладь … воно їсти не просить»)" :cost "8 байт на слот"}
        {:id :fix-b :confidence 30 :is "видати номер читачу перевантаженням TryRead(out TEvent, out long sequence)" :cost "API без споживача — вигадка"}]}
 {:id :c-6
  :kills "DistrictBuildUISystem — вичерпання до тихого виходу"
  :case "Update(state, entity) тікає лише за наявності рядка DistrictBuildUI; поки рядка нема, запити DistrictBuildUIRequestedEvent лишаються непрочитаними"
  :fails "якщо рядок зникне або зʼявиться пізніше за перший клік, старий запит відкриє оверлей пізніше"
  :fix [{:id :fix-a :confidence 75 :is "прийняти: рядок народжує стадія DistrictBuildUiSpawn (810) у MapCreation, до Gameplay; кнопка «Будувати» є лише в Gameplay" :cost "нуль"}
        {:id :fix-b :confidence 25 :is "перевести клас на IUpdatedSystem з TryGetFirst на рядку" :cost "змінює per-frame форму, яку власник лишив «поки що»"}]}
 {:id :c-7
  :kills "правдивість коментарів у Boot поза запитом стану"
  :case "InitializationState: «today only cleanup»; MapCreationState: SettleFrames «let event cleanup drain», summary «ticks its per-frame systems (event cleanup)»"
  :fails "після зникнення EventCleanupSystem коментарі брешуть (:comment/on-the-thing), а Boot поза запитом — межа паузованої задачі"
  :fix [{:id :fix-a :confidence 70 :is "переписати лише коментарі; значення SettleFrames не чіпати (H7 — предмет AppStateRunner)" :cost "два файли стану в дифі"}
        {:id :fix-b :confidence 30 :is "лишити до AppStateRunner" :cost "два хибні коментарі в коді"}]}
 {:id :c-8
  :kills ":consumer-shape «одна форма: while (reader.TryRead(out var evt))»"
  :case "DrainBatch — метод, народжений у s2 для п'яти споживачів «раз на пачку»"
  :fails "друга форма читання на EventReader; власник міг мати на увазі лише цикл у кожному класі"
  :fix [{:id :fix-a :confidence 70 :is "DrainBatch на читачі — примітив п'яти повторів (rule primitive), форма одна: той самий курсор, та сама видача" :cost "один метод"}
        {:id :fix-b :confidence 30 :is "цикл while TryRead з локальним bool у кожному з п'яти класів" :cost "п'ять повторів механіки"}]}
 {:id :c-9
  :kills "Update per-event-споживача — завершення циклу"
  :case "реакція піднімає подію того самого типу, який читає: while (TryRead) бачить щойно підняту подію в тому ж циклі (:same-tick-delivery)"
  :fails "нескінченний цикл у тіку; сьогодні такого споживача нема — BuildDistrictActionSystem читає DistrictBuildConfirmedEvent, а піднімає DistrictTableChangedEvent і BuildDistrictCompleteEvent"
  :fix [{:id :fix-a :confidence 60 :is "дописати в :event/one-way: споживач не піднімає тип, який сам читає" :cost "рядок правила, перевіряє агент"}
        {:id :fix-b :confidence 40 :is "нічого — латентно" :cost "перший такий споживач зависне"}]}]
```

# Slices

```clojure
[{:slice :kernel
  :writes #{"Assets/Scripts/EcsExtensions/IEventTag.cs" "Assets/Scripts/EcsExtensions/EventCapacityAttribute.cs" "Assets/Scripts/EcsExtensions/EventLog.cs" "Assets/Scripts/EcsExtensions/EventRing.cs" "Assets/Scripts/EcsExtensions/EventRingSlot.cs" "Assets/Scripts/EcsExtensions/EventReader.cs" "Assets/Scripts/EcsExtensions/EntityStorages.cs" "Assets/Scripts/EcsExtensions/SystemPriorities.cs" "Assets/Scripts/EcsExtensions/SystemRoleKind.cs" "Assets/Scripts/EcsExtensions/SystemRoleAttribute.cs" "removed kernel files" "Assets/Scripts/Installers/World/WorldInstaller.cs" "Assets/Scripts/Installers/World/EventReaderAotDeclarations.cs" "both asmdef" "13 event types + AppStateRequestedEvent"}
  :carries #{EventLog-types EventLog-methods EventRing-methods EventReader-methods EventLog-data EventLog-wiring EventTypes}
  :confidence 60
  :note "стоїть першим — решта читає його типи"}
 {:slice :consumers
  :writes #{"усі файли EventConsumer-per-event, EventConsumer-any-of-three, EventConsumer-batch, EventProducers, AppStateRequest (Boot, InitializationSystem, стани)"}
  :carries #{EventConsumer-per-event EventConsumer-any-of-three EventConsumer-batch EventProducers AppStateRequest}
  :confidence 60}
 {:slice :rules
  :writes #{"RULES_SPECIFICATION.md" "ARCHITECTURE.md (з дозволу)" "Patterns/*" ".claude/skills/fantasymayor-pattern-choice/SKILL.md" ".claude/skills/fantasymayor-placement/SKILL.md" ".claude/skills/fantasymayor-graph/SKILL.md" ".claude/skills/fantasymayor-graph/references/*" "DOC_STANDARD.md"}
  :carries #{EventLog-rule-edits}
  :confidence 60
  :note "специфікація першою в межах слайса (§ 6)"}
 {:slice :checks
  :writes #{".claude/skills/fantasymayor-graph/scripts/*.py" "Tools/MarkerShapeAnalyzer/*.cs" "Assets/Scripts/EcsExtensions/MarkerShapeAnalyzer.dll" "~/.claude/skills/arch-check/SKILL.md"}
  :carries #{EventLog-check-edits EventLog-check-edits-from-code}
  :confidence 60}]
```

```clojure
{:one-runner {:confidence 40}}   ;; ~60 файлів чотирьох різних видів; один runner — довгий контекст, але одна історія; слайси не перетинаються за файлами
```

# Gate

```clojure
{:gate-after-s2 {:subject "the structure of the code: decomposition, names, lifetime"
                 :owner-verdict ?
                 :code-as #{:one-runner :slices}
                 :questions [{:q :s2-q1 :ask "IEventTag — лишаємо імʼя з винятком у :name/suffix (contra :c-3)?"}
                             {:q :s2-q2 :ask "споживачі без якоря стоять на IUpdatedSystem напряму, без нової бази (:system/base-choice змінюється) — так?"}
                             {:q :s2-q3 :ask "DrainBatch на EventReader для п'яти «раз на пачку» (contra :c-8)?"}
                             {:q :s2-q4 :ask "значення Reactive прибираємо з SystemRoleKind, FM1002 і FM1005 знімаються; маркер лишається лише PerFrame — так?"}
                             {:q :s2-q5 :ask "два нові id понад прийняті в s1: :event/clear-all і :event/aot-reader — приймаєте?"}
                             {:q :s2-q6 :ask "BuildDistrictCompleteEvent — public, InternalsVisibleTo чи друге оголошення (contra :c-1)?"}
                             {:q :s2-q7 :ask "InitializationSystem без таблиці — відхилення коментарем (contra :c-4)?"}
                             {:q :s2-q8 :ask "коментарі InitializationState і MapCreationState — переписати лише коментарі (contra :c-7)?"}
                             {:q :s2-q9 :ask "AppStateRequestedEvent у Boot.Implementation/Events і видалення Boot.Core Components/Tags/Archetypes разом з .meta — так? Debug.Log «[skh] requested state» лишаємо?"}
                             {:q :s2-q10 :ask "fmgraph визначає читача за ПОЛЕМ EventReader, не за injects (RegisterAppStateSystem графом не читається); не-система з читачем — ребро consumes без ролі — так?"}
                             {:q :s2-q11 :ask "код — одним runner чи чотирма слайсами, і якими моделями?"}]}}
```

<!-- doc-lint: on -->

# Read-back

```clojure
[{:file "Assets/Scripts/EcsExtensions/EventLog.cs" :line "1-146"
  :asks "sequence/plot/name/scale/why"
  :finding "matches s2 EventLog-methods exactly: ctor, Raise+helpers (RingOf/EvictOldestWhenFull/EvictOldest/BirthEventEntity), OpenCursor, TryRead+helpers (ClampedCursor/DeliverEventAt), ClearAllEvents, RingOf/CapacityOf — the file's own comment (line 122-123) states the ordering rule, matching s2's stated file order. Every field is a noun of the task (_ringsByEventType, _cursors, _lastSequence)."
  :proposal "keep-because: story-test passes: readable top-to-bottom without scrolling; no invented method absent from s2"}
 {:file "Assets/Scripts/EcsExtensions/EventRing.cs" :line "1-56"
  :asks "sequence/name/why"
  :finding "internal sealed class as s2 declares; OldestOffset/AppendedCount/SlotAt read as plain arithmetic on _appended/_count/_head, matching s2's ring-offset model; no method not implied by EventLog's calls (Append, DropOldest, SlotAt, Oldest)"
  :proposal "keep-because: no drift"}
 {:file "Assets/Scripts/EcsExtensions/EventRingSlot.cs, IEventTag.cs, EventCapacityAttribute.cs" :line "whole"
  :asks "name/why"
  :finding "EventCapacityAttribute's own summary and s2 both say it 'stands nowhere yet' (:status :new, unused) — confirmed by grep, zero event type applies it today; this is a stated-in-s2 fact, not a code-discovered gap"
  :proposal "keep-because: matches s2 note verbatim, not a silent :from-code addition"}
 {:file "Assets/Scripts/EcsExtensions/EntityStorages.cs" :line "10-24"
  :asks "name/scale"
  :finding "+ public EventLog Events {get;} born in ctor alongside World/Singletons exactly as s2 EventLog-types :EntityStorages :role describes; one-word field name (Events) reads as a noun of the task"
  :proposal "keep-because: no drift"}
 {:file "Assets/Scripts/Installers/World/EventReaderAotDeclarations.cs" :line "1-89"
  :asks "scale/why"
  :finding "14 closed EventReader<T> mentioned (SelectedHexChanged, HexIconsVisibilityChanged, ForestHexAppeared, ForestHexRemoved, ContextTabChanged, TurnCompleted, NextTurn, DistrictBuildUIRequested, DistrictBuildConfirmed, BuildDistrictCancel, BuildDistrictComplete, DistrictTableChanged, TerrainGenerationGenerateEventComponent, AppStateRequested) — matches s2's '14 типів' and rule event/aot-reader; trailing throw keeps the compiler from stripping it, matches the doc comment's own explanation"
  :proposal "keep-because: no drift"}
 {:file "Assets/Modules/Boot/Implementation/Boot.cs" :line "17-73"
  :asks "sequence/plot/name/why"
  :finding "TryReadLastRequestedMode(out AppState) matches s2 :method/try signature and :same-tick-delivery family exactly; class comment states the never-switch-inside-tick rule s2's :family :same-tick-delivery table describes; plot is a clean table of contents (Start/Update/LateUpdate/OnDestroy/Construct/helper)"
  :proposal "keep-because: no drift"}
 {:file "Assets/Modules/Boot/Implementation/Systems/InitializationSystem.cs" :line "8-31"
  :asks "why/name"
  :finding "in-code deviation comment (:deviation/in-code) names the accepted contra :c-4 (IUpdatedSystem with no work table, no tick anchor) verbatim to s2's :case text; Update body is one line, matches s2 :flow :step-7"
  :proposal "keep-because: s2 explicitly required the deviation to be carried as a comment (q7/c-4) — present and worded correctly"}
 {:file "Assets/Modules/Boot/Implementation/States/MainMenuState.cs, GameplayState.cs" :line "whole"
  :asks "sequence/why"
  :finding "MainMenuState.RequestMapCreationOnGenerateRequest uses DrainBatch (batch-form consumer) as s2 assigns it; GameplayState.SeedStartOfPlay comment reads 'raise a log event' — matches s2's :comments \"'one-frame event' → 'event'\" rewrite (line 731 of s2)"
  :proposal "keep-because: no drift"}
 {:file "Assets/Modules/Boot/Implementation/States/InitializationState.cs, MapCreationState.cs" :line "whole"
  :asks "why"
  :finding "both changed ONLY in doc comments as s2 :status :changed :role prescribed (contra :c-7); InitializationState summary now says 'today only InitializationSystem'; MapCreationState SettleFrames=3 and its comment both consistent with unmodified behavior"
  :proposal "keep-because: matches s2 comment-only scope exactly"}
 {:file "Assets/Modules/Turn/Systems/TurnCountSystem.cs" :line "16-46"
  :asks "sequence/why"
  :finding "per-event form (while TryRead) as its assigned form; AdvanceTurnCount is fail-loud on a missing TurnCountComponent, matching the project's fail-loud-no-silent-skip rule; class doc explains why it is reactive not polling"
  :proposal "keep-because: no drift"}
 {:file "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelDistrictSystem.cs" :line "40-213"
  :asks "sequence/plot/variable"
  :finding "three independent while(TryRead) loops for the three readers named in s2 (SelectedHexChanged, TurnCompleted, DistrictTableChanged), each calling the same ReconcileDistrictBlock — matches s2's 'each in its own reader ... reconciles the block once'; class doc names the fold of confirm/completion/cancel into one DistrictTableChangedEvent, matching s2's District-table-changed design"
  :proposal "keep-because: no drift"}
 {:file "Assets/Modules/Turn/Systems/TurnProcessorSystem.cs" :line "24-87"
  :asks "why/name"
  :finding "DrainBatch (batch form) as assigned; class doc justifies per-frame-not-reactive (must poll in-flight UniTask); raises TurnCompletedEvent only from the Completed branch — matches s2's producer→TurnCountSystem pairing"
  :proposal "keep-because: no drift"}
 {:file "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictTurnTickSystem.cs, BuildDistrictCompletionSystem.cs" :line "whole"
  :asks "why"
  :finding "':lossy-producer'/'completion-eviction-risk' contra :c-2 wording is carried verbatim into both classes' doc comments; raise-once-per-turn guarded by anyBuildFinishedThisTurn; Allocator.Temp NativeList in both is created and Dispose()'d within the same synchronous Update/Tick call (try/finally or explicit dispose on every exit path) — no Temp held past its frame, consistent with the zero-allocation/native-lifetime rule"
  :proposal "keep-because: no drift; Temp lifetime is correct, not a new violation"}
 {:covered ["Assets/Scripts/EcsExtensions/EventLog.cs" "EventRing.cs" "EventRingSlot.cs" "EventReader.cs" "IEventTag.cs" "EventCapacityAttribute.cs" "EntityStorages.cs" "Assets/Scripts/Installers/World/EventReaderAotDeclarations.cs" "Assets/Modules/Boot/Implementation/Boot.cs" "Systems/InitializationSystem.cs" "States/MainMenuState.cs" "States/GameplayState.cs" "States/InitializationState.cs" "States/MapCreationState.cs" "Assets/Modules/Turn/Systems/TurnCountSystem.cs" "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelDistrictSystem.cs" "Assets/Modules/Turn/Systems/TurnProcessorSystem.cs" "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictTurnTickSystem.cs" "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictCompletionSystem.cs"]
  :not-covered ["Assets/Presentation/HexIcons/Systems/HexIconsVisibilitySystem.cs" "Assets/Scripts/EcsExtensions/SystemRoleAttribute.cs" "Assets/Scripts/Installers/World/WorldInstaller.cs" "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionCancelSystem.cs" "Assets/Presentation/Terrain/Systems/HexSelectionViewSystem.cs" "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionSystem.cs" "Assets/Presentation/Districts/Systems/DistrictBuildProgressViewSpawnSystem.cs" "Assets/Presentation/Districts/Systems/DistrictBuildProgressViewDespawnSystem.cs" "Assets/Presentation/Districts/Systems/DistrictViewSpawnSystem.cs" "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildListUISubSystem.cs" "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelHeaderSystem.cs" "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildUISystem.cs" "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelResourcesSystem.cs" "Assets/Presentation/UI/MainHud/ContextTabs/Systems/ContextTabsAvailabilitySystem.cs" "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelSystem.cs" "Assets/Presentation/UI/MainHud/ContextTabs/Systems/ContextTabSelectionSystem.cs" "Assets/Presentation/HexResources/Systems/ForestSpawnSystem.cs" "Assets/Presentation/HexResources/Systems/ForestDespawnSystem.cs" "Assets/Domains/Economy/DistrictOpenCondition/Systems/DistrictOpenConditionEvaluatorTableChangedSystem.cs" "Assets/Modules/Boot/Implementation/Events/AppStateRequestedEvent.cs (skimmed via s2 only, not read whole)"]
  :stopped-by #{:time}}]
```

```clojure
;; pass 2
[{:file "Assets/Presentation/HexIcons/Systems/HexIconsVisibilitySystem.cs" :line "1-125"
  :asks "sequence/plot/name/why"
  :finding "matches EventConsumer-per-event table row exactly (HexIconsVisibilityChangedEvent → hexIconsVisibilityChanges → RebuildHexIcons); base(...) gone, no IsRipe, no anchor; class doc names the event by see-cref, not by old one-frame/ripe language; container×resource scan comment explains WHY no index is cached ('runs only on event frames') — the why stands exactly where the code would lie without it"
  :proposal "keep-because: no drift"}
 {:file "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionSystem.cs" :line "1-203"
  :asks "sequence/plot/variable/why"
  :finding "matches table row :values true (Reaction takes the event payload — 'in DistrictBuildConfirmedEvent confirmed'); BuildConfirmedDistrict's :also is present verbatim: Events.Raise(DistrictTableChangedEvent{Planned}) always, plus Raise(BuildDistrictCompleteEvent()) only when cost.TurnsToBuild==0; class doc is a full paragraph of why (spend-then-commit ordering, no draft, hex/district/payer from the pulse) that would otherwise have to live in comments scattered through the method"
  :proposal "keep-because: no drift"}
 {:file "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionCancelSystem.cs" :line "1-190"
  :asks "sequence/plot/why"
  :finding "matches table row :values true; CancelDistrictBuild's :also present (Raise DistrictTableChangedEvent{Removed} after both rows deleted); refund math (same-turn full vs proportional) is named in the class doc, not left for the reader to reverse-engineer from the code"
  :proposal "keep-because: no drift"}
 {:file "Assets/Presentation/Districts/Systems/DistrictViewSpawnSystem.cs, DistrictBuildProgressViewSpawnSystem.cs, DistrictBuildProgressViewDespawnSystem.cs" :line "whole, each"
  :asks "sequence/scale/why"
  :finding "all three are per-event consumers on DistrictTableChangedEvent as s2 declares (SpawnMissingDistrictViews/SpawnMissingProgressViews/DespawnStaleProgressViews); each snapshots ids into a Temp NativeList/NativeHashSet before iterating, matching s2's :structural note (birth is not a structural change, the AddComponent that follows is); every Temp collection is created and Dispose()'d inside the same synchronous method call (try/finally or end-of-method) — none held past its frame; class docs each state the idempotency argument (works off current state, not the payload) in one place"
  :proposal "keep-because: no drift; native lifetime correct"}
 {:file "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildUISystem.cs" :line "1-219"
  :asks "sequence/plot/name/why"
  :finding "matches EventConsumer-batch entry: [SystemRole(PerFrame)] kept, UpdatedSystem base with the view-singleton table anchor kept (contra :c-6 — Update(state, entity) only fires with the anchor row present), _overlayRequests.DrainBatch() read before the view-null guard exactly as s2 orders it; OnConfirmed's :also (Raise DistrictBuildConfirmedEvent) present; PopulateSections' pending-UniTask guard is explained by :c13-async-parts-in-update-host in a comment right where the throw would otherwise look arbitrary"
  :proposal "keep-because: no drift"}
 {:file "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildListUISubSystem.cs" :line "1-119"
  :asks "sequence/name/why"
  :finding "own reader as :orchestrator/own-reader prescribes (constructor takes EventReader<DistrictBuildUIRequestedEvent> directly, not via the orchestrator); DrainBatch() gates the default-selection write exactly at s2's step; H4 (repopulate-after-click finds the reader already drained) is named in the comment right above the DrainBatch call, not left implicit"
  :proposal "keep-because: no drift"}
 {:file "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelSystem.cs, HexInfoPanelHeaderSystem.cs" :line "whole, each"
  :asks "sequence/plot/name"
  :finding "both match their EventConsumer-per-event table rows (SwapContextPanel / FillHexHeader on SelectedHexChangedEvent); no IsRipe, no anchor; class docs state each system's narrow ownership (SwapContextPanel does NOT touch the bottom-panel shell, FillHexHeader skips gracefully on a non-grid coordinate) at the point a reader would otherwise wonder why there's no error path there"
  :proposal "keep-because: no drift"}
 {:file "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelResourcesSystem.cs" :line "38-39, 60"
  :asks "variable/why"
  :finding "matches its table row (FillResourcesBlock on SelectedHexChangedEvent) but keeps a private readonly List<HexInfoPanelView.ResourceChip> _chips field (System.Collections.Generic) as a reused buffer on a repeatedly-running IUpdatedSystem — arch-check's Generic-collection ban targets exactly this shape. s2's :class-shape for this family says only 'the rest of the parameters as they were' — this field predates the cascade and is not named in EventConsumer-per-event, so the translation did not invent it"
  :proposal "keep-because: pre-existing, out of s2's stated scope for this family (field untouched by the event-shape change); a genuine arch-check candidate but not a cascade regression — worth a separate arch-check pass over Presentation/UI, not a read-back fix"}
 {:file "Assets/Presentation/UI/MainHud/ContextTabs/Systems/ContextTabSelectionSystem.cs, ContextTabsAvailabilitySystem.cs" :line "whole, each"
  :asks "sequence/name/why"
  :finding "match their table rows; ContextTabsAvailabilitySystem's STUB comment (IsAvailable always true, no player-action model yet) is exactly the kind of why that belongs in the code — it explains a return that would otherwise look like dead logic; both fail loud on a missing singleton per the project's no-silent-skip rule where the field is genuinely required (ContextTabSelectionSystem throws on missing ActiveContextTabComponent / Unknown tab), and no-op where a missing view is a legitimate transient state"
  :proposal "keep-because: no drift"}
 {:file "Assets/Presentation/HexResources/Systems/ForestSpawnSystem.cs, ForestDespawnSystem.cs" :line "whole, each"
  :asks "sequence/scale/why"
  :finding "both match their table rows (dormant consumers, ForestHexAppearedEvent / ForestHexRemovedEvent, no producer yet — event/dormant-consumer named explicitly in both class docs); Temp NativeHashSet/NativeList pairs are created and Dispose()'d inside the same reaction call (try/finally in Spawn, plain end-of-method dispose in Despawn) — none held past the frame; the green-ground-stays-after-chopping design note in ForestDespawnSystem's doc is a why that would otherwise read as a bug (why isn't the paint reverted)"
  :proposal "keep-because: no drift; native lifetime correct"}
 {:file "Assets/Domains/Economy/DistrictOpenCondition/Systems/DistrictOpenConditionEvaluatorTableChangedSystem.cs" :line "1-52"
  :asks "sequence/name/why"
  :finding "matches its table row's :also ('Scoped registration lishayetsya; IReadOnlyList subSystems parameter lishayetsya'); class comment states why it exists (re-run the same-turn evaluator on confirm/cancel instead of waiting for next Preview pass) at the top, not buried in Update"
  :proposal "keep-because: no drift"}
 {:file "Assets/Presentation/Terrain/Systems/HexSelectionViewSystem.cs" :line "1-177"
  :asks "sequence/scale/why"
  :finding "matches its table row (SyncSelectionBorder on SelectedHexChangedEvent); ComputeSelectionRings is the one paragraph-scale heterogeneous step (BFS over vertex rings) s1/s2 would carve out as its own method — about 70 lines but entirely one computation (border ring math), not several unrelated concerns stitched together, so it does not fail the ~40-line/heterogeneous-split test on its own terms; its Temp collections are disposed at the end of the method without try/finally (pre-existing shape, untouched by the event-shape change — the constructor/Update signature is the only thing s2 touches here)"
  :proposal "keep-because: matches s2; the missing try/finally around Temp collections in ComputeSelectionRings predates this cascade and s2 did not touch that method — separate from the migration"}
 {:file "Assets/Scripts/Installers/World/WorldInstaller.cs" :line "49-94"
  :asks "sequence/why"
  :finding "matches EventLog-wiring verbatim: builder.Register(typeof(EventReader<>), Lifetime.Transient) present with the exact comment s2 specifies (each injection builds a new reader with its own cursor; closed types live in EventReaderAotDeclarations); RegisterAppStateSystem<EventCleanupSystem> registration is gone, no other registration touched"
  :proposal "keep-because: no drift"}
 {:file "Assets/Scripts/EcsExtensions/SystemRoleAttribute.cs" :line "1-18"
  :asks "name/why"
  :finding "SystemRoleKind is not shown in this file (enum lives elsewhere) but the attribute's own comment already states the post-migration rule precisely: 'a class that runs the Update loop and still holds a readonly EventReader<TEvent> field yet ticks every frame rather than reacting' — matches :system/marker-value and :system/marker-required verbatim; no residual mention of Reactive as a marker value"
  :proposal "keep-because: no drift"}
 {:file "Assets/Modules/UserInput/Systems/HexSelectionSystem.cs (RaiseSelectionChanged), Assets/Modules/Boot/Implementation/States/GameplayState.cs (SeedStartOfPlay)" :line "121-124 / 41-44"
  :asks "why"
  :finding "both producer sites and their doc comments were rewritten off 'one-frame pulse' language to 'Log event ... reconcile ... without per-frame polling' — matches EventProducers :comments '«One-frame pulse» → «event»' precisely"
  :proposal "keep-because: no drift"}
 {:file "Assets/Presentation/UI/GeneratorMenu/Views/HexesUI.cs, Assets/Presentation/UI/MainHud/ContextTabs/Views/ContextTabsView.cs, Assets/Presentation/UI/MainHud/TurnPanel/Views/TurnPanelView.cs" :line "Events.Raise sites only"
  :asks "why"
  :finding "all three still raise via _storages.Events.Raise(new TEvent()) (correct call shape) but ContextTabsView and TurnPanelView's class docs still say 'raises a one-frame ContextTabChangedEvent' / 'raising the one-frame event' — stale language now that one-frame/ripe semantics are retired everywhere else. s2's EventProducers table marks both with :note 'те саме' pointing at HexesUI's :views-raise-events-kept — i.e. these three views are explicitly OUT of this cascade's writing scope (FLOW.md decision :views-raise-events-kept: 'лишаються як є; не в цьому каскаді')"
  :proposal "keep-because: matches s2's explicit deferral; the stale 'one-frame' wording in these two view docs is a known, named leftover for the separate view/boundary task, not a translation gap"}
 {:file "Assets/Presentation/UI/MainHud/HexInfoPanel/Views/HexInfoPanelView.cs" :line "254-257"
  :asks "why"
  :finding "OnBuildClicked raises DistrictBuildUIRequestedEvent via Events.Raise as s2's EventProducers row requires; did not re-read the full class doc for the 'payload-less one-frame request … No consumer yet' → 'log event, consumers DistrictBuildUISystem and DistrictBuildListUISubSystem' rewrite s2 calls for at this file — time-boxed to the Raise site only per task instruction"
  :proposal "keep-because: site matches; doc-comment rewrite unverified — flag for a future pass if the owner wants full confirmation of this one file's summary"}
 {:covered ["Assets/Presentation/HexIcons/Systems/HexIconsVisibilitySystem.cs" "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionSystem.cs" "Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionCancelSystem.cs" "Assets/Presentation/Districts/Systems/DistrictViewSpawnSystem.cs" "Assets/Presentation/Districts/Systems/DistrictBuildProgressViewSpawnSystem.cs" "Assets/Presentation/Districts/Systems/DistrictBuildProgressViewDespawnSystem.cs" "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildUISystem.cs" "Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildListUISubSystem.cs" "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelSystem.cs" "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelHeaderSystem.cs" "Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelResourcesSystem.cs" "Assets/Presentation/UI/MainHud/ContextTabs/Systems/ContextTabSelectionSystem.cs" "Assets/Presentation/UI/MainHud/ContextTabs/Systems/ContextTabsAvailabilitySystem.cs" "Assets/Presentation/HexResources/Systems/ForestSpawnSystem.cs" "Assets/Presentation/HexResources/Systems/ForestDespawnSystem.cs" "Assets/Domains/Economy/DistrictOpenCondition/Systems/DistrictOpenConditionEvaluatorTableChangedSystem.cs" "Assets/Presentation/Terrain/Systems/HexSelectionViewSystem.cs" "Assets/Scripts/Installers/World/WorldInstaller.cs" "Assets/Scripts/EcsExtensions/SystemRoleAttribute.cs" "Assets/Modules/UserInput/Systems/HexSelectionSystem.cs (Raise site)" "Assets/Presentation/UI/GeneratorMenu/Views/HexesUI.cs (Raise site)" "Assets/Presentation/UI/MainHud/ContextTabs/Views/ContextTabsView.cs (Raise site)" "Assets/Presentation/UI/MainHud/TurnPanel/Views/TurnPanelView.cs (Raise site)" "Assets/Presentation/UI/MainHud/HexInfoPanel/Views/HexInfoPanelView.cs (Raise site)"]
  :not-covered ["Assets/Modules/Boot/Implementation/Events/AppStateRequestedEvent.cs (whole; only checked via s2 in pass 1)" "the 13 EventTypes struct files themselves (EventTypes table, whole-file read never done in either pass — s2's comment-rewrite claim for them is unverified)" "HexInfoPanelView.cs class-level doc comment (only the Raise site was read, per task's time-short instruction)"]
  :stopped-by #{:done}}]
```

# Converge

```clojure
;; :scope :whole — fresh-context converge pass, code already on Tasks/FM-14-district-ux (commits ea72bcd..734ff9b)
;; kernel group: EventLog-types, EventLog-methods, EventRing-methods, EventReader-methods, EventLog-data, EventLog-wiring —
;; every born/changed/removed type and every method read against Assets/Scripts/EcsExtensions/*.cs,
;; Assets/Scripts/Installers/World/{WorldInstaller,EventReaderAotDeclarations}.cs, EntityStorages.cs, SystemPriorities.cs,
;; SystemRoleKind.cs — all :present, signatures and flow match s2 verbatim (SystemRoleKind carries only PerFrame, per
;; :s2-gate q4). 0 non-present entries.
[]
```

```clojure
;; boot group: Boot, InitializationSystem, MainMenuState, GameplayState, InitializationState, MapCreationState (AppStateRequest
;; family) — Boot.Construct/Update/TryReadLastRequestedMode/OnDestroy, InitializationSystem.Update, MainMenuState's
;; EventReader<TerrainGenerationGenerateEventComponent>, GameplayState.SeedStartOfPlay's Raise(HexIconsVisibilityChangedEvent) —
;; all read whole, all :present; AppStateComponent/AppStateTag/CoreArchetypes confirmed removed from disk.
;; 0 non-present entries.
[]
```

```clojure
;; consumers/producers group: EventConsumer-per-event (16 classes), EventConsumer-any-of-three (HexInfoPanelDistrictSystem),
;; EventConsumer-batch (5 classes), EventProducers (6 sites), EventTypes (13 declared structs implement IEventTag) — every
;; class-shape (EventReader field, no base(...) anchor, no IsRipe) and every Raise site grepped and spot-read; matches s2
;; :class-shape and :flow exactly, including BuildDistrictCompleteEvent now public per :s2-gate q6.
;; 0 non-present entries.
[]
```

```clojure
;; tools group: fmgraph ecs_facts.py/roles.py/tag_law.py/source_reading.py/di_facts.py/recipes.py/type_facts.py/queries.py,
;; Tools/MarkerShapeAnalyzer/{MarkerVocabulary,MarkedTypeCheck,MarkerShapeAnalyzer}.cs, ~/.claude/skills/arch-check/SKILL.md —
;; EventReader-based role detection, event/no-tag-on-persistent-row check, register_open_generic site, FM1002/FM1005 confirmed
;; retired (FM1001/1003/1004/1006/1007/1008 remain), RULES_SPECIFICATION.md :retired-ids and the 9 :born event/* + 1
;; orchestrator/own-reader ids all present verbatim. The three :from-code entries already declared in
;; EventLog-check-edits-from-code (queries.py INT_MAX, type_facts.py "generic", ecs_facts.py any_components simplification)
;; verified present in the scripts — already accounted for by s2's own :from-code mark, not new drift.
;; 0 non-present entries.
[]
```

```clojure
{:at "2026-09-17" :present 124 :partial 0 :contradicts 0 :unrequested 0
 :covered [:kernel :boot :consumers-producers :tools]
 :note "N=124 counts named s2 units actually checked against code: kernel types 29 (EventLog-types) + kernel methods 25
        (EventLog 12 incl ctor, EventRing 9 incl ctor, EventReader 3, DeclareClosedReaders 1) + boot/AppStateRequest
        methods 5 (Boot×4, InitializationSystem×1) + consumer/producer classes 28 (16 per-event + 1 any-of-three + 5
        batch + 6 producer sites) + declared event-type structs 13 + rule-surface ids 15 (6 retired + 9 born) +
        fmgraph/MarkerShapeAnalyzer/arch-check check-carriers 9 = 124. No :partial, :contradicts or :unrequested found
        in this pass — corroborates # Read-back's two passes (also clean on everything they covered). Not independently
        re-verified: the 13 EventTypes struct files' full doc-comment rewrite (only IEventTag conformance checked here),
        and HexInfoPanelView.cs's class-doc rewrite (only its Raise site checked) — both already flagged :not-covered
        by # Read-back pass 2, not new gaps from this converge."
 :stopped-by #{:done}}
```

