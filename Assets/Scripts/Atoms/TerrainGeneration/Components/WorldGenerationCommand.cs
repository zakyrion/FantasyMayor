using Unity.Entities;

namespace HW.Data.Components
{
    public struct WorldGenerationCommand : IComponentData
    {
        public int WaveCount;
        public uint Seed;
        public float WaterPercentage;
        public int MountsCount;
        public float HillsPercentage;
        public float HillsCount;
        public WorldGenerationStage Stage;
    }

    public enum WorldGenerationStage
    {
        Await,
        SpawnPoints,
        PlaceTerrainTypes,
        CreateHexes,
        DetailedHexes,
        Finish
    }
}