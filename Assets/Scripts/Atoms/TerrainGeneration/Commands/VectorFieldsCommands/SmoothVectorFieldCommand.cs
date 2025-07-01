using Atoms.Hexes.DataLayer;
using Cysharp.Threading.Tasks;
using Unity.Jobs;

public class SmoothVectorFieldCommand
{
    private readonly HexViewDataLayer _hexDataLayer;

    public SmoothVectorFieldCommand(HexViewDataLayer hexDataLayer)
    {
        _hexDataLayer = hexDataLayer;
    }

    public async UniTask<bool> Execute()
    {
        var job = new SmoothVectorFieldJob
        {
            HexVectors = _hexDataLayer.HexVectors
        };

        await job.Schedule();
        return true;
    }
}