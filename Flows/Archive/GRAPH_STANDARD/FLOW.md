---
category: A
read: archive
status: closed-by-owner
tags:
  - architecture
  - ecs
  - tools
  - enforcement
related:
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[ECS_GRAPH_PATTERN_INSTANCES](../ECS_GRAPH_PATTERN_INSTANCES/FLOW.md)"
---

# Request

```clojure
{:source :user
 :received-at "2026-09-15"
 :raw-request ["Давай закриємо цей flow бо він вже настільки застарів що неможливо зрозуміти що треба робити. З того що я бачу PATTERN_TRANSACTION_ENTITY - треба оновити до сучасного стандарту та підходу."
               "PATTERN_TRANSACTION_ENTITY - спирається на політику тегів яка була змінена відповідно до задачі ECS_GRAPH_PATTERN_INSTANCES\nВзагалі я бачу що ми змінили тулу, змінили політику роботи над тегами, створення entity та систем і походу нікуди це не записали і зараз в нас є тула яка має спиратися на новий архітектурний стандарт але ми проїбали цей стандарт записати і змусити його дотримуватися."
               "1 - бери все\n2 - ARCHITECTURE.md\n3 - процессом, але валідація при компіляції теж потрібна бо це маркер\n4 - закрито за моєю вимогою"
               "go"
               "1 - а\n2 - все що є в папці view\n3 - теж помилка\n4 - запиши всі форми реєстрації\n5 - заміняй\n6 - поясни детально\n7 - поясни детально"
               "1 - RULES_SPECIFICATION.md\n2 - Кожне правило, якого має дотримуватись код. А щодо DOC_STANDARD - думаю можна видаляти. Я не готовий підтримувати купу документів. Замість цього я буду покладатися на код, скіли та інструменти пошуку\n3 - Clojure\n4 - перше\n5 - перше\n6 - я думаю це буде окремий flow через cascade для створення цієї супер специфікації\n7 - шукай, відправ окремих sonnet агентів на пошук по всіх перелічених темах по одному на кожну\n8 - цей блок буде рефакторитися тому я хз що тут записати бо будь-що записане згниє"
               "1 - так\n2 - цей flow повинен бути переписаним під те щоб бути продовженим після виконання каскаду\n3 - CODE_STORY_RULES_PROPOSAL видаляй, це те що вже стало каскадом.\n4 - DOC_STANDARD можна замінити окремим скілом який буде перевіряти чи все ок з написаним документом. Я взагалі не планую більше створювати якісь додаткові документи, хіба що щось додам чи зміню в патернах. Решта має лягти на скіли та тули."]}
```

# Confirmed contract

```clojure
[{:task :record-graph-standard
  :goal "кожне правило, яке читає fantasymayor-graph, записане в ARCHITECTURE.md"
  :path :direct
  :where #{ARCHITECTURE.md "Patterns/ — рецепти, яких торкаються правила" ".claude/skills/fantasymayor-graph/ — лише документація"}
  :decided #{"обсяг — усе: закон тегів, маркери ролей, view-boundary, ознаки 15 рецептів"
             "дім правила — ARCHITECTURE.md; скелет рецепта показує маркер у коді й посилається на закон, текст правила не повторює"
             "документація інструмента посилається на ARCHITECTURE.md, правил не тримає"
             "PATTERN_TRANSACTION_ENTITY.md — у складі"}
  :read #{ARCHITECTURE.md DOC_STANDARD.md "fantasymayor-graph SKILL.md + references/" "Flows/Archive/ECS_GRAPH_PATTERN_INSTANCES — рішення"}
  :skills {fantasymayor-pattern-choice "не запускається — код гри не змінюється" fantasymayor-placement "не застосовний"}
  :tools #{fantasymayor-graph doc_lint roslyn}
  :accept [{:meter "python3 Tools/doc_lint.py --quiet" :target "0 синтаксичних помилок; привиди не зросли"}
           {:meter "python3 Tools/gen_index.py" :target "LINT clean"}
           {:meter "інвентар правил" :target "кожне правило має один дім в ARCHITECTURE.md"}]
  :result "стандарт записаний"}

 {:task :enforce-graph-standard
  :listen :record-graph-standard
  :goal "процес і компіляція змушують дотримуватись стандарту"
  :path :direct                                  ;; 60 — переоцінка після інвентаря; :cascade 40
  :where #{CLAUDE.md ".claude/skills/fantasymayor-pattern-choice/SKILL.md" Tools/MarkerShapeAnalyzer
           "імпортована DLL аналізатора в Assets — лише байти, .meta не чіпати"}
  :off-limits #{"решта Assets/" "код інструмента графа (scripts/)"}
  :decided #{"процес: скіл вибору патерна називає маркер при постановці; перевірка коду в CLAUDE.md ловить відхилення стандарту"
             "компіляція: MarkerShapeAnalyzer дає помилку, коли маркер обов'язковий, а його нема"}
  :skills {fantasymayor-pattern-choice "не застосовний — аналізатор не код гри" fantasymayor-placement "не застосовний — Tools/ поза шарами"}
  :tools #{fantasymayor-graph roslyn}
  :accept [{:meter "dotnet build Tools/MarkerShapeAnalyzer" :target "clean"}
           {:meter "fmgraph.py check" :target "0 невизначених ролей"}
           {:meter "Unity-перевірка власника" :target "проєкт компілюється; клас без обов'язкового маркера дає помилку FM"}]
  :result "порушення стандарту не проходить ні перевірку коду, ні компіляцію"}]
```

# Plan

```clojure
(-> (:pass-1 "інвентар: кожне правило, яке читає код fantasymayor-graph і MarkerShapeAnalyzer → де воно записане зараз; скільки класів без обов'язкового маркера")
    (:findings-gate "знахідки й питання → власник")
    (:pass-2 ?)
    (:plan ?)
    (:implementation-go ?))
```

# Findings

