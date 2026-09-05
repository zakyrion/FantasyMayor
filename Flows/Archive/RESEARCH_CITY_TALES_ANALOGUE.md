---
category: A
read: archive
status: implemented
tags: [research, prior-art, game-design, city-builder, city-tales]
related:
  - "[FLOW_VISION_UNIFICATION](FLOW_VISION_UNIFICATION.md)"
  - "[GAME_MECHANICS](../../GAME_MECHANICS.md)"
---

# RESEARCH_CITY_TALES_ANALOGUE

Rev 5 mechanics against City Tales - Medieval Era: same, different or absent, and what each implies for us.

# Question

```clojure
{:question "що в механіках City Tales - Medieval Era збігається з нашою моделлю rev 5, що влаштовано інакше, чого там немає взагалі — і що з цього переноситься або, навпаки, підтверджує нашу відмінність"
 :why-no-fast-answer "це не довідка про одну гру, а мапа по всіх осях моделі: кожна їхня механіка виграє у своєму режимі (реальний час, один актор, без наслідків), і переноситься не форма, а правило за нею"
 :opened-at "2026-09-05"
 :flow "Flows/FLOW_VISION_UNIFICATION.md"
 :raised-by "власник: «гра що чимось схожа до того що я планую, але вона має більш \"класичний\" геймплей» → «проведи цей аналіз, наших механік проти того що є в механіках гри»"
 :axes [game-centre hex-is-district hex-development building tier-ladder need-is-a-slot population-dynamics
        labour-pool actor-action-capacity turn-order exchange city-supply elite-emergence elite-motivation
        mayor-instruments event-ledger]
 :scope "порівнювати все — рішення власника 2026-09-05"
 :delivered-at "2026-09-05"}
```

# Our conditions

```clojure
;; written BEFORE anything is read — an option wins only inside a regime (canon: deep-research)
{:scale "одне місто; одиниці НАЗВАНИХ еліт; анонімні робочі групи за щаблями; десятки товарів"
 :platform "Unity, покрокова гра, синглплеєр з автономними акторами"
 :turn-model "кільцевий порядок ходу форми Solium Infernum — єдиний розподільник дефіциту (GAME_VISION turn-order)"
 :player-position "мер розпоряджається всією землею і правами, сам будівель не ставить; еліти ставлять будівлі в слотах за дозволом (GAME_VISION rev 5)"
 :economy "фізичні товари; єдина форма обміну — біржа стоячих пропозицій «A за B»; монета відкладена"
 :binding-constraint "AP кожного актора — єдина вісь уваги"
 :design-goal "тераріум персонажів: історії, не оптимізація; місто — сцена й причинна система; «без наслідку немає челенджу» (§10 :unmet-need-consequence)"
 :city-tales-regime "гіпотеза до пошуку: реальний час, один актор-гравець, органічний ріст — кожна їхня механіка читається в цьому режимі"
 :budget "соло-розробник плюс агент"
 :deadline :none
 :why "форма (зона, будинок, торговий пост) лишається в них; переноситься лише правило, яке форма несе, і тільки якщо воно виживає в покроковому багатоакторному режимі з наслідками"}
```

# Prior belief

```clojure
;; записано ДО пошуку, 2026-09-05; :outcome дописано після трьох раундів
[{:hunch "City Tales — одноакторний білдер у реальному часі: гравець сам ставить будівлі і сам є єдиним економічним актором; автономних акторів із власним господарством немає"
  :confidence 60
  :grounded-in "agent knowledge only — нічого ще не прочитано"
  :at "2026-09-05"
  :outcome :survived
  :deviation "компаньйони виглядають як актори, але ними повністю керує гравець: «you can give them any job you want at any time»; це токени уваги гравця, не суб'єкти"}

 {:hunch "ріст кварталу там просторовий і органічний — будинки самі виростають навколо центру; поняття «слот під будівлю» немає"
  :confidence 55
  :grounded-in "agent knowledge only"
  :at "2026-09-05"
  :outcome :killed
  :deviation "житло справді росте органічно, але не-житлові будівлі мають ЖОРСТКИЙ слот-кеп: не більше двох на район — тобто слоти є, просто вкладені в житлову зону"}

 {:hunch "мешканці іменовані, з характерами й потребами; «tales» — це подієвий флейвор (скарги, побажання), а не наслідкова механіка, що змінює економіку"
  :confidence 50
  :grounded-in "agent knowledge only"
  :at "2026-09-05"
  :outcome :weakened
  :deviation "мешканці анонімні (це будинки); іменовані лише компаньйони (6 на старті, 10 у 1.0), і їхні «tales» — авторські квести, що ГЕЙТЯТЬ прогрес (будівлі, masterworks), але не змінюють економіку самі по собі"}

 {:hunch "потреби там плоскі (щастя / задоволення на мешканця), а не драбина щаблів, як в Anno"
  :confidence 45
  :grounded-in "agent knowledge only"
  :at "2026-09-05"
  :outcome :killed
  :deviation "драбина є: будинки тірів I–V, кожен тір вимагає цивільні будівлі свого тіру плюс вироблені товари — це Anno-подібна драбина, де половина потреб — СЕРВІСИ в радіусі, не товари"}

 {:hunch "торгівля там зовнішня — торгові шляхи за гроші; внутрішнього ринку між акторами немає"
  :confidence 55
  :grounded-in "agent knowledge only"
  :at "2026-09-05"
  :outcome :survived
  :deviation "внутрішнього ринку немає з тієї ж причини, що й акторів: актор один; зовнішня торгівля — Trade Post з паузами між угодами і лімітом 8 товарів"}]
```

