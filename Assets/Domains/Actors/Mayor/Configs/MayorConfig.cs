using Domains.Economy.Resource.Data;
using UnityEngine;

namespace Domains.Actors.Mayor.Configs
{
    [CreateAssetMenu(fileName = "MayorConfig", menuName = "FantasyMayor/Actors/MayorConfig")]
    public class MayorConfig : ScriptableObject
    {
        [SerializeField]
        private int _startActionPoints;
        [SerializeField]
        private ResourceAmount[] _resources;

        public ResourceAmount[] Resources => _resources;
        public int StartActionPoints => _startActionPoints;
    }
}
