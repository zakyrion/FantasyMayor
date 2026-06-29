using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views.Binders
{
    /// <summary>
    ///     ДІЇ / ЕФЕКТ section — structural scaffold only. The district-action model is not built yet, so this
    ///     binder holds the placeholder note and is the seam where per-action rows will be cloned once the model
    ///     lands (capacity / actions / yield split / upkeep). Intentionally a no-op for now.
    /// </summary>
    internal sealed class DistrictActionsBinder
    {
        private readonly VisualElement _placeholder;

        public DistrictActionsBinder(VisualElement placeholder)
        {
            _placeholder = placeholder;
        }

        public void Bind()
        {
            // No model yet — the authored placeholder note carries the section. See class summary.
        }
    }
}
