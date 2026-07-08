using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actors.City.Components;
using Domains.Actors.Components;
using Domains.Actors.Mayor.Components;
using Domains.Economy.Resource.Components;
using JetBrains.Annotations;
using Presentation.UI.ResourceBar.Components;
using Presentation.UI.ResourceBar.Views;
using Domains.Actors.City.Tags;
using Domains.Actors.Mayor.Tags;
using Presentation.UI.Tags;

namespace Presentation.UI.ResourceBar.Systems
{
    /// <summary>
    ///     Fills the top-bar resource strip each Gameplay frame with the City and Mayor inventory amounts, and
    ///     reveals it (the strip spawns hidden, like the bottom panel). Anchored on the ResourceBarViewComponent
    ///     singleton so it ticks once per frame, mirroring EndTurnViewSystem.
    ///     Per-frame justification (Reactive-by-default override): there is no ResourcesChanged pulse yet — a
    ///     per-frame read keeps the strip live the moment economy actions begin mutating stacks, at trivial cost
    ///     (2 owners × N columns), without a god-system. Replace with a reactive consumer once such a pulse lands.
    /// </summary>
    [UsedImplicitly]
    public sealed class ResourceBarSystem : UpdatedSystem
    {
        // Actor rows (Table Rule): id PK + ActorTypeComponent discriminator — never a bare key.
        private readonly EntitySet _mayorActor;
        private readonly EntitySet _cityActor;
        // FK 1:N indexes (Table Rule): owner id is a PK on the actor AND a FK on the resource stack.
        private readonly EntityMultiMap<CityIdComponent> _cityResources;
        private readonly EntityMultiMap<MayorIdComponent> _mayorResources;

        public override int Priority => SystemPriorities.RuntimeTick.ResourceBar;

        public ResourceBarSystem(World world)
            : base(world.GetEntities().With<ResourceBarViewComponent>().With<UITag>().AsSet())
        {
            _mayorActor = world.GetEntities().With<MayorIdComponent>().With<MayorTag>().With<ActorTypeComponent>().AsSet();
            _cityActor = world.GetEntities().With<CityIdComponent>().With<CityTag>().With<ActorTypeComponent>().AsSet();
            _cityResources = world.GetEntities()
                .With<CityIdComponent>().With<CityTag>().With<CityResourceTag>().AsMultiMap<CityIdComponent>();
            _mayorResources = world.GetEntities()
                .With<MayorIdComponent>().With<MayorTag>().With<MayorResourceTag>().AsMultiMap<MayorIdComponent>();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            var view = entity.Get<ResourceBarViewComponent>().View;
            if (view == null)
                return;

            if (_mayorActor.Count > 0)
            {
                FillMayor(view, _mayorActor.GetEntities()[0].Get<MayorIdComponent>());
            }

            if (_cityActor.Count > 0)
            {
                FillCity(view, _cityActor.GetEntities()[0].Get<CityIdComponent>());
            }

            view.Show();
        }

        public override void Dispose()
        {
            _mayorActor.Dispose();
            _cityActor.Dispose();
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
