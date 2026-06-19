---
category: A
read: reference
tags: [ui]
related:
  - "[MAIN_UI](../MainUI/MAIN_UI.md)"
status: implemented
---

# MainCanvas

Singleton provider for the main UI canvas root, behind an interface for DI.

## Purpose
Inject `IMainCanvasProvider` anywhere you need the parent transform/GameObject for UI
instantiation, instead of finding the canvas by name or tag.

## Non-Obvious Invariants
- The canvas root is supplied once from a serialized field on the app-root installer
  (a scene reference). The provider only stores and exposes it — it does not create the canvas.

## Current State
Stable. Contract + thin provider.
