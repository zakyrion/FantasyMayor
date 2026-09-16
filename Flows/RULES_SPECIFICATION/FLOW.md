---
category: A
read: always
status: partial
tags: [architecture, rules, specification, cascade]
related:
  - "[GRAPH_STANDARD](../GRAPH_STANDARD/FLOW.md)"
  - "[ARCHITECTURE](../../ARCHITECTURE.md)"
---

# Request

```clojure
{:source :user
 :received-at "2026-09-15"
 :raw-request ["7 - можеш пошукати але що я скажу зі своєї сторони і з того що вже бачу. Покривати все документацією це марна справа, бо будь-яка зміна херить все. Більше того правила це теж свого роду тип документації і як тільки правила змінюються то гниє все: код, документи, тули.\nОтже який вихід я бачу з цього - специфікація правил. Умовно кажучи ARCHITECTURE перестає бути специфікацією, а стає носієм доповнюючих правил в agent pipeline: Codex -> ARCHITECTURE -> skills\nа отже ми повинні окремо мати повну специфікацію і наступного разу якщо захочемо щоб змінювати, то будемо це робити не в 3 місцях ручками, а в одній глобальній специфікації а все решта потім виведеться з цієї специфікації. А отже зараз потрібно зібрати цю повну специфікацію в окремий супер важливий файл і лише потім провалідувати Codex -> ARCHITECTURE -> skills на предмет відповідності."
               "1 - RULES_SPECIFICATION.md\n2 - Кожне правило, якого має дотримуватись код. А щодо DOC_STANDARD - думаю можна видаляти. Я не готовий підтримувати купу документів. Замість цього я буду покладатися на код, скіли та інструменти пошуку\n3 - Clojure\n4 - перше\n5 - перше\n6 - я думаю це буде окремий flow через cascade для створення цієї супер специфікації\n7 - шукай, відправ окремих sonnet агентів на пошук по всіх перелічених темах по одному на кожну\n8 - цей блок буде рефакторитися тому я хз що тут записати бо будь-що записане згниє"
               "1 - так\n2 - цей flow повинен бути переписаним під те щоб бути продовженим після виконання каскаду\n3 - CODE_STORY_RULES_PROPOSAL видаляй, це те що вже стало каскадом.\n4 - DOC_STANDARD можна замінити окремим скілом який буде перевіряти чи все ок з написаним документом. Я взагалі не планую більше створювати якісь додаткові документи, хіба що щось додам чи зміню в патернах. Решта має лягти на скіли та тули."
               "Запускай каскад в авто режимі в окремому агенті. Мені покажеш вже підсумок з фінального артефакту\n1 - так\n2 - так\n3 - перше\n4 - перше\n5 - після каскаду"]}
```

# Confirmed contract

