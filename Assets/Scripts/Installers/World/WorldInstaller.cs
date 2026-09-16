using Friflo.Engine.ECS;
using EcsExtensions;
using Domains.Actions.Installer;
using Domains.Actors.Installer;
using Domains.Economy.Installer;
using Installers.Addressable;
using Presentation.Terrain.Installer;
using Modules.Boot.Core;
using Modules.Boot.Implementation;
using Modules.Boot.Implementation.States;
using Presentation.HexIcons.Installer;
using Domains.Map.HexResources.Installer;
using Presentation.HexResources.Installer;
using Presentation.Districts.Installer;
using Presentation.UI.Installer;
using Modules.MainCanvas.Core;
using Modules.MainCanvas.Implementation;
using Modules.Cameras.Components;
using Domains.Map.Pathfinding.Installer;
using Domains.Map.Generation.Installer;
using Modules.Turn.Installer;
using Modules.UserInput.Components;
using Modules.UserInput.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using Modules.UserInput.Tags;
using Modules.UserInput.Configs;

namespace Installers.World
{
    /// <summary>Registers the named ECS storages, the camera singleton component, and world-level ECS systems.</summary>
    public class WorldInstaller : LifetimeScope
    {
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private GameObject _uiRoot;

        protected override void Awake()
        {
            // WorldInstaller is always the root scope. Clear any serialized parent left from the old scope chain.
            parentReference = default;
            base.Awake();
        }

        /// <inheritdoc />
        protected override void Configure(IContainerBuilder builder)
        {
            var singletonArchetype = SingletonArchetypes.Singleton();
            var entityStorages = new EntityStorages(singletonArchetype);
            var world = entityStorages.World;
            builder.RegisterInstance(entityStorages);

            builder.Register<IMainCanvasProvider, MainCanvasProvider>(Lifetime.Scoped).WithParameter(_uiRoot);

            // CameraComponent is single-instance world state, isolated from game entities.
            entityStorages.Singletons.Set(new CameraComponent
            {
                Camera = _mainCamera
            });

            world.CreateEntity(new PlayerInputComponent { PlayerInput = _playerInput }, Tags.Get<PlayerInputTag>());

            // Every first-order system is registered through RegisterAppStateSystem, which exposes it as
            // IAppStateSystem with its AppState flags; each game state finds its own kept systems by filtering
            // that flag set (AppStateSystems.Filter) — nothing here wires a system into a state by hand.
            builder.RegisterAppStateSystem<EventCleanupSystem>(Lifetime.Singleton,
                AppState.Initialization | AppState.ConfigLoading | AppState.InstanceObjects | AppState.MainMenu |
                AppState.MapCreation | AppState.MapLoading | AppState.Gameplay | AppState.GameOver);
            builder.RegisterAppStateSystem<HexSelectionSystem>(Lifetime.Singleton, AppState.Gameplay);
            builder.RegisterAppStateSystem<ConfigLoaderSystem<CameraMovementConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.CAMERA_MOVEMENT_CONFIG);
            builder.RegisterAppStateSystem<CameraMovementSystem>(Lifetime.Singleton, AppState.Gameplay);

            InstallModules(builder);

            // Every game state is registered here, in AppState order, so GameModeMachine can file each under
            // its own mode; Boot only injects the machine and never names a state or a system.
            builder.Register<InitializationState>(Lifetime.Singleton).As<IAppState>();
            builder.Register<ConfigLoadingState>(Lifetime.Singleton).As<IAppState>();
            builder.Register<InstanceObjectsState>(Lifetime.Singleton).As<IAppState>();
            builder.Register<MainMenuState>(Lifetime.Singleton).As<IAppState>();
            builder.Register<MapCreationState>(Lifetime.Singleton).As<IAppState>();
            builder.Register<MapLoadingState>(Lifetime.Singleton).As<IAppState>();
            builder.Register<GameplayState>(Lifetime.Singleton).As<IAppState>();
            builder.Register<GameOverState>(Lifetime.Singleton).As<IAppState>();

            builder.Register<GameModeMachine>(Lifetime.Singleton);
        }

        private void InstallModules(IContainerBuilder builder)
        {
            new AddressableInstaller().Install(builder);
            new UIInstaller().Install(builder);
            new PathfindingInstaller().Install(builder);
            new TerrainGeneratorInstaller().Install(builder);
            new TerrainViewInstaller().Install(builder);
            new HexResourcesInstaller().Install(builder);
            new HexResourcesViewInstaller().Install(builder);
            new DistrictsInstaller().Install(builder);
            new HexIconsInstaller().Install(builder);
            new TurnInstaller().Install(builder);
            new ActorsInstaller().Install(builder);
            new EconomyInstaller().Install(builder);
            new ActionsInstaller().Install(builder);
        }
    }
}
