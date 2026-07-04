namespace Domains.Actions.BuildDistrictAction.Tags
{
    // Marks a committed build action whose countdown reached zero — "completed; apply the outcome now". Step 5
    // reacts to it, joins the completed action's DistrictType into the BuildDistrictOutcome table, and disposes
    // the action entity.
    public struct ActionCompleteTag
    {
    }
}
