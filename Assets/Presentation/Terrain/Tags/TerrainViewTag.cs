using Friflo.Engine.ECS;
namespace Presentation.Terrain.Tags
{
    // Discriminator: present on every TerrainView entity; filters must combine it with the key component.
    public struct TerrainViewTag : ITag
    {
    }
}
