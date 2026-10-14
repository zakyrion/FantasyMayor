using System;
using System.Threading;
using Core;
using Core.Events;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.MainCanvas.Core;
using Presentation.UI.GeneratorMenu.Views;

namespace Presentation.UI.GeneratorMenu.Systems
{
    /// <summary>
    ///     Shows the map generator menu on every <see cref="Core.AppState.MainMenu" /> entry. When the player
    ///     presses Generate it asks for <see cref="Core.AppState.MapCreation" /> and releases the menu.
    /// </summary>
    [UsedImplicitly]
    [ViewSubscriber(typeof(HexesUI))]
    public class ShowHexesUISystem : IUniTaskSystem
    {
        private const string HexGeneratorUIPath = "UI/HexGeneratorUI";

        private readonly IAddressable _addressable;
        private readonly IMainCanvasProvider _canvasProvider;
        private readonly EntityStorages _storages;

        [StateAllowed("the generator menu this system owns from its load until Generate or Dispose")]
        private Box<HexesUI> _uiBox;

        [StateAllowed("the container disposed this system; a later Execute is a lifecycle error")]
        private bool _isDisposed;

        public AppState AppState { get; }

        public ShowHexesUISystem(AppState appState, IMainCanvasProvider canvasProvider, IAddressable addressable, EntityStorages storages)
        {
            AppState = appState;
            _canvasProvider = canvasProvider;
            _addressable = addressable;
            _storages = storages;
            _uiBox = Box<HexesUI>.Empty();
        }

        /// <summary>Loads the generator menu under the main canvas and hooks its Generate button.</summary>
        /// <exception cref="ObjectDisposedException">The system was already disposed.</exception>
        /// <exception cref="InvalidOperationException">The menu is still shown, or its prefab failed to load or has no <see cref="HexesUI" />.</exception>
        /// <exception cref="OperationCanceledException">The entry was cancelled.</exception>
        public async UniTask Execute(CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            if (_uiBox.Exist)
                throw new InvalidOperationException("The generator menu is still shown — MainMenu was entered again without Generate.");

            var loaded = await _addressable.LoadAndInstanceAsync(HexGeneratorUIPath, cancellationToken, _canvasProvider.RootGO.transform);
            if (loaded.Status != Status.Success)
                throw new InvalidOperationException($"Prefab '{HexGeneratorUIPath}' failed to load.");

            var hexesUI = loaded.Box.Value.GetComponent<HexesUI>();
            if (hexesUI == null)
            {
                loaded.Box.Dispose();
                throw new InvalidOperationException($"Prefab '{HexGeneratorUIPath}' has no HexesUI component.");
            }

            _uiBox = Box<HexesUI>.Wrap(hexesUI, _ => loaded.Box.Dispose());
            hexesUI.GenerateClicked += OnGenerateClicked;
        }

        /// <summary>Releases the menu, if it is still shown.</summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_uiBox.Exist)
                ReleaseMenu();
        }

        // Runs inside the button's click callback; the release destroys the menu at the end of the frame.
        private void OnGenerateClicked()
        {
            _storages.Events.Raise(new AppStateRequestedEvent { Requested = AppState.MapCreation });
            ReleaseMenu();
        }

        private void ReleaseMenu()
        {
            _uiBox.Value.GenerateClicked -= OnGenerateClicked;
            _uiBox.Dispose();
            _uiBox = Box<HexesUI>.Empty();
        }
    }
}
