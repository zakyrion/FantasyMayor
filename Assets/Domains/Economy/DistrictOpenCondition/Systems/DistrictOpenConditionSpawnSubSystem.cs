using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Abstract base for a per-type condition spawner. One concrete subsystem per concrete
    // DistrictOpenConditionConfig type (DoD polymorphism: one base, one implementation per condition kind).
    // The host (DistrictOpenConditionSpawnSystem) collects every subsystem naming it through OrchestratorType and
    // each walks the catalogue itself for the entries of its own kind.
    internal abstract class DistrictOpenConditionSpawnSubSystem : IPrioritizedUniTaskSystem, IDisposable
    {
        protected readonly EntityStorages Storages;

        public bool IsEnabled { get; set; } = true;

        public Type OrchestratorType => typeof(DistrictOpenConditionSpawnSystem);

        public abstract int Priority { get; }

        protected DistrictOpenConditionSpawnSubSystem(EntityStorages storages)
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
