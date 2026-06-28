using System;
using Domains.Map.HexResources.Configs;

namespace Domains.Map.HexResources.Data
{
    [Serializable]
    internal struct HexResourcesConfigEntry
    {
        public HexResourceType Type;
        public ResourceConfig Config;
    }
}
