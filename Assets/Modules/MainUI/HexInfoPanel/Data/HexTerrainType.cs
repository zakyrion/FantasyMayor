namespace Modules.MainUI.HexInfoPanel.Data
{
    /// <summary>
    ///     Terrain kinds shown by the hex info panel header. Mirrors the data-less terrain tags on hex
    ///     entities (HexPlainTag / HexMountTag / HexBedhillTag / HexWaterTag); used only as the lookup key
    ///     into the terrain-icon config.
    /// </summary>
    public enum HexTerrainType
    {
        Plain = 0,
        Mount = 1,
        Bedhill = 2,
        Water = 3,
    }
}