# City Tales — the facts the map rests on

```clojure
;; each fact carries the sources that established it; numbers in # Sources
[{:fact :ct-regime
  :is "реальний час із прискоренням до 2x; синглплеєр; гравець — «the leader your people need»; катастроф і провалу немає"
  :sources [:s-steam :s-softpedia :s-tryhard :s-questdaily-review]}

 {:fact :ct-district
  :is "район = зона довільної форми; розмір задає число житлових ділянок; мешканці самі ділять зону на ділянки і ставлять будинки; НЕ БІЛЬШЕ ДВОХ не-житлових будівель на район (сервіс або виробництво замінює дім); межі після створення не рухаються; вид зони (район, ферма) задається при малюванні"
  :sources [:s-steam :s-cgmag :s-ladiesgamers :s-questdaily-review :s-tryhard]}

 {:fact :ct-zoning-critique
  :is "гравці: межі невидимі, лінії «як bend tool у Photoshop», видалення будівлі знищує ділянку, зони під майбутні будівлі треба резервувати наперед, інакше «forced to create a completely new farming district»; розробник (IS_Jeste) пообіцяв видалення будівлі без втрати ділянки"
  :sources [:s-steam-thread]}

 {:fact :ct-tiers
  :is "будинки тірів I–V; апгрейд вимагає цивільних будівель відповідного тіру в радіусі впливу (Well, Trading Plaza, Chapel, Performance Stage, Pharmacy, Elementary School; 18 цивільних будівель у 1.0) і вироблених товарів; цивільні будівлі мають власні тіри"
  :sources [:s-cgmag :s-tryhard :s-monstervine]}

 {:fact :ct-no-consequence
  :is "«Your citizens don't starve»; складність — спланувати ланцюги під апгрейди; «no looming crisis if you mismanage things»; «I am always fine. It is easy to slap on some Band-Aids»; більшість проблем розв'язується «building more of the same production buildings»"
  :sources [:s-tryhard :s-questdaily-review :s-cgmag]}

 {:fact :ct-gold
  :is "золото не заробляється автоматично: фіксована сума за кожен збудований або апгрейднутий дім, періодичних податків немає; Mint Workshop (masterwork) дає 1 золото за цикл; рецензент: «long stretches spent waiting for funds to roll in, with little indication of when income would increase»"
  :sources [:s-steam-guide :s-tryhard :s-impulse :s-questdaily-masterworks]}

 {:fact :ct-companion-manager
  :is "кожна виробнича будівля вимагає компаньйона-менеджера, «until it reaches its first upgrade and unlocks automation»; далі будівля автономна; вищий скіл (кеп 8) — швидша автономія і на ~35% коротший цикл; залишений компаньйон підвищує ефективність і генерує prestige; бонуси діють лише поки компаньйон у Settlement Hall; «choose whether to start a new building or leave your companions to raise their levels instead»"
  :sources [:s-cgmag :s-impulse :s-tryhard :s-steam-guide :s-questdaily-prestige :s-ladiesgamers]}

 {:fact :ct-companions
  :is "6 компаньйонів на старті, 9 в EA, 10 у 1.0; кожен з передісторією і власною лінією квестів; приходять за сюжетом; «you can give them any job you want at any time»; пізніше очолюють hamlets (сателітні поселення з новими матеріалами)"
  :sources [:s-steam :s-steam-guide :s-cgmag :s-impulse :s-tryhard :s-monstervine]}

 {:fact :ct-prestige
  :is "prestige — ресурс пізньої гри: відкривається квестом прогресу, генерується будівлями з expert level 2 по 1 за цикл, витрачається на вищі віхи квестів і на masterworks"
  :sources [:s-questdaily-prestige]}

 {:fact :ct-masterworks
  :is "11 унікальних будівель із загальноміськими бафами через зони впливу (Faith, Health, Education, Trade); відкриваються квестами, лініями компаньйонів і prestige; Hilde's Trade Post — перший masterwork"
  :sources [:s-questdaily-masterworks :s-monstervine]}

 {:fact :ct-trade
  :is "Trade Post: купівля і продаж напряму за золото, з очікуванням між угодами, до 8 товарів за раз; перше EA-оновлення — Caravan і Escorts для перевезення між поселеннями; ціни та їхня динаміка — не знайдено"
  :sources [:s-questdaily-masterworks :s-simdaily]}

 {:fact :ct-no-labour
  :is "жодне з 12 джерел не описує призначення робітників чи потребу в населенні для виробництва: будівлі йдуть від компаньйона до автономії; «Districts work automatically»; населення читається як число будинків, що платять золото за апгрейд"
  :sources [:s-softpedia :s-tryhard :s-steam-guide]
  :weakened-by "доказ від відсутності — гайд по праці не знайдено, бо, схоже, нема про що писати"}

 {:fact :ct-modes-story
  :is "режими Bard (контрольоване розміщення, легше), Settler (базовий), Advisor (важче), Painter (без обмежень і без нарації, 1.0); квести: головний прогресу, сюжетний, вторинні компаньйонів; два акти в 1.0; квести нагороджують будівлями; рецензент бачив зламані тригери квестів і неузгодженість сюжету з прогресом hamlets"
  :sources [:s-cgmag :s-simdaily :s-gematsu :s-monstervine]}

 {:fact :ct-numbers
  :is "Irregular Shapes (Франція, з 2022, перша гра; CEO Rémi Malet), видавець Firesquid, Unity; EA 2025-05-22; 1.0 2026-01-29; у 1.0: 48 ресурсів, 18 цивільних будівель, 10 компаньйонів, 3 мапи, 11 masterworks; ціна 22.99"
  :sources [:s-wikipedia :s-gematsu :s-monstervine :s-stash]}

 {:fact :ct-district-desires
  :is "Steam: «Each district has its own needs and desires, depending on the people who live there. Some citizens will love a tavern, whilst others prefer the peace of a church» — жоден гайд чи рецензія не описує це як механіку відмінних уподобань; читається як тір-залежні вимоги"
  :sources [:s-steam]
  :weakened-by "маркетингове формулювання без підтвердження в гайдах"}]
```

