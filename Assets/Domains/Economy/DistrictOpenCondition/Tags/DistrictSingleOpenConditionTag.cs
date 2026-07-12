namespace Domains.Economy.DistrictOpenCondition.Tags
{
    // Kind marker on a condition entity: this condition is "single instance" (gated district buildable only
    // while zero of it exist). No payload — the rule has no parameters beyond the entity's DistrictTypeComponent.
    public struct DistrictSingleOpenConditionTag
    {
    }
}
