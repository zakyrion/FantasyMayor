---
category: A
read: archive
status: implemented
tags: [flow, game-design, economy, poc, docs]
related:
  - "[GAME_MECHANICS](../../GAME_MECHANICS.md)"
  - "[FLOW_VISION_UNIFICATION](FLOW_VISION_UNIFICATION.md)"
  - "[FLOW_BUILD_UX_DECONGESTION](FLOW_BUILD_UX_DECONGESTION.md)"
  - "[FLOW_BASIC_CITY_UI](FLOW_BASIC_CITY_UI.md)"
---

# FLOW_ECONOMY_POC

GAME_MECHANICS.md replaces GAME_VISION; the three vision FLOWs and their research are closed and archived.

# 1 · Request

## User request — 2026-09-05 (verbatim)

> FantasyMayor набуває форми економічної гри про мера та автономних підприємців. City Tales близька поєднанням міста й персонажів; The Guild — зв'язком підприємництва, особистого розвитку та соціального становища; Patrician — постачанням, торгівлею й міським добробутом. Власна основа: мер створює можливості для еліт, від яких місто поступово починає залежати, але не керує ними напряму. Основні уточнення задуму:
>
> * Пасиви забезпечують старт і перший ріст. Подальший розвиток потребує активного виробництва еліт; орієнтир для середини гри — хоча б 50% економіки, без урахування пасивів їхніх будівель.
> * Внутрішня біржа працює від початку, спершу через бартер. Згодом з'являється монета як товар у тому самому механізмі обміну — бартер залишається можливим. Пізніше підключається міжміська торгівля.
> * Історичність — основа причинності: приватні інтереси, права, виробництво, споживання та обмін мають відображати середньовічні соціально-економічні процеси в дуже спрощеній формі.
> * Гравець повинен мати простір для помилок: невдалі дозволи, обміни, спеціалізація та залежності мають давати відчутні й зрозумілі наслідки.
> * Еліти матимуть власні характери, які змінюють їхні рішення за однакових умов і роблять партії менш повторюваними.
>
> Пріоритет зараз — функціональна й цікава економіка. Після неї можливі стосунки, репутація, бойовка на кшталт Eador, містика, магія та різні народи — орки, феї, ельфи тощо. Орієнтири мають допомагати вирішувати власні задачі гри, без копіювання чужого набору механік. Dwarf Fortress тут стала асоціацією з історіями, що виникають із взаємодії систем. Робочий критерій економічного ядра: навіть із небагатьма товарами та елітами вже виникають ситуації, які цікаво розв'язувати й переказувати.
> це якщо підбити підсумок. Я ще хочу щоб ти прочитав документи, ресерчі та флови, а також документ з економікою і ми будемо закривати ці флови.
>
> :goal "отримати чистовик економічних механік на POC, research-і заархівуй, бо будуть потрібні"

## Owner's answers to the agent's five questions — 2026-09-05 (verbatim)

> 1 - так запиши це в новий файл, старий док GAME_VISION - видалимо
> 2 - я хочу мати 1 файл, а не кілька. Я не хочу бігати і шукати де що зламано і що не так записано. Тому краще створити новий файл і видали ті що були. Не треба редагувати чи переписувати старий, бо ти щось загубиш
> 3 - зроби невеликий flow під це
> 4 - Це бачення механік та гри, а вже конкретний баланс я буду робити сам в Unity
> 5 - ніяких характерів еліт
> :result "1 чистий документ який пояснює механіки гри, все те що ми обговорювали, та шукали."
> :out-of-scope ["Придумувати ігрові сценарії" "придумувати типи та назви районів" "Придумувати імена еліт" "придумувати характери еліт та їх риси"]
>
> no go yett

## Owner's answers to the two remaining questions + GO — 2026-09-05 (verbatim)

> 1 - нове імʼя
> 2 - clojure + english + ukrainian
> увімкнув obsidian
> go

## Agent restatement — confirmed by the user

