using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Actions.Components;
using Domains.Actors.Components;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.DistrictBuildCost.Components;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Reactive: on the <see cref="DistrictBuildConfirmedEvent" /> pulse creates the IN-PROGRESS build entity —
    ///     stamps <c>HexIdComponent</c> + <c>DistrictTypeComponent</c> from the pulse payload, a unique
    ///     <c>ActionIdComponent</c> (handed out by the shared <c>ActionIdAllocatorComponent</c> counter, seeded here),
    ///     the turn countdown (<c>BuildDistrictTurnsLeftComponent</c> = the district's <c>TurnsToBuild</c>), the
    ///     chosen payer (<c>ActorTypeComponent</c>, captured now but not yet spent — resource spend is R2), and
    ///     <c>BuildDistrictInProgressTag</c>. There is no draft: the build is committed directly on confirm. Hex,
    ///     district, and payer come from the pulse (the Actions assembly can't read the Presentation selection).
    ///     <c>BuildDistrictTurnTickSystem</c> counts the entity down each turn and <c>BuildDistrictCompletionSystem</c>
    ///     materialises the District fact at zero (a <c>TurnsToBuild</c> of 0 completes on the next frame). See
    ///     <c>Patterns/PATTERN_REACTIVE_SYSTEM.md</c> and <c>Flows/FLOW_DISTRICT_BUILD.md</c>.
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
            entity.Set(new BuildDistrictTurnsLeftComponent { Value = ResolveTurnsToBuild(confirmed.Type) });
            entity.Set(new ActorTypeComponent { Type = confirmed.Payer });
            entity.Set(new BuildDistrictInProgressTag());
        }

        // Turns this district needs, from its DistrictBuildCostConfig. Fail loud: a confirmed build with no cost
        // config is a broken invariant (the UI only offers configured districts), not a benign default.
        // NOTE: this "find DistrictBuildCostConfig by DistrictType" scan is duplicated in the price / hex-resources
        // UI subsystems — consolidating into one Economy helper is tracked as dedup-debt in Flows/FLOW_DISTRICT_BUILD.md.
        private int ResolveTurnsToBuild(DistrictType type)
        {
            if (!_world.Has<DistrictBuildCostsConfigComponent>())
                throw new InvalidOperationException(
                    "BuildDistrictActionSystem: DistrictBuildCostsConfigComponent world component is missing.");

            var districts = _world.Get<DistrictBuildCostsConfigComponent>().Value?.Districts;
            if (districts == null)
                throw new InvalidOperationException(
                    "BuildDistrictActionSystem: DistrictBuildCostsConfig carries no districts.");

            for (var i = 0; i < districts.Length; i++)
                if (districts[i] != null && districts[i].DistrictType == type)
                    return districts[i].TurnsToBuild;

            throw new InvalidOperationException(
                $"BuildDistrictActionSystem: no DistrictBuildCostConfig for district type '{type}'.");
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
