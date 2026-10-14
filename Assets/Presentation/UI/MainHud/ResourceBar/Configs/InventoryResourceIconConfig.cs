using System;
using System.Collections.Generic;
using Domains.Economy.Resource.Data;
using UnityEngine;
using EcsExtensions;
using Unity.Collections;

namespace Presentation.UI.MainHud.ResourceBar.Configs
{
    /// <summary>
    ///     Maps each INVENTORY resource type to its top-bar sprite + display name. Authored in the editor and
    ///     loaded as an addressable. The entry list ALSO defines the strip's columns and their order — only the
    ///     ResourceTypes present here get a column. Keys on the Economy ResourceType (Domains.Economy.Resource.Data),
    ///     distinct from Presentation.HexIcons' HexResourceIconConfig (hex resources). Mirrors HexTerrainIconConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryResourceIconConfig",
        menuName = "FantasyMayor/MainUI/InventoryResourceIconConfig")]
    public sealed class InventoryResourceIconConfig : ScriptableObject, IValidatableConfig
    {
        [Serializable]
        public struct ResourceIconEntry
        {
            [SerializeField] private ResourceType _type;
            [SerializeField] private Sprite _sprite;
            [SerializeField] private string _displayName;

            public ResourceType Type => _type;
            public Sprite Sprite => _sprite;
            public string DisplayName => _displayName;
        }

        [SerializeField] private ResourceIconEntry[] _entries;

        public IReadOnlyList<ResourceIconEntry> Entries => _entries;

        public void Validate()
        {
            if (Entries == null || Entries.Count == 0)
                throw new InvalidOperationException("InventoryResourceIconConfig: Entries is null or empty.");

            // NativeHashSet keys require IEquatable<T>, which enums lack — key on the underlying int.
            var seenTypes = new NativeHashSet<int>(Entries.Count, Allocator.Temp);
            try
            {
                foreach (var entry in Entries)
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
