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
  контексту (Patterns/, module MDs, конвенції);
- **функціональні форми** (`->`, `cond`) виражають дію і послідовність легше, ніж
  С#-подібний опис кроків.

Повна формалізація — пастка: нотація, що виражає всю семантику, стає мовою
програмування, і задачу довелося б «писати двічі». Нуль питань від агента — фальшива
ціль; ціль — щоб питання цілились у справді невирішене.

## 2. Атоми — межа якір/проза

| Атом | Що це | Приклад |
|---|---|---|
| `:keyword` | самозначуща мітка: вердикт, enum, ключ мапи; можна з namespace | `:not-enough-gold`, `:economy/ap` |
| голий символ | **літеральний якір** — точне ім'я з коду/репо, агент бере дослівно | `DistrictBuiltEvent`, `PATTERN_CONFIG` |
| `"рядок"` | **проза** — опис, який агент має право тлумачити і перепитати | `"спавнити вьюху району"` |
| число | значення як є | `600`, `0.5` |

**Головне правило нотації: лапки маркують розмите.** Все нечітке живе в лапках і ТІЛЬКИ
в лапках; ім'я, яке вже вирішене, пиши голим символом. `:listen DistrictBuiltEvent` —
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
стартом: `{:event ^:new DistrictBuiltEvent}`.

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
  :pattern :decided :off-limits`. Відповідність блокам шаблона: `:where` = «Працюй
  тільки в», `:off-limits` = «Не дивись», `:pattern` = «Роби за шаблоном», `:decided` =
  «Архітектурні рішення», `:skip`+`:result` = «Не потрібно»+«Результат».
- **Ключі вільні й самоописові** — нові не потребують канонізації, їх покриває правило
  мапи. Але тримайся ядра, де воно підходить: синонімія (`:do` vs `:implement`) на
  великому батчі почне дрейфувати.
- **Відсутній ключ = діра = питання агента.** Гейт шаблону діє без змін: нема поля —
  агент питає, не вигадує; питання цілиться у конкретну мапу.
- Межі мапи = межі deliverable: декомпозиція вже зроблена постановкою.

Потік усередині `:goal` — рядком (`"клік → подія → ентіті"` — стрілка в лапках це
вільний текст) або threading-формою, коли це чистий конвеєр імен.

## 7. Як читаються правила в доках

Rules-блоки `ARCHITECTURE.md`, `Patterns/*`, module MDs — ті самі форми:

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
  :do "маленька реактивна система в Actions: Set() DistrictTypeComponent на draft-сутності"
  :result "draft = єдине джерело правди вибору (PATTERN_TRANSACTION_ENTITY :state)"}

 {:task :ui-reads-draft
  :where Assets/Presentation/UI/DistrictBuild/Systems
  :do "секції читають draft (With<BuildDistrictActionTemplateTag> + DistrictTypeComponent) замість world component"
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
`(def subject {…})`, `!`-суфікс, `?`, `:by-<джерело>`, `:keyword`-посилання на
`:task`-id, `^:meta`-теги, констрейнт-ключі, `;;`.

**На полиці — крадемо, коли знадобиться:**
- деструктуринг `{:keys [goal where]}` — компактне «мені потрібні саме ці поля»;
- `#_` reader-discard — «закоментувати форму», лишивши її валідною;
- `quote`/`'form` — явне «це дані, не інструкція», якщо колись знадобиться розрізняти.

Правило для експериментів: дозволено все, що парситься І читається однозначно вголос;
контрактом нова форма стає через рядок у канонічній таблиці (`~/.claude/CLAUDE.md`) —
без рядка там вона лишається здогадкою агента.