# Options

Thread = one axis of the model. `:relation` reads the pair: `:same` — the same rule under a different form; `:different` — the same question answered another way; `:absent` — City Tales has nothing on this axis. `:transfer` lists what could travel to us, rated; an empty vector means nothing travels.

## Thread 1 · player role and genre centre

```clojure
{:axis game-centre
 :ours "мер — один з акторів кільця; сам будівель не ставить; еліти автономні; історії мають ВИНИКАТИ з механік"
 :city-tales "гравець — єдиний актор; малює зони, обирає, які будівлі замінять доми, розставляє компаньйонів; мешканці анонімні; історії АВТОРСЬКІ — квести компаньйонів"
 :relation :different
 :this-is-the-classic "саме тут «класичний геймплей», який назвав власник: один суб'єкт рішень, усі інші — інструменти або декорації"
 :what-ct-shows "cozy-відчуття «міста з людьми» досягається малим іменованим складом (6–10) з авторськими арками навіть без автономії; але без наслідків воно вироджується у grind — три рецензії незалежно"
 :transfer [{:option "авторські арки для кількох еліт поверх emergent-журналу: квест-подібна лінія, що гейтить одну спроможність або masterwork-подібну будівлю"
             :forces "авторська історія читається без пояснень, але ламається, коли стан світу розходиться зі сценарієм (CGMag: тригери квестів спрацьовували раніше, ніж гравець дійшов)"
             :applies-when "коли emergent-журнал дає події, але не дає АРКИ — це буде видно лише на плейтесті"
             :known-uses "City Tales: компаньйони і masterworks; The Guild: династичні цілі"
             :evidence "чотири рецензії хвалять компаньйонів як носіїв глибини; одна фіксує розсинхрон квестів"
             :weakened-by "рецензії, не вимірювання; наш журнал ще не існує, порівнювати нема з чим"
             :confidence 40
             :buys "читабельну ціль на еліту без алгоритму мотивації"
             :cost-to-build "сценарний шар + тригери на подіях журналу"
             :cost-to-adopt "n/a — форми немає"
             :reversibility :two-way}]}
```

## Thread 2 · hex, district, slots

```clojure
{:axis #{hex-is-district hex-development}
 :ours "гекс = район; спеціалізація призначається і еволюціонує діями розвитку; спеціалізований гекс відкриває N слотів під будівлі еліт; зміна спеціалізації OPEN (:specialization-switch)"
 :city-tales "зона довільної форми; вид зони фіксується при малюванні; мешканці самі ставлять доми; ≤2 не-житлові будівлі на район; межі не рухаються; видалення будівлі знищує ділянку"
 :relation :same
 :the-rule-behind "обмежене число не-житлових слотів на одиницю землі — у них 2 на район, у нас N за спеціалізацією; обидві моделі роблять землю казною через слоти"
 :what-ct-shows "найгучніша скарга гравців — НЕРУХОМІСТЬ: не можна перезонувати, розширити, повернути ділянку; «nothing will stop me playing faster than wasted time»; розробник відступив і пообіцяв повернення ділянки"
 :transfer [{:option "еволюція гексу ДОДАЄ слоти, а зміна спеціалізації дозволена як дія розвитку (закриває :specialization-switch на «так»)"
             :forces "гнучкість проти ваги рішення: якщо слот завжди можна повернути, спеціалізація перестає бути вибором"
             :applies-when "покроково, де кожна дія коштує AP і ходів — ціна вже є, тож гнучкість не знецінює вибір"
             :known-uses "City Tales — негативний контроль: фіксовані межі і втрата ділянки"
             :evidence "Steam-дискусія з кількома гравцями і відповіддю розробника; Quest Daily окремо називає нерухомі межі мінусом"
             :weakened-by "скарги на UX зонування в реальному часі; наша одиниця — гекс, у нас нема проблеми ліній"
             :confidence 70
             :buys "знімає клас скарг «перебудовуй усе», який у CT був причиною рестартів"
             :cost-to-build "одна дія розвитку «змінити спеціалізацію» з правилом, що стається з будівлями еліт у слотах — це нове OPEN"
             :cost-to-adopt "n/a"
             :reversibility :two-way}
            {:option "зайнятий слот звільняється, коли будівля зникає — ділянка не гине разом із будівлею"
             :forces "нічого — це правило-гігієна"
             :applies-when "завжди"
             :known-uses "City Tales після виправлення"
             :evidence "обіцянка розробника у відповідь на скаргу"
             :weakened-by "тривіально; записано, щоб не повторити помилку"
             :confidence 90
             :buys "уникнення відомої помилки"
             :cost-to-build "нуль — уже випливає з «слот під будівлею зайнятий, поки будівля стоїть»"
             :cost-to-adopt "n/a"
             :reversibility :two-way}]}
```

## Thread 3 · building: who builds, who gets the yield

