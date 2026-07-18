using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using Domains.Economy.Resource.Data;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Presentation.UI.MainHud.ResourceBar.Components;
using Presentation.UI.MainHud.ResourceBar.Configs;
using Unity.Collections;

namespace Presentation.UI.MainHud.ResourceBar.Systems
{
    /// <summary>
    ///     Loads the inventory-resource-icon config at boot and publishes it as the
    ///     InventoryResourceIconConfigComponent world component. Ownership of the Box transfers to that component
    ///     so the sprites stay loaded. The config's entries also define the resource strip's columns. Mirrors
    ///     HexTerrainIconConfigLoaderSystem.
    /// </summary>
    [UsedImplicitly]
    internal sealed class InventoryResourceIconConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string INVENTORY_RESOURCE_ICON_CONFIG = "InventoryResourceIconConfig";

        public InventoryResourceIconConfigLoaderSystem(IAddressable addressable, EntityStore world)
            : base(addressable, world) { }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var config = Box<InventoryResourceIconConfig>.Empty();
            try
            {
                config = await LoadConfigAsync<InventoryResourceIconConfig>(
                    INVENTORY_RESOURCE_ICON_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !config.Exist)
                    return;

                ValidateConfig(config.Value);

                World.SetWorldComponent(new InventoryResourceIconConfigComponent(config));
                config = Box<InventoryResourceIconConfig>.Empty();
                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref config);
            }
        }

        private void ValidateConfig(InventoryResourceIconConfig config)
        {
            if (config.Entries == null || config.Entries.Count == 0)
                throw new InvalidOperationException("InventoryResourceIconConfig: Entries is null or empty.");

            // NativeHashSet keys require IEquatable<T>, which enums lack — key on the underlying int.
            var seenTypes = new NativeHashSet<int>(config.Entries.Count, Allocator.Temp);
            try
            {
                foreach (var entry in config.Entries)
                {
                    if (entry.Type == ResourceType.Unknown)
                        throw new InvalidOperationException(
                            "InventoryResourceIconConfig: Entries contains an entry with ResourceType.Unknown.");

                    if (!seenTypes.Add((int)entry.Type))
                        throw new InvalidOperationException(
                            $"InventoryResourceIconConfig: duplicate ResourceType '{entry.Type}' in Entries.");
                }
            }
            finally
            {
                seenTypes.Dispose();
            }
        }
    }
}
