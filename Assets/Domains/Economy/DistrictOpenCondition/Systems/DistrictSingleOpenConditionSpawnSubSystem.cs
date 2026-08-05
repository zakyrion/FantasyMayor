using Friflo.Engine.ECS;
using Domains.Economy.Archetypes;
using Domains.Economy.District.Components;
using Domains.Economy.DistrictOpenCondition.Components;
using Domains.Economy.DistrictOpenCondition.Configs;
using Domains.Economy.DistrictOpenCondition.Data;
using EcsExtensions;
using JetBrains.Annotations;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Handles SingleDistrictOpenConditionConfig: creates one entity carrying the gated district type (FK), the
    // shared condition discriminator, and its kind + state columns (SingleOpen / Closed). No payload — the rule
    // has no params.
    [UsedImplicitly]
    internal sealed class DistrictSingleOpenConditionSpawnSubSystem : DistrictOpenConditionSpawnSubSystem
    {
        public override int Priority => SystemPriorities.SubSystems.DistrictOpenConditionSpawn.Single;

        private readonly Archetype _archetype;

        public DistrictSingleOpenConditionSpawnSubSystem(EntityStorages storages) : base(storages.World)
        {
            _archetype = EconomyArchetypes.OpenConditionSingle(storages.World);
        }

        public override bool TrySpawn(DistrictOpenConditionConfig config)
        {
            if (config is not DistrictSingleOpenConditionConfig singleConfig)
                return false;

            var entity = _archetype.CreateEntity();
            entity.AddComponent(new DistrictTypeFKComponent { Value = singleConfig.DistrictType });
            entity.AddComponent(new DistrictOpenConditionKindComponent { Value = DistrictOpenConditionKind.SingleOpen });
            entity.AddComponent(new DistrictOpenStateComponent { Value = DistrictOpenState.Closed });

            return true;
        }
    }
}
