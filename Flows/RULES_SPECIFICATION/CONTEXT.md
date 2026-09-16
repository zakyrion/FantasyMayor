---
category: A
read: always
status: partial
tags: [architecture, rules, specification, cascade]
related:
  - "[FLOW](FLOW.md)"
---

Контекст для s1: носії правил коду, інвентар кожного правила, суперечності, рішення, межі й перевірка специфікації.

# Task

```clojure
{:task :rules-specification
 :flow "Flows/RULES_SPECIFICATION/FLOW.md"
 :context-written-at "2026-09-16"
 :stage :context
 :next-stage :s1
 :previous-cascade :none
 :mode {:path :cascade
        :auto "ворота власника (знахідки CONTEXT, після s1, після s2, вердикт read-back) — без його слова; спірне місце стадія вирішує сама: варіанти з оцінкою 0-100, обрано найвищий, запис :auto-decided"
        :isolation "кожна стадія — окремий агент із чистим контекстом; s1 читає лише цей файл і файли з # Reads"}
 :goal "RULES_SPECIFICATION.md у корені репозиторію — повна специфікація правил, яких має дотримуватись код FantasyMayor; єдине джерело, з якого надалі виводяться ARCHITECTURE.md, скіли, рецепти Patterns/, fantasymayor-graph і MarkerShapeAnalyzer"
 :result "RULES_SPECIFICATION.md написана; власник бачить підсумок"
 :cascade-mapping {:context "носії правил, знахідки, пошук — цей файл"
                   :s1 "набір правил і дані, про які вони говорять (конструкції коду: типи, атрибути, папки, виклики), без розкладки документа"
                   :s2 "структура документа: розділи, def-імена, стабільні ID правил"
                   :code "RULES_SPECIFICATION.md як переклад s2"
                   :read-back "звірка документа з s2 і з носіями"
                   :converge "звірка документа з s2 і з носіями; contradicts 0, unrequested 0"}
 :s1-makes "повний набір правил коду: кожен запис інвентаря нижче з :kind :code або :definition або входить у правило (разом із дублікатами з :same-as), або виключений явним рішенням з причиною; кожен запис з :kind :di, :non-code, :asset, :stale — у переліку виключених з причиною; кожна суперечність із # Overlaps and contradictions — розв'язана"
 :s1-must-not #{"назви розділів, def-імена і ID правил — це робота s2"
                "текст скелетів C# з рецептів — у специфікацію йде правило, яке скелет кодує, не скелет"
                "живі назви класів як правило — лише як доказ у факті"}
 :decided [{:id :form :value "форма — Clojure-блоки в .md; вміст кожного блоку — валідні EDN-дані: без ^-метаданих, проза лише в рядках, reader-clean" :verified-by "FLOW.md # Confirmed contract :decided; # Decisions :edn-valid (власник: «2 - так»)"}
           {:id :bounds :value "межі — кожне правило, якого має дотримуватись код; правила документів і процесу — ні" :verified-by "FLOW.md :decided; GRAPH_STANDARD :spec-answers :bounds"}
           {:id :stable-rule-ids :value "кожне правило має стабільний ID; помилка аналізатора (FM…) і попередження графа посилатимуться на нього" :verified-by "FLOW.md # Decisions :stable-rule-ids (власник: «1 - так»)"}
           {:id :no-di-registration :value "реєстрація DI (форми Register…, InstallModules) не записується — блок під рефакторингом, записане згниє" :verified-by "FLOW.md # Decisions :no-di-registration (власник: «8 - цей блок буде рефакторитися…»)"}
           {:id :architecture-after :value "ARCHITECTURE.md після каскаду — лише доповнення для агента з посиланням на специфікацію; у цій задачі не змінюється" :verified-by "FLOW.md :decided; GRAPH_STANDARD :spec-answers :architecture-after"}
           {:id :view-definition :value "view — усе, що лежить у папці Views/ (без умови MonoBehaviour)" :verified-by "FLOW.md :decided; GRAPH_STANDARD # Decisions :view-definition (власник: «2 - все що є в папці view»)"}
           {:id :markers :value "маркер [SystemRole] обов'язковий там, де роль не визначає база, і заборонений там, де визначає; підписка += на C#-подію view вимагає [ViewSubscriber(typeof(V))]" :verified-by "FLOW.md :decided; GRAPH_STANDARD :redundant-marker-error (власник: «3 - теж помилка»); поправка 2026-09-16 :enforce-rules :decided"}
           {:id :recipe-signatures :value "ознаки рецептів у специфікації — лише ті, що є правилом написання коду (роди а + б з :signatures-three-kinds); евристики впізнавання (рід в) лишаються в коді інструмента" :verified-by "FLOW.md :decided; GRAPH_STANDARD :signatures-in-standard (власник: «1 - а»)"}
           {:id :view-boundary :value "view-boundary — правило; помилка компіляції для нього не вимагається" :verified-by "FLOW.md :decided; GRAPH_STANDARD :view-boundary-recorded (архівне рішення: помилка компіляції — лише на окреме слово власника)"}
           {:id :flow-contract-removed :value "вимога :flow-contract (Flows/FLOW_<NAME>.md на кожен transaction flow) прибрана — у специфікацію не йде" :verified-by "GRAPH_STANDARD # Decisions :flow-contract-removed (власник: «6 - прибирай цю вимогу»)"}
           {:id :code-story-rules-excluded :value "правила розповіді коду (sdd-cascade # Translation, колишній CODE_STORY_RULES_PROPOSAL) у специфікацію не йдуть" :verified-by "GRAPH_STANDARD :spec-flow-answers :code-story-rules (власник: «3 - CODE_STORY_RULES_PROPOSAL видаляй, це те що вже стало каскадом»)"}
           {:id :compile-errors-later :value "помилки компіляції: відсутній і зайвий [SystemRole], підписка на подію view без [ViewSubscriber] — примус робить GRAPH_STANDARD :enforce-rules після каскаду; специфікація називає правило, аналізатор посилатиметься на його ID" :verified-by "GRAPH_STANDARD # Amendments 2026-09-16 :enforce-rules :decided"}
           {:id :cascade-auto :value "стадії context → s1 → s2 → code → read-back → converge послідовно, кожна окремим агентом; спірне — :auto-decided з оцінкою" :verified-by "FLOW.md # Decisions :cascade-auto (власник: «Запускай каскад в авто режимі в окремому агенті…»)"}]
 :out-of-scope #{"форми реєстрації DI: виклики Register…/As/WithParameter/Lifetime, клас і розміщення інсталера, порядок InstallModules, ручне вписування систем у game state у Boot.Construct (межа — :auto-decided :ad-di-boundary)"
                 "правила документів, процесу, нотації й поведінки інструментів (перелік причин — # Not code rules)"
                 "правила розповіді коду sdd-cascade"
                 "DOC_STANDARD.md і його заміна скілом"
                 "правки ARCHITECTURE.md, CLAUDE.md, .sdd-flow/, скілів, Patterns/, коду fantasymayor-graph і MarkerShapeAnalyzer — їх звіряє й править Flows/GRAPH_STANDARD після каскаду"
                 "примус: нові діагностики аналізатора, крок перевірки в CLAUDE.md, скіл rules-conformance"
                 "пам'ять агента (~/.claude/projects/…/memory) — дзеркало ARCHITECTURE.md, не носій з :sources"
                 "Flows/Archive/ — історія, не носій"
                 ".unity-сцени, prefab і .asset"
                 "виправлення живих відхилень у коді гри"}}
```

# Reads

```clojure
[{:ref "Flows/RULES_SPECIFICATION/FLOW.md" :why "підтверджений контракт, :accept, # Decisions" :read #{:whole} :is #{:number-source}}
 {:ref "ARCHITECTURE.md" :why "головний носій правил; рядки в :at інвентаря — звідти" :read #{:whole} :is #{:number-source}}
 {:ref ".claude/skills/fantasymayor-placement/SKILL.md" :why "шари, залежності, папки, ролі папок" :read #{:whole} :is #{:number-source}}
 {:ref ".claude/skills/fantasymayor-pattern-choice/SKILL.md" :why "дерево вибору рецепта і типів ядра" :read #{:whole} :is #{:number-source}}
 {:ref "~/.claude/skills/arch-check/SKILL.md" :why "три заборони, класифікація каденції, що позначати і що ні" :read #{:whole} :is #{:number-source}}
 {:ref "Patterns/*.md — 15 файлів: ADDRESSABLE_PATTERNS і PATTERN_{CLEANUP_SYSTEM, COMPONENT, CONFIG, CONFIG_LOADER, EVENT, ORCHESTRATOR_SUBSYSTEM, PERFRAME_SYSTEM, PIPELINE_STAGE, POLYMORPHIC_CATALOGUE, REACTIVE_ORCHESTRATOR_SYSTEM, REACTIVE_SYSTEM, TAG, TRANSACTION_ENTITY, VIEW_SYSTEM}" :why "блоки Rules / When / Anti-patterns / Invariants і правила, закодовані в скелетах" :read #{:whole} :is #{:number-source}}
 {:ref ".claude/skills/fantasymayor-graph/SKILL.md, references/graph-facts.md, references/recipe-signatures.md" :why "рішення ролі, маркери, розпізнавання вузлів, аудит тегів, ознаки рецептів" :read #{:whole} :is #{:number-source}}
 {:ref ".claude/skills/fantasymayor-graph/scripts/roles.py tag_law.py view_pairs.py recipes.py ecs_facts.py" :why "правила, які інструмент справді перевіряє" :read #{:whole} :is #{:number-source}}
 {:ref "Tools/MarkerShapeAnalyzer/MarkerShapeAnalyzer.cs MarkedTypeCheck.cs MarkerVocabulary.cs TypeShape.cs" :why "FM1001-FM1004 і як аналізатор міряє форму" :read #{:whole} :is #{:number-source}}
 {:ref "Assets/Scripts/EcsExtensions/SystemRoleAttribute.cs SystemRoleKind.cs ViewSubscriberAttribute.cs TagLabelAttribute.cs TagLabelRole.cs" :why "маркери як типи" :read #{:whole} :is #{:type-source}}
 {:ref "CLAUDE.md § 3 (рядок 71) і § 5 (рядки 89-95)" :why "notation-ecs-ext — кандидат «не правило коду»; бан «silent skip» — дубль fail-loud" :read #{:section} :is #{:number-source}}
 {:ref "Flows/GRAPH_STANDARD/FLOW.md # Findings, # Decisions, # Amendments" :why "знахідки з провенансом і рішення власника, що стосуються специфікації" :read #{:section} :is #{:number-source}}
 {:ref "Assets/Scripts/EcsExtensions/ — UpdatedSystem.cs LateUpdatedSystem.cs IUpdatedSystem.cs ILateUpdatedSystem.cs IUniTaskSystem.cs IPrioritizedUniTaskSystem.cs EventArchetypes.cs EcsEventExtensions.cs EventCleanupSystem.cs EventTag.cs EventFrameComponent.cs TransactionTag.cs SystemPriorities.cs QueryResultExtensions.cs IValidatableConfig.cs ConfigLoaderSystem.cs ConfigAddresses.cs EntityStorages.cs SingletonArchetypeDefinition.cs" :why "реальні імена й сигнатури типів для :data у s1; читати лише коли потрібна сигнатура" :read #{:whole} :is #{:type-source}}
 {:ref "Assets/Scripts/Core/StateAllowedAttribute.cs FrameBox.cs StatusMonitor.cs DisposedMono.cs Result.cs Box.cs Status.cs; Assets/Modules/Turn/Systems/TurnPhaseSubSystem.cs; Assets/Modules/Addressable/Core/IAddressable.cs" :why "типи ядра, які називають правила" :read #{:whole} :is #{:type-source}}
 {:ref ".claude/skills/sdd-cascade/SKILL.md # s1, .sdd-flow/templates/CASCADE.md # s1" :why "форма артефакту s1 і правила синтаксису" :read #{:section} :is #{:example}}
 {:ref "Tools/doc_lint.py (ClojureReader, is_code_claim)" :why "що рахується синтаксичною помилкою Clojure і привидом — для reader-clean і метра :accept" :read #{:section} :is #{:integration-point}}]
```

# Search

```clojure
[{:for "живий доказ до суперечності (скільки класів з маркером, з локальною константою Priority, з полем-прапорцем)"
  :where "grep по Assets/ з --include='*.cs' (zsh: у лапках); python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py pattern <RECIPE> | systems --role <R> | tags | check"
  :settles "яку сторону суперечності підтримує живий код — довід для :auto-decided, не рішення"}
 {:for "реальна сигнатура типу, який називає правило"
  :where "mcp__roslyn__search_symbols / get_symbol_info над FantasyMayor.sln або читання файлу з # Reads"
  :settles ":type у :data s1 — реальний тип, не заглушка"}
 {:for "чи ім'я з правилом-суфіксом є в коді"
  :where "python3 Tools/doc_lint.py --quiet"
  :settles "не внести привида в артефакт"}]
```

# Facts

```clojure
{:verification-legend {:read-whole "носій прочитано цілком (Read / cat -n) 2026-09-16; :at — рядки того читання"
                       :read-partial "прочитано лише діапазон, названий в :at"
                       :grep "grep по Assets/ 2026-09-16, команда в тексті факту"
                       :fmgraph "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py <команда> 2026-09-16"}
 :facts
 [{:fact "Усі носії з :sources існують: ARCHITECTURE.md 239 рядків, placement 124, pattern-choice 130, arch-check 196, 15 Patterns/*.md 1334, graph SKILL 125 + graph-facts 70 + recipe-signatures 22, п'ять скриптів 752, аналізатор 316, п'ять атрибутів 70"
   :at "2026-09-16" :verified-by "wc -l по кожному шляху" :consequence "жоден носій не відсутній — виходу «носія нема» не існує"}
  {:fact "Базова лінія графа: check — curated, integrity clean, 8 попереджень (7 «DeleteEntity … attribute by hand», 1 «FK HexIdFKComponent has no owner key» — власника-PK у коді нема); 479 вузлів, 39 архетипів, 71 система"
   :at "2026-09-16" :verified-by ":fmgraph check, stats" :consequence "числа для :numbers і для доказів суперечностей"}
  {:fact "Ролі систем: cleanup 1, per_frame 7 (2 маркером: DistrictBuildUISystem, TurnProcessorSystem), pipeline_stage 14, reactive 18 (1 маркером: BuildDistrictCompletionSystem), startup_step 2, sub_system 26, turn_phase 3, undecided 0"
   :at "2026-09-16" :verified-by ":fmgraph stats, systems --role undecided, pattern PATTERN_PERFRAME_SYSTEM; grep '\\[SystemRole(' — 3 входження" :consequence "маркер [SystemRole] живе на 3 класах; правило «обов'язковий/заборонений» нічого живого не ламає за інструментом"}
  {:fact "Закон тегів: 0 відхилень; мітки [TagLabel] на 3 структурах — UITag і DistrictOpenConditionTag (членство), TransactionTag (роль Transaction); PATTERN_TRANSACTION_ENTITY має 1 примірник — ActionsArchetypes.BuildDistrictInProgress"
   :at "2026-09-16" :verified-by ":fmgraph tags, pattern PATTERN_TRANSACTION_ENTITY; grep '\\[TagLabel'" :consequence "правила тегів записуються як чинні, без живих порушень"}
  {:fact "PATTERN_VIEW_SYSTEM: 4 класи з [ViewSubscriber], 5 підписок, 0 підписок без маркера; 12 відхилень view-boundary у ContextTabsView, HexInfoPanelView, HexesUI, TurnPanelView (подія з view або поле/параметр EntityStorages)"
   :at "2026-09-16" :verified-by ":fmgraph pattern PATTERN_VIEW_SYSTEM" :consequence "view-boundary — правило з живими відхиленнями; помилка компіляції не вимагається (:view-boundary)"}
  {:fact "У папках Views/ 20 .cs; граф рахує 19 view (клас у Views/, що доходить до MonoBehaviour); різниця — DistrictBuildLabels, internal static class у Presentation/UI/DistrictBuild/Views/; TerrainView і WaterView доходять до MonoBehaviour через DisposedMono"
   :at "2026-09-16" :verified-by "find Assets -path '*/Views/*' -name '*.cs'; grep class; :fmgraph explain DistrictBuildLabels (kind other); grep 'class DisposedMono : MonoBehaviour'" :consequence "за :view-definition view — усі 20 файлів, включно зі статичним класом; визначення аналізатора (будь-який MonoBehaviour) і графа (Views/ + MonoBehaviour) відрізняються від рішення"}
  {:fact "Пріоритети: 0 входжень «const int ExecutionPriority» у Assets, 69 входжень «Priority => SystemPriorities»; SystemPriorities — вкладені класи (WorldInit, RuntimeTick, …), EventCleanup = int.MaxValue"
   :at "2026-09-16" :verified-by "grep -rln 'const int ExecutionPriority' | wc -l → 0; grep -rn 'Priority => SystemPriorities' | wc -l → 69; читання SystemPriorities.cs:1-67" :consequence "скелети рецептів з локальною константою суперечать живому коду і pattern-choice (:x-priority)"}
  {:fact "Поля-прапорці в живих системах без [StateAllowed]: ShowHexesUISystem (_uiBox, _isDisposed, _isLoaded), DistrictBuildUISystem (_view, _chromeHooked); у підсистемі DistrictBuildPriceUISubSystem — _hooked"
   :at "2026-09-16" :verified-by "grep '_hooked\\|_isLoaded' і читання полів ShowHexesUISystem.cs:20-27, DistrictBuildUISystem.cs:39-48" :consequence "доказ до :x-stateless-vs-lifetime-flags"}
  {:fact "StateAllowedAttribute: AttributeTargets.Field, конструктор (string reason = null) — причина необов'язкова в типі; FrameBox<T> — змінна struct (не readonly), Exist залежить від Time.frameCount"
   :at "2026-09-16" :verified-by "grep по Assets/Scripts/Core/StateAllowedAttribute.cs:8-13, FrameBox.cs:11-31" :consequence "доказ до :x-stateallowed-reason і :x-framebox-field"}
  {:fact "TurnPhaseSubSystem — public abstract class : IPrioritizedUniTaskSystem<TurnPhaseStep>; roles.py вважає pipeline_stage лише IPrioritizedUniTaskSystem з аргументом MapGenerationStep"
   :at "2026-09-16" :verified-by "grep Assets/Modules/Turn/Systems/TurnPhaseSubSystem.cs:14; roles.py:64-66" :consequence "доказ до :x-cadence-turn-phase"}
  {:fact "Єдина структура події з легасі-суфіксом …EventComponent — TerrainGenerationGenerateEventComponent; спостерігачів OnComponentAdded / OnComponentRemoved / OnTagsChanged у коді 0"
   :at "2026-09-16" :verified-by "grep 'struct [A-Za-z]*EventComponent'; grep 'OnComponentAdded\\|OnTagsChanged\\|OnComponentRemoved' | wc -l → 0" :consequence "доказ до :x-event-suffix"}
  {:fact "DistrictTypeComponent — IIndexedComponent<DistrictType>, ключ простору-перелічення; PATTERN_POLYMORPHIC_CATALOGUE:34 називає його FK"
   :at "2026-09-16" :verified-by "grep Assets/Domains/Economy/District/Components/DistrictTypeComponent.cs:10" :consequence "доказ до :x-catalogue-key-naming"}
  {:fact "Граф не бачить AddTag<T>() на живій сутності: source_reading.py має гілки для AddComponent, RemoveComponent, RemoveTag, Tags.Add, але не для AddTag; «tag added during life» = AddComponent типу з іменем …Tag"
   :at "2026-09-16" :verified-by ":read-partial source_reading.py:340-390; ecs_facts.py:179-183" :consequence "правило «ніколи не додавати тег живій сутності» мусить бути записане явно — ARCHITECTURE birth-completeness його не перелічує (:x-addtag)"}
  {:fact "doc_lint базово: 3 привиди (HexIdComponent — PATTERN_COMPONENT.md:42, PATTERN_TRANSACTION_ENTITY.md:51, :72), 0 синтаксичних помилок Clojure; привидом рахується PascalCase-токен із суфіксом ролі, якого нема серед ідентифікаторів Assets; рядок зі словом «привид» / ghost / remov не рахується; ^-метадані reader приймає, але :edn-valid їх забороняє"
   :at "2026-09-16" :verified-by "python3 Tools/doc_lint.py; прочитав Tools/doc_lint.py цілком" :consequence "метр :accept «привиди не зросли (3)»; специфікація не називає неіснуючих типів поза рядками-історією"}
  {:fact "Аналізатор підключений у Unity як Assets/Scripts/EcsExtensions/MarkerShapeAnalyzer.dll; Unity дає аналізатору дані через Filename.[AnalyzerName].additionalfile"
   :at "2026-09-16" :verified-by "ls Assets/Scripts/EcsExtensions; GRAPH_STANDARD :unity-additional-files" :consequence "EDN-дані специфікації згодом можна спроєктувати для аналізатора — довід на користь :edn-valid, s1 цього не проєктує"}]}
```

