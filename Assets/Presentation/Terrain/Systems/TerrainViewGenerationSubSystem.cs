using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.Hex.Tags;
using Domains.Map.Hex.Utils;
using Presentation.Terrain.Components;
using Presentation.Terrain.CurveBuilders;
using Presentation.Terrain.Data;
using Presentation.Terrain.Isolines;
using Presentation.Terrain.Smooth;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Presentation.Terrain.Systems
{
    /// <summary>
    ///     Builds isoline-based terrain geometry and applies erosion passes to the
    ///     <see cref="VertexGrid" /> loaded from ECS. Runs as a <see cref="ViewSubSystem" />
    ///     under the <see cref="TerrainViewSystem" /> orchestrator.
    /// </summary>
    [UsedImplicitly]
    internal sealed class TerrainViewGenerationSubSystem : ViewSubSystem
    {
        private const int ExecutionPriority = 100;

        private readonly EntitySet _hexSet;
        private readonly EntityMultiMap<HexTypeComponent> _hexesByType;
        private readonly World _world;

        /// <inheritdoc />
        public override int Priority => ExecutionPriority;

        /// <summary>
        ///     Creates the generation subsystem bound to the shared ECS world.
        /// </summary>
        /// <param name="world">World used to query terrain configs and hex entities.</param>
        public TerrainViewGenerationSubSystem(World world)
        {
            _world = world;
            _hexSet = world.GetEntities().With<HexIdComponent>().AsSet();
            _hexesByType = world.GetEntities().With<HexTag>().AsMultiMap<HexTypeComponent>();
        }

        /// <inheritdoc />
        public override async UniTask Update(GameState state, CancellationToken cancellationToken)
        {
            if (!HasRequiredConfigEntities())
                return;

            if (!_world.Has<VertexGridComponent>())
                throw new InvalidOperationException("TerrainViewGenerationSubSystem: VertexGridComponent world component is missing.");

            var vertexGrid = _world.Get<VertexGridComponent>().Grid;
            var config = _world.Get<TerrainViewConfigComponent>();
            var innerConfig = _world.Get<InnerIsolineConfigComponent>();
            var outerConfig = _world.Get<OuterIsolineConfigComponent>();
            var heightSmoothingConfig = _world.Get<HeightSmoothingConfigComponent>();
            var hydraulicConfig = _world.Get<HydraulicErosionConfigComponent>();
            var windConfig = _world.Get<WindErosionConfigComponent>();

            await BuildIsolinesAsync(vertexGrid, config, innerConfig, outerConfig, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            await ApplyWindErosionAsync(vertexGrid, config, windConfig, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            await ApplyHydraulicErosionAsync(vertexGrid, hydraulicConfig, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            await ApplyFinalHeightSmoothingAsync(vertexGrid, heightSmoothingConfig, cancellationToken);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _hexSet.Dispose();
            base.Dispose();
        }

        /// <summary>
        ///     Validates that all required singleton config entity sets contain at least one entity.
        /// </summary>
        /// <returns><c>true</c> if all config sets are populated; <c>false</c> with a logged error otherwise.</returns>
        private bool HasRequiredConfigEntities()
        {
            if (_world.Has<TerrainViewConfigComponent>() &&
                _world.Has<InnerIsolineConfigComponent>() && _world.Has<OuterIsolineConfigComponent>() &&
                _world.Has<HeightSmoothingConfigComponent>() && _world.Has<HydraulicErosionConfigComponent>() &&
                _world.Has<WindErosionConfigComponent>())
                return true;

            Debug.LogError("[TerrainViewGenerationSubSystem] One or more required configs are missing.");
            return false;
        }

        /// <summary>
        ///     Collects hex data, builds the vertex grid and applies all isoline transitions
        ///     (hills, elevated terrain, water) on a background thread.
        /// </summary>
        /// <param name="vertexGrid">Vertex grid to populate and shape.</param>
        /// <param name="config">Terrain view config carrying height scale.</param>
        /// <param name="innerConfig">Inner isoline curve parameters.</param>
        /// <param name="outerConfig">Outer isoline curve parameters.</param>
        /// <param name="cancellationToken">Token that aborts the work.</param>
        private async UniTask BuildIsolinesAsync(
            VertexGrid vertexGrid,
            TerrainViewConfigComponent config,
            InnerIsolineConfigComponent innerConfig,
            OuterIsolineConfigComponent outerConfig,
            CancellationToken cancellationToken)
        {
            var hexCoordDomain = CollectHexCoordDomain();
            var waterEdgePaddingHexes = CollectWaterEdgePaddingHexCoords(hexCoordDomain);
            var hexCoordsArray = CollectHexCoordsArray(waterEdgePaddingHexes);
            var elevatedHexes  = CollectElevatedHexCoords();
            var hillHexes      = CollectHexCoordsOfType(HexType.Mount);
            var waterHexes     = CollectWaterHexCoords(waterEdgePaddingHexes);

            try
            {
                await UniTask.RunOnThreadPool(() =>
                {
                    vertexGrid.BuildVertices(hexCoordsArray);

                    if (!BuildAndApplyIsolineTransition(hillHexes, 2f * config.HeightScale, 0f, vertexGrid, innerConfig, outerConfig))
                        Debug.LogWarning("[TerrainViewGenerationSubSystem] Hill isoline transition failed.");

                    if (!BuildAndApplyIsolineTransition(elevatedHexes, 1f * config.HeightScale, 0f, vertexGrid, innerConfig, outerConfig))
                        Debug.LogWarning("[TerrainViewGenerationSubSystem] Elevated isoline transition failed.");

                    if (!BuildAndApplyIsolineTransition(waterHexes, -1f * config.HeightScale, 0f, vertexGrid, innerConfig, outerConfig, isDepression: true))
                        Debug.LogWarning("[TerrainViewGenerationSubSystem] Water isoline transition failed.");
                }, cancellationToken: cancellationToken);
            }
            finally
            {
                hexCoordDomain.Dispose();
                waterEdgePaddingHexes.Dispose();
                hexCoordsArray.Dispose();
                elevatedHexes.Dispose();
                hillHexes.Dispose();
                waterHexes.Dispose();
            }
        }

        /// <summary>
        ///     Allocates a <see cref="NativeList{T}" /> with all elevated <see cref="HexCoord" /> values.
        ///     Elevated hexes are those of <see cref="HexType" /> Mount or Bedhill. Caller is
        ///     responsible for disposing the returned list.
        /// </summary>
        private NativeList<HexCoord> CollectElevatedHexCoords()
        {
            var result = new NativeList<HexCoord>(Allocator.Persistent);
            AddCoordsOfType(HexType.Mount, ref result);
            AddCoordsOfType(HexType.Bedhill, ref result);
            return result;
        }

        /// <summary>
        ///     Allocates a <see cref="NativeList{T}" /> with the <see cref="HexCoord" /> of every hex of
        ///     the given <see cref="HexType" /> (the matching bucket of the AsMultiMap&lt;HexTypeComponent&gt;
        ///     index). Caller is responsible for disposing the returned list.
        /// </summary>
        private NativeList<HexCoord> CollectHexCoordsOfType(HexType type)
        {
            var result = new NativeList<HexCoord>(Allocator.Persistent);
            AddCoordsOfType(type, ref result);
            return result;
        }

        /// <summary>
        ///     Appends the <see cref="HexCoord" /> of every hex of <paramref name="type" /> into
        ///     <paramref name="result" />. Passed by ref so a list re-allocation is visible to the caller.
        /// </summary>
        private void AddCoordsOfType(HexType type, ref NativeList<HexCoord> result)
        {
            if (!_hexesByType.TryGetEntities(new HexTypeComponent { Type = type }, out var entities))
                return;

            foreach (ref readonly var entity in entities)
                result.Add(entity.Get<HexIdComponent>().Coords);
        }

        /// <summary>
        ///     Allocates a <see cref="NativeHashSet{T}" /> with all map hex coordinates currently present in ECS.
        ///     Caller is responsible for disposing the returned set.
        /// </summary>
        private NativeHashSet<HexCoord> CollectHexCoordDomain()
        {
            var entities = _hexSet.GetEntities();
            var hexDomain = new NativeHashSet<HexCoord>(math.max(1, entities.Length), Allocator.Persistent);

            foreach (ref readonly var entity in entities)
                hexDomain.Add(entity.Get<HexIdComponent>().Coords);

            return hexDomain;
        }

        /// <summary>
        ///     Allocates a <see cref="NativeList{T}" /> with ghost water hex coordinates that sit just outside the map.
        ///     A ghost hex is added for each missing direct neighbor of a water hex touching the map border.
        ///     Caller is responsible for disposing the returned list.
        /// </summary>
        /// <param name="hexDomain">Set of all real hex coordinates present on the map.</param>
        private NativeList<HexCoord> CollectWaterEdgePaddingHexCoords(NativeHashSet<HexCoord> hexDomain)
        {
            var ghostHexes = new NativeList<HexCoord>(Allocator.Persistent);
            if (!_hexesByType.TryGetEntities(new HexTypeComponent { Type = HexType.Water }, out var waterEntities))
                return ghostHexes;

            var uniqueGhostHexes = new NativeHashSet<HexCoord>(
                math.max(1, waterEntities.Length * AxialMath.NeighborCount),
                Allocator.Temp);

            try
            {
                foreach (ref readonly var entity in waterEntities)
                {
                    var waterHex = entity.Get<HexIdComponent>().Coords;
                    for (var direction = 0; direction < AxialMath.NeighborCount; direction++)
                    {
                        var neighbor = waterHex + AxialMath.NeighborsPointyTop[direction];
                        if (hexDomain.Contains(neighbor) || !uniqueGhostHexes.Add(neighbor))
                            continue;

                        ghostHexes.Add(neighbor);
                    }
                }

                return ghostHexes;
            }
            finally
            {
                uniqueGhostHexes.Dispose();
            }
        }

        /// <summary>
        ///     Allocates a <see cref="NativeList{T}" /> with real water hexes plus ghost padding hexes used to
        ///     keep water transitions flat when the water body reaches the map edge.
        ///     Caller is responsible for disposing the returned list.
        /// </summary>
        /// <param name="waterEdgePaddingHexes">Ghost water hexes located outside the real map domain.</param>
        private NativeList<HexCoord> CollectWaterHexCoords(NativeList<HexCoord> waterEdgePaddingHexes)
        {
            var waterHexes = CollectHexCoordsOfType(HexType.Water);

            foreach (var ghostHex in waterEdgePaddingHexes)
                waterHexes.Add(ghostHex);

            return waterHexes;
        }

        /// <summary>
        ///     Allocates a <see cref="NativeArray{T}" /> and fills it with <see cref="HexCoord" /> values
        ///     from all entities carrying a <see cref="HexIdComponent" /> plus any extra ghost hexes.
        ///     Caller is responsible for disposing the returned array.
        /// </summary>
        /// <param name="extraHexes">Additional hexes to append to the vertex-grid build domain.</param>
        private NativeArray<HexCoord> CollectHexCoordsArray(NativeList<HexCoord> extraHexes)
        {
            var entities = _hexSet.GetEntities();
            var hexCoords = new NativeArray<HexCoord>(entities.Length + extraHexes.Length, Allocator.Persistent);

            for (var i = 0; i < entities.Length; i++)
                hexCoords[i] = entities[i].Get<HexIdComponent>().Coords;

            for (var i = 0; i < extraHexes.Length; i++)
                hexCoords[entities.Length + i] = extraHexes[i];

            return hexCoords;
        }

        /// <summary>
        ///     Builds inner and outer isolines from <paramref name="sourceHexes" />, fills the interior
        ///     at <paramref name="innerHeight" />, then applies a smooth slope transition to
        ///     <paramref name="outerHeight" />. When <paramref name="isDepression" /> is true, uses
        ///     depression-specific fill and transition (for water / below-ground tiles).
        /// </summary>
        /// <returns><c>false</c> if either isoline could not be built or the source list is empty.</returns>
        private bool BuildAndApplyIsolineTransition(
            NativeList<HexCoord> sourceHexes,
            float innerHeight,
            float outerHeight,
            VertexGrid vertexGrid,
            in InnerIsolineConfigComponent innerConfig,
            in OuterIsolineConfigComponent outerConfig,
            bool isDepression = false)
        {
            if (sourceHexes.Length == 0)
                return false;

            var insideSeed = vertexGrid.GetCenterVertexCoord(sourceHexes[0]);

            var innerCurveBuilder = new NaturalLineCurveBuilder(
                innerConfig.BaseFrequency, innerConfig.Octaves, innerConfig.Persistence,
                innerConfig.Lacunarity, innerConfig.SmoothingPasses, innerConfig.CurveSeed);

            var innerIsoline = new FieldBasedIsolineBuilder(
                    vertexGrid,
                    innerCurveBuilder,
                    new IsolineBuilderData(innerHeight, innerConfig.DepthCenter, innerConfig.DepthDeviation, innerConfig.Side))
                .BuildIsoLine(sourceHexes.AsArray());

            if (innerIsoline == null)
                return false;

            if (isDepression)
                IsolineSlopeTransition.FillDepressionInsideIsoline(vertexGrid, innerIsoline, insideSeed, innerHeight);
            else
                IsolineSlopeTransition.FillInsideIsoline(vertexGrid, innerIsoline, insideSeed, innerHeight);

            var outerCurveBuilder = new NaturalLineCurveBuilder(
                outerConfig.BaseFrequency, outerConfig.Octaves, outerConfig.Persistence,
                outerConfig.Lacunarity, outerConfig.SmoothingPasses, outerConfig.CurveSeed);

            var outerIsoline = new FieldBasedIsolineBuilder(
                    vertexGrid,
                    outerCurveBuilder,
                    new IsolineBuilderData(outerHeight, outerConfig.DepthCenter, outerConfig.DepthDeviation, outerConfig.Side))
                .BuildIsoLine(sourceHexes.AsArray());

            if (outerIsoline == null)
                return false;

            if (isDepression)
                IsolineSlopeTransition.ApplyDepressionTransitionBetweenIsoLines(vertexGrid, innerIsoline, outerIsoline, insideSeed);
            else
                IsolineSlopeTransition.ApplyTransitionBetweenIsoLines(vertexGrid, innerIsoline, outerIsoline, insideSeed);

            return true;
        }

        /// <summary>
        ///     Applies the hydraulic erosion pipe model using the loaded config and awaits completion.
        /// </summary>
        /// <param name="vertexGrid">Vertex grid whose heights are eroded in place.</param>
        /// <param name="hydraulicConfig">Flattened hydraulic erosion config loaded from ECS.</param>
        /// <param name="cancellationToken">Cancellation token for skipping work before execution starts.</param>
        private async UniTask ApplyHydraulicErosionAsync(
            VertexGrid vertexGrid,
            HydraulicErosionConfigComponent hydraulicConfig,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (!hydraulicConfig.EnableHydraulicErosion)
                return;

            var settings = new HeightSmoothing.HydraulicErosionSettings
            {
                Iterations = hydraulicConfig.HydraulicIterations,
                RainAmount = hydraulicConfig.HydraulicRainAmount,
                FlowRate = hydraulicConfig.HydraulicFlowRate,
                Evaporation = hydraulicConfig.HydraulicEvaporation,
                SedimentCapacity = hydraulicConfig.HydraulicSedimentCapacity,
                ErosionRate = hydraulicConfig.HydraulicErosionRate,
                DepositionRate = hydraulicConfig.HydraulicDepositionRate
            };

            await HeightSmoothing.ApplyHydraulicErosionPipeModel(vertexGrid, settings);
        }

        /// <summary>
        ///     Applies directional wind erosion for the configured number of passes, choosing a new random
        ///     axial wind direction for each pass.
        /// </summary>
        /// <param name="vertexGrid">Vertex grid whose heights are eroded in place.</param>
        /// <param name="terrainConfig">Terrain view config carrying the number of wind passes.</param>
        /// <param name="windConfig">Flattened wind erosion config loaded from ECS.</param>
        /// <param name="cancellationToken">Cancellation token for skipping work before execution starts.</param>
        private async UniTask ApplyWindErosionAsync(
            VertexGrid vertexGrid,
            TerrainViewConfigComponent terrainConfig,
            WindErosionConfigComponent windConfig,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (!windConfig.EnableWindErosion)
                return;

            if (terrainConfig.WindErosionPasses <= 0)
                return;

            var directions = new HexCoord[terrainConfig.WindErosionPasses];
            for (var pass = 0; pass < directions.Length; pass++)
            {
                var directionIndex = Random.Range(0, AxialMath.NeighborCount);
                directions[pass] = new HexCoord(AxialMath.NeighborsPointyTop[directionIndex]);
            }

            await UniTask.RunOnThreadPool(() =>
            {
                for (var pass = 0; pass < directions.Length; pass++)
                {
                    var settings = new HeightSmoothing.WindErosionSettings
                    {
                        WindDirection = directions[pass],
                        WindStrength = windConfig.WindStrength,
                        TransportRate = windConfig.WindTransportRate,
                        Iterations = windConfig.WindErosionIterations,
                        MaxTransportPerIteration = windConfig.WindMaxTransportPerIteration
                    };

                    HeightSmoothing.ApplyDirectionalWindErosion(vertexGrid, settings);
                }
            }, cancellationToken: cancellationToken);
        }

        /// <summary>
        ///     Applies the configured height blur as the final terrain pass to soften sharp artifacts
        ///     left by erosion and deposition before the mesh is updated.
        ///     Offloaded to a background thread via <see cref="UniTask.RunOnThreadPool" /> to avoid blocking the main loop.
        /// </summary>
        /// <param name="vertexGrid">Vertex grid whose heights are smoothed in place.</param>
        /// <param name="smoothingConfig">Flattened smoothing config loaded from ECS.</param>
        /// <param name="cancellationToken">Cancellation token for skipping work before the pass starts.</param>
        private async UniTask ApplyFinalHeightSmoothingAsync(
            VertexGrid vertexGrid,
            HeightSmoothingConfigComponent smoothingConfig,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (!smoothingConfig.EnableHeightBlur)
                return;

            await UniTask.RunOnThreadPool(() =>
                HeightSmoothing.ApplyRadiusBlur(
                    vertexGrid,
                    smoothingConfig.HeightBlurRadius,
                    smoothingConfig.HeightBlurIterations),
                cancellationToken: cancellationToken);
        }
    }
}
