using Core;
using Presentation.HexIcons.Configs;

namespace Presentation.HexIcons.Components
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
