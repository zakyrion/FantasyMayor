using Core;
using Presentation.UI.ResourceBar.Configs;

namespace Presentation.UI.ResourceBar.Components
{
    /// <summary>
    ///     World component wrapping the loaded inventory-resource-icon config. Holds the <see cref="Box{T}" /> so
    ///     the sprite assets stay loaded for the resource strip's lifetime (ownership is transferred here by the
    ///     loader). Mirrors HexTerrainIconConfigComponent.
    /// </summary>
    public readonly struct InventoryResourceIconConfigComponent
    {
        private readonly Box<InventoryResourceIconConfig> _config;

        public InventoryResourceIconConfig Value => _config.Value;

        public InventoryResourceIconConfigComponent(Box<InventoryResourceIconConfig> config)
        {
            _config = config;
        }
    }
}
