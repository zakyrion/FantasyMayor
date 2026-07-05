using System;
using DefaultEcs;
using Domains.Actors.Components;
using Domains.Actors.Data;
using Domains.Actors.Mayor.Components;
using Domains.Economy.Resource.Tags;
using JetBrains.Annotations;

namespace Domains.Actions.ResourceSpend.Systems
{
    // Mayor resource pool AND the game's sole Action-Point pool. Resource stacks keyed by MayorIdComponent (FK).
    // AP is always paid here, regardless of who pays the resources: TryGetActionPointStacks resolves the single
    // mayor's stacks (the AP stack itself is picked out by ResourceType.ActionPoint inside ResourceLedger).
    [UsedImplicitly]
    public sealed class MayorResourceSpendSubSystem : ResourceSpendSubSystem
    {
        private readonly EntityMultiMap<MayorIdComponent> _stacksByMayor;
        private readonly EntitySet _mayors;

        public MayorResourceSpendSubSystem(World world) : base(world)
        {
            _stacksByMayor = world.GetEntities().With<ResourceTag>().With<MayorIdComponent>().AsMultiMap<MayorIdComponent>();
            // The mayor actor row: MayorIdComponent (PK) + ActorTypeComponent (discriminator). Resource rows carry
            // MayorIdComponent as an FK but no ActorTypeComponent, so this isolates the actor from its stacks.
            _mayors = world.GetEntities().With<MayorIdComponent>().With<ActorTypeComponent>().AsSet();
        }

        public override ActorType Owner => ActorType.Mayor;
        public override bool HandlesActionPoints => true;

        public override bool TryGetStacks(int ownerId, out ReadOnlySpan<Entity> stacks) =>
            _stacksByMayor.TryGetEntities(new MayorIdComponent { Value = ownerId }, out stacks);

        public override bool TryGetActionPointStacks(out ReadOnlySpan<Entity> stacks)
        {
            var mayors = _mayors.GetEntities();
            if (mayors.Length != 1)
                throw new InvalidOperationException(
                    $"Expected exactly one mayor for Action-Point spend, found {mayors.Length}.");

            return TryGetStacks(mayors[0].Get<MayorIdComponent>().Value, out stacks);
        }

        public override void Dispose()
        {
            _stacksByMayor.Dispose();
            _mayors.Dispose();
            base.Dispose();
        }
    }
}
