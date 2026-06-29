# SEARCH_POLICY

The law for how code knowledge is obtained in this project. Two readers:
- **`discovery-scout`** (the discovery agent) — this is your **charter**; read it FIRST, every run.
- **the main agent** — these are your **constraints**; the PreToolUse hook `.claude/hooks/search-gate.py`
  enforces the mechanizable subset. The hook is the teeth; this document is the law.

---

## 0. Why this exists
Reading source files into the main agent's context, then embedding + reasoning over them, is the most
expensive and least reliable way to answer a question that a tool or a doc already answers. It also goes
stale. So: **the main agent does NOT do raw source discovery.** It either (a) calls the bounded
`mcp__roslyn__*` tools directly for surgical code-structure lookups, or (b) delegates a trace to
`discovery-scout`, which uses roslyn + the graphs + docs and returns a *distilled* answer — not raw dumps.
The main agent spends its budget on reasoning and edits, not on raw output.

## 1. The model — bounded tools + one front door
```
main agent ──(surgical code-structure Q)──▶ mcp__roslyn__*  (bounded, direct)
main agent ──(ECS/DI/orchestration/docs Q)─▶ @agent-discovery-scout ──▶ roslyn-mcp | ecs-graph | di-graph | module-MD ─┐
     ▲                                                                                                       │
     └──────────────────────────── distilled answer ◀───────────────────────────────────────────────────────┘
```
- **`mcp__roslyn__*` tool calls return bounded, structured results (not raw files), so the main agent MAY
  call them directly** — `search_symbols`, `go_to_definition`, `find_references`, `get_symbol_info`,
  `get_document_outline`, `find_callers`, `get_type_hierarchy`. The ban is on raw source *sweeps/reads*,
  not on bounded tool calls.
- **`mcp__ecs-graph__*` / `mcp__di-graph__*` (the typed ECS / DI graph facades) are bounded too, but their
  main-agent use is governed by the TWO-CONDITION RULE (§1a): default ECS/DI discovery goes to
  `discovery-scout`.**
- The **single front door for ECS/DI/orchestration/docs discovery and heavy multi-step traces is
  `discovery-scout`.** When the answer needs read/write classification, reactive flow, DI collection
  resolution, or doc semantics, delegate — do not search source yourself.
- The scout is allowed everything discovery needs (roslyn, graphs, docs, and source as a last resort). The
  main agent is not.

## 1a. The two-condition rule (main-agent direct tool use)
Default: discovery is the **scout's** job, not the main agent's. The main agent calls the discovery MCP
tools (`mcp__ecs-graph__*` / `mcp__di-graph__*`, and `mcp__roslyn__*` beyond a one-off surgical lookup) **directly only when
(a)** it is already editing code and needs a quick fact, **or (b)** information is critically insufficient
and delegating would stall progress. Otherwise → `@agent-discovery-scout`. The rule kills the two failure
modes it exists for: (1) constant mindless grep/read, and (2) cyclic search that bloats context. It is
**not hook-enforceable** (the hook cannot see "am I editing"), so it is on you, main agent.

## 2. Decision table
Verdict ∈ `ALLOW` · `DENY→scout` (delegate to `discovery-scout`) · `SCOUT` (must delegate, not mechanizable).
`session ∈ {main, subagent}`. Scope of "source" = `Assets/**/*.cs` only.

```
;; ── subagents are the discovery path: never gated ───────────────────────────
(any-tool                         :session subagent)                 → ALLOW

;; ── bounded code-structure tools: allowed for the main agent ────────────────
(mcp__roslyn__*                   :session main)                      → ALLOW        ;; bounded, not raw files
(mcp__ecs-graph__* | mcp__di-graph__*  :main :editing-or-critical)    → ALLOW        ;; two-condition rule (§1a)
(mcp__ecs-graph__* | mcp__di-graph__*  :main :speculative)            → SCOUT        ;; default ECS/DI discovery → scout

;; ── source DISCOVERY by the main agent: forbidden, delegate ─────────────────
(grep|glob                        :over Assets/**/*.cs :session main) → DENY→scout
(bash rg|ag|ack                   :session main)                      → DENY→scout
(bash grep|egrep|find             :over Assets        :session main)  → DENY→scout
(bash ecsg|dig.py  [query CLI]    :session main)                      → DENY→scout   ;; raw text dump → MCP/scout
(bash build_graph|build_di_graph  :session main)                      → ALLOW        ;; build = maintenance, not a query

;; ── source EDITING by the main agent: allowed, but bounded ──────────────────
(read Assets/**/*.cs :new-file    :session main :under-budget)        → ALLOW        ;; reading to edit
(read Assets/**/*.cs :same-file-again)                                → ALLOW        ;; re-read is free
(read Assets/**/*.cs :new-file    :session main :budget-reached)      → DENY→STOP    ;; see §3

;; ── never gated ─────────────────────────────────────────────────────────────
(read *.md | config | graph-artifact | plan | any non-Assets-.cs)     → ALLOW
(edit | write | task/agent | git | build-tools)                       → ALLOW

;; ── intent rule (not mechanizable; on you, main agent) ──────────────────────
(question :answerable-by roslyn-mcp|ecs-graph|di-graph|module-MD)     → use the tool / SCOUT, never source
```

