using Domains.Economy.Resource.Components;
using UnityEngine;

namespace Domains.Economy.Resource.Configs
{
    [CreateAssetMenu(fileName = "MayorConfig", menuName = "FantasyMayor/Economy/MayorConfig")]
    public class MayorConfig : ScriptableObject
    {
        [SerializeField]
        private int _startAPCount;
        [SerializeField]
        private ResourceComponent[] _resources;

        public int StartActionPoints => _startAPCount;
        public ResourceComponent[] Resources => _resources;
    }
}
