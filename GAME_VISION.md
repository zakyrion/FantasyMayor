---
category: C
read: trigger
trigger: "before game-design work on the core loop, economy, actors, elites, population needs, ownership or politics"
tags: [game-design, vision, economy, actors, politics]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[GLOSSARY](GLOSSARY.md)"
---

# GAME_VISION

FantasyMayor is a mayoral character sandbox: physical goods drive autonomous lives, stories and political choices.

> **Scope.** This file is the single source of truth for the game's durable premise and economic model. It says
> what the game does, not how code implements it (`ARCHITECTURE.md` / `ECS_CONVENTIONS.md`). It absorbs and
> replaces `ECONOMY_MODEL.md`.
>
> **Where the UI lives.** Not here, and not in one document. `UI_LANGUAGE.md` owns display STYLE only; a
> mechanic that needs an interface gets its own mechanic document, its own UI document and its own mockup.
> §9 states only the model-side facts those UI documents must honour.
>
> **Status.** Rules are confirmed by the owner unless explicitly marked `OPEN`. Family, estate and motivation
> details remain derivative mechanics; this document fixes only the foundation they must preserve.

---

## 1 · Game thesis

The player is the mayor of a living fantasy city. The city-builder is the setting and control layer; the emotional
subject is a small cast of autonomous actors whose work, property, households and ambitions generate observable
histories. The intended feeling is a character terrarium or tamagotchi at city scale, not an optimisation board with
portraits attached.

Life is simulated through consequential mechanics rather than continuous physical routines. An actor's production,
consumption, ownership, agreements and conflicts stand in for walking to places and performing everyday animations.
The player creates conditions, grants rights, places orders and changes incentives, then watches what actors choose.

```clojure
(def game-centre  ;; 2026-09-02 — confirmed
  {:player-role "мер: спочатку прямий менеджер, згодом розпорядник умов і правил"
   :world-role  "фентезійне місто як сцена та причинна система для історій"
   :elite-role  "небагато автономних персоналій із власними ресурсами, AP і життєвим розвитком"
   :story-source #{виробництво споживання власність дефіцит угоди амбіції сім'я}
   :simulation-boundary "значущі рішення й наслідки, не безперервний побут або ручне пересування персонажів"
   :success-shape "місто розвивається разом із біографіями його мешканців; єдина переможна шкала не визначена"})
```

```clojure
(def causal-story-loop  ;; 2026-09-02 — confirmed foundation
  {:actor "персоналія з власною історією та пріоритетами"
   :wants "товари, майно, безпеку, статус, сімейний або особистий розвиток"
   :chooses "дію в межах власних AP, ресурсів, прав і спроможностей"
   :changes "матеріальний стан міста та власне подальше становище"
   :produces "нові потреби, можливості, конфлікти й історії"
   :exact-choice-policy :OPEN})
```

---

## 2 · Actors and scarcities

```clojure
(def actors  ;; 2026-09-02 — revised and confirmed
  {Місто {:is :субстрат
          :generates робоча-сила
          :absorbs товари
          :also :політичний-суб'єкт}
   Мер   {:is :гравець
          :controls "міські активи, прямі дії та інституційні права"}
   Еліти {:is :автономні-персоналії
          :control "власні AP, ресурси, активи й життєві пріоритети"
          :never #{підлеглі "обов'язкові опоненти"}}}
  ;; «Власник» is an economic right; «оператор» is a function an actor performs.
  )
```

```clojure
(def scarcities
  {:увага {:чия "кожного актора окремо"
           :обмежує "скільки намірів він одночасно підтримує"}
   :праця {:чия Місто
           :обмежує "скільки відповідних робочих груп існує"}
   :товари {:чия "конкретного власника"
            :обмежує "що фізично можна виробити, передати або спожити"}
   :права {:чия Мер
           :обмежує "хто може використати землю та дозволені міські можливості"}
   :міст "домовленість: чужі AP, товари або права спрямовуються на твій результат"})
```

