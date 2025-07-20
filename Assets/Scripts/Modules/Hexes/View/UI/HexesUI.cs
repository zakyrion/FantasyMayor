using System.Threading;
using Core.EventDataBus;
using Cysharp.Threading.Tasks;
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
        private IBus<HexesGenerateOperation> _bus;
        private bool _clicked;

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

            _bus.PublishAsync(new HexesGenerateOperation
            {
                WaveCount = 1
            }, _cancellationTokenSource.Token).ContinueWith(() =>
            {
                if (this)
                {
                    _clicked = false;
                }
            });
        }
    }
}