# Inventory

Кожен запис — одне правило одного носія одним рядком з якорем. `:kind`: `:code` — правило коду; `:definition` — визначення конструкції, про яку говорять правила; `:di` — форма реєстрації DI (виключено); `:non-code` — процес, документ, нотація, поведінка інструмента; `:asset` — ассет, не код; `:stale` — жива назва, історія або застарілий запис. `:same-as` — дубль іншого запису. ID інвентаря (`:arch-01` …) — лише для цього файлу; стабільні ID правил дає s2.

```clojure
{:inventory-tally {:entries 457 :code 366 :definition 43 :di 17 :non-code 21 :asset 3 :stale 7}
 :by-carrier {ARCHITECTURE.md 108 fantasymayor-placement 42 fantasymayor-pattern-choice 22 arch-check 19
              "Patterns (15 файлів)" 187 "fantasymayor-graph — документація" 34 "fantasymayor-graph — скрипти" 24
              MarkerShapeAnalyzer 13 "маркери-атрибути" 6 CLAUDE.md 2}
 :verified-by "перелічено скриптом по цьому файлу 2026-09-16: записи з :at і :kind"}
```

## ARCHITECTURE.md

```clojure
{:carrier "ARCHITECTURE.md" :lines 239 :verified-by :read-whole
 :entries
 [{:id :arch-00 :at "15-16 Stack" :kind :definition :rule "стек: Unity; ECS Friflo.Engine.ECS 3.6 (DoD, не Unity DOTS); DI VContainer; async UniTask; Addressables; InputSystem; URP; UI Toolkit + Unity App UI" :verified-by :read-whole}
  {:id :arch-01 :at "21-24 decomposition :split-when" :kind :code :rule "система ділиться, коли вона створює і знищує той самий вид вмісту, щокадру порівнює стан світу, тримає більше двох непов'язаних сімейств запитів або працює в кількох game state з різних причин" :verified-by :read-whole}
  {:id :arch-02 :at "25-27 decomposition :split-into" :kind :code :rule "ділиться на: стартовий обсяг → разова pipeline-стадія або підсистема створення мапи; runtime-обов'язок → одна reactive-система на відповідальність; спільне обчислення → stateless helper у Helpers/" :verified-by :read-whole}
  {:id :arch-03 :at "30 reactive :is" :kind :code :rule "reactive — типовий вибір для runtime-логіки і єдиний реактивний механізм проєкту" :verified-by :read-whole}
  {:id :arch-04 :at "31 reactive :event" :kind :code :rule "подія — однокадрова; її компонент — звичайні дані, значення, потрібні споживачу" :verified-by :read-whole}
  {:id :arch-05 :at "32 reactive :consumer" :kind :code :rule "споживач якориться на архетипі самої події (нуль вартості, поки події нема); ворота — IsRipe" :verified-by :read-whole}
  {:id :arch-06 :at "33-35 reactive :reaction :reconcile-gives" :kind :code :rule "реакція — або діяти прямо на значеннях події, або reconcile: зібрати поточну множину зі стану світу, порівняти, діяти на різниці; reconcile робить другу подію порожньою, а пропущену лагодить наступна" :verified-by :read-whole}
  {:id :arch-07 :at "36 reactive :timing" :kind :code :rule "ніколи не розраховувати на реакцію в тому ж кадрі — ланцюг подій коштує кадр на ланку" :verified-by :read-whole}
  {:id :arch-08 :at "37 reactive :emitters" :kind :code :rule "емітер може з'явитися пізніше: споживача можна зібрати першим як сплячий каркас" :verified-by :read-whole}
  {:id :arch-09 :at "38 reactive :per-frame" :kind :code :rule "per-frame — лише коли логіка справді неперервна і не може бути реактивною" :verified-by :read-whole}
  {:id :arch-10 :at "39-41 reactive :component-change-observers" :kind :code :rule "ніколи store.OnComponentAdded / OnComponentRemoved / OnTagsChanged чи будь-який спостерігач зміни значення; натомість — подія поруч із записом; прийняти спостерігача — рішення для всього проєкту, не локальне" :verified-by :read-whole}
  {:id :arch-11 :at "44 stateless-systems :rule" :kind :code :rule "система не тримає змінного стану екземпляра" :verified-by :read-whole}
  {:id :arch-12 :at "45-47 stateless-systems :not-state" :kind :code :rule "не стан: readonly DI-залежності й дескриптори сховища; кеші запитів Archetype, ArchetypeQuery, ComponentIndex, розв'язані раз у конструкторі; const і static readonly" :verified-by :read-whole}
  {:id :arch-13 :at "48-49 stateless-systems :state" :kind :code :rule "стан: будь-яке переприсвоюване поле; readonly-поле, чий вміст змінюється між кадрами — колекції, масиви, StringBuilder, native-буфери між тіками" :verified-by :read-whole}
  {:id :arch-14 :at "50 stateless-systems :default-home" :kind :code :rule "прапорець статусу, маркер «в роботі», лічильник прогресу, біт «запущено/завершено», дескриптор оброблюваного — доменний стан на сутності або однопримірниковий стан поза системою" :verified-by :read-whole}
  {:id :arch-15 :at "51-54 stateless-systems :escape" :kind :code :rule "вихід із заборони за порядком: компонент (завжди першим) → FrameBox (стан на обмежену кількість кадрів) → StateAllowedAttribute (переглянутий виняток, завжди з причиною)" :verified-by :read-whole}
  {:id :arch-16 :at "59-60 system-collections view" :kind :code :rule "MonoBehaviour view може використовувати System.Collections.Generic — view не система" :verified-by :read-whole}
  {:id :arch-17 :at "62-64 system-collections once" :kind :code :rule "система, що працює раз або кілька разів (startup step, config loader, стадія чи підсистема pipeline створення мапи), може використовувати System.Collections.Generic" :verified-by :read-whole}
  {:id :arch-18 :at "66-70 system-collections repeatedly" :kind :code :rule "система, що працює повторно (per-frame, reactive — кожна подія, turn phase — кожен хід), zero-allocation: пам'ять, взята на початку прогону, звільняється в його кінці; native-контейнери Unity.Collections, struct, span; ніколи керована колекція чи масив, збудовані на прогін" :verified-by :read-whole}
  {:id :arch-19 :at "71 system-collections :except" :kind :code :rule "виняток: елементи керованого типу (GameObject, посилання на view) — колекція System.Collections.Generic, виділена раз і утримувана" :verified-by :read-whole}
  {:id :arch-20 :at "72 system-collections :enum-key" :kind :code :rule "enum не може бути ключем NativeHashSet чи NativeParallelHashMap — ключ за базовим int; як значення enum придатний" :verified-by :read-whole}
  {:id :arch-21 :at "75-77 native-allocator :lifetime" :kind :code :rule "алокатор за часом життя: у межах кадру Allocator.Temp; у межах чотирьох кадрів Allocator.TempJob; довше — Allocator.Persistent або колекція System.Collections.Generic, яку звільняє один названий власник" :verified-by :read-whole}
  {:id :arch-22 :at "78 native-allocator :async" :kind :code :rule "async-система ніколи не бере Allocator.Temp — Temp прив'язаний до потоку, а async може продовжитись поза головним" :verified-by :read-whole}
  {:id :arch-23 :at "79-81 native-allocator :temp-on" :kind :code :rule "Temp на головному потоці — блок кадру, скидається в кінці кадру; всередині job — блок job; деінде — ніколи" :verified-by :read-whole}
  {:id :arch-24 :at "82 native-allocator :breach" :kind :code :rule "порушення алокатора — помилка, не рекомендація" :verified-by :read-whole}
  {:id :arch-25 :at "88 table-rule :table" :kind :code :rule "таблиця = ключовий компонент + дискримінатор (головний тег) разом в одному оголошеному архетипі" :verified-by :read-whole}
  {:id :arch-26 :at "89 table-rule :filter" :kind :code :rule "фільтр — архетип таблиці; ніколи голий ключовий компонент (це об'єднання всіх таблиць цього простору ключів)" :verified-by :read-whole}
  {:id :arch-27 :at "90 table-rule :sweep" :kind :code :rule "обхід таблиці — ітерація її архетипу, без об'єкта запиту" :verified-by :read-whole}
  {:id :arch-28 :at "91 table-rule :keyed-join" :kind :code :rule "з'єднання за ключем — ComponentIndex над ключовою колонкою, оголошений раз у конструкторі" :verified-by :read-whole}
  {:id :arch-29 :at "92 table-rule :cross-archetype-query" :kind :code :rule "запит через кілька архетипів — лише коли фільтр справді охоплює кілька архетипів, і лише погоджений з власником" :verified-by :read-whole}
  {:id :arch-30 :at "93 table-rule :join" :kind :code :rule "з'єднання — пошук за значенням ключа в точці використання; ніколи збережене посилання Entity з рядка однієї таблиці на рядок іншої" :verified-by :read-whole}
  {:id :arch-31 :at "94 table-rule :index" :kind :code :rule "індекс — лише для гарячого з'єднання (щокадру або багато разів за хід); ніколи для пошуку з частотою кліку — сканувати архетип" :verified-by :read-whole}
  {:id :arch-32 :at "97 tag-law :archetype" :kind :code :rule "кожне оголошення архетипу має рівно один головний тег, записаний першим у Tags.Get; двох головних тегів бути не може" :verified-by :read-whole}
  {:id :arch-33 :at "98-100 tag-law :main-tag" :kind :code :rule "головний тег — дискримінатор таблиці, ідентичність типу сутності; системи фільтрують за архетипом, який він називає; один головний тег називає один архетип; виняток — EventTag, головний тег кожного архетипу події" :verified-by :read-whole}
  {:id :arch-34 :at "101-102 tag-law :label-tag" :kind :code :rule "label-тег — маркер ролі чи членства поруч із головним, структура з [TagLabel]; TagLabelRole.Transaction позначає transaction-сутність; без ролі — членство" :verified-by :read-whole}
  {:id :arch-35 :at "103-104 tag-law :label-tag :count :never" :kind :code :rule "label-тегів 0-4 поруч із головним (Tags.Get бере не більше 5 аргументів типу); label-тег ніколи не фільтр запиту" :verified-by :read-whole}
  {:id :arch-36 :at "105 tag-law :event-archetype" :kind :code :rule "кожна подія несе EventTag як головний тег; її архетип названо компонентом події" :verified-by :read-whole}
  {:id :arch-37 :at "106 tag-law :state" :kind :code :rule "стан — …StateComponent над enum, не перемикаваний тег; пишеться через AddComponent лише при зміні" :verified-by :read-whole}
  {:id :arch-38 :at "107 tag-law :kind" :kind :code :rule "вид — …KindComponent над enum, не другий головний тег; пишеться раз при народженні" :verified-by :read-whole}
  {:id :arch-39 :at "108 tag-law :enum-columns" :kind :code :rule "колонка стану чи виду — законний ключ self-index: IIndexedComponent<TEnum> над рядками свого сімейства, читання index[value]" :verified-by :read-whole}
  {:id :arch-40 :at "112 key-role-law :pk" :kind :code :rule "PK — …IdComponent, власна ідентичність рядка; власник — рівно одна таблиця, у парі з її тегом; унікальність індексу — за контрактом, рушій не гарантує" :verified-by :read-whole}
  {:id :arch-41 :at "113 key-role-law :fk" :kind :code :rule "FK — …FKComponent, посилання з рядка іншої таблиці в простір ключів власника; ніколи тип PK власника; індекс 1:N" :verified-by :read-whole}
  {:id :arch-42 :at "114 key-role-law :data" :kind :code :rule "data — …Component, значення атрибута; ніколи не ключ індексів двох різних таблиць" :verified-by :read-whole}
  {:id :arch-43 :at "115 key-role-law :enum-space" :kind :code :rule "простір-перелічення — простір ключів без таблиці PK: сторона власника — data, сторона посилання — FK" :verified-by :read-whole}
  {:id :arch-44 :at "116-117 key-role-law :self-index" :kind :code :rule "self-index дозволено, коли кожен рядок з цим Data-компонентом належить одній таблиці (значення пошуку може прийти ззовні); ніколи той самий Data-тип, індексований у двох таблицях — розділити на пару PK + FK" :verified-by :read-whole}
  {:id :arch-45 :at "120 component-index :keyed-on" :kind :definition :rule "ComponentIndex ключується типом компонента через усі таблиці, що його несуть — роль несе тип ключа, таблицю несе тег" :verified-by :read-whole}
  {:id :arch-46 :at "121 component-index :maintained-by" :kind :code :rule "індекс підтримує виклик запису: AddComponent перекладає рядок, видалення сутності прибирає його" :verified-by :read-whole}
  {:id :arch-47 :at "122 component-index :pk-uniqueness" :kind :code :rule "дубль ключа PK повертає обидва рядки без помилки — перевіряти в місці виділення ключа і кидати там" :verified-by :read-whole}
  {:id :arch-48 :at "123 component-index :bucket-cap" :kind :code :rule "не більше 100 сутностей на однакове значення ключа — вставка й видалення O(N) по дублях" :verified-by :read-whole}
  {:id :arch-49 :at "124 component-index :one-fk-per-space" :kind :code :rule "сутність має один компонент на тип, тож не більше одного FK у простір ключів; два посилання — власна пара FK-типів, рішення дизайну" :verified-by :read-whole}
  {:id :arch-50 :at "125 component-index :key-equality" :kind :code :rule "компонент оголошує IIndexedComponent<TValue> і повертає ключ з GetIndexedValue(); enum працює прямо, struct-ключ реалізує IEquatable" :verified-by :read-whole}
  {:id :arch-51 :at "128-130 archetype-law :home" :kind :code :rule "один static-тримач <Assembly>Archetypes на збірку, що оголошує архетипи; <Assembly> — ім'я asmdef без крапок і без префікса Domains.; виняток — shared kernel і app-root" :verified-by :read-whole}
  {:id :arch-52 :at "131 archetype-law :reach" :kind :code :rule "архетип називає лише компоненти, які його збірка може бачити" :verified-by :read-whole}
  {:id :arch-53 :at "132 archetype-law :shape" :kind :code :rule "кожен член тримача повертає живий Archetype: store.GetArchetype(ComponentTypes.Get<…>(), Tags.Get<…>()) у тілі методу; store приходить параметром" :verified-by :read-whole}
  {:id :arch-54 :at "133 archetype-law :state" :kind :code :rule "тримач архетипів нічого не зберігає" :verified-by :read-whole}
  {:id :arch-55 :at "134 archetype-law :owner" :kind :code :rule "система тримає свій Archetype у readonly-полі, розв'язаному раз у конструкторі" :verified-by :read-whole}
  {:id :arch-56 :at "135 archetype-law :birth" :kind :code :rule "народження — archetype.CreateEntity() (рядок з усіма колонками за замовчуванням); ніколи голий store.CreateEntity() у місці виклику" :verified-by :read-whole}
  {:id :arch-57 :at "136 archetype-law :bulk" :kind :code :rule "масово — archetype.CreateEntities(n); коли кількість відома — спершу EnsureCapacity(n)" :verified-by :read-whole}
  {:id :arch-58 :at "137 archetype-law :values" :kind :code :rule "новий рядок присвоїти локальній змінній, потім писати; ніколи ланцюжок записів із виклику створення" :verified-by :read-whole}
  {:id :arch-59 :at "138 archetype-law :arity-cap" :kind :code :rule "ComponentTypes.Get і Tags.Get беруть не більше 5 аргументів типу" :verified-by :read-whole}
  {:id :arch-60 :at "141 birth-completeness :rule" :kind :code :rule "сутність народжується з кожною колонкою, яку коли-небудь матиме" :verified-by :read-whole}
  {:id :arch-61 :at "142 birth-completeness :presence" :kind :code :rule "присутність — не предикат, а значення-сторож (Unknown, Idle, порожня коробка)" :verified-by :read-whole}
  {:id :arch-62 :at "143 birth-completeness :composition" :kind :code :rule "склад — ніколи необов'язкова колонка; зміна складу — видалити рядок і створити новий у його архетипі, перенісши значення PK/FK" :verified-by :read-whole}
  {:id :arch-63 :at "144 birth-completeness :never" :kind :code :rule "ніколи RemoveComponent, RemoveTag, пізній AddComponent колонки, якої архетип не називає (вона виносить рядок з архетипу)" :verified-by :read-whole}
  {:id :arch-64 :at "147 view-boundary :view" :kind :code :rule "MonoBehaviour view ніколи не створює сутність, не піднімає подію і не отримує EntityStorages чи EntityStore" :verified-by :read-whole}
  {:id :arch-65 :at "148 view-boundary :instead" :kind :code :rule "натомість view піднімає C#-подію; її керівна система підписується і пише в сховище" :verified-by :read-whole}
  {:id :arch-66 :at "152 component-writes :write" :kind :code :rule "запис — entity.AddComponent(value), upsert; за birth-completeness завжди простий запис значення" :verified-by :read-whole}
  {:id :arch-67 :at "153-154 component-writes :never" :kind :code :rule "ніколи мутація через ref у сховище компонентів (індекс перекладає рядок лише на виклик запису); ніколи GetComponent<T>() як дескриптор для мутації" :verified-by :read-whole}
  {:id :arch-68 :at "155 component-writes :change-only" :kind :code :rule "лише при зміні: спершу порівняти, писати при відмінності — перезапис того самого значення даремно перекладає індексовані рядки" :verified-by :read-whole}
  {:id :arch-69 :at "158 links :persistent" :kind :code :rule "стійке посилання — доменний ключ: …IdComponent власника, на який посилаються …FKComponent цього простору; ніколи збережений Entity" :verified-by :read-whole}
  {:id :arch-70 :at "159 links :runtime-only" :kind :code :rule "лише runtime-посилання (не серіалізоване, пов'язане часом життя, зазвичай шар view) може тримати пряме посилання або Entity" :verified-by :read-whole}
  {:id :arch-71 :at "160 links :resolution" :kind :code :rule "пошук за ключем, що промахнувся, кидає виняток" :verified-by :read-whole}
  {:id :arch-72 :at "166 event-lifecycle :archetype" :kind :definition :rule "архетип події = EventTag + EventFrameComponent + компонент події, розв'язаний EventArchetypes.Of<T>(store)" :verified-by :read-whole}
  {:id :arch-73 :at "167 event-lifecycle :raise" :kind :code :rule "підняти подію — store.CreateEvent(new TEvent { … }); кадр штампується всередині виклику створення" :verified-by :read-whole}
  {:id :arch-74 :at "168 event-lifecycle :ripe" :kind :code :rule "споживач діє лише поки IsRipe(entity) — кадр після народження" :verified-by :read-whole}
  {:id :arch-75 :at "169 event-lifecycle :delivery" :kind :definition :rule "кожен споживач бачить подію рівно раз, незалежно від пріоритету системи" :verified-by :read-whole}
  {:id :arch-76 :at "170 event-lifecycle :cleanup" :kind :definition :rule "EventCleanupSystem з Priority int.MaxValue видаляє дозрілі події в кінці того кадру" :verified-by :read-whole}
  {:id :arch-77 :at "171 event-lifecycle :priorities" :kind :code :rule "пріоритет — лише порядок виконання; ніколи правило, що споживач стоїть вище чи нижче виробника" :verified-by :read-whole}
  {:id :arch-78 :at "177 store-thread :main-thread-only" :kind :code :rule "кожен виклик сховища — лише головний потік: створення, AddComponent, GetComponent, видалення, пошук в індексі, підняття події" :verified-by :read-whole}
  {:id :arch-79 :at "178 store-thread :off-thread" :kind :code :rule "поза головним потоком — лише обчислення над простими даними й native-контейнерами; читання полів ScriptableObject-конфіга — прості дані" :verified-by :read-whole}
  {:id :arch-80 :at "179-180 store-thread :bridge :bridge-cost" :kind :code :rule "міст: await UniTask.SwitchToMainThread() перед першим викликом сховища; SwitchToThreadPool для подальших обчислень; перехід на головний потік продовжується на наступному Update — близько кадру" :verified-by :read-whole}
  {:id :arch-81 :at "181 store-thread :never" :kind :code :rule "ніколи запис у сховище поза головним потоком — індексована колонка перекладає рядок і псує індекс" :verified-by :read-whole}
  {:id :arch-82 :at "184-185 structural-change :is :is-not" :kind :definition :rule "структурна зміна — AddComponent, RemoveComponent, AddTag, RemoveTag; народження архетипом — ні" :verified-by :read-whole}
  {:id :arch-83 :at "186 structural-change :throws" :kind :code :rule "структурна зміна всередині перелічення кидає виняток — також AddComponent у колонку, яку архетип уже має, і для непов'язаної сутності; сторож — на все сховище" :verified-by :read-whole}
  {:id :arch-84 :at "187 structural-change :delete" :kind :code :rule "видалення — не структурна зміна, але змінює перелічувану множину" :verified-by :read-whole}
  {:id :arch-85 :at "188-191 structural-change :idiom" :kind :code :rule "ідіома: зібрати entity.Id у NativeList<int>, закрити перелічення, знову взяти кожен id через store.TryGetEntityById, створювати й писати в другому проході" :verified-by :read-whole}
  {:id :arch-86 :at "192 structural-change :never" :kind :code :rule "ніколи знімок значень Entity — Entity не unmanaged" :verified-by :read-whole}
  {:id :arch-87 :at "193 structural-change :base" :kind :definition :rule "UpdatedSystem уже робить знімок для своїх нащадків" :verified-by :read-whole}
  {:id :arch-88 :at "200-203 code-shape :structure :pattern-only-when" :kind :code :rule "найпростіша структура, що розв'язує задачу; патерн — лише коли його вимагають кардинальність чи реальна складність, ніколи тому, що про нього згадує документ чи коментар (множині з одного елемента не потрібен знімок; однорядковій заміні тегу — сімейство підсистем)" :verified-by :read-whole}
  {:id :arch-89 :at "204-205 code-shape :comments :home :only-where" :kind :code :rule "коментар — короткий, на самій речі (заголовок класу / метод), оновлюється в тому ж диффі: намір, неочевидні інваріанти, контракти; лише там, де логіка перестає бути простою" :verified-by :read-whole}
  {:id :arch-90 :at "206-207 code-shape :comments :never" :kind :code :rule "ніколи коментар про ІНШИЙ файл (лише посилання за ім'ям або перенести факт до власника); ніколи шаблонна XML-документація" :verified-by :read-whole}
  {:id :arch-91 :at "210 naming :suffix" :kind :code :rule "суфікси: дані …Component, тег …Tag, подія …Event, FK …FKComponent — ім'я ключа власника з FK перед Component" :verified-by :read-whole}
  {:id :arch-92 :at "211-214 naming :type-name" :kind :code :rule "ім'я типу зрозуміле без простору імен (C# FDG); ім'я фічі повторюється завжди (<Feature>CostConfig, ніколи голий Config); прибирається лише чистий доменний префікс; ніколи розрізнення через using-псевдонім чи кваліфікатор простору імен" :verified-by :read-whole}
  {:id :arch-93 :at "215 naming :prefix-exceptions" :kind :code :rule "виняток для префікса: FK/PK-компонент ідентичності або дискримінатор таблиці, на які посилаються з інших доменів; DI-інсталер <Domain>Installer" :verified-by :read-whole}
  {:id :arch-94 :at "216 naming :static" :kind :code :rule "static — лише для stateless-утиліти без полів" :verified-by :read-whole}
  {:id :arch-95 :at "219 output-methods :may-fail" :kind :code :rule "метод, що може не вдатися: bool Try…(in вхід, out або ref вихід); викликач розгалужується за bool" :verified-by :read-whole}
  {:id :arch-96 :at "220 output-methods :sure-single-output" :kind :code :rule "певний один результат — повернути значення" :verified-by :read-whole}
  {:id :arch-97 :at "221-222 output-methods :never" :kind :code :rule "ніколи виводити успіх з результату (null, Count == 0, Length == 0); ніколи передавати за значенням колекцію, яку метод пише — за ref" :verified-by :read-whole}
  {:id :arch-98 :at "223 output-methods :inline" :kind :code :rule "одноразовий private-helper, що читає поля й розгортає лінійний потік викликача, лишається inline; виносити лише самодостатній за сигнатурою метод, що перевикористовується" :verified-by :read-whole}
  {:id :arch-99 :at "226 fail-loud :rule" :kind :code :rule "крок, що не може виконати роботу, кидає описовий виняток з назвою того, чого бракує" :verified-by :read-whole}
  {:id :arch-100 :at "227 fail-loud :never" :kind :code :rule "ніколи тихий return чи return UniTask.CompletedTask на відсутній передумові; ніколи Debug.LogWarning і пропуск" :verified-by :read-whole}
  {:id :arch-101 :at "228 fail-loud :validate" :kind :code :rule "валідувати в джерелі (IValidatableConfig.Validate) і знову в споживачі" :verified-by :read-whole}
  {:id :arch-102 :at "229 fail-loud :cancel" :kind :code :rule "скасування — cancellationToken.ThrowIfCancellationRequested() окремим рядком; ніколи тихий return" :verified-by :read-whole}
  {:id :arch-103 :at "230 fail-loud :cleanup" :kind :code :rule "звільняти те, чим володіє скасована робота, у try/finally; прибрати часткову роботу перед винятком" :verified-by :read-whole}
  {:id :arch-104 :at "231 fail-loud :exception" :kind :code :rule "виняток за замовчуванням — InvalidOperationException з повідомленням" :verified-by :read-whole}
  {:id :arch-105 :at "237 deviations :record" :kind :code :rule "відоме відхилення записується коментарем у коді в місці відхилення" :verified-by :read-whole}
  {:id :arch-106 :at "238 deviations :never" :kind :non-code :rule "ніколи перелік відхилень у документі — правило документа" :verified-by :read-whole}
  {:id :arch-107 :at "55, 109, 149 :checked-by" :kind :non-code :rule "вказівники на інструмент перевірки (/arch-check, fantasymayor-graph tags, MarkerShapeAnalyzer, pattern PATTERN_VIEW_SYSTEM) — не правило, метадані примусу" :verified-by :read-whole}]}
```

