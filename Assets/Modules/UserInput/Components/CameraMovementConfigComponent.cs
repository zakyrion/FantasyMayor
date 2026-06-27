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

        /// <summary>World-Y units the target camera height changes per scroll tick.</summary>
        public float ZoomStep;

        /// <summary>Lowest allowed camera height (world Y).</summary>
        public float MinHeight;

        /// <summary>Highest allowed camera height (world Y).</summary>
        public float MaxHeight;

        /// <summary>Lerp speed toward the target camera height.</summary>
        public float ZoomSpeed;

        /// <summary>Returns a safe fallback config when no ScriptableObject asset is assigned.</summary>
        public static CameraMovementConfigComponent Default => new()
        {
            PanSpeed = 12f,
            DragPanSpeed = 0.3f,
            ZoomStep = 5f,
            MinHeight = 10f,
            MaxHeight = 60f,
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
                MinHeight = config.MinHeight,
                MaxHeight = config.MaxHeight,
                ZoomSpeed = config.ZoomSpeed
            };
        }
    }
}
