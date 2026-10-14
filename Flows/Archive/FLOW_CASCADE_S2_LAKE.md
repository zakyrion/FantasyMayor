---
category: A
read: archive
status: implemented
tags: [flow, cascade, code-story, lake, generation]
related:
  - "[CODE_STORY_RULES_PROPOSAL](../../CODE_STORY_RULES_PROPOSAL.md)"
---

# FLOW_CASCADE_S2_LAKE

Другий прогін каскаду CODE_STORY_RULES — без s1, одразу s2, на живому озері.

# 1 · Request

## User request — 2026-09-09 (verbatim)

> Читай CODE_STORY_RULES_PROPOSAL документ, читай клас LakeGenerationSubSystem
> і давай проводити тест. На цей раз я хочу щоб ти проігнорував артефакт s1 і одразу почав з s2

Відповіді на п'ять питань гейту, 2026-09-09 (verbatim):

> 1 - :s2-only а далі подивимося
> 2 - давай розділений
> 3 - ніякого s1, зчитай базовий алгоритм з класу, і пиши одразу s2 так щоб він мав читабельний вид
> 4 - ні
> 5 - ок

## Agent restatement — confirmed by the user

```clojure
{:task :cascade-s2-lake
 :goal "написати артефакт s2 для генерації озера — без s1, читаючи базовий алгоритм з живого класу"
 :where #{CODE_STORY_RULES_PROPOSAL.md
          Assets/Domains/Map/Generation/Systems/LakeGenerationSubSystem.cs
          Flows/FLOW_CASCADE_S2_LAKE.md}
 :off-limits #{"переписування коду — цей прогін зупиняється на s2"
               "калібрувальний запис :lake-2026-09-09 у CODE_STORY_RULES_PROPOSAL.md"}
 :decided {:skip-s1 true
           :subject :split-owner            ;; система + власник прогону, який світу не знає
           :existing-code :number-source
           :s1-substitute "s2 несе реальний тип у кожному записі data замість :from-s1"
           :deliverable-kind :plan}
 :do "артефакт s2 у читабельному вигляді: методи, дані, хребет"
 :skip "апрув-крок 7 каскаду — код цей прогін не пише"
 :accept [{:meter data-coverage :target "undeclared 0, orphan 0, one-step-fields 0, unborn 0"}
          {:meter reader-clean :target true}
          {:meter how-semicolons :target 0}
          {:meter "вердикт власника на гейті :after-s2" :target "структура коду прийнята"}]
 :result "s2 у цьому FLOW, обговорений за гейтом :after-s2"}
```

## Amendments (append-only)

```clojure
[]
```

# 2 · Contract

## Findings

