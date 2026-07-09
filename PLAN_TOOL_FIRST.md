---
category: C
read: trigger
trigger: "resuming the tool-first program (doc-lint / why-to-code / root-docs purge / workflow flip) in a new session"
tags: [plan, tool-first, docs, program]
---

# PLAN — Tool-First Program

Багатосесійна програма (2026-07-09): перевернути discovery-модель проєкту з **doc-first** на
**tool-first + code**. Причина: module-MD гниють швидше, ніж курація встигає (доведено на
BuildDistrict-зрізі — свіжа внутрішня суперечність у `DISTRICT_BUILD.md` через день після цільової
звірки, подія-привид `DistrictBuildSelectionRequestedEvent` у доку І в код-коментарі), тоді як
ecs-graph / di-graph / roslyn / код дають ті самі факти завжди свіжими.

## Вирішено (не переглядати без користувача)

```clojure
(def decided
  {:direction        "tool-first + code; MD = гіпотеза, не істина"
   :md-compression   :cancelled      ;; district-MD НЕ редагуємо — їх доля вирішується в :md-fate (можливо delete/rewrite-from-zero)
   :why-to-code-scope "пілот: тільки BuildDistrict-зріз"
   :doc-lint-run     #{"ручний виклик" "рядок у session-start"}  ;; без hook'а
   :memory-purge     "разом із root-docs: правило remove-completely, one pass (repo + .claude + ~/.claude + memory)"
   :architecture-md  "FROZEN — кожна правка через hook → явний approve користувача"
   :md-fate          "окрема РОЗМОВА після фаз 1-4, не виконання"
   :dynamic-data     {:never "динамічні факти в доках"   ;; 2026-07-09, після 31-привидного baseline: current state / статуси / ростери / wiring / пріоритети — ТІЛЬКИ тули+код
                      :docs-may-hold "policy, why, target-контракти (змінюються рішенням, не дрейфом коду)"
                      :live-symbol-in-doc "дозволений лише як якір, що проходить doc-lint"}})  ;; навіть приклади в Patterns/ гниють — PATTERN_TRANSACTION_ENTITY-прецедент
```

## Фази

| # | Фаза | Зміст | Статус |
|---|---|---|---|
| 0 | план-док | цей файл | ✅ 2026-07-09 |
| 1 | `:doc-lint` | `Tools/doc_lint.py`: детермінований детектор привидів — code_refs + Pascal-символи в MD проти оголошень у .cs; + рядок у session-start | ✅ 2026-07-09 |
| 2 | `:kill-module-mds` | triage → salvage «why» у код-коменти → видалено ВСІ 32 module/domain/presentation MD (+ `.meta`); `ADDRESSABLE_PATTERNS.md` → `Patterns/`; INDEX перебудовано; поглинув старі фази `:why-to-code` і `:md-fate`-щодо-module-MD | ✅ 2026-07-09 |
| 3 | `:root-docs-purge` | doc-first правила вирізано: `CLAUDE.md` (Research tool-first, «Module MD Files» → «Code Knowledge Policy», curator звужено до di-graph STEP-2), `ARCHITECTURE.md` (3 правки, approve), `DOC_STANDARD.md` переписано (жанри: Flow/Recipe/Policy; Category A = FLOW), `GENERAL_UI_STYLE.md` §13 (per-window docs скасовано), `ECS_CONVENTIONS.md` якорі, `PATTERN_TRANSACTION_ENTITY.md` (плейсхолдери + degenerate-case), `CLOJURE_GUIDE.md` (історичний приклад + doc-lint: off/on регіони), INDEX agent-zone. **doc-lint = 0 привидів** | ✅ 2026-07-09 |
| 4 | `:workflow-flip` | чистка agent-memory (`feedback_read_md_files`, `feedback_docs_always_synced`, `feedback_md_behavioral_contract`, `feedback_docs_sync_needs_permission`, …) + `.claude/agents/docs-curator.md` переписати під нову роль (di-graph STEP-2 only) або retire + перевірити hooks/graph-gate.py | ⬜ |

## Контекст для відновлення сесії

- Discovery-звіт (2026-07-09, сесія 1): ~70% вмісту `DISTRICT_BUILD.md` виводиться з тулів або
  продубльовано xml-doc'ами в коді; ~20% — справжній залишок (target-контракти, gap list,
  Unity-пререквізити); ~10% — статусна проза, що гниє найшвидше.
- Знайдені артефакти гниття: привид-подія `DistrictBuildSelectionRequestedEvent`
  (`DISTRICT_BUILD.md` Current State + `DistrictBuildListUISubSystem.cs:18`); суперечність
  «хто пише default-selection» всередині `DISTRICT_BUILD.md` (Flow-секція проти Selection-секції);
  мертвий каркас `Assets/Flows/DistrictBuild/{Components,Events}` (порожні, лише .meta);
  `Economy/District` і `Economy/DistrictBuild` не мають MD взагалі — і це не зламало роботу.
- FLOW-доки (`Flows/FLOW_*.md`) лишаються: target-контракт + gap list — те, чого нема в коді.
- Baseline doc-lint (2026-07-09, момент створення): **31 привид у 14 файлах**. Після смерті module-MD
  лишилось **17 привидів у root-доках** — worklist фази `:root-docs-purge`: найважчий —
  `PATTERN_TRANSACTION_ENTITY.md` досі вчить видаленому draft/template-життєциклу; решта — привиди
  видалених типів у `ECS_CONVENTIONS.md` / `DOC_STANDARD.md` / `CLOJURE_GUIDE.md` (точний список:
  `python3 Tools/doc_lint.py`).
