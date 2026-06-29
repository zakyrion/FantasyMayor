using System;
using System.Collections.Generic;
using Domains.Economy.Resource.Data;
using UnityEngine;

namespace Presentation.UI.ResourceBar.Configs
{
    /// <summary>
    ///     Maps each INVENTORY resource type to its top-bar sprite + display name. Authored in the editor and
    ///     loaded as an addressable. The entry list ALSO defines the strip's columns and their order — only the
    ///     ResourceTypes present here get a column. Keys on the Economy ResourceType (Domains.Economy.Resource.Data),
    ///     distinct from Presentation.HexIcons' HexResourceIconConfig (hex resources). Mirrors HexTerrainIconConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryResourceIconConfig",
        menuName = "FantasyMayor/MainUI/InventoryResourceIconConfig")]
    public sealed class InventoryResourceIconConfig : ScriptableObject
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
    }
}