### Per-rule notes
- **`DENY→scout`** denials return a reason the main agent sees; on it, either call the bounded
  `mcp__roslyn__*` tool yourself, or delegate the same question to `@agent-discovery-scout` — do not work
  around the gate with raw source.
- **Reading to edit is legitimate.** To change a class you must read it — that is `ALLOW` (under budget).
  The ban is on *discovery*, not *editing*.
- **Graph QUERY CLI (`ecsg`/`dig.py`) is gated for the main agent** — running it dumps raw graph output
  into your context; use the bounded `mcp__ecs-graph__*` tools or let the scout hand you distilled facts.
  **Build scripts (`build_graph`/`build_di_graph`) are NOT gated** — they only write artifacts, so a
  `--force` re-build is maintenance you may run directly.
- **`*.md`, configs, `.ecs-graph/`/`.di-graph/` artifacts, the plan file** are always readable.

## 3. The `.cs` read budget (main agent)
- **Limit: 8 unique `.cs` files per session.** Counts unique files; re-reading a counted file is free.
- **On exhaustion the hook denies and you must STOP.** Do not keep reading, do not route around it.
  Instead, write the user a short request: *what* you are looking for, *what* you already found, and *why*
  more source is needed. The user can answer directly, or authorize more. (Most code-structure questions
  should be answered by `mcp__roslyn__*` instead of a source read.)
- **Extension (user-authorized only):**
  - `python3 .claude/hooks/search-gate.py reset` — fresh allowance now.
  - `python3 .claude/hooks/search-gate.py bump <N>` — raise the limit to N.

## 4. Source → tool/doc mapping (use these, not source)
| Question | Go to |
|---|---|
| general code structure / definition / references / call-or-type hierarchy / outline / blast-radius | **`roslyn-mcp`** (`mcp__roslyn__*`) — directly, or via scout |
| entity archetype / who writes-or-reads a component / reactive event consumers / producer→consumer / AsSet-AsMap-AsMultiMap bindings / Table-Rule PK-FK / system role+priority | **`ecs-graph`** — typed MCP `mcp__ecs-graph__*` (main agent: two-condition rule §1a) or via scout |
| what a type is registered AS / its Lifetime / which installer / who injects it / what fills a collection injection (`IReadOnlyList<T>`) / which `GameMode` a system runs in (Boot composition) | **`di-graph`** — typed MCP `mcp__di-graph__*` (main agent: two-condition rule §1a) or via scout |
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
- Read this file FIRST. You are the discovery front door; the main agent depends on you.
- Pick the source by §4. Prefer `roslyn-mcp`/`ecs-graph`/`di-graph`/module-MD; read source only after a
  tool narrows to a specific file, minimum fragment, `source_location` first. For ECS questions prefer the
  typed MCP `mcp__ecs-graph__*` over the `ecsg` CLI (the CLI is build/fallback).
- **Docs are Obsidian-first, and `INDEX.md` is the init access point:** for any doc/canvas question, load
  `INDEX.md` FIRST (the doc map), then open only what it points to. Read `.md` via
  `mcp__obsidian__vault_read` / `search_query` when the `obsidian` server is connected, else plain `Read`.
  Read `.canvas` via `Tools/read_canvas.sh` (never raw — drop node positions).
- Return a **distilled** report (symbols, signatures, `source_location`, the chain that answers the
  question). Never raw graph dumps. If empty/ambiguous, say so and state what you narrowed to.
- Read-only. Never edit, never propose code unless asked.
