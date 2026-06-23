using Domains.Economy.Resource.Configs;

namespace Domains.Economy.Resource.Components
{
    // World component: flattened MayorConfig (CONFIGTEMPLATE block 2). Carries the Mayor's authored
    // starting inventory loadout (a sparse set of entries — ResourceTypes the author omits start at 0)
    // and the Mayor's starting Action Points. StartActionPoints is published here but NOT yet applied to
    // any Mayor entity: no Action Points system exists yet (see ECONOMY.md). The array is copied out of
    // the SO so the loader can release the addressable after flattening.
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
