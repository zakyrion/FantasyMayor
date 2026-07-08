using System;
using DefaultEcs;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Utils;
using Domains.Map.Pathfinding;
using Domains.Map.Generation.Components;
using Domains.Map.Generation.Data;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using DefaultECSExtensions;

namespace Domains.Map.Generation.Systems
{
    /// <summary>
    ///     Runs river generation when invoked by <see cref="GenerationSystem" />.
    /// </summary>
    [UsedImplicitly]
    internal sealed class RiverGenerationSubSystem : GenerationSubSystem
    {
        private const int RiverLevel = -1;
        private const int SideCount = 6;

        /// <summary>Range of a single perimeter side inside the flattened side-hex buffer.</summary>
        private readonly struct SideRange
        {
            public readonly int Start;
            public readonly int Count;

            public SideRange(int start, int count)
            {
                Start = start;
                Count = count;
            }
        }

        private readonly World _world;
        private readonly EntitySet _hexSet;
        private readonly IHexPathfindingUtility _pathfindingUtility;

        public override int Priority => SystemPriorities.SubSystems.Generation.River;

        public RiverGenerationSubSystem(World world, IHexPathfindingUtility pathfindingUtility)
        {
            _pathfindingUtility = pathfindingUtility;
            _world = world;
            _hexSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexLevelComponent>()
                .AsSet();
        }

        public override void Update(DefaultECSExtensions.GameState state)
        {
            if (!_world.Has<TerrainGenerationConfigComponent>())
                return;

            ref readonly var config = ref _world.Get<TerrainGenerationConfigComponent>();

            if (config.WaterType != WaterType.River)
                return;

            if (!_world.Has<RiverConfigComponent>())
                return;

            ref readonly var riverConfig = ref _world.Get<RiverConfigComponent>();

            Generate(config.WaveCount, riverConfig.CornerOffsetTiles);
        }

        public override void Dispose()
        {
            base.Dispose();
            _hexSet.Dispose();
        }

        private void Generate(int waveCount, int cornerOffsetTiles)
        {
            var sideHexes = new NativeList<HexCoord>(Allocator.Temp);
            var sideRanges = new NativeArray<SideRange>(SideCount, Allocator.Temp);
            try
            {
                BuildPerimeterSideHexes(waveCount, cornerOffsetTiles, sideHexes, sideRanges);
                if (!TrySelectEndpoints(sideHexes, sideRanges, out var start, out var end))
                    return;

                NativeList<HexCoord> path = default;
                try
                {
                    if (!_pathfindingUtility.TryFindPath(_hexSet, start, end, Allocator.Temp, out path))
                        return;

                    ApplyRiver(path);
                }
                finally
                {
                    if (path.IsCreated)
                        path.Dispose();
                }
            }
            finally
            {
                sideHexes.Dispose();
                if (sideRanges.IsCreated)
                    sideRanges.Dispose();
            }
        }

        /// <summary>
        ///     Fills <paramref name="sideHexes" /> with the perimeter hexes of all six sides, concatenated,
        ///     and records each side's range in <paramref name="sideRanges" />. Empty ranges when the radius
        ///     is too small or fully consumed by the corner offset.
        /// </summary>
        private void BuildPerimeterSideHexes(
            int waveCount,
            int cornerOffsetTiles,
            NativeList<HexCoord> sideHexes,
            NativeArray<SideRange> sideRanges)
        {
            for (var side = 0; side < SideCount; side++)
                sideRanges[side] = new SideRange(0, 0);

            var radius = waveCount - 1;
            if (radius < 2)
                return;

            var clampedOffset = Mathf.Max(0, cornerOffsetTiles);
            var startStep = clampedOffset;
            var endStep = radius - clampedOffset;

            if (startStep > endStep)
                return;

            Span<HexCoord> corners = stackalloc HexCoord[SideCount];
            corners[0] = new HexCoord(radius, 0);
            corners[1] = new HexCoord(radius, -radius);
            corners[2] = new HexCoord(0, -radius);
            corners[3] = new HexCoord(-radius, 0);
            corners[4] = new HexCoord(-radius, radius);
            corners[5] = new HexCoord(0, radius);

            for (var side = 0; side < SideCount; side++)
            {
                var rangeStart = sideHexes.Length;
                for (var step = startStep; step <= endStep; step++)
                    sideHexes.Add(corners[side] + HexesUtil.Neighbour(side) * step);

                sideRanges[side] = new SideRange(rangeStart, sideHexes.Length - rangeStart);
            }
        }

        private bool TrySelectEndpoints(
            NativeList<HexCoord> sideHexes,
            NativeArray<SideRange> sideRanges,
            out HexCoord start,
            out HexCoord end)
        {
            start = default;
            end = default;

            var validSides = new NativeList<int>(SideCount, Allocator.Temp);
            var candidateSides = new NativeList<int>(SideCount, Allocator.Temp);
            try
            {
                for (var side = 0; side < sideRanges.Length; side++)
                    if (sideRanges[side].Count > 0)
                        validSides.Add(side);

                if (validSides.Length < 2)
                    return false;

                var firstSide = validSides[Random.Range(0, validSides.Length)];

                for (var i = 0; i < validSides.Length; i++)
                {
                    var side = validSides[i];
                    if (side == firstSide || CircularSideDistance(firstSide, side) < 2)
                        continue;

                    candidateSides.Add(side);
                }

                if (candidateSides.Length == 0)
                    return false;

                var secondSide = candidateSides[Random.Range(0, candidateSides.Length)];
                var firstRange = sideRanges[firstSide];
                var secondRange = sideRanges[secondSide];

                start = sideHexes[firstRange.Start + Random.Range(0, firstRange.Count)];
                end = sideHexes[secondRange.Start + Random.Range(0, secondRange.Count)];
                return true;
            }
            finally
            {
                validSides.Dispose();
                candidateSides.Dispose();
            }
        }

        private void ApplyRiver(NativeList<HexCoord> path)
        {
            var entities = _hexSet.GetEntities();
            var entityByCoord = new NativeParallelHashMap<HexCoord, Entity>(entities.Length, Allocator.Temp);
            try
            {
                foreach (ref readonly var entity in entities)
                    entityByCoord[entity.Get<HexIdComponent>().Coords] = entity;

                for (var index = 0; index < path.Length; index++)
                {
                    var coord = path[index];
                    if (!entityByCoord.TryGetValue(coord, out var entity))
                        continue;

                    entity.Set(new HexLevelComponent { Level = RiverLevel });
                }
            }
            finally
            {
                entityByCoord.Dispose();
            }
        }

        private int CircularSideDistance(int first, int second)
        {
            var distance = Mathf.Abs(first - second);
            return Mathf.Min(distance, SideCount - distance);
        }
    }
}
