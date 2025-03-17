using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

public class StartService : MonoBehaviour
{
    [Header("Global")] [SerializeField] private uint _seed = 1234;

    [Header("Hex Generation")] [SerializeField]
    private int _waves = 1;

    [FormerlySerializedAs("_sendStartSignal")] [SerializeField]
    private bool _generateTerrainOnStart = true;

    private ITerrainGenerationAPI _terrainGenerationAPI;

    private void Start()
    {
        if (_generateTerrainOnStart) GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        _terrainGenerationAPI.CreateTerrainVectorField(_waves);
    }

    [Inject]
    private void Init(ITerrainGenerationAPI terrainGenerationAPI, SeedDataLayer seedDataLayer)
    {
        _terrainGenerationAPI = terrainGenerationAPI;
        seedDataLayer.SetSeed(_seed);
    }
}