```clojure
[{:finding :live-algorithm-is-not-the-documented-one
  :at "2026-09-09"
  :fact "живе озеро — одне зерно + зважений фронтир: 9 методів, 4 константи ваг, жодних середин, берега, острівців чи зшивання. Алгоритм, який цитують усі десять :case у розділі 5 CODE_STORY_RULES (LakeRun.cs, PlaceMiddles, StitchIslands, CarveCorridor), у git не існує жодного разу — він жив тільки в робочому дереві прогону 2026-09-08 і був відкинутий"
  :verified-by "прочитав LakeGenerationSubSystem.cs цілком; git log --diff-filter=D по *LakeRun* порожній"
  :consequence "§8 :rule-changes-when цей прогін не задовольняє — предмет інший, тож правила розділу 5 лишаються гіпотезами і після нього"}

 {:finding :world-boundary-is-three-points
  :at "2026-09-09"
  :fact "клас торкається світу рівно тричі: Update читає два singleton-конфіги; BuildMapCoords знімає координати з _hexSet.Entities; ApplyLakeLevels пише HexLevelComponent назад. Усе між ними — чиста математика на int2"
  :verified-by "прочитав кожен метод класу"
  :consequence "розділ :split-owner проходить точно по цих трьох — власник прогону отримує знімок і замовлення, віддає набір клітин"}

 {:finding :map-radius-comes-from-config-not-snapshot
  :at "2026-09-09"
  :fact "mapRadius = terrainConfig.WaveCount - 1 — число з конфіга, не зі знімка координат; seedRadius = mapRadius - EdgeMarginTiles"
  :verified-by "Generate, рядки 231-232; TerrainGenerationConfigComponent.WaveCount"
  :consequence "власник прогону сам радіус не виведе — він мусить приїхати в замовленні, інакше це буде інше число, ніж рахує живий код"}

 {:finding :clear-lake-levels-is-dead
  :at "2026-09-09"
  :fact "ClearLakeLevels() — 23 рядки, жодного виклику в усьому Assets/. Він не потрібен, бо ApplyLakeLevels і так пише рівень КОЖНОМУ гексу: lakeCoords.Contains(coord) ? LakeLevel : 0"
  :verified-by "grep -rn ClearLakeLevels Assets/ — одна лінія, сама декларація"
  :consequence "у s2 його немає; це не втрата, а мертвий код, який переклад не переносить"}

 {:finding :seed-fallback-is-unreachable
  :at "2026-09-09"
  :fact "SelectSeed завершується return seedDomain[^.Length - 1]. Кожна вага = maxDistance - distance + 1, а seedDomain відфільтрований умовою distance <= seedRadius = maxDistance, отже кожна вага >= 1 і сума вагів = totalWeight; Random.Range(0, totalWeight) на int-перевантаженні дає roll <= totalWeight - 1, тому цикл завжди повертає"
  :verified-by "прочитав BuildSeedDomain і SelectSeed разом; арифметика вагів"
  :consequence "правило 7 — гілка, доведена неможливою, у коді не існує; s2 її не має"}

 {:finding :neighbour-loop-written-twice
  :at "2026-09-09"
  :fact "цикл по шести сусідах написаний двічі — CountLakeNeighbors і ExpandFrontier, обидва через AxialMath.NeighborsPointyTop[direction]. AxialMath.GetNeighbor(position, index) існує і в класі не вжитий жодного разу"
  :verified-by "grep по AxialMath.cs: GetNeighbor на рядку 160; два цикли в озері"
  :consequence "двічі — під планкою правила 6 (>=3) у межах класу; примітив не народжується, але виклики переходять на GetNeighbor"}

 {:finding :sea-is-a-near-twin
  :at "2026-09-09"
  :fact "SeaGenerationSubSystem має ті самі методи під іншими іменами: BuildMapCoords, ExpandFrontier, CountSeaNeighbors, GrowSea, SelectNextSeaHex, ApplySeaLevels, і ті самі три ваги-константи. Mountain — той самий кістяк, розгорнутий ширше (23 методи)"
  :verified-by "перелік членів обох класів"
  :consequence "власник прогону, спроектований тут, є шаблоном для трьох систем; поза scope цього прогону, але вирішує, наскільки лейк-локальними бути іменам у s2"}]
```

## Decisions

