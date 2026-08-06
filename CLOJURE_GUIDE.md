---
category: B
read: reference
tags: [docs, clojure, guide]
related:
  - "[DOC_STANDARD](DOC_STANDARD.md)"
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# Як читати і писати Clojure-інструкції

Людський підручник до Clojure-нотації задач і правил: реальний синтаксис Clojure як мова
інструкцій — атоми і межа якір/проза, колекції, форми, постановка задач батчем мап,
приклади. Нормативна специфікація для агентів — `DOC_STANDARD.md` → Rule Style.

Канонічний глосарій нотації — ОДИН, у `~/.claude/CLAUDE.md` → «Clojure instruction
notation»: самодостатня таблиця (форма + читання + приклад), яку автоматично бачить
кожен агент і сабагент у КОЖНОМУ проєкті. Контракт живе ТІЛЬКИ там; цей гайд — особистий
підручник користувача, жоден агентський док на нього не посилається. Нова форма чи
літерал: рядок у каноні — обов'язково, розгорнутий розділ тут — для себе.

## 1. Філософія: мова інструкцій, не програма

Задача чи правило ставиться у ДВОХ рівноправних формах: проза або **Clojure**. Це
реальний синтаксис Clojure — усе парситься в Clojure-едіторі — але **семантика
інструкційна**: ніщо не обчислюється, частина листів навмисно абстрактна. Критерій
правильності — прочитати форму вголос.

Чому саме цей мікс працює:
- **строгий синтаксис** дає структуру: межі мап = межі deliverable, відсутній ключ =
  видима діра, у яку агент цілить питання;
- **абстрактні листи в лапках** дають свободу: не треба формалізувати семантику
  поведінки — «зроби подію, напиши систему, тип ось такий», решту агент добирає з
  контексту (Patterns/, конвенції, коментарі в коді);
- **функціональні форми** (`->`, `cond`) виражають дію і послідовність легше, ніж
  С#-подібний опис кроків.

Повна формалізація — пастка: нотація, що виражає всю семантику, стає мовою
програмування, і задачу довелося б «писати двічі». Нуль питань від агента — фальшива
ціль; ціль — щоб питання цілились у справді невирішене.

## 2. Атоми — межа якір/проза

| Атом | Що це | Приклад |
|---|---|---|
| `:keyword` | самозначуща мітка: вердикт, enum, ключ мапи; можна з namespace | `:not-enough-gold`, `:economy/ap` |
| голий символ | **літеральний якір** — точне ім'я з коду/репо, агент бере дослівно | `DistrictBuildConfirmedEvent`, `PATTERN_CONFIG` |
| `"рядок"` | **проза** — опис, який агент має право тлумачити і перепитати | `"спавнити вьюху району"` |
| число | значення як є | `600`, `0.5` |

**Головне правило нотації: лапки маркують розмите.** Все нечітке живе в лапках і ТІЛЬКИ
в лапках; ім'я, яке вже вирішене, пиши голим символом. `:listen DistrictBuildConfirmedEvent` —
нуль інтерпретації; `:goal "подія-сигнал: район побудовано"` — агент тлумачить. Це
symbols-vs-strings різниця Clojure, зроблена семантичною.

## 3. Колекції

| Форма | Читається | Коли |
|---|---|---|
| `{:k v, :k2 v2}` | поля/факти одного суб'єкта; кома = пробіл | робоча конячка: задачі, правила |
| `[a b c]` | упорядкований список / батч | декомпозиція задач, переліки |
| `#{a b}` | рівноцінні альтернативи, «одне з» | заміна старому «або» |

Вкладеність — глибина ≤ 2: вкладена мапа = поля конкретного запису. Третій рівень
означає, що ти описуєш структуру даних, а не інструкцію — розбий на два суб'єкти.

## 4. Форми

