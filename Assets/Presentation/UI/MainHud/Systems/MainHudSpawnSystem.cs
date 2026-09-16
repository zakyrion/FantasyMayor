using System;
using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.Boot.Core;
using Modules.MainCanvas.Core;
using Presentation.UI.MainHud.Components;

namespace Presentation.UI.MainHud.Systems
{
    /// <summary>
    ///     Map-creation stage (priority 800). Instantiates the shared Main UI prefab (<c>UI/MainUI</c>,
    ///     which carries every window's view) under the main canvas, then runs every kept
    ///     <see cref="MainHudSpawnSubSystem" />, in priority order, so each can resolve its view and
    ///     publish its component. Owns the single addressable handle for the whole Main UI.
    /// </summary>
    [UsedImplicitly]
    internal sealed class MainHudSpawnSystem : IPipelineStageSystem
    {
        private const string MainUIPath = "MainHUD";

        private readonly IAddressable _addressable;
        private readonly IMainCanvasProvider _canvasProvider;
        private readonly IReadOnlyList<IPrioritizedUniTaskSystem> _subSystems;
        private readonly EntityStorages _storages;

        public AppState AppState { get; }

        public int Priority => SystemPriorities.WorldInit.MainHudSpawn;

        public MainHudSpawnSystem(AppState appState,
            EntityStorages storages,
            IAddressable addressable,
            IMainCanvasProvider canvasProvider,
            IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
        {
            AppState = appState;
            _addressable = addressable;
            _canvasProvider = canvasProvider;
            _storages = storages;
            _subSystems = OrchestratorSubSystems.SelectForOrchestrator(typeof(MainHudSpawnSystem), allSubSystems);
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            if (_storages.Singletons.Get<MainHudComponent>().RootBox.Exist)
                return;

            var canvas = _canvasProvider.RootGO;

            if (cancellationToken.IsCancellationRequested)
                return;

            var result = await _addressable.LoadAndInstanceAsync(MainUIPath, cancellationToken, canvas.transform);

            if (cancellationToken.IsCancellationRequested)
            {
                if (result.Status == Status.Success)
                    result.Box.Dispose();

                return;
            }

            if (result.Status != Status.Success || !result.Box.Exist)
                throw new InvalidOperationException(
                    $"MainHudSpawnSystem: failed to load Main UI by address '{MainUIPath}'.");

            _storages.Singletons.Set(new MainHudComponent
            {
                RootBox = result.Box
            });

            // Subsystems do not instantiate — each pulls its view off the shared Main UI instance.
            await OrchestratorSubSystems.RunAsync(_subSystems, cancellationToken);
        }

        public void Dispose()
        {
            if (_storages != null && _storages.Singletons.Get<MainHudComponent>().RootBox.Exist)
            {
                _storages.Singletons.Get<MainHudComponent>().RootBox.Dispose();
                _storages.Singletons.Set(new MainHudComponent());
            }
        }
    }
}
