---
category: A
read: archive
status: closed-by-owner
tags:
  - ecs
  - events
  - cascade
related:
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[PATTERN_EVENT](../../../Patterns/PATTERN_EVENT.md)"
---

# EVENT_LOG — журнал подій на кшталт Kafka

# Request

```clojure
{:source :user
 :received-at "2026-09-16"
 :raw-request ["Можна зробити доволі простий consumer log по типу kafka. Щоб кожна система могла підписуватися на свої події і отримувати завжди останню непрочитану чи всі непрочитані, відпрацьовувати їх і потім помічати, а далі мати якийсь сталий capacity на події і видаляти їх по ttl чи id\nтоді ніяких проблем ніколи не буде. Думаю для цього можна буде навіть створити свій окремий EntityStore"
               "давай цю систему закинемо і зробимо через каскад. \nЩодо твоїх запитань\n1 - пропонуй варіанти, в цілому я за окремий EntityStore з обмеженою кількістю entity на кожен event tag\n2 - думаю варіант б. \n3 - а\n4 - заклади, скоріше за все користуватися я ним не буду, але нехай, воно їсти не просить\n5 - подія може використовувати PK в якості посилання, а PK ніколи не перевикористовуються\n6 - події зливати не будемо\nЩодо буферу - мені без різниці"
               "Q1 - подивися як це зроблено в kafka чи RabbitMQ не хочу вигадувати велосипед і не хочу постійно в кожній системі займатися цим мікроменджментом того що я вже прочитав, \nQ7 - подія не повинна переповнюватися, для цього є capacity я не хочу думати про те що можливо десь там в майбутньому якась система це ще має прочитати. Не прочитала - її проблеми. \n го"]
 :context "виник з обговорення переходу MainMenu → MapCreation у паузованій задачі AppStateRunner (єдина реалізація IAppState)"}
```

# Confirmed contract

```clojure
{:task :event-log
 :goal "журнал подій як у Kafka: кожна система отримує свої непрочитані події без ручного обліку прочитаного; журнал очищується детерміновано"
 :path :cascade
 :where "Assets/Scripts/EcsExtensions (події, системи-бази), Assets/Modules/Boot (цикл систем), споживачі й виробники подій, RULES_SPECIFICATION.md і carriers"
 :off-limits #{"AppStateRunner — паузована задача" "Unity-side assets"}
 :decided "див. # Decisions"
 :do "research (depth :replace) → findings gate → CONTEXT → s1 → s2 → code → read-back → converge"
 :skip #{"злиття подій (conflation)"}
 :accept [{:meter "mcp__roslyn__get_diagnostics" :target "чисто на змінених файлах"}
          {:meter "/arch-check" :target "без нових порушень; облік прочитаного не є станом систем"}
          {:meter "fmgraph.py check" :target "без попереджень на змінених файлах; виробник → споживач видно"}
          {:meter "fantasymayor-rules-conformance" :target "no :diverges, :incomplete, :uncarried"}
          {:meter "Unity, запуск власника" :target "кожен нинішній ланцюжок подій працює як раніше"}]
 :result "події живуть у власному журналі з лімітом на тип; система бачить кожну подію свого типу рівно раз, поки та не витіснена"}
```

# Plan

```clojure
(-> (:step-1 "research pass 1: характеристика нинішніх подій + Kafka/RabbitMQ облік зміщень і утримання")
    (:step-2 "findings gate")
    (:step-3 "research pass 2 — що відкрили відповіді")
    (:step-4 "CONTEXT.md → s1 → s2 → code (sdd-cascade)"))
```

# Findings

