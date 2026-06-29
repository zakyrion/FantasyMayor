using System;
using Domains.Economy.District.Configs;
using Domains.Economy.District.Data;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views.Binders
{
    /// <summary>
    ///     Master list (left column). Clones a <c>DistrictRow</c> template per roster district into the list
    ///     ScrollView and binds icon / name / availability status / AP pill, marking the active + locked rows.
    ///     A row click reports its index through <c>onSelect</c>; the view re-binds list + detail.
    /// </summary>
    internal sealed class DistrictListBinder
    {
        private readonly VisualElement _container;
        private readonly VisualTreeAsset _rowTemplate;
        private readonly IDistrictBuildData _data;
        private readonly Action<int> _onSelect;

        public DistrictListBinder(VisualElement container, VisualTreeAsset rowTemplate, IDistrictBuildData data,
            Action<int> onSelect)
        {
            _container = container;
            _rowTemplate = rowTemplate;
            _data = data;
            _onSelect = onSelect;
        }

        public void Bind(DistrictsBuildConfig config, int selected)
        {
            _container.Clear();
            var districts = config?.Districts;
            if (districts == null)
                return;

            for (var index = 0; index < districts.Length; index++)
            {
                var district = districts[index];
                if (district == null || district.DistrictType == DistrictType.Unknown)
                    continue;

                var available = _data.IsAvailable(district);
                var rowIndex = index;

                var item = _rowTemplate.Instantiate();
                var row = item.Q<VisualElement>("DistrictRow");
                if (index == selected)
                    row.AddToClassList("drow--active");
                if (!available)
                    row.AddToClassList("drow--locked");

                item.Q<Label>("Icon").text = DistrictBuildLabels.DistrictIcon(district.DistrictType);
                item.Q<Label>("Name").text = DistrictBuildLabels.DistrictName(district.DistrictType);

                var status = item.Q<Label>("Status");
                status.text = available ? "✓ доступно" : "✕ вимога не виконана";
                status.AddToClassList(available ? "drow-req--ok" : "drow-req--no");

                item.Q<Label>("ApPill").text = $"{_data.CostFor(district.DistrictType).ApPrice} AP";

                row.RegisterCallback<ClickEvent>(_ => _onSelect(rowIndex));
                _container.Add(item);
            }
        }
    }
}
