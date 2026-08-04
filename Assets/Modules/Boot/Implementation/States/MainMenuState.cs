using System.Threading;
using Cysharp.Threading.Tasks;
using Friflo.Engine.ECS;
using EcsExtensions;
using Modules.Boot.Core;
using Presentation.UI.GeneratorMenu.Systems;
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
        private readonly Archetype _generateRequests;

        private GameMode? _requestedMode;

        public GameMode Mode => GameMode.MainMenu;
        public GameMode? RequestedMode => _requestedMode;

        public MainMenuState(EntityStore world, ShowHexesUISystem ui)
        {
            _ui = ui;
            _generateRequests = EventArchetypes.Of<TerrainGenerationGenerateEventComponent>(world);
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
            if (!HasRipeRequest())
                return;

            // The event entity is cleaned up by EventCleanupSystem once MapCreation starts ticking.
            _requestedMode = GameMode.MapCreation;
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
        }

        public void Exit()
        {
            _ui.Hide();
        }
    }
}
