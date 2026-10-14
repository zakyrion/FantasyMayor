using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;

namespace Presentation.UI.DistrictBuild.Systems
{
    // One per-concern populator of the district-build overlay, run by DistrictBuildUISystem through the
    // sub-system contract (orchestrator + subsystem family, mirroring DistrictOpenConditionSpawnSubSystem). Each
    // subsystem owns its own section view and fills it from its own ECS read; OrchestratorType names the host
    // that keeps it, and the host only sequences the kept subsystems by Priority. Plain object — not an ECS
    // system (ARCHITECTURE :system/subsystem-is-not-a-system) — so it may hold its own query/state.
    public abstract class DistrictBuildUISubSystem : IPrioritizedUniTaskSystem, IDisposable
    {
        protected readonly EntityStore World;

        protected DistrictBuildUISubSystem(EntityStore world)
        {
            World = world;
        }

        public bool IsEnabled { get; set; } = true;

        // Set by the orchestrator at composition: a subsystem that changes the shared selection (List) calls this to
        // re-run every section populator against the new state — the direct C# replacement for the old re-populate
        // ECS pulse. Null-safe: subsystems that never mutate selection leave it unused.
        public Action Repopulate { get; set; }

        public abstract int Priority { get; }

        public Type OrchestratorType => typeof(DistrictBuildUISystem);

        public abstract UniTask Update(CancellationToken cancellationToken);

        public virtual void Dispose()
        {
        }
    }
}