```clojure
[{:finding :f1-current-contract
  :at "2026-09-16"
  :fact "нинішня подія: CreateEvent у World ставить EventFrameComponent{Frame}; IsRipe = Frame < Time.frameCount, тобто подію видно лише в кадрі N+1; EventCleanupSystem (int.MaxValue, у всіх станах) в кінці тіку видаляє всі дозрілі; споживач перевіряє IsRipe першим рядком"
  :verified-by "прочитано EcsEventExtensions, EventCleanupSystem, EventArchetypes, ARCHITECTURE.md → Events"
  :consequence "замінюються правила event/raise, ripe, anchor, no-same-frame, cleanup, lossy-producer, startup-bulk; нова модель мусить зберегти event/one-way, no-tag-on-persistent-row, dormant-consumer"}
 {:finding :f2-inventory
  :at "2026-09-16"
  :fact "14 точок CreateEvent (12 типів подій), 21 перевірка IsRipe (19 споживачів + MainMenuState + EventCleanupSystem); 16 споживачів передають EventArchetypes.Of<T> у base(...), 5 тримають архетип окремо й опитують кілька типів (TurnProcessorSystem, BuildDistrictCompletionSystem, DistrictBuildUISystem, DistrictBuildListUISubSystem, MainMenuState)"
  :verified-by "rg по Assets: CreateEvent(, IsRipe(, EventArchetypes.Of<"
  :consequence "потрібні дві форми читання: система на один тип (замість якоря в base) і читач, яким система опитує кілька типів"}
 {:finding :f3-producers-outside-loop
  :at "2026-09-16"
  :fact "5 виробників — не системи: views HexesUI, HexInfoPanelView, ContextTabsView, TurnPanelView і GameplayState (після async входу)"
  :verified-by "rg CreateEvent("
  :consequence "журнал має приймати запис ззовні циклу систем; модель зі зміщенням на споживача знімає питання «в якому кадрі дозріє» для таких виробників"}
 {:finding :f4-dormant
  :at "2026-09-16"
  :fact "ForestHexAppearedEvent і ForestHexRemovedEvent мають споживачів (ForestSpawnSystem, ForestDespawnSystem), але не мають виробника"
  :verified-by "rg по Assets — лише декларації, якорі та doc-коментарі"
  :consequence "за правилом event/dormant-consumer це дозволено; нова модель повинна підтримати споживача без виробника"}
 {:finding :f5-tooling-couples-to-anchor
  :at "2026-09-16"
  :fact "fmgraph.py визначає роль reactive за EventArchetypes.Of у base(...) і зв'язки emits/polls за CreateEvent і held archetype; MarkerShapeAnalyzer вимірює той самий якір"
  :verified-by "fmgraph.py explain DistrictBuildUISystem / TurnProcessorSystem (decided_by, emits, polls); CLAUDE.md role-marker правило в fantasymayor-pattern-choice"
  :consequence "зміна форми споживача ламає розпізнавання ролей — fmgraph.py і MarkerShapeAnalyzer входять у каскад (CLAUDE.md § 6 step-3)"}
 {:finding :f6-observer-violation
  :at "2026-09-16"
  :fact "Boot підписується на entity.OnComponentChanged сутності AppState; ARCHITECTURE.md event/no-change-observers забороняє будь-який спостерігач зміни значення"
  :verified-by "прочитано Boot.cs і ARCHITECTURE.md → Events"
  :consequence "зміна з попередньої задачі порушує чинне правило; запит на зміну стану природно стає подією журналу"}
 {:finding :f7-kafka
  :at "2026-09-16"
  :fact "Kafka: позиція споживача сама просувається на кожному poll(); enable.auto.commit=true за замовчуванням комітить позицію у фоні (зміщення попереднього poll) — код споживача зміщень не торкається; зміщення зберігає брокер по group.id; кожна група отримує всі повідомлення незалежно; retention.bytes/retention.ms (cleanup.policy=delete) видаляють старі сегменти незалежно від споживачів; auto.offset.reset (latest|earliest|none, default latest) — що робити, коли збереженого зміщення вже немає в журналі"
  :verified-by "KafkaConsumer javadoc 4.1 (Offsets and Consumer Position), kafka.apache.org consumer-configs і topic-configs 4.1"
  :consequence "рішення власника на Q7 = retention delete; «без мікроменеджменту» = auto-commit + зміщення в брокері; витиснене зміщення = auto.offset.reset"}
 {:finding :f8-rabbitmq
  :at "2026-09-16"
  :fact "RabbitMQ classic: черга на споживача, в режимі auto-ack повідомлення вважається доставленим одразу після відправки; max-length з overflow drop-head (default) відкидає найстаріші. RabbitMQ Streams: неруйнівне читання, споживач обирає старт (first|last|next|offset|timestamp), брокер сам зберігає зміщення споживача в потоці, утримання max-length-bytes / max-age"
  :verified-by "rabbitmq.com/docs/confirms, /maxlength, /streams"
  :consequence "дві моделі: копія в чергу кожного споживача (classic) або спільний журнал зі зміщеннями, які веде брокер (Streams ≈ Kafka)"}
 {:finding :f2-correction
  :at "2026-09-16"
  :fact "уточнення f2: 14 точок CreateEvent над 11 типами (не 12); 23 виклики IsRipe (не 21) — 21 клас-споживач, MainMenuState, EventCleanupSystem"
  :verified-by "context stage (Opus-агент) і повторний rg у головній розмові: CreateEvent(new <T> з uniq -c, rg -c IsRipe("
  :consequence "f2 недолічував; модель і рішення від цього не змінюються"}
 {:finding :f9-ids-recycled
  :at "2026-09-16"
  :fact "Friflo 3.6.0: EntityStore.RecycleIds = true за замовчуванням — id видаленої сутності видається новій; з false сховище лише росте"
  :verified-by "Friflo.Engine.ECS.xml 3.6.0, P:EntityStore.RecycleIds"
  :consequence "Entity.Id не може бути зміщенням у журналі — потрібен власний монотонний номер"}
 {:finding :f10-no-insertion-order
  :at "2026-09-16"
  :fact "Archetype.Entities у документації Friflo — «mainly used for debugging»; порядок вставки не гарантовано; упорядкований доступ дає лише RangeIndex + QueryFilter.ValueInRange, O(N log N) по унікальних значеннях"
  :verified-by "Friflo.Engine.ECS.xml 3.6.0: P:Archetype.Entities, M:QueryFilter.ValueInRange, T:RangeIndex`2"
  :consequence "читати «події після мого зміщення» перебором сутностей не можна; потрібен або індекс порядку, який веде журнал (кільце id), або RangeIndex на номері"}
 {:finding :f11-storages-registry
  :at "2026-09-16"
  :fact "EntityStorages тримає World (EntityStore) і Singletons (SingletonComponents) та конфіги; сховища створюються в конструкторі"
  :verified-by "прочитано Assets/Scripts/EcsExtensions/EntityStorages.cs"
  :consequence "третє сховище Events лягає в той самий реєстр без нового DI-типу для сховища"}
 {:finding :f12-structural-guard-per-store
  :at "2026-09-16"
  :fact "StructuralChangeException кидає ArchetypeQuery свого сховища (ThrowOnStructuralChange); народження сутності за архетипом і видалення — не структурні зміни (ARCHITECTURE.md → structural-change)"
  :verified-by "Friflo.Engine.ECS.xml: T:StructuralChangeException, P:ArchetypeQuery.ThrowOnStructuralChange; ARCHITECTURE.md"
  :consequence "запис і витіснення подій в окремому сховищі не заважають системам, що перебирають World; читач, який не перебирає сутності, від цього захищений повністю"}
 {:finding :f13-cursor-no-persistence
  :at "2026-09-16"
  :fact "події не серіалізуються і не переживають сесію; споживач живе стільки ж, скільки контейнер"
  :verified-by "rg: жодна подія не проходить через Easy Save / серіалізацію; EventCleanupSystem видаляє все"
  :consequence "ключу курсора не потрібна стабільність між сесіями — достатньо id, виданого журналом при створенні читача"}]
