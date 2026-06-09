using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultECSExtensions;
using Modules.Boot.Core;
using UnityEngine;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Builds a fresh world: runs the one-shot generation pipeline
    ///     (<see cref="IPrioritizedUniTaskSystem{T}" /> for <see cref="TerrainGenerationStep" />) in priority order,
    ///     then ticks its per-frame systems for a few "settle" frames so reactive view systems
    ///     (e.g. forest sync) can build from the freshly generated entities, then transitions to
    ///     <see cref="GameMode.Gameplay" />.
    /// </summary>
    public sealed class MapCreationState : IAppState
    {
        // Reactive view systems build over 1-2 frames after entities appear; 3 gives a safe margin.
        // Safe because those systems are idempotent (diff-based) — extra ticks are no-ops.
        private const int SettleFrames = 3;

        private readonly IReadOnlyList<IPrioritizedUniTaskSystem<TerrainGenerationStep>> _pipeline;
        private readonly IReadOnlyList<IUpdatedSystem> _systems;

        private GameMode? _requestedMode;
        private int _settledFrames;

        public GameMode Mode => GameMode.MapCreation;
        public GameMode? RequestedMode => _requestedMode;

        public MapCreationState(
            IReadOnlyList<IPrioritizedUniTaskSystem<TerrainGenerationStep>> pipeline,
            params IUpdatedSystem[] systems)
        {
            _pipeline = pipeline.OrderBy(stage => stage.Priority).ToArray();
            _systems = systems.OrderBy(system => system.Priority).ToArray();
        }

        public async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            _requestedMode = null;
            _settledFrames = 0;

            var step = new TerrainGenerationStep();
            foreach (var stage in _pipeline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await stage.Update(step, cancellationToken);
            }
        }

        public void Tick(GameState state)
        {
            foreach (var system in _systems)
                system.Update(state);

            _settledFrames++;
            if (_settledFrames >= SettleFrames)
                _requestedMode = GameMode.Gameplay;
        }

        public void LateTick(GameState state)
        {
        }

        public void Exit()
        {
        }
    }
}
