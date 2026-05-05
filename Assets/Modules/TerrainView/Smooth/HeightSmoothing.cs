using Cysharp.Threading.Tasks;
using Modules.AxialSystem;
using Modules.HexesCore.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Modules.TerrainView.Smooth
{
    /// <summary>Smoothing and erosion algorithms that operate on the vertex-grid height field.</summary>
    public static class HeightSmoothing
    {

        /// <summary>
        ///     Applies directional wind erosion: material is transported downwind along each iteration's
        ///     height gradient. Heights are kept in a single NativeArray pair (source/result) across all
        ///     iterations — no per-iteration dictionary rebuild.
        /// </summary>
        /// <param name="vertexGrid">Vertex grid whose Y heights are updated in place.</param>
        /// <param name="settings">Wind erosion parameters.</param>
        public static void ApplyDirectionalWindErosion(VertexGrid vertexGrid, WindErosionSettings settings)
        {
            if (vertexGrid == null)
                return;
            if (settings.Iterations <= 0)
                return;

            var direction = settings.WindDirection.Value;
            if (!AxialMath.AreNeighbors(int2.zero, direction))
                return;

            var windStrength = math.saturate(settings.WindStrength);
            var transportRate = math.saturate(settings.TransportRate);
            var maxTransport = math.max(0f, settings.MaxTransportPerIteration);
            if (windStrength <= 0f || transportRate <= 0f)
                return;

            var simCoords = BuildSimCoords(vertexGrid, Allocator.TempJob);

            if (simCoords.Length == 0)
            {
                simCoords.Dispose();
                return;
            }

            var count = simCoords.Length;
            var indexByCoord = new NativeHashMap<VertexCoord, int>(count, Allocator.TempJob);
            var sourceHts = new NativeArray<float>(count, Allocator.TempJob);
            var resultHts = new NativeArray<float>(count, Allocator.TempJob);
            var downwindIndex = new NativeArray<int>(count, Allocator.TempJob);
            var upwindIndex = new NativeArray<int>(count, Allocator.TempJob);
            const int jobBatchSize = 64;
            var jobHandle = default(JobHandle);
            var hasScheduledWork = false;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    indexByCoord[simCoords[i]] = i;
                    vertexGrid.TryGet(simCoords[i], out var v);
                    sourceHts[i] = v.Position.y;
                }

                for (var i = 0; i < count; i++)
                {
                    downwindIndex[i] = indexByCoord.TryGetValue(simCoords[i] + direction, out var nIdx) ? nIdx : -1;
                    upwindIndex[i] = indexByCoord.TryGetValue(simCoords[i] - direction, out var uIdx) ? uIdx : -1;
                }

                var transportFactor = transportRate * windStrength;
                for (var iteration = 0; iteration < settings.Iterations; iteration++)
                {
                    jobHandle = new WindErosionStepJob
                    {
                        SourceHeights = sourceHts,
                        ResultHeights = resultHts,
                        DownwindIndex = downwindIndex,
                        UpwindIndex = upwindIndex,
                        TransportFactor = transportFactor,
                        MaxTransport = maxTransport
                    }.ScheduleParallel(count, jobBatchSize, jobHandle);
                    hasScheduledWork = true;

                    (sourceHts, resultHts) = (resultHts, sourceHts);
                }

                if (hasScheduledWork)
                {
                    jobHandle.Complete();
                    hasScheduledWork = false;
                }

                for (var i = 0; i < count; i++)
                {
                    if (!vertexGrid.TryGet(simCoords[i], out var vertex))
                        continue;

                    vertex.Position.y = sourceHts[i];
                    vertexGrid.Set(simCoords[i], vertex);
                }
            }
            finally
            {
                if (hasScheduledWork)
                    jobHandle.Complete();

                simCoords.Dispose();
                indexByCoord.Dispose();
                sourceHts.Dispose();
                resultHts.Dispose();
                downwindIndex.Dispose();
                upwindIndex.Dispose();
            }
        }

        [BurstCompile]
        private struct WindErosionStepJob : IJobFor
        {
            [ReadOnly] public NativeArray<float> SourceHeights;
            [WriteOnly] public NativeArray<float> ResultHeights;
            [ReadOnly] public NativeArray<int> DownwindIndex;
            [ReadOnly] public NativeArray<int> UpwindIndex;
            public float TransportFactor;
            public float MaxTransport;

            public void Execute(int index)
            {
                var outgoing = ComputeTransfer(index, DownwindIndex[index], SourceHeights, TransportFactor, MaxTransport);
                var incoming = ComputeTransfer(UpwindIndex[index], index, SourceHeights, TransportFactor, MaxTransport);
                ResultHeights[index] = SourceHeights[index] + incoming - outgoing;
            }

            private static float ComputeTransfer(
                int fromIndex,
                int toIndex,
                NativeArray<float> heights,
                float transportFactor,
                float maxTransport)
            {
                if (fromIndex < 0 || toIndex < 0)
                    return 0f;

                var slope = heights[fromIndex] - heights[toIndex];
                if (slope <= 0f)
                    return 0f;

                var transfer = slope * transportFactor;
                if (maxTransport > 0f)
                    transfer = math.min(transfer, maxTransport);

                return transfer > 0f ? transfer : 0f;
            }
        }

        /// <summary>
        ///     Applies the Mei–Decaudin–Hu virtual-pipe hydraulic erosion model to the heights of
        ///     <paramref name="vertexGrid" /> on a 6-neighbor flat-top axial hex lattice. Per iteration:
        ///     1. rain adds water to every vertex;
        ///     2. outflow is accumulated per pipe (f[d] += flowRate · Δsurface) — persistent across
        ///     iterations; this flux-state memory is what gives the model fluid inertia and
        ///     distinguishes it from a plain cellular height equalizer;
        ///     3. per-cell outflows are scaled so a vertex never drains below zero in a single step;
        ///     4. water is updated from net flux (Σin − Σout);
        ///     5. a 2D velocity field is recovered from per-direction flux imbalances;
        ///     6. capacity = sedimentCapacity · sin(tilt) · |v| drives erosion where suspended
        ///     sediment is below capacity, deposition where above it;
        ///     7. suspended sediment is advected semi-Lagrangian along the velocity field
        ///     (back-trace → barycentric sample on the 3 nearest hex cells);
        ///     8. water evaporates by a fraction each step.
        ///     Any sediment still suspended when iterations end is deposited in place, representing
        ///     material that would settle when the remaining water fully evaporated.
        ///     All per-vertex buffers are NativeArrays on the unmanaged heap; neighbor topology and
        ///     lookup are stored in flat NativeArrays (row-major, stride = NeighborCount) and a
        ///     NativeHashMap, eliminating all managed-heap pressure during the simulation loop.
        /// </summary>
        /// <param name="vertexGrid">Vertex grid whose Y heights are updated in place.</param>
        /// <param name="settings">Pipe-model simulation parameters.</param>
        public static async UniTask ApplyHydraulicErosionPipeModel(VertexGrid vertexGrid, HydraulicErosionSettings settings)
        {
            if (vertexGrid == null)
                return;
            if (settings.Iterations <= 0)
                return;

            var flowRate = math.saturate(settings.FlowRate);
            if (flowRate <= 0f)
                return;

            // This async path can span many frames; TempJob lifetime (4 frames) is too short here.
            const Allocator simulationAllocator = Allocator.Persistent;

            var simCoords = BuildSimCoords(vertexGrid, simulationAllocator);
            if (simCoords.Length == 0)
            {
                simCoords.Dispose();
                return;
            }

            var verticesCount = simCoords.Length;
            const int nc = AxialMath.NeighborCount;
            const int jobBatchSize = 64;

            var indexByCoord = new NativeHashMap<VertexCoord, int>(verticesCount, simulationAllocator);
            // Flat row-major [i * nc + d] replaces managed 2D arrays.
            var neighborIndices = new NativeArray<int>(verticesCount * nc, simulationAllocator);
            var oppositeDir = new NativeArray<int>(nc, simulationAllocator);
            var dirUnit = new NativeArray<float2>(nc, simulationAllocator);
            var terrain = new NativeArray<float>(verticesCount, simulationAllocator);
            var water = new NativeArray<float>(verticesCount, simulationAllocator);
            var sediment = new NativeArray<float>(verticesCount, simulationAllocator);
            var outFlow = new NativeArray<float>(verticesCount * nc, simulationAllocator);
            var velocity = new NativeArray<float2>(verticesCount, simulationAllocator);
            var deltaWater = new NativeArray<float>(verticesCount, simulationAllocator);
            var nextSediment = new NativeArray<float>(verticesCount, simulationAllocator);
            var centerWorld = new NativeArray<float2>(verticesCount, simulationAllocator);
            var simulationHandle = default(JobHandle);
            var hasScheduledWork = false;

            try
            {
                // Index map.
                for (var i = 0; i < verticesCount; i++)
                    indexByCoord[simCoords[i]] = i;

                // Neighbor topology.
                for (var i = 0; i < verticesCount; i++)
                    for (var d = 0; d < nc; d++)
                    {
                        var neighborCoord = simCoords[i] + AxialMath.NeighborDirs[d];
                        neighborIndices[i * nc + d] = indexByCoord.TryGetValue(neighborCoord, out var idx) ? idx : -1;
                    }

                // Opposite-direction lookup — needed to read a neighbor's flow back into us.
                for (var d = 0; d < nc; d++)
                {
                    var dir = AxialMath.NeighborDirs[d];
                    for (var k = 0; k < nc; k++)
                    {
                        if (math.all(AxialMath.NeighborDirs[k] == -dir))
                        {
                            oppositeDir[d] = k;
                            break;
                        }
                    }
                }

                // Unit vectors in world XZ for each flat-top axial neighbor direction.
                // All 6 neighbors are equidistant in a regular hex grid, so len = sqrt(3) is constant —
                // compute it once instead of inside the loop.
                const float flatTopNeighborLen = 1.7320508f; // sqrt(3), exact for flat-top hex
                for (var d = 0; d < nc; d++)
                {
                    var delta = AxialMath.NeighborDirs[d];
                    var dx = 1.5f * delta.x;
                    var dz = math.sqrt(3f) * (delta.y + delta.x * 0.5f);
                    dirUnit[d] = new float2(dx / flatTopNeighborLen, dz / flatTopNeighborLen);
                }

                // Initial terrain heights and world-space centers — v.Position already holds world XYZ.
                for (var i = 0; i < verticesCount; i++)
                {
                    vertexGrid.TryGet(simCoords[i], out var v);
                    terrain[i] = v.Position.y;
                    centerWorld[i] = new float2(v.Position.x, v.Position.z);
                }

                var cellSize = vertexGrid.CellSize;
                // Flat-top hex geometry: center-to-center distance is √3 · cellSize; used for tilt.
                var pipeLength = math.sqrt(3f) * cellSize;
                var rainAmount = math.max(0f, settings.RainAmount);
                var evaporation = math.saturate(settings.Evaporation);
                var sedimentCapacity = math.max(0f, settings.SedimentCapacity);
                var erosionRate = math.saturate(settings.ErosionRate);
                var depositionRate = math.saturate(settings.DepositionRate);

                // Prevents capacity from collapsing to zero on near-flat moving water.
                const float minTilt = 0.05f;
                const float epsilon = 1e-6f;

                // CFL-1 clamp: never back-trace further than one hex-edge per step.
                var maxBackTrace = cellSize;

                // Stabilizers. Without these the raw pipe model either digs isolated pits (flux spikes
                // on persistent heads explode velocity → capacity → single-step erosion) or smears the
                // terrain flat (sediment advects off the relief and is dumped where it doesn't belong).
                const float fluxDamping = 0.02f; // bleeds persistent pipe flux each tick
                const float dryThreshold = 1e-4f; // auto-settles sediment in drying cells
                var maxSpeed = cellSize; // caps |v| → caps capacity → caps erosion
                var maxStepDelta = cellSize * 0.1f; // caps per-tick terrain change (anti-pit)

                for (var iteration = 0; iteration < settings.Iterations; iteration++)
                {
                    // 0-1. Flux damping + rain.
                    simulationHandle = new FluxDampingRainJob
                    {
                        KeepFlux = 1f - fluxDamping,
                        RainAmount = rainAmount,
                        OutFlow = outFlow,
                        Water = water
                    }.ScheduleParallel(verticesCount, jobBatchSize, simulationHandle);
                    hasScheduledWork = true;

                    // 2. Flux accumulation per pipe.
                    simulationHandle = new FluxAccumulationJob
                    {
                        FlowRate = flowRate,
                        Terrain = terrain,
                        Water = water,
                        NeighborIndices = neighborIndices,
                        OutFlow = outFlow
                    }.ScheduleParallel(verticesCount, jobBatchSize, simulationHandle);

                    // 3. Scale per-cell outflow.
                    simulationHandle = new ScaleOutflowJob
                    {
                        Water = water,
                        OutFlow = outFlow
                    }.ScheduleParallel(verticesCount, jobBatchSize, simulationHandle);

                    // 4. Water update from net flux.
                    simulationHandle = new ComputeDeltaWaterJob
                    {
                        OutFlow = outFlow,
                        NeighborIndices = neighborIndices,
                        OppositeDir = oppositeDir,
                        DeltaWater = deltaWater
                    }.ScheduleParallel(verticesCount, jobBatchSize, simulationHandle);

                    simulationHandle = new ApplyWaterDeltaJob
                    {
                        Water = water,
                        DeltaWater = deltaWater
                    }.ScheduleParallel(verticesCount, jobBatchSize, simulationHandle);

                    // 5. Velocity field.
                    simulationHandle = new ComputeVelocityJob
                    {
                        Epsilon = epsilon,
                        MaxSpeed = maxSpeed,
                        OutFlow = outFlow,
                        NeighborIndices = neighborIndices,
                        OppositeDir = oppositeDir,
                        DirUnit = dirUnit,
                        Water = water,
                        Velocity = velocity
                    }.ScheduleParallel(verticesCount, jobBatchSize, simulationHandle);

                    // 6. Capacity / erosion / deposition.
                    simulationHandle = new ErodeDepositJob
                    {
                        VertexCount = verticesCount,
                        PipeLength = pipeLength,
                        MinTilt = minTilt,
                        SedimentCapacity = sedimentCapacity,
                        ErosionRate = erosionRate,
                        DepositionRate = depositionRate,
                        MaxStepDelta = maxStepDelta,
                        NeighborIndices = neighborIndices,
                        Velocity = velocity,
                        Terrain = terrain,
                        Sediment = sediment
                    }.Schedule(simulationHandle);

                    // 7. Semi-Lagrangian sediment advection.
                    //    AdvectSedimentJob writes to nextSediment; swap buffers instead of copying —
                    //    the scheduled job captured its own NativeArray references before the swap.
                    simulationHandle = new AdvectSedimentJob
                    {
                        CellSize = cellSize,
                        MaxBackTrace = maxBackTrace,
                        Velocity = velocity,
                        CenterWorld = centerWorld,
                        Sediment = sediment,
                        IndexByCoord = indexByCoord,
                        NextSediment = nextSediment
                    }.ScheduleParallel(verticesCount, jobBatchSize, simulationHandle);
                    (sediment, nextSediment) = (nextSediment, sediment);

                    // 8. Evaporation.
                    simulationHandle = new EvaporationJob
                    {
                        Keep = 1f - evaporation,
                        Water = water
                    }.ScheduleParallel(verticesCount, jobBatchSize, simulationHandle);

                    // 9. Auto-settle.
                    simulationHandle = new AutoSettleJob
                    {
                        DryThreshold = dryThreshold,
                        Water = water,
                        Sediment = sediment,
                        Terrain = terrain
                    }.ScheduleParallel(verticesCount, jobBatchSize, simulationHandle);
                }

                // Final safety dump — should be near zero after auto-settle handled drying cells.
                simulationHandle = new FinalDepositJob
                {
                    Terrain = terrain,
                    Sediment = sediment
                }.ScheduleParallel(verticesCount, jobBatchSize, simulationHandle);
                hasScheduledWork = true;

                await simulationHandle.ToUniTask(PlayerLoopTiming.Update);
                hasScheduledWork = false;

                // Write back directly — no resultHeights dictionary needed.
                for (var i = 0; i < verticesCount; i++)
                {
                    if (!vertexGrid.TryGet(simCoords[i], out var vertex))
                        continue;

                    vertex.Position.y = terrain[i];
                    vertexGrid.Set(simCoords[i], vertex);
                }
            }
            finally
            {
                if (hasScheduledWork)
                    simulationHandle.Complete();

                simCoords.Dispose();
                indexByCoord.Dispose();
                neighborIndices.Dispose();
                oppositeDir.Dispose();
                dirUnit.Dispose();
                terrain.Dispose();
                water.Dispose();
                sediment.Dispose();
                outFlow.Dispose();
                velocity.Dispose();
                deltaWater.Dispose();
                nextSediment.Dispose();
                centerWorld.Dispose();
            }
        }

        /// <summary>
        ///     Blurs vertex heights by averaging each vertex with its radius-hop neighborhood.
        ///     All simulation data lives in unmanaged NativeArrays to avoid GC pressure.
        /// </summary>
        /// <param name="vertexGrid">Vertex grid whose Y heights are updated in place.</param>
        /// <param name="radius">BFS radius in hex steps.</param>
        /// <param name="iterations">Number of blur passes.</param>
        public static void ApplyRadiusBlur(VertexGrid vertexGrid, int radius, int iterations = 1)
        {
            if (vertexGrid == null)
                return;
            if (radius <= 0 || iterations <= 0)
                return;

            var simCoords = BuildSimCoords(vertexGrid, Allocator.Persistent);
            if (simCoords.Length == 0)
                return;

            var count = simCoords.Length;
            var coordIndex = new NativeHashMap<VertexCoord, int>(count, Allocator.Persistent);
            var sourceHts = new NativeArray<float>(count, Allocator.Persistent);
            var blurredHts = new NativeArray<float>(count, Allocator.Persistent);

            try
            {
                for (var i = 0; i < count; i++)
                {
                    coordIndex[simCoords[i]] = i;
                    vertexGrid.TryGet(simCoords[i], out var v);
                    sourceHts[i] = v.Position.y;
                }

                for (var iteration = 0; iteration < iterations; iteration++)
                {
                    for (var i = 0; i < count; i++)
                        blurredHts[i] = ComputeAverageHeightInRadius(simCoords[i], radius, vertexGrid, sourceHts, coordIndex);

                    blurredHts.CopyTo(sourceHts);
                }

                for (var i = 0; i < count; i++)
                {
                    if (!vertexGrid.TryGet(simCoords[i], out var vertex))
                        continue;

                    vertex.Position.y = sourceHts[i];
                    vertexGrid.Set(simCoords[i], vertex);
                }
            }
            finally
            {
                simCoords.Dispose();
                coordIndex.Dispose();
                sourceHts.Dispose();
                blurredHts.Dispose();
            }
        }

        /// <summary>
        ///     Snapshots all vertex coords from the grid into a flat NativeArray.
        ///     Every coord present in vertexGrid is valid and owned — no filtering needed.
        /// </summary>
        private static NativeArray<VertexCoord> BuildSimCoords(VertexGrid vertexGrid, Allocator allocator)
        {
            var result = new NativeArray<VertexCoord>(vertexGrid.Count, allocator);
            var index = 0;
            foreach (var coord in vertexGrid.Coords)
                result[index++] = coord;
            return result;
        }

        /// <summary>
        ///     BFS average height within <paramref name="radius" /> hops of <paramref name="center" />.
        ///     Height lookups use a pre-built NativeArray indexed by <paramref name="coordIndex" />.
        ///     The BFS queue and visited set remain managed (local, short-lived, not in a hot loop).
        /// </summary>
        private static float ComputeAverageHeightInRadius(
            VertexCoord center,
            int radius,
            VertexGrid vertexGrid,
            NativeArray<float> sourceHeights,
            NativeHashMap<VertexCoord, int> coordIndex)
        {
            var queue = new NativeQueue<(VertexCoord Coord, int Depth)>(Allocator.Persistent);
            var visited = new NativeHashSet<VertexCoord>(50, Allocator.Persistent);

            visited.Add(center);
            queue.Enqueue((center, 0));

            var sum = 0f;
            var count = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (coordIndex.TryGetValue(current.Coord, out var idx))
                {
                    sum += sourceHeights[idx];
                    count++;
                }

                if (current.Depth >= radius)
                    continue;

                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var neighbor = current.Coord + AxialMath.NeighborDirs[d];
                    if (!vertexGrid.Contains(neighbor))
                        continue;
                    if (!visited.Add(neighbor))
                        continue;

                    queue.Enqueue((neighbor, current.Depth + 1));
                }
            }

            queue.Dispose();
            visited.Dispose();

            if (count == 0)
                return coordIndex.TryGetValue(center, out var cIdx) ? sourceHeights[cIdx] : 0f;

            return sum / count;
        }

        /// <summary>
        ///     Samples a per-cell scalar field at an arbitrary world-XZ point on a flat-top axial hex
        ///     grid using barycentric interpolation over the three nearest hex centers (the triangle
        ///     of the enclosing axial parallelogram the point falls into). Weights are renormalized
        ///     when enclosing cells are missing from the grid; returns <paramref name="fallback" />
        ///     if none of the three cells exist.
        /// </summary>
        /// <param name="worldXZ">Sampling location in world XZ coordinates.</param>
        /// <param name="cellSize">Axial cell size of the source grid (flat-top orientation).</param>
        /// <param name="values">Scalar field indexed by <paramref name="indexByCoord" />.</param>
        /// <param name="indexByCoord">Lookup from VertexCoord to field index.</param>
        /// <param name="fallback">Value returned when the 3 enclosing cells are all outside the grid.</param>
        private static float SampleAxialScalar(
            float2 worldXZ,
            float cellSize,
            NativeArray<float> values,
            NativeHashMap<VertexCoord, int> indexByCoord,
            float fallback)
        {
            // Flat-top inverse mapping: q = x/(1.5·c); r = z/(√3·c) − q/2.
            var qf = worldXZ.x / (1.5f * cellSize);
            var rf = worldXZ.y / (math.sqrt(3f) * cellSize) - qf * 0.5f;

            var q0 = (int)math.floor(qf);
            var r0 = (int)math.floor(rf);
            var fx = qf - q0;
            var fy = rf - r0;

            // The axial unit parallelogram splits into two hex-mutual-neighbor triangles along fx+fy=1.
            int2 cA, cB, cC;
            float wA, wB, wC;
            if (fx + fy <= 1f)
            {
                cA = new int2(q0, r0);
                wA = 1f - fx - fy;
                cB = new int2(q0 + 1, r0);
                wB = fx;
                cC = new int2(q0, r0 + 1);
                wC = fy;
            }
            else
            {
                cA = new int2(q0 + 1, r0 + 1);
                wA = fx + fy - 1f;
                cB = new int2(q0, r0 + 1);
                wB = 1f - fx;
                cC = new int2(q0 + 1, r0);
                wC = 1f - fy;
            }

            var sum = 0f;
            var weightSum = 0f;
            if (indexByCoord.TryGetValue(new VertexCoord(cA), out var iA))
            {
                sum += wA * values[iA];
                weightSum += wA;
            }
            if (indexByCoord.TryGetValue(new VertexCoord(cB), out var iB))
            {
                sum += wB * values[iB];
                weightSum += wB;
            }
            if (indexByCoord.TryGetValue(new VertexCoord(cC), out var iC))
            {
                sum += wC * values[iC];
                weightSum += wC;
            }

            return weightSum > 0f ? sum / weightSum : fallback;
        }

        public struct HydraulicErosionSettings
        {
            /// <summary>
            ///     Number of simulation passes.
            /// </summary>
            public int Iterations;

            /// <summary>
            ///     Added water per vertex per pass.
            /// </summary>
            public float RainAmount;

            /// <summary>
            ///     Fraction of water moved to lower neighbors per pass [0..1].
            /// </summary>
            public float FlowRate;

            /// <summary>
            ///     Fraction of water evaporated per pass [0..1].
            /// </summary>
            public float Evaporation;

            /// <summary>
            ///     Global multiplier of carried sediment capacity.
            /// </summary>
            public float SedimentCapacity;

            /// <summary>
            ///     Terrain-to-sediment conversion rate [0..1].
            /// </summary>
            public float ErosionRate;

            /// <summary>
            ///     Sediment-to-terrain conversion rate [0..1].
            /// </summary>
            public float DepositionRate;
        }

        public struct WindErosionSettings
        {
            /// <summary>
            ///     Axial neighbor direction of wind (must be one of AxialMath.NeighborDirs).
            ///     Example: new HexCoord(1, 0), new HexCoord(0, 1), etc.
            /// </summary>
            public HexCoord WindDirection;

            /// <summary>
            ///     Global multiplier for erosion intensity [0..1].
            /// </summary>
            public float WindStrength;

            /// <summary>
            ///     Material transport factor per iteration [0..1].
            /// </summary>
            public float TransportRate;

            /// <summary>
            ///     Number of erosion passes.
            /// </summary>
            public int Iterations;

            /// <summary>
            ///     Absolute cap of moved material per vertex per pass.
            /// </summary>
            public float MaxTransportPerIteration;
        }

        [BurstCompile]
        private struct AdvectSedimentJob : IJobFor
        {
            public float CellSize;
            public float MaxBackTrace;
            [ReadOnly] public NativeArray<float2> Velocity;
            [ReadOnly] public NativeArray<float2> CenterWorld;
            [ReadOnly] public NativeArray<float> Sediment;
            [ReadOnly] public NativeHashMap<VertexCoord, int> IndexByCoord;
            [WriteOnly] public NativeArray<float> NextSediment;

            public void Execute(int index)
            {
                var disp = Velocity[index];
                var magnitude = math.length(disp);
                if (magnitude > MaxBackTrace)
                    disp *= MaxBackTrace / magnitude;

                NextSediment[index] = SampleAxialScalar(
                    CenterWorld[index] - disp,
                    CellSize,
                    Sediment,
                    IndexByCoord,
                    Sediment[index]);
            }
        }

        [BurstCompile]
        private struct ApplyWaterDeltaJob : IJobFor
        {
            public NativeArray<float> Water;
            [ReadOnly] public NativeArray<float> DeltaWater;

            public void Execute(int index)
            {
                Water[index] = math.max(0f, Water[index] + DeltaWater[index]);
            }
        }

        [BurstCompile]
        private struct AutoSettleJob : IJobFor
        {
            public float DryThreshold;
            [ReadOnly] public NativeArray<float> Water;
            public NativeArray<float> Sediment;
            public NativeArray<float> Terrain;

            public void Execute(int index)
            {
                if (Water[index] >= DryThreshold || Sediment[index] <= 0f)
                    return;

                Terrain[index] += Sediment[index];
                Sediment[index] = 0f;
            }
        }

        [BurstCompile]
        private struct ComputeDeltaWaterJob : IJobFor
        {
            [ReadOnly] public NativeArray<float> OutFlow;
            [ReadOnly] public NativeArray<int> NeighborIndices;
            [ReadOnly] public NativeArray<int> OppositeDir;
            public NativeArray<float> DeltaWater;

            public void Execute(int index)
            {
                const int nc = AxialMath.NeighborCount;
                var rowStart = index * nc;
                var sumOut = 0f;
                var sumIn = 0f;
                for (var d = 0; d < nc; d++)
                {
                    sumOut += OutFlow[rowStart + d];

                    var neighborIndex = NeighborIndices[rowStart + d];
                    if (neighborIndex < 0)
                        continue;

                    sumIn += OutFlow[neighborIndex * nc + OppositeDir[d]];
                }

                DeltaWater[index] = sumIn - sumOut;
            }
        }

        [BurstCompile]
        private struct ComputeVelocityJob : IJobFor
        {
            public float Epsilon;
            public float MaxSpeed;
            [ReadOnly] public NativeArray<float> OutFlow;
            [ReadOnly] public NativeArray<int> NeighborIndices;
            [ReadOnly] public NativeArray<int> OppositeDir;
            [ReadOnly] public NativeArray<float2> DirUnit;
            [ReadOnly] public NativeArray<float> Water;
            [WriteOnly] public NativeArray<float2> Velocity;

            public void Execute(int index)
            {
                const int nc = AxialMath.NeighborCount;
                var rowStart = index * nc;
                var acc = float2.zero;
                for (var d = 0; d < nc; d++)
                {
                    var outD = OutFlow[rowStart + d];
                    var neighborIndex = NeighborIndices[rowStart + d];
                    var inD = neighborIndex >= 0
                        ? OutFlow[neighborIndex * nc + OppositeDir[d]]
                        : 0f;
                    acc += 0.5f * (outD - inD) * DirUnit[d];
                }

                var velocity = acc / math.max(Water[index], Epsilon);
                var speed = math.length(velocity);
                if (speed > MaxSpeed)
                    velocity *= MaxSpeed / speed;

                Velocity[index] = velocity;
            }
        }

        /// <summary>
        ///     Applies erosion and deposition to each vertex based on sediment capacity.
        ///     Runs as <see cref="IJob"/> (single background thread, not main thread) because the loop is
        ///     Gauss-Seidel: terrain[neighborIndex] read at vertex i may already reflect changes from
        ///     an earlier vertex in the same pass. Switching to IJobFor (Jacobi) would require a
        ///     separate delta buffer and changes the numerical character of the erosion.
        /// </summary>
        [BurstCompile]
        private struct ErodeDepositJob : IJob
        {
            public int VertexCount;
            public float PipeLength;

            public float MinTilt;
            public float SedimentCapacity;
            public float ErosionRate;
            public float DepositionRate;
            public float MaxStepDelta;
            [ReadOnly] public NativeArray<int> NeighborIndices;
            [ReadOnly] public NativeArray<float2> Velocity;
            public NativeArray<float> Terrain;
            public NativeArray<float> Sediment;

            public void Execute()
            {
                const int nc = AxialMath.NeighborCount;
                for (var index = 0; index < VertexCount; index++)
                {
                    var rowStart = index * nc;
                    var maxDrop = 0f;
                    for (var d = 0; d < nc; d++)
                    {
                        var neighborIndex = NeighborIndices[rowStart + d];
                        if (neighborIndex < 0)
                            continue;

                        var drop = Terrain[index] - Terrain[neighborIndex];
                        if (drop > maxDrop)
                            maxDrop = drop;
                    }

                    var tilt = maxDrop / math.sqrt(maxDrop * maxDrop + PipeLength * PipeLength);
                    tilt = math.max(tilt, MinTilt);

                    var speed = math.length(Velocity[index]);
                    var capacity = SedimentCapacity * tilt * speed;

                    if (Sediment[index] > capacity)
                    {
                        var deposit = math.min((Sediment[index] - capacity) * DepositionRate, MaxStepDelta);
                        Sediment[index] -= deposit;
                        Terrain[index] += deposit;
                        continue;
                    }

                    // Terrain height is an absolute elevation, not a "remaining material" budget.
                    // Negative elevations (for river/lake/sea beds) must still be erodible, so the
                    // per-step clamp may depend on simulation stability only, not on Terrain[index].
                    var erode = math.min((capacity - Sediment[index]) * ErosionRate, MaxStepDelta);
                    if (erode <= 0f)
                        continue;

                    Terrain[index] -= erode;
                    Sediment[index] += erode;
                }
            }
        }

        [BurstCompile]
        private struct EvaporationJob : IJobFor
        {
            public float Keep;
            public NativeArray<float> Water;

            public void Execute(int index)
            {
                Water[index] *= Keep;
            }
        }

        [BurstCompile]
        private struct FinalDepositJob : IJobFor
        {
            public NativeArray<float> Terrain;
            [ReadOnly] public NativeArray<float> Sediment;

            public void Execute(int index)
            {
                Terrain[index] += Sediment[index];
            }
        }

        [BurstCompile]
        private struct FluxAccumulationJob : IJobFor
        {
            public float FlowRate;
            [ReadOnly] public NativeArray<float> Terrain;
            [ReadOnly] public NativeArray<float> Water;
            [ReadOnly] public NativeArray<int> NeighborIndices;
            [NativeDisableParallelForRestriction] public NativeArray<float> OutFlow;

            public void Execute(int index)
            {
                var rowStart = index * AxialMath.NeighborCount;
                var surface = Terrain[index] + Water[index];
                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var neighborIndex = NeighborIndices[rowStart + d];
                    if (neighborIndex < 0)
                    {
                        OutFlow[rowStart + d] = 0f;
                        continue;
                    }

                    var head = surface - (Terrain[neighborIndex] + Water[neighborIndex]);
                    var updated = OutFlow[rowStart + d] + FlowRate * head;
                    OutFlow[rowStart + d] = math.max(0f, updated);
                }
            }
        }

        [BurstCompile]
        private struct FluxDampingRainJob : IJobFor
        {
            public float KeepFlux;
            public float RainAmount;
            [NativeDisableParallelForRestriction] public NativeArray<float> OutFlow;
            public NativeArray<float> Water;

            public void Execute(int index)
            {
                var rowStart = index * AxialMath.NeighborCount;
                for (var d = 0; d < AxialMath.NeighborCount; d++)
                    OutFlow[rowStart + d] *= KeepFlux;

                if (RainAmount > 0f)
                    Water[index] += RainAmount;
            }
        }

        [BurstCompile]
        private struct ScaleOutflowJob : IJobFor
        {
            [ReadOnly] public NativeArray<float> Water;
            [NativeDisableParallelForRestriction] public NativeArray<float> OutFlow;

            public void Execute(int index)
            {
                var rowStart = index * AxialMath.NeighborCount;
                var sumOut = 0f;
                for (var d = 0; d < AxialMath.NeighborCount; d++)
                    sumOut += OutFlow[rowStart + d];
                if (sumOut <= 0f || sumOut <= Water[index])
                    return;

                var scale = Water[index] / sumOut;
                for (var d = 0; d < AxialMath.NeighborCount; d++)
                    OutFlow[rowStart + d] *= scale;
            }
        }
    }
}
