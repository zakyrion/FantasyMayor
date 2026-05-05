namespace Modules.TerrainView.Components
{
    /// <summary>
    ///     Event entity marker that signals <see cref="Systems.TerrainViewSystem" /> to load and instantiate the
    ///     TerrainView prefab. The entity is consumed (disposed) by the system on the same frame it is processed.
    /// </summary>
    public struct ShowTerrainViewEventComponent
    {
    }
}
