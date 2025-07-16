using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Zenject;

namespace Modules.Addressable
{
    [UsedImplicitly]
    public class Addressable : IAddressable
    {
        private readonly DiContainer _container;

        public Addressable(DiContainer container)
        {
            _container = container;
            Addressables.InitializeAsync();
        }

        public async UniTask<AddressableResult<GameObject>> LoadAndInstanceAsync(string asset, CancellationToken token, Transform root = null)
        {
            if (string.IsNullOrEmpty(asset))
            {
                return AddressableResult<GameObject>.Empty(AddressableStatus.AssetIsEmpty);
            }

            var result = await LoadAsync<GameObject>(asset);
            if (token.IsCancellationRequested)
            {
                Release(result);
                return AddressableResult<GameObject>.Cancelled();
            }

            if (result.Status == Status.Failed)
            {
                return AddressableResult<GameObject>.Empty(AddressableStatus.NotFound);
            }

            var instance = _container.InstantiatePrefab(result.Value, root);

            return !instance ? AddressableResult<GameObject>.Empty(AddressableStatus.Failed) : AddressableResult<GameObject>.Success(instance, Release);
        }

        public async UniTask<AddressableResult<T>> LoadAsync<T>(string asset, CancellationToken token) where T : class
        {
            if (string.IsNullOrEmpty(asset))
            {
                return AddressableResult<T>.Empty(AddressableStatus.AssetIsEmpty);
            }

            var result = await LoadAsync<T>(asset);
            if (token.IsCancellationRequested)
            {
                Release(result);
                return AddressableResult<T>.Cancelled();
            }

            return result.Status == Status.Failed ? AddressableResult<T>.Empty(AddressableStatus.NotFound) : AddressableResult<T>.Success(result.Value, Release);
        }

        private async UniTask<Result<T>> LoadAsync<T>(string path)
        {
            var operationHandle = Addressables.LoadAssetAsync<T>(path);
            await operationHandle.Task;

            return operationHandle.Status == AsyncOperationStatus.Succeeded ? Result<T>.Success(operationHandle.Result) : Result<T>.Fail();
        }

        private void Release(object obj)
        {
            if (obj == null)
            {
                return;
            }

            Addressables.Release(obj);
        }
    }
}
