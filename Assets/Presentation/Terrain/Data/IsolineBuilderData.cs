using Unity.Mathematics;

namespace Presentation.Terrain.Data
{
    public readonly struct IsolineBuilderData
    {
        public readonly float Height;
        public readonly int DepthCenter;
        public readonly int DepthDeviation;
        public readonly int MinDepth;
        public readonly int MaxDepth;
        public readonly TraversalSideData sideData;

        public IsolineBuilderData(
            float height,
            int depthCenter,
            int depthDeviation,
            TraversalSideData sideData)
            : this(
                height,
                depthCenter,
                depthDeviation,
                math.max(0, depthCenter - math.max(0, depthDeviation)),
                depthCenter + math.max(0, depthDeviation),
                sideData)
        {
        }

        public IsolineBuilderData(
            float height,
            int depthCenter,
            int depthDeviation,
            int minDepth,
            int maxDepth,
            TraversalSideData sideData)
        {
            Height = height;
            DepthCenter = depthCenter;
            DepthDeviation = math.max(0, depthDeviation);
            MinDepth = math.max(0, math.min(minDepth, maxDepth));
            MaxDepth = math.max(0, math.max(minDepth, maxDepth));
            this.sideData = sideData;
        }
    }
}