```clojure
(def actor-action-capacity  ;; 2026-08-31 — confirmed; каденцію AP переписано 2026-09-02
  {:action {:requires #{AP товари робочі-групи}}
   :cadence {:paid-at "початок ходу"
             :instant "AP списується один раз, результат одразу"
             :duration "потрібна кількість AP щохода, доки дія триває"}
   :pause {:AP :not-spent
           :work-groups :released          ;; вони більше не зайняті, доки дія стоїть
           :resources :not-returned}
   :cancel {:resources "пропорційна частина повертається"
            :AP "повертається за цей хід"
            :proportion-basis :OPEN}
   :attention-axis "AP — єдина вісь уваги; окремої валюти «слот наказу» в моделі немає"
   :hex-constraint ^:optional "можливо 1 наказ на гекс — не замок, модель лишається гнучкішою"
   :early-game "мер — лінійний менеджер: сам підтримує базові дії своїм AP"
   :elite-AP {:pool :private
              :never "не додається до AP мера і не розподіляється гравцем"
              :used-by "еліта на власний розсуд"}
   :delegation "актор може підтримувати дію власним AP замість мера — за угодою, не за наказом"
   :labour "робочі групи — вимога дії, але мер не призначає їх вручну"
   :budget-per-actor :OPEN})
```

```clojure
(def turn-order  ;; 2026-09-02 — confirmed; форма з Solium Infernum
  {:shape "список акторів, мер серед них; ходять по черзі, старт зміщується на одного щохода"
   :is "єдиний розподільник дефіциту — і для робочих груп, і для гексів"
   :claim (-> "хід актора" "віддав наказ" "витратив AP" "групи з пулу пішли працювати до нього")
   :consequence "хто ходить раніше — той перший забирає дефіцитне; кільцевий зсув не дає це закріпити"
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
   :typed true
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
(def labour-pool  ;; 2026-09-02 — confirmed; форма з Victoria 3
  {:scope :global                    ;; у Victoria 3 пул на штат; у нас місто одне, тож штат один
   :typed "кваліфікація гейтить, які роботи група взагалі може взяти"
   :hiring "дія оголошує потрібні типи й кількість; заповнення автоматичне, мер не призначає"
   :deficit "рахується окремо в кожній категорії й показується як незакрита вимога"
   :allocation turn-order            ;; конкуренцію розв'язує порядок ходу, не ставка
   :wage :not-needed
   :not-imported "ринок зарплат Victoria 3 і переманювання вищою ставкою"
   :held-while "дія триває; пауза й скасування звільняють групи"})
```

---

## 4 · Goods, money and prices

Physical goods are the authoritative economy. They are produced, owned, transferred and consumed in actual units.
Money exists because goods cross ownership boundaries, but the total money supply is not a simulated material system.

```clojure
(def economic-authority  ;; 2026-09-02 — confirmed
  {:goods {:are :physical
           :must-exist "before an action, purchase or consumption can use them"
           :created-by "an authored production action"
           :removed-by #{споживання будівництво виробничий-вхід}}
   :money {:is "абстрактний засіб обміну та локальний баланс власника"
           :purpose "купувати реальні товари в інших власників"
           :never "окремий матеріальний інгредієнт поверх потрібних товарів"}
   :global-money-supply {:status :not-simulated
                         :reason "на малому масштабі реальна грошова маса ускладнює модель, але не покращує товарні рішення"}
   :balance-law "виробництво визначається товарами; гроші лише маршрутизують доступ до них"})
```

```clojure
(def purchase-semantics  ;; 2026-09-02 — confirmed
  {:owned-goods "власник використовує їх без додаткової грошової ціни"
   :missing-goods "можуть бути придбані в іншого власника або через абстрактний ринок"
   :money-readout "приблизна сума закупівлі потрібних товарів за поточними цінами"
   :hard-block "відсутній фізичний товар не замінюється будь-якою кількістю грошей"
   :never "показувати одну потребу як «товари плюс незалежна грошова вартість»"})
```

```clojure
(def price-band  ;; 2026-09-02 — shape confirmed; formula OPEN
  {:shape "кожен товар має дозволений діапазон ціни за принципом Victoria 3 / X4: Foundations"
   :signal "дефіцит рухає ціну вгору, надлишок — вниз"
   :purpose "приблизно оцінити обмін реальних товарів, не симулювати фінансовий ринок"
   :background-market "може вводити й поглинати гроші, але ніколи не створює фізичний товар"
   :formula :OPEN
   :range-values :OPEN
   :priority :later})
```

