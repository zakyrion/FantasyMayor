using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.Boot.Core;
using UnityEngine;

namespace EcsExtensions
{
    // One instance per config type, registered in an installer:
    // builder.RegisterAppStateSystem<ConfigLoaderSystem<X>>(Lifetime.Singleton, AppState.ConfigLoading).WithParameter("address", ConfigAddresses.X).
    // The loaded asset is never released — configs live for the whole session (see EntityStorages).
    [UsedImplicitly]
    public sealed class ConfigLoaderSystem<T> : IUniTaskSystem where T : ScriptableObject
    {
        private readonly string _address;
        private readonly IAddressable _addressable;
        private readonly EntityStorages _storages;

        public ConfigLoaderSystem(AppState appState, string address, IAddressable addressable, EntityStorages storages)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentException($"Address for {typeof(T).Name} is empty.", nameof(address));

            AppState = appState;
            _address = address;
            _addressable = addressable;
            _storages = storages;
        }

        public AppState AppState { get; }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            var result = await _addressable.LoadAsync<T>(_address, cancellationToken);
            if (result.Status != Status.Success)
                throw new InvalidOperationException($"Failed to load {typeof(T).Name} by address '{_address}'.");

            var config = result.Box.Value;
            if (config is IValidatableConfig validatable)
                validatable.Validate();

            _storages.Add(config);
        }

        public void Dispose()
        {
        }
    }
}
