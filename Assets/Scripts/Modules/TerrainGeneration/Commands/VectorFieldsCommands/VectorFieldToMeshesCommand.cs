using Modules.Hexes.DataLayer;

public class VectorFieldToMeshesCommand
{
    private readonly HexesViewDataLayer _hexesDataLayer;

    public VectorFieldToMeshesCommand(HexesViewDataLayer hexesDataLayer)
    {
        _hexesDataLayer = hexesDataLayer;
    }

    public void Execute()
    {
        /*var hexVectors = _hexDataLayer.HexVectors;

        foreach (var hexData in _hexDataLayer.Hexes)
        {
            var vertices = hexData.Vertices;
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var gridPosition = HexVectorUtil.CalculateGridPosition(vertex);
                vertices[i] = hexVectors[gridPosition].WorldPosition;
            }

            hexData.SetVertices(vertices);
        }*/
    }
}