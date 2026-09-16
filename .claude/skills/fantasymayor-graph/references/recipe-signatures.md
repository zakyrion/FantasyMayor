# fantasymayor-graph — recipe → signature

`RECIPE_SIGNATURES` in `scripts/recipes.py` is the source of truth; this table is its reading. `decided-by` says
where an instance's evidence comes from; "0 means" is what `pattern <RECIPE>` prints when nothing matches. Every
deviation cites the rule it breaks and is a warning of `check` as well.

| recipe | decided-by | signature | 0 means |
|---|---|---|---|
| PATTERN_COMPONENT | kind | declared struct of kind component | no struct implements `IComponent` or is named `…Component` |
| PATTERN_TAG | kind | declared struct implementing `ITag`, split into main and label groups; deviations = the tag audit | no tag struct |
| PATTERN_EVENT | kind | declared struct of kind event | no struct named `…Event` / `…EventComponent` |
| PATTERN_CONFIG | base | non-abstract class reaching `ScriptableObject`; group `abstract_bases` | no ScriptableObject class |
| PATTERN_CONFIG_LOADER | di | a registration with `AppState.ConfigLoading` whose implementation is exposed as `IUniTaskSystem` in the same statement; group `instance_objects` = registrations with `AppState.InstanceObjects`; deviations = a hand-written config loader, a descendant of `ConfigLoaderSystem`, a `…ConfigComponent`, an archetype column named `…Config` | no config-loading registration |
| PATTERN_PIPELINE_STAGE | base | role `pipeline_stage` | no class implements `IPrioritizedUniTaskSystem<MapGenerationStep>` |
| PATTERN_ORCHESTRATOR_SUBSYSTEM | lexical | instances = non-abstract descendants of a contract; group `contracts` = abstract classes a constructor collects (hosts edges); group `hosts` = their collectors | no constructor collects an abstract class |
| PATTERN_REACTIVE_SYSTEM | reader | role `reactive`: an Update-loop class holding an `EventReader<TEvent>` field with no `PerFrame` marker | no class holds an `EventReader<TEvent>` field without the `PerFrame` marker |
| PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM | lexical | role `reactive` and an outgoing hosts edge | no reactive class collects a family |
| PATTERN_PERFRAME_SYSTEM | base, marker | role `per_frame` | no Update-loop class either without an `EventReader<TEvent>` field or under the `PerFrame` marker |
| PATTERN_VIEW_SYSTEM | marker | subscribes edges; groups `views`, `subscriptions`; deviations = subscriptions without a marker and the view boundary over the whole view layer — everything declared in a `Views/` folder — that raises an event, creates an entity, or receives `EntityStorages` / `EntityStore` | no `[ViewSubscriber]` class subscribes to its view |
| PATTERN_TRANSACTION_ENTITY | label-tag | archetype carrying a label tag marked `[TagLabel(TagLabelRole.Transaction)]` | no archetype carries the transaction label |
| PATTERN_POLYMORPHIC_CATALOGUE | lexical | abstract ScriptableObject base B named `…Config`, a non-abstract ScriptableObject container with a `[SerializeField]` array of B, and a `…KindComponent` named after B carried by an archetype; deviations = a base holding more than the shared key, a row without a reference key, a row without a family label when there are several, a kind column written outside the member that creates the row | no such triple |
| ADDRESSABLE_PATTERNS | lexical | a class injecting `IAddressable` through its constructor | no constructor takes `IAddressable` |
