using System.Collections.Generic;
using DefaultEcs;
using DefaultECSExtensions;
using Flows.DistrictBuild.Events;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using static Unity.AppUI.UI.VisualElementExtensions;

namespace Presentation.UI.MainHud.HexInfoPanel.Views
{
    /// <summary>
    ///     View layer for the CONTEXT sub-panel of the shared bottom panel. Owns its VisualElements and exposes
    ///     a per-block populate API for the panel systems. Holds no game logic. It does NOT own the bottom-panel
    ///     shell (TurnPanelView reveals/hides that) — it only swaps the context content between the filled blocks
    ///     (a hex is selected) and the empty placeholder (nothing selected). The full-screen layers stay
    ///     click-through so empty-area clicks reach the map, but the bottom panel itself blocks clicks. Lives on
    ///     the shared Main UI PanelRenderer; because PanelRenderer builds its tree asynchronously, the view binds
    ///     elements in the reload callback and replays its last block state on (re)bind.
    /// </summary>
    public sealed class HexInfoPanelView : MonoBehaviour
    {
        private const string RootName = "Root";
        private const string ContextFilledName = "ContextFilled";
        private const string ContextEmptyName = "ContextEmpty";
        private const string HeaderIconName = "HeaderIcon";
        private const string HeaderTitleName = "HeaderTitle";
        private const string ResourcesSectionName = "ResourcesSection";
        private const string ResourcesContainerName = "ResourcesContainer";
        private const string DistrictSectionName = "DistrictSection";
        private const string ProductionSectionName = "ProductionSection";
        private const string DistrictBuildSectionName = "DistrictBuildSection";
        private const string DistrictBuildButtonName = "DistrictBuildButton";

        private const string ChipClass = "chip";

        [SerializeField] private PanelRenderer _renderer;

        private World _world;
        private VisualElement _root;
        private VisualElement _contextFilled;
        private VisualElement _contextEmpty;
        private VisualElement _headerIcon;
        private Label _headerTitle;
        private VisualElement _resourcesSection;
        private VisualElement _resourcesContainer;
        private VisualElement _districtSection;
        private VisualElement _productionSection;
        private VisualElement _districtBuildSection;
        private VisualElement _districtBuildButton;

        // Logical state replayed on (re)bind. The reactive panel systems re-push on the next selection; this
        // covers the pre-selection initial state and a live UI reload while a hex is selected.
        private bool _showFilled;
        private bool _headerSet;
        private Sprite _headerSprite;
        private string _headerText;
        private bool _resourcesVisible;
        private IReadOnlyList<ResourceChip> _resources;
        private DistrictState _districtState;

        // Managed UI elements → System.Collections.Generic (NativeContainer holds unmanaged only).
        private readonly List<VisualElement> _chipPool = new();
        private bool _cached;

        [Inject]
        public void Construct(World world)
        {
            _world = world;
        }

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
            UnhookBuildButton();
        }

        // PanelRenderer (re)built its visual tree: re-query elements off the fresh root, re-hook the (recreated)
        // build slot, reapply picking, and replay the last block state.
        private void OnUiReloaded(PanelRenderer renderer, VisualElement root)
        {
            UnhookBuildButton();
            _root = root;
            _cached = false;

            if (!TryCache())
                return;

            _districtBuildButton.RegisterCallback<ClickEvent>(OnBuildClicked);
            ConfigurePicking();
            ApplyState();
        }

        /// <summary>A hex is selected: show the filled context blocks, hide the empty placeholder.</summary>
        public void ShowSelection()
        {
            _showFilled = true;
            if (!TryCache())
                return;

            _contextFilled.style.display = DisplayStyle.Flex;
            _contextEmpty.style.display = DisplayStyle.None;
        }

        /// <summary>Nothing selected: hide the filled blocks, show the empty placeholder. The shell stays up.</summary>
        public void ShowEmpty()
        {
            _showFilled = false;
            if (!TryCache())
                return;

            _contextFilled.style.display = DisplayStyle.None;
            _contextEmpty.style.display = DisplayStyle.Flex;
        }

        public void SetHeader(Sprite icon, string title)
        {
            _headerSet = true;
            _headerSprite = icon;
            _headerText = title;
            if (!TryCache())
                return;

            SetBackground(_headerIcon, icon);
            _headerTitle.text = title;
        }

        public void SetResources(IReadOnlyList<ResourceChip> resources)
        {
            _resourcesVisible = true;
            _resources = resources;
            if (!TryCache())
                return;

            FillResources(resources);
            _resourcesSection.style.display = DisplayStyle.Flex;
        }

        public void HideResources()
        {
            _resourcesVisible = false;
            if (!TryCache())
                return;

            _resourcesSection.style.display = DisplayStyle.None;
        }

        /// <summary>
        ///     "No district yet" state: show the build-prompt block (Район + the build slot), hide the
        ///     district-details blocks. Mutually exclusive with <see cref="ShowDistrictDetails" /> /
        ///     <see cref="HideDistrict" /> — exactly one district state is visible at a time.
        /// </summary>
        public void ShowDistrictBuildPrompt() => SetDistrictState(DistrictState.Build);

        /// <summary>
        ///     "District exists" state: show the District block (Район) and the "Праця та виробництво" production
        ///     block (they share the same backing data, so they appear together), hide the build prompt.
        ///     SCAFFOLD content until District-economy components land — unreachable while no district is built.
        /// </summary>
        public void ShowDistrictDetails() => SetDistrictState(DistrictState.Details);

