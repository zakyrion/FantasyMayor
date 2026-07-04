namespace Domains.Actions.BuildDistrictAction.Components
{
    // AP price snapshot on a build-action entity, copied from BuildDistrictCostConfig.ApPrice at commit (Step 4).
    // Deducted from the owner on commit; kept on the row so the committed action is self-contained.
    public struct ActionAPCostComponent
    {
        public int Value;
    }
}
