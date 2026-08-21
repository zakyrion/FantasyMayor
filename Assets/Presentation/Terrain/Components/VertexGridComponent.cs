using Domains.Map.Hex.Utils;
using Friflo.Engine.ECS;

namespace Presentation.Terrain.Components
{
    /// <summary>Holds a reference to the shared <see cref="VertexGrid"/> instance.</summary>
    public struct VertexGridComponent : IComponent
    {
        public VertexGrid Grid;
    }
}