```clojure
{:axis #{building world-is-inert}
 :ours "будівлю в слоті ставить еліта за дозволом; пасив щохода йде місту без праці й AP; еліта витрачає власне AP на дії на будівлі і забирає вихід"
 :city-tales "будівлю ставить гравець; компаньйон-менеджер потрібен до першого апгрейду, далі будівля автономна; весь вихід — гравцю; залишений компаньйон дає ефективність і prestige"
 :relation :same
 :the-rule-behind "одноразова інвестиція уваги → сталий пасив без уваги; додаткова увага на тій самій будівлі → додатковий вихід. У них компаньйон = увага, у нас AP еліти = увага"
 :what-ct-shows "правило працює як основний ритм гри: «choose whether to start a new building or leave your companions to raise their levels» — це рівно наше «еліта: нова будівля чи дія на наявній»; рецензенти називають цей цикл задовільним після виправлення темпу"
 :transfer [{:option "пасив будівлі починається не одразу, а після періоду «запуску», який вимагає AP еліти — як компаньйон до першого апгрейду"
             :forces "додає стан будівлі («запускається») проти простоти «збудував — пасив пішов»; дає еліті причину бути присутньою"
             :applies-when "якщо на плейтесті будівлі ставляться і забуваються, а еліти нічого не роблять на них"
             :known-uses "City Tales"
             :evidence "шість джерел узгоджено описують менеджера-до-автономії"
             :weakened-by "у CT це компенсує відсутність інших джерел напруги; у нас напруга є з праці й кільця"
             :confidence 35
             :buys "видиму присутність еліти на будівлі в перші ходи"
             :cost-to-build "один стан + правило переходу"
             :cost-to-adopt "n/a"
             :reversibility :two-way}]
 :confirms "rev 5 (def building :passive) — форма «пасив без праці» вже випробувана в shipped-грі й читається гравцями"}
```

## Thread 4 · tiers and needs

```clojure
{:axis #{tier-ladder need-is-a-slot construction-goods}
 :ours "щабель = 3 успадковані + 3 нові клітинки потреб; клітинка закривається ТОВАРОМ з умовою (категорія, рівень якості); будівельні товари — окрема модельна категорія на HUD"
 :city-tales "тіри I–V; апгрейд дому вимагає цивільних будівель свого тіру в радіусі впливу ПЛЮС вироблених товарів; цивільні будівлі мають власні тіри; 18 цивільних будівель у 1.0"
 :relation :same
 :the-rule-behind "драбина Anno в обох; відмінність — половина потреб CT це СЕРВІС у радіусі (колодязь, каплиця, школа), а в нас потреба завжди товар"
 :what-ct-shows "потреба-як-будівля з'їдає слот району (їх 2) — тобто сервіси конкурують із виробництвом за слоти; це і є їхня головна просторова головоломка"
 :transfer [{:option "клітинка потреби фази 2 може закриватися ВПЛИВОМ будівлі еліти (каплиця, лазня), не лише товаром — умова клітинки стає #{товар вплив}"
             :forces "сервіс не витрачається, тож клітинка закрита назавжди — це ламає споживання як out-трубу; має бути окремий тип клітинки, не заміна товару"
             :applies-when "коли з'являться еліти й будівлі, і коли треба дати еліті будівлю без товарного виходу"
             :known-uses "City Tales; Anno 1800 (громадські будівлі як потреби)"
             :evidence "три рецензії описують радіусні сервіси як умову тіру"
             :weakened-by "у CT це працює, бо нема споживання; у нас споживання — суть петлі"
             :confidence 45
             :buys "тип будівлі для еліт поза виробництвом; масштабує §5 :phase-2"
             :cost-to-build "новий тип умови клітинки + радіус на мапі гексів"
             :cost-to-adopt "n/a"
             :reversibility :two-way}]
 :confirms "D1 і §5 (def tier-ladder) — драбина тірів читається гравцями cozy-жанру без пояснень"}
```

## Thread 5 · population dynamics and consequence

```clojure
{:axis #{population-dynamics city-supply unmet-need-consequence}
 :ours "потреби щабля закриті → групи ростуть; не закриті → падають; місто їсть зі свого пулу щохода; «без наслідку немає челенджу»"
 :city-tales "населення = будинки в зонах; росте з розміром зон і апгрейдами; «Your citizens don't starve»; падіння немає; катастроф немає"
 :relation :different
 :the-rule-behind "CT свідомо прибрав out-трубу: товари лише ВХОДИ апгрейду, не споживання; тому немає ні дефіциту, ні падіння"
 :what-ct-shows "ціна цього вибору виміряна рецензентами незалежно: «no downside management at all», «I am always fine», «difficulty is almost trivial», середина гри — grind між віхами. Це негативний контроль для D8"
 :transfer []
 :confirms "D8 (def population-dynamics :decline) і §10 :unmet-need-consequence :carries — CT показує, ЩО саме зникає, коли падіння немає: напруга і привід повертатися до міста"}
```

## Thread 6 · labour

```clojure
{:axis #{labour-pool work-group category-non-interchange}
 :ours "типізовані групи за щаблем — спільний дефіцитний ресурс; дії мера і еліт беруть з одного пулу; кільце ділить"
 :city-tales "механіки праці не знайдено: будівлі не потребують робітників; єдина «праця» — компаньйон-менеджер; населення не працює"
 :relation :absent
 :what-ct-shows "гра відвантажилась без праці взагалі — праця не є обов'язковою для читабельної економіки cozy-жанру; але водночас ЇЇ відсутність — одна з причин «завжди все добре»"
 :transfer []
 :implication "наша праця має ОКУПАТИСЯ напругою (конкуренція за групи в кільці), інакше це складність без віддачі — CT доводить, що без неї можна; RESEARCH_INTERMEDIATE вже фіксувало, що без ціни арбітром лишається порядок ходу"
 :weakened-by "доказ від відсутності в 12 джерелах"}
```

