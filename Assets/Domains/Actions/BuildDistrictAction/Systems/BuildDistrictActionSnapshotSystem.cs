using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Actions.BuildDistrictAction.Tags;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.DistrictBuildCost.Components;
using Domains.Economy.DistrictBuildCost.Configs;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    // Reactive (PATTERN_REACTIVE_SYSTEM): on a snapshot-requested pulse, writes the selection (DistrictType +
    // target hex FK) and a frozen cost snapshot (AP + resource prices, joined from Economy's DistrictBuildCost
    // catalogue by DistrictType) onto the draft BuildDistrictAction. Runs after the draft spawn (priority 560).
    // Fails loud when a snapshot arrives with no draft, no cost catalogue, or a DistrictType absent from the
    // catalogue — all are authoring/ordering errors, not tolerated states. TurnsToBuild is seeded at commit (4c).
    [UsedImplicitly]
    public sealed class BuildDistrictActionSnapshotSystem : UpdatedSystem
    {
        // Runs before EventCleanupSystem (int.MaxValue) and after BuildDistrictDraftSpawnSystem (560) so the draft
        // exists when a same-frame snapshot lands.
        private const int ExecutionPriority = 563;

        private readonly World _world;
        private readonly EntitySet _drafts;

        public override int Priority => ExecutionPriority;

        public BuildDistrictActionSnapshotSystem(World world)
            : base(world.GetEntities().With<BuildDistrictActionSnapshotRequestedEvent>().AsSet())
        {
            _world = world;
            _drafts = world.GetEntities().With<BuildDistrictActionTag>().AsSet();
        }

        protected override void Update(GameState state, in Entity pulse)
        {
            var request = pulse.Get<BuildDistrictActionSnapshotRequestedEvent>();

            var drafts = _drafts.GetEntities();
            if (drafts.Length == 0)
                throw new InvalidOperationException(
                    "BuildDistrictAction snapshot requested but no draft exists — draft creation must precede snapshot.");

            var draft = drafts[0];
            draft.Set(new DistrictTypeComponent { Value = request.DistrictType });
            draft.Set(request.TargetHex);

            var cost = ResolveCost(request.DistrictType);
            draft.Set(new ActionAPCostComponent { Value = cost.ApPrice });
            draft.Set(BuildResourcePrice(cost));
        }

        private DistrictBuildCostConfig ResolveCost(DistrictType districtType)
        {
            if (!_world.Has<DistrictBuildCostsConfigComponent>())
                throw new InvalidOperationException("DistrictBuildCosts catalogue is not loaded.");

            foreach (var config in _world.Get<DistrictBuildCostsConfigComponent>().Value.Districts)
                if (config.DistrictType == districtType)
                    return config;

            throw new InvalidOperationException($"No DistrictBuildCost entry for district type {districtType}.");
        }

        private static ActionResourcePriceComponent BuildResourcePrice(DistrictBuildCostConfig cost)
        {
            var prices = cost.DistrictPrices;
            if (prices.Count > ActionResourcePriceComponent.Capacity)
                throw new InvalidOperationException(
                    $"District {cost.DistrictType} lists {prices.Count} resource prices; max is {ActionResourcePriceComponent.Capacity}.");

            var price = new ActionResourcePriceComponent();
            for (var index = 0; index < prices.Count; index++)
                price[index] = prices[index];
            price.Count = prices.Count;
            return price;
        }

        public override void Dispose()
        {
            _drafts.Dispose();
            base.Dispose();
        }
    }
}
