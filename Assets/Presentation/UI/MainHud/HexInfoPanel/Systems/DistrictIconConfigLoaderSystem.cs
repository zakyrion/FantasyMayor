using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Presentation.UI.MainHud.HexInfoPanel.Components;
using Presentation.UI.MainHud.HexInfoPanel.Configs;

namespace Presentation.UI.MainHud.HexInfoPanel.Systems
{
    /// <summary>
    ///     Loads the district-icon config at boot and publishes it as the DistrictIconConfigComponent world
    ///     component. Ownership of the Box transfers to that component so the sprites stay loaded.
    /// </summary>
    [UsedImplicitly]
    internal sealed class DistrictIconConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string DISTRICT_ICON_CONFIG = "DistrictIconConfig";

        public DistrictIconConfigLoaderSystem(IAddressable addressable, EntityStore world)
            : base(addressable, world) { }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var districtIconConfig = Box<DistrictIconConfig>.Empty();
            try
            {
                districtIconConfig = await LoadConfigAsync<DistrictIconConfig>(DISTRICT_ICON_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !districtIconConfig.Exist)
                    return;

                ValidateConfig(districtIconConfig.Value);

                World.SetWorldComponent(new DistrictIconConfigComponent(districtIconConfig));
                districtIconConfig = Box<DistrictIconConfig>.Empty();
                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref districtIconConfig);
            }
        }

        private void ValidateConfig(DistrictIconConfig config)
        {
            if (config.Entries == null || config.Entries.Count == 0)
                throw new InvalidOperationException("DistrictIconConfigLoaderSystem: Entries is null or empty.");
        }
    }
}
