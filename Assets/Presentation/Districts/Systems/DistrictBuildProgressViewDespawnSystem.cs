using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
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
    ///     Reactive runtime construction-progress remover. Anchored on the one-frame <see cref="DistrictBuiltEvent" />
    ///     pulse (the same pulse <c>DistrictViewSpawnSystem</c> reacts to — <c>BuildDistrictCompletionSystem</c>
    ///     disposes the in-progress entity BEFORE raising it): on its presence it reconciles state — every
    ///     progress-view hex whose hex no longer carries a <c>BuildDistrictInProgressTag</c> entity has its view
    ///     entity (and GameObject) destroyed. Works with current world state, not transitive deltas, so it is
    ///     idempotent. Cancel (R5) teardown is a future emitter of the same reconcile, not built yet.
    /// </summary>
    [UsedImplicitly]
    public sealed class DistrictBuildProgressViewDespawnSystem : UpdatedSystem
    {
        // In-progress build entities indexed by the hex FK -> the currently-building set is the current truth.
        private readonly EntityMultiMap<HexIdComponent> _inProgressByHex;

        // Progress-view entities indexed by the hex FK -> candidates for despawn once their hex stops building.
        private readonly EntityMultiMap<HexIdComponent> _viewsByHex;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildProgressViewDespawn;

        public DistrictBuildProgressViewDespawnSystem(World world)
            : base(world.GetEntities()
                .With<DistrictBuiltEvent>()
                .AsSet())
        {
            _inProgressByHex = world.GetEntities()
                .With<BuildDistrictInProgressTag>()
                .With<HexIdComponent>()
                .AsMultiMap<HexIdComponent>();

            _viewsByHex = world.GetEntities()
                .With<HexIdComponent>()
                .With<DistrictBuildProgressViewComponent>().With<DistrictBuildProgressViewTag>()
                .AsMultiMap<HexIdComponent>();
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            var buildingHexes = new NativeHashSet<HexCoord>(64, Allocator.Temp);
            foreach (var hexId in _inProgressByHex.Keys)
                buildingHexes.Add(hexId.Coords);

            // Snapshot stale views before destroying: DestroyView disposes entities, which would mutate the
            // view map mid-enumeration.
            var staleViews = new NativeList<Entity>(8, Allocator.Temp);
            foreach (var hexId in _viewsByHex.Keys)
            {
                if (buildingHexes.Contains(hexId.Coords))
                    continue;

                if (_viewsByHex.TryGetEntities(hexId, out var views))
                    foreach (ref readonly var view in views)
                        staleViews.Add(view);
            }

            for (var i = 0; i < staleViews.Length; i++)
                DestroyView(staleViews[i]);

            staleViews.Dispose();
            buildingHexes.Dispose();
        }

        public override void Dispose()
        {
            _inProgressByHex.Dispose();
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
