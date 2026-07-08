using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Actions.Components;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using Modules.AxialSystem;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Reactive: on the <see cref="DistrictBuildConfirmedEvent" /> pulse creates the committed build entity —
    ///     stamps <c>HexIdComponent</c> + <c>DistrictTypeComponent</c> from the pulse payload, a unique
    ///     <c>ActionIdComponent</c> (handed out by the shared <c>ActionIdAllocatorComponent</c> counter, seeded here),
    ///     and <c>BuildDistrictActionTag</c>. There is no draft: the district is built directly on confirm. Hex and
    ///     district come from the pulse (the Actions assembly can't read the Presentation selection). See
    ///     <c>Patterns/PATTERN_REACTIVE_SYSTEM.md</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictActionSystem : UpdatedSystem
    {
        private readonly World _world;

        public override int Priority => SystemPriorities.RuntimeTick.BuildDistrictAction;

        public BuildDistrictActionSystem(World world)
            : base(world.GetEntities().With<DistrictBuildConfirmedEvent>().AsSet())
        {
            _world = world;

            // Seed the shared action-id counter once; ids start at 1 (0 = unset).
            if (!world.Has<ActionIdAllocatorComponent>())
                world.Set(new ActionIdAllocatorComponent { Next = 1 });
        }

        protected override void Update(GameState state, in Entity pulse)
        {
            var confirmed = pulse.Get<DistrictBuildConfirmedEvent>();

            var entity = _world.CreateEntity();
            entity.Set(new HexIdComponent { Coords = confirmed.Coords });
            entity.Set(new DistrictTypeComponent { Value = confirmed.Type });
            entity.Set(new ActionIdComponent { Value = AllocateId() });
            entity.Set(new BuildDistrictActionTag());

            RaiseDistrictBuilt(confirmed.Coords, confirmed.Type);
        }

        // One-frame built pulse on its own entity; carries the hex + district (copied from the payload) as sibling
        // components. Signals DistrictViewSpawnSystem to spawn the district view. Cleared by EventCleanupSystem.
        private void RaiseDistrictBuilt(HexCoord coords, DistrictType type)
        {
            var entity = _world.CreateEntity();
            entity.Set(new DistrictBuiltEvent());
            entity.Set(new HexIdComponent { Coords = coords });
            entity.Set(new DistrictTypeComponent { Value = type });
            entity.Set(new EventTag());
        }

        // Hands out the next unique action id and advances the shared counter (write via Set).
        private int AllocateId()
        {
            var id = _world.Get<ActionIdAllocatorComponent>().Next;
            _world.Set(new ActionIdAllocatorComponent { Next = id + 1 });
            return id;
        }
    }
}
