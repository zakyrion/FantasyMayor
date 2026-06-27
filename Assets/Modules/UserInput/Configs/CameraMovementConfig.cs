using UnityEngine;

namespace Modules.UserInput.Configs
{
    /// <summary>
    ///     ScriptableObject that tunes player-driven camera panning and zooming.
    ///     Referenced by <see cref="Installers.World.WorldInstaller" /> and flattened into ECS at startup.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraMovementConfig", menuName = "FantasyMayor/User Input/Camera Movement Config")]
    public class CameraMovementConfig : ScriptableObject
    {
        [Header("Panning")]
        [SerializeField]
        [Tooltip("World-units per second applied while the move input is held.")]
        private float _panSpeed = 12f;

        [Header("Drag Pan")]
        [SerializeField]
        [Tooltip("World-units per screen-pixel applied while drag-panning with the right mouse button or touch.")]
        private float _dragPanSpeed = 0.3f;

        [Header("Zoom (dolly along camera forward; FOV stays fixed)")]
        [SerializeField]
        [Tooltip("World-Y units the target camera height changes per mouse-wheel scroll tick.")]
        private float _zoomStep = 5f;

        [SerializeField]
        [Tooltip("Lowest allowed camera height (world Y). Closest zoom-in.")]
        private float _minHeight = 10f;

        [SerializeField]
        [Tooltip("Highest allowed camera height (world Y). Farthest zoom-out.")]
        private float _maxHeight = 60f;

        [SerializeField]
        [Tooltip("Lerp speed toward the target height. Higher values snap faster.")]
        private float _zoomSpeed = 8f;

        /// <summary>World-units per second applied while the move input is held.</summary>
        public float PanSpeed => _panSpeed;

        /// <summary>World-units per screen-pixel applied while drag-panning.</summary>
        public float DragPanSpeed => _dragPanSpeed;

        /// <summary>World-Y units the target camera height changes per mouse-wheel scroll tick.</summary>
        public float ZoomStep => _zoomStep;

        /// <summary>Lowest allowed camera height (world Y).</summary>
        public float MinHeight => _minHeight;

        /// <summary>Highest allowed camera height (world Y).</summary>
        public float MaxHeight => _maxHeight;

        /// <summary>Lerp speed toward the target camera height.</summary>
        public float ZoomSpeed => _zoomSpeed;
    }
}
