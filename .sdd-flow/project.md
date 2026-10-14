# Identity

```clojure
{:project FantasyMayor
 :adapter-version 1
 :written-at "2026-08-30"
 :written-by "sdd-project-init, confirmed by the project owner"
 :case :origin}   ;; the project carried a hand-maintained copy of the canon (see # Canon copy)
```

# Entry

```clojure
{:entry INDEX.md                       ;; AGENTS.md § 1 Session start; a doc INDEX does not list is invisible by rule
 :read-always #{AGENTS.md
                "any Category A FLOW with status: partial — reconstruct its stage, findings, decisions, disproven, attempted"}
 :never-preload #{"anything INDEX.md does not send you to"}
 :read-priority "INDEX carries always | trigger | reference per doc — follow it, do not wander the vault"
 :task-docs "AGENTS.md § 2 request-routing fills the statement's :read from INDEX triggers (+ ARCHITECTURE.md for an engineering task)"}
```

# Tools

```clojure
[{:tool roslyn
  :is "C# structure over FantasyMayor.sln"
  :answers "declarations, references, callers, type hierarchy, diagnostics"
  :prefer-when "a symbol question has a definite answer in source"
  :never-for "generic-typed ECS/DI relationships — roslyn is blind to them"
  :use-when #{tool-who-calls tool-code-name-of-term tool-where-asset-used}
  :invoke "mcp__roslyn__*"}

 {:tool fantasymayor-graph
  :is "derived code graph: ECS (Friflo.Engine.ECS) + DI (VContainer) + recipe instances"
  :answers "archetypes with main and label tags, which system writes/reads a component, event producer→consumer, Table-Rule PK/FK, singletons; what a type is registered as, Lifetime, installer, injectors, collection fill, the game state each system runs in; system roles; instances of the 15 Patterns/ recipes"
  :prefer-when "the question is about ECS or DI relationships or recipe instances rather than syntax"
  :never-for "hand-reading or hand-editing the graph artifacts — the graph-gate hook denies it"
  :use-when #{tool-who-writes-component tool-di-wiring tool-recipe-instances}
  :invoke "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py <command> (rebuild: build)"}

 {:tool doc_lint
  :is "symbol-claim linter over the vault's .md files"
  :answers "whether a symbol a document names still exists in C#"
  :prefer-when "a surviving doc makes a claim about code — the claim is a hypothesis until this passes"
  :invoke "python3 Tools/doc_lint.py [--quiet]"}

 {:tool asmdef_reach
  :is "assembly-reference reachability check"
  :answers "whether a consuming .asmdef can see the types new code will use"
  :prefer-when "new code consumes types across an asmdef boundary — run before asking for GO"
  :use-when #{tool-can-assembly-see}
  :invoke "python3 Tools/asmdef_reach.py"}

 {:tool arch-check
  :is "architectural ban detector"
  :answers "stateful systems, System.Collections.Generic usage, managed-collection allocation in systems"
  :never-for "auto-fixing — it detects only"
  :invoke "/arch-check"}

 {:tool unity-asset-graph
  :is "Unity asset reachability graph"
  :answers "what ships in the build, how an asset got there, where it is used, what is dead, serialized [SerializeField] enum values"
  :use-when #{tool-where-asset-used}
  :invoke "/unity-asset-graph"}

 {:tool unity-mcp
  :is "the official Unity MCP server built into the Unity CLI (unity mcp), backed by the project's com.unity.pipeline package"
  :answers "the live Editor's own view: asset database, addressable entries, hierarchy, commands the Editor exposes; whether the scripts compile (recompile, recompile_status) and what the console says (console, console_status)"
  :prefer-when "a question only the running Editor answers — an address → asset, what a live scene holds"
  :never-for "Unity-side authoring (prefabs, .asset, scenes, addressable entries) without the owner's word, player builds, Play mode — AGENTS.md § 3"
  :use-when #{tool-where-asset-used}
  :invoke "mcp__unity__* (editor_status first; find_assets, search, get_scene_hierarchy …) — the server lives in the Claude Desktop config, --project-path pinned; without it: unity command --project-path <repo> --caller plugin --skill <skill>"
  :replaced "the third-party mcp-for-unity (uvx mcpforunityserver) — removed 2026-09-23"}

 {:tool GLOSSARY.md
  :is "domain vocabulary → code anchor map"
  :answers "the canonical code name behind a domain term, in any language"
  :prefer-when "before searching roslyn / fantasymayor-graph for a term the user named in prose"
  :use-when #{tool-code-name-of-term}}

 {:tool obsidian-mcp
  :is "the vault's search and navigation path"
  :answers "where a doc or a section lives"
  :never-for "editing .md — plain Read/Edit/Write is the edit path (heading-targeted patching was this project's top tool-error source)"
  :invoke "mcp__obsidian__search_query | search_simple | vault_get_document_map | vault_read"}

 {:tool read_canvas
  :is "jq projection over a .canvas file"
  :answers "node text/labels and edges without positions"
  :prefer-when "reading a canvas; full vault_read only when layout must be edited"
  :invoke "Tools/read_canvas.sh <file.canvas>"}

 {:tool context7
  :is "library and API documentation lookup"
  :prefer-when "library/API usage, setup or configuration steps are in doubt"
  :invoke "mcp__plugin_context7_context7__*"}]
```

