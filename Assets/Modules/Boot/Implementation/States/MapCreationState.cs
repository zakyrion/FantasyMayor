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
    ///     (<see cref="IPrioritizedUniTaskSystem{T}" /> for <see cref="MapGenerationStep" />) in priority order,
    ///     then ticks its per-frame systems for a few "settle" frames (e.g. event cleanup) before transitioning
    ///     to <see cref="GameMode.Gameplay" />. View building is done synchronously inside the pipeline.
    /// </summary>
    public sealed class MapCreationState : IAppState
    {
        // A few frames let the per-frame settle systems (e.g. event cleanup) drain anything the pipeline
        // raised before handing off to Gameplay; 3 is a safe margin. The systems here are idempotent, so
        // extra ticks are no-ops.
        private const int SettleFrames = 3;

        private readonly IReadOnlyList<IPrioritizedUniTaskSystem<MapGenerationStep>> _pipeline;
        private readonly IReadOnlyList<IUpdatedSystem> _systems;

        private GameMode? _requestedMode;
        private int _settledFrames;

        public GameMode Mode => GameMode.MapCreation;
        public GameMode? RequestedMode => _requestedMode;

        public MapCreationState(
            IReadOnlyList<IPrioritizedUniTaskSystem<MapGenerationStep>> pipeline,
            params IUpdatedSystem[] systems)
        {
            _pipeline = pipeline.OrderBy(stage => stage.Priority).ToArray();
            _systems = systems.OrderBy(system => system.Priority).ToArray();
        }

        public async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            _requestedMode = null;
            _settledFrames = 0;

            var step = new MapGenerationStep();
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
