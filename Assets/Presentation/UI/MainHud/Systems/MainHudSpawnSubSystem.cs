using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Presentation.UI.MainHud.Components;
using UnityEngine;

namespace Presentation.UI.MainHud.Systems
{
    /// <summary>
    ///     Abstract base for Main UI spawn subsystems run by <see cref="MainHudSpawnSystem" /> after it
    ///     instantiates the shared Main UI prefab. A subsystem does NOT instantiate anything — it resolves its
    ///     window's view off the Main UI root (<see cref="ReadMainHudRoot" />) and publishes the view's ECS
    ///     component. <see cref="Priority" /> orders execution; <see cref="IsEnabled" /> skips a subsystem;
    ///     <see cref="OrchestratorType" /> names <see cref="MainHudSpawnSystem" /> as the host that keeps it.
    /// </summary>
    internal abstract class MainHudSpawnSubSystem : IPrioritizedUniTaskSystem
    {
        protected readonly EntityStorages Storages;

        protected MainHudSpawnSubSystem(EntityStorages storages)
        {
            Storages = storages;
        }

        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }

        public Type OrchestratorType => typeof(MainHudSpawnSystem);

        /// <summary>Gives the shared Main UI instance the host wrote into the world.</summary>
        protected GameObject ReadMainHudRoot()
        {
            var rootBox = Storages.Singletons.Get<MainHudComponent>().RootBox;
            if (!rootBox.Exist)
                throw new InvalidOperationException(
                    $"{GetType().Name}: MainHudComponent.RootBox does not exist — this subsystem ran before MainHudSpawnSystem wrote it.");

            return rootBox.Value;
        }

        /// <summary>Resolves this window's view from the shared Main UI instance and publishes its component.</summary>
        public abstract UniTask Update(CancellationToken cancellationToken);
    }
}
