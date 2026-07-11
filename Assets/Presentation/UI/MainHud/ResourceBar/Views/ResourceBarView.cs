using System.Collections.Generic;
using Domains.Economy.Resource.Data;
using Presentation.UI.MainHud.ResourceBar.Configs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Presentation.UI.MainHud.ResourceBar.Views
{
    /// <summary>
    ///     View layer for the LEFT-edge resource panel: the two inventory pools (Місто / Мер) as a VERTICAL
    ///     scroll list — one row per authored resource (icon + City value + Mayor value). The owner header
    ///     (Місто / Мер) is authored statically in UXML above the ScrollView; this view only builds the rows.
    ///     Owns its VisualElements and exposes a build-once + per-frame value API for ResourceBarSystem. Holds
    ///     no game logic. Lives on the shared Main UI PanelRenderer (like HexInfoPanelView / EndTurnView) and
    ///     toggles only its own ResourcePanel (the left panel) + the thin TopBar strip — never the document root
    ///     (that would blank the whole Main UI). Those two elements are not raycast-transparent, so they block
    ///     clicks over themselves. PanelRenderer builds its tree asynchronously, so the view binds elements in
    ///     the reload callback and replays its logical state (built rows + visibility) on (re)bind — public
    ///     methods called before the root exists just record intent.
    /// </summary>
    public sealed class ResourceBarView : MonoBehaviour
    {
        private const string TopBarName = "TopBar";
        private const string PanelName = "ResourcePanel";
        private const string ResourcePoolName = "ResourcePool";

        private const string RowClass = "res-row";
        private const string RowIconClass = "res-row-icon";
        private const string ValueClass = "res-val";
        private const string ValueCityClass = "res-val--city";
        private const string ValueMayorClass = "res-val--mayor";

        private const string EmptyValue = "—";

        [SerializeField] private PanelRenderer _renderer;

        private VisualElement _root;
        private VisualElement _topBar;
        private VisualElement _panel;
        private VisualElement _resourcePool;
        private bool _cached;

        // Logical state replayed on (re)bind — the row schema and the panel visibility. Per-owner amounts are
        // NOT stored: ResourceBarSystem re-pushes them every frame, so they self-heal once rows exist.
        private IReadOnlyList<InventoryResourceIconConfig.ResourceIconEntry> _entries;
        private bool _visible;

        // Managed UI elements → System.Collections.Generic (allowed in a view MonoBehaviour). One entry per
        // built row, keyed by resource type, so the per-frame system can set each owner's value by type.
        private readonly Dictionary<ResourceType, OwnerValues> _rows = new();

        private void OnEnable()
        {
            // PanelRenderer delivers the (re)built root via this callback — for both the initial build and a
            // late registration (it invokes a pending callback if the tree already exists) — so binding never
            // races the async tree build. The callback also re-fires on a live UXML reload.
            _renderer.RegisterUIReloadCallback(OnUiReloaded);
        }

        private void OnDisable()
        {
            _renderer.UnregisterUIReloadCallback(OnUiReloaded);
        }

        // PanelRenderer (re)built its visual tree: re-query elements off the fresh root and replay state.
        private void OnUiReloaded(PanelRenderer renderer, VisualElement root)
        {
            _root = root;
            _cached = false;

            if (!TryCache())
                return;

            if (_entries != null)
                BuildRows(_entries);

            ApplyVisibility();
        }

        public void Show()
        {
            _visible = true;
            if (TryCache())
                ApplyVisibility();
        }

        public void Hide()
        {
            _visible = false;
            if (TryCache())
                ApplyVisibility();
        }

        /// <summary>
        ///     Builds the list from the authored config entries (the entry list IS the row set + order).
        ///     Records the entries so the list rebuilds on a panel reload; when the root is not yet ready the
        ///     build is deferred to the reload callback. Creates one resource row per entry (icon + City value
        ///     + Mayor value), caching the two value labels by resource type.
        /// </summary>
        public void Build(IReadOnlyList<InventoryResourceIconConfig.ResourceIconEntry> entries)
        {
            _entries = entries;
            if (TryCache())
                BuildRows(entries);
        }

        public void SetCityAmount(ResourceType type, int amount)
        {
            if (_rows.TryGetValue(type, out var values))
                values.City.text = amount.ToString();
        }

        public void SetMayorAmount(ResourceType type, int amount)
        {
            if (_rows.TryGetValue(type, out var values))
                values.Mayor.text = amount.ToString();
        }

        private void ApplyVisibility()
        {
            var display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
            _panel.style.display = display;
            _topBar.style.display = display;
        }

        // Idempotent: a rebuild clears the previous rows first.
        private void BuildRows(IReadOnlyList<InventoryResourceIconConfig.ResourceIconEntry> entries)
        {
            _resourcePool.Clear();
            _rows.Clear();

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];

                var icon = new VisualElement();
                icon.AddToClassList(RowIconClass);
                SetBackground(icon, entry.Sprite);

                var cityValue = new Label(EmptyValue);
                cityValue.AddToClassList(ValueClass);
                cityValue.AddToClassList(ValueCityClass);

                var mayorValue = new Label(EmptyValue);
                mayorValue.AddToClassList(ValueClass);
                mayorValue.AddToClassList(ValueMayorClass);

                var row = new VisualElement { name = $"res-row-{entry.Type}" };
                row.AddToClassList(RowClass);
                row.Add(icon);
                row.Add(cityValue);
                row.Add(mayorValue);

                _resourcePool.Add(row);
                _rows[entry.Type] = new OwnerValues(cityValue, mayorValue);
            }
        }

        // A null sprite clears the inline value so the USS placeholder background shows through.
        private void SetBackground(VisualElement element, Sprite sprite)
        {
            element.style.backgroundImage = sprite == null
                ? StyleKeyword.Null
                : new StyleBackground(Background.FromSprite(sprite));
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

            // PanelRenderer hands the root via the reload callback; until then there is nothing to bind.
            if (_root == null)
                return;

            _topBar = _root.Q<VisualElement>(TopBarName);
            _panel = _root.Q<VisualElement>(PanelName);
            _resourcePool = _root.Q<VisualElement>(ResourcePoolName);
            _cached = true;

            // Strip the editor-preview sample rows authored in UXML so the runtime build starts clean.
            _resourcePool.Clear();
        }

        // The two value labels of one resource row (Місто / Мер).
        private readonly struct OwnerValues
        {
            public readonly Label City;
            public readonly Label Mayor;

            public OwnerValues(Label city, Label mayor)
            {
                City = city;
                Mayor = mayor;
            }
        }
    }
}