**`(cond test result …)`** — розгалуження: тести згори вниз, перший істинний виграє,
`:else` — дефолт. Невизначений предикат читається як прозова умова; `!` у імені дії —
Clojure-конвенція «мутує світ»:

```clojure
(cond
  (< gold cost)        :not-enough-gold
  (district-occupied?) :tile-busy
  :else                (build-district!))
```

**`(-> a b c)`** — конвеєр: «a породжує b, b породжує c». У Clojure threading-макрос
протягує значення крізь функції; у нас — той самий потік як інструкція:

```clojure
(-> confirm-click DistrictBuildConfirmedEvent BuildDistrictActionSystem committed-entity)
```

**`(def subject {…})`** — іменований блок правил у доках: «сталі правила <суб'єкта>
такі». Це те, чим написані Rules-блоки в `ARCHITECTURE.md` і `Patterns/PATTERN_*.md`:

```clojure
(def place
  {:domain-rule "Assets/Domains/<Domain>/<Feature>/"
   :hud         "Assets/Presentation/UI/<Window>/"})
```

**`^:meta`-теги** — декорація наступної форми: `^:new` = створити (на відміну від
існуючих імен-контексту), `^:optional` = nice-to-have, `^:risky` = обговорити перед
стартом:

<!-- doc-lint: off — приклад ^:new: якір навмисно НЕ існує, в цьому його сенс -->
```clojure
{:event ^:new DistrictDemolishedEvent}   ;; ^:new = «створи, цього типу ще нема»
```
<!-- doc-lint: on -->

**`(:label payload …)`** — розмічений вузол інструкції (канонізовано 2026-08-05):
keyword називає РОЛЬ вузла, payload читається, але не виконується. Один рядок канону
покриває всю родину міток — `:step-N`, `:flow-N`, `:conclusion-N`, `:assumption`,
`:question`, будь-яка самоописова мітка; нумерація довільна. Сусідні мітки задають
структуру сценарію — кроки, розгалуження, висновки, припущення — не перетворюючи його
на програму; payload може бути прозою, якорем або формою (`->`, `cond`):

```clojure
[(:step-1 "на початку ходу зменшити лічильник будівництва")
 (:step-2 (-> NextTurnEvent BuildDistrictTurnsComponent "decrement via AddComponent"))
 (:flow-1 "лічильник дійшов нуля?")
 (:conclusion-1 (cond (zero? turns) DistrictBuildConfirmedEvent
                      :else         :wait-next-turn))
 (:assumption "системи ходу виконуються послідовно, не паралельно")
 (:question "чи скидається лічильник при скасуванні будівництва?")]
```

Читання вголос: «крок 1 — …; крок 2 — NextTurnEvent веде до декременту компонента;
розвилка — …; висновок — якщо нуль, подія підтвердження, інакше чекати; припущення —
…; відкрите питання — …». Мітка = адреса вузла: на неї можна послатись у розмові
(«у :flow-1 забув про cancel») так само, як `:listen` посилається на `:task`-id.

## 5. Спеціальні значення поля

| Значення | Читається |
|---|---|
| `?` | поле навмисно відкрите — агент ПИТАЄ, не вигадує |
| `:by-<джерело>` | відкрите, але джерело рішення назване: агент пропонує за <джерелом>, я вето |
| `:keyword` (id сусідньої мапи) | посилання на таску батча за її `:task`-id |

`?` і `:by-…` — брати: перший каже «спитай мене», другий — «не питай, запропонуй
звідти». Констрейнт-ключі для інваріантів у доках: `:requires :never :must-not
:contains :only-when :exists-only-under :in` — значення = обмеження.

## 6. Постановка задачі — батч мап

Основна форма: **одна мапа = одна механіка**, вектор мап = батч (перевірено 2026-07-08
на 6-тасковому батчі district-view: «стало легше описувати, що саме я хочу»):

