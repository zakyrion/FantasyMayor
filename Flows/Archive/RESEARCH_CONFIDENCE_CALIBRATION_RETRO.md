---
category: A
read: archive
status: implemented
tags: [research, process, calibration]
related:
  - "[RESEARCH_TEMPLATE](../../RESEARCH_TEMPLATE.md)"
---

# RESEARCH — Confidence Calibration Retro

Deep-research: чи вимірювано покращує калібрування періодичне ретро записаних confidence проти фактичних результатів.

# Question

```clojure
{:question "чи періодичне читання записаних confidence-оцінок проти фактичних результатів (ретро по архівних FLOW) вимірювано покращує калібрування у воркфлоу людина+LLM, де модель НЕ вчиться між сесіями"
 :why-no-fast-answer "калібрувальна література міряла людей-прогнозистів; LLM-канал зворотного зв'язку структурно інший (нема ваг, тільки контекст і довіра юзера) — переносимість під питанням"
 :opened-at "2026-08-30"
 :flow :standalone}
```

# Our conditions

```clojure
;; записано ДО читання будь-яких джерел
{:scale "числа з'являються у FLOW-доках (option-confidence, SDD 0.1.4); N рейтингованих рішень на місяць — малий, десятки не сотні"
 :platform "LLM-агент без міжсесійного навчання: канал покращення = (a) калібрування ДОВІРИ юзера до чисел, (b) канон-пріори, влиті назад у контекст (типу «85 агента в домені X історично ≈ 60»)"
 :budget "одне читання при закритті кожних N FLOW; ніяких live-метрик у пайплайні (мораторій 2026-07-08 на pipeline-tuning стоїть)"
 :team "1 людина"
 :deadline "нема"
 :why "класичні виміри калібрування — люди з тисячами прогнозів; наш режим — малий N і агент без пам'яті ваг"}
```

# Prior belief

```clojure
;; здогадки агента, зафіксовані до пошуку
[{:hunch "ретро оцінок проти результатів покращить калібрування чисел у цьому воркфлоу — щонайменше через канал довіри юзера і письмові пріори в каноні"
  :confidence 60
  :grounded-in "тільки знання агента: пам'ять про forecasting-літературу (тренування калібрування працює у людей) + власна інференція про контекст-канал; ще нічого не читано"
  :at "2026-08-30"
  :kill-if "література показує, що сирий outcome-feedback БЕЗ proper scoring/reference classes калібрування не покращує; або що verbalized confidence LLM нечутлива до in-context feedback"
  :outcome ?}
 {:hunch "головна цінність ретро — не покращення майбутніх чисел агента, а калібрування ДОВІРИ юзера до них"
  :confidence 55
  :grounded-in "знання агента: структурний факт відсутності міжсесійного навчання"
  :at "2026-08-30"
  :kill-if "виміри показують, що LLM суттєво покращує калібрування від few-shot прикладів власних минулих помилок у контексті — тоді канал агента не слабший за канал юзера"
  :outcome ?}]
```

# Options

```clojure
[{:option "ретро як покращувач МАЙБУТНІХ ЧИСЕЛ агента (оригінальна ідея: числа стануть каліброванішими)"
  :forces "надія на навчання vs структурний факт: модель не вчиться між сесіями, N малий"
  :applies-when "сотні оцінок + інтенсивний фідбек — НЕ наш режим"
  :evidence "виміряно ПРОТИ у людей: trial-by-trial scoring-фідбек НЕ покращив калібрування (N=610+871, байєсівський strong null, Martin 2025); класика: сирий outcome-feedback «not encouraging», допомагає лише на важких/низькоімовірних задачах; успішні тренування історично вимагали сотень трайлів. Для LLM: канал «few-shot власних минулих результатів у контексті → краща калібрування» вимірами не підтверджений (знайдені методи — Batch Calibration тощо — про інше)"
  :weakened-by "прямих вимірів САМЕ нашого сетапу (LLM + людина + десятки рішень) нема — перенос з людської літератури"
  :confidence 25
  :buys "мало — при N у десятки статистичний зсув чисел невідрізнимий від шуму"
  :reversibility :two-way}

 {:option "ретро як калібрування ДОВІРИ юзера + похідні пріори в канон («85 агента в домені X історично ≈ 60»)"
  :forces "цінність зворотного зв'язку vs церемонія читання архіву"
  :applies-when "числа вже пишуться у FLOW (option-confidence :survives) — рівно наш випадок; раз на N закритих FLOW, БЕЗ live-метрик (мораторій 2026-07-08 не зачіпається)"
  :known-uses "форкастинг: тренування з reference classes і доменними базовими частотами — виміряний виграш 6-7% Brier (GJP, мультирічні турніри); software-оцінювання: historical error intervals + чеклісти покращують точність"
  :evidence "виміряно (суміжний домен): ТРЕНУВАННЯ з осмисленим розбором (не сирий outcome-фідбек) працює; environmental/task-фідбек б'є outcome-фідбек; LLM verbalized confidence системно overconfident (ECE до 0.7+ у гірших моделей) — тобто поправочні пріори мають що виправляти"
  :weakened-by "перенос з іншого домену; головний бенефіціар — людина, не модель; ефект на числа агента не обіцяний"
  :confidence 60
  :buys "чесна вага чисел при читанні + записані доменні поправки, які агент отримує в контекст КОЖНОЇ сесії через канон"
  :cost-to-build "≈0 — одне читання архіву + рядок у канон"
  :cost-to-adopt "пів години на кожне ретро"
  :reversibility :two-way}]
```

