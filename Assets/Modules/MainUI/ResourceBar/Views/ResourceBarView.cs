using System.Collections.Generic;
using Domains.Economy.Resource.Data;
using Modules.MainUI.ResourceBar.Configs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.MainUI.ResourceBar.Views
{
    /// <summary>
    ///     View layer for the top-bar resource strip: the owner×resource matrix (rows Місто / Мер, one column per
    ///     authored resource). Owns its VisualElements and exposes a build-once + per-frame value API for
    ///     ResourceBarSystem. Holds no game logic. Lives on the shared Main UI PanelRenderer (like HexInfoPanelView
    ///     / EndTurnView) and toggles only its own TopBar element — never the document root (that would blank the
    ///     whole Main UI). The TopBar is not raycast-transparent, so it blocks clicks across the top strip.
    ///     PanelRenderer builds its tree asynchronously, so the view binds elements in the reload callback and
    ///     replays its logical state (built columns + visibility) on (re)bind — public methods called before the
    ///     root exists just record intent.
    /// </summary>
    public sealed class ResourceBarView : MonoBehaviour
    {
        private const string TopBarName = "TopBar";
        private const string ResourcePoolName = "ResourcePool";

        private const string OwnersClass = "res-owners";
        private const string OwnerSpacerClass = "res-owner-spacer";
        private const string OwnerLabelClass = "res-owner-lbl";
        private const string OwnerLabelCityClass = "res-owner-lbl--city";
        private const string OwnerLabelMayorClass = "res-owner-lbl--mayor";
        private const string ColumnClass = "res-col";
        private const string ColumnIconClass = "res-col-icon";
        private const string ValueClass = "res-val";
        private const string ValueCityClass = "res-val--city";
        private const string ValueMayorClass = "res-val--mayor";

        private const string CityRowLabel = "Місто";
        private const string MayorRowLabel = "Мер";
        private const string EmptyValue = "—";

        [SerializeField] private PanelRenderer _renderer;

        private VisualElement _root;
        private VisualElement _topBar;
        private VisualElement _resourcePool;
        private bool _cached;

        // Logical state replayed on (re)bind — the column schema and the strip visibility. Per-owner amounts
        // are NOT stored: ResourceBarSystem re-pushes them every frame, so they self-heal once columns exist.
        private IReadOnlyList<InventoryResourceIconConfig.ResourceIconEntry> _entries;
        private bool _visible;

        // Managed UI elements → System.Collections.Generic (allowed in a view MonoBehaviour). One entry per
        // built column, keyed by resource type, so the per-frame system can set each owner's value by type.
        private readonly Dictionary<ResourceType, OwnerValues> _columns = new();

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
                BuildColumns(_entries);

            _topBar.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Show()
        {
            _visible = true;
            if (TryCache())
                _topBar.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _visible = false;
            if (TryCache())
                _topBar.style.display = DisplayStyle.None;
        }

        /// <summary>
        ///     Builds the strip from the authored config entries (the entry list IS the column set + order).
        ///     Records the entries so the strip rebuilds on a panel reload; when the root is not yet ready the
        ///     build is deferred to the reload callback. Creates a leading owner-label column (Місто / Мер) and
        ///     one resource column per entry, caching the two value labels by resource type.
        /// </summary>
        public void Build(IReadOnlyList<InventoryResourceIconConfig.ResourceIconEntry> entries)
        {
            _entries = entries;
            if (TryCache())
                BuildColumns(entries);
        }

        public void SetCityAmount(ResourceType type, int amount)
        {
            if (_columns.TryGetValue(type, out var values))
                values.City.text = amount.ToString();
        }

        public void SetMayorAmount(ResourceType type, int amount)
        {
            if (_columns.TryGetValue(type, out var values))
                values.Mayor.text = amount.ToString();
        }

        // Idempotent: a rebuild clears the previous columns first.
        private void BuildColumns(IReadOnlyList<InventoryResourceIconConfig.ResourceIconEntry> entries)
        {
            _resourcePool.Clear();
            _columns.Clear();

            _resourcePool.Add(BuildOwnerLabels());

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                var cityValue = new Label(EmptyValue);
                cityValue.AddToClassList(ValueClass);
                cityValue.AddToClassList(ValueCityClass);

                var mayorValue = new Label(EmptyValue);
                mayorValue.AddToClassList(ValueClass);
                mayorValue.AddToClassList(ValueMayorClass);

                var icon = new VisualElement();
                icon.AddToClassList(ColumnIconClass);
                SetBackground(icon, entry.Sprite);

                var col = new VisualElement { name = $"res-col-{entry.Type}" };
                col.AddToClassList(ColumnClass);
                col.Add(icon);
                col.Add(cityValue);
                col.Add(mayorValue);

                _resourcePool.Add(col);
                _columns[entry.Type] = new OwnerValues(cityValue, mayorValue);
            }
        }

        private VisualElement BuildOwnerLabels()
        {
            var owners = new VisualElement();
            owners.AddToClassList(OwnersClass);

            // Spacer aligns the owner labels with the value rows below the column icons.
            var spacer = new VisualElement();
            spacer.AddToClassList(OwnerSpacerClass);
            owners.Add(spacer);

            var cityLabel = new Label(CityRowLabel);
            cityLabel.AddToClassList(OwnerLabelClass);
            cityLabel.AddToClassList(OwnerLabelCityClass);
            owners.Add(cityLabel);

            var mayorLabel = new Label(MayorRowLabel);
            mayorLabel.AddToClassList(OwnerLabelClass);
            mayorLabel.AddToClassList(OwnerLabelMayorClass);
            owners.Add(mayorLabel);

            return owners;
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
            _resourcePool = _root.Q<VisualElement>(ResourcePoolName);
            _cached = true;

            // Strip the editor-preview sample columns authored in UXML so the runtime build starts clean.
            _resourcePool.Clear();
        }

        // The two value labels of one resource column (Місто / Мер rows).
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