## Thread 7 · attention as the binding constraint

```clojure
{:axis #{actor-action-capacity elite-AP dual-progression}
 :ours "AP — єдина вісь уваги кожного актора; дія вимагає AP щохода; тривала дія забирає AP, доки триває"
 :city-tales "компаньйони (6–10) — токени уваги: будівля не стартує без компаньйона; токен ВІДПУСКАЄТЬСЯ, коли будівля стала автономною; залишений токен дає бонус; скіл компаньйона (1–8) — це ріст спроможності токена"
 :relation :same
 :the-rule-behind "обмежене число токенів уваги, які треба РОЗСТАВЛЯТИ, і які повертаються після інвестиції — це AP, розкладене в просторі замість лічильника"
 :what-ct-shows "форма токена-на-мапі читається гравцями cozy-жанру без пояснень; конфлікт «нова будівля чи прокачка» — центральне рішення гри за рецензіями; дуальна прогресія у них теж є: токен росте (скіл), місто росте (тіри)"
 :transfer [{:option "показувати AP еліти не лічильником, а ТОКЕНАМИ на її будівлях: де стоїть еліта — там її дія"
             :forces "видимість проти абстракції: токен на будівлі не покриває дії без будівлі (біржа, політика)"
             :applies-when "у спеці аркуша гекса й у r-standing — це питання UI, не моделі"
             :known-uses "City Tales; worker placement у настільних іграх (RESEARCH у FLOW_BUILD_UX_DECONGESTION)"
             :evidence "чотири рецензії описують розстановку компаньйонів як головний цикл"
             :weakened-by "UI-висновок з рецензій, без вимірювань; GAME_VISION §9 :action-author вже вимагає «чий AP» на дії"
             :confidence 55
             :buys "читання «хто де працює» без таблиці"
             :cost-to-build "лише спека; модель не змінюється"
             :cost-to-adopt "n/a"
             :reversibility :two-way}]
 :confirms "rev 5 (def actor-action-capacity :attention-axis) і (def elite-capabilities :axes AP) — токен уваги, що росте у спроможності, вже є shipped-формою"}
```

## Thread 8 · turn order

```clojure
{:axis turn-order
 :ours "кільце Solium Infernum — єдиний розподільник дефіциту між акторами"
 :city-tales "реальний час, 1x–2x; актор один; розподіляти нема між ким"
 :relation :absent
 :transfer []
 :implication "кільце існує лише тому, що акторів кілька; CT нічого не додає і нічого не спростовує"}
```

## Thread 9 · exchange, gold and outside trade

```clojure
{:axis #{exchange economic-authority city-supply intercity-trade}
 :ours "біржа стоячих пропозицій між акторами; монета відкладена; міжміська торгівля OPEN"
 :city-tales "золото — фіксована сума за збудований або апгрейднутий дім, податків немає; Trade Post — зовнішня купівля/продаж за золото, з очікуванням між угодами, до 8 товарів; внутрішнього ринку немає"
 :relation :different
 :the-rule-behind "у CT гроші — Anno-подібна декорація «з повітря»: власник сам назвав цю форму прийнятною 2026-09-02; але CT їх прив'язав до ЗРОСТАННЯ, не до продажу — рецензент не розумів, коли й чому приходять гроші"
 :what-ct-shows "непрозорість ПРИХОДУ ресурсу — окрема скарга, навіть у cozy-грі; для нашої біржі це означає: момент заповнення лота і його причина мають бути видимі (§9 :attention, :ledger уже це вимагають)"
 :transfer [{:option "міжміська торгівля як «Trade Post»: зовнішній контрагент, стоячий, з КАДЕНЦІЄЮ (раз на K ходів) і лімітом лотів за раз — той самий шаблон пропозиції, що й біржа"
             :forces "зовнішній контрагент з нескінченним запасом ламає world-is-inert; ліміт і каденція — саме те, що тримає його скінченним"
             :applies-when "коли відкривається §10 :intercity-trade; каденція природно лягає на ходи"
             :known-uses "City Tales (очікування між угодами, 8 товарів); Patrician (локальний ринок міста)"
             :evidence "два джерела на ліміт і паузу; ціни не знайдено"
             :weakened-by "ціноутворення CT невідоме; у нас монети ще немає, тож контрагент мусить брати товар за товар"
             :confidence 50
             :buys "форму для :intercity-trade без нового шаблону"
             :cost-to-build "один зовнішній актор із власною книгою пропозицій і каденцією"
             :cost-to-adopt "n/a"
             :reversibility :two-way}]}
```

## Thread 10 · elite emergence, capabilities, motivation