# Disconfirmation

```clojure
{:target "лідер до пошуку: «ретро покращить калібрування чисел»"
 :searched-for "виміри, де калібрувальний фідбек НЕ працює"
 :came-back "прямий null: Martin 2025 (strong evidence for null, 40-100 трайлів недостатньо); McClelland&Bolger — outcome-фідбек сам по собі слабкий; успішні кейси = сотні трайлів або тренування з reference classes"
 :outcome :killed-as-stated}  ;; питання ПЕРЕТВОРИЛОСЬ: не «чи покращаться числа», а «який канал працює при малому N» → канал юзера/пріорів
```

# Verdict

```clojure
{:recommends "брати у МОДИФІКОВАНІЙ формі: ретро раз на N закритих FLOW як (a) калібрування довіри юзера і (b) джерело письмових пріорів у канон; НЕ обіцяти собі покращення самих чисел агента — при нашому N це не підтверджується літературою"
 :because "сирий outcome-фідбек слабкий і в людей; модель між сесіями не вчиться; але доменні поправки, влиті в канон, працюють через контекст кожної нової сесії — це environmental-фідбек, єдина форма, яку література підтримує"
 :evidence-bar "двостороннє рішення — планка помірна; для модифікованої форми перекрита, для оригінальної — ні"
 :no-verdict false}
```

```clojure
;; 2026-08-30 — РІШЕННЯ ЮЗЕРА (фінальне; supersedes рекомендації вище для цілей впровадження)
{:decision :not-adopted
 :user-verbatim "Нє... не бачу нічого корисного."
 :scope "не впроваджується ні планове ретро, ні вироджена форма принагідних пріорів; option-confidence 0.1.4 працює як є — числа ранжують варіанти в моменті, без петлі"
 :recurrence-note "не пропонувати повторно як нову ідею; повернення — тільки з ініціативи юзера"
 :at "2026-08-30"}
```

# Prior belief — outcomes (post-search)

```clojure
[{:hunch "ретро покращить калібрування чисел у цьому воркфлоу"
  :was 60 :outcome :killed-as-stated
  :deviation "питання перетворилось: не «чи покращаться числа», а «який канал зворотного зв'язку взагалі може працювати при малому N і моделі без міжсесійної пам'яті»"}
 {:hunch "головна цінність — калібрування ДОВІРИ юзера, не чисел агента"
  :was 55 :outcome :survived-strengthened
  :note "література рівно це й каже: сирий outcome-фідбек слабкий, канал моделі не доведений, канал людини+пріорів структурно доступний"}]
```

# Sources

```clojure
[{:source "Martin 2025, Calibration Feedback With the Practical Scoring Rule Does Not Improve Calibration (Futures & Foresight Science)"
  :established "trial-by-trial scoring-фідбек за 40-100 трайлів калібрування НЕ покращує (N=1481 сумарно, Bayesian strong null); історично успішні тренування = сотні трайлів" :kind :measured-study :at "2026-08-30"}
 {:source "Chang/Tetlock et al., Developing expert political judgment (GJP)"
  :established "коротке тренування (reference classes, розбір) + практика з фідбеком → 6-7% Brier-виграш у мультирічних турнірах" :kind :measured-study :at "2026-08-30"}
 {:source "McClelland & Bolger; Outcome Feedback Effects on Under-/Overconfident Judgments; Aligning confidence with accuracy (ScienceDirect)"
  :established "сирий outcome-фідбек «not encouraging»: не знімає overconfidence на легких/високоімовірних задачах, іноді шкодить; performance- і environmental-фідбек діють на РІЗНІ компоненти" :kind :measured-study :at "2026-08-30"}
 {:source "Xiong et al. 2306.13063; LLMs Are Overconfident in Their Own Responses (2606.03437); Dunning-Kruger in LLMs (2603.09985); Overconfidence is Key (2405.02917)"
  :established "verbalized confidence LLM системно overconfident (більшість відповідей у бакеті 90-100%; ECE у гірших ~0.7) — поправочним пріорам є що виправляти" :kind :measured-study :at "2026-08-30"}
 {:source "NAACL 2024 survey confidence estimation + Batch Calibration (2309.17249)"
  :established "in-context калібрувальні методи існують, але канал «власна минула історія у few-shot → краща verbalized calibration» вимірами не закритий" :kind :documentation :at "2026-08-30"}
 {:source "Jørgensen-лінія: historical error intervals, чеклісти у software estimation"
  :established "локальний контекст + історичні інтервали похибки покращують оцінки — практика, споріднена з пріорами в каноні" :kind :practitioner-report :at "2026-08-30"}]
```

# Search log

```clojure
[{:round 1 :queries ["calibration training feedback Brier improvement" "outcome feedback insufficient calibration" "LLM verbalized confidence overconfident" "software estimation historical feedback"]
  :new-options 2 :new-evidence 5 :at "2026-08-30"}
 {:round 2 :queries ["fetch Martin 2025 ffo2.199" "LLM in-context past outcomes calibration"]
  :new-options 0 :new-evidence 2 :at "2026-08-30"}
 {:stop :question-settled :note "прямий null-результат + класика збіглись; питання переформульовано і закрито для двосторонніх дверей" :at "2026-08-30"}]
```