```

# Decisions

```clojure
[{:decision :store
  :status :confirmed
  :at "2026-09-16"
  :value "окремий EntityStore для подій, з обмеженою кількістю сутностей на кожен event tag"
  :verified-by "відповідь власника"
  :reason "власник: «в цілому я за окремий EntityStore»"}
 {:decision :inactive-consumer
  :status :confirmed
  :at "2026-09-16"
  :value "система неактивного стану, повернувшись, обробляє все, що лишилось у журналі"
  :verified-by "відповідь власника (Q2 б)"
  :reason "власник обрав повний хвіст"}
 {:decision :retention-min-offset
  :status :confirmed
  :at "2026-09-16"
  :value "видалення нижче найменшого підтвердженого зміщення; переповнення = виняток"
  :verified-by "відповідь власника (Q3 а)"
  :reason "запропоновано агентом як детерміноване"}
 {:decision :retention-capacity
  :status :confirmed
  :supersedes :retention-min-offset
  :at "2026-09-16"
  :value "ліміт на тип — єдине правило утримання: нова подія понад ліміт витісняє найстарішу; споживач, що не встиг прочитати, її втрачає; переповнення не кидає виняток"
  :verified-by "відповідь власника на Q7"
  :reason "перевірено: min-offset + повний хвіст (Q2 б) + виняток на переповненні блокує журнал неактивним споживачем; нове — власник: «Не прочитала — її проблеми»"}
 {:decision :global-sequence
  :status :confirmed
  :at "2026-09-16"
  :value "глобальний монотонний номер події між усіма типами"
  :verified-by "відповідь власника (Q4)"
  :reason "дешево, може знадобитись"}
 {:decision :entity-links
  :status :confirmed
  :at "2026-09-16"
  :value "подія посилається на сутності через PK; PK ніколи не перевикористовуються"
  :verified-by "відповідь власника (Q5)"
  :reason "стале посилання, без хендлів Entity"}
 {:decision :no-conflation
  :status :confirmed
  :at "2026-09-16"
  :value "події ніколи не зливаються"
  :verified-by "відповідь власника (Q6)"
  :reason "—"}
 {:decision :cursor-home
  :status :confirmed
  :at "2026-09-16"
  :value "спільний журнал на тип; зміщення кожного споживача веде сховище подій і просуває механізм читання — система зміщень не торкається (модель Kafka / RabbitMQ Streams)"
  :verified-by "findings f7, f8; відповідь власника QA (а) 75"
  :reason "власник не хоче ручного обліку прочитаного; варіант «копія в чергу кожного споживача» (25) відкинуто"}
 {:decision :commit-timing
  :status :confirmed
  :at "2026-09-16"
  :value "подія вважається прочитаною в момент видачі системі (auto-ack); виняток в обробці її не повертає"
  :verified-by "відповідь власника QB (б) 35"
  :reason "власник обрав простішу семантику; проєкт і так зупиняється на винятку (fail loud)"}
 {:decision :first-read-position
  :status :confirmed
  :at "2026-09-16"
  :value "споживач без зміщення читає з найстарішої події в журналі; витиснене зміщення продовжує з найстарішої, що лишилась"
  :verified-by "відповідь власника QC (а) 65; рішення :retention-capacity"
  :reason "узгоджено з :inactive-consumer (повний хвіст)"}
 {:decision :same-tick-delivery
  :status :confirmed
  :at "2026-09-16"
  :value "споживач після виробника бачить подію в тому ж тіку, перед ним — у наступному; правило event/no-same-frame скасовується"
  :verified-by "відповідь власника QD (а) 70"
  :reason "штучна затримка на кадр (30) відкинута"}
 {:decision :capacity-declaration
  :status :confirmed
  :at "2026-09-16"
  :value "ліміт оголошується атрибутом на структурі події, напр. [EventCapacity(64)]"
  :verified-by "відповідь власника QE (а) 55"
  :reason "ліміт живе поряд із типом"}
 {:decision :app-state-request-as-event
  :status :confirmed
  :at "2026-09-16"
  :value "запит на зміну стану застосунку стає подією журналу в цьому каскаді; підписка Boot на OnComponentChanged прибирається"
  :verified-by "finding f6; відповідь власника QG (а) 60"
  :reason "усуває порушення event/no-change-observers"}
 {:decision :consumer-shape
  :status :confirmed
  :at "2026-09-16"
  :value "одна форма споживача: система тримає readonly EventReader<TEvent> і сама читає while (reader.TryRead(out var evt)); окремої бази-споживача немає; роль reactive = «тримає EventReader»"
  :verified-by "відповідь власника QF (б); переоцінка агента (б) 70 / (а) 30 після QB auto-ack"
  :reason "перше пропонування (а) 70 не врахувало, що auto-ack знімає єдину точку коміту; одна форма прибирає маркер [SystemRole] і подвійну перевірку форми в fmgraph.py / MarkerShapeAnalyzer"}
 {:decision :buffer-shape
  :status :confirmed
  :at "2026-09-16"
  :value "дані події — сутність в окремому сховищі Events (EntityStorages); порядок — кільце id сутностей на кожен тип, яке веде журнал; TryRead O(1); витіснення = видалення сутності в голові кільця"
  :verified-by "findings f9, f10, f12; відповідь власника QH (а) 60"
  :reason "RangeIndex-запит щокадру на кожного споживача (25) і кільце структур без EntityStore (15) відкинуто"}
 {:decision :reader-source
  :status :confirmed
  :at "2026-09-16"
  :value "EventReader<TEvent> інжектиться DI як Transient: кожна ін'єкція — новий читач із власним курсором, id якого видає журнал; курсор живе в сховищі, читач тримає лише хендл"
  :verified-by "finding f13; відповідь власника QI (а) 55"
  :reason "fmgraph.py уже бачить ін'єкції — роль споживача розпізнається без нової ознаки; фабрика в конструкторі (45) відкинута"}
 {:decision :capacity-per-event-tag
  :status :confirmed
  :supersedes :capacity-declaration
  :at "2026-09-16"
  :value "кожна подія — сутність у сховищі Events з власним головним тегом свого типу (напр. <Name>EventTag); ліміт і кільце — на цей тег; спільний EventTag прибирається, бо сховище Events і так містить лише події; атрибут ліміту стоїть на цьому тезі"
  :verified-by "CONTEXT.md: у всіх подій один EventTag, «ліміт на event tag» був неоднозначним; відповідь власника на питання 1 після context stage"
  :reason "раніше ліміт оголошувався атрибутом на структурі події; нове — власник: подія це сутність зі своїм типом тегу, спільний тег зайвий в окремому сховищі"}
 {:decision :default-capacity
  :status :confirmed
  :at "2026-09-16"
  :value "тип без явного атрибута ліміту отримує типове значення — розмір одного чанку архетипу Friflo, якщо s1 його підтвердить, інакше 128"
  :verified-by "відповідь власника на питання 7"
  :reason "власник не хоче винятку для невказаного ліміту; число невелике"}
 {:decision :readers-outside-systems
  :status :confirmed
  :at "2026-09-16"
  :value "EventReader дозволений не лише системам (стан, підсистема, Boot); у графі роль — «споживач події», без ролі системи"
  :verified-by "відповідь власника на питання 2 (а) 65"
  :reason "варіант «лише системи» (35) відкинуто"}
 {:decision :per-frame-readers
  :status :confirmed
  :at "2026-09-16"
  :value "TurnProcessorSystem і DistrictBuildUISystem лишаються per_frame, хоч тримають EventReader; правило «reactive = тримає EventReader» має для них виняток маркером"
  :verified-by "відповідь власника на питання 3 («поки що»)"
  :reason "агент пропонував reactive (75); власник залишає per_frame поки що"}
 {:decision :batch-reaction
  :status :confirmed
  :at "2026-09-16"
  :value "журнал не зливає події, але споживач сам вирішує, чи реагувати на кожну, чи дочитати пачку й відреагувати раз; п'ять нинішніх «є хоч одна» споживачів дочитують пачку й реагують раз"
  :verified-by "CONTEXT.md гіпотези H1, H2; відповідь власника на питання 4 (а) 60"
  :reason "зберігає нинішню поведінку TurnProcessorSystem (H1 — без зайвого ходу) і чотирьох інших"}
 {:decision :views-raise-events-kept
  :status :confirmed
  :at "2026-09-16"
  :value "чотири view, що самі піднімають події (порушення view/boundary), лишаються як є; не в цьому каскаді"
  :verified-by "відповідь власника на питання 5"
  :reason "окрема задача"}
 {:decision :lossy-producer
  :status :confirmed
  :at "2026-09-16"
  :value "правило event/lossy-producer знімається; BuildDistrictTurnTickSystem піднімає BuildDistrictCompleteEvent один раз, коли лічильник уперше доходить до нуля; BuildDistrictCompletionSystem лишається звіркою зі станом за event/reaction; коментарі «never optimize this into raise-once» переписуються"
  :verified-by "пояснення агента (журнал прибирає вікно очищення, втрата лише витісненням; перепідняття засмічує журнал копіями); відповідь власника (а) 70"
  :reason "«лишити як захист від витіснення» (30) відкинуто — важливому типу дається більший ліміт"}
 {:decision :event-is-one-component
  :status :confirmed
  :supersedes :capacity-per-event-tag
  :at "2026-09-16"
  :value "подія — сутність у Events рівно з одним компонентом; тип цього компонента реалізує інтерфейс-маркер події (власник називає його «event tag»), що походить від Friflo IComponent; кільце, ліміт і атрибут ліміту — за цим типом; окремого ITag на подію немає; спільний EventTag прибирається"
  :verified-by "CASCADE.md s1 contra c-1 (читач знає тип значення, кільце стояло на тезі); відповідь власника на гейті s1, питання 1"
  :reason "раніше: власний тег + значення (два типи на подію, 10 з 13 без полів); нове — власник: подія завжди матиме 1 компонент, і цей компонент і є «тегом» події"}
 {:decision :capacity-all-default
  :status :confirmed
  :at "2026-09-16"
  :value "усім нинішнім типам подій — типовий ліміт 128; явний атрибут ніде не потрібен зараз"
  :verified-by "CASCADE.md s1 :numbers (розмір чанку Friflo не підтверджено); відповідь власника, питання 2"
  :reason "—"}
 {:decision :initialization-request-kept
  :status :confirmed
  :at "2026-09-16"
  :value "запит InitializationSystem на наступний стан лишається як є; «один раз» тримається тим, що Boot перемикає стан одразу після тіку"
  :verified-by "CASCADE.md s1 contra c-3; відповідь власника (б) 45"
  :reason "разовий крок входу (55) торкався паузованої задачі AppStateRunner"}
 {:decision :aot-closed-generics
  :status :confirmed
  :at "2026-09-16"
  :value "для IL2CPP один клас наперед оголошує закриті EventReader<TEvent> для кожного типу події, щоб AOT згенерував код; відкрита generic-реєстрація в DI лишається"
  :verified-by "CASCADE.md s1 contra c-4 (H9); відповідь власника: «задефайнити їх наперед … це просто 1 клас»"
  :reason "закриті реєстрації на кожен тип (30) і фабрика (20) не потрібні"}
 {:decision :clear-all-events
  :status :confirmed
  :at "2026-09-16"
  :value "журнал має API, що видаляє всі події з усіх кілець (і скидає курсори до порожнього журналу); хто і коли його викликає — не визначено цим рішенням"
  :verified-by "CASCADE.md s1 contra c-5 (H5/H6 — старі події після повернення в стан); відповідь власника: «мати API щоб почистити всі події»"
  :reason "замість скидання курсорів на виході зі стану, що суперечило :inactive-consumer"}
 {:decision :rule-ids-and-cleanup-recipe
  :status :confirmed
  :at "2026-09-16"
  :value "нові id правил з s1 прийнято; Patterns/PATTERN_CLEANUP_SYSTEM.md видаляється через vault_delete"
  :verified-by "відповідь власника на гейті s1, питання 5"
  :reason "—"}
 {:decision :completion-eviction-risk
  :status :confirmed
  :at "2026-09-16"
  :value "ризик прийнято: витіснений до читання BuildDistrictCompleteEvent лишає будівництво на нулі; страховочної звірки на TurnCompletedEvent немає"
  :verified-by "CASCADE.md s1 contra c-2; відповідь власника (а) 70"
  :reason "BuildDistrictCompletionSystem читає щотіку — 128 подій між читаннями недосяжні"}
 ;; <!-- doc-lint: off --> — майбутні імена до етапу code
 {:decision :s2-gate
  :status :confirmed
  :at "2026-09-16"
  :value {:q1 "IEventTag лишається, виняток у законі суфікса …Tag (c-3)"
          :q2 "споживачі без якоря реалізують IUpdatedSystem напряму, нової бази немає"
          :q3 "DrainBatch на EventReader для п'яти «раз на пачку» (c-8)"
          :q4 "Reactive прибирається з SystemRoleKind; FM1002 і FM1005 знімаються; маркер лише PerFrame"
          :q5 "нові id :event/clear-all і :event/aot-reader прийнято"
          :q6 "BuildDistrictCompleteEvent стає public (c-1)"
          :q7 "InitializationSystem без таблиці — відхилення коментарем (c-4)"
          :q8 "InitializationState і MapCreationState — переписати лише коментарі (c-7)"
          :q9 "AppStateRequestedEvent у Boot.Implementation/Events; файли Boot.Core Components/Tags/Archetypes видаляються БЕЗ .meta — Unity прибере їх сам; Debug.Log «[skh]» прибирається"
          :q10 "fmgraph визначає читача за полем EventReader; не-система з читачем — ребро consumes без ролі"
          :q11 "код — чотири слайси (kernel → consumers → rules → checks), кожен на свіжому Sonnet-агенті"
          :c-2 "guard, що кидає виняток, якщо схема Friflo не бачить компонент через IEventTag + запуск власника"
          :c-9 "реакція, що піднімає свій же тип, дописується в :event/one-way"}
  :verified-by "CASCADE.md # Gate і # Contra s2; відповідь власника: «за рекомендаціями, .meta не видаляй unity сам це зробить, [skh] прибери, го але код пишуть sonnet агенти»"
  :reason "рекомендації агента прийнято; два відхилення власника — .meta і модель коду"}
 ;; <!-- doc-lint: on -->
 {:decision :stage-runner
  :status :confirmed
  :at "2026-09-16"
  :value "кожен етап каскаду виконує окремий Opus-агент у свіжому контексті, запущений з основної розмови; результат чекаємо в ній; гейти лишаються за власником"
  :verified-by "пряме слово власника: «каскад запускай на opus агенті і чекаємо його результат тут»"
  :reason "відхилення від stage-isolation :mechanism (\"subagents are not the mechanism\") і від адаптера (:subagents :abandoned) — за рішенням власника; ізоляцію зберігає те, що агент читає лише артефакт етапу"}]
