using System.Collections.Generic;
using Domains.Economy.District.Data;
using Domains.Economy.Resource.Components;
using UnityEngine;

namespace Domains.Actions.Configs
{
    [CreateAssetMenu(fileName = "ActionsDistrictBuildConfig", menuName = "FantasyMayor/Actions/ActionsDistrictBuildConfig")]
    public class ActionsDistrictBuildConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictType _districtType;
        [SerializeField]
        private List<ResourceComponent> _districtPrices;
        [SerializeField]
        private int _apPrice;
        [SerializeField][Range(1,100)]
        private int _turnsToBuild;

        public DistrictType DistrictType => _districtType;
        public List<ResourceComponent> DistrictPrices => _districtPrices;
        public int ApPrice => _apPrice;
        public int TurnsToBuild => _turnsToBuild;
    }
}
