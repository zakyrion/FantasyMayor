using Friflo.Engine.ECS;
using Presentation.Terrain.Configs;

namespace Presentation.Terrain.Components
{
    /// <summary>ECS component that carries the flattened terrain view configuration.</summary>
    public struct TerrainViewConfigComponent : IComponent
    {
        /// <summary>Hex cell radius in world units.</summary>
        public float CellSize;

        /// <summary>Number of triangle subdivisions per hex face.</summary>
        public int Subdivisions;
        /// <summary>Number of wind erosion passes to apply.</summary>
        public int WindErosionPasses;

        /// <summary>World-unit height per terrain level.</summary>
        public float HeightScale;

        /// <summary>Creates a component from a <see cref="TerrainViewConfig" /> asset.</summary>
        /// <param name="config">Source scriptable object.</param>
        /// <returns>Populated component.</returns>
        public static TerrainViewConfigComponent FromConfig(TerrainViewConfig config)
        {
            return new TerrainViewConfigComponent
            {
                CellSize = config.CellSize,
                Subdivisions = config.Subdivisions,
                WindErosionPasses = config.WindErosionPass,
                HeightScale = config.HeightScale
            };
        }
    }
}
