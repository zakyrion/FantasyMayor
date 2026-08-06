using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Presentation.HexIcons.Components;
using Presentation.HexIcons.Configs;

namespace Presentation.HexIcons.Systems
{
    [UsedImplicitly]
    internal sealed class HexIconsConfigLoaderSystem : ConfigLoaderSystem
    {
        private readonly EntityStorages _storages;
        private const string HEX_ICONS_CONFIG = "HexIconsConfig";
        private const string HEX_RESOURCE_ICON_CONFIG = "HexResourceIconConfig";

        public HexIconsConfigLoaderSystem(IAddressable addressable, EntityStorages storages)
            : base(addressable)
        {
            _storages = storages;
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            // Both boxes stay owned here until every Singletons.Set succeeds; the shared finally rolls back
            // whichever load already committed if a later one fails or cancels.
            var iconsConfig = Box<HexIconsConfig>.Empty();
            var resourceIconConfig = Box<HexResourceIconConfig>.Empty();
            try
            {
                iconsConfig = await LoadConfigAsync<HexIconsConfig>(HEX_ICONS_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !iconsConfig.Exist)
                    return;

                resourceIconConfig = await LoadConfigAsync<HexResourceIconConfig>(HEX_RESOURCE_ICON_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !resourceIconConfig.Exist)
                    return;

                ValidateConfig(iconsConfig.Value);
                ValidateResourceIconConfig(resourceIconConfig.Value);

                _storages.Singletons.Set(new HexIconsConfigComponent(iconsConfig));
                _storages.Singletons.Set(new HexResourceIconConfigComponent(resourceIconConfig));
                iconsConfig = Box<HexIconsConfig>.Empty();
                resourceIconConfig = Box<HexResourceIconConfig>.Empty();
                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref iconsConfig);
                DisposeBox(ref resourceIconConfig);
            }
        }

        private static void ValidateConfig(HexIconsConfig config)
        {
            if (config.Prefab == null)
                throw new InvalidOperationException("HexIconsConfig: Prefab is null.");
        }

        private static void ValidateResourceIconConfig(HexResourceIconConfig config)
        {
            if (config.Entries == null || config.Entries.Count == 0)
                throw new InvalidOperationException("HexResourceIconConfig: Entries is null or empty.");
        }
    }
}
