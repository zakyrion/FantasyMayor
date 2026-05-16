using UnityEngine.InputSystem;

namespace Modules.UserInput.Components
{
    /// <summary>
    ///     Marks the entity that owns the scene <see cref="PlayerInput" /> instance.
    ///     Set once at startup by <see cref="Installers.World.WorldInstaller" />.
    /// </summary>
    public struct PlayerInputComponent
    {
        /// <summary>Reference to the Unity input entry point configured in the scene.</summary>
        public PlayerInput PlayerInput;
    }
}
