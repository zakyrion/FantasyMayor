using Domains.Economy.DistrictBuildCost.Configs;
using Friflo.Engine.ECS;

namespace Domains.Economy.DistrictBuildCost.Components{
    // World component carrying a REFERENCE to the loaded BuildDistrictCostsConfig SO (the district-build cost
    // catalogue: AP + resource price per DistrictType). No copy/flatten — the SO already holds the data;
    // BuildDistrictCostsConfigLoaderSystem keeps the addressable Box alive for the catalogue's lifetime and
    // releases it on teardown. Read by DistrictBuildUISystem (joined to the Economy gating catalogue by DistrictType).
    public readonly struct DistrictBuildCostsConfigComponent : IComponent
    {
        public readonly DistrictBuildCostsConfig Value;

        public DistrictBuildCostsConfigComponent(DistrictBuildCostsConfig value)
        {
            Value = value;
        }
    }
}