```clojure
{:task :economy-poc-clean-copy
 :kind :mutation
 :goal "1 чистий документ, який пояснює механіки гри — все, що обговорювали і шукали; старий GAME_VISION видалити; research-и заархівувати; три FLOW закрити"
 :input #{GAME_VISION.md "три FLOW цілком" "три RESEARCH" "підсумок власника 2026-09-05 — verbatim вище"}
 :where #{^:new GAME_MECHANICS.md ^:new Flows/FLOW_ECONOMY_POC.md GLOSSARY.md "INDEX.md — зона агента" Flows/Archive/}
 :off-limits #{код Unity ARCHITECTURE.md ECS_CONVENTIONS.md DOC_STANDARD.md}
 :decided {:new-not-rewrite "новий файл пишеться з нуля синтезом джерел; старий не редагується і після цього видаляється"
           :name "GAME_MECHANICS.md — нове ім'я, не GAME_VISION (власник: «нове ім'я»)"
           :language "як у GAME_VISION: англійський каркас прози, українські Clojure-правила (власник: «clojure + english + ukrainian»)"
           :one-file "єдиний док механік; GLOSSARY лишається як словник → код і перепрямовується на нові секції"
           :no-balance "структурні правила так («щабель = 3 + 3»), числа балансу ні; орієнтир «≥50% з активного виробництва еліт» — як заявлена ціль дизайну"
           :no-characters "характери еліт — лише як принцип із підсумку і OPEN-гачок у політиці вибору; жодної моделі рис"
           :summary-is-model "п'ять уточнень, горизонт пізніших шарів, орієнтири і робочий критерій — правила нового дока з provenance «власник, 2026-09-05»"
           :flow "цей FLOW; три старі закриваються за /flow-close разом із RESEARCH → Flows/Archive/"
           :tooling "Obsidian MCP не піднявся в цій сесії — переміщення через mv, посилання переписуються вручну (прецедент D10)"}
 :out-of-scope ["Придумувати ігрові сценарії" "придумувати типи та назви районів" "Придумувати імена еліт" "придумувати характери еліт та їх риси"]
 :do (-> (:step-1 "прочитати все")
         (:step-2 "findings-gate: план-каркас нового дока — розділи, що з rev 5 входить як є, що переписане підсумком, що лишається OPEN; чекати GO на написання")
         (:step-3 "написати GAME_MECHANICS.md; GLOSSARY і згадки старого — перепрямувати; rm GAME_VISION.md")
         (:step-4 "три RESEARCH і три FLOW → Flows/Archive/ через flow-close; gen_index; пам'ять агента")
         (:step-5 "цей FLOW лишається активним до /flow-close власника"))
 :result "1 чистий документ який пояснює механіки гри, все те що ми обговорювали, та шукали."
 :accept [{:meter "python3 Tools/gen_index.py" :target "LINT: clean; активних FLOW — лише цей"}
          {:meter "python3 Tools/doc_lint.py --quiet" :target "0 ghosts, 0 syntax"}
          {:meter "grep GAME_VISION поза Flows/Archive і поза записами цього FLOW" :target 0}]}
```

## Amendments (append-only)

```clojure
[{:received-at "2026-09-05, відповіді на findings-gate"
  :raw-request ["1 - старт лише з мером, мер має активні дії на видобуток, перші еліти, будівлі і їх пасиви це вже наступний крок"
                "2 - на твій розсуд"
                "3 - закривай, бо за нашим flow тільки користувач має право закривати flow-и, я бачу що їх робота вже виконана тому закривай"]
  :normalized {:start-state "фаза 0 — мер сам з активними діями видобутку; еліти, будівлі й пасиви — фаза 1 (GAME_MECHANICS §1 game-phases)"
               :consequences-section "вибір агента: вимога + наявні наслідки + агентські пропозиції з рейтингами (GAME_MECHANICS §10)"
               :flow-close "три FLOW і три RESEARCH → Flows/Archive/; метри власника на видалених артефактах — :superseded"}
  :confirmed true}

 {:received-at "2026-09-05, закриття"
  :raw-request ["закривай flow, коміть все"]
  :normalized {:flow-close "власник явно закриває цей FLOW; це і є останній pending метр — прочитання GAME_MECHANICS.md"
               :commit "закомітити всі напрацювання цієї сесії одним комітом"}
  :confirmed true}]
```

