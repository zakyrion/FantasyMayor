using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Modules.Boot.Core;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Runs the systems flagged <see cref="AppState.Initialization" /> — today only cleanup — then hands off;
    ///     Boot advances past this mode by entry completion, never by anything this state requests.
    /// </summary>
    public sealed class InitializationState : IAppState
    {
        private readonly AppStateSystems _systems;

        public AppState Mode => AppState.Initialization;
        public AppState? RequestedMode => null;

        public InitializationState(IReadOnlyList<IAppStateSystem> allSystems)
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