        /// <summary>Nothing selected (or no grid hex): hide every district block.</summary>
        public void HideDistrict() => SetDistrictState(DistrictState.None);

        private void SetDistrictState(DistrictState districtState)
        {
            _districtState = districtState;
            if (!TryCache())
                return;

            ApplyDistrictState();
        }

        private void ApplyState()
        {
            _contextFilled.style.display = _showFilled ? DisplayStyle.Flex : DisplayStyle.None;
            _contextEmpty.style.display = _showFilled ? DisplayStyle.None : DisplayStyle.Flex;

            if (_headerSet)
            {
                SetBackground(_headerIcon, _headerSprite);
                _headerTitle.text = _headerText;
            }

            if (_resourcesVisible && _resources != null)
                FillResources(_resources);
            _resourcesSection.style.display = _resourcesVisible ? DisplayStyle.Flex : DisplayStyle.None;

            ApplyDistrictState();
        }

        private void ApplyDistrictState()
        {
            var detailsDisplay = _districtState == DistrictState.Details ? DisplayStyle.Flex : DisplayStyle.None;
            _districtSection.style.display = detailsDisplay;
            _productionSection.style.display = detailsDisplay;
            _districtBuildSection.style.display =
                _districtState == DistrictState.Build ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // The build slot raises a payload-less one-frame request; the future build window reads the current
        // HexSelectedComponent for the target hex. No consumer yet (dormant emitter).
        private void OnBuildClicked(ClickEvent evt)
        {
            var entity = _world.CreateEntity();
            entity.Set(new DistrictBuildUIRequestedEvent());
            entity.Set(new EventTag());
        }

        private void UnhookBuildButton()
        {
            _districtBuildButton?.UnregisterCallback<ClickEvent>(OnBuildClicked);
        }

        private void FillResources(IReadOnlyList<ResourceChip> resources)
        {
            for (var index = 0; index < resources.Count; index++)
            {
                // Icon-only tile: the sprite is the whole chip; the resource name is a hover tooltip, not a
                // visible label (the unified square-icon look, GENERAL_UI_STYLE.md §7).
                var chip = GetOrCreateChip(index);
                SetBackground(chip, resources[index].Icon);
                chip.tooltip = resources[index].Label;
                chip.style.display = DisplayStyle.Flex;
            }

            for (var index = resources.Count; index < _chipPool.Count; index++)
                _chipPool[index].style.display = DisplayStyle.None;
        }

        /// <summary>
        ///     Makes the full-screen layers (the panel root + the "Root" container) click-through so
        ///     empty-area clicks reach the map, while the bottom panel stays pickable so it blocks clicks.
        ///     Picking does not propagate from a parent to its children, so leaving the panel and its children
        ///     at the default pickable mode is enough — only the two full-screen ancestors are disabled.
        ///     HexSelectionSystem then skips selection over the panel via EventSystem.IsPointerOverGameObject.
        /// </summary>
        private void ConfigurePicking()
        {
            _root.EnablePicking(false);
            _root.Q<VisualElement>(RootName)?.EnablePicking(false);
        }

        // A null sprite clears the inline value so the USS placeholder background shows through.
        private void SetBackground(VisualElement element, Sprite sprite)
        {
            element.style.backgroundImage = sprite == null
                ? StyleKeyword.Null
                : new StyleBackground(Background.FromSprite(sprite));
        }

        private VisualElement GetOrCreateChip(int index)
        {
            if (index < _chipPool.Count)
                return _chipPool[index];

            // One resource = one square icon tile (no inner icon/label children — icon-only). The sprite is set
            // as the tile's own background; the resource name is a tooltip. Default pickable mode is fine — the
            // panel already blocks map clicks across its whole area.
            var chip = new VisualElement { name = $"chip-{index}" };
            chip.AddToClassList(ChipClass);

            _resourcesContainer.Add(chip);
            _chipPool.Add(chip);
            return chip;
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

            _contextFilled = _root.Q<VisualElement>(ContextFilledName);
            _contextEmpty = _root.Q<VisualElement>(ContextEmptyName);
            _headerIcon = _root.Q<VisualElement>(HeaderIconName);
            _headerTitle = _root.Q<Label>(HeaderTitleName);
            _resourcesSection = _root.Q<VisualElement>(ResourcesSectionName);
            _resourcesContainer = _root.Q<VisualElement>(ResourcesContainerName);
            _districtSection = _root.Q<VisualElement>(DistrictSectionName);
            _productionSection = _root.Q<VisualElement>(ProductionSectionName);
            _districtBuildSection = _root.Q<VisualElement>(DistrictBuildSectionName);
            _districtBuildButton = _root.Q<VisualElement>(DistrictBuildButtonName);
            _cached = true;

            // A panel reload recreates the elements, so the pooled chips are stale — drop them and strip the
            // editor-preview sample chips authored in UXML; the resources block rebuilds from real data.
            _chipPool.Clear();
            _resourcesContainer.Clear();
        }

        // Which of the three mutually-exclusive district blocks is shown.
        private enum DistrictState
        {
            None,
            Build,
            Details
        }

        /// <summary>One resource entry to render as a chip in the resources block.</summary>
        public readonly struct ResourceChip
        {
            public readonly Sprite Icon;
            public readonly string Label;

            public ResourceChip(Sprite icon, string label)
            {
                Icon = icon;
                Label = label;
            }
        }
    }
}