```clojure
[{:task :add-event
  :goal "подія-сигнал: район побудовано"
  :where Actions.BuildDistrictAction.Events}

 {:task :create-spawner-system
  :listen :add-event                ;; ← посилання на сусідню мапу за :task-id
  :pattern PATTERN_REACTIVE_SYSTEM  ;; ← якір у рецепт — агент бере how звідти
  :name :by-naming-policy           ;; ← агент пропонує за політикою, я вето
  :do "реактивна система за шаблоном"
  :skip "AP-spending"
  :result "пульс події → префаб району на гексі"}]
```

- **Ядро ключів** (з живого узусу): `:task :goal :where :listen :do :skip :result
  :accept :pattern :decided :off-limits`. Відповідність блокам шаблона: `:where` = «Працюй
  тільки в», `:off-limits` = «Не дивись», `:pattern` = «Роби за шаблоном», `:decided` =
  «Архітектурні рішення», `:skip`+`:result` = «Не потрібно»+«Результат»; `:accept` =
  вимірюване доповнення «Результату» (§9).
- **Ключі вільні й самоописові** — нові не потребують канонізації, їх покриває правило
  мапи. Але тримайся ядра, де воно підходить: синонімія (`:do` vs `:implement`) на
  великому батчі почне дрейфувати.
- **Відсутній ключ = діра = питання агента.** Гейт шаблону діє без змін: нема поля —
  агент питає, не вигадує; питання цілиться у конкретну мапу.
- Межі мапи = межі deliverable: декомпозиція вже зроблена постановкою.

Потік усередині `:goal` — рядком (`"клік → подія → ентіті"` — стрілка в лапках це
вільний текст) або threading-формою, коли це чистий конвеєр імен.

## 7. Як читаються правила в доках

Rules-блоки `ARCHITECTURE.md`, `Patterns/*`, `Flows/*` — ті самі форми:

```clojure
(def role-invariants
  {:startup-bulk-work #{pipeline-stage sub-system}   ;; set = «одне з, рівноцінно»
   :per-frame-system  {:requires "written justification"}
   :sub-system        {:exists-only-under orchestrator}})
```

Читання: «startup-bulk-work — це pipeline-stage або sub-system; per-frame система
завжди вимагає письмового обґрунтування; sub-system існує лише під оркестратором».
Підстав «завжди/ніколи» до констрейнт-ключа — і рядок читається вголос. `;;` після
запису — «чому»; якщо «чому» не влазить в одне підрядне, це design decision і воно живе
прозою, не в блоці.

Дисципліни-близнюки в одному доку — дві `def`-мапи поруч з однаковими ключами, різниця
читається як діф (`routing` vs `non-routing` у PATTERN_POLYMORPHIC_CATALOGUE).

## 8. Як писати

1. Скажи інструкцію вголос одним реченням.
2. Вибери форму: факти/поля → мапа; послідовність → `->`; розгалуження → `cond`;
   перелік → вектор; альтернативи → set.
3. Вирішені імена — голими символами (якорі); нечітке — в лапки; те, що агент має
   запропонувати — `?` або `:by-<джерело>`.
4. Додай `;; чому` там, де правило неочевидне.
5. Перечитай уголос. Не складається в речення — форма вибрана криво.

**Що НЕ конвертувати у форми:** інтент (навіщо модуль існує), rationale (чому так
спроєктовано), поведінкові контракти (side-effects, live-vs-copy, порядок викликів) —
нюанс і є корисним навантаженням, форма його вб'є. Проза лишається прозою.

## 9. Приклади — жанри

**Конфіг на ScriptableObject:**

```clojure
{:task :district-cost-config
 :goal "хардкод вартості → конфіг"
 :pattern #{PATTERN_CONFIG PATTERN_CONFIG_LOADER}
 :where Domains/DistrictBuild/Configs/
 :decided "компонент тримає ПОСИЛАННЯ на живий SO, не flattened-копію"
 :name ^:new DistrictBuildCostConfig       ;; ім'я повторює фічу повністю, не голе CostConfig
 :skip "міграція старих асетів"
 :result "SO + компонент + вартість читається з конфігу"}
```

