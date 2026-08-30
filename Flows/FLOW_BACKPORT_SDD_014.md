---
category: A
read: trigger
trigger: "before changing the deliverable-kind / research-canon / provenance-attempted zones, FLOW/RESEARCH templates, or the session-start reconstruction"
status: implemented
tags: [process, sdd, backport]
related:
  - "[DOC_STANDARD](../DOC_STANDARD.md)"
  - "[FLOW_TEMPLATE](../FLOW_TEMPLATE.md)"
---

# FLOW_BACKPORT_SDD_014

Backport of the sdd-flow framework delta 0.1.1 → 0.1.4 into the FantasyMayor process canon.

# 1 · Request

## User request — 2026-08-29 (verbatim)

> я оновив мій sdd framework до версії 0.1.4 яка вже встановлена, то ж мені потрібно зробити зворотній порт сюди

## User confirmation — 2026-08-29 (verbatim)

> 1 - все що є актуальним в 0.1.4 тобто включаючи всі зміни що відбулися з моменту останнього порту. або ж можеш зробити повний реімпорт з версії 0.1.4
> 2 - розширення flow а також RESEARCH.md-шаблон входить. Весь функціонал sdd має бути інтегровано, з тим щоб все це працювало через скіл fantasy mayor start а також з урахуванням проєктної документації
> 3 - повна підтримка як в оригінальному sdd фреймворку

## Agent restatement — confirmed by the user

```clojure
{:task :backport-sdd-014
 :goal "зворотній порт усієї актуальної дельти sdd-flow 0.1.1→0.1.4 (0.1.2 + 0.1.4; повний реімпорт з 0.1.4 допустимий) у канон FantasyMayor"
 :source "~/Documents/CoWork/SDDClojureFlow — останній порт сюди: коміт 0c846fb (SDD 0.1.1)"
 :where #{CLAUDE.md FLOW_TEMPLATE.md DOC_STANDARD.md ".claude/commands/fantasymayor-session-start.md" ".claude/commands/flow-close.md" AGENTS.md "нові шаблони/доки за потреби (RESEARCH-шаблон)"}
 :decided ["розширення FLOW-шаблону + RESEARCH.md-шаблон входять у scope"
           "повна підтримка функціоналу як в оригінальному фреймворку (deliverable-kind, late-discovered ?, провенанс claim'ів, ранжування джерел, deep-research pass)"
           "все має працювати через скіл fantasymayor-session-start і з урахуванням проєктної документації"]
 :result "канон-доки і скіли FantasyMayor несуть повний функціонал sdd-flow 0.1.4 у стилі наявних Clojure-блоків; AGENTS.md перегенеровано; gen_index/doc_lint чисті"}
```

## Amendments (append-only)

```clojure
[{:received-at "2026-08-29 (відповідь на findings-gate батч)"
  :raw-request "1 - так\n2 - так\n3 - go"
  :normalized {:d11-global-glossary-number :confirmed
               :d12-examples-to-guide :confirmed
               :d1-d10 "без вето — підтверджені як запропоновано"
               :implementation-go true}
  :confirmed true}]
```

# 2 · Contract

