using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.Boot.Core;
using Modules.HexesUI.Components;
using Modules.HexesUI.Views;
using Modules.MainCanvas.Core;

namespace Modules.HexesUI.Systems
{
    /// <summary>
    ///     Loads and instantiates the hex info panel under the main canvas during the generation pipeline,
    ///     publishes its view on a singleton entity, and owns the addressable handle. The panel starts hidden
    ///     (USS default); HexInfoPanelSystem shows it on selection. Idempotent after the first load.
    /// </summary>
    [UsedImplicitly]
    internal sealed class HexInfoPanelSpawnSystem : IPrioritizedUniTaskSystem<TerrainGenerationStep>
    {
        private const int ExecutionPriority = 800;
        private const string HexInfoPanelPath = "UI/HexInfoPanelView";

        private readonly IAddressable _addressable;
        private readonly IMainCanvasProvider _canvasProvider;
        private readonly World _world;

        private Box<HexInfoPanelView> _viewBox;
        private bool _isDisposed;
        private bool _isLoaded;

        public int Priority => ExecutionPriority;

        public HexInfoPanelSpawnSystem(IAddressable addressable, IMainCanvasProvider canvasProvider, World world)
        {
            _addressable = addressable;
            _canvasProvider = canvasProvider;
            _world = world;
            _viewBox = Box<HexInfoPanelView>.Empty();
        }

        public async UniTask Update(TerrainGenerationStep state, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            if (_isLoaded)
                return;

            var canvas = _canvasProvider.RootGO;

            if (cancellationToken.IsCancellationRequested)
                return;

            var result = await _addressable.LoadAndInstanceAsync<HexInfoPanelView>(
                HexInfoPanelPath, cancellationToken, canvas.transform);

            if (cancellationToken.IsCancellationRequested)
            {
                if (result.Status == Status.Success)
                    result.Box.Dispose();

                return;
            }

            if (result.Status != Status.Success || !result.Box.Exist)
                throw new InvalidOperationException(
                    $"HexInfoPanelSpawnSystem: failed to load info panel by address '{HexInfoPanelPath}'.");

            _viewBox = result.Box;

            var panelEntity = _world.CreateEntity();
            panelEntity.Set(new HexInfoPanelViewComponent(_viewBox.Value));

            // USS no longer hides the panel by default (so it previews in UI Builder) — hide it now so it
            // stays hidden at runtime until HexInfoPanelSystem shows it on selection.
            _viewBox.Value.Hide();

            _isLoaded = true;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_viewBox.Exist)
                _viewBox.Dispose();
        }
    }
}
