using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views
{
    // ВИМОГИ section of the build overlay: the selected district's name header + one requirement line per active
    // gate dimension (✓/✕). Driven by DistrictBuildHexResourcesUISubSystem (push-to-view: SetDistrictName, then
    // Clear + one AddRequirement per dimension). Bound to the shared PanelRenderer tree; elements (re)cached on
    // reload (mirrors DistrictBuildUIView). Pushes before the first reload are no-ops (async-panel reality).
    public sealed class DistrictBuildHexResourcesUIView : MonoBehaviour
    {
        [SerializeField] private PanelRenderer _renderer;
        [SerializeField] private VisualTreeAsset _reqLineTemplate;
        [SerializeField] private string _detailNameName = "DetailName";
        [SerializeField] private string _reqLinesName = "ReqLines";

        private Label _detailName;
        private VisualElement _reqLines;

        public void SetDistrictName(string districtName)
        {
            if (_detailName != null)
                _detailName.text = districtName;
        }

        public void ClearRequirements()
        {
            _reqLines?.Clear();
        }

        public void AddRequirement(string text, bool met)
        {
            if (_reqLines == null)
                return;

            var item = _reqLineTemplate.Instantiate();
            var label = item.Q<Label>("Line");
            label.text = (met ? "✓ " : "✕ ") + text;
            label.AddToClassList(met ? "req-line--ok" : "req-line--no");
            _reqLines.Add(item);
        }

        private void OnEnable()
        {
            // Fail loud at init (NOT in the reload callback — a throw there blanks every UIDocument). Mirrors
            // DistrictBuildUIView.
            if (_renderer == null)
                throw new InvalidOperationException(
                    "DistrictBuildHexResourcesUIView: PanelRenderer is not assigned on the prefab.");
            if (_reqLineTemplate == null)
                throw new InvalidOperationException(
                    "DistrictBuildHexResourcesUIView: the ReqLine VisualTreeAsset template is not assigned on the prefab.");

            _renderer.RegisterUIReloadCallback(OnUiReloaded);
        }

        private void OnDisable()
        {
            if (_renderer != null)
                _renderer.UnregisterUIReloadCallback(OnUiReloaded);
        }

        private void OnUiReloaded(PanelRenderer renderer, VisualElement root)
        {
            _detailName = root.Q<Label>(_detailNameName);
            _reqLines = root.Q<VisualElement>(_reqLinesName);
        }
    }
}
