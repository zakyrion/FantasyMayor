namespace Modules.TerrainView.Components
{
    /// <summary>
    ///     Published by <see cref="Systems.WaterViewSubSystem" /> once the animated water
    ///     mesh has been generated and placed in the scene.
    /// </summary>
    public struct WaterViewComponent
    {
        /// <summary>Reference to the active water view MonoBehaviour.</summary>
        public Views.WaterView ObjectRef;
    }
}
