namespace Domains.Actions.BuildDistrictAction.Components
{
    // Marks a committed build-district action entity (the player confirmed «Збудувати»). Set when the entity is
    // created on DistrictBuildConfirmedEvent; the build mechanics (spend, turns-left, …) act on it from here.
    public struct BuildDistrictActionTag
    {
    }
}
