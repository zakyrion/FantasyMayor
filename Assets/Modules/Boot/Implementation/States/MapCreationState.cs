using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Builds a fresh world: on entry runs the systems flagged <see cref="AppState.MapCreation" /> — the
    ///     world-building pipeline stages, in priority order. View building is done synchronously inside the
    ///     pipeline. MapCreationSettleSystem ticks a few frames after this entry completes, then requests
    ///     <see cref="AppState.Gameplay" />.
    /// </summary>
    [UsedImplicitly]
    public sealed class MapCreationState : IAppState
    {
        private readonly AppStateSystems _systems;

        public AppState Mode => AppState.MapCreation;

        public MapCreationState(IReadOnlyList<IAppStateSystem> allSystems)
        {
            _systems = AppStateSystems.Filter(Mode, allSystems);
        }

        public UniTask EnterAsync(CancellationToken cancellationToken)
        {
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
        }
    }
}
