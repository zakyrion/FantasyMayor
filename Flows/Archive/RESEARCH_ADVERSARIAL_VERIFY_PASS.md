---
category: A
read: archive
status: implemented
tags: [research, process, verification]
related:
  - "[RESEARCH_TEMPLATE](../../RESEARCH_TEMPLATE.md)"
---

# RESEARCH — Adversarial Verify Pass

Deep-research: чи вимірювано допомагає окремий fresh-context адверсарний пас над виводом агента, і в якій формі.

# Question

```clojure
{:question "чи окремий адверсарний верифікаційний пас (свіжий контекст, установка атакувати) над виводом агента вимірювано ловить помилки, яких self-review не бачить; у якій формі (свіжий контекст тієї ж моделі vs інша модель; refute-установка vs нейтральний review); якою ціною (false positives, токени)"
 :why-no-fast-answer "простір trade-off'ів: форми верифікації різні, виміри розкидані по бенчмарках і vendor-звітах, переносимість на соло-режим без тестів неочевидна"
 :opened-at "2026-08-30"
 :flow :standalone}
```

# Our conditions

```clojure
;; записано ДО читання будь-яких джерел
{:scale "соло-розробник + AI-агенти (Claude Code / Codex); Unity/C# pet-проект + власний SDD-фреймворк"
 :platform "Claude Code сесії; без CI-тестів принципово; фінальний авторитет коректності = playtest"
 :budget "толерантність до церемонії низька; дешеві двосторонні експерименти ок; окремий верифікатор — тільки за вимогою, з підтвердженням і ручним вибором моделі (рішення юзера 2026-08-30)"
 :team "1 людина; агент = контрагент, не команда"
 :deadline "нема жорсткого; хобі-каденс"
 :why "виграш має переважати вартість у РЕЖИМІ соло без тестів — не в режимі команди з CI, де міряли більшість джерел"}
```

# Prior belief

```clojure
;; здогадки агента, зафіксовані до пошуку — досить конкретні, щоб пошук міг їх вбити
[{:hunch "fresh-context адверсарний пас ловить суттєву частку реальних помилок, яких self-review автора не бачить; на великих diff'ах виграш переважає вартість"
  :confidence 70
  :grounded-in "тільки знання агента: пам'ять про self-correction/critic літературу + practitioner-звіти; для ЦЬОГО питання ще нічого не читано"
  :at "2026-08-30"
  :kill-if "виміряні роботи показують, що critic/review-пас додає ~0 реальних знахідок або тоне у false positives на код-задачах"
  :outcome ?}
 {:hunch "свіжого контексту ТІЄЇ Ж моделі достатньо; інша модель — необов'язкова надбудова"
  :confidence 55
  :grounded-in "знання агента: враження, що ключ — незалежний контекст, не інші ваги"
  :at "2026-08-30"
  :kill-if "виміряний self-preference bias зберігається і в свіжому контексті — та сама модель систематично м'якша до власного стилю виводу"
  :outcome ?}]
```

# Options