```clojure
(def research-findings  ;; 2026-08-29 · verified-by: прочитано diff d28ca36..cdb7baf у CoWork/SDDClojureFlow + git show 0c846fb і відновлений FLOW_BACKPORT_SDD_011 з історії FM
  {:source-delta "sdd-flow 0.1.2 (2a76802) + 0.1.4 (cdb7baf; 0.1.3 влито) — templates/core/FLOW_CONTRACT.md +213, FLOW.md +34, RESEARCH.md новий (83), sdd-clojure-flow SKILL +36, sdd-deep-research SKILL новий (106), команда sdd-research нова, ADAPTERS/CLOJURE_NOTATION/EXAMPLES розширені"
   :new-defs-012 {:deliverable-kind "вісь answer/plan/mutation; kind задають ТІЛЬКИ слова юзера; агент ніколи не ескалює; answer-task іде без гейта і без FLOW, класифікація inline разом з відповіддю"
                  :answer-contract "лінія відповіді: артефакт = сама відповідь, рівно в межах спитаного; перевірити передумову питання; в кінці віддати контроль; можливу задачу — одним рядком, ніколи не розгортати; never: непрошені роадмапи/плани/тиха ескалація"
                  :emergent-decision "рішення, що виринає під час Execute і відсутнє в підтвердженій мапі = late-discovered ?; спливає ДО дії, ніколи не вирішується мовчки; батчиться; «немає альтернативи» — теж рішення, заявити до дії"
                  :go-kind-bound "go успадковує kind підтвердженої мапи — go на answer/plan ніколи не відкриває mutation"}
   :new-defs-014 {:option-confidence "кожен запропонований варіант несе int 0-100 (наскільки ймовірно це правильне РІШЕННЯ, не істинність факту); оцінюється до показу юзеру; числа пишуться у FLOW поруч з варіантом"
                  :provenance ":verified-by (проза: як встановлено) + :at (дата) на findings/decisions/disproven/attempted; never: запис без вказаного ґрунту"
                  :attempted "секція # Attempted: {:approach :confidence :dropped-because :problems :at}; ≠ disproven (спростоване переконання ≠ випробуваний-і-покинутий підхід); write у момент відмови"
                  :recurrence-guard "перед пропозицією будь-якого підходу перечитати Attempted+Disproven; вже пробуване — назвати поверненням і сказати, що змінилось; never: подати як нову ідею"
                  :decision-revisit "нове датоване entry з :supersedes; старе лишається як було; never: правити підтверджене рішення на місці / переперевирішувати без «що змінилось»"
                  :research-passes "два паси з гейтом: пас-1 = уточнити задачу → пошук → повернутись зі знахідками і відкритими питаннями (НЕ планом); план — тільки після відповідей; пас-2 колапсує, якщо відповіді нічого не відкрили"
                  :findings-gate "після першого research-пасу: показати знахідки + загострені питання, СТОП; :questions :follow-up (багатоітераційна дискусія заслуговує на загострене питання) + :stop-rule (юзер сказав досить → питання закінчились)"
                  :prior-art "додаткова нога research :method — як ця ж проблема вже вирішена ПОЗА проектом; рамку задає задача, не фіксований список; діє на всіх глибинах, включно з :port"
                  :outbound-gate "web-пошук/чужий репозиторій — назвати що шукаємо і що це має вирішити, підтвердження ДО виходу; 1 підтвердження на research-пас; після 2-го — спитати про вікно без переспросу; ungated: документація названої бібліотеки, context7"
                  :deep-research "skill sdd-deep-research для питань без швидкої правильної відповіді; вмикає ТІЛЬКИ юзер (агент просить дозвіл); артефакт Flows/RESEARCH_<TOPIC>.md, лінкується з Findings, архівується разом з FLOW; standalone = документ і є деліверабл; no-verdict — легітимний результат"
                  :evidence-weight "confidence 0-100 + поруч прозою :grounded-in (звідки віра) + :weakened-by (чому докази тонкі); порядок: виміряв > запустив-і-відзвітував > стверджує > знання агента (найнижча вага); реверсивність задає планку доказів"
                  :disconfirmation "до пошуку записати власну здогадку агента, досить конкретну щоб її можна було вбити; шукати те, що вб'є лідера, не те, що підтвердить; девіація питання — записується"}
   :flow-template-delta "FLOW.md: нова # Findings [{:finding :at :fact :verified-by :research-document :consequence}]; Decisions + :at/:verified-by + revisited-приклад з :supersedes; Disproven + :at; нова # Attempted; resume-contract реконструює також findings/research-document/attempted; done-contract + :research-document archived-with-FLOW"
   :research-template "RESEARCH.md (новий): Question / Our conditions (наш режим ДО читання) / Prior belief / Options (forces·applies-when·known-uses·evidence·weakened-by·confidence·buys·cost-to-build vs cost-to-adopt·reversibility) / Disconfirmation / Verdict (no-verdict allowed) / Sources (:kind + :established) / Search log"
   :notation-delta "CLOJURE_NOTATION: новий літерал — число (confidence 0-100); канонічна глосарій-таблиця FM живе в ~/.claude/CLAUDE.md і вимагає row ПЕРШИМ"
   :skill-delta "lifecycle-SKILL: Route + answer-lane; Offer (rated options, recurrence, follow-up/stop-rule); Hand over (deep-research через дозвіл); Execute/research-go: passes/findings-gate/prior-art/outbound-gate/provenance"
   :fm-canon-side "9 SHARED зон CLAUDE.md→AGENTS.md (gen_agents.py; нова зона = обидва маркери вручну); done-contract фенс — ПОЗА зонами, синк вручну; Working Contract проза — agent-specific, ручний same-pass синк; Codex-ноги: skills/fantasymayor-session-start/SKILL.md (repo) + ~/.codex/skills/* (ручний синк); flow-close = .claude/commands/flow-close.md (Claude-only команда)"
   :fm-deep-research-engine "плагін-скіл anthropic-skills:sdd-deep-research вже встановлений глобально і тригериться за описом — FM-канону досить назвати його і правила активації"
   :precedent-note "FLOW_BACKPORT_SDD_011.md видалено з репо у f382bf8 «[FM-14] remove old flows» разом з іншими Archive-FLOW — всупереч flow-fate :never delete; зроблено юзером свідомо; запис відновлюваний з git"})
```

