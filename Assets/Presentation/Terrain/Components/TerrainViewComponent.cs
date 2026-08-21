using Friflo.Engine.ECS;
namespace Presentation.Terrain.Components
{
    public struct TerrainViewComponent : IComponent
    {
        public bool Active;
        public Views.TerrainView ObjectRef;
    }
}