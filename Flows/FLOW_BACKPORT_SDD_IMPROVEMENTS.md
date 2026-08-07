---
category: A
read: trigger
trigger: "before changing Tools/gen_agents.py, the SHARED/GENERATED canon zones, or the close-ritual"
status: implemented
tags: [flow, process, backport, sdd]
related:
  - "[DOC_STANDARD](../DOC_STANDARD.md)"
  - "[Design Installable Engineering Flow](FLOW_DESIGN_INSTALLABLE_ENGINEERING_FLOW.md)"
---

# FLOW — Backport SDD Improvements

Бекпорт 4 покращень SDDClojureFlow: генерований канон-синк, FLOW-шаблон, Clojure-чекер (вердикт), ритуал закриття.

---

# 1 · Request

## User request — 2026-08-07 (verbatim)

> Підготуй задачу на Backport за ось цим коментарем
>
> Що зроблено КРАЩЕ, ніж у FantasyMayor flow (кандидати на бекпорт)
> 1. Один канон + згенеровані адаптери замість нашого ручного SYNC INVARIANT …
> 2. `doctor` як process-lint. … checksum-перевірка пари CLAUDE.md/AGENTS.md derived-блоків.
> 3. `FLOW.md` як fill-in шаблон із секціями Decisions та Acceptance …
> 4. Протестований Clojure-reader з line:col помилками — якщо doc_lint-ів чекер слабший, є що запозичити.
> 5. Явна команда `close` з `(check-all-acceptance!) (archive-only-when-done!)` …

## Уточнення (verbatim, у порядку отримання)

> 2 - на твій розсуд, але дивися щоб в нас не було копії між FLOW_TEMPLATE та DOC_STANDARD, але як на мене це окремий документ що не є частиною DOC_STANDARD
> 4 - всі 4
> 1 - b
> 2 - команда + канон-блок

## Confirmed contract — research-go 2026-08-07

```clojure
[{:task :bp-canon-sync
  :goal "спільні блоки канону генеруються в AGENTS.md скриптом; дрейф неможливий, не просто видимий"
  :where #{CLAUDE.md AGENTS.md Tools/ ".claude/skills/fantasymayor-session-start/"}
  :decided {:approach :generator
            :model "двопрохідна модель gen_index.py: generated-зона між маркерами, ручна зона поза ними"
            :restructure "канон ріжеться на shared verbatim блоки + Codex-специфічну обгортку поза зоною"
            :tool ^:new Tools/gen_agents.py
            :check "--check режим = process-lint; session-start додає його рядок поруч із doc_lint"}
  :skip #{"глобальні копії ~/.codex/* — лишаються ручними"
          "generated-зона в session-start скілі"}
  :accept [{:meter "gen_agents.py --check" :target "clean після build; навмисна правка generated-зони → fail loud"}]
  :result "правка канону + один build-крок = синхронний AGENTS.md"}

 {:task :bp-flow-template
  :goal "fill-in шаблон Category A FLOW: Request / Contract / Plan + Decisions + Acceptance + Amendments"
  :where #{^:new FLOW_TEMPLATE.md DOC_STANDARD.md INDEX.md}
  :decided {:home "root-рівень, category B, read: trigger — «коли створюєш новий FLOW»"
            :no-duplication "shape живе ТІЛЬКИ в шаблоні; DOC_STANDARD тримає lifecycle/fate-rules + один рядок-вказівник"
            :source "templates/core/templates/FLOW.md пакета, адаптований під 3-стадійну структуру"}
  :accept [{:meter doc_lint :target "0 ghosts, 0 Clojure syntax errors"}
           {:meter "grep DOC_STANDARD" :target "вказівник є, дубля shape немає"}]
  :result "новий FLOW = копія шаблону, а не відтворення shape по пам'яті"}

 {:task :bp-clojure-reader
  :goal "doc_lint парсить Clojure-фенси не слабше за reader пакета"
  :where Tools/doc_lint.py
  :do [(:step-1 "research: можливості поточного чекера")
       (when "чекер слабший (немає line:col / пропускає malformed)"
         (:then (port-tokenizer-to-python!)))]
  :skip "shell-out у node"
  :accept [{:meter "malformed-фікстури" :target "(cond порт → всі ловляться з line:col; не слабший → факт у FLOW, без змін)"}]
  :result "один вердикт: порт або зафіксоване «не потрібно»"}

 {:task :bp-close-ritual
  :goal "явний ритуал закриття FLOW"
  :where #{^:new ".claude/commands/flow-close.md" CLAUDE.md AGENTS.md}
  :decided {:form "команда + канон-блок"
            :canon-block "ритуал — shared verbatim блок, іде в generated-зону :bp-canon-sync"
            :codex "отримує ритуал через AGENTS.md; окремого артефакта немає"
            :no-auto "закриття — тільки явний виклик користувача"}
  :listen :bp-canon-sync
  :accept [{:meter "grep обох доків" :target "блок ритуалу присутній і байт-ідентичний в обох"}]
  :result "/flow-close: перевірка метрів по-справжньому → акцепт-аудит → архів"}]
```

---

# 2 · Contract

