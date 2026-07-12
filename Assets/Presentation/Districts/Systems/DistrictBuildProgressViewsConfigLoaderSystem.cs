using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Presentation.Districts.Components;
using Presentation.Districts.Configs;

namespace Presentation.Districts.Systems
{
    // Config Loader (ConfigLoadStep, one-shot): loads the DistrictBuildProgressViewsConfig SO from Addressables,
    // validates it, and publishes the DistrictBuildProgressViewsConfigComponent world component carrying the SO
    // reference. The reactive DistrictBuildProgressViewSpawnSystem reads this catalogue throughout play, so the
    // loader RETAINS the addressable Box and releases it in OnDispose. Mirrors DistrictViewsConfigLoaderSystem.
    [UsedImplicitly]
    internal sealed class DistrictBuildProgressViewsConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string DISTRICT_BUILD_PROGRESS_VIEWS_CONFIG = "DistrictBuildProgressViewsConfig";

        private Box<DistrictBuildProgressViewsConfig> _config = Box<DistrictBuildProgressViewsConfig>.Empty();

        public DistrictBuildProgressViewsConfigLoaderSystem(IAddressable addressable, World world)
            : base(addressable, world)
        {
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var box = await LoadConfigAsync<DistrictBuildProgressViewsConfig>(DISTRICT_BUILD_PROGRESS_VIEWS_CONFIG, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                DisposeBox(ref box);
                return;
            }

            ValidateConfig(box.Value);

            _config = box;
            World.Set(new DistrictBuildProgressViewsConfigComponent(box.Value));
            MarkAsLoaded();
        }

        protected override void OnDispose()
        {
            DisposeBox(ref _config);
        }

        private void ValidateConfig(DistrictBuildProgressViewsConfig config)
        {
            if (config.Views == null)
                throw new InvalidOperationException("DistrictBuildProgressViewsConfig: Views array is null.");

            for (var index = 0; index < config.Views.Length; index++)
            {
                if (config.Views[index] == null)
                    throw new InvalidOperationException(
                        $"DistrictBuildProgressViewsConfig: view entry at index {index} is null.");

                if (config.Views[index].Prefab == null)
                    throw new InvalidOperationException(
                        $"DistrictBuildProgressViewsConfig: view entry at index {index} ('{config.Views[index].DistrictType}') has no prefab.");
            }
        }
    }
}
