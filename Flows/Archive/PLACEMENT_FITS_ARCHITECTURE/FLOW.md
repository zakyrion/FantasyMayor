---
category: A
read: archive
status: implemented
tags:
  - skills
  - placement
  - rules
---

fantasymayor-placement states the project's layering in full and fires on every placement, in the flow and in the cascade.

# Request

```clojure
{:source :user
 :received-at "2026-09-26"
 :raw-request ["треба щоб fantasymayor-placement відповідав нашій архітектурі і використовувався як для звичайного flow так і для каскада"
               "1 — «A (70) Адаптер + AGENTS.md»; 2 — «A (75) Будь-яке розміщення»; «Так»"]}
```

# Confirmed contract

```clojure
{:task :placement-fits-architecture
 :goal "fantasymayor-placement відповідає архітектурі проєкту і спрацьовує і в звичайному flow, і в каскаді"
 :path :direct
 :where #{".claude/skills/fantasymayor-placement/SKILL.md" "AGENTS.md § 2 request-routing" ".sdd-flow/project.md # Ceremonies"
          "RULES_SPECIFICATION.md — лише для нового правила (§ 6)"}
 :off-limits #{".claude/skills/sdd-* і канон .sdd-flow/ — керовані файли"}
 :decided {:hook "адаптер project.md (# Ceremonies) + AGENTS.md § 2 — канон не чіпаємо"
           :trigger "будь-який варіант, формулювання чи структура, що створює, переносить або видаляє тип чи файл, або додає чи прибирає asmdef-посилання — до того, як власник це побачить"}
 :accept [{:meter "fantasymayor-rules-conformance" :target "no :diverges / :incomplete / :uncarried по placement-правилах"}
          {:meter "прогін скілу на кейсах сесії" :target "кожен кейс ловиться до показу варіанта"}]
 :result "скіл розміщення каже все, що каже архітектура, і його викликають flow і каскад у кожній точці розміщення"}
```

# Plan

```clojure
(-> (:step-1 "RULES_SPECIFICATION (§ 6 step-1): :folder/no-code-outside-a-role + виняток shared kernel (файли в корені, події — Events/); нові :place/installer, :place/app-root, :place/assets (конфіг-ассети Assets/Addressables/Configs/, prefabs/uxml/uss — <Area>/Prefabs/, текстури — <Area>/Textures/), :di/installer (простий IInstaller, MonoBehaviour лише з [SerializeField]), :di/install-order (InstallModules, залежності перед залежними, новий інсталер — один рядок); :folder/roles + :prefabs :textures")
    (:step-2 "fantasymayor-placement: опис і тригер — будь-яке розміщення, перенесення, видалення, asmdef-посилання, до показу; рамка Clean Architecture (Dependency Rule всередину, Main = Installers.World); процедура + крок перенесення (нова збірка бачить усе, що тип сам називає — asmdef_reach can на кожен) + крок видалення (тека спорожніла → тека, .meta, мертві using); дописати :event/declare, :key/fk-home, :archetype/reach, :transaction/no-substrate-state, нові правила кроку 1; :app-state-contracts — у (def place)")
    (:step-3 "AGENTS.md § 2 :skills — fantasymayor-placement: новий тригер; .sdd-flow/project.md # Ceremonies — placement-check: огляд перед кожним варіантом розміщення, структура для кожного файлу й посилання, пряме формулювання так само")
    (:step-4 "fantasymayor-rules-conformance по зміненому (placement-правила, носій — скіл) — ціль clean")
    (:step-5 "прогін скілу на чотирьох кейсах сесії — кожен ловиться")
    (:step-6 "close"))
```

# Findings

