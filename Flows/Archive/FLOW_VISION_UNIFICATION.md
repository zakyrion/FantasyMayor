---
category: A
read: archive
status: implemented
tags: [flow, game-design, vision, ui, spec, main-screen]
related:
  - "[GAME_MECHANICS](../../GAME_MECHANICS.md)"
  - "[FLOW_BUILD_UX_DECONGESTION](FLOW_BUILD_UX_DECONGESTION.md)"
  - "[FLOW_BASIC_CITY_UI](FLOW_BASIC_CITY_UI.md)"
---

# FLOW_VISION_UNIFICATION

One vision: close the mechanic-level conflicts in GAME_VISION rev 4, then derive ONE main-screen spec from it.

# 1 · Request

## User request — 2026-09-04 (verbatim, via /sdd-flow:resume)

> Проаналізуй зміни механік, специфікації головного вікна, там їх зараз дві і з них треба буде щось одне зліпити.
> Цікавить детальний, багатокроковий аналіз

## Answers to the agent's three questions — 2026-09-04 (verbatim)

> 1 - поки що тут
> 2 - GAME_VISION rev 3 - ок
> 3 - зараз плейтесту немає і я хочу зрозуміти що саме взагалі є сенс брати в розробку для плейтесту

## User request — 2026-09-04 (verbatim; turns the analysis into a mutation task)

> Це треба все упорядкувати в єдине бачення, бо зараз там багато конфліктів і взаємних посилань. Головне виправити конфлікти на рівні ігрових механік, UI це вже похідна

## Owner's answers to decisions D1–D10 — 2026-09-05 (verbatim)

> D1 "варіант з Anno"
> D2 "у района можуть бути певні потреби: тип терейну, якийсь ресурс, тощо. А дії частково розблоковуються районами, частково будівлями і еліти можуть мати свої унікальні дії. Але тут краще сприймати це реверсивно. Що кожна дія має якусь свою умову для її виконання."
> D3 "це спеціалізація гексу, розвиток гексу, його еволюція"
> D4 "давай віддамо це елітам, а там подивимося"
> D5 "що відбувається. Деталізацію можна буде обдумати"
> D6 "наказ це окремий вид політичних дій, які не стосуються гексів"
> D7 "так, можна будівельні товари"
> D8 "це точно зростання груп кількістно, це мінімальний режим, а далі подивимося. Якщо потреби не задоволені то падіння"
> D9 "up to you"
> D10 "та можеш видалити і макети і спеку"

## GO — 2026-09-05 (verbatim)

> go

The GO answered a message that asked for three things at once: permission to rewrite GAME_VISION, the FLOW home
(new FLOW recommended at 65) and the list of mockups to delete (the three main-screen renders). All three are read
as granted by that GO.

## Agent restatement — confirmed by the user

```clojure
[{:task :unify-mechanics
  :kind :mutation
  :goal "одне бачення без внутрішніх конфліктів: GAME_VISION rev 4 закриває розбіжності між моделлю, двома спеками і кодом на рівні механік"
  :where #{GAME_VISION.md GLOSSARY.md Flows/}
  :off-limits #{код Unity UI_LANGUAGE.md "RESEARCH-доки: лише читання" design-mockups/}
  :decided #{"rev 3 лишається основою" "гроші, наслідок незакритої потреби, мотивація еліт не чіпаються"}
  :do "рішення D1–D9 → записати як confirmed із датою і підставою; агентські :proposed блоки або підтвердити, або звести в §10"
  :skip #{"нові механіки" "баланс і числа" "код"}
  :permission "GO 2026-09-05 у відповідь на явний запит дозволу"
  :result "GAME_VISION rev 4: кожен пункт D1–D9 має одну відповідь; жодного :proposed поза §10"
  :accept [{:meter "python3 Tools/doc_lint.py --quiet" :target "0 ghosts, 0 syntax"}
           {:meter "grep GAME_VISION UI_LANGUAGE.md" :target 0}]}

 {:task :derive-main-screen-spec
  :listen :unify-mechanics
  :kind :mutation
  :goal "одна спека головного екрана, виведена з rev 4; базовий зріз як фаза, еліти як пізніша фаза"
  :where #{UISpecs/ main_UI_specification.md INDEX.md Flows/}
  :off-limits #{код Unity UI_LANGUAGE.md "перемальовування макетів"}
  :decided #{"жанр A: власники фактів, геометрія вільна для макета" "спека джерело, макет рендер"}
  :do "A поглинає B; піксельна геометрія B у нотатки рендера; кожен факт B дістає id; взаємні посилання знімаються; B і pointer ідуть за fate-правилами DOC_STANDARD"
  :skip #{"новий макет" "спека аркуша гекса" "спека угод"}
  :name "UISpecs/UISPEC_MAIN_SCREEN.md лишається іменем єдиної спеки — за naming policy UISPEC_<SUBJECT>"
  :result "один файл у UISpecs/, INDEX перегенеровано, 0 битих посилань на знятий файл"
  :accept [{:meter "python3 Tools/gen_index.py" :target "lint clean, 0 broken links"}
           {:meter "grep -rl main_UI_specification --include=*.md . поза Archive" :target "лише історичні згадки всередині code fences у FLOW-записах; жодного живого посилання"}]}]
```

## Amendments (append-only)

