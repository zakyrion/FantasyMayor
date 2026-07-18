using Domains.Economy.DistrictOpenCondition.Configs;
using Friflo.Engine.ECS;

namespace Domains.Economy.DistrictOpenCondition.Components
{
    // World component carrying a REFERENCE to the loaded DistrictOpenConditionsConfig SO (the conditions
    // catalogue). No copy/flatten — the SO holds the concrete condition references, consumed by the spawn
    // orchestrator at MapGenerationStep. DistrictOpenConditionsConfigLoaderSystem keeps the addressable Box
    // alive for the catalogue's lifetime and releases it on teardown. Mirrors DistrictsBuildConfigComponent.
    public readonly struct DistrictOpenConditionsConfigComponent : IComponent
    {
        public readonly DistrictOpenConditionsConfig Value;

        public DistrictOpenConditionsConfigComponent(DistrictOpenConditionsConfig value)
        {
            Value = value;
        }
    }
}
