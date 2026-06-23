using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Modules.Boot.Core;
using Modules.MainUI.GeneratorMenu.Systems;
using Domains.Map.Generation.Components;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Shows the map generator UI and waits for the Generate request. On request, hides the UI and
    ///     transitions to <see cref="GameMode.MapCreation" />. (Currently the only screen — the ersatz main menu.)
    /// </summary>
    public sealed class MainMenuState : IAppState
    {
        private readonly ShowHexesUISystem _ui;
        private readonly EntitySet _generateRequests;

        private GameMode? _requestedMode;

        public GameMode Mode => GameMode.MainMenu;
        public GameMode? RequestedMode => _requestedMode;

        public MainMenuState(World world, ShowHexesUISystem ui)
        {
            _ui = ui;
            _generateRequests = world.GetEntities()
                .With<TerrainGenerationGenerateEventComponent>()
                .AsSet();
        }

        public async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            _requestedMode = null;
            // Idempotent: loads the UI on first entry, no-op afterwards.
            await _ui.Update(new FirstUIStep(), cancellationToken);
            _ui.Show();
        }

        public void Tick(GameState state)
        {
            if (_generateRequests.Count == 0)
                return;

            // The event entity is cleaned up by EventCleanupSystem once MapCreation starts ticking.
            _requestedMode = GameMode.MapCreation;
        }

        public void LateTick(GameState state)
        {
        }

        public void Exit()
        {
            _ui.Hide();
        }
    }
}
