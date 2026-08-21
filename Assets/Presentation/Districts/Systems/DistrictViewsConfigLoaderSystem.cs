using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Presentation.Districts.Components;
using Presentation.Districts.Configs;

namespace Presentation.Districts.Systems
{
    // Config Loader (ConfigLoadStep, one-shot): loads the DistrictViewsConfig SO from Addressables, validates it,
    // and publishes the DistrictViewsConfigComponent singleton component carrying the SO reference. The reactive
    // DistrictViewSpawnSystem reads this catalogue throughout play, so the loader RETAINS the addressable Box and
    // releases it in OnDispose. Mirrors the Economy district-config loaders (DistrictBuildCostsConfigLoaderSystem).
    [UsedImplicitly]
    internal sealed class DistrictViewsConfigLoaderSystem : ConfigLoaderSystem
    {
        private readonly EntityStorages _storages;
        private const string DISTRICT_VIEWS_CONFIG = "DistrictViewsConfig";

        private Box<DistrictViewsConfig> _config = Box<DistrictViewsConfig>.Empty();

        public DistrictViewsConfigLoaderSystem(IAddressable addressable, EntityStorages storages)
            : base(addressable)
        {
            _storages = storages;
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var box = await LoadConfigAsync<DistrictViewsConfig>(DISTRICT_VIEWS_CONFIG, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                DisposeBox(ref box);
                return;
            }

            ValidateConfig(box.Value);

            _config = box;
            _storages.Singletons.Set(new DistrictViewsConfigComponent(box.Value));
            MarkAsLoaded();
        }

        protected override void OnDispose()
        {
            DisposeBox(ref _config);
        }

        private void ValidateConfig(DistrictViewsConfig config)
        {
            if (config.Views == null)
                throw new InvalidOperationException("DistrictViewsConfig: Views array is null.");

            for (var index = 0; index < config.Views.Length; index++)
            {
                if (config.Views[index] == null)
                    throw new InvalidOperationException(
                        $"DistrictViewsConfig: view entry at index {index} is null.");

                if (config.Views[index].Prefab == null)
                    throw new InvalidOperationException(
                        $"DistrictViewsConfig: view entry at index {index} ('{config.Views[index].DistrictType}') has no prefab.");
            }
        }
    }
}
