using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Modules.AxialSystem;
using Domains.Map.Hex.Utils;
using Presentation.Terrain.Data;
using Unity.Collections;
using Unity.Mathematics;

namespace Presentation.Terrain.Isolines
{
    public static class IsolineSlopeTransition
    {
        private const int DISTANCE_STEP = 1;
        private const float HEIGHT_EPSILON = 1e-6f;
        private const int MIN_NATIVE_CAPACITY = 1;
        private const int ZERO_DISTANCE = 0;

        public static void ApplyTransitionBetweenIsoLines(
            VertexGrid vertexGrid,
            IsolineData upperIsolineData,
            IsolineData lowerIsolineData,
            VertexCoord insideBand)
        {
            if (upperIsolineData == null || lowerIsolineData == null)
                return;
            if (upperIsolineData.Vertices == null || lowerIsolineData.Vertices == null)
                return;
            if (upperIsolineData.Vertices.Count == 0 || lowerIsolineData.Vertices.Count == 0)
                return;
            if (math.abs(upperIsolineData.Height - lowerIsolineData.Height) < HEIGHT_EPSILON)
                return;

            NativeHashSet<VertexCoord> upperSet = default;
            NativeHashSet<VertexCoord> lowerSet = default;
            NativeHashSet<VertexCoord> barriers = default;
            NativeHashSet<VertexCoord> insideRegion = default;
            NativeHashSet<VertexCoord> bandAllVertices = default;
            NativeHashMap<VertexCoord, int> distFromUpper = default;
            NativeHashMap<VertexCoord, int> distFromLower = default;
            try
            {
                upperSet = CreateNativeSet(upperIsolineData.Vertices, Allocator.Persistent);
                lowerSet = CreateNativeSet(lowerIsolineData.Vertices, Allocator.Persistent);
                barriers = CreateUnionSet(upperSet, lowerSet, Allocator.Persistent);

                insideRegion = BfsFlood(vertexGrid, insideBand, barriers, Allocator.Persistent);
                bandAllVertices = DiscoverBandBetweenIsoLines(
                    vertexGrid,
                    upperIsolineData.Vertices,
                    upperSet,
                    lowerSet,
                    insideRegion,
                    Allocator.Persistent);
                if (bandAllVertices.Count == 0)
                    return;

                distFromUpper = BfsDistancesInBand(upperIsolineData.Vertices, bandAllVertices, Allocator.Persistent);
                distFromLower = BfsDistancesInBand(lowerIsolineData.Vertices, bandAllVertices, Allocator.Persistent);

                foreach (var coord in bandAllVertices)
                {
                    if (!distFromUpper.TryGetValue(coord, out var dUpper))
                        continue;
                    if (!distFromLower.TryGetValue(coord, out var dLower))
                        continue;

                    var total = dUpper + dLower;
                    if (total <= 0)
                        continue;

                    var t = (float)dUpper / total;
                    if (!vertexGrid.TryGet(coord, out var vertex))
                        continue;

                    var targetHeight = math.lerp(upperIsolineData.Height, lowerIsolineData.Height, t);
                    if (vertex.Position.y > targetHeight)
                        continue;

                    vertex.Position.y = targetHeight;
                    vertexGrid.Set(coord, vertex);
                }
            }
            finally
            {
                if (distFromLower.IsCreated)
                    distFromLower.Dispose();
                if (distFromUpper.IsCreated)
                    distFromUpper.Dispose();
                if (bandAllVertices.IsCreated)
                    bandAllVertices.Dispose();
                if (insideRegion.IsCreated)
                    insideRegion.Dispose();
                if (barriers.IsCreated)
                    barriers.Dispose();
                if (lowerSet.IsCreated)
                    lowerSet.Dispose();
                if (upperSet.IsCreated)
                    upperSet.Dispose();
            }
        }

