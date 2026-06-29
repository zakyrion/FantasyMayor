using Domains.Actions.Configs;

namespace Domains.Actions.Components
{
    // World component carrying a REFERENCE to the loaded ActionsDistrictsBuildConfig SO (the district-build cost
    // catalogue: AP + resource price per DistrictType). No copy/flatten — the SO already holds the data;
    // ActionsDistrictsBuildConfigLoaderSystem keeps the addressable Box alive for the catalogue's lifetime and
    // releases it on teardown. Read by DistrictBuildUISystem (joined to the Economy gating catalogue by DistrictType).
    public readonly struct ActionsDistrictsBuildConfigComponent
    {
        public readonly ActionsDistrictsBuildConfig Value;

        public ActionsDistrictsBuildConfigComponent(ActionsDistrictsBuildConfig value)
        {
            Value = value;
        }
    }
}
