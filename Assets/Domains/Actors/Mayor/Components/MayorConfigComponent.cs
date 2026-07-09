using Domains.Actors.Mayor.Configs;
using Domains.Economy.Resource.Data;

namespace Domains.Actors.Mayor.Components
{
    // World component: flattened MayorConfig. Carries the Mayor's authored
    // starting inventory loadout (a sparse set of entries — ResourceTypes the author omits start at 0)
    // and the Mayor's starting Action Points. Both are consumed by MayorSpawnSystem at map creation:
    // Resources seed the per-ResourceType inventory loadout; StartActionPoints seeds BOTH the per-turn restore
    // amount (MayorAPRestoreComponent) and the starting ActionPoint resource stack (live AP pool).
    // The array is copied out of the SO so the loader can release the addressable after flattening.
    public struct MayorConfigComponent
    {
        public ResourceAmount[] Resources;
        public int StartActionPoints;

        public static MayorConfigComponent FromConfig(MayorConfig config)
        {
            return new MayorConfigComponent
            {
                Resources = (ResourceAmount[])config.Resources.Clone()
            };
        }
    }
}
