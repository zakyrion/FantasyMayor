using Core;
using Friflo.Engine.ECS;
using Presentation.UI.MainHud.ResourceBar.Configs;

namespace Presentation.UI.MainHud.ResourceBar.Components
{
    /// <summary>
    ///     Singleton component wrapping the loaded inventory-resource-icon config. Holds the <see cref="Box{T}" /> so
    ///     the sprite assets stay loaded for the resource strip's lifetime (ownership is transferred here by the
    ///     loader). Mirrors HexTerrainIconConfigComponent.
    /// </summary>
    public readonly struct InventoryResourceIconConfigComponent : IComponent
    {
        private readonly Box<InventoryResourceIconConfig> _config;

        public InventoryResourceIconConfig Value => _config.Value;

        public InventoryResourceIconConfigComponent(Box<InventoryResourceIconConfig> config)
        {
            _config = config;
        }
    }
}
