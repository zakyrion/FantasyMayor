using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Runs the systems flagged <see cref="AppState.ConfigLoading" /> — every config loader, then cleanup —
    ///     then hands off; Boot advances past this mode by entry completion, never by anything this state requests.
    /// </summary>
    [UsedImplicitly]
    public sealed class ConfigLoadingState : IAppState
    {
        private readonly AppStateSystems _systems;
        public AppState? RequestedMode => null;

        public AppState Mode => AppState.ConfigLoading;

        public ConfigLoadingState(IReadOnlyList<IAppStateSystem> allSystems)
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
