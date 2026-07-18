using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Presentation.HexResources.Components;
using Presentation.HexResources.Configs;

namespace Presentation.HexResources.Systems
{
    [UsedImplicitly]
    internal sealed class ClayViewConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string CLAY_VIEW_CONFIG = "ClayViewConfig";

        public ClayViewConfigLoaderSystem(IAddressable addressable, EntityStore world)
            : base(addressable, world)
        {
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var loadedConfig = Box<ClayViewConfig>.Empty();

            try
            {
                loadedConfig = await LoadConfigAsync<ClayViewConfig>(CLAY_VIEW_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !loadedConfig.Exist)
                    return;

                // Flattened component owns its data — the ScriptableObject is not retained past load.
                World.SetWorldComponent(ClayViewConfigComponent.FromConfig(loadedConfig.Value));
                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref loadedConfig);
            }
        }
    }
}
