---
category: A
read: archive
status: implemented
tags:
  - generation
  - readability
  - code-style
  - cascade
  - validation
related:
  - "[CODE_STYLE_RULES_WIP](../../CODE_STYLE_RULES_WIP.md)"
  - "[ECS_CONVENTIONS](../../ECS_CONVENTIONS.md)"
  - "[FLOW_TEMPLATE](../../FLOW_TEMPLATE.md)"
---

# FLOW — Lake rewrite by style rules

Перша перевірка CODE_STYLE_RULES_WIP на класі, з якого правила не виводились: каскад ПРОВАЛЕНО на рівні процесу — записано як результат, чернетку видалено.

# 0 · Вердикт — каскад провалено

```clojure
(def outcome  ;; 2026-09-07, вердикт власника після читання всіх трьох артефактів
  {:status :failed-at-process
   :deliverable-deleted LakeGenerationSubSystemRewrited   ;; клас видалено на вимогу власника
   :raw-verdict ["я не бачив розвитку алгоритму, flow мав працювати як мінімальні деталі -
                  розвиток концепції - готовий алгоритм. Натомість ти зконцентрувався на коді який прочитав"
                 "алгоритм взагалі ніде не був виведений чи пояснений"
                 "жоден з трьох артефактів не був тим чим має бути short snippet"
                 "фіксація на деталях яких не було: наприклад колекції чи я передавати значення,
                  це все аналіз коду що вже був написаний до цього"]

   :diagnosis
   {:substitution "предметом каскаду став НАЯВНИЙ КОД, а не алгоритм"
    :evidence ["s1 :steps описували методи оригіналу (ApplyLakeLevels), а не те, чим є озеро"
               "усі 12 findings — про наявний код: try/finally, mapDomain, TryGetEntityById, ClearLakeLevels"
               "s2 «5 колекцій → 2» — метрика чужого коду, не властивість озера"
               "s3 struct-проти-class, копія структури, in-параметр — деталі реалізації"
               "формула 3/2/0.35 ПЕРЕНЕСЕНА з рядків 326-329; агент жодного разу не сказав,
                чому вага сусідів утричі більша за відстань"]
    :size "s1 ≈ 60 рядків Clojure замість short snippet — усе фронт-лоуднуто на першому етапі"
    :root "оригінал лежав у контексті, і агент узяв його за ПРЕДМЕТ опису, а не за джерело чисел"
    :repeat "власник назвав цю ж підміну ще на s1 (:contra відхилено цілком); агент виправив
             один артефакт, але не спосіб роботи — і повторив її ще двічі"}

   :what-it-should-have-been
   {:s1 "Озеро — зв'язна пляма води біля центру. Площа — частка карти."
    :s2 "Росте від насінини: щоразу приєднується найкращий сусід плями.
         «Найкращий» = тримається купи й не тікає від центру."
    :s3 "формули, ваги, причини зупинки, крайові випадки"
    :subject #{:зв'язність :компактність :площа}
    :original "відкривається РІВНО ОДИН раз — зняти числа"}

   :value "єдиний надійний результат сесії: каскад у руках агента перероджується в реверс-інжиніринг,
           якщо старий код лежить у контексті. Це те, чого CODE_STYLE_RULES_WIP не забороняв"})
```

# 1 · Request

## User request — 2026-09-07 (verbatim)

> в нас сьогодні за планом перевірка CODE_STYLE_RULES_WIP.
> В нас є GenerationSubSystem там 3 нащадки які цікаві: lake, river та sea і будемо пробувати йти за тим підходом що описаний в CODE_STYLE_RULES_WIP, почнемо з LakeGenerationSubSystem і зробимо файл LakeGenerationSubSystemRewrited

Відповідь на питання агента (2026-09-07):

> новий FLOW, повний каскад, контракт - зберігай алгоритм, але не переписуй код, а пиши з 0, supervised, чистий аркуш, чернетка інертна

## Agent restatement — confirmed by the user

```clojure
{:task :lake-rewrite-by-style-rules
 :goal "перевірити CODE_STYLE_RULES_WIP на класі, з якого правила НЕ виводились — Lake перший
        з трьох цікавих нащадків GenerationSubSystem (далі River, Sea окремими задачами)"
 :where [LakeGenerationSubSystem                      ;; читаю як джерело АЛГОРИТМУ
         ^:new "Assets/Domains/Map/Generation/Systems/LakeGenerationSubSystemRewrited.cs"]
 :pattern CODE_STYLE_RULES_WIP                        ;; каскад + правила 0-8 + метри
 :off-limits #{MountainGenerationSubSystem MountainGenerationSubSystemNative
               MountainGenerationSubSystemRewrited MountainGenerationSubSystemNoConst
               MountainGenerationSubSystemStateless
               "оригінал LakeGenerationSubSystem" RiverGenerationSubSystem SeaGenerationSubSystem
               GenerationSystem TerrainGeneratorInstaller
               "Unity-side: .meta, компіляція, реєстрація"}
 :decided ["новий FLOW — не дописуємо у FLOW_READABLE_GENERATION"
           "повний каскад s1→s2→s3, кожен етап з dual-check"
           ":supervised — стоп після кожного етапу, чекаю слова власника"
           "алгоритм зберігається; код НЕ переписується — пишеться з нуля за розкладкою"
           "чистий аркуш: видалену Codex-чернетку Lake з git-історії не читаю"
           "чернетка інертна — реєстрацію не чіпаю, власник перемикає сам"]
 :skip #{"зміни поведінки" "River і Sea" "hook" "правки оригіналу"}
 :accept [{:meter "метри доку" :target {:ref-params 0 :finally 0 :xml-doc-lines 0
                                        :mutable-system-fields 0 :max-params "≤3"
                                        :median-params 1 :wrapped-signatures "0%"}}
          {:meter mcp__roslyn__get_diagnostics :target "0 Warning+ на новому файлі"}
          {:meter arch-check :target "0 порушень"}
          {:meter "читання власника" :target "зверху вниз без стрибків"}]
 :result "LakeGenerationSubSystemRewrited.cs + звіт валідації правил на НЕ-Mountain класі"}
```

```clojure
{:decision :validation-report-home :status :assumed :at "2026-09-07"
 :value "звіт валідації живе в цьому FLOW; правка CODE_STYLE_RULES_WIP пропонується власнику окремо в кінці"
 :reason "питання :q7 лишилось без відповіді; редагування доку потребує дозволу власника — відкрито для вето"}
```

## Amendments (append-only)

```clojure
[]
```

# 2 · Contract

## Findings

