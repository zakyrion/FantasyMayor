using System;
using Friflo.Engine.ECS;
using Domains.Economy.DistrictBuildOutcome.Configs;

namespace Domains.Economy.DistrictBuildOutcome.Systems{
    // Abstract base for a per-type outcome spawner. One concrete subsystem per concrete BuildDistrictOutcomeConfig
    // type (DoD polymorphism: one base, one implementation per outcome kind). The orchestrator routes each
    // authored config to the subsystem that handles its concrete type.
    internal abstract class DistrictBuildOutcomeSpawnSubSystem : IDisposable
    {
        protected readonly EntityStore World;

        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }

        protected DistrictBuildOutcomeSpawnSubSystem(EntityStore world)
        {
            World = world;
        }

        // Try-pattern: returns false when this subsystem does not handle the config's concrete type. On a match
        // it creates the outcome entity and returns true. The caller branches on the bool (Collector rule).
        public abstract bool TrySpawn(DistrictBuildOutcomeConfig config);

        public virtual void Dispose()
        {
        }
    }
}
