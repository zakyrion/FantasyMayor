namespace Domains.Map.Hex.Data
{
    /// <summary>
    ///     Terrain kind of a hex. Carried on the hex row by HexTypeComponent (one column, group-queryable
    ///     via AsMultiMap&lt;HexTypeComponent&gt;); also the lookup key into the terrain-icon config.
    /// </summary>
    public enum HexType
    {
        Plain = 0,
        Mount = 1,
        Bedhill = 2,
        Water = 3,
    }
}
