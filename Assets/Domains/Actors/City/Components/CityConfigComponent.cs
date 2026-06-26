using Domains.Actors.City.Configs;
using Domains.Economy.Resource.Components;

namespace Domains.Actors.City.Components
{
    // World component: flattened CityConfig (CONFIGTEMPLATE block 2). Carries the City's authored starting
    // inventory loadout (a sparse set of entries — ResourceTypes the author omits start at 0). Consumed by
    // CitySpawnSystem at map creation to seed the City's per-ResourceType loadout (see ACTORS.md). The City
    // has no Action Points, so — unlike MayorConfigComponent — this carries resources only.
    // The array is copied out of the SO so the loader can release the addressable after flattening.
    public struct CityConfigComponent
    {
        public ResourceComponent[] Resources;

        public static CityConfigComponent FromConfig(CityConfig config)
        {
            return new CityConfigComponent
            {
                Resources = (ResourceComponent[])config.Resources.Clone()
            };
        }
    }
}
