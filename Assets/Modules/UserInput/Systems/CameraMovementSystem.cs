using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Cameras.Components;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Tags;
using Presentation.Terrain.Components;
using Modules.UserInput.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Modules.UserInput.Systems
{
    /// <summary>
    ///     Drives player-controlled camera movement each LateUpdate tick.
    ///     Reads <see cref="CameraComponent" />, <see cref="PlayerInputComponent" />, and
    ///     <see cref="CameraMovementConfigComponent" /> to apply WASD panning, drag panning,
    ///     bounds clamping, and smoothed zoom.
    /// </summary>
    [UsedImplicitly]
    public sealed class CameraMovementSystem : LateUpdatedSystem
    {
        /// <summary>World-space Y of the horizontal plane used for the center-ray bounds check.</summary>
        private const float BoundsPlaneHeight = 1f;

        /// <summary>Below this |forward.y| the camera looks too flat to change height by dollying — skip.</summary>
        private const float MinForwardPitch = 1e-4f;

        private readonly EntitySet _hexIdSet;
        private readonly EntitySet _playerInputSet;
        private readonly World _world;

        private Rect _bounds;
        private bool _boundsValid;
        private InputAction _lookAction;
        private InputAction _moveAction;
        private Vector2 _panInput;
        private InputAction _rightClickAction;
        private float _targetHeight;
        private bool _targetHeightInitialized;
        private InputAction _zoomAction;
        private float _zoomTicks;

        /// <inheritdoc />
        public override int Priority => 0;

        /// <param name="world">The ECS world used to build the entity set.</param>
        public CameraMovementSystem(World world)
            // Anchored on the single PlayerInputComponent entity so Update ticks once per frame;
            // the camera itself is a world component (CameraComponent), read via world.Get below.
            : base(world.GetEntities().With<PlayerInputComponent>().AsSet())
        {
            _world = world;
            _playerInputSet = world.GetEntities()
                .With<PlayerInputComponent>()
                .AsSet();
            _hexIdSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexTag>()
                .AsSet();

            TryBindInputActions();
        }

        /// <inheritdoc />
        protected override void Update(GameState state, in Entity entity)
        {
            if (_moveAction == null || _zoomAction == null || _rightClickAction == null || _lookAction == null)
            {
                if (!TryBindInputActions())
                    return;
            }

            if (!_world.Has<CameraMovementConfigComponent>() || !_world.Has<CameraComponent>())
                return;

            var camera = _world.Get<CameraComponent>().Camera;
            if (camera == null)
                return;

            var config = _world.Get<CameraMovementConfigComponent>();
            var cameraTransform = camera.transform;

            MoveCamera(cameraTransform, config, state.DeltaTime);
            ApplyDolly(cameraTransform, config, state.DeltaTime);
            ClampCameraPosition(cameraTransform, camera);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            UnbindInputActions();
            _playerInputSet.Dispose();
            _hexIdSet.Dispose();
            base.Dispose();
        }

        /// <summary>
        ///     Lazily computes world-space AABB from all <see cref="HexIdComponent" /> entities and caches it.
        ///     Padded by one hex outer radius so the camera cannot leave the playable area.
        /// </summary>
        /// <returns><c>true</c> when bounds were successfully computed and cached.</returns>
        private bool TryComputeBounds()
        {
            if (!_world.Has<TerrainViewConfigComponent>() || _hexIdSet.Count == 0)
                return false;

            var cellSize = _world.Get<TerrainViewConfigComponent>().CellSize;

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;

            foreach (var hexEntity in _hexIdSet.GetEntities())
            {
                var coord = hexEntity.Get<HexIdComponent>().Coords.Value;
                var world = AxialMath.AxialToWorldPointTop(coord, cellSize);

                if (world.x < minX) minX = world.x;
                if (world.x > maxX) maxX = world.x;
                if (world.z < minZ) minZ = world.z;
                if (world.z > maxZ) maxZ = world.z;
            }

            var pad = cellSize;
            _bounds = Rect.MinMaxRect(minX - pad, minZ - pad, maxX + pad, maxZ + pad);
            _boundsValid = true;
            return true;
        }

        /// <summary>
        ///     Clamps the camera by projecting the viewport center onto the y=<see cref="BoundsPlaneHeight"/>
        ///     plane and keeping that point within the hex grid bounds.
        ///     The camera stops when the center of the screen reaches a map border;
        ///     sky may become visible at the far edge of the screen, which is standard for strategy cameras.
        /// </summary>
        /// <param name="cameraTransform">Transform of the camera being controlled.</param>
        /// <param name="camera">Camera used to cast the center ray.</param>
        private void ClampCameraPosition(Transform cameraTransform, Camera camera)
        {
            if (!_boundsValid && !TryComputeBounds())
                return;

            var centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!TryIntersectPlane(centerRay, out var hit))
                return;

            var pos = cameraTransform.position;

            if (hit.x < _bounds.xMin)
                pos.x += _bounds.xMin - hit.x;
            else if (hit.x > _bounds.xMax)
                pos.x -= hit.x - _bounds.xMax;

            if (hit.z < _bounds.yMin)
                pos.z += _bounds.yMin - hit.z;
            else if (hit.z > _bounds.yMax)
                pos.z -= hit.z - _bounds.yMax;

            cameraTransform.position = pos;
        }

        /// <summary>
        ///     Intersects <paramref name="ray" /> with the horizontal plane at y=<see cref="BoundsPlaneHeight"/>.
        ///     Returns <c>false</c> when the ray is parallel to the plane or points away from it.
        /// </summary>
        /// <param name="ray">Ray to intersect.</param>
        /// <param name="hit">World position of the intersection point.</param>
        /// <returns><c>true</c> when a valid forward intersection was found.</returns>
        private bool TryIntersectPlane(Ray ray, out Vector3 hit)
        {
            hit = Vector3.zero;
            if (Mathf.Approximately(ray.direction.y, 0f))
                return false;

            var t = (BoundsPlaneHeight - ray.origin.y) / ray.direction.y;
            if (t <= 0f)
                return false;

            hit = ray.origin + ray.direction * t;
            return true;
        }

        /// <summary>
        ///     Zooms by dollying the camera strictly along its own forward axis, so the tilt (pitch) and FOV
        ///     stay fixed and only the camera height changes. Accumulated scroll ticks move the target height
        ///     (clamped to <see cref="CameraMovementConfigComponent.MinHeight" />/<c>MaxHeight</c>); deltaTime
        ///     drives the lerp. The forward translation is solved from the desired height delta so the camera
        ///     stays on its view line — X/Z shift together with Y and the framed point does not jump.
        /// </summary>
        /// <param name="cameraTransform">Transform of the camera being controlled.</param>
        /// <param name="config">Zoom settings loaded into ECS.</param>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        private void ApplyDolly(Transform cameraTransform, in CameraMovementConfigComponent config, float deltaTime)
        {
            var pos = cameraTransform.position;
            var forward = cameraTransform.forward;

            if (!_targetHeightInitialized)
            {
                _targetHeight = Mathf.Clamp(pos.y, config.MinHeight, config.MaxHeight);
                _targetHeightInitialized = true;
            }

            if (!Mathf.Approximately(_zoomTicks, 0f))
            {
                // Positive scroll = zoom in = lower the camera. ZoomStep is world-Y units per tick.
                _targetHeight = Mathf.Clamp(
                    _targetHeight - _zoomTicks * config.ZoomStep,
                    config.MinHeight,
                    config.MaxHeight);
                _zoomTicks = 0f;
            }

            // A near-horizontal camera cannot change height by moving along forward — leave it untouched.
            if (Mathf.Abs(forward.y) < MinForwardPitch)
                return;

            var newHeight = Mathf.Lerp(pos.y, _targetHeight, deltaTime * config.ZoomSpeed);
            var distanceAlongForward = (newHeight - pos.y) / forward.y;
            cameraTransform.position = pos + forward * distanceAlongForward;
        }

        /// <summary>
        ///     Moves the camera along the ground plane using either WASD input or right-click/touch drag.
        ///     Drag state is polled each LateUpdate tick so it is always in sync with the actual
        ///     button state — event-based canceled callbacks are not relied upon.
        /// </summary>
        /// <param name="cameraTransform">Transform of the camera being controlled.</param>
        /// <param name="config">Pan settings loaded into ECS.</param>
        /// <param name="deltaTime">Frame delta time in seconds.</param>
        private void MoveCamera(Transform cameraTransform, in CameraMovementConfigComponent config, float deltaTime)
        {
            var right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;

            if (_panInput != Vector2.zero)
            {
                var movement = (right * _panInput.x + forward * _panInput.y) * (config.PanSpeed * deltaTime);
                cameraTransform.position += movement;
            }

            if (_rightClickAction != null && _rightClickAction.IsPressed() && _lookAction != null)
            {
                var delta = _lookAction.ReadValue<Vector2>();
                if (delta != Vector2.zero)
                {
                    // Negate: dragging right moves the camera left (world-grab feel).
                    // Pointer delta is already frame-relative (pixels/frame), so no deltaTime multiplication.
                    var movement = (right * -delta.x + forward * -delta.y) * config.DragPanSpeed;
                    cameraTransform.position += movement;
                }
            }
        }

        /// <summary>Clears pan input once the move action is released.</summary>
        /// <param name="context">Input callback context for the move action.</param>
        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _panInput = Vector2.zero;
        }

        /// <summary>Stores the latest pan vector whenever the move action changes.</summary>
        /// <param name="context">Input callback context for the move action.</param>
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            _panInput = context.ReadValue<Vector2>();
        }

        /// <summary>Accumulates one zoom tick per scroll direction event.</summary>
        /// <param name="context">Input callback context for the scroll action.</param>
        private void OnZoomPerformed(InputAction.CallbackContext context)
        {
            var scrollValue = context.ReadValue<Vector2>().y;
            if (Mathf.Approximately(scrollValue, 0f))
                return;

            _zoomTicks += Mathf.Sign(scrollValue);
        }

        /// <summary>Tries to resolve the configured scene actions and subscribe this system to them.</summary>
        /// <returns><c>true</c> when all required actions were found.</returns>
        private bool TryBindInputActions()
        {
            if (_moveAction != null && _zoomAction != null && _rightClickAction != null && _lookAction != null)
                return true;

            if (_playerInputSet.Count == 0)
                return false;

            var playerInput = _playerInputSet.GetEntities()[0].Get<PlayerInputComponent>().PlayerInput;
            if (playerInput == null || playerInput.actions == null)
                return false;

            var moveAction = playerInput.actions.FindAction("Player/Move");
            var zoomAction = playerInput.actions.FindAction("UI/ScrollWheel");
            var rightClickAction = playerInput.actions.FindAction("UI/RightClick");
            var lookAction = playerInput.actions.FindAction("Player/Look");

            if (moveAction == null || zoomAction == null || rightClickAction == null || lookAction == null)
                return false;

            _moveAction = moveAction;
            _zoomAction = zoomAction;
            _rightClickAction = rightClickAction;
            _lookAction = lookAction;

            _moveAction.performed += OnMovePerformed;
            _moveAction.canceled += OnMoveCanceled;
            _zoomAction.performed += OnZoomPerformed;
            // _rightClickAction and _lookAction are polled each tick — no event subscription needed.

            return true;
        }

        /// <summary>Removes previously registered input callbacks.</summary>
        private void UnbindInputActions()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMovePerformed;
                _moveAction.canceled -= OnMoveCanceled;
                _moveAction = null;
            }

            if (_zoomAction != null)
            {
                _zoomAction.performed -= OnZoomPerformed;
                _zoomAction = null;
            }

            if (_rightClickAction != null)
                _rightClickAction = null;

            if (_lookAction != null)
                _lookAction = null;
        }
    }
}
