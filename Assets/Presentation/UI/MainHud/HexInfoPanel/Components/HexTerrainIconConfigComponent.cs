using Core;
using Friflo.Engine.ECS;
using Presentation.UI.MainHud.HexInfoPanel.Configs;

namespace Presentation.UI.MainHud.HexInfoPanel.Components
{
    /// <summary>
    ///     Singleton component wrapping the loaded terrain-icon config. Holds the <see cref="Box{T}" /> so the
    ///     sprite assets stay loaded for the panel's lifetime (ownership is transferred here by the loader).
    /// </summary>
    public readonly struct HexTerrainIconConfigComponent : IComponent
    {
        private readonly Box<HexTerrainIconConfig> _config;

        public HexTerrainIconConfig Value => _config.Value;

        public HexTerrainIconConfigComponent(Box<HexTerrainIconConfig> config)
        {
            _config = config;
        }
    }
}
