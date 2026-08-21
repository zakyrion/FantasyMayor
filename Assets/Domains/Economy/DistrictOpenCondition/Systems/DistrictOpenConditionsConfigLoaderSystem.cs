using Core;
using Cysharp.Threading.Tasks;
using Domains.Economy.DistrictOpenCondition.Components;
using Domains.Economy.DistrictOpenCondition.Configs;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using System.Threading;
using System;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Config Loader (ConfigLoadStep, one-shot): loads the DistrictOpenConditionsConfig SO from Addressables,
    // validates it, and publishes DistrictOpenConditionsConfigComponent carrying the SO reference. The spawn
    // orchestrator consumes the concrete condition references at MapGenerationStep, so the loader RETAINS the
    // addressable Box and releases it in OnDispose. Mirrors DistrictsBuildConfigLoaderSystem.
    [UsedImplicitly]
    internal sealed class DistrictOpenConditionsConfigLoaderSystem : ConfigLoaderSystem
    {
        private readonly EntityStorages _storages;
        private const string DISTRICT_OPEN_CONDITIONS_CONFIG = "DistrictOpenConditionsConfig";

        private Box<DistrictOpenConditionsConfig> _config = Box<DistrictOpenConditionsConfig>.Empty();

        public DistrictOpenConditionsConfigLoaderSystem(IAddressable addressable, EntityStorages storages)
            : base(addressable)
        {
            _storages = storages;
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var box = await LoadConfigAsync<DistrictOpenConditionsConfig>(
                DISTRICT_OPEN_CONDITIONS_CONFIG, cancellationToken);
            if (cancellationToken.IsCancellationRequested || !box.Exist)
            {
                DisposeBox(ref box);
                return;
            }

            ValidateConfig(box.Value);

            _config = box;
            _storages.Singletons.Set(new DistrictOpenConditionsConfigComponent(box.Value));
            MarkAsLoaded();
        }

        protected override void OnDispose()
        {
            DisposeBox(ref _config);
        }

        private void ValidateConfig(DistrictOpenConditionsConfig config)
        {
            if (config.Conditions == null)
                throw new InvalidOperationException("DistrictOpenConditionsConfig: Conditions array is null.");

            for (var index = 0; index < config.Conditions.Length; index++)
            {
                if (config.Conditions[index] == null)
                    throw new InvalidOperationException(
                        $"DistrictOpenConditionsConfig: condition entry at index {index} is null.");
            }
        }
    }
}
