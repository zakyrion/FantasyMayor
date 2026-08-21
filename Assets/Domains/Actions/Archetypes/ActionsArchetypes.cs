using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.Components;
using Domains.Actors.Components;
using Domains.Economy.District.Components;
using Friflo.Engine.ECS;

namespace Domains.Actions.Archetypes
{
    /// <summary>
    ///     Resolves the archetypes owned by the Domains.Actions assembly. Scope is the ASSEMBLY, not a
    ///     feature. Each method returns the live <see cref="Archetype" /> — entities are created BY it and
    ///     iterated THROUGH it; the caller keeps it in its own field. This holder stores nothing.
    /// </summary>
    public static class ActionsArchetypes
    {
        /// <summary>
        ///     The in-flight build action: its own PK, the district it builds, the turns left, and who pays.
        /// </summary>
        public static Archetype BuildDistrictInProgress(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<DistrictIdFKComponent, ActionIdComponent, BuildDistrictTurnsComponent,
                    ActorTypeComponent>(),
                Tags.Get<BuildDistrictInProgressTag>());
    }
}
