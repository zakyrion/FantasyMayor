using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actors.City.Components;
using Domains.Actors.Components;
using Domains.Actors.Data;
using Domains.Actors.Mayor.Components;
using Domains.Economy.Resource.Components;
using Domains.Economy.Resource.Tags;
using JetBrains.Annotations;
using Modules.MainUI.ResourceBar.Components;
using Modules.MainUI.ResourceBar.Views;

namespace Modules.MainUI.ResourceBar.Systems
{
    /// <summary>
    ///     Fills the top-bar resource strip each Gameplay frame with the City and Mayor inventory amounts, and
    ///     reveals it (the strip spawns hidden, like the bottom panel). Anchored on the ResourceBarViewComponent
    ///     singleton so it ticks once per frame, mirroring EndTurnSystem.
    ///     Per-frame justification (Reactive-by-default override): there is no ResourcesChanged pulse yet — a
    ///     per-frame read keeps the strip live the moment economy actions begin mutating stacks, at trivial cost
    ///     (2 owners × N columns), without a god-system. Replace with a reactive consumer once such a pulse lands.
    /// </summary>
    [UsedImplicitly]
    public sealed class ResourceBarSystem : UpdatedSystem
    {
        private const int ExecutionPriority = 562;

        // FK 1:N indexes (Table Rule): owner id is a PK on the actor AND a FK on the resource stack.
        private readonly EntityMultiMap<ActorTypeComponent> _actors;
        private readonly EntityMultiMap<CityIdComponent> _cityResources;
        private readonly EntityMultiMap<MayorIdComponent> _mayorResources;

        public override int Priority => ExecutionPriority;

        public ResourceBarSystem(World world)
            : base(world.GetEntities().With<ResourceBarViewComponent>().AsSet())
        {
            _actors = world.GetEntities().With<ActorTypeComponent>().AsMultiMap<ActorTypeComponent>();
            _cityResources = world.GetEntities()
                .With<CityIdComponent>().With<ResourceTag>().AsMultiMap<CityIdComponent>();
            _mayorResources = world.GetEntities()
                .With<MayorIdComponent>().With<ResourceTag>().AsMultiMap<MayorIdComponent>();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            var view = entity.Get<ResourceBarViewComponent>().View;
            if (view == null)
                return;

            if (_actors.TryGetEntities(new ActorTypeComponent { Type = ActorType.Mayor }, out var mayor))
            {
                FillMayor(view, mayor[0].Get<MayorIdComponent>());
            }

            if (_actors.TryGetEntities(new ActorTypeComponent { Type = ActorType.City }, out var city))
            {
                FillCity(view, city[0].Get<CityIdComponent>());
            }

            view.Show();
        }

        public override void Dispose()
        {
            _actors.Dispose();
            _cityResources.Dispose();
            _mayorResources.Dispose();
            base.Dispose();
        }

        private void FillCity(ResourceBarView view, CityIdComponent owner)
        {
            if (!_cityResources.TryGetEntities(owner, out var stacks))
                return;

            foreach (var stack in stacks)
            {
                var resource = stack.Get<ResourceComponent>();
                view.SetCityAmount(resource.Type, resource.Amount);
            }
        }

        private void FillMayor(ResourceBarView view, MayorIdComponent owner)
        {
            if (!_mayorResources.TryGetEntities(owner, out var stacks))
                return;

            foreach (var stack in stacks)
            {
                var resource = stack.Get<ResourceComponent>();
                view.SetMayorAmount(resource.Type, resource.Amount);
            }
        }
    }
}
