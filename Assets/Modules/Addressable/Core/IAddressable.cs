using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Modules.Addressable.Core
{
    public interface IAddressable
    {
        public UniTask<Result<GameObject>> LoadAndInstanceAsync(string asset, CancellationToken token, Transform root = null);
        public UniTask<Result<T>> LoadAndInstanceAsync<T>(string asset, CancellationToken token, Transform root = null) where T : Component;
        public UniTask<Result<T>> LoadAsync<T>(string asset, CancellationToken token) where T : class;
    }
}
