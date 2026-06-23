using System.Collections.Generic;
using Modules.AxialSystem;
using Modules.CurveBuilders;
using Domains.Map.Hex.Utils;
using Presentation.Terrain.Data;
using Unity.Mathematics;
using UnityEngine;

namespace Presentation.Terrain.Isolines
{
    /// <summary>
    ///     Builds an isoline by:
    ///     1) constructing a baseline region with uniform depth offset,
    ///     2) sampling the curve on that baseline contour,
    ///     3) applying local signed wave edits (positive = add, negative = remove),
    ///     4) extracting a new contour from the edited region.
    /// </summary>
    public sealed class FieldBasedIsolineBuilder
    {
        private readonly ICurveBuilder _curveBuilder;
        private readonly VertexGrid _vertexGrid;
        private readonly IsolineBuilderData data;

        public FieldBasedIsolineBuilder(
            VertexGrid vertexGrid,
            ICurveBuilder curveBuilder,
            IsolineBuilderData data)
        {
            _vertexGrid = vertexGrid;
            _curveBuilder = curveBuilder;
            this.data = data;
        }

        /// <summary>
        ///     Builds an isoline from the given source hexes using field-based contour extraction.
        /// </summary>
        /// <param name="sourceHexes">Hex coordinates that define the source region.</param>
        /// <returns>The resulting isoline data, or null if extraction failed.</returns>
        public IsolineData BuildIsoLine(IEnumerable<HexCoord> sourceHexes)
        {
            if (_vertexGrid == null || sourceHexes == null || _curveBuilder == null)
                return null;

            var sourceSet = new HashSet<HexCoord>(sourceHexes);
            if (sourceSet.Count == 0)
                return null;

            if (!CollectRegion(_vertexGrid, sourceSet, out var regionSet))
                return null;

            var sourceBoundary = BuildBaseIsoLine(_vertexGrid, regionSet, sourceSet);
            if (sourceBoundary == null || sourceBoundary.Vertices.Count < 3)
                return null;

            var depthMap = BuildDepthMap(sourceBoundary.Vertices, regionSet, data.sideData, data.MaxDepth);
            if (depthMap.Count == 0)
                return null;

            var baselineDepth = math.clamp(data.DepthCenter, data.MinDepth, data.MaxDepth);
            var activeRegion = BuildBaselineActiveRegion(regionSet, depthMap, baselineDepth, data.sideData);

            var baselineContour = BuildBoundaryIsoLine(activeRegion);
            if (baselineContour == null || baselineContour.Vertices.Count < 3)
                return null;

            var baselineCurve = BuildCurveValues(baselineContour, _curveBuilder);
            var baselineTargets = BuildTargetDistances(
                baselineCurve,
                baselineDepth,
                data.DepthDeviation,
                data.MinDepth,
                data.MaxDepth);

            var waveDomain = new HashSet<VertexCoord>(depthMap.Keys);
            ApplySignedWaveEdits(activeRegion, baselineContour, baselineTargets, baselineDepth, waveDomain);

            var isoline = BuildBoundaryIsoLine(activeRegion);
            if (isoline == null || isoline.Vertices.Count < 3)
                return null;

            isoline.Height = data.Height;
            return isoline;
        }

        private void ApplySignedWaveEdits(
            HashSet<VertexCoord> activeRegion,
            IsolineData baselineContour,
            int[] baselineTargets,
            int baselineDepth,
            HashSet<VertexCoord> waveDomain)
        {
            for (var i = 0; i < baselineContour.Vertices.Count; i++)
            {
                var seed = baselineContour.Vertices[i];
                if (!waveDomain.Contains(seed))
                    continue;

                var targetDepth = i < baselineTargets.Length ? baselineTargets[i] : baselineDepth;
                var signedWave = targetDepth - baselineDepth;
                if (signedWave == 0)
                    continue;

                var waveCount = math.abs(signedWave);
                if (waveCount <= 0)
                    continue;

                var waveVertices = CollectWave(seed, waveCount, waveDomain);
                if (signedWave > 0)
                {
                    foreach (var coord in waveVertices)
                        activeRegion.Add(coord);
                }
                else
                {
                    foreach (var coord in waveVertices)
                        activeRegion.Remove(coord);
                }
            }
        }

