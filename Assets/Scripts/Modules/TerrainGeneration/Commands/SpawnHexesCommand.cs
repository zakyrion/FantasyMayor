using Cysharp.Threading.Tasks;
using Modules.Hexes.DataLayer;

public class SpawnHexesCommand
{
    private readonly HexesViewDataLayer _hexesDataLayer;
    private readonly IHexesAPI _hexesAPI;

    public SpawnHexesCommand(HexesViewDataLayer hexesDataLayer, IHexesAPI hexesAPI)
    {
        _hexesDataLayer = hexesDataLayer;
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
