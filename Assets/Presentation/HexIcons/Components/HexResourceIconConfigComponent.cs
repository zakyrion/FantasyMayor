using Core;
using Friflo.Engine.ECS;
using Presentation.HexIcons.Configs;

namespace Presentation.HexIcons.Components
{
    public readonly struct HexResourceIconConfigComponent : IComponent
    {
        private readonly Box<HexResourceIconConfig> _config;

        public HexResourceIconConfig Value => _config.Value;

        internal HexResourceIconConfigComponent(Box<HexResourceIconConfig> config)
        {
            _config = config;
        }
    }
}