```clojure
[{:received-at "2026-09-05, після rev 4"
  :raw-request ["Вношу зміну в механіку, в цілому вона має бути в дусі середніх віків:"
                "Нехай всіма гексами володію я як мер. І дії над гексами можу робити я, як мер."
                "Кожен же спеціалізований гекс матиме кілька слотів під будівлі."
                "Будівлі будуть будувати еліти з мого дозволу."
                "Для ферми це можуть бути поля наприклад."
                "Кожен будинок буде давати якийсь пасив своєму власнику. "
                "Від кожного будинку місто теж буде отримувати якийсь свій пасив."
                "На своїх будинках еліти можуть витрачати свої AP для різних дій. "
                "Основна ціль мене як мера - балансувати міським ринком і домовлятися з елітами щоб задовольнити потреби міста"
                "От єдине що я поки що собі не можу скласти в голові, це як саме мені цим ринком балансувати. А так виходить доволі зрозуміли механіка."]
  :normalized "кандидат rev 5: слоти під будівлі на спеціалізованому гексі; будівлі еліт за дозволом; пасив власнику і місту; дії еліт на будівлях; ціль мера — ринок і угоди; ринок відкритий"
  :confirmed true}

 {:received-at "2026-09-05"
  :raw-request ["основна економічна проблема ось яка:"
                "населення міста споживає товари. В реальному житті це завжди був обмін праці на якусь форму оплати. Але в мене немає цієї механіки. Населення ніхто не годує окрім міста. Тобто в нас є out труба, але немає розуміння як створити сталу in трубу, бо вона не одна, це серія незалежних акторів."]
  :normalized "постановка: in-труба міста мусить бути сталою попри те, що виробники — незалежні актори"
  :confirmed true}

 {:received-at "2026-09-05"
  :raw-request "Тоді можна зробити так, що пасив завжди йде лише місту, а еліти на своїх будинках можуть додатково робити дії які будуть давати прибуток їм. І ці дії будуть брати населення з пулу як працівників. Тоді це теоретично розвʼязує багато питань"
  :normalized "пасив будівлі — лише місту; дії еліт на будівлях — прибуток еліті, праця з міського пулу"
  :confirmed true}

 {:received-at "2026-09-05"
  :raw-request ["Мер може встановлювати ціну обміну. Або як бартерну, або потім грошову, як окрема механіка. Це якраз буде балансувати ринок та споживання. Я як мер зможу виставити якусь кількість товару на ринок і вказати ціну за неї. Отримуємо такий собі спрощений, але доволі функціональний ринок."
                "Щодо пасиву - нехай буде повністю пасивний. Тут я бачу наступний вихід, будинки привʼязані під еліти. А мер еліти не створює, це буде окрема, автономна ігрова механіка. "
                "А еліти будуть змушені розвивати себе. Бо їх розвиток має бути тоді привʼязаним до їх спроможностей. Слотів під те скільки будівель вони можуть мати, кількість AP, можливо ще якісь пасивні бонуси. "
                "Чимось на Guild 2 схоже, тільки покроково і з іншої сторони_"]
  :normalized "ринок = лістинги мера з кількістю і ціною, бартер зараз, монета пізніше; пасив безумовний; будівлі прив'язані до еліт; еліт породжує окрема автономна механіка; розвиток еліти = спроможності (слоти, AP, бонуси)"
  :confirmed true}

 {:received-at "2026-09-05"
  :raw-request ["Щодо купівлі містом:"
                "в мене є кількість, є тип товару який я продаю, тип товару який я хочу отримати, і кількість. Умовно 2 пшениці за 1 пиво. Де еліти зможуть купувати чи продавати товар, але не весь пул одразу, а ту кількість яку хочуть за встановлену ціну."
                "Еліти: "
                "вони можуть створювати свої пропозиції аналогічні за механікою як мер. Виходить як біржа, попит та пропозиція і за одним шаблоном все"
                "ціна лістингу - має бути на розміщення, а на реакцію на вже розміщену ціни не буде"
                "Форма бартеру - конкретний товар на конкретний товар. Далі вже, можна буде ввести гроші"]
  :normalized "біржа: пропозиція = віддаю A за B, лотами, з лімітом; будь-який актор; 1 AP на розміщення, 0 на заповнення; бартер товар на товар; монета пізніше"
  :confirmed true}

 {:received-at "2026-09-05"
  :raw-request ["Щодо виставленого на продаж товару - згоден." "go"]
  :normalized "escrow підтверджено; GO на GAME_VISION rev 5 + amendment + GLOSSARY; спека — окремим GO"
  :confirmed true}

 {:received-at "2026-09-05, via /sdd-flow:resume"
  :raw-request ["City Tales - Medieval Era\nгра що чимось схожа до того що я планую, але вона має більш \"класичний\" геймплей"
                "Я хочу щоб ти провів цей аналіз, наших механік проти того що є в механіках гри City Tales - Medieval Era"
                "1 - так\n2 - так, зроби якийсь документ\n3 - так з rev 5\n4 - порівнюй все"]
  :normalized {:task :city-tales-analogue-analysis
               :kind :answer
               :do "research-пас: усі механіки rev 5 проти City Tales - Medieval Era; веб-пошук дозволено на весь пас; окремий Flows/RESEARCH_CITY_TALES_ANALOGUE.md, лінк із Findings цього FLOW"
               :off-limits #{код Unity GAME_VISION.md GLOSSARY.md UISpecs/ design-mockups/}
               :result "документ-мапа з :verified-by на кожному твердженні; вердикт може бути «немає що брати»"}
  :confirmed true}

 {:received-at "2026-09-05, після research-пасу"
  :raw-request ["Я видалив файли які вважав непотрібними"
                "1 - спека була застаріла. Те що ми робили в цій гілці це більше був на UI, а core mechanics/core loop обговорення.\n2 -  хз чи потрібен взагалі, бо я видалив макети як ті що не відповідають моєму баченню, так само як і специфікація.\n3 - можеш закрити чи видалити, це ми вже потім зробимо."
                ":achieved-result \"Як на мене ми пропрацювали core mechanics/core loop економіки і вже маємо те що можна брати в роботу на перший прототип. Ми відмовилися від механік які звучали прикольно, але були занадто складними та незрозумілими. І ми відмовилися від попереднього UI, який підтримував ці механіки\""
                "Видаляй UI_LANGUAGE"]
  :normalized {:task :retire-deleted-ui-artifacts
               :kind :mutation
               :deleted-by-owner #{UISpecs/UISPEC_MAIN_SCREEN.md "design-mockups/*.html — усі п'ять" UI_LANGUAGE.md}
               :do "зняти биті посилання; :spec-rederivation → :spec-retired; зона INDEX; два пойнтери в GAME_VISION; amendment у два superseded FLOW; пам'ять агента"
               :achieved-result "core mechanics / core loop економіки пропрацьовані до стану «можна брати в перший прототип»; складні й незрозумілі механіки відкинуті; попередній UI, що їх підтримував, відкинутий"}
  :confirmed true}

 {:received-at "2026-09-05, закриття"
  :raw-request "закривай, бо за нашим flow тільки користувач має право закривати flow-и, я бачу що їх робота вже виконана тому закривай"   ;; owner, у FLOW_ECONOMY_POC
  :normalized "flow-close: усе довговічне переписане в GAME_MECHANICS.md (новий документ замість GAME_VISION); цей FLOW і RESEARCH_CITY_TALES_ANALOGUE → Flows/Archive/"
  :confirmed true}]
```

