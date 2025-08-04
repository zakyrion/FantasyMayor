using Cysharp.Threading.Tasks;
using Modules.Hexes.DataLayer;
using Unity.Jobs;

public class SmoothVectorFieldCommand
{
    private readonly HexesViewDataLayer _hexesDataLayer;

    public SmoothVectorFieldCommand(HexesViewDataLayer hexesDataLayer)
    {
        _hexesDataLayer = hexesDataLayer;
    }

    public async UniTask<bool> Execute()
    {
        /*var job = new SmoothVectorFieldJob
        {
            HexVectors = _hexDataLayer.HexVectors
        };

        await job.Schedule();*/
        return true;
    }
}