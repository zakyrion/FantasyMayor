using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Friflo.Engine.ECS;
using EcsExtensions;
using Modules.Boot.Core;
using Domains.Map.Generation.Components;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Runs the systems flagged <see cref="AppState.MainMenu" /> — today only ShowHexesUISystem, which loads
    ///     and shows the map generator UI on entry — and watches for a ripe Generate request to transition to
    ///     <see cref="AppState.MapCreation" />. (Currently the only screen — the ersatz main menu.)
    /// </summary>
    public sealed class MainMenuState : IAppState
    {
        private readonly Archetype _generateRequests;
        private readonly AppStateSystems _systems;

        private AppState? _requestedMode;

        public AppState Mode => AppState.MainMenu;
        public AppState? RequestedMode => _requestedMode;

        public MainMenuState(EntityStorages storages, IReadOnlyList<IAppStateSystem> allSystems)
        {
            _generateRequests = EventArchetypes.Of<TerrainGenerationGenerateEventComponent>(storages.World);
            _systems = AppStateSystems.Filter(Mode, allSystems);
        }

        public async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            _requestedMode = null;
            await _systems.RunEntryAsync(cancellationToken);
        }

        public void Tick(GameState state)
        {
            RequestMapCreationOnRipeRequest();
            _systems.Tick(state);
        }

        private void RequestMapCreationOnRipeRequest()
        {
            if (HasRipeRequest())
                // The event entity is cleaned up by EventCleanupSystem once MapCreation starts ticking.
                _requestedMode = AppState.MapCreation;
        }

        private bool HasRipeRequest()
        {
            foreach (var pulse in _generateRequests.Entities)
                if (EcsEventExtensions.IsRipe(pulse))
                    return true;

            return false;
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