# 2 · Contract

## Findings

```clojure
[{:finding :two-genres-not-two-versions
  :at "2026-09-04"
  :fact "UISPEC_MAIN_SCREEN описує власників фактів на повному екрані ранньої фази з елітами (регіони, id фактів, стани, порядок взаємодії, заборони похідного; геометрія вільна). main_UI_specification описував прототип базового зрізу без еліт і без мапи (позиції, ширини в px, вікна, навігація, демо-числа). Спільних id фактів не було; кожен посилався на інший як на «читай спершу»"
  :verified-by "прочитав обидва файли повністю 2026-09-04"
  :consequence "злиття неможливе як «взяти кращу з двох»; це поглинання одного жанру іншим із конвертацією фактів"}

 {:finding :basic-spec-carried-model-claims
  :at "2026-09-04"
  :fact "main_UI_specification декларував «нових економічних правил не вводить», але містив чотири модельні твердження: спеціалізація сумісна з місцевістю і відкриває накази; зайнятий район блокує зміну спеціалізації; тип робочих груп = щабель («селянські групи» у рядках макета); облік товару за хід (попередній / надійшло / витрачено / зміна)"
  :verified-by "прочитав спеку і grep по рядках FantasyMayor-BasicCity.html (name:'…', «Селянські»)"
  :consequence "ці твердження мусили стати рішеннями моделі (D1, D2) або демо; вони стали D1 і D2"}

 {:finding :basic-render-lacked-turn-commit
  :at "2026-09-04"
  :fact "FantasyMayor-BasicCity.html не мав жодного «Завершити хід»; AP 3/5 показувався, але дії, що витрачає хід, не було. Скасування наказу нічого не повертало"
  :verified-by "grep -o 'заверш… хід' по файлу — 0 збігів; спека §7 order-prototype-boundary"
  :consequence "єдина спека мусить володіти :f-turn-commit у всіх фазах і показувати повернення при скасуванні за моделлю"}

 {:finding :rev3-left-three-residues-in-spec-a
  :at "2026-09-04"
  :fact "після звірки 2026-09-04 у UISPEC_MAIN_SCREEN лишились: (1) :r-standing показує дії еліт із AP за хід, хоча §9 забороняє залишок AP еліти як приватний; (2) :f-sel-payer вироджується — мер діє лише на міських гексах, платник завжди місто; (3) будівля стала окремим іменником, але факту в спеці немає, як і відповіді, чи ставить мер будівлі"
  :verified-by "зіставив спеку з GAME_VISION rev 3 та рендером MainScreen.html, де рядок «ПЛАТНИК · МІСТО» стояв окремо"
  :consequence "D4 і D5 закривають (1) і (3); (2) стає забороною похідного, не фактом"}

 {:finding :labour-visibility-conflict
  :at "2026-09-04"
  :fact "UISPEC_MAIN_SCREEN кладе вільні групи за категоріями на головну поверхню; main_UI_specification показував їх лише в деталях наказу. GAME_VISION §9 :labour вимагає дефіцит по кожній категорії"
  :verified-by "прочитав обидві спеки і GAME_VISION §9"
  :consequence "з D1 (щабель = категорія) лічильник груп живе на щаблі: регіон населення володіє ним, а вимога дії його цитує"}

 {:finding :goods-on-main-surface-conflict
  :at "2026-09-04"
  :fact "UISPEC_MAIN_SCREEN: «кожен товар: кількість» на головній поверхні; main_UI_specification: два базові товари на HUD, решта у вікні складу з категоріями, пошуком і деталями на місці. Питання 40–50 товарів бачила лише друга спека"
  :verified-by "прочитав обидві спеки"
  :consequence "D7 робить HUD-набір модельним: будівельні товари; склад стає окремою поверхнею єдиної спеки"}

 {:finding :tools-check-links
  :at "2026-09-05"
  :fact "Tools/gen_index.py перевіряє биті відносні .md/.canvas посилання у frontmatter і в тілі поза code fences; doc_lint перевіряє символи коду"
  :verified-by "прочитав LINK_RE і коментар у Tools/gen_index.py"
  :consequence "після видалення файлів живі посилання на них у старих FLOW треба перекласти; згадки всередині fences лишаються історією"}

 {:finding :old-flow-stale-marks
  :at "2026-09-04"
  :fact "у FLOW_BUILD_UX_DECONGESTION три рішення стоять :written-to :pending, хоча пауза лише на будівництві, журнал подій і визначення будівлі вже записані в GAME_VISION rev 3; resume-context містить абзац про модель rev 2, який нижче скасовує абзац про розворот v3"
  :verified-by "зіставив рішення FLOW із GAME_VISION rev 3"
  :consequence "виправляється amendment-ом у цьому FLOW; старий FLOW дістає лише посилання сюди і виправлення позначок"}

 {:finding :city-tales-analogue-mapped
  :at "2026-09-05"
  :fact "City Tales - Medieval Era (Irregular Shapes; EA 2025-05-22, 1.0 2026-01-29) — одноакторний білдер у реальному часі без праці, без споживання і без наслідків. Збіги з rev 5: слот-кеп на землю (≤2 не-житлові будівлі на район), пасив після одноразової інвестиції уваги (компаньйон до автономії), увага як несуча вісь, драбина тірів I–V. Розбіжності: один актор, населення не працює і не голодує, золото — фіксована сума за апгрейд дому, зовнішній Trade Post замість біржі, авторські квести замість журналу. Рецензенти незалежно фіксують «I am always fine» і grind — негативний контроль для D8; нерухомі зони — головна скарга гравців"
  :research-document "Flows/RESEARCH_CITY_TALES_ANALOGUE.md"
  :verified-by "research-пас 2026-09-05, три раунди: Steam-сторінка, Wikipedia, 6 рецензій, 2 гайди, Steam-дискусія з відповіддю розробника, роадмап EA, два 1.0-релізи, медіакіт; повний список у документі"
  :weakened-by "усе читано через видачу та окремі сторінки; PC Gamer, Steam-новини, Games Press і Galaxus не відкрились; жодне джерело нічого не міряло; відсутність праці — доказ від відсутності"
  :consequence "модель rev 5 не змінюється; шість опцій з рейтингами стоять у вердикті документа проти §10-питань (:specialization-switch 70, :intercity-trade 50, :elite-emergence-mechanic 45, need-is-a-slot phase-2 45, :elite-capabilities-content 40) і одна ^:risky (політика з носієм-будівлею, 55 — торкається D4); рішення — власника"}]
```

