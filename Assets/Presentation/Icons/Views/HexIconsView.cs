using System;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.AppUI.UI.VisualElementExtensions;

namespace Presentation.Icons.Views
{
    internal sealed class HexIconsView : MonoBehaviour
    {
        private const string RootName = "hex-icons-root";
        private const string RaycastTransparentClass = "raycast-transparent";

        [SerializeField] private UIDocument _document;

        // Content root (`hex-icons-root`) the spawn system sizes and future steps fill with per-hex containers.
        public VisualElement Root
        {
            get
            {
                if (_document == null)
                    throw new InvalidOperationException("HexIconsView: UIDocument reference is not assigned.");

                var documentRoot = _document.rootVisualElement;
                if (documentRoot == null)
                    throw new InvalidOperationException("HexIconsView: UIDocument root visual element is not ready.");

                var content = documentRoot.Q(RootName);
                if (content == null)
                    throw new InvalidOperationException($"HexIconsView: '{RootName}' element was not found in the UXML.");

                return content;
            }
        }

        // Marker-class convention (mirrors HexesUI): elements tagged `raycast-transparent` in the UXML
        // get picking disabled so pointer rays pass through. The full-map world-space canvas must not
        // swallow world/hex clicks; icon children keep default picking.
        public void ApplyRaycastTransparent()
        {
            if (_document == null)
                throw new InvalidOperationException("HexIconsView: UIDocument reference is not assigned.");

            var documentRoot = _document.rootVisualElement;
            if (documentRoot == null)
                throw new InvalidOperationException("HexIconsView: UIDocument root visual element is not ready.");

            foreach (var element in documentRoot.Query(className: RaycastTransparentClass).ToList())
                element.EnablePicking(false);
        }

        // Applies the raycast-transparent marker to a single runtime-created element: adds the class (for
        // consistency with the UXML convention) and disables picking so pointer rays pass through. Use for
        // elements added after ApplyRaycastTransparent has already run — e.g. per-hex containers spawned
        // lazily by the spawn system.
        public void MakeRaycastTransparent(VisualElement element)
        {
            if (element == null)
                throw new InvalidOperationException("HexIconsView.MakeRaycastTransparent: element is null.");

            element.AddToClassList(RaycastTransparentClass);
            element.EnablePicking(false);
        }
    }
}