        private HashSet<VertexCoord> BuildBaselineActiveRegion(
            HashSet<VertexCoord> regionSet,
            Dictionary<VertexCoord, int> depthMap,
            int baselineDepth,
            TraversalSideData sideData)
        {
            var activeRegion = new HashSet<VertexCoord>(regionSet);

            if (sideData == TraversalSideData.Inner)
            {
                foreach (var kvp in depthMap)
                {
                    if (kvp.Value < baselineDepth)
                        activeRegion.Remove(kvp.Key);
                }

                return activeRegion;
            }

            foreach (var kvp in depthMap)
            {
                if (kvp.Value == 0)
                    continue;
                if (kvp.Value <= baselineDepth)
                    activeRegion.Add(kvp.Key);
            }

            return activeRegion;
        }

        private IsolineData BuildBoundaryIsoLine(HashSet<VertexCoord> activeRegion)
        {
            if (activeRegion.Count < 3)
                return new IsolineData();

            var regionToProcess = GetLargestConnectedComponent(activeRegion);
            if (regionToProcess.Count < 3)
                return new IsolineData();

            var contourSet = ExtractContourByFloodFill(_vertexGrid, regionToProcess);
            if (contourSet.Count < 3)
                return new IsolineData();

            var ordered = OrderContourLoop(_vertexGrid, contourSet);
            if (ordered.Count < 3)
                return new IsolineData();

            return new IsolineData { Vertices = ordered, Distance = ordered.Count };
        }

        private Dictionary<VertexCoord, int> BuildDepthMap(
            IReadOnlyList<VertexCoord> boundary,
            HashSet<VertexCoord> regionSet,
            TraversalSideData sideData,
            int maxDepth)
        {
            var clampedMaxDepth = math.max(0, maxDepth);
            var depthMap = new Dictionary<VertexCoord, int>();
            var frontier = new Queue<VertexCoord>();

            for (var i = 0; i < boundary.Count; i++)
            {
                var seed = boundary[i];
                if (depthMap.ContainsKey(seed))
                    continue;
                depthMap[seed] = 0;
                frontier.Enqueue(seed);
            }

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                var currentDepth = depthMap[current];
                if (currentDepth >= clampedMaxDepth)
                    continue;

                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var neighbor = current + AxialMath.NeighborsPointyTop[d];
                    if (depthMap.ContainsKey(neighbor))
                        continue;
                    if (!_vertexGrid.Contains(neighbor))
                        continue;

                    var insideRegion = regionSet.Contains(neighbor);
                    if (sideData == TraversalSideData.Inner && !insideRegion)
                        continue;
                    if (sideData == TraversalSideData.Outer && insideRegion)
                        continue;

                    depthMap[neighbor] = currentDepth + 1;
                    frontier.Enqueue(neighbor);
                }
            }

