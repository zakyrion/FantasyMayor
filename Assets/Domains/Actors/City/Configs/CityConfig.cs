using Domains.Economy.Resource.Data;
using UnityEngine;
using System;
using EcsExtensions;
using Unity.Collections;

namespace Domains.Actors.City.Configs
{
    [CreateAssetMenu(fileName = "CityConfig", menuName = "FantasyMayor/Actors/CityConfig")]
    public class CityConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField]
        private ResourceAmount[] _resources;

        public ResourceAmount[] Resources => _resources;

        public void Validate()
        {
            if (Resources == null)
                throw new InvalidOperationException("CityConfig: Resources array is null.");

            // NativeHashSet keys require IEquatable<T>, which enums lack — key on the underlying int.
            var seenTypes = new NativeHashSet<int>(4, Allocator.Temp);
            try
            {
                foreach (var resource in Resources)
                {
                    if (resource.Type == ResourceType.Unknown)
                        throw new InvalidOperationException(
                            "CityConfig: Resources contains an entry with ResourceType.Unknown.");

                    if (!seenTypes.Add((int)resource.Type))
                        throw new InvalidOperationException(
                            $"CityConfig: duplicate ResourceType '{resource.Type}' in Resources.");
                }
            }
            finally
            {
                seenTypes.Dispose();
            }
        }
    }
}
