using Friflo.Engine.ECS;
namespace Presentation.Terrain.Components
{
    /// <summary>
    ///     Published once the hex-selection view carrier has been instantiated and configured.
    /// </summary>
    public struct HexSelectionViewComponent : IComponent
    {
        /// <summary>Reference to the active selection-view MonoBehaviour.</summary>
        public Views.HexSelectionView ObjectRef;
    }
}
