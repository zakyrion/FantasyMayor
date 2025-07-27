using System.Threading;
using Core.DataLayer;
using Core.EventDataBus;
using Cysharp.Threading.Tasks;
using Modules.AppFlow.Data;
using Modules.AppFlow.DataLayers;
using Modules.Hexes.DataLayer;
using Modules.Hexes.Operations;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Modules.Hexes.View.UI
{
    public class HexesUI : MonoBehaviour
    {
        [SerializeField]
        private Button _generateButton;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        [Inject]
        private IDataContainer<AppFlowDataLayer> _appFlowDataContainer;
        [Inject]
        private IBus<HexesGenerateOperation> _bus;
        private bool _clicked;
        [Inject]
        private IDataContainer<HexesSettingsDataLayer> _settingsDataContainer;

        private void Awake()
        {
            _generateButton.onClick.AddListener(GenerateHexes);
        }

        private void OnDestroy()
        {
            _cancellationTokenSource.Cancel();
        }

        private void GenerateHexes()
        {
            if (_clicked)
            {
                return;
            }

            _clicked = true;

            _settingsDataContainer.GetAsync(_cancellationTokenSource.Token).ContinueWith(result =>
            {
                var settings = result.Exist ? result.DataLayer : new HexesSettingsDataLayer();
                settings.WaveCount = 5;
                return _settingsDataContainer.AddOrUpdateAsync(settings, _cancellationTokenSource.Token);
            }).ContinueWith(() => _appFlowDataContainer.GetAsync(_cancellationTokenSource.Token)).ContinueWith(result =>
            {
                _clicked = false;
                var appFlowData = result.Exist ? result.DataLayer : new AppFlowDataLayer();
                appFlowData.State = AppState.GenerateNewGame;
                return _appFlowDataContainer.AddOrUpdateAsync(appFlowData, _cancellationTokenSource.Token);
            }).Forget();

            /*_bus.PublishAsync(new HexesGenerateOperation
            {
                WaveCount = 1
            }, _cancellationTokenSource.Token).ContinueWith(() =>
            {
                if (this)
                {
                    _clicked = false;
                }
            });*/
        }
    }
}