## Decisions

```clojure
(def decisions  ;; every value below is the owner's answer of 2026-09-05 unless :verified-by says otherwise
  [{:decision :D1-tier-is-labour-category
    :status :confirmed :at "2026-09-05"
    :value "одна вісь за Anno 1800: селяни дають селянські групи, ремісники ремісничі; дія вимагає груп конкретного щабля"
    :verified-by "власник: «варіант з Anno»"
    :reason "еталон економіки — Anno 1800; так уже було зроблено в базовому рендері"}

   {:decision :D2-action-precondition
    :status :confirmed :at "2026-09-05"
    :value "кожна дія несе власну умову виконання; умовою може бути спеціалізація району, місцевість чи ресурс гекса, будівля, або сама еліта (унікальні дії); район має власні вимоги (місцевість, ресурс)"
    :verified-by "власник: «краще сприймати це реверсивно. Що кожна дія має якусь свою умову для її виконання»"
    :reason "одне правило замість трьох списків «що що відкриває»"
    :derived "зайнятий гекс не починає розвиток — випливає з «одна активна дія на гекс», не нове правило"}

   {:decision :D3-construction-is-hex-development
    :status :confirmed :at "2026-09-05"
    :value "будівництво = розвиток гексу: призначення спеціалізації та її еволюція; правило «пауза лише на будівництві» прив'язане саме до цих дій"
    :verified-by "власник: «це спеціалізація гексу, розвиток гексу, його еволюція»"
    :reason "дає паузі конкретний об'єкт; транзакція будівництва району в коді з ходами й ціною і є цією дією (гіпотеза з FLOW_BUILD_UX_DECONGESTION, у цій задачі код не читався)"}

   {:decision :D4-buildings-belong-to-elites
    :status :confirmed :provisional true :at "2026-09-05"
    :value "будівлі — справа еліт через дозвіл; мер будівель не ставить"
    :verified-by "власник: «давай віддамо це елітам, а там подивимося»"
    :reason "тримає драбину мера чистою: спеціалізація і базові дії; будівлі приходять з елітами"}

   {:decision :D5-leased-hex-shows-what-happens
    :status :confirmed :at "2026-09-05"
    :value "на зданому гексі мер бачить, ЩО відбувається; глибина деталей OPEN"
    :verified-by "власник: «що відбувається. Деталізацію можна буде обдумати»"
    :reason "агентський варіант «лише угода» відхилено; див. # Attempted"}

   {:decision :D6-order-is-political
    :status :confirmed :at "2026-09-05"
    :value "«наказ» = окремий вид політичних дій, не прив'язаних до гекса (замовлення на закупівлю, політики); дія на гексі = «дія»"
    :verified-by "власник: «наказ це окремий вид політичних дій, які не стосуються гексів»"
    :reason "знімає колізію між «наказами мера» базового зрізу і «наказом на закупівлю» моделі; базовий зріз мав дії, не накази"}

   {:decision :D7-construction-goods
    :status :confirmed :at "2026-09-05"
    :value "«будівельні товари» — модельна категорія: те, що споживають дії розвитку гексу; HUD показує саме їх"
    :verified-by "власник: «так, можна будівельні товари»"
    :reason "еталон Anno 1800, де зверху виділено товари для будівництва; закриває питання «які товари на HUD»"}

   {:decision :D8-population-dynamics-minimal
    :status :confirmed :at "2026-09-05"
    :value "мінімальний режим: потреби щабля закриті → групи щабля ростуть кількісно; не закриті → падають; підйом на наступний щабель OPEN"
    :verified-by "власник: «це точно зростання груп кількістно, це мінімальний режим, а далі подивимося. Якщо потреби не задоволені то падіння»"
    :reason "дає базовому зрізу петлю зворотного зв'язку; частково закриває :unmet-need-consequence у мінімальному режимі, ширші наслідки лишаються окремою темою"}

   {:decision :D9-proposed-blocks-folded
    :status :confirmed :at "2026-09-05"
    :value "gameplay-layers, category-cap, ring-admission, choice-loop еліти → по одному рядку в GAME_VISION §10 як OPEN; блоки з тіла знято"
    :verified-by "власник: «up to you»; вибір агента"
    :reason "єдине бачення не носить чужих рамок із позначкою «запропоновано»"}

   {:decision :D10-files-deleted
    :status :confirmed :at "2026-09-05"
    :value #{main_UI_specification.md UISpecs/UISPEC_BASIC_CITY_MENUS.md
             design-mockups/FantasyMayor-BasicCity.html design-mockups/FantasyMayor-MainScreen.html design-mockups/FantasyMayor-MainScreen-V2.html}
    :verified-by "власник: «та можеш видалити і макети і спеку»; перелік макетів названо агентом у запиті GO, GO дано"
    :reason "усі п'ять були не в git; спека і рендери двох спек, що зливаються; Directions, Material, SlotRow лишаються як записи UI_LANGUAGE"
    :note "Obsidian MCP був недоступний — видалено через rm, посилання перекладено вручну, gen_index підтверджує 0 битих"}

   {:decision :flow-home
    :status :confirmed :at "2026-09-05"
    :value "новий FLOW_VISION_UNIFICATION; два старі FLOW дістають amendment із посиланням сюди і лишаються активними до окремого /flow-close"
    :verified-by "GO на повідомлення, що пропонувало це з рейтингом 65"
    :reason "закриття FLOW — явна церемонія власника, не побічний ефект"}

   {:decision :spec-language-convention
    :status :confirmed :at "2026-09-05"
    :value "єдина спека зберігає конвенцію UISPEC_MAIN_SCREEN: англійський каркас прози, українські Clojure-правила"
    :verified-by "вибір агента; власник питання мови не закривав, GAME_VISION має ту саму конвенцію"
    :reason "переписувати мову без запиту — незамовлена робота"}

   {:decision :glossary-anchors-open
    :status :confirmed :at "2026-09-05"
    :value "нові терміни (дія, наказ, будівельні товари, щабель = категорія праці, розвиток гексу) записані в GLOSSARY з :anchors ? — коду під них ще немає"
    :verified-by "GLOSSARY вимагає якорі, перевірені по графу; таких символів у коді немає"
    :reason "термін має існувати до коду, інакше код назве його інакше"}

   {:decision :playtest-slice
    :status :open :at "2026-09-04"
    :value ?
    :note "аналіз радив зріз S1 «лінійний мер» (75) як перший плейтест; власник не обирав — це окрема задача після цієї"}

   ;; ── revision 5, 2026-09-05 — усі значення з verbatim-слів власника в Amendments ──
   {:decision :R1-slots-and-buildings
    :status :confirmed :at "2026-09-05"
    :value "спеціалізований гекс має N слотів під будівлі; будівлі ставлять еліти за дозволом мера; будівля прив'язана до однієї еліти"
    :verified-by "власник: «Кожен же спеціалізований гекс матиме кілька слотів під будівлі… Будівлі будуть будувати еліти з мого дозволу… будинки привʼязані під еліти»"
    :reason "повернення слотів у новій формі: слоти будівель, не слоти дій — recurrence названо"}

   {:decision :R2-passive-to-city-only
    :status :confirmed :supersedes :D4-buildings-belong-to-elites :at "2026-09-05"
    :value "пасив будівлі йде лише місту, щохода, безумовно, без праці; еліта отримує лише право дій на будівлі"
    :verified-by "власник: «пасив завжди йде лише місту… нехай буде повністю пасивний»"
    :reason "in-труба міста перестає залежати від рішень еліт; залежить від землі й слотів — важелів мера"}

   {:decision :R3-elite-actions-on-buildings
    :status :confirmed :at "2026-09-05"
    :value "еліти витрачають власне AP на дії на власних будівлях; групи беруть із міського пулу через кільце; вихід — еліті; місто годує групи незалежно від зайнятості, платні немає"
    :verified-by "власник: «еліти на своїх будинках можуть додатково робити дії які будуть давати прибуток їм. І ці дії будуть брати населення з пулу як працівників»"
    :reason "праця стає спільним ресурсом без ціни; рішення «зарплата не потрібна» стоїть без перегляду"}

   {:decision :R4-exchange
    :status :confirmed :at "2026-09-05"
    :value "біржа стоячих пропозицій за одним шаблоном для всіх акторів: віддаю A за B, лотами, з лімітом; 1 AP на розміщення, 0 на заповнення; заповнення на ході того, хто бере, кільце як тайбрейк; бартер товар на товар, монета пізніше як зустрічний товар"
    :verified-by "власник: «2 пшениці за 1 пиво… не весь пул одразу… Виходить як біржа… ціна лістингу - має бути на розміщення… конкретний товар на конкретний товар»"
    :reason "ринок дістає предмет; замовлення на закупівлю і оголошена ставка стають окремими випадками пропозиції"}

   {:decision :R5-escrow
    :status :confirmed :at "2026-09-05"
    :value "виставлений товар покидає склад у момент розміщення; невиставлене купити не можна — резерв за конструкцією"
    :verified-by "агентська пропозиція (70), власник: «Щодо виставленого на продаж товару - згоден»"
    :reason "інакше населення з'їсть лот раніше, ніж його заповнять; ліміт тримається саме escrow-ом"}

   {:decision :R6-elite-emergence-autonomous
    :status :confirmed :at "2026-09-05"
    :value "мер еліти не створює; появу еліт дає окрема автономна механіка (OPEN)"
    :verified-by "власник: «мер еліти не створює, це буде окрема, автономна ігрова механіка»"
    :reason "вихід із базової фази стає несучим відкритим правилом"}

   {:decision :R7-elite-capabilities
    :status :confirmed :at "2026-09-05"
    :value "розвиток еліти прив'язаний до спроможностей: кількість слотів під будівлі, розмір AP, пасивні бонуси; зміст драбини OPEN"
    :verified-by "власник: «їх розвиток має бути тоді привʼязаним до їх спроможностей. Слотів під те скільки будівель вони можуть мати, кількість AP, можливо ще якісь пасивні бонуси»"
    :reason "еліта змушена розвивати себе; прецедент The Guild 3 у research нитці 5"}

   {:decision :R8-leases-retired
    :status :confirmed :supersedes :D5-leased-hex-shows-what-happens :at "2026-09-05"
    :value "оренда гекса, частка в дозволі, данина натурою — зняті; на гексі діє лише мер; еліта тримає будівлю в слоті"
    :verified-by "випливає з R1–R3: «дії над гексами можу робити я, як мер»; власник не заперечив у пропозиції агента"
    :reason "одна форма права (слот) і одна форма обміну (лот) замість часток і строків; D5 переноситься на дію еліти на її будівлі"}

   {:decision :R9-mayor-goal
    :status :confirmed :at "2026-09-05"
    :value "ціль мера: балансувати міський ринок і домовлятися з елітами, щоб задовольнити потреби міста"
    :verified-by "власник, verbatim"
    :reason "game-centre :mayor-goal дістає механіку: біржа і дозволи"}

   {:decision :R10-agent-defaults
    :status :confirmed :at "2026-09-05"
    :value {:withdraw "зняття власної пропозиції безкоштовне; зміна = нове розміщення за 1 AP"
            :early-game "мер до першої еліти: розвиток гексу і базове збирання на нерозвинених гексах — ^:provisional"}
    :verified-by "запропоновано агентом із правом вето у повідомленні перед GO; GO дано без вето"
    :reason "власник цих пунктів не торкався; записані як provisional там, де це сказано"}

   {:decision :spec-rederivation
    :status :open :at "2026-09-05"
    :value ?
    :note "UISpecs/UISPEC_MAIN_SCREEN.md виведена з rev 4 і після rev 5 застаріла: поверхня угод стає поверхнею біржі, standing дістає дії еліт на будівлях, r-selection — слоти; перевиведення за окремим GO (власник погодив: модель зараз, спека потім, 70)"}

   ;; ── після research-пасу, 2026-09-05 — власник видалив спеку, всі макети і UI_LANGUAGE ──
   {:decision :spec-retired
    :status :confirmed :supersedes :spec-rederivation :at "2026-09-05"
    :value "UISpecs/UISPEC_MAIN_SCREEN.md видалена власником; перевиведення не планується — UI виводиться наново з rev 5 окремою задачею, коли настане час"
    :verified-by "власник: «спека була застаріла. Те що ми робили в цій гілці це більше був на UI, а core mechanics/core loop обговорення»"
    :reason "що перевірялось раніше: спека виведена з rev 4 і після rev 5 застаріла (:spec-rederivation, 70 на «модель зараз, спека потім»); що нове: власник зняв її сам замість замовити перевиведення"}

   {:decision :mockups-retired
    :status :confirmed :at "2026-09-05"
    :value "усі п'ять рендерів design-mockups/ (Directions, Directions2, Material, Material2, SlotRow) видалені власником; тека порожня"
    :verified-by "власник: «я видалив макети як ті що не відповідають моєму баченню»; ls design-mockups/ порожній"
    :reason "D10 лишала їх «як записи UI_LANGUAGE»; з видаленням UI_LANGUAGE записувати нема куди"}

   {:decision :ui-language-retired
    :status :confirmed :at "2026-09-05"
    :value "UI_LANGUAGE.md видалений через rm (Obsidian MCP недоступний); стиль виводиться наново з першого прототипу; два пойнтери в GAME_VISION (Scope, §9) переписані на «UI-документа немає»"
    :verified-by "власник: «Видаляй UI_LANGUAGE» у відповідь на три рейтингові варіанти — лишити й не чіпати 50, лишити й переписати абзац 35, зняти 15"
    :reason "власник обрав найнижче рейтингований варіант; рейтинги записані, щоб пізніше прочитати їх проти результату"}

   {:decision :branch-result
    :status :confirmed :at "2026-09-05"
    :value "core mechanics / core loop економіки пропрацьовані і готові в перший прототип; складні й незрозумілі механіки відкинуті; попередній UI, що їх підтримував, відкинутий"
    :verified-by "власник, verbatim у Amendments (:achieved-result)"
    :reason "заявлений результат гілки — підстава для майбутнього /flow-close трьох FLOW; наступний крок за власником"}])
```

