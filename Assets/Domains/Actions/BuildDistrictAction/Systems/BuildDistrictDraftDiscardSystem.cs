using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Actions.BuildDistrictAction.Tags;
using JetBrains.Annotations;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    // Reactive (PATTERN_REACTIVE_SYSTEM): on a discard-requested pulse, disposes the pending draft
    // BuildDistrictAction. Idempotent — no draft ⇒ no-op. Until the commit slice exists EVERY window close is a
    // discard; once commit lands, a committed action is no longer a bare draft and survives the close.
    [UsedImplicitly]
    public sealed class BuildDistrictDraftDiscardSystem : UpdatedSystem
    {
        // Runs before EventCleanupSystem (int.MaxValue) so the pulse is consumed the tick it is raised.
        private const int ExecutionPriority = 561;

        private readonly EntitySet _drafts;

        public override int Priority => ExecutionPriority;

        public BuildDistrictDraftDiscardSystem(World world)
            : base(world.GetEntities().With<BuildDistrictActionDiscardRequestedEvent>().AsSet())
        {
            _drafts = world.GetEntities().With<BuildDistrictActionTag>().AsSet();
        }

        // The pulse entity is ignored — the discard target is the current draft set (a singleton by construction;
        // the loop disposes high→low so a swap-remove never disturbs a not-yet-visited index).
        protected override void Update(GameState state, in Entity pulse)
        {
            var drafts = _drafts.GetEntities();
            for (var index = drafts.Length - 1; index >= 0; index--)
                drafts[index].Dispose();
        }

        public override void Dispose()
        {
            _drafts.Dispose();
            base.Dispose();
        }
    }
}
