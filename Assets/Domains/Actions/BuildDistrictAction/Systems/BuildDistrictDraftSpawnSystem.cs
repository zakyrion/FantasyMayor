using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Actions.BuildDistrictAction.Tags;
using Domains.Actions.Components;
using JetBrains.Annotations;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    // Reactive (PATTERN_REACTIVE_SYSTEM): on a draft-requested pulse, reconciles to exactly one draft
    // BuildDistrictAction. Idempotent — a pulse while a draft already exists is a no-op. The draft is a bare
    // skeleton here (PK + discriminator); later slices Set district/hex/owner/cost onto it, commit it, and tick
    // it. Disposal is BuildDistrictDraftDiscardSystem's job — the create/destroy split (one event each).
    [UsedImplicitly]
    public sealed class BuildDistrictDraftSpawnSystem : UpdatedSystem
    {
        // Runs before EventCleanupSystem (int.MaxValue) so the pulse is consumed the tick it is raised, and
        // before the DistrictBuild UI cluster (~562-565) so the draft exists when edit subsystems read it.
        private const int ExecutionPriority = 560;

        private readonly World _world;
        private readonly EntitySet _drafts;

        public override int Priority => ExecutionPriority;

        public BuildDistrictDraftSpawnSystem(World world)
            : base(world.GetEntities().With<BuildDistrictActionDraftRequestedEvent>().AsSet())
        {
            _world = world;
            _drafts = world.GetEntities().With<BuildDistrictActionTag>().AsSet();
        }

        // The pulse entity is ignored — reconciliation is over current world state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (_drafts.Count > 0)
                return;

            var draft = _world.CreateEntity();
            draft.Set(new ActionIdComponent { Value = AllocateActionId() });
            draft.Set(new BuildDistrictActionTag());
        }

        private int AllocateActionId()
        {
            if (!_world.Has<ActionIdAllocatorComponent>())
                _world.Set(new ActionIdAllocatorComponent { Next = 1 });

            var next = _world.Get<ActionIdAllocatorComponent>().Next;
            _world.Set(new ActionIdAllocatorComponent { Next = next + 1 });
            return next;
        }

        public override void Dispose()
        {
            _drafts.Dispose();
            base.Dispose();
        }
    }
}
