using System.Threading;
using Core;

namespace Modules.Hexes.Creators
{
    internal interface IHexesCreator
    {
        Box<HexView> CreateHexAsync(CancellationToken cancellationToken);
    }
}
