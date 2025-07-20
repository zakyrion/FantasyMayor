using System.Collections.Generic;
using System.Linq;
using Modules.Hexes.DataLayer;
using Unity.Mathematics;
using Zenject;

public class PathfindingSystem : IPathfindingAPI
{
    private readonly HexesViewDataLayer _hexesDataLayer;

    [Inject]
    public PathfindingSystem(HexesViewDataLayer hexesDataLayer)
    {
        _hexesDataLayer = hexesDataLayer;
    }

    List<int2> IPathfindingAPI.GetPath(int2 start, int2 end)
    {
        /*var openSet = new List<HexViewData>();
        var closedSet = new HashSet<int2>();
        var cameFrom = new Dictionary<int2, int2>();

        openSet.Add(_hexDataLayer[start]);

        var gScore = new Dictionary<int2, float> {{start, 0}};
        var fScore = new Dictionary<int2, float> {{start, HeuristicCostEstimate(start, end)}};

        while (openSet.Count > 0)
        {
            var current = openSet.Aggregate((x, y) => fScore[x.HexId] < fScore[y.HexId] ? x : y);

            if (current.HexId.Equals(end)) return ReconstructPath(cameFrom, end);

            openSet.Remove(current);
            closedSet.Add(current.HexId);

            foreach (var neighbor in current.Neighbors)
            {
                if (closedSet.Contains(neighbor.HexId)) continue;

                var tentativeGScore =
                    gScore[current.HexId] + 1; // assuming distance between any two neighbors is 1

                if (!openSet.Contains(neighbor) || tentativeGScore < gScore[neighbor.HexId])
                {
                    cameFrom[neighbor.HexId] = current.HexId;
                    gScore[neighbor.HexId] = tentativeGScore;
                    fScore[neighbor.HexId] = tentativeGScore + HeuristicCostEstimate(neighbor.HexId, end);
                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }*/

        return null;
    }

    private float HeuristicCostEstimate(int2 a, int2 b) => math.distance(a, b);

    private List<int2> ReconstructPath(Dictionary<int2, int2> cameFrom, int2 current)
    {
        if (cameFrom.ContainsKey(current))
        {
            var path = ReconstructPath(cameFrom, cameFrom[current]);
            path.Add(current);
            return path;
        }

        return new List<int2> {current};
    }
}