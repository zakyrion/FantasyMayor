using Friflo.Engine.ECS;
namespace Modules.UserInput.Tags
{
    // Discriminator: present on every PlayerInput entity; filters must combine it with the key component.
    public struct PlayerInputTag : ITag
    {
    }
}
