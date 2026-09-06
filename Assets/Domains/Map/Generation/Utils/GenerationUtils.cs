using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;

namespace Domains.Map.Generation.Utils
{
    public static class GenerationUtils
    {
        public static HexTypeComponent ToHexType(this HexLevelComponent levelComponent)
        {
            return new HexTypeComponent
            {
                Type = levelComponent.Level switch
                {
                    2 => HexType.Mount,
                    1 => HexType.Bedhill,
                    0 => HexType.Plain,
                    _ => HexType.Water
                }
            };
        }
    }
}