**Багфікс — `?` лишає scope відкритим:**

```clojure
{:task :fix-ap-reset
 :goal "кінець ходу: AP не скидаються до max"
 :where ?                                  ;; не знаю, де живе reset — знайди і запропонуй
 :decided "БЕЗ рефактору turn-циклу — тільки фікс"
 :skip "нові тести"
 :result "1-2 рядки правки + пояснення причини"}
```

**Потік як threading (чистий конвеєр імен):**

```clojure
{:task :district-build-flow
 :goal (-> confirm-click DistrictBuildConfirmedEvent BuildDistrictActionSystem committed-entity)
 :skip "AP-spending"
 :result "ланцюжок видно в ecs-graph"}
```

**Задача з вимірюваним прийманням — `:accept` (розгорнуто в §13):**

```clojure
{:task :fix-tag-law-deviations
 :goal "прибрати подвійні identity-теги: другий tag → kind/state enum"
 :where Assets/Domains/
 :pattern PATTERN_TAG
 :result "кожна сутність несе рівно один identity-tag"
 :accept {:meter ecs-graph :target "tag deviations: 12 → 0"}}  ;; приймання вимірюване, не «на око»
```

**Рантайм-правило з розгалуженням:**

```clojure
(cond
  (< gold cost)        :not-enough-gold
  (district-occupied?) {:verdict :tile-busy :do "show popup"}
  :else                (build-district!))
```

Живі приклади правил для читання: `ARCHITECTURE.md`, будь-який `Patterns/PATTERN_*.md`
→ Rules.

> **Історичний приклад.** Батч нижче — реальна постановка з 2026-07, яку згодом скасував півот
> (draft-механіку видалено з кодової бази; згадані типи більше не існують). Читай його як
> ілюстрацію НОТАЦІЇ, не як актуальні якорі коду.

<!-- doc-lint: off — історичний приклад, якорі навмисно мертві -->
```clojure
[{:task :relocate-selection-command
  :goal "команда вибору стає доменною подією"
  :where #{Assets/Domains/Actions/BuildDistrictAction/Events
           Assets/Presentation/UI/DistrictBuild}
  :do "DistrictBuildSelectionRequestedEvent переїжджає в Domains.Actions.BuildDistrictAction.Events + identifying payload DistrictType"
  :decided "Actions НЕ МОЖЕ споживати подію з Presentation.UI (asmdef-напрямок) — тому дім команди мусить бути verb-домен, як у Started/Confirmed/Cancelled"}

 {:task :draft-updater
  :listen :relocate-selection-command
  :pattern PATTERN_REACTIVE_SYSTEM
  :name :by-naming-policy                       ;; пропозиція: BuildDistrictTemplateSelectSystem
  :do "маленька реактивна система в Actions: AddComponent DistrictTypeComponent на draft-сутності"
  :result "draft = єдине джерело правди вибору (PATTERN_TRANSACTION_ENTITY :state)"}

 {:task :ui-reads-draft
  :where Assets/Presentation/UI/DistrictBuild/Systems
  :do "секції читають draft (архетип BuildDistrictActionTemplateTag + DistrictTypeComponent) замість singleton-компонента"
  :decided "draft від народження = DistrictType.None, ніколи Unknown (Unknown лишається error-маркером)"}

 {:task :purge-old-home
  :do "видалити DistrictBuildSelectionComponent з Economy; прибрати re-stamp на confirm у BuildDistrictActionSystem; DistrictBuildStartedEvent втрачає Type (лишається HexCoord)"
  :result "grep DistrictBuildSelectionComponent == 0; :started-before-populate інваріант мертвий"}

 {:task :sync-flow-contract
  :where #{Flows/FLOW_DISTRICT_BUILD.md .ecs-graph}
  :do "закрити gap 1 у FLOW-доку (state-ownership :now, event-таблиця, інваріанти) + build_graph.py"
  :skip "модульні MD — куратору за milestone-каденцією"}]   ;; NB: module-MD і цей каденс скасовано 2026-07-09 (tool-first)
```
<!-- doc-lint: on -->

