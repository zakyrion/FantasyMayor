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

        [Header("Zoom")]
        [SerializeField]
        [Tooltip("Field-of-view degrees applied for one mouse-wheel scroll tick.")]
        private float _zoomStep = 4f;

        [SerializeField]
        [Tooltip("Closest allowed field of view. Lower values zoom in further.")]
        private float _minFieldOfView = 25f;

        [SerializeField]
        [Tooltip("Farthest allowed field of view. Higher values zoom out further.")]
        private float _maxFieldOfView = 65f;

        [SerializeField]
        [Tooltip("Lerp speed toward target field of view. Higher values snap faster.")]
        private float _zoomSpeed = 8f;

        /// <summary>World-units per second applied while the move input is held.</summary>
        public float PanSpeed => _panSpeed;

        /// <summary>World-units per screen-pixel applied while drag-panning.</summary>
        public float DragPanSpeed => _dragPanSpeed;

        /// <summary>Field-of-view degrees applied for one mouse-wheel scroll tick.</summary>
        public float ZoomStep => _zoomStep;

        /// <summary>Closest allowed field of view.</summary>
        public float MinFieldOfView => _minFieldOfView;

        /// <summary>Farthest allowed field of view.</summary>
        public float MaxFieldOfView => _maxFieldOfView;

        /// <summary>Lerp speed toward the target field of view.</summary>
        public float ZoomSpeed => _zoomSpeed;
    }
}
