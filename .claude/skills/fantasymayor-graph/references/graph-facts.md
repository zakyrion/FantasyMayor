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
| `view` | a class in a `Views/` folder whose ancestry reaches `MonoBehaviour` |
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
| `main_tag`, `label_tags` | the archetype's `Tags.Get` arguments split by the `[TagLabel]` marker on each tag struct |
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
- `tag_audit` — archetype without a main tag, several main tags, a main tag other than EventTag shared by two
  archetypes, EventTag outside an event archetype, a label tag in a cross-archetype filter, a non-event filter with a
  tag count other than 1, a tag added during an entity's life, tags composed through `Tags.Add`.
- Registration forms the reader does not follow — `RegisterFactory`, `RegisterComponentInHierarchy`,
  `RegisterEntryPoint`, `AsImplementedInterfaces`, `RegisterInstance` of a non-identifier — are warnings.
