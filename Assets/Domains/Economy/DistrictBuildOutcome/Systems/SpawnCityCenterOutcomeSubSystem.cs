using System.Threading;
using Cysharp.Threading.Tasks;
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
    // outcome kind; the host routes by walking the catalogue and letting each part spawn its own kind.
    [UsedImplicitly]
    internal sealed class SpawnCityCenterOutcomeSubSystem : DistrictBuildOutcomeSpawnSubSystem
    {
        public override int Priority => SystemPriorities.SubSystems.DistrictBuildOutcomeSpawn.CityCenter;

        private readonly Archetype _archetype;

        public SpawnCityCenterOutcomeSubSystem(EntityStorages storages) : base(storages)
        {
            _archetype = EconomyArchetypes.BuildOutcome(storages.World);
        }

        // Walks the whole catalogue and spawns only its own kind — another entry is another part's to handle.
        public override UniTask Update(CancellationToken cancellationToken)
        {
            var outcomes = Storages.Get<DistrictBuildOutcomesConfig>().Outcomes;

            for (var index = 0; index < outcomes.Length; index++)
            {
                if (outcomes[index] is not SpawnCityCenterOutcomeConfig cityCenterConfig)
                    continue;

                var entity = _archetype.CreateEntity();
                entity.AddComponent(new DistrictTypeFKComponent { Value = cityCenterConfig.DistrictType });
                entity.AddComponent(new DistrictBuildOutcomeKindComponent { Value = DistrictBuildOutcomeKind.SpawnCityCenter });
            }

            return UniTask.CompletedTask;
        }
    }
}
