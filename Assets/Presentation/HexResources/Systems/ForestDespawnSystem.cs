using System;
using Domains.Map.Hex.Components;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.Boot.Core;
using Presentation.Archetypes;
using Presentation.HexResources.Components;
using Presentation.HexResources.Events;
using Unity.Collections;
using Object = UnityEngine.Object;

namespace Presentation.HexResources.Systems
{
    /// <summary>
    ///     Reactive runtime forest remover. Reads the <see cref="ForestHexRemovedEvent" /> log event: on every
    ///     event it reconciles state — every forest-view hex whose hex no longer carries a forest resource has
    ///     its tree entities (and GameObjects) destroyed. Works with current world state, not transitive deltas:
    ///     it builds the set of currently-forested hexes and drops views outside it, so it is idempotent. The
    ///     green ground paint is intentionally left in place (a chopped forest leaves vegetated ground, not bare
    ///     terrain — append-only paint is never reverted). No producer raises this event yet (future gameplay:
    ///     chopping) — a dormant consumer (event/dormant-consumer).
    /// </summary>
    [UsedImplicitly]
    public sealed class ForestDespawnSystem : IUpdatedSystem
    {
        // HexResource table indexed by its discriminator value -> the Forest bucket is the current truth.
        private readonly ComponentIndex<HexResourceComponent, HexResourceType> _resourcesByType;

        private readonly Archetype _forestViews;
        private readonly EntityStorages _storages;
        private readonly EventReader<ForestHexRemovedEvent> _forestHexRemovals;

        public AppState AppState { get; }
        public int Priority => SystemPriorities.RuntimeTick.ForestDespawn;

        public ForestDespawnSystem(AppState appState, EntityStorages storages, EventReader<ForestHexRemovedEvent> forestHexRemovals)
        {
            AppState = appState;
            _storages = storages;
            _forestHexRemovals = forestHexRemovals;
            _resourcesByType = storages.World.ComponentIndex<HexResourceComponent, HexResourceType>();
            _forestViews = PresentationArchetypes.ForestView(storages.World);
        }

        public void Update(GameState state)
        {
            while (_forestHexRemovals.TryRead(out _))
                DespawnStaleForests();
        }

        // Reconciliation is global over current state — the event's payload itself is ignored.
        private void DespawnStaleForests()
        {
            var forestResources = _resourcesByType[HexResourceType.Forest];
            var forestHexes = new NativeHashSet<HexCoord>(Math.Max(1, forestResources.Count), Allocator.Temp);
            var staleViewIds = new NativeList<int>(Math.Max(1, _forestViews.Count), Allocator.Temp);

            try
            {
                foreach (var resource in forestResources)
                    forestHexes.Add(resource.GetComponent<HexIdFKComponent>().Coords);

                // Snapshot only ForestView ids before their deletion mutates the archetype.
                foreach (var view in _forestViews.Entities)
                {
                    var coords = view.GetComponent<HexIdFKComponent>().Coords;
                    if (!forestHexes.Contains(coords))
                        staleViewIds.Add(view.Id);
                }

                for (var i = 0; i < staleViewIds.Length; i++)
                    if (_storages.World.TryGetEntityById(staleViewIds[i], out var view))
                        DestroyView(view);
            }
            finally
            {
                staleViewIds.Dispose();
                forestHexes.Dispose();
            }
        }

        private void DestroyView(Entity entity)
        {
            var view = entity.GetComponent<ForestViewComponent>().View;
            if (view != null)
                Object.Destroy(view.gameObject);

            entity.DeleteEntity();
        }
    }
}
