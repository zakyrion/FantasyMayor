using System.Threading;
using Core.Data;
using Cysharp.Threading.Tasks;

namespace Core.EventDataBus
{
    public interface IBus<TData> where TData : struct
    {
        UniTask PublishAsync(TData data, CancellationToken cancellationToken);
        SubscribeResult Subscribe(IProcessor<TData> processor, int order);
        UnsubscribeResult Unsubscribe(int order);
    }
}
