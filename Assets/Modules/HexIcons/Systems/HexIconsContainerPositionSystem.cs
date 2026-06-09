using System;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Cameras.Components;
using Modules.HexCore.Components;
using Modules.HexIcons.Components;
using Modules.HexesCore.Utils;
using Modules.TerrainView.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modules.HexIcons.Systems
{
    /// <summary>
    ///     Per-frame positioner for the per-hex icon containers. Each container is a parallel entity
    ///     (<see cref="HexIdComponent" /> FK + <see cref="HexIconContainerComponent" />); every frame this
    ///     re-projects the hex's real on-mesh center (from the <see cref="VertexGrid" /> — the same geometry
    ///     the selection outline uses) to the screen-space panel so the container tracks the camera.
    ///     Runs in LateUpdate so it projects AFTER CameraMovementSystem has moved the camera this frame —
    ///     in Update it would lag the camera by one frame and the icons would visibly slide. Runs
    ///     unconditionally (no camera-moved gate yet — see HEXICONS.md "Next Step").
    /// </summary>
    [UsedImplicitly]
    public sealed class HexIconsContainerPositionSystem : LateUpdatedSystem
    {
        // Must be > CameraMovementSystem (LateUpdate, priority 0) so it re-projects after the camera moves
        // this frame; otherwise the icons lag the camera by one frame and slide.
        private const int ExecutionPriority = 700;

        private readonly World _world;
        private readonly EntitySet _vertexGridSet;

        // Resolved once per frame in PreUpdate, consumed by the per-entity Update.
        private IPanel _panel;
        private Camera _camera;
        private VertexGrid _grid;

        public override int Priority => ExecutionPriority;

        public HexIconsContainerPositionSystem(World world)
            : base(world.GetEntities()
                .With<HexIdComponent>()
                .With<HexIconContainerComponent>()
                .AsSet())
        {
            _world = world;
            _vertexGridSet = world.GetEntities()
                .With<VertexGridComponent>()
                .AsSet();
        }

        protected override void PreUpdate(GameState state)
        {
            if (!_world.Has<HexIconsViewComponent>())
                throw new InvalidOperationException(
                    "HexIconsContainerPositionSystem: HexIconsViewComponent is missing.");
            if (!_world.Has<CameraComponent>())
                throw new InvalidOperationException(
                    "HexIconsContainerPositionSystem: CameraComponent is missing.");

            var panel = _world.Get<HexIconsViewComponent>().View.Root.panel;
            if (panel == null)
                throw new InvalidOperationException("HexIconsContainerPositionSystem: panel is not ready.");

            var camera = _world.Get<CameraComponent>().Camera;
            if (camera == null)
                throw new InvalidOperationException("HexIconsContainerPositionSystem: scene camera is null.");

            if (_vertexGridSet.Count == 0)
                throw new InvalidOperationException(
                    "HexIconsContainerPositionSystem: VertexGridComponent is missing.");
            var grid = _vertexGridSet.GetEntities()[0].Get<VertexGridComponent>().Grid;
            if (grid == null)
                throw new InvalidOperationException("HexIconsContainerPositionSystem: VertexGrid is null.");

            _panel = panel;
            _camera = camera;
            _grid = grid;
        }

        // Projects the hex's real on-mesh center to the screen-space panel and pins the container there.
        // Camera.WorldToScreenPoint yields BOTTOM-left-origin pixels (Y up), while ScreenToPanel expects
        // TOP-left-origin screen Y — so we flip with `Screen.height − y` before converting (Unity's
        // documented world-anchored-UI pattern). Missing grid cell / behind-camera → hide the container.
        protected override void Update(GameState state, in Entity entity)
        {
            var coords = entity.Get<HexIdComponent>().Coords;
            var container = entity.Get<HexIconContainerComponent>().Container;

            var centerCoord = _grid.GetCenterVertexCoord(coords);
            if (!_grid.TryGet(centerCoord, out var centerVertex))
            {
                container.style.display = DisplayStyle.None;
                return;
            }

            Vector3 worldPoint = centerVertex.Position;
            var screenPoint = _camera.WorldToScreenPoint(worldPoint);

            // z <= 0 means the hex is behind the camera; its screen coords are mirrored garbage, so hide it.
            if (screenPoint.z <= 0f)
            {
                container.style.display = DisplayStyle.None;
                return;
            }

            var screenForPanel = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            var panelPoint = RuntimePanelUtils.ScreenToPanel(_panel, screenForPanel);
            container.style.display = DisplayStyle.Flex;
            container.style.left = panelPoint.x;
            container.style.top = panelPoint.y;
        }
    }
}
