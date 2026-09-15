using Friflo.Engine.ECS;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Tags;
using Presentation.UI.MainHud.HexInfoPanel.Components;
using Presentation.UI.MainHud.HexInfoPanel.Tags;
using Presentation.UI.MainHud.ResourceBar.Components;
using Presentation.UI.MainHud.ResourceBar.Tags;
using Presentation.UI.MainHud.TurnPanel.Components;
using Presentation.UI.MainHud.TurnPanel.Tags;
using Presentation.UI.Tags;
// Presentation.UI.Tags (our namespace) shadows Friflo's Tags type inside Presentation.UI.* — alias it.
using EcsTags = Friflo.Engine.ECS.Tags;

namespace Presentation.UI.Archetypes
{
    /// <summary>
    ///     Resolves the archetypes owned by the Presentation.UI assembly. Scope is the ASSEMBLY, not a
    ///     feature. Each method returns the live <see cref="Archetype" /> — entities are created BY it and
    ///     iterated THROUGH it; the caller keeps it in its own field. This holder stores nothing.
    ///     Each HUD panel carries its own main tag; <see cref="UITag" /> stands beside it as the label of HUD
    ///     membership and is never a filter.
    /// </summary>
    public static class PresentationUIArchetypes
    {
        public static Archetype HexInfoPanel(EntityStore store) =>
            store.GetArchetype(ComponentTypes.Get<HexInfoPanelViewComponent>(), EcsTags.Get<HexInfoPanelTag, UITag>());

        public static Archetype TurnPanel(EntityStore store) =>
            store.GetArchetype(ComponentTypes.Get<TurnPanelViewComponent>(), EcsTags.Get<TurnPanelTag, UITag>());

        public static Archetype ResourceBar(EntityStore store) =>
            store.GetArchetype(ComponentTypes.Get<ResourceBarViewComponent>(), EcsTags.Get<ResourceBarTag, UITag>());

        public static Archetype DistrictBuildUI(EntityStore store) =>
            store.GetArchetype(ComponentTypes.Get<DistrictBuildUIViewComponent>(), EcsTags.Get<DistrictBuildUITag, UITag>());

        /// <summary>
        ///     The overlay's single selection row. Its column is a birth column: <c>CreateEntity()</c>
        ///     provides it at <c>DistrictType.Unknown</c> ("nothing picked yet"), so readers that require a
        ///     real pick check that sentinel instead of the column's presence.
        /// </summary>
        public static Archetype DistrictBuildSelection(EntityStore store) =>
            store.GetArchetype(
                ComponentTypes.Get<DistrictBuildSelectionComponent>(),
                EcsTags.Get<DistrictBuildSelectionTag>());
    }
}
