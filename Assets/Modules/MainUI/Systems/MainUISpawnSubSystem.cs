using UnityEngine;

namespace Modules.MainUI.Systems
{
    /// <summary>
    ///     Abstract base for Main UI spawn subsystems run by <see cref="MainUISpawnSystem" /> after it
    ///     instantiates the shared Main UI prefab. A subsystem does NOT instantiate anything — it resolves its
    ///     window's view from the passed GameObject (GetComponent) and publishes the view's ECS component.
    ///     <see cref="Priority" /> orders execution; <see cref="IsEnabled" /> skips a subsystem.
    /// </summary>
    internal abstract class MainUISpawnSubSystem
    {
        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }

        /// <summary>Resolves this window's view from the shared Main UI instance and publishes its component.</summary>
        public abstract void Prepare(GameObject mainUi);
    }
}
