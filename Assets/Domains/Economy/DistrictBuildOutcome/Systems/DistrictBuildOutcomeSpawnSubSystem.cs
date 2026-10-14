using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;

namespace Domains.Economy.DistrictBuildOutcome.Systems{
    // Abstract base for a per-type outcome spawner. One concrete subsystem per concrete BuildDistrictOutcomeConfig
    // type (DoD polymorphism: one base, one implementation per outcome kind). The host
    // (DistrictBuildOutcomeSpawnSystem) collects every subsystem naming it through OrchestratorType and each walks
    // the catalogue itself for the entries of its own kind.
    internal abstract class DistrictBuildOutcomeSpawnSubSystem : IPrioritizedUniTaskSystem, IDisposable
    {
        protected readonly EntityStorages Storages;

        public bool IsEnabled { get; set; } = true;

        public Type OrchestratorType => typeof(DistrictBuildOutcomeSpawnSystem);

        public abstract int Priority { get; }

        protected DistrictBuildOutcomeSpawnSubSystem(EntityStorages storages)
        {
            Storages = storages;
        }

        // Walks the catalogue for this subsystem's concrete config kind and creates one entity per matching entry.
        public abstract UniTask Update(CancellationToken cancellationToken);

        public virtual void Dispose()
        {
        }
    }
}
