using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using Zenject;

namespace Core.DataLayer
{
    [UsedImplicitly]
    internal class DataHub : IDataHub
    {
        private readonly DiContainer _container;
        private readonly ConcurrentDictionary<Type, object> _containerCache = new();

        [Inject]
        public DataHub(DiContainer container)
        {
            _container = container;
        }

        public IDataContainer<TDataLayer> GetDataLayer<TDataLayer>() where TDataLayer : struct
        {
            var dataLayerType = typeof(TDataLayer);

            // Check cache first
            if (_containerCache.TryGetValue(dataLayerType, out var cachedContainer))
            {
                return (IDataContainer<TDataLayer>)cachedContainer;
            }

            // Try to resolve from DI container
            var containerType = typeof(IDataContainer<TDataLayer>);

            if (_container.HasBinding(containerType))
            {
                var container = _container.Resolve<IDataContainer<TDataLayer>>();
                _containerCache.TryAdd(dataLayerType, container);
                return container;
            }

            // If not bound, create a new instance on demand
            var newContainer = new DataContainer<TDataLayer>();

            // Optionally bind it to the container for future use
            _container.Bind<IDataContainer<TDataLayer>>().FromInstance(newContainer).AsSingle();

            _containerCache.TryAdd(dataLayerType, newContainer);
            return newContainer;
        }
    }
}
