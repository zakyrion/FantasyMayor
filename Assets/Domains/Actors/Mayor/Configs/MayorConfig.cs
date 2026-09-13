using Domains.Economy.Resource.Data;
using UnityEngine;
using System;
using EcsExtensions;

namespace Domains.Actors.Mayor.Configs
{
    [CreateAssetMenu(fileName = "MayorConfig", menuName = "FantasyMayor/Actors/MayorConfig")]
    public class MayorConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField]
        private int _startActionPoints;
        [SerializeField]
        private ResourceAmount[] _resources;

        public ResourceAmount[] Resources => _resources;
        public int StartActionPoints => _startActionPoints;

        public void Validate()
        {
            if (Resources == null)
                throw new InvalidOperationException("MayorConfig: Resources array is null.");

            if (StartActionPoints < 0)
                throw new InvalidOperationException(
                    $"MayorConfig: StartActionPoints must be >= 0, was {StartActionPoints}.");
        }
    }
}
