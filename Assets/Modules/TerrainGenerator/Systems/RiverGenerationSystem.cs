using System.Collections.Generic;
using DefaultEcs;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.HexesCore.Components;
using Modules.HexesCore.Utils;
using Modules.Pathfinding;
using Modules.TerrainGenerator.Components;
using Modules.TerrainGenerator.Data;
using Unity.Collections;
using UnityEngine;

namespace Modules.TerrainGenerator.Systems
{
    /// <summary>
    ///     Runs river generation when invoked by <see cref="TerrainGenerationSystem" />.
    /// </summary>
    [UsedImplicitly]
    internal sealed class RiverGenerationSystem : GenerationSystem
    {
        private const int ExecutionPriority = 200;
        private const int RiverLevel = -1;
        private const int SideCount = 6;

        private readonly EntitySet _configSet;
        private readonly EntitySet _hexSet;
        private readonly EntitySet _riverConfigSet;
        private readonly IHexPathfindingSystem _pathfindingSystem;

        public override int Priority => ExecutionPriority;

        public RiverGenerationSystem(World world, IHexPathfindingSystem pathfindingSystem)
        {
            _pathfindingSystem = pathfindingSystem;
            _configSet = world.GetEntities()
                .With<TerrainGenerationConfigComponent>()
                .AsSet();
            _riverConfigSet = world.GetEntities()
                .With<RiverConfigComponent>()
                .AsSet();
            _hexSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexLevelComponent>()
                .AsSet();
        }

        public override void Update(DefaultECSExtensions.GameState state)
        {
            if (_configSet.Count == 0)
                return;

            ref readonly var config = ref _configSet.GetEntities()[0]
                .Get<TerrainGenerationConfigComponent>();

            if (config.WaterType != WaterType.River)
                return;

            if (_riverConfigSet.Count == 0)
                return;

            ref readonly var riverConfig = ref _riverConfigSet.GetEntities()[0]
                .Get<RiverConfigComponent>();

            Generate(config.WaveCount, riverConfig.CornerOffsetTiles);
        }

        public override void Dispose()
        {
            base.Dispose();
            _configSet.Dispose();
            _riverConfigSet.Dispose();
            _hexSet.Dispose();
        }

        private void Generate(int waveCount, int cornerOffsetTiles)
        {
            var sideHexes = BuildPerimeterSideHexes(waveCount, cornerOffsetTiles);
            if (!TrySelectEndpoints(sideHexes, out var start, out var end))
                return;

            NativeList<HexCoord> path = default;
            try
            {
                if (!_pathfindingSystem.TryFindPath(start, end, Allocator.Temp, out path))
                    return;

                ApplyRiver(path);
            }
            finally
            {
                if (path.IsCreated)
                    path.Dispose();
            }
        }

        private static IReadOnlyList<List<HexCoord>> BuildPerimeterSideHexes(int waveCount, int cornerOffsetTiles)
        {
            var radius = waveCount - 1;
            var result = new List<List<HexCoord>>(SideCount);

            for (var side = 0; side < SideCount; side++)
                result.Add(new List<HexCoord>());

            if (radius < 2)
                return result;

            var clampedOffset = Mathf.Max(0, cornerOffsetTiles);
            var startStep = clampedOffset;
            var endStep = radius - clampedOffset;

            if (startStep > endStep)
                return result;

            HexCoord[] corners =
            {
                new(radius, 0),
                new(radius, -radius),
                new(0, -radius),
                new(-radius, 0),
                new(-radius, radius),
                new(0, radius)
            };

            for (var side = 0; side < SideCount; side++)
            {
                for (var step = startStep; step <= endStep; step++)
                    result[side].Add(corners[side] + HexesUtil.Neighbour(side) * step);
            }

            return result;
        }

        private static bool TrySelectEndpoints(IReadOnlyList<List<HexCoord>> sideHexes, out HexCoord start, out HexCoord end)
        {
            start = default;
            end = default;

            var validSides = new List<int>(SideCount);
            for (var side = 0; side < sideHexes.Count; side++)
            {
                if (sideHexes[side].Count > 0)
                    validSides.Add(side);
            }

            if (validSides.Count < 2)
                return false;

            var firstSide = validSides[Random.Range(0, validSides.Count)];
            var candidateSides = new List<int>(SideCount - 1);

            foreach (var side in validSides)
            {
                if (side == firstSide || CircularSideDistance(firstSide, side) < 2)
                    continue;

                candidateSides.Add(side);
            }

            if (candidateSides.Count == 0)
                return false;

            var secondSide = candidateSides[Random.Range(0, candidateSides.Count)];
            start = sideHexes[firstSide][Random.Range(0, sideHexes[firstSide].Count)];
            end = sideHexes[secondSide][Random.Range(0, sideHexes[secondSide].Count)];
            return true;
        }

        private void ApplyRiver(NativeList<HexCoord> path)
        {
            var entities = _hexSet.GetEntities();
            var entityByCoord = new Dictionary<HexCoord, Entity>(entities.Length);

            foreach (var entity in entities)
                entityByCoord[entity.Get<HexIdComponent>().Coords] = entity;

            for (var index = 0; index < path.Length; index++)
            {
                var coord = path[index];
                if (!entityByCoord.TryGetValue(coord, out var entity))
                    continue;

                entity.Set(new HexLevelComponent { Level = RiverLevel });
            }
        }

        private static int CircularSideDistance(int first, int second)
        {
            var distance = Mathf.Abs(first - second);
            return Mathf.Min(distance, SideCount - distance);
        }
    }
}