```clojure
[{:option "on-demand адверсарний refute-пас на великих/ризикових diff'ах (свіжий контекст, установка спростувати, людина — фінальний арбітр)"
  :forces "покриття помилок vs шум false positives і токени"
  :applies-when "великий diff, зона ризику, або deep-research оцінювання; НЕ always-on"
  :known-uses "CriticGPT-процес RLHF-рев'ю OpenAI; Refute-or-Promote stage-gating (wolfSSL/libfuse знахідки); /code-review у Claude Code"
  :evidence "виміряно: critic-модель ловить ~85% підкинутих багів проти ~25% у людей-контракторів; human+critic — повніші рев'ю з меншим FP, ніж модель сама; механізм підтверджений з іншого боку — intrinsic self-correction НЕ працює (7.6% виправлених проти 8.8% зіпсованих правильних, ICLR 2024), а «виправляти чуже» моделі вміють краще, ніж своє"
  :weakened-by "виміри — режим команд/RLHF, не соло-гейдев; CriticGPT все одно галюцинує частину знахідок"
  :confidence 75
  :buys "клас помилок, який self-review автора структурно не бачить (боттлнек = ЛОКАЛІЗАЦІЯ помилки)"
  :cost-to-build "≈0 — /code-review існує; кастомний refute-промпт = година"
  :cost-to-adopt "токени пасу + твій час на тріаж знахідок"
  :reversibility :two-way}

 {:option "always-on загальний review-агент на кожен diff"
  :applies-when "команди з великим потоком PR — НЕ наш режим"
  :evidence "виміряно ПРОТИ: 60.2% PR, які рев'ювили тільки агенти, мають 0-30% сигналу; 92% review-агентів — нижче 60% сигналу; рекомендація дослідження — вузькі специфічні перевірки замість general-purpose"
  :weakened-by "—"
  :confidence 10
  :reversibility :two-way}

 {:option "multi-agent debate (N агентів сперечаються)"
  :evidence "виміряно ПРОТИ: MAD програє self-consistency за cost-accuracy у систематичних оцінках (5 методів × 9 бенчмарків × 4 моделі); ізольована self-correction вигідніша за unguided debate"
  :confidence 10
  :reversibility :two-way}

 {:option "для ОЦІНЮВАЛЬНИХ ролей (judge/evaluator) — модель ІНШОЇ сім'ї, вибрана вручну"
  :applies-when "оцінка результатів research-пасу, A/B-судження — рівно майбутній evaluator-агент"
  :evidence "виміряно: self-preference bias корелює з self-recognition (NeurIPS 2024) — свіжий контекст його НЕ знімає, модель впізнає власний стиль; cross-family judging і ансамблі знижують bias (структуровані схеми — до ~31.5%)"
  :weakened-by "для баг-критики (не judge-ролі) same-family критик у CriticGPT працював — bias б'є передусім по оцінювальних судженнях"
  :confidence 70
  :reversibility :two-way}]
```

# Disconfirmation

```clojure
{:target "лідер: окремий адверсарний пас як практика"
 :searched-for "виміри, де review/critic-пас додає ~0 або тоне в шумі; де debate не кращий за одну модель"
 :came-back "наївні форми ВБИТО: general-purpose CRA — переважно шум (92% агентів <60% сигналу); MAD програє self-consistency. Вузька форма ВИЖИЛА: критик з refute-установкою + людина, виміряний виграш"
 :outcome :weakened-and-reshaped}  ;; опція вижила зі звуженням: on-demand + вузький скоуп + людина-арбітр
```

# Verdict

```clojure
{:recommends "брати у вузькій формі: on-demand адверсарний refute-пас для великих/ризикових diff'ів (/code-review як готовий механізм); для evaluator-ролей — модель іншої сім'ї з ручним вибором (= гейти юзера 2026-08-30); НІКОЛИ always-on і НІКОЛИ debate-каруселі"
 :because "у нашому режимі (соло, без CI-тестів) пас закриває структурну діру self-review — локалізацію власних помилок; шумова ціна контролюється рівно тими гейтами, які юзер сам поставив: окремо, за вимогою, з підтвердженням"
 :evidence-bar "двостороннє рішення — планка помірна; виміряні джерела її перекривають"
 :no-verdict false}
```

