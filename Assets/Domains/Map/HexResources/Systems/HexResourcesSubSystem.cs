using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Configs;
using Domains.Map.HexResources.Data;

namespace Domains.Map.HexResources.Systems
{
    internal abstract class HexResourcesSubSystem : IPrioritizedUniTaskSystem, IDisposable
    {
        private readonly EntityStorages _storages;

        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }
        public Type OrchestratorType => typeof(HexResourcesSystem);
        protected abstract HexResourceType TargetHexResourceType { get; }

        protected HexResourcesSubSystem(EntityStorages storages)
        {
            _storages = storages;
        }

        public abstract UniTask Update(CancellationToken cancellationToken);

        protected bool TryGetResourceConfig(out ResourceConfig config)
        {
            config = null;

            var resourcesConfig = _storages.Get<HexResourcesConfig>();
            foreach (var resource in resourcesConfig.Resources)
            {
                if (resource.Type != TargetHexResourceType)
                    continue;

                config = resource.Config;
                return config != null;
            }

            return false;
        }

        public virtual void Dispose()
        {
        }
    }
}