## fantasymayor-placement

```clojure
{:carrier ".claude/skills/fantasymayor-placement/SKILL.md" :lines 124 :verified-by :read-whole
 :entries
 [{:id :plc-00 :at "13-19 placement (procedure)" :kind :non-code :rule "процедура розміщення нових файлів — процес" :verified-by :read-whole}
  {:id :plc-01 :at "25-27 layers :domain" :kind :code :rule "domain — Assets/Domains/<Domain>/: правила гри одного bounded context (таблиці сутностей і логіка, що їх змінює); ніколи залежність від presentation; ніколи view, prefab, текстури" :verified-by :read-whole}
  {:id :plc-02 :at "28-30 layers :presentation" :kind :code :rule "presentation — Assets/Presentation/ і Assets/Presentation/UI/: показ доменного стану і прийом наміру гравця; ніколи правило гри, стан, яким володіє домен, пряма зміна доменної таблиці — лише командна подія" :verified-by :read-whole}
  {:id :plc-03 :at "31-33 layers :module" :kind :code :rule "module — Assets/Modules/<Module>/: чиста логіка біля рушія, що не знає правил цієї гри; ніколи правило гри" :verified-by :read-whole}
  {:id :plc-04 :at "34-35 layers :shared-kernel" :kind :code :rule "shared kernel — Assets/Scripts/Core/ і Assets/Scripts/EcsExtensions/: примітиви й базові ECS-типи для всіх шарів" :verified-by :read-whole}
  {:id :plc-05 :at "36-37 layers :app-root" :kind :di :rule "app-root — Assets/Scripts/Installers/: DI-композиція всього застосунку" :verified-by :read-whole}
  {:id :plc-06 :at "40 depends :shared-kernel" :kind :code :rule "shared kernel залежить лише від зовнішніх бібліотек і рушія" :verified-by :read-whole}
  {:id :plc-07 :at "41-43 depends :module" :kind :code :rule "module: ідеально без залежностей; може — інший module, shared kernel; ніколи domain чи presentation" :verified-by :read-whole}
  {:id :plc-08 :at "44-47 depends :layer->module" :kind :code :rule "domain і presentation посилаються на будь-яку кількість модулів, але лише на Core/-збірку контракту або module з одним asmdef; ніколи на Implementation/-збірку" :verified-by :read-whole}
  {:id :plc-09 :at "48 depends :layer->shared-kernel" :kind :code :rule "будь-який шар може посилатися на shared kernel" :verified-by :read-whole}
  {:id :plc-10 :at "49-50 depends :presentation->domain :domain->presentation" :kind :code :rule "presentation → domain в один бік (на те, що показує); domain → presentation ніколи" :verified-by :read-whole}
  {:id :plc-11 :at "51-55 depends :domain->domain" :kind :code :rule "домени мають яруси substrate → agents → verbs: описує, що робить власник, — verbs; знає конкретного власника — agents; інакше substrate; домен посилається лише на свій або попередній ярус" :verified-by :read-whole}
  {:id :plc-12 :at "56 depends :cycles" :kind :code :rule "циклів залежностей нема" :verified-by :read-whole}
  {:id :plc-13 :at "57 depends :exempt" :kind :code :rule "Boot.Implementation — поза всіма правилами залежностей (складає game state)" :verified-by :read-whole}
  {:id :plc-14 :at "58 depends :check" :kind :non-code :rule "перевірка через Tools/asmdef_reach.py — інструмент" :verified-by :read-whole}
  {:id :plc-15 :at "59 depends :namespace" :kind :code :rule "простір імен іде за шляхом папки" :verified-by :read-whole}
  {:id :plc-16 :at "66 place :domain-rule" :kind :code :rule "доменне правило — у Assets/Domains/<Domain>/<Feature>/ домену, що ним володіє; новий домен — лише рішенням власника" :verified-by :read-whole}
  {:id :plc-17 :at "67-69 place :world-view :hud :infra" :kind :code :rule "вигляд світу — Assets/Presentation/<SubArea>/; HUD — Assets/Presentation/UI/<Window>/; інфраструктура — Assets/Modules/<Module>/" :verified-by :read-whole}
  {:id :plc-18 :at "70 place :shared-kernel" :kind :code :rule "у shared kernel — лише код, потрібний кільком шарам, що не знає жодного з них" :verified-by :read-whole}
  {:id :plc-19 :at "71-76 place :installer :app-root" :kind :di :rule "де лежить інсталер для domain / presentation / module / пари Core+Implementation / ядра рушія; app-root лише для композиції застосунку" :verified-by :read-whole}
  {:id :plc-20 :at "77 place :config-asset" :kind :asset :rule "ассет конфіга — Assets/Addressables/Configs/" :verified-by :read-whole}
  {:id :plc-21 :at "78 place :ui-assets" :kind :asset :rule "prefab, uxml, uss — <Area>/Prefabs/" :verified-by :read-whole}
  {:id :plc-22 :at "85-86 folder-layout :domain" :kind :code :rule "domain — один asmdef на домен у Assets/Domains/<Domain>/; фіча — Assets/Domains/<Domain>/<Feature>/" :verified-by :read-whole}
  {:id :plc-23 :at "87-88 folder-layout :presentation" :kind :code :rule "збірки presentation — Assets/Presentation/ і Assets/Presentation/UI/; області — <Assembly>/<SubArea>/ і Assets/Presentation/UI/<Window>/<Panel>/" :verified-by :read-whole}
  {:id :plc-24 :at "89-90 folder-layout :single-module" :kind :code :rule "module з одним asmdef — модуль є однією фічею; рольові папки прямо під коренем збірки" :verified-by :read-whole}
  {:id :plc-25 :at "91-92 folder-layout :pair-module" :kind :code :rule "парний module — збірки Core/ і Implementation/; рольові папки всередині кожної" :verified-by :read-whole}
  {:id :plc-26 :at "93 folder-layout Archetypes/" :kind :code :rule "Archetypes/ — прямо під коренем збірки і лише коли збірка оголошує архетипи" :verified-by :read-whole}
  {:id :plc-27 :at "94-95 folder-layout Installer/" :kind :di :rule "Installer/ прямо під коренем domain, presentation чи module з одним asmdef; ніколи у фічі, області чи парному модулі" :verified-by :read-whole}
  {:id :plc-28 :at "96 folder-layout :shared-role" :kind :code :rule "спільна рольова папка прямо під коренем domain чи presentation — лише коли її вміст ділять кілька фіч чи областей" :verified-by :read-whole}
  {:id :plc-29 :at "97 folder-layout :never" :kind :code :rule "ніколи код поза рольовою папкою" :verified-by :read-whole}
  {:id :plc-30 :at "100 role-folders :where" :kind :code :rule "рольові папки — усередині фічі, області чи збірки модуля; прямо під коренем domain чи presentation — лише як спільна роль" :verified-by :read-whole}
  {:id :plc-31 :at "101 role-folders Components/" :kind :code :rule "Components/ — чисті структури даних; ніколи логіка чи побічні ефекти" :verified-by :read-whole}
  {:id :plc-32 :at "102 role-folders Tags/" :kind :code :rule "Tags/ — тег-компоненти" :verified-by :read-whole}
  {:id :plc-33 :at "103 role-folders Events/" :kind :code :rule "Events/ — однокадрові компоненти подій" :verified-by :read-whole}
  {:id :plc-34 :at "104 role-folders Configs/" :kind :code :rule "Configs/ — класи ScriptableObject; ніколи runtime-логіка чи ассети конфігів" :verified-by :read-whole}
  {:id :plc-35 :at "105 role-folders Data/" :kind :code :rule "Data/ — колекції, записи, enum; ніколи ECS-системи чи MonoBehaviour" :verified-by :read-whole}
  {:id :plc-36 :at "106 role-folders Systems/" :kind :code :rule "Systems/ — ECS-системи й оркестрація; ніколи логіка view чи визначення конфігів" :verified-by :read-whole}
  {:id :plc-37 :at "107 role-folders Helpers/" :kind :code :rule "Helpers/ — stateless-обчислення, єдина назва цієї ролі; ніколи стан між кадрами чи володіння сутностями" :verified-by :read-whole}
  {:id :plc-38 :at "108 role-folders Views/" :kind :code :rule "Views/ — шар MonoBehaviour view; ніколи бізнес-логіка; лише в presentation" :verified-by :read-whole}
  {:id :plc-39 :at "109-110 role-folders Prefabs/ Textures/" :kind :asset :rule "Prefabs/ (prefab, uxml, uss) і Textures/ — лише в presentation" :verified-by :read-whole}
  {:id :plc-40 :at "117-122 di-composition :root … :per-frame-system" :kind :di :rule "WorldInstaller — єдиний LifetimeScope; корінь першим реєструє ядро рушія; інсталер — простий клас : IInstaller (Mono лише з [SerializeField]); порядок у InstallModules; per-frame система реєструється конкретним типом, ніколи As<IUpdatedSystem>, Boot вписує вручну" :verified-by :read-whole}
  {:id :plc-41 :at "123 di-composition :public-api-module" :kind :code :rule "module з публічним API — збірка контракту Core/ + збірка Implementation/; споживачі посилаються лише на Core/" :same-as [:plc-08 :plc-25] :verified-by :read-whole}]}
```

## fantasymayor-pattern-choice

```clojure
{:carrier ".claude/skills/fantasymayor-pattern-choice/SKILL.md" :lines 130 :verified-by :read-whole
 :entries
 [{:id :pch-00 :at "13-20 pattern-choice (procedure)" :kind :non-code :rule "процедура вибору рецепта при постановці — процес" :verified-by :read-whole}
  {:id :pch-01 :at "26-33 recipe-tree" :kind :code :rule "частина коду: ECS-дані → рецепти даних; конфіг → рецепти конфігів; поведінка → рецепти поведінки; view → PATTERN_VIEW_SYSTEM; addressables → ADDRESSABLE_PATTERNS; інакше рецепта нема" :verified-by :read-whole}
  {:id :pch-02 :at "35-39 data-recipes" :kind :code :rule "несе значення → компонент; позначає ідентичність рядка → тег; сигналізує зміну → подія" :verified-by :read-whole}
  {:id :pch-03 :at "41-44 config-recipes" :kind :code :rule "багато видів у таблицю сутностей → поліморфний каталог; завантажується чи будується на старті → конфіг + loader; інакше конфіг" :verified-by :read-whole}
  {:id :pch-04 :at "46-55 behavior-recipes" :kind :code :rule "охоплює кілька піддоменів → transaction entity; будує світ при створенні мапи з незалежно впорядкованими частинами → orchestrator + subsystem; будує світ при створенні мапи → pipeline stage; фаза ходу → orchestrator + subsystem; реагує на подію з незалежними частинами → reactive orchestrator; реагує на подію → reactive; неперервно щокадру → per-frame; прибирає події → cleanup" :verified-by :read-whole}
  {:id :pch-05 :at "64-65 kernel-tree :ordering-systems" :kind :code :rule "Priority повертає константу з SystemPriorities, ніколи локальне число; кожен вкладений клас — окремий простір порядку, порівнянний лише всередині; кожна система має окреме значення" :verified-by :read-whole}
  {:id :pch-06 :at "69-70 kernel-tree :looking-up-a-single-row" :kind :code :rule "пошук одного рядка — QueryResultExtensions.TryGetFirst(out entity) на запиті, архетипі чи множині Entities; розгалуження за bool" :verified-by :read-whole}
  {:id :pch-07 :at "71-72 kernel-tree :long-running-async-task" :kind :code :rule "довга async-задача бере StatusMonitor.Token; скасування — ThrowIfCancellationRequested, ніколи тихий return" :verified-by :read-whole}
  {:id :pch-08 :at "76-77 system-bases :runs-once-at-startup" :kind :code :rule "разовий крок старту — IUniTaskSystem: Execute(token) і AppState — прапорець кроку, на якому Boot його запускає" :verified-by :read-whole}
  {:id :pch-09 :at "77 system-bases «register .As<IUniTaskSystem>()»" :kind :di :rule "реєстрація разового кроку як IUniTaskSystem" :verified-by :read-whole}
  {:id :pch-10 :at "78-79 system-bases :runs-in-an-ordered-pipeline" :kind :code :rule "впорядкований конвеєр — IPrioritizedUniTaskSystem<T>: Update(token) і Priority (менший раніше); T — стадія, за якою DI збирає сімейство; якщо сімейство має абстрактну базу — наслідувати її" :verified-by :read-whole}
  {:id :pch-11 :at "80-81 system-bases :must-run-after-every-update" :kind :code :rule "мусить працювати після кожного Update — LateUpdatedSystem, той самий контракт, що UpdatedSystem, з LateUpdate" :verified-by :read-whole}
  {:id :pch-12 :at "82-83 system-bases :else" :kind :code :rule "інакше UpdatedSystem: store і один архетип з тримача (ArchetypeQuery лише коли тригер охоплює кілька архетипів); Update(state, entity) і Priority; структурні зміни в Update безпечні, у переліченні, яке Update відкриває сам, — ні" :verified-by :read-whole}
  {:id :pch-13 :at "86-87 config-kernel :registering-a-config-for-loading" :kind :di :rule "одна реєстрація ConfigLoaderSystem<X> на конфіг з AppState.ConfigLoading і адресою; ніколи рукописний loader" :verified-by :read-whole}
  {:id :pch-14 :at "88-89 config-kernel ConfigAddresses" :kind :code :rule "адреса addressable — константа в ConfigAddresses з ім'ям запису, як його названо в Unity; передається константа, ніколи рядковий літерал" :verified-by :read-whole}
  {:id :pch-15 :at "90-91 config-kernel :authoring-a-config-type" :kind :code :rule "тип конфіга реалізує IValidatableConfig: Validate() кидає на кожне порушення авторингу; викликається одразу після завантаження" :verified-by :read-whole}
  {:id :pch-16 :at "95-96 event-kernel :raising-an-event" :kind :code :rule "підняти подію — store.CreateEvent(new TEvent { … }): рядок народжується в архетипі події зі значеннями; ніколи AddComponent на живу сутність" :same-as [:arch-73] :verified-by :read-whole}
  {:id :pch-17 :at "97-100 event-kernel :consuming-an-event" :kind :code :rule "споживач якориться на EventArchetypes.Of<TEvent>(store) і тримає його; обробляє лише поки IsRipe(entity); значення — entity.GetComponent<TEvent>()" :same-as [:arch-05 :arch-74] :verified-by :read-whole}
  {:id :pch-18 :at "104-105 state-kernel :system-needs-an-instance-field" :kind :code :rule "поле екземпляра системи — [StateAllowed(«причина»)]; arch-check пропускає лише такі поля" :verified-by :read-whole}
  {:id :pch-19 :at "106-107 state-kernel :value-valid-for-bounded-frames" :kind :code :rule "значення на обмежену кількість кадрів — FrameBox<T>.OneFrame / TwoFrames / ForFrames(value, n); Value читати лише після Exist (застаріле кидає); Dispose при знятті" :verified-by :read-whole}
  {:id :pch-20 :at "114-120 instances" :kind :non-code :rule "як шукати живі реалізації (fmgraph.py systems / pattern, roslyn); шаблон — рецепт, не жива реалізація; перелік реалізацій не пишеться в документ — процес і правило документа" :verified-by :read-whole}
  {:id :pch-21 :at "126-129 tag-law-before" :kind :non-code :rule "перед новим тегом — fmgraph.py explain і звірка з tag-law — процес" :verified-by :read-whole}]}
```

