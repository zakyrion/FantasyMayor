using System;
using System.Collections.Generic;
using Domains.Map.HexResources.Data;
using UnityEngine;
using EcsExtensions;

namespace Presentation.HexIcons.Configs
{
    [CreateAssetMenu(fileName = "HexResourceIconConfig",
        menuName = "FantasyMayor/HexIcons/HexResourceIconConfig")]
    public sealed class HexResourceIconConfig : ScriptableObject, IValidatableConfig
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

        public void Validate()
        {
            if (Entries == null || Entries.Count == 0)
                throw new InvalidOperationException("HexResourceIconConfig: Entries is null or empty.");
        }
    }
}
