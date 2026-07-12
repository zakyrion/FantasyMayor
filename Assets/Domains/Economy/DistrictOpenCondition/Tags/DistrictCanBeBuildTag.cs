namespace Domains.Economy.DistrictOpenCondition.Tags
{
    // Read-only discriminator: present on an open-condition entity whose gated district is currently buildable.
    // The DistrictBuild list reads entities carrying it; a future condition evaluator owns attaching/removing it.
    public struct DistrictCanBeBuildTag
    {
    }
}
