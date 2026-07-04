using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.Components;
using Domains.Actions.Configs;
using JetBrains.Annotations;
using Modules.Addressable.Core;

namespace Domains.Actions.Systems
{
    // Config Loader (ConfigLoadStep, one-shot): loads the DistrictsBuildCostConfig SO from Addressables,
    // validates it, and publishes the DistrictsBuildCostConfigComponent world component carrying the SO
    // reference. The build window reads this catalogue throughout play, so the loader RETAINS the addressable
    // Box (ADDRESSABLE_PATTERNS "Load non-GameObject asset") and releases it in OnDispose. Sibling of Economy's
    // DistrictsBuildConfigLoaderSystem (gating catalogue); this one owns the cost catalogue.
    [UsedImplicitly]
    internal sealed class DistrictsBuildCostConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string ACTIONS_DISTRICTS_BUILD_CONFIG = "ActionsDistrictsBuildConfig";

        private Box<DistrictsBuildCostConfig> _config = Box<DistrictsBuildCostConfig>.Empty();

        public DistrictsBuildCostConfigLoaderSystem(IAddressable addressable, World world)
            : base(addressable, world)
        {
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var box = await LoadConfigAsync<DistrictsBuildCostConfig>(ACTIONS_DISTRICTS_BUILD_CONFIG, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                DisposeBox(ref box);
                return;
            }

            ValidateConfig(box.Value);

            _config = box;
            World.Set(new DistrictsBuildCostConfigComponent(box.Value));
            MarkAsLoaded();
        }

        protected override void OnDispose()
        {
            DisposeBox(ref _config);
        }

        private void ValidateConfig(DistrictsBuildCostConfig config)
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
