using Domains.Economy.District.Configs;
using Domains.Map.HexResources.Data;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views.SubViews
{
    /// <summary>
    ///     ВИМОГИ panel (display only). Emits one <c>ReqLine</c> per ACTIVE requirement dimension, each with its
    ///     own ✓/✕. The per-line marks mirror the <c>CanBuildOn</c> predicates (terrain blacklist / whitelist +
    ///     resource / empty-hex), so they can never disagree with the overall
    ///     <see cref="IDistrictBuildData.IsAvailable" /> gate. Dimensions left unset in the config render nothing.
    /// </summary>
    internal sealed class DistrictRequirementsSubView
    {
        private readonly VisualElement _container;
        private readonly VisualTreeAsset _lineTemplate;
        private readonly IDistrictBuildData _data;

        public DistrictRequirementsSubView(VisualElement container, VisualTreeAsset lineTemplate,
            IDistrictBuildData data)
        {
            _container = container;
            _lineTemplate = lineTemplate;
            _data = data;
        }

        public void Bind(DistrictBuildingConfig district)
        {
            _container.Clear();
            var hexType = _data.SelectedHexType;

            if (district.ImpossibleToBuildTypes.Count > 0)
                AddLine(!district.ImpossibleToBuildTypes.Contains(hexType),
                    $"Заборонений терен: {DistrictBuildLabels.HexTypeLabels(district.ImpossibleToBuildTypes)}");

            if (district.NeedEmptyHexResourcesToBuild)
                AddLine(_data.HexResources.Length == 0, "Гекс без ресурсів");
            else if (district.RequiredHexResourceType != HexResourceType.Unknown)
                AddLine(HexHasResource(district.RequiredHexResourceType),
                    $"Потрібен ресурс: {DistrictBuildLabels.HexResourceLabel(district.RequiredHexResourceType)}");
        }

        private void AddLine(bool met, string text)
        {
            var item = _lineTemplate.Instantiate();
            var label = item.Q<Label>("Line");
            label.text = (met ? "✓ " : "✕ ") + text;
            label.AddToClassList(met ? "req-line--ok" : "req-line--no");
            _container.Add(item);
        }

        private bool HexHasResource(HexResourceType type)
        {
            var resources = _data.HexResources;
            for (var i = 0; i < resources.Length; i++)
                if (resources[i] == type)
                    return true;

            return false;
        }
    }
}
