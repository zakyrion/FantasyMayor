using Modules.UserInput.Configs;

namespace Modules.UserInput.Components
{
    /// <summary>
    ///     Flattened ECS snapshot of <see cref="CameraMovementConfig" />.
    ///     Carries camera pan, drag-pan, and zoom tuning used by <see cref="Systems.CameraMovementSystem" />.
    /// </summary>
    public struct CameraMovementConfigComponent
    {
        /// <summary>World-units per second applied while the move action is held.</summary>
        public float PanSpeed;

        /// <summary>World-units per screen-pixel applied while drag-panning.</summary>
        public float DragPanSpeed;

        /// <summary>Field-of-view degrees applied for one scroll tick.</summary>
        public float ZoomStep;

        /// <summary>Closest allowed field of view.</summary>
        public float MinFieldOfView;

        /// <summary>Farthest allowed field of view.</summary>
        public float MaxFieldOfView;

        /// <summary>Lerp speed toward the target field of view.</summary>
        public float ZoomSpeed;

        /// <summary>Returns a safe fallback config when no ScriptableObject asset is assigned.</summary>
        public static CameraMovementConfigComponent Default => new()
        {
            PanSpeed = 12f,
            DragPanSpeed = 0.3f,
            ZoomStep = 4f,
            MinFieldOfView = 25f,
            MaxFieldOfView = 65f,
            ZoomSpeed = 8f
        };

        /// <summary>Creates a flattened component from a <see cref="CameraMovementConfig" /> asset.</summary>
        /// <param name="config">Source ScriptableObject asset.</param>
        /// <returns>Flattened ECS-friendly config data.</returns>
        public static CameraMovementConfigComponent FromConfig(CameraMovementConfig config)
        {
            return new CameraMovementConfigComponent
            {
                PanSpeed = config.PanSpeed,
                DragPanSpeed = config.DragPanSpeed,
                ZoomStep = config.ZoomStep,
                MinFieldOfView = config.MinFieldOfView,
                MaxFieldOfView = config.MaxFieldOfView,
                ZoomSpeed = config.ZoomSpeed
            };
        }
    }
}
