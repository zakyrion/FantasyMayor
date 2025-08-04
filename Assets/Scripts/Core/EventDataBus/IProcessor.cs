using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.EventDataBus
{
    public interface IProcessor<TData> : IProcessor
    {
        UniTask ProcessAsync(TData data, CancellationToken cancellationToken);
    }

    public interface IProcessor
    {
    }
}
