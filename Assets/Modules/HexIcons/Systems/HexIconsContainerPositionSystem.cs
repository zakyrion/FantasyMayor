using System;
using Core;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Cameras.Components;
using Modules.HexCore.Components;
using Modules.HexesCore.Utils;
using Modules.HexIcons.Components;
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
    ///     unconditionally (no camera-moved gate yet).
    ///     Sizing is per-hex perspective: a map-wide zoom baseline (<see cref="ComputeZoomScale" />) times a
    ///     per-hex factor (focus depth / hex depth), so the same hex is bigger near the bottom of the screen
    ///     and smaller in the distance, while still growing/shrinking with zoom.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexIconsContainerPositionSystem : LateUpdatedSystem
    {
        // Must be > CameraMovementSystem (LateUpdate, priority 0) so it re-projects after the camera moves
        // this frame; otherwise the icons lag the camera by one frame and slide.
        private const int ExecutionPriority = 700;

        private readonly World _world;

        // The frame's shared projection inputs — panel/camera/grid, the world Y offset, and the resolved
        // zoom scale + focus depth — computed once in PreUpdate and read by the per-entity Update. Held in a
        // frame-stamped FrameBox so the references are valid only within this tick: a stale read throws, and
        // the box drops the references on Dispose so a destroyed Camera/VertexGrid never dangles on this
        // singleton system between ticks. The system holds no cross-frame state — the zoom baseline is the
        // camera's startup FOV (CameraComponent.ReferenceFieldOfView), not a latched runtime value.
        [StateAllowed("Per-frame projection inputs; frame-stamped FrameBox, filled in PreUpdate, valid one frame.")]
        private FrameBox<FramePose> _pose;

        public override int Priority => ExecutionPriority;

        public HexIconsContainerPositionSystem(World world)
            : base(world.GetEntities()
                .With<HexIdComponent>()
                .With<HexIconContainerComponent>()
                .AsSet())
        {
            _world = world;
        }

        // Projects the hex's real on-mesh center to the screen-space panel and pins the container there.
        // Camera.WorldToScreenPoint yields BOTTOM-left-origin pixels (Y up), while ScreenToPanel expects
        // TOP-left-origin screen Y — so we flip with `Screen.height − y` before converting (Unity's
        // documented world-anchored-UI pattern). Missing grid cell / behind-camera → hide the container.
        protected override void Update(GameState state, in Entity entity)
        {
            var pose = _pose.Value;

            var coords = entity.Get<HexIdComponent>().Coords;
            var container = entity.Get<HexIconContainerComponent>().Container;

            var centerCoord = pose.Grid.GetCenterVertexCoord(coords);
            if (!pose.Grid.TryGet(centerCoord, out var centerVertex))
            {
                container.style.display = DisplayStyle.None;
                return;
            }

            // Lift the anchor in world space so the icon floats above the hex; the offset foreshortens with
            // perspective, and the lifted point's depth (.z) feeds both the position and the per-hex scale.
            Vector3 worldPoint = centerVertex.Position;
            worldPoint.y += pose.WorldYOffset;
            var screenPoint = pose.Camera.WorldToScreenPoint(worldPoint);

            // z <= 0 means the hex is behind the camera; its screen coords are mirrored garbage, so hide it.
            if (screenPoint.z <= 0f)
            {
                container.style.display = DisplayStyle.None;
                return;
            }

            var screenForPanel = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            var panelPoint = RuntimePanelUtils.ScreenToPanel(pose.Panel, screenForPanel);
            container.style.display = DisplayStyle.Flex;
            container.style.left = panelPoint.x;
            container.style.top = panelPoint.y;

            // Per-hex perspective layered on the zoom baseline: scale = zoomScale * (focusDepth / hexDepth).
            // The factor is 1 at the focus point, >1 nearer the camera (bottom of screen → bigger) and <1
            // farther (distance → smaller) — the screenshot look. Equivalent to a true per-hex
            // pixels-per-world-unit (pppu ∝ 1/depth) but reuses screenPoint.z, no extra projection. The
            // focusDepth > 0 guard keeps it at factor 1 on a degenerate focus ray. Scale is around the
            // element's center (default transform-origin), so it composes with the creation-time
            // `-50% -50%` centering translate without shifting the anchor (icons stay pinned, no sliding).
            var perspective = pose.FocusDepth > 0f ? pose.FocusDepth / screenPoint.z : 1f;
            var finalScale = pose.ZoomScale * perspective;
            container.style.scale = new Scale(new Vector2(finalScale, finalScale));
        }

        public override void Dispose()
        {
            _pose.Dispose();
            base.Dispose();
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

            var cameraComponent = _world.Get<CameraComponent>();
            var camera = cameraComponent.Camera;
            if (camera == null)
                throw new InvalidOperationException("HexIconsContainerPositionSystem: scene camera is null.");

            if (!_world.Has<VertexGridComponent>())
                throw new InvalidOperationException(
                    "HexIconsContainerPositionSystem: VertexGridComponent is missing.");
            var grid = _world.Get<VertexGridComponent>().Grid;
            if (grid == null)
                throw new InvalidOperationException("HexIconsContainerPositionSystem: VertexGrid is null.");

            if (!_world.Has<HexIconsConfigComponent>())
                throw new InvalidOperationException(
                    "HexIconsContainerPositionSystem: HexIconsConfigComponent is missing.");

            var worldYOffset = _world.Get<HexIconsConfigComponent>().Value.WorldYOffset;
            var zoomScale = ComputeZoomScale(camera, cameraComponent.ReferenceFieldOfView);
            var focusDepth = ComputeFocusDepth(camera);

            _pose = FrameBox<FramePose>.OneFrame(
                new FramePose(panel, camera, grid, worldYOffset, zoomScale, focusDepth));
        }

        // View-space depth of the screen-center point on the ground plane (y = 0): the per-hex perspective
        // reference (factor = focusDepth / hexDepth). Returns 0 on a degenerate ray (parallel to the ground /
        // ground behind the camera); Update's `> 0` guard then keeps the perspective factor at 1.
        private float ComputeFocusDepth(Camera camera)
        {
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Mathf.Abs(ray.direction.y) < 1e-5f)
                return 0f;

            var distance = -ray.origin.y / ray.direction.y;
            if (distance <= 0f)
                return 0f;

            var focus = ray.origin + ray.direction * distance;
            return camera.WorldToScreenPoint(focus).z;
        }

        // Map-wide zoom baseline. Zoom is FOV-driven (CameraMovementSystem changes fieldOfView), so the on-
        // screen size of a fixed world span scales with 1/tan(fov/2). Icons are 1× at the camera's startup FOV
        // (ReferenceFieldOfView); zooming in (smaller FOV) enlarges the world, so the factor grows. This is the
        // analytic equivalent of measuring pixels-per-world-unit, but with no latched reference and no per-hex
        // depth term (that is the separate perspective factor in Update) — the focus depth cancels out.
        private float ComputeZoomScale(Camera camera, float referenceFieldOfView)
        {
            var referenceTan = Mathf.Tan(referenceFieldOfView * Mathf.Deg2Rad * 0.5f);
            var currentTan = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            return referenceTan / currentTan;
        }

        // This frame's shared projection inputs, broadcast from PreUpdate to the per-entity Update. Private and
        // nested: it is an internal transport for one tick, not a reusable type.
        private readonly struct FramePose
        {
            public readonly IPanel Panel;
            public readonly Camera Camera;
            public readonly VertexGrid Grid;
            public readonly float WorldYOffset;
            public readonly float ZoomScale;
            public readonly float FocusDepth;

            public FramePose(IPanel panel, Camera camera, VertexGrid grid, float worldYOffset,
                float zoomScale, float focusDepth)
            {
                Panel = panel;
                Camera = camera;
                Grid = grid;
                WorldYOffset = worldYOffset;
                ZoomScale = zoomScale;
                FocusDepth = focusDepth;
            }
        }
    }
}
