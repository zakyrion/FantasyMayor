---
name: fantasymayor-pattern-choice
description: Pick the Patterns/ recipes and shared-kernel types every code part of a FantasyMayor task needs, find what already implements them, and check the tag law — one main tag plus label tags — before a new tag or archetype. Use when a FantasyMayor task statement is formed for work that creates or changes code (CLAUDE.md § 2 puts the recipes into :read and the result into :skills), or when the user invokes fantasymayor-pattern-choice.
---

# Pattern choice

The recipe, the shared-kernel type and the live family behind every code part of a FantasyMayor task — chosen before the statement is shown.

## Procedure

```clojure
(def pattern-choice
  (-> (:step-1 "split the task into the code parts it creates or changes; name the situation of each part")
      (:step-2 "each part: walk recipe-tree — every recipe it returns goes into :read; :no-recipe is written into the statement as such")
      (:step-3 "each situation: walk kernel-tree — every class it returns goes into the result; read the class before using it and act by its :use")
      (:step-4 "each recipe: find what already implements it by instances")
      (:step-5 (when (declares-a-tag-an-archetype-or-a-tag-query? part)
                 (:then "run tag-law-before")))
      (:result "{:read [recipes …] :skills {fantasymayor-pattern-choice {:recipes … :kernel … :instances … :tags …}}} — shown with the statement")))
```

## Recipes

```clojure
(def recipe-tree
  (cond
    (ecs-data? part)     data-recipes
    (config? part)       config-recipes
    (behavior? part)     behavior-recipes
    (view? part)         view-recipe
    (addressables? part) ADDRESSABLE_PATTERNS
    :else                :no-recipe))

(def data-recipes
  (cond (carries-values?)     PATTERN_COMPONENT
        (marks-row-identity?) PATTERN_TAG
        (signals-a-change?)   PATTERN_EVENT
        :else                 :no-recipe))

(def config-recipes
  (cond (many-kinds-into-an-entity-table?) PATTERN_POLYMORPHIC_CATALOGUE
        (loaded-or-built-at-startup?)      [PATTERN_CONFIG PATTERN_CONFIG_LOADER]
        :else                              PATTERN_CONFIG))

(def behavior-recipes
  (cond (and (spans-several-subdomains?) (multi-step-session-state?))        transaction-branch
        (and (builds-world-at-map-creation?) (independently-ordered-parts?)) PATTERN_ORCHESTRATOR_SUBSYSTEM
        (builds-world-at-map-creation?)                                      PATTERN_PIPELINE_STAGE
        (runs-as-a-turn-phase?)                                              PATTERN_ORCHESTRATOR_SUBSYSTEM
        (and (reacts-to-an-event?) (independently-ordered-parts?))           PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM
        (reacts-to-an-event?)                                                PATTERN_REACTIVE_SYSTEM
        (continuous-every-frame?)                                            PATTERN_PERFRAME_SYSTEM
        :else                                                                :no-recipe))

(def transaction-branch
  {:recipe PATTERN_TRANSACTION_ENTITY
   :when   "a transaction entity is taken when there are two or more subdomains AND the behaviour is multi-step — between the opening and the committing pulse the result can still change, so there is session state somebody must own; two subdomains and one pulse is a reactive system"
   :degenerate "when there is nothing to own between opening and commit — no resource spent, no preview on the map, the choice is pure interface state — the commit happens straight on the confirming pulse, with no draft stages; the branch falls through to PATTERN_REACTIVE_SYSTEM and the selection stays in presentation"
   :ask    "name what is owned between the opening and the committing pulse; when nothing is, the draft stage is dead weight"})

(def view-recipe
  {:recipe PATTERN_VIEW_SYSTEM
   :marker "a class that subscribes to a view's C# event with its own += carries [ViewSubscriber(typeof(TheView))] naming that view; a marker without a real subscription and a subscription without a marker are both violations, and the named type must derive from MonoBehaviour"
   :why    "PATTERN_VIEW_SYSTEM never mentions the marker, so this branch is the only place it is decided"
   :fails  "MarkerShapeAnalyzer, category FantasyMayor.Markers, severity Error — a marker that contradicts the shape fails the Unity compile"})
```

## Shared kernel