        public static void FillInsideIsoline(
            VertexGrid vertexGrid,
            IsolineData isolineData,
            VertexCoord insideSeed,
            float fillHeight)
        {
            if (isolineData == null || isolineData.Vertices == null || isolineData.Vertices.Count == 0)
                return;

            NativeHashSet<VertexCoord> barriers = default;
            NativeHashSet<VertexCoord> insideRegion = default;
            try
            {
                barriers = CreateNativeSet(isolineData.Vertices, Allocator.Persistent);
                insideRegion = BfsFlood(vertexGrid, insideSeed, barriers, Allocator.Persistent);

                foreach (var coord in insideRegion)
                {
                    if (!vertexGrid.TryGet(coord, out var vertex))
                        continue;

                    if (vertex.Position.y > fillHeight)
                        continue;

                    vertex.Position.y = fillHeight;
                    vertexGrid.Set(coord, vertex);
                }
            }
            finally
            {
                if (insideRegion.IsCreated)
                    insideRegion.Dispose();
                if (barriers.IsCreated)
                    barriers.Dispose();
            }
        }

        /// <summary>
        ///     Sets every vertex inside <paramref name="isolineData" /> to <paramref name="fillHeight" />,
        ///     only if the vertex currently sits <em>above</em> that value — i.e. only depresses
        ///     terrain, never raises it. Intended for water / below-ground depressions.
        /// </summary>
        /// <param name="vertexGrid">The grid whose vertex heights are modified in place.</param>
        /// <param name="isolineData">Isoline that acts as the depression boundary.</param>
        /// <param name="insideSeed">Any vertex known to lie inside the isoline boundary.</param>
        /// <param name="fillHeight">Target height (expected to be negative).</param>
        public static void FillDepressionInsideIsoline(
            VertexGrid vertexGrid,
            IsolineData isolineData,
            VertexCoord insideSeed,
            float fillHeight)
        {
            if (isolineData == null || isolineData.Vertices == null || isolineData.Vertices.Count == 0)
                return;

            NativeHashSet<VertexCoord> barriers = default;
            NativeHashSet<VertexCoord> insideRegion = default;
            try
            {
                barriers = CreateNativeSet(isolineData.Vertices, Allocator.Persistent);
                insideRegion = BfsFlood(vertexGrid, insideSeed, barriers, Allocator.Persistent);

                foreach (var coord in insideRegion)
                {
                    if (!vertexGrid.TryGet(coord, out var vertex))
                        continue;

                    // Depression: only lower, never raise.
                    if (vertex.Position.y < fillHeight)
                        continue;

                    vertex.Position.y = fillHeight;
                    vertexGrid.Set(coord, vertex);
                }
            }
            finally
            {
                if (insideRegion.IsCreated)
                    insideRegion.Dispose();
                if (barriers.IsCreated)
                    barriers.Dispose();
            }
        }

