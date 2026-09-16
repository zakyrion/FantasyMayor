using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Modules.Boot.Core;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Runs the systems flagged <see cref="AppState.MapLoading" /> — today only cleanup. Placeholder for
    ///     restoring a saved world (deserialize → settle → Gameplay); no state currently requests this mode.
    ///     Kept as an explicit mode so the machine and call sites already account for it.
    /// </summary>
    public sealed class MapLoadingState : IAppState
    {
        private readonly AppStateSystems _systems;

        public AppState Mode => AppState.MapLoading;
        public AppState? RequestedMode => null;

        public MapLoadingState(IReadOnlyList<IAppStateSystem> allSystems)
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
