using Friflo.Engine.ECS;
using Modules.AxialSystem;
using Unity.Collections;

namespace Domains.Map.Pathfinding
{
    /// <summary>
    ///     Finds hex-grid paths between two coordinates using native Unity collections.
    /// </summary>
    public interface IHexPathfindingUtility
    {
        /// <summary>
        ///     Attempts to find a path between two hexes and returns it in a native list allocated with the requested allocator.
        /// </summary>
        /// <param name="hexArchetype">Archetype that provides the current pathfinding domain.</param>
        /// <param name="start">Start hex coordinate.</param>
        /// <param name="end">Target hex coordinate.</param>
        /// <param name="allocator">Allocator used for all native containers created during the search.</param>
        /// <param name="path">Result path when found. Caller owns disposal of the returned list.</param>
        /// <returns><c>true</c> if a path exists; otherwise <c>false</c>.</returns>
        bool TryFindPath(Archetype hexArchetype, HexCoord start, HexCoord end, Allocator allocator, out NativeList<HexCoord> path);
    }
}