## Disproven (append-only)

```clojure
[{:hypothesis "дві специфікації головного екрана — це дві версії одного екрана, і злиття означає вибрати кращу"
  :refuted-by "читання обох: різні жанри (власники фактів проти опису прототипу) і різні обсяги (повна рання фаза проти базового зрізу); жодного спільного id"
  :at "2026-09-04"
  :details "finding :two-genres-not-two-versions"}

 {:hypothesis "main_UI_specification не вводить економічних правил, як сама декларує"
  :refuted-by "чотири модельні твердження у її тілі й рядках рендера"
  :at "2026-09-04"
  :details "finding :basic-spec-carried-model-claims"}]
```

## Attempted (append-only)

```clojure
[{:approach "на зданому гексі мер бачить лише угоду (орендар, частка, строк); дія і AP еліти приватні"
  :confidence 60
  :dropped-because "власник: «що відбувається» — видно саму подію, глибина деталей пізніше"
  :problems "нічого не коштувало; урок — приватність AP не означає невидимість дії"
  :at "2026-09-05"}

 {:approach "«наказ» = дія мера на гексі, а закупівля перейменовується на «замовлення»"
  :confidence 55
  :dropped-because "власник: наказ — політична дія без прив'язки до гекса"
  :problems "нічого не коштувало; наслідок — базовий рендер називав дії наказами, термін у спеці змінюється"
  :at "2026-09-05"}

 {:approach "мер ставить будівлі за міські товари, той самий іменник"
  :confidence 45
  :dropped-because "власник віддав будівлі елітам, «а там подивимося»"
  :problems "нічого не коштувало"
  :at "2026-09-05"}

 {:approach "данина за працю: пасив будівлі місту дорівнює утриманню зайнятих нею груп і платиться її продуктом"
  :confidence 55
  :dropped-because "власник дав чистіший варіант: пасив іде місту завжди й безумовно, незалежно від зайнятості"
  :problems "нічого не коштувало; урок — прив'язка до зайнятості робила in-трубу залежною від рішень еліт"
  :at "2026-09-05"}

 {:approach "господар годує своїх: еліта закриває потреби зайнятих у неї груп зі свого складу"
  :confidence 35
  :dropped-because "не обрано; місто годує всіх, праця без платні"
  :problems "нічого не коштувало; лишається природною еволюцією на пізніший шар, не заміною"
  :at "2026-09-05"}

 {:approach "оголошена ставка обміну (assize): місто задає стоячу ставку за одиницю товару"
  :confidence 60
  :dropped-because "поглинута біржею: ставка є окремим випадком пропозиції з лімітом"
  :problems "нічого не коштувало"
  :at "2026-09-05"}]
```

