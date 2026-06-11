using System;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.HexCore.Components;
using Modules.HexCore.Tags;
using Modules.HexesUI.Components;
using Modules.HexesUI.Configs;
using Modules.HexesUI.Data;
using Modules.HexesUI.Events;
using Modules.HexesUI.Views;
using UnityEngine;

namespace Modules.HexesUI.Systems
{
    /// <summary>
    ///     Fills the always-present header (terrain icon + name + coordinate) on a panel refresh. Resolves the
    ///     selected coordinate to its hex entity and reads its terrain tag.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexInfoPanelHeaderSystem : UpdatedSystem
    {
        private const int ExecutionPriority = 560;

        private readonly World _world;
        private readonly EntitySet _viewSet;
        private readonly EntitySet _hexSet;

        public override int Priority => ExecutionPriority;

        public HexInfoPanelHeaderSystem(World world)
            : base(world.GetEntities().With<HexInfoPanelRefreshEvent>().AsSet())
        {
            _world = world;
            _viewSet = world.GetEntities().With<HexInfoPanelViewComponent>().AsSet();
            _hexSet = world.GetEntities().With<HexTag>().With<HexIdComponent>().AsSet();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (_viewSet.Count == 0)
                return;

            if (!_world.Has<HexTerrainIconConfigComponent>())
                throw new InvalidOperationException(
                    "HexInfoPanelHeaderSystem: HexTerrainIconConfigComponent is missing.");

            var coords = entity.Get<HexInfoPanelRefreshEvent>().Coords;
            var view = _viewSet.GetEntities()[0].Get<HexInfoPanelViewComponent>().View;
            if (view == null)
                return;

            // The controller only refreshes for real hexes, so a missing terrain tag is a broken invariant.
            if (!TryGetHexTerrainType(coords, out var terrainType))
                throw new InvalidOperationException(
                    $"HexInfoPanelHeaderSystem: no hex with a terrain tag at {coords}.");

            var config = _world.Get<HexTerrainIconConfigComponent>().Value;
            if (!TryGetTerrainEntry(config, terrainType, out var sprite, out var displayName))
            {
                // No authored entry yet — the out values already carry the fallback (enum name + null icon,
                // which falls back to the USS placeholder).
            }

            view.SetHeader(sprite, displayName, coords.ToString());
        }

        private bool TryGetHexTerrainType(HexCoord coords, out HexTerrainType terrainType)
        {
            terrainType = default;

            foreach (var hexEntity in _hexSet.GetEntities())
            {
                if (hexEntity.Get<HexIdComponent>().Coords != coords)
                    continue;

                if (hexEntity.Has<HexPlainTag>())
                    terrainType = HexTerrainType.Plain;
                else if (hexEntity.Has<HexMountTag>())
                    terrainType = HexTerrainType.Mount;
                else if (hexEntity.Has<HexBedhillTag>())
                    terrainType = HexTerrainType.Bedhill;
                else if (hexEntity.Has<HexWaterTag>())
                    terrainType = HexTerrainType.Water;
                else
                    return false;

                return true;
            }

            return false;
        }

        private bool TryGetTerrainEntry(HexTerrainIconConfig config, HexTerrainType type, out Sprite sprite,
            out string displayName)
        {
            foreach (var entry in config.Entries)
            {
                if (entry.TerrainType != type)
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
            _hexSet.Dispose();
            base.Dispose();
        }
    }
}
