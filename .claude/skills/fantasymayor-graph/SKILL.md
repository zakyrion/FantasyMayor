---
name: fantasymayor-graph
description: Build and query ONE deterministic graph of FantasyMayor's code that roslyn-mcp cannot classify: ECS (Friflo.Engine.ECS) archetypes with main and label tags, component writers and readers, event producer→consumer, Table-Rule PK/FK, singletons; DI (VContainer) registrations, Lifetime, installers, injectors, collection fill, the game state each system runs in; system roles decided by base or by markers; and the live instances of every Patterns/ recipe via pattern <RECIPE>. Use WHENEVER a question is about ECS or DI relationships or recipe instances — prefer it over reading .cs or grepping generic calls.
---

# fantasymayor-graph — one graph, one CLI, no LLM

roslyn-mcp sees definitions and references; it is blind to what lives in generic arguments and data: archetypes
declared in `<Domain>Archetypes` holders, `EventReader<TEvent>` fields and `Events.Raise` sites, `AddComponent<T>`
writes, `ComponentIndex<TComponent,TValue>` tables, `Register<Impl>().As<Contract>()` and
`RegisterAppStateSystem<T>(Lifetime, AppState.X)`, `IReadOnlyList<T>` collection injection, the game states each
system's AppState flags put it in. This skill extracts all of it with tree-sitter in ONE
deterministic pass (well under a second) into `<project>/.fantasymayor-graph/graph.json` and answers from it. The
same pass ends by drawing the boot as PlantUML into `Assets/Diagrams/` (see Diagrams).

## Deps

`pip install tree-sitter tree-sitter-c-sharp` (present on this machine). Python 3.10+.

## Quick start

Run from the project root (the directory above `Assets/`), or pass `--path`.

```bash
FMG=.claude/skills/fantasymayor-graph/scripts/fmgraph.py

python3 "$FMG" build                          # rebuild now (every command also self-heals a stale graph)
python3 "$FMG" check                          # integrity + every warning; exit 1 on an integrity error
python3 "$FMG" pattern PATTERN_REACTIVE_SYSTEM
python3 "$FMG" tags                           # main tag + label tags per archetype, tag-law deviations
```

`.fantasymayor-graph/` is derived and git-ignored. Query and rebuild ONLY through `fmgraph.py` — never read or
edit the JSON by hand (the graph-gate hook denies it).

## Commands

| command | answers |
|---|---|
| `build` | rebuild now: files, nodes, edges, warnings, seconds |
| `check` | dangling edges, table and set owners outside the nodes, archetypes without components, recipe instances without a node, curated, INFERRED / AMBIGUOUS counts, every warning — recipe deviations included, each with the rule it breaks; integrity errors exit 1 |
| `stats` | nodes by kind, edges by rel, systems by role, instances per recipe, warnings, curated |
| `pattern <RECIPE>` | the recipe's instances with `source_location` and `decided_by`, its groups and deviations; zero instances come with what the signature looked for |
| `systems [--role R]` | every system by role and priority; roles: `reactive`, `per_frame`, `pipeline_stage`, `turn_phase`, `startup_step`, `sub_system` |
| `tags` | archetypes with the main tag and the label tags apart, then every tag-law deviation (the label cap included) |
| `explain <node>` | every ECS and DI facet of a node, its outgoing and incoming edges with args, the tables it keys or owns |
| `neighbors <node> [--rel a,b]` | the edges of a node, filtered by rel |
| `search <keyword> [--role R]` | substring over id, kind and role |
| `bfs <node> [--depth N] [--rel R] [--in]` | a walk out of (or, with `--in`, into) a node |
| `tables` | every `ComponentIndex` field grouped by key component, with role and archetype |
| `spaces` | key spaces: the PK / FK pair, carriers, map sites, key-role conflations |
| `resolve <Contract[<Args>]>` | what fills `IReadOnlyList<Contract>` — `exposes` edges, filtered by args when given (`"IPrioritizedUniTaskSystem"`) |
| `consumers <node>` | who injects the type |
| `installer <node>` | what an installer registers: args, Lifetime, As targets, AppState |
| `state <node>` | the systems a game state runs: every registration exposed As `IAppStateSystem` whose AppState flags name the state's `Mode` |
| `unresolved` | injected types that are never registered or exposed |
| `uml` | redraw `Assets/Diagrams/` from the graph now; prints the files whose text changed |
| `members <node>` | every member a type declares — enum values, consts, fields, properties, events, indexers, methods — with its place |
| `member <Name>` | every type that declares a member of exactly this name (`member Mode` → the eight app states), or none — the answer to "does X still exist" |

`explain` on an enum lists its values as written (`Initialization = 1 << 0`).

A text search over C# under `Assets/` is stopped by the graph-gate hook for every agent, subagents included, unless
the command says why on the same line (`… # why: <what no tool answers>`); the Grep tool over it is stopped
outright. Ask this graph or roslyn first.