## arch-check

```clojure
{:carrier "~/.claude/skills/arch-check/SKILL.md" :lines 196 :verified-by :read-whole
 :entries
 [{:id :ach-01 :at "13-14 ban 1" :kind :code :rule "клас системи не тримає змінного стану екземпляра" :same-as [:arch-11] :verified-by :read-whole}
  {:id :ach-02 :at "15-16 ban 2" :kind :code :rule "System.Collections.Generic заборонено в системах, що працюють повторно (per-frame, reactive, turn-phase); разові (startup, config loader, стадії й підсистеми створення мапи) можуть" :same-as [:arch-17 :arch-18] :verified-by :read-whole}
  {:id :ach-03 :at "17-21 ban 3" :kind :code :rule "zero-allocation у повторних системах; native-алокатор за часом життя в кожній системі; порушення — помилка" :same-as [:arch-18 :arch-21 :arch-24] :verified-by :read-whole}
  {:id :ach-04 :at "52-58 System cadence" :kind :definition :rule "repeated — наслідує UpdatedSystem / LateUpdatedSystem / IUpdatedSystem / ILateUpdatedSystem, EventCleanupSystem або базу turn phase; one-shot — IUniTaskSystem (включно з ConfigLoaderSystem<T>), IPrioritizedUniTaskSystem<T> pipeline stage, підсистема разового оркестратора; async — робота в async UniTask; інакше каденція неясна" :verified-by :read-whole}
  {:id :ach-05 :at "62-77 Rule 1" :kind :code :rule "заборонені using System.Collections.Generic і повні імена System.Collections.Generic.*: List, Dictionary, HashSet, Queue, Stack, LinkedList, SortedList, SortedDictionary, SortedSet, IList<>, IDictionary<>, ISet<>, IReadOnlyList<>, IReadOnlyDictionary<>, IReadOnlyCollection<>, ICollection<>, IEnumerable<>; System.Array, Span<> і негенеричні System.Collections — не ця заборона" :verified-by :read-whole}
  {:id :ach-06 :at "80-83 Rule 2 system" :kind :definition :rule "система — клас, що прямо чи транзитивно наслідує UpdatedSystem, LateUpdatedSystem, ConfigLoaderSystem<T>, EventCleanupSystem, IUpdatedSystem, ILateUpdatedSystem, IUniTaskSystem, IPrioritizedUniTaskSystem" :verified-by :read-whole}
  {:id :ach-07 :at "88-96 Rule 2 flag" :kind :code :rule "поле екземпляра без [StateAllowed] — стан, коли (а) не readonly або (б) readonly змінного контейнера чи буфера: List/Dictionary/HashSet/Queue/Stack/LinkedList/Sorted* та їхні I*, масиви, StringBuilder, Native*" :same-as [:arch-13] :verified-by :read-whole}
  {:id :ach-08 :at "97-101 Rule 2 not flagged" :kind :code :rule "не стан: readonly незмінних типів і дескрипторів (DI-залежності, EntityStorages, EntityStore, Archetype, ArchetypeQuery, ComponentIndex<,>, Camera, readonly struct, int), const, static readonly, static-поля, [StateAllowed]; settable авто-властивість екземпляра — стан" :verified-by :read-whole}
  {:id :ach-09 :at "107-108 Rule 2 PreUpdate caches" :kind :code :rule "per-frame кеш, розв'язаний у PreUpdate, — стан; рішення з власником: [StateAllowed(«причина»)] або рефакторинг" :verified-by :read-whole}
  {:id :ach-10 :at "122-126 allocator lifetime errors" :kind :code :rule "помилка: будь-який Allocator.Temp у коді async-системи; Temp-пам'ять у полі чи жива через await; TempJob у полі чи через await, коли звільнення не видно в межах 4 кадрів" :same-as [:arch-21 :arch-22] :verified-by :read-whole}
  {:id :ach-11 :at "129-131 per-call managed garbage" :kind :code :rule "на повторному шляху (цикл, per-frame Update, helper такого шляху) — ніколи new T[…], .ToArray(), .ToList(), матеріалізатори Enumerable чи інший щойно виділений T[] / керована колекція" :verified-by :read-whole}
  {:id :ach-12 :at "132-134 managed collection handed out" :kind :code :rule "система не будує й не повертає керований знімок (T[] чи System.Collections.Generic) — його звільнення лягає на викликача, час життя не названо" :verified-by :read-whole}
  {:id :ach-13 :at "135 no named owner" :kind :code :rule "кожне виділення має названого власника, що його звільняє" :verified-by :read-whole}
  {:id :ach-14 :at "137-138 not on type alone" :kind :code :rule "не порушення за типом: керований буфер, виділений раз і детерміновано звільнений, проходить; утримання між кадрами — питання стану" :verified-by :read-whole}
  {:id :ach-15 :at "140-148 do not flag" :kind :code :rule "не виділення: Span<T> / ReadOnlySpan<T>; Friflo Entities з archetype.Entities, ArchetypeQuery.Entities, ComponentIndex<,>[value]; читання масиву чи List<>, виставленого SO-конфігом чи компонентом; Native* і поля [StateAllowed]" :verified-by :read-whole}
  {:id :ach-16 :at "145-148 zero-alloc relay" :kind :code :rule "передати дані без виділення — штовхати значення по одному споживачу (view.SetXxx) або передати посилання; ніколи керований знімок" :verified-by :read-whole}
  {:id :ach-17 :at "159-161 Rationale" :kind :code :rule "інваріант — детерміноване звільнення, не тип контейнера" :verified-by :read-whole}
  {:id :ach-18 :at "23-24 home; 28-48 Usage, Engine contract, Scope; 150-157 procedures; 163-196 Output" :kind :non-code :rule "дім правил, використання, двигун пошуку rg, межі сканування, процедура, формат звіту — поведінка інструмента" :verified-by :read-whole}
  {:id :ach-19 :at "36-38 Engine contract" :kind :stale :rule "згадки «arch-scout subagent» і «.claude/SEARCH_POLICY.md» — скаутів видалено, файлу нема" :verified-by "ls .claude/SEARCH_POLICY.md → No such file 2026-09-16; .sdd-flow/project.md :subagents :abandoned"}]}
```

## Patterns — дані й конфіги

```clojure
{:carrier "Patterns/PATTERN_COMPONENT.md" :lines 79 :verified-by :read-whole
 :entries
 [{:id :cmp-01 :at "13-14" :kind :code :rule "компонент — проста struct runtime-значень; без поведінки й методів, крім рівності, коли він ключ таблиці" :verified-by :read-whole}
  {:id :cmp-02 :at "18-26 skeleton" :kind :code :rule "struct [Name]Component : IComponent у просторі імен Domains.[Domain].[Feature].Components" :verified-by :read-whole}
  {:id :cmp-03 :at "29-38" :kind :code :rule "компонент-ключ індексу оголошує IIndexedComponent<TValue> і повертає поле ключа з GetIndexedValue()" :same-as [:arch-50] :verified-by :read-whole}
  {:id :cmp-04 :at "40-41" :kind :code :rule "тип ключа порівнюваний: enum як є; struct-ключ сам реалізує IEquatable<T> і GetHashCode (HexCoord)" :same-as [:arch-50] :verified-by :read-whole}
  {:id :cmp-05 :at "41-42" :kind :code :rule "PK, який лише порівнюють і ніколи не індексують, — простий IComponent з IEquatable (приклад у рецепті — привид, типу в коді нема)" :verified-by :read-whole}
  {:id :cmp-06 :at "46-58 FK component" :kind :code :rule "рядок, що посилається на простір ключів іншої таблиці, несе окремий FK-тип, ніколи тип PK власника; FK обгортає те саме значення, що PK власника; FK — індексована форма IIndexedComponent<OwnerKeyType>" :same-as [:arch-41] :verified-by :read-whole}
  {:id :cmp-07 :at "60-61" :kind :code :rule "з'єднання — значення ключа власника шукається в FK-індексі в точці використання" :same-as [:arch-30] :verified-by :read-whole}
  {:id :cmp-08 :at "62; 74" :kind :code :rule "FK-компонент лежить у папці фічі власника поруч із PK — один простір, одна пара типів, визначена раз" :verified-by :read-whole}
  {:id :cmp-09 :at "68 component-rules :write" :kind :code :rule "запис — entity.AddComponent(v) або storages.Singletons.Set(v); ніколи мутація через ref" :same-as [:arch-66 :arch-67] :verified-by :read-whole}
  {:id :cmp-10 :at "69 component-rules :declare" :kind :code :rule "компонент оголошується struct : IComponent — невидимий рушію компонент мовчки нічого не робить при народженні" :verified-by :read-whole}
  {:id :cmp-11 :at "70-71 component-rules :naming-data :naming-fk" :kind :code :rule "імена: …Component; безполевий маркер — …Tag; однокадровий імпульс — …Event; …FKComponent — єдиний законний тип посилання між таблицями" :same-as [:arch-91] :verified-by :read-whole}
  {:id :cmp-12 :at "72 component-rules :naming-prefix" :kind :code :rule "без доменного префікса; виняток — FK/PK-компоненти ідентичності" :same-as [:arch-93] :verified-by :read-whole}
  {:id :cmp-13 :at "73 component-rules :singleton-component" :kind :code :rule "singleton-компонент не збігається з запитом: прихований рядок має оголошений архетип, але споживачі читають лише storages.Singletons.Get<T>()" :verified-by :read-whole}
  {:id :cmp-14 :at "74-77 component-rules :index-key :index-bucket :fk-per-space :birth-column" :kind :code :rule "ключ індексу — IIndexedComponent<TValue> з порівнюваним TValue; не більше 100 сутностей на значення ключа; один FK на простір; кожну колонку називає архетип" :same-as [:arch-48 :arch-49 :arch-50 :arch-60] :verified-by :read-whole}
  {:id :cmp-15 :at "78 component-rules :component-shape" :kind :non-code :rule "поля й типи компонентів виводять інструменти — не переписувати в документах модулів" :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/PATTERN_TAG.md" :lines 43 :verified-by :read-whole
 :entries
 [{:id :tag-01 :at "13" :kind :code :rule "тег — порожня struct, що позначає сутність; даних не несе, його присутність і є інформацією" :verified-by :read-whole}
  {:id :tag-02 :at "14-16" :kind :code :rule "два вжитки: головний тег — дискримінатор таблиці, рівно один на архетип і унікальний; label-тег поруч із ним з [TagLabel]" :same-as [:arch-32 :arch-33 :arch-34] :verified-by :read-whole}
  {:id :tag-03 :at "20-28 skeleton" :kind :code :rule "struct [Name]Tag : ITag у просторі імен Domains.[Domain].[Feature].Tags" :verified-by :read-whole}
  {:id :tag-04 :at "34 tag-rules :add" :kind :code :rule "тег додається лише через оголошення архетипу таблиці — Tags.Get<[Name]Tag>() у методі тримача; тег живій сутності не додається (виносить рядок з архетипу)" :verified-by :read-whole}
  {:id :tag-05 :at "35 tag-rules :per-entity" :kind :code :rule "на сутність: головний 1, label 0-4; EventTag — головний тег кожної події" :same-as [:arch-35 :arch-36] :verified-by :read-whole}
  {:id :tag-06 :at "36 tag-rules :label" :kind :code :rule "label — [TagLabel] або [TagLabel(TagLabelRole.X)] на структурі; у Tags.Get після головного тегу; ніколи фільтр запиту" :same-as [:arch-32 :arch-35] :verified-by :read-whole}
  {:id :tag-07 :at "37 tag-rules :query-table" :kind :code :rule "таблиця для запиту — [Domain]Archetypes.[Table](store); ключ і дискримінатор оголошені раз і перевикористані для народження й фільтра; ніколи голий ключ" :same-as [:arch-26] :verified-by :read-whole}
  {:id :tag-08 :at "38 tag-rules :never-state" :kind :code :rule "маркер, що перемикається в runtime, — …StateComponent над enum; склад фіксований при народженні, заміна — видалити й створити" :same-as [:arch-37 :arch-62] :verified-by :read-whole}
  {:id :tag-09 :at "39 tag-rules :never-kind" :kind :code :rule "маркер підтипу в сімействі таблиць — …KindComponent над enum; label ніколи не означає вид" :same-as [:arch-38] :verified-by :read-whole}
  {:id :tag-10 :at "40 tag-rules :naming" :kind :code :rule "суфікс …Tag, тег безполевий; щойно потрібне значення — це компонент" :verified-by :read-whole}
  {:id :tag-11 :at "41 tag-rules :naming-prefix" :kind :code :rule "без доменного префікса; виняток — дискримінатори Table Rule" :same-as [:arch-93] :verified-by :read-whole}
  {:id :tag-12 :at "42 tag-rules :producers+consumers" :kind :non-code :rule "теги не перелічувати в документах модулів — правило документа" :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/PATTERN_EVENT.md" :lines 60 :verified-by :read-whole
 :entries
 [{:id :evt-01 :at "15" :kind :code :rule "подія — struct, піднята на власній сутності; її поля — значення, потрібні споживачу" :same-as [:arch-04] :verified-by :read-whole}
  {:id :evt-02 :at "16-17" :kind :definition :rule "кожен reactive-споживач бачить подію рівно раз, у кадрі після підняття, за будь-яких пріоритетів; cleanup видаляє її в кінці того кадру" :same-as [:arch-74 :arch-75 :arch-76] :verified-by :read-whole}
  {:id :evt-03 :at "22-31 skeleton" :kind :code :rule "struct [Name]Event : IComponent у просторі імен Domains.[Domain].[Feature].Events" :verified-by :read-whole}
  {:id :evt-04 :at "35-38; 49 :raise" :kind :code :rule "підняти — одним викликом store.CreateEvent(new [Name]Event { … }): штамп кадру + компонент + EventTag в архетип події; ніколи не збирати імпульс вручну" :same-as [:arch-73] :verified-by :read-whole}
  {:id :evt-05 :at "40-42; 50-51 :view-source" :kind :code :rule "view не піднімає імпульс для своєї системи — локальна C#-подія; ECS-імпульс лише для перетину межі кадру чи asmdef, і піднімає його СИСТЕМА, не view" :verified-by :read-whole}
  {:id :evt-06 :at "52 :startup-bulk-work" :kind :code :rule "стартова масова робота — pipeline stage, ніколи подія: однокадрові події не переживають async-конвеєр створення мапи" :verified-by :read-whole}
  {:id :evt-07 :at "53 :naming" :kind :code :rule "суфікс …Event, папка Events/, без доменного префікса" :same-as [:arch-91] :verified-by :read-whole}
  {:id :evt-08 :at "54-55 :visibility :latency" :kind :code :rule "видимість не залежить від пріоритету; ланцюг коштує кадр на ланку; споживач не розраховує на реакцію в тому ж кадрі" :same-as [:arch-07 :arch-77] :verified-by :read-whole}
  {:id :evt-09 :at "56 :feedback-loop" :kind :code :rule "ніколи петля populate → command → populate на однокадрових подіях — дані мають текти в один бік" :verified-by :read-whole}
  {:id :evt-10 :at "57 :raise-thread" :kind :code :rule "піднімати подію лише з головного потоку; поза потоком — повернутися перед підняттям" :same-as [:arch-78] :verified-by :read-whole}
  {:id :evt-11 :at "58 :lossy-producer" :kind :code :rule "виробник, що не контролює вікно кадру (turn phase, async), — дзвінок за рівнем: перепіднімати кожен хід чи тік, поки умова тримається; споживач reconcile і не довіряє одній доставці" :verified-by :read-whole}
  {:id :evt-12 :at "59 :producer->consumer" :kind :non-code :rule "виробник → споживач шукати в fantasymayor-graph — вказівник на інструмент" :verified-by :read-whole}
  {:id :evt-13 :at "56, 58 коментарі «proven 2026-07-08», «decreed: FLOW_DISTRICT_BUILD»" :kind :stale :rule "історія й посилання на неіснуючий FLOW — не частина правила" :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/PATTERN_CLEANUP_SYSTEM.md" :lines 36 :verified-by :read-whole
 :entries
 [{:id :cln-01 :at "14-17" :kind :code :rule "один глобальний EventCleanupSystem (EcsExtensions): per-frame, останній у тіку, видаляє кожну дозрілу сутність з EventTag" :same-as [:arch-76] :verified-by :read-whole}
  {:id :cln-02 :at "16-25" :kind :code :rule "подію віддають прибиранню лише підняттям через helper CreateEvent — більше нічого" :verified-by :read-whole}
  {:id :cln-03 :at "31 :per-event-cleanup" :kind :code :rule "ніколи власне прибирання для події; EventCleanupSystem без нащадків" :verified-by :read-whole}
  {:id :cln-04 :at "32 EventCleanupSystem" :kind :code :rule "EventCleanupSystem працює останнім з int.MaxValue і видаляє лише дозрілі події" :same-as [:arch-76] :verified-by :read-whole}
  {:id :cln-05 :at "33 :hand-rolled-pulse" :kind :code :rule "імпульс, зібраний без CreateEvent, губить EventTag чи штамп кадру — ніколи не дозріває і тече" :verified-by :read-whole}
  {:id :cln-06 :at "34 :persistent-data-entity" :kind :code :rule "стійка сутність даних ніколи не несе EventTag" :verified-by :read-whole}
  {:id :cln-07 :at "35 :sweep-is-cross-archetype" :kind :code :rule "обхід за EventTag — єдиний свідомо міжархетипний фільтр (усі типи подій разом)" :same-as [:arch-29] :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/PATTERN_CONFIG.md" :lines 68 :verified-by :read-whole
 :entries
 [{:id :cfg-01 :at "13; 52-54" :kind :code :rule "конфіг — ScriptableObject у EntityStorages за типом; читання storages.Get<T>(), що кидає, коли конфіг не завантажено" :verified-by :read-whole}
  {:id :cfg-02 :at "17-38 skeleton" :kind :code :rule "sealed class : ScriptableObject з [CreateAssetMenu(fileName, menuName FantasyMayor/[Group]/[ConfigName])], [SerializeField] private поле + властивість лише для читання; Validate кидає InvalidOperationException" :verified-by :read-whole}
  {:id :cfg-03 :at "30" :kind :code :rule "IValidatableConfig реалізувати лише тоді, коли авторські дані можуть бути хибними" :verified-by :read-whole}
  {:id :cfg-04 :at "40-45" :kind :code :rule "контейнер-каталог — SO з масивом під-конфігів SO" :verified-by :read-whole}
  {:id :cfg-05 :at "47-48; 64 :key" :kind :code :rule "ключ сховища — тип: один збережений примірник на тип; два ассети однієї форми — абстрактна база і sealed-нащадок на кожен" :verified-by :read-whole}
  {:id :cfg-06 :at "60 :storage" :kind :code :rule "storages.Add<T> при завантаженні, storages.Get<T> скрізь інде; ніколи …ConfigComponent, ніколи таблиця сутностей для запитів" :verified-by :read-whole}
  {:id :cfg-07 :at "61 :read" :kind :code :rule "читати сам SO — без копії, без сплющення; без Has-сторожа й тихого пропуску навколо Get<T>" :verified-by :read-whole}
  {:id :cfg-08 :at "62 :lifetime" :kind :code :rule "конфіг завантажується раз на AppState.ConfigLoading і ніколи не звільняється" :verified-by :read-whole}
  {:id :cfg-09 :at "63 :mutation" :kind :code :rule "конфіг ніколи не мутується; масиви спільні — лише читання, без захисного клонування" :verified-by :read-whole}
  {:id :cfg-10 :at "65 :validation" :kind :code :rule "IValidatableConfig.Validate на SO; loader викликає її перед Add; один запис на тип, без null, не порожнє — кидати" :same-as [:arch-101] :verified-by :read-whole}
  {:id :cfg-11 :at "66 :derived" :kind :code :rule "об'єкти, збудовані з конфіга, належать системі AppState.InstanceObjects" :verified-by :read-whole}
  {:id :cfg-12 :at "67 :off-thread" :kind :code :rule "читати поля SO всередині RunOnThreadPool дозволено" :same-as [:arch-79] :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/PATTERN_CONFIG_LOADER.md" :lines 76 :verified-by :read-whole
 :entries
 [{:id :ldr-01 :at "17-22; 58-62 реєстрації" :kind :di :rule "builder.Register<ConfigLoaderSystem<X>>(Lifetime.Singleton).As<IUniTaskSystem>().WithParameter(AppState.ConfigLoading).WithParameter(address, …); реєстрація InstanceObjects-системи" :verified-by :read-whole}
  {:id :ldr-02 :at "24-26" :kind :code :rule "ConfigLoaderSystem<T> завантажує ассет, кидає при невдачі, викликає Validate(), якщо SO реалізує IValidatableConfig, кладе SO в EntityStorages і ніколи не звільняє; без нащадків і файлу loader на конфіг" :verified-by :read-whole}
  {:id :ldr-03 :at "30-56 InstanceObjects skeleton" :kind :code :rule "система похідних об'єктів — [UsedImplicitly] sealed : IUniTaskSystem з AppState у конструкторі; Execute(token) читає конфіг і пише похідний singleton-компонент через Singletons.Set" :verified-by :read-whole}
  {:id :ldr-04 :at "68 :loader" :kind :code :rule "loader конфіга — лише ConfigLoaderSystem<T>" :verified-by :read-whole}
  {:id :ldr-05 :at "69 :address" :kind :code :rule "адреса — const у ConfigAddresses (Ecs.Extensions), SCREAMING_SNAKE_CASE, названа за типом конфіга, значення — ім'я запису addressable; ніколи рядковий літерал" :same-as [:pch-14] :verified-by :read-whole}
  {:id :ldr-06 :at "70 :runner" :kind :di :rule "Boot запускає кожну IUniTaskSystem за прапорцем AppState — ConfigLoading, InstanceObjects, MainMenu — у порядку реєстрації" :verified-by :read-whole}
  {:id :ldr-07 :at "71 :validation" :kind :code :rule "валідація — у SO (IValidatableConfig), ніколи в системі" :verified-by :read-whole}
  {:id :ldr-08 :at "72-73 :instance-step" :kind :code :rule "крок InstanceObjects може будувати runtime singleton-компоненти й об'єкти з конфігів; не може завантажувати ассети чи виконувати per-frame / ігрову логіку" :verified-by :read-whole}
  {:id :ldr-09 :at "74 :cancellation" :kind :code :rule "ThrowIfCancellationRequested після кожного await; ніколи тихий return" :same-as [:arch-102] :verified-by :read-whole}
  {:id :ldr-10 :at "75 :never" :kind :code :rule "ніколи …ConfigComponent, loader-нащадок на конфіг, звільнення конфіга" :same-as [:cfg-06 :cfg-08] :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/ADDRESSABLE_PATTERNS.md" :lines 145 :verified-by :read-whole
 :entries
 [{:id :adr-01 :at "16-19 Files" :kind :definition :rule "IAddressable — Assets/Modules/Addressable/Core/; реалізація Addressable (IObjectResolver.Instantiate) — Implementation/; Result, Box, Status — Assets/Scripts/Core/" :verified-by :read-whole}
  {:id :adr-02 :at "19 Files AddressableInstaller" :kind :di :rule "app-root інсталер реєструє IAddressable з Lifetime.Scoped" :verified-by :read-whole}
  {:id :adr-03 :at "22-48 API; 50" :kind :definition :rule "IAddressable: LoadAndInstanceAsync (GameObject), LoadAndInstanceAsync<T> де T : Component, LoadAsync<T> де T : class і не GameObject; скасований токен → OperationCanceledException після звільнення завантаженого; Status {Unknow, Failed, Success}; Result<T> {Box, Status}; Box<T> — readonly struct над класом BoxHandle<T>: Value кидає без Exist, Dispose ідемпотентний і викликає звільнення рівно раз, копії ділять дескриптор" :verified-by :read-whole}
  {:id :adr-04 :at "53 invariant 1" :kind :code :rule "звільнення вирішує Box, не Status: Exist означає, що всередині щось є, і тоді рівно один власник викликає Dispose(); інакше витік" :verified-by :read-whole}
  {:id :adr-05 :at "54 invariant 2" :kind :code :rule "Status != Success → Box порожній; Dispose порожнього чи default Box нічого не робить — безумовне звільнення у finally безпечне" :verified-by :read-whole}
  {:id :adr-06 :at "55 invariant 3" :kind :code :rule "звільнення LoadAndInstanceAsync знищує GameObject; ніколи Object.Destroy самому" :verified-by :read-whole}
  {:id :adr-07 :at "56 invariant 4" :kind :code :rule "LoadAsync<GameObject> захищено → Fail; prefab — лише LoadAndInstanceAsync" :verified-by :read-whole}
  {:id :adr-08 :at "57 invariant 5" :kind :code :rule "скасування — виняток, не Status: Box не доходить; після власних подальших await — token.ThrowIfCancellationRequested(); утримувані Box звільняти у try/finally" :verified-by :read-whole}
  {:id :adr-09 :at "58 invariant 6" :kind :code :rule "один власник на Box<T>; звільнений Box у полі замінюється на Box<T>.Empty()" :verified-by :read-whole}
  {:id :adr-10 :at "59 invariant 7" :kind :code :rule "IAddressable інжектується лише через конструктор; UnityEngine.AddressableAssets.Addressables поза реалізацією не чіпати" :verified-by :read-whole}
  {:id :adr-11 :at "60 invariant 8" :kind :code :rule "клас, що зберігає поле Box<T>, реалізує IDisposable (або інакше маршрутизує звільнення)" :verified-by :read-whole}
  {:id :adr-12 :at "64-72" :kind :code :rule "невдале завантаження не-GameObject — кинути InvalidOperationException з адресою; тримати Box, при знятті Dispose і Empty" :verified-by :read-whole}
  {:id :adr-13 :at "74-82" :kind :code :rule "prefab без потрібного компонента — звільнити Box і кинути; типізований компонент повертається як Box<T>.Wrap з каскадним звільненням" :verified-by :read-whole}
  {:id :adr-14 :at "84-106" :kind :code :rule "послідовні завантаження з відкатом: локальні Box.Empty, завантаження в try, фіксація в поля, скидання локальних, у finally — звільнення кожного локального" :verified-by :read-whole}
  {:id :adr-15 :at "108-120" :kind :code :rule "звільнення полів: сторож _isDisposed, helper DisposeBox(ref) звільняє і скидає в Empty" :verified-by :read-whole}
  {:id :adr-16 :at "122-127 Config handoff" :kind :code :rule "виняток до інваріанту 1: ConfigLoaderSystem<T> тримає лише Box.Value і ніколи не звільняє Box — конфіг живе всю сесію" :verified-by :read-whole}
  {:id :adr-17 :at "132-140 Anti-patterns" :kind :code :rule "ніколи LoadAsync<T>(nameof(T)) — лише явна константа адреси; ніколи Object.Destroy(result.Box.Value); ніколи зберегти Box.Value і кинути Box; ніколи один Box двом власникам; ніколи Value без перевірки Exist/Status; ніколи тихий return на IsCancellationRequested; ніколи звільнення в гілці перед throw — try/finally; ніколи поле Box<T> без IDisposable" :same-as [:adr-04 :adr-06 :adr-08 :adr-09 :adr-11] :verified-by :read-whole}
  {:id :adr-18 :at "143 Conventions" :kind :code :rule "ключ адреси — private const string у SCREAMING_SNAKE_CASE, значення — адреса addressable" :verified-by :read-whole}
  {:id :adr-19 :at "144 Conventions configs" :kind :code :rule "конфіг — без файлу loader і без компонента; кожна адреса конфіга — у ConfigAddresses" :same-as [:ldr-05 :ldr-10] :verified-by :read-whole}
  {:id :adr-20 :at "13; 145" :kind :non-code :rule "«читай це, не grep»; «ситуація без патерна — зупинись і спитай» — процес" :verified-by :read-whole}]}
```

