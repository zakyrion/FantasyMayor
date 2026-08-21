using Core;
using Cysharp.Threading.Tasks;
using Domains.Economy.DistrictBuildCost.Components;
using Domains.Economy.DistrictBuildCost.Configs;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using System.Threading;
using System;

namespace Domains.Economy.DistrictBuildCost.Systems{
    // Config Loader (ConfigLoadStep, one-shot): loads the DistrictsBuildCostConfig SO from Addressables,
    // validates it, and publishes the DistrictsBuildCostConfigComponent singleton component carrying the SO
    // reference. The build window reads this catalogue throughout play, so the loader RETAINS the addressable
    // Box (ADDRESSABLE_PATTERNS "Load non-GameObject asset") and releases it in OnDispose. Sibling of Economy's
    // DistrictsBuildConfigLoaderSystem (gating catalogue); this one owns the cost catalogue.
    [UsedImplicitly]
    internal sealed class DistrictBuildCostsConfigLoaderSystem : ConfigLoaderSystem
    {
        private readonly EntityStorages _storages;
        private const string ACTIONS_DISTRICTS_BUILD_CONFIG = "DistrictBuildCostsConfig";

        private Box<DistrictBuildCostsConfig> _config = Box<DistrictBuildCostsConfig>.Empty();

        public DistrictBuildCostsConfigLoaderSystem(IAddressable addressable, EntityStorages storages)
            : base(addressable)
        {
            _storages = storages;
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var box = await LoadConfigAsync<DistrictBuildCostsConfig>(ACTIONS_DISTRICTS_BUILD_CONFIG, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                DisposeBox(ref box);
                return;
            }

            ValidateConfig(box.Value);

            _config = box;
            _storages.Singletons.Set(new DistrictBuildCostsConfigComponent(box.Value));
            MarkAsLoaded();
        }

        protected override void OnDispose()
        {
            DisposeBox(ref _config);
        }

        private void ValidateConfig(DistrictBuildCostsConfig config)
        {
            if (config.Districts == null)
                throw new InvalidOperationException("DistrictsBuildCostConfig: Districts array is null.");

            for (var index = 0; index < config.Districts.Length; index++)
                if (config.Districts[index] == null)
                    throw new InvalidOperationException(
                        $"DistrictsBuildCostConfig: district entry at index {index} is null.");
        }
    }
}