```clojure
(def kernel-tree
  (cond
    (declaring-a-system? situation)      system-bases
    (ordering-systems? situation)        {:read SystemPriorities
                                          :use  "Priority returns a constant from here, never a local number; each nested class is its own order space, comparable only inside it; every system gets a distinct value"}
    (config? situation)                  config-kernel
    (event? situation)                   event-kernel
    (keeping-state? situation)           state-kernel
    (looking-up-a-single-row? situation) {:read QueryResultExtensions
                                          :use  "TryGetFirst(out entity) on a query, an archetype or an Entities set; branch on the bool"}
    (long-running-async-task? situation) {:read StatusMonitor
                                          :use  "pass StatusMonitor.Token; observe cancellation with ThrowIfCancellationRequested, never a quiet return"}
    :else                                :none))

(def system-bases
  (cond (runs-once-at-startup?)        {:read IUniTaskSystem
                                        :use  "implement Execute(token) and AppState — the startup-step flag Boot runs it on; register .As<IUniTaskSystem>()"}
        (runs-in-an-ordered-pipeline?) {:read IPrioritizedUniTaskSystem
                                        :use  "implement Update(token) and Priority (lower runs first); T is the stage DI collects the family by; when instances shows an abstract base of the family, derive from that base"}
        (must-run-after-every-update?) {:read LateUpdatedSystem
                                        :use  "same contract as UpdatedSystem, driven from LateUpdate"}
        (reacts-to-an-event?)          {:read IUpdatedSystem
                                        :use  "implement it directly — no base at all; a readonly EventReader<TEvent> from DI drives Update via while (reader.TryRead(out var evt)) (PATTERN_REACTIVE_SYSTEM)"}
        :else                          {:read UpdatedSystem
                                        :use  "pass the store and one archetype from its holder — an ArchetypeQuery only when the trigger spans several archetypes; implement Update(state, entity) and Priority; structural changes inside Update are safe, but not inside an enumeration Update opens itself (ARCHITECTURE.md → Threading, structural-change)"
                                        :then "decide the marker by role-marker before writing the class"}))

(def role-marker
  {:shape      "an Update-loop class holding a readonly EventReader<TEvent> field is reactive by that shape alone — no anchor is measured for it; a table anchor in base(...) of UpdatedSystem or LateUpdatedSystem still decides per_frame the same way it always did"
   :required   "[SystemRole(SystemRoleKind.PerFrame)] is required in exactly one shape — the one form no ordering rule decides: an Update-loop class that holds an EventReader<TEvent> field yet still ticks every frame instead of reacting"
   :forbidden  "a role marker on a class holding no EventReader field is forbidden — its role is already decided by its table anchor or the absence of one"
   :value      "PerFrame demands the Update-loop contract plus an EventReader<TEvent> field; Reactive has no legal shape any more — the value was removed from SystemRoleKind"
   :not-inherited "no marker is inherited — every concrete class carries its own"
   :fails      "MarkerShapeAnalyzer, category FantasyMayor.Markers, severity Error — a marker that contradicts the shape fails the Unity compile"})

(def config-kernel
  (cond (registering-a-config-for-loading?) [{:read ConfigLoaderSystem
                                              :use  "one installer registration per config: Register<ConfigLoaderSystem<X>>().As<IUniTaskSystem>().WithParameter(AppState.ConfigLoading).WithParameter(«address», ConfigAddresses.X) — never a hand-written loader"}
                                             {:read ConfigAddresses
                                              :use  "add a constant holding the addressable entry name as authored in Unity; installers pass the constant, never a string literal"}]
        (authoring-a-config-type?)          {:read IValidatableConfig
                                             :use  "implement Validate() and throw on every authoring violation; it runs right after loading"}
        :else                               :none))

(def event-kernel
  (cond (raising-an-event?)   {:read EventLog
                               :use  "storages.Events.Raise(new TEvent { … }) — the event is born in its own type's ring with its values; AddComponent of an event type anywhere else is a violation"}
        (consuming-an-event?) {:read EventReader
                               :use  "readonly EventReader<TEvent>, injected Transient by DI; read every unread event with while (reader.TryRead(out var evt)), or drain a whole batch and react once with reader.DrainBatch()"}
        :else                 :none))

(def state-kernel
  (cond (system-needs-an-instance-field?) {:read StateAllowedAttribute
                                           :use  "[StateAllowed(«reason»)] on the field; arch-check skips only such fields — without it the field breaks the stateless-system ban"}
        (value-valid-for-bounded-frames?) {:read FrameBox
                                           :use  "FrameBox<T>.OneFrame / TwoFrames / ForFrames(value, n); read Value only after Exist — a stale Value throws; Dispose on teardown"}
        :else                             :none))
```

## Existing implementations

```clojure
(def instances
  {:systems  "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py systems --role <role> — roles: reactive, per_frame, pipeline_stage, turn_phase, startup_step, sub_system"
   :recipe   "fmgraph.py pattern <RECIPE> — the recipe's live instances with decided_by base | marker | lexical and deviations; zero instances come with what the signature lacks"
   :families "fmgraph.py pattern PATTERN_ORCHESTRATOR_SUBSYSTEM, and mcp__roslyn__get_type_hierarchy direction Descendants on an abstract base; mcp__roslyn__find_implementations only on an interface"
   :use      #{"join an existing family" "pick a free priority" "avoid a duplicate"}
   :template "the recipe, never a live implementation"
   :never    "a list of implementations written into a document"})

(def signatures-by-marker
  {:law    "a recipe instance is recognized by its signature, and the signature IS a rule for writing the code — a class that misses it is not an instance, however the file reads"
   :branch "decided_by reader is PATTERN_REACTIVE_SYSTEM's own branch: an Update-loop class holding an EventReader<TEvent> field, no marker of its own; decided_by marker is the one branch no base decides — that same field on a class that still ticks every frame, carrying [SystemRole(SystemRoleKind.PerFrame)]; decided_by base is the table/tick-anchor shape, decided_by lexical is a name match and never a substitute for any of these"
   :marks  {PATTERN_PERFRAME_SYSTEM    "[SystemRole(SystemRoleKind.PerFrame)] on a class that ALSO holds an EventReader<TEvent> field"
            PATTERN_VIEW_SYSTEM        "[ViewSubscriber(typeof(TheView))] on the subscribing class, together with the real += it names"
            PATTERN_TRANSACTION_ENTITY "[TagLabel(TagLabelRole.Transaction)] on the label tag the transaction archetype carries"}})
```

## Tag Law before a new tag

```clojure
(def tag-law-before
  (-> (:step-1 "fmgraph.py explain <Tag> — which archetypes carry the tag, as main tag or as label")
      (:step-2 "hold the result against ARCHITECTURE.md → Entities, tag-law")
      (:result "the archetypes that already carry the tag, under :tags of the skill result")))
```
