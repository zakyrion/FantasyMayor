using Friflo.Engine.ECS;

namespace EcsExtensions
{
    /// <summary>
    ///     Marks an ECS entity as a one-frame event. See <see cref="EcsEventExtensions" /> for the
    ///     create/ripen/cleanup lifecycle.
    /// </summary>
    public struct EventTag : ITag
    {
    }
}
