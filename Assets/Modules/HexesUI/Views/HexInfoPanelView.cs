using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.AppUI.UI.VisualElementExtensions;

namespace Modules.HexesUI.Views
{
    /// <summary>
    ///     View layer for the hex info panel. Owns the UIDocument's VisualElements and exposes a per-block
    ///     populate API for the panel systems. Holds no game logic. The full-screen layers stay click-through
    ///     so empty-area clicks reach the map, but the panel card blocks clicks — clicking the card does not
    ///     select/deselect a hex behind it.
    /// </summary>
    public sealed class HexInfoPanelView : MonoBehaviour
    {
        private const string RootName = "Root";
        private const string PanelName = "HexInfoPanelView";
        private const string HeaderIconName = "HeaderIcon";
        private const string HeaderTitleName = "HeaderTitle";
        private const string HeaderCoordName = "HeaderCoord";
        private const string ResourcesSectionName = "ResourcesSection";
        private const string ResourcesContainerName = "ResourcesContainer";
        private const string DistrictSectionName = "DistrictSection";

        private const string ChipClass = "chip";
        private const string ChipIconClass = "chip-icon";
        private const string ChipLabelClass = "chip-label";

        [SerializeField] private UIDocument _document;

        private VisualElement _panel;
        private VisualElement _headerIcon;
        private Label _headerTitle;
        private Label _headerCoord;
        private VisualElement _resourcesSection;
        private VisualElement _resourcesContainer;
        private VisualElement _districtSection;

        // Managed UI elements → System.Collections.Generic (NativeContainer holds unmanaged only).
        private readonly List<VisualElement> _chipPool = new();
        private bool _cached;

        private void Start()
        {
            ConfigurePicking();
        }

        /// <summary>
        ///     Makes the full-screen layers (the document root + the "Root" container) click-through so
        ///     empty-area clicks reach the map, while the panel card stays pickable so it blocks clicks.
        ///     Picking does not propagate from a parent to its children, so leaving the card and its children
        ///     at the default pickable mode is enough — only the two full-screen ancestors are disabled.
        ///     HexSelectionSystem then skips selection over the card via EventSystem.IsPointerOverGameObject.
        /// </summary>
        public void ConfigurePicking()
        {
            EnsureCached();
            _document.rootVisualElement.EnablePicking(false);
            _document.rootVisualElement.Q<VisualElement>(RootName)?.EnablePicking(false);
        }

        public void Show()
        {
            EnsureCached();
            _panel.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            EnsureCached();
            _panel.style.display = DisplayStyle.None;
        }

        public void SetHeader(Sprite icon, string title, string coord)
        {
            EnsureCached();
            SetBackground(_headerIcon, icon);
            _headerTitle.text = title;
            _headerCoord.text = coord;
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

        public void SetDistrictVisible(bool visible)
        {
            EnsureCached();
            _districtSection.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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
            _panel = root.Q<VisualElement>(PanelName);
            _headerIcon = root.Q<VisualElement>(HeaderIconName);
            _headerTitle = root.Q<Label>(HeaderTitleName);
            _headerCoord = root.Q<Label>(HeaderCoordName);
            _resourcesSection = root.Q<VisualElement>(ResourcesSectionName);
            _resourcesContainer = root.Q<VisualElement>(ResourcesContainerName);
            _districtSection = root.Q<VisualElement>(DistrictSectionName);
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
