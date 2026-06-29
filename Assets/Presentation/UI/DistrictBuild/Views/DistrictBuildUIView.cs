using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.Configs;
using Domains.Economy.District.Configs;
using Domains.Economy.District.Data;
using Domains.Economy.Resource.Data;
using Domains.Map.Hex.Data;
using Domains.Map.HexResources.Data;
using Presentation.UI.DistrictBuild.Events;
using Presentation.UI.DistrictBuild.Views.Binders;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Presentation.UI.DistrictBuild.Views
{
    /// <summary>
    ///     Coordinator for the district-build overlay (a separate UIDocument from the HUD). The design is authored
    ///     in UXML (<c>DistrictBuildAction.uxml</c> chrome + detail skeleton, plus the <c>DistrictRow</c> /
    ///     <c>ReqLine</c> / <c>CostRow</c> item templates); this view only binds data into it. It owns the data
    ///     pushed by the driving system with no collection crossing the boundary — references to the gating
    ///     (<see cref="DistrictsBuildConfig" />, Economy) + cost (<see cref="ActionsDistrictsBuildConfig" />,
    ///     Actions) catalogues + the selected hex type (<see cref="SetContext" />), the hex's HexResources
    ///     (<see cref="AddHexResource" />), and each payer's AP / per-type stockpile one call at a time
    ///     (<see cref="SetMayorAp" /> / <see cref="SetMayorResource" /> / <see cref="SetCityResource" />), recorded
    ///     in pre-allocated managed pools (MonoBehaviour view — exempt from the system no-managed-collection rule).
    ///     Rendering is delegated to per-section binders (list / requirements / cost / payer / actions); this class
    ///     stays the single source of selection + payer state and of the authoritative <see cref="IsAvailable" />
    ///     gate. PanelRenderer builds its tree asynchronously, so binders are (re)constructed and state replays in
    ///     the reload callback.
    /// </summary>
    public sealed class DistrictBuildUIView : MonoBehaviour, IDistrictBuildData
    {
        private const string OverlayName = "DistrictBuildRoot";
        private const string ScrimName = "Scrim";
        private const string CloseButtonName = "CloseButton";
        private const string ListName = "DistrictList";
        private const string DetailNameLabel = "DetailName";
        private const string ReqLinesName = "ReqLines";
        private const string PayerMayorName = "PayerMayor";
        private const string PayerCityName = "PayerCity";
        private const string ApRowName = "ApRow";
        private const string CostRowsName = "CostRows";
        private const string ActionsPlaceholderName = "ActionsPlaceholder";
        private const string ConfirmButtonName = "ConfirmButton";

        private static readonly int ResourceTypeCount = Enum.GetValues(typeof(ResourceType)).Length;
        private static readonly int HexResourceTypeCount = Enum.GetValues(typeof(HexResourceType)).Length;

        [SerializeField] private PanelRenderer _renderer;
        [SerializeField] private VisualTreeAsset _districtRowTemplate;
        [SerializeField] private VisualTreeAsset _reqLineTemplate;
        [SerializeField] private VisualTreeAsset _costRowTemplate;

        private World _world;
        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _scrim;
        private Button _closeButton;
        private Button _confirmButton;
        private Label _detailName;
        private bool _cached;

        private DistrictListBinder _listBinder;
        private DistrictRequirementsBinder _requirementsBinder;
        private DistrictCostBinder _costBinder;
        private DistrictPayerBinder _payerBinder;
        private DistrictActionsBinder _actionsBinder;

        // Logical state, replayed on (re)bind. Pools are indexed by (int)ResourceType, pre-allocated once.
        private bool _visible;
        // Economy catalogue = roster + buildability gating (CanBuildOn); Actions catalogue = cost (AP + prices).
        // Pushed together by the driving system; joined per row by DistrictType.
        private DistrictsBuildConfig _config;
        private ActionsDistrictsBuildConfig _actionsConfig;
        private HexType _hexType;
        private int _mayorAp;
        private readonly int[] _mayorAmounts = new int[ResourceTypeCount];
        private readonly int[] _cityAmounts = new int[ResourceTypeCount];
        // The selected hex's HexResources (≤ one per type). Filled by the system on open; drives the resource gate.
        private readonly HexResourceType[] _hexResources = new HexResourceType[HexResourceTypeCount];
        private int _hexResourceCount;
        private int _selected;
        private Payer _payer;

        [Inject]
        public void Construct(World world)
        {
            _world = world;
        }

        // ---- IDistrictBuildData (read surface for the section binders) -------------------------------------

        public HexType SelectedHexType => _hexType;
        public int MayorAp => _mayorAp;
        public ReadOnlySpan<HexResourceType> HexResources => _hexResources.AsSpan(0, _hexResourceCount);

        public int AmountOfActivePayer(ResourceType type)
        {
            var index = (int)type;
            var pool = _payer == Payer.Mayor ? _mayorAmounts : _cityAmounts;
            return index >= 0 && index < pool.Length ? pool[index] : 0;
        }

        // Authoritative buildability gate (terrain + resource), single-sourced on the Economy config.
        public bool IsAvailable(DistrictBuildingConfig district) =>
            district.CanBuildOn(_hexType, _hexResources.AsSpan(0, _hexResourceCount));

        // Cost lives in the Actions catalogue, joined to an Economy roster district by DistrictType. A roster
        // district with no matching cost entry is a config authoring error — fail loud (the list already skips
        // DistrictType.Unknown rows before this is called).
        public ActionsDistrictBuildConfig CostFor(DistrictType type)
        {
            if (!TryGetActionsConfig(type, out var cost))
                throw new InvalidOperationException(
                    $"DistrictBuildUIView: ActionsDistrictsBuildConfig has no cost entry for district {type}.");

            return cost;
        }

        // ---- lifecycle ------------------------------------------------------------------------------------

        private void OnEnable()
        {
            // Fail loud at init (NOT in the reload callback — a throw there runs inside the global panel-update
            // loop and would blank every UIDocument, not just this overlay).
            if (_renderer == null)
                throw new InvalidOperationException("DistrictBuildUIView: PanelRenderer is not assigned on the prefab.");

            if (_districtRowTemplate == null || _reqLineTemplate == null || _costRowTemplate == null)
                throw new InvalidOperationException(
                    "DistrictBuildUIView: assign the DistrictRow / ReqLine / CostRow VisualTreeAsset templates on "
                    + "the prefab.");

            _renderer.RegisterUIReloadCallback(OnUiReloaded);
        }

        private void OnDisable()
        {
            _renderer.UnregisterUIReloadCallback(OnUiReloaded);
            UnhookChrome();
        }

        private void OnUiReloaded(PanelRenderer renderer, VisualElement root)
        {
            UnhookChrome();
            _root = root;
            _cached = false;

            if (!TryCache())
                return;

            _closeButton.clicked += OnCloseClicked;
            _confirmButton.clicked += OnConfirmClicked;
            _scrim.RegisterCallback<ClickEvent>(OnScrimClicked);
            ApplyState();
        }

        // ---- system-facing contract (unchanged) -----------------------------------------------------------

        /// <summary>Starts a fresh fill: stores the gating + cost catalogue references + selected hex, clears the payer pools.</summary>
        public void SetContext(DistrictsBuildConfig config, ActionsDistrictsBuildConfig actionsConfig, HexType hexType)
        {
            _config = config;
            _actionsConfig = actionsConfig;
            _hexType = hexType;
            _mayorAp = 0;
            _hexResourceCount = 0;
            Array.Clear(_mayorAmounts, 0, _mayorAmounts.Length);
            Array.Clear(_cityAmounts, 0, _cityAmounts.Length);
        }

        /// <summary>Records one HexResources type present on the selected hex (pushed by the driving system).</summary>
        public void AddHexResource(HexResourceType type)
        {
            if (_hexResourceCount < _hexResources.Length)
                _hexResources[_hexResourceCount++] = type;
        }

        public void SetMayorAp(int actionPoints) => _mayorAp = actionPoints;
        public void SetMayorResource(ResourceType type, int amount) => _mayorAmounts[(int)type] = amount;
        public void SetCityResource(ResourceType type, int amount) => _cityAmounts[(int)type] = amount;

        public void Show()
        {
            _visible = true;
            if (!TryCache())
                return;

            BindAll();
            _overlay.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _visible = false;
            if (TryCache())
                _overlay.style.display = DisplayStyle.None;
        }

        // ---- binding --------------------------------------------------------------------------------------

        private void ApplyState()
        {
            if (_config != null)
                BindAll();

            _overlay.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void BindAll()
        {
            if (_config?.Districts == null)
                return;

            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, _config.Districts.Length - 1));
            _listBinder.Bind(_config, _selected);
            BindDetail();
        }

        private void BindDetail()
        {
            var district = CurrentDistrict();
            if (district == null)
                return;

            _detailName.text = DistrictBuildLabels.DistrictName(district.DistrictType);
            _requirementsBinder.Bind(district);
            _payerBinder.Bind(_payer);
            _costBinder.Bind(district);
            _actionsBinder.Bind();
            BindConfirm(district);
        }

        // ЗБУДУВАТИ — closes for now (build is a dormant later slice); disabled when the gate fails.
        private void BindConfirm(DistrictBuildingConfig district)
        {
            var available = IsAvailable(district);
            _confirmButton.text = available ? "ЗБУДУВАТИ" : "НЕДОСТУПНО";
            _confirmButton.EnableInClassList("confirm--disabled", !available);
            _confirmButton.SetEnabled(available);
        }

        private DistrictBuildingConfig CurrentDistrict()
        {
            if (_config?.Districts == null || _config.Districts.Length == 0)
                return null;

            _selected = Mathf.Clamp(_selected, 0, _config.Districts.Length - 1);
            return _config.Districts[_selected];
        }

        private void OnDistrictSelected(int index)
        {
            _selected = index;
            BindAll();
        }

        private void OnPayerSelected(Payer payer)
        {
            _payer = payer;
            _payerBinder.Bind(_payer);

            var district = CurrentDistrict();
            if (district != null)
                _costBinder.Bind(district);
        }

        // ---- close flow -----------------------------------------------------------------------------------

        private void OnConfirmClicked() => RaiseClose();
        private void OnCloseClicked() => RaiseClose();
        private void OnScrimClicked(ClickEvent evt) => RaiseClose();

        // One-frame close pulse; DistrictBuildUISystem hides the window. Build is dormant for now.
        private void RaiseClose()
        {
            var entity = _world.CreateEntity();
            entity.Set(new DistrictBuildClosedEvent());
            entity.Set(new EventTag());
        }

        private void UnhookChrome()
        {
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseClicked;
            if (_confirmButton != null)
                _confirmButton.clicked -= OnConfirmClicked;
            _scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);
        }

        // ---- actions-catalogue join -----------------------------------------------------------------------

        // Linear scan — the catalogue holds a handful of districts and this is view code (a per-context map would
        // out-cost the scan). MonoBehaviour view, exempt from the system no-managed-collection rule.
        private bool TryGetActionsConfig(DistrictType type, out ActionsDistrictBuildConfig cost)
        {
            cost = null;
            var entries = _actionsConfig?.Districts;
            if (entries == null)
                return false;

            for (var i = 0; i < entries.Length; i++)
                if (entries[i] != null && entries[i].DistrictType == type)
                {
                    cost = entries[i];
                    return true;
                }

            return false;
        }

        private bool TryCache()
        {
            EnsureCached();
            return _cached;
        }

        private void EnsureCached()
        {
            if (_cached)
                return;

            if (_root == null)
                return;

            // _root is the PanelRenderer container; the authored top element (display:none in UXML) is its child
            // and is what we toggle — never the container, which would not clear that inline display:none.
            _overlay = _root.Q<VisualElement>(OverlayName);
            _scrim = _root.Q<VisualElement>(ScrimName);
            _closeButton = _root.Q<Button>(CloseButtonName);
            _confirmButton = _root.Q<Button>(ConfirmButtonName);
            _detailName = _root.Q<Label>(DetailNameLabel);
            var list = _root.Q<VisualElement>(ListName);
            var reqLines = _root.Q<VisualElement>(ReqLinesName);
            var payerMayor = _root.Q<VisualElement>(PayerMayorName);
            var payerCity = _root.Q<VisualElement>(PayerCityName);
            var apRow = _root.Q<VisualElement>(ApRowName);
            var costRows = _root.Q<VisualElement>(CostRowsName);
            var actionsPlaceholder = _root.Q<VisualElement>(ActionsPlaceholderName);

            if (_overlay == null || _scrim == null || _closeButton == null || _confirmButton == null
                || _detailName == null || list == null || reqLines == null || payerMayor == null
                || payerCity == null || apRow == null || costRows == null || actionsPlaceholder == null)
                return;

            // Templates are validated in OnEnable (fail-loud), so they are guaranteed non-null here.
            _listBinder = new DistrictListBinder(list, _districtRowTemplate, this, OnDistrictSelected);
            _requirementsBinder = new DistrictRequirementsBinder(reqLines, _reqLineTemplate, this);
            _costBinder = new DistrictCostBinder(apRow, costRows, _costRowTemplate, this);
            _payerBinder = new DistrictPayerBinder(payerMayor, payerCity, OnPayerSelected);
            _actionsBinder = new DistrictActionsBinder(actionsPlaceholder);
            _cached = true;
        }
    }
}
