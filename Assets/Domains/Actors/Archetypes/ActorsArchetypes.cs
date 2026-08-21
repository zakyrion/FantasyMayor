using Domains.Actors.City.Components;
using Domains.Actors.City.Tags;
using Domains.Actors.Components;
using Domains.Actors.Mayor.Components;
using Domains.Actors.Mayor.Tags;
using Friflo.Engine.ECS;

namespace Domains.Actors.Archetypes
{
    /// <summary>
    ///     Resolves the archetypes owned by the Domains.Actors assembly. Scope is the ASSEMBLY, not a
    ///     feature. Each method returns the live <see cref="Archetype" /> — entities are created BY it and
    ///     iterated THROUGH it; the caller keeps it in its own field. This holder stores nothing.
    ///     The owner-keyed resource rows are NOT resolved here: their archetype is generic and lives with
    ///     ResourceComponent in Domains.Economy — this assembly only closes it with its FK + tag types.
    /// </summary>
    public static class ActorsArchetypes
    {
        /// <summary>The city actor row: PK + actor discriminator.</summary>
        public static Archetype City(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<CityIdComponent, ActorTypeComponent>(),
                Tags.Get<CityTag>());

        /// <summary>The mayor actor row: PK + actor discriminator + the AP budget columns.</summary>
        public static Archetype Mayor(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<MayorIdComponent, ActorTypeComponent, MayorAPRestoreComponent,
                    MayorAPComponent>(),
                Tags.Get<MayorTag>());
    }
}
