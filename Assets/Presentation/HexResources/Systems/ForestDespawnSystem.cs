using System;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
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
        private readonly EntityMultiMap<HexResourceComponent> _resourcesByType;

        // ResourceView (forest) table indexed by the hex FK -> N tree entities per coordinate.
        private readonly EntityMultiMap<HexIdComponent> _forestViewsByHex;

        public override int Priority => SystemPriorities.RuntimeTick.ForestDespawn;

        public ForestDespawnSystem(World world)
            : base(world.GetEntities()
                .With<ForestHexRemovedEvent>()
                .AsSet())
        {
            _resourcesByType = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexResourceComponent>()
                .AsMultiMap<HexResourceComponent>();

            _forestViewsByHex = world.GetEntities()
                .With<HexIdComponent>()
                .With<ForestViewComponent>()
                .AsMultiMap<HexIdComponent>();
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            var forestKey = new HexResourceComponent { Type = HexResourceType.Forest };

            var forestHexes = new NativeHashSet<HexCoord>(64, Allocator.Temp);
            if (_resourcesByType.TryGetEntities(forestKey, out var forestResources))
                foreach (ref readonly var resource in forestResources)
                    forestHexes.Add(resource.Get<HexIdComponent>().Coords);

            // Snapshot stale views before destroying: DestroyView disposes entities, which would mutate the
            // view map mid-enumeration.
            var staleViews = new NativeList<Entity>(8, Allocator.Temp);
            foreach (var hexId in _forestViewsByHex.Keys)
            {
                if (forestHexes.Contains(hexId.Coords))
                    continue;

                if (_forestViewsByHex.TryGetEntities(hexId, out var views))
                    foreach (ref readonly var view in views)
                        staleViews.Add(view);
            }

            for (var i = 0; i < staleViews.Length; i++)
                DestroyView(staleViews[i]);

            staleViews.Dispose();
            forestHexes.Dispose();
        }

        public override void Dispose()
        {
            _resourcesByType.Dispose();
            _forestViewsByHex.Dispose();
            base.Dispose();
        }

        private void DestroyView(Entity entity)
        {
            var view = entity.Get<ForestViewComponent>().View;
            if (view != null)
                Object.Destroy(view.gameObject);

            if (entity.IsAlive)
                entity.Dispose();
        }
    }
}
