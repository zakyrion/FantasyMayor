using Friflo.Engine.ECS;
namespace Presentation.Terrain.Tags
{
    // Discriminator: present on every HexSelection entity; filters must combine it with the key component.
    public struct HexSelectionTag : ITag
    {
    }
}
