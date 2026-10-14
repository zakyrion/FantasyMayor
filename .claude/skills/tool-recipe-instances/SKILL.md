---
name: tool-recipe-instances
description: "Which Patterns/ recipe a system or type follows, every live instance of a recipe and its deviations, the systems of one role (reactive, per_frame, pipeline_stage, startup_step, turn_phase, sub_system) with priorities — ask fmgraph (pattern / systems / search), never open a live system as a template."
user-invocable: false
---

# Instances of a recipe

```clojure
{:tool-skill "tool-recipe-instances"
 :question "which recipe does this follow, where are its live instances and deviations, which systems have this role"
 :order "run fmgraph: pattern <RECIPE> for instances, groups and deviations with the rule id; systems --role <role> for systems by role and priority; search <keyword> --role <role> to locate one"
 :because "the recipe signature and the role decision are computed from bases, markers and DI together — reading one system by hand shows one case, not the rule or its exceptions"
 :invoke "python3 .claude/skills/fantasymayor-graph/scripts/fmgraph.py systems --role reactive"
 :example {:call "fmgraph.py systems --role reactive"
           :returned "18 systems — 501 HexSelectionViewSystem (reader), 550 HexInfoPanelSystem (reader) …"}
 :blind-spots #{"a recipe with zero instances prints what its signature looked for — zero can be a signature gap, not an absence"
                "the recipe TEXT is Patterns/PATTERN_*.md — the recipe to follow goes into :read, this skill only finds instances"}
 :fallback "read the recipe file itself; never a live system as a template"}
```