```clojure
{:task :rules-specification
 :goal "RULES_SPECIFICATION.md — повна специфікація правил, яких має дотримуватись код FantasyMayor; єдине джерело, з якого надалі виводяться ARCHITECTURE.md, скіли, рецепти Patterns/, fantasymayor-graph і MarkerShapeAnalyzer"
 :path :cascade
 :where #{"RULES_SPECIFICATION.md у корені репозиторію — новий файл"
          "Flows/RULES_SPECIFICATION/ — FLOW.md, CONTEXT.md, CASCADE.md"
          "INDEX.md — лише запуском python3 Tools/gen_index.py"}
 :off-limits #{"Assets/ — лише читати"
               "код інструментів: .claude/skills/fantasymayor-graph/scripts/, Tools/MarkerShapeAnalyzer/ — лише читати"
               "правки ARCHITECTURE.md, CLAUDE.md, .sdd-flow/, скілів .claude/skills/ і ~/.claude/skills/, Patterns/ — лише читати; їх звіряє й править Flows/GRAPH_STANDARD після цього каскаду"
               "Flows/GRAPH_STANDARD/FLOW.md — лише читати"
               "DOC_STANDARD.md — лише читати; його замінить скіл після каскаду"}
 :decided #{"форма — Clojure-блоки в .md; вміст кожного блоку — валідні EDN-дані: без ^-метаданих, проза лише в рядках, reader-clean"
            "межі — кожне правило, якого має дотримуватись код; правила документів і процесу — ні"
            "кожне правило має стабільний ID; помилка аналізатора (FM…) і попередження графа посилатимуться на нього"
            "реєстрація DI (форми Register…, InstallModules) не записується — блок під рефакторингом, записане згниє"
            "ARCHITECTURE.md після — лише доповнення для агента з посиланням на специфікацію; тут не змінюється"
            "view — усе, що лежить у папці Views/"
            "маркер [SystemRole] обов'язковий там, де роль не визначає база, і заборонений там, де визначає; підписка += на C#-подію view вимагає [ViewSubscriber(typeof(V))]"
            "ознаки рецептів: у специфікації — ті, що є правилом написання коду (роди а + б); евристики впізнавання (рід в) лишаються в коді інструмента"
            "view-boundary — правило; помилка компіляції для нього не вимагається"
            "каскад для документа: CONTEXT — носії правил, знахідки й пошук; s1 — набір правил і дані, про які вони говорять (конструкції коду), без розкладки документа; s2 — структура документа: розділи, def-імена, ID правил; «код» — RULES_SPECIFICATION.md як переклад s2; read-back і converge — звірка документа з s2 і з носіями"
            "авто-режим: ворота власника (знахідки CONTEXT, після s1, після s2, вердикт read-back) без його слова; кожна стадія — окремий агент з чистим контекстом, послідовно; власник бачить підсумок фінального артефакту"}
 :sources #{"ARCHITECTURE.md"
            ".claude/skills/fantasymayor-placement/SKILL.md"
            ".claude/skills/fantasymayor-pattern-choice/SKILL.md"
            "~/.claude/skills/arch-check/SKILL.md"
            "Patterns/*.md — блоки правил (Rules, When, Anti-patterns)"
            ".claude/skills/fantasymayor-graph/SKILL.md і references/ — графові факти, ролі, маркери"
            ".claude/skills/fantasymayor-graph/scripts/roles.py tag_law.py recipes.py view_pairs.py ecs_facts.py — правила, які читає інструмент"
            "Tools/MarkerShapeAnalyzer/*.cs — FM1001-FM1004"
            "Assets/Scripts/EcsExtensions/SystemRoleAttribute.cs ViewSubscriberAttribute.cs TagLabelAttribute.cs TagLabelRole.cs SystemRoleKind.cs"
            "CLAUDE.md § 5 notation-ecs-ext — перевірити, чи це правило коду"}
 :read #{"Flows/GRAPH_STANDARD/FLOW.md — # Findings і # Decisions (інвентар і зовнішній пошук)"
         "ARCHITECTURE.md"}
 :skills {fantasymayor-pattern-choice "не застосовний — код гри не змінюється"
          fantasymayor-placement      "не застосовний — нових файлів коду нема"}
 :tools #{fantasymayor-graph roslyn doc_lint}
 :accept [{:meter "python3 Tools/doc_lint.py --quiet" :target "0 синтаксичних помилок Clojure; привиди не зросли (3)"}
          {:meter "python3 Tools/gen_index.py" :target "LINT clean"}
          {:meter "інвентар носіїв" :target "кожне правило кожного носія з :sources — у специфікації з ID або з рішенням «не правило коду»"}
          {:meter "ID правил" :target "кожне правило має ID; дублів 0"}
          {:meter "converge" :target "contradicts 0, unrequested 0"}]
 :result "RULES_SPECIFICATION.md написана; власник бачить підсумок"}
```

# Plan

