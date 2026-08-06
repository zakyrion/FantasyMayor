using Friflo.Engine.ECS;
using UnityEngine;

namespace Modules.Cameras.Components
{
    /// <summary>
    ///     Singleton component carrying the active scene camera.
    ///     Set once at startup by WorldInstaller via <c>Singletons.Set</c>; read via <c>Singletons.Get</c>.
    /// </summary>
    public struct CameraComponent : IComponent
    {
        /// <summary>Reference to the Unity scene camera controlled by the player.</summary>
        public Camera Camera;
    }
}
