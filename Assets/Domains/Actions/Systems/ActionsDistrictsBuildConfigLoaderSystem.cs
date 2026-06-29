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
    // Config Loader (ConfigLoadStep, one-shot): loads the ActionsDistrictsBuildConfig SO from Addressables,
    // validates it, and publishes the ActionsDistrictsBuildConfigComponent world component carrying the SO
    // reference. The build window reads this catalogue throughout play, so the loader RETAINS the addressable
    // Box (ADDRESSABLE_PATTERNS "Load non-GameObject asset") and releases it in OnDispose. Sibling of Economy's
    // DistrictsBuildConfigLoaderSystem (gating catalogue); this one owns the cost catalogue.
    [UsedImplicitly]
    internal sealed class ActionsDistrictsBuildConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string ACTIONS_DISTRICTS_BUILD_CONFIG = "ActionsDistrictsBuildConfig";

        private Box<ActionsDistrictsBuildConfig> _config = Box<ActionsDistrictsBuildConfig>.Empty();

        public ActionsDistrictsBuildConfigLoaderSystem(IAddressable addressable, World world)
            : base(addressable, world)
        {
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var box = await LoadConfigAsync<ActionsDistrictsBuildConfig>(ACTIONS_DISTRICTS_BUILD_CONFIG, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                DisposeBox(ref box);
                return;
            }

            ValidateConfig(box.Value);

            _config = box;
            World.Set(new ActionsDistrictsBuildConfigComponent(box.Value));
            MarkAsLoaded();
        }

        protected override void OnDispose()
        {
            DisposeBox(ref _config);
        }

        private void ValidateConfig(ActionsDistrictsBuildConfig config)
        {
            if (config.Districts == null)
                throw new InvalidOperationException("ActionsDistrictsBuildConfig: Districts array is null.");

            for (var index = 0; index < config.Districts.Length; index++)
                if (config.Districts[index] == null)
                    throw new InvalidOperationException(
                        $"ActionsDistrictsBuildConfig: district entry at index {index} is null.");
        }
    }
}
