using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Actions.Components;
using Domains.Economy.District.Components;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using UnityEngine;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Reactive: on the <see cref="DistrictBuildConfirmedEvent" /> pulse promotes the draft build entity into a
    ///     committed one — stamps a unique <c>ActionIdComponent</c> (handed out by the shared
    ///     <c>ActionIdAllocatorComponent</c> counter, seeded here) and swaps <c>BuildDistrictActionTemplateTag</c> →
    ///     <c>BuildDistrictActionTag</c>. The pulse entity is ignored — the target is the entity carrying the template
    ///     tag (exactly one while the overlay is open). See <c>Patterns/PATTERN_REACTIVE_SYSTEM.md</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictActionSystem : UpdatedSystem
    {
        private readonly World _world;
        private readonly EntitySet _templates;

        public override int Priority => SystemPriorities.RuntimeTick.BuildDistrictAction;

        public BuildDistrictActionSystem(World world)
            : base(world.GetEntities().With<DistrictBuildConfirmedEvent>().AsSet())
        {
            _world = world;
            _templates = world.GetEntities().With<BuildDistrictActionTemplateTag>().AsSet();

            // Seed the shared action-id counter once; ids start at 1 (0 = unset).
            if (!world.Has<ActionIdAllocatorComponent>())
                world.Set(new ActionIdAllocatorComponent { Next = 1 });
        }

        protected override void Update(GameState state, in Entity pulse)
        {
            var templates = _templates.GetEntities();

            // Confirm can only fire while the overlay is open, which means the draft was spawned — its absence is a
            // broken invariant, not a benign no-op.
            if (templates.Length == 0)
                throw new InvalidOperationException(
                    "BuildDistrictActionSystem: confirm pulse with no BuildDistrictActionTemplateTag entity — " +
                    "the draft build entity must exist while the overlay is open.");

            // The district being built is the LIVE UI selection at confirm — read from the world component, not the
            // draft's DistrictTypeComponent (stamped once at open, never re-synced on later picks). Re-stamp the
            // entity so the promoted build carries the confirmed district. Get throws if the selection is missing —
            // a broken invariant while the overlay is open, not a benign no-op.
            var districtType = new DistrictTypeComponent { Value = _world.Get<DistrictBuildSelectionComponent>().Selected };

            // Exactly one draft at a time; dropping the template tag ends this single-element iteration.
            foreach (var entity in templates)
            {
                var hexId = entity.Get<HexIdComponent>();

                entity.Set(districtType);
                entity.Set(new ActionIdComponent { Value = AllocateId() });
                entity.Remove<BuildDistrictActionTemplateTag>();
                entity.Set(new BuildDistrictActionTag());

                RaiseDistrictBuilt(hexId, districtType);
            }
        }

        // One-frame built pulse on its own entity; carries the hex + district (copied from the draft) as sibling
        // components. Signals DistrictViewSpawnSystem to spawn the district view. Cleared by EventCleanupSystem.
        private void RaiseDistrictBuilt(HexIdComponent hexId, DistrictTypeComponent districtType)
        {
            var entity = _world.CreateEntity();
            entity.Set(new DistrictBuiltEvent());
            entity.Set(hexId);
            entity.Set(districtType);
            entity.Set(new EventTag());
        }

        // Hands out the next unique action id and advances the shared counter (write via Set).
        private int AllocateId()
        {
            var id = _world.Get<ActionIdAllocatorComponent>().Next;
            _world.Set(new ActionIdAllocatorComponent { Next = id + 1 });
            return id;
        }

        public override void Dispose()
        {
            _templates.Dispose();
            base.Dispose();
        }
    }
}
