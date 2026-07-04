namespace Domains.Economy.DistrictBuildOutcome.Tags{
    // Kind marker on an outcome entity: this outcome spawns the City Center district when a build action for it
    // completes. Parameter-less today — when SpawnCityCenterOutcomeConfig grows City-Center-specific consequence
    // fields, those move onto the entity as a payload component (per the Polymorphic Config Catalogue rule).
    public struct SpawnCityCenterOutcomeTag
    {
    }
}
