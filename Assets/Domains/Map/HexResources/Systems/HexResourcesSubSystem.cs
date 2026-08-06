using EcsExtensions;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Configs;
using Domains.Map.HexResources.Data;

namespace Domains.Map.HexResources.Systems
{
    internal abstract class HexResourcesSubSystem : EcsExtensions.ISystem<GameState>
    {
        private readonly EntityStorages _storages;

        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }
        protected abstract HexResourceType TargetHexResourceType { get; }

        protected HexResourcesSubSystem(EntityStorages storages)
        {
            _storages = storages;
        }

        public abstract void Update(GameState state);

        protected bool TryGetResourceConfig(out ResourceConfig config)
        {
            config = null;

            if (!_storages.Singletons.Has<HexResourcesConfigComponent>())
                return false;

            var resourcesConfig = _storages.Singletons.Get<HexResourcesConfigComponent>().Value;
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
