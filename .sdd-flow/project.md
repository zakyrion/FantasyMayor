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
{:entry INDEX.md                       ;; decreed by CLAUDE.md → "Start Working"; a doc INDEX does not list is invisible by rule
 :read-always #{ARCHITECTURE.md
                CLAUDE.md
                "any Category A FLOW with status: partial — reconstruct its stage, findings, decisions, disproven, attempted"}
 :never-preload #{GAMEPLAY_FOUNDATION.md            ;; explicit user request only — never on session start, never by keyword
                  "anything INDEX.md does not send you to"}
 :read-priority "INDEX carries always | trigger | reference per doc — follow it, do not wander the vault"}
```

# Tools

```clojure
[{:tool roslyn
  :is "C# structure over FantasyMayor.sln"
  :answers "declarations, references, callers, type hierarchy, diagnostics"
  :prefer-when "a symbol question has a definite answer in source"
  :never-for "generic-typed ECS/DI relationships — roslyn is blind to them"
  :invoke "mcp__roslyn__*"}

 {:tool ecs-graph
  :is "derived DoD/ECS graph (Friflo.Engine.ECS)"
  :answers "archetypes, which system writes/reads a component, event producer→consumer, Table-Rule PK/FK, singletons"
  :prefer-when "the question is about ECS relationships rather than syntax"
  :never-for "hand-reading or hand-editing the graph artifacts — the graph-gate hook denies it"
  :invoke "python3 ~/.claude/skills/ecs-graph/scripts/ecsg.py (rebuild: build_graph.py)"}

 {:tool di-graph
  :is "derived VContainer injection graph"
  :answers "what a type is registered as, with which Lifetime, by which installer; who injects it; what fills a collection injection; which GameMode a system runs in"
  :prefer-when "the question is about DI wiring rather than syntax"
  :never-for "hand-reading or hand-editing the graph artifacts"
  :invoke "python3 ~/.claude/skills/di-graph/scripts/dig.py (rebuild: build_di_graph.py)"}

 {:tool doc_lint
  :is "symbol-claim linter over the vault's .md files"
  :answers "whether a symbol a document names still exists in C#"
  :prefer-when "a surviving doc makes a claim about code — the claim is a hypothesis until this passes"
  :invoke "python3 Tools/doc_lint.py [--quiet]"}

 {:tool asmdef_reach
  :is "assembly-reference reachability check"
  :answers "whether a consuming .asmdef can see the types new code will use"
  :prefer-when "new code consumes types across an asmdef boundary — run before asking for GO"
  :invoke "python3 Tools/asmdef_reach.py"}

 {:tool arch-check
  :is "architectural ban detector"
  :answers "stateful systems, System.Collections.Generic usage, managed-collection allocation in systems"
  :never-for "auto-fixing — it detects only"
  :invoke "/arch-check"}

 {:tool unity-asset-graph
  :is "Unity asset reachability graph"
  :answers "what ships in the build, how an asset got there, where it is used, what is dead, serialized [SerializeField] enum values"
  :invoke "/unity-asset-graph"}

 {:tool GLOSSARY.md
  :is "domain vocabulary → code anchor map"
  :answers "the canonical code name behind a domain term, in any language"
  :prefer-when "before searching roslyn / ecs-graph / di-graph for a term the user named in prose"}

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
{:discovery-default "the main agent's own path: roslyn / graph CLIs → targeted code reads"
 :subagents :abandoned      ;; the owner dropped the project's own search/orchestration scouts — do not spawn them
 :retired "the «Discovery Scouts» section and the .claude/agents/*.md scout definitions were removed 2026-08-30"}
```

# Meters

```clojure
[{:meter "mcp__roslyn__get_diagnostics — solutionPath FantasyMayor.sln, scoped to the edited project/files"
  :target "clean on the edited scope"
  :when :code-changed
  :note "a pre-check that filters plain C# errors out of the round-trip; it never replaces the owner's Unity check"}

 {:meter "the owner's Unity-side check"
  :target "compiles and behaves"
  :when "only the owner can verify it"
  :authority :final}                    ;; the agent never runs Unity, dotnet, msbuild or Unity CLI builds

 {:meter "python3 Tools/doc_lint.py --quiet"
  :target "ghost-claim count not grown"
  :when :docs-changed}

 {:meter "python3 Tools/gen_index.py"
  :target "INDEX skeleton regenerated, lint clean"
  :when "a doc's frontmatter or first line changed"}

 {:meter "python3 ~/.claude/skills/ecs-graph/scripts/ecsg.py stats"
  :target "curated: true, no ghost nodes"
  :when :ecs-changed}

 {:meter "python3 ~/.claude/skills/di-graph/scripts/dig.py stats"
  :target "curated: true, no ghost nodes"
  :when :di-changed}]
```

# Bans

```clojure
{:never #{"Unity project builds, dotnet build, msbuild, xbuild, Unity CLI builds"
          "generating or hand-writing Unity .meta files"
          "reading .unity scenes or scene-serialized assets without the owner's explicit allowance in the current task"
          "editing .md through vault_patch / vault_write"
          "hand-editing the derived graph artifacts"
          "editing between INDEX.md's BEGIN/END GENERATED markers"
          "spawning the project's retired discovery/arch/asset scouts"
          "proposing automated test infrastructure — the owner's stance is playtest + fail-loud throws"
          "a silent skip on a missing prerequisite — throw instead"}
 :ask-first #{ARCHITECTURE.md
              "editing Flows / Patterns / the INDEX agent zone / a canvas after a code change"
              "Unity-side authoring — prefabs, .asset, scenes, addressable entries are the owner's territory"}
 :enforced-elsewhere ".claude/hooks/graph-gate.py (PreToolUse deny/ask) and Tools/githooks/pre-commit"}
