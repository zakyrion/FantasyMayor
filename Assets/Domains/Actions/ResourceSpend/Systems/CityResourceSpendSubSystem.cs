using System;
using DefaultEcs;
using Domains.Actors.City.Components;
using Domains.Actors.Data;
using Domains.Economy.Resource.Tags;
using JetBrains.Annotations;

namespace Domains.Actions.ResourceSpend.Systems
{
    // City resource pool: resource stacks keyed by CityIdComponent (FK) + ResourceTag (discriminator). The city
    // owns no Action-Point pool (only the mayor does), so it does not override the AP hooks.
    [UsedImplicitly]
    public sealed class CityResourceSpendSubSystem : ResourceSpendSubSystem
    {
        private readonly EntityMultiMap<CityIdComponent> _stacksByCity;

        public CityResourceSpendSubSystem(World world) : base(world) =>
            _stacksByCity = world.GetEntities().With<ResourceTag>().With<CityIdComponent>().AsMultiMap<CityIdComponent>();

        public override ActorType Owner => ActorType.City;

        public override bool TryGetStacks(int ownerId, out ReadOnlySpan<Entity> stacks) =>
            _stacksByCity.TryGetEntities(new CityIdComponent { Value = ownerId }, out stacks);

        public override void Dispose()
        {
            _stacksByCity.Dispose();
            base.Dispose();
        }
    }
}
