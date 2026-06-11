using UnityEngine;

namespace Modules.Cameras.Components
{
    /// <summary>
    ///     Single-instance world component carrying the active scene camera.
    ///     Set once at startup by WorldInstaller via <c>world.Set</c>; read via <c>world.Get</c>.
    /// </summary>
    public struct CameraComponent
    {
        /// <summary>Reference to the Unity scene camera controlled by the player.</summary>
        public Camera Camera;

        /// <summary>
        ///     The camera's authored startup field of view (degrees) — the neutral "1× zoom" baseline.
        ///     Captured once at startup (before any zoom input), so view systems can express a zoom factor
        ///     relative to it without latching a runtime reference of their own.
        /// </summary>
        public float ReferenceFieldOfView;
    }
}
