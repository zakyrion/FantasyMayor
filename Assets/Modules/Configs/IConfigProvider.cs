using System.Threading;
using Core;
using Cysharp.Threading.Tasks;

namespace Modules.Configs
{
    internal interface IConfigProvider<T> where T : class
    {
        UniTask<Result<T>> GetConfigAsync(CancellationToken token);
    }
}
