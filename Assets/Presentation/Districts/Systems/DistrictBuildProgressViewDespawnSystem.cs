using Domains.Economy.Archetypes;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
using Domains.Map.Hex.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.Boot.Core;
using Presentation.Archetypes;
using Presentation.Districts.Components;
using Unity.Collections;
using Object = UnityEngine.Object;

namespace Presentation.Districts.Systems
{
    /// <summary>
    ///     Reactive runtime construction-progress remover. Reads the <see cref="DistrictTableChangedEvent" /> log
    ///     event (folds the former dual <c>DistrictBuiltEvent</c>/<c>BuildDistrictCancelEvent</c> trigger,
    ///     FLOW_DISTRICT_BUILD unification, 2026-07-17): on every event it reconciles state — every progress-view
    ///     hex that no longer has a District row staged <c>DistrictBuildState.Planned</c> (completion flipped it
    ///     to <c>Built</c>, or cancel disposed it) has its view entity (and GameObject) destroyed. Works with
    ///     current world state, not transitive deltas, so it is idempotent regardless of which stage transition
    ///     fired this event.
    /// </summary>
    [UsedImplicitly]
    public sealed class DistrictBuildProgressViewDespawnSystem : IUpdatedSystem
    {
        // District rows: one per hex, carrying the hex FK and its build stage.
        private readonly Archetype _districts;

        // Progress-view entities -> candidates for despawn once their hex stops building. HexIdFKComponent is
        // shared by every hex-anchored entity kind (the District row itself included) — a bare ComponentIndex
        // over it would offer the District row up for despawn too (NRE: no DistrictBuildProgressViewComponent
        // on it) — scope the query to the view archetype.
        private readonly Archetype _views;

        private readonly EntityStorages _storages;
        private readonly EventReader<DistrictTableChangedEvent> _districtTableChanges;

        public AppState AppState { get; }
        public int Priority => SystemPriorities.RuntimeTick.DistrictBuildProgressViewDespawn;

        public DistrictBuildProgressViewDespawnSystem(AppState appState, EntityStorages storages, EventReader<DistrictTableChangedEvent> districtTableChanges)
        {
            AppState = appState;
            _storages = storages;
            _districtTableChanges = districtTableChanges;
            _districts = EconomyArchetypes.District(storages.World);
            _views = PresentationArchetypes.DistrictBuildProgressView(storages.World);
        }

        public void Update(GameState state)
        {
            while (_districtTableChanges.TryRead(out _))
                DespawnStaleProgressViews();
        }

        // Reconciliation is global over current state — the event's payload itself is ignored.
        private void DespawnStaleProgressViews()
        {
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
