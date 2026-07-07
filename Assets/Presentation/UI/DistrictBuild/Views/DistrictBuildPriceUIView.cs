using System;
using Domains.Kernel.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views
{
    // БУДІВНИЦТВО section: the AP row (need vs Mayor pool — AP is always Mayor-paid) + one cost row per resource
    // price (need vs payer stockpile, short-marked) + the payer segments (one per allowed owner). Driven by
    // DistrictBuildPriceUISubSystem (push-to-view); payer selection is owned HERE (view-local, not ECS) and
    // surfaced via SelectedOwner + the PayerChanged event the subsystem listens to. Bound to the shared
    // PanelRenderer tree; container refs are (re)bound on reload.
    public sealed class DistrictBuildPriceUIView : MonoBehaviour
    {
        [SerializeField] private PanelRenderer _renderer;
        [SerializeField] private VisualTreeAsset _costRowTemplate;
        [SerializeField] private VisualTreeAsset _payerRowTemplate;
        [SerializeField] private string _apRowName = "ApRow";
        [SerializeField] private string _costRowsName = "CostRows";
        [SerializeField] private string _payerRowsName = "PayerRows";

        private VisualElement _apRow;
        private Label _apNeed;
        private Label _apHave;
        private VisualElement _costRows;
        private VisualElement _payerRows;
        private ActorType _selectedOwner = ActorType.Unknown;

        public event Action<ActorType> PayerChanged;

        public ActorType SelectedOwner => _selectedOwner;

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

        public void ClearPayers()
        {
            _payerRows?.Clear();
            _selectedOwner = ActorType.Unknown;
        }

        public void AddPayer(ActorType owner, string icon, string who)
        {
            if (_payerRows == null)
                return;

            var item = _payerRowTemplate.Instantiate();
            var row = item.Q<VisualElement>("PayerRow");
            row.Q<Label>("Icon").text = icon;
            row.Q<Label>("Who").text = who;
            row.AddToClassList(owner == ActorType.City ? "payseg--city" : "payseg--mayor");
            row.RegisterCallback<ClickEvent>(_ => OnRowClicked(owner));
            // Add the segment itself (not the TemplateContainer wrapper) so it stays a direct .payer flex child.
            _payerRows.Add(row);
        }

        public void SetSelectedPayer(ActorType owner)
        {
            _selectedOwner = owner;
            ApplyHighlight();
        }

        private void OnRowClicked(ActorType owner)
        {
            if (owner == _selectedOwner)
                return;

            _selectedOwner = owner;
            ApplyHighlight();
            PayerChanged?.Invoke(owner);
        }

        // The "on" state is styled only via the compound selectors .payseg--on.payseg--mayor/--city, so match on
        // the row's own modifier class rather than tracking element references. Unknown selection = nothing lit.
        private void ApplyHighlight()
        {
            if (_payerRows == null)
                return;

            var onClass = _selectedOwner == ActorType.City ? "payseg--city" : "payseg--mayor";
            foreach (var row in _payerRows.Children())
                row.EnableInClassList("payseg--on",
                    _selectedOwner != ActorType.Unknown && row.ClassListContains(onClass));
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
            if (_payerRowTemplate == null)
                throw new InvalidOperationException(
                    "DistrictBuildPriceUIView: the PayerRow VisualTreeAsset template is not assigned on the prefab.");

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
            _payerRows = root.Q<VisualElement>(_payerRowsName);
        }
    }
}
