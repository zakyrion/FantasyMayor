using EcsExtensions;
namespace Presentation.HexIcons.Events
{
    /// <summary>
    ///     Signal that the icon visibility state changed and the icons must be re-rendered. Pure signal —
    ///     carries no payload; the consumer reads <c>HexIconsVisibilityComponent</c> for the current state.
    ///     Lives in the event log until evicted past its ring capacity. Public so the producer (Boot assembly)
    ///     can raise it.
    /// </summary>
    public struct HexIconsVisibilityChangedEvent : IEventTag
    {
    }
}
