using Friflo.Engine.ECS;
namespace Domains.Actions.BuildDistrictAction.Components
{
    // Turns remaining + the district's original turn cost. Both stamped at confirm from
    // DistrictBuildCostConfig.TurnsToBuild (TurnsToBuild never changes after); BuildDistrictTurnTickSystem
    // decrements TurnsLeft once per turn, and BuildDistrictCompletionSystem materialises the District fact when
    // it reaches 0 (a TurnsToBuild of 0 completes on the next frame). TurnsLeft == TurnsToBuild doubles as the
    // "still the confirm turn" test for cancel refunds (BuildDistrictActionCancelSystem) — no separate turn
    // stamp needed. Value-only writes — never added/removed after confirm, so the per-turn decrement stays a
    // benign off-main-thread write. See Flows/FLOW_DISTRICT_BUILD.md.
    public struct BuildDistrictTurnsComponent : IComponent
    {
        public int TurnsLeft;
        public int TurnsToBuild;
    }
}
