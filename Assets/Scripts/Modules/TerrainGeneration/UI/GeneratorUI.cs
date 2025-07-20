using System;
using Modules.TerrainGeneration.Mono;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Atoms.TerrainGeneration.UI
{
    public class GeneratorUI : MonoBehaviour
    {
        [SerializeField] private Button _applyButton;

        private IDisposable _disposable;


        private void OnEnable()
        {
            _applyButton.onClick.AddListener(ApplyButtonClicked);
        }

        private void OnDisable()
        {
            _applyButton.onClick.RemoveAllListeners();

            _disposable?.Dispose();
        }

        private void ApplyButtonClicked()
        {
        }

        private void CastButtonClicked()
        {
            Debug.Log("[skh] GeneratorUIView.OnClick()");
        }

        private void ConvertToMeshButtonClicked()
        {
        }

        private void HeightmapChanged(Texture texture)
        {
        }

        private void ShowPointsButtonClicked()
        {
        }
    }
}
