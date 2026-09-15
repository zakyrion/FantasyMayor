---
name: fantasymayor-graph
description: Build and query ONE deterministic graph of FantasyMayor's code that roslyn-mcp cannot classify: ECS (Friflo.Engine.ECS) archetypes with main and label tags, component writers and readers, event producer→consumer, Table-Rule PK/FK, singletons; DI (VContainer) registrations, Lifetime, installers, injectors, collection fill, GameMode; system roles decided by base or by markers; and the live instances of every Patterns/ recipe via pattern <RECIPE>. Use WHENEVER a question is about ECS or DI relationships or recipe instances — prefer it over reading .cs or grepping generic calls.
---

# fantasymayor-graph — one graph, one CLI, no LLM

roslyn-mcp sees definitions and references; it is blind to what lives in generic arguments and data: archetypes
declared in `<Domain>Archetypes` holders, `EventArchetypes.Of<T>` anchors, `AddComponent<T>` writes,
`ComponentIndex<TComponent,TValue>` tables, `Register<Impl>().As<Contract>()`, `IReadOnlyList<T>` collection
injection, Boot's hand-wiring of systems into game states. This skill extracts all of it with tree-sitter in ONE
deterministic pass (well under a second) into `<project>/.fantasymayor-graph/graph.json` and answers from it.

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
| `check` | dangling edges, table and set owners outside the nodes, archetypes without components, recipe instances without a node, curated, INFERRED / AMBIGUOUS counts, every warning; integrity errors exit 1 |
| `stats` | nodes by kind, edges by rel, systems by role, instances per recipe, warnings, curated |
| `pattern <RECIPE>` | the recipe's instances with `source_location` and `decided_by`, its groups and deviations; zero instances come with what the signature looked for |
| `systems [--role R]` | every system by role and priority; roles: `reactive`, `per_frame`, `cleanup`, `pipeline_stage`, `turn_phase`, `startup_step`, `sub_system`, `undecided` |
| `tags` | archetypes with the main tag and the label tags apart, then every tag-law deviation |
| `explain <node>` | every ECS and DI facet of a node, its outgoing and incoming edges with args, the tables it keys or owns |
| `neighbors <node> [--rel a,b]` | the edges of a node, filtered by rel |
| `search <keyword> [--role R]` | substring over id, kind and role |
| `bfs <node> [--depth N] [--rel R] [--in]` | a walk out of (or, with `--in`, into) a node |
| `tables` | every `ComponentIndex` field grouped by key component, with role and archetype |
| `spaces` | key spaces: the PK / FK pair, carriers, map sites, key-role conflations |
| `resolve <Contract[<Args>]>` | what fills `IReadOnlyList<Contract>` — `exposes` edges, filtered by args when given (`"IPrioritizedUniTaskSystem<MapGenerationStep>"`) |
| `consumers <node>` | who injects the type |
| `installer <node>` | what an installer registers: args, Lifetime, As targets, AppState |
| `state <node>` | the systems Boot wires into a game state |
| `unresolved` | injected types that are never registered or exposed |

`<node>` is an id, an exact name (any case), or a substring matching exactly one name; no match or several matches
end with the candidates on stderr and exit 1. A type's id is its name with the names of the types it is nested in;
an archetype's id is `Holder.Member`, a closed generic holder adds its arguments: `EventArchetypes.Of<NextTurnEvent>`.

## Roles and markers

A non-abstract class takes the first true branch; the role is decided by its base where the base decides, and by a
marker only where it does not.

```clojure
(def role-decision
  (cond
    (and (update-loop?) (sweeps-events?))         :cleanup          ;; lexical — an EventTag query and DeleteEntity in one class
    (anchored-on-event?)                          :reactive         ;; base — EventArchetypes.Of or an events-only AnyComponents in base(...)
    (and (update-loop?) (holds-event-archetype?)) "the SystemRole marker, or :undecided with a warning"
    (update-loop?)                                :per_frame        ;; base
    (pipeline-member?)                            :pipeline_stage   ;; base — IPrioritizedUniTaskSystem with MapGenerationStep
    (turn-phase-member?)                          :turn_phase       ;; base — TurnPhaseSubSystem
    (startup-step?)                               :startup_step     ;; base — the non-generic IUniTaskSystem
    (family-member?)                              :sub_system       ;; lexical — an abstract ancestor some constructor collects
    :else                                         :none))
```

```clojure
{:markers {SystemRoleAttribute     "[SystemRole(SystemRoleKind.PerFrame | Reactive)] — only where the base does not decide"
           ViewSubscriberAttribute "[ViewSubscriber(typeof(TheView))] on the class that adds handlers to the view's C# events"
           TagLabelAttribute       "[TagLabel] or [TagLabel(TagLabelRole.Transaction)] on a tag struct — a label beside the main tag, never a filter"}
 :guard "MarkerShapeAnalyzer in the Unity compilation — a marker whose type does not have the claimed shape is error FM1001-FM1004"
 :tool  "a redundant role marker, a class that needs one, a view marker without a subscription and a view subscription without a marker are warnings"}
```

A base-anchored event is `reacts_to`; an event archetype held outside `base(...)` is `reacts_to` only under the
reactive marker and `polls` otherwise.

## Schema

`graph.json`, schema 1 — a change of schema makes every graph stale.

```text
meta:       {schema, roots[], files, generated, curated}
nodes:      {id: {name, kind, namespace, source_location, declared, abstract,
                  role, decided_by, priority, base_anchor, anchor_events[], held_events[],   # classes
                  components[], main_tag, label_tags[],                                      # archetypes
                  lifetime, installer, contract, state, markers}}                            # DI facets, markers
            kind: component | tag | event | data | config | view | system | archetype | installer | interface | other
edges:      [{src, dst, rel, via, args[]?, app_state?, collection?, source_location, confidence}]
            rel:  has writes reads removes emits reacts_to polls disposes fk_of references
                  inherits hosts registers exposes injects runs_in subscribes
ecs_tables: {tables[], index_usages[], sets[], dispose_sites[], late_writes[], tags_add_sites[],
             indexed_components{}, component_field_types{}, singleton_used[]}
tag_audit:  [{kind, archetype_or_owner, tags[], source_location}]
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
- A stale graph never answers: every command rebuilds it first; a build that fails ends the command with its
  traceback and exit 1.
- The tool reads the repository and writes only `.fantasymayor-graph/`. It never builds Unity.
