# SEARCH_POLICY

The law for how code knowledge is obtained in this project. Two readers:
- **`discovery-scout`** (the discovery agent) — its charter is embedded in `.claude/agents/discovery-scout.md`
  (kept in sync with this file; the scout does NOT read this file at runtime).
- **the main agent** — these are your **constraints**; the PreToolUse hook `.claude/hooks/search-gate.py`
  enforces the mechanizable subset. The hook is the teeth; this document is the law.

Style note: mechanizable rules in this file are written as s-expr decision tables (§2). When adding or
editing rules, extend the table in kind — one `(condition … ) → VERDICT ;; why` line per rule — instead
of adding prose.

---

## 0. Why this exists
Reading source files into the main agent's context, then embedding + reasoning over them, is the most
expensive and least reliable way to answer a question that a tool or a doc already answers. It also goes
stale. So the main agent's discovery order is: (a) the bounded `mcp__roslyn__*` tools or the ECS/DI graph
CLIs (`ecsg.py` / `dig.py`) for surgical code-structure / ECS / DI lookups; (b) a delegated trace to
`discovery-scout`, which uses roslyn + the graphs + docs and returns a *distilled* answer — not raw
dumps; (c) as a LAST-resort fallback, a small **budgeted** allowance of raw grep-family searches
(§3b — 4 per task, hook-counted). Grep is legal, but it is the fallback, never the first move: the
bounded tools answer better and cheaper. The main agent spends its budget on reasoning and edits,
not on raw output.

## 1. The model — bounded tools + one front door
```
main agent ──(surgical code-structure Q)──▶ mcp__roslyn__*   (bounded, direct)
main agent ──(surgical ECS / DI Q)────────▶ ecsg.py / dig.py (CLI, direct — gate-allowed)
main agent ──(asmdef reachability Q)──────▶ Tools/asmdef_reach.py (CLI, direct)
main agent ──(ECS/DI/orchestration/docs Q)─▶ @agent-discovery-scout ──▶ roslyn-mcp | ecs-graph | di-graph | module-MD ─┐
     ▲                                                                                                       │
     └──────────────────────────── distilled answer ◀───────────────────────────────────────────────────────┘
```
- **`mcp__roslyn__*` tool calls return bounded, structured results (not raw files), so the main agent MAY
  call them directly** — `search_symbols`, `go_to_definition`, `find_references`, `get_symbol_info`,
  `get_document_outline`, `find_callers`, `get_type_hierarchy`. The ban is on raw source *sweeps/reads*,
  not on bounded tool calls.
- **The ECS / DI graph CLIs (`ecsg.py` / `dig.py`) are now the interface (the typed MCP facades are
  RETIRED).** The gate ALLOWS the main agent to run them directly; their main-agent use is still governed
  by the TWO-CONDITION RULE (§1a): default ECS/DI discovery goes to `discovery-scout`. Both CLIs
  **auto-refresh** stale mechanical facts before answering (they run `build_graph.py --update` /
  `build_di_graph.py --update` when any `Assets/*.cs` changed) and print a stderr banner when STEP-2 AI
  curation is pending — so a query self-heals; only the AI curation stays a manual opt-in.
