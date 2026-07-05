using Domains.Economy.Resource.Data;
using UnityEngine;

namespace Domains.Actors.City.Configs
{
    [CreateAssetMenu(fileName = "CityConfig", menuName = "FantasyMayor/Actors/CityConfig")]
    public class CityConfig : ScriptableObject
    {
        [SerializeField]
        private ResourceAmount[] _resources;

        public ResourceAmount[] Resources => _resources;
    }
}
