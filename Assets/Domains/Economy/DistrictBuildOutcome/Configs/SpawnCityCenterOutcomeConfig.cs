using UnityEngine;
using Domains.Economy.DistrictBuildOutcome.Configs;

namespace Domains.Economy.DistrictBuildOutcome.Configs{
    // Concrete outcome for the City Center district: when a build action for CityCenter completes, spawn the
    // City Center district (plus any City-Center-specific consequences). Its DistrictType key (from the base) is
    // CityCenter. This is the per-district outcome contract — each district gets its OWN Spawn<District>Outcome
    // subclass carrying that district's UNIQUE consequence config, handled by its OWN subsystem (Open-Closed:
    // a new district plugs in a new config + subsystem without touching the orchestrator).
    // Parameter-less for now; City-Center-specific consequence fields go here as they are designed.
    [CreateAssetMenu(
        fileName = "SpawnCityCenterOutcomeConfig",
        menuName = "FantasyMayor/Districts/Outcomes/SpawnCityCenterOutcomeConfig")]
    public sealed class SpawnCityCenterOutcomeConfig : DistrictBuildOutcomeConfig
    {
    }
}
