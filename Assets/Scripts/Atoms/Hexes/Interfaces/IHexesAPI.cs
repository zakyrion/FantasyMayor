using Cysharp.Threading.Tasks;
using Unity.Mathematics;

public interface IHexesAPI : IAPI
{
    UniTask<bool> CreateHex(HexViewData hexData, int detailLevel);
    void CreateAllHexes();
    
    void SetHexLevel(HexId hexId, int level);
}