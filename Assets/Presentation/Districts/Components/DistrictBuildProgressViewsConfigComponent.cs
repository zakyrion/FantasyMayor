using Friflo.Engine.ECS;
using Presentation.Districts.Configs;

namespace Presentation.Districts.Components
{
    // World component carrying a REFERENCE to the loaded DistrictBuildProgressViewsConfig SO (the progress-view
    // catalogue: one prefab per DistrictType). No copy/flatten — the SO already holds the data;
    // DistrictBuildProgressViewsConfigLoaderSystem keeps the addressable Box alive for the catalogue's lifetime
    // and releases it on teardown. Read by DistrictBuildProgressViewSpawnSystem to pick a prefab by DistrictType.
    public readonly struct DistrictBuildProgressViewsConfigComponent : IComponent
    {
        public readonly DistrictBuildProgressViewsConfig Value;

        public DistrictBuildProgressViewsConfigComponent(DistrictBuildProgressViewsConfig value)
        {
            Value = value;
        }
    }
}
