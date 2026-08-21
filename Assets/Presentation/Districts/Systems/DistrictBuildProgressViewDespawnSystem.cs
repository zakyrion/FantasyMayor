using Domains.Economy.Archetypes;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
using Domains.Map.Hex.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Archetypes;
using Presentation.Districts.Components;
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
        private readonly Archetype _districts;

        // Progress-view entities -> candidates for despawn once their hex stops building. HexIdFKComponent is
        // shared by every hex-anchored entity kind (the District row itself included) — a bare ComponentIndex
        // over it would offer the District row up for despawn too (NRE: no DistrictBuildProgressViewComponent
        // on it) — scope the query to the view archetype.
        private readonly Archetype _views;

        private readonly EntityStorages _storages;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildProgressViewDespawn;

        public DistrictBuildProgressViewDespawnSystem(EntityStorages storages)
            : base(storages.World, EventArchetypes.Of<DistrictTableChangedEvent>(storages.World))
        {
            _storages = storages;
            _districts = EconomyArchetypes.District(storages.World);
            _views = PresentationArchetypes.DistrictBuildProgressView(storages.World);
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
            // deleting mid-enumeration of the query throws StructuralChangeException.
            var staleViewIds = new NativeList<int>(8, Allocator.Temp);
            foreach (var view in _views.Entities)
            {
                var coords = view.GetComponent<HexIdFKComponent>().Coords;
                if (!plannedHexes.Contains(coords))
                    staleViewIds.Add(view.Id);
            }

            for (var i = 0; i < staleViewIds.Length; i++)
                if (_storages.World.TryGetEntityById(staleViewIds[i], out var view))
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