```clojure
[{:finding :water-types-are-exclusive
  :at "2026-09-07"
  :fact "River / Lake / Sea взаємно виключні: кожна система виходить, якщо
         TerrainGenerationConfigComponent.WaterType не її тип"
  :verified-by "прочитано guard-и: Lake рядок 55, River рядок 60, Sea рядок 55"
  :consequence "суцільний запис нуля в Lake НЕ затирає River — вони ніколи не працюють разом"}

 {:finding :lake-writes-the-whole-map
  :at "2026-09-07"
  :fact "ApplyLakeLevels пише HexLevelComponent КОЖНОМУ гексу: -1 озеру, 0 усім іншим —
         це не фарбування озера, це ініціалізація базового рівня всієї карти"
  :verified-by "LakeGenerationSubSystem.cs:76-87 — цикл по всіх entities архетипу Hex"
  :consequence "крок «запис» має ДВІ відповідальності; в розкладці це має бути видно, а не ховатись у назві ApplyLakeLevels"}

 {:finding :mountain-runs-after-lake
  :at "2026-09-07"
  :fact "SystemPriorities.SubSystems.Generation: River 200, Lake 210, Sea 220, Mountain 300"
  :verified-by "Assets/Scripts/EcsExtensions/SystemPriorities.cs:95-101"
  :consequence "Mountain читає рівні ПІСЛЯ Lake; базовий нуль від Lake — його вхідна умова.
                Порядок-чутливий зв'язок, який зобов'язаний потрапити в :order-sensitive розкладки"}

 {:finding :clear-lake-levels-is-dead
  :at "2026-09-07"
  :fact "ClearLakeLevels (рядки 137-159) не має жодного виклику в Assets"
  :verified-by "grep ClearLakeLevels по Assets --include=*.cs поза самим файлом → 0 збігів"
  :consequence "мертвий код; у версію з нуля не переноситься"}

 {:finding :ownership-transfer-drives-the-ladder
  :at "2026-09-07"
  :fact "GrowLake ПОВЕРТАЄ NativeParallelHashSet — саме передача власності породжує
         catch/Dispose/throw усередині нього і три вкладені try/finally у Generate"
  :verified-by "рядки 209-258 (три рівні try) і 274-303 (try{try{…}catch{Dispose;throw}}finally)"
  :consequence "правило 3 тут лікує наслідок; причина — правило 1 (немає власника прогону)"}

 {:finding :two-shapes-of-one-set
  :at "2026-09-07"
  :fact "mapCoords (NativeList<int2>) і mapDomain (NativeParallelHashSet<int2>) тримають
         ОДНІ Й ТІ САМІ координати у двох формах: список для перебору, множина для Contains"
  :verified-by "BuildMapCoords рядки 106-111 заповнює обидві в одному циклі"
  :consequence "не рівно правило 2 (індексної синхронності немає), але той самий корінь:
                зв'язок двох колекцій тримає читач у голові"}

 {:finding :entity-refetch-ignores-failure
  :at "2026-09-07"
  :fact "TryGetEntityById повертає bool, який ігнорується; при провалі AddComponent
         викликається на default(Entity)"
  :verified-by "рядки 85-86 і 151-152 — out var entity без перевірки результату"
  :consequence "порушення fail-loud; у версії з нуля провал має кидати явно"}

 {:finding :seed-fallback-is-unreachable-but-empty-domain-crashes
  :at "2026-09-07"
  :fact "SelectSeed завершується `return seedDomain[seedDomain.Length - 1]`; за коректної
         вагової суми цей рядок недосяжний, але на порожньому seedDomain падає з
         IndexOutOfRange — і раніше за нього Random.Range(0, 0) поверне 0"
  :verified-by "рядки 348-368; ваги додатні (maxDistance - distance + 1 ≥ 1 у межах домену)"
  :consequence ":ends-with кроку «вибір насінини» мусить назвати порожній домен як окрему причину"}

 {:finding :zero-level-is-born-not-written
  :at "2026-09-07"
  :fact "HexLevelComponent — birth-колонка архетипу Hex, і GenerationSystem.Generate пише
         `new HexLevelComponent { Level = 0 }` КОЖНОМУ гексу під час створення карти"
  :verified-by "MapArchetypes.cs:28 (ComponentTypes.Get<HexIdPKComponent, HexLevelComponent, HexTypeComponent>)
                + GenerationSystem.cs:100 — явний запис нуля при CreateEntity; підтверджує слова власника
                «все по 0 виставлено з самого початку, генерація води це завжди перший крок»"
  :consequence "суцільний запис нуля в Lake — надлишковий: він пише значення, яке там уже стоїть.
                Новий Lake пише ЛИШЕ гекси озера. Спростовує наслідок finding :lake-writes-the-whole-map:
                Mountain залежить не від Lake, а від народження гексу"}

 {:finding :map-is-a-full-hexagon
  :at "2026-09-07"
  :fact "карта — суцільний шестикутник радіуса WaveCount-1 навколо (0,0): GenerationSystem створює
         рівно GetTotalHexCountForWaves(WaveCount) = 3N²-3N+1 гексів по спіральному індексу,
         а це точно кільця 0..N-1"
  :verified-by "HexesUtil.cs:21-24 і 26-62 (спіраль: центр, далі кільце k з 6k гексів)
                + GenerationSystem.cs:94-101; збігається з mapRadius = WaveCount-1 в Lake:231 і Sea:208"
  :consequence "належність до карти — ПРЕДИКАТ (distance(coord,0) ≤ mapRadius), а не колекція:
                mapDomain і mapCoords можуть зникнути обидва. Це прибирає корінь finding :two-shapes-of-one-set"}

 {:finding :seed-domain-is-an-index-prefix
  :at "2026-09-07"
  :fact "спіральний індекс іде кільцями від центру, тож УСІ гекси в межах відстані r — це суцільний
         префікс індексів [0, GetTotalHexCountForWaves(r+1))"
  :verified-by "структура IndexToAxialCoords: ringStart += 6*ring по кільцях;
                GetTotalHexCountForWaves(r+1) = 3(r+1)²-3(r+1)+1 = сума кілець 0..r"
  :consequence "BuildSeedDomain (фільтрувальний прохід + власна NativeList + try/finally) може зникнути;
                але ціна — виклик IndexToAxialCoords, див. finding :index-to-coords-allocates"}

 {:finding :index-to-coords-allocates
  :at "2026-09-07"
  :fact "IndexToAxialCoords алокує КЕРОВАНИЙ масив `int2[] dirs = {…}` на кожен виклик"
  :verified-by "HexesUtil.cs:44; єдиний нинішній виклик — GenerationSystem.cs:98, hexCount разів при створенні карти"
  :consequence "сміття в кучі на кожен виклик — але одноразове, при створенні карти.
                Порядок: seed-домен радіуса 16 ≈ 817 гексів × 2 проходи × ~72 Б ≈ 120 КБ, один раз.
                Не дискваліфікує варіант — здорожчує його при нульовій вигоді, бо координати
                працюють напряму (відповідь власника 2)"
  :corrected-at "2026-09-07"
  :correction "перша редакція цієї знахідки називала це «прямим конфліктом із zero-allocation»;
               помилка — див. finding :zero-allocation-is-about-lifetime"}

 {:finding :zero-allocation-is-about-lifetime
  :at "2026-09-07"
  :fact "zero-allocation у цьому проєкті — про ЧАС ЖИТТЯ й тиск на GC, а не про заборону керованих
         типів: «памʼять виділена на початку і звільнена в кінці роботи алгоритму чи сутності;
         можна structure, spans, native collections — усе, що ми можемо вручну вичистити»;
         порушення — «значний обʼєм памʼяті лишився в кучі і чекає GC проходу»"
  :verified-by "пряме виправлення власника 2026-09-07, після того як агент застосував правило як заборону типу"
  :consequence "критерій — «хто і коли це звільняє», а не «який це тип». Виправлено пам'ять агента;
                /arch-check Rule 3 і його опис досі кодують старий плоский бан на керовані масиви
                в системах, тобто перетриггерюють — правку запропоновано власнику окремо"}

 {:finding :rule-4-collides-with-try-convention
  :at "2026-09-07"
  :status :void
  :fact "правило 4 вимагає sentinel-ТИП (.None/.IsFound) для «не знайдено»; стала конвенція проєкту
         вимагає Try-патерн — bool-повернення плюс out/ref"
  :verified-by "CODE_STYLE_RULES_WIP правило 4 проти конвенції collector-методів"
  :voided-by "власник 2026-09-07: у Lake стану «не знайдено» не існує — ні для насінини (навіть 0,0
              це середина карти), ні для межі (цикл стереже frontier.Count() > 0)"
  :consequence "зіткнення БУЛО передчасним: воно виникає лише там, де «не знайдено» — реальний стан.
                Урок для звіту валідації: агент шукав, до чого застосувати правило, замість спитати,
                чи є в задачі та ідея взагалі. Для River/Sea перевірити наново — там воно може бути живим"}

 {:finding :dual-check-has-no-target-rule
  :at "2026-09-07"
  :fact "dual-check-quality відсіює НЕКОНКРЕТНУ критику (:rejects «може не масштабуватись»),
         але не має правила про ЦІЛЬ критики — і агент видав чотири конкретні заперечення,
         усі спрямовані не на той об'єкт: на вхідний код замість власного артефакту"
  :verified-by "власник відхилив усі чотири на s1: «зараз працює по коду що вже написаний,
                а не по твоєму артефакту. В тебе підміна уваги»"
  :consequence "кандидат у ПРОПУЩЕНІ правила (питання :q4 доку «яких правил тут БРАКУЄ»):
                :contra зобов'язаний атакувати артефакт цього етапу; критика входу — не :contra.
                Другий висновок для :to-measure — структурне вбивство знову з боку власника"}

 {:finding :rng-unseeded
  :at "2026-09-07"
  :fact "UnityEngine.Random ніде не сідиться — відтворюваності конкретної карти немає й в оригіналі"
  :verified-by "перенесено з FLOW_READABLE_GENERATION, finding :rng-unseeded (2026-09-06)"
  :consequence "конкретна карта для того самого стану RNG не є критерієм збереження алгоритму;
                критерій — формули, ваги і причини зупинки"}]
```

