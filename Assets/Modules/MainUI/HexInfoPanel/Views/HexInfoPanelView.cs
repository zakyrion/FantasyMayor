using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.AppUI.UI.VisualElementExtensions;

namespace Modules.MainUI.HexInfoPanel.Views
{
    /// <summary>
    ///     View layer for the CONTEXT sub-panel of the shared bottom panel. Owns its VisualElements and exposes
    ///     a per-block populate API for the panel systems. Holds no game logic. It does NOT own the bottom-panel
    ///     shell (EndTurnView reveals/hides that) — it only swaps the context content between the filled blocks
    ///     (a hex is selected) and the empty placeholder (nothing selected). The full-screen layers stay
    ///     click-through so empty-area clicks reach the map, but the bottom panel itself blocks clicks.
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

        private const string ChipClass = "chip";
        private const string ChipIconClass = "chip-icon";
        private const string ChipLabelClass = "chip-label";

        [SerializeField] private UIDocument _document;

        private VisualElement _contextFilled;
        private VisualElement _contextEmpty;
        private VisualElement _headerIcon;
        private Label _headerTitle;
        private VisualElement _resourcesSection;
        private VisualElement _resourcesContainer;
        private VisualElement _districtSection;
        private VisualElement _productionSection;

        // Managed UI elements → System.Collections.Generic (NativeContainer holds unmanaged only).
        private readonly List<VisualElement> _chipPool = new();
        private bool _cached;

        private void Start()
        {
            ConfigurePicking();
        }

        /// <summary>
        ///     Makes the full-screen layers (the document root + the "Root" container) click-through so
        ///     empty-area clicks reach the map, while the bottom panel stays pickable so it blocks clicks.
        ///     Picking does not propagate from a parent to its children, so leaving the panel and its children
        ///     at the default pickable mode is enough — only the two full-screen ancestors are disabled.
        ///     HexSelectionSystem then skips selection over the panel via EventSystem.IsPointerOverGameObject.
        /// </summary>
        public void ConfigurePicking()
        {
            EnsureCached();
            _document.rootVisualElement.EnablePicking(false);
            _document.rootVisualElement.Q<VisualElement>(RootName)?.EnablePicking(false);
        }

        /// <summary>A hex is selected: show the filled context blocks, hide the empty placeholder.</summary>
        public void ShowSelection()
        {
            EnsureCached();
            _contextFilled.style.display = DisplayStyle.Flex;
            _contextEmpty.style.display = DisplayStyle.None;
        }

        /// <summary>Nothing selected: hide the filled blocks, show the empty placeholder. The shell stays up.</summary>
        public void ShowEmpty()
        {
            EnsureCached();
            _contextFilled.style.display = DisplayStyle.None;
            _contextEmpty.style.display = DisplayStyle.Flex;
        }

        public void SetHeader(Sprite icon, string title)
        {
            EnsureCached();
            SetBackground(_headerIcon, icon);
            _headerTitle.text = title;
        }

        public void SetResources(IReadOnlyList<ResourceChip> resources)
        {
            EnsureCached();

            for (var index = 0; index < resources.Count; index++)
            {
                var chip = GetOrCreateChip(index);
                SetBackground(chip.Q<VisualElement>(className: ChipIconClass), resources[index].Icon);
                chip.Q<Label>(className: ChipLabelClass).text = resources[index].Label;
                chip.style.display = DisplayStyle.Flex;
            }

            for (var index = resources.Count; index < _chipPool.Count; index++)
                _chipPool[index].style.display = DisplayStyle.None;

            _resourcesSection.style.display = DisplayStyle.Flex;
        }

        public void HideResources()
        {
            EnsureCached();
            _resourcesSection.style.display = DisplayStyle.None;
        }

        /// <summary>
        ///     Toggles the District-economy scaffold — both the District block (Район) and the "Праця та
        ///     виробництво" production block. They share the same backing data, so they appear and disappear
        ///     together. Hidden by HexInfoPanelDistrictPlaceholderSystem until that data lands.
        /// </summary>
        public void SetDistrictVisible(bool visible)
        {
            EnsureCached();
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _districtSection.style.display = display;
            _productionSection.style.display = display;
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

            // Chips keep the default pickable mode — the card already blocks map clicks across its whole area.
            var chip = new VisualElement { name = $"chip-{index}" };
            chip.AddToClassList(ChipClass);

            var icon = new VisualElement();
            icon.AddToClassList(ChipIconClass);
            chip.Add(icon);

            var label = new Label();
            label.AddToClassList(ChipLabelClass);
            chip.Add(label);

            _resourcesContainer.Add(chip);
            _chipPool.Add(chip);
            return chip;
        }

        private void EnsureCached()
        {
            if (_cached)
                return;

            var root = _document.rootVisualElement;
            _contextFilled = root.Q<VisualElement>(ContextFilledName);
            _contextEmpty = root.Q<VisualElement>(ContextEmptyName);
            _headerIcon = root.Q<VisualElement>(HeaderIconName);
            _headerTitle = root.Q<Label>(HeaderTitleName);
            _resourcesSection = root.Q<VisualElement>(ResourcesSectionName);
            _resourcesContainer = root.Q<VisualElement>(ResourcesContainerName);
            _districtSection = root.Q<VisualElement>(DistrictSectionName);
            _productionSection = root.Q<VisualElement>(ProductionSectionName);
            _cached = true;

            // Strip the editor-preview sample chips authored in UXML so the runtime chip pool starts clean;
            // the resources system rebuilds chips from real data.
            _resourcesContainer.Clear();
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