## 10. Канон і полиця

**Канонізовано** (кожне має рядок у глобальній таблиці): мапа, вектор, set `#{}`,
`:keyword` (+ namespaced), символ-vs-рядок (якір/проза), `(cond)`, `(-> …)`,
`(def subject {…})`, `(:label payload …)`-вузол, `!`-суфікс, `?`, `:by-<джерело>`,
`:keyword`-посилання на `:task`-id, `^:meta`-теги, констрейнт-ключі, `:accept`, `;;`.

**На полиці — крадемо, коли знадобиться:**
- деструктуринг `{:keys [goal where]}` — компактне «мені потрібні саме ці поля»;
- `#_` reader-discard — «закоментувати форму», лишивши її валідною;
- `quote`/`'form` — явне «це дані, не інструкція», якщо колись знадобиться розрізняти.

Правило для експериментів: дозволено все, що парситься І читається однозначно вголос;
контрактом нова форма стає через рядок у канонічній таблиці (`~/.claude/CLAUDE.md`) —
без рядка там вона лишається здогадкою агента.

## 11. Розбір реальної правки — before→after (2026-07-10)

Жива ілюстрація §2/§4/§6 на твоєму власному district-build батчі. Дві нотаційні шорсткості
й чому вони важать.

**Правка 1 — унікальний `:task`-id.** Було два різні deliverable під одним id:

<!-- doc-lint: off — навчальний приклад, частина якорів навмисно ще не існує -->
```clojure
;; ❌ було — дві різні механіки, однаковий :task
[{:task :add-turns  :goal "будувати за N ходів" #_…}
 {:task :add-turns  :goal "витратити ресурси"   #_…}]

;; ✅ стало — унікальні id, кожну можна адресувати з :listen
[{:task :build-turns     :goal "будувати за N ходів" #_…}
 {:task :spend-resources :goal "витратити ресурси"    #_…}]
```

Чому (§6): сусідні мапи посилаються за `:task`-id — `:listen :build-turns`. Дубль робить
`:listen :add-turns` неоднозначним: на яку з двох? Id = адреса, адреса мусить бути одна.

**Правка 2 — `^:new Symbol` замість `:keyword` для типу, що створюється:**

```clojure
;; ❌ було — :keyword натякає на «мітку/вердикт», а мається на увазі новий C#-тип
(:step-4 "Add :TurnsComponent for turns to build district")

;; ✅ стало — голий символ = якір + ^:new = «створити, ще не існує»
(:step-4 "+ ^:new TurnsComponent = ходи до завершення")
```
<!-- doc-lint: on -->

Чому (§2 + §4): `:keyword` — самозначуща мітка (`:not-enough-gold`). Клас, який треба
написати, — код-якір: голий символ + `^:new`. Різниця не косметична: `^:new` каже агенту
«створи», а `:keyword` він прочитає як існуючий ярлик — і тип не з'явиться.

**Нюанс, який лишили як є.** Усе в межах `"…"` — проза (§2: лапки маркують розмите), тож
символ чи `^:new` усередині рядка-кроку декоративні. Строга форма винесла б якорі за лапки;
для чорнового плану рядок-крок читабельніший — свідомий компроміс, не помилка.

## 12. ECS entity через компоненти — :pk/:fk/:tag/:kind/:state/:data (2026-07-16)