## Decisions

```clojure
(def decisions
  [{:decision :flow-home :status :confirmed :at "2026-09-07"
    :value "новий Category A FLOW, не FLOW_READABLE_GENERATION"
    :verified-by "пряма відповідь власника" :reason "READABLE_GENERATION має чужу (Codex) історію і відстав на три ітерації"}
   {:decision :cascade-depth :status :confirmed :at "2026-09-07"
    :value "повний каскад s1→s2→s3 з dual-check на кожному етапі"
    :verified-by "пряма відповідь власника"
    :reason "каскад досі :hypothesis — вечір 09-06/07 був ітерацією з виправленнями, а не каскадом"}
   {:decision :mode :status :confirmed :at "2026-09-07"
    :value ":supervised — стоп після кожного етапу"
    :verified-by "пряма відповідь власника" :reason "автономний режим не має підстав, поки не виміряно, хто вбиває гіпотези"}
   {:decision :from-scratch :status :confirmed :at "2026-09-07"
    :value "алгоритм зберігається; код пишеться з нуля за розкладкою, а не рефакториться з оригіналу"
    :verified-by "пряма відповідь власника"
    :reason "рефакторинг успадковує декомпозицію оригіналу — саме те, що правила мають замінити"}
   {:decision :clean-slate :status :confirmed :at "2026-09-07"
    :value "видалена Codex-чернетка Lake (173 рядки) не читається з git-історії"
    :verified-by "пряма відповідь власника" :reason "інакше успадкуємо її рішення замість перевірити правила"}
   {:decision :inert-draft :status :confirmed :at "2026-09-07"
    :value "TerrainGeneratorInstaller не змінюється; перемикає власник після читання"
    :verified-by "пряма відповідь власника" :reason "так само, як з MountainGenerationSubSystemNative"}
   {:decision :run-owner-shape :status :confirmed :at "2026-09-07"
    :value "окремого типу-власника немає: стану прогону лишилось три скаляри (seed, цільова площа, mapRadius)"
    :verified-by "s2 :data-structures+ після смерті трьох колекцій"
    :reason "правило 1 виконується тим, що стану майже не лишилось; тип на три скаляри не окупається"}
   {:decision :map-is-a-hexagon-invariant :status :confirmed :at "2026-09-07"
    :value "приймаємо інваріант «карта = суцільний шестикутник радіуса WaveCount−1»:
            належність — предикат distance ≤ mapRadius, розмір — GetTotalHexCountForWaves(WaveCount)"
    :verified-by "пряме рішення власника після пред'явлення s2 :contra-1 і :contra-3"
    :reason "обидва заперечення стояли на одному неписаному інваріанті; власник зробив його писаним.
             Наслідок: mapDomain, mapCoords і seedDomain-фільтр зникають — п'ять колекцій стають двома"}
   {:decision :zero-allocation-definition :status :confirmed :at "2026-09-07"
    :value "zero-allocation = детермінований час життя (виділено на початку роботи, звільнено в кінці);
            порушення = значний обʼєм у кучі, що чекає GC. Канонічна копія — ECS_CONVENTIONS.md
            → Statelessness And Collections; /arch-check Rule 3 переписано під неї"
    :verified-by "виправлення власника 2026-09-07 плюс його вибір ECS_CONVENTIONS як домівки правила"
    :reason "агент застосовував правило як заборону ТИПУ і через це хибно дискваліфікував власний варіант"}
   {:decision :not-found-shape :status :confirmed :at "2026-09-07"
    :value "форми немає — «не знайдено» не існує в Lake ні для насінини, ні для межі"
    :verified-by "власник: «варіант коли гексу для початку озера немає - неможливий, навіть гекс 0 0
                  це середина мапи»; агент перевірив другий випадок — цикл росту стереже
                  frontier.Count() > 0, тож вибір на межі теж завжди має кандидата"
    :reason "обидва методи повертають координату без гілки відмови; якщо стан усе ж настане —
             fail loud (кидок), а не sentinel. Правило 4 тут не застосовне, бо немає ідеї «не знайдено»"}
   {:decision :run-owner-shape :status :confirmed :supersedes :run-owner-shape :at "2026-09-07"
    :value "власник прогону ПОТРІБЕН: struct LakeShaper тримає обидві множини плюс скаляри прогону,
            створюється в Update через using, не знає про ECS"
    :verified-by "перевірено до: три скаляри не варті типу. Порахував параметри — і метр це вбив:
                  без власника GrowLake бере (seed, target, mapRadius, lake, frontier) = 5 параметрів
                  проти цільових ≤3. З власником — один"
    :reason "мій s2-висновок «власник не потрібен» був передчасний; метр max-params його спростував
             раніше, ніж власник встиг це прочитати"}])
```

