# SEARCH_POLICY

The law for how code knowledge is obtained in this project. Two readers:
- **`graphify-scout`** (the discovery agent) — this is your **charter**; read it FIRST, every run.
- **the main agent** — these are your **constraints**; the PreToolUse hook `.claude/hooks/search-gate.py`
  enforces the mechanizable subset. The hook is the teeth; this document is the law.

---

## 0. Why this exists
Reading source files into the main agent's context, then embedding + reasoning over them, is the most
expensive and least reliable way to answer a question that a graph or a doc already answers. It also goes
stale. So: **the main agent does NOT do discovery.** Discovery is delegated to `graphify-scout`, which
reads docs + graphs (and, only as a last resort, source), and returns a *distilled* answer — not raw
dumps. The main agent spends its budget on reasoning and edits, not on raw output.

## 1. The model — one front door
```
main agent ──(question about the code)──▶ @agent-graphify-scout ──▶ ecs-graph | graphify | module-MD ─┐
     ▲                                                                                                │
     └──────────────────────────── distilled answer ◀──────────────────────────────────────────────┘
```
- The **single discovery front door is `graphify-scout`.** When you (main agent) need to know *where /
  what / who-calls / who-reads-writes / how-connected*, you delegate — you do not search source yourself.
- The scout is allowed everything discovery needs (graphs, docs, and source as a last resort). The main
  agent is not.

## 2. Decision table
Verdict ∈ `ALLOW` · `DENY→scout` (delegate to `graphify-scout`) · `SCOUT` (must delegate, not mechanizable).
`session ∈ {main, subagent}`. Scope of "source" = `Assets/**/*.cs` only.

```
;; ── subagents are the discovery path: never gated ───────────────────────────
(any-tool                         :session subagent)                 → ALLOW

;; ── source DISCOVERY by the main agent: forbidden, delegate ─────────────────
(grep|glob                        :over Assets/**/*.cs :session main) → DENY→scout
(bash rg|ag|ack                   :session main)                      → DENY→scout
(bash grep|egrep|find             :over Assets        :session main)  → DENY→scout
(bash ecsg|build_graph|graphify   :session main)                      → DENY→scout   ;; even the graph CLI

;; ── source EDITING by the main agent: allowed, but bounded ──────────────────
(read Assets/**/*.cs :new-file    :session main :under-budget)        → ALLOW        ;; reading to edit
(read Assets/**/*.cs :same-file-again)                                → ALLOW        ;; re-read is free
(read Assets/**/*.cs :new-file    :session main :budget-reached)      → DENY→STOP    ;; see §3

;; ── never gated ─────────────────────────────────────────────────────────────
(read *.md | config | graph-artifact | plan | any non-Assets-.cs)     → ALLOW
(edit | write | task/agent | git | build-tools)                       → ALLOW

;; ── intent rule (not mechanizable; on you, main agent) ──────────────────────
(question :answerable-by ecs-graph|graphify|module-MD)                → SCOUT        ;; never read source for it
```

### Per-rule notes
- **`DENY→scout`** denials return a reason the main agent sees; on it, immediately delegate the same
  question to `@agent-graphify-scout` instead of working around the gate.
- **Reading to edit is legitimate.** To change a class you must read it — that is `ALLOW` (under budget).
  The ban is on *discovery*, not *editing*.
- **Graph CLI (`ecsg`/`graphify`) is gated for the main agent too.** Running it yourself dumps raw graph
  output into your context. Let the scout run it and hand you the distilled facts.
- **`*.md`, configs, `.ecs-graph/`/`graphify-out/` artifacts, the plan file** are always readable.

## 3. The `.cs` read budget (main agent)
- **Limit: 8 unique `.cs` files per session.** Counts unique files; re-reading a counted file is free.
- **On exhaustion the hook denies and you must STOP.** Do not keep reading, do not route around it.
  Instead, write the user a short request: *what* you are looking for, *what* you already found, and *why*
  more source is needed. The user can answer directly, or authorize more.
- **Extension (user-authorized only):**
  - `python3 .claude/hooks/search-gate.py reset` — fresh allowance now.
  - `python3 .claude/hooks/search-gate.py bump <N>` — raise the limit to N.

## 4. Source → graph/doc mapping (use these, not source)
| Question | Go to |
|---|---|
| entity archetype / who writes-or-reads a component / reactive event consumers / event producer→consumer / Table-Rule PK-FK / system role+priority | **`ecs-graph`** (via scout) |
| general call / import / dependency / orchestration chain / blast-radius | **`graphify`** (via scout) |
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

## 5. Scout charter (graphify-scout)
- Read this file FIRST. You are the single discovery front door; the main agent depends on you.
- Pick the source by §4. Prefer `ecs-graph`/`graphify`/module-MD; read source only after the graph
  narrows to a specific file, minimum fragment, `source_location` first.
- **Docs are Obsidian-first, and `INDEX.md` is the init access point:** for any doc/canvas question, load
  `INDEX.md` FIRST (the doc map), then open only what it points to. Read `.md` via
  `mcp__obsidian__vault_read` / `search_query` when the `obsidian` server is connected, else plain `Read`.
  Read `.canvas` via `Tools/read_canvas.sh` (never raw — drop node positions).
- Return a **distilled** report (symbols, signatures, `source_location`, the chain that answers the
  question). Never raw graph dumps. If empty/ambiguous, say so and state what you narrowed to.
- Read-only. Never edit, never propose code unless asked.
