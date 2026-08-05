using Core;
using Cysharp.Threading.Tasks;
using Domains.Economy.DistrictBuild.Components;
using Domains.Economy.DistrictBuild.Configs;
using Domains.Kernel.Data;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using System.Threading;
using System;

namespace Domains.Economy.DistrictBuild.Systems
{
    // Config Loader (ConfigLoadStep, one-shot): loads the DistrictsBuildConfig SO from Addressables, validates
    // it, and publishes the DistrictsBuildConfigComponent world component carrying the SO reference. Unlike the
    // copy-out actor loaders, the build window reads this catalogue throughout play, so the loader RETAINS the
    // addressable Box (ADDRESSABLE_PATTERNS "Load non-GameObject asset") and releases it in OnDispose.
    [UsedImplicitly]
    internal sealed class DistrictsBuildConfigLoaderSystem : ConfigLoaderSystem
    {
        private readonly EntityStorages _storages;
        private const string DISTRICTS_BUILD_CONFIG = "DistrictBuildsConfig";

        private Box<DistrictBuildsConfig> _config = Box<DistrictBuildsConfig>.Empty();

        public DistrictsBuildConfigLoaderSystem(IAddressable addressable, EntityStorages storages) : base(addressable, storages.World)
        {
            _storages = storages;
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var box = await LoadConfigAsync<DistrictBuildsConfig>(DISTRICTS_BUILD_CONFIG, cancellationToken);
            if (cancellationToken.IsCancellationRequested || !box.Exist)
            {
                DisposeBox(ref box);
                return;
            }

            ValidateConfig(box.Value);

            _config = box;
            _storages.World.SetWorldComponent(new DistrictBuildsConfigComponent(box.Value));
            MarkAsLoaded();
        }

        protected override void OnDispose()
        {
            DisposeBox(ref _config);
        }

        private void ValidateConfig(DistrictBuildsConfig config)
        {
            if (config.Districts == null)
                throw new InvalidOperationException("DistrictsBuildConfig: Districts array is null.");

            for (var index = 0; index < config.Districts.Length; index++)
            {
                var district = config.Districts[index];

                if (district == null)
                    throw new InvalidOperationException(
                        $"DistrictsBuildConfig: district entry at index {index} is null.");

                if (district.AllowedOwners == ActorType.Unknown)
                    throw new InvalidOperationException(
                        $"DistrictsBuildConfig: district '{district.DistrictType}' at index {index} " +
                        "has no allowed owners (AllowedOwners is Unknown).");
            }
        }
    }
}