```clojure
{:discovery-default "the main agent's own path: the generated tool skills (.sdd-flow/tool-skills.md) → roslyn / graph CLIs → targeted code reads"
 :subagents :abandoned      ;; the owner dropped the project's own search/orchestration scouts — do not spawn them
 :retired "the «Discovery Scouts» section and the .claude/agents/*.md scout definitions were removed 2026-08-30"}
```

# Meters

```clojure
[{:meter "python3 Tools/doc_lint.py --quiet"
  :target "ghost-claim count not grown"
  :when :docs-changed}

 {:meter "python3 Tools/gen_index.py"
  :target "INDEX skeleton regenerated, lint clean"
  :when "a doc's frontmatter or first line changed"}

 {:meter "mcp__unity__recompile → recompile_status → console_status"
  :target "completed or up_to_date; compilationFailed false; no console error the change introduced"
  :when :code-changed}

 {:meter "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py check"
  :target "curated: true, integrity clean, no new warnings"
  :when #{:ecs-changed :di-changed}}]
```

```clojure
{:code-verification "AGENTS.md § 2 (def code-verification) — roslyn diagnostics, /arch-check, fmgraph.py tags, the owner's Unity check; joins :accept of every statement that changes code"
 :project-skills    "AGENTS.md § 2 :skills — fantasymayor-pattern-choice, fantasymayor-placement (.claude/skills/)"}
```

# Bans

```clojure
{:home "AGENTS.md § 3 Project bans — (def bans); the owner keeps project bans there so they bind every session"
 :enforced-elsewhere ".claude/hooks/graph-gate.py (PreToolUse deny/ask) and Tools/githooks/pre-commit"}
```

# Ceremonies

```clojure
[{:ceremony index-regen
  :runs-at "after any frontmatter or first-line change"
  :owner "python3 Tools/gen_index.py (pass 1) + the agent's curated zone (pass 2)"}

 {:ceremony placement-check
  :runs-at #{"the survey (sdd-code-survey), before any finding turns into an option that places, moves or deletes a type or file or touches an asmdef reference"
             "the algorithm and structure steps (sdd-algorithm-sketch contra, sdd-code-structure) — for every file, type, asmdef reference and registration the structure names, before the gate"
             "a direct task — before the statement or any option is shown"}
  :owner "the project skill fantasymayor-placement — its verdicts go into the finding, the structure's # Traps or the statement's :skills; a forbidden placement is dropped from the option set"
  :why "2026-09-26: three options were offered that the layering law forbade — the check ran after the owner answered, not before"}

 {:ceremony graph-rebuild
  :runs-at "after changing an ECS archetype, DI wiring or a marker"
  :owner "fmgraph.py build — deterministic, one pass, no LLM"}]
```

# Shape

```clojure
{:flow-home "the canon's — Flows/<TASK>/FLOW.md; legacy flat Flows/FLOW_<TASK>.md exist only in Flows/Archive/"
 :sections "the canon's .sdd-flow/templates/ — no project template"   ;; FLOW_TEMPLATE.md and RESEARCH_TEMPLATE.md deleted 2026-09-14
 :lifecycle "the canon's"
 :adds {:frontmatter "DOC_STANDARD.md → Frontmatter: active FLOW read: always + status: partial; archived read: archive + status: implemented — so INDEX.md sees active work"}
 :overrides "none"}
```

# Canon copy

```clojure
{:files #{AGENTS.md}
 :status :reference                     ;; separated 2026-08-30 — the agent doc (then CLAUDE.md) now measures 0 in sync, 0 drifted
 :was {:at "2026-08-30" :measured "CLAUDE.md and the old AGENTS.md, both 8 in sync, 10 drifted, 8 local-only, 16 not copied"}
 :residue-moved-here #{prior-art done-contract deep-research attempted disproven}
 :deliberate-patches []                 ;; flow-progress and blocked-outcome deleted 2026-09-14 — 0.3.0 carries # Progress and the blocked branch; owner: no local patches
 :project-scoped #{notation-ecs-ext request-routing bans commit}   ;; the project's own rules in AGENTS.md — local-only by right, never promotion candidates
 :renamed-canon #{research-depth task-normalization}   ;; deleted with the copy — bodies matched (def research) / (def normalization)
 :retired "the Codex-era AGENTS.md (the name was reused 2026-09-26 when CLAUDE.md moved here), Tools/gen_agents.py, the session-start canon-sync step and the pre-commit branch that guarded them (owner's decision, 2026-08-30)"}
```

# Extension

```clojure
{:footprint #{AGENTS.md}               ;; one marked block: BEGIN SDD-FLOW: pointer … END SDD-FLOW: pointer
 :exclusive false                      ;; the copy IS this canon — after separation the project runs one framework, not two
 :notation-ecs-ext "AGENTS.md declares a project-scoped ECS reading of the notation; it stays project-scoped"
 :openspec :removed-2026-08-05         ;; the CLI remains installed; it returns only on the owner's direct ask
 :tools #{claude}                      ;; codex dropped from config.json 2026-09-14 (0.3.0 update) — no .agents/, no Codex skills, repo or global
 :updating "any git checkout of the package regenerates the managed files: `npx --yes github:zakyrion/sdd-flow update .`, a global install from github, or a pulled checkout the global CLI is linked to — the owner has not fixed one; `update` never touches this file"
 :tooling "Tools/gen_index.py PRUNE and Tools/doc_lint.py SKIP_DIRS must keep .sdd-flow — the framework's own .md files carry no category/read frontmatter, and giving them any would break `update`"}
```
