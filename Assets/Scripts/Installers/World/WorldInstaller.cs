using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.Installer;
using Domains.Actors.Installer;
using Domains.Economy.Installer;
using Installers.Addressable;
using Presentation.Terrain.Installer;
using Modules.Boot.Core;
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

namespace Installers.World
{
    /// <summary>Registers the DefaultEcs <see cref="World" />, the camera world component, and world-level ECS systems.</summary>
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
            var world = new DefaultEcs.World();
            builder.RegisterInstance(world);
            builder.Register<IMainCanvasProvider, MainCanvasProvider>(Lifetime.Scoped).WithParameter(_uiRoot);

            // CameraComponent is single-instance world state, stored as a world component, not an entity.
            world.Set(new CameraComponent
            {
                Camera = _mainCamera
            });

            var playerInputEntity = world.CreateEntity();
            playerInputEntity.Set(new PlayerInputComponent { PlayerInput = _playerInput });
            playerInputEntity.Set(new PlayerInputTag());

            // Per-frame systems are registered as concrete singletons; Boot wires them into game states by hand.
            builder.Register<EventCleanupSystem>(Lifetime.Singleton).As<EventCleanupSystem>();
            builder.Register<HexSelectionSystem>(Lifetime.Singleton).As<HexSelectionSystem>();
            builder.Register<CameraMovementConfigLoaderSystem>(Lifetime.Singleton)
                .As<CameraMovementConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();
            builder.Register<CameraMovementSystem>(Lifetime.Singleton).As<CameraMovementSystem>();

            InstallModules(builder);
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
