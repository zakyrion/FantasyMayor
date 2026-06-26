namespace Presentation.HexIcons.Events
{
    /// <summary>
    ///     One-frame signal that the icon visibility state changed and the icons must be re-rendered.
    ///     Pure signal — carries no payload; the consumer reads <c>HexIconsVisibilityComponent</c> for the
    ///     current state. Lives one frame on an entity tagged with <c>EventTag</c> (disposed by
    ///     <c>EventCleanupSystem</c>). Public so the producer (Boot assembly) can raise it.
    /// </summary>
    public struct HexIconsVisibilityChangedEvent
    {
    }
}
