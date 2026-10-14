using System;
using System.Collections.Generic;
using Domains.Economy.District.Data;
using UnityEngine;
using EcsExtensions;

namespace Presentation.UI.MainHud.HexInfoPanel.Configs
{
    /// <summary>
    ///     Maps each district type to its panel sprite + display name. Authored in the editor and loaded as an
    ///     addressable. Mirrors the structure of HexTerrainIconConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "DistrictIconConfig",
        menuName = "FantasyMayor/MainUI/DistrictIconConfig")]
    public sealed class DistrictIconConfig : ScriptableObject, IValidatableConfig
    {
        [Serializable]
        public struct DistrictIconEntry
        {
            [SerializeField] private DistrictType _type;
            [SerializeField] private Sprite _sprite;
            [SerializeField] private string _displayName;

            public DistrictType Type => _type;
            public Sprite Sprite => _sprite;
            public string DisplayName => _displayName;
        }

        [SerializeField] private DistrictIconEntry[] _entries;

        public IReadOnlyList<DistrictIconEntry> Entries => _entries;

        public void Validate()
        {
            if (Entries == null || Entries.Count == 0)
                throw new InvalidOperationException("DistrictIconConfig: Entries is null or empty.");
        }
    }
}
