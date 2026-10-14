using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Runs the systems flagged <see cref="AppState.ConfigLoading" /> — every config loader, then cleanup.
    ///     Requests no transition itself: ConfigLoadingCompletedSystem raises it once the machine starts ticking
    ///     this state, which only happens after this entry has completed.
    /// </summary>
    [UsedImplicitly]
    public sealed class ConfigLoadingState : IAppState
    {
        private readonly AppStateSystems _systems;

        public AppState Mode => AppState.ConfigLoading;

        public ConfigLoadingState(IReadOnlyList<IAppStateSystem> allSystems)
        {
            _systems = AppStateSystems.Filter(Mode, allSystems);
        }

        public UniTask EnterAsync(CancellationToken cancellationToken)
        {
            return _systems.RunAllAsync(cancellationToken);
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
