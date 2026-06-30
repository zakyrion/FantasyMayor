using System;
using Presentation.UI.DistrictBuild.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views
{
    // БУДІВНИЦТВО section: the AP row (need vs Mayor pool — AP is always Mayor-paid) + one cost row per resource
    // price (need vs payer stockpile, short-marked) + the payer toggle (Мер/Місто). Driven by
    // DistrictBuildPriceUISubSystem (push-to-view); the payer toggle is a local C# event the subsystem listens
    // to (payer state is NOT in ECS). Bound to the shared PanelRenderer tree; elements + segment callbacks
    // (re)bound on reload.
    public sealed class DistrictBuildPriceUIView : MonoBehaviour
    {
        [SerializeField] private PanelRenderer _renderer;
        [SerializeField] private VisualTreeAsset _costRowTemplate;
        [SerializeField] private string _apRowName = "ApRow";
        [SerializeField] private string _costRowsName = "CostRows";
        [SerializeField] private string _payerMayorName = "PayerMayor";
        [SerializeField] private string _payerCityName = "PayerCity";

        private VisualElement _apRow;
        private Label _apNeed;
        private Label _apHave;
        private VisualElement _costRows;
        private VisualElement _payerMayor;
        private VisualElement _payerCity;

        public event Action<Payer> PayerChanged;

        public void SetAp(int need, int have)
        {
            if (_apRow == null)
                return;

            BindRow(_apRow, _apNeed, _apHave, need, have);
        }

        public void ClearCosts()
        {
            _costRows?.Clear();
        }

        public void AddCost(string icon, string resourceName, int need, int have)
        {
            if (_costRows == null)
                return;

            var item = _costRowTemplate.Instantiate();
            var row = item.Q<VisualElement>("CostRow");
            item.Q<Label>("Icon").text = icon;
            item.Q<Label>("Name").text = resourceName;
            BindRow(row, item.Q<Label>("Need"), item.Q<Label>("Have"), need, have);
            _costRows.Add(item);
        }

        public void SetPayer(Payer current)
        {
            _payerMayor?.EnableInClassList("payseg--on", current == Payer.Mayor);
            _payerCity?.EnableInClassList("payseg--on", current == Payer.City);
        }

        // need = build cost; have = payer stockpile (or AP). Short-marks when have < required.
        private static void BindRow(VisualElement row, Label need, Label have, int required, int amount)
        {
            need.text = required.ToString();
            have.text = amount.ToString();
            row.EnableInClassList("costrow--short", amount < required);
        }

        private void OnEnable()
        {
            if (_renderer == null)
                throw new InvalidOperationException(
                    "DistrictBuildPriceUIView: PanelRenderer is not assigned on the prefab.");
            if (_costRowTemplate == null)
                throw new InvalidOperationException(
                    "DistrictBuildPriceUIView: the CostRow VisualTreeAsset template is not assigned on the prefab.");

            _renderer.RegisterUIReloadCallback(OnUiReloaded);
        }

        private void OnDisable()
        {
            if (_renderer != null)
                _renderer.UnregisterUIReloadCallback(OnUiReloaded);
        }

        private void OnUiReloaded(PanelRenderer renderer, VisualElement root)
        {
            _apRow = root.Q<VisualElement>(_apRowName);
            _apNeed = _apRow?.Q<Label>("ApNeed");
            _apHave = _apRow?.Q<Label>("ApHave");
            _costRows = root.Q<VisualElement>(_costRowsName);
            _payerMayor = root.Q<VisualElement>(_payerMayorName);
            _payerCity = root.Q<VisualElement>(_payerCityName);

            _payerMayor?.RegisterCallback<ClickEvent>(_ => PayerChanged?.Invoke(Payer.Mayor));
            _payerCity?.RegisterCallback<ClickEvent>(_ => PayerChanged?.Invoke(Payer.City));
        }
    }
}
