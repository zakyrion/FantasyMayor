using System;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.UI.MainHud.ResourceBar.Components;
using Presentation.UI.MainHud.ResourceBar.Views;
using Presentation.UI.MainHud.Systems;
using Presentation.UI.Tags;
using UnityEngine;
using EcsExtensions;

namespace Presentation.UI.MainHud.ResourceBar.Systems
{
    /// <summary>
    ///     Main UI spawn subsystem: resolves the resource-strip view from the shared Main UI instance, builds its
    ///     columns from the loaded icon config, and publishes ResourceBarViewComponent. Instantiates nothing — the
    ///     orchestrator owns the Main UI handle. Leaves the strip hidden; ResourceBarSystem reveals it in Gameplay.
    /// </summary>
    [UsedImplicitly]
    internal sealed class ResourceBarSpawnSubSystem : MainHudSpawnSubSystem
    {
        private readonly EntityStore _world;

        public override int Priority => SystemPriorities.SubSystems.MainHudSpawn.ResourceBar;

        public ResourceBarSpawnSubSystem(EntityStore world)
        {
            _world = world;
        }

        public override void Prepare(GameObject mainUi)
        {
            var view = mainUi.GetComponentInChildren<ResourceBarView>(true);
            if (view == null)
                throw new InvalidOperationException(
                    "ResourceBarSpawnSubSystem: ResourceBarView is missing from the Main UI prefab.");

            if (!_world.HasWorldComponent<InventoryResourceIconConfigComponent>())
                throw new InvalidOperationException(
                    "ResourceBarSpawnSubSystem: InventoryResourceIconConfigComponent missing — " +
                    "InventoryResourceIconConfigLoaderSystem must run at ConfigLoadStep first.");

            view.Build(_world.GetWorldComponent<InventoryResourceIconConfigComponent>().Value.Entries);
            view.Hide();

            var entity = _world.CreateEntity();
            entity.AddComponent(new ResourceBarViewComponent(view));
            entity.AddTag<UITag>();
        }
    }
}