## Patterns — системи

```clojure
{:carrier "Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md" :lines 79 :verified-by :read-whole
 :entries
 [{:id :orc-01 :at "13-15" :kind :code :rule "сімейство реалізацій за однією абстрактною базою, зібране DI в оркестратор, що впорядковує їх за Priority — для «одна база, одна реалізація на фічу» або стадії з незалежно впорядкованими частинами" :verified-by :read-whole}
  {:id :orc-02 :at "19-31 abstract base" :kind :code :rule "база підсистеми — простий об'єкт, не система: abstract class [Name]SubSystem : IDisposable; protected readonly EntityStore World з конструктора; IsEnabled { get; set; } = true; abstract int Priority; abstract Run([Args]) — одна операція сімейства; virtual Dispose" :verified-by :read-whole}
  {:id :orc-03 :at "35-50 concrete" :kind :code :rule "конкретна підсистема [UsedImplicitly] sealed; DI дає EntityStorages, ніколи голий EntityStore — base(storages.World); свої архетипи й індекси — у конструкторі; Priority через локальну const ExecutionPriority" :verified-by :read-whole}
  {:id :orc-04 :at "54-64 orchestrator" :kind :code :rule "оркестратор (pipeline stage або per-frame): [StateAllowed] readonly IReadOnlyList<[Name]SubSystem>, упорядкований у конструкторі OrderBy(Priority).ToArray(); у Update — по порядку, лише IsEnabled, Run" :verified-by :read-whole}
  {:id :orc-05 :at "70 :orchestrator" :kind :code :rule "оркестратор не містить доменної логіки — сортує, пропускає вимкнені, запускає; уся робота — у підсистемах" :verified-by :read-whole}
  {:id :orc-06 :at "71-72 :di-subsystem :di-orchestrator" :kind :di :rule "підсистема реєструється як база .As<[Feature]SubSystem, [Name]SubSystem>(); оркестратор — за контрактом хоста (pipeline або concrete + Boot.Construct)" :verified-by :read-whole}
  {:id :orc-07 :at "73 :StateAllowed" :kind :code :rule "[StateAllowed] на списку — лише коли оркестратор є системою; база підсистеми — простий IDisposable, не система: arch-check її не бачить, стан і кеші запитів там вільні" :verified-by :read-whole}
  {:id :orc-08 :at "74-75 :queries" :kind :code :rule "спільні запити — у базі разом із Try…-helper; власні — кожна підсистема розв'язує свої архетипи й індекси в конструкторі; вони належать сховищу, звільняти нічого" :verified-by :read-whole}
  {:id :orc-09 :at "76 :priority-scope" :kind :code :rule "Priority підсистем порівнюється лише всередині свого оркестратора; не пов'язаний з пріоритетами pipeline-стадій" :verified-by :read-whole}
  {:id :orc-10 :at "77 :routing-variant" :kind :code :rule "маршрутизація: bool TrySpawn(config), перший збіг виграє, жодного збігу — кинути" :verified-by :read-whole}
  {:id :orc-11 :at "78 :naming" :kind :code :rule "без доменного префікса в [Name] і [Feature]; [Domain]Installer префікс зберігає" :same-as [:arch-93] :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/PATTERN_PERFRAME_SYSTEM.md" :lines 53 :verified-by :read-whole
 :entries
 [{:id :pfr-01 :at "13-15" :kind :code :rule "per-frame — справді неперервна логіка (рух камери, проєкція щокадру, опитування вводу, стеження за виділенням); типово — reactive; per-frame лише коли можна сказати, чому імпульс не замінить тік; наслідує UpdatedSystem або LateUpdatedSystem" :same-as [:arch-09] :verified-by :read-whole}
  {:id :pfr-02 :at "19-39 skeleton" :kind :code :rule "[UsedImplicitly] sealed : UpdatedSystem; конструктор (EntityStorages) → base(storages.World, [Domain]Archetypes.[Table](storages.World)); Update(GameState, in Entity); без полів екземпляра між кадрами; Priority через локальну const ExecutionPriority" :verified-by :read-whole}
  {:id :pfr-03 :at "46 :driven-by" :kind :code :rule "веде або робоча таблиця — оголошений архетип, який система обробляє, або якір тіку — singleton-архетип, присутність якого вмикає тік; ArchetypeQuery — лише для справді міжархетипної множини" :verified-by :read-whole}
  {:id :pfr-04 :at "47 LateUpdatedSystem" :kind :code :rule "LateUpdatedSystem — коли треба бачити остаточний стан кадру (після камери й ігрових записів)" :verified-by :read-whole}
  {:id :pfr-05 :at "48 :state" :kind :code :rule "стану між кадрами нема: стійкий → компонент чи singleton-компонент; лише цей кадр → PreUpdate + FrameBox<T>; невідворотний → [StateAllowed(«причина»)]" :same-as [:arch-15] :verified-by :read-whole}
  {:id :pfr-06 :at "49 :shared-inputs" :kind :code :rule "спільні входи кадру — розв'язати в PreUpdate(GameState) з fail-loud і нести у FrameBox<T>; поле з [StateAllowed]; заборонено в UniTask-системах — await перетинає кадри, коробка застаріває" :verified-by :read-whole}
  {:id :pfr-07 :at "50 :scan-diff-each-tick" :kind :code :rule "сканувати й порівнювати щотіку — запах god-system: зробити reactive, подія там, де відбувається зміна" :same-as [:arch-01] :verified-by :read-whole}
  {:id :pfr-08 :at "51 :view-output" :kind :code :rule "вивід у view — штовхати по одному значенню; ніколи керований знімок у системі" :same-as [:ach-16] :verified-by :read-whole}
  {:id :pfr-09 :at "52 :wiring" :kind :di :rule "concrete в інсталері + вручну в Boot.Construct; система, не вписана в стан, не працює" :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/PATTERN_PIPELINE_STAGE.md" :lines 71 :verified-by :read-whole
 :entries
 [{:id :pst-01 :at "13-15" :kind :code :rule "pipeline stage — разове async-будування під час створення мапи (сутності й view, runtime singleton-компоненти, prefab); працює раз, упорядкована Priority; реалізує IPrioritizedUniTaskSystem<MapGenerationStep>" :verified-by :read-whole}
  {:id :pst-02 :at "19-52 skeleton" :kind :code :rule "[UsedImplicitly] internal sealed; readonly EntityStorages, EntityStore, Archetype таблиці; async UniTask Update(MapGenerationStep, token): перевірка передумови Singletons.Has → InvalidOperationException; ThrowIfCancellationRequested; рядки через _rows.CreateEntity(); Dispose звільняє власні дескриптори; Priority через локальну const ExecutionPriority (~100..900 з кроком ~100)" :verified-by :read-whole}
  {:id :pst-03 :at "59 :execution" :kind :code :rule "стадії послідовні, за зростанням Priority, з await; стадія може покладатися на все, що зробили стадії з меншим пріоритетом; відсутню передумову — кинути" :verified-by :read-whole}
  {:id :pst-04 :at "60 :re-entry" :kind :code :rule "лише коли регенерація може повторно увійти в стадію: сторож _isLoaded або знищити й створити заново" :verified-by :read-whole}
  {:id :pst-05 :at "61 :addressable-handle" :kind :code :rule "дескриптор addressable — утримувати і звільнити в Dispose" :verified-by :read-whole}
  {:id :pst-06 :at "62 :cancellation" :kind :code :rule "ThrowIfCancellationRequested після кожного await, окремим рядком, ніколи разом із перевіркою валідності, ніколи тихий return" :same-as [:arch-102] :verified-by :read-whole}
  {:id :pst-07 :at "63 :singleton-view" :kind :code :rule "singleton view публікується як …ViewComponent для споживачів" :verified-by :read-whole}
  {:id :pst-08 :at "64 :singleton-non-queried" :kind :code :rule "одиничні дані, які не запитують, — singleton-компонент" :verified-by :read-whole}
  {:id :pst-09 :at "65 :wiring" :kind :di :rule ".As<IPrioritizedUniTaskSystem<MapGenerationStep>>, конвеєр збирає сам" :verified-by :read-whole}
  {:id :pst-10 :at "66-67 :native-scratch" :kind :code :rule "native-чернетка: TempJob у межах 4 кадрів, інакше Persistent з Dispose одного власника; ніколи Allocator.Temp — стадія є async-системою" :same-as [:arch-21 :arch-22] :verified-by :read-whole}
  {:id :pst-11 :at "68-69 :thread-hops" :kind :code :rule "код у RunOnThreadPool — лише обчислення над простими даними, жодного виклику сховища (навіть читання) і жодного Allocator.Temp; доступ до сховища — лише після await UniTask.SwitchToMainThread()" :same-as [:arch-78 :arch-79 :arch-80] :verified-by :read-whole}
  {:id :pst-12 :at "70 :family-of-parts" :kind :code :rule "кілька незалежно впорядкованих частин чи одна база з багатьма реалізаціями — PATTERN_ORCHESTRATOR_SUBSYSTEM" :same-as [:pch-04] :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/PATTERN_REACTIVE_SYSTEM.md" :lines 65 :verified-by :read-whole
 :entries
 [{:id :rea-01 :at "13-15" :kind :code :rule "reactive — типовий вибір для runtime-логіки: реагує на однокадрову подію через її архетип, діє на значеннях або reconcile з поточним станом; наслідує UpdatedSystem" :same-as [:arch-03 :arch-06] :verified-by :read-whole}
  {:id :rea-02 :at "19-48 skeleton" :kind :code :rule "[UsedImplicitly] sealed : UpdatedSystem; readonly EntityStorages, ComponentIndex, Archetype, розв'язані в конструкторі; Priority => SystemPriorities.RuntimeTick.[Name]; base(storages.World, EventArchetypes.Of<[Name]Event>(storages.World)); Update(GameState, in Entity pulse): першим рядком IsRipe або return; далі сторожі передумов з fail-loud, значення pulse.GetComponent<[Name]Event>() або бажаний стан; дія лише на різниці" :verified-by :read-whole}
  {:id :rea-03 :at "55-56 :on-pulse" :kind :code :rule "на імпульс — діяти прямо на значеннях або reconcile (ідемпотентно)" :same-as [:arch-06] :verified-by :read-whole}
  {:id :rea-04 :at "57 :driven-by" :kind :code :rule "веде EventArchetypes.Of<TheEvent>" :same-as [:arch-05] :verified-by :read-whole}
  {:id :rea-05 :at "58 :ripe-gate" :kind :code :rule "IsRipe(pulse) або return — обов'язковий перший рядок; без нього система реагує і в кадрі народження, тобто двічі" :same-as [:arch-74] :verified-by :read-whole}
  {:id :rea-06 :at "59 :priority" :kind :code :rule "Priority — лише детермінований порядок; ніколи «вище чи нижче виробника»" :same-as [:arch-77] :verified-by :read-whole}
  {:id :rea-07 :at "60-61 :structural-in-update :structural-in-own-enumeration" :kind :code :rule "структурні зміни в Update безпечні (UpdatedSystem знімає id якірного архетипу і викликає Update поза своїм переліченням); у переліченні, яке система відкриває сама, — заборонено: ідіома NativeList<int> + TryGetEntityById" :same-as [:arch-85 :arch-87] :verified-by :read-whole}
  {:id :rea-08 :at "62-63 :smell" :kind :code :rule "створює і знищує той самий вміст → дві reactive-системи, по події кожна; per-frame лише щоб перевірити → це reactive, подія в джерелі зміни" :same-as [:arch-01] :verified-by :read-whole}
  {:id :rea-09 :at "64 :wiring" :kind :di :rule "concrete в інсталері + Boot.Construct" :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM.md" :lines 89 :verified-by :read-whole
 :entries
 [{:id :ror-01 :at "15-19" :kind :code :rule "reactive orchestrator — reactive-система, чия обробка завелика для одного файлу: на імпульс розсилає сімейству підсистем, зібраному DI і впорядкованому Priority; власної доменної логіки нема; для однієї події, що веде кілька незалежних механік" :verified-by :read-whole}
  {:id :ror-02 :at "22-25" :kind :code :rule "обробка вміщується в одне тіло Update → reactive-система; інакше (складно, кілька незалежних частин, сумнів) → reactive orchestrator" :verified-by :read-whole}
  {:id :ror-03 :at "29-60 skeleton" :kind :code :rule "sealed : UpdatedSystem; [StateAllowed] readonly IReadOnlyList<[Name]SubSystem>, упорядкований у конструкторі; Priority => SystemPriorities.RuntimeTick.[Name]; base(…, EventArchetypes.Of<[Name]Event>(…)); Update: IsRipe або return, потім цикл for по ввімкнених Run" :verified-by :read-whole}
  {:id :ror-04 :at "62-63" :kind :code :rule "база й конкретні підсистеми — як PATTERN_ORCHESTRATOR_SUBSYSTEM: простий IDisposable, не система; кожна володіє своїми кешами запитів і звільняє їх" :verified-by :read-whole}
  {:id :ror-05 :at "67-70; 86 :empty-collection" :kind :code :rule "VContainer кидає на порожню ін'єкцію IReadOnlyList<T>: поки підсистем нема — reactive-оболонка (якір на архетипі події, порожній Update, список не інжектується); список і цикл з'являються з першою підсистемою" :verified-by :read-whole}
  {:id :ror-06 :at "70-72" :kind :stale :rule "живий приклад оболонки BuildDistrictActionSystem, «no subsystems yet» — жива назва, не правило" :verified-by :read-whole}
  {:id :ror-07 :at "78 :driven-by" :kind :code :rule "веде EventArchetypes.Of<TheEvent>, нуль вартості без імпульсу" :same-as [:arch-05] :verified-by :read-whole}
  {:id :ror-08 :at "79-80 :on-pulse" :kind :code :rule "підсистеми діють на значеннях події або reconcile з ПОТОЧНИМ станом; reconcile-Run ідемпотентний" :same-as [:arch-06] :verified-by :read-whole}
  {:id :ror-09 :at "81 :ripe-gate" :kind :code :rule "IsRipe перевіряє оркестратор один раз перед розсиланням; підсистема не перевіряє повторно" :verified-by :read-whole}
  {:id :ror-10 :at "82-83 :orchestrator :subsystem" :kind :code :rule "оркестратор без доменної логіки; підсистема — простий IDisposable, не система; [StateAllowed] на списку оркестратора; кеші запитів — у кожній підсистемі" :same-as [:orc-05 :orc-07] :verified-by :read-whole}
  {:id :ror-11 :at "84-85 :di-subsystem :di-orchestrator" :kind :di :rule ".As<[Feature]SubSystem, [Name]SubSystem>(); concrete + Boot.Construct, групування в GameMode вручну" :verified-by :read-whole}
  {:id :ror-12 :at "87-88 :structural-in-update :structural-in-own-enumeration" :kind :code :rule "структурні зміни в Update безпечні; у переліченні, яке відкриває підсистема, — ідіома знімка id" :same-as [:rea-07] :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/PATTERN_TRANSACTION_ENTITY.md" :lines 104 :verified-by :read-whole
 :entries
 [{:id :trx-01 :at "24-31 When" :kind :code :rule "піддоменів ≥ 2 і поведінка багатокрокова → transaction entity; ≥ 2 піддомени, один імпульс → reactive-система; інакше — звичайні однодоменні патерни; багатокрокова = між відкривальним і фіксувальним імпульсом результат ще може змінитися, тобто є сесійний стан, яким хтось має володіти" :verified-by :read-whole}
  {:id :trx-02 :at "33-38 Degenerate case" :kind :code :rule "якщо між відкриттям і фіксацією нема чим володіти (не витрачено ресурс, нема прев'ю на мапі, вибір — чистий UI-стан) — фіксувати прямо на підтверджувальному імпульсі з UI-значеннями, без стадій чернетки" :verified-by :read-whole}
  {:id :trx-03 :at "16-19; 42-44" :kind :code :rule "багатокрокова поведінка між піддоменами має рівно один дім — transaction-сутність у verb-домені; verb-домен володіє сутністю і кожним записом у неї; одна архетипна форма з усіма колонками, включно з колонкою стадії, тож зміна стадії не переносить рядок" :verified-by :read-whole}
  {:id :trx-04 :at "48-76 skeleton" :kind :code :rule "відкриття — reactive-система verb-домену створює рядок архетипом і пише ідентифікацію, сесійний стан, стадію; зміна — командна подія зі значеннями, яку споживає мала reactive-система verb-домену; фіксація — запис колонки стадії; завершення — ФАКТ-сутність у таблиці substrate-домену і подія" :verified-by :read-whole}
  {:id :trx-05 :at "19 «an entity with a tag lifecycle»" :kind :stale :rule "«життєвий цикл тегами» — суперечить tag-law і власному :lifecycle (стадія — колонка)" :verified-by "прочитав рядок 19; GRAPH_STANDARD :transaction-entity-stale"}
  {:id :trx-06 :at "51, 72 привид; 53, 68" :kind :stale :rule "скелет: ключ через тип-привид (у коді лише HexIdFKComponent), MyStageComponent замість …StateComponent, оголошення архетипу без TransactionTag — застарілі" :verified-by "doc_lint — 3 привиди базово; GRAPH_STANDARD :transaction-entity-stale"}
  {:id :trx-07 :at "78-80" :kind :code :rule "UI — проєкція: її підсистеми читають transaction-сутність прямо (Presentation → Domains дозволено) і reconcile ідемпотентно; ввід гравця — командні імпульси, ніколи записи" :verified-by :read-whole}
  {:id :trx-08 :at "86 :home" :kind :code :rule "дім — verb-домен (Actions); ніколи substrate, ніколи Presentation" :verified-by :read-whole}
  {:id :trx-09 :at "87 :state" :kind :code :rule "увесь стан транзакції — на ОДНІЙ сутності; без копій у singleton-компоненті, без другого дому" :verified-by :read-whole}
  {:id :trx-10 :at "88 :reachability" :kind :code :rule "ніколи класти стан у substrate-домен, щоб його прочитав інший шар; чистий UI-стан вибору лишається в Presentation" :verified-by :read-whole}
  {:id :trx-11 :at "89 :ui" :kind :code :rule "UI-система читає сутність і піднімає командні імпульси через межу; view живить її локальною C#-подією і сам імпульс не піднімає; UI не тримає стану транзакції" :same-as [:evt-05] :verified-by :read-whole}
  {:id :trx-12 :at "90 :command" :kind :code :rule "команда — однокадрова подія зі значеннями команди; споживає лише reactive-система verb-домену, що пише сутність" :verified-by :read-whole}
  {:id :trx-13 :at "91 :lifecycle" :kind :code :rule "стадія — колонка …StateComponent; один архетип на всю сесію; головний тег не змінюється; архетип несе label TransactionTag поруч; стадія з іншим складом — видалити й створити в її архетипі з перенесенням PK/FK; стадія чернетки необов'язкова" :same-as [:arch-34 :arch-37 :arch-62] :verified-by :read-whole}
  {:id :trx-14 :at "92 :completion" :kind :code :rule "завершення — ФАКТ-сутність у substrate-домені + подія; споживачі читають таблицю фактів" :verified-by :read-whole}
  {:id :trx-15 :at "93 :views" :kind :code :rule "view показують факти або живу сутність, ніколи копію-знімок" :verified-by :read-whole}
  {:id :trx-16 :at "94 :flow-contract" :kind :non-code :rule "Flows/FLOW_<NAME>.md на кожен flow — процес і документ; прибрано рішенням :flow-contract-removed" :verified-by :read-whole}
  {:id :trx-17 :at "99-104 Anti-patterns" :kind :code :rule "ніколи стан транзакції як singleton-компонент у substrate; ніколи копія сесійного стану на подію через межу (подія несе значення команди); ніколи view показує verb-сутність як результат; ніколи один логічний стан, продубльований по піддоменах і синхронізований подіями" :same-as [:trx-09 :trx-10 :trx-14] :verified-by :read-whole}]}
```