```clojure
(def decisions
  [{:decision :stop-at-s2 :status :confirmed :at "2026-09-09" :value :s2-only
    :verified-by "user's answer 1 at the entry gate"
    :reason "«а далі подивимося» — ескалація в :mutation окремим рішенням"}
   {:decision :split-owner :status :confirmed :at "2026-09-09" :value :split-owner
    :verified-by "user's answer 2 at the entry gate"
    :reason "правила 5 і 9 написані під власника прогону, який світу не знає; на одному класі вони не перевіряються"}
   {:decision :no-s1 :status :confirmed :at "2026-09-09"
    :value "s2 несе реальний тип у кожному записі data; базовий алгоритм зчитано з живого класу"
    :verified-by "user's answer 3 at the entry gate"
    :reason "без s1 :from-s1 не має дому — тип переїжджає в s2"}
   {:decision :no-calibration-edit :status :confirmed :at "2026-09-09" :value false
    :verified-by "user's answer 4 at the entry gate"
    :reason "CODE_STORY_RULES_PROPOSAL.md цей прогін не редагує"}
   {:decision :order-carries-radius :status :confirmed :at "2026-09-09"
    :value "замовлення несе частку карти, відступ від краю і радіус карти; власник прогону рахує з них замовлену площу і радіус зерна"
    :verified-by "user's answer 1 at the findings gate"
    :reason "радіус карти береться з конфігу, і власник прогону сам його не виведе"}
   {:decision :run-returns-cells :status :confirmed :at "2026-09-09"
    :value "власник прогону віддає набір клітин озера; пара гекс-рівень стає народженим записом на боці системи"
    :verified-by "user's answer 2 at the findings gate"
    :reason "інакше власник прогону знає про сутності і межа світу ламається"}
   {:decision :map-snapshot-keeps-both :status :confirmed :at "2026-09-09"
    :value "знімок карти несе і список для перебору, і хеш-сет для належності"
    :verified-by "user's answer 3 at the findings gate"
    :reason "два різні доступи, а не пара за індексом; порядок перебору потрібен жеребу зерна"}
   {:decision :seed-name-kept :status :confirmed :at "2026-09-09" :value seed
    :verified-by "user's answer 4 at the findings gate"
    :reason "тут зерно і є точкою, з якої озеро росте; вердикт проти seed стосувався алгоритму з серединами"}])
```

## Disproven (append-only)

```clojure
[{:hypothesis "ApplyLakeLevels затирає роботу попередніх кроків генерації, бо пише рівень 0 кожному не-озерному гексу, а River (пріоритет 200) біжить раніше за Lake (210)"
  :refuted-by "усі три водні системи гейтяться на взаємовиключному terrainConfig.WaterType — River на WaterType.River, Lake на .Lake, Sea на .Sea; дві ніколи не біжать в одному прогоні"
  :at "2026-09-09"
  :details "SystemPriorities.SubSystems.Generation: River 200, Lake 210, Sea 220, Mountain 300; guard-и Update у трьох класах"}]
```

## Attempted (append-only)

```clojure
[]
```

# 3 · Plan

```clojure
{:status :complete   ;; closed 2026-09-13 by the owner's word — «цей теж можеш закрити»
 :completed #{:research-pass-1 :findings-gate :write-s2 :owner-close}
 :current :closed
 :remaining #{}
 :resume-context "CLOSED 2026-09-13: s2 для LakeGenerationSubSystem написаний (розділ 4) і лишається історією прогону; гейт :after-s2 не пройдений як «структура прийнята» — вердикт власника 2026-09-09 записаний у пам'яті (feedback_code_as_story_not_decisions), переписування озера скасоване. Код за цим s2 не писався."}
```

```clojure
;; harvested 2026-09-13 (Rule 2d): висновки про каскад → CODE_STORY_RULES_PROPOSAL.md і пам'ять (feedback_code_as_story_not_decisions, project_code_style_rules_program); нового для перенесення немає.
;; План прогону (research, findings-гейт, s2, гейт :after-s2) скинутий; record = commit log.
```

## Acceptance

```clojure
[{:meter data-coverage :target "undeclared 0, orphan 0, one-step-fields 0, unborn 0" :actual "усі чотири 0 — розділ 4.3" :status :met}
 {:meter reader-clean :target true :actual "дужки і мапи збалансовані в кожному clojure-fence; ^ стоїть лише на символах типів" :status :met}
 {:meter how-semicolons :target 0 :actual "жоден :how не несе крапки з комою" :status :met}
 {:meter "вердикт власника на гейті :after-s2" :target "структура коду прийнята" :actual "не прийнята — вердикт 2026-09-09 (C# не читається), переписування скасоване; FLOW закритий наказом власника 2026-09-13" :status :superseded}]
```

# 4 · s2 — псевдокод майбутніх класів

Прогін без s1: кожен запис `data` несе `:type` — реальний тип призначення — замість `:from-s1`.
Базовий алгоритм зчитано з живого `LakeGenerationSubSystem`.

