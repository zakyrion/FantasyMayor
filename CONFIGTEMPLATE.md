# FantasyMayor — Config template

This file will provide instructions and template to create new config related classes.

## 1 - Config - scriptable object

CONDITION:
Use this if you need to create new config
Use this if config should be ScriptableObject
Use this if config data should be authored in Unity editor
Use this if I will ask you about this

DO:
Try to use and addapt to next template

using UnityEngine;

namespace Modules.[ModuleName].Data
{
    [CreateAssetMenu(fileName = "[ConfigName]", menuName = "FantasyMayor/[ModuleGroup]/[ConfigMenuName]")]
    public class [ConfigName] : ScriptableObject
    {
        [Header("[GroupName]")]
        public [FieldType] [fieldName];
        //Add here all required fields for engine resources or config variable
    }
}

## 2 - Component - struct

CONDITION:
Use this if you need runtime ECS representation of config
Use this if loaded config should be transformed to component
Use this if runtime systems should read flattened config data

DO:
Try to use and addapt to next template

using Modules.[ModuleName].Data;

namespace Modules.[ModuleName].Components
{
    public struct [Name]ConfigComponent
    {
        public [FieldType] [FieldName];
        public int [IntField];
        public float [FloatField];

        public static [Name]ConfigComponent FromConfig([ConfigType] config)
        {
            return new [Name]ConfigComponent
            {
                [FieldName] = config.[fieldName],
                [IntField] = config.[intField],
                [FloatField] = config.[floatField],
            };
        }
    }
}

## 3 - ConfigLoaderSystem

CONDITION:
This system is responsible for module initialization and setup during config loading stage
Use it if you need to load some config from addressable
Use it if you need to prepare module data or runtime entities required by other systems
Use it if I will ask you about this

RULES:
This system can:
- load one or many configs from addressable
- create flattened ECS config components from loaded configs
- create derived runtime data, helper structures, singleton entities, or initialization entities
- prepare and initialize module state required for full work of other systems
- validate loaded config data before marking module as loaded

This system should not:
- contain regular Update or LateUpdate runtime logic
- contain frame to frame gameplay logic
- react to gameplay events that belong to regular systems or subsystems

DO:
Use next template

[UsedImplicitly]
public sealed class [SystemName]ConfigLoaderSystem : ConfigLoaderSystem
{
    private const string [Name]_CONFIG = [Addressable name for config];

    public [SystemName]ConfigLoaderSystem(IAddressable addressable, World world) : base(addressable, world){}

     protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var [ConfigType]Config = Box<[ConfigType]>.Empty();

            try
            {
                [ConfigType]Config = await LoadConfigAsync<[ConfigType]>([Name]_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return;

                World.Set([ComponentName]ConfigComponent.FromConfig([ConfigType]Config.Value));
                //optional: create additional runtime setup entities or derived data for module initialization
                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref [ConfigType]Config);
            }
        }
}

STORAGE RULE:
The flattened config component is stored as a WORLD component via
`World.Set<[ComponentName]ConfigComponent>(...)` — not on a created entity.
- Consumers read it with `World.Get<[ComponentName]ConfigComponent>()`, guarded by `World.Has<...>()`.
- A world component is NOT an entity: it never appears in `world.GetEntities()` and cannot be matched
  by `With<T>` / `WhenAdded<T>` / `WhenChanged<T>`. If a config must trigger reactive systems, raise an
  explicit event component on an entity instead.
- Use `World.CreateEntity().Set(...)` only for the optional DERIVED runtime entities a loader may also
  create, never for the config component itself.

`CONFIGTEMPLATE.md` is the canonical source of truth for `ConfigLoaderSystem` templates.
`SYSTEMTEMPLATE.md` should only reference this section instead of duplicating it.

## 4 - IAddressable

CONDITION:
Use this if you work with addressable config loading
Use this before writing or changing code that depends on IAddressable, Box<T>, Result<T>, or addressable asset ownership

DO:
Read next file

Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md
