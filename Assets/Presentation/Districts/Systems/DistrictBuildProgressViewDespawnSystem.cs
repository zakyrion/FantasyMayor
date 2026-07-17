using DefaultEcs;
using DefaultECSExtensions;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
using Domains.Economy.District.Tags;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Districts.Components;
using Presentation.Districts.Tags;
using Unity.Collections;
using Object = UnityEngine.Object;

namespace Presentation.Districts.Systems
{
    /// <summary>
    ///     Reactive runtime construction-progress remover. Anchored on the one-frame
    ///     <see cref="DistrictTableChangedEvent" /> pulse (folds the former dual
    ///     <c>DistrictBuiltEvent</c>/<c>BuildDistrictCancelEvent</c> trigger, FLOW_DISTRICT_BUILD unification,
    ///     2026-07-17): on its presence it reconciles state — every progress-view hex that no longer has a
    ///     District row staged <c>DistrictBuildState.Planned</c> (completion flipped it to <c>Built</c>, or
    ///     cancel disposed it) has its view entity (and GameObject) destroyed. Works with current world state,
    ///     not transitive deltas, so it is idempotent regardless of which stage transition fired this tick.
    /// </summary>
    [UsedImplicitly]
    public sealed class DistrictBuildProgressViewDespawnSystem : UpdatedSystem
    {
        // District rows: one per hex, carrying the hex FK and its build stage.
        private readonly EntitySet _districts;

        // Progress-view entities indexed by the hex FK -> candidates for despawn once their hex stops building.
        private readonly EntityMultiMap<HexIdFKComponent> _viewsByHex;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildProgressViewDespawn;

        public DistrictBuildProgressViewDespawnSystem(World world)
            : base(world.GetEntities()
                .With<DistrictTableChangedEvent>()
                .AsSet())
        {
            _districts = world.GetEntities()
                .With<DistrictTag>()
                .With<HexIdFKComponent>()
                .With<DistrictBuildStateComponent>()
                .AsSet();

            _viewsByHex = world.GetEntities()
                .With<HexIdFKComponent>()
                .With<DistrictBuildProgressViewComponent>().With<DistrictBuildProgressViewTag>()
                .AsMultiMap<HexIdFKComponent>();
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            var plannedHexes = new NativeHashSet<HexCoord>(64, Allocator.Temp);
            foreach (var district in _districts.GetEntities())
                if (district.Get<DistrictBuildStateComponent>().Value == DistrictBuildState.Planned)
                    plannedHexes.Add(district.Get<HexIdFKComponent>().Coords);

            // Snapshot stale views before destroying: DestroyView disposes entities, which would mutate the
            // view map mid-enumeration.
            var staleViews = new NativeList<Entity>(8, Allocator.Temp);
            foreach (var hexId in _viewsByHex.Keys)
            {
                if (plannedHexes.Contains(hexId.Coords))
                    continue;

                if (_viewsByHex.TryGetEntities(hexId, out var views))
                    foreach (ref readonly var view in views)
                        staleViews.Add(view);
            }

            for (var i = 0; i < staleViews.Length; i++)
                DestroyView(staleViews[i]);

            staleViews.Dispose();
            plannedHexes.Dispose();
        }

        public override void Dispose()
        {
            _districts.Dispose();
            _viewsByHex.Dispose();
            base.Dispose();
        }

        private void DestroyView(Entity entity)
        {
            var view = entity.Get<DistrictBuildProgressViewComponent>().View;
            if (view != null)
                Object.Destroy(view.gameObject);

            if (entity.IsAlive)
                entity.Dispose();
        }
    }
}