```clojure
[{:finding :tag-law-mostly-recorded
  :at "2026-09-15"
  :fact "Закон тегів, який перевіряє tag_law.py, записаний в ARCHITECTURE.md tag-law і PATTERN_TAG: один головний тег, унікальний (крім EventTag), мітки [TagLabel] 0-4, ніколи не фільтр, TagLabelRole.Transaction. Не записані, але перевіряються: фільтр не-подій називає рівно один тег; тег не додається за життя сутності (AddTag — birth-completeness перелічує лише RemoveTag); EventTag лише в архетипі події; теги складаються тільки через Tags.Get, не Tags.Add. Живих відхилень 0"
  :verified-by "прочитав tag_law.py і ARCHITECTURE.md цілком; fmgraph.py tags — 0 deviations"
  :consequence "у tag-law дописати чотири правила"}

 {:finding :role-markers-unrecorded
  :at "2026-09-15"
  :fact "Рішення ролі системи (8 гілок: cleanup, reactive за base(...), маркер, per_frame, pipeline_stage, turn_phase, startup_step, sub_system) і правило [SystemRole] — лише там, де база не вирішує (Update-клас тримає архетип події поза base(...)); маркер там, де база вирішує, — «redundant»; тримане без Reactive-маркера — polls. Усе це живе лише в fantasymayor-graph/SKILL.md, roles.py і текстах FM1001/FM1002. ARCHITECTURE.md reactive каже лише «consumer anchors on the event's own archetype»; PATTERN_REACTIVE_SYSTEM, PERFRAME_SYSTEM, REACTIVE_ORCHESTRATOR_SYSTEM, CLEANUP_SYSTEM — 0 згадок маркера. У коді: 2 PerFrame, 1 Reactive, 0 невизначених"
  :verified-by "прочитав roles.py, SKILL.md, MarkedTypeCheck.cs; grep SystemRole|marker по ARCHITECTURE.md і Patterns/; fmgraph.py systems --role undecided — 0; grep атрибутів по Assets"
  :consequence "головна прогалина стандарту — роль системи і маркер"}

 {:finding :view-subscriber-unrecorded
  :at "2026-09-15"
  :fact "Правило [ViewSubscriber(typeof(V))] — клас, що підписується += на C#-подію view, несе маркер; маркер без підписки — попередження; FM1003 — форма — записане лише в документації інструмента. PATTERN_VIEW_SYSTEM — 0 згадок. У коді: 4 маркери, 5 підписок, 0 підписок без маркера"
  :verified-by "прочитав view_pairs.py, recipes.py, graph-facts.md; grep ViewSubscriber по Patterns/ і ARCHITECTURE.md; fmgraph.py pattern PATTERN_VIEW_SYSTEM"
  :consequence "дописати в ARCHITECTURE.md поруч з view-boundary"}

 {:finding :view-definition-diverges
  :at "2026-09-15"
  :fact "Інструмент вважає view клас у папці Views/, що доходить до MonoBehaviour (graph-facts.md; скіл розміщення: Views/ — MonoBehaviour view layer). FM1003 вважає view будь-який MonoBehaviour. ARCHITECTURE.md view-boundary каже лише «a MonoBehaviour view»"
  :verified-by "graph-facts.md # Nodes; MarkedTypeCheck.cs DerivesFrom(view, MonoBehaviour); fantasymayor-placement SKILL.md:108"
  :consequence "перевірка «підписка без маркера» при компіляції за визначенням аналізатора і за визначенням інструмента дасть різні множини — визначення view треба зафіксувати законом"}

 {:finding :view-boundary-recorded
  :at "2026-09-15"
  :fact "view-boundary записаний в ARCHITECTURE.md; живих відхилень 12 у 4 view (ContextTabsView, HexInfoPanelView, HexesUI, TurnPanelView). Архівне рішення :view-boundary: помилка компіляції — ні, лише на окреме слово власника"
  :verified-by "fmgraph.py pattern PATTERN_VIEW_SYSTEM; Flows/Archive/ECS_GRAPH_PATTERN_INSTANCES/FLOW.md # Decisions"
  :consequence "не маркер — лишається процесом"}

 {:finding :signatures-three-kinds
  :at "2026-09-15"
  :fact "Ознаки 15 рецептів (recipes.py RECIPE_SIGNATURES) трьох родів. (а) Уже закон в ARCHITECTURE.md: COMPONENT, EVENT (суфікси naming), TAG, TRANSACTION_ENTITY (мітка Transaction). (б) Правило написання коду, законом не записане: REACTIVE / PERFRAME (якір у base(...) + маркер), VIEW_SYSTEM (маркер), PIPELINE_STAGE (IPrioritizedUniTaskSystem<MapGenerationStep>), turn phase (TurnPhaseSubSystem), startup step (негенеричний IUniTaskSystem), CONFIG_LOADER (реєстрація з AppState.ConfigLoading і As<IUniTaskSystem> в одному виразі), POLYMORPHIC_CATALOGUE (…KindComponent = назва базового конфіга без Config), CLEANUP (sweep EventTag + DeleteEntity). (в) Лише евристика впізнавання: CONFIG, ORCHESTRATOR_SUBSYSTEM (hosts), REACTIVE_ORCHESTRATOR, ADDRESSABLE. Архівне рішення :signature-home: «зв'язок рецепт → ознака живе в коді інструмента»"
  :verified-by "прочитав recipes.py і roles.py; порівняв з ARCHITECTURE.md; скелети рецептів звірені лише для REACTIVE / PERFRAME / REACTIVE_ORCHESTRATOR (base(...) у коді є, закону нема)"
  :consequence "обсяг «ознаки 15 рецептів» у ARCHITECTURE.md суперечить архівному :signature-home — потрібен перегляд рішення"}

 {:finding :other-tool-rules
  :at "2026-09-15"
  :fact "Інші правила, які інструмент читає і за порушення дає попередження: лише відомі форми реєстрації DI (RegisterFactory, RegisterComponentInHierarchy, RegisterEntryPoint, AsImplementedInterfaces, RegisterInstance не-ідентифікатора — попередження); DeleteEntity приписується одному архетипу (7 живих попереджень «attribute by hand»); маніфест синглтонів — рівно один архетип Singleton, використані = оголошені; Priority — вираз з const int; запис компонента поза оголошеним архетипом (є в birth-completeness). В ARCHITECTURE.md нема: форм реєстрації, приписування DeleteEntity, маніфесту синглтонів, Priority"
  :verified-by "grep warn( по di_facts.py, ecs_facts.py; fmgraph.py check — 8 попереджень"
  :consequence "рішення власника: чи це стандарт, чи обмеження інструмента; маніфест синглтонів зачіпає сховище, яке чекає рефакторингу"}

 {:finding :enforcement-now
  :at "2026-09-15"
  :fact "Компіляція: MarkerShapeAnalyzer реєструє перевірку лише для типів з маркером («A type without a marker registers nothing»); відсутній маркер і зайвий маркер не перевіряються. Процес: CLAUDE.md code-verification крок 3 — лише fmgraph.py tags; project.md метр fmgraph.py check «no new warnings» ловить маркер-потрібен, підписку без маркера і закон тегів (усе це попередження), але відносно базової лінії; відхилення view-boundary — не попередження, їх не ловить жоден крок. Скіл вибору патерна: дерево рецептів маркерів не називає. Базова лінія: 8 попереджень (7 DeleteEntity, 1 HexIdFKComponent без власника), 0 невизначених ролей, 0 підписок без маркера, 0 відхилень тегів, 12 view-boundary"
  :verified-by "прочитав MarkerShapeAnalyzer.cs, CLAUDE.md § 2, project.md # Meters, скіл pattern-choice; fmgraph.py check / stats / tags / pattern"
  :consequence "примус зараз — лише форма наявного маркера"}

 {:finding :compile-check-feasible
  :at "2026-09-15"
  :fact "Аналізатор уже вимірює все, з чого випливає «маркер обов'язковий»: контракт Update-циклу, якір у base(...), тримані архетипи подій, підписки +=. Розширення — реєструвати перевірку і для типів без маркера. За інструментом живий код дає 0 невизначених і 0 підписок без маркера — нова помилка компіляції нічого не зламає, якщо визначення view збігається (:view-definition-diverges). Правило ролі реалізоване двічі — roles.py і аналізатор; розбіжність між ними нічим не ловиться"
  :verified-by "прочитав MarkedTypeCheck.cs і roles.py; fmgraph.py; сам аналізатор на незмаркованих типах не запускав"
  :consequence "ARCHITECTURE.md стає єдиною специфікацією для обох реалізацій"}

 {:finding :transaction-entity-stale
  :at "2026-09-15"
  :fact "PATTERN_TRANSACTION_ENTITY.md: «an entity with a tag lifecycle» (19) проти tag-law — головний тег не змінюється, стадія — колонка; мітка TransactionTag лише в ;; коментарі (91), скелет не показує оголошення архетипу з Tags.Get<головний, TransactionTag> — єдину ознаку, за якою інструмент його знаходить; MyStageComponent (53, 68) проти «…StateComponent» (91); привиди HexIdComponent (51, 72) — реальний ключ HexIdFKComponent; :flow-contract Flows/FLOW_<NAME>.md — стара форма; живі назви й історія прецеденту (Actions, BuildDistrictActionSystem, DistrictBuildConfirmedEvent); «consumers reconcile from the fact table» (75) — reconcile тепер один із двох варіантів"
  :verified-by "прочитав PATTERN_TRANSACTION_ENTITY.md цілком; fmgraph.py check (HexIdFKComponent); ARCHITECTURE.md tag-law, reactive"
  :consequence "рецепт — у складі правок"}

 {:finding :prior-art-reused
  :at "2026-09-15"
  :fact "Архівна задача ECS_GRAPH уже шукала prior art: jMolecules дає роль за базою без анотації, ArchUnit поєднує маркер із правилом, що перевіряє форму; маркери читає tree-sitter"
  :verified-by "Flows/Archive/ECS_GRAPH_PATTERN_INSTANCES/FLOW.md # Progress :prior-art-guess-outcome; у цьому проході зовнішнього пошуку не було"
  :consequence "питання «одна специфікація — дві перевірки» поза проєктом не шукали; окремий пошук — лише за словом власника"}

 ;; звірка ланцюжка проти RULES_SPECIFICATION.md — скіл fantasymayor-rules-conformance, 4 паралельні проходи 2026-09-16
 {:finding :architecture-is-a-second-home
  :at "2026-09-16"
  :fact "ARCHITECTURE.md: 123 твердження — 108 :second-home, 8 :diverges, 1 :extra, 6 :aligned, 0 :stale. 109 із 244 правил (45%) мають там повний другий дім, 21 блок переказує :says іншими словами разом із числами закону (100 на ключ, 5 аргументів типу, 0-4 мітки, int.MaxValue) — це прямо ламає правило «число має рівно один дім». Документ жодного разу не називає RULES_SPECIFICATION.md: перша стрілка конвеєра відсутня. Розбіжності, що міняють поведінку: :state/is-state втратив settable авто-властивість (тобто { get; set; } читається як не-стан), :alloc/managed-element-exception втратив вимогу позначення (List<GameObject> у полі системи без StateAllowed), :table/cross-archetype втратив виняток обходу подій (EventCleanupSystem читається як порушення). :extra — «DI installer — <Domain>Installer», яке специфікація свідомо винесла в межі набору"
  :verified-by "прохід скіла fantasymayor-rules-conformance над ARCHITECTURE.md проти всіх 244 правил, агент Opus 2026-09-16; звіт scratchpad/conformance-architecture.md"
  :consequence "після чистки жоден блок не виживає цілком — лишається 40-60 рядків: словник «ID правила → живий тип», «де саме», «чому», пульт примусу, deviations"}

 {:finding :recipes-drift
  :at "2026-09-16"
  :fact "15 рецептів Patterns/, 12 знахідок: 5 :stale, 3 :diverges, 4 :missing. PATTERN_TRANSACTION_ENTITY — 5: привид HexIdComponent (51, 72; той самий у PATTERN_COMPONENT.md:42), «an entity with a tag lifecycle» (19) проти власного рядка 91 і правил :tag/state-column + :transaction/one-archetype, скелет називає стадію MyStageComponent замість суфікса стану (у коді 0 хітів на StageComponent), :flow-contract на форму документа, якої нема. PATTERN_EVENT.md:58 цитує мертвий FLOW_DISTRICT_BUILD.md. PATTERN_POLYMORPHIC_CATALOGUE.md:269 каже, що loader звільняє дескриптор — проти :config/lifetime. Маркери відсутні в скелетах REACTIVE_SYSTEM, PERFRAME_SYSTEM, REACTIVE_ORCHESTRATOR_SYSTEM ([SystemRole]) і VIEW_SYSTEM ([ViewSubscriber]), хоча живий код їх несе. Сім рецептів чисті. Підозра про EntityStorages в PATTERN_CONFIG не підтвердилась — перевірено"
  :verified-by "прохід скіла над Patterns/, агент Sonnet 2026-09-16; doc_lint, fmgraph.py pattern, grep по Assets; звіт scratchpad/conformance-patterns.md"
  :consequence "правки рецептів — прямі й дешеві, крім рішення про маркери в скелетах"}

 {:finding :skills-restate-and-arch-check-scope
  :at "2026-09-16"
  :fact "Скіли: aligned 25, diverges 2, missing 1, extra 1, stale 1, second-home 2 системні (placement переписує 17 правил розміщення, pattern-choice — 18-20 правил вибору), uncited 1 системна. Жодного ID правила в обох скілах — 0 збігів grep. arch-check: згадує неіснуючі .claude/SEARCH_POLICY.md і arch-scout; білий список «Not flagged» дозволяє звичайні static-поля проти :state/mutable-static; :alloc/temp-scope не ловить Temp у коді, зданому в пул потоків поза async. ПРОГАЛИНА ОХОПЛЕННЯ: arch-check сканує Assets/Modules (40) + Assets/Scripts (46) = 86 файлів; Assets/Domains (133) + Assets/Presentation (151) = 285 із 371 файлу (77%) поза досяжністю — саме там живуть reactive, per-frame і turn-phase системи. Три правила :checked-by :none: :system/marker-vocabulary-parity, :state/mutable-static, :state/allowed-reason. behavior-recipes губить другу умову :transaction/when і не згадує :transaction/degenerate"
  :verified-by "прохід скіла над двома проєктними скілами і arch-check, агент Sonnet 2026-09-16; find по Assets; звіт scratchpad/conformance-skills.md"
  :consequence "77% коду поза детектором — найбільша діра примусу, дешевша за будь-яку іншу"}

 {:finding :tools-implement-three-quarters
  :at "2026-09-16"
  :fact "Граф, 58 правил :checked-by :graph: 23 :aligned, 21 :diverges, 14 :missing (перевірки нема: :archetype/birth, :tag/is, :tag/label-count, :archetype/arity-cap, :index/pk-uniqueness, :transaction/one-home та інші), :extra 0 — жоден чек не примушує правила поза специфікацією. Аналізатор, 4 правила: FM1004 і FM1001/FM1002 :aligned; FM1003 :diverges (ловить маркер без підписки, але «підписка без маркера» конструктивно недосяжна і живе в графі); :system/marker-not-inherited :missing. ЦИТАТ 0 із 48: ні [rule …] у 19 попередженнях графа, ні rule <prefix>/<slug> у 4 дескрипторах. Відхилення рецептів друкує лише pattern, не check — тому 12 живих порушень :view/boundary у check не видно. Обидві підозри підтверджені: roles.py:77-78 бере значення маркера без огляду на якір (RoleEvidence навіть не несе table_anchored), тоді як аналізатор відкидає Reactive на табличному якорі слово в слово за правилом; type_facts.py:144 вимагає папку І MonoBehaviour, а правило :view/layer каже «усе, що оголошено в папці». Живе непіймане порушення :archetype/birth — WorldInstaller.cs:61, голе world.CreateEntity"
  :verified-by "прохід скіла над fantasymayor-graph і MarkerShapeAnalyzer, агент Opus 2026-09-16; fmgraph.py check/stats/tags/pattern; звіт scratchpad/conformance-tools.md"
  :consequence "примус тримає 44 з 58 правил графа; цитування ID — робота на один прохід, 14 відсутніх перевірок — окрема"}

 {:finding :spec-node-kind-repeats-tool
  :at "2026-09-16"
  :fact "Сама специфікація в одному місці описує код інструмента, а не правило: конструкція :node-kind у (def constructs) повторює те, як інструмент упізнає вид вузла — рід (в), який власник вирішив лишити в коді інструмента"
  :verified-by "прохід звірки інструментів, агент Opus 2026-09-16"
  :consequence "дрібна правка специфікації — прибрати або переформулювати як конструкцію, а не як евристику"}

 {:finding :coverage-of-carriers
  :at "2026-09-16"
  :fact "Прохід покриття за новим критерієм (два агенти Opus, 244 правила): :carried 220, :incomplete 13, :uncarried 11. Не несе жоден носій, який читає агент: уся сімʼя маркера ролі — :system/marker-required, :system/marker-forbidden, :system/marker-value, :system/marker-not-inherited, :system/marker-vocabulary-parity, :system/role-order; далі :view/subscriber-marker, :state/mutable-static, :state/lifetime-flags, :alloc/binds-the-path, :singleton/manifest. grep SystemRole і ViewSubscriber по всіх носіях = 0, хоча обидва маркери дають помилку компіляції. Неповні 13: :tag/composed-by-declaration, :event/suffix, :system/definition, :state/is-state, :state/preupdate-cache, :alloc/banned-generic-types, :alloc/no-temp-in-async, :alloc/named-owner, :alloc/not-by-type, :turn-phase/base, :view/layer, :fail/silence-boundary, :recipe/signatures. Перерахунок: rules-configs несе 27 правил, не 26 — у специфікації число розділу занижене. Розміщення покрите на 100%"
  :verified-by "скіл fantasymayor-rules-conformance за критерієм :uncarried/:incomplete, два агенти Opus 2026-09-16; звіти scratchpad/uncarried-parts-1-2.md і uncarried-parts-3-4.md"
  :consequence "перегенерація носіїв починається з цих 24 правил: 11 дописати, 13 доповнити"}

 ;; прохід 2 — зовнішній пошук, 4 агенти Sonnet (:outbound-search-go); джерела — у звітах агентів, основні URL тут
 {:finding :no-precedent-python-plus-roslyn
  :at "2026-09-15"
  :fact "Жодного перевіреного випадку, де Python / tree-sitter інструмент і Roslyn-аналізатор читають один машиночитаний файл правил. Найближче: SonarSource RSPEC — одна специфікація правила, з якої rule-api генерує реалізації для кількох мов і сторінку правила (github.com/SonarSource/sonar-dotnet scripts/rspec) — але це великий командний конвеєр в одній родині інструментів. jMolecules issue #19: навіть зі спільними маркерами кожен верифікатор (ArchUnit, jQAssistant) заново реалізує їхнє тлумачення"
  :verified-by "агенти Sonnet (Roslyn-аналізатори; архітектурні правила як документація), веб 2026-09-15; я не перевіряв"
  :consequence "спільна специфікація для графа й аналізатора — нова територія, не спростована; дублювання логіки тлумачення зникає лише частково"}

 {:finding :drift-control-without-tests
  :at "2026-09-15"
  :fact "Звірку похідного з джерелом без тестів роблять п'ятьма способами: (1) згенерувати й порівняти з git diff у збірці — Meziantou.Analyzer генерує docs/README, сторінки правил і .editorconfig з метаданих аналізатора, CI падає на різниці (вторинне джерело DeepWiki); (2) мета-аналізатор при компіляції — RS2000-RS2008 з Microsoft.CodeAnalysis.Analyzers вимагають, щоб кожен ID діагностики був у AnalyzerReleases.Shipped/Unshipped.md; (3) LLM-прохід лише на читання — Spec Kit /speckit.analyze звіряє constitution / spec / plan / tasks шістьма проходами (дублі, неоднозначність, недовизначеність, відповідність constitution, покриття, неузгодженість) і не змінює файлів; (4) AST-відбитки — Fiberplane drift прив'язує твердження документа до коду і порівнює tree-sitter-відбитки; (5) скан графа з обмеженнями — jQAssistant (Neo4j, Cypher concepts / constraints) як крок збірки, з того ж графа генерує документацію arc42. Для .NET бібліотеки архітектурних правил (NetArchTest, ArchUnitNET) працюють лише всередині тестів"
  :verified-by "агенти Sonnet, веб 2026-09-15; сам прочитав templates/commands/analyze.md у github/spec-kit — read-only і шість проходів підтверджені; Meziantou — вторинне джерело, не перевіряв"
  :consequence "здогадка «похідне перевіряють тестами» спростована: звірка без тестів — звична практика; тутешня пара граф + аналізатор + крок процесу — у цьому ж класі"}

 {:finding :unity-additional-files
  :at "2026-09-15"
  :fact "Unity передає Roslyn-аналізатору дані з файлу: файл у Assets/ з назвою Filename.[AnalyzerName].additionalfile (назва аналізатора чутлива до регістру, у Filename без крапок) отримує кожна асемблі, для якої працює цей аналізатор; без імені аналізатора файл імпортується, але в компіляцію не йде. Документація — Unity 6.3 LTS"
  :verified-by "сам прочитав docs.unity3d.com/6000.3/Documentation/Manual/roslyn-analyzers-additional-files.html 2026-09-15"
  :consequence "аналізатор може читати проєкцію специфікації як дані при компіляції — механізм є, без зміни інструментів Unity"}

 {:finding :rule-ids-link-carriers
  :at "2026-09-15"
  :fact "Правило аналізатора зв'язується з документацією стабільним ID: DiagnosticDescriptor.HelpLinkUri веде на сторінку <Id>_<Name>.md, написану вручну (dotnet/roslyn-analyzers docs/Documenting your analyzers.md); RS2000 ловить ID без запису; SonarSource — ID RSPEC-NNNN спільний для специфікації, реалізації й сторінки"
  :verified-by "агент Sonnet (Roslyn-аналізатори), веб 2026-09-15; RS2000 — прочитано агентом напряму з каталогу правил"
  :consequence "стабільний ID правила — спосіб зв'язати запис специфікації з помилкою FM і попередженням графа"}

 {:finding :agent-spec-layering
  :at "2026-09-15"
  :fact "Інструменти spec-driven розробки (Spec Kit, Kiro steering, Cursor rules, AGENTS.md, Claude Code CLAUDE.md + skills) тримають джерело прозою в Markdown для LLM, не даними для скриптів; шари — умови завантаження (always / за glob / за описом / вручну). Claude Code: «Create a skill when a section of CLAUDE.md has grown into a procedure rather than a fact». Практики Kiro: без інструментальної синхронізації специфікації гниють, «AI does silently break things». Fowler: Tessl (специфікація як джерело коду) недетермінований при регенерації, паралель з провалом MDD"
  :verified-by "агент Sonnet (spec-driven), веб 2026-09-15; code.claude.com/docs/en/skills — первинне; dev.to звіти McAree і rizasaputra — практики; martinfowler.com sdd-3-tools — незалежний автор"
  :consequence "агентні шари без механічної звірки гниють; генерація похідного LLM-ом недетермінована — виводити краще звіркою, ніж регенерацією"}

 {:finding :policy-as-code-single-artifact
  :at "2026-09-15"
  :fact "OPA: один пакет (Rego + data, маніфест, підписи) читають усі точки примусу напряму, кожна вбудовує той самий парсер, а не пише свій читач. Метадані правил (# METADATA) існують, але офіційного генератора документації з них нема — мейнтейнер: «There's no official tooling for that currently» (github.com/orgs/open-policy-agent/discussions/415). Перевірка самих політик — opa test; Cedar Analysis — формальна перевірка політик, не документів. Практики: окрема мова політик — реальна ціна впровадження"
  :verified-by "агент Sonnet (policy-as-code), веб 2026-09-15; я не перевіряв"
  :consequence "урок: один канонічний парсер специфікації і згенерована проміжна проєкція (напр. JSON) замість двох ручних читачів Clojure-in-Markdown — висновок агента, джерелом не підтверджений"}

 {:finding :silent-rule-risk
  :at "2026-09-15"
  :fact "Правило, що ніколи не спрацьовує, не відрізнити від правила, що перестало щось знаходити; ArchUnit на байткоді дає хибні спрацювання й пропуски (TNG/ArchUnit issues 1424, 1160); аналізатор, що реєструється на кожен символ, дорожчає на кожне натискання клавіші в IDE; Unity Project Auditor вимикає Roslyn-перевірки за замовчуванням через час аналізу"
  :verified-by "агенти Sonnet, веб 2026-09-15; сторінка про «known-bad» — лише сніпет пошуку, слабко перевірено"
  :consequence "ризик для примусу: розширення аналізатора на незмарковані типи дорожчає; «0 порушень» не доводить, що правило живе"}]
```