```clojure
{:axis #{elite-emergence elite-capabilities elite-motivation}
 :ours "еліту породжує окрема автономна механіка (OPEN); розвиток = слоти, AP, бонуси; мотивація — власне життя; вибір OPEN"
 :city-tales "компаньйони приходять за СЮЖЕТОМ (віхи квесту прогресу), не з населення; спроможність = скіл 1–8 за ремеслом → швидша автономія, коротший цикл, prestige; мотивації немає — виконують будь-яку роботу; одна квестова примха (Lewellin просить гроші й повертає)"
 :relation :different
 :the-rule-behind "CT відповідає на :elite-emergence-mechanic найпростішим способом — авторським списком, що відкривається віхами; і на :elite-capabilities-content — однією шкалою скілу, яка МНОЖИТЬ вихід, а не відкриває нові дії"
 :transfer [{:option "поява еліти за віхами міста (щабель N досягнуто → приходить еліта ремесла N) — авторський список замість генерації з населення"
             :forces "передбачуваність проти емерджентності: список читається і балансується, генерація дає історії"
             :applies-when "для першого плейтесту, поки :elite-roster OPEN — як тимчасове правило"
             :known-uses "City Tales; більшість cozy-білдерів з «радниками»"
             :evidence "чотири джерела: компаньйони приходять сюжетом"
             :weakened-by "у CT це і є весь механізм; для нас це тільки заглушка до емерджентної механіки"
             :confidence 45
             :buys "закриває вихід із базової фази для плейтесту без нової механіки"
             :cost-to-build "таблиця віха → еліта"
             :cost-to-adopt "n/a"
             :reversibility :two-way}
            {:option "рівень спроможності еліти скорочує ТРИВАЛІСТЬ її дій (ходи), а не лише додає слоти й AP"
             :forces "третя вісь спроможності проти простоти двох; але у покроковій грі скорочення ходів читається краще, ніж +% виходу"
             :applies-when "коли заповнюється §10 :elite-capabilities-content"
             :known-uses "City Tales: скіл 8 → цикл на ~35% коротший"
             :evidence "одне джерело з числом"
             :weakened-by "одне джерело; число з гайду, не з патч-ноутів"
             :confidence 40
             :buys "видиму різницю між молодою і зрілою елітою на тій самій будівлі"
             :cost-to-build "поле «тривалість» стає функцією спроможності"
             :cost-to-adopt "n/a"
             :reversibility :two-way}]}
```

## Thread 11 · mayor's instruments and politics

```clojure
{:axis #{mayor-instruments political-actor}
 :ours "дозвіл на слот, пропозиція на біржі, політика; наказу еліті немає; політичні актори з важелями"
 :city-tales "інструментів немає — гравець будує сам; найближче до «політики» — masterworks: унікальні будівлі із ЗАГАЛЬНОМІСЬКИМ впливом (Faith, Health, Education, Trade), відкриті квестами й prestige"
 :relation :absent
 :the-rule-behind "загальна умова, що змінює всіх одразу, у CT прив'язана до БУДІВЛІ — те саме, що RESEARCH_MEDIEVAL нитка 7 зафіксувала історично: будівля розблоковує інституцію"
 :transfer [{:option "політика мера має носія-будівлю на міському гексі (ратуша, суд, ринок): без будівлі немає політики цього виду"
             :forces "політика як окрема поверхня проти політики як наслідку землі; носій робить політику видимою на мапі і вразливою (слот зайнятий)"
             :applies-when "коли поверхня політики буде виводитися; підтримується двома незалежними прецедентами — CT і історичним"
             :known-uses "City Tales masterworks; історичні комуни (нитка 7)"
             :evidence "гайд по masterworks + історична нитка"
             :weakened-by "у CT це ендгейм-нагорода, не інструмент; перенос — правило, не форма"
             :confidence 55
             :buys "місце політики на мапі і ціну для неї (слот, будівельні товари)"
             :cost-to-build "тип будівлі «інституція» на міському гексі, яку ставить МІСТО — це ламає «мер будівель не ставить» (D4) і потребує окремого рішення"
             :cost-to-adopt "n/a"
             :reversibility :one-way   ;; торкається D4
             :note "^:risky — суперечить D4 «мер будівель не ставить»; обговорити до будь-якої дії"}]}
```

## Thread 12 · stories and the ledger

```clojure
{:axis #{event-ledger causal-story-loop}
 :ours "історія = журнал наслідкових подій з ECS-пульсів; на родину; емерджентна"
 :city-tales "історія авторська: головний квест, сюжетний квест, квести компаньйонів; два акти; нагорода — будівлі й masterworks; рецензент: тригери спрацьовували до того, як гравець дійшов; сюжет не знав про стан hamlets"
 :relation :different
 :the-rule-behind "авторська арка дає ціль і тон, але живе окремо від стану світу — тому розсинхронюється; журнал не розсинхронюється, але не дає арки"
 :what-ct-shows "клас помилок авторського шару у симуляції з вільним порядком дій — задокументований у рецензії 1.0, не в EA"
 :transfer []   ;; опція гібриду вже стоїть у нитці 1 (40)
 :confirms "вибір журналу як носія історії — CT показує ціну альтернативи; але також показує, що cozy-гравці ЦІНУЮТЬ авторську арку компаньйона — журнал мусить давати читабельну арку, інакше це лог"}
```

# Disconfirmation

```clojure
{:target "нитка 7 (70 у синтезі): «компаньйон CT = токен уваги, який відпускається після автономії будівлі — та сама форма, що наш AP і пасив після інвестиції»"
 :searched-for "чи потребують будівлі CT РОБІТНИКІВ з населення — тоді обмеженням була б праця, а не увага, і аналогія з AP розсипалась би; шукав гайди по workforce/jobs, Steam-обговорення про робітників, рецензії на слово «workers»"
 :came-back "жодного джерела про робітників; Try Hard прямо: «Your citizens don't starve» і «each new building must have a companion assigned… until it… unlocks automation»; Softpedia: «Districts work automatically»; гайд: «Assigning them to a building will eventually lead to an automated production line»"
 :outcome :survived
 :weakened-by "доказ від відсутності; пошук по Steam-обговореннях з фільтром домену не дав результатів, reddit недоступний"}
```