- **`Tools/asmdef_reach.py` answers Unity asmdef reachability / layering questions** ("can assembly A use
  type T?" + shortest reference path) — a gap roslyn/ecs/di cannot fill. Direct, gate-allowed.
- The **single front door for ECS/DI/orchestration/docs discovery and heavy multi-step traces is
  `discovery-scout`.** When the answer needs read/write classification, reactive flow, DI collection
  resolution, or doc semantics, delegate — do not search source yourself.
- The scout is allowed everything discovery needs (roslyn, graphs, docs, and source as a last resort). The
  main agent is not.

## 1a. The two-condition rule (main-agent direct tool use)
Default: discovery is the **scout's** job, not the main agent's. The main agent calls the discovery tools
(the `ecsg.py` / `dig.py` graph CLIs, and `mcp__roslyn__*` beyond a one-off surgical lookup) **directly
only when (a)** it is already editing code and needs a quick fact, **or (b)** information is critically
insufficient and delegating would stall progress. Otherwise → `@agent-discovery-scout`. The rule kills
the two failure modes it exists for: (1) constant mindless grep/read, and (2) cyclic search that bloats
context. It is **not hook-enforceable** (the hook cannot see "am I editing"), so it is on you, main agent.

**Escalation (anti-cycle):** if `discovery-scout` returns an UNANCHORED or EMPTY result **twice for the
same question**, do NOT send a third scout round on that question. Switch to the bounded tools yourself
(`mcp__roslyn__*`, the `ecsg.py` / `dig.py` CLIs) under this rule — or, if they cannot answer
it either, ask the user. Cyclic re-delegation is the same failure mode as cyclic search.

**No double-spend (the two-path trap).** Pick ONE path per question. If you delegated to `discovery-scout`,
TRUST its distilled answer — do NOT re-verify the same facts with your own `mcp__roslyn__*` / grep / reads;
a genuine gap goes back to the SAME scout as one follow-up, not a self-run re-derivation. And do NOT spawn a
(Sonnet) scout for a SMALL or already-anchored scope that a couple of bounded `mcp__roslyn__*` calls settle
directly — the scout earns its cost on broad multi-file traces, not on what one `search_symbols` + one
`find_references` answer. Scout-then-re-verify is the measured waste that turns a 5-tool task into 30.

## 1b. Delegation brief (required fields for every discovery-scout spawn)
An unanchored one-liner is what makes a scout wander (the pre-Sonnet median was 23 tool calls/run).
Every spawn prompt carries four fields:

```lisp
(question → ONE specific question)             ;; with a yes/no/list-shaped answer — a topic is not a question
(anchors  → symbols | files | GLOSSARY terms)  ;; where to start; none known → write "no anchor — check GLOSSARY first"
(shape    → expected output form)              ;; e.g. "list of (system, event, source_location)" / "yes/no + the chain"
(stop     → when to stop digging)              ;; e.g. "first producer found" / "≤2 tool rounds per lead, then report"
```

## 1c. The search flow (main agent) — walk in order, first match wins
Discovery is a **DISPATCH, not a reflex**: classify the question, then take the FIRST rung that answers it.
grep sits at the BOTTOM by construction — you reach it only after every rung above has nothing. The single
failure this ladder kills: reaching for grep/`find` before classifying (grep jumping the queue). This is the
ORDERED reading of §2 + §4; §2 is the same law as a mechanizable table, §4 is the per-question tool map.

```lisp
;; STEP 0 — is it even discovery?
(reading-to-edit :file-known         → Read, budgeted §3)         ;; editing ≠ discovery — not a search at all
(answerable-by-a-doc                 → INDEX.md → the doc)        ;; docs = the one direct knowledge layer (§4)

;; STEP 1 — a bounded tool OWNS the question → call it directly, then STOP (always allowed)
(asmdef reachability|layering|owner  → Tools/asmdef_reach.py)     ;; NEVER find/grep over *.asmdef
(code structure :one-off [defn|refs|callers|hierarchy|outline]   → mcp__roslyn__*)  ;; bounded, not raw files
(domain-term → code-anchor           → GLOSSARY.md, then the owning tool)

;; STEP 2 — tool-owned but under the two-condition rule (§1a): direct ONLY if editing-or-critical, else → STEP 3
(ECS edge [archetype|writes|reads|reactive|Table-Rule|priority]  → ecsg.py [§1a] | else scout)
(DI wiring [registered-as|lifetime|injects|collection|GameMode]  → dig.py  [§1a] | else scout)
(roslyn :as-a-discovery-sweep [not one-off]                      → [§1a]         | else scout)

;; STEP 3 — no single tool owns it → the front door
(semantics|intent|invariants|call-order                          → @agent-discovery-scout §1b)
(heavy multi-step | read/write | reactive | collection classify  → @agent-discovery-scout §1b)

;; STEP 4 — locating a FILE by name/glob (NOT its content)
(file-by-name|by-pattern             → Glob)                      ;; a path lookup is not a content search; never `find` (budget nuance §3b)

;; STEP 5 — LAST resort: raw content-substring, only when nothing above fits
(content-substring :no-tool-equivalent :grep-budget<4            → grep-family, counted §3b)  ;; a string literal / comment / a just-renamed type roslyn can't see yet (Freshness policy)
(grep-family :budget-spent                                       → @agent-discovery-scout)    ;; escalate — do NOT ask to bump (§3b)

;; the invariants the ladder encodes
(grep                → the FLOOR)                                 ;; every other rung is tried first; grep is a WASTE of the fallback allowance if a rung above can answer
(grep :legitimate-only-for content-substring w/o a tool equivalent)
(structural [asmdef|DI|ECS|refs]     → its owning tool, NEVER grep)
(file-location                       → Glob, its own lane, NEVER find|grep)
```

## 2. Decision table
This table is the **mechanizable form of the §1c flow** — the hook enforces the subset it can see; where the
table and the flow seem to differ, the §1c ORDER is the intent the table cannot express.

Verdict ∈ `ALLOW` · `DENY→scout` (delegate to `discovery-scout`) · `SCOUT` (must delegate, not mechanizable).
`session ∈ {main, subagent}`. Scope of "source" = `Assets/**/*.cs` only.

```
;; ── subagents are the discovery path: never gated ───────────────────────────
(any-tool                         :session subagent)                 → ALLOW

;; ── bounded code-structure tools: allowed for the main agent ────────────────
(mcp__roslyn__*                   :session main)                      → ALLOW        ;; bounded, not raw files
(bash ecsg.py|dig.py [query CLI]  :main :editing-or-critical)         → ALLOW        ;; graph CLIs = the interface; two-condition rule (§1a)
(bash ecsg.py|dig.py [query CLI]  :main :speculative)                 → SCOUT        ;; default ECS/DI discovery → scout
(bash asmdef_reach.py             :session main)                      → ALLOW        ;; asmdef reachability / layering — bounded CLI

;; ── source DISCOVERY by the main agent: budgeted fallback (§3b) ─────────────
(grep|glob :over Assets/**/*.cs   :session main :grep-budget<4)       → ALLOW (counted) ;; legal fallback, never the first move
(bash rg|ag|ack                   :session main :grep-budget<4)       → ALLOW (counted) ;; same counter
(bash grep|egrep|find :over Assets :session main :grep-budget<4)      → ALLOW (counted) ;; same counter
(grep-family                      :session main :grep-budget-spent)   → DENY→scout
(bash cat|head|tail|sed|awk       :over Assets/**/*.cs :session main) → DENY→Read    ;; bypasses the Read budget — use Read
(bash build_graph|build_di_graph  :session main)                      → ALLOW        ;; build = maintenance, not a query

;; ── source EDITING by the main agent: allowed, but bounded ──────────────────
(read Assets/**/*.cs :new-file    :session main :under-budget)        → ALLOW        ;; reading to edit
(read Assets/**/*.cs :same-file-again)                                → ALLOW        ;; re-read is free
(read Assets/**/*.cs :new-file    :session main :budget-reached)      → DENY→STOP    ;; see §3

;; ── never gated ─────────────────────────────────────────────────────────────
(read *.md | plan-doc             :session both)                      → ALLOW    ;; docs = the one direct knowledge layer
(read .ecs-graph/ | .di-graph/ artifact)                              → use ecsg.py / dig.py  ;; never raw-read the JSON
(edit | write | task/agent | git | build-tools)                       → ALLOW

;; ── intent rule (not mechanizable; on you, main agent) ──────────────────────
(question :answerable-by roslyn-mcp|ecs-graph|di-graph|module-MD)     → use the tool / SCOUT, never source
```

### Per-rule notes
- **`DENY→scout`** denials return a reason the main agent sees; on it, either call the bounded
  `mcp__roslyn__*` tool / the `ecsg.py`|`dig.py` CLI yourself (§1a), or delegate the same question to
  `@agent-discovery-scout` — do not work around the gate with raw source.
- **Grep-family searches are a budgeted fallback, not a workflow.** The 4-op allowance (§3b) exists for
  the "just-added/renamed type" freshness gap and quick anchoring when the bounded tools have nothing.
  Spending it on speculative sweeps that roslyn/`ecsg`/`dig` would answer is a waste of the allowance —
  and once it is spent, the deny routes you to the scout for the rest of the task.
- **Reading to edit is legitimate.** To change a class you must read it — that is `ALLOW` (under budget).
  The ban is on *discovery*, not *editing*.
- **Graph QUERY CLIs (`ecsg.py`/`dig.py`) are the interface and the gate ALLOWS them for the main agent**
  (the typed ECS/DI graph MCP facades are RETIRED). Their use still follows the two-condition rule (§1a):
  default ECS/DI discovery is the scout's, direct calls only when editing or critically blocked. Each
  query **auto-refreshes** stale mechanical facts first (runs the incremental `--update`) and prints a
  stderr banner when STEP-2 AI curation is pending. **Build scripts (`build_graph`/`build_di_graph`) are
  NOT gated** — they only write artifacts, so a `--force` re-build is maintenance you may run directly.
- **`Tools/asmdef_reach.py` is NOT gated** — a bounded reachability CLI for asmdef layering questions
  roslyn/ecs/di cannot answer; run it directly.
- **Docs (`.md`, incl. the plan file) are the ONE knowledge layer both agents read directly** — the
  curated why-only layer, meant to be read straight. Everything else stays behind a tool: the
  `.ecs-graph/`/`.di-graph/` artifacts are served by the `ecsg.py` / `dig.py` CLIs and must **never** be
  raw-read; `Assets/**/*.cs` source is gated. **Discovery is the scout's job — the main agent reaches for
  a discovery tool only as a fallback (§1a), never as the first move.**

### Freshness policy (new/renamed types)
- **For JUST-ADDED or RENAMED types, prefer ecs-graph (`ecsg.py`) or grep over roslyn.** ecs-graph runs
  tree-sitter over source and is fresh immediately; roslyn works over the Unity-generated `.csproj`
  workspace and **cannot see a new or renamed type until Unity regenerates the project** (a round-trip).
  A `--force` rebuild + STEP-2 curation is the fix after a rename; a bare query already auto-`--update`s
  for freshly-edited (non-renamed) files.

## 3. The `.cs` read budget (main agent)
- **A "session" = ONE task.** The user runs `/clear` or `/compact` between tasks; budgets are re-armed
  per task by the `task` ritual below, not per conversation.
- **Limit: 12 unique `.cs` files per task.** Counts unique files; re-reading a counted file is free.
- **The new-task ritual (main agent runs it itself).** Immediately after the user confirms the task
  statement, run ONE command:
  `python3 .claude/hooks/search-gate.py task <path...>` — it resets all budgets (read + grep) AND
  registers the `.cs` files the user named in the statement's «Працюй тільки в» block as granted task
  scope (reads of them do not consume the budget). The user's confirmation of the statement IS the
  authorization; never grant a file the user did not name, and never grant a folder wildcard. A grant
  relaxes ONLY the read budget — the grep budget (§3b) is unaffected.
- **On exhaustion the hook denies and you must STOP.** Do not keep reading, do not route around it.
  Instead, write the user a short request: *what* you are looking for, *what* you already found, and *why*
  more source is needed. The user can answer directly, or authorize more. (Most code-structure questions
  should be answered by `mcp__roslyn__*` instead of a source read.)
- **Extension (user-authorized only):**
  - `python3 .claude/hooks/search-gate.py bump <N>` — raise the read limit to N.
  - `python3 .claude/hooks/search-gate.py reset` — fresh allowance now (also clears grants).
  - `python3 .claude/hooks/search-gate.py grant <path...>` — add task-scope files without a reset.

## 3b. The grep budget (main agent)
- **Limit: 4 grep-family ops per task**, one shared counter for: `Grep`/`Glob` over source, Bash
  `rg`/`ag`/`ack`, Bash `grep`/`egrep`/`find` over `Assets`. Reset by the `task` ritual (§3).
- **It is a fallback allowance, not a search workflow.** Intended uses: anchoring a just-added/renamed
  type roslyn cannot see yet (§ Freshness policy), or one quick existence check before editing. The
  bounded tools (`mcp__roslyn__*`, `ecsg.py`/`dig.py`) stay the FIRST move — they answer better and
  do not spend the allowance.
- **On exhaustion: deny→scout.** Delegate the question to `@agent-discovery-scout`; do not ask the user
  to bump this one — if 4 greps did not anchor it, the scout's tool stack is the right escalation.

## 3a. Reading discipline (HOW to read source, once a read is legitimate)
Direct procedure — follow it literally; do not improvise a cheaper-looking shortcut:
1. **File ≤ ~200 lines → ONE full `Read`.** A full read of a small file is cheaper than several
   roslyn round-trips. Do not outline it first.
2. **File > ~200 lines → `mcp__roslyn__get_document_outline` FIRST** (~300 tokens for the whole
   structure), then `Read` ONLY the fragments you need via `offset`/`limit`. Never full-`Read` a
   large file to "get oriented" — the outline is the orientation. Most ECS files in this repo sit
   at 200–300 lines: outline-first is the expected default for them.
3. **Generic call-site semantics are NOT a reading problem.** What `Set<T>`/`With<T>`/`Register<T>`/
   `[Inject]` sites MEAN (read vs write, reactive trigger, archetype, DI wiring) comes from
   `ecs-graph`/`di-graph` (§4) — Roslyn sees the call but cannot classify it, and reading more
   source will not tell you either. Do not re-derive graph facts from source.
4. **Budget note:** a fragment `Read` counts exactly like a full one — 1 unique file (§3). The
   fragment discipline saves your CONTEXT, not your budget; re-reads of a counted file are free,
   so narrowing with the outline first costs nothing extra.

## 4. Source → tool/doc mapping (use these, not source)
| Question | Go to |
|---|---|
| general code structure / definition / references / call-or-type hierarchy / outline / blast-radius | **`roslyn-mcp`** (`mcp__roslyn__*`) — directly, or via scout |
| entity archetype / who writes-or-reads a component / reactive event consumers / producer→consumer / AsSet-AsMap-AsMultiMap bindings / Table-Rule PK-FK / system role+priority | **`ecs-graph`** — the `ecsg.py` CLI (main agent: two-condition rule §1a) or via scout |
| what a type is registered AS / its Lifetime / which installer / who injects it / what fills a collection injection (`IReadOnlyList<T>`) / which `GameMode` a system runs in (Boot composition) | **`di-graph`** — the `dig.py` CLI (main agent: two-condition rule §1a) or via scout |
| can assembly A use type T? / shortest asmdef reference path / which assembly owns a type / asmdef layering & boundaries | **`Tools/asmdef_reach.py`** (`can` / `path` / `refs` / `assembly-of`) — directly, or via scout |
| a domain term (any language) → its canonical code names to search from | **`GLOSSARY.md`** (root) — read the term's `:anchors`, feed them to roslyn/ecsg/dig |
| intent, invariants, side-effects, ownership, call order, current state | **module-MD** (via scout) |
| visual map of entities / economy / relations | **`.canvas`** via `Tools/read_canvas.sh` (via scout) |
| architecture policy, conventions, taxonomy | `ARCHITECTURE.md` |

**How docs & canvases are read (Obsidian-first — this repo is an Obsidian vault):**
- **Entry point = `INDEX.md`.** It is the generated doc map and the single key to every doc + canvas
  (read-priority + one-liner each). Load it FIRST for any doc/canvas navigation, then open ONLY what it
  sends you to — never wander the vault.
- `.md` docs → Obsidian MCP `mcp__obsidian__vault_read` / `search_query` / `search_simple` when the
  `obsidian` server is connected (Obsidian open, HTTP server on); **fallback to plain `Read`** if not.
- `.canvas` → **`Tools/read_canvas.sh <file.canvas>`** — a jq projection of node text/label + edges that
  drops the noise (positions/sizes/colors). The Obsidian MCP cannot project inside a JSON canvas (it
  returns the raw file), so use the script. A full `vault_read`/`Read` of a `.canvas` is ONLY for editing
  its layout.

## 5. Scout charter (discovery-scout)
The operative charter lives IN the agent definition (`.claude/agents/discovery-scout.md`) — the scout
does not read this file at runtime. This section is the normative summary; when editing either, keep
the two in sync.
- You are the discovery front door; the main agent depends on you.
- Pick the source by §4. Prefer `roslyn-mcp`/`ecs-graph`/`di-graph`/module-MD; read source only after a
  tool narrows to a specific file, minimum fragment, `source_location` first. For ECS/DI questions use the
  `ecsg.py` / `dig.py` CLIs (via Bash) — they auto-refresh stale facts and banner pending curation.
- **Docs are Obsidian-first, and `INDEX.md` is the init access point:** for any doc/canvas question, load
  `INDEX.md` FIRST (the doc map), then open only what it points to. Read `.md` via
  `mcp__obsidian__vault_read` / `search_query` when the `obsidian` server is connected, else plain `Read`.
  Read `.canvas` via `Tools/read_canvas.sh` (never raw — drop node positions).
- Return a **distilled** report (symbols, signatures, `source_location`, the chain that answers the
  question). Never raw graph dumps. If empty/ambiguous, say so and state what you narrowed to.
- **Label a no-answer verdict explicitly** — start the report with `UNANCHORED` (claims found but no
  anchor) or `EMPTY` (nothing found). The main agent counts these: two such rounds on one question
  trigger its escalation to direct bounded tools (§1a). Padding an unanchored guess breaks that
  circuit-breaker — never do it.
- Read-only. Never edit, never propose code unless asked.
