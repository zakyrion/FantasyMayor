using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actors.Mayor.Components;
using Domains.Actors.Mayor.Configs;
using Domains.Economy.Resource.Data;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Unity.Collections;

namespace Domains.Actors.Mayor.Systems
{
    // Config Loader (ConfigLoadStep, one-shot): loads the MayorConfig SO from Addressables, validates it,
    // and publishes the flattened MayorConfigComponent world component. MayorSpawnSystem reads it at map
    // creation to seed the Mayor's starting resources + Action Points. See CONFIGTEMPLATE.md / ACTORS.md.
    [UsedImplicitly]
    internal sealed class MayorConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string MAYOR_CONFIG = "MayorConfig";

        public MayorConfigLoaderSystem(IAddressable addressable, World world) : base(addressable, world)
        {
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var configBox = Box<MayorConfig>.Empty();

            try
            {
                configBox = await LoadConfigAsync<MayorConfig>(MAYOR_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !configBox.Exist)
                    return;

                ValidateConfig(configBox.Value);

                World.Set(MayorConfigComponent.FromConfig(configBox.Value));
                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref configBox);
            }
        }

        private void ValidateConfig(MayorConfig config)
        {
            if (config.Resources == null)
                throw new InvalidOperationException("MayorConfig: Resources array is null.");

            if (config.StartActionPoints < 0)
                throw new InvalidOperationException(
                    $"MayorConfig: StartActionPoints must be >= 0, was {config.StartActionPoints}.");
        }
    }
}
