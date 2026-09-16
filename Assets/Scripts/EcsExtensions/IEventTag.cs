using Friflo.Engine.ECS;

namespace EcsExtensions
{
    /// <summary>
    ///     Marks a struct as the payload of a log event. The type itself is the event's discriminator — the
    ///     ring it lives in, its capacity and its AOT declaration all key on this type, not on a separate tag.
    ///     See <see cref="EventLog" /> and <see cref="EventReader{TEvent}" />.
    /// </summary>
    public interface IEventTag : IComponent
    {
    }
}