Розділення: система знає світ і не знає алгоритму; `LakeRun` знає алгоритм і не знає світу.
Замовлення і знятa карта приїжджають у нього позикою, назад їде набір клітин.

## 4.1 · LakeGenerationSubSystem — та, що знає світ

```clojure
(def LakeGenerationSubSystem-methods
  {Update
   {:does  "провести прогін озера, коли карта замовила саме озеро"
    :in    #{:сховища :набір-гексів :замовлення}
    :out   :none
    :flow  (-> (:step-1 "зняти карту зі світу" {:calls SnapshotMap :out :карта})
               (:step-2 "виростити озеро" {:calls LakeRun.Build :out :озеро})
               (:step-3 "записати рівень кожному гексу" {:calls WriteLakeLevels :out :none}))
    :exits #{"немає конфігу місцевості або конфігу озера — генерація ще не налаштована"
             "тип води не озеро — цю карту робить інша система; River 200, Lake 210, Sea 220 гейтяться на взаємовиключному WaterType (Findings :disproven)"}
    :note  "замовлення складається одним рядком-значенням із двох конфігів, уже прочитаних guard-ами: це не крок хребта, а рядок часу життя (правило 5). Рядок new LakeRun(order, map) — теж"
    :ends-with #{"кожен гекс має HexLevelComponent: -1 у клітинах озера, 0 у решті"}}

   SnapshotMap
   {:does "зняти координати всіх гексів карти у дві форми доступу"
    :in   #{:набір-гексів}
    :out  :карта
    :how  "один прохід по гексах архетипу: координата лягає і в список для перебору, і в хеш-сет для перевірки належності"
    :note "дві форми того самого вмісту — це два різні доступи, не пара за індексом; правило 8 сюди не дістає (:contra c-2)"
    :ends-with #{"знімок, який власник прогону читає і не змінює"}}

   WriteLakeLevels
   {:does    "записати рівень кожному гексу карти"
    :in      #{:набір-гексів :сховища :озеро}
    :out     :none
    :scratch #{:рівні-до-запису}
    :flow    (-> (:step-1 "зібрати пари гекс-рівень одним проходом по архетипу")
                 (:step-2 "перебрати зібране, дістати гекс за ідентифікатором і записати рівень"))
    :note    "два проходи, а не один: AddComponent усередині перебору Entities — структурна зміна, Friflo кидає StructuralChangeException. Це «чому», без якого код бреше (правило 10)"
    :ends-with #{"жодного гекса без HexLevelComponent"}}})
```

```clojure
(def LakeGenerationSubSystem-data
  {:сховища        {:type EntityStorages :as _storages :lives :ззовні
                    :holds "іменовані ECS-сховища — World і Singletons"
                    :note "дає і тримає DI; readonly-залежність системи"}
   :набір-гексів   {:type Archetype :as _hexSet :lives :ззовні
                    :holds "архетип усіх гексів карти"
                    :note "MapArchetypes.Hex(World), знятий у конструкторі; readonly-залежність системи"}

   :замовлення     {:type ^:new LakeOrder :as order :lives Update
                    :fields {SizeFraction :частка-карти-під-воду
                             EdgeMarginTiles :відступ-від-краю
                             MapRadius :радіус-карти}
                    :holds "усе, що власник прогону знає про побажання конфігу; ECS цей запис не знає"}
   :карта          {:type ^:new MapSnapshot :as map :lives Update
                    :fields {Cells :клітини-по-порядку
                             CellSet :клітини-для-належності}
                    :holds "координати всіх гексів у двох формах; звільняється однією Dispose"}
   :озеро          {:type NativeParallelHashSet<int2> :as lake :lives Update
                    :holds "клітини, які стали водою"
                    :note "народив його власник прогону; звільняє той, хто отримав — using var у Update"}
   :рівні-до-запису {:type NativeList<HexLevelWrite> :as levelWrites :lives WriteLakeLevels
                     :holds "пари гекс-рівень, зняті до першого запису"}
   :запис-рівня    {:type ^:new HexLevelWrite :lives WriteLakeLevels
                    :fields {EntityId :ідентифікатор-гекса
                             Level :рівень-гекса}
                    :holds "народжений тип замість int2, у якому .x був ідентифікатором, а .y рівнем (правило 8)"}

   :клітини-по-порядку     {:type NativeList<int2> :as Cells :lives :карта
                            :holds "усі координати карти в порядку перебору архетипу"}
   :клітини-для-належності {:type NativeParallelHashSet<int2> :as CellSet :lives :карта
                            :holds "ті самі координати для перевірки «чи ця клітина є на карті»"}
   :частка-карти-під-воду  {:type float :holds "яку частку карти замовлено під озеро, 0-1"}
   :відступ-від-краю       {:type int :holds "скільки клітин від краю карти зерно ставити не можна"}
   :радіус-карти           {:type int :holds "WaveCount - 1; число з конфігу, не зі знімка (Findings :map-radius-comes-from-config-not-snapshot)"}
   :ідентифікатор-гекса    {:type int :holds "Entity.Id гекса"}
   :рівень-гекса           {:type int :holds "-1 для води, 0 для суходолу"}})
```

