# fantasymayor-graph — code pattern → graph fact

What `scripts/source_reading.py` reads from the game code and which node, facet or edge each reading becomes.
FantasyMayor uses Friflo.Engine.ECS (not Unity DOTS) and VContainer. Scan roots: `Assets/Domains`,
`Assets/Presentation`, `Assets/Modules`, `Assets/Scripts`, `Assets/Flows`.

## Nodes

| kind | recognized by |
|---|---|
| `component` | a struct implementing `IComponent` or named `…Component`, not an event |
| `tag` | a struct implementing `ITag` |
| `event` | a struct named `…Event` or `…EventComponent`, or a type raised with `CreateEvent` |
| `data` | any other struct; an undeclared name without a role suffix |
| `config` | a non-abstract class whose ancestry reaches `ScriptableObject` or `SerializedScriptableObject` |
| `view` | a class declared in a `Views/` folder — the folder alone decides it; a ScriptableObject class there stays a `config` and keeps `view_layer` |
| `system` | a non-abstract class whose role is not `none` (SKILL.md → Roles and markers) |
| `archetype` | a holder member `store.GetArchetype(ComponentTypes.Get<…>(), Tags.Get<…>())`, the singleton manifest, or a closure of a generic holder (`EventArchetypes.Of<T>`) reached directly or through relaying generic helpers |
| `installer` | a class named `…Installer` or based on `IInstaller` / `LifetimeScope` |
| `interface` / `other` | any other declaration |

One node per declaration: partial declarations share it; a type referenced but not declared in the scanned code is a
node with `declared: false` and a kind guessed from its suffix. Two types with the same nesting chain in different
namespaces take `Namespace.Chain` ids; a bare name is resolved through the namespaces the file sees, and a name that
stays ambiguous is a warning, not an edge.

## Node facets

| facet | from |
|---|---|
| `main_tag`, `label_tags`, `tag_order` | the archetype's `Tags.Get` arguments split by the `[TagLabel]` marker on each tag struct; `tag_order` keeps them as written, so the law "the main tag is written first" can be measured |
| `view_layer` | the declaration — of any kind, class, struct or enum — stands in a `Views/` folder: the view layer, and the population the view boundary is measured over |
| `scene_object` | a class whose ancestry reaches `MonoBehaviour`: the extra the partner of a view system carries, and what view ↔ subscriber pairing runs on |
| `base_anchor`, `anchor_events`, `held_events` | where a class binds an archetype: inside `base(...)` of a class whose direct base is `UpdatedSystem` or `LateUpdatedSystem` (anchor), anywhere else in the class, `this(...)` included (held); inside `base(...)` of any other base it is neither, as MarkerShapeAnalyzer measures; an event archetype (EventFrameComponent + EventTag) names its events |
| `role`, `decided_by` | the role decision — `base`, `marker` or `lexical` |
| `priority` | the `Priority` expression resolved against every `const int` by its nesting path |
| `lifetime`, `installer` | every registration of the type, comma-joined and sorted |
| `contract` | an `As<…>` target or the element of a collection injection |
| `state` | a `…State` created in `Boot.Construct` |
| `markers` | the SystemRole / ViewSubscriber / TagLabel attributes on the declaration |

## Edges

| rel | matched from |
|---|---|
| `has` | archetype → each component and tag of its declaration |
| `writes` | `AddComponent(new T{…})`, `AddComponent<T>()`, `AddComponent(localOfT)` (INFERRED), `Singletons.Set` |
| `reads` | `GetComponent<T>`, `HasComponent<T>`, `Singletons.Get<T>` / `Has<T>`, and a class → table archetype binding |
| `removes` | `RemoveComponent<T>`, `RemoveTag<T>` |
| `emits` | `CreateEvent(new TEvent{…})` |
| `reacts_to` | a reactive class → the events its `base(...)` anchors, or its held events under `[SystemRole(Reactive)]` |
| `polls` | any other class → the event archetypes it holds outside `base(...)` |
| `disposes` | `DeleteEntity()` attributed through the owner's single table binding (INFERRED); event sweeps are skipped |
| `fk_of` | two signals kept apart: the suffix law (`…FKComponent` → `…Component`) and a real `ComponentIndex<FK,TValue>` lookup whose TValue matches the owner key |
| `references` | archetype → archetype through has(A, FK) + fk_of(FK, PK) + has(B, PK), one per fk_of signal |
| `inherits` | a declaration → each written base, with its generic `args` |
| `hosts` | a class whose constructor takes a collection of an abstract class → that class |
| `registers` | installer → implementation: `Register<Impl>` / `Register<Contract, Impl>` with `args`, `via Register(Lifetime)`, `app_state` from `WithParameter(AppState.X)`; `RegisterInstance(identifier)` → the PascalCased type |
| `exposes` | implementation → each `As<…>` target (itself excepted) and the contract of `Register<Contract, Impl>` |
| `injects` | the constructor of a registered class and every `[Inject]` method → each non-primitive parameter; a collection parameter points at its element with `collection: true` |
| `runs_in` | a `Boot.Construct` parameter named inside `new …State(…)` → that state (`Boot(collection)` for a collection) |
| `subscribes` | a `[ViewSubscriber(typeof(V))]` class → V, confirmed by its own `+=` on an event V or a base of V declares |

