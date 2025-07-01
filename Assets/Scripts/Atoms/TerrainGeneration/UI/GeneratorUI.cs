using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Atoms.TerrainGeneration.UI
{
    public class GeneratorUI : MonoBehaviour
    {
        [SerializeField] private StartService _startService;
        [SerializeField] private Button _applyButton;

        private IDisposable _disposable;

        [Inject] private ITerrainGenerationAPI _terrainGenerationAPI;

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
            _terrainGenerationAPI.Apply();
        }

        private void CastButtonClicked()
        {
            Debug.Log("[skh] GeneratorUIView.OnClick()");
            _startService.GenerateTerrain();
        }

        private void ConvertToMeshButtonClicked()
        {
            _terrainGenerationAPI.GenerateMount();
        }

        private void HeightmapChanged(Texture texture)
        {
        }

        private void ShowPointsButtonClicked()
        {
        }
    }
}