Тіло точки входу виводиться з хребта рядок у рядок:

```csharp
public override void Update(GameState state)
{
    if (!_storages.Singletons.Has<TerrainGenerationConfigComponent>() ||
        !_storages.Singletons.Has<LakeConfigComponent>())
        return;

    var terrain = _storages.Singletons.Get<TerrainGenerationConfigComponent>();
    if (terrain.WaterType != WaterType.Lake)
        return;

    var lakeConfig = _storages.Singletons.Get<LakeConfigComponent>();
    var order = new LakeOrder(lakeConfig.SizeFraction, lakeConfig.EdgeMarginTiles, terrain.WaveCount - 1);

    using var map = SnapshotMap();
    var run = new LakeRun(order, map);

    using var lake = run.Build();
    WriteLakeLevels(lake);
}
```

## 4.2 · LakeRun — той, що знає алгоритм

```clojure
(def LakeRun-methods
  {Build
   {:does  "виростити зв'язне озеро на знятій карті за замовленням"
    :in    #{:замовлення :карта}
    :out   :озеро
    :flow  (-> (:step-1 "порахувати, що замовлення означає на цій карті" {:calls ComputeRunNumbers :out :числа-прогону})
               (:step-2 "кинути жереб на клітину, з якої озеро росте" {:calls SelectSeed :out :зерно})
               (:step-3 "розростити озеро до замовленої площі" {:calls Grow :out :озеро}))
    :exits #{"замовлена площа = 0 — конфіг дозволяє частку 0, і тоді озера немає (:contra c-1)"}
    :ends-with #{"набір клітин озера; звільняє його той, хто отримав"}}

   ComputeRunNumbers
   {:does    "порахувати, що замовлення означає на цій карті"
    :in      #{:замовлення :карта}
    :out     :числа-прогону
    :how     "площа — частка від числа клітин, обрізана зверху самим числом клітин; радіус зерна — радіус карти без відступу від краю"
    :numbers {OrderedWaterCells "min(RoundToInt(частка × скільки клітин на карті), скільки клітин на карті)"
              SeedReachRadius   "max(0, радіус карти - max(0, відступ від краю))"}
    :ends-with #{"два числа під іменами задачі, а не три поля, в які щось призначено"}}

   SelectSeed
   {:does    "кинути жереб на клітину, з якої озеро росте — ближче до центру важче"
    :in      #{:карта :числа-прогону}
    :out     :зерно
    :scratch #{:домен-зерна}
    :flow    (-> (:step-1 "відібрати клітини не далі радіуса зерна від центру")
                 (:step-2 "скласти ваги всіх відібраних")
                 (:step-3 "кинути жереб і йти доменом, віднімаючи вагу, доки не піде за нуль"))
    :numbers {CellWeight "радіус зерна - Distance(клітина, центр) + 1"}
    :note    "тихого fallback наприкінці немає: кожна вага >= 1, сума вагів = загальна вага, а Random.Range(0, загальна вага) на int-перевантаженні дає жереб <= загальна вага - 1, тому цикл завжди повертає (Findings :seed-fallback-is-unreachable, правило 7)"
    :ends-with #{"одна клітина карти в межах радіуса зерна"}}

   Grow
   {:does    "розростити озеро від зерна до замовленої площі"
    :in      #{:карта :зерно :числа-прогону}
    :out     :озеро
    :scratch #{:фронтир}
    :flow    (-> (:step-1 "покласти зерно в озеро і відкрити його сусідів")
                 (:step-2 (cond
                            (:flow-1 "поки озеро менше замовленої площі і фронтир не порожній")
                            (:conclusion-1 "обрати найкращу клітину фронтиру, забрати її звідти, додати в озеро, відкрити її сусідів — і знову сюди")
                            (:flow-2 "інакше")
                            (:conclusion-2 "віддати озеро"))))
    :calls   #{ExpandFrontier SelectNextLakeCell}
    :note    "клітина, вже додана в озеро, пропускається без роботи — вона могла потрапити у фронтир з двох боків одночасно; це привід continue (правило 7)"
    :ends-with #{"зв'язний набір клітин; фронтир звільнений, озеро віддане далі"}}

   ExpandFrontier
   {:does   "відкрити сусідів клітини для росту"
    :in     #{:карта :озеро :фронтир}
    :writes #{:фронтир}
    :out    :none
    :how    "шість сусідів через наявний AxialMath.GetNeighbor; сусід поза картою або вже в озері пропускається"
    :ends-with #{"фронтир містить кожного придатного сусіда цієї клітини"}}

   SelectNextLakeCell
   {:does    "обрати наступну клітину озера — найбільше сусідів-озера, найближче до зерна, плюс дрож"
    :in      #{:фронтир :озеро :зерно}
    :out     :наступна-клітина
    :how     "перебрати фронтир, порахувати оцінку кожної клітини, лишити найкращу; підрахунок сусідів-озера — фаза всередині абзацу, не окремий метод"
    :numbers {NeighbourWeight "3 — скільки важить кожен сусід-озеро"
              DistanceWeight  "2 — скільки важить відстань від зерна"
              ScoreJitter     "0.35 — межа випадкового дрожу оцінки"
              CellScore       "сусіди-озера × NeighbourWeight - Distance(клітина, зерно) × DistanceWeight + Random.Range(-ScoreJitter, ScoreJitter)"}
    :note    "прапорця «чи є кандидат» немає: Grow заходить сюди лише поки фронтир не порожній, а оцінка — скінченна сума, тому перша ж клітина б'є стартовий float.MinValue"
    :ends-with #{"одна клітина фронтиру"}}})
```

