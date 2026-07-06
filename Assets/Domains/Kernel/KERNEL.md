---
category: A
read: reference
tags: [kernel, domain, shared-kernel]
related:
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[ECONOMY](../Economy/ECONOMY.md)"
  - "[ACTORS](../Actors/ACTORS.md)"
status: implemented
code_refs:
  enums: [ActorType]
---

# Kernel

The DDD Shared Kernel: cross-context vocabulary tokens that would otherwise force a dependency
between unrelated domains.

## Purpose
`ActorType` is owner-type vocabulary needed by BOTH `Economy` (district owner-affinity, e.g. "who
may build this district type") and `Actors` (actor identity, the Table Rule discriminator). Putting
it in either domain would force the other to depend on it. Kernel is the neutral home: a leaf both
domains can reference without creating a cross-context edge between them.

## Non-Obvious Invariants
- **The kernel stays minimal by design.** It holds ONLY cross-context vocabulary — today just
  `ActorType`. It carries no logic, no systems, no components, no Unity engine dependency, and no
  owner foreign-key components. Do not add behavior here: a kernel that grows systems or state
  stops being a shared kernel and becomes a dependency magnet.
- **Owner foreign keys are NOT kernel material.** `CityIdComponent` / `MayorIdComponent` stay in
  `Actors` deliberately — they are actor-owned identity, not shared vocabulary. Only the type-token
  enum (`ActorType`) lives here; `ActorTypeComponent` (the component wrapping it) stays in `Actors`.
- `ActorType` is a `[Flags]` enum so callers can express an allowed-owner **mask** (e.g. "Mayor or
  City"), not just a single owner kind.

## Design Decisions
- Kernel is a DAG leaf: `Actors → Kernel`, `Economy → Kernel`, and Kernel depends on nothing. This
  keeps the domain DAG acyclic while letting Economy speak the owner-type vocabulary without
  depending on the `Actors` domain.

## Current State
Implemented and minimal: one enum (`ActorType`), no systems, no components. Any addition here should
be re-justified against the "shared cross-context vocabulary only" bar above.
