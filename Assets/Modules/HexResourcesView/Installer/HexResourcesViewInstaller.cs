using DefaultECSExtensions;
using Modules.Boot.Core;
using Modules.HexResourcesView.Systems;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Modules.HexResourcesView.Installer
{
    public sealed class HexResourcesViewInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<HexResourcesViewConfigLoaderSystem>(Lifetime.Singleton)
                .As<HexResourcesViewConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<ClayViewConfigLoaderSystem>(Lifetime.Singleton)
                .As<ClayViewConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            Debug.Log($"[skh] install {nameof(HexResourcesViewInstaller)}");
            builder.Register<HexResourcesViewSystem>(Lifetime.Singleton)
                .As<HexResourcesViewSystem, IPrioritizedUniTaskSystem<TerrainGenerationStep>>();

            // Forest view is reactive (per-frame Update), not a one-shot pipeline subsystem — see ForestViewSyncSystem.
            // Concrete registration: Boot wires it into the MapCreation and Gameplay states by hand.
            builder.Register<ForestViewSyncSystem>(Lifetime.Singleton)
                .As<ForestViewSyncSystem>();

            builder.Register<ClayResourceViewSubSystem>(Lifetime.Singleton)
                .As<ClayResourceViewSubSystem, HexResourcesViewSubSystem>();
            builder.Register<FishResourceViewSubSystem>(Lifetime.Singleton)
                .As<FishResourceViewSubSystem, HexResourcesViewSubSystem>();
        }
    }
}
