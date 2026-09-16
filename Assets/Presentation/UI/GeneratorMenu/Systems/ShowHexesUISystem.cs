using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.Boot.Core;
using Modules.MainCanvas.Core;
using Presentation.UI.GeneratorMenu.Views;

namespace Presentation.UI.GeneratorMenu.Systems
{
    /// <summary>
    ///     Loads and instantiates the hex generator UI on <see cref="Modules.Boot.Core.AppState.MainMenu" /> entry.
    ///     Runs once; subsequent calls to <see cref="Execute" /> are no-ops.
    /// </summary>
    [UsedImplicitly]
    public class ShowHexesUISystem : IUniTaskSystem
    {
        private const string HexGeneratorUIPath = "UI/HexGeneratorUI";

        private readonly IAddressable _addressable;
        private readonly IMainCanvasProvider _canvasProvider;
        private Box<HexesUI> _uiBox;
        private bool _isDisposed;
        private bool _isLoaded;

        public AppState AppState { get; }

        public ShowHexesUISystem(AppState appState, IMainCanvasProvider canvasProvider, IAddressable addressable)
        {
            AppState = appState;
            _canvasProvider = canvasProvider;
            _addressable = addressable;
            _uiBox = Box<HexesUI>.Empty();
        }

        /// <summary>
        ///     Loads and instantiates the hex UI prefab under the main canvas. Idempotent after first successful load.
        /// </summary>
        /// <param name="cancellationToken">Token to abort the async load.</param>
        public async UniTask Execute(CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            if (_isLoaded)
                return;

            var canvas = _canvasProvider.RootGO;

            if (cancellationToken.IsCancellationRequested)
                return;

            var hexesUIPrefab = await _addressable.LoadAndInstanceAsync(HexGeneratorUIPath, cancellationToken, canvas.transform);

            if (cancellationToken.IsCancellationRequested || hexesUIPrefab.Status == Status.Failed)
            {
                if (hexesUIPrefab.Status == Status.Success)
                    hexesUIPrefab.Box.Dispose();

                return;
            }

            var hexesUIInstance = hexesUIPrefab.Box.Value.GetComponent<HexesUI>();

            if (hexesUIInstance == null)
            {
                hexesUIPrefab.Box.Dispose();
                return;
            }

            _uiBox = Box<HexesUI>.Wrap(hexesUIInstance, _ => hexesUIPrefab.Box.Dispose());
            _isLoaded = true;
        }

        /// <summary>Makes the instantiated UI visible. No-op until the UI is loaded.</summary>
        public void Show()
        {
            if (_uiBox.Exist)
                _uiBox.Value.gameObject.SetActive(true);
        }

        /// <summary>Hides the instantiated UI. No-op until the UI is loaded.</summary>
        public void Hide()
        {
            if (_uiBox.Exist)
                _uiBox.Value.gameObject.SetActive(false);
        }

        /// <summary>Releases the instantiated UI and its underlying addressable handle.</summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_uiBox.Exist)
                _uiBox.Dispose();
        }
    }
}
