---
category: C
read: trigger
trigger: "before game-design work on the core loop, economy, actors, elites, population needs, slots, buildings, the exchange or politics"
tags: [game-design, vision, economy, actors, politics]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[GLOSSARY](GLOSSARY.md)"
---

# GAME_VISION

FantasyMayor is a mayoral character sandbox: the city owns the land, elites build on it by permit, goods drive lives.

> **Scope.** This file is the single source of truth for the game's durable premise and economic model. It says
> what the game does, not how code implements it (`ARCHITECTURE.md` / `ECS_CONVENTIONS.md`). It absorbs and
> replaces `ECONOMY_MODEL.md`.
>
> **Where the UI lives.** Not here, and not in one document. `UI_LANGUAGE.md` owns display STYLE only; a
> mechanic that needs an interface gets its own mechanic document, its own UI document and its own mockup.
> §9 states only the model-side facts those UI documents must honour.
>
> **Status.** Rules are confirmed by the owner unless explicitly marked `OPEN`; agent proposals live only in §10. Family, estate
> and motivation details remain derivative mechanics; this document fixes only the foundation they must preserve.
>
> **Revision 3 — 2026-09-04.** The owner reversed the ownership model: the city owns every hex, elites hold
> land and buildings only by DEAL (a lease or a permit with a share and a term), and every other transfer type
> of revision 2 (elite ownership, tax in kind, buyback, the reserve decree) is retired. The superseded rules
> are preserved with their reasoning in `Flows/FLOW_BUILD_UX_DECONGESTION.md`.
>
> **Revision 4 — 2026-09-05.** Unification pass: the model now answers the questions the two main-screen
> specifications had answered differently. Tier = labour category; every action carries its own precondition;
> construction = hex development; buildings belong to elites; «наказ» is a political action; construction goods
> are a model category; population grows or declines with its needs. Agent-proposed framings were folded into
> §10 as open items. The owner's verbatim answers and the reasoning are in `Flows/FLOW_VISION_UNIFICATION.md`.
>
> **Revision 5 — 2026-09-05.** The economic loop closed. A specialized hex has building slots; elites build in
> them by permit; a building's passive yield goes to the city ONLY and needs no labour; elites act on their own
> buildings with their own AP and pooled labour, keeping the output; goods move between actors only through an
> exchange of standing offers («2 wheat for 1 beer») that any actor may post for 1 AP and any actor may fill
> for free on their turn. Leases, shares and dues are retired; elites arrive by an autonomous mechanic and
> develop through capabilities. `Flows/FLOW_VISION_UNIFICATION.md` holds the owner's verbatim words.

---

## 1 · Game thesis

The player is the mayor of a living fantasy city. The city-builder is the setting and control layer; the emotional
subject is a small cast of autonomous actors whose work, buildings, households and ambitions generate observable
histories. The intended feeling is a character terrarium or tamagotchi at city scale, not an optimisation board with
portraits attached.

Life is simulated through consequential mechanics rather than continuous physical routines. An actor's production,
consumption, buildings, offers and conflicts stand in for walking to places and performing everyday animations.
The player works and develops the city's hexes directly, grants building slots, posts offers on the exchange and
changes incentives, then watches what actors choose.

```clojure
(def game-centre  ;; 2026-09-02 — confirmed; :player-role уточнено 2026-09-04
  {:player-role "мер: розпорядник усієї міської землі — спочатку сам працює на ній, згодом роздає слоти й править ринком"
   :world-role  "фентезійне місто як сцена та причинна система для історій"
   :elite-role  "небагато автономних персоналій із власними ресурсами, AP і життєвим розвитком; будівлі ставлять у слотах міської землі за дозволом, землі не тримають"
   :story-source #{виробництво споживання будівлі біржа дефіцит амбіції сім'я}
   :simulation-boundary "значущі рішення й наслідки, не безперервний побут або ручне пересування персонажів"
   :mayor-goal "забезпечити місто всім необхідним і розвивати його: балансувати міський ринок і домовлятися з елітами; еліти в цьому або допомагають, або заважають"   ;; 2026-09-05
   :success-shape "місто розвивається разом із біографіями його мешканців; єдина переможна шкала не визначена"})
```

```clojure
(def causal-story-loop  ;; 2026-09-02 — confirmed foundation
  {:actor "персоналія з власною історією та пріоритетами"
   :wants "товари, будівлі, безпеку, статус, сімейний або особистий розвиток"
   :chooses "дію в межах власних AP, ресурсів, будівель і спроможностей"
   :changes "матеріальний стан міста та власне подальше становище"
   :produces "нові потреби, можливості, конфлікти й історії"
   :exact-choice-policy :OPEN})
```

---

## 2 · Actors and scarcities

```clojure
(def actors  ;; 2026-09-05 — revised for revision 5
  {Місто {:is :субстрат
          :owns "усі гекси і все, що місто виробило або отримало як пасив будівель"
          :generates робоча-сила
          :absorbs товари
          :also :політичний-суб'єкт}
   Мер   {:is :гравець
          :controls "спеціалізацію й дії на міських гексах; дозволи на слоти; власні пропозиції на біржі; політики"
          :output "усе, що зроблено його AP, належить місту"}
   Еліти {:is :автономні-персоналії
          :control "власні AP, ресурси, приватне життя, власні будівлі в слотах міських гексів"
          :output "вихід власних дій — собі; пасив їхніх будівель — місту (def building)"
          :never #{підлеглі "обов'язкові опоненти" "власники гекса" "орендарі гекса"}}}
  ;; «Власник будівлі» is a standing right won by permit; «оператор» is a function an actor performs.
  )
```

