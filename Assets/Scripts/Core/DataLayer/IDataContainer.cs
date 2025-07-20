using System;
using System.Threading;
using Core.Data;
using Cysharp.Threading.Tasks;

namespace Core.DataLayer
{
    public interface IDataContainer<TData> where TData : struct
    {
        // Add/Update methods
        UniTask AddOrUpdateAsync(TData data, CancellationToken cancellationToken);
        UniTask AddOrUpdateAsync(int id, TData data, CancellationToken cancellationToken);
        UniTask AddOrUpdateAsync(Guid id, TData data, CancellationToken cancellationToken);
        UniTask AddOrUpdateAsync(string id, TData data, CancellationToken cancellationToken);

        // Get methods
        UniTask<DataLayerResult<TData>> GetAsync(CancellationToken cancellationToken);
        UniTask<DataLayerResult<TData>> GetAsync(int id, CancellationToken cancellationToken);
        UniTask<DataLayerResult<TData>> GetAsync(Guid id, CancellationToken cancellationToken);
        UniTask<DataLayerResult<TData>> GetAsync(string id, CancellationToken cancellationToken);

        // Subscribe methods
        DataSubscribeResult SubscribeOnUpdate(Func<DataContext<TData>, CancellationToken, UniTask> callback, int order);
        DataSubscribeResult SubscribeOnUpdate(Func<DataContext<TData>, CancellationToken, UniTask> callback, int id, int order);
        DataSubscribeResult SubscribeOnUpdate(Func<DataContext<TData>, CancellationToken, UniTask> callback, Guid id, int order);
        DataSubscribeResult SubscribeOnUpdate(Func<DataContext<TData>, CancellationToken, UniTask> callback, string id, int order);

        // Unsubscribe
        void UnsubscribeOnUpdate(SubscriptionId id);
    }
}
