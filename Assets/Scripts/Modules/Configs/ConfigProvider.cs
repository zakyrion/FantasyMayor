using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Modules.Addressable;

namespace Modules.Configs
{
    [UsedImplicitly]
    internal class ConfigProvider<T> : IConfigProvider<T> where T : class
    {
        private readonly IAddressable _addressable;

        public ConfigProvider(IAddressable addressable)
        {
            _addressable = addressable;
        }

        public UniTask<AddressableResult<T>> GetConfigAsync(CancellationToken token)
        {
            return _addressable.LoadAsync<T>(nameof(T), token);
        }
    }
}