```clojure
(def scarcities  ;; 2026-09-04 — :права переписано
  {:увага {:чия "кожного актора окремо"
           :обмежує "скільки намірів він одночасно підтримує"}
   :праця {:чия Місто
           :обмежує "скільки відповідних робочих груп існує"}
   :товари {:чия "конкретного власника — міста або еліти"
            :обмежує "що фізично можна виробити, передати або спожити"}
   :земля {:чия Місто
           :обмежує "скільки гексів і слотів узагалі є; слот під будівлею зайнятий, поки будівля стоїть"}
   :права {:чия Мер
           :обмежує "хто може поставити будівлю в слоті; що місто взагалі виставляє на біржу"}
   :міст "дозвіл або пропозиція на біржі: чуже приходить до тебе за те, що ти віддаєш"})
```

```clojure
(def actor-action-capacity  ;; 2026-08-31 — confirmed; каденцію AP переписано 2026-09-02; паузу звужено 2026-09-04
  {:action {:requires #{AP товари робочі-групи}}
   :cadence {:paid-at "початок ходу"
             :instant "AP списується один раз, результат одразу"
             :duration "потрібна кількість AP щохода, доки дія триває"}
   :pause {:only-for "дії розвитку гексу (def hex-development) — це і є «будівництво», 2026-09-05"     ;; виробничі дії мають лише скасування
           :AP :not-spent
           :work-groups :released
           :resources :not-returned
           :resume :OPEN}                    ;; групи могли забрати через кільце — стан «відновлення заблоковане» не описаний
   :cancel {:resources "повертається частка, пропорційна ХОДАМ, ЩО ЛИШИЛИСЬ — скасував після 1 з 3, назад дві третини"
            :AP "повертається за цей хід"}
   :overcommit {:when "на початку ходу активні дії просять більше AP, ніж актор має"
                :rule "перехід ходу ЗАБЛОКОВАНО, доки актор не звільнить достатньо AP"
                :resolved-by "актор ставить на паузу або скасовує стільки активних дій, скільки треба"
                :never #{автопауза "борг AP" "тихе скасування"}
                :elites "еліта розв'язує це сама на своєму ході; як саме — лежить в :elite-choice-policy"
                :trigger :OPEN}              ;; дія перевіряється на старті і щохода просить ту саму суму — стан спрацьовує лише коли спроможність AP змінюється; що її змінює, не сказано
   :attention-axis "AP — єдина вісь уваги; окремої валюти «слот наказу» в моделі немає"
   :hex-constraint ^:provisional "одна активна дія на гекс — «поки що», перегляд не закритий"
   :hex-busy "зайнятий гекс не починає розвиток — випливає з :hex-constraint, не окреме правило"   ;; 2026-09-05
   :early-game ^:provisional "мер — лінійний менеджер: розвиток гексів і базове збирання на нерозвинених гексах своїм AP — до першої еліти"   ;; 2026-09-05
   :elite-AP {:pool :private
              :never "не додається до AP мера і не розподіляється гравцем"
              :used-by "еліта на власний розсуд"}
   :labour "робочі групи — вимога дії, але мер не призначає їх вручну"
   :elite-actions "на власних будівлях: AP еліти + групи з міського пулу через кільце → вихід еліті (def building)"   ;; 2026-09-05
   :budget-per-actor :OPEN})
```

```clojure
(def action-precondition  ;; 2026-09-05 — confirmed (D2); читати реверсивно: дія знає свою умову, а не район «відкриває» дії
  {:rule "кожна дія несе власну умову виконання"
   :condition-may-be #{"спеціалізація району" "місцевість гекса" "ресурс на гексі" "власна будівля — дії еліт існують лише на них" "конкретна еліта — унікальні дії"}
   :district-requirements "район теж має вимоги: місцевість, ресурс, тощо"
   :consequence "перелік «які дії доступні тут» ВИВОДИТЬСЯ з умов дій і стану гекса; окремих таблиць «що що відкриває» немає"
   :ui "недоступна дія показує, яка саме умова не виконана"})
```

```clojure
(def vocabulary  ;; 2026-09-05 — confirmed (D6); терміни в GLOSSARY
  {:дія "те, що робиться НА ГЕКСІ: виробництво або розвиток гексу; вимагає AP, товарів і груп (def actor-action-capacity)"
   :наказ "окремий вид ПОЛІТИЧНИХ дій без прив'язки до гекса: пропозиція на біржі, дозвіл на слот, політика (def mayor-instruments)"
   :пропозиція "стояча заявка на біржі: віддаю A за B (def exchange)"   ;; 2026-09-05
   :never "називати дію на гексі наказом"})
```

```clojure
(def turn-order  ;; 2026-09-02 — confirmed; форма з Solium Infernum; історичний аналог звірено 2026-09-04
  {:shape "список акторів, мер серед них; ходять по черзі, старт зміщується на одного щохода"
   :is "єдиний розподільник дефіциту — для робочих груп, слотів і лотів на біржі"
   :claim (-> "хід актора" "віддав наказ" "витратив AP" "групи з пулу пішли працювати до нього")
   :exchange "пропозиція заповнюється на ході того, хто бере; двоє хочуть той самий лот — перший у кільці (def exchange)"   ;; 2026-09-05
   :consequence "хто ходить раніше — той перший забирає дефіцитне; кільцевий зсув не дає це закріпити"
   :precedent "магдебурзьке право: бурмистр по черзі на квартал; Львів: три бурмистри по місяцю — Flows/RESEARCH_MEDIEVAL_CITY_ANALOGUES.md"
   :membership "справжня влада — СКЛАД кільця (спростування там само); хто приймає еліту до кільця — §10 :ring-admission"
   :not-imported "слот-мажорне переплетення наказів Solium Infernum — беремо тільки обертання регентства"})
```

