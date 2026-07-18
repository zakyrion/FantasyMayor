using System;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.Hex.Tags;
using Presentation.UI.MainHud.HexInfoPanel.Components;
using Presentation.UI.MainHud.HexInfoPanel.Configs;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;
using UnityEngine;
using Presentation.Terrain.Tags;
using Presentation.UI.Tags;

namespace Presentation.UI.MainHud.HexInfoPanel.Systems
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
        private readonly EntityStore _world;
        private readonly ArchetypeQuery _viewSet;
        private readonly ArchetypeQuery _selectedHexSet;
        private readonly ArchetypeQuery _hexSet;

        public override int Priority => SystemPriorities.RuntimeTick.HexInfoPanelHeader;

        public HexInfoPanelHeaderSystem(EntityStore world)
            : base(world.Query<SelectedHexChangedEvent>())
        {
            _world = world;
            _viewSet = world.Query<HexInfoPanelViewComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<UITag>());
            _selectedHexSet = world.Query<HexSelectedComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexSelectionTag>());
            _hexSet = world.Query<HexIdComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexTag>());
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (!EcsEventExtensions.IsRipe(entity))
                return;

            if (!_viewSet.TryGetFirst(out var viewEntity) || !_selectedHexSet.TryGetFirst(out var selectedHexEntity))
                return;

            if (!_world.HasWorldComponent<HexTerrainIconConfigComponent>())
                throw new InvalidOperationException(
                    "HexInfoPanelHeaderSystem: HexTerrainIconConfigComponent is missing.");

            var coords = selectedHexEntity.GetComponent<HexSelectedComponent>().Coords;
            var view = viewEntity.GetComponent<HexInfoPanelViewComponent>().View;
            if (view == null)
                return;

            // A non-grid coordinate carries no hex — a valid empty selection (the panel shows empty), so skip
            // rather than fill a header for a nonexistent hex.
            if (!TryGetHexTerrainType(coords, out var terrainType))
                return;

            var config = _world.GetWorldComponent<HexTerrainIconConfigComponent>().Value;
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

            foreach (var hexEntity in _hexSet.Entities)
            {
                if (hexEntity.GetComponent<HexIdComponent>().Coords != coords)
                    continue;

                type = hexEntity.GetComponent<HexTypeComponent>().Type;
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
    }
}