The quantity of money may constrain one actor's immediate purchases without becoming a conserved world resource or
the primary balance target. This is the same abstraction boundary the owner identified in X4: Foundations and Anno
1800: goods carry the economy; money keeps exchange legible.

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
(def city-procurement  ;; 2026-09-02 — загальний опис МОЖЛИВОЇ механіки, не підтверджене правило
  {:status :proposed
   :rule "місто споживає зі свого пулу"
   :shortfall (-> "пул міста порожній" "купити в еліти-власника" "або на глобальному ринку")
   :is "головний торговельний цикл: мер не виробляє все сам, отже мусить домовлятися"
   :global-market :OPEN
   :unmet-need :OPEN})            ;; що стається, коли товару немає і купити ніде
```

The city and its population remain major consumers, but elites are consumers too. Their private demand connects the
anonymous tier economy to named biographies.

---

## 6 · Elite life and motivation

An elite has two parallel development branches. The public branch changes what the actor can do in the city. The
private branch explains why the actor wants goods and income at all.

```clojure
(def elite-development  ;; 2026-09-02 — confirmed foundation
  {:public {:contains #{професія AP земля виробничі-активи спроможності угоди}
            :lives "у матеріальному місті та на його гексах"}
   :private {:contains #{маєток сім'я діти рівень-життя амбіції}
             :lives "у профілі актора, поза просторовою сіткою гексів"}
   :bridge {:goods "реальні товари виробляються містом і споживаються приватним розвитком"
            :money "дохід дозволяє придбати чужі товари"
            :personality "визначає, на що актор прагне перетворити доступні гроші й товари"}})
```

```clojure
(def virtual-estate  ;; 2026-09-02 — confirmed boundary
  {:is "приватний віртуальний актив еліти"
   :not "район, будівля на гексі або частина просторової виробничої карти"
   :consumes "реальні товари з міської економіки"
   :connects-to #{сім'я діти статус амбіції особисті-події}
   :structure :OPEN
   :effects :OPEN
   :succession :OPEN})
```

```clojure
(def elite-motivation  ;; 2026-09-02 — foundation confirmed, derivatives OPEN
  {:root "власне життя персоналії: споживання, маєток, сім'я, діти й амбіції"
   :money "засіб отримати товари й можливості, не фінальна мета"
   :work "джерело товарів, доходу, майна та спроможностей для життєвого розвитку"
   :consumption "фізично забирає товари з економіки"
   :personality "задає різні пріоритети однаково забезпеченим акторам"
   :choice-algorithm :OPEN
   :consumption-cadence :OPEN
   :life-cycle :OPEN
   :social-memory :OPEN})
```

Elites remain few and named. Work groups remain anonymous economic units. Tiers may generate new elites, but the
exact roster and transition from population group to person are open.

---

## 7 · Political layer and dual progression

```clojure
(def political-actor  ;; 2026-09-02 — revised foundation
  {:identity "конкретна персоналія, не абстрактна фракційна шкала"
   :motivation elite-motivation
   :claim "конкретна вимога до товару, права, дозволу, активу або рішення"
   :leverage "що робить сам, якщо claim не закритий"
   :conflicts-with "той, чиї цілі використовують той самий дефіцит"
   :never :єдина-шкала-задоволення
   :social-model :OPEN})
```

```clojure
(def tension-source  ;; 2026-08-31 — confirmed
  {:is :менеджмент-дефіциту
   :not "політичний процес як окрема ізольована гра"
   :slogan "хочеш сильне та процвітаюче місто — вмій домовлятися"})
```

```clojure
(def dual-progression  ;; 2026-09-02 — confirmed
  {:elite "розвиває виробничі спроможності та приватне життя; зрілі еліти відкривають дії, недоступні меру"
   :mayor "розвиває непряме керування: торгівлю, замовлення, продаж і оренду землі, дозволи та укази"
   :arc (-> "пряма дія мера"
            "угода: та сама дія на AP еліти за винагороду"
            "замовлення результату замість керування виробництвом"
            "зміна стимулів законами, указами й субсидіями")
   :ui-law "новий рівень стискає ручну роботу попереднього, а не нашаровує ще один обов'язок"})
```

```clojure
(def mayor-instruments  ;; 2026-09-02 — confirmed
  {:угода {:is "адресна домовленість із конкретною елітою"
           :requires :згода-еліти          ;; еліта не підлеглий — вона може відмовити
           :surface "екран угод"}
   :політика {:is "загальні умови, що змінюють стимули всіх одразу"
              :example "субсидія фермерам"
              :requires nil}
   :never "наказ еліті — делегування як команда в моделі відсутнє"
   :blocked-by :elite-choice-policy})      ;; правило згоди ще не визначене
