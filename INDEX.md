---
category: C
read: always
tags: [index, navigation]
related:
  - "[DOC_STANDARD](DOC_STANDARD.md)"
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# INDEX

> ⚠️ **Key file — the single entry point for all doc navigation. Keep it short and informative.** Built in 2 passes: (1) `python3 Tools/gen_index.py` rebuilds the skeleton between the markers from each doc's frontmatter + first line; (2) the agent curates descriptions, statuses, and context. To change a description or status, edit the doc's first line / `status` frontmatter and re-run pass 1 — do not edit between the markers. The agent zone below the END marker is preserved across runs.

<!-- BEGIN GENERATED — Tools/gen_index.py rebuilds everything between these markers; edits here are overwritten -->

Totals: 25 docs — 2 always · 21 trigger · 2 reference · 3 canvas.

## Read at start (always)

Read these every session before doing anything else.

- [FantasyMayor — Architecture Reference](ARCHITECTURE.md) — `Unity App UI` (`com.unity.dt.app-ui`) as the component foundation — see `GENERAL_UI_STYLE.md` §15
- [CLAUDE.md](CLAUDE.md) — This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Read on demand (by trigger)

Do **not** preload. Read only when the trigger condition holds.

| Doc | Read it… | What it is |
|---|---|---|
| [DOC_STANDARD.md](DOC_STANDARD.md) | before authoring or reviewing any .md (Flows / Patterns / policy) for standard compliance | Single source of truth for how to write Markdown docs in this project. |
| [FantasyMayor — ECS & Runtime Conventions](ECS_CONVENTIONS.md) | before writing or editing any ECS system, component, event, config, or query | The ECS/runtime rulebook: where state lives, how systems are decomposed, and the write / collection / |
| [FLOW — District Build](Flows/FLOW_DISTRICT_BUILD.md) | before touching district build, the District table, or district open conditions (Domains.Actions.BuildDistrictAction, Domains.Economy.District, Domains.Economy.DistrictOpenCondition, Presentation.Districts) | One row per district from confirm to built; the buildable set is derived from that same table. |
| [FantasyMayor - Gameplay Foundation](GAMEPLAY_FOUNDATION.md) | ONLY when the user explicitly asks to open this file — never on session-start, never by topic/keyword | `FantasyMayor` is a turn-based game about governing a city through a scarcity of `Action Points`, limited resources, population as a productive and political force, and an unstable balance of power between the mayor and the local elites. |
| [GENERAL_UI_STYLE.md](GENERAL_UI_STYLE.md) | before creating or changing UI (UI Toolkit, panels, tokens, USS) | The general UI design language for FantasyMayor: the global HUD layout model, design principles, visual |
| [GLOSSARY — domain vocabulary → code anchors](GLOSSARY.md) | when a domain term (any language) needs its canonical code name before searching roslyn / ecs-graph / di-graph | Map from human vocabulary (game-design terms, Ukrainian/English synonyms, abbreviations) to the |
| [IAddressable Contract](Patterns/ADDRESSABLE_PATTERNS.md) | before writing/editing/reviewing Addressables, IAddressable, Box<T> or Result<T> code | Single source of truth for addressable loading. Read this; do not grep. |
| [Pattern — One-Frame Event Cleanup](Patterns/PATTERN_CLEANUP_SYSTEM.md) | before writing any one-frame-event cleanup (and to learn why you usually should not) | **You almost never write a cleanup system.** There is ONE global `EventCleanupSystem` (`EcsExtensions`): a |
| [Pattern — ECS Data Component](Patterns/PATTERN_COMPONENT.md) | before creating an ECS data component (a struct holding runtime values) | A component is a plain `struct` of runtime values. No behavior, no methods (except equality when it is a |
| [Pattern — Config (ScriptableObject + Component)](Patterns/PATTERN_CONFIG.md) | before creating a ScriptableObject config and its runtime component | Authored data lives in a `ScriptableObject`, loaded via Addressables, and published as a **world component** |
| [Pattern — Config Loader System](Patterns/PATTERN_CONFIG_LOADER.md) | before creating a config loader system | A one-shot system that runs at `ConfigLoadStep`: loads config SO(s) from Addressables, validates them, and |
| [Pattern — One-Frame Event (Pulse)](Patterns/PATTERN_EVENT.md) | before creating a one-frame ECS event (pulse) | An event is a **payload-less `struct`** raised on its own entity. It says "something changed — re-read the |
| [Pattern — Orchestrator + SubSystems](Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md) | before creating an orchestrator + subsystem family (DoD polymorphism / independently ordered parts) | A family of implementations behind one abstract base, DI-collected into an orchestrator that sequences them by |
| [Pattern — Per-Frame System](Patterns/PATTERN_PERFRAME_SYSTEM.md) | before creating a per-frame system (genuinely continuous logic) | Logic that is genuinely continuous: camera movement, per-frame projection, input polling, selection watching. |
| [Pattern — Pipeline Stage (one-shot, world-init)](Patterns/PATTERN_PIPELINE_STAGE.md) | before creating a world-init pipeline stage (build/spawn content once during map creation) | One-shot async construction during map creation: spawn entities/views, build runtime world components, load |
| [Pattern — Polymorphic Config Catalogue → Entity Table](Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md) | before creating a polymorphic ScriptableObject config catalogue that materializes into an entity table (many kinds keyed by a shared FK), or a per-kind polymorphic system family over such a table | A **heterogeneous** set of authored rules/effects — many *kinds*, each with its own parameters — that you (1) author as |
| [Pattern — Reactive Orchestrator System (pulse → fan-out)](Patterns/PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM.md) | before creating a reactive system whose event handling has several independently-ordered parts (fan-out) | A [reactive system](PATTERN_REACTIVE_SYSTEM.md) whose handling is **too big for one file**: on the pulse it fans |
| [Pattern — Reactive System (pulse + reconcile)](Patterns/PATTERN_REACTIVE_SYSTEM.md) | before creating a reactive (event-driven) system | **The default for runtime logic.** Responds to a one-frame [event](PATTERN_EVENT.md): the event's archetype |
| [Pattern — ECS Tag](Patterns/PATTERN_TAG.md) | before creating an ECS tag (field-less marker / table discriminator) | A tag is an **empty `struct`** that marks an entity. It carries no data; its presence IS the information. |
| [Pattern — Transaction Entity (cross-domain behavior)](Patterns/PATTERN_TRANSACTION_ENTITY.md) | before building any multi-step behavior that spans more than one subdomain (a cross-domain transaction) | A multi-step behavior that spans subdomains gets exactly ONE home: a **transaction entity** in the |
| [Pattern — View ↔ System](Patterns/PATTERN_VIEW_SYSTEM.md) | before creating a MonoBehaviour view + its driving system, or wiring how a view and its system talk | A MonoBehaviour View is dumb chrome driven by its System; they talk directly: C# event in, push-to-view out, never ECS. |

## Reference map (on demand)

Reference docs read on demand.

| Doc | Cat | Status | What it is |
|---|---|---|---|
| [AGENTS.md](AGENTS.md) | C | — | Codex bootstrap: full FantasyMayor process contract, Codex-native — HARD GATE, go/done, notation ext, tools, policies. |
| [Як читати і писати Clojure-інструкції](CLOJURE_GUIDE.md) | B | — | Людський підручник до Clojure-нотації задач і правил: реальний синтаксис Clojure як мова |

## Canvas map (on demand)

Visual maps (Obsidian Canvas). Read/edit via Obsidian MCP; not preloaded.

| Canvas | What it maps |
|---|---|
| [DISTRICT_BUILDING_UI](DISTRICT_BUILDING_UI.canvas) | DistrictBuildUISystem |
| [ECONOMY_ACTORS](ECONOMY_ACTORS.canvas) | Ownables — each carries one OwnerFK + a Tag · My domain view · Owners — actors with an Id used as OwnerFK · Resource… |
| [WORK](WORK.canvas) | Колись на потім |

<!-- END GENERATED — content below is the agent zone (pass 2), preserved across runs -->

## Context & Notes (agent-maintained — pass 2)

Curate what the script can't derive: current focus, stale docs, cross-doc orientation. Keep it short. Preserved across `gen_index.py` runs.

- **Module/domain/presentation MDs are ABOLISHED (2026-07-09, tool-first flip)** — never recreate one.
  Module knowledge = code header comments + `roslyn` / `ecsg.py` / `dig.py`; doc symbol claims are
  checked by `Tools/doc_lint.py`.
- **Pattern recipes (`Patterns/PATTERN_*.md`)** are the granular, one-approach-per-file skeletons for the ECS
  building blocks (component / tag / event / config / config-loader / pipeline-stage / orchestrator+subsystem /
  per-frame / reactive / cleanup). **Read the matching recipe instead of opening a live system as a reference.**
  They replace the retired `SYSTEMTEMPLATE.md` / `CONFIGTEMPLATE.md` monoliths; the picker index is
  `ARCHITECTURE.md` → **Pattern Recipes**.
- **`WORK.canvas` is the user's living task-intake scratchpad** — he states tasks there as a graphic
  scheme instead of text. It always changes and contains nothing finished: never treat it as stale,
  orphaned, or a deletion candidate.