            return depthMap;
        }

        private HashSet<VertexCoord> CollectWave(
            VertexCoord seed,
            int maxDepth,
            HashSet<VertexCoord> domain)
        {
            var result = new HashSet<VertexCoord>();
            if (maxDepth < 0 || !domain.Contains(seed))
                return result;

            var queue = new Queue<(VertexCoord Coord, int Depth)>();
            result.Add(seed);
            queue.Enqueue((seed, 0));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Depth >= maxDepth)
                    continue;

                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var neighbor = current.Coord + AxialMath.NeighborsPointyTop[d];
                    if (!domain.Contains(neighbor) || !result.Add(neighbor))
                        continue;

                    queue.Enqueue((neighbor, current.Depth + 1));
                }
            }

            return result;
        }

        private bool CollectRegion(
            VertexGrid vertexGrid,
            HashSet<HexCoord> sourceSet,
            out HashSet<VertexCoord> regionSet)
        {
            regionSet = new HashSet<VertexCoord>();
            foreach (var hex in sourceSet)
            {
                foreach (var coord in vertexGrid.GetOwnedVertexCoords(hex))
                    regionSet.Add(coord);
            }

            return regionSet.Count > 0;
        }

        private IsolineData BuildBaseIsoLine(
            VertexGrid vertexGrid,
            HashSet<VertexCoord> regionSet,
            HashSet<HexCoord> sourceSet)
        {
            var regionToProcess = GetLargestConnectedComponent(regionSet);
            var contourSet = ExtractContourByFloodFill(vertexGrid, regionToProcess, EnumerateCenterSeeds(vertexGrid, sourceSet));

            if (contourSet.Count == 0)
                return null;

            var ordered = OrderContourLoop(vertexGrid, contourSet);
            if (ordered.Count == 0)
                return null;

            return new IsolineData { Vertices = ordered, Distance = ordered.Count };
        }

        private HashSet<VertexCoord> ExtractContourByFloodFill(
            VertexGrid vertexGrid,
            HashSet<VertexCoord> regionSet,
            IEnumerable<VertexCoord> preferredSeeds = null)
        {
            _ = vertexGrid;

            var contourSet = new HashSet<VertexCoord>();
            if (regionSet.Count < 3)
                return contourSet;
            if (!TryGetFloodFillSeed(regionSet, preferredSeeds, out var seed))
                return contourSet;

            var visited = new HashSet<VertexCoord> { seed };
            var queue = new Queue<VertexCoord>();
            queue.Enqueue(seed);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                CountRegionNeighbors(regionSet, current, out _, out var outsideCount);
                if (outsideCount > 0)
                    contourSet.Add(current);

                // "Ears" show up when the flood reaches a boundary vertex that only slightly opens to the outside.
                // We stop the wave there so it marks the contour without spreading along the boundary band.
                if (outsideCount is 1 or 2)
                    continue;

                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var neighbor = current + AxialMath.NeighborsPointyTop[d];
                    if (!regionSet.Contains(neighbor) || !visited.Add(neighbor))
                        continue;

                    queue.Enqueue(neighbor);
                }
            }

            if (contourSet.Count == 0)
            {
                Debug.LogWarning($"[Isoline] Flood-fill contour extraction did not find a boundary from seed {seed}.");
                return contourSet;
            }

            var contourComponent = GetLargestConnectedComponent(contourSet);
            if (contourComponent.Count != contourSet.Count)
            {
                Debug.LogWarning(
                    $"[Isoline] Flood-fill contour extraction produced {contourSet.Count} boundary vertices across multiple components; " +
                    $"using largest component with {contourComponent.Count} vertices.");
            }

            return contourComponent;
        }

        private HashSet<VertexCoord> GetLargestConnectedComponent(HashSet<VertexCoord> vertices)
        {
            var visited = new HashSet<VertexCoord>();
            var bestComponent = new HashSet<VertexCoord>();
            var queue = new Queue<VertexCoord>();

            foreach (var start in vertices)
            {
                if (!visited.Add(start))
                    continue;

                var component = new HashSet<VertexCoord> { start };
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    for (var d = 0; d < AxialMath.NeighborCount; d++)
                    {
                        var neighbor = current + AxialMath.NeighborsPointyTop[d];
                        if (!vertices.Contains(neighbor) || !visited.Add(neighbor))
                            continue;

                        component.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }

                if (component.Count > bestComponent.Count)
                    bestComponent = component;
            }

            return bestComponent;
        }

        private List<VertexCoord> OrderContourLoop(VertexGrid vertexGrid, HashSet<VertexCoord> contourSet)
        {
            _ = vertexGrid;

            if (contourSet.Count < 3)
                return new List<VertexCoord>();

            if (!TryBuildContourAdjacency(contourSet, out var adjacency, out var invalidCoord, out var invalidDegree))
            {
                Debug.LogWarning(
                    $"[Isoline] Contour loop ordering requires a simple cycle, but found degree {invalidDegree} at {invalidCoord}. " +
                    "Contour extraction should produce vertices with exactly two contour neighbors.");
                return new List<VertexCoord>();
            }

            var start = GetDeterministicStart(contourSet);
            var ordered = new List<VertexCoord>(contourSet.Count);
            var visited = new HashSet<VertexCoord>();
            var previous = default(VertexCoord);
            var hasPrevious = false;
            var current = start;

            while (true)
            {
                ordered.Add(current);
                visited.Add(current);

                var neighbors = adjacency[current];
                var next = hasPrevious && neighbors[0].Equals(previous) ? neighbors[1] : neighbors[0];
                if (next.Equals(start))
                    break;
                if (visited.Contains(next))
                {
                    Debug.LogWarning($"[Isoline] Contour loop ordering hit an unexpected revisit at {next}.");
                    return new List<VertexCoord>();
                }

                previous = current;
                hasPrevious = true;
                current = next;
            }

            if (ordered.Count != contourSet.Count)
            {
                Debug.LogWarning(
                    $"[Isoline] Contour loop ordering visited {ordered.Count} of {contourSet.Count} contour vertices. " +
                    "Expected a single closed cycle.");
                return new List<VertexCoord>();
            }

            return ordered;
        }

        private float[] BuildCurveValues(IsolineData line, ICurveBuilder curveBuilder)
        {
            var vertexCount = line.Vertices.Count;
            var normalized = new float[vertexCount];
            for (var i = 0; i < vertexCount; i++)
                normalized[i] = curveBuilder.GetValue(math.saturate(line.S(i)));
            return normalized;
        }

        private int[] BuildTargetDistances(
            float[] curve,
            int depthCenter,
            int depthDeviation,
            int minDepth,
            int maxDepth)
        {
            var lower = math.min(minDepth, maxDepth);
            var upper = math.max(minDepth, maxDepth);
            var deviation = math.max(0, depthDeviation);

            var targetDistances = new int[curve.Length];
            for (var i = 0; i < curve.Length; i++)
            {
                var signedOffset = (curve[i] * 2f - 1f) * deviation;
                var depth = depthCenter + (int)math.round(signedOffset);
                targetDistances[i] = math.clamp(depth, lower, upper);
            }

            return targetDistances;
        }

        private bool TryBuildContourAdjacency(
            HashSet<VertexCoord> contourSet,
            out Dictionary<VertexCoord, VertexCoord[]> adjacency,
            out VertexCoord invalidCoord,
            out int invalidDegree)
        {
            adjacency = new Dictionary<VertexCoord, VertexCoord[]>(contourSet.Count);
            invalidCoord = default;
            invalidDegree = 0;

            foreach (var coord in contourSet)
            {
                var neighbors = new VertexCoord[2];
                var degree = 0;
                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var neighbor = coord + AxialMath.NeighborsPointyTop[d];
                    if (!contourSet.Contains(neighbor))
                        continue;
                    if (degree >= 2)
                    {
                        invalidCoord = coord;
                        invalidDegree = degree + 1;
                        return false;
                    }

                    neighbors[degree++] = neighbor;
                }

                if (degree != 2)
                {
                    invalidCoord = coord;
                    invalidDegree = degree;
                    return false;
                }

                adjacency.Add(coord, neighbors);
            }

            return true;
        }

        private VertexCoord GetDeterministicStart(HashSet<VertexCoord> contourSet)
        {
            var hasStart = false;
            var start = default(VertexCoord);
            foreach (var coord in contourSet)
            {
                if (!hasStart || CompareCoords(coord, start) < 0)
                {
                    start = coord;
                    hasStart = true;
                }
            }

            return start;
        }

        private int CompareCoords(VertexCoord a, VertexCoord b)
        {
            if (a.Value.x != b.Value.x)
                return a.Value.x.CompareTo(b.Value.x);
            return a.Value.y.CompareTo(b.Value.y);
        }

        private IEnumerable<VertexCoord> EnumerateCenterSeeds(
            VertexGrid vertexGrid,
            IEnumerable<HexCoord> sourceSet)
        {
            foreach (var hex in sourceSet)
                yield return vertexGrid.GetCenterVertexCoord(hex);
        }

        private bool TryGetFloodFillSeed(
            HashSet<VertexCoord> regionSet,
            IEnumerable<VertexCoord> preferredSeeds,
            out VertexCoord seed)
        {
            if (TrySelectBestSeed(regionSet, preferredSeeds, out seed))
                return true;

            return TrySelectBestSeed(regionSet, regionSet, out seed);
        }

        private bool TrySelectBestSeed(
            HashSet<VertexCoord> regionSet,
            IEnumerable<VertexCoord> candidates,
            out VertexCoord bestSeed)
        {
            bestSeed = default;
            var found = false;
            var bestInsideCount = int.MinValue;
            var bestOutsideCount = int.MaxValue;

            if (candidates == null)
                return false;

            foreach (var candidate in candidates)
            {
                if (!regionSet.Contains(candidate))
                    continue;

                CountRegionNeighbors(regionSet, candidate, out var insideCount, out var outsideCount);
                if (!found ||
                    insideCount > bestInsideCount ||
                    insideCount == bestInsideCount && outsideCount < bestOutsideCount ||
                    insideCount == bestInsideCount && outsideCount == bestOutsideCount && CompareCoords(candidate, bestSeed) < 0)
                {
                    bestSeed = candidate;
                    bestInsideCount = insideCount;
                    bestOutsideCount = outsideCount;
                    found = true;
                }
            }

            return found;
        }

        private void CountRegionNeighbors(
            HashSet<VertexCoord> regionSet,
            VertexCoord coord,
            out int insideCount,
            out int outsideCount)
        {
            insideCount = 0;
            outsideCount = 0;

            for (var d = 0; d < AxialMath.NeighborCount; d++)
            {
                var neighbor = coord + AxialMath.NeighborsPointyTop[d];
                if (regionSet.Contains(neighbor))
                    insideCount++;
                else
                    outsideCount++;
            }
        }
    }
}
