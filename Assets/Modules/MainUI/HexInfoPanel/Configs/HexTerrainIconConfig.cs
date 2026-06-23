using System;
using System.Collections.Generic;
using Domains.Map.Hex.Data;
using UnityEngine;

namespace Modules.MainUI.HexInfoPanel.Configs
{
    /// <summary>
    ///     Maps each terrain type to its panel header sprite + display name. Authored in the editor and loaded
    ///     as an addressable. Mirrors the structure of HexIcons' HexResourceIconConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "HexTerrainIconConfig",
        menuName = "FantasyMayor/MainUI/HexTerrainIconConfig")]
    public sealed class HexTerrainIconConfig : ScriptableObject
    {
        [Serializable]
        public struct TerrainIconEntry
        {
            [SerializeField] private HexType _type;
            [SerializeField] private Sprite _sprite;
            [SerializeField] private string _displayName;

            public HexType Type => _type;
            public Sprite Sprite => _sprite;
            public string DisplayName => _displayName;
        }

        [SerializeField] private TerrainIconEntry[] _entries;

        public IReadOnlyList<TerrainIconEntry> Entries => _entries;
    }
}
