using Modules.Hexes.DataLayer;

public class VectorFieldToMeshesCommand
{
    private readonly HexViewDataLayer _hexDataLayer;

    public VectorFieldToMeshesCommand(HexViewDataLayer hexDataLayer)
    {
        _hexDataLayer = hexDataLayer;
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