using UniRx;

/// <summary>
/// Represents a data layer class used to store seed data.
/// </summary>
public class SeedDataLayer : IDataContainer
{
    private readonly ReactiveProperty<uint> _seed = new(1);
    public ReadOnlyReactiveProperty<uint> Seed => _seed.ToReadOnlyReactiveProperty();

    public void SetSeed(uint seed)
    {
        _seed.Value = seed;
    }

    public void Dispose()
    {
        _seed?.Dispose();
    }
}