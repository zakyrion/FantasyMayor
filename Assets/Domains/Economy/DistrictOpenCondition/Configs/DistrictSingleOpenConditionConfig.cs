using UnityEngine;

namespace Domains.Economy.DistrictOpenCondition.Configs
{
    // Concrete condition: the gated district (DistrictType, from the base) is buildable only while zero instances
    // of it exist in the game (e.g. CityCenter — one per game). Carries no parameters of its own — the rule is
    // fully expressed by the gated district type plus its "single instance" kind.
    [CreateAssetMenu(
        fileName = "DistrictSingleOpenConditionConfig",
        menuName = "FantasyMayor/Districts/Conditions/DistrictSingleOpenConditionConfig")]
    public sealed class DistrictSingleOpenConditionConfig : DistrictOpenConditionConfig
    {
    }
}