# 3 · Plan

```clojure
{:status :complete   ;; closed 2026-09-05 by the owner's word — «закривай»
 :completed #{:analysis-in-chat :decisions-d1-d10 :flow-created :game-vision-rev4 :glossary-terms :single-main-screen-spec :delete-absorbed-files :old-flow-amendments :index-regen :meters
              :rev5-design-thread :game-vision-rev5 :glossary-rev5-terms :city-tales-analogue-research :retire-deleted-ui-artifacts :flow-close}
 :current :closed
 :remaining #{}
 :resume-context "CLOSED 2026-09-05: усе довговічне з цього FLOW живе в GAME_MECHANICS.md (GAME_VISION видалений); RESEARCH_CITY_TALES_ANALOGUE заархівований поруч; продовження — Flows/FLOW_ECONOMY_POC.md. RETIRE 2026-09-05: власник видалив спеку UISPEC_MAIN_SCREEN, усі макети design-mockups/ і UI_LANGUAGE.md («не відповідають баченню»); :spec-rederivation → :spec-retired; биті посилання зняті, зона INDEX і два пойнтери GAME_VISION переписані на «UI-документа немає»; пам'ять агента оновлена; UI виводиться наново з rev 5 окремою задачею, коли власник її поставить. Заявлений результат гілки — :branch-result: core loop готовий у перший прототип, складне відкинуте, старий UI відкинутий. Далі лише за власником: /flow-close для трьох FLOW і commit (обидва не робились; усе нижче в git не закомічене). Абзаци нижче, де сказано «спека чекає перевиведення» — історія до цього рішення. CITY TALES 2026-09-05: research-пас проти City Tales - Medieval Era завершений — Flows/RESEARCH_CITY_TALES_ANALOGUE.md (status implemented), finding :city-tales-analogue-mapped, amendment записаний. Модель не змінена. У вердикті шість рейтингових опцій проти §10-питань + одна ^:risky (політика з носієм-будівлею, торкається D4). Далі: власник читає мапу і вирішує, які опції стають рішеннями; будь-яка з них — окремий GO і запис у GAME_VISION. REV 5 записано 2026-09-05 (слоти, пасив лише місту, дії еліт на будівлях, біржа, escrow, автономна поява еліт, спроможності) — GAME_VISION rev 5, GLOSSARY +4 терміни. Спека UISPEC_MAIN_SCREEN виведена з rev 4 і ЧЕКАЄ перевиведення за окремим GO (:spec-rederivation :open). Раніше того ж дня: GAME_VISION rev 4 (D1–D9, :proposed → §10), GLOSSARY +5 модельних термінів з :anchors ?, UISpecs/UISPEC_MAIN_SCREEN.md — єдина спека з фазами :basic / :with-elites, п'ять файлів D10 видалені, обидва старі FLOW позначені :superseded і посилаються сюди. Метри пройдені (див. Acceptance). Лишається: власник читає GAME_VISION rev 4 і спеку; далі /flow-close для трьох FLOW (цей і два старі). Наступна задача за вибором власника — зріз плейтесту (:playtest-slice :open) або спека аркуша гекса. Код і Unity не чіпались."}
```

