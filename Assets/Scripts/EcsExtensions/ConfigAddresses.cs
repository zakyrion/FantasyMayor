namespace EcsExtensions
{
    /// <summary>
    ///     The single place that defines the addressable address of every config loaded by
    ///     <see cref="ConfigLoaderSystem{T}" />. Installers pass these constants to
    ///     <c>WithParameter("address", …)</c> instead of string literals. The value is the addressable entry name
    ///     authored in Unity — it does not always match the config type name.
    /// </summary>
    public static class ConfigAddresses
    {
        public const string CAMERA_MOVEMENT_CONFIG = "CameraMovementConfig";
        public const string CITY_CONFIG = "CityConfig";
        public const string CLAY_VIEW_CONFIG = "ClayViewConfig";
        public const string DISTRICT_BUILD_COSTS_CONFIG = "DistrictBuildCostsConfig";
        public const string DISTRICT_BUILD_OUTCOMES_CONFIG = "BuildDistrictOutcomesConfig";
        public const string DISTRICT_BUILD_PROGRESS_VIEWS_CONFIG = "DistrictBuildProgressViewsConfig";
        public const string DISTRICT_BUILDS_CONFIG = "DistrictBuildsConfig";
        public const string DISTRICT_ICON_CONFIG = "DistrictIconConfig";
        public const string DISTRICT_OPEN_CONDITIONS_CONFIG = "DistrictOpenConditionsConfig";
        public const string DISTRICT_VIEWS_CONFIG = "DistrictViewsConfig";
        public const string HEIGHT_SMOOTHING_CONFIG = "HeightSmoothingConfig";
        public const string HEX_ICONS_CONFIG = "HexIconsConfig";
        public const string HEX_RESOURCE_ICON_CONFIG = "HexResourceIconConfig";
        public const string HEX_RESOURCES_CONFIG = "HexResourcesConfig";
        public const string HEX_RESOURCES_VIEW_CONFIG = "HexResourcesViewConfig";
        public const string HEX_TERRAIN_ICON_CONFIG = "HexTerrainIconConfig";
        public const string HYDRAULIC_EROSION_CONFIG = "HydraulicErosionConfig";
        public const string INNER_ISOLINE_CONFIG = "InnerIsolineConfig";
        public const string INVENTORY_RESOURCE_ICON_CONFIG = "InventoryResourceIconConfig";
        public const string MAYOR_CONFIG = "MayorConfig";
        public const string OUTER_ISOLINE_CONFIG = "OuterIsolineConfig";
        public const string TERRAIN_GENERATION_CONFIG = "TerrainGenerationConfig";
        public const string TERRAIN_TEXTURE_CONFIG = "TerrainTextureConfig";
        public const string TERRAIN_VIEW_CONFIG = "TerrainViewConfig";
        public const string WATER_VIEW_CONFIG = "WaterViewConfig";
        public const string WIND_EROSION_CONFIG = "WindErosionConfig";
    }
}
