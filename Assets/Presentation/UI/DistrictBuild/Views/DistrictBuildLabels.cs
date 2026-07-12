using System.Collections.Generic;
using System.Text;
using Domains.Economy.District.Data;
using Domains.Economy.Resource.Data;
using Domains.Kernel.Data;
using Domains.Map.Hex.Data;
using Domains.Map.HexResources.Data;

namespace Presentation.UI.DistrictBuild.Views
{
    /// <summary>
    ///     Ukrainian label + emoji-icon maps for the district-build overlay, shared by the section binders.
    ///     Stateless and field-less — a pure lookup, so a static class is justified.
    /// </summary>
    internal static class DistrictBuildLabels
    {
        internal static string DistrictName(DistrictType type) => type switch
        {
            DistrictType.CityCenter => "Міський центр",
            DistrictType.Farm => "Ферма",
            _ => type.ToString()
        };

        internal static string DistrictIcon(DistrictType type) => type switch
        {
            DistrictType.CityCenter => "🏛️",
            DistrictType.Farm => "🌾",
            _ => "🏗️"
        };

        internal static string HexResourceLabel(HexResourceType type) => type switch
        {
            HexResourceType.Forest => "Ліс",
            HexResourceType.Clay => "Глина",
            HexResourceType.Fish => "Риба",
            _ => type.ToString()
        };

        // Comma-joined terrain labels for a requirement line. View-layer string work (not a system) — managed
        // StringBuilder is fine here.
        internal static string HexTypeLabels(List<HexType> types)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < types.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(HexTypeLabel(types[i]));
            }

            return builder.ToString();
        }

        internal static string HexTypeLabel(HexType type) => type switch
        {
            HexType.Plain => "Рівнина",
            HexType.Mount => "Гора",
            HexType.Bedhill => "Пагорб",
            HexType.Water => "Вода",
            _ => type.ToString()
        };

        internal static string ResourceLabel(ResourceType type) => type switch
        {
            ResourceType.Grain => "Зерно",
            ResourceType.Clay => "Глина",
            ResourceType.Wood => "Колоди",
            ResourceType.RawMeat => "Сире м'ясо",
            ResourceType.RawFish => "Сира риба",
            ResourceType.SmokedMeat => "Копчене м'ясо",
            ResourceType.SmokedFish => "Копчена риба",
            _ => type.ToString()
        };

        internal static string ResourceIcon(ResourceType type) => type switch
        {
            ResourceType.Grain => "🌾",
            ResourceType.Clay => "🧱",
            ResourceType.Wood => "🪵",
            ResourceType.RawMeat => "🥩",
            ResourceType.RawFish => "🐟",
            ResourceType.SmokedMeat => "🍖",
            ResourceType.SmokedFish => "🐠",
            _ => "📦"
        };

        internal static string OwnerLabel(ActorType owner) => owner switch
        {
            ActorType.Mayor => "Мер",
            ActorType.City => "Місто",
            _ => owner.ToString()
        };

        internal static string OwnerIcon(ActorType owner) => owner switch
        {
            ActorType.Mayor => "👑",
            ActorType.City => "🏛️",
            _ => "👤"
        };
    }
}