        /// <summary>
        ///     Applies a smooth depression slope across the band between two isolines,
        ///     interpolating from <see cref="IsolineData.Height" /> of <paramref name="upperIsolineData" />
        ///     to that of <paramref name="lowerIsolineData" /> based on BFS distance.
        ///     Only lowers vertices toward the interpolated target — never raises them.
        ///     Intended for water / below-ground depressions.
        /// </summary>
        /// <param name="vertexGrid">The grid whose vertex heights are modified in place.</param>
        /// <param name="upperIsolineData">Inner (deeper) isoline — typically at a negative height.</param>
        /// <param name="lowerIsolineData">Outer (shallower) isoline — typically at 0.</param>
        /// <param name="insideBand">Seed vertex used to flood-fill the region inside the band.</param>
        public static void ApplyDepressionTransitionBetweenIsoLines(
            VertexGrid vertexGrid,
            IsolineData upperIsolineData,
            IsolineData lowerIsolineData,
            VertexCoord insideBand)
        {
            if (upperIsolineData == null || lowerIsolineData == null)
                return;
            if (upperIsolineData.Vertices == null || lowerIsolineData.Vertices == null)
                return;
            if (upperIsolineData.Vertices.Count == 0 || lowerIsolineData.Vertices.Count == 0)
                return;
            if (math.abs(upperIsolineData.Height - lowerIsolineData.Height) < HEIGHT_EPSILON)
                return;

            NativeHashSet<VertexCoord> upperSet = default;
            NativeHashSet<VertexCoord> lowerSet = default;
            NativeHashSet<VertexCoord> barriers = default;
            NativeHashSet<VertexCoord> insideRegion = default;
            NativeHashSet<VertexCoord> bandAllVertices = default;
            NativeHashMap<VertexCoord, int> distFromUpper = default;
            NativeHashMap<VertexCoord, int> distFromLower = default;
            try
            {
                upperSet = CreateNativeSet(upperIsolineData.Vertices, Allocator.Persistent);
                lowerSet = CreateNativeSet(lowerIsolineData.Vertices, Allocator.Persistent);
                barriers = CreateUnionSet(upperSet, lowerSet, Allocator.Persistent);

                insideRegion = BfsFlood(vertexGrid, insideBand, barriers, Allocator.Persistent);
                bandAllVertices = DiscoverBandBetweenIsoLines(
                    vertexGrid,
                    upperIsolineData.Vertices,
                    upperSet,
                    lowerSet,
                    insideRegion,
                    Allocator.Persistent);
                if (bandAllVertices.Count == 0)
                    return;

                distFromUpper = BfsDistancesInBand(upperIsolineData.Vertices, bandAllVertices, Allocator.Persistent);
                distFromLower = BfsDistancesInBand(lowerIsolineData.Vertices, bandAllVertices, Allocator.Persistent);

                foreach (var coord in bandAllVertices)
                {
                    if (!distFromUpper.TryGetValue(coord, out var dUpper))
                        continue;
                    if (!distFromLower.TryGetValue(coord, out var dLower))
                        continue;

                    var total = dUpper + dLower;
                    if (total <= 0)
                        continue;

                    var t = (float)dUpper / total;
                    if (!vertexGrid.TryGet(coord, out var vertex))
                        continue;

                    var targetHeight = math.lerp(upperIsolineData.Height, lowerIsolineData.Height, t);

                    // Depression: only lower toward the target, never raise.
                    if (vertex.Position.y < targetHeight)
                        continue;

                    vertex.Position.y = targetHeight;
                    vertexGrid.Set(coord, vertex);
                }
            }
            finally
            {
                if (distFromLower.IsCreated)
                    distFromLower.Dispose();
                if (distFromUpper.IsCreated)
                    distFromUpper.Dispose();
                if (bandAllVertices.IsCreated)
                    bandAllVertices.Dispose();
                if (insideRegion.IsCreated)
                    insideRegion.Dispose();
                if (barriers.IsCreated)
                    barriers.Dispose();
                if (lowerSet.IsCreated)
                    lowerSet.Dispose();
                if (upperSet.IsCreated)
                    upperSet.Dispose();
            }
        }

        private static NativeHashMap<VertexCoord, int> BfsDistancesInBand(
            List<VertexCoord> sources,
            NativeHashSet<VertexCoord> band,
            Allocator allocator)
        {
            var dist = new NativeHashMap<VertexCoord, int>(GetInitialCapacity(band.Count), allocator);
            var queue = new NativeQueue<DistanceQueueItem>(allocator);

            try
            {
                foreach (var src in sources)
                {
                    if (!band.Contains(src))
                        continue;
                    if (!dist.TryAdd(src, ZERO_DISTANCE))
                        continue;

                    queue.Enqueue(new DistanceQueueItem(src, ZERO_DISTANCE));
                }

                while (!queue.IsEmpty())
                {
                    var current = queue.Dequeue();
                    var nextDistance = current.Distance + DISTANCE_STEP;
                    for (var d = 0; d < AxialMath.NeighborCount; d++)
                    {
                        var neighbor = current.Coord + AxialMath.NeighborsPointyTop[d];
                        if (!band.Contains(neighbor))
                            continue;
                        if (!dist.TryAdd(neighbor, nextDistance))
                            continue;

                        queue.Enqueue(new DistanceQueueItem(neighbor, nextDistance));
                    }
                }

                return dist;
            }
            catch
            {
                if (dist.IsCreated)
                    dist.Dispose();
                throw;
            }
            finally
            {
                if (queue.IsCreated)
                    queue.Dispose();
            }
        }