```clojure
{:target "нитка 5: «відсутність падіння населення — причина grind-у, отже D8 :decline потрібне»"
 :searched-for "рецензію, яка хвалить CT саме за відсутність наслідків як свідомий дизайн, що ПРАЦЮЄ — тоді D8 було б смаком, не необхідністю"
 :came-back "усі три рецензії з думкою про складність (CGMag, Quest Daily, Try Hard) називають відсутність наслідків мінусом або «майже тривіальною» складністю; Softpedia і LadiesGamers хвалять cozy-тон, але не відсутність наслідків як таку; Galaxus («mixed feelings») не відкрився двічі"
 :outcome :survived
 :weakened-by "рецензенти жанру можуть систематично хотіти більше челенджу, ніж cozy-аудиторія; вимірювань утримання гравців немає"}
```

# Verdict

```clojure
{:recommends "нічого не імпортувати цілком; модель rev 5 не змінюється цим документом"
 :because "City Tales — та сама драбина тірів і той самий слот-кеп на землю, але з ОДНИМ актором, без праці, без споживання і без наслідків; усе, що в нас відрізняється (кільце, еліти, біржа, падіння), відрізняється саме там, де рецензенти CT бачать порожнечу"
 :confirmed-by-ct #{"D8 падіння населення — негативний контроль (нитка 5)"
                    "слоти на землі як казна — shipped-форма (нитка 2)"
                    "пасив без праці після інвестиції уваги — shipped-форма (нитка 3)"
                    "увага як несуча вісь, токен росте у спроможності — shipped-форма (нитка 7)"}
 :warned-by-ct #{"нерухомість зон породжує рестарти — еволюція має додавати, а не вимагати зносу (нитка 2, 70)"
                 "прихід ресурсу без видимої причини — окрема скарга навіть у cozy-грі (нитка 9)"
                 "авторський сюжет розсинхронюється зі станом — ціна альтернативи журналу (нитка 12)"}
 :options-worth-a-decision [{:for :specialization-switch :option "еволюція додає слоти, зміна спеціалізації дозволена як дія" :confidence 70}
                            {:for :elite-emergence-mechanic :option "поява за віхами як заглушка для плейтесту" :confidence 45}
                            {:for :elite-capabilities-content :option "спроможність скорочує тривалість дій" :confidence 40}
                            {:for :intercity-trade :option "зовнішній контрагент з каденцією і лімітом лотів" :confidence 50}
                            {:for "need-is-a-slot :phase-2" :option "клітинка закривається впливом будівлі" :confidence 45}
                            {:for "поверхня політики" :option "політика з носієм-будівлею" :confidence 55 :note "^:risky — торкається D4"}]
 :evidence-bar "усі двері двосторонні, крім носія політики (торкається D4); поріг доказів низький, кожне твердження про гру має джерело"
 :no-verdict "рішення по кожній опції — власника; це мапа, не план"}
```

# Sources

