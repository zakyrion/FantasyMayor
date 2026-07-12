using Domains.Economy.District.Data;
using UnityEngine;

namespace Domains.Economy.DistrictOpenCondition.Configs
{
    // Concrete condition: the gated district (DistrictType, from the base) becomes buildable once a district of
    // RequiredDistrict already exists. First concrete condition type; others follow the same shape.
    [CreateAssetMenu(
        fileName = "DistrictExistConditionConfig",
        menuName = "FantasyMayor/Districts/Conditions/DistrictExistConditionConfig")]
    public sealed class DistrictExistConditionConfig : DistrictOpenConditionConfig
    {
        [Header("District Exist Condition")]
        [SerializeField] private DistrictType _requiredDistrict;

        // The district type that must already exist for the gated district to be buildable.
        public DistrictType RequiredDistrict => _requiredDistrict;
    }
}
