using System;
using System.Collections.Generic;
using Modules.HexesUI.Data;
using UnityEngine;

namespace Modules.HexesUI.Configs
{
    /// <summary>
    ///     Maps each terrain type to its panel header sprite + display name. Authored in the editor and loaded
    ///     as an addressable. Mirrors the structure of HexIcons' HexResourceIconConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "HexTerrainIconConfig",
        menuName = "FantasyMayor/HexesUI/HexTerrainIconConfig")]
    public sealed class HexTerrainIconConfig : ScriptableObject
    {
        [Serializable]
        public struct TerrainIconEntry
        {
            [SerializeField] private HexTerrainType _terrainType;
            [SerializeField] private Sprite _sprite;
            [SerializeField] private string _displayName;

            public HexTerrainType TerrainType => _terrainType;
            public Sprite Sprite => _sprite;
            public string DisplayName => _displayName;
        }

        [SerializeField] private TerrainIconEntry[] _entries;

        public IReadOnlyList<TerrainIconEntry> Entries => _entries;
    }
}
