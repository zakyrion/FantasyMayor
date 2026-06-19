using Modules.AxialSystem;

namespace Modules.TerrainView.Components
{
    /// <summary>
    ///     Marks the currently selected hex in world space.
    ///     Expected to exist on at most one entity at a time.
    /// </summary>
    public struct HexSelectedComponent
    {
        /// <summary>Axial coordinates of the selected hex.</summary>
        public HexCoord Coords;
    }
}
