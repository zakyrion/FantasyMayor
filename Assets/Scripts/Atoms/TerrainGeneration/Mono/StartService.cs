using System.Threading;
using Core.DataLayer;
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
        if (_generateTerrainOnStart)
            GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        _terrainGenerationAPI.CreateTerrainVectorField(_waves);
    }

    [Inject]
    private void Init(ITerrainGenerationAPI terrainGenerationAPI, IDataContainer<SeedDataLayer> seedDataLayer)
    {
        _terrainGenerationAPI = terrainGenerationAPI;
        seedDataLayer.AddOrUpdateAsync(new SeedDataLayer { Seed = _seed }, CancellationToken.None);
    }
}
