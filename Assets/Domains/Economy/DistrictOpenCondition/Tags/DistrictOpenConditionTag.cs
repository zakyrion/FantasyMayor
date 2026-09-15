using EcsExtensions;
using Friflo.Engine.ECS;
namespace Domains.Economy.DistrictOpenCondition.Tags
{
    // Family label of the two district-open-condition archetypes (key: DistrictTypeFKComponent); each archetype's own main tag discriminates it.
    [TagLabel]
    public struct DistrictOpenConditionTag : ITag
    {
    }
}
