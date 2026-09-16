using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Modules.Boot.Core;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Builds a fresh world: on entry runs the systems flagged <see cref="AppState.MapCreation" /> — the
    ///     world-building pipeline stages, in priority order — then ticks its per-frame systems for a few
    ///     "settle" frames before requesting <see cref="AppState.Gameplay" />. View building is done
    ///     synchronously inside the pipeline.
    /// </summary>
    public sealed class MapCreationState : IAppState
    {
        // A few frames let the per-frame settle systems finish reacting to anything the pipeline raised
        // before handing off to Gameplay; 3 is a safe margin. The systems here are idempotent, so extra
        // ticks are no-ops.
        private const int SettleFrames = 3;

        private readonly AppStateSystems _systems;

        private AppState? _requestedMode;
        private int _settledFrames;

        public AppState Mode => AppState.MapCreation;
        public AppState? RequestedMode => _requestedMode;

        public MapCreationState(IReadOnlyList<IAppStateSystem> allSystems)
        {
            _systems = AppStateSystems.Filter(Mode, allSystems);
        }

        public UniTask EnterAsync(CancellationToken cancellationToken)
        {
            _requestedMode = null;
            _settledFrames = 0;

            return _systems.RunEntryAsync(cancellationToken);
        }

        public void Exit()
        {
        }

        public void LateTick(GameState state)
        {
            _systems.LateTick(state);
        }

        public void Tick(GameState state)
        {
            _systems.Tick(state);
            CountSettleFrame();
        }

        private void CountSettleFrame()
        {
            _settledFrames++;
            if (_settledFrames >= SettleFrames)
                _requestedMode = AppState.Gameplay;
        }
    }
}
