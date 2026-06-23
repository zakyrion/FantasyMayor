using Core;
using Presentation.Icons.Configs;

namespace Presentation.Icons.Components
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