Не нова форма — та сама `(def subject {…})`-мапа з §4, застосована до предметної області
«entity + його компоненти», щоб описувати мені (і я тобі) сутність одним поглядом замість
переказу composition прозою. Ключі мапляться на вже decreed `key-role-law` / `tag-law`
(`ARCHITECTURE.md`), тому окремих рядків у таблиці форм не потребують (§10: нові ключі мапи
покриває правило мапи). Єдина справжня семантична добавка — читання `#{}` у колекційних
полях (`:data`, `:fk` → «УСІ члени, невпорядковано», а не глобальне «одне з») — канонізована
2026-07-17 як project-scoped розширення: блок `notation-ecs-ext` у проєктному `CLAUDE.md`
(кардинальність поля вирішує читання; глобальний канон дозволяє такі розширення явним
carve-out'ом). ECS-специфіка живе в проєкті, не в глобальній таблиці.

```clojure
(def entity-shape
  {:archetype "ім'я — довідково, не тип"
   :tag       "РІВНО ОДИН голий символ — identity tag (Tag Law); НІКОЛИ set"
   :pk        "власна ідентичність рядка в СВОЄМУ ключовому просторі (…IdComponent)"
   :fk        "посилання в ЧУЖИЙ ключовий простір (…FKComponent); #{} якщо їх декілька"
   :kind      "enum-компонент-підтип (self-index дискримінатор), не другий tag"
   :state     "enum-компонент-стан (лічильник стадії), change-only AddComponent"
   :data      "прості значення-атрибути → #{} (порядок не важливий)"})
```

П'ять живих прикладів з коду (2026-07-16, взяті через `ecsg.py explain`), від найпростішого
до найбільшого — і один навмисно ЗЛАМАНИЙ, щоб бачити, як форма ловить порушення.

<!-- doc-lint: off — приклади містять FM-11 target-типи, яких ще нема (HexIdFKComponent,
     DistrictTypeFKComponent, DistrictOpenConditionKindComponent/StateComponent) -->
```clojure
;; 1. базова "чиста" сутність (Map)
(def Hex
  {:archetype Hex
   :tag       HexTag
   :pk        HexIdComponent
   :kind      HexTypeComponent    ;; легасі назва — грає роль kind (self-index за HexTag), не …KindComponent
   :data      #{HexLevelComponent}})

;; 2. FK-конфляція, яку лагодить FM-11 (S4a)
(def HexResource
  {:archetype HexResource
   :tag       HexResourceTag
   :fk        HexIdComponent      ;; ^:migrating — сьогодні той самий тип, що й Hex-PK; ціль S4a: HexIdFKComponent
   :data      #{HexResourceComponent}})

;; 3. актор із kind-маркером і кількома data
(def Mayor
  {:archetype Mayor
   :tag       MayorTag
   :pk        MayorIdComponent
   :kind      ActorTypeComponent  ;; спільний з City — категорійний маркер (не другий tag)
   :data      #{MayorAPComponent MayorAPRestoreComponent}})

;; 4. найбільша — транзакційна сутність (PATTERN_TRANSACTION_ENTITY)
(def BuildDistrictActionTransaction
  {:archetype BuildDistrictActionTransaction
   :tag       BuildDistrictInProgressTag  ;; STAGE tag — swap на наступний tag при переході стадії
   :pk        ActionIdComponent
   :fk        #{HexIdComponent DistrictTypeComponent}  ;; ^:migrating — обидва Data-типізовані сьогодні; ціль S4a/S3: HexIdFKComponent + DistrictTypeFKComponent
   :data      #{BuildDistrictTurnsComponent ActorTypeComponent}})  ;; ActorTypeComponent тут = "хто платить" (Payer), не kind-маркер

;; 5. ЖИВИЙ приклад порушення — для розпізнавання смороду
(def DistrictSingleOpenCondition
  {:archetype DistrictSingleOpenCondition
   :tag       #{DistrictOpenConditionTag DistrictSingleOpenConditionTag}  ;; ⚠ ILLEGAL — 2 identity tags = Tag Law violation (1 з 12 deviations, FM-11 S3 лагодить)
   :fk        DistrictTypeComponent})  ;; ^:migrating — сьогодні той самий тип, що й District-факт; ціль S3: DistrictTypeFKComponent + kind SingleOpen + state
```
<!-- doc-lint: on -->

Читання: `:tag` як `#{...}` замість голого символу — це і є сигнал помилки (приклад 5), не
альтернативний синтаксис; `set` тут означає «насправді їх два, а мало бути одне». `:fk` як
`#{...}` (приклад 4) — легальний випадок: одна сутність може нести кілька посилань у різні
чужі простори одночасно.

## 13. Вимірюване приймання — `:accept` (2026-07-17, запозичено з SDD)

Єдине запозичення з мейнстрімного SDD після порівняння процесів: acceptance criterion
як першокласне поле задачі. У SDD цю роль грають тести; тут тестів нема — вимірювачем
служать інструменти репо. Розподіл ролей: `:result` описує результат для людини,
`:accept` дає машинну перевірку. **Done = вимірювач показує ціль**, а не «виглядає
готовим». Канонічний рядок — у глобальній таблиці (`~/.claude/CLAUDE.md`), як і все з §10.

Форма значення:

```clojure
:accept {:meter <якір-інструмента> :target <покази>}   ;; один критерій
:accept [{:meter …} {:meter …}]                        ;; кілька критеріїв = вектор мап
```

- `:meter` — голий символ = якір на реальний вимірювач. Живі вимірювачі репо:
  `ecs-graph` (key-конфляції, tag-відхилення, archetype-факти), `doc_lint`
  (ghost-символи в доках), `arch-check` (3 арх-заборони), `grep` (повнота
  видалення/перейменування), `asmdef_reach` (шари/залежності).
- `:target` — покази, при яких задача прийнята: число (`0`) або рядок-відлік
  (`"tag deviations: 12 → 0"` — стрілка в лапках = вільний текст, §2).

Як ключ працює в циклі задачі:

1. **Постановка** — done визначено ДО початку роботи: агент знає, чим його міряти,
   і не може перевизначити критерій під те, що вийшло.
2. **Фініш Execute** — агент сам запускає вимірювач і показує фактичні покази поруч
   із ціллю. Покази ≠ ціль → задача НЕ done; «майже досягнуто» не існує.
3. **Гейт** — опціональний, як `:pattern`: нема природного вимірювача — нема ключа;
   відсутній `:accept` ніколи не є діркою для питання.

Коли ставити:
- міграції/рефактори з лічильником відхилень (FM-11: конфляції і tag-відхилення → 0);
- повні видалення/перейменування — `{:meter grep :target "== 0"}` замість обіцянки
  «прибрав усюди» (пара до правила «Remove completely, no deferred tails»);
- док-гігієна (`doc_lint` → 0) та арх-аудити (`arch-check` → clean).

Коли НЕ ставити: поведінкові й геймплейні задачі, де справжня перевірка — playtest.
Не вигадуй метрику заради ключа: фальшивий вимірювач гірший за відсутній — він
штампує done, бо щось порахував, а не бо результат правильний.

<!-- doc-lint: off — якорі в прикладах ілюстративні (тип уже видалений з кодової бази) -->
```clojure
;; повне видалення: критерій = grep, не обіцянка
{:task :purge-selection-component
 :do "видалити DistrictBuildSelectionComponent повністю"
 :accept {:meter grep :target "DistrictBuildSelectionComponent == 0"}}

;; кілька критеріїв — вектор мап
{:task :component-roles-migration
 :goal "PK/FK розділені, tag-закон відновлений"
 :accept [{:meter ecs-graph :target "key conflations: 3 → 0"}
          {:meter ecs-graph :target "tag deviations: 12 → 0"}
          {:meter doc_lint  :target 0}]}
```
<!-- doc-lint: on -->