```clojure
{:carrier "Patterns/PATTERN_VIEW_SYSTEM.md" :lines 90 :verified-by :read-whole
 :entries
 [{:id :vws-01 :at "15-20" :kind :code :rule "MonoBehaviour View — тупа оболонка, яку веде її System; спілкуються напряму: C#-подія всередину, push-to-view назовні, ніколи ECS; ECS-імпульс тут — хибний інструмент, бо це глобальний сигнал" :verified-by :read-whole}
  {:id :vws-02 :at "22-24" :kind :stale :rule "живий приклад DistrictBuildUIView / DistrictBuildUISystem — жива назва" :verified-by :read-whole}
  {:id :vws-03 :at "30 :in; 76" :kind :code :rule "всередину: view → локальна C#-подія (event Action<T>) → керівна система чи підсистема підписується прямо; view не знає сховища" :same-as [:arch-65] :verified-by :read-whole}
  {:id :vws-04 :at "31 :out; 77" :kind :code :rule "назовні: система → view по ОДНОМУ значенню (Set/Add/Clear); ніколи керований знімок у системі" :same-as [:ach-16] :verified-by :read-whole}
  {:id :vws-05 :at "32 :ecs; 80" :kind :code :rule "ECS-імпульс — лише коли сигнал перетинає межу кадру чи asmdef, яку C#-виклик не досягає; пара view/система в одному asmdef і кадрі — C#-подія; імпульс піднімає СИСТЕМА" :same-as [:evt-05] :verified-by :read-whole}
  {:id :vws-06 :at "33 :why; 81" :kind :code :rule "кожна підписка видна у підписника — grep C#-події дає всіх слухачів" :verified-by :read-whole}
  {:id :vws-07 :at "38-50 view skeleton; 75" :kind :code :rule "view — sealed : MonoBehaviour; public event Action<[Payload]>, піднята на взаємодії; методи SetValue прив'язують VisualElement; view ніколи не інжектує EntityStore, не створює сутностей, не піднімає ECS-імпульс для своєї системи" :same-as [:arch-64] :verified-by :read-whole}
  {:id :vws-08 :at "53-69 hook skeleton; 78" :kind :code :rule "система підписується на view ОДИН раз (view живе довше) зі сторожем bool і відписується в Dispose" :verified-by :read-whole}
  {:id :vws-09 :at "59-62; 79 :handler" :kind :code :rule "обробник працює синхронно в UI-колбеку на головному потоці: запис ECS через AddComponent(), потім push у view" :verified-by :read-whole}
  {:id :vws-10 :at "86-90 Anti-patterns" :kind :code :rule "ніколи view інжектує EntityStore і піднімає ECS-імпульс для своєї системи; ніколи система будує список чи масив для view; ніколи обробник відкладає ECS-запис на наступний тік прапорцем" :same-as [:vws-04 :vws-07] :verified-by :read-whole}
  {:id :vws-11 :at "увесь файл" :kind :definition :rule "рецепт не згадує [ViewSubscriber] — правило маркера живе лише в документації й коді інструментів" :verified-by "grep ViewSubscriber по Patterns/ — 0 (GRAPH_STANDARD :view-subscriber-unrecorded)"}]}
```

```clojure
{:carrier "Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md" :lines 276 :verified-by :read-whole
 :entries
 [{:id :cat-01 :at "33-36 use-when" :kind :code :rule "каталог — коли разом: кілька авторських видів різної форми зі спільним ключем; runtime запитує чи з'єднує записи за ключем; новий вид додається без правки оркестратора" :verified-by :read-whole}
  {:id :cat-02 :at "39-43 do not use" :kind :code :rule "один конфіг однієї форми → PATTERN_CONFIG (ніколи рядок для singleton-конфіга); однорідний список на одне читання → читати масив SO; поведінка на конфігу — ні: SO — чисті дані, Validate — єдиний метод, перемикання типів — у підсистемах" :verified-by :read-whole}
  {:id :cat-03 :at "51-76 part 1" :kind :code :rule "абстрактний базовий конфіг тримає лише спільний ключ; конкретні види додають параметри або нічого; контейнер SO тримає масив базових конфігів Items" :verified-by :read-whole}
  {:id :cat-04 :at "78-91 part 2" :kind :code :rule "контейнер вантажить один ConfigLoaderSystem на ConfigLoading; контейнер реалізує IValidatableConfig і перевіряє, що записи не null — порожній каталог зупиняє гру на старті" :verified-by :read-whole}
  {:id :cat-05 :at "93-150 part 3" :kind :code :rule "оркестратор спавну — pipeline stage на MapGenerationStep: обходить Items і віддає кожен запис першій підсистемі, що обробляє його конкретний тип; null-запис чи тип без підсистеми — кинути; підсистема — abstract bool TrySpawn(config), не свій тип → false; рядок народжується архетипом з усіма колонками виду: FK, колонка виду, параметри виду, початковий стан" :verified-by :read-whole}
  {:id :cat-06 :at "154-164 part 4" :kind :code :rule "рядок таблиці: ключ — …FKComponent простору суб'єкта; рівно один головний тег архетипу; label-тег сімейства [TagLabel], коли сімейство має кілька архетипів (не фільтр); компонент виду IIndexedComponent<Kind>, записаний раз при народженні; параметри виду в архетипі цього виду; необов'язковий компонент стану IIndexedComponent<State> з записом лише при зміні" :verified-by :read-whole}
  {:id :cat-07 :at "166-169" :kind :code :rule "фільтр — таблиця, не голий ключ; зріз виду чи стану — законний self-index ComponentIndex<KindComponent, Kind>()[kind]; запис перекладає рядок між зрізами" :same-as [:arch-26 :arch-39] :verified-by :read-whole}
  {:id :cat-08 :at "171-198 part 5" :kind :code :rule "необов'язкове сімейство evaluator — рядки перевіряються з часом: читає свій зріз виду з self-index, з'єднує цільову таблицю ComponentIndex за FK, reconcile колонки стану лише при зміні; порожній кошик — валідний стан; без стану — лише спавн" :verified-by :read-whole}
  {:id :cat-09 :at "207-211 routing" :kind :code :rule "маршрутизація (спавн): на кожен запис TrySpawn по списку, перший збіг; вид без підсистеми спавну — помилка авторингу, кинути" :same-as [:orc-10] :verified-by :read-whole}
  {:id :cat-10 :at "213-217 non-routing" :kind :code :rule "без маршрутизації (evaluator): запускати кожну ввімкнену підсистему безумовно, кожна сама запитує свої рядки; вид без evaluator — тихий no-op, свідомо" :verified-by :read-whole}
  {:id :cat-11 :at "225-231 per-kind-rule" :kind :code :rule "вид — один enum-компонент на сімейство, на кожному рядку, записаний раз; вибір — self-index виду, ніколи другий головний тег і ніколи вгадування за присутністю параметрів; параметри лише коли вид їх має; ключ — суб'єкт (1:N), не дискримінатор" :same-as [:arch-38] :verified-by :read-whole}
  {:id :cat-12 :at "235-240 dual-host" :kind :code :rule "evaluator для ходу 1 і кожного наступного — два тонкі хости над тим самим списком: bootstrap IPrioritizedUniTaskSystem<MapGenerationStep> одразу після спавну і TurnPhaseSubSystem у хвості конвеєра ходу" :verified-by :read-whole}
  {:id :cat-13 :at "244-252 DI wiring" :kind :di :rule "усе Singleton; підсистеми .As<AbstractBase>()" :verified-by :read-whole}
  {:id :cat-14 :at "254-255" :kind :code :rule "поле зібраного DI списку несе [StateAllowed] — свідомий виняток стану, не порушення zero-alloc" :same-as [:orc-07] :verified-by :read-whole}
  {:id :cat-15 :at "259-262" :kind :code :rule "новий вид — конкретний підклас конфіга, нове значення enum (+ компонент параметрів), підсистема спавну (+ evaluator); оркестратор не змінюється" :verified-by :read-whole}
  {:id :cat-16 :at "267-275 checklist" :kind :code :rule "контрольний перелік частин 1-5; :loader — «тримає Box, звільняє в dispose»" :verified-by :read-whole}
  {:id :cat-17 :at "26-27 Canonical implementation" :kind :stale :rule "живий приклад Economy.DistrictOpenCondition — жива назва" :verified-by :read-whole}]}
```

## fantasymayor-graph — документація

```clojure
{:carrier ".claude/skills/fantasymayor-graph/SKILL.md" :lines 125 :verified-by :read-whole
 :entries
 [{:id :gsk-01 :at "60-76 role-decision" :kind :code :rule "роль неабстрактного класу бере першу істинну гілку: Update-цикл і обхід подій → cleanup (лексично); якір на події в base(...) → reactive (база); Update-цикл і утримуваний архетип події → маркер [SystemRole], інакше undecided з попередженням; Update-цикл → per_frame (база); IPrioritizedUniTaskSystem з MapGenerationStep → pipeline_stage (база); TurnPhaseSubSystem → turn_phase (база); негенеричний IUniTaskSystem → startup_step (база); абстрактний предок, якого збирає конструктор, → sub_system (лексично); інакше ролі нема" :verified-by :read-whole}
  {:id :gsk-02 :at "80 markers SystemRoleAttribute" :kind :code :rule "[SystemRole(SystemRoleKind.PerFrame | Reactive)] — лише там, де база не вирішує" :verified-by :read-whole}
  {:id :gsk-03 :at "81 markers ViewSubscriberAttribute" :kind :code :rule "[ViewSubscriber(typeof(TheView))] — на класі, що додає обробники до C#-подій view" :verified-by :read-whole}
  {:id :gsk-04 :at "82 markers TagLabelAttribute" :kind :code :rule "[TagLabel] або [TagLabel(TagLabelRole.Transaction)] — на структурі тегу: мітка поруч із головним тегом, ніколи фільтр" :same-as [:arch-34] :verified-by :read-whole}
  {:id :gsk-05 :at "83 :guard" :kind :non-code :rule "сторож — MarkerShapeAnalyzer у компіляції Unity: маркер без заявленої форми — FM1001-FM1004" :verified-by :read-whole}
  {:id :gsk-06 :at "84 :tool" :kind :code :rule "попередження інструмента: зайвий маркер ролі, клас, якому маркер потрібен, маркер view без підписки і підписка на подію view без маркера" :verified-by :read-whole}
  {:id :gsk-07 :at "87-88" :kind :definition :rule "подія, на яку клас якориться в base(...), — reacts_to; архетип події, утримуваний поза base(...), — reacts_to лише під reactive-маркером, інакше polls" :verified-by :read-whole}
  {:id :gsk-08 :at "8-12, 14-58, 90-113, 115-125" :kind :non-code :rule "призначення інструмента, залежності, команди, схема graph.json, правила чесності (EXTRACTED / INFERRED / AMBIGUOUS, попередження, стале не відповідає, пише лише .fantasymayor-graph/) — поведінка інструмента" :verified-by :read-whole}
  {:id :gsk-09 :at "31-32" :kind :non-code :rule "запит і перебудова лише через fmgraph.py, JSON руками не читати й не правити — заборона процесу (hook graph-gate)" :verified-by :read-whole}]}
```

```clojure
{:carrier ".claude/skills/fantasymayor-graph/references/graph-facts.md" :lines 70 :verified-by :read-whole
 :entries
 [{:id :gfx-01 :at "4-5 Scan roots" :kind :definition :rule "корені сканування коду: Assets/Domains, Assets/Presentation, Assets/Modules, Assets/Scripts, Assets/Flows" :verified-by :read-whole}
  {:id :gfx-02 :at "9-20 Nodes" :kind :definition :rule "розпізнавання конструкцій: component — struct з IComponent або з іменем …Component, що не подія; tag — struct з ITag; event — struct з іменем …Event чи …EventComponent або тип, піднятий CreateEvent; data — будь-яка інша struct; config — неабстрактний клас, чия спадковість доходить до ScriptableObject чи SerializedScriptableObject; view — клас у папці Views/, чия спадковість доходить до MonoBehaviour; system — неабстрактний клас із роллю; archetype — член тримача store.GetArchetype(ComponentTypes.Get<…>(), Tags.Get<…>()), маніфест singleton або замикання генерик-тримача; installer — клас з іменем …Installer або на IInstaller / LifetimeScope" :verified-by :read-whole}
  {:id :gfx-03 :at "22-25" :kind :definition :rule "один вузол на оголошення (partial ділять його); неоголошений тип — вузол з declared false і видом за суфіксом; однойменні типи в різних просторах імен беруть ідентифікатор з простором; неоднозначне ім'я — попередження, не ребро" :verified-by :read-whole}
  {:id :gfx-04 :at "31 facets main_tag" :kind :definition :rule "головний і label теги архетипу — аргументи Tags.Get, розділені маркером [TagLabel] на кожній структурі" :same-as [:arch-32] :verified-by :read-whole}
  {:id :gfx-05 :at "32 facets base_anchor" :kind :definition :rule "якір — усередині base(...) класу, чия ПРЯМА база UpdatedSystem або LateUpdatedSystem; будь-де інде, включно з this(...), — утримання; усередині base(...) іншої бази — ні те, ні те (так само міряє MarkerShapeAnalyzer); архетип події = EventFrameComponent + EventTag називає свої події" :verified-by :read-whole}
  {:id :gfx-06 :at "34 facets priority" :kind :definition :rule "Priority — вираз, розв'язаний проти кожної const int за шляхом вкладеності" :same-as [:pch-05] :verified-by :read-whole}
  {:id :gfx-07 :at "42-60 Edges" :kind :definition :rule "читання коду: writes — AddComponent(new T{…}) / AddComponent<T>() / AddComponent(local) / Singletons.Set; reads — GetComponent<T>, HasComponent<T>, Singletons.Get/Has і прив'язка класу до архетипу таблиці; removes — RemoveComponent<T>, RemoveTag<T>; emits — CreateEvent(new TEvent{…}); reacts_to / polls — за роллю; disposes — DeleteEntity через єдину прив'язку власника; fk_of — закон суфікса (…FKComponent → …Component) і реальний пошук ComponentIndex<FK,TValue>; inherits, hosts, registers, exposes, injects, runs_in; subscribes — клас з [ViewSubscriber(typeof(V))] → V, підтверджений власним += на подію, оголошену V або базою V" :verified-by :read-whole}
  {:id :gfx-08 :at "64-65 tables" :kind :definition :rule "таблиці — кожне поле ComponentIndex<TComponent,TValue> з роллю за суфіксом ключа (pk, fk, data) і єдиним архетипом, що несе ключ" :same-as [:arch-40 :arch-41 :arch-42] :verified-by :read-whole}
  {:id :gfx-09 :at "66-68 tag_audit" :kind :code :rule "аудит тегів: архетип без головного тегу; кілька головних; головний тег, крім EventTag, спільний для двох архетипів; EventTag поза архетипом події; label-тег у міжархетипному фільтрі; фільтр не-події з кількістю тегів, відмінною від 1; тег, доданий за життя сутності; теги, складені через Tags.Add" :verified-by :read-whole}
  {:id :gfx-10 :at "69-70" :kind :di :rule "форми реєстрації, яких читач не веде (RegisterFactory, RegisterComponentInHierarchy, RegisterEntryPoint, AsImplementedInterfaces, RegisterInstance не-ідентифікатора), — попередження" :verified-by :read-whole}]}
```

