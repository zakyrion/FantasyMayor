using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Steady-state play. Runs the systems flagged <see cref="AppState.Gameplay" />, ticking its update and
    ///     late-update systems every frame in priority order. Requests no transition — returning to the menu
    ///     (with world teardown) is a separate concern.
    /// </summary>
    public sealed class GameplayState : IAppState
    {
        private readonly AppStateSystems _systems;

        public AppState Mode => AppState.Gameplay;

        public GameplayState(IReadOnlyList<IAppStateSystem> allSystems)
        {
            _systems = AppStateSystems.Filter(Mode, allSystems);
        }

        public UniTask EnterAsync(CancellationToken cancellationToken)
        {
            return _systems.RunEntryAsync(cancellationToken);
        }

        public void Tick(GameState state)
        {
            _systems.Tick(state);
        }

        public void LateTick(GameState state)
        {
            _systems.LateTick(state);
        }

        public void Exit()
        {
        }
    }
}
