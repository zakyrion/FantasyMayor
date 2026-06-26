using System;
using System.Collections.Generic;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Presentation.HexIcons.Components;
using Presentation.HexIcons.Configs;
using Presentation.UI.HexInfoPanel.Components;
using Presentation.UI.HexInfoPanel.Views;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;
using UnityEngine;

namespace Presentation.UI.HexInfoPanel.Systems
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
        private const int ExecutionPriority = 561;

        private readonly World _world;
        private readonly EntitySet _viewSet;
        private readonly EntitySet _selectedHexSet;
        private readonly EntitySet _resourceSet;

        // Managed UI payload → System.Collections.Generic. Reused buffer to avoid per-refresh allocation.
        private readonly List<HexInfoPanelView.ResourceChip> _chips = new();

        public override int Priority => ExecutionPriority;

        public HexInfoPanelResourcesSystem(World world)
            : base(world.GetEntities().With<SelectedHexChangedEvent>().AsSet())
        {
            _world = world;
            _viewSet = world.GetEntities().With<HexInfoPanelViewComponent>().AsSet();
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().AsSet();
            _resourceSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexResourcesComponent>()
                .AsSet();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (_viewSet.Count == 0)
                return;

            var view = _viewSet.GetEntities()[0].Get<HexInfoPanelViewComponent>().View;
            if (view == null)
                return;

            if (_selectedHexSet.Count == 0)
            {
                view.HideResources();
                return;
            }

            if (!_world.Has<HexResourceIconConfigComponent>())
                throw new InvalidOperationException(
                    "HexInfoPanelResourcesSystem: HexResourceIconConfigComponent is missing.");

            var coords = _selectedHexSet.GetEntities()[0].Get<HexSelectedComponent>().Coords;
            var entries = _world.Get<HexResourceIconConfigComponent>().Value.Entries;

            _chips.Clear();
            foreach (var resourceEntity in _resourceSet.GetEntities())
            {
                if (resourceEntity.Get<HexIdComponent>().Coords != coords)
                    continue;

                var type = resourceEntity.Get<HexResourcesComponent>().Type;

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
            ResourceType type, out Sprite sprite)
        {
            foreach (var entry in entries)
            {
                if (entry.ResourceType != type || entry.Sprite == null)
                    continue;

                sprite = entry.Sprite;
                return true;
            }

            sprite = null;
            return false;
        }

        public override void Dispose()
        {
            _viewSet.Dispose();
            _selectedHexSet.Dispose();
            _resourceSet.Dispose();
            base.Dispose();
        }
    }
}
