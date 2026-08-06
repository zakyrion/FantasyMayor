using System;
using Core;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Utils;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Cameras.Components;
using Presentation.Archetypes;
using Presentation.HexIcons.Components;
using Presentation.Terrain.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Presentation.HexIcons.Systems
{
    /// <summary>
    ///     Per-frame positioner for the per-hex icon containers. Each container is a parallel entity
    ///     (<see cref="HexIdFKComponent" /> + <see cref="HexIconContainerComponent" />); every frame this
    ///     re-projects the hex's real on-mesh center (from the <see cref="VertexGrid" /> — the same geometry
    ///     the selection outline uses) to the screen-space panel so the container tracks the camera.
    ///     Runs in LateUpdate so it projects AFTER CameraMovementSystem has moved the camera this frame —
    ///     in Update it would lag the camera by one frame and the icons would visibly slide. Runs
    ///     unconditionally (no camera-moved gate yet).
    ///     Sizing is pure per-hex perspective (focus depth / hex depth), so the same hex is bigger near the
    ///     bottom of the screen and smaller in the distance. There is no map-wide zoom term: zoom is a
    ///     fixed-FOV dolly and the depth ratio is dolly-invariant, so icons keep a constant on-screen size.
    ///     Needs a PreUpdate hook the plain <see cref="LateUpdatedSystem" /> base does not have, so this
    ///     implements <see cref="ILateUpdatedSystem" /> directly — same precedent as
    ///     <c>TurnProcessorSystem</c>/<c>BuildDistrictCompletionSystem</c>. <see cref="IDisposable" /> is added
    ///     on top (not required by the interface) so VContainer still disposes the frame-stamped
    ///     <see cref="FrameBox{T}" /> — per-frame systems are DI-registered concrete, so the container disposes
    ///     any resolved instance implementing it regardless of the registered interface.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexIconsContainerPositionSystem : ILateUpdatedSystem, IDisposable
    {
        private readonly EntityStorages _storages;
        private readonly Archetype _containerSet;

        // The frame's shared projection inputs — panel/camera/grid, the world Y offset, and the focus depth —
        // computed once in PreUpdate and read by the per-entity Update. Held in a frame-stamped FrameBox so the
        // references are valid only within this tick: a stale read throws, and the box drops the references on
        // Dispose so a destroyed Camera/VertexGrid never dangles on this singleton system between ticks. The
        // system holds no cross-frame state.
        [StateAllowed("Per-frame projection inputs; frame-stamped FrameBox, filled in PreUpdate, valid one frame.")]
        private FrameBox<FramePose> _pose;

        public int Priority => SystemPriorities.RuntimeTick.HexIconsContainerPosition;

        public HexIconsContainerPositionSystem(EntityStorages storages)
        {
            _storages = storages;
            _containerSet = PresentationArchetypes.HexIconContainer(storages.World);
        }

        public void Update(GameState state)
        {
            PreUpdate(state);
            foreach (var entity in _containerSet.Entities)
                Update(state, entity);
        }

        // Projects the hex's real on-mesh center to the screen-space panel and pins the container there.
        // Camera.WorldToScreenPoint yields BOTTOM-left-origin pixels (Y up), while ScreenToPanel expects
        // TOP-left-origin screen Y — so we flip with `Screen.height − y` before converting (Unity's
        // documented world-anchored-UI pattern). Missing grid cell / behind-camera → hide the container.
        private void Update(GameState state, in Entity entity)
        {
            var pose = _pose.Value;

            var coords = entity.GetComponent<HexIdFKComponent>().Coords;
            var container = entity.GetComponent<HexIconContainerComponent>().Container;

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

            // Per-hex perspective only: scale = focusDepth / hexDepth. The factor is 1 at the focus point,
            // >1 nearer the camera (bottom of screen → bigger) and <1 farther (distance → smaller) — the
            // screenshot look. There is NO map-wide zoom term: zoom is now a fixed-FOV dolly, and this depth
            // ratio is dolly-invariant (both depths scale together), so icons keep a constant on-screen size
            // at every zoom level. Equivalent to a true per-hex pixels-per-world-unit (pppu ∝ 1/depth) but
            // reuses screenPoint.z, no extra projection. The focusDepth > 0 guard keeps it at factor 1 on a
            // degenerate focus ray. Scale is around the element's center (default transform-origin), so it
            // composes with the creation-time `-50% -50%` centering translate without shifting the anchor
            // (icons stay pinned, no sliding).
            var finalScale = pose.FocusDepth > 0f ? pose.FocusDepth / screenPoint.z : 1f;
            container.style.scale = new Scale(new Vector2(finalScale, finalScale));
        }

        public void Dispose()
        {
            _pose.Dispose();
        }

        private void PreUpdate(GameState state)
        {
            if (!_storages.Singletons.Has<HexIconsViewComponent>())
                throw new InvalidOperationException(
                    "HexIconsContainerPositionSystem: HexIconsViewComponent is missing.");
            if (!_storages.Singletons.Has<CameraComponent>())
                throw new InvalidOperationException(
                    "HexIconsContainerPositionSystem: CameraComponent is missing.");

            var panel = _storages.Singletons.Get<HexIconsViewComponent>().View.Root.panel;
            if (panel == null)
                throw new InvalidOperationException("HexIconsContainerPositionSystem: panel is not ready.");

            var camera = _storages.Singletons.Get<CameraComponent>().Camera;
            if (camera == null)
                throw new InvalidOperationException("HexIconsContainerPositionSystem: scene camera is null.");

            if (!_storages.Singletons.Has<VertexGridComponent>())
                throw new InvalidOperationException(
                    "HexIconsContainerPositionSystem: VertexGridComponent is missing.");
            var grid = _storages.Singletons.Get<VertexGridComponent>().Grid;
            if (grid == null)
                throw new InvalidOperationException("HexIconsContainerPositionSystem: VertexGrid is null.");

            if (!_storages.Singletons.Has<HexIconsConfigComponent>())
                throw new InvalidOperationException(
                    "HexIconsContainerPositionSystem: HexIconsConfigComponent is missing.");

            var worldYOffset = _storages.Singletons.Get<HexIconsConfigComponent>().Value.WorldYOffset;
            var focusDepth = ComputeFocusDepth(camera);

            _pose = FrameBox<FramePose>.OneFrame(
                new FramePose(panel, camera, grid, worldYOffset, focusDepth));
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

        // This frame's shared projection inputs, broadcast from PreUpdate to the per-entity Update. Private and
        // nested: it is an internal transport for one tick, not a reusable type.
        private readonly struct FramePose
        {
            public readonly IPanel Panel;
            public readonly Camera Camera;
            public readonly VertexGrid Grid;
            public readonly float WorldYOffset;
            public readonly float FocusDepth;

            public FramePose(IPanel panel, Camera camera, VertexGrid grid, float worldYOffset,
                float focusDepth)
            {
                Panel = panel;
                Camera = camera;
                Grid = grid;
                WorldYOffset = worldYOffset;
                FocusDepth = focusDepth;
            }
        }
    }
}
