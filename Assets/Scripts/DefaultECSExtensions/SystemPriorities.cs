namespace DefaultECSExtensions
{
    /// <summary>
    ///     The single place that defines system execution order. Each system's <c>Priority</c> reads one constant
    ///     from here instead of a local <c>ExecutionPriority</c>, so a whole sequence can be re-ordered in one file.
    ///     Values are a pure lift of the former per-system constants — nothing was renumbered. Each nested class is
    ///     an independent priority space (ascending order within it); values are only comparable inside the same space.
    /// </summary>
    public static class SystemPriorities
    {
        /// <summary>
        ///     World-init pipeline stages (<c>MapCreation</c>): the ascending order in which the world is built once.
        /// </summary>
        public static class WorldInit
        {
            public const int Generation = 100;
            public const int HexResources = 200;
            public const int TerrainView = 300;
            public const int HexResourcesView = 400;
            public const int HexSelectionViewLoading = 500;
            public const int TerrainViewDebug = 600;
            public const int HexIconsSpawn = 700;
            public const int MainHudSpawn = 800;
            public const int DistrictBuildUiSpawn = 810;
            public const int CitySpawn = 900;
            public const int MayorSpawn = 910;
            public const int DistrictOpenConditionSpawn = 920;
            public const int DistrictOpenConditionEvaluatorBootstrap = 925;
            public const int DistrictBuildOutcomeSpawn = 930;
        }

        /// <summary>
        ///     Gameplay per-frame + reactive tick order. Reactive systems must sit below <see cref="EventCleanup" />
        ///     so their pulse is consumed the tick it is raised. Every system gets a distinct value — even two
        ///     systems with no known dependency today — so ordering stays explicit and a later dependency between
        ///     them is never silently order-agnostic.
        /// </summary>
        public static class RuntimeTick
        {
            public const int Camera = 0;
            public const int HexSelection = 1;
            public const int HexSelectionView = 501; // historically HexSelectionViewLoading + 1
            public const int HexInfoPanel = 550;
            public const int HexInfoPanelHeader = 560;
            public const int ContextTabSelection = 561;
            public const int ContextTabsAvailability = 562;
            public const int HexInfoPanelResources = 563;
            public const int ResourceBar = 565;
            public const int DistrictBuildUi = 566;
            public const int BuildDistrictAction = 600; // raises DistrictTableChangedEvent{Planned} on confirm
            public const int DistrictBuildProgressViewSpawn = 601; // > BuildDistrictAction (600): sees its {Planned} pulse the same tick, spawns the progress view for the just-created District row
            public const int BuildDistrictActionCancel = 602; // > DistrictBuildProgressViewSpawn (601), < DistrictBuildProgressViewDespawn (604): consumes BuildDistrictCancelEvent, disposes both rows and raises DistrictTableChangedEvent{Removed} in the SAME frame the views reconcile
            public const int BuildDistrictCompletion = 603; // > BuildDistrictActionCancel (602), < DistrictBuildProgressViewDespawn (604): consume the completed pulse, flip the District row to Built + raise DistrictTableChangedEvent{Built} in the SAME frame the views reconcile
            public const int ForestSpawn = 599; // moved off 601 to free that slot for BuildDistrictCompletion; forest ordering is independent of the district-build chain
            public const int DistrictBuildProgressViewDespawn = 604; // > BuildDistrictActionCancel (602) and BuildDistrictCompletion (603): sees their {Removed}/{Built} pulses the same tick the District row stops being Planned
            public const int DistrictViewSpawn = 605; // > BuildDistrictCompletion (603): sees its {Built} pulse the same tick
            public const int ForestDespawn = 606;
            public const int HexIconsContainerPosition = 700; // > Camera (0): re-project after the camera moves this frame
            public const int HexIconsVisibility = 800;
            public const int TurnProcessor = 1000;
            public const int TurnCount = 1010;
            public const int TurnPanelView = 1020; // > TurnCount (1010): reads TurnProcessorComponent/TurnCountComponent every frame — must run after both write
            public const int HexInfoPanelDistrict = 1030; // > TurnProcessor (1000): reacts to EITHER SelectedHexChangedEvent (produced at 501), TurnCompletedEvent (produced at 1000), OR DistrictTableChangedEvent (produced at 600/602/603) — must sit above all three producers to see any pulse the same frame
            public const int EventCleanup = int.MaxValue; // always last: disposes the frame's event entities
        }

        /// <summary>
        ///     Turn-phase order (<c>TurnPhaseSubSystem</c>), run per turn on a <c>NextTurnEvent</c> pulse —
        ///     i.e. AFTER the player finished acting, so the runtime order is NOT the presentation order.
        ///     Planned bands (ascending): Citizen → Resolution → Upkeep → Consequences → Preview at the TAIL —
        ///     Preview computes the snapshot the player reads at the start of the NEXT Mayor Phase.
        ///     The Mayor Phase itself is NOT a phase subsystem: it is the interactive player↔game layer running
        ///     as ordinary Gameplay systems; its "end turn" action is what raises the pulse. Everything here is
        ///     non-interactive computation — that is exactly WHY it may run off the main thread; a phase needing
        ///     player input mid-run would break the model and must be designed separately. The FIRST turn starts
        ///     on an EMPTY snapshot by design: Preview only ever runs as the pipeline tail — do NOT add a
        ///     bootstrap/startup preview pass.
        /// </summary>
        public static class TurnPhase
        {
            public const int MayorApRestore = 500; // Upkeep band
            public const int BuildDistrictTurnTick = 510; // Upkeep band: count down in-progress build countdowns each turn
            public const int DistrictOpenConditionEvaluator = 1000; // tail of the turn pipeline (Preview band)
        }

        /// <summary>
        ///     Per-orchestrator subsystem spaces. Each nested class is one orchestrator's own ascending order;
        ///     values are independent across orchestrators.
        /// </summary>
        public static class SubSystems
        {
            public static class Generation
            {
                public const int River = 200;
                public const int Lake = 210;
                public const int Sea = 220;
                public const int Mountain = 300;
            }

            public static class HexResourceGeneration
            {
                public const int Forest = 100;
                public const int Clay = 200;
                public const int Fish = 300;
            }

            public static class TerrainView
            {
                public const int Generation = 100;
                public const int Texture = 200;
                public const int Water = 300;
            }

            public static class HexResourceView
            {
                public const int Clay = 200;
                public const int Fish = 300;
                public const int Forest = 400;
            }

            public static class MainHudSpawn
            {
                public const int HexInfoPanel = 0;
                public const int TurnPanel = 10;
                public const int ContextTabs = 20;
                public const int ResourceBar = 30;
            }

            public static class DistrictBuildUi
            {
                public const int List = 100;
                public const int HexResources = 200;
                public const int Price = 300;
                public const int Actions = 400;
            }

            public static class DistrictOpenConditionSpawn
            {
                public const int Exist = 100;
                public const int Single = 200;
            }

            public static class DistrictOpenConditionEvaluator
            {
                public const int Single = 100;
            }

            public static class DistrictBuildOutcomeSpawn
            {
                public const int CityCenter = 200;
            }
        }
    }
}