```clojure
(def world-is-inert  ;; 2026-08-31 — confirmed; звужено 2026-09-02
  {:rule "жоден товар не ПОЯВЛЯЄТЬСЯ без автора: кожен ресурс створив хтось, витративши на це своє AP"
   :example "поки ніхто не рубає ліс — деревини не більшає; поки ніхто не вирощує зерно на полях — його не більшає"
   :consequence "у кожної ПОЯВИ фізичного товару є відповідь на питання «хто це зробив?»"
   :consumption {:is "постійне й автоматичне"
                 :who "місто на своє існування і еліти на своє"
                 :needs-no-author true}
   :passive "пасив будівлі з'являється щохода без AP — його автор той, хто збудував; AP витрачене на будівлю, не на кожен хід (def building)"   ;; 2026-09-05
   :risk "повторне погодження кожної дії щокроку"
   :mitigation "активна дія стоїть, доки її не зупинено; повторного кліку не потребує, але щохода забирає AP автора"})
```

---

## 3 · Labour

```clojure
(def work-group  ;; 2026-08-31 — confirmed
  {:unit "1 робоча група"
   :indivisible true
   :why-not-population "мале лічильне число читається; неподільність робить вимогу рішенням, а не мікрооптимізацією"
   :typed "за щаблем: селянські групи, ремісничі групи — тип групи і щабель населення одна вісь (def tier-is-labour-category)"
   :type-gates "які роботи взагалі доступні цій групі"
   :count-per-type :is-a-resource
   :mayor-control false
   :relation-to-action "дія декларує потрібну кількість і типи; мер не розкладає групи вручну"})
```

```clojure
(def category-non-interchange  ;; 2026-08-31 — confirmed
  {:rule "категорії праці не взаємозамінні"
   :means "надлишок фермерів не закриває нестачу ремісників"
   :consequence "дефіцит існує окремо в кожній категорії"
   :is "самостійна вісь конфлікту поряд із товарами, увагою та правами"})
```

```clojure
(def tier-is-labour-category  ;; 2026-09-05 — confirmed (D1); еталон Anno 1800
  {:rule "щабель населення і категорія праці — одна вісь: селяни дають селянські групи, ремісники — ремісничі"
   :action "вимагає груп конкретного щабля; «потрібен 1 ремісник» читається як «одна реміснича група»"
   :count "групи щабля — одне число: вільно/усього, з дельтою (def population-dynamics)"
   :never #{"окрема професія всередині щабля як друга вісь" "сума груп усіх щаблів — категорії не взаємозамінні"}})
```

```clojure
(def labour-pool  ;; 2026-09-02 — confirmed; форма з Victoria 3
  {:scope :global                    ;; у Victoria 3 пул на штат; у нас місто одне, тож штат один
   :typed "щабель гейтить, які роботи група взагалі може взяти — кваліфікація і є щабель (def tier-is-labour-category)"
   :hiring "дія оголошує потрібні типи й кількість; заповнення автоматичне, мер не призначає"
   :deficit "рахується окремо в кожній категорії й показується як незакрита вимога"
   :allocation turn-order            ;; конкуренцію розв'язує порядок ходу, не ставка
   :wage :not-needed
   :upkeep "групи їдять зі складу міста незалежно від того, хто їх зайняв; платні немає — праця є спільним ресурсом, який ділить кільце"   ;; 2026-09-05
   :employers "дії мера на гексах і дії еліт на будівлях беруть з одного пулу"
   :not-imported "ринок зарплат Victoria 3 і переманювання вищою ставкою"
   :held-while "дія триває; пауза й скасування звільняють групи"})
```

---

## 4 · Goods, money and prices

Physical goods are the authoritative economy. They are produced, owned, transferred and consumed in actual units.
Money is finite and enters the world only through two named doors; its mechanic is deferred, its principle is fixed.

```clojure
(def economic-authority  ;; 2026-09-02 — confirmed; :money переписано 2026-09-04
  {:goods {:are :physical
           :must-exist "before an action, purchase or consumption can use them"
           :created-by "an authored production action"
           :removed-by #{споживання будівництво виробничий-вхід лот-на-біржі}}
   :money {:is "кількісний ресурс, не декорація"
           :finite true
           :sources #{"торгівля між містами" "карбування монети з металу"}
           :never "з повітря"
           :control "частково у мера — карбування йде через міські дії й біржу"
           :purpose "купувати реальні товари на біржі, коли бартер незручний"
           :mechanic :OPEN                 ;; власник: «тоді можна буде вводити гроші» — принцип зараз, механіка пізніше
           :enters-as "універсальний зустрічний товар на біржі — шаблон пропозиції не змінюється (def exchange)"}   ;; 2026-09-05
   :balance-law "виробництво визначається товарами; гроші лише маршрутизують доступ до них"})
```

```clojure
(def purchase-semantics  ;; 2026-09-02 — confirmed; джерела входів еліти переписано 2026-09-05 (revision 5)
  {:owned-goods "власник використовує їх без додаткової грошової ціни"
   :elite-inputs #{"взяти лот, який місто виставило на біржу"
                   "взяти лот іншої еліти"
                   "виробити самій дією на власній будівлі"}
   :city-stock "закритий для еліт; в обіг іде лише те, що мер виставив на біржу — резерв за конструкцією"
   :hard-block "відсутній фізичний товар не замінюється будь-якою кількістю грошей"
   :never "показувати одну потребу як «товари плюс незалежна грошова вартість»"})
```

