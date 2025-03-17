using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

public class SpawnHexesByWavesCommand
{
    private readonly HexViewDataLayer _hexDataLayer;

    public SpawnHexesByWavesCommand(HexViewDataLayer hexDataLayer)
    {
        _hexDataLayer = hexDataLayer;
    }

    public void Execute(int waves, float size)
    {
        var spawnPositions = new NativeList<int2>(Allocator.Temp);
        var existPositions = new NativeList<int2>(Allocator.Temp);

        spawnPositions.Add(new int2(0, 0));

        for (var i = 0; i < waves; i++)
        {
            var nextIterationSpawnPositions = new NativeList<int2>(Allocator.Temp);

            foreach (var position in spawnPositions)
            {
                if (existPositions.Contains(position)) continue;

                AddHex(position);
                existPositions.Add(position);

                for (var j = 0; j < 6; j++)
                {
                    var neighborPosition = HexUtil.Neighbour(j, position);
                    if (!existPositions.Contains(neighborPosition)) nextIterationSpawnPositions.Add(neighborPosition);
                }
            }

            spawnPositions.Dispose();
            spawnPositions = nextIterationSpawnPositions;
        }

        spawnPositions.Dispose();
        existPositions.Dispose();

        void AddHex(int2 position)
        {
            var hex = new HexViewData(0, position, size);
            _hexDataLayer.AddHex(hex);
        }

        JointNeighbours();
    }

    private void JointNeighbours()
    {
        var dictionary = _hexDataLayer.Hexes.ToDictionary(hex => hex.HexId);

        foreach (var hex in _hexDataLayer.Hexes)
        {
            foreach (var neighbour in HexUtil.Neighbours(hex.HexId.Coords))
            {
                if (dictionary.TryGetValue(neighbour, out var data))
                    hex.AddNeighbor(data);
            }
        }
    }
}