```clojure
{:carrier ".claude/skills/fantasymayor-graph/references/recipe-signatures.md" :lines 22 :verified-by :read-whole
 :note "рід (а) — ознака вже є законом; (б) — правило написання коду, законом не записане; (в) — лише евристика впізнавання (GRAPH_STANDARD :signatures-three-kinds); за рішенням :recipe-signatures у специфікацію йдуть роди а + б"
 :entries
 [{:id :sig-01 :at "8 PATTERN_COMPONENT" :kind :code :rule "рід а: оголошена struct виду component (IComponent або …Component, не подія)" :same-as [:cmp-01 :cmp-10] :verified-by :read-whole}
  {:id :sig-02 :at "9 PATTERN_TAG" :kind :code :rule "рід а: оголошена struct : ITag, поділена на головні й label; відхилення — аудит тегів" :same-as [:tag-01 :gfx-09] :verified-by :read-whole}
  {:id :sig-03 :at "10 PATTERN_EVENT" :kind :code :rule "рід а: оголошена struct з іменем …Event або …EventComponent" :same-as [:evt-07] :verified-by :read-whole}
  {:id :sig-04 :at "20 PATTERN_TRANSACTION_ENTITY" :kind :code :rule "рід а: архетип несе label-тег ролі transaction" :same-as [:trx-13] :verified-by :read-whole}
  {:id :sig-05 :at "15 PATTERN_REACTIVE_SYSTEM" :kind :code :rule "рід б: клас із роллю reactive — якір на архетипі події в base(...) або reactive-маркер" :same-as [:gsk-01 :rea-02] :verified-by :read-whole}
  {:id :sig-06 :at "17 PATTERN_PERFRAME_SYSTEM" :kind :code :rule "рід б: клас із роллю per_frame; відхилення — класи з роллю undecided" :same-as [:gsk-01 :pfr-02] :verified-by :read-whole}
  {:id :sig-07 :at "19 PATTERN_VIEW_SYSTEM" :kind :code :rule "рід б: ребра subscribes від класу з [ViewSubscriber]; відхилення — підписки без маркера і порушення межі view" :same-as [:gsk-03 :arch-64] :verified-by :read-whole}
  {:id :sig-08 :at "13 PATTERN_PIPELINE_STAGE" :kind :code :rule "рід б: роль pipeline_stage — клас реалізує IPrioritizedUniTaskSystem<MapGenerationStep>" :same-as [:pst-01] :verified-by :read-whole}
  {:id :sig-09 :at "12 PATTERN_CONFIG_LOADER" :kind :di :rule "рід б, але ознака — форма реєстрації: реєстрація з AppState.ConfigLoading, чия реалізація в тому ж виразі виставлена як IUniTaskSystem; група instance_objects — реєстрації з AppState.InstanceObjects" :verified-by :read-whole}
  {:id :sig-10 :at "21 PATTERN_POLYMORPHIC_CATALOGUE" :kind :code :rule "рід б: абстрактна SO-база з іменем …Config + неабстрактний SO-контейнер з масивом [SerializeField] цієї бази + …KindComponent, названий за базою і несений архетипом" :same-as [:cat-03 :cat-11] :verified-by :read-whole}
  {:id :sig-11 :at "18 PATTERN_CLEANUP_SYSTEM" :kind :code :rule "рід б: роль cleanup — Update-клас обходить EventTag і видаляє" :same-as [:cln-01] :verified-by :read-whole}
  {:id :sig-12 :at "SKILL.md 71-75 turn_phase, startup_step" :kind :code :rule "рід б: фаза ходу — нащадок TurnPhaseSubSystem; крок старту — негенеричний IUniTaskSystem" :same-as [:pch-08 :cat-12] :verified-by :read-whole}
  {:id :sig-13 :at "11 PATTERN_CONFIG" :kind :non-code :rule "рід в: неабстрактний клас, чия спадковість доходить до ScriptableObject — евристика впізнавання" :verified-by :read-whole}
  {:id :sig-14 :at "14, 16 PATTERN_ORCHESTRATOR_SUBSYSTEM, PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM" :kind :non-code :rule "рід в: нащадки контракту, який збирає конструктор (hosts); reactive-клас із ребром hosts — евристика впізнавання" :verified-by :read-whole}
  {:id :sig-15 :at "22 ADDRESSABLE_PATTERNS" :kind :non-code :rule "рід в: клас, що інжектує IAddressable через конструктор — евристика впізнавання" :verified-by :read-whole}]}
```

## fantasymayor-graph — скрипти

```clojure
{:carrier ".claude/skills/fantasymayor-graph/scripts/roles.py tag_law.py view_pairs.py recipes.py ecs_facts.py" :lines 752 :verified-by :read-whole
 :entries
 [{:id :rol-01 :at "roles.py:35, 10" :kind :definition :rule "роль дістають лише оголошені неабстрактні класи видів other, installer, config, view" :verified-by :read-whole}
  {:id :rol-02 :at "roles.py:71-89" :kind :definition :rule "порядок гілок ролі — слово в слово як у SKILL.md role-decision" :same-as [:gsk-01] :verified-by :read-whole}
  {:id :rol-03 :at "roles.py:58-68" :kind :definition :rule "докази: Update-цикл — IUpdatedSystem чи ILateUpdatedSystem у спадковості; обхід подій — EventTag у фільтрі того самого класу І місце DeleteEntity; pipeline — IPrioritizedUniTaskSystem з аргументом рівно MapGenerationStep; фаза ходу — TurnPhaseSubSystem у спадковості; крок старту — IUniTaskSystem без аргументів; член сімейства — абстрактний предок, якого хтось збирає" :verified-by :read-whole}
  {:id :rol-04 :at "roles.py:43-48" :kind :code :rule "попередження: «marker needed» — Update-клас тримає архетип події поза base(...), база не вирішує; «redundant marker» — клас заявляє SystemRole, а роль вирішена базою чи лексично" :same-as [:gsk-06] :verified-by :read-whole}
  {:id :rol-05 :at "roles.py:92-102" :kind :definition :rule "клас без ролі все одно polls архетипи подій, які тримає" :verified-by :read-whole}
  {:id :tgl-01 :at "tag_law.py:25-26, 38" :kind :definition :rule "головний чи label вирішує лише маркер [TagLabel] на структурі тегу" :verified-by :read-whole}
  {:id :tgl-02 :at "tag_law.py:39-55" :kind :code :rule "перевіряються: архетип без головного тегу; кілька головних; головний тег (крім EventTag), спільний для кількох архетипів; EventTag без EventFrameComponent" :same-as [:gfx-09] :verified-by :read-whole}
  {:id :tgl-03 :at "tag_law.py:57-65" :kind :code :rule "фільтр не-події: label-тег у фільтрі — відхилення; кількість тегів у фільтрі, відмінна від 1, — відхилення (обхід подій за EventTag виключено)" :same-as [:arch-26 :arch-35] :verified-by :read-whole}
  {:id :tgl-04 :at "tag_law.py:66-71" :kind :code :rule "тег, доданий за життя сутності, і склад тегів через Tags.Add — відхилення" :verified-by :read-whole}
  {:id :tgl-05 :at "tag_law.py увесь файл" :kind :definition :rule "порядок тегів у Tags.Get (головний першим) інструмент не перевіряє" :verified-by :read-whole}
  {:id :vpr-01 :at "view_pairs.py:20-30" :kind :code :rule "клас із [ViewSubscriber(typeof(V))] має мати власний += на подію, оголошену V або базою V; маркер без підписки — попередження" :same-as [:gsk-03 :gsk-06] :verified-by :read-whole}
  {:id :vpr-02 :at "view_pairs.py:32-42" :kind :code :rule "підписка на подію рівно одного view без маркера — попередження; якщо подію оголошують кілька view — AMBIGUOUS" :verified-by :read-whole}
  {:id :rcp-01 :at "recipes.py:31-62" :kind :definition :rule "RECIPE_SIGNATURES — джерело правди «рецепт → ознака»; recipe-signatures.md його читання" :same-as [:sig-01] :verified-by :read-whole}
  {:id :rcp-02 :at "recipes.py:12, 151-167" :kind :code :rule "порушення межі view: клас виду view піднімає подію (CreateEvent), створює сутність (CreateEntity / CreateEntities) або отримує EntityStorages чи EntityStore полем, параметром конструктора, [Inject]- чи Construct-методу" :same-as [:arch-64] :verified-by :read-whole}
  {:id :rcp-03 :at "recipes.py:178-196" :kind :definition :rule "поліморфний каталог знаходиться трійкою: абстрактна SO-база з іменем …Config, неабстрактний SO-контейнер із масивом [SerializeField] цієї бази, компонент виду, названий базою без суфікса Config, який несе архетип" :same-as [:cat-03 :sig-10] :verified-by :read-whole}
  {:id :ecs-01 :at "ecs_facts.py:77-86" :kind :definition :rule "архетип: label-теги — ті, що мають маркер; головний тег записується у вузол лише коли він рівно один" :verified-by :read-whole}
  {:id :ecs-02 :at "ecs_facts.py:65-74, 128-155" :kind :definition :rule "AnyComponents(ComponentTypes.Get<…>()), де всі типи — події, якорить чи утримує так само, як EventArchetypes.Of; якір — лише в base(...) класу з прямою базою UpdatedSystem/LateUpdatedSystem" :same-as [:gfx-05] :verified-by :read-whole}
  {:id :ecs-03 :at "ecs_facts.py:173-174, 179-183" :kind :definition :rule "AddComponent події одразу після CreateEvent не рахується записом; «тег доданий за життя» = AddComponent типу з іменем …Tag" :verified-by :read-whole}
  {:id :ecs-04 :at "ecs_facts.py:193-213" :kind :non-code :rule "DeleteEntity приписується єдиному архетипу з прив'язок власника; кілька кандидатів — попередження «attribute by hand»; обхідники подій пропускаються — межа висновку інструмента" :verified-by :read-whole}
  {:id :ecs-05 :at "ecs_facts.py:244-268" :kind :code :rule "роль ключа за суфіксом: …FKComponent — fk, …IdComponent — pk, інакше data; PK, який несуть кілька архетипів, — порушення (чужі носії мусять перейти на …FKComponent); FK без ключа власника в графі — попередження" :same-as [:arch-40 :arch-41] :verified-by :read-whole}
  {:id :ecs-06 :at "ecs_facts.py:270-288" :kind :code :rule "FK, який шукають через ComponentIndex<FK,TValue>, мусить мати TValue, що збігається зі значенням ключа власника (IIndexedComponent<…> або єдине поле); розбіжність — порушення" :same-as [:cmp-06] :verified-by :read-whole}
  {:id :ecs-07 :at "ecs_facts.py:299-315" :kind :code :rule "Priority мусить розв'язуватися в const int (літерал, крапкове чи просте ім'я константи); нерозв'язаний вираз — попередження" :same-as [:pch-05] :verified-by :read-whole}
  {:id :ecs-08 :at "ecs_facts.py:318-329" :kind :code :rule "маніфест singleton: рівно один архетип Singleton, коли singleton-компоненти взагалі вживаються; використані, але не оголошені — порушення; оголошені, але не використані — порушення" :verified-by :read-whole}
  {:id :ecs-09 :at "ecs_facts.py:331-336" :kind :code :rule "запис компонента, якого не називає жоден оголошений архетип, — порушення (осиротіла колонка)" :same-as [:arch-60 :arch-63] :verified-by :read-whole}]}
```

## MarkerShapeAnalyzer і маркери

```clojure
{:carrier "Tools/MarkerShapeAnalyzer/*.cs" :lines 316 :verified-by :read-whole
 :entries
 [{:id :msa-01 :at "MarkerShapeAnalyzer.cs:16-20 FM1001" :kind :code :rule "FM1001 — помилка: клас заявляє SystemRole(PerFrame), але per-frame система — неабстрактний IUpdatedSystem чи ILateUpdatedSystem, чий base(...) не якориться на архетипі події" :verified-by :read-whole}
  {:id :msa-02 :at "MarkerShapeAnalyzer.cs:22-26 FM1002" :kind :code :rule "FM1002 — помилка: клас заявляє SystemRole(Reactive), але reactive-система — неабстрактний IUpdatedSystem чи ILateUpdatedSystem, не якорений на таблиці в base(...), що якориться на архетипі події або тримає його" :verified-by :read-whole}
  {:id :msa-03 :at "MarkerShapeAnalyzer.cs:28-32 FM1003" :kind :code :rule "FM1003 — помилка: клас заявляє ViewSubscriber(V), але V мусить походити від MonoBehaviour, а клас — додавати обробник через += до події, оголошеної V або базою V" :verified-by :read-whole}
  {:id :msa-04 :at "MarkerShapeAnalyzer.cs:34-38 FM1004" :kind :code :rule "FM1004 — помилка: тип заявляє TagLabel, але міткою може бути лише struct, що реалізує ITag" :verified-by :read-whole}
  {:id :msa-05 :at "MarkerShapeAnalyzer.cs:14, 20-38" :kind :definition :rule "категорія діагностик FantasyMayor.Markers, severity Error, увімкнені за замовчуванням" :verified-by :read-whole}
  {:id :msa-06 :at "MarkerShapeAnalyzer.cs:9-10, 61-63" :kind :definition :rule "тип без маркера не реєструє перевірок; зайвий маркер аналізатор не перевіряє" :verified-by :read-whole}
  {:id :msa-07 :at "MarkedTypeCheck.cs:30-56" :kind :definition :rule "якір міряється лише в base(...) типу, чия ПРЯМА база UpdatedSystem або LateUpdatedSystem; аргумент EventArchetypes.Of або AnyComponents(ComponentTypes.Get<…>), де кожен аргумент — struct з іменем …Event чи …EventComponent, робить якір подією, інакше якір — таблиця" :same-as [:gfx-05] :verified-by :read-whole}
  {:id :msa-08 :at "MarkedTypeCheck.cs:62-74" :kind :definition :rule "утримуваний архетип події — виклик EventArchetypes.Of поза base(...); виклик у this(...) рахується утриманням" :verified-by :read-whole}
  {:id :msa-09 :at "MarkedTypeCheck.cs:76-85" :kind :definition :rule "підписка — += , ліворуч якого символ події; += над числом чи рядком підпискою не є" :verified-by :read-whole}
  {:id :msa-10 :at "MarkedTypeCheck.cs:33, 68, 79" :kind :definition :rule "вузли вкладеного типу належать вкладеному типу, не зовнішньому" :verified-by :read-whole}
  {:id :msa-11 :at "MarkedTypeCheck.cs:118-125" :kind :definition :rule "форма per-frame = контракт циклу і не подієвий якір; форма reactive = контракт циклу, не табличний якір, і подієвий якір або утримуваний архетип події" :verified-by :read-whole}
  {:id :msa-12 :at "MarkerVocabulary.cs:24-35" :kind :definition :rule "словник за метаданими: EcsExtensions.SystemRoleAttribute, ViewSubscriberAttribute, TagLabelAttribute, IUpdatedSystem, ILateUpdatedSystem, UpdatedSystem, LateUpdatedSystem, EventArchetypes; Friflo.Engine.ECS.ComponentTypes і ITag; UnityEngine.MonoBehaviour" :verified-by :read-whole}
  {:id :msa-13 :at "MarkerVocabulary.cs:42-46" :kind :code :rule "копія enum SystemRoleKind в аналізаторі мусить збігатися з EcsExtensions.SystemRoleKind — аргумент атрибута приходить як базовий int" :verified-by :read-whole}]}
```

```clojure
{:carrier "Assets/Scripts/EcsExtensions — атрибути маркерів" :lines 70 :verified-by :read-whole
 :entries
 [{:id :atr-01 :at "SystemRoleAttribute.cs:5-8" :kind :code :rule "[SystemRole] — лише на класі, не множинний, не успадковується; коментар типу: роль, якої не вирішує база, — Update-клас, що тримає архетип події поза base(...)" :verified-by :read-whole}
  {:id :atr-02 :at "SystemRoleKind.cs:3-7" :kind :definition :rule "SystemRoleKind — PerFrame, Reactive" :verified-by :read-whole}
  {:id :atr-03 :at "ViewSubscriberAttribute.cs:5-7" :kind :code :rule "[ViewSubscriber(typeof(V))] — на класі, множинний, не успадковується; view не знає свого підписника" :verified-by :read-whole}
  {:id :atr-04 :at "TagLabelAttribute.cs:5-19" :kind :code :rule "[TagLabel] — лише на структурі, не множинний, не успадковується; без аргументу роль None" :verified-by :read-whole}
  {:id :atr-05 :at "TagLabelRole.cs:3-7" :kind :definition :rule "TagLabelRole — None, Transaction" :verified-by :read-whole}
  {:id :atr-06 :at "усі три атрибути, Inherited = false" :kind :code :rule "маркери не успадковуються — кожен конкретний клас чи структура несе свій" :verified-by :read-whole}]}
```

## CLAUDE.md

```clojure
{:carrier "CLAUDE.md" :verified-by :read-partial
 :entries
 [{:id :ntn-01 :at "93-95 notation-ecs-ext" :kind :non-code :rule "проєктне читання нотації: форма опису сутності, читання #{} за полем, #{} під :tag показує порушення закону тегів — правило нотації, не коду; закон, на який воно спирається, уже є в tag-law" :same-as [:arch-32] :verified-by :read-partial}
  {:id :ntn-02 :at "71 bans" :kind :code :rule "тихий пропуск відсутньої передумови заборонений — кидати; поза :sources, але дубль fail-loud" :same-as [:arch-99 :arch-100] :verified-by :read-partial}]}
```

# Graph standard findings

Знахідки з провенансом уже записані у Flows/GRAPH_STANDARD/FLOW.md # Findings; тут — лише те, що несе на специфікацію, з посиланням на id знахідки.