## Disproven (append-only)

```clojure
[{:hypothesis "суцільний запис нуля в ApplyLakeLevels затирає результат River"
  :refuted-by "River/Lake/Sea взаємно виключні за WaterType — одночасно не працюють"
  :at "2026-09-07"
  :details "finding :water-types-are-exclusive"}]
```

## Attempted (append-only)

```clojure
[]
```

# 3 · Plan

```clojure
{:status :active
 :completed #{:flow-created :s1-artifact :s1-owner-review :s2-artifact :s2-owner-review
              :s3-artifact :s3-owner-review :write-lake-rewrited :meters :validation-report}
 :current :owner-read-and-unity-check
 :remaining #{:propose-code-style-rules-edit}
 :resume-context "Каскад у режимі :supervised. s1 (спина + структури даних) написаний нижче;
                  його :contra власник відхилив цілком — критика цілила в оригінал, не в артефакт
                  (finding :dual-check-has-no-target-rule), тож :contra порожній свідомо.
                  Три :open питання s1 закриті. s2 написаний і прочитаний власником:
                  інваріант «карта = шестикутник» ПРИЙНЯТО (п'ять колекцій → дві), правило
                  zero-allocation переписано під визначення власника (ECS_CONVENTIONS + arch-check Rule 3).
                  Каскад пройдено повністю. :contra-3 розв'язано виходом 3 власника (class LakeShaper,
                  using var, Dispose у кінці Update); :contra-2 власник відпустив («пофіг») — метр
                  правила 6 «guards only» фіксується як НЕ виконаний свідомо.
                  LakeGenerationSubSystemRewrited.cs написаний: 206 рядків проти 370 оригіналу,
                  усі агентські метри зелені. Чернетка ІНЕРТНА — реєстрація не чіпалась, .meta не створювався
                  (правило проєкту: .meta ніколи не пише агент). Лишилось читання власника + Unity.
                  Оригінал Lake — 370 рядків, прочитаний; Codex-чернетка Lake видалена власником,
                  з історії не читається. Реєстрація не чіпається."}
```

```clojure
(-> (:step-1 "s1 — спина алгоритму і структури даних + dual-check; стоп на читання")
    (:step-2 "s2 — залежності, розширений алгоритм і структури + dual-check; стоп")
    (:step-3 "s3 — деталі, потоки, use-cases, крайові випадки, виходи, обґрунтування + dual-check; стоп")
    (:step-4 "переклад s3 у C#: LakeGenerationSubSystemRewrited.cs з нуля")
    (:step-5 "метри доку + roslyn get_diagnostics + arch-check")
    (:step-6 "звіт валідації: які правила вистояли, які опиралися, яких бракує")
    (:step-7 "власник: Unity-компіляція + читання; пропозиція правки CODE_STYLE_RULES_WIP окремим GO"))
```

## Каскад — s1 · База

```clojure
(def s1-lake
  {:stage :s1-base
   :covers #{:algorithm :data-structures}
   :at "2026-09-07"

   :what "Озеро — це ОДНА зв'язна пляма води біля центру карти, площею у частку від карти.
          Робимо так: вибираємо центр ближче до середини, далі нарощуємо пляму по одному гексу,
          щоразу беручи найкращого сусіда плями, поки не набрали площу."

   :spine (-> ReadMap ChooseSeed GrowLake WriteLevels)

   :steps
   [(:read-map    {:what "знімок карти з ECS" :out "усі координати гексів"})
    (:choose-seed {:what "центр озера: випадковий, але центральні координати важать більше"
                   :in "координати в межах seedRadius від нуля" :out "одна координата"})
    (:grow-lake   {:what "жадібний ріст: щоразу найкращий гекс на межі плями"
                   :score "сусіди-в-озері × 3 − відстань-від-центру × 2 + шум ±0.35"
                   :ends-with #{"набрано площу" "межа порожня"}})
    (:write-levels {:what "рівень −1 гексам озера, 0 УСІМ іншим" :note "два діла в одному кроці — див. dual-check"})]

   :data-structures
   {:owner-of-the-run "^:new один тип, що тримає стан прогону озера і НЕ знає про ECS
                       (правило 1); система дає йому знімок, забирає відповідь"
    :map-representation {:option "індексований масив гексів + сусіди за індексами"
                         :replaces "пара mapCoords(список) + mapDomain(множина) з оригіналу"
                         :confidence 65
                         :note "не рішення, а кандидат — закривається на s2"}
    :lake-membership "множина належності до озера (за координатою або індексом — залежить від вибору вище)"
    :frontier "межа плями: кандидати на наступний крок росту"
    :choice-type "^:new тип «найкращий кандидат або нічого» замість пари hasCandidate+bestScore (правило 4)"
    :allocator Allocator.Temp}                     ;; правило 3

   :invariants
   ["озеро зв'язне: кожен доданий гекс має сусіда в озері (наслідок росту з межі)"
    "озеро не виходить за межі карти: у межу потрапляють лише координати карти"
    "площа — best-effort: менша площа при вичерпаній межі не є помилкою"
    "seedRadius обмежує ЛИШЕ насінину; сама пляма може дорости до краю карти"
    "після Lake кожен гекс карти має HexLevelComponent — це вхідна умова Mountain"]

   :order-sensitive
   ["Lake(210) → Mountain(300): Mountain читає рівні після Lake; базовий нуль ставить саме Lake"
    "у самому Lake: спершу межа розширюється від насінини, лише потім починається вибір —
     інакше перший вибір відбувається на порожній межі"]

   :open
   [{:q "чи лишається суцільний нуль усій карті всередині Lake, чи це окремий крок з власною назвою"
     :status ? :note "поведінку зберігаємо в будь-якому разі; питання лише про те, чи видно її в коді"}
    {:q "координати чи індекси як ключ множин" :status ? :closes-at :s2}
    {:q "чи потрібен окремий тип для гексу-кандидата, чи вистачає координати" :status ? :closes-at :s2}]})
```

