using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Modules.Turn.Components;
using Modules.Turn.Data;
using Modules.Turn.Events;
using UnityEngine;

namespace Modules.Turn.Systems
{
    /// <summary>
    ///     Drives turn processing in the Gameplay state. On a <see cref="NextTurnEvent" /> pulse it marks the
    ///     turn in progress (<see cref="TurnProcessorComponent" />) and runs the ordered phase set inline on the
    ///     main thread; it polls completion each frame and resets the component to <see cref="TurnProcessorStatus.Idle" />
    ///     when the run finishes. Deliberately a per-frame system (not a reactive entity-set system): it must tick
    ///     every frame to poll the in-flight task, which a pulse-anchored set cannot do. Law 1: store I/O is
    ///     main-thread only.
    /// </summary>
    [UsedImplicitly]
    [SystemRole(SystemRoleKind.PerFrame)]
    public sealed class TurnProcessorSystem : IUpdatedSystem
    {
        private readonly Archetype _nextTurnPulses;

        [StateAllowed]
        private readonly IReadOnlyList<IPrioritizedUniTaskSystem> _phases;

        private readonly EntityStorages _storages;

        public AppState AppState { get; }

        public bool IsEnabled { get; set; } = true;

        public int Priority => SystemPriorities.RuntimeTick.TurnProcessor;

        public TurnProcessorSystem(
            AppState appState, EntityStorages storages, IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
        {
            AppState = appState;
            _storages = storages;
            _phases = OrchestratorSubSystems.SelectForOrchestrator(typeof(TurnProcessorSystem), allSubSystems);
            _nextTurnPulses = EventArchetypes.Of<NextTurnEvent>(storages.World);
        }

        public void Update(GameState state)
        {
            if (_storages.Singletons.Get<TurnProcessorComponent>().Status == TurnProcessorStatus.Idle)
            {
                if (!HasRipePulse())
                    return;

                _storages.Singletons.Set(new TurnProcessorComponent { Status = TurnProcessorStatus.Running });
                Debug.Log("[TurnProcessorSystem] Turn started.");
                RunTurnAsync().Forget();
                return;
            }

            if (_storages.Singletons.Get<TurnProcessorComponent>().Status == TurnProcessorStatus.Completed)
            {
                _storages.Singletons.Set(new TurnProcessorComponent { Status = TurnProcessorStatus.Idle });
                Debug.Log("[TurnProcessorSystem] Turn completed.");

                // Announce the turn boundary so the counter (and future turn-boundary reactors) advance,
                // without coupling them to this completion check. One-frame pulse, cleared by EventCleanup.
                _storages.World.CreateEvent(new TurnCompletedEvent());
            }
        }

        private bool HasRipePulse()
        {
            foreach (var pulse in _nextTurnPulses.Entities)
                if (EcsEventExtensions.IsRipe(pulse))
                    return true;

            return false;
        }

        // Runs the phase set inline on the main thread, then publishes completion.
        private async UniTaskVoid RunTurnAsync()
        {
            var token = StatusMonitor.Token;

            await OrchestratorSubSystems.RunAsync(_phases, token);

            _storages.Singletons.Set(new TurnProcessorComponent { Status = TurnProcessorStatus.Completed });
        }
    }
}
