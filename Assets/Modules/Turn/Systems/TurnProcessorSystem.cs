using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
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
    ///     turn in progress (<see cref="TurnProcessorComponent" />) and runs the ordered phase set inline on the
    ///     main thread; it polls completion each frame and removes the component when the run finishes.
    ///     Deliberately a per-frame system (not a reactive entity-set system): it must tick every frame to poll
    ///     the in-flight task, which a pulse-anchored set cannot do. Law 1: store I/O is main-thread only.
    /// </summary>
    [UsedImplicitly]
    public sealed class TurnProcessorSystem : IUpdatedSystem
    {
        private readonly Archetype _nextTurnPulses;
        private readonly IReadOnlyList<TurnPhaseSubSystem> _phases;
        private readonly TurnPhaseRunner _runner = new();
        private readonly EntityStore _world;

        public bool IsEnabled { get; set; } = true;

        public int Priority => SystemPriorities.RuntimeTick.TurnProcessor;

        public TurnProcessorSystem(EntityStore world, IReadOnlyList<TurnPhaseSubSystem> phases)
        {
            _world = world;
            _phases = phases;
            _nextTurnPulses = EventArchetypes.Of<NextTurnEvent>(world);
        }

        public void Update(GameState state)
        {
            if (!_world.HasWorldComponent<TurnProcessorComponent>())
            {
                if (!HasRipePulse())
                    return;

                _world.SetWorldComponent(new TurnProcessorComponent { Status = TurnProcessorStatus.Running });
                Debug.Log("[TurnProcessorSystem] Turn started.");
                RunTurnAsync().Forget();
                return;
            }

            if (_world.GetWorldComponent<TurnProcessorComponent>().Status == TurnProcessorStatus.Completed)
            {
                _world.RemoveWorldComponent<TurnProcessorComponent>();
                Debug.Log("[TurnProcessorSystem] Turn completed.");

                // Announce the turn boundary so the counter (and future turn-boundary reactors) advance,
                // without coupling them to this completion check. One-frame pulse, cleared by EventCleanup.
                _world.CreateEvent(new TurnCompletedEvent());
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

            await _runner.RunAsync(_phases, new TurnPhaseStep(), token);

            _world.SetWorldComponent(new TurnProcessorComponent { Status = TurnProcessorStatus.Completed });
        }
    }
}
