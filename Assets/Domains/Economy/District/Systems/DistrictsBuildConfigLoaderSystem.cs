using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Economy.District.Components;
using Domains.Economy.District.Configs;
using JetBrains.Annotations;
using Modules.Addressable.Core;

namespace Domains.Economy.District.Systems
{
    // Config Loader (ConfigLoadStep, one-shot): loads the DistrictsBuildConfig SO from Addressables, validates
    // it, and publishes the DistrictsBuildConfigComponent world component carrying the SO reference. Unlike the
    // copy-out actor loaders, the build window reads this catalogue throughout play, so the loader RETAINS the
    // addressable Box (ADDRESSABLE_PATTERNS "Load non-GameObject asset") and releases it in OnDispose.
    [UsedImplicitly]
    internal sealed class DistrictsBuildConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string DISTRICTS_BUILD_CONFIG = "DistrictsBuildConfig";

        private Box<DistrictsBuildConfig> _config = Box<DistrictsBuildConfig>.Empty();

        public DistrictsBuildConfigLoaderSystem(IAddressable addressable, World world) : base(addressable, world)
        {
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var box = await LoadConfigAsync<DistrictsBuildConfig>(DISTRICTS_BUILD_CONFIG, cancellationToken);
            if (cancellationToken.IsCancellationRequested || !box.Exist)
            {
                DisposeBox(ref box);
                return;
            }

            ValidateConfig(box.Value);

            _config = box;
            World.Set(new DistrictsBuildConfigComponent(box.Value));
            MarkAsLoaded();
        }

        protected override void OnDispose()
        {
            DisposeBox(ref _config);
        }

        private void ValidateConfig(DistrictsBuildConfig config)
        {
            if (config.Districts == null)
                throw new InvalidOperationException("DistrictsBuildConfig: Districts array is null.");

            for (var index = 0; index < config.Districts.Length; index++)
            {
                if (config.Districts[index] == null)
                    throw new InvalidOperationException(
                        $"DistrictsBuildConfig: district entry at index {index} is null.");
            }
        }
    }
}
