using Core;
using Modules.HexIcons.Configs;

namespace Modules.HexIcons.Components
{
    public readonly struct HexResourceIconConfigComponent
    {
        private readonly Box<HexResourceIconConfig> _config;

        public HexResourceIconConfig Value => _config.Value;

        internal HexResourceIconConfigComponent(Box<HexResourceIconConfig> config)
        {
            _config = config;
        }
    }
}
