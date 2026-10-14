using System;
using EcsExtensions;
using UnityEngine;

namespace Domains.Actors.Mayor.Configs
{
    [CreateAssetMenu(fileName = "MayorConfig", menuName = "FantasyMayor/Actors/MayorConfig")]
    public class MayorConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField]
        private int _startActionPoints;
        public int StartActionPoints => _startActionPoints;

        public void Validate()
        {
            if (StartActionPoints < 0)
                throw new InvalidOperationException(
                    $"MayorConfig: StartActionPoints must be >= 0, was {StartActionPoints}.");
        }
    }
}
