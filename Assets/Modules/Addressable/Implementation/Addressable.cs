using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Modules.Addressable.Implementation
{
    // Each load keeps what it loaded in a local Box until it is handed to the caller (the local becomes Empty);
    // the finally disposes that local, so cancel, failure and exception all release what was not handed over.
    [UsedImplicitly]
    public class Addressable : IAddressable
    {
        private readonly IObjectResolver _container;

        public Addressable(IObjectResolver container)
        {
            _container = container;
            Addressables.InitializeAsync();
        }

        public async UniTask<Result<GameObject>> LoadAndInstanceAsync(string asset, CancellationToken token, Transform root = null)
        {
            if (string.IsNullOrEmpty(asset))
                return Result<GameObject>.Fail();

            var prefab = (await LoadAsync(asset)).Box;
            try
            {
                token.ThrowIfCancellationRequested();

                if (!prefab.Exist)
                    return Result<GameObject>.Fail();

                var instance = _container.Instantiate(prefab.Value, root);
                if (!instance)
                    return Result<GameObject>.Fail();

                var ownedPrefab = prefab;
                prefab = Box<GameObject>.Empty();

                return Result<GameObject>.Success(instance, go =>
                {
                    if (go != null)
                        Object.Destroy(go);
                    ownedPrefab.Dispose();
                });
            }
            finally
            {
                prefab.Dispose();
            }
        }

        public async UniTask<Result<T>> LoadAndInstanceAsync<T>(string asset, CancellationToken token, Transform root = null) where T : Component
        {
            var instance = (await LoadAndInstanceAsync(asset, token, root)).Box;
            try
            {
                token.ThrowIfCancellationRequested();

                if (!instance.Exist)
                    return Result<T>.Fail();

                var component = instance.Value.GetComponent<T>();
                if (component == null)
                    return Result<T>.Fail();

                var ownedInstance = instance;
                instance = Box<GameObject>.Empty();

                return Result<T>.Success(component, _ => ownedInstance.Dispose());
            }
            finally
            {
                instance.Dispose();
            }
        }

        public async UniTask<Result<T>> LoadAsync<T>(string asset, CancellationToken token) where T : class
        {
            if (string.IsNullOrEmpty(asset) || typeof(T) == typeof(GameObject))
                return Result<T>.Fail();

            var result = await LoadAsync<T>(asset);
            var loaded = result.Box;
            try
            {
                token.ThrowIfCancellationRequested();

                loaded = Box<T>.Empty();
                return result;
            }
            finally
            {
                loaded.Dispose();
            }
        }

        private async UniTask<Result<T>> LoadAsync<T>(string path) where T : class
        {
            var operationHandle = Addressables.LoadAssetAsync<T>(path);
            try
            {
                await operationHandle.Task;

                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                    return Result<T>.Success(operationHandle.Result, Release);

                Addressables.Release(operationHandle);
                return Result<T>.Fail();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Addressables.Release(operationHandle);
                return Result<T>.Fail();
            }
        }

        private async UniTask<Result<GameObject>> LoadAsync(string path)
        {
            var operationHandle = Addressables.LoadAssetAsync<GameObject>(path);
            try
            {
                await operationHandle.Task;

                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                    return Result<GameObject>.Success(operationHandle.Result, Release);

                Addressables.Release(operationHandle);
                return Result<GameObject>.Fail();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Addressables.Release(operationHandle);
                return Result<GameObject>.Fail();
            }
        }

        private void Release(object obj)
        {
            if (obj == null)
                return;

            Addressables.Release(obj);
        }
    }
}