## Tables, audit, warnings

- `ecs_tables.tables` — every `ComponentIndex<TComponent,TValue>` field with its role by key suffix (`pk`, `fk`,
  `data`) and the one archetype carrying the key; `index_usages` — every `_field[key]` lookup.
- `tag_audit` — archetype without a main tag, several main tags, a main tag not written first, more than four label
  tags beside it, a main tag other than EventTag shared by two archetypes, an event carried by no EventTag archetype,
  EventTag outside an event archetype, a label tag in a cross-archetype filter, a non-event filter with a tag count
  other than 1, a tag added during an entity's life, tags composed through `Tags.Add`. Every entry carries the `rule`
  it breaks.
- Registration forms the reader does not follow — `RegisterFactory`, `RegisterComponentInHierarchy`,
  `RegisterEntryPoint`, `AsImplementedInterfaces`, `RegisterInstance` of a non-identifier — are warnings.

## Warnings and the rules they enforce

Every warning that enforces a rule of `RULES_SPECIFICATION.md` ends with that rule's id, `[rule <prefix>/<slug>]`;
a recipe deviation is a warning too, so `check` shows everything `pattern` shows.

| rule | what the graph measures |
|---|---|
| `table/is` | a `ComponentIndex` key named by no declared archetype |
| `table/filter` | a non-event filter whose tag count is not 1 |
| `table/join-at-use` | an `Entity` stored in a column — a field of a component or event struct |
| `tag/is` | a struct named `…Tag` without the `ITag` contract, or a tag struct that carries a field |
| `tag/one-main-tag` | no main tag, several main tags, or a label written before the main tag |
| `tag/label-count` | more than four label tags beside the main tag |
| `tag/state-column`, `tag/kind-column` | a `…StateComponent` / `…KindComponent` over no declared enum; a kind column written outside the member that creates the row |
| `tag/main-tag-unique` | one main tag other than EventTag on two archetypes |
| `tag/event-tag` | a declared event carried by no archetype whose main tag is EventTag |
| `tag/label-not-a-filter` | a label tag in a query filter |
| `tag/added-by-archetype-only` | `AddComponent` of a `…Tag` during the entity's life |
| `tag/composed-by-declaration` | tags composed through `Tags.Add` |
| `archetype/shape` | a holder member that is not `GetArchetype(ComponentTypes.Get<…>(), Tags.Get<…>())`, or takes no `EntityStore` parameter |
| `archetype/birth` | a creation call whose receiver names no archetype, or that composes the row out of components and tags at the call site |
| `archetype/arity-cap` | an archetype declaration naming more than five columns, or more than five tags (the singleton manifest names its columns one `Add<T>()` at a time and is not a list of type arguments) |
| `archetype/no-orphan-column` | a written column no declared archetype names |
| `birth/no-late-structural` | `RemoveComponent` / `RemoveTag` after birth |
| `singleton/manifest` | manifests other than one, a singleton component used-undeclared or declared-unused |
| `key/pk-single-carrier` | a PK carried by several archetypes |
| `key/data` | a data column keying an index across several archetypes |
| `key/fk-value-parity` | `ComponentIndex<FK,TValue>` whose TValue is not the owner key's value |
| `key/enum-space` | an FK whose indexed value is a declared enum while no archetype carries a data column over that enum |
| `index/key-equality` | an index key, or the owner key of an FK lookup, without an `IIndexedComponent<…>` contract |
| `index/one-fk-per-space` | one archetype carrying two foreign keys into one key space |
| `index/pk-uniqueness` | a write of a primary key that keys a `ComponentIndex`, standing in a member that throws nowhere |
| `component/is` | a component struct with a method beyond `Equals`, `GetHashCode` and `GetIndexedValue`, or a declaration that is not a struct or an enum in a `Components/` folder |
| `component/declare`, `event/declare` | a component or an event struct without the component contract; an event outside an `Events/` folder |
| `link/persistent-key` | an `Entity` descriptor stored in a field of a class |
| `event/raise` | `AddComponent` of an event type outside the member that raises it with `CreateEvent` |
| `event/suffix` | a second live type carrying the legacy `…EventComponent` suffix |
| `event/no-tag-on-persistent-row` | EventTag on an archetype without `EventFrameComponent` |
| `event/cleanup` | cleanup systems other than one, a cleanup Priority other than `int.MaxValue`, a cleanup with descendants |
| `name/suffix` | a tag without the `Tag` suffix, a component without the `Component` suffix, a field-less component, an FK whose owner key is not in the graph |
| `system/marker-required`, `system/marker-forbidden`, `system/marker-value` | a role the base does not decide and no marker; a marker where the base decides; `Reactive` claimed on a table-anchored class |
| `system/priority-source` | a `Priority` literal, or a `Priority` taken from a const of the class itself |
| `transaction/one-home` | a transaction entity declared in presentation, or a write of a column only it carries — or the deletion of its row — from outside its home domain |
| `transaction/one-archetype` | a transaction archetype carrying no stage column (`…StateComponent`) |
| `view/boundary` | a declaration of the view layer that creates an entity, raises an ECS event, or receives `EntityStorages` / `EntityStore` |
| `view/subscriber-marker` | a view marker with no subscription, a subscription to a view event with no marker |
| `config/loader` | a hand-written config loader, a descendant of `ConfigLoaderSystem`, a `…ConfigComponent`, an archetype column named `…Config` |
| `catalogue/config-shape`, `catalogue/row-shape` | an abstract catalogue base with more than the shared key; a catalogue row without a reference key, without a family label when there are several, or with a kind written outside the member that creates the row |

