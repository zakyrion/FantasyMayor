using Domains.Economy.District.Data;
using UnityEngine;

namespace Domains.Economy.DistrictOpenCondition.Configs
{
    // Abstract base for a single district-open condition ("how to unblock building of a district type").
    // Concrete conditions differ widely, so each is its own ScriptableObject subclass with its own payload.
    // The base carries only what every condition shares: the district type this condition gates.
    // Authoring artifact only — the spawn pipeline turns each concrete config into its own entity.
    public abstract class DistrictOpenConditionConfig : ScriptableObject
    {
        [Header("District Open Condition")]
        [SerializeField] private DistrictType _districtType;

        // The district type that this condition unblocks for building.
        public DistrictType DistrictType => _districtType;
    }
}
