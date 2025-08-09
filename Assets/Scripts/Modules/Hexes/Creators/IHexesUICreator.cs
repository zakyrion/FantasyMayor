using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Modules.Hexes.View.UI;

namespace Modules.Hexes.Creators
{
    public interface IHexesUICreator
    {
        UniTask<Box<HexesUIView>> CreateHexesUIAsync(CancellationToken cancellationToken);
    }
}