## Where a rule says more than the reading can show

The check is built, and it measures the half of its rule the code shows. The other half is named here so the rule can
be read honestly, never silently narrowed.

| rule | measured | not measured, and why |
|---|---|---|
| `tag/state-column` | the column stands over an enumeration | "written only on change" — a compare-before-write is data flow, not a call the reader sees; "never a toggled tag" is held by `tag/added-by-archetype-only` |
| `tag/kind-column` | the column stands over an enumeration, and is written in the member that creates the row | "never a second main tag" is held by `tag/one-main-tag` |
| `index/pk-uniqueness` | the member that writes an indexed primary key contains a `throw` | the allocation site itself: the reader cannot tell which expression mints a key value, so it looks at the member the key is written in |
| `component/is` | methods and operators of the struct, and non-struct declarations in a `Components/` folder | properties with bodies, and "no side effects" — a body is not read |
| `key/enum-space` | the owner's side of an enum space carries a data column over that enum | the reference side's suffix, which `key/fk` and `name/suffix` hold |
| `link/persistent-key` | an `Entity` descriptor in a field of a class | whether the link is runtime-only, which `link/runtime-only` allows: nothing in the declaration says whether it is bound to a lifetime |
| `table/join-at-use` | an `Entity` stored in a component or an event struct | a `data` struct holding one: it is neither a row nor a field that outlives a call |
| `transaction/one-home` | the layer of the declaration, and every write of a column only this archetype carries, plus the deletion of its row | whether the home domain is a substrate one — that is a reading of the domain, not of the code; a write of a column two archetypes share cannot be attributed to a row |
| `transaction/one-archetype` | the stage column is carried | "the main tag does not change" is held by `tag/added-by-archetype-only`; "another composition means a new row" is a decision, not a call |
| `archetype/birth` | the receiver of the creation call, and a row composed at the call site | nothing: a bulk creation passes its count, so only the receiver decides it |

Warnings that enforce NO rule carry no id, and that is the tool being honest about itself: a parse error, an
`AMBIGUOUS` name or view event, a `DeleteEntity` without one candidate archetype, a holder call whose type arguments
do not close, a `Priority` expression that resolves to nothing, and the DI registration forms above — the form of a
registration is explicitly outside the rule set.
