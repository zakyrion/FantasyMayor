using Domains.Economy.DistrictBuild.Configs;

namespace Domains.Economy.DistrictBuild.Components
{
    // World component carrying a REFERENCE to the loaded DistrictsBuildConfig SO (the district catalogue).
    // No copy/flatten — the SO already holds the data; DistrictsBuildConfigLoaderSystem keeps the addressable
    // Box alive for the catalogue's lifetime and releases it on teardown. Read by DistrictBuildUISystem.
    public readonly struct DistrictBuildsConfigComponent
    {
        public readonly DistrictBuildsConfig Value;

        public DistrictBuildsConfigComponent(DistrictBuildsConfig value)
        {
            Value = value;
        }
    }
}
