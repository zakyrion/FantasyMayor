using Core;
using Friflo.Engine.ECS;
using Presentation.HexIcons.Configs;

namespace Presentation.HexIcons.Components
{
    public readonly struct HexIconsConfigComponent : IComponent
    {
        private readonly Box<HexIconsConfig> _config;

        internal HexIconsConfig Value => _config.Value;

        internal HexIconsConfigComponent(Box<HexIconsConfig> config)
        {
            _config = config;
        }
    }
}
