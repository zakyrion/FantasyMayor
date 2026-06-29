using System;
using Domains.Economy.District.Configs;
using Domains.Economy.District.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views.SubViews
{
    /// <summary>
    ///     Master list panel. Owns the selected-row state and clones a <c>DistrictRow</c> per roster district into
    ///     the list ScrollView (icon / name / availability / AP pill, marking the active + locked rows). A row click
    ///     updates the selection and raises <see cref="SelectionChanged" />; the coordinator re-binds the list
    ///     (to move the highlight) and the detail. <see cref="SelectedIndex" /> is the canonical selection.
    /// </summary>
    internal sealed class DistrictListSubView
    {
        private readonly VisualElement _container;
        private readonly VisualTreeAsset _rowTemplate;
        private readonly IDistrictBuildData _data;
        private int _selected;

        public event Action<int> SelectionChanged;

        public DistrictListSubView(VisualElement container, VisualTreeAsset rowTemplate, IDistrictBuildData data)
        {
            _container = container;
            _rowTemplate = rowTemplate;
            _data = data;
        }

        public int SelectedIndex => _selected;

        public void Bind(DistrictsBuildConfig config)
        {
            _container.Clear();
            var districts = config?.Districts;
            if (districts == null)
                return;

            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, districts.Length - 1));

            for (var index = 0; index < districts.Length; index++)
            {
                var district = districts[index];
                if (district == null || district.DistrictType == DistrictType.Unknown)
                    continue;

                var available = _data.IsAvailable(district);
                var rowIndex = index;

                var item = _rowTemplate.Instantiate();
                var row = item.Q<VisualElement>("DistrictRow");
                if (index == _selected)
                    row.AddToClassList("drow--active");
                if (!available)
                    row.AddToClassList("drow--locked");

                item.Q<Label>("Icon").text = DistrictBuildLabels.DistrictIcon(district.DistrictType);
                item.Q<Label>("Name").text = DistrictBuildLabels.DistrictName(district.DistrictType);

                var status = item.Q<Label>("Status");
                status.text = available ? "✓ доступно" : "✕ вимога не виконана";
                status.AddToClassList(available ? "drow-req--ok" : "drow-req--no");

                item.Q<Label>("ApPill").text = $"{_data.CostFor(district.DistrictType).ApPrice} AP";

                row.RegisterCallback<ClickEvent>(_ => Select(rowIndex));
                _container.Add(item);
            }
        }

        private void Select(int index)
        {
            if (_selected == index)
                return;

            _selected = index;
            SelectionChanged?.Invoke(index);
        }
    }
}