```clojure
(def research-findings  ;; 2026-08-07
  {:agents-structure "AGENTS.md = Codex-обгортка + блоки, дослівно спільні з CLAUDE.md"
   :shared-verbatim #{"Engineering Task Template (text-блок + Clojure-приклад + field-mapping)"
                      go-contract task-normalization task-amendments
                      flow-progress blocked-outcome notation-ecs-ext openspec-policy}
   :agent-specific #{"HARD GATE параграф (plan-mode згадка тільки в Claude)"
                     "done-contract (:diagnostic — roslyn у Claude, careful re-read у Codex)"
                     "усі прозові секції (tool surface)"}
   :session-start "живе як КОМАНДА .claude/commands/fantasymayor-session-start.md — НЕ скіл;
                   :where контракту уточнено цим фактом (шлях, не scope)"
   :doclint-reader "ClojureReader у doc_lint.py НЕ слабший за reader пакета:
                    line:col точні до файлу (base_line від фенса), парність мап,
                    escape-и включно з \\u, #{} І #_ (пакет #_ не має), ^meta, quote,
                    mismatched-delimiter з очікуваним/знайденим, unclosed fence"
   :reader-verdict :no-port-needed})   ;; умовна мапа :bp-clojure-reader розв'язана в «без змін»
```

```clojure
(def canon-sync-design  ;; механіка генератора
  {:canon CLAUDE.md                                    ;; семантичне джерело — без змін статусу
   :markers {:canon "<!-- BEGIN/END SHARED: <block-id> -->"
             :derived "<!-- BEGIN/END GENERATED: <block-id> (Tools/gen_agents.py) -->"}
   :tool ^:new Tools/gen_agents.py
   :modes {:build "переписує кожну GENERATED-зону в AGENTS.md контентом SHARED-блоку канону"
           :check "--check: порівнює, друкує дрейф, exit 1 — process-lint"}
   :outside-zones "Codex-специфічний текст — генератор не торкається"
   :session-start "команда session-start отримує §1c: python3 Tools/gen_agents.py --check"})
```

```clojure
(def close-ritual-design
  {:canon-block "(def close-ritual …) — новий SHARED-блок у CLAUDE.md, генерується в AGENTS.md"
   :command ^:new ".claude/commands/flow-close.md"     ;; тонкий тригер канон-блоку
   :steps (-> (check-all-acceptance!) (record-acceptance-audit!)
              (harvest-plan!) (choose-fate!) (regen-index!))
   :blocked "метр не на target → FLOW лишається partial, блокер за blocked-outcome"
   :never #{"auto-close" "архів із падаючим метром"}})
```

```clojure
(def flow-template-design
  {:home ^:new FLOW_TEMPLATE.md                        ;; root, category B, read: trigger
   :trigger "when creating a new Category A FLOW"
   :sections "3 стадії Rule 2: Request (verbatim + agent restatement + amendments) /
              Contract (decisions :confirmed|:open + інваріанти) /
              Plan (progress-shape + кроки + acceptance-аудит :meter/:target/:actual/:status)"
   :doc-standard "one-task-one-flow отримує :template FLOW_TEMPLATE.md — один вказівник, нуль дублю"
   :source "templates/core/templates/FLOW.md пакета, адаптований"})
```

```clojure
(def closed-resolutions  ;; user 2026-08-07 — every resolution is CLOSED, execute in order
  {:open-1 {:status :confirmed
            :value "done-contract і HARD GATE — агент-специфічні, ПОЗА shared-зонами"
            :reason "у FM обов'язкові власні тули; їх використання вбудоване в канон КОЖНОГО агента окремо"}
   :open-2 {:status :confirmed
            :value "gen_agents.py --check іде в pre-commit поруч із gen_index"}})
```

---

# 3 · Plan

```clojure
(def plan-tombstone  ;; harvested 2026-08-07
  {:harvested-on "2026-08-07"
   :went-where "механіка зон → маркери в самих CLAUDE.md/AGENTS.md + docstring gen_agents.py; ритуал → close-ritual канон-блок + .claude/commands/flow-close.md; shape → FLOW_TEMPLATE.md; reader-вердикт → Contract цього FLOW"
   :record "commit log"})
```

## Acceptance audit — 2026-08-07

```clojure
[{:meter "gen_agents.py --check"
  :target "clean після build; навмисна правка зони → exit 1"
  :actual "build: rebuilt 2 zones → 7 zone(s) in sync; injected drift у openspec-policy → DRIFT + exit 1; rebuild відновив canon"
  :status :pass}
 {:meter doc_lint
  :target "0 ghosts, 0 Clojure syntax errors"
  :actual "0 ghost(s) · 0 Clojure syntax error(s) · 37 md scanned"
  :status :pass}
 {:meter "grep обох доків"
  :target "close-ritual блок байт-ідентичний в обох"
  :actual "close-ritual ×3 у кожному (BEGIN/def/END); байт-ідентичність зони гарантує --check in sync"
  :status :pass}
 {:meter "grep DOC_STANDARD"
  :target ":template вказівник є, дубля shape немає"
  :actual ":template рядок у one-task-one-flow; shape-секції існують тільки у FLOW_TEMPLATE.md"
  :status :pass}]
```

```clojure
(def zones-note  ;; фактичне групування — 7 зон покривають 8 спільних блоків
  {:zones [go-contract close-ritual task-template task-clojure-example
           task-contracts notation-ecs-ext openspec-policy]
   :task-contracts "4 defs (task-normalization task-amendments flow-progress blocked-outcome) в одному фенсі = одна зона"
   :extra "pre-commit hook (Tools/githooks/pre-commit) тепер жене gen_agents --check після gen_index"})
```