```clojure
(def LakeRun-data
  {:замовлення   {:type LakeOrder :as _order :lives :ззовні
                  :holds "побажання конфігу; дала система, вона ж і володіє"}
   :карта        {:type MapSnapshot :as _map :lives :ззовні
                  :holds "знімок координат у двох формах"
                  :note "зняла система, звільняє система — власник прогону його не чіпає"}

   :числа-прогону {:type ^:new LakeRunNumbers :as numbers :lives Build
                   :fields {OrderedWaterCells :замовлена-площа
                            SeedReachRadius :радіус-зерна}
                   :holds "що замовлення означає на цій карті; запис народжений тут, щоб два числа їхали під іменами задачі"}
   :зерно        {:type int2 :as seed :lives Build
                  :holds "клітина, з якої озеро росте, і якір компактності"}
   :озеро        {:type NativeParallelHashSet<int2> :as lake :lives Build
                  :holds "клітини, які стали водою"
                  :note "народжується в Grow, Build віддає його далі і не звільняє"}

   :домен-зерна  {:type NativeList<int2> :as seedDomain :lives SelectSeed
                  :holds "клітини, яким дозволено стати зерном — scratch жеребу, поле для нього було б помилкою"}
   :фронтир      {:type NativeParallelHashSet<int2> :as frontier :lives Grow
                  :holds "клітини карти, суміжні з озером і ще не в ньому"
                  :grows "росте на кожній доданій клітині, зменшується на кожній забраній"}
   :наступна-клітина {:type int2 :as nextCell :lives Grow
                      :holds "клітина фронтиру з найкращою оцінкою"}

   :замовлена-площа {:type int :holds "скільки клітин замовлено під воду"}
   :радіус-зерна    {:type int :holds "як далеко від центру дозволено ставити зерно"}})
```