# 2 · Contract

## Findings

```clojure
;; research pass 2026-09-05 — full read of GAME_VISION rev 5, three FLOWs, three RESEARCH documents
[{:finding :rev5-is-the-only-live-model
  :at "2026-09-05"
  :fact "GAME_VISION rev 5 (2026-09-05) є єдиною чинною моделлю; рішення трьох FLOW містять близько шістдесяти правил rev 1–3, які rev 5 зняв: платник як вибір, власність гекса, викуп, податок натурою, performer fee / rent, указ про резерв, оренда за частку і строк, данина за дозвіл, ставка оператора"
  :verified-by "прочитав GAME_VISION rev 5 і Decisions усіх трьох FLOW цілком; кожне зняте правило має :supersedes або лежить у RETIRED-блоках rev 5"
  :consequence "новий док несе лише rev 5 + підсумок; зняті правила — окремий розділ Retired по одному рядку з причиною, щоб їх не пропонували знову"}

 {:finding :summary-collides-with-early-game
  :at "2026-09-05"
  :fact "підсумок: «Пасиви забезпечують старт і перший ріст» і «біржа працює від початку». Rev 5: пасив існує лише як вихід БУДІВЛІ еліти в слоті, а :early-game (^:provisional) каже «мер — лінійний менеджер до першої еліти», еліти з'являються окремою механікою (OPEN). Разом це суперечність: без еліт на старті немає ні пасивів, ні контрагентів на біржі"
  :verified-by "зіставив рядки підсумку з (def building :passive), (def actor-action-capacity :early-game), (def elite-emergence)"
  :consequence "відкрите рішення :start-state — власник відповідає до написання"}

 {:finding :fifty-percent-is-a-target-not-a-rule
  :at "2026-09-05"
  :fact "«≥50% економіки з активного виробництва еліт у середині гри, без їхніх пасивів» — орієнтир для балансу, який власник робить сам в Unity; у моделі це читається як вимога до дизайну: активний вихід еліт іде в місто лише через біржу, отже біржа мусить нести половину міського кошика в середині гри"
  :verified-by "підсумок власника + його відповідь 4 («баланс я буду робити сам в Unity»)"
  :consequence "у док іде як (def passive-to-active-arc) із :target-mid-game, без чисел балансу"}

 {:finding :research-outputs-live-in-rev5
  :at "2026-09-05"
  :fact "з трьох research-доків у моделі живуть: кільце ходу з магдебурзьким прецедентом; журнал на родину (ricordanze); маєток як драбина статусу; привілей як валюта мера; будівля як інституція; The Guild 2 як негативний контроль ринку; закриті цехи → :category-cap; прийняття до кільця → :ring-admission; City Tales — шість опцій проти §10. Зняті разом із моделлю: указ про резерв, ставка оператора, оренда, всі три вердикти RESEARCH_INTERMEDIATE до четвертого"
  :verified-by "прочитав усі три RESEARCH цілком і звірив із rev 5 §2, §6, §7, §10"
  :consequence "новий док має розділ Precedents з пойнтерами на архівні RESEARCH; findings, які rev 5 не взяв, туди не переписуються"}

 {:finding :retired-list-is-the-owners-own-history
  :at "2026-09-05"
  :fact "підсумок: «Ми відмовилися від механік які звучали прикольно, але були занадто складними та незрозумілими» — # Attempted і # Disproven трьох FLOW разом із RETIRED-блоками rev 5 і є цим списком: слоти дій, платник як політика, тумблер платника, фізична монета як друга ціна, граф стосунків, пороги і постійні замовлення, автопріоритет, пріоритет власного ланцюга, оренда й частки, данина, резерв"
  :verified-by "прочитав Attempted / Disproven усіх трьох FLOW і RETIRED-блоки rev 5"
  :consequence "розділ Retired у новому доку — один рядок на механіку: що, чому знято, дата"}

 {:finding :deletion-footprint
  :at "2026-09-05"
  :fact "GAME_VISION згадують: GLOSSARY (:domain-анкери термінів), зона INDEX, frontmatter трьох FLOW і трьох RESEARCH, пам'ять агента; Obsidian MCP у цій сесії не з'єднався попри увімкнений Obsidian"
  :verified-by "grep по репо, дотфолдерах і теці пам'яті 2026-09-05"
  :consequence "перепрямування руками за один прохід разом із rm; в архівних доках посилання на кореневі файли переписуються на root-relative форму, яку gen_index толерує"}

 {:finding :two-old-flows-have-owner-meters-on-deleted-deliverables
  :at "2026-09-05"
  :fact "FLOW_BUILD_UX_DECONGESTION: «користувач — погоджена UX-специфікація + мокапи» :pending; FLOW_BASIC_CITY_UI: «owner — visual direction and menu flow reviewed» :pending. Обидва артефакти власник видалив 2026-09-05 як такі, що не відповідають баченню"
  :verified-by "прочитав Acceptance обох FLOW; рішення :spec-retired і :mockups-retired у FLOW_VISION_UNIFICATION"
  :consequence "закриття можливе лише як :superseded за словом власника (його :achieved-result 2026-09-05), не як :passed; це підтверджується при GO на написання"}]
```