```clojure
(def exchange  ;; 2026-09-05 — confirmed (revision 5): «спрощений, але доволі функціональний ринок»; біржа за одним шаблоном для всіх
  {:offer {:by "будь-який актор: мер чи еліта"
           :gives "[товар A, кількість a]" :wants "[товар B, кількість b]"   ;; «2 пшениці за 1 пиво»
           :lot "a:b — заповнюється цілими лотами"
           :cap "виставлена кількість; решта складу в обіг не йде"}
   :escrow "виставлений товар покидає склад у момент розміщення — населення його не з'їсть, і саме цим тримається ліміт"   ;; власник: «згоден»
   :post {:cost "1 AP — політична дія (def vocabulary :наказ)" :stands "до вичерпання або зняття" :withdraw "безкоштовно; зміна = нове розміщення"}
   :fill {:cost 0 :when "на ході того, хто бере" :partial "будь-яку кількість лотів, не весь пул одразу"}
   :tie-break turn-order                   ;; двоє хочуть той самий лот — перший у кільці бере
   :visibility "уся книга видима меру; торгівля еліт між собою йде через неї"
   :price "глобальної ціни немає; найкращі пропозиції в книзі і є ціною"
   :balances "кошик пасивів міста проти клітинок потреб щаблів: надлишок віддається за те, чого бракує"
   :phase-2 "монета входить як універсальний зустрічний товар без зміни шаблону (def economic-authority :money)"
   :absorbs #{"замовлення на закупівлю" "оголошена ставка обміну"}   ;; обидва — окремий випадок пропозиції
   :precedent "Patrician: правило випуску на локальний ринок; The Guild 2 — негативний контроль: без реакції AI на пропозиції ринок мертвий (§10 :elite-choice-policy)"})
```

```clojure
(def construction-goods  ;; 2026-09-05 — confirmed (D7); еталон Anno 1800 — виділені товари для будівництва
  {:is "модельна категорія товарів: те, що споживають дії розвитку гексу (def hex-development)"
   :examples #{деревина камінь}          ;; зміст авторський, не структурний
   :ui "постійно видимі на HUD; решта товарів — на поверхні складу"
   :not "окремий ресурс — це ті самі фізичні товари з окремою роллю"})
```

```clojure
(def price-band  ;; 2026-09-02 — shape confirmed; formula OPEN; парковано разом із механікою грошей
  {:shape "кожен товар має дозволений діапазон ціни за принципом Victoria 3 / X4: Foundations"
   :signal "дефіцит рухає ціну вгору, надлишок — вниз"
   :alt-form "ціна стала, кількість змінна (Assize of Bread, бл. 1266) — ближче до товарної моделі; див. research-док"
   :formula :OPEN
   :range-values :OPEN
   :in-revision-5 "глобальної ціни немає — ціною є найкращі пропозиції в книзі (def exchange); діапазон повертається лише разом із монетою, якщо повернеться"
   :priority :later})
```

---

## 5 · Population tiers and needs

```clojure
(def tier-ladder  ;; 2026-08-31 — confirmed; economy follows Anno 1800 «так простіше»
  {:shape :ladder
   :rule "щабель = 3 потреби з попереднього рівня, якщо такий є, плюс 3 нові"
   :why "рання потреба лишається живою, нова нашаровується зверху"
   :content-is-authored "що саме — овочі, хліб, м'ясні делікатеси — вибір авторський, не структурний"})
```

```clojure
(def need-is-a-slot  ;; 2026-08-31 — confirmed
  {:need "клітинка з умовою, не конкретний товар"
   :condition {:категорія :їжа|:паливо|:товар|…
               :рівень-якості 1|2|3|…}
   :example {:селянин [:їжа :паливо :товар]
             :ремісник "3 успадковані плюс 3 нові"}
   :why "клітинка з умовою вже є заміщенням — ринковий вибір додається фазою, а не переписуванням"
   :phase-1 "умові відповідає рівно один товар — читається як Anno"
   :phase-2 "умові відповідає кілька товарів; доступність і ціна визначають фактичне заповнення"
   :never "хардкодити перелік товарів у щабель"})
```

```clojure
(def progression-guards  ;; 2026-08-31 — confirmed
  {:quality "рівень якості на товарі — клітинка «їжа-2» рибою не закривається"
   :variety "N клітинок закриваються N різними товарами, не N одиницями одного"
   :without-them "гравець вічно виробляє найдешевший товар і драбина перестає вести"})
```

```clojure
(def population-dynamics  ;; 2026-09-05 — confirmed (D8) як МІНІМАЛЬНИЙ режим; «а далі подивимося»
  {:unit "групи щабля (def work-group)"
   :grow "усі клітинки потреб щабля закриті → кількість груп щабля росте"
   :decline "клітинки не закриті → кількість груп щабля падає"
   :promotion :OPEN                     ;; чи і як група підіймається на наступний щабель
   :rates :authored                     ;; скільки ходів і на скільки груп — авторські числа, не модель
   :near-cap "біля стелі (def city-supply :cap) ріст і падіння чергуються — падіння має бути повільнішим за ріст або мати пам'ять; авторське число"   ;; 2026-09-05
   :consequence "це петля зворотного зв'язку базового зрізу; дельта біля числа груп і є її читанням"})
```

