using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.HexesUI.Components;
using Modules.HexesUI.Configs;

namespace Modules.HexesUI.Systems
{
    /// <summary>
    ///     Loads the terrain-icon config at boot and publishes it as the HexTerrainIconConfigComponent world
    ///     component. Ownership of the Box transfers to that component so the sprites stay loaded.
    /// </summary>
    [UsedImplicitly]
    internal sealed class HexTerrainIconConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string HEX_TERRAIN_ICON_CONFIG = "HexTerrainIconConfig";

        public HexTerrainIconConfigLoaderSystem(IAddressable addressable, World world)
            : base(addressable, world) { }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var terrainIconConfig = Box<HexTerrainIconConfig>.Empty();
            try
            {
                terrainIconConfig = await LoadConfigAsync<HexTerrainIconConfig>(HEX_TERRAIN_ICON_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !terrainIconConfig.Exist)
                    return;

                ValidateConfig(terrainIconConfig.Value);

                World.Set(new HexTerrainIconConfigComponent(terrainIconConfig));
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
