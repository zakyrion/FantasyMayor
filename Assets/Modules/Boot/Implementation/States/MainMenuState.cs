using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Runs the systems flagged <see cref="AppState.MainMenu" /> — today only ShowHexesUISystem, which loads
    ///     and shows the map generator UI on entry. (Currently the only screen — the ersatz main menu.)
    ///     MapCreationRequestSystem watches for a Generate request and raises the transition to
    ///     <see cref="AppState.MapCreation" />.
    /// </summary>
    [UsedImplicitly]
    public sealed class MainMenuState : IAppState
    {
        private readonly AppStateSystems _systems;

        public AppState Mode => AppState.MainMenu;

        public MainMenuState(IReadOnlyList<IAppStateSystem> allSystems)
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
