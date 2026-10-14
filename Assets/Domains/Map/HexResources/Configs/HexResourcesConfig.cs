using System;
using Domains.Map.HexResources.Data;
using UnityEngine;
using EcsExtensions;
using Unity.Collections;

namespace Domains.Map.HexResources.Configs
{
    [CreateAssetMenu(fileName = "HexResourcesConfig", menuName = "FantasyMayor/HexResources/HexResourcesConfig")]
    internal sealed class HexResourcesConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField] private HexResourcesConfigEntry[] _resources = Array.Empty<HexResourcesConfigEntry>();

        public HexResourcesConfigEntry[] Resources => _resources ?? Array.Empty<HexResourcesConfigEntry>();

        public void Validate()
        {
            // NativeHashSet keys require IEquatable<T>, which enums lack — key on the underlying int.
            var seenTypes = new NativeHashSet<int>(8, Allocator.Temp);
            try
            {
                foreach (var resource in Resources)
                {
                    if (resource.Config == null)
                        throw new Exception($"Game resources config contains null config for resource type '{resource.Type}'.");

                    if (!seenTypes.Add((int)resource.Type))
                        throw new Exception($"Game resources config contains duplicate resource type '{resource.Type}'.");
                }
            }
            finally
            {
                seenTypes.Dispose();
            }
        }
    }
}
