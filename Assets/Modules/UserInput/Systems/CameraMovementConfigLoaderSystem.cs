using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.UserInput.Components;
using Modules.UserInput.Configs;
using System.Threading;

namespace Modules.UserInput.Systems
{
    /// <summary>
    ///     Loads <see cref="CameraMovementConfig" /> from Addressables during the config-load boot phase
    ///     and publishes a flattened ECS component for runtime camera systems.
    /// </summary>
    [UsedImplicitly]
    public sealed class CameraMovementConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string CAMERA_MOVEMENT_CONFIG = "CameraMovementConfig";

        /// <param name="addressable">Addressable loader abstraction.</param>
        /// <param name="world">World that receives the flattened config entity.</param>
        public CameraMovementConfigLoaderSystem(IAddressable addressable, EntityStore world)
            : base(addressable, world)
        {
        }

        /// <inheritdoc />
        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var cameraMovementConfig = Box<CameraMovementConfig>.Empty();

            try
            {
                cameraMovementConfig = await LoadConfigAsync<CameraMovementConfig>(CAMERA_MOVEMENT_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return;

                World.SetWorldComponent(CameraMovementConfigComponent.FromConfig(cameraMovementConfig.Value));
                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref cameraMovementConfig);
            }
        }
    }
}