Тіло точки входу власника прогону:

```csharp
public NativeParallelHashSet<int2> Build()
{
    var numbers = ComputeRunNumbers();
    var seed = SelectSeed(numbers.SeedReachRadius);

    return Grow(seed, numbers.OrderedWaterCells);
}
```

## 4.3 · Зведення

```clojure
(def s2-coverage
  {:undeclared 0
   :orphan 0
   :one-step-fields 0            ;; жоден іменник прогону не став полем: озеро, зерно і числа живуть у Build
   :unborn 0                     ;; int2 (.x = id, .y = рівень) став HexLevelWrite; два числа прогону стали LakeRunNumbers
   :how-semicolons 0
   :methods {:live 11 :s2 9}     ;; Generate розчинився в Update; ClearLakeLevels мертвий і не переїхав; CountLakeNeighbors — фаза
   :fields  {:live 6 :s2 4}      ;; жодного мутабельного поля: 2 readonly у системи, 2 позики у власника прогону
   :finally {:live 5 :s2 0}      ;; using var замість чотирирівневого гнізда try/finally
   :born-types #{LakeOrder MapSnapshot HexLevelWrite LakeRunNumbers}})
```

## :contra — перед переходом

```clojure
[{:id :c-1
  :kills "вихід із Build за замовленою площею 0"
  :case "конфіг із часткою карти під воду = 0"
  :fails "живий код повертається з Generate ДО того, як щось записано — жоден гекс рівня не отримує. У цьому s2 порожнє озеро доїжджає до WriteLakeLevels, і той запише 0 кожному гексу. Поведінка інша, і число народжується в кроці 1, а не на вході — тобто це не :exits за формою"
  :fix [{:id :fix-empty-lake-writes-zero :confidence 55
         :is "лишити як є: порожнє озеро означає «води немає», і 0 усюди — це саме воно"
         :cost "змінює поведінку відносно живого коду; чи є до озера хтось, хто вже поклав рівень, я не перевіряв"}
        {:id :fix-guard-in-entry :confidence 30
         :is "система рахує замовлену площу сама і виходить до прогону"
         :cost "арифметика задачі тікає з власника прогону в систему — проти рішення :order-carries-radius"}
        {:id :fix-order-validates-itself :confidence 15
         :is "LakeOrder відмовляється народжуватись із часткою 0, і guard стоїть на його створенні в Update"
         :cost "запис починає щось вирішувати"}]}

 {:id :c-2
  :kills "імена двох полів MapSnapshot"
  :case "map.Cells проти map.CellSet"
  :fails "обидва тримають той самий вміст, і різниця між ними — форма доступу, а не слово задачі. Правило 3 вимагає, щоб імʼя казало ЩО, а не як; тут воно каже саме як"
  :fix [{:id :fix-keep-access-names :confidence 60
         :is "лишити Cells і CellSet — форма доступу тут і є єдина чесна різниця"
         :cost "правило 3 має виняток, якого документ не називає"}
        {:id :fix-name-by-use :confidence 40
         :is "назвати за вжитком: SeedCandidates і MapMembership"
         :cost "SeedCandidates бреше — список несе всі клітини, а не тільки придатні під зерно"}]}]
```
