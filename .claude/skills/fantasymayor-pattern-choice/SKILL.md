---
name: fantasymayor-pattern-choice
description: Pick the Patterns/ recipes and shared-kernel types every code part of a FantasyMayor task needs, find what already implements them, and check the Tag Law before a new tag or archetype. Use when a FantasyMayor task statement is formed for work that creates or changes code (CLAUDE.md § 2 puts the recipes into :read and the result into :skills), or when the user invokes fantasymayor-pattern-choice.
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
    (view? part)         PATTERN_VIEW_SYSTEM
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
  (cond (spans-several-subdomains?)                                          PATTERN_TRANSACTION_ENTITY
        (and (builds-world-at-map-creation?) (independently-ordered-parts?)) PATTERN_ORCHESTRATOR_SUBSYSTEM
        (builds-world-at-map-creation?)                                      PATTERN_PIPELINE_STAGE
        (runs-as-a-turn-phase?)                                              PATTERN_ORCHESTRATOR_SUBSYSTEM
        (and (reacts-to-an-event?) (independently-ordered-parts?))           PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM
        (reacts-to-an-event?)                                                PATTERN_REACTIVE_SYSTEM
        (continuous-every-frame?)                                            PATTERN_PERFRAME_SYSTEM
        (cleans-up-events?)                                                  PATTERN_CLEANUP_SYSTEM
        :else                                                                :no-recipe))
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
        :else                          {:read UpdatedSystem
                                        :use  "pass the store and one archetype from its holder — an ArchetypeQuery only when the trigger spans several archetypes; implement Update(state, entity) and Priority; structural changes inside Update are safe, but not inside an enumeration Update opens itself (ARCHITECTURE.md → Threading, structural-change)"}))

(def config-kernel
  (cond (registering-a-config-for-loading?) [{:read ConfigLoaderSystem
                                              :use  "one installer registration per config: Register<ConfigLoaderSystem<X>>().As<IUniTaskSystem>().WithParameter(AppState.ConfigLoading).WithParameter(«address», ConfigAddresses.X) — never a hand-written loader"}
                                             {:read ConfigAddresses
                                              :use  "add a constant holding the addressable entry name as authored in Unity; installers pass the constant, never a string literal"}]
        (authoring-a-config-type?)          {:read IValidatableConfig
                                             :use  "implement Validate() and throw on every authoring violation; it runs right after loading"}
        :else                               :none))

(def event-kernel
  (cond (raising-an-event?)   {:read EcsEventExtensions
                               :use  "store.CreateEvent(new TEvent { … }) — the row is born in its event archetype with its values; never AddComponent on a live entity"}
        (consuming-an-event?) [{:read EventArchetypes
                                :use  "EventArchetypes.Of<TEvent>(store) is the archetype the consuming system anchors on; the caller keeps it"}
                               {:read EcsEventExtensions
                                :use  "handle the event only while IsRipe(entity) is true; its values are entity.GetComponent<TEvent>()"}]
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
  {:systems  "python3 ~/.claude/skills/ecs-graph/scripts/ecsg.py systems --role <role> — roles: per_frame, pipeline_stage, sub_system, turn_phase"
   :reactive "python3 ~/.claude/skills/ecs-graph/scripts/ecsg.py explain <System> — a reacts_to edge marks a reactive system; ecs-graph lists reactive systems under per_frame"
   :families "mcp__roslyn__find_implementations on the recipe's base type or interface — the family's abstract base and its members"
   :use      #{"join an existing family" "pick a free priority" "avoid a duplicate"}
   :template "the recipe, never a live implementation"
   :never    "a list of implementations written into a document"})
```

## Tag Law before a new tag

```clojure
(def tag-law-before
  (-> (:step-1 "python3 ~/.claude/skills/ecs-graph/scripts/ecsg.py explain <Tag> — which archetypes already carry the tag")
      (:step-2 "hold the result against ARCHITECTURE.md → Entities, tag-law")
      (:result "the archetypes that already carry the tag, under :tags of the skill result")))
```
