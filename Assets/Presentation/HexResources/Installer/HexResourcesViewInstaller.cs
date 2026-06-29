using DefaultECSExtensions;
using Modules.Boot.Core;
using Presentation.HexResources.Systems;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Presentation.HexResources.Installer
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
                .As<HexResourcesViewSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            // Reactive runtime forest systems are event-driven (idle until a pulse). Concrete registration:
            // Boot wires them into the Gameplay state by hand.
            builder.Register<ForestSpawnSystem>(Lifetime.Singleton)
                .As<ForestSpawnSystem>();
            builder.Register<ForestDespawnSystem>(Lifetime.Singleton)
                .As<ForestDespawnSystem>();

            builder.Register<ForestHexResourceViewSubSystem>(Lifetime.Singleton)
                .As<ForestHexResourceViewSubSystem, HexResourcesViewSubSystem>();
            builder.Register<ClayHexResourceViewSubSystem>(Lifetime.Singleton)
                .As<ClayHexResourceViewSubSystem, HexResourcesViewSubSystem>();
            builder.Register<FishHexResourceViewSubSystem>(Lifetime.Singleton)
                .As<FishHexResourceViewSubSystem, HexResourcesViewSubSystem>();
        }
    }
}
