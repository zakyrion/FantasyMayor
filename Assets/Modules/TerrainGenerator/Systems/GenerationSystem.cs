using DefaultEcs.System;
using DefaultECSExtensions;

namespace Modules.TerrainGenerator.Systems
{
    internal abstract class GenerationSystem : ISystem<GameState>
    {
        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }

        public abstract void Update(GameState state);

        public virtual void Dispose()
        {
        }
    }
}