```clojure
(def city-supply  ;; 2026-09-04 — replaces city-procurement; :rule і :shortfall переписано 2026-09-05 (revision 5)
  {:rule "місто споживає зі свого пулу; пул наповнюють дії мера повністю і пасиви всіх будівель повністю (def building)"
   :cap "населення ≤ (сума пасивів + вихід мера) / споживання групи — стеля задана землею і слотами, не AP"
   :shortfall (-> "пул міста порожній або кошик не той"
                  "пропозиція на біржі: місто віддає надлишок за потрібне (def exchange)"
                  "або міжміська торгівля")
   :purchase-order {:is "окремий випадок пропозиції на біржі: місто віддає Y, хоче X"
                    :replaces reserve-decree}
   :intercity-trade :OPEN
   :unmet-need {:minimal "потреби щабля не закриті → групи щабля падають (def population-dynamics)"   ;; 2026-09-05
                :wider :OPEN}})        ;; що ще стається, коли товару немає і купити ніде — окрема тема
```

The city and its population remain major consumers, but elites are consumers too. Their private demand connects the
anonymous tier economy to named biographies.

---

## 6 · Elite life and motivation

An elite has two parallel development branches. The public branch changes what the actor can do in the city. The
private branch explains why the actor wants goods and income at all.

```clojure
(def elite-development  ;; 2026-09-02 — confirmed foundation; :public переписано 2026-09-04
  {:public {:contains #{AP будівлі-в-слотах спроможності пропозиції-на-біржі}
            :lives "у будівлях на міських гексах, за дозволом"
            :never "власний гекс"
            :capabilities "слоти під будівлі, розмір пулу AP, пасивні бонуси — (def elite-capabilities)"}   ;; 2026-09-05
   :private {:contains #{маєток сім'я діти рівень-життя амбіції}
             :lives "у профілі актора, поза просторовою сіткою гексів"}
   :bridge {:goods "реальні товари виробляються містом і споживаються приватним розвитком"
            :money "дохід дозволяє придбати чужі товари"
            :personality "визначає, на що актор прагне перетворити доступні гроші й товари"}})
```

```clojure
(def elite-capabilities  ;; 2026-09-05 — confirmed shape (revision 5); зміст OPEN
  {:is "розвиток еліти прив'язаний до її спроможностей — вона змушена розвивати себе"
   :axes #{"кількість слотів під будівлі, які вона може тримати" "розмір пулу AP" "пасивні бонуси"}
   :forces "кожна вісь обмежує підприємство: більше будівель без AP їх не обробити, більше AP без слотів нема де"
   :content :OPEN
   :precedent "The Guild 3: титул гейтить кількість бізнесів (RESEARCH_MEDIEVAL нитка 5); конверсія статусу — та сама драбина, що й маєток"})
```

```clojure
(def elite-emergence  ;; 2026-09-05 — confirmed principle (revision 5); механіка OPEN
  {:created-by "окрема автономна ігрова механіка"
   :never "мер створює еліту"
   :ties-to "щаблі породжують еліт — §10 :elite-roster"
   :mechanic :OPEN
   :load-bearing "це вихід із базової фази: без еліт немає будівель, без будівель немає пасивів, стеля міста дорівнює AP мера"})
```

```clojure
(def virtual-estate  ;; 2026-09-02 — confirmed boundary; :structure має research-напрям
  {:is "приватний віртуальний актив еліти"
   :not "район, будівля на гексі або частина просторової виробничої карти"
   :consumes "реальні товари з міської економіки"
   :connects-to #{сім'я діти статус амбіції особисті-події}
   :structure :OPEN
   :research-direction "конверсія статусу: земля → дім → шлюб → посада → титул, як драбина клітинок потреб (RESEARCH_MEDIEVAL нитка 5, 80)"
   :effects :OPEN
   :succession :OPEN})
```

```clojure
(def elite-motivation  ;; 2026-09-02 — foundation confirmed, derivatives OPEN; петлю вибору звужено розворотом 2026-09-04
  {:root "власне життя персоналії: споживання, маєток, сім'я, діти й амбіції"
   :money "засіб отримати товари й можливості, не фінальна мета"
   :work "джерело товарів, доходу, майна та спроможностей для життєвого розвитку"
   :consumption "фізично забирає товари з економіки"
   :personality "задає різні пріоритети однаково забезпеченим акторам"
   :choice-algorithm :OPEN
   :load-bearing "без реакції еліт на пропозиції біржі ринок мертвий — The Guild 2 як негативний контроль; найпростіше правило: взяти вхід для наступної дії, якщо ціна не вища за поріг; виставити надлишок понад власне споживання"   ;; 2026-09-05
   :consumption-cadence :OPEN
   :life-cycle :OPEN
   :social-memory event-ledger})
```

```clojure
(def event-ledger  ;; 2026-09-04 — confirmed («згоден»); гранулярність OPEN
  {:is "журнал наслідкових подій — носій історії замість графа стосунків"
   :source "ECS-пульси, що вже існують; журнал — їх запис"
   :shows "шар еліта↔еліта: лоти між елітами, шлюби, претензії; та всі заповнені пропозиції біржі"
   :granularity :OPEN
   :agent-position "на РОДИНУ, з причиною в спадкуванні — флорентійські ricordanze; де статус закріплений законом, журналів не вели (85)"
   :closes :elite-social-model})
```

Elites remain few and named. Work groups remain anonymous economic units. Elites arrive by an autonomous mechanic
the mayor does not control (`elite-emergence`); its rule and the transition from population group to person are open.

---

## 7 · Political layer and dual progression

```clojure
(def political-actor  ;; 2026-09-02 — revised foundation; :leverage конкретизовано 2026-09-04
  {:identity "конкретна персоналія, не абстрактна фракційна шкала"
   :motivation elite-motivation
   :claim "конкретна вимога: слот, дозвіл, лот на біржі, умови, закриття категорії"
   :leverage #{"не заповнювати пропозиції міста" "притримати власні лоти" "перестати продавати іншій еліті"
               "домагатись закриття категорії" "піти в інше місто — з появою міжміської торгівлі"}
   :conflicts-with "той, чиї цілі використовують той самий дефіцит"
   :never :єдина-шкала-задоволення
   :social-model event-ledger})
```

