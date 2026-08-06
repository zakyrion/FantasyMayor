using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Presentation.UI.MainHud.HexInfoPanel.Components;
using Presentation.UI.MainHud.HexInfoPanel.Configs;

namespace Presentation.UI.MainHud.HexInfoPanel.Systems
{
    /// <summary>
    ///     Loads the terrain-icon config at boot and publishes it as the HexTerrainIconConfigComponent world
    ///     component. Ownership of the Box transfers to that component so the sprites stay loaded.
    /// </summary>
    [UsedImplicitly]
    internal sealed class HexTerrainIconConfigLoaderSystem : ConfigLoaderSystem
    {
        private readonly EntityStorages _storages;
        private const string HEX_TERRAIN_ICON_CONFIG = "HexTerrainIconConfig";

        public HexTerrainIconConfigLoaderSystem(IAddressable addressable, EntityStorages storages)
            : base(addressable)
        {
            _storages = storages;
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var terrainIconConfig = Box<HexTerrainIconConfig>.Empty();
            try
            {
                terrainIconConfig = await LoadConfigAsync<HexTerrainIconConfig>(HEX_TERRAIN_ICON_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !terrainIconConfig.Exist)
                    return;

                ValidateConfig(terrainIconConfig.Value);

                _storages.Singletons.Set(new HexTerrainIconConfigComponent(terrainIconConfig));
                terrainIconConfig = Box<HexTerrainIconConfig>.Empty();
                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref terrainIconConfig);
            }
        }

        private void ValidateConfig(HexTerrainIconConfig config)
        {
            if (config.Entries == null || config.Entries.Count == 0)
                throw new InvalidOperationException("HexTerrainIconConfig: Entries is null or empty.");
        }
    }
}
