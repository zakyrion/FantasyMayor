using UnityEngine.UIElements;

namespace Presentation.Icons.Components
{
    // One hex's icon container, the visual half of the icon-container entity
    // (HexIdComponent FK + this). VisualElement is a managed reference.
    internal readonly struct HexIconContainerComponent
    {
        public readonly VisualElement Container;

        internal HexIconContainerComponent(VisualElement container)
        {
            Container = container;
        }
    }
}