        private static NativeHashSet<VertexCoord> BfsFlood(
            VertexGrid vertexGrid,
            VertexCoord seed,
            NativeHashSet<VertexCoord> barriers,
            Allocator allocator)
        {
            var visited = new NativeHashSet<VertexCoord>(GetInitialCapacity(vertexGrid.Count), allocator);
            var queue = new NativeQueue<VertexCoord>(allocator);

            try
            {
                if (vertexGrid.Contains(seed) && !barriers.Contains(seed))
                {
                    visited.Add(seed);
                    queue.Enqueue(seed);
                }

                while (!queue.IsEmpty())
                {
                    var current = queue.Dequeue();

                    for (var d = 0; d < AxialMath.NeighborCount; d++)
                    {
                        var neighbor = current + AxialMath.NeighborsPointyTop[d];
                        if (!vertexGrid.Contains(neighbor))
                            continue;
                        if (barriers.Contains(neighbor))
                            continue;
                        if (visited.Add(neighbor))
                            queue.Enqueue(neighbor);
                    }
                }

                return visited;
            }
            catch
            {
                if (visited.IsCreated)
                    visited.Dispose();
                throw;
            }
            finally
            {
                if (queue.IsCreated)
                    queue.Dispose();
            }
        }

        private static NativeHashSet<VertexCoord> CreateNativeSet(List<VertexCoord> vertices, Allocator allocator)
        {
            var set = new NativeHashSet<VertexCoord>(GetInitialCapacity(vertices.Count), allocator);
            foreach (var coord in vertices)
                set.Add(coord);
            return set;
        }

        private static NativeHashSet<VertexCoord> CreateUnionSet(
            NativeHashSet<VertexCoord> first,
            NativeHashSet<VertexCoord> second,
            Allocator allocator)
        {
            var union = new NativeHashSet<VertexCoord>(GetInitialCapacity(first.Count + second.Count), allocator);
            foreach (var coord in first)
                union.Add(coord);
            foreach (var coord in second)
                union.Add(coord);
            return union;
        }

        private static NativeHashSet<VertexCoord> DiscoverBandBetweenIsoLines(
            VertexGrid vertexGrid,
            List<VertexCoord> upperIsoVertices,
            NativeHashSet<VertexCoord> upperSet,
            NativeHashSet<VertexCoord> lowerSet,
            NativeHashSet<VertexCoord> insideRegion,
            Allocator allocator)
        {
            var allVertices = CreateUnionSet(upperSet, lowerSet, allocator);
            var visited = new NativeHashSet<VertexCoord>(GetInitialCapacity(vertexGrid.Count), allocator);
            var queue = new NativeQueue<VertexCoord>(allocator);

            try
            {
                foreach (var coord in upperSet)
                    visited.Add(coord);

                foreach (var coord in upperIsoVertices)
                {
                    if (!upperSet.Contains(coord))
                        continue;
                    queue.Enqueue(coord);
                }

                while (!queue.IsEmpty())
                {
                    var current = queue.Dequeue();

                    for (var d = 0; d < AxialMath.NeighborCount; d++)
                    {
                        var neighbor = current + AxialMath.NeighborsPointyTop[d];
                        if (!vertexGrid.Contains(neighbor))
                            continue;
                        if (insideRegion.Contains(neighbor))
                            continue;
                        if (!visited.Add(neighbor))
                            continue;

                        allVertices.Add(neighbor);

                        if (!lowerSet.Contains(neighbor))
                            queue.Enqueue(neighbor);
                    }
                }

                return allVertices;
            }
            catch
            {
                if (allVertices.IsCreated)
                    allVertices.Dispose();
                throw;
            }
            finally
            {
                if (queue.IsCreated)
                    queue.Dispose();
                if (visited.IsCreated)
                    visited.Dispose();
            }
        }

        private static int GetInitialCapacity(int count)
        {
            return math.max(MIN_NATIVE_CAPACITY, count);
        }

        private struct DistanceQueueItem
        {
            public readonly VertexCoord Coord;
            public readonly int Distance;

            public DistanceQueueItem(VertexCoord coord, int distance)
            {
                Coord = coord;
                Distance = distance;
            }
        }
    }
}
