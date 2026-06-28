using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Economy.District.Configs;
using Domains.Economy.District.Data;
using Domains.Economy.Resource.Data;
using Domains.Map.Hex.Data;
using Domains.Map.HexResources.Data;
using Presentation.UI.DistrictBuild.Events;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Presentation.UI.DistrictBuild.Views
{
    /// <summary>
    ///     View for the district-build overlay (a separate UIDocument from the HUD). Builds the master-detail
    ///     picker — district list (left) + detail (right: requirements, payer, build cost, actions/effect) — and
    ///     toggles the full-screen modal. The driving system pushes data with no collection crossing the boundary:
    ///     a reference to the <see cref="DistrictsBuildConfig" /> catalogue + the selected hex type
    ///     (<see cref="SetContext" />), then the hex's HexResources (<see cref="AddHexResource" />) and each payer's
    ///     AP / per-type stockpile amount one call at a time (<see cref="SetMayorAp" /> / <see cref="SetMayorResource" />
    ///     / <see cref="SetCityResource" />). The view records them in its own pre-allocated managed pools
    ///     (MonoBehaviour view — exempt from the system no-managed-collection rule). Real data: catalogue, AP cost,
    ///     resource costs vs payer stockpile, buildability (terrain + the hex-resource / empty-hex gate), payer
    ///     Мер/Місто; the unmodelled ДІЇ/ЕФЕКТ block is a marked placeholder. PanelRenderer builds its tree
    ///     asynchronously, so elements bind + state replays in the reload callback.
    /// </summary>
    public sealed class DistrictBuildActionView : MonoBehaviour
    {
        private const string OverlayName = "DistrictBuildRoot";
        private const string ScrimName = "Scrim";
        private const string CloseButtonName = "CloseButton";
        private const string ListName = "DistrictList";
        private const string DetailName = "DistrictDetail";

        private static readonly int ResourceTypeCount = Enum.GetValues(typeof(ResourceType)).Length;
        private static readonly int HexResourceTypeCount = Enum.GetValues(typeof(HexResourceType)).Length;

        [SerializeField] private PanelRenderer _renderer;

        private World _world;
        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _scrim;
        private Button _closeButton;
        private VisualElement _list;
        private VisualElement _detail;
        private bool _cached;

        // Logical state, replayed on (re)bind. Pools are indexed by (int)ResourceType, pre-allocated once.
        private bool _visible;
        private DistrictsBuildConfig _config;
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

        private void OnEnable()
        {
            _renderer.RegisterUIReloadCallback(OnUiReloaded);
        }

        private void OnDisable()
        {
            _renderer.UnregisterUIReloadCallback(OnUiReloaded);
            UnhookClose();
        }

        private void OnUiReloaded(PanelRenderer renderer, VisualElement root)
        {
            UnhookClose();
            _root = root;
            _cached = false;

            if (!TryCache())
                return;

            _closeButton.clicked += OnCloseClicked;
            _scrim.RegisterCallback<ClickEvent>(OnScrimClicked);
            ApplyState();
        }

        /// <summary>Starts a fresh fill: stores the catalogue reference + selected hex, clears the payer pools.</summary>
        public void SetContext(DistrictsBuildConfig config, HexType hexType)
        {
            _config = config;
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

            Render();
            _overlay.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _visible = false;
            if (TryCache())
                _overlay.style.display = DisplayStyle.None;
        }

        private void ApplyState()
        {
            if (_config != null)
                Render();

            _overlay.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ---- rendering (mirrors the mockup's renderList / renderDetail) -------------------------------------

        private void Render()
        {
            RenderList();
            RenderDetail();
        }

        private void RenderList()
        {
            _list.Clear();
            if (_config?.Districts == null)
                return;

            var districts = _config.Districts;
            for (var index = 0; index < districts.Length; index++)
            {
                var district = districts[index];
                if (district == null || district.DistrictType == DistrictType.Unknown)
                    continue;

                var available = IsAvailable(district);
                var rowIndex = index;

                var row = Block("drow");
                if (index == _selected)
                    row.AddToClassList("drow--active");
                if (!available)
                    row.AddToClassList("drow--locked");

                row.Add(Glyph(DistrictIcon(district.DistrictType), "drow-ic"));

                var mid = Block("drow-mid");
                mid.Add(Text(DistrictName(district.DistrictType), "drow-name"));
                mid.Add(Text(available ? "✓ доступно" : "✕ вимога не виконана",
                    available ? "drow-req drow-req--ok" : "drow-req drow-req--no"));
                row.Add(mid);

                row.Add(Text($"{district.ActionPointsRequired} AP", "appill"));

                row.RegisterCallback<ClickEvent>(_ =>
                {
                    _selected = rowIndex;
                    Render();
                });
                _list.Add(row);
            }
        }

        private void RenderDetail()
        {
            _detail.Clear();
            if (_config?.Districts == null || _config.Districts.Length == 0)
                return;

            _selected = Mathf.Clamp(_selected, 0, _config.Districts.Length - 1);
            var district = _config.Districts[_selected];
            if (district == null)
                return;

            _detail.Add(Text(DistrictName(district.DistrictType), "ddetail-name"));

            // ВИМОГИ — terrain gate + the real hex-resource gate (one required resource, or an empty hex).
            _detail.Add(SectionHeader("ВИМОГИ"));
            var available = IsAvailable(district);

            var terrainOk = MeetsTerrain(district);
            _detail.Add(ReqLine(terrainOk, terrainOk ? "Терен гекса підходить" : "Терен гекса не підходить"));

            if (district.NeedEmptyHexResourcesToBuild)
            {
                var emptyOk = _hexResourceCount == 0;
                _detail.Add(ReqLine(emptyOk, emptyOk ? "Гекс без ресурсів" : "Гекс має бути без ресурсів"));
            }
            else
            {
                var hasRequired = HexHasResource(district.RequiredHexResourceType);
                _detail.Add(ReqLine(hasRequired,
                    $"Потрібен ресурс: {HexResourceLabel(district.RequiredHexResourceType)}"));
            }

            // ПЛАТНИК — Мер / Місто (Payer = Owner).
            _detail.Add(SectionHeader("ПЛАТНИК"));
            var payerRow = Block("payer");
            payerRow.Add(PayerSegment(Payer.Mayor, "👑", "Мер"));
            payerRow.Add(PayerSegment(Payer.City, "🏛️", "Місто"));
            _detail.Add(payerRow);

            // БУДІВНИЦТВО — real AP + resource costs vs the active payer's stockpile.
            _detail.Add(SectionHeader("БУДІВНИЦТВО"));
            // AP is always spent from the Mayor's pool, regardless of the resource payer (Мер / Місто).
            _detail.Add(CostRow("⚡", "AP", district.ActionPointsRequired, _mayorAp, true));
            if (district.DistrictPrices != null)
            {
                foreach (var price in district.DistrictPrices)
                    _detail.Add(CostRow(ResourceIcon(price.Type), ResourceLabel(price.Type), price.Amount,
                        AmountOf(price.Type), true));
            }

            // ДІЇ / ЕФЕКТ — not modelled yet (capacity / actions / yield split / upkeep). Marked placeholder.
            _detail.Add(SectionHeader("ДІЇ / ЕФЕКТ"));
            _detail.Add(Text("Місткість, дії та виробництво (split Місто / Власн. / Опер.) і утримання — "
                             + "з'являться разом з моделлю району.", "placeholder-note"));

            // ЗБУДУВАТИ — closes for now (build is a dormant later slice); disabled when the terrain gate fails.
            var foot = Block("dfoot");
            var confirm = new Button { text = available ? "ЗБУДУВАТИ" : "НЕДОСТУПНО" };
            confirm.AddToClassList("confirm");
            if (!available)
                confirm.AddToClassList("confirm--disabled");
            confirm.SetEnabled(available);
            confirm.clicked += OnConfirmClicked;
            foot.Add(confirm);
            _detail.Add(foot);
        }

        private VisualElement PayerSegment(Payer payer, string emoji, string label)
        {
            var seg = Block("payseg");
            seg.AddToClassList(payer == Payer.Mayor ? "payseg--mayor" : "payseg--city");
            if (_payer == payer)
                seg.AddToClassList("payseg--on");

            seg.Add(Glyph(emoji, "payseg-pe"));
            seg.Add(Text(label, "payseg-who"));
            seg.Add(Text("· власник", "payseg-own"));
            seg.RegisterCallback<ClickEvent>(_ =>
            {
                _payer = payer;
                RenderDetail();
            });
            return seg;
        }

        // required = build cost; have = payer stockpile (or AP). short-marks when have < required (have >= 0).
        private VisualElement CostRow(string icon, string name, int required, int have, bool showHave)
        {
            var row = Block("costrow");
            var shortfall = showHave && have >= 0 && have < required;
            if (shortfall)
                row.AddToClassList("costrow--short");

            row.Add(Glyph(icon, "costrow-ic"));
            row.Add(Text(name, "costrow-name"));
            row.Add(Text(required.ToString(), "costrow-need"));
            row.Add(Text(showHave && have >= 0 ? have.ToString() : "—", "costrow-have"));
            return row;
        }

        private int AmountOf(ResourceType type)
        {
            var index = (int)type;
            var pool = _payer == Payer.Mayor ? _mayorAmounts : _cityAmounts;
            return index >= 0 && index < pool.Length ? pool[index] : 0;
        }

        // Authoritative buildability gate (terrain + resource), single-sourced on the config.
        private bool IsAvailable(DistrictBuildingConfig district) =>
            district.CanBuildOn(_hexType, _hexResources.AsSpan(0, _hexResourceCount));

        // Display-only: mirrors the terrain half of CanBuildOn so the ВИМОГИ block can show a per-line ✓/✕.
        private bool MeetsTerrain(DistrictBuildingConfig district)
        {
            if (district.ImpossibleToBuildTypes.Contains(_hexType))
                return false;

            return district.HexTypesRequirement.Count == 0 || district.HexTypesRequirement.Contains(_hexType);
        }

        private bool HexHasResource(HexResourceType type)
        {
            for (var i = 0; i < _hexResourceCount; i++)
                if (_hexResources[i] == type)
                    return true;

            return false;
        }

        private void OnConfirmClicked() => RaiseClose();
        private void OnCloseClicked() => RaiseClose();
        private void OnScrimClicked(ClickEvent evt) => RaiseClose();

        // One-frame close pulse; DistrictBuildActionSystem hides the window. Build is dormant for now.
        private void RaiseClose()
        {
            var entity = _world.CreateEntity();
            entity.Set(new DistrictBuildClosedEvent());
            entity.Set(new EventTag());
        }

        private void UnhookClose()
        {
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseClicked;
            _scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);
        }

        // ---- small element builders -----------------------------------------------------------------------

        private static VisualElement Block(string className)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            return element;
        }

        private static Label Text(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static Label Glyph(string glyph, string className) => Text(glyph, className);

        private static VisualElement SectionHeader(string text) => Text(text, "sec-h");

        private static Label ReqLine(bool met, string text) =>
            Text((met ? "✓ " : "✕ ") + text, met ? "req-line req-line--ok" : "req-line req-line--no");

        private static string DistrictName(DistrictType type) => type switch
        {
            DistrictType.CityCenter => "Міський центр",
            DistrictType.Farm => "Ферма",
            _ => type.ToString()
        };

        private static string DistrictIcon(DistrictType type) => type switch
        {
            DistrictType.CityCenter => "🏛️",
            DistrictType.Farm => "🌾",
            _ => "🏗️"
        };

        private static string HexResourceLabel(HexResourceType type) => type switch
        {
            HexResourceType.Forest => "Ліс",
            HexResourceType.Clay => "Глина",
            HexResourceType.Fish => "Риба",
            _ => type.ToString()
        };

        private static string ResourceLabel(ResourceType type) => type switch
        {
            ResourceType.Grain => "Зерно",
            ResourceType.Clay => "Глина",
            ResourceType.Wood => "Колоди",
            ResourceType.RawMeat => "Сире м'ясо",
            ResourceType.RawFish => "Сира риба",
            ResourceType.SmokedMeat => "Копчене м'ясо",
            ResourceType.SmokedFish => "Копчена риба",
            _ => type.ToString()
        };

        private static string ResourceIcon(ResourceType type) => type switch
        {
            ResourceType.Grain => "🌾",
            ResourceType.Clay => "🧱",
            ResourceType.Wood => "🪵",
            ResourceType.RawMeat => "🥩",
            ResourceType.RawFish => "🐟",
            ResourceType.SmokedMeat => "🍖",
            ResourceType.SmokedFish => "🐠",
            _ => "📦"
        };

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
            _list = _root.Q<VisualElement>(ListName);
            _detail = _root.Q<VisualElement>(DetailName);
            _cached = _overlay != null && _scrim != null && _closeButton != null && _list != null && _detail != null;
        }

        private enum Payer
        {
            Mayor,
            City
        }
    }
}
