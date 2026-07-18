using Friflo.Engine.ECS;
using EcsExtensions;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Configs;
using Domains.Map.HexResources.Data;

namespace Domains.Map.HexResources.Systems
{
    internal abstract class HexResourcesSubSystem : EcsExtensions.ISystem<GameState>
    {
        private readonly EntityStore _world;

        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }
        protected abstract HexResourceType TargetHexResourceType { get; }

        protected HexResourcesSubSystem(EntityStore world)
        {
            _world = world;
        }

        public abstract void Update(GameState state);

        protected bool TryGetResourceConfig(out ResourceConfig config)
        {
            config = null;

            if (!_world.HasWorldComponent<HexResourcesConfigComponent>())
                return false;

            var resourcesConfig = _world.GetWorldComponent<HexResourcesConfigComponent>().Value;
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
