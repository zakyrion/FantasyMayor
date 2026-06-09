using DefaultEcs;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.HexCore.Components;
using Modules.HexesCore.Utils;
using Unity.Collections;
using Unity.Mathematics;

namespace Modules.Pathfinding
{
    /// <summary>
    ///     Stateless DoD utility that performs BFS over a caller-provided set of hex entities.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexPathfindingUtility : IHexPathfindingUtility
    {
        /// <inheritdoc />
        public bool TryFindPath(EntitySet hexSet, HexCoord start, HexCoord end, Allocator allocator, out NativeList<HexCoord> path)
        {
            path = default;

            var capacity = math.max(1, hexSet.Count);
            var domain = new NativeParallelHashSet<int2>(capacity, allocator);
            var frontier = new NativeQueue<int2>(allocator);
            var visited = new NativeParallelHashSet<int2>(capacity, allocator);
            var cameFrom = new NativeParallelHashMap<int2, int2>(capacity, allocator);

            try
            {
                BuildDomain(hexSet, ref domain);

                if (!domain.Contains(start.Value) || !domain.Contains(end.Value))
                    return false;

                frontier.Enqueue(start.Value);
                visited.Add(start.Value);

                while (frontier.Count > 0)
                {
                    var current = frontier.Dequeue();
                    if (current.Equals(end.Value))
                    {
                        path = ReconstructPath(start.Value, end.Value, cameFrom, allocator);
                        return true;
                    }

                    for (var neighbourIndex = 0; neighbourIndex < HexesUtil.Count; neighbourIndex++)
                    {
                        var neighbour = current + HexesUtil.Neighbour(neighbourIndex);
                        if (!domain.Contains(neighbour) || !visited.Add(neighbour))
                            continue;

                        cameFrom.TryAdd(neighbour, current);
                        frontier.Enqueue(neighbour);
                    }
                }

                return false;
            }
            finally
            {
                domain.Dispose();
                frontier.Dispose();
                visited.Dispose();
                cameFrom.Dispose();
            }
        }

        /// <summary>
        ///     Copies all currently existing hex coordinates from the provided ECS entity set into the native domain set.
        /// </summary>
        /// <param name="hexSet">Entity set used as the pathfinding domain source.</param>
        /// <param name="domain">Target native set to populate.</param>
        private void BuildDomain(EntitySet hexSet, ref NativeParallelHashSet<int2> domain)
        {
            var entities = hexSet.GetEntities();

            foreach (var entity in entities)
                domain.Add(entity.Get<HexIdComponent>().Coords.Value);
        }

        /// <summary>
        ///     Reconstructs the BFS path from the predecessor map into a native list owned by the caller.
        /// </summary>
        /// <param name="start">Start coordinate.</param>
        /// <param name="end">End coordinate.</param>
        /// <param name="cameFrom">Predecessor map produced by BFS.</param>
        /// <param name="allocator">Allocator for the output path.</param>
        /// <returns>Reconstructed path from start to end.</returns>
        private NativeList<HexCoord> ReconstructPath(int2 start, int2 end, NativeParallelHashMap<int2, int2> cameFrom, Allocator allocator)
        {
            var reversePath = new NativeList<int2>(allocator);
            try
            {
                reversePath.Add(end);
                var current = end;

                while (!current.Equals(start))
                {
                    if (!cameFrom.TryGetValue(current, out current))
                        break;

                    reversePath.Add(current);
                }

                var path = new NativeList<HexCoord>(reversePath.Length, allocator);
                for (var index = reversePath.Length - 1; index >= 0; index--)
                    path.Add(new HexCoord(reversePath[index]));

                return path;
            }
            finally
            {
                reversePath.Dispose();
            }
        }
    }
}
