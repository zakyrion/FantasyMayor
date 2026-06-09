using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.HexResources.Components;
using Modules.HexResources.Configs;
using Modules.HexResources.Data;
using Unity.Collections;

namespace Modules.HexResources.Systems
{
    [UsedImplicitly]
    internal sealed class HexResourcesConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string GAME_RESOURCES_CONFIG = "HexResourcesConfig";

        private Box<HexResourcesConfig> _config = Box<HexResourcesConfig>.Empty();

        public HexResourcesConfigLoaderSystem(IAddressable addressable, World world)
            : base(addressable, world)
        {
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var loadedConfig = Box<HexResourcesConfig>.Empty();

            try
            {
                loadedConfig = await LoadConfigAsync<HexResourcesConfig>(GAME_RESOURCES_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !loadedConfig.Exist)
                    return;

                ValidateConfig(loadedConfig.Value);

                _config = loadedConfig;
                loadedConfig = Box<HexResourcesConfig>.Empty();

                World.Set(new HexResourcesConfigComponent { Value = _config.Value });
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

        private void ValidateConfig(HexResourcesConfig config)
        {
            // NativeHashSet keys require IEquatable<T>, which enums lack — key on the underlying int.
            var seenTypes = new NativeHashSet<int>(8, Allocator.Temp);
            try
            {
                foreach (var resource in config.Resources)
                {
                    if (resource.Config == null)
                        throw new Exception($"Game resources config contains null config for resource type '{resource.Type}'.");

                    if (!seenTypes.Add((int)resource.Type))
                        throw new Exception($"Game resources config contains duplicate resource type '{resource.Type}'.");
                }
            }
            finally
            {
                seenTypes.Dispose();
            }
        }
    }
}
