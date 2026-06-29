namespace Domains.Map.Hex.Data
{
    /// <summary>
    ///     Terrain kind of a hex. Carried on the hex row by HexTypeComponent (one column, group-queryable
    ///     via AsMultiMap&lt;HexTypeComponent&gt;); also the lookup key into the terrain-icon config.
    /// </summary>
    public enum HexType
    {
        Unknown = 0,
        Plain = 1,
        Mount = 2,
        Bedhill = 3,
        Water = 4,
    }
}
