using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Modules.Turn.Components;
using Modules.Turn.Data;
using Modules.Turn.Events;
using UnityEngine;

namespace Modules.Turn.Systems
{
    /// <summary>
    ///     Drives turn processing in the Gameplay state. On a <see cref="NextTurnEvent" /> it marks the turn in
    ///     progress (<see cref="TurnProcessorComponent" />) and runs the ordered phase set inline on the main
    ///     thread; it polls completion each frame and resets the component to <see cref="TurnProcessorStatus.Idle" />
    ///     when the run finishes. Deliberately per-frame (not reactive on the reader alone): it must tick every
    ///     frame to poll the in-flight task, which draining the reader once cannot do. Law 1: store I/O is
    ///     main-thread only.
    /// </summary>
    [UsedImplicitly]
    [SystemRole(SystemRoleKind.PerFrame)]
    public sealed class TurnProcessorSystem : IUpdatedSystem
    {
        private readonly EventReader<NextTurnEvent> _nextTurnRequests;

        [StateAllowed]
        private readonly IReadOnlyList<IPrioritizedUniTaskSystem> _phases;

        private readonly EntityStorages _storages;

        public AppState AppState { get; }

        public bool IsEnabled { get; set; } = true;

        public int Priority => SystemPriorities.RuntimeTick.TurnProcessor;

        public TurnProcessorSystem(
            AppState appState, EntityStorages storages, EventReader<NextTurnEvent> nextTurnRequests,
            IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
        {
            AppState = appState;
            _storages = storages;
            _nextTurnRequests = nextTurnRequests;
            _phases = OrchestratorSubSystems.SelectForOrchestrator(typeof(TurnProcessorSystem), allSubSystems);
        }

        public void Update(GameState state)
        {
            // Drained every tick regardless of status: a request that arrives mid-run is not lost silently, it
            // is simply not the one this tick acts on (a click during Running/Completed costs one turn — H1).
            var turnRequested = _nextTurnRequests.DrainBatch();

            if (_storages.Singletons.Get<TurnProcessorComponent>().Status == TurnProcessorStatus.Idle)
            {
                if (!turnRequested)
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
                // without coupling them to this completion check.
                _storages.Events.Raise(new TurnCompletedEvent());
            }
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
