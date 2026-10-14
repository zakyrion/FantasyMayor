using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.UI.Archetypes;
using Presentation.UI.MainHud.ResourceBar.Components;
using Presentation.UI.MainHud.ResourceBar.Configs;
using Presentation.UI.MainHud.ResourceBar.Views;
using Presentation.UI.MainHud.Systems;

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
        private readonly EntityStorages _storages;
        private readonly Archetype _archetype;

        public override int Priority => SystemPriorities.SubSystems.MainHudSpawn.ResourceBar;

        public ResourceBarSpawnSubSystem(EntityStorages storages) : base(storages)
        {
            _storages = storages;
            _archetype = PresentationUIArchetypes.ResourceBar(storages.World);
        }

        public override UniTask Update(CancellationToken cancellationToken)
        {
            var mainUi = ReadMainHudRoot();

            var view = mainUi.GetComponentInChildren<ResourceBarView>(true);
            if (view == null)
                throw new InvalidOperationException(
                    "ResourceBarSpawnSubSystem: ResourceBarView is missing from the Main UI prefab.");

            view.Build(_storages.Get<InventoryResourceIconConfig>().Entries);
            view.Hide();

            var entity = _archetype.CreateEntity();
            entity.AddComponent(new ResourceBarViewComponent(view));

            return UniTask.CompletedTask;
        }
    }
}