```clojure
(def tension-source  ;; 2026-08-31 — confirmed
  {:is :менеджмент-дефіциту
   :not "політичний процес як окрема ізольована гра"
   :slogan "хочеш сильне та процвітаюче місто — вмій домовлятися"
   :in-revision-5 "кожен відданий слот дає місту сталий пасив, але дії еліти на ньому беруть групи з того самого пулу, що й дії мера; ринок балансує кошик пасивів проти клітинок потреб"})
```

```clojure
(def dual-progression  ;; 2026-09-02 — confirmed; :arc переписано 2026-09-04
  {:elite "розвиває виробничі спроможності та приватне життя; зрілі еліти відкривають дії, недоступні меру"
   :mayor "розвиває непряме керування: слоти, дозволи, пропозиції на біржі, політики"
   :arc (-> "пряма дія мера на міському гексі"
            "розвиток гексу: з'являються слоти"
            "дозвіл: еліта ставить будівлю, місто дістає її пасив"
            "біржа: потрібне купується за надлишок, не керується"
            "зміна стимулів політиками")
   :ui-law "новий рівень стискає ручну роботу попереднього, а не нашаровує ще один обов'язок"})
```

```clojure
(def mayor-instruments  ;; 2026-09-05 — revised for revision 5: торг живе в книзі біржі, не в угоді
  {:form "стояче право або стояча пропозиція; часток і строків немає"
   :дозвіл {:is "слот у спеціалізованому гексі під будівлю еліти; пасив будівлі місту — за типом будівлі, не за торгом" :requires :згода-обох}
   :пропозиція {:is "лот на біржі: віддаю A за B (def exchange)" :cost "1 AP на розміщення" :requires nil}
   :політика {:is "загальні умови, що змінюють стимули всіх одразу" :example "субсидія фермерам; закриття щабля" :requires nil}
   :never #{"наказ еліті" "конфіскація будівлі" "примусове заповнення лота"}
   :deal-ap-cost "розміщення 1 AP; заповнення чужої пропозиції 0"
   :blocked-by :elite-choice-policy})
```

```clojure
(def reserve-decree  ;; RETIRED 2026-09-04 — revision 3 closes the leak by construction
  {:status :retired
   :was "указ «зарезервувати N товару X», 1 AP, 5 ходів, зв'язував лише еліт"
   :why-retired "еліта бере зі складу міста ТІЛЬКИ за угодою з мером — витоку, від якого указ захищав, більше немає"
   :replaced-by "наказ на закупівлю (def city-supply) — інструмент спонукання, не заборони"
   :record "Flows/FLOW_BUILD_UX_DECONGESTION.md і Flows/RESEARCH_INTERMEDIATE_GOODS_CONTENTION.md, вердикт 4"})
```

```clojure
(def mayor-currency  ;; 2026-09-05 — revised for revision 5: слоти замість оренди
  {:слот "місце під будівлю в спеціалізованому гексі — дозвіл"
   :доступ-до-складу "лише лоти, які мер виставив на біржу"
   :property "еліти не дістануть цього ніде більше — тільки в мера"
   :self-limiting "мапа — це казна мера: слотів скінченна кількість; відданий слот дає сталий пасив, але дії еліти на ньому беруть групи з того самого пулу"
   :arc "до пізньої гри всі слоти забудовані — правиш біржею і політиками, а не працею"
   :asymmetry "еліти багатші товарами й спроможностями, мер багатший правом і землею"
   :retired #{"грант гекса у власність" "викуп гекса" "оренда гекса" "частка в дозволі" "данина натурою"}})
```

---

## 8 · Land, slots and buildings

```clojure
(def hex-is-district  ;; 2026-09-02 — confirmed; :states і :slots переписано 2026-09-05 (revision 5)
  {:identity "гекс і район — один об'єкт; на гексі рівно один район"
   :owner Місто                         ;; завжди; власності й оренди еліт немає
   :mayor "розпоряджається всіма гексами: спеціалізація, дії, дозволи на слоти"
   :states #{"нерозвинений" "спеціалізований — має слоти під будівлі"}
   :specialization {:decides Мер
                    :is "шлях розвитку гексу — призначається і еволюціонує діями будівництва (def hex-development)"
                    :requires "вимоги району: місцевість, ресурс гекса, тощо (def action-precondition)"
                    :opens "N слотів під будівлі; N авторське за спеціалізацією"}
   :closes "одиниця власності — гекс; одиниця права — слот"})
```

```clojure
(def hex-development  ;; 2026-09-05 — confirmed (D3): «це спеціалізація гексу, розвиток гексу, його еволюція»
  {:is "будівництво у значенні моделі: дії, що призначають спеціалізацію гексу або ведуть її далі"
   :action "звичайна дія (def actor-action-capacity): AP за хід, тривалість, групи щабля, будівельні товари (def construction-goods)"
   :pause "єдина категорія дій, яку можна ставити на паузу"
   :busy-hex "гекс з активною дією не починає розвиток — з :hex-constraint"
   :switch :OPEN                        ;; чи є дія зміни спеціалізації на іншу, чи гекс лише розвивається далі
   :yields "розвиток відкриває слоти — єдине, що робить землю казною мера (def mayor-currency)"   ;; 2026-09-05
   :buildings "не сюди: будівлі ставлять еліти за дозволом (def building)"})
```