```clojure
(-> (:stage-context   "CONTEXT.md — окремий агент")
    (:stage-s1        "CASCADE.md # s1 — окремий агент")
    (:stage-s2        "CASCADE.md # s2 і # Contra — окремий агент")
    (:stage-code      "RULES_SPECIFICATION.md + gen_index + doc_lint — окремий агент")
    (:stage-read-back "CASCADE.md # Read-back — окремий агент")
    (:stage-converge  "CASCADE.md # Converge і # Calibration — окремий агент")
    (:summary         "головний агент показує власнику підсумок RULES_SPECIFICATION.md і рішення, прийняті в авто-режимі"))
```

# Findings

```clojure
[{:finding :findings-live-in-graph-standard
  :at "2026-09-16"
  :fact "Інвентар носіїв правил (11 знахідок проходу 1) і зовнішній пошук (7 знахідок проходу 2) записані у Flows/GRAPH_STANDARD/FLOW.md # Findings; ця задача їх не повторює"
  :verified-by "записано головним агентом 2026-09-15"
  :consequence "стадія CONTEXT читає ті знахідки як факти з провенансом"}]
```

# Decisions

```clojure
[{:decision :cascade-auto
  :status :confirmed
  :at "2026-09-16"
  :value "авто-режим: стадії context → s1 → s2 → code → read-back → converge, кожна — окремий агент з чистим контекстом, послідовно; ворота власника без його слова; кожна стадія сама пише свій # Contra і в спірному місці обирає найвище оцінений варіант, записуючи його як :auto-decided з оцінкою; власник бачить підсумок фінального артефакту"
  :verified-by "власник: «Запускай каскад в авто режимі в окремому агенті. Мені покажеш вже підсумок з фінального артефакту»; прецедент — Flows/Archive/ECS_GRAPH_PATTERN_INSTANCES :path-cascade-auto, :isolation-in-auto"
  :reason "власник довіряє стадіям і дивиться лише на результат"}

 {:decision :stable-rule-ids
  :status :confirmed
  :at "2026-09-16"
  :value "кожне правило має стабільний ID, на який посилатимуться помилка аналізатора й попередження графа"
  :options [{:option "так" :confidence 75} {:option "ні, def-імен досить" :confidence 25}]
  :verified-by "власник: «1 - так»"
  :reason "зв'язок запису специфікації з перевірками; прецедент — RSPEC SonarSource, RS2000"}

 {:decision :edn-valid
  :status :confirmed
  :at "2026-09-16"
  :value "специфікація — валідні EDN-дані в Clojure-блоках: без ^-метаданих, проза лише в рядках"
  :options [{:option "EDN-валідні дані" :confidence 65} {:option "вільні форми нотації" :confidence 35}]
  :verified-by "власник: «2 - так»"
  :reason "один парсер згодом зробить проєкцію для графа і .additionalfile для аналізатора"}

 {:decision :no-di-registration
  :status :confirmed
  :at "2026-09-15"
  :value "форми реєстрації DI не записуються"
  :verified-by "власник: «8 - цей блок буде рефакторитися тому я хз що тут записати бо будь-що записане згниє»"
  :reason "блок під рефакторингом"}]
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
{:status :active
 :why-not-complete "усі шість стадій каскаду пройдені й поправки власника застосовані; лишився один приймальний метр — звірка носіїв (ARCHITECTURE.md, Patterns/, скіли, інструменти) проти цієї специфікації, яку робить Flows/GRAPH_STANDARD. Архівувати задачу до тієї звірки не можна: # Contra s2 (s2-c-2) робить вікно з живим каскадом обовʼязковим"
 :completed #{"постановка підтверджена 2026-09-16 — go на каскад в авто-режимі"
              "context 2026-09-16 — CONTEXT.md: інвентар 457 правил з 10 носіїв (366 правил коду, 43 визначення, 45 виключених), 20 суперечностей, 8 рішень :auto-decided, 3 питання власнику; doc_lint 0 помилок Clojure і 3 старі привиди, gen_index LINT clean"
              "s1 2026-09-16 — CASCADE.md # s1: 243 правила в 17 групах, 43 конструкції коду в :data, 48 виключених записів з причиною, усі 20 суперечностей розвʼязані (24 рішення :auto-decided з оцінками, 5 нових правил, 3 питання лишились :needs-owner); # Contra 8 записів; doc_lint 0 помилок Clojure і 3 старі привиди, gen_index LINT clean"
              "s2 2026-09-16 — CASCADE.md # s2: структура RULES_SPECIFICATION.md — публічний ID = тотожність s1 дослівно (реєстр 38 префіксів, закон карбування й цитування), форма запису (:id :says :governs :checked-by + 7 необовʼязкових), 17 розділів 1:1 з групами s1 у 4 частинах читання, словник 55 конструкцій, блоки відкритих читань, меж набору і звірки; 13 рішень :auto-decided з оцінками, усі 8 записів # Contra s1 розвʼязані, 7 власних записів # Contra s2; покриття 243/243, винайдено 0 правил; doc_lint 0 помилок Clojure і 3 старі привиди, gen_index LINT clean"
              "code 2026-09-16 — RULES_SPECIFICATION.md написана, 835 рядків: 30 блоків Clojure, 243 правила з унікальними ID у 17 розділах і 4 частинах, словник 55 конструкцій, позначки 25 :choice / 8 :enforcement / 3 :needs-owner / 9 :numbers / 10 :note, блоки відкритих читань, меж набору і звірки; усі 243 :says дослівно з s1, 0 винайдених і 0 загублених; 4 рішення :from-code дописані в CASCADE.md # s2; doc_lint 0 помилок Clojure і ті самі 3 привиди, gen_index LINT clean"
              "read-back 2026-09-16 — CASCADE.md # Read-back: 19 знахідок з вердиктами, 2 :fixed (словник несе 56 конструкцій, не 55 — (def tally) і проза перед ним правлені; девʼять записів без :governs, не вісім), 17 :kept-because; звірка точна — 243/243 ID, :says байт у байт із s1, :governs і :checked-by без розбіжностей, порядок ключів витриманий, 30 блоків валідні, 4 записи :from-code підтверджені; три знахідки на власника — :rb-3 розкид правил однієї конструкції по шести розділах, :rb-8 :addressable/never дублює сім сусідів, :rb-6 сімнадцять абзаців дослівно дорівнюють :does; doc_lint 0 помилок Clojure і ті самі 3 привиди, gen_index LINT clean"
              "converge 2026-09-16 — CASCADE.md # Converge і # Calibration: 49 записів s2 класифіковано проти документа перерахунком, не читанням його чисел — 39 :present, 8 :partial, 2 :contradicts, 0 :unrequested; обидва :contradicts — один факт (s2 недорахував запис словника :stack), усі чотири записи :from-code підтверджені, знайдено три неназвані рішення стадії коду і одну внутрішню розбіжність документа (преамбула тримає покажчик # s1, який (def entry-shape) уже зняв); калібрування прогону записане; doc_lint 0 помилок Clojure і ті самі 3 привиди, gen_index LINT clean"}
 :completed-last "поправки власника 2026-09-16 — CASCADE.md # Поправки власника після converge (5 записів): 55 → 56 у трьох місцях s2; :addressable/never розбито (дві заборони дістали власні ID, шість уже жили поіменно у сусідів) — правил 244; 17 абзаців-повторів знято, файл 836 → 777 рядків; додано (def gather-by-construct); три читання закриті словом власника — ключ :needs-owner, блок (def open-readings) і абзац преамбули знято"
 :current :none
 :remaining #{"звірка носіїв — Flows/GRAPH_STANDARD"}
 :stage :done
 :next-invocation :none
 :resume-context "Артефакт задачі — RULES_SPECIFICATION.md у корені (836 рядків, 30 блоків, 243 правила). Провенанс — Flows/RULES_SPECIFICATION/: CONTEXT.md (інвентар носіїв), CASCADE.md (# s1 набір і якорі, # s2 структура, # Read-back 19 знахідок, # Converge 49 класифікацій, # Calibration міри прогону). Відкритих питань чотири. Три — :needs-owner у самому документі, блок (def open-readings): :oq-stateless-vs-lifetime-flags, :oq-singleton-manifest, :oq-view-in-views-folder. Четверте — куди лягає розбіжність 55/56 у словнику конструкцій: запис :from-code у # s2 чи слово власника. Крім них власникові адресовані три знахідки read-back (:rb-3, :rb-8, :rb-6) і три неназвані рішення стадії коду з (def converge-from-code). Наступна робота — Flows/GRAPH_STANDARD: звірка ARCHITECTURE.md, Patterns/, скілів та інструментів проти цієї специфікації, поки каскад ще не архівований (s2-c-2 fix-a робить це вікно обовʼязковим)."}
```

