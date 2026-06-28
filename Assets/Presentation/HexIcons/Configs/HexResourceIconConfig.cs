using System;
using System.Collections.Generic;
using Domains.Map.HexResources.Data;
using UnityEngine;

namespace Presentation.HexIcons.Configs
{
    [CreateAssetMenu(fileName = "HexResourceIconConfig",
        menuName = "FantasyMayor/HexIcons/HexResourceIconConfig")]
    public sealed class HexResourceIconConfig : ScriptableObject
    {
        [Serializable]
        public struct ResourceIconEntry
        {
            [SerializeField] private HexResourceType _hexResourceType;
            [SerializeField] private Sprite _sprite;

            public HexResourceType HexResourceType => _hexResourceType;
            public Sprite Sprite => _sprite;
        }

        [SerializeField] private ResourceIconEntry[] _entries;
        public IReadOnlyList<ResourceIconEntry> Entries => _entries;
    }
}
