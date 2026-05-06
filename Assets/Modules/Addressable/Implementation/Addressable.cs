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

            var result = await LoadAsync(asset);

            if (token.IsCancellationRequested)
            {
                if (result.Status == Status.Success)
                    result.Box.Dispose();

                return Result<GameObject>.Cancelled();
            }

            if (result.Status == Status.Failed)
                return Result<GameObject>.Fail();

            var instance = _container.Instantiate(result.Box.Value, root);

            if (!instance)
            {
                result.Box.Dispose();
                return Result<GameObject>.Fail();
            }

            var releaseAction = new Action<GameObject>(go =>
            {
                if (go != null)
                    Object.Destroy(go);
                result.Box.Dispose();
            });

            return Result<GameObject>.Success(instance, releaseAction);
        }

        public async UniTask<Result<T>> LoadAndInstanceAsync<T>(string asset, CancellationToken token, Transform root = null) where T : Component
        {
            var resultGO = await LoadAndInstanceAsync(asset, token, root);

            if (resultGO.Status != Status.Success)
            {
                return Result<T>.Fail();
            }

            if (token.IsCancellationRequested)
            {
                resultGO.Box.Dispose();
                return Result<T>.Cancelled();
            }

            var component = resultGO.Box.Value.GetComponent<T>();
            if (component == null)
            {
                resultGO.Box.Dispose();
                return Result<T>.Fail();
            }

            return Result<T>.Success(component, _ =>
            {
                if (resultGO.Box.Exist)
                {
                    resultGO.Box.Dispose();
                }
            });
        }

        public async UniTask<Result<T>> LoadAsync<T>(string asset, CancellationToken token) where T : class
        {
            if (string.IsNullOrEmpty(asset) || typeof(T) == typeof(GameObject))
                return Result<T>.Fail();

            var result = await LoadAsync<T>(asset);

            if (token.IsCancellationRequested)
            {
                if (result.Status == Status.Success)
                    Release(result.Box.Value);
                return Result<T>.Cancelled();
            }

            return result.Status == Status.Failed ? Result<T>.Fail() : Result<T>.Success(result.Box.Value, Release);
        }

        private async UniTask<Result<T>> LoadAsync<T>(string path)
        {
            var operationHandle = Addressables.LoadAssetAsync<T>(path);
            try
            {
                await operationHandle.Task;

                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                    return Result<T>.Success(operationHandle.Result);

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
