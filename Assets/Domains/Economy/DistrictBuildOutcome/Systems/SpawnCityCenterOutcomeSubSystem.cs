using Friflo.Engine.ECS;
using Domains.Economy.Archetypes;
using Domains.Economy.District.Components;
using EcsExtensions;
using JetBrains.Annotations;
using Domains.Economy.DistrictBuildOutcome.Components;
using Domains.Economy.DistrictBuildOutcome.Configs;
using Domains.Economy.DistrictBuildOutcome.Data;

namespace Domains.Economy.DistrictBuildOutcome.Systems{
    // Handles SpawnCityCenterOutcomeConfig: creates the City Center outcome row — the gated district type (FK),
    // the shared outcome discriminator, and the City-Center spawn kind column. One subsystem per district-specific
    // outcome kind; the orchestrator routes each authored config to the subsystem matching its concrete type.
    [UsedImplicitly]
    internal sealed class SpawnCityCenterOutcomeSubSystem : DistrictBuildOutcomeSpawnSubSystem
    {
        public override int Priority => SystemPriorities.SubSystems.DistrictBuildOutcomeSpawn.CityCenter;

        private readonly Archetype _archetype;

        public SpawnCityCenterOutcomeSubSystem(EntityStore world) : base(world)
        {
            _archetype = EconomyArchetypes.BuildOutcome(world);
        }

        public override bool TrySpawn(DistrictBuildOutcomeConfig config)
        {
            if (config is not SpawnCityCenterOutcomeConfig cityCenterConfig)
                return false;

            var entity = _archetype.CreateEntity();
            entity.AddComponent(new DistrictTypeFKComponent { Value = cityCenterConfig.DistrictType });
            entity.AddComponent(new DistrictBuildOutcomeKindComponent { Value = DistrictBuildOutcomeKind.SpawnCityCenter });

            return true;
        }
    }
}
