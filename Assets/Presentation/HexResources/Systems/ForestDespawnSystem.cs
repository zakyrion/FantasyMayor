using System;
using Domains.Map.Hex.Components;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Archetypes;
using Presentation.HexResources.Components;
using Presentation.HexResources.Events;
using Unity.Collections;
using Object = UnityEngine.Object;

namespace Presentation.HexResources.Systems
{
    /// <summary>
    ///     Reactive runtime forest remover. Anchored on the one-frame <see cref="ForestHexRemovedEvent" />
    ///     pulse: on its presence it reconciles state — every forest-view hex whose hex no longer carries a
    ///     forest resource has its tree entities (and GameObjects) destroyed. Works with current world state,
    ///     not transitive deltas: it builds the set of currently-forested hexes and drops views outside it,
    ///     so it is idempotent. The green ground paint is intentionally left in place (a chopped forest leaves
    ///     vegetated ground, not bare terrain — append-only paint is never reverted). No emitter raises this
    ///     pulse yet (future gameplay: chopping).
    /// </summary>
    [UsedImplicitly]
    public sealed class ForestDespawnSystem : UpdatedSystem
    {
        // HexResource table indexed by its discriminator value -> the Forest bucket is the current truth.
        private readonly ComponentIndex<HexResourceComponent, HexResourceType> _resourcesByType;

        private readonly Archetype _forestViews;
        private readonly EntityStorages _storages;

        public override int Priority => SystemPriorities.RuntimeTick.ForestDespawn;

        public ForestDespawnSystem(EntityStorages storages)
            : base(storages.World, EventArchetypes.Of<ForestHexRemovedEvent>(storages.World))
        {
            _storages = storages;
            _resourcesByType = storages.World.ComponentIndex<HexResourceComponent, HexResourceType>();
            _forestViews = PresentationArchetypes.ForestView(storages.World);
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!EcsEventExtensions.IsRipe(pulse))
                return;

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