```clojure
(def lease  ;; RETIRED 2026-09-05 — revision 5
  {:status :retired
   :was "гекс на строк за частку виходу місту; суборенда еліта→еліта"
   :why-retired "на гексі діє лише мер; еліта тримає БУДІВЛЮ в слоті, не гекс; частки немає — пасив будівлі йде місту повністю, а вихід дій еліти — їй повністю"
   :replaced-by "дозвіл на слот (def building) і біржа (def exchange)"
   :record "Flows/FLOW_VISION_UNIFICATION.md"})
```

```clojure
(def building  ;; 2026-09-05 — revised for revision 5: пасив лише місту, підприємство лише еліті
  {:is "окремий іменник у СЛОТІ спеціалізованого гексу, не сам район"
   :built-by "еліта за дозволом мера, за власні товари й AP"
   :bound-to "одна еліта — власник; мер будівель не ставить (2026-09-05, «а там подивимося»)"
   :passive {:to Місто
             :form "щохода, безумовний: без праці, без AP, без торгу"
             :fixed-by :building-type
             :why "це праця мешканців будівлі, абстрагована; автор пасиву — той, хто збудував (def world-is-inert :passive)"}
   :elite-return "право дій на цій будівлі: AP еліти + групи з пулу → вихід еліті"
   :effects #{"відкриває дії" "дає модифікатор" "щось розблоковує"}
   :example "поле на фермі"
   :precedent "історична будівля розблоковує ІНСТИТУЦІЮ — суд, мито, ринок (research нитка 7)"
   :permit {:is "дозвіл на слот — угода з мером" :due :none :risk "еліти — чи окупиться будівля її діями"}   ;; пасив і є платою місту
   :retired #{"данина натурою за дозволом" "будівля на орендованому гексі" "невиконана данина"}})
```

```clojure
(def value-transfers  ;; 2026-09-05 — revision 5: жодних часток
  {:mayor-actions "усе місту"
   :building-passive "усе місту"
   :elite-actions "усе еліті"
   :exchange "лот за лот на біржі — єдина форма обміну між акторами (def exchange)"
   :payment-form "натурою; монета — коли з'явиться, як зустрічний товар"
   :retired #{performer-fee tax rent "частка + строк"}
   :never "трактувати приклад «2 пшениці за 1 пиво» як універсальну ставку"})
```

---

## 9 · Reading the model on screen

`UI_LANGUAGE.md` owns display style only; the interface for a mechanic belongs to that mechanic's own UI
document. What follows are the model-side facts any such document must honour.

```clojure
(def display-consequences  ;; 2026-09-02 — confirmed; :tenure/:rights переписано 2026-09-04
  {:labour "категорії = щаблі й не взаємозамінні, тому кожен щабель показує власні групи: вільно/усього з дельтою"
   :labour-control "для мера робочі групи показуються як стан і вимога, ніколи як ручний контрол"
   :needs-widget "щабель має віджет сталого розміру незалежно від кількості товарів у грі"
   :action-author "кожна активна дія показує, чий AP її підтримує; дію еліти на її будівлі мер бачить: ЩО робиться — глибина деталей OPEN (§10 :elite-action-detail)"
   :action-cadence "тривала дія показує ціну за хід, а не разову — і те, що пауза звільняє AP і групи"
   :turn-order "чий зараз хід і хто ходить наступним — це видима інформація: від неї залежить, кому дістанеться дефіцитне"
   :goods "показуються як реальні вимоги, наявність і споживання"
   :construction-goods "будівельні товари виділені на HUD окремо від решти складу (def construction-goods)"
   :population "групи щабля ростуть або падають — дельта біля числа груп (def population-dynamics)"
   :vocabulary "дія на гексі і наказ — різні слова на екрані, як у моделі (def vocabulary)"
   :money "показується лише коли має механіку; до того — ніде"
   :hex-state "нерозвинений або спеціалізований: скільки слотів, які зайняті і ким; «платник» не є фактом узагалі — мер витрачає міське, еліта своє"
   :building "будівля в слоті показує власника і свій пасив місту"
   :offer "одна форма читання для всієї біржі: хто, віддає A за B, скільки лотів лишилось"
   :attention "лот міста заповнено або вичерпано, еліта просить слот — заздалегідь, не по факту"
   :elite-life "маєток, сім'я та амбіції належать профілю актора, не гексу й не постійному HUD"
   :ledger "шар еліта↔еліта видимий лише через журнал — інакше він несподіванка без пояснення"
   :surfaces "економіка і біржа — різні поверхні, з'єднані намірами конкретних персоналій"})
```

---

## 10 · Open derivatives

