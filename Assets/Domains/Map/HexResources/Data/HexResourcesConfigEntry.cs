using System;
using Domains.Map.HexResources.Configs;

namespace Domains.Map.HexResources.Data
{
    [Serializable]
    internal struct HexResourcesConfigEntry
    {
        public ResourceType Type;
        public ResourceConfig Config;
    }
}
