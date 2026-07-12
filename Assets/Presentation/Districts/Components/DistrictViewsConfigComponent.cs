using Presentation.Districts.Configs;

namespace Presentation.Districts.Components
{
    // World component carrying a REFERENCE to the loaded DistrictViewsConfig SO (the district view catalogue:
    // one prefab per DistrictType). No copy/flatten — the SO already holds the data; DistrictViewsConfigLoaderSystem
    // keeps the addressable Box alive for the catalogue's lifetime and releases it on teardown. Read by
    // DistrictViewSpawnSystem to pick a built district's prefab by DistrictType.
    public readonly struct DistrictViewsConfigComponent
    {
        public readonly DistrictViewsConfig Value;

        public DistrictViewsConfigComponent(DistrictViewsConfig value)
        {
            Value = value;
        }
    }
}
