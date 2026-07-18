using Friflo.Engine.ECS;
using UnityEngine;

namespace Modules.Cameras.Components
{
    /// <summary>
    ///     Single-instance world component carrying the active scene camera.
    ///     Set once at startup by WorldInstaller via <c>world.Set</c>; read via <c>world.Get</c>.
    /// </summary>
    public struct CameraComponent : IComponent
    {
        /// <summary>Reference to the Unity scene camera controlled by the player.</summary>
        public Camera Camera;
    }
}