## Diagrams

Every `build` — and so every command that finds the graph stale — ends by writing PlantUML views of the boot into
`Assets/Diagrams/` (git-ignored with its `.meta`; Rider shows them under Assets). A file whose text did not change is
left untouched, a `.puml` no longer drawn is removed, a missing folder makes the graph stale. Everything is drawn from
the graph alone (`scripts/uml.py`):

| file | shows |
|---|---|
| `flow.puml` | the state machine: `[*]` → the state the switcher (`Boot`, the `Switch` caller) starts in, then every `requests_state` edge labelled with who asks, how and when (end of entry, or the method); solid when raised or switched, dashed when only assigned; a state no live request reaches is grey, a reached state with no live exit says so; a note names the switcher and the request event it reads; a red note on a state that reads a singleton only a never-entered state writes |
| `structure.puml` | the switcher → the machine that injects every `IAppState` → the states with their system counts; the request event: raised by the live requesters, read by the switcher; dashed red for a mode only assigned; installers merged into one box per `Assets/<Layer>`, one dotted arrow per (layer, state) with the number of systems; a dotted arrow between states for the singletons one writes and the other reads — red when the writer state is entered by nobody |
| `states/<State>.flow.puml` | enter (a red note when no live request leads here) → entry steps (all at once when `entry_run` is `parallel`, else in registration order) → pipeline stages by Priority → the requests made at the end of entry → each frame Tick and LateTick by Priority → the requests made while ticking |
| `states/<State>.structure.puml` | the state and its systems grouped by installer, each with role and Priority; the events each emits, consumes, reacts to or polls; who outside the state raises an event it reads, and the switcher when it reads one it raises; a note with the singletons the state reads from, and writes for, other states |

Registration order is the container's: each root installer's registrations line by line, an installer it hands the
builder to (`installs`) expanded at the line of that call. Line order stands in for call order inside one installer.

The kind of a system — entry step, pipeline stage, updated, late-updated — is read from the interfaces its bases
reach (`IUniTaskSystem`, `IPipelineStageSystem`, `IUpdatedSystem`, `ILateUpdatedSystem`), as `AppStateSystems.Filter`
files it. Never edit a diagram: the next build overwrites it.

Every diagram is checked before anything is written, and a failure ends the build: line 1 is `@startuml`, the last
line is `@enduml`, one diagram per file. Rider's plantuml4idea previews the diagram under the caret and opens a
file with the caret on line 1, so a comment above `@startuml` gives an empty preview while the command-line
renderer still passes. The generated header comment therefore stands inside `@startuml`.

`<node>` is an id, an exact name (any case), or a substring matching exactly one name; no match or several matches
end with the candidates on stderr and exit 1. A type's id is its name with the names of the types it is nested in;
an archetype's id is `Holder.Member`, a closed generic type adds its arguments: `EventReader<NextTurnEvent>`.

## Roles and markers

A non-abstract class takes the first true branch; the role is decided by its base where the base decides, and by a
marker only where it does not.

```clojure
(def role-decision
  (cond
    (and (update-loop?) (holds-event-reader-field?)) "the SystemRole marker decides: PerFrame, else reactive"
    (update-loop?)                                   :per_frame        ;; base
    (pipeline-member?)                               :pipeline_stage   ;; base — IPipelineStageSystem
    (turn-phase-member?)                             :turn_phase       ;; base — TurnPhaseSubSystem
    (startup-step?)                                  :startup_step     ;; base — IUniTaskSystem that is not a stage
    (family-member?)                                 :sub_system       ;; base — IPrioritizedUniTaskSystem outside the turn phase
    :else                                            :none))
;; a class with no role that still holds an EventReader<TEvent> field gets a `consumes` edge, not a role
;; an orchestrator hosts its family: every subsystem names it in OrchestratorType => typeof(X) — the hosts edge runs
;; from X to the class declaring the property; a constructor collecting an abstract class is the older hosts form
```

```clojure
{:markers {SystemRoleAttribute     "[SystemRole(SystemRoleKind.PerFrame)] — only on an Update-loop class that ALSO holds an EventReader<TEvent> field yet still ticks every frame; Reactive is no longer a legal value"
           ViewSubscriberAttribute "[ViewSubscriber(typeof(TheView))] on the class that adds handlers to the view's C# events"
           TagLabelAttribute       "[TagLabel] or [TagLabel(TagLabelRole.Transaction)] on a tag struct — a label beside the main tag, never a filter"}
 :guard "MarkerShapeAnalyzer in the Unity compilation — a marker whose type does not have the claimed shape is error FM1001, FM1006"
 :tool  "a redundant role marker, a class that needs one, a view marker without a subscription and a view subscription without a marker are warnings"
 :marker-value "the marker decides only when its value matches the shape: [SystemRole(PerFrame)] on a class holding no EventReader<TEvent> field is refused — a warning, exactly as the analyzer refuses it"}
```

