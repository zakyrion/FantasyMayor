using Domains.Economy.DistrictBuildOutcome.Configs;
using Friflo.Engine.ECS;

namespace Domains.Economy.DistrictBuildOutcome.Components{
    // Singleton component carrying a REFERENCE to the loaded BuildDistrictOutcomesConfig SO (the outcomes catalogue).
    // No copy/flatten — the SO holds the concrete outcome references, consumed by the spawn orchestrator at
    // MapGenerationStep. BuildDistrictOutcomesConfigLoaderSystem keeps the addressable Box alive for the
    // catalogue's lifetime and releases it on teardown. Mirrors DistrictOpenConditionsConfigComponent.
    public readonly struct DistrictBuildOutcomesConfigComponent : IComponent
    {
        public readonly DistrictBuildOutcomesConfig Value;

        public DistrictBuildOutcomesConfigComponent(DistrictBuildOutcomesConfig value)
        {
            Value = value;
        }
    }
}
