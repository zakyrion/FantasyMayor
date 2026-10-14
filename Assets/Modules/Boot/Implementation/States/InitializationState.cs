using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Runs the systems flagged <see cref="AppState.Initialization" /> — today only InitializationSystem.
    ///     Requests no transition itself: InitializationSystem raises it once the machine starts ticking this
    ///     state, which only happens after this entry has completed.
    /// </summary>
    [UsedImplicitly]
    public sealed class InitializationState : IAppState
    {
        private readonly AppStateSystems _systems;

        public AppState Mode => AppState.Initialization;

        public InitializationState(IReadOnlyList<IAppStateSystem> allSystems)
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