# Decisions

```clojure
[{:decision :standard-scope
  :status :confirmed
  :at "2026-09-15"
  :value "усе, що код інструмента читає як правило: закон тегів, маркери ролей, view-boundary, ознаки 15 рецептів"
  :verified-by "власник: «1 - бери все»"
  :reason "стандарт, на який спирається інструмент, не має лишатися в документації інструмента"}

 {:decision :standard-home
  :status :confirmed
  :at "2026-09-15"
  :value ARCHITECTURE.md
  :options [{:option "закони в ARCHITECTURE.md, скелет рецепта показує маркер, документація інструмента посилається" :confidence 65}
            {:option "джерело — recipe-signatures.md інструмента, ARCHITECTURE.md посилається" :confidence 20}
            {:option "окремий документ стандарту" :confidence 15}]
  :verified-by "власник: «2 - ARCHITECTURE.md»"
  :reason "архітектурний стандарт — один документ"}

 {:decision :enforcement
  :status :confirmed
  :at "2026-09-15"
  :value "процесом (скіл вибору патерна + перевірка коду в CLAUDE.md) і перевіркою при компіляції (MarkerShapeAnalyzer)"
  :options [{:option "процесом" :confidence 55}
            {:option "процесом і компіляцією — окремою задачею" :confidence 45}]
  :verified-by "власник: «3 - процессом, але валідація при компіляції теж потрібна бо це маркер»"
  :reason "маркер — частина коду, тож компілятор має його перевіряти; компіляційна частина — у цій задачі, не окремо"}

 {:decision :doc-agent-review-status
  :status :confirmed
  :at "2026-09-15"
  :value "Flows/Archive/DOC_AGENT_REVIEW — status: closed-by-owner; значення додане в DOC_STANDARD і gen_index.py"
  :options [{:option "implemented, правда в Progress" :confidence 55}
            {:option "окремий архівний статус" :confidence 35}
            {:option "лишити partial" :confidence 10}]
  :verified-by "власник: «4 - закрито за моєю вимогою»; gen_index LINT clean після зміни"
  :reason "закриття без виконаних метрів записується як закриття власником, не як завершення"}

 {:decision :signatures-in-standard
  :status :confirmed
  :supersedes "Flows/Archive/ECS_GRAPH_PATTERN_INSTANCES → :signature-home"
  :at "2026-09-15"
  :value "в ARCHITECTURE.md — ознаки, що є правилом написання коду (роди а + б з :signatures-three-kinds); евристики впізнавання (рід в) лишаються в коді інструмента"
  :options [{:option "роди а + б у стандарті, рід в — у коді інструмента" :confidence 70}
            {:option "усі 15 ознак у стандарті" :confidence 30}]
  :verified-by "власник: «1 - а»"
  :reason "було: «зв'язок рецепт → ознака живе в коді інструмента»; перевірено: recipes.py — ознаки трьох родів; нове: стандарт, на який спирається інструмент, має бути записаний, а евристика впізнавання — не стандарт"}

 {:decision :view-definition
  :status :confirmed
  :at "2026-09-15"
  :value "view — усе, що лежить у папці Views/"
  :options [{:option "MonoBehaviour у Views/" :confidence 55}
            {:option "будь-який MonoBehaviour" :confidence 45}]
  :verified-by "власник: «2 - все що є в папці view»"
  :reason "одне визначення для закону, інструмента й аналізатора; обидва варіанти — не те, що обрав власник: без умови MonoBehaviour"}

 {:decision :redundant-marker-error
  :status :confirmed
  :at "2026-09-15"
  :value "[SystemRole] там, де роль визначає база, — помилка компіляції, як і відсутній маркер"
  :options [{:option "помилка компіляції" :confidence 60}
            {:option "лише попередження інструмента" :confidence 40}]
  :verified-by "власник: «3 - теж помилка»"
  :reason "маркер, що дублює базу, — ще одне місце, де маркер може збрехати"}

 {:decision :registration-forms
  :status :confirmed
  :at "2026-09-15"
  :value "в ARCHITECTURE.md записати всі форми реєстрації DI"
  :options [{:option "форми реєстрації + DeleteEntity + Priority, без маніфесту синглтонів" :confidence 60}
            {:option "усе" :confidence 25}
            {:option "нічого" :confidence 15}]
  :verified-by "власник: «4 - запиши всі форми реєстрації»"
  :reason "відповідь вужча за будь-який варіант; доля DeleteEntity, Priority, маніфесту і статус не підтримуваних інструментом форм — :registration-forms-scope"}

 {:decision :registration-forms-scope
  :status :open
  :at "2026-09-15"
  :value ?}

 {:decision :verification-step
  :status :confirmed
  :at "2026-09-15"
  :value "CLAUDE.md code-verification: fmgraph.py tags → fmgraph.py check з ціллю «жодного попередження у змінених файлах», плюс fmgraph.py pattern PATTERN_VIEW_SYSTEM, коли зачеплені view"
  :options [{:option "check, жодного попередження у змінених файлах, + pattern PATTERN_VIEW_SYSTEM" :confidence 60}
            {:option "check, попереджень не побільшало" :confidence 40}]
  :verified-by "власник: «5 - заміняй»"
  :reason "tags бачить лише закон тегів; check бачить і маркери"}

 {:decision :flow-contract
  :status :open
  :at "2026-09-15"
  :value ?
  :verified-by "власник: «6 - поясни детально»"}

 {:decision :outbound-search
  :status :open
  :at "2026-09-15"
  :value ?
  :verified-by "власник: «7 - поясни детально»"}

 {:decision :flow-contract-removed
  :status :confirmed
  :supersedes :flow-contract
  :at "2026-09-15"
  :value "вимогу :flow-contract з PATTERN_TRANSACTION_ENTITY прибрати"
  :options [{:option "прибрати вимогу" :confidence 50}
            {:option "каскад як контракт" :confidence 40}
            {:option "окремий постійний документ" :confidence 10}]
  :verified-by "власник: «6 - прибирай цю вимогу»"
  :reason "було: відкрите питання; нове: FLOW за 0.3.0 — історія, ланцюжок подій і записів дає fantasymayor-graph"}

 {:decision :outbound-search-allowed
  :status :open
  :supersedes :outbound-search
  :at "2026-09-15"
  :value "пошук дозволено; тема змінилась поправкою :rules-specification — переформулювання чекає підтвердження"
  :options [{:option "не шукати" :confidence 60}
            {:option "шукати в проході 2" :confidence 40}]
  :verified-by "власник: «7 - можеш пошукати…»"
  :reason "тема з «синхронізації трьох перевірок» стала «одна специфікація, з якої виводиться решта» — за outbound-gate переформулювати й підтвердити"}

 {:decision :spec-answers
  :status :confirmed
  :at "2026-09-15"
  :value {:name RULES_SPECIFICATION.md
          :bounds "кожне правило, якого має дотримуватись код"
          :form "Clojure-блоки в .md"
          :validate "ARCHITECTURE.md, скіли, рецепти Patterns/, код fantasymayor-graph, MarkerShapeAnalyzer, arch-check — читання й знахідки"
          :architecture-after "лише доповнення для агента з посиланням на специфікацію; текст правил не повторює"
          :spec-flow "окремий FLOW, шлях :cascade — створення специфікації"
          :di-registration "не записується — блок під рефакторингом, записане згниє"
          :doc-standard "видалити — власник спирається на код, скіли й інструменти пошуку"}
  :options {:name [{:option CODEX.md :confidence 60} {:option RULES_SPECIFICATION.md :confidence 40}]
            :bounds [{:option "правила коду" :confidence 60} {:option "+ документи й процес" :confidence 25} {:option "лише механічно перевірювані" :confidence 15}]
            :form [{:option "Clojure у .md" :confidence 60} {:option ".edn" :confidence 40}]
            :validate [{:option "агентний ланцюжок + інструменти" :confidence 60} {:option "лише агентний ланцюжок" :confidence 40}]
            :architecture-after [{:option "доповнення + посилання" :confidence 60} {:option "похідна копія правил" :confidence 40}]
            :enforcement [{:option "у цьому FLOW після специфікації" :confidence 55} {:option "окремою задачею" :confidence 45}]
            :di-forms [{:option "не читані — заборонені" :confidence 65} {:option "дозволені" :confidence 35}]}
  :verified-by "власник: «1 - RULES_SPECIFICATION.md 2 - Кожне правило, якого має дотримуватись код. А щодо DOC_STANDARD - думаю можна видаляти … 3 - Clojure 4 - перше 5 - перше 6 - я думаю це буде окремий flow через cascade для створення цієї супер специфікації … 8 - цей блок буде рефакторитися тому я хз що тут записати бо будь-що записане згниє»"
  :reason "відповіді на питання поправки :rules-specification; :registration-forms і :registration-forms-scope знято відповіддю 8"}

 {:decision :outbound-search-go
  :status :confirmed
  :supersedes :outbound-search-allowed
  :at "2026-09-15"
  :value "веб-пошук за чотирма темами, по одному агенту Sonnet на тему: policy-as-code (OPA/Rego); правила архітектури як жива документація (ArchUnit/ArchUnitNET, jMolecules); spec-driven розробка для агентів (GitHub Spec Kit, Kiro); правила аналізаторів Roslyn з документацією на кожен ID діагностики"
  :options [{:option "звичайний пошук у проході 2" :confidence 60} {:option "повний /sdd-research" :confidence 40}]
  :verified-by "власник: «7 - шукай, відправ окремих sonnet агентів на пошук по всіх перелічених темах по одному на кожну»"
  :reason "форма специфікації і звірка похідного з джерелом без тестів"}

 {:decision :spec-flow-answers
  :status :confirmed
  :at "2026-09-16"
  :value {:cascade-mapping "CONTEXT — носії правил, знахідки й пошук; s1 — набір правил і дані, про які вони говорять, без розкладки; s2 — структура документа (розділи, def-імена); «код» — RULES_SPECIFICATION.md як переклад s2; read-back і converge — звірка з носіями"
          :this-flow "GRAPH_STANDARD переписати так, щоб він продовжився після каскаду специфікації"
          :code-story-rules "CODE_STORY_RULES_PROPOSAL.md видалити — він став каскадом; у специфікацію не йде"
          :doc-standard "DOC_STANDARD.md замінити окремим скілом, що перевіряє написаний документ"
          :docs-direction "нових документів не створювати, хіба доповнення чи зміни в Patterns/; решта — на скілах і інструментах"}
  :options {:cascade-mapping [{:option "відображення каскаду на документ" :confidence 70} {:option "без s2" :confidence 30}]
            :this-flow [{:option "лишається, бере звірку й примус після специфікації" :confidence 65} {:option "закрити" :confidence 35}]
            :code-story-rules [{:option "включити в специфікацію" :confidence 55} {:option "не включати" :confidence 45}]
            :doc-standard [{:option "видалення — окрема пряма задача" :confidence 70} {:option "у FLOW специфікації" :confidence 30}]}
  :verified-by "власник: «1 - так 2 - цей flow повинен бути переписаним під те щоб бути продовженим після виконання каскаду 3 - CODE_STORY_RULES_PROPOSAL видаляй, це те що вже стало каскадом. 4 - DOC_STANDARD можна замінити окремим скілом … Решта має лягти на скіли та тули.»; правила розповіді знайдено в .claude/skills/sdd-cascade/SKILL.md (sequence, plot, guard-occasion, pair-is-a-type, lives)"
  :reason "відповіді на питання постановки FLOW специфікації"}

 {:decision :cascade-launch-answers
  :status :confirmed
  :at "2026-09-16"
  :value {:cascade "Flows/RULES_SPECIFICATION — каскад в авто-режимі, кожна стадія окремим агентом; власник бачить підсумок фінального артефакту"
          :stable-rule-ids :yes
          :edn-valid :yes
          :validation "скіл rules-conformance, який можна перезапускати: LLM-прохід лише на читання — дублі, суперечності, покриття специфікація ↔ носії"
          :doc-check-skill {:checks "запуск gen_index і doc_lint + написано для агента, без живих назв, без історії, правила у значеннях" :when "після каскаду"}}
  :options {:stable-rule-ids [{:option "так" :confidence 75} {:option "ні" :confidence 25}]
            :edn-valid [{:option "так" :confidence 65} {:option "вільні форми" :confidence 35}]
            :validation [{:option "скіл rules-conformance" :confidence 65} {:option "разовий прохід" :confidence 35}]
            :doc-check-checks [{:option "метри + агентні вимоги" :confidence 60} {:option "лише метри" :confidence 40}]
            :doc-check-when [{:option "зараз" :confidence 55} {:option "після каскаду" :confidence 45}]}
  :verified-by "власник: «Запускай каскад в авто режимі в окремому агенті. Мені покажеш вже підсумок з фінального артефакту 1 - так 2 - так 3 - перше 4 - перше 5 - після каскаду»"
  :reason "go на каскад і відповіді на питання трьох постановок"}

 {:decision :carriers-repeat-the-rule
  :status :confirmed
  :supersedes :spec-answers
  :at "2026-09-16"
  :value "ARCHITECTURE.md і CLAUDE.md — єдині файли, з яких агент бере правила; скіли й рецепти — такі самі носії. Носій несе правило ПОВНІСТЮ; специфікація під час роботи не читається — вона джерело для підтримки. Повтор між специфікацією і носієм — норма"
  :verified-by "власник: «ARCHITECTURE та CLAUDE це ті два єдині файли з яких ми маємо все отримувати. Це ок якщо специфікація для цих документів містить те ж саме що й вони, бо специфікація ніколи не буде читатися агентом. Тому твоя ідея що давай посилатися на специфікація - повна дурня»; «так, скіли теж носії — переписуй звірку під цей критерій»"
  :reason "було (:spec-answers :architecture-after): ARCHITECTURE лишає лише доповнення з посиланням на специфікацію; нове: носій, що лише посилається, не вчить агента нічого"}

 {:decision :conformance-criterion
  :status :confirmed
  :at "2026-09-16"
  :value "скіл fantasymayor-rules-conformance переписаний: міряє не «чи цитує носій», а «чи каже те саме і чи нічого не загубив». Вердикти: :aligned :diverges :incomplete :missing :uncarried :extra :stale :uncited (лише для точок примусу); повтор формулювання вердиктом не є"
  :verified-by "власник: «переписуй звірку під цей критерій»; файл .claude/skills/fantasymayor-rules-conformance/SKILL.md переписаний 2026-09-16"
  :reason "критерій першого проходу був хибний — 108 знахідок «другий дім» анульовані"}]
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
 :completed #{"постановка підтверджена 2026-09-15 (go на прохід 1)"
              "прохід 1 — інвентар, 11 знахідок 2026-09-15"
              "прохід 2 — 4 пошуки Sonnet, 7 знахідок; Unity additionalfile і Spec Kit analyze перевірені вручну 2026-09-15"
              "CODE_STORY_RULES_PROPOSAL.md видалено за словом власника 2026-09-16: файл (vault_delete), рядок і абзац у INDEX, пам'ять project_code_style_rules_program + рядок MEMORY.md, 3 вікілінки перенаправлені на sdd-cascade; gen_index LINT clean, doc_lint 3 старі привиди"}
 :closed {:at "2026-09-16"
          :raw "закривай всі flow-и. Подивлюся що там буде в реальних задачах"
          :outcome "звірка ланцюжка і примус зроблені й закомічені (e853ef5, 512aa1f, ~/.claude acded80); решта не виконана і чекає окремих задач"
          :left-over #{"скіл перевірки документів і видалення DOC_STANDARD.md разом зі згадками — gen_index.py, .sdd-flow/project.md # Shape, шапка ARCHITECTURE.md, INDEX, 6 файлів памʼяті"
                       "перевірка власника в Unity — чи не зʼявилось нових помилок FM1001-FM1008"
                       "8 живих порушень, які знайшли нові перевірки: 2 народження повз архетип (WorldInstaller.cs:61, EcsEventExtensions.cs:21), 4 збережені дескриптори сутності (SingletonComponents.cs:12, TerrainViewSystem.cs:42, WaterViewSubSystem.cs:39, HexSelectionViewLoadingSystem.cs:29), запис індексованого PK без throw (BuildDistrictActionSystem.cs:99), BuildDistrictInProgress без колонки стадії"
                       "2 розбіжності перевірок графа, де правий інструмент, а не правило — :catalogue/row-shape і :recipe/signatures (12 ознак проти 15 рецептів)"
                       "давній список рефакторингу коду з архівного DOC_AGENT_REVIEW"}}
 :current :none   ;; 2026-09-16: продовжується після каскаду Flows/RULES_SPECIFICATION — поправка 2026-09-16 у # Amendments: :validate-rules-chain → :enforce-rules, :doc-check-skill
 :completed #{"скіл .claude/skills/fantasymayor-rules-conformance створено 2026-09-16 — процедура звірки носіїв проти RULES_SPECIFICATION.md, лише знахідки, без правок"
              "звірка ланцюжка 2026-09-16 — 4 паралельні проходи (ARCHITECTURE, Patterns, скіли, інструменти); знахідки в # Findings, звіти в теці сесії"
              "критерій звірки виправлено власником 2026-09-16 — носій несе правило повністю; скіл переписаний, 108 знахідок «другий дім» анульовані"
              "покриття виміряне 2026-09-16 — 220 :carried, 13 :incomplete, 11 :uncarried"
              "носії оновлені 2026-09-16 — ARCHITECTURE.md перегенерований зі специфікації (228 → 817 рядків, 214 правил, ID як ключ запису); CLAUDE.md (крок check замість tags + гілка view, скіл звірки в маршрутизації, специфікація поза :read, новий § 6 «Як змінити правило»); 10 рецептів Patterns/ (транзакція, маркери в 4 скелетах, мертві посилання, звільнення дескриптора, пріоритети, межа view); pattern-choice (маркери, обидві умови транзакції, вироджений випадок, гілка ознак за маркером); arch-check переписаний (5 коренів сканування замість 2, цитування 28 ID, білий список під :state/mutable-static, блок відомих прогалин); placement звірений — змін не потребує; привиди 3 → 0"
              "специфікація приведена до нових перевірок 2026-09-16 — :state/escape-order і :state/mutable-static тепер :checked-by :arch-check; tally: агент 152, граф 58, arch-check 28, аналізатор 4, ніхто 2"
              "звірка ланцюжка закрита 2026-09-16 — переміряно після оновлення носіїв: 244/244 :carried, 0 :incomplete, 0 :uncarried; 13 битих вказівників на розділи ARCHITECTURE і 3 розбіжності рецептів виправлені; коміт e853ef5 (проєкт) і acded80 (~/.claude)"
              "примус зроблено 2026-09-16, коміт 512aa1f — аналізатор: FM1005 відсутній маркер, FM1006 зайвий, FM1007 підписка без маркера, FM1008 успадковуваний маркер, FM1003 добудований, цитати ID у всіх діагностиках, view за папкою; граф: 14 відсутніх перевірок реалізовано, 19 із 21 розбіжності закрито, роль зважує маркер проти якоря, 34 точки цитують правило; check 8 → 32 попереджень, 8 живих порушень у коді; позначки специфікації приведені до дійсності"}
 :search-guess-outcome {:outcome :confirmed-with-deviation
                        :deviation "перша половина (інструменти читають специфікацію як дані) — підтримана OPA, але для пари Python + Roslyn прецеденту нема; друга половина (похідне перевіряють тестами) — спростована: звірка без тестів звична (generate-and-diff, мета-аналізатор, LLM-прохід, AST-відбитки, скан графа); лише .NET-бібліотеки архітектурних правил прив'язані до тестів"}
 :search-guess {:hunch "практика сходиться на «правило як код»: виконуване правило і є специфікацією (ArchUnit, OPA); специфікація-дані, яку читають кілька незалежних перевірок без генерації і без тестів, трапляється рідко; похідне зазвичай генерують з одного машиночитаного джерела, а відповідність рукописного похідного перевіряють тестами, які тут заборонені — тож найімовірніший вихід: інструменти читають специфікацію як дані напряму, документи для агента виводить агент"
                :confidence 55
                :grounded-in "знання агента — до пошуку"
                :at "2026-09-15"}
 :remaining #{":doc-check-skill — скіл перевірки документів і видалення DOC_STANDARD.md разом зі згадками" "шапка ARCHITECTURE.md досі посилається на DOC_STANDARD" "перевірка власника в Unity — нових помилок FM нема" "8 живих порушень, які знайшли нові перевірки — окрема задача рефакторингу коду"}
 :resume-context "Проходи 1-2 завершено (# Findings). Задача переписана поправкою 2026-09-16: спершу Flows/RULES_SPECIFICATION (каскад в авто-режимі), потім тут — скіл rules-conformance і звірка ланцюжка, примус (аналізатор + CLAUDE.md), скіл перевірки документів замість DOC_STANDARD."}
```

