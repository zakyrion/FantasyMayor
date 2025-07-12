using Unity.Mathematics;

/// <summary>
///     Provides data for generating mounts.
/// </summary>
public class MountsGeneratorDataLayer
{
    public int SpotCount { get; set; }

    public int2 SpotSizeRange { get; set; }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}
