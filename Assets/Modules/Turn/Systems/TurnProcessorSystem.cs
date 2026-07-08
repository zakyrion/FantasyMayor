using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Turn.Components;
using Modules.Turn.Data;
using Modules.Turn.Events;
using Modules.Turn.Helpers;
using UnityEngine;

namespace Modules.Turn.Systems
{
    /// <summary>
    ///     Drives turn processing in the Gameplay state. On a <see cref="NextTurnEvent" /> pulse it marks the
    ///     turn in progress (<see cref="TurnProcessorComponent" />) and runs the ordered phase set off the main
    ///     thread via <see cref="UniTask.RunOnThreadPool" />; it polls completion each frame and removes the
    ///     component when the run finishes. Deliberately a per-frame system (not a reactive entity-set system):
    ///     it must tick every frame to poll the in-flight task, which a pulse-anchored set cannot do. The pool
    ///     thread only computes — every world write happens back on the main thread.
    /// </summary>
    [UsedImplicitly]
    public sealed class TurnProcessorSystem : IUpdatedSystem
    {
        private readonly EntitySet _nextTurnPulses;
        private readonly IReadOnlyList<TurnPhaseSubSystem> _phases;
        private readonly TurnPhaseRunner _runner = new();
        private readonly World _world;

        public bool IsEnabled { get; set; } = true;

        public int Priority => SystemPriorities.RuntimeTick.TurnProcessor;

        public TurnProcessorSystem(World world, IReadOnlyList<TurnPhaseSubSystem> phases)
        {
            _world = world;
            _phases = phases;
            _nextTurnPulses = world.GetEntities().With<NextTurnEvent>().AsSet();
        }

        public void Update(GameState state)
        {
            if (!_world.Has<TurnProcessorComponent>())
            {
                if (_nextTurnPulses.Count == 0)
                    return;

                _world.Set(new TurnProcessorComponent { Status = TurnProcessorStatus.Running });
                Debug.Log("[TurnProcessorSystem] Turn started.");
                RunTurnAsync().Forget();
                return;
            }

            if (_world.Get<TurnProcessorComponent>().Status == TurnProcessorStatus.Completed)
            {
                _world.Remove<TurnProcessorComponent>();
                Debug.Log("[TurnProcessorSystem] Turn completed.");

                // Announce the turn boundary so the counter (and future turn-boundary reactors) advance,
                // without coupling them to this completion check. One-frame pulse, cleared by EventCleanup.
                var completed = _world.CreateEntity();
                completed.Set(new TurnCompletedEvent());
                completed.Set(new EventTag());
            }
        }

        public void Dispose()
        {
            _nextTurnPulses.Dispose();
        }

        // Computes the turn off the main thread, then publishes completion back on the main thread.
        private async UniTaskVoid RunTurnAsync()
        {
            var token = StatusMonitor.Token;

            await UniTask.RunOnThreadPool(
                () => _runner.RunAsync(_phases, new TurnPhaseStep(), token),
                cancellationToken: token);

            await UniTask.SwitchToMainThread();

            _world.Set(new TurnProcessorComponent { Status = TurnProcessorStatus.Completed });
        }
    }
}
