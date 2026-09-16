using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Modules.Boot.Core;
using Domains.Map.Generation.Components;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Runs the systems flagged <see cref="AppState.MainMenu" /> — today only ShowHexesUISystem, which loads
    ///     and shows the map generator UI on entry — and watches for a Generate request to transition to
    ///     <see cref="AppState.MapCreation" />. (Currently the only screen — the ersatz main menu.)
    /// </summary>
    public sealed class MainMenuState : IAppState
    {
        private readonly EventReader<TerrainGenerationGenerateEventComponent> _generateRequests;
        private readonly AppStateSystems _systems;

        private AppState? _requestedMode;

        public AppState Mode => AppState.MainMenu;
        public AppState? RequestedMode => _requestedMode;

        public MainMenuState(IReadOnlyList<IAppStateSystem> allSystems, EventReader<TerrainGenerationGenerateEventComponent> generateRequests)
        {
            _generateRequests = generateRequests;
            _systems = AppStateSystems.Filter(Mode, allSystems);
        }

        public async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            _requestedMode = null;
            await _systems.RunEntryAsync(cancellationToken);
        }

        public void Tick(GameState state)
        {
            RequestMapCreationOnGenerateRequest();
            _systems.Tick(state);
        }

        private void RequestMapCreationOnGenerateRequest()
        {
            if (_generateRequests.DrainBatch())
                _requestedMode = AppState.MapCreation;
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
