using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Modules.Boot.Core;
using Modules.Boot.Implementation.States;
using Modules.HexIcons.Systems;
using Modules.HexResourcesView.Systems;
using Modules.HexesUI.Systems;
using Modules.TerrainView.Systems;
using Modules.UserInput.Systems;
using UnityEngine;
using VContainer;

namespace Modules.Boot.Implementation
{
    /// <summary>
    ///     Entry point MonoBehaviour. Runs the one-time config bootstrap, then hands control to a
    ///     <see cref="GameModeMachine" /> whose states are wired here by hand: Boot knows every module and
    ///     decides which systems belong to which <see cref="GameMode" />. Systems remain DI singletons —
    ///     only their grouping into states is manual.
    /// </summary>
    public class Boot : MonoBehaviour
    {
        private IReadOnlyList<IUniTaskSystem<ConfigLoadStep>> _configLoadSystems;
        private GameModeMachine _machine;
        private bool _configsLoaded;

        private async UniTask Start()
        {
            Debug.Log($"Starting {GetType().Name}");
            Application.targetFrameRate = 60;

            var loadConfigs = new UniTaskSequentialSystem<ConfigLoadStep>(_configLoadSystems);
            await loadConfigs.Update(new ConfigLoadStep(), CancellationToken.None);

            _configsLoaded = true;
            _machine.Switch(GameMode.MainMenu);
        }

        private void Update()
        {
            if (!_configsLoaded)
                return;

            _machine.Tick(new GameState(Time.deltaTime));
        }

        private void LateUpdate()
        {
            if (!_configsLoaded)
                return;

            _machine.LateTick(new GameState(Time.deltaTime));
        }

        private void OnDestroy()
        {
            _machine?.Dispose();
        }

        /// <summary>
        ///     Receives the config-load bootstrap, the generation pipeline, and every per-frame system as
        ///     concrete singletons, then manually composes the state machine.
        /// </summary>
        [Inject]
        public void Construct(
            IReadOnlyList<IUniTaskSystem<ConfigLoadStep>> configLoadSystems,
            IReadOnlyList<IPrioritizedUniTaskSystem<TerrainGenerationStep>> generationPipeline,
            ShowHexesUISystem showHexesUI,
            HexSelectionSystem hexSelection,
            HexSelectionViewSystem hexSelectionView,
            ForestViewSyncSystem forestViewSync,
            HexIconsContainerPositionSystem hexIconsContainerPosition,
            HexIconsVisibilitySystem hexIconsVisibility,
            EventCleanupSystem eventCleanup,
            CameraMovementSystem cameraMovement,
            World world)
        {
            _configLoadSystems = configLoadSystems;

            var mainMenu = new MainMenuState(world, showHexesUI);

            // Forest sync + event cleanup must run during the generation settle frames.
            var mapCreation = new MapCreationState(generationPipeline, forestViewSync, eventCleanup);

            var gameplay = new GameplayState(
                world,
                new IUpdatedSystem[]
                    { hexSelection, hexSelectionView, forestViewSync, hexIconsVisibility, eventCleanup },
                new ILateUpdatedSystem[] { cameraMovement, hexIconsContainerPosition });

            var mapLoading = new MapLoadingState();

            _machine = new GameModeMachine(new Dictionary<GameMode, IAppState>
            {
                [GameMode.MainMenu] = mainMenu,
                [GameMode.MapCreation] = mapCreation,
                [GameMode.MapLoading] = mapLoading,
                [GameMode.Gameplay] = gameplay
            });
        }
    }
}
