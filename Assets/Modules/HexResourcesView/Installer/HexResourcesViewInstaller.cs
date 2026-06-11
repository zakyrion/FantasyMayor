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

            // Reactive runtime forest systems are event-driven (idle until a pulse). Concrete registration:
            // Boot wires them into the Gameplay state by hand.
            builder.Register<ForestSpawnSystem>(Lifetime.Singleton)
                .As<ForestSpawnSystem>();
            builder.Register<ForestDespawnSystem>(Lifetime.Singleton)
                .As<ForestDespawnSystem>();

            builder.Register<ForestResourceViewSubSystem>(Lifetime.Singleton)
                .As<ForestResourceViewSubSystem, HexResourcesViewSubSystem>();
            builder.Register<ClayResourceViewSubSystem>(Lifetime.Singleton)
                .As<ClayResourceViewSubSystem, HexResourcesViewSubSystem>();
            builder.Register<FishResourceViewSubSystem>(Lifetime.Singleton)
                .As<FishResourceViewSubSystem, HexResourcesViewSubSystem>();
        }
    }
}
