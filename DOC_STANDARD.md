---
category: C
read: trigger
trigger: "before authoring or reviewing any .md (Flows / Patterns / policy) for standard compliance"
tags: [docs, conventions]
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[INDEX](INDEX.md)"
---

# DOC_STANDARD.md

Single source of truth for how to write Markdown docs in this project.
Read this before creating or editing any `.md` file.

> **Module/domain/presentation MDs are ABOLISHED (2026-07-09) — never recreate them.** Present-tense
> narrative about code state rots faster than curation keeps up. Module knowledge lives in code
> comments (distance zero), tools (`roslyn` / `ecs-graph` / `di-graph`), commit messages, and the
> genres below. `Tools/doc_lint.py` lint-checks every doc's symbol claims against the code.

---

## Rule 0 — Documentation is written for the AI agent, not for a human

Every `.md` file in this project is written for an AI agent that will act on it. Write for the
**least-capable agent likely to read it**: explicit, literal, no implied reasoning chains.

## Rule 1 — Only non-rotting genres

A doc may hold ONLY content that does not decay when code drifts:

```clojure
(def doc-genres
  {:flow-contract "Flows/FLOW_<NAME>.md"   ;; dated TARGET contract of one cross-domain behavior + gap list; changes by decision, not drift
   :recipe        "Patterns/PATTERN_*.md"  ;; one-approach-per-file skeleton for a code role; changes when the convention changes
   :policy        "root *.md"              ;; ARCHITECTURE / ECS_CONVENTIONS / CLAUDE / this file / GENERAL_UI_STYLE / GLOSSARY
   :never         "present-tense mirror of code state"})  ;; current-state prose, rosters, wiring — tools own those
```

```clojure
;; ── structural facts: a tool owns them, a doc NEVER does ────────────────────
(def tool-owns
  {#{types signatures references hierarchy outline}                   roslyn-mcp
   #{writers readers reactive-consumers archetypes PK-FK priorities}  ecs-graph
   #{registered-as Lifetime installer injectors collections GameMode} di-graph
   #{asmdef-reachability layering}                                    Tools/asmdef_reach.py
   :doc-symbol-claims                                                 Tools/doc_lint.py
   :domain-term->code-anchor                                          GLOSSARY.md})
```

A live code symbol may appear in a doc only as an ANCHOR (bare name) — doc-lint verifies it exists.

---

## Rule Style — Clojure rule blocks (all categories)

Mechanizable rules — conditions→verdicts, invariants, checklists, do/don't lists — are written as
**Clojure rule blocks** inside a `clojure` code fence: real Clojure syntax (parses in a Clojure
editor), instruction semantics (nothing evaluates).

```text
(def subject {:key value …})   ;; named rule-set: the standing facts/invariants of ONE subject — THE default
{:key value}                   ;; tiny rule-set / single rule — a bare map, no def needed
(cond test result …)           ;; runtime branching: first true test wins, :else = fallback
(-> a b c)                     ;; pipeline / flow
```

- **`(def subject {…})`** — the workhorse: subject named once, one `:key value ;; why` entry per
  rule; align the value column. Keys are kebab-case concepts or constraint keys
  (`:requires :never :must-not :contains :only-when :exists-only-under :in`).
  A nested map = that entry's own fields (depth ≤ 2).
- **values** — bare symbol / exact API name = literal anchor (`EventCleanupSystem`); `"string"` =
  prose leaf (all fuzziness lives in quotes); `#{a b}` = equal alternatives; `:keyword` = verdict.
- **`;; why`** — one clause per entry. A "why" needing more than a clause is a design decision →
  prose section, not a rule entry.
- The reading glossary lives in ONE place: `~/.claude/CLAUDE.md` → "Clojure instruction notation".
  A new form/literal gets its glossary row there BEFORE it appears in any doc.
- `;; ── section label ───` divider comments group related entries inside one fence.

```clojure
(def conversion  ;; what converts to a rule block, what stays prose
  {#{invariants checklists do-donts condition->verdict} "Clojure rule block"
   #{intent design-rationale behavioral-contract}       "prose"              ;; the nuance IS the payload
   :code-skeleton     "its own language"                ;; C# stays C#
   :doc-reference     "bare name inside a fence"        ;; the doc↔doc edge lives in frontmatter related
   :extending-a-block "in kind"})                       ;; one new entry per rule — never grow it back into prose
```

---

## Document Categories

| Category | What it is | Files | Rule |
|---|---|---|---|
| **A — Flow contract** | The dated cross-domain contract of ONE behavior: event vocabulary, state ownership (`:now` vs `:target`), ordering invariants, gap list | `Flows/FLOW_*.md` | Diffable against the ecs-graph — a mismatch is drift to fix or a deliberate contract change. Code comments link here, never retell it. |
| **B — Template / Reference** | How to build new code, or how to use a tricky API | `Patterns/PATTERN_*.md` (incl. `ADDRESSABLE_PATTERNS.md`) | Do **not** strip. Keep accurate, keep complete. Examples use placeholder names (`Foo*`, `My*`) or live anchors that pass doc-lint. |
| **C — Policy** | Project-wide rules | `CLAUDE.md`, `ARCHITECTURE.md`, `ECS_CONVENTIONS.md`, `GENERAL_UI_STYLE.md`, `GLOSSARY.md`, this file | Rules and orientation. Change by user decision only. |

---

## Frontmatter (YAML properties)

Navigation only — doc-meta and doc↔doc relations, never code structure.

| Field | Required | Rule |
|---|---|---|
| `category` | all files | `A` \| `B` \| `C` per the table above |
| `read` | all files | `always` (session-start set, keep tiny) · `trigger` (+ one imperative `trigger:` line — it is the doc's INDEX entry) · `reference` |
| `tags` | encouraged | lowercase domain labels, never type/assembly names |
| `related` | omit if none | doc↔doc **relative markdown links** only |
| `status` | Category A only | `partial` \| `implemented` — how much of the flow's TARGET the code meets |
| `code_refs` | Category A only | bare symbol names the contract reasons about, nested by kind — doc-lint drift anchors |

After adding/removing/renaming a doc or changing `read`/`trigger`/`status`: re-run
`python3 Tools/gen_index.py` (the pre-commit hook enforces it).

---

## AI-First Writing Rules

- **Explicit over implicit.** State the rule; do not make the reader infer it.
- **Short declarative sentences.** One claim per sentence.
- **Concrete over abstract.** The specific case, not a general description.
- **Mark future/unbuilt loudly** (gap list entries, `:target` values) — never imply it exists.
- **No narrative.** No "first we… then we… finally". List facts.
- First content line ≤ 120 chars — it IS the INDEX description.

---

## Checklist Before Saving Any MD

- [ ] The content fits a non-rotting genre (flow contract / recipe / policy) — no present-tense
      mirror of code state, no rosters, no wiring, no priorities, no signatures.
- [ ] Frontmatter per the table; `python3 Tools/gen_index.py` re-run if doc-meta changed.
- [ ] `python3 Tools/doc_lint.py` reports no new ghosts for this doc.
- [ ] Mechanizable rules are Clojure rule blocks (Rule Style), not prose bullets.
- [ ] A junior model could act on this without reading the source.
