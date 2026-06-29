using Domains.Economy.District.Configs;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views.SubViews
{
    /// <summary>
    ///     БУДІВНИЦТВО panel (display only). Binds the static AP row (cost vs the Mayor's pool — AP is always
    ///     Mayor-paid regardless of the resource payer) and clones a <c>CostRow</c> per resource price, comparing
    ///     each against the payer passed in by the coordinator and marking shortfalls.
    /// </summary>
    internal sealed class DistrictCostSubView
    {
        private readonly VisualElement _apRow;
        private readonly Label _apNeed;
        private readonly Label _apHave;
        private readonly VisualElement _costRows;
        private readonly VisualTreeAsset _costRowTemplate;
        private readonly IDistrictBuildData _data;

        public DistrictCostSubView(VisualElement apRow, VisualElement costRows, VisualTreeAsset costRowTemplate,
            IDistrictBuildData data)
        {
            _apRow = apRow;
            _apNeed = apRow.Q<Label>("ApNeed");
            _apHave = apRow.Q<Label>("ApHave");
            _costRows = costRows;
            _costRowTemplate = costRowTemplate;
            _data = data;
        }

        public void Bind(DistrictBuildingConfig district, Payer payer)
        {
            var cost = _data.CostFor(district.DistrictType);
            BindRow(_apRow, _apNeed, _apHave, cost.ApPrice, _data.MayorAp);

            _costRows.Clear();
            var prices = cost.DistrictPrices;
            if (prices == null)
                return;

            foreach (var price in prices)
            {
                var item = _costRowTemplate.Instantiate();
                var row = item.Q<VisualElement>("CostRow");
                item.Q<Label>("Icon").text = DistrictBuildLabels.ResourceIcon(price.Type);
                item.Q<Label>("Name").text = DistrictBuildLabels.ResourceLabel(price.Type);
                BindRow(row, item.Q<Label>("Need"), item.Q<Label>("Have"), price.Amount,
                    _data.AmountOf(payer, price.Type));
                _costRows.Add(item);
            }
        }

        // need = build cost; have = payer stockpile (or AP). Short-marks when have < required.
        private static void BindRow(VisualElement row, Label need, Label have, int required, int amount)
        {
            need.text = required.ToString();
            have.text = amount.ToString();
            row.EnableInClassList("costrow--short", amount < required);
        }
    }
}
