using Domains.Map.Hex.Components;
using Friflo.Engine.ECS;
using Presentation.Districts.Components;
using Presentation.Districts.Tags;
using Presentation.HexIcons.Components;
using Presentation.HexIcons.Tags;
using Presentation.HexResources.Components;
using Presentation.HexResources.Tags;
using Presentation.Terrain.Components;
using Presentation.Terrain.Tags;

namespace Presentation.Archetypes
{
    /// <summary>
    ///     Resolves the archetypes owned by the Presentation assembly — the world/scene view rows. Scope is
    ///     the ASSEMBLY, not a feature. Each method returns the live <see cref="Archetype" /> — entities are
    ///     created BY it and iterated THROUGH it; the caller keeps it in its own field. This holder stores
    ///     nothing.
    /// </summary>
    public static class PresentationArchetypes
    {
        /// <summary>The single "a hex is selected" row — created and destroyed on click.</summary>
        public static Archetype HexSelection(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<HexSelectedComponent>(),
                Tags.Get<HexSelectionTag>());

        /// <summary>A planted forest view, FK-keyed to its hex.</summary>
        public static Archetype ForestView(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<HexIdFKComponent, ForestViewComponent>(),
                Tags.Get<ForestViewTag>());

        /// <summary>A built district's view, FK-keyed to its hex.</summary>
        public static Archetype DistrictView(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<HexIdFKComponent, DistrictViewComponent>(),
                Tags.Get<DistrictViewTag>());

        /// <summary>The under-construction view of a district, FK-keyed to its hex.</summary>
        public static Archetype DistrictBuildProgressView(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<HexIdFKComponent, DistrictBuildProgressViewComponent>(),
                Tags.Get<DistrictBuildProgressViewTag>());

        /// <summary>The per-hex icon container, FK-keyed to its hex.</summary>
        public static Archetype HexIconContainer(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<HexIdFKComponent, HexIconContainerComponent>(),
                Tags.Get<HexIconContainerTag>());

        /// <summary>The single terrain view row.</summary>
        public static Archetype TerrainView(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<TerrainViewComponent>(),
                Tags.Get<TerrainViewTag>());

        /// <summary>The single water view row.</summary>
        public static Archetype WaterView(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<WaterViewComponent>(),
                Tags.Get<WaterViewTag>());

        /// <summary>The single hex-selection highlight view row.</summary>
        public static Archetype HexSelectionView(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<HexSelectionViewComponent>(),
                Tags.Get<HexSelectionViewTag>());
    }
}
