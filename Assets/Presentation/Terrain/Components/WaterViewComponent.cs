using Friflo.Engine.ECS;
namespace Presentation.Terrain.Components
{
    /// <summary>
    ///     Published by <see cref="Systems.WaterViewSubSystem" /> once the animated water
    ///     mesh has been generated and placed in the scene.
    /// </summary>
    public struct WaterViewComponent : IComponent
    {
        /// <summary>Reference to the active water view MonoBehaviour.</summary>
        public Views.WaterView ObjectRef;
    }
}
