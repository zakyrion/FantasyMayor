using Core;
using Cysharp.Threading.Tasks;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Configs;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using System.Threading;
using System;
using Unity.Collections;

namespace Domains.Map.HexResources.Systems
{
    [UsedImplicitly]
    internal sealed class HexResourcesConfigLoaderSystem : ConfigLoaderSystem
    {
        private readonly EntityStorages _storages;
        private const string GAME_RESOURCES_CONFIG = "HexResourcesConfig";

        private Box<HexResourcesConfig> _config = Box<HexResourcesConfig>.Empty();

        public HexResourcesConfigLoaderSystem(IAddressable addressable, EntityStorages storages)
            : base(addressable)
        {
            _storages = storages;
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

                _storages.Singletons.Set(new HexResourcesConfigComponent { Value = _config.Value });
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
