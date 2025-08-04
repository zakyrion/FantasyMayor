using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Core.Data;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.EventDataBus
{
    [UsedImplicitly]
    public class Bus<TData> : IBus<TData> where TData : struct
    {
        // Queue to preserve order of events
        private readonly ConcurrentQueue<(TData data, CancellationToken token)> _eventQueue = new();
        private readonly ConcurrentDictionary<int, IProcessor<TData>> _handlers = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1); // ensures one event processes at a time
        private bool _isProcessing;

        public UniTask PublishAsync(TData data, CancellationToken cancellationToken)
        {
            _eventQueue.Enqueue((data, cancellationToken));
            return ProcessQueueAsync();
        }

        public SubscribeResult Subscribe(IProcessor<TData> processor, int order)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            return !_handlers.TryAdd(order, processor)
                ? SubscribeResult.AlreadySubscribed
                : SubscribeResult.Success;
        }

        public UnsubscribeResult Unsubscribe(int order)
        {
            return _handlers.TryRemove(order, out _)
                ? UnsubscribeResult.Success
                : UnsubscribeResult.NotSubscribed;
        }

        private async UniTask ProcessQueueAsync()
        {
            // Ensure only one task processes the queue at a time
            await _semaphore.WaitAsync();
            try
            {
                if (_isProcessing)
                    return;

                _isProcessing = true;
            }
            finally
            {
                _semaphore.Release();
            }

            while (_eventQueue.TryDequeue(out var item))
            {
                var (data, token) = item;
                var handlers = _handlers
                    .OrderBy(x => x.Key)
                    .Select(x => x.Value.ProcessAsync(data, token));

                try
                {
                    await UniTask.WhenAll(handlers);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing {typeof(TData).Name} event: {ex}");
                }
            }

            // Reset processing flag
            await _semaphore.WaitAsync();
            try
            {
                _isProcessing = false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