```

```clojure
(def mayor-currency  ;; 2026-08-31 — confirmed
  {:дозвіл "право збудувати конкретний район"
   :грант "гекс у власність"
   :property "еліти не дістануть цього ніде більше — тільки в мера"
   :self-limiting "мапа — це казна мера: на старті багатий землею і бідний товарами"
   :arc "до пізньої гри земля роздана — правиш авторитетом, а не роздачею"
   :asymmetry "еліти багатші товарами й спроможностями, мер багатший правом"})
```

---

## 8 · Ownership and transfers

```clojure
(def hex-is-district  ;; 2026-09-02 — confirmed
  {:identity "гекс і район — один об'єкт; на гексі рівно один район"
   :mayor "розпоряджається правами на міські гекси"
   :tenure #{оренда власність}
   :closes "одиниця власності більше не відкрита — весь шар прав спирається на гекс"})
```

```clojure
(def hex-tenure  ;; 2026-09-02 — confirmed
  {:оренда {:who "еліта в міста"
            :term "1 хід"
            :purpose "запустити на гексі одну дію"
            :is "кістяк першої фази гри"
            :contention turn-order}
   :власність {:transfer "мер передає гекс еліті"
               :owner-decides #{спеціалізація будівлі дії}
               :inputs "власні ресурси власника"
               :owes податок
               :mayor-keeps "податок, не дозвіл"}
   :payer-is-derived "платник дії = власник гекса; окремим вибором платник більше не є"})
```

```clojure
(def hex-buyback  ;; 2026-08-31 — confirmed, details deferred
  {:allowed true
   :price "називає власник — за поточною вартістю, яка зросла від його розвитку"
   :effect "помилковий грант не скидається, а дорожчає — вага лишається, глухого кута немає"
   :possible-side-effect "якщо мотивація це підтримує, еліта може взяти дешеве, розвинути й продати дорого"
   :requires-ui "вартість гекса має читатись до гранта і до викупу"
   :deferred "точна форма ціни"})
```

```clojure
(def ownership-and-production  ;; 2026-09-02 — confirmed
  {:default {:inputs "фізичні товари власника гекса або товари, які він купує"
             :output "майже весь результат належить власнику, крім зовнішніх зобов'язань"}
   :city-owned {:inputs :товари-міста
                :mayor-operated "AP мера підтримує дію; результат міський"
                :elite-operated "AP еліти підтримує дію; еліта отримує договірну винагороду"}
   :actor-owned {:inputs :товари-актора
                 :AP :AP-актора
                 :output "результат актора за вирахуванням податку місту"}
   :operator "конкретний актор, який витрачає AP; не третій власник ресурсів"
   :mayor-personal-material-pool :OPEN})
```

```clojure
(def value-transfers  ;; 2026-09-02 — distinction confirmed; formulas OPEN
  {:performer-fee "винагорода актору за використання його AP на чужому активі"
   :tax "окрема механіка на користь міста з приватного виробництва"
   :rent "плата власнику за використання його гекса"
   :payment-form "гроші або договірна частка фізичних товарів — точна форма залежить від механіки"
   :never "трактувати приклади 20% або 30% як універсальні ставки"})
```

```clojure
(def renting  ;; 2026-09-02 — предмет визначено; ставки лишаються OPEN
  {:object "гекс"                    ;; :rent-scope закрито — орендується гекс, не будівля й не право
   :city->elite {:term "1 хід"
                 :is "основний шлях першої фази: еліта бере гекс, щоб запустити на ньому дію"}
   :elite->elite {:why "активів у власника може бути більше, ніж його обмеженого AP"
                  :example "лісник із трьома лісопилками здає одну іншому ліснику"
                  :owner "зберігає гекс і отримує орендну плату"
                  :tenant "витрачає власні AP та операційні товари"}
   :rate :OPEN})