# Acceptance

```clojure
[{:meter "python3 Tools/doc_lint.py --quiet" :target "0 синтаксичних помилок; привиди не зросли" :actual ? :status :pending}
 {:meter "python3 Tools/gen_index.py" :target "LINT clean" :actual ? :status :pending}
 {:meter "інвентар правил" :target "кожне правило має один дім в ARCHITECTURE.md" :actual ? :status :pending}
 {:meter "dotnet build Tools/MarkerShapeAnalyzer" :target "clean" :actual ? :status :pending}
 {:meter "fmgraph.py check" :target "0 невизначених ролей" :actual ? :status :pending}
 {:meter "Unity-перевірка власника" :target "проєкт компілюється; клас без обов'язкового маркера дає помилку FM" :actual ? :status :pending}]
```

# Amendments

```clojure
[{:received-at "2026-09-15"
  :raw-request ["6 - прибирай цю вимогу.\n7 - можеш пошукати але що я скажу зі своєї сторони і з того що вже бачу. Покривати все документацією це марна справа, бо будь-яка зміна херить все. Більше того правила це теж свого роду тип документації і як тільки правила змінюються то гниє все: код, документи, тули.\nОтже який вихід я бачу з цього - специфікація правил. Умовно кажучи ARCHITECTURE перестає бути специфікацією, а стає носієм доповнюючих правил в agent pipeline: Codex -> ARCHITECTURE -> skills\nа отже ми повинні окремо мати повну специфікацію і наступного разу якщо захочемо щоб змінювати, то будемо це робити не в 3 місцях ручками, а в одній глобальній специфікації а все решта потім виведеться з цієї специфікації. А отже зараз потрібно зібрати цю повну специфікацію в окремий супер важливий файл і лише потім провалідувати Codex -> ARCHITECTURE -> skills на предмет відповідності."]
  :normalized {:task :rules-specification
               :goal "одна повна специфікація правил — єдине джерело; ARCHITECTURE.md і скіли стають похідними носіями доповнюючих правил для агента"
               :path :direct                     ;; 65 — документ і звірка; :cascade 35
               :decided #{"покривати все документацією марно — правило змінюється в одному місці, у специфікації"
                          "спершу зібрати специфікацію, потім звірити ланцюжок Codex -> ARCHITECTURE -> skills"
                          "наступна зміна правила — лише в специфікації; решта виводиться з неї"}
               :do (-> "зібрати повну специфікацію з усіх нинішніх носіїв правил"
                       "власник приймає специфікацію"
                       "звірити Codex -> ARCHITECTURE -> skills на відповідність → знахідки"
                       "рішення за знахідками")
               :skip #{"механізм автоматичного виведення — наступного разу"}
               :open #{"назва й форма файлу специфікації" "межі «повної»" "що звіряється" "доля ARCHITECTURE.md" "місце примусу в порядку" "тема зовнішнього пошуку"}}
  :confirmed true}                      ;; 2026-09-15: відкриті питання закриті :spec-answers; специфікація — окремий FLOW через :cascade, його постановка чекає підтвердження

 {:received-at "2026-09-16"
  :raw-request ["2 - цей flow повинен бути переписаним під те щоб бути продовженим після виконання каскаду"]
  :normalized [{:task :validate-rules-chain
                :listen "Flows/RULES_SPECIFICATION → :rules-specification"
                :goal "ARCHITECTURE.md, скіли, рецепти Patterns/, fantasymayor-graph, MarkerShapeAnalyzer, arch-check відповідають RULES_SPECIFICATION.md"
                :path :direct
                :decided #{"ARCHITECTURE.md — лише доповнення для агента з посиланням на специфікацію"
                           ":flow-contract прибрати з PATTERN_TRANSACTION_ENTITY; решта :transaction-entity-stale — за специфікацією"
                           "view — усе в Views/"
                           "звірка — скіл rules-conformance, який можна перезапускати (:cascade-launch-answers)"}
                :do (-> "скіл rules-conformance" "звірка → знахідки" "власник" "правки документів і скілів; код інструментів — лише знахідки")
                :result "ланцюжок без розбіжностей зі специфікацією"}
               {:task :enforce-rules
                :listen :validate-rules-chain
                :path :direct
                :where #{Tools/MarkerShapeAnalyzer "DLL аналізатора в Assets — лише байти" CLAUDE.md ".claude/skills/fantasymayor-pattern-choice/SKILL.md"}
                :decided #{"помилка компіляції: відсутній і зайвий [SystemRole], підписка на подію view без [ViewSubscriber]"
                           "CLAUDE.md: fmgraph.py check — жодного попередження у змінених файлах + pattern PATTERN_VIEW_SYSTEM, коли зачеплені view"}
                :result "порушення не проходить ні компіляцію, ні перевірку коду"}
               {:task :doc-check-skill
                :listen :rules-specification
                :path :direct
                :goal "скіл перевіряє написаний документ; DOC_STANDARD.md видаляється разом зі згадками (gen_index.py, project.md # Shape, ARCHITECTURE.md, INDEX, пам'ять)"
                :decided #{"перевіряє: gen_index і doc_lint + написано для агента, без живих назв, без історії, правила у значеннях" "після каскаду"}
                :name :by-naming-policy}]
  :supersedes #{":record-graph-standard — дім стандарту тепер RULES_SPECIFICATION.md" ":enforce-graph-standard — переписано як :enforce-rules"}
  :confirmed true}]                     ;; 2026-09-16: власник наказав переписати; відповіді — :cascade-launch-answers
```
