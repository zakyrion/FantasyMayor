using Cysharp.Threading.Tasks;
using Modules.Hexes.DataLayer;

public class SpawnHexesCommand
{
    private readonly HexViewDataLayer _hexDataLayer;
    private readonly IHexesAPI _hexesAPI;

    public SpawnHexesCommand(HexViewDataLayer hexDataLayer, IHexesAPI hexesAPI)
    {
        _hexDataLayer = hexDataLayer;
        _hexesAPI = hexesAPI;
    }

    public async UniTask<bool> Execute(int hexDetailLevel)
    {
        /*var tasks = new List<UniTask>();
        foreach (var hex in _hexDataLayer.Hexes)
        {
            tasks.Add(_hexesAPI.CreateHex(hex, hexDetailLevel));
        }

        var awaiter = UniTask.WhenAll(tasks);
        await awaiter;

        return awaiter.Status == UniTaskStatus.Succeeded;*/

        return true;
    }
}
