using UnityEngine;

namespace Modules.UserInput.Components
{
    /// <summary>
    ///     Marks the entity that owns the scene camera and carries a reference to it.
    ///     Set once at startup by <see cref="Installers.World.WorldInstaller" />.
    /// </summary>
    public struct CameraComponent
    {
        /// <summary>Reference to the Unity scene camera controlled by the player.</summary>
        public Camera Camera;
    }
}
