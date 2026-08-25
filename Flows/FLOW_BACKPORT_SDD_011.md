---
category: A
read: trigger
trigger: "before changing the agent-output / research-depth / disproven canon zones, FLOW_TEMPLATE's Disproven section, or the session-start reconstruction"
status: implemented
tags: [flow, process, backport, sdd]
related:
  - "[DOC_STANDARD](../DOC_STANDARD.md)"
  - "[Backport SDD Improvements](FLOW_BACKPORT_SDD_IMPROVEMENTS.md)"
  - "[Design Installable Engineering Flow](FLOW_DESIGN_INSTALLABLE_ENGINEERING_FLOW.md)"
---

# FLOW — Backport SDD 0.1.1

Бекпорт SDDClojureFlow 0.1.1: agent-output (проза для відповідей), Disproven-секція FLOW, research-depth за типом зміни.

---

# 1 · Request

## User request — 2026-08-25 (verbatim)

> Так, sdd flow оновлено, тепер треба щоб ти якось ці оновлення підтягнув

## Уточнення — 2026-08-25 (verbatim, відповіді на батч питань)

> 1 - всі
> 2 - b
> 3 - так
> 4 - це все має бути інтегравано в нас fantasy mayor start skill

## Agent restatement — confirmed by the user (research-go 2026-08-25)

```clojure
[{:task :bp2-agent-output
  :goal "канонізувати розділення: Clojure = мова артефактів, проза = мова відповідей"
  :source "SDDClojureFlow d28ca36 → FLOW_CONTRACT (def agent-output)"
  :where #{CLAUDE.md AGENTS.md}                     ;; user answer 2: варіант (b) — shared зона проектного канону
  :result "агент ніколи не відповідає формою; артефакти лишаються Clojure"}

 {:task :bp2-disproven
  :goal "секція Disproven у FLOW-шаблоні + канон-правило write/read/resume"
  :where #{FLOW_TEMPLATE.md CLAUDE.md AGENTS.md}
  :decided {:shape "тільки у FLOW_TEMPLATE.md (no-duplication правило першого бекпорту)"
            :canon-def "def disproven → існуючий shared фенс task-contracts"      ;; :by-precedent
            :resume "реконструкція активного FLOW включає Disproven"}
  :result "спростована гіпотеза = індекс-запис, який переживає компакт сесії"}

 {:task :bp2-research-depth
  :goal "Research масштабується за типом зміни port/replace/new; характеризація-гейт для replace"
  :where #{CLAUDE.md AGENTS.md}
  :decided {:form "новий shared def-блок + рядок-вказівник у прозі фази Research"}  ;; :by-precedent
  :result "рішення про заміну механізму не проходить Plan без поведінкового контракту оригіналу"}]
```

## Amendments (append-only)

```clojure
[{:received-at "2026-08-25 (у батч-відповіді, пункт 4)"
  :raw-request "це все має бути інтегравано в нас fantasy mayor start skill"
  :normalized {:target ".claude/commands/fantasymayor-session-start.md"
               :patch "session-start реконструює й Disproven активного FLOW; output shape вказує на agent-output канон"}
  :confirmed true}   ;; частина підтверджувальної відповіді; конкретика інтеграції — decision :d5

 {:received-at "2026-08-25 (після close-ritual)"
  :raw-request "ок, давай."   ;; go на репорт дрейфу Codex-ноги session-start
  :normalized {:target #{"skills/fantasymayor-session-start/SKILL.md" "~/.codex/skills/fantasymayor-session-start/SKILL.md"}
               :patch "3 пропуски: §1c canon-sync check (борг бекпорту №1) + Disproven у FLOW-реконструкції + agent-output в Output shape"
               :direction "встановлена копія була новіша (active-FLOW реконструкція) → взята базою, repo-копія вирівняна по ній, зверху 3 вставки, розкат назад; diff = identical"}
  :confirmed true}]
```

```clojure
(def naming-note  ;; Q4 про ім'я FLOW лишилось без вето — :by-naming-policy
  {:file FLOW_BACKPORT_SDD_011.md
   :status :unvetoed-proposal})
```

# 2 · Contract

