# FantasyMayor — System template

This file will provide instructions and template to create a new system.

## Config loading systems

Config-loading systems belong to the config workflow and are documented in `CONFIGTEMPLATE.md`.

Use `CONFIGTEMPLATE.md` when:
- a module needs `ScriptableObject` config authoring;
- config data must be flattened into ECS components;
- initialization work loads config through `IAddressable`.

Do not maintain a second copy of the `ConfigLoaderSystem` template here. `CONFIGTEMPLATE.md` is the canonical source of truth for that flow.

## System for some specific feature

CONDITION:
Regular and most common system that implements one piece of functionality.
Use this if you need `Update` or `LateUpdate` logic.

DO:
Try to adapt the next template.

ADDON:
This system can be a root system and have a list of specific subsystems.
In this case inject this list.

[UsedImplicitly]
internal sealed class [Name]System : UpdatedSystem
{
    // Constructor with World world and base constructor with a specific entity set.

    protected override void Update(GameState state, in Entity entity)
    {
        // Implement system logic here.
    }

    public override void Dispose()
    {
        // Dispose persistent memory allocations and entity sets here.
    }
}

## SubSystem for complex step-by-step logic

CONDITION:
Subsystems can be implemented under a root system.
They have priority to define execution order.
Use this if you need complex step-by-step logic.
Use this if you need an abstraction layer or DoD polymorphism.

DO:
Try to adapt the next template.
You can choose between `IUniTaskSystem<in T>` or another specific `I[]System<in T>`.

[UsedImplicitly]
internal sealed class [Name]SubSystem : [SystemName]SubSystem
{
    // Constructor with World world and base constructor with a specific entity set.

    // Set execution order by priority.
    // Implement the update method according to the [SystemName]SubSystem signature.

    public override void Dispose()
    {
        // Dispose persistent memory allocations and entity sets here.
    }
}