```clojure
(def s1-dual-check
  {:pro
   [(:1 "чотири кроки спини — це чотири речення опису озера; читач бачить алгоритм без коду")
    (:2 "власник прогону забирає з системи все, крім читання й запису світу: ECS лишається
         на двох краях, як у правилі 1 (ReadHexes / WriteRaisedLevels у Native)")
    (:3 "Allocator.Temp + один власник знімають ПРИЧИНУ трьох вкладених try з оригіналу,
         а не симптом: жоден крок більше нічого не повертає у власність")
    (:4 "розкладка вже назвала два невидимих у коді факти — суцільний нуль усій карті
         і залежність Mountain від нього; в оригіналі обидва живуть тільки в голові читача")]

   :contra []                                     ;; порожньо — і це заява, не пропуск
   :contra-note {:first-attempt "чотири заперечення, усі ВІДХИЛЕНІ власником 2026-09-07"
                 :why-rejected "жодне не цілило в артефакт — усі критикували оригінальний
                                LakeGenerationSubSystem, тобто вхідні дані розкладки"
                 :case-by-case
                 {:1 "«:write-levels робить два діла» — атакує назву ApplyLakeLevels в оригіналі;
                      мій крок прямо каже «0 УСІМ іншим», тобто вже не ховає нічого"
                  :2 "«індексований масив перенесений з Mountain» — дослівно повторює мій же
                      :open «координати чи індекси», тож це не критика, а дублікат питання"
                  :3 "«що повертає .None на порожній межі» — атакує рядок 320 оригіналу"
                  :4 "«s1 мовчить про targetHexCount» — виходи належать s3 за самим каскадом
                      (:s3-final :covers :exits); це межа етапу, а не дефект артефакту"}
                 :verdict "валідної критики s1 немає; порожній :contra прийнятніший за ввічливі застереження"}

   :stage-gate {:names-the-parts true
                :closes-a-? "жодного — алгоритм уже існував у коді, як і з Mountain"
                :concrete-enough-to-attack "так: чотири заперечення вище цілять у конкретні рядки"
                :verdict :real}})
```

## Каскад — s2 · Середина

```clojure
(def s2-lake
  {:stage :s2-middle
   :covers #{:dependencies :algorithm+ :data-structures+}
   :at "2026-09-07"
   :answers-from-owner {:1 "усе по 0 з самого початку — вода завжди перший крок"
                        :2 "координати краще за індекси, ЯКЩО індекс не виводиться напряму"
                        :3 "усі гекси default — розтікатися можна куди завгодно"}

   :dependencies
   {:from-base   {GenerationSubSystem #{Priority "Update(GameState)" Dispose}}
    :from-ecs    {EntityStorages #{"Singletons: TerrainGenerationConfigComponent, LakeConfigComponent"
                                   "World: архетип Hex"}
                  MapArchetypes.Hex #{HexIdPKComponent HexLevelComponent}}
    :from-math   {AxialMath #{Distance NeighborsPointyTop NeighborCount}
                  HexesUtil #{GetTotalHexCountForWaves}}     ;; IndexToAxialCoords — відхилено, див. :contra-2
    :from-config {LakeConfigComponent #{SizeFraction EdgeMarginTiles}
                  TerrainGenerationConfigComponent #{WaterType WaveCount}}
    :random      "UnityEngine.Random, не сідиться"
    :order       "SystemPriorities.SubSystems.Generation.Lake = 210"}

   :algorithm+
   [(:guard      {:in "Singletons" :how "дві наявності плюс WaterType = Lake" :out "дозвіл або тиша"
                  :skips-step "нема конфігів або вода не озеро"})
    (:target     {:in "SizeFraction, розмір карти" :how "round(частка × розмір), обрізка розміром карти"
                  :out "цільова площа" :aborts-generation "площа ≤ 0"})
    (:seed       {:in "mapRadius, EdgeMarginTiles"
                  :how "два проходи по гексах у межах seedRadius: сума ваг (seedRadius − distance + 1),
                        далі roll і хода до вичерпання"
                  :out "координата центру" :ends-with #{"roll спожито"}})
    (:grow       {:in "seed, цільова площа, mapRadius"
                  :how "множина озера плюс межа; щоразу argmax по межі
                        (сусіди-в-озері × 3 − відстань-від-seed × 2 + шум ±0.35),
                        обраний переходить з межі в озеро, його сусіди-на-карті доливаються в межу"
                  :out "множина координат озера" :ends-with #{"площа набрана" "межа порожня"}})
    (:write      {:in "множина озера" :how "знімок id гексів-членів озера, потім AddComponent(−1)"
                  :out "світ" :ends-with #{"усі члени записані"}})]

   :data-structures+
   {:survive {:lake     "NativeParallelHashSet<int2> — Contains потрібен і при рості, і при записі"
              :frontier "NativeParallelHashSet<int2> — межа плями"}
    :die     {mapDomain  "→ предикат distance(coord,0) ≤ mapRadius (finding :map-is-a-full-hexagon)"
              mapCoords  "→ GetTotalHexCountForWaves(WaveCount), замкнена форма"
              seedDomain "→ див. :open :seed-domain-shape"
              ClearLakeLevels "→ мертвий код (finding :clear-lake-levels-is-dead)"
              "суцільний нуль" "→ надлишковий (finding :zero-level-is-born-not-written)"}
    :net     "п'ять колекцій оригіналу → дві; жодна не передається у власність, отже жодного try/finally"
    :owner-of-the-run "решта стану — seed, цільова площа, mapRadius — це три скаляри;
                       окремий тип-власник для трьох скалярів не окупається (правило 7 за аналогією).
                       Правило 1 виконується інакше: система не має полів стану, бо стану майже немає"
    :allocator Allocator.Temp}

   :invariants
   ["карта = суцільний шестикутник радіуса WaveCount−1 навколо (0,0)"
    "кожен гекс народжується з HexLevelComponent.Level = 0 — Lake не ставить базу, лише −1"
    "озеро зв'язне; площа best-effort; seedRadius обмежує тільки насінину"]

   :order-sensitive
   ["межа наповнюється від насінини ДО першого argmax"
    "запис −1 — після завершення росту, одним проходом; всередині обходу entities структурна зміна заборонена"]

   :open
   [{:decision :seed-domain-shape
     :options [{:option "подвійний цикл по гексах у межах seedRadius, двічі (сума ваг, потім roll)"
                :confidence 70 :cost "той самий обхід записаний двічі"}
               {:option "індексний префікс [0, GetTotalHexCountForWaves(seedRadius+1)) + IndexToAxialCoords"
                :confidence 40 :cost "~120 КБ одноразового сміття в кучі при нульовій вигоді проти координат
                                      — finding :index-to-coords-allocates (оцінка виправлена з 15)"}
               {:option "замкнена форма суми ваг ((R+1) + Σ 6d(R−d+1)), обхід лише для roll"
                :confidence 55 :cost "формула замість циклу — арифметично тотожно, але менш очевидно, що це той самий алгоритм"}]
     :status ?}
    {:decision :not-found-shape
     :options [{:option "sentinel-тип LakeCandidate з .None/.IsFound (правило 4)" :confidence 50}
               {:option "Try-патерн bool + out (конвенція проєкту)" :confidence 50}]
     :note "finding :rule-4-collides-with-try-convention — це рішення власника, не агента"
     :status ?}]})
```

