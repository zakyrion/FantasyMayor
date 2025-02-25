using System;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

public class GeneratorUIView : DocViewBase<IGeneratorPagesDataLayer, GeneratorPages>
{
    [SerializeField] private StartService _startService;

    private IDisposable _disposable;
    [Inject] private HeightmapDataLayer _heightmapDataLayer;
    [Inject] private ITerrainGenerationAPI _terrainGenerationAPI;

    protected override GeneratorPages UiType => GeneratorPages.TestGenerator;

    private void OnEnable()
    {
        var root = UiDocument.rootVisualElement;

        var castButton = root.Q<Button>("CastStartEvent");
        castButton.RegisterCallback<ClickEvent>(CastButtonClicked);

        var showPointsButton = root.Q<Button>("GenerateWater");
        showPointsButton.RegisterCallback<ClickEvent>(ShowPointsButtonClicked);

        var convertToMeshButton = root.Q<Button>("GenerateMount");
        convertToMeshButton.RegisterCallback<ClickEvent>(ConvertToMeshButtonClicked);

        var applyButton = root.Q<Button>("ApplyGenerator");
        applyButton.RegisterCallback<ClickEvent>(ApplyButtonClicked);

        _disposable = _heightmapDataLayer.HeightmapTexture.Skip(1).Subscribe(HeightmapChanged);
    }

    private void OnDisable()
    {
        _disposable?.Dispose();
    }

    private void ApplyButtonClicked(ClickEvent evt)
    {
        _terrainGenerationAPI.Apply();
    }

    private void HeightmapChanged(Texture texture)
    {
        var root = UiDocument.rootVisualElement;

        var image = root.Q<VisualElement>("HeightMapContainer");
        image.style.backgroundImage = new StyleBackground(texture as Texture2D);
    }

    private void ConvertToMeshButtonClicked(ClickEvent evt)
    {
        _terrainGenerationAPI.GenerateMount();
    }

    private void ShowPointsButtonClicked(ClickEvent evt)
    {
        _terrainGenerationAPI.GenerateWater();
    }

    private void CastButtonClicked(ClickEvent evt)
    {
        Debug.Log("[skh] GeneratorUIView.OnClick()");
        _startService.GenerateTerrain();
    }
}