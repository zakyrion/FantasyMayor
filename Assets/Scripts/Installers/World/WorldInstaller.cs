using DefaultEcs;
using DefaultECSExtensions;
using Modules.Boot.Core;
using Modules.UserInput.Components;
using Modules.UserInput.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace Installers.World
{
    /// <summary>Registers the DefaultEcs <see cref="World" />, the camera entity, and world-level ECS systems.</summary>
    public class WorldInstaller : LifetimeScope
    {
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private PlayerInput _playerInput;

        /// <inheritdoc />
        protected override void Configure(IContainerBuilder builder)
        {
            var world = new DefaultEcs.World();
            builder.RegisterInstance(world);

            var cameraEntity = world.CreateEntity();
            cameraEntity.Set(new CameraComponent { Camera = _mainCamera });

            var playerInputEntity = world.CreateEntity();
            playerInputEntity.Set(new PlayerInputComponent { PlayerInput = _playerInput });

            builder.Register<EventCleanupSystem>(Lifetime.Singleton).As<UpdatedSystem, IUpdatedSystem>();
            builder.Register<HexSelectionSystem>(Lifetime.Singleton).As<UpdatedSystem, IUpdatedSystem>();
            builder.Register<CameraMovementConfigLoaderSystem>(Lifetime.Singleton)
                .As<CameraMovementConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();
            builder.Register<CameraMovementSystem>(Lifetime.Singleton).As<LateUpdatedSystem, ILateUpdatedSystem>();
        }
    }
}
