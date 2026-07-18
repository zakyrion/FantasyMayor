using Friflo.Engine.ECS;

namespace EcsExtensions
{
    /// <summary>Stamps the Unity frame an event entity was created on; see <see cref="EcsEventExtensions" />.</summary>
    public struct EventFrameComponent : IComponent
    {
        public int Frame;
    }
}
