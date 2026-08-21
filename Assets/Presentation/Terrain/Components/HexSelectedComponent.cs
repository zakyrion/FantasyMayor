using Friflo.Engine.ECS;
using Modules.AxialSystem;

namespace Presentation.Terrain.Components
{
    /// <summary>
    ///     Marks the currently selected hex in world space.
    ///     Expected to exist on at most one entity at a time. ABSENCE means "nothing selected" —
    ///     consumers handle the no-entity case; there is no null/sentinel value. Selection is a toggle:
    ///     clicking the selected hex disposes the entity.
    /// </summary>
    public struct HexSelectedComponent : IComponent
    {
        /// <summary>Axial coordinates of the selected hex.</summary>
        public HexCoord Coords;
    }
}