```clojure
;; harvested 2026-09-05 (Rule 2d): invariants → GAME_MECHANICS.md; the owner's verbatim words stay in # 1 · Request; record = commit log.
;; The executed ten-step plan (rev 4, GLOSSARY, single spec, D10 deletions, old-FLOW amendments, meters, rev 5, spec retired, close) is dropped.
```

## Acceptance

```clojure
[{:meter "python3 Tools/doc_lint.py --quiet" :target "0 ghosts, 0 Clojure syntax errors" :actual "0 ghosts; 0 syntax errors; 49 md scanned (2026-09-05)" :status :passed}
 {:meter "python3 Tools/gen_index.py" :target "lint clean, 0 broken links, INDEX regenerated" :actual "INDEX.md written: 39 docs (5 always, 25 trigger, 1 reference, 8 archive); LINT: clean (2026-09-05)" :status :passed}
 {:meter "grep -c GAME_VISION UI_LANGUAGE.md" :target 0 :actual 0 :status :passed}
 {:meter "grep -rn ':proposed' GAME_VISION.md" :target "0 поза §10" :actual "0 у всьому файлі — колишні :proposed живуть у §10 як :agent-proposal" :status :passed}
 {:meter "ls main_UI_specification.md UISpecs/UISPEC_BASIC_CITY_MENUS.md design-mockups/FantasyMayor-BasicCity.html design-mockups/FantasyMayor-MainScreen.html design-mockups/FantasyMayor-MainScreen-V2.html" :target "5 × No such file" :actual "5 × No such file; жоден не був у git" :status :passed}
 {:meter "власник" :target "GAME_VISION rev 4 і єдина спека прочитані; жодного конфлікту, який аналіз назвав, не лишилось" :actual "rev 4 прочитано і одразу переглянуто власником — rev 5 того ж дня" :status :superseded}

 ;; ── revision 5, 2026-09-05 ──
 {:meter "python3 Tools/doc_lint.py --quiet" :target "0 ghosts, 0 Clojure syntax errors" :actual "0 ghosts; 0 syntax errors; 49 md scanned" :status :passed}
 {:meter "python3 Tools/gen_index.py" :target "lint clean; перший рядок GAME_VISION оновлено" :actual "INDEX.md written: 39 docs; LINT: clean" :status :passed}
 {:meter "grep -c GAME_VISION UI_LANGUAGE.md" :target 0 :actual 0 :status :passed}
 {:meter "grep -c ':proposed' GAME_VISION.md" :target 0 :actual 0 :status :passed}
 {:meter "grep 'оренд' GAME_VISION.md поза retired-блоками та історією" :target "0 живих правил про оренду" :actual "0 — лишились тільки RETIRED-блок lease, коментар :owner і провенанс rev 3" :status :passed}
 {:meter "власник" :target "GAME_VISION rev 5 прочитано; цикл замикається; спека перевиведена за окремим GO" :actual "rev 5 прочитано; власник 2026-09-05: core loop готовий у перший прототип; спека не перевиводиться — видалена (:spec-retired)" :status :superseded}

 ;; ── retire deleted UI artifacts, 2026-09-05 ──
 {:meter "python3 Tools/gen_index.py" :target "LINT: clean" :actual "LINT: clean; INDEX.md written: 38 docs (5 always, 24 trigger, 1 reference, 8 archive); рядок UI_LANGUAGE зі скелета зник сам (2026-09-05)" :status :passed}
 {:meter "python3 Tools/doc_lint.py --quiet" :target "0 ghosts, 0 syntax" :actual "0 ghosts; 0 syntax errors; 48 md scanned (2026-09-05)" :status :passed}
 {:meter "grep живих посилань на UISPEC_MAIN_SCREEN, design-mockups/*.html і UI_LANGUAGE поза Archive і поза записами" :target 0 :actual "0 markdown-посилань; у прозі лишились тільки речення «видалено 2026-09-05» у зоні INDEX і в історії FLOW_BASIC_CITY_UI" :status :passed}

 ;; ── flow-close, 2026-09-05 ──
 {:meter "python3 Tools/gen_index.py після перепрямування посилань на GAME_MECHANICS і mv в Archive" :target "LINT: clean" :actual "LINT: clean; 39 docs, 14 archive (2026-09-05)" :status :passed}
 {:meter "python3 Tools/doc_lint.py --quiet" :target "0 ghosts, 0 syntax" :actual "0 ghosts; 0 syntax errors; 43 md scanned (2026-09-05)" :status :passed}
 {:meter "власник" :target "дозвіл на закриття" :actual "«закривай, бо за нашим flow тільки користувач має право закривати flow-и, я бачу що їх робота вже виконана тому закривай» — 2026-09-05" :status :passed}]
```
