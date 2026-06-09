using DefaultEcs;
using DefaultEcs.System;
using DefaultECSExtensions;
using Modules.HexResources.Components;
using Modules.HexResources.Configs;
using Modules.HexResources.Data;

namespace Modules.HexResources.Systems
{
    internal abstract class HexResourcesSubSystem : ISystem<GameState>
    {
        private readonly World _world;

        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }
        protected abstract ResourceType TargetResourceType { get; }

        protected HexResourcesSubSystem(World world)
        {
            _world = world;
        }

        public abstract void Update(GameState state);

        protected bool TryGetResourceConfig(out ResourceConfig config)
        {
            config = null;

            if (!_world.Has<HexResourcesConfigComponent>())
                return false;

            var resourcesConfig = _world.Get<HexResourcesConfigComponent>().Value;
            foreach (var resource in resourcesConfig.Resources)
            {
                if (resource.Type != TargetResourceType)
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
