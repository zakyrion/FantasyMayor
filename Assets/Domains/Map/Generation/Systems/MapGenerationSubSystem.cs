using DefaultEcs.System;
using DefaultECSExtensions;

namespace Domains.Map.Generation.Systems
{
    internal abstract class MapGenerationSubSystem : ISystem<GameState>
    {
        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }

        public abstract void Update(GameState state);

        public virtual void Dispose()
        {
        }
    }
}
