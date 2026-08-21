using System;
using Friflo.Engine.ECS;
using Domains.Economy.DistrictOpenCondition.Configs;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Abstract base for a per-type condition spawner. One concrete subsystem per concrete
    // DistrictOpenConditionConfig type (DoD polymorphism: one base, one implementation per condition kind).
    // The orchestrator routes each authored config to the subsystem that handles its concrete type.
    internal abstract class DistrictOpenConditionSpawnSubSystem : IDisposable
    {
        protected readonly EntityStore World;

        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }

        protected DistrictOpenConditionSpawnSubSystem(EntityStore world)
        {
            World = world;
        }

        // Try-pattern: returns false when this subsystem does not handle the config's concrete type. On a match
        // it creates the condition entity and returns true. The caller branches on the bool (Collector rule).
        public abstract bool TrySpawn(DistrictOpenConditionConfig config);

        public virtual void Dispose()
        {
        }
    }
}
