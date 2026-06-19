using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.Cameras.Components;
using Modules.TerrainView.Components;
using Modules.TerrainView.Events;
using Modules.UserInput.Components;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.Mathematics;

namespace Modules.UserInput.Systems
{
    /// <summary>
    ///     Converts left-click pointer input into a selected hex singleton entity.
    ///     Clicking the already selected hex toggles the selection off.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexSelectionSystem : UpdatedSystem
    {
        private const float SelectionPlaneHeight = 0f;

        private readonly EntitySet _playerInputSet;
        private readonly EntitySet _selectedHexSet;
        private readonly World _world;

        private InputAction _clickAction;
        private InputAction _pointAction;

        /// <inheritdoc />
        public override int Priority => 0;

        /// <param name="world">The ECS world used to query camera, config, and selection state.</param>
        public HexSelectionSystem(World world)
            // Anchored on the single PlayerInputComponent entity so Update ticks once per frame;
            // the camera itself is a world component (CameraComponent), read via world.Get below.
            : base(world.GetEntities().With<PlayerInputComponent>().AsSet())
        {
            _world = world;
            _playerInputSet = world.GetEntities()
                .With<PlayerInputComponent>()
                .AsSet();
            _selectedHexSet = world.GetEntities()
                .With<HexSelectedComponent>()
                .AsSet();

            TryBindInputActions();
        }

        /// <inheritdoc />
        protected override void Update(GameState state, in Entity entity)
        {
            if (_clickAction == null || _pointAction == null)
            {
                if (!TryBindInputActions())
                    return;
            }

            if (!_world.Has<TerrainViewConfigComponent>())
                return;

            if (!_clickAction.WasPressedThisFrame())
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (!_world.Has<CameraComponent>())
                return;

            var camera = _world.Get<CameraComponent>().Camera;
            if (camera == null)
                return;

            var cellSize = _world.Get<TerrainViewConfigComponent>().CellSize;
            if (cellSize <= 0f)
                return;

            var clickScreenPosition = _pointAction.ReadValue<Vector2>();
            var clickRay = camera.ScreenPointToRay(clickScreenPosition);
            if (!TryIntersectPlane(clickRay, out var hit))
                return;

            var coord = new HexCoord(AxialMath.WorldToAxial(new float2(hit.x, hit.z), cellSize));
            ApplySelection(coord);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            UnbindInputActions();
            _playerInputSet.Dispose();
            _selectedHexSet.Dispose();
            base.Dispose();
        }

        /// <summary>
        ///     Creates, updates, or removes the singleton <see cref="HexSelectedComponent" /> entity.
        /// </summary>
        /// <param name="coord">Hex that was clicked.</param>
        private void ApplySelection(HexCoord coord)
        {
            var selectedEntities = _selectedHexSet.GetEntities();
            if (selectedEntities.Length == 0)
            {
                _world.CreateEntity().Set(new HexSelectedComponent { Coords = coord });
                RaiseSelectionChanged();
                return;
            }

            for (var index = 1; index < selectedEntities.Length; index++)
                selectedEntities[index].Dispose();

            var selectedEntity = selectedEntities[0];
            if (selectedEntity.Get<HexSelectedComponent>().Coords == coord)
            {
                selectedEntity.Dispose();
                RaiseSelectionChanged();
                return;
            }

            // Write through Set (publishing path), never in-place ref-mutation — see ARCHITECTURE.md.
            selectedEntity.Set(new HexSelectedComponent { Coords = coord });
            RaiseSelectionChanged();
        }

        /// <summary>
        ///     One-frame pulse so selection consumers (the hex info panel, the context tabs) reconcile against
        ///     the new <see cref="HexSelectedComponent" /> state without per-frame polling. Raised on every
        ///     mutation — create, deselect (dispose), and re-select to another coord.
        /// </summary>
        private void RaiseSelectionChanged()
        {
            var pulse = _world.CreateEntity();
            pulse.Set(new SelectedHexChangedEvent());
            pulse.Set(new EventTag());
        }

        /// <summary>
        ///     Intersects <paramref name="ray" /> with the horizontal plane at y=<see cref="SelectionPlaneHeight"/>.
        /// </summary>
        /// <param name="ray">Ray to intersect.</param>
        /// <param name="hit">World position of the intersection point.</param>
        /// <returns><c>true</c> when a valid forward intersection was found.</returns>
        private static bool TryIntersectPlane(Ray ray, out Vector3 hit)
        {
            hit = Vector3.zero;
            if (Mathf.Approximately(ray.direction.y, 0f))
                return false;

            var t = (SelectionPlaneHeight - ray.origin.y) / ray.direction.y;
            if (t <= 0f)
                return false;

            hit = ray.origin + ray.direction * t;
            return true;
        }

        /// <summary>Tries to resolve the configured pointer actions used by the selection system.</summary>
        /// <returns><c>true</c> when all required actions were found.</returns>
        private bool TryBindInputActions()
        {
            if (_clickAction != null && _pointAction != null)
                return true;

            if (_playerInputSet.Count == 0)
                return false;

            var playerInput = _playerInputSet.GetEntities()[0].Get<PlayerInputComponent>().PlayerInput;
            if (playerInput == null || playerInput.actions == null)
                return false;

            var clickAction = playerInput.actions.FindAction("UI/Click");
            var pointAction = playerInput.actions.FindAction("UI/Point");

            if (clickAction == null || pointAction == null)
                return false;

            _clickAction = clickAction;
            _pointAction = pointAction;
            return true;
        }

        /// <summary>Clears cached input action references.</summary>
        private void UnbindInputActions()
        {
            if (_clickAction != null)
                _clickAction = null;

            if (_pointAction != null)
                _pointAction = null;
        }
    }
}
