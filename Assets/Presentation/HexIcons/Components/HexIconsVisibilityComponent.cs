using Friflo.Engine.ECS;
namespace Presentation.HexIcons.Components
{
    /// <summary>
    ///     Mutable world-level state: whether per-hex resource icons are currently shown. The player toggles
    ///     it (via UI later); the producer writes it, then raises a <c>HexIconsVisibilityChangedEvent</c> so
    ///     the consumer re-renders. Public so the producer (Boot assembly) can write it.
    /// </summary>
    public readonly struct HexIconsVisibilityComponent : IComponent
    {
        public readonly bool IsVisible;

        public HexIconsVisibilityComponent(bool isVisible)
        {
            IsVisible = isVisible;
        }
    }
}
