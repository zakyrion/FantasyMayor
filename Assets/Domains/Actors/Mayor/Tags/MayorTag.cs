using Friflo.Engine.ECS;
namespace Domains.Actors.Mayor.Tags
{
    // Discriminator: present on every Mayor entity; filters must combine it with the key component.
    public struct MayorTag : ITag
    {
    }
}