```clojure
;; 2026-08-30 — REVISIT (supersedes вердикт вище; той лишається як був)
;; Що змінилось: юзер справедливо вказав, що ядро self-correction-доказів міряне на GPT-3.5/GPT-4 (2023) —
;; давнина для галузі; загострене питання: «пас має сенс ТІЛЬКИ з іншою моделлю, точно не в межах одного вендора?»
;; Проведено раунд 3 по сучасних reasoning-моделях і кореляції помилок вендорів.
{:recommends "пас лишається рекомендованим; форма уточнена ГРАДІЄНТОМ незалежності: (1) свіжий контекст + refute-роль — найбільший виміряний виграш, працює і в межах вендора; (2) інший вендор — виміряно кращий дефолт для evaluator-ролей і high-stakes перевірок (декорелює сліпі плями); (3) «точно не в межах одного вендора» — НЕ підтверджено як абсолют: same-family критика має виміряну цінність (CriticGPT), а крос-вендорність не панацея — частина сліпих плям спільна для ВСІХ вендорів"
 :because "на сучасних LRM (o1/R1-клас) intrinsic self-correction ПОКРАЩИВСЯ у reasoning-трасах, але межі self-verification задокументовані й у 2025-2026; generation-verification gap структурний (верифікувати легше, ніж генерувати) — тож окремий верифікатор платить незалежно від покоління моделі; кореляція помилок росте зі спільністю тренування: моделі одного вендора помиляються разом частіше (~60% збігу помилок при спільному факапі; GPT-сім'я ділить error-патерни), тому крос-вендор додає другий, менший приріст поверх свіжого контексту"
 :evidence-bar "двостороннє — помірна; перекрита і для уточненої форми"
 :no-verdict false}
```

```clojure
;; 2026-08-30 — РІШЕННЯ ЮЗЕРА (фінальне; supersedes рекомендації вище для цілей впровадження)
{:decision :not-adopted
 :user-verbatim "Нє... не бачу нічого корисного."
 :scope "не впроваджується ні як процес, ні як «полиця»; вердикти агента вище лишаються як були — історія, не поточна рекомендація"
 :recurrence-note "не пропонувати повторно як нову ідею; повернення — тільки з ініціативи юзера або при зміні режиму"
 :at "2026-08-30"}
```

# Prior belief — outcomes (post-search)

```clojure
[{:hunch "fresh-context адверсарний пас ловить суттєву частку помилок; на великих diff'ах виграш переважає вартість"
  :was 70 :outcome :survived-with-conditions
  :note "вижила ТІЛЬКИ вузька форма: on-demand + refute-gate + людина; always-on варіант убитий CRA-шумом"}
 {:hunch "свіжого контексту тієї ж моделі достатньо; інша модель необов'язкова"
  :was 55 :outcome :weakened
  :note "для judge/evaluator-ролей — вбита: self-preference їде на self-recognition і свіжий контекст не рятує; для баг-критики same-family прийнятний (CriticGPT)"}]
```

# Sources

