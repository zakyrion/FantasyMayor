using Domains.Actors.Archetypes;
using Domains.Actors.City.Components;
using Domains.Actors.Mayor.Components;
using Domains.Economy.Resource.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.UI.Archetypes;
using Presentation.UI.MainHud.ResourceBar.Components;
using Presentation.UI.MainHud.ResourceBar.Views;

namespace Presentation.UI.MainHud.ResourceBar.Systems
{
    /// <summary>
    ///     Fills the top-bar resource strip each Gameplay frame with the City and Mayor inventory amounts, and
    ///     reveals it (the strip spawns hidden, like the bottom panel). Anchored on the ResourceBarViewComponent
    ///     singleton so it ticks once per frame, mirroring TurnPanelViewSystem.
    ///     Per-frame justification (Reactive-by-default override): there is no ResourcesChanged pulse yet — a
    ///     per-frame read keeps the strip live the moment economy actions begin mutating stacks, at trivial cost
    ///     (2 owners × N columns), without a god-system. Replace with a reactive consumer once such a pulse lands.
    /// </summary>
    [UsedImplicitly]
    public sealed class ResourceBarSystem : UpdatedSystem
    {
        // Actor rows (Table Rule): id PK + ActorTypeComponent discriminator — never a bare key.
        private readonly Archetype _mayorActor;
        private readonly Archetype _cityActor;
        // FK 1:N indexes (Table Rule): owner id is a PK on the actor AND a FK on the resource stack.
        private readonly ComponentIndex<CityIdFKComponent, int> _cityResources;
        private readonly ComponentIndex<MayorIdFKComponent, int> _mayorResources;

        public override int Priority => SystemPriorities.RuntimeTick.ResourceBar;

        public ResourceBarSystem(EntityStorages storages)
            : base(storages.World, PresentationUIArchetypes.ResourceBar(storages.World))
        {
            _mayorActor = ActorsArchetypes.Mayor(storages.World);
            _cityActor = ActorsArchetypes.City(storages.World);
            _cityResources = storages.World.ComponentIndex<CityIdFKComponent, int>();
            _mayorResources = storages.World.ComponentIndex<MayorIdFKComponent, int>();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            var view = entity.GetComponent<ResourceBarViewComponent>().View;
            if (view == null)
                return;

            if (_mayorActor.TryGetFirst(out var mayor))
                FillMayor(view, mayor.GetComponent<MayorIdComponent>());

            if (_cityActor.TryGetFirst(out var city))
                FillCity(view, city.GetComponent<CityIdComponent>());

            view.Show();
        }

        private void FillCity(ResourceBarView view, CityIdComponent owner)
        {
            foreach (var stack in _cityResources[owner.Value])
            {
                var resource = stack.GetComponent<ResourceComponent>();
                view.SetCityAmount(resource.Type, resource.Amount);
            }
        }

        private void FillMayor(ResourceBarView view, MayorIdComponent owner)
        {
            foreach (var stack in _mayorResources[owner.Value])
            {
                var resource = stack.GetComponent<ResourceComponent>();
                view.SetMayorAmount(resource.Type, resource.Amount);
            }
        }
    }
}