```clojure
(def s2-dual-check
  {:pro
   [(:1 "п'ять колекцій → дві, і жодна не передається у власність: try/finally зникають не через
         правило 3, а тому що ділити стало нічого")
    (:2 "дві з трьох смертей колекцій виведені з ФАКТУ про карту (суцільний шестикутник),
         а не зі стилю — цього не було видно, поки алгоритм не розклали")
    (:3 "правило 1 виконується без нового типу: стану прогону лишилось три скаляри")]

   :contra                                        ;; цього разу ціль — мій артефакт, не оригінал
   [(:1 {:attack "заміна mapDomain на предикат distance ≤ mapRadius спирається на інваріант,
                  якого в коді НЕ записано: що множина гексів дорівнює повному шестикутнику"
         :concrete "прибери один гекс із карти — предикат далі каже «на карті», озеро туди виросте,
                    крок :write його не знайде, і озеро мовчки вийде меншим за замовлену площу;
                    оригінал із mapDomain у цьому випадку правильний"
         :cost "мій артефакт міняє правильність на дві колекції — і робить це непомітно"
         :status :needs-owner-veto})

    (:2 {:attack "я дискваліфікував власний варіант індексного префікса ХИБНИМ правилом:
                  прочитав zero-allocation як заборону керованих типів, а воно про час життя пам'яті"
         :concrete "HexesUtil.cs:44 алокує int2[] на виклик — це реальне сміття, але одноразове,
                    порядку 120 КБ при створенні карти; це не «прямий конфлікт», як я написав"
         :cost "оцінка 15 стояла на неправильній підставі; правильна підстава дає ~40 —
                варіант програє не забороні, а тому, що платить за нульову вигоду"
         :status :corrected-by-owner
         :see finding/:zero-allocation-is-about-lifetime})

    (:3 {:attack "цільова площа тепер рахується із замкненої форми, а не з фактичної кількості гексів"
         :concrete "оригінал: mapCoords.Length (скільки entities реально є);
                    мій: GetTotalHexCountForWaves(WaveCount) (скільки МАЄ бути) —
                    розійдуться рівно в тому ж сценарії, що й contra-1"
         :cost "дві мої економії спираються на один і той самий неписаний інваріант; якщо він падає — падають обидві"
         :status :same-root-as-contra-1})]

   :stage-gate {:names-the-parts true
                :closes-a-? "так: три колекції отримали підставу зникнути, і закрито питання про суцільний нуль"
                :concrete-enough-to-attack true
                :verdict :real}})
```

## s3 · Розв'язка :contra-3 — три виходи власника

```clojure
{:received-at "2026-09-07"
 :raw-request "в тебе є як мінімум 3 виходи. 1 - це ref. 2 - це in-out параметр, коли метод і приймає
               структуру і повертає її вже змінену. 3 - це стан в класі вище, де в кінці алгоритму
               ти робиш йому dispose"
 :reading "власник називає простір розв'язків для пастки копії mutable struct — не вибирає за агента"

 :hard-constraint {:fact "using-змінна ReadOnly: `ref shaper` = CS1657, `shaper = …` = CS1656"
                   :provenance "встановлено у FLOW_READABLE_GENERATION 2026-09-06 проти MS docs,
                                під час рішення :data-direction-vocabulary"
                   :effect "виходи 1 і 2 ЗАБИРАЮТЬ `using var` — потрібен явний Dispose або try/finally"}

 :options
 [{:option :ref-param
   :shape "Grow(ref LakeShaper shaper, int2 seed)"
   :confidence 15
   :fails #{"метр :ref-params 0" "`using var` неможливий"}
   :note "саме ref і відкрив цю програму: «ref це теж костиль, бо тоді взагалі ніхуя не зрозуміло
          що за сатана відбувається в методі» — власник, 2026-09-06"}

  {:option :in-out
   :shape "shaper = Grow(shaper, seed)"
   :confidence 35
   :passes #{":ref-params 0"}
   :fails #{"`using var` неможливий — присвоєння в using-змінну CS1656"}
   :real-problem "захист напівсправжній: native-хендли всередині СПІЛЬНІ, тож ріст пройде й БЕЗ
                  присвоєння — розійдуться лише скалярні поля. Пастка не зникає, вона стає тихішою"}

  {:option :class-owned-by-caller
   :shape "using var shaper = new LakeShaper(mapRadius, targetArea); shaper.Grow(seed);"
   :confidence 80
   :passes #{":ref-params 0" ":finally 0 (using var)" "max-params ≤3" "mutable-system-fields 0"}
   :costs "одна керована алокація на прогон генерації; native-пам'ять звільняється детерміновано
           через Dispose, шкаралупа класу лишається GC — обсяг мізерний, разово"
   :why "посилальна семантика прибирає пастку копії ПРИЧИННО, а не дисципліною виклику;
         за новим визначенням zero-allocation (час життя, не тип) це прохідно"}

  {:option :no-owner-type
   :shape "обидві множини — using var локальні в Update, вниз як звичайні параметри"
   :confidence 20
   :fails #{"max-params ≤3 — Grow(seed, target, mapRadius, lake, frontier) = 5"}
   :note "інлайнити ріст в Update заборонено правилом 6"}]

 :recommendation :class-owned-by-caller
 :status :awaiting-owner}
```

## Каскад — s3 · Фінал

