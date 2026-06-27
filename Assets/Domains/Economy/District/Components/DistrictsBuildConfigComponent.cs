using Domains.Economy.District.Configs;

namespace Domains.Economy.District.Components
{
    // World component carrying a REFERENCE to the loaded DistrictsBuildConfig SO (the district catalogue).
    // No copy/flatten — the SO already holds the data; DistrictsBuildConfigLoaderSystem keeps the addressable
    // Box alive for the catalogue's lifetime and releases it on teardown. Read by DistrictBuildActionSystem.
    public readonly struct DistrictsBuildConfigComponent
    {
        public readonly DistrictsBuildConfig Value;

        public DistrictsBuildConfigComponent(DistrictsBuildConfig value)
        {
            Value = value;
        }
    }
}