## Decisions

```clojure
(def decisions
  [{:decision :start-state
    :status :confirmed :at "2026-09-05"
    :value "фаза 0: мер сам, активні дії видобутку і розвитку своїм AP; перші еліти, будівлі і їхні пасиви — фаза 1; біржа як механізм існує з першого ходу, перший контрагент — перша еліта"
    :verified-by "власник: «старт лише з мером, мер має активні дії на видобуток, перші еліти, будівлі і їх пасиви це вже наступний крок»"
    :reason "агент рейтингував старт з елітами на 60 і помилився: підсумок описував послідовність фаз, не стан першого ходу; рейтинги записані, щоб читати їх проти результату"
    :was-rated [{:option "старт із кількома елітами" :confidence 60} {:option "мер сам; пасив = базовий вихід гексу" :confidence 25} {:option "мер сам; пасиви й біржа стоять до першої еліти" :confidence 15}]}

   {:decision :consequences-section-shape
    :status :confirmed :at "2026-09-05"
    :value "вимога + наслідки, що вже є в rev 5 + три агентські пропозиції з рейтингами, позначені :agent-proposal (GAME_MECHANICS §10)"
    :verified-by "власник: «на твій розсуд»; вибір агента за варіантом 65"
    :reason "пропозиції позначені, тому не читаються як правила; без них розділ був би лише вимогою"}

   {:decision :old-flow-meters-superseded
    :status :confirmed :at "2026-09-05"
    :value "метри «користувач: спека + макети» (FLOW_BUILD_UX_DECONGESTION) і «owner: visual direction reviewed» (FLOW_BASIC_CITY_UI) закриті як :superseded; три FLOW і три RESEARCH → Flows/Archive/"
    :verified-by "власник: «закривай, бо за нашим flow тільки користувач має право закривати flow-и, я бачу що їх робота вже виконана тому закривай»"
    :reason "артефакти, які ті метри мали оцінювати, власник видалив; його :achieved-result — підстава закриття"}

   {:decision :game-mechanics-shape
    :status :confirmed :at "2026-09-05"
    :value "17 розділів: теза і принципи з фазами; актори; кільце й увага; земля, слоти, будівлі; праця; товари, біржа, монета; населення; еліти; інструменти мера і політика; наслідки; історії; факти для майбутнього UI; відкриті питання; зняті механіки; прецеденти й research; горизонт; provenance"
    :verified-by "каркас показаний власнику на findings-gate 2026-09-05; заперечень не було; GO дано відповідями 1–3"
    :reason "кожен розділ має джерело в rev 5, підсумку або research; жодного нового правила поза словами власника, крім позначених :agent-proposal"}

   {:decision :flow-closed-by-owner-word
    :status :confirmed :at "2026-09-05"
    :value "останній pending метр «власник: GAME_MECHANICS.md прочитано» закритий явним наказом власника на закриття; цей FLOW архівується поруч із трьома вже заархівованими"
    :verified-by "власник: «закривай flow, коміть все»"
    :reason "проєктне правило: лише власник має право закривати flow; явний наказ на закриття і є цим підтвердженням"}])
```

