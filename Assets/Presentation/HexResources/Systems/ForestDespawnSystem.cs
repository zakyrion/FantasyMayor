using System;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using Presentation.HexResources.Components;
using Presentation.HexResources.Events;
using Unity.Collections;
using Object = UnityEngine.Object;
using Presentation.HexResources.Tags;
using Domains.Map.HexResources.Tags;

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

        // ResourceView (forest) table indexed by the hex FK -> N tree entities per coordinate.
        private readonly ComponentIndex<HexIdFKComponent, HexCoord> _forestViewsByHex;

        private readonly EntityStore _world;

        public override int Priority => SystemPriorities.RuntimeTick.ForestDespawn;

        public ForestDespawnSystem(EntityStore world)
            : base(world.Query<ForestHexRemovedEvent>())
        {
            _world = world;
            _resourcesByType = world.ComponentIndex<HexResourceComponent, HexResourceType>();
            _forestViewsByHex = world.ComponentIndex<HexIdFKComponent, HexCoord>();
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!EcsEventExtensions.IsRipe(pulse))
                return;

            var forestHexes = new NativeHashSet<HexCoord>(64, Allocator.Temp);
            foreach (var resource in _resourcesByType[HexResourceType.Forest])
                forestHexes.Add(resource.GetComponent<HexIdFKComponent>().Coords);

            // Snapshot ids first, not entities: Friflo's Entity carries a store reference (not unmanaged), and
            // deleting mid-enumeration of the index throws StructuralChangeException.
            var staleViewIds = new NativeList<int>(8, Allocator.Temp);
            foreach (var coords in _forestViewsByHex.Values)
            {
                if (forestHexes.Contains(coords))
                    continue;

                foreach (var view in _forestViewsByHex[coords])
                    staleViewIds.Add(view.Id);
            }

            for (var i = 0; i < staleViewIds.Length; i++)
                if (_world.TryGetEntityById(staleViewIds[i], out var view))
                    DestroyView(view);

            staleViewIds.Dispose();
            forestHexes.Dispose();
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
