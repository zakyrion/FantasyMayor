namespace Domains.Actions.BuildDistrictAction.Components
{
    // Turns remaining until the committed build finishes. Seeded from BuildDistrictCostConfig.TurnsToBuild at
    // commit; a turn-phase system decrements it each turn and Sets ActionCompleteTag at zero (Step 4).
    public struct ActionTurnLeftComponent
    {
        public int Value;
    }
}
