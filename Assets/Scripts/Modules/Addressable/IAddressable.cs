using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Modules.Addressable
{
    public interface IAddressable
    {
        public UniTask<AddressableResult<GameObject>> LoadAndInstanceAsync(string asset, CancellationToken token, Transform root = null);
        public UniTask<AddressableResult<T>> LoadAsync<T>(string asset, CancellationToken token) where T : class;
    }
}