```clojure
[{:finding :conformance-aligned
  :at "2026-09-26"
  :fact "aligned (17): :place/domain :place/presentation :place/module :place/shared-kernel :dep/shared-kernel :dep/module :dep/layer-to-module :dep/presentation-to-domain :dep/domain-tiers :dep/no-cycles :dep/boot-exempt :place/namespace :folder/archetypes :folder/shared-role :folder/roles, :view/no-business-logic, :wiring/app-state-registration"
  :verified-by "fantasymayor-rules-conformance, scope — carrier fantasymayor-placement проти RULES_SPECIFICATION (rules-placement + правила з :governs :layer/:assembly/:role-folder/:event поза блоком)"
  :consequence "ядро закону шарів скіл несе правильно"}
 {:finding :conformance-missing
  :at "2026-09-26"
  :fact ":missing у скілі: :event/declare («подія — у Events/ своєї фічі»; скіл лише перелічує теку Events/), :key/fk-home («FK-колонка — у папці фічі власника поруч із PK»), :archetype/reach («архетип називає лише компоненти, які бачить його збірка»), :transaction/no-substrate-state («стан вибору інтерфейсу — у presentation, не в підкладковому домені»)"
  :verified-by "RULES_SPECIFICATION.md:415, :381, :351, :572 проти SKILL.md — тексту немає"
  :consequence "дописати в скіл; :event/declare — саме той промах, що дав подію в корені Core"}
 {:finding :conformance-extra
  :at "2026-09-26"
  :fact ":extra — скіл несе, специфікація ні: дім інсталера за видом коду (:place :installer), :app-root, дім конфіг-ассетів Assets/Addressables/Configs/, дім UI-ассетів <Area>/Prefabs/, усе (def di-composition) крім first-order-system (root-first, install-order, new-installer, public-api-module), рольові теки Prefabs/ і Textures/; :place/app-state-contracts стоїть під ключем :depends, а не в місці"
  :verified-by "SKILL.md :place, (def di-composition), (def role-folders) проти rules-placement і :wiring/*"
  :consequence "власник вирішує: підняти в специфікацію чи лишити порадою — питання 4"}
 {:finding :kernel-root-vs-role-rule
  :at "2026-09-26"
  :fact ":folder/no-code-outside-a-role каже «коду поза рольовою папкою не буває», але shared kernel живе в корені: Assets/Scripts/Core/*.cs і Assets/Scripts/EcsExtensions/*.cs; сьогоднішня структура поклала туди ще 5 типів"
  :verified-by "ls Assets/Scripts/Core, Assets/Scripts/EcsExtensions; RULES_SPECIFICATION.md:675"
  :consequence "закон і живий код розходяться — питання 3"}
 {:finding :trigger-too-narrow
  :at "2026-09-26"
  :fact "опис і процедура скілу — лише «нові файли, типи, asmdef-посилання, інсталери» в implementation map; AGENTS.md § 2 так само; каскад (канон sdd-*) скілу не знає; адаптер project.md не має церемонії розміщення"
  :verified-by "SKILL.md frontmatter + (def placement); AGENTS.md § 2 :skills; grep fantasymayor-placement по .sdd-flow і .claude/skills/sdd-*"
  :consequence "рішення 1 і 2: тригер на будь-яке розміщення + церемонія в адаптері"}
 {:finding :session-cases
  :at "2026-09-26"
  :fact "кейси сесії й правило, що їх ловить: (1) Presentation.UI → Boot.Implementation — :dep/layer-to-module; (2) подія в Boot.Core — жодне правило, бо ламала замкненість залежностей ТИПУ, що переїжджає (IEventTag у Ecs.Extensions → цикл); (3) подія в корені Core — :event/declare; (4) видалення останнього файлу теки — жодне правило: тека, .meta і простір імен зникли, 7 мертвих using"
  :verified-by "ця сесія: asmdef_reach, fmgraph check, roslyn CS0234"
  :consequence "(2) і (4) — кроки процедури скілу (перенесення перевіряє все, що тип сам називає; видалення перевіряє, чи спорожніє тека), а не нові закони"}]
```

# Decisions

