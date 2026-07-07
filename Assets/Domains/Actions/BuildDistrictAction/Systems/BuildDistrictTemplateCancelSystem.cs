using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using JetBrains.Annotations;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Reactive: on the <see cref="DistrictBuildCancelledEvent" /> pulse (overlay dismissed without confirming)
    ///     discards the draft build entity — disposes the entity carrying <c>BuildDistrictActionTemplateTag</c>
    ///     (exactly one while the overlay is open). Committed entities (template tag already stripped on confirm) are
    ///     untouched. Deleting nothing is a valid idempotent no-op (a cancel is the sanctioned quiet return). See
    ///     <c>Patterns/PATTERN_REACTIVE_SYSTEM.md</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictTemplateCancelSystem : UpdatedSystem
    {
        // Reactive: must run before the cleanup pass so the cancel pulse is consumed the tick it is raised.
        private const int ExecutionPriority = 592;

        private readonly EntitySet _templates;

        public override int Priority => ExecutionPriority;

        public BuildDistrictTemplateCancelSystem(World world)
            : base(world.GetEntities().With<DistrictBuildCancelledEvent>().AsSet())
        {
            _templates = world.GetEntities().With<BuildDistrictActionTemplateTag>().AsSet();
        }

        protected override void Update(GameState state, in Entity pulse)
        {
            // Exactly one draft at a time; disposing it ends this single-element iteration.
            foreach (var entity in _templates.GetEntities())
                entity.Dispose();
        }

        public override void Dispose()
        {
            _templates.Dispose();
            base.Dispose();
        }
    }
}