```clojure
[{:id :s-steam :source "Steam store page — City Tales - Medieval Era (app 3265070)"
  :established "зонування без сітки; «each district has its own needs and desires»; 9 компаньйонів, що тренують виробництво до автономії; «the leader your people need»"
  :kind :documentation :at "2026-09-05"}
 {:id :s-wikipedia :source "Wikipedia — City Tales: Medieval Era (stub)"
  :established "Irregular Shapes / Firesquid / Unity / EA 2025-05-22 / синглплеєр"
  :kind :documentation :at "2026-09-05"}
 {:id :s-cgmag :source "CGMagazine — review (1.0)"
  :established "розмір району → число домів; ≤2 сервісні будівлі на район; тіри I–V і список цивільних будівель; компаньйон як обов'язковий менеджер, автономія з часом, бонус лише в Settlement Hall; 5 hamlets; режими Bard/Settler/Advisor/Painter; «I am always fine»; зламані тригери квестів"
  :kind :practitioner-report :at "2026-09-05"}
 {:id :s-softpedia :source "Softpedia — review (PC)"
  :established "«Districts work automatically»; будівлі стають автоматичними; прискорення лише до 2x; «a little more control during district management» як мінус"
  :kind :practitioner-report :at "2026-09-05"}
 {:id :s-impulse :source "Impulse Gamer — review (PC)"
  :established "≤2 цивільні будівлі на район; кожна виробнича будівля потребує одного компаньйона спочатку; скіли до 8; hamlets; «long stretches spent waiting for funds… little indication of when income would increase»"
  :kind :practitioner-report :at "2026-09-05"}
 {:id :s-questdaily-review :source "Quest Daily — review «Medieval Mindfulness»"
  :established "межі районів не рухаються; компаньйони тренують «apprentices»; «production aligns neatly with consumption»; prestige у середині-кінці; «no looming crisis», «almost no downside management»; grind у середині"
  :kind :practitioner-report :at "2026-09-05"}
 {:id :s-tryhard :source "Try Hard Guides — review «From Small to Sprawl»"
  :established "зони з пінами, вид зони при малюванні; апгрейд дому = товари + радіус амening; золото — фіксована сума за дім/апгрейд, без податків; «Your citizens don't starve»; компаньйон до першого апгрейду → автоматизація; «difficulty is almost trivial»"
  :kind :practitioner-report :at "2026-09-05"}
 {:id :s-ladiesgamers :source "LadiesGamers — review"
  :established "≤2 не-житлові будівлі на район; розробник розширив радіус впливу після скарг; «choose whether to start a new building or leave your companions to raise their levels»; 6 стартових компаньйонів"
  :kind :practitioner-report :at "2026-09-05"}
 {:id :s-steam-guide :source "Steam Community guide — «5 tips to start City Tales»"
  :established "«You don't earn gold automatically»; малі райони (5–6 ділянок) легше довести до високих тірів; «give them any job you want at any time»; прокачка через цикли виробництва"
  :kind :practitioner-report :at "2026-09-05"}
 {:id :s-steam-thread :source "Steam discussion — «The District Zoning needs a Complete Overhaul» + відповідь IS_Jeste"
  :established "три скарги на зонування; рестарти через нерухомість зон; розробник пообіцяв видалення будівлі без втрати ділянки"
  :kind :confirmed-answer :at "2026-09-05"}
 {:id :s-questdaily-masterworks :source "Quest Daily — «Every Masterwork and How to Unlock Them»"
  :established "11 masterworks; зони впливу Faith/Health/Education/Trade; Trade Post: купівля/продаж напряму, паузи між угодами, ≤8 товарів; Mint 1 золото/цикл; Journeyman's Hall; відкриття лініями компаньйонів і prestige"
  :kind :practitioner-report :at "2026-09-05"}
 {:id :s-questdaily-prestige :source "Quest Daily — «Prestige Explained»"
  :established "prestige з expert level 2 по 1/цикл; скіл 8 → цикл на ~35% коротший; три стадії будівлі з вимогами до рівня компаньйона"
  :kind :practitioner-report :at "2026-09-05"}
 {:id :s-simdaily :source "Simulation Daily — EA roadmap"
  :established "на старті EA: 2 режими, 9 компаньйонів, 50+ будівель, 40 ресурсів, 4 тіри, 2 мапи; перше оновлення — Caravan і Escorts; далі Castle, Wonders, Walls, Trading, Bridges"
  :kind :documentation :at "2026-09-05"}
 {:id :s-gematsu :source "Gematsu — 1.0 launch date (2025-11)"
  :established "1.0 2026-01-29; Painter Mode; два акти; 11 masterworks; 10 компаньйонів; 3 мапи; 48 ресурсів; 18 цивільних будівель"
  :kind :documentation :at "2026-09-05"}
 {:id :s-monstervine :source "MonsterVine — 1.0 launch"
  :established "ті самі числа 1.0 + цитата Rémi Malet; ціна 22.99"
  :kind :documentation :at "2026-09-05"}
 {:id :s-stash :source "ICO Partners media kit — fact sheet"
  :established "Firesquid / Irregular Shapes / 2026-01-29 / «Forsake rigid grids…» / 9 радників"
  :kind :documentation :at "2026-09-05"}
 {:id :s-not-opened :source "PC Gamer (обидві статті), Steam news (EA-анонс, 1.0-ноутси), Games Press (401), Galaxus (двічі timeout), reddit (заблокований для агента)"
  :established "нічого — названі, щоб наступний пас не шукав їх знову тим самим шляхом"
  :kind :agent-knowledge :at "2026-09-05"}]
```

# Search log

```clojure
[{:round 1
  :queries ["\"City Tales\" \"Medieval Era\" game features districts citizens"
            "\"City Tales: Medieval Era\" Steam early access release"
            "\"City Tales Medieval Era\" review gameplay mechanics needs production trade"
            "\"City Tales\" Medieval Era developer interview dev diary organic city growth characters stories"]
  :added "каркас: розробник, дати, зони, ≤2 будівлі, компаньйони до автономії, 4 тіри, 40+ ресурсів, без катастроф"
  :new-options 0 :new-evidence 9
  :at "2026-09-05"}
 {:round 2
  :queries ["Steam store, Wikipedia, PC Gamer launch, CGMag, GameLuster, Softpedia, Steam EA news (fetch)"
            "\"City Tales\" Medieval Era trade merchant trading post caravan gold money mechanics"
            "\"City Tales\" Medieval Era citizens needs desires happiness tiers upgrade homes guide"
            "\"City Tales\" Medieval Era workers jobs workforce unemployed assign buildings guide"
            "\"City Tales\" Medieval Era Bard mode Settle mode difference story quests companions apprentice"]
  :added "тіри I–V і цивільні будівлі; Settlement Hall; hamlets; режими; Trade Post (Hilde, 8 товарів, паузи); золото не автоматичне; 1.0 2026-01-29; пошук по праці дав лише історичні статті"
  :new-options 4 :new-evidence 8
  :at "2026-09-05"}
 {:round 3
  :queries ["Steam guide, Steam zoning thread, Galaxus, LadiesGamers, Simulation Daily, Games Press, Firesquid, Quest Daily masterworks, Impulse Gamer, Quest Daily review (fetch)"
            "villagers consume food… shortage" "how to earn gold taxes income" "companion manager Settlement Hall autonomous" "1.0 launch patch notes"
            "Galaxus retry, Try Hard Guides, Quest Daily prestige, Gematsu, MonsterVine, PC Gamer preview, ICO media kit (fetch)"
            "villagers workers site:steamcommunity.com" "Trade Post Hilde prices" "influence Faith Health Education" "food consumption hunger shortage"]
  :added "«citizens don't starve»; золото — фіксована сума за дім, без податків; prestige; скіл 8 → −35% циклу; скарги на зонування з відповіддю розробника; числа 1.0"
  :new-options 2 :new-evidence 7
  :saturation "раунд дав докази, але не нові опції; ціни Trade Post і механіка «desires» не знайшлись — лишено як ?"
  :at "2026-09-05"}]
```
