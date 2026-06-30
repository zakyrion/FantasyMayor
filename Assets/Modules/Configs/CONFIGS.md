---
category: A
read: reference
tags: [config, addressables]
related:
  - "[ADDRESSABLE_PATTERNS](../Addressable/ADDRESSABLE_PATTERNS.md)"
status: implemented
code_refs:
  interfaces: [IConfigProvider]
  types:      [ConfigProvider]
---

# Configs

Generic async loader pattern for ScriptableObject configs from Addressables.

## Purpose
Shared `IConfigProvider<T>` / `ConfigProvider<T>` so feature modules don't each reimplement
"load a config ScriptableObject by address". Compiled into consuming modules — no dedicated assembly.

## How To Use Correctly
- **Common mistake:** `ConfigProvider<T>` uses `nameof(T)` as the addressable address.
  The ScriptableObject asset's addressable key **must exactly match the type name**.
  Rename the type → the address breaks silently.
- Used internally by a module's `ConfigLoaderSystem`. See `CameraMovementConfigLoaderSystem`
  (UserInput) for the canonical wiring.
- For the Box/Result ownership rules around the underlying `IAddressable.LoadAsync`,
  follow `Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md`.

## Current State
Stable.
