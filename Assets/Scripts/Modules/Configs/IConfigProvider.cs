using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Addressable;

namespace Modules.Configs
{
    internal interface IConfigProvider<T> where T : class
    {
        UniTask<AddressableResult<T>> GetConfigAsync(CancellationToken token);
    }
}