```clojure
(def decisions
  [{:decision :scope-delta :status :confirmed :at "2026-08-29" :value "0.1.2 + 0.1.4 повністю (повний реімпорт з 0.1.4 допустимий)" :verified-by "user answer 1" :reason "user answer 1"}
   {:decision :flow-and-research-templates :status :confirmed :at "2026-08-29" :value "входять; інтеграція через fantasymayor-session-start + проєктні доки" :verified-by "user answer 2" :reason "user answer 2"}
   {:decision :deep-research-support :status :confirmed :at "2026-08-29" :value "повна, як в оригіналі" :verified-by "user answer 3" :reason "user answer 3"}

   ;; ── placement-пропозиції (закриваються відповідями + implementation-go); confidence за option-confidence, яку ж і портуємо ──
   {:decision :d1-deliverable-kind-home :status :proposed :confidence 85
    :value "нова SHARED зона deliverable-kind (def deliverable-kind + def answer-contract, адаптовані до FM-стилю) перед HARD GATE; + рядок у прозі HARD GATE: гейт тримає :plan/:mutation, :answer іде лінією відповіді (класифікація inline, без FLOW)"
    :reason "це вхідна класифікація — її місце біля гейта, який вона розгалужує"}
   {:decision :d2-option-confidence-home :status :proposed :confidence 80
    :value "розширити SHARED зону agent-output: entry :options + def option-confidence у тому ж фенсі"
    :reason "правило про форму пропозицій = genre agent-output"}
   {:decision :d3-lifecycle-defs-home :status :proposed :confidence 80
    :value "у SHARED фенс task-contracts: провенанс-поля в def disproven (:at) + нові defs provenance, attempted, recurrence-guard, decision-revisit, emergent-decision (після blocked-outcome/disproven)"
    :reason "той самий genre контрактів життєвого циклу — прецедент d2 бекпорту 0.1.1"}
   {:decision :d4-research-canon-home :status :proposed :confidence 75
    :value "розширити SHARED зону research-depth: у def research-depth додати :prior-art/:passes/:record-provenance; поруч у тому ж фенсі defs research-passes, prior-art, outbound-gate, deep-research, evidence-weight, disconfirmation; + 1-2 рядки в прозі фаз Research/Plan (два паси, findings-gate)"
    :reason "одна зона «як робиться research» замість розсипу; проза фаз — agent-specific, синк вручну"}
   {:decision :d5-gates-patch :status :proposed :confidence 85
    :value "go-contract зона: + :kind-bound; done-contract фенс (поза зонами, обидва доки вручну): + :research-document; close-ritual зона: у :steps choose-fate! згадка «linked RESEARCH_* їде разом з FLOW»"
    :reason "точкові патчі наявних контрактів"}
   {:decision :d6-flow-template :status :proposed :confidence 85
    :value "FLOW_TEMPLATE.md: research-findings → провенанс-вектор Findings; Decisions shape + :at/:verified-by + revisited-приклад; Disproven + :at; нова підсекція «## Attempted (append-only)» у # 2 · Contract"
    :reason "attempted/findings — durable Contract-стадія, переживають drop плану"}
   {:decision :d7-research-template :status :proposed :confidence 75
    :value "новий кореневий RESEARCH_TEMPLATE.md (Category B, read: trigger «when starting a deep-research pass — copy → Flows/RESEARCH_<TOPIC>.md»), порт framework RESEARCH.md у FM-стилі"
    :reason "симетрія з FLOW_TEMPLATE.md" }
   {:decision :d8-research-doc-genre :status :proposed :confidence 70
    :value "DOC_STANDARD: doc-genres + :research-artifact (Flows/RESEARCH_<TOPIC>.md, сателіт свого FLOW або standalone-деліверабл); frontmatter category: A, read: trigger, related → FLOW; архівується разом з FLOW (flow-fate)"
    :reason "без genre-правила research-док стане ще одним рот-джерелом"}
   {:decision :d9-session-start-integration :status :proposed :confidence 85
    :value "session-start (команда + repo Codex-SKILL + ~/.codex ноги same-pass): реконструкція активного FLOW включає Findings (з провенансом), Attempted, залінкований RESEARCH-док; «Start Working» проза CLAUDE/AGENTS — те саме"
    :reason "user answer 2: працює через fantasymayor-session-start"}
   {:decision :d10-deep-research-entry :status :proposed :confidence 70
    :value "без нової репо-команди: канон (def deep-research) називає глобальний плагін-скіл sdd-deep-research + правило «вмикає тільки юзер, агент просить дозвіл»; артефакт-шлях і архівація — FM-специфічні"
    :reason "движок уже встановлений і тригериться за описом; репо-команда дублювала б плагін"}
   {:decision :d11-global-glossary-number :status :open :confidence 80
    :value "ряд «число-літерал (confidence 0-100)» у канонічну таблицю ~/.claude/CLAUDE.md — глосарій вимагає row ПЕРШИМ, файл глобальний юзерський"
    :reason "без ряду confidence-числа в FM-доках порушують контракт глосарія"}
   {:decision :d12-examples-to-guide :status :open :confidence 55
    :value "порт нових прикладів EXAMPLES.md (answer-kind, rated options, provenance, weighed, prior-belief) у CLOJURE_GUIDE.md"
    :reason "FM-аналог прикладів — людський підручник; але це необов'язкова нога порту"}])
```