```clojure
[{:source "Huang et al., Large Language Models Cannot Self-Correct Reasoning Yet (ICLR 2024, arxiv 2310.01798)"
  :established "intrinsic self-correction не покращує/погіршує: 7.6% виправлено vs 8.8% зіпсовано (GSM8K, GPT-3.5); працює лише зовнішній фідбек" :kind :measured-study :at "2026-08-30"}
 {:source "TACL survey: When Can LLMs Actually Correct Their Own Mistakes?"
  :established "по багатьох задачах self-correction без зовнішнього сигналу не працює; боттлнек — локалізація помилки" :kind :measured-study :at "2026-08-30"}
 {:source "The Self-Correction Illusion (arxiv 2606.05976)"
  :established "моделі виправляють ЧУЖЕ краще, ніж своє — механізм на користь окремого верифікатора" :kind :measured-study :at "2026-08-30"}
 {:source "McAleese et al., LLM Critics Help Catch LLM Bugs (OpenAI, CriticGPT)"
  :established "критик ловить ~85% підкинутих багів vs ~25% людей; критики preferred у 63%; human+critic повніше і з меншим FP" :kind :measured-study :at "2026-08-30"}
 {:source "An Empirical Study of Code Review Agents in Pull Requests (arxiv 2604.03196)"
  :established "13 CRA: 60.2% agent-only PR у зоні 0-30% сигналу; 92.31% агентів <60%; рекомендація — вузькі перевірки + людина" :kind :measured-study :at "2026-08-30"}
 {:source "Refute-or-Promote (arxiv 2604.19049)"
  :established "adversarial stage-gated refute-архітектура для high-precision пошуку дефектів; реальні знахідки (wolfSSL, libfuse); повних таблиць у фетчі не видно" :kind :practitioner-report :at "2026-08-30"}
 {:source "Panickssery et al., LLM Evaluators Recognize and Favor Their Own Generations (NeurIPS 2024)"
  :established "лінійна кореляція self-recognition ↔ self-preference — свіжий контекст bias не знімає" :kind :measured-study :at "2026-08-30"}
 {:source "Stop Overvaluing Multi-Agent Debate (arxiv 2502.08788) + MAD-стаді + Cost of Consensus (2605.00914)"
  :established "MAD систематично не б'є self-consistency/самокорекцію за cost-accuracy" :kind :measured-study :at "2026-08-30"}
 {:source "Quantifying and Mitigating Self-Preference Bias (2604.22891) + cross-family практики"
  :established "cross-family judging/ансамблі знижують self-preference (до ~31.5% у структурованих схемах)" :kind :measured-study :at "2026-08-30"}

 ;; ── раунд 3 (revisit: сучасні моделі + вендор-питання) ──
 {:source "Deep Self-Evolving Reasoning (2510.17498); Self-Verification Dilemma (2602.03485); Illusion of Insight (2601.00514); Verification Mirage (2605.10850)"
  :established "LRM (o1/R1-клас) мають трейновану self-correction у трасах — стан покращився проти GPT-3.5-ери; АЛЕ межі self-verification/refinement/stability задокументовані й у 2025-2026: перечитування ≠ надійна перевірка, «aha-moments» часто ілюзорні" :kind :measured-study :at "2026-08-30"}
 {:source "Mind the Gap (ICLR 2025); Shrinking the Generation-Verification Gap (2506.18203); Variation in Verification (2509.17995)"
  :established "generation-verification gap: верифікувати легше, ніж генерувати — структурна основа окремого верифікатора, незалежна від покоління моделі; крихка при поганому верифікаторі" :kind :measured-study :at "2026-08-30"}
 {:source "Correlated Errors in LLMs (2506.07962); Failure Independence in LLM-Generated Code (2607.02808)"
  :established "помилки моделей корельовані: ~60% збігу, коли обидві помиляються; кореляція росте зі спільністю тренування/вендора (GPT-сім'я ділить error-патерни); cross-model ансамблі дають більшу redundancy, ніж same-model" :kind :measured-study :at "2026-08-30"}
 {:source "Cross-Provider Failure-Mode Convergence (2606.26116); Consensus is Not Verification (2603.06612)"
  :established "КОНТРдоказ абсолюту: 95.1% joint-failure-mode convergence між різними провайдерами в одному домені — частина сліпих плям спільна для всіх (спільні корпуси); крос-вендор не знімає їх, людина лишається арбітром" :kind :measured-study :at "2026-08-30"}
 {:source "практика cross-model adversarial review (Augment Code, Codex KB 2026)"
  :established "same-vendor рев'юери ділять сліпі плями; свіжа сесія без історії білду — обов'язкова умова критика" :kind :practitioner-report :at "2026-08-30"}]
```

# Search log

```clojure
[{:round 1 :queries ["LLM cannot self-correct reasoning" "CriticGPT catches bugs vs humans" "LLM self-preference bias own output" "LLM code review agent false positive empirical 2025"]
  :new-options 3 :new-evidence 6 :at "2026-08-30"}
 {:round 2 :queries ["fetch 2604.03196" "fetch 2604.19049" "multi-agent debate does not outperform self-consistency" "cross-model judge reduces self-preference"]
  :new-options 1 :new-evidence 4 :at "2026-08-30"}
 {:stop :question-settled :note "третій раунд додавав би ширину, не рішення; двосторонні двері" :at "2026-08-30"}
 {:round 3 :reopened-by "юзер: GPT-3.5-докази застарілі для галузі; чи пас = тільки інший вендор?"
  :queries ["reasoning models self-correction o1 R1 2025" "generation-verification gap frontier" "same-family critic blind spots cross-vendor" "correlated failures vendor ensemble diversity"]
  :new-options 0 :new-evidence 5 :at "2026-08-30"}
 {:stop :question-settled :note "вердикт уточнено градієнтом незалежності (revisit-блок у Verdict); подальші раунди — ширина" :at "2026-08-30"}]
```