```

# Disproven

```clojure
[]
```

# Attempted

```clojure
[]
```

# Progress

```clojure
{:status :closed-by-owner
 :completed #{"read-back" "converge" "research pass 1" "research pass 2" "CONTEXT" "s1" "s2" "code :kernel" "code :consumers" "code :rules" "code :checks"}
 :current "закрито власником 2026-09-17 до Unity-перевірки; код закомічено"
 :remaining #{"Unity-перевірка власника — лишається за власником"}
 :stage :converge
 :next-invocation "—"
 :resume-context "паузована задача AppStateRunner чекає Q3–Q6 (MainMenu→MapCreation, осідання MapCreation, сід Gameplay, VContainer дублікати); Q1 = AppStateRunner, Q2 = стартовий ланцюжок веде Boot"}
```

# Acceptance

```clojure
[{:meter "mcp__roslyn__get_diagnostics" :target "чисто на змінених файлах" :actual "реальних помилок у файлах слайсів немає; решта — застарілий csproj до реімпорту Unity" :status :partial}
 {:meter "/arch-check" :target "без нових порушень" :actual "EcsExtensions і Boot — порушень, внесених зміною, немає" :status :met}
 {:meter "fmgraph.py check" :target "без попереджень на змінених файлах" :actual "0 попереджень на файлах задачі; per_frame/reactive ролі як вирішено" :status :met}
 {:meter "fantasymayor-rules-conformance" :target "no :diverges, :incomplete, :uncarried" :actual "0 / 0 / 0" :status :met}
 {:meter "cascade converge" :target "contradicts 0, unrequested 0" :actual "present 124, partial 0, contradicts 0, unrequested 0" :status :met}
 {:meter "Unity, запуск власника" :target "ланцюжки подій працюють як раніше" :actual ? :status :pending}]
```

# Amendments

```clojure
[]
```
