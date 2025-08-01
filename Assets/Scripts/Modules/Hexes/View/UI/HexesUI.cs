using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.Hexes.View.UI
{
    public class HexesUI : MonoBehaviour
    {
        [SerializeField]
        private Button _generateButton;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
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
        }
    }
}