```clojure
[{:finding :tag-law-mostly-recorded :bears "закон тегів записаний у tag-law і PATTERN_TAG, але чотири правила, які перевіряє інструмент, у законі не записані: фільтр не-події називає рівно один тег; тег не додається за життя сутності; EventTag лише в архетипі події; теги складаються лише через Tags.Get, не Tags.Add" :for-s1 "ці чотири — окремі правила специфікації (:tgl-02 :tgl-03 :tgl-04)"}
 {:finding :role-markers-unrecorded :bears "рішення ролі (8 гілок) і правило [SystemRole] живуть лише в SKILL.md, roles.py і текстах FM1001/FM1002; ARCHITECTURE.md і рецепти маркера не згадують" :for-s1 "головна прогалина: роль системи і маркер — правила специфікації (:gsk-01 :gsk-02 :msa-01 :msa-02)"}
 {:finding :view-subscriber-unrecorded :bears "правило [ViewSubscriber] записане лише в документації інструмента; PATTERN_VIEW_SYSTEM його не згадує" :for-s1 "правило специфікації (:gsk-03 :vpr-01 :msa-03 :vws-11)"}
 {:finding :view-definition-diverges :bears "інструмент: клас у Views/, що доходить до MonoBehaviour; FM1003: будь-який MonoBehaviour; ARCHITECTURE: MonoBehaviour view" :for-s1 "рішення :view-definition закриває розбіжність: view — усе в папці Views/ (:x-view-definition)"}
 {:finding :view-boundary-recorded :bears "view-boundary записаний в ARCHITECTURE; живих відхилень 12; помилки компіляції не буде без окремого слова власника" :for-s1 "правило лишається, примус — процесом"}
 {:finding :signatures-three-kinds :bears "ознаки 15 рецептів трьох родів; у специфікацію йдуть роди а + б" :for-s1 "розділ ознак = записи :sig-01 … :sig-12; :sig-13 … :sig-15 виключаються з причиною"}
 {:finding :other-tool-rules :bears "інструмент попереджає ще про: відомі форми реєстрації DI, приписування DeleteEntity, маніфест синглтонів, вираз Priority, запис компонента поза оголошеним архетипом" :for-s1 "перші дві — виключено (:ad-di-boundary, :ad-delete-entity); решта — правила (:ecs-07 :ecs-08 :ecs-09)"}
 {:finding :enforcement-now :bears "компіляція перевіряє лише форму наявного маркера; базова лінія — 8 попереджень, 0 невизначених ролей, 0 підписок без маркера, 0 відхилень тегів, 12 відхилень межі view" :for-s1 "числа базової лінії для :numbers"}
 {:finding :compile-check-feasible :bears "правило ролі реалізоване двічі — roles.py і аналізатор — і розбіжність між ними нічим не ловиться" :for-s1 "специфікація мусить формулювати правило ролі один раз, так, щоб обидві реалізації були його читанням (:x-role-marker)"}
 {:finding :transaction-entity-stale :bears "PATTERN_TRANSACTION_ENTITY застарів: тегове життя, привид ключа, MyStageComponent, скелет без мітки транзакції, прибрана вимога flow-contract" :for-s1 "брати правило, не застарілий текст (:trx-05 :trx-06 :trx-16)"}
 {:finding :rule-ids-link-carriers :bears "стабільний ID правила — спосіб зв'язати запис специфікації з помилкою FM і попередженням графа (RSPEC, HelpLinkUri, RS2000)" :for-s1 "ID дає s2; s1 лише не змішує два правила в одне"}
 {:finding :policy-as-code-single-artifact :bears "урок OPA: один канонічний артефакт, який точки примусу читають напряму" :for-s1 "правило формулюється як дані, придатні для проєкції, а не як проза"}
 {:finding :silent-rule-risk :bears "правило, що ніколи не спрацьовує, не відрізнити від мертвого" :for-s1 "кожне правило описує спостережувану ситуацію коду, а не намір"}
 {:finding :agent-spec-layering :bears "агентні шари без механічної звірки гниють; генерація похідного LLM-ом недетермінована" :for-s1 "специфікація — джерело, з якого похідне ВИВОДЯТЬ звіркою; текст правила не дублюється на носіях"}]
```

# Overlaps and contradictions

Те саме правило, сказане двома носіями по-різному. Кожен запис — факт із провенансом; розв'язує s1.

```clojure
[{:id :x-view-definition
  :carriers [:arch-64 :arch-16 :plc-38 :gfx-02 :msa-03 :vws-07]
  :says "ARCHITECTURE і рецепт кажуть «MonoBehaviour view»; граф — «клас у Views/, що доходить до MonoBehaviour»; FM1003 — «будь-який тип, що походить від MonoBehaviour»; рішення власника — «усе, що лежить у папці Views/»"
  :live "20 .cs у папках Views/, 19 вузлів view у графі; різниця — статичний клас DistrictBuildLabels"
  :for-s1 "одне визначення view для межі view, винятку System.Collections.Generic і правила [ViewSubscriber]"
  :verified-by :fmgraph}
 {:id :x-role-marker
  :carriers [:gsk-01 :rol-02 :rol-03 :msa-11 :msa-02]
  :says "roles.py: Update-клас, що тримає архетип події, бере роль з маркера — навіть коли його base(...) якорився на таблиці; FM1002: заявка Reactive на класі з табличним якорем — помилка; roles.py має гілку cleanup перед reactive, аналізатор про cleanup не знає; зайвий маркер: у графа попередження, в аналізатора мовчання, за рішенням власника — помилка компіляції"
  :live "3 маркери [SystemRole] у коді, 0 невизначених ролей"
  :for-s1 "одне правило ролі й маркера, з якого обидві реалізації виводяться без розбіжності"
  :verified-by :read-whole}
 {:id :x-priority
  :carriers [:pch-05 :orc-03 :pfr-02 :pst-02 :cat-05 :ecs-07]
  :says "pattern-choice: Priority — константа з SystemPriorities, ніколи локальне число; скелети PERFRAME, PIPELINE_STAGE, ORCHESTRATOR_SUBSYSTEM тримають локальну const ExecutionPriority, POLYMORPHIC_CATALOGUE — літерал 920"
  :live "0 входжень локальної const ExecutionPriority, 69 читань SystemPriorities"
  :for-s1 "одне правило джерела Priority; окремо — простір порядку підсистем (:orc-09) проти «кожна система має окреме значення»"
  :verified-by :grep}
 {:id :x-stateless-vs-lifetime-flags
  :carriers [:arch-11 :arch-13 :ach-07 :vws-08 :pst-04 :pst-05 :adr-15]
  :says "закон: система не тримає змінного стану, переприсвоюване поле — стан; рецепти вимагають у системі полів _hooked (підписка раз), _isLoaded (повторний вхід), Box<T> дескриптора і _isDisposed"
  :live "ShowHexesUISystem: _uiBox, _isDisposed, _isLoaded без [StateAllowed]; DistrictBuildUISystem: _view, _chromeHooked; підсистеми (не системи) вільні за :orc-07"
  :for-s1 "або ці поля — випадок [StateAllowed] з причиною, або дім стану інший; правило мусить сказати, який саме"
  :verified-by :grep}
 {:id :x-generic-in-repeated-orchestrator
  :carriers [:ach-05 :arch-19 :orc-04 :ror-03 :cat-14]
  :says "arch-check Rule 1 забороняє IReadOnlyList<> та інші типи System.Collections.Generic у повторних системах без винятку для [StateAllowed]; ARCHITECTURE дозволяє колекцію керованих елементів, виділену раз і утримувану; reactive-оркестратор — повторна система з [StateAllowed] IReadOnlyList і OrderBy().ToArray()"
  :for-s1 "межа винятку: чи виняток «елементи керованого типу» покриває зібраний DI список у повторній системі"
  :verified-by :read-whole}
 {:id :x-config-box-lifetime
  :carriers [:cat-16 :cfg-08 :ldr-02 :ldr-10 :adr-16]
  :says "каталог у чеклісті: loader тримає Box і звільняє його в dispose; PATTERN_CONFIG, PATTERN_CONFIG_LOADER і ADDRESSABLE_PATTERNS: конфіг живе всю сесію, ConfigLoaderSystem тримає лише Box.Value і ніколи не звільняє"
  :for-s1 "одне правило часу життя конфіга"
  :verified-by :read-whole}
 {:id :x-subsystem-query-caches
  :carriers [:orc-08 :ror-04]
  :says "ORCHESTRATOR_SUBSYSTEM: кеші запитів належать сховищу, звільняти нічого; REACTIVE_ORCHESTRATOR: кожна підсистема володіє своїми кешами і звільняє їх"
  :for-s1 "одне правило володіння кешами запитів"
  :verified-by :read-whole}
 {:id :x-validatable-config
  :carriers [:cfg-03 :pch-15 :cat-02 :cat-04]
  :says "PATTERN_CONFIG: IValidatableConfig — лише коли авторські дані можуть бути хибними; pattern-choice: тип конфіга реалізує IValidatableConfig і кидає на кожне порушення; каталог: Validate — єдиний метод SO і контейнер мусить його мати"
  :for-s1 "коли валідація обов'язкова"
  :verified-by :read-whole}
 {:id :x-naming-prefix-exception
  :carriers [:arch-93 :tag-11 :cmp-12]
  :says "ARCHITECTURE: виняток — FK/PK-компонент ідентичності або дискримінатор таблиці, на які посилаються З ІНШИХ ДОМЕНІВ; PATTERN_TAG: виняток — дискримінатори Table Rule (без умови міждоменності); PATTERN_COMPONENT: виняток — компоненти ідентичності FK/PK"
  :for-s1 "одна межа винятку для префікса"
  :verified-by :read-whole}
 {:id :x-event-suffix
  :carriers [:arch-91 :evt-07 :gfx-02 :msa-07]
  :says "закон іменування знає лише суфікс …Event; граф і аналізатор приймають також …EventComponent"
  :live "одна жива структура з легасі-суфіксом"
  :for-s1 "або легасі-виняток записаний, або правило одне, а інструменти читають ширше"
  :verified-by :grep}
 {:id :x-catalogue-key-naming
  :carriers [:arch-41 :arch-43 :cat-01 :cat-05 :cat-06]
  :says "key-role-law: сторона посилання простору-перелічення — …FKComponent; каталог у таблиці частини 4 каже те саме, але в тексті й скелеті ключем названо FooKeyComponent і живий DistrictTypeComponent"
  :live "DistrictTypeComponent — IIndexedComponent<DistrictType>, суфікса FK не має"
  :for-s1 "правило імені ключа для простору-перелічення проти живого коду"
  :verified-by :grep}
 {:id :x-transaction-tag-lifecycle
  :carriers [:trx-05 :trx-13 :arch-33 :arch-37]
  :says "вступ рецепта: «сутність із життєвим циклом тегів»; його ж :lifecycle і tag-law: головний тег не змінюється, стадія — колонка"
  :for-s1 "формулювання без тегового життєвого циклу"
  :verified-by :read-whole}
 {:id :x-live-tag-write
  :carriers [:arch-63 :arch-82 :tag-04 :tgl-04 :ecs-03]
  :says "birth-completeness перелічує лише видалення компонента, видалення тегу й пізній AddComponent; додавання тегу живій сутності прямо не заборонене, хоча структурною зміною названо і його; інструмент бачить лише AddComponent типу з іменем …Tag"
  :for-s1 "правило про додавання тегу живій сутності записати явно"
  :verified-by :read-partial}
 {:id :x-system-definition
  :carriers [:ach-06 :rol-01 :orc-07 :ror-10]
  :says "arch-check: система — клас на восьми базових типах (підсистеми сімейств виключені); граф: система — клас із роллю, і роль sub_system теж робить його системою; рецепти: база підсистеми — простий IDisposable, не система, стан там вільний"
  :live "26 класів з роллю sub_system"
  :for-s1 "одне визначення системи для заборони стану, заборони колекцій і zero-allocation"
  :verified-by :fmgraph}
 {:id :x-cadence-turn-phase
  :carriers [:ach-04 :arch-18 :rol-03]
  :says "arch-check: one-shot — IPrioritizedUniTaskSystem<T> pipeline stage, repeated — база turn phase; TurnPhaseSubSystem реалізує IPrioritizedUniTaskSystem<TurnPhaseStep>, тож підпадає під обидва описи; граф вважає pipeline лише MapGenerationStep"
  :for-s1 "каденція за стадією T, а не за інтерфейсом"
  :verified-by :grep}
 {:id :x-stateallowed-reason
  :carriers [:arch-15 :pch-18 :orc-04 :ror-03 :cat-14]
  :says "закон і pattern-choice: [StateAllowed] завжди з причиною; скелети трьох рецептів ставлять голий [StateAllowed]; тип має конструктор з необов'язковою причиною"
  :for-s1 "чи причина обов'язкова"
  :verified-by :grep}
 {:id :x-framebox-field
  :carriers [:arch-15 :pfr-06 :ach-07 :pch-19]
  :says "закон дає FrameBox як окремий вихід перед [StateAllowed]; PERFRAME каже, що поле FrameBox несе [StateAllowed]; arch-check не має FrameBox у переліку непозначуваних, а FrameBox<T> — змінна struct"
  :for-s1 "чи поле FrameBox потребує [StateAllowed]"
  :verified-by :grep}
 {:id :x-fail-loud-vs-silent-no-op
  :carriers [:arch-99 :arch-100 :cat-10]
  :says "fail-loud: крок, що не може зробити роботу, кидає; каталог: вид без evaluator — свідомо тихий no-op (асиметрія названа в самому рецепті)"
  :for-s1 "межа fail-loud: коли відсутність обробника — помилка авторингу, а коли очікуваний стан"
  :verified-by :read-whole}
 {:id :x-static-field
  :carriers [:arch-12 :ach-08]
  :says "ARCHITECTURE не називає станом лише const і static readonly; arch-check не позначає будь-яке static-поле"
  :for-s1 "чи змінне static-поле в системі — стан"
  :verified-by :read-whole}
 {:id :x-view-boundary-receives
  :carriers [:arch-64 :vws-07 :rcp-02]
  :says "ARCHITECTURE: view не отримує EntityStorages чи EntityStore; рецепт каже лише про EntityStore і «не інжектує»; інструмент перевіряє поля, конструктори, [Inject]- і Construct-методи для обох типів"
  :for-s1 "одне формулювання, що саме заборонено отримувати і як"
  :verified-by :read-whole}]
```

# Not code rules

```clojure
{:criteria [{:kind :di :reason "форма реєстрації DI під рефакторингом — рішення :no-di-registration; межа — :ad-di-boundary" :entries [:plc-05 :plc-19 :plc-27 :plc-40 :pch-09 :pch-13 :ldr-01 :ldr-06 :adr-02 :orc-06 :pfr-09 :pst-09 :rea-09 :ror-11 :cat-13 :gfx-10 :sig-09]}
            {:kind :non-code :reason "процес агента, правило документа, нотація або поведінка інструмента — поза межами :bounds" :entries [:plc-00 :plc-14 :pch-00 :pch-20 :pch-21 :ach-18 :arch-106 :arch-107 :cmp-15 :tag-12 :evt-12 :adr-20 :gsk-05 :gsk-08 :gsk-09 :ecs-04 :sig-13 :sig-14 :sig-15 :trx-16 :ntn-01]}
            {:kind :asset :reason "розміщення ассетів, не коду — :ad-assets" :entries [:plc-20 :plc-21 :plc-39]}
            {:kind :stale :reason "жива назва, історія або застарілий текст — доказ, не правило" :entries [:evt-13 :ach-19 :ror-06 :vws-02 :cat-17 :trx-05 :trx-06]}]
 :note "записи :kind :definition — не правила поведінки, а визначення конструкцій, про які правила говорять; s1 бере їх у :data, а не в перелік правил"}
```

# Auto-decided

```clojure
[{:id :ad-di-boundary
  :question "що саме означає «реєстрація DI не записується»"
  :options [{:option "виключено все, чий предмет — форма виклику реєстрації VContainer, Lifetime, ціль As, WithParameter, клас і розміщення інсталера, порядок InstallModules, ручне вписування систем у game state; включено правила про типи, константи й конструктори, які реєстрація споживає (ConfigAddresses, IValidatableConfig, вибір базового типу, форма конструктора, порожня колекція)" :confidence 65}
            {:option "виключено все, що згадує інсталер, Boot чи VContainer, включно з константами адрес і ін'єкцією через конструктор" :confidence 35}]
  :chosen "перший"
  :reason "власник виключив блок, що рефакториться — форми реєстрації; константа адреси й контракт конструктора переживуть рефакторинг DI"}
 {:id :ad-config-loader-signature
  :question "чи входить ознака PATTERN_CONFIG_LOADER (реєстрація ConfigLoading + As IUniTaskSystem в одному виразі) у роди а + б"
  :options [{:option "не входить — це форма реєстрації; у специфікації лишаються правила loader-а, що формою реєстрації не є" :confidence 70}
            {:option "входить як ознака рецепта, попри :no-di-registration" :confidence 30}]
  :chosen "перший"
  :reason "два рішення власника перетинаються; вужче (DI не записуємо) сформульоване пізніше й категоричніше"}
 {:id :ad-delete-entity
  :question "чи «DeleteEntity приписується одному архетипу» — правило коду"
  :options [{:option "ні — межа висновку інструмента (7 живих попереджень нічого в коді не порушують)" :confidence 70}
            {:option "так — правило «клас, що видаляє рядки, прив'язаний до однієї таблиці»" :confidence 30}]
  :chosen "перший"
  :reason "жоден носій не формулює цього як вимоги до коду; це спосіб приписати ребро"}
 {:id :ad-singleton-manifest
  :question "чи входить маніфест синглтонів (рівно один архетип Singleton; використані = оголошені)"
  :options [{:option "входить як правило коду" :confidence 60}
            {:option "не входить — сховище чекає рефакторингу, як і блок DI" :confidence 40}]
  :chosen "перший"
  :reason "власник виключив лише реєстрацію DI; маніфест — чинний закон ECS, який інструмент перевіряє сьогодні"}
 {:id :ad-assets
  :question "чи входять правила розміщення ассетів (Assets/Addressables/Configs/, Prefabs/, Textures/)"
  :options [{:option "не входять — це не код" :confidence 60}
            {:option "входять як правила дерева репозиторію" :confidence 40}]
  :chosen "перший"
  :reason "межа :bounds — правила, яких має дотримуватись КОД; ассети авторить власник у Unity"}
 {:id :ad-selection-rules
  :question "чи входять правила вибору форми (дерево рецептів, коли ділити систему, коли per-frame замість reactive)"
  :options [{:option "входять — вони вирішують, якої форми набуде код" :confidence 65}
            {:option "не входять — це процедура агента при постановці" :confidence 35}]
  :chosen "перший"
  :reason "ARCHITECTURE вже містить такі правила (decomposition, reactive :per-frame :only-when); процедурою є лише порядок кроків скіла"}
 {:id :ad-skeletons
  :question "що робити зі скелетами C# у рецептах"
  :options [{:option "у специфікацію йде правило, яке скелет кодує (база, перший рядок IsRipe, розв'язання в конструкторі, папка й простір імен); сам скелет лишається в Patterns/" :confidence 75}
            {:option "скелети переносяться у специфікацію" :confidence 25}]
  :chosen "перший"
  :reason "рішення :architecture-after і GRAPH_STANDARD :standard-home: носій показує форму в коді й посилається на закон, тексту правила не повторює"}
 {:id :ad-definitions
  :question "чи входять визначення конструкцій, за якими інструменти впізнають вид (component, tag, event, view, system, archetype)"
  :options [{:option "входять як :data s1 — правила говорять про них; евристики впізнавання роду в лишаються в коді інструмента" :confidence 70}
            {:option "не входять — це справа інструмента" :confidence 30}]
  :chosen "перший"
  :reason "без визначення «що таке система» заборона стану не має адресата (:x-system-definition)"}]
```

# Open questions

```clojure
[{:id :oq-stateless-vs-lifetime-flags
  :ask "закон «система не тримає змінного стану» проти живих полів _hooked / _isLoaded / Box<T> у п'яти живих системах і в трьох рецептах: правило лишається як є (а поля стають випадками [StateAllowed] з причиною) чи закон дістає виняток для підписки й дескриптора ассета?"
  :why-not-auto "зачіпає живий код і те, що власник вважає станом; s1 обере варіант з оцінкою, власник бачить його в підсумку"}
 {:id :oq-singleton-manifest
  :ask "маніфест синглтонів записується у специфікацію (обрано авто) — чи він теж під рефакторингом, як блок DI?"
  :why-not-auto "власник назвав під рефакторингом лише реєстрацію DI; сховище синглтонів згадувалось як таке, що чекає змін"}
 {:id :oq-view-in-views-folder
  :ask "view — усе в папці Views/: чи поширюються межа view і виняток System.Collections.Generic на статичний клас-довідник у тій самій папці?"
  :why-not-auto "буквальне читання рішення власника дає наслідок, якого власник міг не мати на увазі"}]
```

# Occasions

```clojure
[{:exit "носія з :sources нема — стадія кидає, а не пропускає" :occasion "не настав: усі носії на місці (# Facts, перший факт)"}
 {:exit "правило носія не можна виразити як EDN-дані" :occasion "не настав: кожен запис інвентаря вже стоїть рядком у Clojure-блоці"}]
```

# Verification

```clojure
{:meters [{:meter "python3 Tools/doc_lint.py --quiet" :target "0 синтаксичних помилок Clojure; привидів не більше 3"}
          {:meter "python3 Tools/gen_index.py" :target "LINT clean"}
          {:meter "інвентар носіїв" :target "кожен запис цього інвентаря — у специфікації з ID або в переліку виключених з причиною"}
          {:meter "ID правил" :target "кожне правило має ID; дублів 0 (s2)"}
          {:meter "суперечності" :target "кожен запис # Overlaps and contradictions розв'язаний у s1"}
          {:meter "converge" :target "contradicts 0, unrequested 0"}]
 :owner-check "власник бачить підсумок фінального артефакту і рішення, прийняті в авто-режимі"}
```

# Complete

```clojure
{:names-every-file-the-next-stage-may-read true
 :states-out-of-scope true
 :ends-with-verification true
 :links-to-follow-on-own-initiative 0}
```