## Disproven (append-only)

```clojure
[]
```

# 3 · Plan

```clojure
(def plan-tombstone  ;; harvested 2026-08-30 (close-ritual)
  {:harvested-on "2026-08-30"
   :went-where "все durable вже лежить у цілях порту: канон-зони CLAUDE.md→AGENTS.md (10 зон, gen_agents), FLOW_TEMPLATE/RESEARCH_TEMPLATE, DOC_STANDARD research-map genre, session-start обидві ноги + ~/.codex, flow-close, CLOJURE_GUIDE §14, глобальний глосарій (число-літерал); конспект дельти 0.1.2+0.1.4 і рішення d1-d12 — Contract цього FLOW; not-adopted рішення пізнішого research-пасу — у Flows/Archive/RESEARCH_*"
   :record "commit log"})
```

## Acceptance

```clojure
[{:meter "python3 Tools/doc_lint.py --quiet" :target "0 ghosts, 0 Clojure syntax errors"
  :actual "0 ghost(s) in 0 file(s) · 0 Clojure syntax error(s) · 36 md scanned" :at "2026-08-29" :status :pass}
 {:meter "python3 Tools/gen_agents.py --check" :target "усі зони в синхроні"
  :actual "10 zone(s) in sync with CLAUDE.md canon (було 9; +deliverable-kind)" :at "2026-08-29" :status :pass}
 {:meter "python3 Tools/gen_index.py" :target "INDEX перегенеровано без warnings"
  :actual "INDEX.md written: 32 docs; лишились ДО-задачні warnings: GAMEPLAY_FOUNDATION line-budget + 2 broken links в архіві на FLOW-и, видалені юзером у f382bf8 — поза scope цього порту"
  :at "2026-08-29" :status :pass-with-preexisting-warnings}]
```

## Close-ritual audit — 2026-08-30 (метри перечитані наживо)

```clojure
[{:meter doc_lint :actual "0 ghost(s) · 0 Clojure syntax error(s) · 36 md scanned" :status :pass}
 {:meter "gen_agents --check" :actual "10 zone(s) in sync with CLAUDE.md canon" :status :pass}
 {:meter gen_index :actual "34 docs (2 always, 23 trigger, 2 reference, 7 archive); 2 warnings — до-задачні биті лінки на FLOW-и, видалені юзером у f382bf8" :status :pass-with-preexisting-warnings}
 {:note "close включив: gen_index.py навчений RESEARCH_* в Archive (жанр d8); research-доки заархівовано з not-adopted вердиктами; пам'ять-індекс оновлено"}]
```
