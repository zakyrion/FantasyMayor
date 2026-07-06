using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Events;
using JetBrains.Annotations;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Reactive-orchestrator for the build-district verb. Its base set is the
    ///     <see cref="DistrictBuildConfirmedEvent" /> pulse, so it fires the tick the player confirms «Збудувати».
    ///     This is the SHELL: it owns no domain logic. The build mechanics (spawn the build entity, spend
    ///     resources, set turns-left, …) land as a DI-collected subsystem family fanned out from here in the next
    ///     one-mechanic steps; until the first subsystem exists this is an intentional no-op catch that proves the
    ///     confirm → pulse → system wiring. See <c>Patterns/PATTERN_REACTIVE_ORCHESTRATOR_SYSTEM.md</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictActionSystem : UpdatedSystem
    {
        // Reactive: must run before the cleanup pass so the confirm pulse is consumed the tick it is raised.
        private const int ExecutionPriority = 600;

        public override int Priority => ExecutionPriority;

        public BuildDistrictActionSystem(World world)
            : base(world.GetEntities().With<DistrictBuildConfirmedEvent>().AsSet())
        {
        }

        // Catch-only shell. Reconciliation against the current selection/hex and the fan-out to build subsystems
        // arrive with the first mechanic; the pulse entity is ignored by design (reconcile-against-state, not delta).
        protected override void Update(GameState state, in Entity pulse)
        {
        }
    }
}
