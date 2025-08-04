public interface ITerrainGenerationAPI : IAPI
{
    void CreateTerrainVectorField(int waves);
    void GenerateMount();

    void Apply();
}