```

---

## 9 · Reading the model on screen

`UI_LANGUAGE.md` owns display style only; the interface for a mechanic belongs to that mechanic's own UI
document. What follows are the model-side facts any such document must honour.

```clojure
(def display-consequences  ;; 2026-09-02 — confirmed
  {:labour "категорії не взаємозамінні, тому кожна показує власний дефіцит"
   :labour-control "для мера робочі групи показуються як стан і вимога, ніколи як ручний контрол"
   :needs-widget "щабель має віджет сталого розміру незалежно від кількості товарів у грі"
   :action-author "кожна активна дія показує, чий AP її підтримує"
   :action-cadence "тривала дія показує ціну за хід, а не разову — і те, що пауза звільняє AP і групи"
   :turn-order "чий зараз хід і хто ходить наступним — це видима інформація: від неї залежить, кому дістанеться дефіцитне"
   :goods "показуються як реальні вимоги, наявність і споживання"
   :money "показується як приблизна вартість закупівлі відсутніх товарів, не як додатковий інгредієнт"
   :tenure "власність і оренда — різні стани гекса; платник із них ВИВОДИТЬСЯ, окремим контролом не є"
   :rights "власник, оператор, податок і оренда — окремі факти"
   :elite-life "маєток, сім'я та амбіції належать профілю актора, не гексу й не постійному HUD"
   :surfaces "економіка і політика — різні поверхні, з'єднані намірами конкретних персоналій"})
```

---

## 10 · Open derivatives

```clojure
[{:open :elite-roster
  :question "як саме щаблі породжують конкретних персоналій"}
 {:open :elite-choice-policy
  :question "як персоналія обирає між виробництвом, бізнесом, споживанням, маєтком, сім'єю та амбіцією"
  :blocks #{mayor-instruments dual-progression}}
 {:open :elite-social-model
  :question "які стосунки та спогади потрібні для історій; попереднє рішення «граф не потрібен» переглядається"}
 {:open :estate-structure
  :question "рівні, складові, ефекти та товари віртуального маєтку"}
 {:open :family-life-cycle
  :question "сім'я, діти, старіння, спадкування та завершення історії персонажа"}
 {:open :elite-consumption
  :question "ритм, пріоритети та наслідки споживання фізичних товарів"}
 {:open :global-market
  :question "чи існує глобальний ринок як джерело товарів, коли їх немає ні в міста, ні в еліт"}
 {:open :unmet-need-consequence
  :question "що стається, коли клітинка потреби лишається порожньою і купити товар ніде"}
 {:open :market-price-band
  :question "точний діапазон і правило руху ціни всередині нього"
  :deferred true}
 {:open :buyback-price-form
  :question "як рахується ціна викупу та чи можна платити поступкою"}
 {:open :rent-rate
  :question "скільки коштує оренда гекса і чим платиться"}
 {:open :distribution-order
  :question "у якій послідовності застосовуються податок, оренда, винагорода й інші зобов'язання"}
 {:open :refund-proportion-basis
  :question "«пропорційна частина ресурсів» при скасуванні — пропорційно решті кроків чи вже внесеному"}
 {:open :ap-budget-per-actor
  :question "скільки AP має кожен тип актора і як ця спроможність росте"}
 {:open :hex-order-constraint
  :question "чи діє обмеження «1 наказ на гекс» — записане як можливість, не як замок"}
 {:open :mayor-personal-pool
  :question "чи має мер окремий особистий ресурс; матеріальний гаманець не підтверджений"}
 {:open :slots-in-districts
  :question "концепція слотів у районах відкладена; слоти потреб — інша підтверджена механіка"}]
```

---

## Provenance

The economic model was established on **2026-08-31** and extended on **2026-09-02** — twice: first the
goods-first reframing, then the turn, attention and tenure rules. `Flows/FLOW_BUILD_UX_DECONGESTION.md`
preserves the verbatim requests, reasoning, rejected approaches and superseded decisions; when that FLOW closes
it moves to `Flows/Archive/`.

The owner's named analogues are Anno 1800 for simple goods-led consumption, Victoria 3 / X4: Foundations for
bounded prices, The Sims / tamagotchi for the intended character feeling, **Solium Infernum** for the rotating
turn order, and **Victoria 3** again for the shape of the labour pool. The last two were checked against
sources on 2026-09-02 and both diverge from their model deliberately: we take Solium Infernum's rotating
regency but not its slot-major order interleaving, and Victoria 3's qualification-gated auto-hired pool but not
its wage market — the turn order allocates scarce labour instead. The findings and their provenance live in the
FLOW.