```clojure
(def s3-lake
  {:stage :s3-final
   :covers #{:algorithm-details :flows :use-cases :edge-cases :exits :rationale}
   :at "2026-09-07"

   :members
   {LakeGenerationSubSystemRewrited
    [(:fields "readonly EntityStorages _storages, readonly Archetype _hexSet — більше нічого")
     (:consts "LakeLevel −1, NeighbourWeight 3, DistanceWeight 2, NoiseAmplitude 0.35")
     (:Priority "SystemPriorities.SubSystems.Generation.Lake")
     (:Update "guard-и + похідні конфігу + три кроки")
     (:ChooseSeed "(int seedRadius) → int2")
     (:WriteLake "(in LakeShaper) → void")]

    LakeShaper                                   ;; вкладений struct — власник прогону, правило 1
    [(:ctor "(int mapRadius, int targetArea) — створює обидві множини на Allocator.Temp")
     (:Grow "(int2 seed) → void — увесь ріст")
     (:Contains "(int2 coord) → bool — читання результату для запису у світ")
     (:Count "int — розмір озера, для ємності знімка")
     (:Expand "(int2 hex) — доливає сусідів-на-карті в межу")
     (:BestOnFrontier "(int2 seed) → int2 — argmax по межі")
     (:Score "(int2 candidate, int2 seed) → float — формула")
     (:LakeNeighbors "(int2 hex) → int — скільки сусідів уже в озері")
     (:IsOnMap "(int2 hex) → bool — distance ≤ mapRadius")
     (:Dispose "звільняє обидві множини — один раз, у кінці Update")]}

   :flow (-> Guard DeriveOrder ChooseSeed Grow WriteLake)

   :steps
   [(:guard {:in "Singletons"
             :how "дві перевірки наявності, потім WaterType = Lake"
             :out "дозвіл"
             :ends-with #{"дозвіл" "тиша"}
             :skips-step "нема TerrainGenerationConfigComponent або LakeConfigComponent, або вода не озеро"})

    (:derive-order {:in "TerrainGenerationConfigComponent.WaveCount, LakeConfigComponent"
                    :how "mapRadius = WaveCount−1; mapSize = GetTotalHexCountForWaves(WaveCount);
                          targetArea = min(mapSize, round(SizeFraction × mapSize));
                          seedRadius = max(0, mapRadius − max(0, EdgeMarginTiles))"
                    :out "чотири скаляри прогону"
                    :ends-with #{"скаляри готові"}
                    :skips-step "targetArea ≤ 0 — озера цього прогону немає"})

    (:choose-seed {:in "seedRadius"
                   :how "вага гексу = seedRadius − distance + 1. Сума ваг рахується ПО КІЛЬЦЯХ
                         (кільце k має GetHexCountInWave(k+1) гексів однакової ваги) — R+1 ітерацій.
                         Далі один обхід шестикутника: roll спадає на вагу гексу, перший від'ємний виграє"
                   :out "координата насінини"
                   :ends-with #{"roll спожито"}
                   :never "порожній домен — навіть при seedRadius 0 лишається (0,0) (рішення власника);
                           якщо обхід усе ж завершився без вибору — кидок, не sentinel"})

    (:grow {:in "seed, targetArea, mapRadius"
            :how "озеро ← seed; межа ← сусіди-на-карті насінини. Далі поки площа не набрана і межа
                  не порожня: argmax по межі за (LakeNeighbors×3 − distance-від-seed×2 + шум ±0.35),
                  обраний геть з межі, в озеро, його сусіди-на-карті доливаються в межу"
            :out "множина координат озера всередині власника"
            :ends-with #{"площа набрана" "межа порожня"}
            :note "обидва виходи нормальні; менша площа не є помилкою"})

    (:write-lake {:in "власник прогону"
                  :how "прохід по entities архетипу Hex: id тих, чия координата в озері, — у знімок;
                        далі повторний прохід по знімку з AddComponent(−1)"
                  :out "світ"
                  :ends-with #{"усі члени записані"}
                  :throws "зниклий entity — TryGetEntityById = false → кидок (fail loud)"
                  :why-two-passes "AddComponent під час перебору того самого запиту — структурна зміна;
                                   Friflo кидає StructuralChangeException навіть на value-only upsert"})]

   :collections
   {:lake     "NativeParallelHashSet<int2>, ємність targetArea — у власнику"
    :frontier "NativeParallelHashSet<int2> — у власнику"
    :id-snapshot "NativeList<int> у WriteLake, using var — вимушений другим проходом"
    :count 3                                     ;; ВИПРАВЛЕННЯ до s2, де я написав «дві» — див. :contra-1
    :released "власник — один Dispose у кінці Update; знімок — using var"}

   :edge-cases
   [{:case "SizeFraction ≤ 0" :result "targetArea 0 → крок пропущено, світ не змінено"}
    {:case "SizeFraction ≥ 1" :result "targetArea = mapSize; ріст спиняє межа на краю карти"}
    {:case "EdgeMarginTiles ≥ mapRadius" :result "seedRadius 0 → насінина (0,0)"}
    {:case "EdgeMarginTiles < 0" :result "обрізається до 0, як в оригіналі"}
    {:case "WaveCount ≤ 1" :result "mapRadius 0, mapSize 1 — карта з одного гексу"}]

   :rationale
   {:why-owner "без нього GrowLake бере п'ять параметрів (seed, target, mapRadius, lake, frontier);
                з ним — один. Метр max-params ≤3 і є причиною, а не стиль"
    :why-no-enumerator "правило 7 забороняє примітив на два місця виклику; обхід шести сусідів
                        живе рівно у двох (Expand і LakeNeighbors) — лишається розгорнутим"
    :why-predicate-IsOnMap "точніше за distance(hex,(0,0)) ≤ mapRadius і коротше — правило 5 «за»"
    :why-ring-sum "сума ваг по кільцях прибирає ДРУГИЙ обхід шестикутника: розподіл тотожний,
                   бо в межах кільця всі ваги рівні"
    :why-throw-not-sentinel "стани «не знайдено» тут неможливі; проєктне правило — fail loud"}

   :behavior-deltas
   ["суцільний запис 0 не-озерним гексам зник: значення вже стоїть від народження гексу"
    "порядок обходу кандидатів при виборі насінини інший (шестикутник замість порядку entities);
     розподіл той самий, конкретна карта для того самого стану RNG не є критерієм"
    "зниклий entity кидає замість мовчазного пропуску"
    "ClearLakeLevels немає — мертвий код"]

   :open []})
```