```clojure
[{:id 1 :asked "Як підключити скіл до каскаду, якщо скіли sdd-* — керований канон?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "адаптер project.md + AGENTS.md § 2" :confidence 70}
            {:id :b :option "A + пропозиція промоції в канон" :confidence 20}
            {:id :c :option "локальний патч канону" :confidence 10}]
  :chosen :a :value "адаптер + AGENTS.md" :verified-by "«A (70) Адаптер + AGENTS.md»"}
 {:id 2 :asked "Коли скіл має спрацьовувати?" :status :confirmed :at "2026-09-26"
  :options [{:id :a :option "будь-яке розміщення, переміщення, видалення, asmdef-посилання — до показу" :confidence 75}
            {:id :b :option "лише нові файли" :confidence 25}]
  :chosen :a :value "будь-яке розміщення — до показу" :verified-by "«A (75) Будь-яке розміщення»"}
 {:id 3 :asked "Shared kernel живе в корені тек, а закон каже «коду поза рольовою папкою не буває». Що робимо?" :status :confirmed :at "2026-09-26"
  :chosen :a :value "змінити закон: shared kernel тримає файли в корені, події — у Events/" :verified-by "власник спитав «Що краще відповідає принципам Clean Architecture?», агент порадив A (CA мовчить про теки всередині збірки; гл. 13, 21, 34 — з знання агента, не перевірено), власник: «погоджуюся»"
  :options [{:id :a :option "змінити закон: shared kernel тримає файли в корені збірки, але події — у своїй Events/" :confidence 60}
            {:id :b :option "розкласти Core і Ecs.Extensions по рольових теках (Systems/, Data/, Events/ …) — масова зміна using" :confidence 40}]}
 {:id 4 :asked "Скіл несе правила, яких немає в специфікації: дім інсталера, app-root, дім конфіг- і UI-ассетів, склад DI, теки Prefabs/ і Textures/. Що з ними?" :status :confirmed :at "2026-09-26"
  :chosen :a :value "RULES_SPECIFICATION лишається; ці правила піднімаються в неї (§ 6)" :verified-by "власник: «Можливо нам цей документ більше не потрібен»; агент порадив лишити специфікацію (на ній fmgraph, conformance, § 6) і підняти правила; власник: «погоджуюся»"
  :options [{:id :a :option "підняти в RULES_SPECIFICATION як правила (§ 6) — тоді conformance їх перевіряє" :confidence 70}
            {:id :b :option "лишити в скілі як пораду без правила" :confidence 30}]}]
```

# Progress

```clojure
{:status :complete
 :completed #{"task statement" "research" "findings gate — decisions 3, 4" "implementation go «го»" "spec § 6" "skill" "AGENTS.md § 2 + adapter ceremony" "conformance" "case run"}
 :current "closed 2026-09-26"
 :remaining #{}
 :resume-context "research go given; nothing edited"}
```

# Acceptance

```clojure
[{:meter "fantasymayor-rules-conformance" :target "clean по placement-правилах"
  :actual "scope — носій fantasymayor-placement проти змінених і нових правил: aligned — :folder/no-code-outside-a-role (виняток ядра), :place/installer, :place/app-root, :place/assets, :di/installer, :di/install-order, :folder/roles (+prefabs, textures), :event/declare, :key/fk-home, :archetype/reach, :transaction/no-substrate-state, :place/app-state-contracts, :orchestrator/dispatch (реєстрація підсистеми); diverges 0, incomplete 0, missing 0, stale 0; :extra — лише рамка Clean Architecture (пояснення, не закон) і кроки процедури; doc_lint — 0 ghosts, 0 Clojure errors"
  :status :met}
 {:meter "прогін скілу на кейсах сесії" :target "кожен кейс ловиться до показу варіанта"
  :actual "(1) Presentation.UI → Boot.Implementation — depends :layer->module :never Implementation; asmdef_reach can Presentation.UI GameModeMachine → NO → :forbidden, rule dep/layer-to-module. (2) подія в Boot.Core — крок 3: нова збірка не бачить IEventTag, виправлення потребує посилання, яке крок 4 бачить як цикл (аналог сьогодні: can Core UpdatedSystem → NO; path Ecs.Extensions Core → Ecs.Extensions -> Core) → :forbidden, rule dep/no-cycles. (3) подія в корені Core — place :event «у Events/ своєї фічі — і в shared kernel» → :forbidden, rule event/declare. (4) видалення останнього файлу теки — крок 5: тека + .meta + мертві using через CS0234"
  :status :met}]
```
