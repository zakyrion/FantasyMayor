using System;
using Domains.Economy.District.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views
{
    // District-list section of the build overlay, driven by DistrictBuildListUISubSystem (push-to-view: Clear,
    // then one AddDistrict per buildable district). Its own MonoBehaviour, bound to the shared PanelRenderer tree.
    // The row click is a local C# event (SelectionChanged) the subsystem translates into the ECS selection pulse —
    // the view stays World-free. Container (re)cached on reload; pushes before the first reload are no-ops.
    public sealed class DistrictBuildListUIView : MonoBehaviour
    {
        [SerializeField] private PanelRenderer _renderer;
        [SerializeField] private VisualTreeAsset _rowTemplate;
        [SerializeField] private string _listContainerName = "DistrictList";

        private VisualElement _container;

        public event Action<DistrictType> SelectionChanged;

        public void Clear()
        {
            _container?.Clear();
        }

        public void AddDistrict(DistrictType district, bool selected)
        {
            if (_container == null)
                return;

            var item = _rowTemplate.Instantiate();
            var row = item.Q<VisualElement>("DistrictRow") ?? item;
            if (selected)
                row.AddToClassList("drow--active");

            var icon = item.Q<Label>("Icon");
            if (icon != null)
                icon.text = DistrictBuildLabels.DistrictIcon(district);

            var nameLabel = item.Q<Label>("Name");
            if (nameLabel != null)
                nameLabel.text = DistrictBuildLabels.DistrictName(district);

            row.RegisterCallback<ClickEvent>(_ => SelectionChanged?.Invoke(district));
            _container.Add(item);
        }

        private void OnEnable()
        {
            // Fail loud at init (NOT in the reload callback — a throw there blanks every UIDocument). Mirrors
            // DistrictBuildUIView.
            if (_renderer == null)
                throw new InvalidOperationException(
                    "DistrictBuildListUIView: PanelRenderer is not assigned on the prefab.");
            if (_rowTemplate == null)
                throw new InvalidOperationException(
                    "DistrictBuildListUIView: the district-row VisualTreeAsset template is not assigned on the prefab.");

            _renderer.RegisterUIReloadCallback(OnUiReloaded);
        }

        private void OnDisable()
        {
            if (_renderer != null)
                _renderer.UnregisterUIReloadCallback(OnUiReloaded);
        }

        private void OnUiReloaded(PanelRenderer renderer, VisualElement root)
        {
            _container = root.Q<VisualElement>(_listContainerName);
        }
    }
}