```clojure
[{:open :elite-roster
  :question "як саме щаблі породжують конкретних персоналій"}
 {:open :elite-choice-policy
  :question "як персоналія обирає між будівництвом, діями на будівлях, біржею, споживанням, маєтком, сім'єю та амбіцією"
  :narrowed-by "revision 5: єдиний шлях до входів — біржа; несуча для ринку (def elite-motivation :load-bearing)"
  :agent-proposal "петля: потреба → чи є лот на біржі → чи можу зробити дією на своїй будівлі → виставити власну пропозицію (переписано під rev 5, 2026-09-05)"
  :blocks #{mayor-instruments dual-progression}}
 {:open :estate-structure
  :question "рівні, складові, ефекти та товари віртуального маєтку"
  :research-direction "конверсія статусу як драбина клітинок"}
 {:open :family-life-cycle
  :question "сім'я, діти, старіння, спадкування та завершення історії персонажа"
  :research-direction "шлюб як трансфер будівель і місця в кільці; спадкування як причина журналу"}
 {:open :elite-consumption
  :question "ритм, пріоритети та наслідки споживання фізичних товарів"}
 {:open :intercity-trade
  :question "міжміська торгівля — джерело монети й товарів, яких немає ні в міста, ні в еліт; поверхня ще не описана"}
 {:open :unmet-need-consequence
  :question "що стається, коли клітинка потреби лишається порожньою і купити товар ніде"
  :minimal-mode "групи щабля падають (def population-dynamics), 2026-09-05; ширший наслідок відкритий"
  :research-input "люди йдуть туди, де є їжа; заворушення б'є по складу (RESEARCH_MEDIEVAL нитка 2, 65)"
  :separate-discussion true          ;; власник 2026-09-04
  :carries "без наслідку немає челенджу"}
 {:open :money-mechanic
  :question "коли і як входить скінченна монета: ланцюг карбування, міжміська торгівля, монета як зустрічний товар на біржі"
  :principle-fixed true}
 {:open :market-price-band
  :question "точний діапазон і правило руху ціни всередині нього"
  :deferred true}
 {:open :elite-emergence-mechanic
  :question "як саме окрема автономна механіка породжує еліту: тригер, стартові спроможності, зв'язок зі щаблями (def elite-emergence)"}
 {:open :elite-capabilities-content
  :question "рівні драбини спроможностей: скільки слотів, AP і які бонуси на кожному (def elite-capabilities)"}
 {:open :pause-resume
  :question "стан «відновлення заблоковане», коли групи паузованого будівництва забрали через кільце"}
 {:open :ap-overcommit-trigger
  :question "що змінює спроможність AP актора — без цього стан перевитрати не спрацьовує"}
 {:open :goods-rates-and-orders
  :question "швидкості споживання і розміри партій: скільки товару йде за хід і скільки створює одна дія"
  :classification {:перший-порядок "дія БЕЗ вхідних товарів — пшениця, риба, м'ясо, дошки"
                   :другий-порядок "дія З вхідними товарами — те, що хтось переробив"}
  :why-it-matters "швидкість споживання перетворює запас у ХОДИ: «20 пшениці» не значить нічого, «12 ходів їжі» значить усе"
  :coupled-to :unmet-need-consequence
  :also-needs :elite-consumption
  :deferred true
  :why-deferrable "перші кілька ходів мають лише першопорядкові ресурси"}
 {:open :ap-budget-per-actor
  :question "скільки AP має кожен тип актора і як ця спроможність росте"}
 {:open :category-cap
  :question "чи є стеля кількості груп у щаблі політичним об'єктом: еліти щабля хочуть закрити, мер відкриває політикою; прецедент Zunftzwang"
  :agent-proposal "так (75); складено сюди 2026-09-05, D9"}
 {:open :ring-admission
  :question "хто приймає еліту до кільця і чи це право мера"
  :agent-proposal "право мера — research: справжня влада це склад кільця (65); складено сюди 2026-09-05, D9"}
 {:open :gameplay-layers
  :question "чи тримається рамка трьох шарів: субстрат міста; мер↔еліта (дозволи й біржа); еліта↔еліта (лоти між елітами, конкуренція за групи, закриття щабля, шлюб) — мер спостерігає через журнал"
  :agent-proposal "так (70); власник дав GO на розворот, не на рамку; складено сюди 2026-09-05, D9"}
 {:open :tier-promotion
  :question "чи і як група підіймається на наступний щабель — мінімальний режим (def population-dynamics) має лише ріст і падіння"}
 {:open :elite-action-detail
  :question "як глибоко мер бачить дію еліти на її будівлі: видно, ЩО робиться; деталізація пізніше (D5, 2026-09-05)"}
 {:open :specialization-switch
  :question "чи є дія зміни спеціалізації на іншу, чи гекс лише розвивається далі — «еволюція» (def hex-development)"}]
```

---

## Provenance

The economic model was established on **2026-08-31**, extended on **2026-09-02** (goods-first reframing; turn,
attention and tenure rules) and **reversed on 2026-09-04** (revision 3): the city owns every hex, elites hold land
and buildings only by deal, the three transfer types collapse into one, the reserve decree retires, money becomes
finite in principle with its mechanic deferred. `Flows/FLOW_BUILD_UX_DECONGESTION.md` preserves the verbatim
requests, the superseded rules and the reasoning; `Flows/RESEARCH_MEDIEVAL_CITY_ANALOGUES.md` holds the
historical precedents each rule was checked against (leases for a share, city granaries, rotating office,
closed guilds, family journals, purchased privileges).

On **2026-09-05** (revision 4) the model was unified against the two main-screen specifications that had drifted
apart: tier = labour category, per-action preconditions, construction = hex development, buildings to elites,
«наказ» as a political action, construction goods as a model category, minimal population dynamics; agent-proposed
framings were folded into §10. `Flows/FLOW_VISION_UNIFICATION.md` holds the owner's verbatim answers and the reasoning.

On **2026-09-05** (revision 5) the loop closed: building slots on specialized hexes, elite-built buildings whose passive
goes to the city only, elite actions on their own buildings with pooled labour, and an exchange of standing offers as the
only form of transfer. Leases, shares and dues retired; elite emergence became an autonomous mechanic and elite development
a ladder of capabilities. The same FLOW holds the owner's verbatim words.

The owner's named analogues are Anno 1800 for simple goods-led consumption, Victoria 3 / X4: Foundations for
bounded prices, The Sims / tamagotchi for the intended character feeling, **Solium Infernum** for the rotating
turn order, **Victoria 3** for the shape of the labour pool, and **Patrician** / **The Guild** for the city-and-
dynasty frame — The Guild 2 turn-based and seen from the mayor's side, revision 5.