```clojure
(def research-findings  ;; 2026-08-25
  {:source "CoWork/SDDClojureFlow commit d28ca36 (v0.1.1) — templates/core/{FLOW_CONTRACT.md, references/CLOJURE_NOTATION.md, templates/FLOW.md}"
   :additions {:agent-output "артефакти = Clojure (таск-мапи, FLOW-записи, чернетки контрактів, обрамлені прозою); відповіді = проза; форми малі (nesting ≤ 2); never: відповідь формою, посилання на голий лейбл (:s2) без переказу змісту"
               :disproven "entry {:hypothesis :refuted-by :details}; write у момент спростування (індекс, не лише рядок у лозі); read перед новою гіпотезою; включено в resume-reconstruct"
               :research-depth ":depth {port → інвентар; replace → характеризація + divergence-гіпотези; new → гіпотези на точках інтеграції}; :characterize gate — заміна не підтверджується без поведінкового контракту оригіналу (git history / live behavior); :hypotheses — пункт контракту → дешева перевірка; :observability — емпіричний залишок → self-diagnosing guards; :never + «не підтверджувати заміну на неперевіреному 'нове вже вміє'»"}
   :fm-canon "7 SHARED зон у CLAUDE.md: go-contract close-ritual task-template task-clojure-example task-contracts notation-ecs-ext openspec-policy"
   :gen-agents-mechanics "нова зона = ОБИДВА маркери вручну (SHARED у CLAUDE.md + порожня GENERATED у AGENTS.md); незбіг множин id = exit 2; build заповнює; --check ловить дрейф"
   :task-contracts-fence "один clojure-фенс, 4 defs (task-normalization task-amendments flow-progress blocked-outcome) — п'ятий def додається редагуванням SHARED-вмісту, нова зона не потрібна"
   :research-phase "у FM немає (def research)-блоку — фаза Research це агент-специфічна проза в обох доках"
   :session-start "команда .claude/commands/fantasymayor-session-start.md (не skill-папка); §2 Reconstruct не згадує активний FLOW/Disproven; Output shape вже prose-first"
   :start-working "CLAUDE.md «Start Working» реконструює active FLOW: «current stage, unresolved decisions and next plan item» — без disproven"})
```

```clojure
(def decisions  ;; placements — :proposed закриваються implementation-go
  [{:decision :d1-agent-output-home :status :proposed
    :value "нова SHARED зона agent-output після task-clojure-example; def адаптується до FM-стилю"
    :reason "правило про стиль виводу — окремий блок, не член task-contracts"}
   {:decision :d2-disproven-def-home :status :proposed
    :value "def disproven п'ятим у фенс task-contracts (після blocked-outcome)"
    :reason "той самий genre (контракти життєвого циклу задачі); нова зона не потрібна"}
   {:decision :d3-disproven-template :status :proposed
    :value "FLOW_TEMPLATE: підсекція «## Disproven (append-only)» в кінці # 2 · Contract"
    :reason "спростовані гіпотези — durable факти → Contract-стадія (переживають drop плану); shape тільки в шаблоні"}
   {:decision :d4-research-depth-home :status :proposed
    :value "нова SHARED зона research-depth після зони go-contract (біля Working Contract) + один рядок-вказівник у прозі фази Research обох доків (agent-specific, поза зонами)"
    :reason "проза Research різна в агентів — ділиться тільки def-блок"}
   {:decision :d5-session-start-integration :status :proposed
    :value "§2 Reconstruct: активний FLOW → реконструювати і Disproven (никогда не re-enter оплачений глухий кут); Output shape: відповіді прозою за agent-output; + «Start Working» CLAUDE.md: '…unresolved decisions, disproven hypotheses and next plan item'"
    :reason "user answer 4 — інтеграція в start skill"}])
```

# 3 · Plan

```clojure
(def plan-tombstone  ;; harvested 2026-08-25
  {:harvested-on "2026-08-25"
   :went-where "канон-зони agent-output/research-depth + def disproven у task-contracts (CLAUDE.md → gen_agents → AGENTS.md, 9 зон); скелет секції → FLOW_TEMPLATE.md; session-start інтеграція → .claude/commands/fantasymayor-session-start.md; факти й рішення d1–d5 → Contract цього FLOW"
   :record "commit log"})
```

## Acceptance audit — 2026-08-25

```clojure
[{:meter "gen_agents.py --check"
  :target "9 zone(s) in sync"
  :actual "build: rebuilt 3 zone(s): agent-output, research-depth, task-contracts → 9 zone(s) in sync with CLAUDE.md canon"
  :status :pass}
 {:meter doc_lint
  :target "0 ghosts, 0 Clojure syntax errors"
  :actual "0 ghost(s) · 0 Clojure syntax error(s) · 38 md scanned"
  :status :pass}
 {:meter "grep FLOW_TEMPLATE.md"
  :target "Disproven є, без дубля shape"
  :actual "## Disproven (append-only) скелет — тільки в шаблоні; канон-def disproven описує :entry правилом (дуальність = дизайн пакета: FLOW_CONTRACT def + FLOW.md skeleton)"
  :status :pass}
 {:meter "grep session-start"
  :target "Disproven + agent-output вказівник"
  :actual "§2: Disproven-реконструкція активного FLOW (рядки 66-68); Output shape: проза за agent-output (82-83)"
  :status :pass}]
```
