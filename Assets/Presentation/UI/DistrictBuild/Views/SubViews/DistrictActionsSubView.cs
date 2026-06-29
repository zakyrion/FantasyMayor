using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views.SubViews
{
    /// <summary>
    ///     ДІЇ / ЕФЕКТ panel — structural scaffold only. The district-action model is not built yet, so this
    ///     sub-view holds the placeholder note and is the seam where per-action rows will be cloned once the model
    ///     lands (capacity / actions / yield split / upkeep). Intentionally a no-op for now.
    /// </summary>
    internal sealed class DistrictActionsSubView
    {
        private readonly VisualElement _placeholder;

        public DistrictActionsSubView(VisualElement placeholder)
        {
            _placeholder = placeholder;
        }

        public void Bind()
        {
            // No model yet — the authored placeholder note carries the section. See class summary.
        }
    }
}