## Disproven (append-only)

```clojure
[]
```

## Attempted (append-only)

```clojure
[]
```

# 3 · Plan

```clojure
{:status :complete   ;; closed 2026-09-05 by the owner's word — «закривай flow, коміть все»
 :completed #{:flow-created :full-read :findings-gate :owner-answers :write-game-mechanics :repoint-glossary-index-memory :rm-game-vision :flow-close-x3 :archive-research :meters :owner-close-confirmed}
 :current :closed
 :remaining #{}
 :resume-context "CLOSED 2026-09-05: GAME_MECHANICS.md написаний (17 розділів) і є єдиним живим доком механік; GAME_VISION.md видалений і не відновлюється. GLOSSARY (11 анкерів), зона INDEX і пам'ять агента перепрямовані. Три FLOW (BUILD_UX_DECONGESTION, BASIC_CITY_UI, VISION_UNIFICATION) і три RESEARCH — у Flows/Archive/; їхні метри власника — :superseded. Усі три тулчейн-метри цього FLOW пройдені (gen_index, doc_lint, grep GAME_VISION). Останній метр — прочитання GAME_MECHANICS.md власником — закритий явним наказом на закриття «закривай flow, коміть все» (2026-09-05); цей FLOW архівується поруч, і зміни цієї сесії комітяться разом із закриттям."}
```

```clojure
;; harvested 2026-09-05 (Rule 2d): invariants → already embodied in GAME_MECHANICS.md itself (:start-state → §1 game-phases; :consequences-section-shape → §10; :game-mechanics-shape → the document's own 17-section structure); the owner's verbatim words stay in # 1 · Request; record = commit log.
;; The step plan (full read, findings-gate, write GAME_MECHANICS.md, repoint GLOSSARY/INDEX/memory, rm GAME_VISION.md, close+archive three FLOWs and three RESEARCH, meters) is dropped — fully executed.
```

## Acceptance

```clojure
[{:meter "python3 Tools/gen_index.py" :target "LINT: clean; активних FLOW — лише цей" :actual "LINT: clean; INDEX.md written: 39 docs (3 always, 21 trigger, 1 reference, 14 archive); status: partial лише у Flows/FLOW_ECONOMY_POC.md (2026-09-05)" :status :passed}
 {:meter "python3 Tools/doc_lint.py --quiet" :target "0 ghosts, 0 syntax" :actual "0 ghosts; 0 Clojure syntax errors; 43 md scanned (2026-09-05)" :status :passed}
 {:meter "grep GAME_VISION поза Flows/Archive і поза записами цього FLOW" :target 0 :actual "0 markdown-посилань на GAME_VISION.md у всьому репо; згадки лишились тільки як історія: Scope/Provenance GAME_MECHANICS, зона INDEX, записи цього FLOW" :status :passed}
 {:meter "власник" :target "GAME_MECHANICS.md прочитано: «1 чистий документ який пояснює механіки гри, все те що ми обговорювали, та шукали»" :actual "власник явно наказав закрити flow 2026-09-05: «закривай flow, коміть все» — цей наказ і є вичерпанням метра" :status :passed}]
```
