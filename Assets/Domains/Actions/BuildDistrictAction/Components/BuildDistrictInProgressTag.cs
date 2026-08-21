using Friflo.Engine.ECS;
namespace Domains.Actions.BuildDistrictAction.Components
{
    // Marks a district-build action entity while the build is IN PROGRESS (the player confirmed «Збудувати»,
    // the district is not yet a fact). Created on DistrictBuildConfirmedEvent and carried until completion, when
    // BuildDistrictCompletionSystem materialises the Economy District fact (DistrictTag) and disposes this entity.
    // The verb entity is transient; the persistent record is the District fact — see Flows/FLOW_DISTRICT_BUILD.md.
    public struct BuildDistrictInProgressTag : ITag
    {
    }
}
