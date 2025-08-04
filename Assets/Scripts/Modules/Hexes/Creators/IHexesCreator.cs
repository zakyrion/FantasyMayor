using System.Threading;
using Core;
using Cysharp.Threading.Tasks;

namespace Modules.Hexes.Creators
{
    internal interface IHexesCreator
    {
        UniTask<Box<HexView>> CreateHexAsync(CancellationToken cancellationToken);
    }
}
