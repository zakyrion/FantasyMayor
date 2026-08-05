using System;
using System.Collections.Generic;
using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.Archetypes;
using Presentation.HexIcons.Components;
using Presentation.HexIcons.Configs;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;
using Presentation.UI.Archetypes;
using Presentation.UI.MainHud.HexInfoPanel.Components;
using Presentation.UI.MainHud.HexInfoPanel.Views;
using UnityEngine;

namespace Presentation.UI.MainHud.HexInfoPanel.Systems
{
    /// <summary>
    ///     Fills the resources block with one square icon tile per resource on the selected hex, or hides it
    ///     when the hex has none (or nothing is selected). Reactive on <see cref="SelectedHexChangedEvent" />;
    ///     reads the current HexSelectedComponent for the coordinate. Sprites are reused from HexIcons'
    ///     resource-icon config; the tiles are icon-only, so the resource name is carried as a hover tooltip
    ///     (the resource type until a localized resource-name source exists).
    /// </summary>
    [UsedImplicitly]
    public sealed class HexInfoPanelResourcesSystem : UpdatedSystem
    {
        private readonly EntityStorages _storages;
        private readonly Archetype _viewSet;
        private readonly Archetype _selectedHexSet;
        private readonly Archetype _resourceSet;

        // Managed UI payload → System.Collections.Generic. Reused buffer to avoid per-refresh allocation.
        private readonly List<HexInfoPanelView.ResourceChip> _chips = new();

        public override int Priority => SystemPriorities.RuntimeTick.HexInfoPanelResources;

        public HexInfoPanelResourcesSystem(EntityStorages storages)
            : base(storages.World, EventArchetypes.Of<SelectedHexChangedEvent>(storages.World))
        {
            _storages = storages;
            _viewSet = PresentationUIArchetypes.HexInfoPanel(storages.World);
            _selectedHexSet = PresentationArchetypes.HexSelection(storages.World);
            _resourceSet = MapArchetypes.HexResource(storages.World);
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (!EcsEventExtensions.IsRipe(entity))
                return;

            if (!_viewSet.TryGetFirst(out var viewEntity))
                return;

            var view = viewEntity.GetComponent<HexInfoPanelViewComponent>().View;
            if (view == null)
                return;

            if (!_selectedHexSet.TryGetFirst(out var selectedHexEntity))
            {
                view.HideResources();
                return;
            }

            if (!_storages.World.HasWorldComponent<HexResourceIconConfigComponent>())
                throw new InvalidOperationException(
                    "HexInfoPanelResourcesSystem: HexResourceIconConfigComponent is missing.");

            var coords = selectedHexEntity.GetComponent<HexSelectedComponent>().Coords;
            var entries = _storages.World.GetWorldComponent<HexResourceIconConfigComponent>().Value.Entries;

            _chips.Clear();
            foreach (var resourceEntity in _resourceSet.Entities)
            {
                if (resourceEntity.GetComponent<HexIdFKComponent>().Coords != coords)
                    continue;

                var type = resourceEntity.GetComponent<HexResourceComponent>().Type;

                // A missing sprite is fine — the chip still shows the USS placeholder, so we keep the result
                // regardless of the lookup outcome.
                TryGetSprite(entries, type, out var sprite);
                _chips.Add(new HexInfoPanelView.ResourceChip(sprite, type.ToString()));
            }

            if (_chips.Count == 0)
            {
                view.HideResources();
                return;
            }

            view.SetResources(_chips);
        }

        private bool TryGetSprite(IReadOnlyList<HexResourceIconConfig.ResourceIconEntry> entries,
            HexResourceType type, out Sprite sprite)
        {
            foreach (var entry in entries)
            {
                if (entry.HexResourceType != type || entry.Sprite == null)
                    continue;

                sprite = entry.Sprite;
                return true;
            }

            sprite = null;
            return false;
        }
    }
}