```clojure
(def s3-dual-check
  {:pro
   [(:1 "кожен крок має названі виходи, і жоден не ховає причину зупинки: два нормальні кінці росту
         й один кидок стоять у розкладці, а не в голові читача")
    (:2 "власник прогону виведений МЕТРОМ, а не смаком: п'ять параметрів проти одного")
    (:3 "правила 4 і 7 у Lake не спрацювали — і це результат, а не пропуск: доку бракує рядка
         «спершу спитай, чи існує в задачі та ідея»")]

   :contra
   [(:1 {:attack "я двічі оголосив «п'ять колекцій → дві», і це неправда — їх три"
         :concrete "WriteLake зобов'язаний зробити знімок id: AddComponent під час перебору того
                    самого запиту кидає StructuralChangeException навіть на value-only upsert
                    (GenerationSystem.cs:65-72 документує це для HexTypeComponent)"
         :cost "цифра «дві» пішла у два звіти власнику до того, як я дописав крок запису;
                виправлено тут, але помилка була в бік красивішого числа"
         :status :corrected-in-s3})

    (:2 {:attack "Update не проходить власний метр правила 6 «складність точки входу = самі guard-и»"
         :concrete "після трьох guard-ів там чотири рядки арифметики (mapRadius, mapSize, targetArea,
                    seedRadius) плюс четвертий guard на targetArea ≤ 0 — це похідні, але не «кроки»"
         :alternatives "сховати їх у ctor власника означає дати власнику знати конфіг-компоненти,
                        тобто ECS — прямо проти правила 1"
         :cost "правило 6 і правило 1 тягнуть у різні боки; я лишаю арифметику в Update і фіксую,
                що метр «guards only» тут НЕ виконаний"
         :status :unresolved-tension})

    (:3 {:attack "LakeShaper — mutable struct; копія при передачі мовчки розійдеться з оригіналом"
         :concrete "WriteLake бере (in LakeShaper) — читання безпечне; але будь-хто, хто в
                    майбутньому візьме LakeShaper за значенням і викличе Grow, отримає ріст у копію,
                    тоді як native-хендли всередині лишаться спільними — напівзламаний стан"
         :cost "struct обрано заради нуля алокацій; ціна — пастка, яку компілятор не ловить"
         :status :needs-owner-veto
         :alternative "class: одна керована алокація на прогон генерації, звільнення за GC —
                       за новим визначенням zero-allocation це прохідно (обсяг мізерний, разово)"})]

   :stage-gate {:names-the-parts true
                :closes-a-? "так: члени, виходи, крайові випадки й число колекцій"
                :concrete-enough-to-attack true
                :verdict :real}})
```

## Acceptance

```clojure
[{:meter "ref-параметри" :target 0 :actual 0 :status :met}
 {:meter "try / finally" :target 0 :actual "0 / 0" :status :met}
 {:meter "рядки XML-doc" :target 0 :actual 0 :status :met}
 {:meter "поля класу системи поза ctor-залежностями" :target 0
  :actual "0 — лише readonly _hexSet (Archetype) і _storages (EntityStorages)" :status :met}
 {:meter "максимум / медіана параметрів" :target "≤3 / 1" :actual "2 / 1 (14 сигнатур)" :status :met}
 {:meter "сигнатури, що не влазять в рядок" :target "0%" :actual "0% — і жодного рядка >120 символів" :status :met}
 {:meter "System.Collections.Generic" :target 0 :actual 0 :status :met}
 {:meter "коментарі" :target "лише факт, якого в коді не видно" :actual "3: структурна зміна у WriteLake, кільце↔хвиля у TotalSeedWeight" :status :met}
 {:meter mcp__roslyn__get_diagnostics :target "0 Warning+ на новому файлі" :actual "0; get_document_outline резолвить усі 29 членів із типами, тож чистота не хибна" :status :met}
 {:meter arch-check :target "0 порушень" :actual "0 / 0 / 0 — Rule 3 оцінено за НОВИМ визначенням (час життя)" :status :met}
 {:meter "рядків" :target "не метр — довідково" :actual "206 проти 370 оригіналу" :status :info}
 {:meter "правило 6: складність точки входу = guards only" :target "guards only"
  :actual "НЕ виконано — 4 рядки арифметики похідних у Update; власник прийняв («пофіг»)" :status :not-met-accepted}
 {:meter "Unity-компіляція (власник)" :target "чисто" :actual ? :status :pending}
 {:meter "читання власника зверху вниз" :target "без стрибків за кожним кроком" :actual ? :status :pending}]
```

## Звіт валідації CODE_STYLE_RULES_WIP на Lake

```clojure
(def validation-lake
  {:at "2026-09-07"
   :class LakeGenerationSubSystemRewrited
   :note "Lake — теж алгоритм-генератор, тож питання доку «чи тримаються правила на НЕ-генераторі»
          лишається відкритим; закрито інше — чи дає каскад результат на класі, з якого правил не виводили"

   :held
   {:0 "розкладка виступила ключовим рішенням: три колекції з п'яти померли через ФАКТ про карту,
        а не через стиль — цього не було видно, поки алгоритм лежав у коді"
    :1 "власник прогону підтверджений МЕТРОМ: без нього Grow бере 5 параметрів проти ≤3"
    :3 "нуль try/finally — але не тому, що правило заборонило, а тому що зникла передача власності"
    :5 "IsOnMap точніше за distance(hex,(0,0)) ≤ mapRadius; TotalSeedWeight, BestOnFrontier — теж виграші"
    :6 "внутрішній рівень (Grow → argmax/Expand) тримається; точка входу — ні, див. :tensions"
    :8 "3 коментарі проти 40 рядків XML-doc оригіналу; кожен — факт, невидимий у коді"}

   :did-not-apply
   {:2 "паралельних колекцій у Lake не було"
    :4 "стану «не знайдено» в Lake не існує — ні для насінини, ні для межі (рішення власника);
        правило не має до чого причепитись"
    :7 "обхід шести сусідів живе рівно у ДВОХ місцях — правило само забороняє примітив на два виклики"}

   :tensions
   [{:between #{:rule-6 :rule-1}
     :case "чотири рядки похідних конфігу в Update"
     :why "прибрати їх можна лише в ctor власника прогону — а це дасть власнику знати конфіг-компоненти,
           тобто ECS, проти правила 1"
     :owner-verdict "пофіг — прийнято як усвідомлений компроміс"}]

   :missing-rules                       ;; відповідь на :q4 доку «яких правил тут БРАКУЄ»
   [{:candidate "dual-check зобов'язаний атакувати артефакт ЦЬОГО етапу, не вхідний код"
     :evidence "s1: чотири конкретні заперечення, усі відхилені власником як підміна уваги"
     :why-doc-misses-it "dual-check-quality відсіює НЕконкретну критику, але про ЦІЛЬ не каже нічого"}
    {:candidate "спершу спитай, чи існує в задачі ідея, до якої правило застосовне"
     :evidence "агент завів зіткнення правила 4 з Try-конвенцією там, де стану «не знайдено» немає"
     :why-doc-misses-it "правила сформульовані як «роби так», без входу «чи є тут це взагалі»"}
    {:candidate "метр може вбити рішення раніше за рев'ю — рахуй параметри ДО того, як захищати форму"
     :evidence "s2-висновок «власник прогону не потрібен» прожив рівно до підрахунку параметрів"}]

   :rules-changed-during-the-pass
   [{:rule :zero-allocation
     :was "агент читав як заборону керованих ТИПІВ (тільки Unity.Collections)"
     :now "детермінований час життя; порушення = значний обʼєм у кучі, що чекає GC"
     :landed-in #{ECS_CONVENTIONS.md "arch-check Rule 3" "пам'ять агента"}
     :cost-of-the-error "хибно дискваліфікований власний варіант (оцінка 15 замість ~40)"}]})
```
```
