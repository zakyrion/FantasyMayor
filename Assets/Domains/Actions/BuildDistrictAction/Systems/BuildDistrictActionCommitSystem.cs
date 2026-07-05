using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Actions.BuildDistrictAction.Tags;
using Domains.Actions.ResourceSpend.Systems;
using Domains.Actors.City.Components;
using Domains.Actors.Data;
using Domains.Actors.Mayor.Components;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.DistrictBuildCost.Components;
using Domains.Economy.Resource.Data;
using JetBrains.Annotations;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    // Reactive (PATTERN_REACTIVE_SYSTEM): on a commit-requested pulse, turns the drafted BuildDistrictAction into a
    // committed, counting-down build. Spends FIRST (atomic + fail-loud via ResourceSpender), so an unaffordable
    // commit throws before the draft is mutated; then stamps the payer owner FK, seeds the turn countdown from the
    // cost catalogue, and flips the draft to committed. Targets the single UNCOMMITTED draft. Runs after the
    // snapshot (563), before EventCleanupSystem. DORMANT — Boot wiring + the UI raise-site are a later slice.
    [UsedImplicitly]
    public sealed class BuildDistrictActionCommitSystem : UpdatedSystem
    {
        // After BuildDistrictActionSnapshotSystem (563) so the cost snapshot exists; before EventCleanupSystem.
        private const int ExecutionPriority = 565;

        private readonly World _world;
        private readonly ResourceSpender _resourceSpender;
        private readonly EntitySet _drafts;

        public override int Priority => ExecutionPriority;

        public BuildDistrictActionCommitSystem(World world, ResourceSpender resourceSpender)
            : base(world.GetEntities().With<BuildDistrictActionCommitRequestedEvent>().AsSet())
        {
            _world = world;
            _resourceSpender = resourceSpender;
            _drafts = world.GetEntities()
                .With<BuildDistrictActionTag>()
                .Without<BuildDistrictActionCommittedTag>()
                .AsSet();
        }

        protected override void Update(GameState state, in Entity pulse)
        {
            var request = pulse.Get<BuildDistrictActionCommitRequestedEvent>();

            var drafts = _drafts.GetEntities();
            if (drafts.Length == 0)
                throw new InvalidOperationException(
                    "BuildDistrictAction commit requested but no uncommitted draft exists — draft + snapshot must precede commit.");

            var draft = drafts[0];

            // Spend first: unaffordable/unroutable throws here, leaving the draft untouched.
            var actionPoints = draft.Get<ActionAPCostComponent>().Value;
            var priceSnapshot = draft.Get<DistrictBuildCostResourcePriceComponent>();
            Span<ResourceAmount> resourcePrices = stackalloc ResourceAmount[priceSnapshot.Count];
            for (var index = 0; index < priceSnapshot.Count; index++)
            {
                // ResourceCost → ResourceAmount: distinct value aliases, mapped by explicit field assignment
                // (no conversion operators) so the cost catalogue and the spend currency never silently mix.
                var cost = priceSnapshot[index];
                resourcePrices[index] = new ResourceAmount { Type = cost.Type, Amount = cost.Amount };
            }
            _resourceSpender.Spend(request.OwnerType, request.OwnerId, actionPoints, resourcePrices);

            // Commit: stamp the payer FK, seed the countdown, flip the discriminator to committed.
            AttachOwner(draft, request.OwnerType, request.OwnerId);
            draft.Set(new BuildDistrictActionTurnLeftComponent { Value = ResolveTurnsToBuild(draft.Get<DistrictTypeComponent>().Value) });
            draft.Set(new BuildDistrictActionCommittedTag());
        }

        private static void AttachOwner(Entity draft, ActorType ownerType, int ownerId)
        {
            switch (ownerType)
            {
                case ActorType.City:
                    draft.Set(new CityIdComponent { Value = ownerId });
                    break;
                case ActorType.Mayor:
                    draft.Set(new MayorIdComponent { Value = ownerId });
                    break;
                default:
                    throw new InvalidOperationException($"Cannot commit a build to owner type {ownerType}.");
            }
        }

        private int ResolveTurnsToBuild(DistrictType districtType)
        {
            if (!_world.Has<DistrictBuildCostsConfigComponent>())
                throw new InvalidOperationException("DistrictBuildCosts catalogue is not loaded.");

            foreach (var config in _world.Get<DistrictBuildCostsConfigComponent>().Value.Districts)
                if (config.DistrictType == districtType)
                    return config.TurnsToBuild;

            throw new InvalidOperationException($"No DistrictBuildCost entry for district type {districtType}.");
        }

        public override void Dispose()
        {
            _drafts.Dispose();
            base.Dispose();
        }
    }
}
