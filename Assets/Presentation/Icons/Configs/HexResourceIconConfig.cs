using System;
using System.Collections.Generic;
using Domains.Map.HexResources.Data;
using UnityEngine;

namespace Presentation.Icons.Configs
{
    [CreateAssetMenu(fileName = "HexResourceIconConfig",
        menuName = "FantasyMayor/HexIcons/HexResourceIconConfig")]
    public sealed class HexResourceIconConfig : ScriptableObject
    {
        [Serializable]
        public struct ResourceIconEntry
        {
            [SerializeField] private ResourceType _resourceType;
            [SerializeField] private Sprite _sprite;

            public ResourceType ResourceType => _resourceType;
            public Sprite Sprite => _sprite;
        }

        [SerializeField] private ResourceIconEntry[] _entries;
        public IReadOnlyList<ResourceIconEntry> Entries => _entries;
    }
}
