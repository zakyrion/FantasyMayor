---
name: Architecture Reference
description: Pointer to the project architecture document with module structure, ECS style rules, and dependency graph
type: reference
---

`ARCHITECTURE.md` (repo root) is the source of truth for:
- Module folder structure and what belongs where (Components, Systems, Views, etc.)
- ECS entity creation style rules (1 component vs 2+, no chained `.Set().Set()`)
- Assembly dependency graph
- Installer composition rules
- Where to place new feature code
