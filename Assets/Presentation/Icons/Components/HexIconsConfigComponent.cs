using Core;
using Presentation.Icons.Configs;

namespace Presentation.Icons.Components
{
    internal readonly struct HexIconsConfigComponent
    {
        private readonly Box<HexIconsConfig> _config;

        public HexIconsConfig Value => _config.Value;

        internal HexIconsConfigComponent(Box<HexIconsConfig> config)
        {
            _config = config;
        }
    }
}
