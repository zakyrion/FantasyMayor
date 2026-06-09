using System;
using Modules.HexResources.Configs;

namespace Modules.HexResources.Data
{
    [Serializable]
    internal struct HexResourcesConfigEntry
    {
        public ResourceType Type;
        public ResourceConfig Config;
    }
}
