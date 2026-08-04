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
    // Handles DistrictExistConditionConfig: creates one entity carrying the gated district type (FK), the
    // required-district payload, the condition discriminator tag, and its kind + state columns (Exist / Closed).
    [UsedImplicitly]
    internal sealed class DistrictExistConditionSpawnSubSystem : DistrictOpenConditionSpawnSubSystem
    {
        public override int Priority => SystemPriorities.SubSystems.DistrictOpenConditionSpawn.Exist;

        private readonly Archetype _archetype;

        public DistrictExistConditionSpawnSubSystem(EntityStore world) : base(world)
        {
            _archetype = EconomyArchetypes.OpenConditionExist(world);
        }

        public override bool TrySpawn(DistrictOpenConditionConfig config)
        {
            if (config is not DistrictExistConditionConfig existConfig)
                return false;

            var entity = _archetype.CreateEntity();
            entity.AddComponent(new DistrictTypeFKComponent { Value = existConfig.DistrictType });
            entity.AddComponent(new DistrictExistConditionComponent { RequiredDistrict = existConfig.RequiredDistrict });
            entity.AddComponent(new DistrictOpenConditionKindComponent { Value = DistrictOpenConditionKind.Exist });
            entity.AddComponent(new DistrictOpenStateComponent { Value = DistrictOpenState.Closed });

            return true;
        }
    }
}
