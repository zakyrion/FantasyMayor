using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Data;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.DataLayer
{
    [UsedImplicitly]
    public class DataContainer<TData> : IDataContainer<TData> where TData : struct
    {
        private readonly ConcurrentDictionary<SubscriptionId, ISubscriptionInfo> _activeSubscriptions = new();
        private readonly SemaphoreSlim _guidLock = new(1, 1);
        private readonly ConcurrentQueue<GuidDataOperation> _guidQueue = new();
        private readonly SemaphoreSlim _intLock = new(1, 1);
        private readonly ConcurrentQueue<IntDataOperation> _intQueue = new();

        // Separate processing locks for each key type
        private readonly SemaphoreSlim _noIdLock = new(1, 1);

        // Separate queues for each key type to guarantee order
        private readonly ConcurrentQueue<DataOperation> _noIdQueue = new();

        // Separate storage for each key type
        private readonly DataStorage _storage = new();
        private readonly SemaphoreSlim _stringLock = new(1, 1);
        private readonly ConcurrentQueue<StringDataOperation> _stringQueue = new();
        private readonly SubscriberStorage _subscribers = new();
        private bool _isProcessingGuid;
        private bool _isProcessingInt;

        private bool _isProcessingNoId;
        private bool _isProcessingString;

        #region Unsubscribe Methods
        public void UnsubscribeOnUpdate(SubscriptionId id)
        {
            if (_activeSubscriptions.TryRemove(id, out var info))
            {
                info.RemoveFrom(_subscribers, id);
            }
        }
        #endregion

        #region Helper Methods
        private async UniTask NotifySubscribersAsync(IEnumerable<ISubscriber> subscribers, TData data, CancellationToken cancellationToken)
        {
            foreach (var subscriber in subscribers.OrderBy(s => s.Order))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    await subscriber.Callback(data, cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                        break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in subscriber callback: {ex}");
                }
            }
        }
        #endregion

        #region Add/Update Methods
        public UniTask AddOrUpdateAsync(TData data, CancellationToken cancellationToken)
        {
            var operation = new DataOperation(data, cancellationToken);
            _noIdQueue.Enqueue(operation);
            return ProcessNoIdQueueAsync();
        }

        public UniTask AddOrUpdateAsync(int id, TData data, CancellationToken cancellationToken)
        {
            var operation = new IntDataOperation(id, data, cancellationToken);
            _intQueue.Enqueue(operation);
            return ProcessIntQueueAsync();
        }

        public UniTask AddOrUpdateAsync(Guid id, TData data, CancellationToken cancellationToken)
        {
            var operation = new GuidDataOperation(id, data, cancellationToken);
            _guidQueue.Enqueue(operation);
            return ProcessGuidQueueAsync();
        }

        public UniTask AddOrUpdateAsync(string id, TData data, CancellationToken cancellationToken)
        {
            var operation = new StringDataOperation(id, data, cancellationToken);
            _stringQueue.Enqueue(operation);
            return ProcessStringQueueAsync();
        }
        #endregion

        #region Get Methods
        public UniTask<DataLayerResult<TData>> GetAsync(CancellationToken cancellationToken)
        {
            return UniTask.FromResult(_storage.TryGetNoIdValue(out var data)
                ? new DataLayerResult<TData>(data, true)
                : new DataLayerResult<TData>(default, false));
        }

        public UniTask<DataLayerResult<TData>> GetAsync(int id, CancellationToken cancellationToken)
        {
            return UniTask.FromResult(_storage.TryGetIntValue(id, out var data)
                ? new DataLayerResult<TData>(data, true)
                : new DataLayerResult<TData>(default, false));
        }

        public UniTask<DataLayerResult<TData>> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            return UniTask.FromResult(_storage.TryGetGuidValue(id, out var data)
                ? new DataLayerResult<TData>(data, true)
                : new DataLayerResult<TData>(default, false));
        }

        public UniTask<DataLayerResult<TData>> GetAsync(string id, CancellationToken cancellationToken)
        {
            return UniTask.FromResult(_storage.TryGetStringValue(id, out var data)
                ? new DataLayerResult<TData>(data, true)
                : new DataLayerResult<TData>(default, false));
        }
        #endregion

        #region Subscribe Methods
        public DataSubscribeResult SubscribeOnUpdate(Func<TData, CancellationToken, UniTask> callback, int order)
        {
            var subscriptionId = new SubscriptionId(Guid.NewGuid());
            var subscriber = new Subscriber(subscriptionId, callback, order);

            var result = _subscribers.AddNoIdSubscriber(subscriber);

            if (result == SubscribeResult.Success)
            {
                _activeSubscriptions.TryAdd(subscriptionId, new NoIdSubscriptionInfo());
            }

            return new DataSubscribeResult(result, subscriptionId);
        }

        public UniTask<DataSubscribeResult> SubscribeOnUpdateAsync(Func<TData, CancellationToken, UniTask> callback, int id, int order, CancellationToken cancellationToken)
        {
            var subscriptionId = new SubscriptionId(Guid.NewGuid());
            var subscriber = new Subscriber(subscriptionId, callback, order);

            var result = _subscribers.AddIntSubscriber(id, subscriber);

            if (result == SubscribeResult.Success)
            {
                _activeSubscriptions.TryAdd(subscriptionId, new IntSubscriptionInfo(id));
            }

            return UniTask.FromResult(new DataSubscribeResult(result, subscriptionId));
        }

        public UniTask<DataSubscribeResult> SubscribeOnUpdateAsync(Func<TData, CancellationToken, UniTask> callback, Guid id, int order, CancellationToken cancellationToken)
        {
            var subscriptionId = new SubscriptionId(Guid.NewGuid());
            var subscriber = new Subscriber(subscriptionId, callback, order);

            var result = _subscribers.AddGuidSubscriber(id, subscriber);

            if (result == SubscribeResult.Success)
            {
                _activeSubscriptions.TryAdd(subscriptionId, new GuidSubscriptionInfo(id));
            }

            return UniTask.FromResult(new DataSubscribeResult(result, subscriptionId));
        }

        public UniTask<DataSubscribeResult> SubscribeOnUpdateAsync(Func<TData, CancellationToken, UniTask> callback, string id, int order, CancellationToken cancellationToken)
        {
            var subscriptionId = new SubscriptionId(Guid.NewGuid());
            var subscriber = new Subscriber(subscriptionId, callback, order);

            var result = _subscribers.AddStringSubscriber(id, subscriber);

            if (result == SubscribeResult.Success)
            {
                _activeSubscriptions.TryAdd(subscriptionId, new StringSubscriptionInfo(id));
            }

            return UniTask.FromResult(new DataSubscribeResult(result, subscriptionId));
        }
        #endregion

        #region Queue Processing
        private async UniTask ProcessNoIdQueueAsync()
        {
            await _noIdLock.WaitAsync();
            try
            {
                if (_isProcessingNoId)
                    return;
                _isProcessingNoId = true;
            }
            finally
            {
                _noIdLock.Release();
            }

            while (_noIdQueue.TryDequeue(out var operation))
            {
                try
                {
                    if (operation.CancellationToken.IsCancellationRequested)
                        continue;

                    _storage.SetNoIdValue(operation.Data);
                    var subscribers = _subscribers.GetNoIdSubscribers();
                    if (subscribers != null)
                    {
                        await NotifySubscribersAsync(subscribers, operation.Data, operation.CancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing no-id operation: {ex}");
                }
            }

            await _noIdLock.WaitAsync();
            try
            {
                _isProcessingNoId = false;
            }
            finally
            {
                _noIdLock.Release();
            }
        }

        private async UniTask ProcessIntQueueAsync()
        {
            await _intLock.WaitAsync();
            try
            {
                if (_isProcessingInt)
                    return;
                _isProcessingInt = true;
            }
            finally
            {
                _intLock.Release();
            }

            while (_intQueue.TryDequeue(out var operation))
            {
                try
                {
                    if (operation.CancellationToken.IsCancellationRequested)
                        continue;

                    _storage.SetIntValue(operation.Id, operation.Data);
                    var subscribers = _subscribers.GetIntSubscribers(operation.Id);
                    if (subscribers != null)
                    {
                        await NotifySubscribersAsync(subscribers, operation.Data, operation.CancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing int operation: {ex}");
                }
            }

            await _intLock.WaitAsync();
            try
            {
                _isProcessingInt = false;
            }
            finally
            {
                _intLock.Release();
            }
        }

        private async UniTask ProcessGuidQueueAsync()
        {
            await _guidLock.WaitAsync();
            try
            {
                if (_isProcessingGuid)
                    return;
                _isProcessingGuid = true;
            }
            finally
            {
                _guidLock.Release();
            }

            while (_guidQueue.TryDequeue(out var operation))
            {
                try
                {
                    if (operation.CancellationToken.IsCancellationRequested)
                        continue;

                    _storage.SetGuidValue(operation.Id, operation.Data);
                    var subscribers = _subscribers.GetGuidSubscribers(operation.Id);
                    if (subscribers != null)
                    {
                        await NotifySubscribersAsync(subscribers, operation.Data, operation.CancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing guid operation: {ex}");
                }
            }

            await _guidLock.WaitAsync();
            try
            {
                _isProcessingGuid = false;
            }
            finally
            {
                _guidLock.Release();
            }
        }

        private async UniTask ProcessStringQueueAsync()
        {
            await _stringLock.WaitAsync();
            try
            {
                if (_isProcessingString)
                    return;
                _isProcessingString = true;
            }
            finally
            {
                _stringLock.Release();
            }

            while (_stringQueue.TryDequeue(out var operation))
            {
                try
                {
                    if (operation.CancellationToken.IsCancellationRequested)
                        continue;

                    _storage.SetStringValue(operation.Id, operation.Data);
                    var subscribers = _subscribers.GetStringSubscribers(operation.Id);
                    if (subscribers != null)
                    {
                        await NotifySubscribersAsync(subscribers, operation.Data, operation.CancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing string operation: {ex}");
                }
            }

            await _stringLock.WaitAsync();
            try
            {
                _isProcessingString = false;
            }
            finally
            {
                _stringLock.Release();
            }
        }
        #endregion

        #region Storage Classes
        private class DataStorage
        {
            private readonly ConcurrentDictionary<Guid, TData> _guidData = new();
            private readonly ConcurrentDictionary<int, TData> _intData = new();
            private readonly ConcurrentDictionary<string, TData> _stringData = new();
            private TData? _noIdData;

            public void SetGuidValue(Guid id, TData data)
            {
                _guidData.AddOrUpdate(id, data, (_, _) => data);
            }

            public void SetIntValue(int id, TData data)
            {
                _intData.AddOrUpdate(id, data, (_, _) => data);
            }

            public void SetNoIdValue(TData data)
            {
                _noIdData = data;
            }

            public void SetStringValue(string id, TData data)
            {
                _stringData.AddOrUpdate(id, data, (_, _) => data);
            }

            public bool TryGetGuidValue(Guid id, out TData data)
            {
                return _guidData.TryGetValue(id, out data);
            }

            public bool TryGetIntValue(int id, out TData data)
            {
                return _intData.TryGetValue(id, out data);
            }

            public bool TryGetNoIdValue(out TData data)
            {
                if (_noIdData.HasValue)
                {
                    data = _noIdData.Value;
                    return true;
                }
                data = default;
                return false;
            }

            public bool TryGetStringValue(string id, out TData data)
            {
                return _stringData.TryGetValue(id, out data);
            }
        }

        private class SubscriberStorage
        {
            private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<SubscriptionId, Subscriber>> _guidSubscribers = new();
            private readonly ConcurrentDictionary<int, ConcurrentDictionary<SubscriptionId, Subscriber>> _intSubscribers = new();
            private readonly ConcurrentDictionary<SubscriptionId, Subscriber> _noIdSubscribers = new();
            private readonly ConcurrentDictionary<string, ConcurrentDictionary<SubscriptionId, Subscriber>> _stringSubscribers = new();

            public SubscribeResult AddGuidSubscriber(Guid id, Subscriber subscriber)
            {
                var dict = _guidSubscribers.GetOrAdd(id, _ => new ConcurrentDictionary<SubscriptionId, Subscriber>());
                return dict.TryAdd(subscriber.Id, subscriber) ? SubscribeResult.Success : SubscribeResult.AlreadySubscribed;
            }

            public SubscribeResult AddIntSubscriber(int id, Subscriber subscriber)
            {
                var dict = _intSubscribers.GetOrAdd(id, _ => new ConcurrentDictionary<SubscriptionId, Subscriber>());
                return dict.TryAdd(subscriber.Id, subscriber) ? SubscribeResult.Success : SubscribeResult.AlreadySubscribed;
            }

            public SubscribeResult AddNoIdSubscriber(Subscriber subscriber)
            {
                return _noIdSubscribers.TryAdd(subscriber.Id, subscriber) ? SubscribeResult.Success : SubscribeResult.AlreadySubscribed;
            }

            public SubscribeResult AddStringSubscriber(string id, Subscriber subscriber)
            {
                var dict = _stringSubscribers.GetOrAdd(id, _ => new ConcurrentDictionary<SubscriptionId, Subscriber>());
                return dict.TryAdd(subscriber.Id, subscriber) ? SubscribeResult.Success : SubscribeResult.AlreadySubscribed;
            }

            public IEnumerable<ISubscriber> GetGuidSubscribers(Guid id)
            {
                return _guidSubscribers.TryGetValue(id, out var dict) ? dict.Values : null;
            }

            public IEnumerable<ISubscriber> GetIntSubscribers(int id)
            {
                return _intSubscribers.TryGetValue(id, out var dict) ? dict.Values : null;
            }

            public IEnumerable<ISubscriber> GetNoIdSubscribers()
            {
                return _noIdSubscribers.Values;
            }

            public IEnumerable<ISubscriber> GetStringSubscribers(string id)
            {
                return _stringSubscribers.TryGetValue(id, out var dict) ? dict.Values : null;
            }

            public bool RemoveGuidSubscriber(Guid id, SubscriptionId subscriptionId)
            {
                if (_guidSubscribers.TryGetValue(id, out var dict))
                {
                    var result = dict.TryRemove(subscriptionId, out _);
                    if (dict.IsEmpty)
                    {
                        _guidSubscribers.TryRemove(id, out _);
                    }
                    return result;
                }
                return false;
            }

            public bool RemoveIntSubscriber(int id, SubscriptionId subscriptionId)
            {
                if (_intSubscribers.TryGetValue(id, out var dict))
                {
                    var result = dict.TryRemove(subscriptionId, out _);
                    if (dict.IsEmpty)
                    {
                        _intSubscribers.TryRemove(id, out _);
                    }
                    return result;
                }
                return false;
            }

            public bool RemoveNoIdSubscriber(SubscriptionId subscriptionId)
            {
                return _noIdSubscribers.TryRemove(subscriptionId, out _);
            }

            public bool RemoveStringSubscriber(string id, SubscriptionId subscriptionId)
            {
                if (_stringSubscribers.TryGetValue(id, out var dict))
                {
                    var result = dict.TryRemove(subscriptionId, out _);
                    if (dict.IsEmpty)
                    {
                        _stringSubscribers.TryRemove(id, out _);
                    }
                    return result;
                }
                return false;
            }
        }
        #endregion

        #region Helper Types
        private class DataOperation
        {
            public CancellationToken CancellationToken { get; }
            public TData Data { get; }

            public DataOperation(TData data, CancellationToken cancellationToken)
            {
                Data = data;
                CancellationToken = cancellationToken;
            }
        }

        private class IntDataOperation
        {
            public CancellationToken CancellationToken { get; }
            public TData Data { get; }
            public int Id { get; }

            public IntDataOperation(int id, TData data, CancellationToken cancellationToken)
            {
                Id = id;
                Data = data;
                CancellationToken = cancellationToken;
            }
        }

        private class GuidDataOperation
        {
            public CancellationToken CancellationToken { get; }
            public TData Data { get; }
            public Guid Id { get; }

            public GuidDataOperation(Guid id, TData data, CancellationToken cancellationToken)
            {
                Id = id;
                Data = data;
                CancellationToken = cancellationToken;
            }
        }

        private class StringDataOperation
        {
            public CancellationToken CancellationToken { get; }
            public TData Data { get; }
            public string Id { get; }

            public StringDataOperation(string id, TData data, CancellationToken cancellationToken)
            {
                Id = id;
                Data = data;
                CancellationToken = cancellationToken;
            }
        }

        private interface ISubscriber
        {
            Func<TData, CancellationToken, UniTask> Callback { get; }
            SubscriptionId Id { get; }
            int Order { get; }
        }

        private class Subscriber : ISubscriber
        {
            public Func<TData, CancellationToken, UniTask> Callback { get; }
            public SubscriptionId Id { get; }
            public int Order { get; }

            public Subscriber(SubscriptionId id, Func<TData, CancellationToken, UniTask> callback, int order)
            {
                Id = id;
                Callback = callback;
                Order = order;
            }
        }

        private interface ISubscriptionInfo
        {
            void RemoveFrom(SubscriberStorage storage, SubscriptionId subscriptionId);
        }

        private class NoIdSubscriptionInfo : ISubscriptionInfo
        {
            public void RemoveFrom(SubscriberStorage storage, SubscriptionId subscriptionId)
            {
                storage.RemoveNoIdSubscriber(subscriptionId);
            }
        }

        private class IntSubscriptionInfo : ISubscriptionInfo
        {
            private readonly int _id;

            public IntSubscriptionInfo(int id)
            {
                _id = id;
            }

            public void RemoveFrom(SubscriberStorage storage, SubscriptionId subscriptionId)
            {
                storage.RemoveIntSubscriber(_id, subscriptionId);
            }
        }

        private class GuidSubscriptionInfo : ISubscriptionInfo
        {
            private readonly Guid _id;

            public GuidSubscriptionInfo(Guid id)
            {
                _id = id;
            }

            public void RemoveFrom(SubscriberStorage storage, SubscriptionId subscriptionId)
            {
                storage.RemoveGuidSubscriber(_id, subscriptionId);
            }
        }

        private class StringSubscriptionInfo : ISubscriptionInfo
        {
            private readonly string _id;

            public StringSubscriptionInfo(string id)
            {
                _id = id;
            }

            public void RemoveFrom(SubscriberStorage storage, SubscriptionId subscriptionId)
            {
                storage.RemoveStringSubscriber(_id, subscriptionId);
            }
        }
        #endregion
    }
}