A class holding an `EventReader<TEvent>` is `reacts_to` when it carries no `PerFrame` marker, and `polls` when it
does; a non-system holder of an `EventReader<TEvent>` — a game state, a subsystem, Boot — gets a `consumes` edge
instead, with no role at all.

## The laws over rows, keys and links

Beside the tag law and the role decision, the pass measures the shape of a row and of its keys. Each deviation is a
warning citing its rule; `references/graph-facts.md` names rule by rule what is measured and what the code cannot
show.

```clojure
(def row-laws
  {:birth       "an entity is born by a creation call ON ITS ARCHETYPE — a receiver that names no archetype, or a row composed out of components and tags at the call site, is the bare creation on the store"
   :arity       "an archetype declaration names at most five columns and at most five tags; beside the main tag stand 0-4 labels"
   :tag-shape   "a tag is an empty struct; the state and the kind of a row are columns over an ENUMERATION, and the kind column is written once, at birth"
   :component   "a column is a plain struct of runtime values: no method beyond Equals, GetHashCode and GetIndexedValue, and no logic in a Components/ folder"
   :key-space   "one archetype holds at most one foreign key into a key space; a space of enum values carries a data column on the owner's side; a primary key that keys an index is written in a member that throws"
   :link        "an Entity descriptor is never stored: in a column it is a join that belongs at the point of use, in a field of a class a link that belongs to a domain key"
   :transaction "a transaction entity lives in ONE home — its verb domain owns every write into a column only it carries — and in ONE archetypal form, stage column included"})
```

## The view layer

A view is EVERYTHING declared in a `Views/` folder — the folder alone decides it, for a struct and an enum as much as
for a class (`view_layer`). MonoBehaviour ancestry is a separate fact (`scene_object`): the extra the law asks of the
type a `[ViewSubscriber]` marker names, so view ↔ subscriber pairing runs on views that are scene objects, while the
view boundary — no entity created, no ECS event raised, no `EntityStorages` / `EntityStore` received — is measured
over the whole layer.

## Schema

`graph.json`, schema 1 — a change of schema makes every graph stale.

```text
meta:       {schema, roots[], files, generated, curated}
nodes:      {id: {name, kind, namespace, source_location, declared, abstract,
                  role, decided_by, priority, event_readers[],                               # classes
                  components[], main_tag, label_tags[], tag_order[],                         # archetypes
                  view_layer, scene_object,                                                  # the view layer
                  lifetime, installer, contract, state, entry_run, markers,                  # DI facets, markers
                  members[{name, kind, value?, source_location}], enum}}                     # every declaration
            kind: component | tag | event | data | config | view | system | archetype | installer | interface | other
edges:      [{src, dst, rel, via, args[]?, app_state?, collection?, member?, source_location, confidence}]
            rel:  has writes reads removes emits reacts_to polls consumes disposes fk_of references
                  inherits hosts registers installs exposes injects runs_in requests_state subscribes
ecs_tables: {tables[], index_usages[], sets[], dispose_sites[], late_writes[], tags_add_sites[],
             indexed_components{}, component_field_types{}, singleton_used[]}
tag_audit:  [{kind, archetype_or_owner, tags[], source_location, rule}]
recipes:    {RECIPE: {instances[{id, source_location, decided_by, …}], groups{}, deviations[], decided_by, empty_reason}}
warnings:   [...]
```

Generic arguments live on edges, never in node ids: 26 registrations of the generic config loader are 26 `registers`
edges to one node, each with its `args`. The code → graph mapping is `references/graph-facts.md`; the recipe →
signature mapping is `references/recipe-signatures.md`.

## Honesty rules

- Never an invented edge: mechanical facts are `EXTRACTED`, an inferred one (a camelCase `AddComponent(local)`, a
  `DeleteEntity` attributed through the owner's single binding) is `INFERRED`; a name that resolves to several
  declarations is an `AMBIGUOUS` warning and no edge.
- A warning is never silent: parse errors, ambiguous names, `DeleteEntity` without one candidate archetype,
  key-role conflations, undecided roles, redundant markers, subscriptions without a marker, registration forms
  and archetype member forms outside the known ones — all in `check`.
- A warning that enforces a rule of `RULES_SPECIFICATION.md` ends with that rule's id, `[rule <prefix>/<slug>]`, and
  the rule is found in the specification by text search, never by section. A warning with NO id enforces no rule and
  says so by the absence: the tool is reporting on itself — a parse error, an `AMBIGUOUS` name, a `DeleteEntity` or a
  holder call or a `Priority` expression it cannot resolve, a DI registration form it does not follow. Every recipe
  deviation is a warning too, so `pattern` and `check` never disagree. `references/graph-facts.md` lists rule by rule
  what the graph measures.
- A stale graph never answers: every command rebuilds it first; a build that fails ends the command with its
  traceback and exit 1.
- The tool reads the repository and writes only `.fantasymayor-graph/` and `Assets/Diagrams/`. It never builds Unity.
