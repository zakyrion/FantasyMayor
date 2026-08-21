using System;
using Friflo.Engine.ECS;
using UnityEngine;

namespace Presentation.UI.DistrictBuild.Systems
{
    // One per-concern populator of the district-build overlay, dispatched by DistrictBuildUISystem on open
    // (orchestrator + subsystem family, mirroring DistrictOpenConditionSpawnSubSystem). Each subsystem owns its
    // own section view and fills it from its own ECS read; the orchestrator only sequences them by Priority and
    // hands over the overlay root. Plain object — not an ECS system — so it may hold its own query/state.
    public abstract class DistrictBuildUISubSystem : IDisposable
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

        public abstract void Populate(GameObject root);

        public virtual void Dispose()
        {
        }
    }
}
