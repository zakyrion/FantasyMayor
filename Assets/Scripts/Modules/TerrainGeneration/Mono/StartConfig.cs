using UnityEngine;

namespace Modules.TerrainGeneration.Mono
{
    [CreateAssetMenu(fileName = "StartConfig", menuName = "Configs/StartConfig")]
    public class StartConfig : ScriptableObject
    {
        [Header("Global")] [SerializeField] private uint _seed = 1234;

        [Header("Hex Generation")] [SerializeField]
        private int _waves = 1;

        [SerializeField]
        private bool _generateTerrainOnStart = true;
    }
}
