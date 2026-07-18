using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Friflo.Engine.ECS;
using Modules.Addressable.Core;
using Modules.Boot.Core;

namespace EcsExtensions
{
    public abstract class ConfigLoaderSystem : IUniTaskSystem<ConfigLoadStep>
    {
        protected readonly EntityStore World;
        private readonly IAddressable _addressable;

        protected bool IsDisposed { get; private set; }
        protected bool IsLoaded { get; private set; }

        protected ConfigLoaderSystem(IAddressable addressable, EntityStore world)
        {
            _addressable = addressable;
            World = world;
        }

        public async UniTask Update(ConfigLoadStep state, CancellationToken cancellationToken)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);

            if (IsLoaded)
                return;

            await LoadConfigsAsync(cancellationToken);
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            OnDispose();
        }

        protected abstract UniTask LoadConfigsAsync(CancellationToken cancellationToken);

        protected void DisposeBox<T>(ref Box<T> box)
        {
            if (!box.Exist)
                return;

            box.Dispose();
            box = Box<T>.Empty();
        }

        protected async UniTask<Box<T>> LoadConfigAsync<T>(string address, CancellationToken cancellationToken) where T : class
        {
            var result = await _addressable.LoadAsync<T>(address, cancellationToken);

            if (cancellationToken.IsCancellationRequested || result.Status == Status.Cancelled)
                return Box<T>.Empty();

            if (result.Status != Status.Success || !result.Box.Exist)
                throw new Exception($"Failed to load terrain config by address '{address}'.");

            return result.Box;
        }

        protected void MarkAsLoaded()
        {
            IsLoaded = true;
        }

        protected virtual void OnDispose()
        {
        }
    }
}
