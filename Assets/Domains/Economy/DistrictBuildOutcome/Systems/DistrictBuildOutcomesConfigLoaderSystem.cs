using Core;
using Cysharp.Threading.Tasks;
using Domains.Economy.DistrictBuildOutcome.Components;
using Domains.Economy.DistrictBuildOutcome.Configs;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using System.Threading;
using System;

namespace Domains.Economy.DistrictBuildOutcome.Systems{
    // Config Loader (ConfigLoadStep, one-shot): loads the BuildDistrictOutcomesConfig SO from Addressables,
    // validates it, and publishes BuildDistrictOutcomesConfigComponent carrying the SO reference. The spawn
    // orchestrator consumes the concrete outcome references at MapGenerationStep, so the loader RETAINS the
    // addressable Box and releases it in OnDispose. Mirrors DistrictOpenConditionsConfigLoaderSystem.
    [UsedImplicitly]
    internal sealed class DistrictBuildOutcomesConfigLoaderSystem : ConfigLoaderSystem
    {
        private readonly EntityStorages _storages;
        private const string DISTRICT_BUILD_OUTCOMES_CONFIG = "BuildDistrictOutcomesConfig";

        private Box<DistrictBuildOutcomesConfig> _config = Box<DistrictBuildOutcomesConfig>.Empty();

        public DistrictBuildOutcomesConfigLoaderSystem(IAddressable addressable, EntityStorages storages)
            : base(addressable)
        {
            _storages = storages;
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var box = await LoadConfigAsync<DistrictBuildOutcomesConfig>(
                DISTRICT_BUILD_OUTCOMES_CONFIG, cancellationToken);
            if (cancellationToken.IsCancellationRequested || !box.Exist)
            {
                DisposeBox(ref box);
                return;
            }

            ValidateConfig(box.Value);

            _config = box;
            _storages.Singletons.Set(new DistrictBuildOutcomesConfigComponent(box.Value));
            MarkAsLoaded();
        }

        protected override void OnDispose()
        {
            DisposeBox(ref _config);
        }

        private void ValidateConfig(DistrictBuildOutcomesConfig config)
        {
            if (config.Outcomes == null)
                throw new InvalidOperationException("BuildDistrictOutcomesConfig: Outcomes array is null.");

            for (var index = 0; index < config.Outcomes.Length; index++)
            {
                if (config.Outcomes[index] == null)
                    throw new InvalidOperationException(
                        $"BuildDistrictOutcomesConfig: outcome entry at index {index} is null.");
            }
        }
    }
}