# Acceptance

```clojure
[{:meter "python3 Tools/doc_lint.py --quiet" :target "0 синтаксичних помилок Clojure; привиди не зросли (3)" :actual "converge 2026-09-16: 0 помилок Clojure на 44 файлах; 3 привиди, усі старі й усі в Patterns/ (HexIdComponent у PATTERN_COMPONENT.md і PATTERN_TRANSACTION_ENTITY.md) — ні в RULES_SPECIFICATION.md, ні в CASCADE.md жодного" :status :met}
 {:meter "python3 Tools/gen_index.py" :target "LINT clean" :actual "converge 2026-09-16: LINT clean; INDEX.md переписано, 48 документів (5 always, 20 trigger, 23 archive), 3 canvas" :status :met}
 {:meter "інвентар носіїв" :target "кожне правило кожного носія з :sources — у специфікації з ID або з рішенням «не правило коду»" :actual "converge міряв ланку s2 → документ і взяв її повністю: 243 з 243 правил, 0 винайдених, 0 загублених. Ланку носії → CONTEXT → s1 converge не переміряв — носії й CONTEXT.md поза його читанням, і закриває цей метр звірка носіїв у Flows/GRAPH_STANDARD" :status :pending}
 {:meter "ID правил" :target "кожне правило має ID; дублів 0" :actual "після поправок власника: 244 записи, 244 унікальні ID, дублів 0; 38 префіксів — кожен у реєстрі й кожен ужитий, найрідші пʼять по одному правилу; форма :prefix/slug витримана у 243 з 243; порядок ключів без жодного порушення" :status :met}
 {:meter "converge" :target "contradicts 0, unrequested 0" :actual "unrequested 0 (у файлі нема жодного правила, дана, числа чи ключа, яких s2 не знає); contradicts 0 після поправки власника 2026-09-16: 55 → 56 вписано в s2 («впиши 56 у s2»), документ не чіпали. Поправки того ж дня — розбиття :addressable/never, зняття 17 абзаців-повторів, (def gather-by-construct) і закриття трьох читань — записані в CASCADE.md # Поправки власника після converge як :from-code" :status :met}]
```

# Amendments

```clojure
[{:received-at "2026-09-16"
  :raw-request ["впиши 56 у s2"
                "Щодо правил: якщо це дійсно проблема то тоді можна написати інструкцію як це шукати і збирати в єдине ціле. Для агента це не буде проблемою мати щось що буде вказувати на процедуру відновлення цілісності"
                "id для заборон розбивай"
                "Повтори прибирай"
                "погоджуюсь"]
  :normalized {:task :owner-amendments-after-converge
               :do (-> (:step-1 "s2 :constructs 55 → 56")
                       (:step-2 "правила однієї конструкції — процедура збирання в документі, не другий дім")
                       (:step-3 ":addressable/never розбити на власні ID")
                       (:step-4 "17 абзаців-повторів прибрати")
                       (:step-5 "три читання закрити згодою власника"))
               :result "244 правила; contradicts 0; у наборі не лишилось правила на здогадці"}
  :confirmed true}]
```
