using System;
using DefaultEcs;
using JetBrains.Annotations;
using Presentation.UI.ResourceBar.Components;
using Presentation.UI.ResourceBar.Views;
using Presentation.UI.Systems;
using Presentation.UI.Tags;
using UnityEngine;
using DefaultECSExtensions;

namespace Presentation.UI.ResourceBar.Systems
{
    /// <summary>
    ///     Main UI spawn subsystem: resolves the resource-strip view from the shared Main UI instance, builds its
    ///     columns from the loaded icon config, and publishes ResourceBarViewComponent. Instantiates nothing — the
    ///     orchestrator owns the Main UI handle. Leaves the strip hidden; ResourceBarSystem reveals it in Gameplay.
    /// </summary>
    [UsedImplicitly]
    internal sealed class ResourceBarSpawnSubSystem : MainUISpawnSubSystem
    {
        private readonly World _world;

        public override int Priority => SystemPriorities.SubSystems.MainUiSpawn.ResourceBar;

        public ResourceBarSpawnSubSystem(World world)
        {
            _world = world;
        }

        public override void Prepare(GameObject mainUi)
        {
            var view = mainUi.GetComponentInChildren<ResourceBarView>(true);
            if (view == null)
                throw new InvalidOperationException(
                    "ResourceBarSpawnSubSystem: ResourceBarView is missing from the Main UI prefab.");

            if (!_world.Has<InventoryResourceIconConfigComponent>())
                throw new InvalidOperationException(
                    "ResourceBarSpawnSubSystem: InventoryResourceIconConfigComponent missing — " +
                    "InventoryResourceIconConfigLoaderSystem must run at ConfigLoadStep first.");

            view.Build(_world.Get<InventoryResourceIconConfigComponent>().Value.Entries);
            view.Hide();

            var entity = _world.CreateEntity();
            entity.Set(new ResourceBarViewComponent(view));
            entity.Set<UITag>();
        }
    }
}
