namespace Domains.Actions.BuildDistrictAction.Components
{
    // Turns remaining until an in-progress district build completes. Stamped at confirm from the district's
    // DistrictBuildCostConfig.TurnsToBuild; BuildDistrictTurnTickSystem decrements it once per turn, and
    // BuildDistrictCompletionSystem materialises the District fact when it reaches 0 (a TurnsToBuild of 0
    // completes on the next frame). Value-only writes — never added/removed after confirm, so the per-turn
    // decrement stays a benign off-main-thread write. See Flows/FLOW_DISTRICT_BUILD.md.
    public struct BuildDistrictTurnsLeftComponent
    {
        public int Value;
    }
}
