public class WaterGeneratorDataLayer : IDataContainer
{
    public enum WaterType
    {
        River,
        Lake
    }

    public WaterType Type { get; set; }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}