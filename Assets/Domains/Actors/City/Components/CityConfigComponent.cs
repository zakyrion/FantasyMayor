using Domains.Actors.City.Configs;
using Domains.Economy.Resource.Data;
using Friflo.Engine.ECS;

namespace Domains.Actors.City.Components
{
    // Singleton component: flattened CityConfig. Carries the City's authored starting inventory loadout
    // (a sparse set of entries — ResourceTypes the author omits start at 0). Consumed by CitySpawnSystem
    // at map creation to seed the City's per-ResourceType loadout. The City has no Action Points,
    // so — unlike MayorConfigComponent — this carries resources only.
    // The array is copied out of the SO so the loader can release the addressable after flattening.
    public struct CityConfigComponent : IComponent
    {
        public ResourceAmount[] Resources;

        public static CityConfigComponent FromConfig(CityConfig config)
        {
            return new CityConfigComponent
            {
                Resources = (ResourceAmount[])config.Resources.Clone()
            };
        }
    }
}
