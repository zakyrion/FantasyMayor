using Domains.Actors.Mayor.Configs;
using Domains.Economy.Resource.Components;

namespace Domains.Actors.Mayor.Components
{
    // World component: flattened MayorConfig (CONFIGTEMPLATE block 2). Carries the Mayor's authored
    // starting inventory loadout (a sparse set of entries — ResourceTypes the author omits start at 0)
    // and the Mayor's starting Action Points. Both are consumed by MayorSpawnSystem at map creation:
    // Resources seed the per-ResourceType inventory loadout; StartActionPoints seeds BOTH the per-turn restore
    // amount (MayorAPRestoreComponent) and the starting ActionPoint resource stack (live AP pool) — see ACTORS.md.
    // The array is copied out of the SO so the loader can release the addressable after flattening.
    public struct MayorConfigComponent
    {
        public ResourceComponent[] Resources;
        public int StartActionPoints;

        public static MayorConfigComponent FromConfig(MayorConfig config)
        {
            return new MayorConfigComponent
            {
                Resources = (ResourceComponent[])config.Resources.Clone(),
                StartActionPoints = config.StartActionPoints
            };
        }
    }
}
