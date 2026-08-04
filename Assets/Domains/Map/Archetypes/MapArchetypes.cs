using Domains.Map.Hex.Components;
using Domains.Map.Hex.Tags;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Tags;
using Friflo.Engine.ECS;

namespace Domains.Map.Archetypes
{
    /// <summary>
    ///     Resolves the archetypes owned by the Domains.Map assembly. Scope is the ASSEMBLY, not a feature —
    ///     an archetype may only name components its own assembly can reference.
    ///     Each method returns the live <see cref="Archetype" />: entities are created BY it
    ///     (<c>archetype.CreateEntity()</c>) and iterated THROUGH it (<c>archetype.Entities</c>). A caller
    ///     resolves once and keeps the archetype in its own field — this holder stores nothing, so it never
    ///     binds to a particular <see cref="EntityStore" />.
    ///     A row is born COMPLETE: every column an entity will ever carry is part of its archetype, so
    ///     <c>CreateEntity()</c> already provides it at <c>default</c> and no later write can move the entity.
    /// </summary>
    public static class MapArchetypes
    {
        /// <summary>
        ///     The terrain grid row: hex PK, generated level, terrain kind. The terrain kind is a birth
        ///     column starting at <c>HexType.Unknown</c> — the generation pass writes the real value into a
        ///     column that already exists, so assigning a type is a value write, never a structural change.
        /// </summary>
        public static Archetype Hex(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<HexIdComponent, HexLevelComponent, HexTypeComponent>(),
                Tags.Get<HexTag>());

        /// <summary>A resource deposit on a hex, FK-keyed to the hex row.</summary>
        public static Archetype HexResource(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<HexIdFKComponent, HexResourceComponent>(),
                Tags.Get<HexResourceTag>());
    }
}
