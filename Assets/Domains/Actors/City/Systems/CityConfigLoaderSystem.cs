using Core;
using Cysharp.Threading.Tasks;
using Domains.Actors.City.Components;
using Domains.Actors.City.Configs;
using Domains.Economy.Resource.Data;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using System.Threading;
using System;
using Unity.Collections;

namespace Domains.Actors.City.Systems
{
    // Config Loader (ConfigLoadStep, one-shot): loads the CityConfig SO from Addressables, validates it,
    // and publishes the flattened CityConfigComponent singleton component. CitySpawnSystem reads it at map
    // creation to seed the City's starting resources (Patterns/PATTERN_CONFIG_LOADER.md).
    [UsedImplicitly]
    internal sealed class CityConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string CITY_CONFIG = "CityConfig";
        private readonly EntityStorages _storages;

        public CityConfigLoaderSystem(IAddressable addressable, EntityStorages storages)
            : base(addressable)
        {
            _storages = storages;
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var configBox = Box<CityConfig>.Empty();

            try
            {
                configBox = await LoadConfigAsync<CityConfig>(CITY_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !configBox.Exist)
                    return;

                ValidateConfig(configBox.Value);

                _storages.Singletons.Set(CityConfigComponent.FromConfig(configBox.Value));
                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref configBox);
            }
        }

        private void ValidateConfig(CityConfig config)
        {
            if (config.Resources == null)
                throw new InvalidOperationException("CityConfig: Resources array is null.");

            // NativeHashSet keys require IEquatable<T>, which enums lack — key on the underlying int.
            var seenTypes = new NativeHashSet<int>(4, Allocator.Temp);
            try
            {
                foreach (var resource in config.Resources)
                {
                    if (resource.Type == ResourceType.Unknown)
                        throw new InvalidOperationException(
                            "CityConfig: Resources contains an entry with ResourceType.Unknown.");

                    if (!seenTypes.Add((int)resource.Type))
                        throw new InvalidOperationException(
                            $"CityConfig: duplicate ResourceType '{resource.Type}' in Resources.");
                }
            }
            finally
            {
                seenTypes.Dispose();
            }
        }
    }
}
