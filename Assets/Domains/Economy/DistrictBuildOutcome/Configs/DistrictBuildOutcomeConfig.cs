using Domains.Economy.District.Data;
using UnityEngine;

namespace Domains.Economy.DistrictBuildOutcome.Configs{
    // Abstract base for a single build-district outcome ("what happens when a build action for a district type
    // completes"). Concrete outcomes differ per kind, so each is its own ScriptableObject subclass with its own
    // payload. The base carries only what every outcome shares: the district type this outcome belongs to.
    // Authoring artifact only — the spawn pipeline turns each concrete config into its own entity (a lookup row
    // joined by DistrictType when a build action completes).
    public abstract class DistrictBuildOutcomeConfig : ScriptableObject
    {
        [Header("Build District Outcome")]
        [SerializeField] private DistrictType _districtType;

        // The district type this outcome belongs to (the join key against a completed build action).
        public DistrictType DistrictType => _districtType;
    }
}
