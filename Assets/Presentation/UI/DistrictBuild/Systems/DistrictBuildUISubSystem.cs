using System;
using DefaultEcs;
using UnityEngine;

namespace Presentation.UI.DistrictBuild.Systems
{
    // One per-concern populator of the district-build overlay, dispatched by DistrictBuildUISystem on open
    // (orchestrator + subsystem family, mirroring DistrictOpenConditionSpawnSubSystem). Each subsystem owns its
    // own section view and fills it from its own ECS read; the orchestrator only sequences them by Priority and
    // hands over the overlay root. Plain object — not an ECS system — so it may hold its own EntitySet/state.
    public abstract class DistrictBuildUISubSystem : IDisposable
    {
        protected readonly World World;

        protected DistrictBuildUISubSystem(World world)
        {
            World = world;
        }

        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }

        public abstract void Populate(GameObject root);

        public virtual void Dispose()
        {
        }
    }
}
