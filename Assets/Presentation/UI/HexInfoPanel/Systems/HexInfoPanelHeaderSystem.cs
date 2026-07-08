using System;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.Hex.Tags;
using Presentation.UI.HexInfoPanel.Components;
using Presentation.UI.HexInfoPanel.Configs;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;
using UnityEngine;

namespace Presentation.UI.HexInfoPanel.Systems
{
    /// <summary>
    ///     Fills the hex header (terrain icon + name) on a selection change. Reactive on
    ///     <see cref="SelectedHexChangedEvent" />: reads the current HexSelectedComponent, resolves it to its hex
    ///     entity and terrain tag. Skips gracefully when nothing — or a coordinate with no hex — is selected (a
    ///     non-grid click is a valid empty selection; HexInfoPanelSystem shows the empty panel).
    /// </summary>
    [UsedImplicitly]
    public sealed class HexInfoPanelHeaderSystem : UpdatedSystem
    {
        private readonly World _world;
        private readonly EntitySet _viewSet;
        private readonly EntitySet _selectedHexSet;
        private readonly EntitySet _hexSet;

        public override int Priority => SystemPriorities.RuntimeTick.HexInfoPanelHeader;

        public HexInfoPanelHeaderSystem(World world)
            : base(world.GetEntities().With<SelectedHexChangedEvent>().AsSet())
        {
            _world = world;
            _viewSet = world.GetEntities().With<HexInfoPanelViewComponent>().AsSet();
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().AsSet();
            _hexSet = world.GetEntities().With<HexTag>().With<HexIdComponent>().AsSet();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (_viewSet.Count == 0 || _selectedHexSet.Count == 0)
                return;

            if (!_world.Has<HexTerrainIconConfigComponent>())
                throw new InvalidOperationException(
                    "HexInfoPanelHeaderSystem: HexTerrainIconConfigComponent is missing.");

            var coords = _selectedHexSet.GetEntities()[0].Get<HexSelectedComponent>().Coords;
            var view = _viewSet.GetEntities()[0].Get<HexInfoPanelViewComponent>().View;
            if (view == null)
                return;

            // A non-grid coordinate carries no hex — a valid empty selection (the panel shows empty), so skip
            // rather than fill a header for a nonexistent hex.
            if (!TryGetHexTerrainType(coords, out var terrainType))
                return;

            var config = _world.Get<HexTerrainIconConfigComponent>().Value;
            if (!TryGetTerrainEntry(config, terrainType, out var sprite, out var displayName))
            {
                // No authored entry yet — the out values already carry the fallback (enum name + null icon,
                // which falls back to the USS placeholder).
            }

            view.SetHeader(sprite, displayName);
        }

        private bool TryGetHexTerrainType(HexCoord coords, out HexType type)
        {
            type = default;

            foreach (var hexEntity in _hexSet.GetEntities())
            {
                if (hexEntity.Get<HexIdComponent>().Coords != coords)
                    continue;

                type = hexEntity.Get<HexTypeComponent>().Type;
                return true;
            }

            return false;
        }

        private bool TryGetTerrainEntry(HexTerrainIconConfig config, HexType type, out Sprite sprite,
            out string displayName)
        {
            foreach (var entry in config.Entries)
            {
                if (entry.Type != type)
                    continue;

                sprite = entry.Sprite;
                displayName = entry.DisplayName;
                return true;
            }

            sprite = null;
            displayName = type.ToString();
            return false;
        }

        public override void Dispose()
        {
            _viewSet.Dispose();
            _selectedHexSet.Dispose();
            _hexSet.Dispose();
            base.Dispose();
        }
    }
}