```

# Ceremonies

```clojure
[{:ceremony session-start
  :runs-at "the beginning of a session"
  :owner "/fantasymayor-session-start"
  :steps "the command file states them"
  :lifecycle-hook :research-entry}

 {:ceremony flow-close
  :runs-at "the owner's explicit ask to close a task — never automatically"
  :owner "/flow-close"
  :steps "the command file IS their home — the canon carries no close ritual of its own"
  :lifecycle-hook :done-contract}

 {:ceremony index-regen
  :runs-at "after any frontmatter or first-line change"
  :owner "python3 Tools/gen_index.py (pass 1) + the agent's curated zone (pass 2)"}

 {:ceremony graph-rebuild
  :runs-at "after changing an ECS archetype or DI wiring"
  :owner "build_graph.py / build_di_graph.py — deterministic, one pass, no LLM"}]
```

# Shape

```clojure
{:flow-home "Flows/FLOW_<TASK>.md"
 :archive "Flows/Archive/"
 :sections FLOW_TEMPLATE.md            ;; three Rule 2 stages; the template is the only home of the shape
 :durable "Findings, Decisions, Disproven and Attempted live in the Contract stage and survive the plan drop"
 :research-document "RESEARCH_TEMPLATE.md → Flows/RESEARCH_<TOPIC>.md, archived together with the FLOW that links it"
 :lifecycle DOC_STANDARD.md            ;; active / contract / archive fate rules, and the Rule 2d harvest split
 :overrides "none — this arrangement fills the framework's template, it does not replace it"}
```

# Canon copy

```clojure
{:files #{CLAUDE.md}
 :status :reference                     ;; separated 2026-08-30 — CLAUDE.md now measures 0 in sync, 0 drifted
 :was {:at "2026-08-30" :measured "CLAUDE.md and AGENTS.md, both 8 in sync, 10 drifted, 8 local-only, 16 not copied"}
 :residue-moved-here #{prior-art done-contract deep-research attempted disproven}
 :deliberate-patches [{:definition flow-progress
                       :why "no canon equivalent for the progress shape; the owner declined promotion, so it stays in CLAUDE.md as a stated patch"
                       :at "2026-08-30"}
                      {:definition blocked-outcome
                       :why "no canon equivalent; same decision"
                       :at "2026-08-30"}]
 :project-scoped #{notation-ecs-ext openspec-policy}   ;; the project's own rules — local-only by right, never promotion candidates
 :renamed-canon #{research-depth task-normalization}   ;; deleted with the copy — bodies matched (def research) / (def normalization)
 :retired "AGENTS.md, Tools/gen_agents.py, the session-start canon-sync step and the pre-commit branch that guarded them (owner's decision, 2026-08-30)"}
```

# Extension

```clojure
{:footprint #{CLAUDE.md}               ;; one marked block: BEGIN SDD-FLOW: pointer … END SDD-FLOW: pointer
 :exclusive false                      ;; the copy IS this canon — after separation the project runs one framework, not two
 :notation-ecs-ext "CLAUDE.md declares a project-scoped ECS reading of the notation; it stays project-scoped"
 :openspec :removed-2026-08-05         ;; the CLI remains installed; it returns only on the owner's direct ask
 :codex-leg "Codex reads the .agents/skills/sdd-* skills; AGENTS.md and Tools/gen_agents.py were retired 2026-08-30"
 :updating "any git checkout of the package regenerates the managed files: `npx --yes github:zakyrion/sdd-flow update .`, a global install from github, or a pulled checkout the global CLI is linked to — the owner has not fixed one; `update` never touches this file"
 :tooling "Tools/gen_index.py PRUNE and Tools/doc_lint.py SKIP_DIRS must keep .sdd-flow — the framework's own .md files carry no category/read frontmatter, and giving them any would break `update`"}
```
