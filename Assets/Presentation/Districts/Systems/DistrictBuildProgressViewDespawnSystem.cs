using EcsExtensions;
using Friflo.Engine.ECS;
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
        private readonly ArchetypeQuery _districts;

        // Progress-view entities indexed by the hex FK -> candidates for despawn once their hex stops building.
        private readonly ComponentIndex<HexIdFKComponent, HexCoord> _viewsByHex;

        private readonly EntityStore _world;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildProgressViewDespawn;

        public DistrictBuildProgressViewDespawnSystem(EntityStore world)
            : base(world.Query<DistrictTableChangedEvent>())
        {
            _world = world;
            _districts = world.Query<HexIdFKComponent, DistrictBuildStateComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<DistrictTag>());
            _viewsByHex = world.ComponentIndex<HexIdFKComponent, HexCoord>();
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!EcsEventExtensions.IsRipe(pulse))
                return;

            var plannedHexes = new NativeHashSet<HexCoord>(64, Allocator.Temp);
            foreach (var district in _districts.Entities)
                if (district.GetComponent<DistrictBuildStateComponent>().Value == DistrictBuildState.Planned)
                    plannedHexes.Add(district.GetComponent<HexIdFKComponent>().Coords);

            // Snapshot ids first, not entities: Friflo's Entity carries a store reference (not unmanaged), and
            // deleting mid-enumeration of the index throws StructuralChangeException.
            var staleViewIds = new NativeList<int>(8, Allocator.Temp);
            foreach (var coords in _viewsByHex.Values)
            {
                if (plannedHexes.Contains(coords))
                    continue;

                foreach (var view in _viewsByHex[coords])
                    staleViewIds.Add(view.Id);
            }

            for (var i = 0; i < staleViewIds.Length; i++)
                if (_world.TryGetEntityById(staleViewIds[i], out var view))
                    DestroyView(view);

            staleViewIds.Dispose();
            plannedHexes.Dispose();
        }

        private void DestroyView(Entity entity)
        {
            var view = entity.GetComponent<DistrictBuildProgressViewComponent>().View;
            if (view != null)
                Object.Destroy(view.gameObject);

            entity.DeleteEntity();
        }
    }
}
