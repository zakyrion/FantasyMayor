using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Presentation.Resources.Components;
using Presentation.Resources.Configs;

namespace Presentation.Resources.Systems
{
    [UsedImplicitly]
    internal sealed class HexResourcesViewConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string GAME_RESOURCES_VIEW_CONFIG = "HexResourcesViewConfig";

        private Box<HexResourcesViewConfig> _config = Box<HexResourcesViewConfig>.Empty();

        public HexResourcesViewConfigLoaderSystem(IAddressable addressable, World world)
            : base(addressable, world)
        {
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var loadedConfig = Box<HexResourcesViewConfig>.Empty();

            try
            {
                loadedConfig = await LoadConfigAsync<HexResourcesViewConfig>(GAME_RESOURCES_VIEW_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !loadedConfig.Exist)
                    return;

                ValidateConfig(loadedConfig.Value);

                _config = loadedConfig;
                loadedConfig = Box<HexResourcesViewConfig>.Empty();

                World.Set(new HexResourcesViewConfigComponent { Value = _config.Value });
                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref loadedConfig);
            }
        }

        protected override void OnDispose()
        {
            DisposeBox(ref _config);
            base.OnDispose();
        }

        private static void ValidateConfig(HexResourcesViewConfig config)
        {
            foreach (var resource in config.Resources)
                if (resource.Prefab == null)
                    throw new Exception($"Game resources view config contains null prefab for resource type '{resource.Type}'.");
        }
    }
}
