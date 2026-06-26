using Domains.Economy.Resource.Components;
using UnityEngine;

namespace Domains.Actors.City.Configs
{
    [CreateAssetMenu(fileName = "CityConfig", menuName = "FantasyMayor/Actors/CityConfig")]
    public class CityConfig : ScriptableObject
    {
        [SerializeField]
        private ResourceComponent[] _resources;

        public ResourceComponent[] Resources => _resources;
    }
}
