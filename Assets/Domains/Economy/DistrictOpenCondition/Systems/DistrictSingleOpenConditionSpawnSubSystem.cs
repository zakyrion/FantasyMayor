using Friflo.Engine.ECS;
using Domains.Economy.District.Components;
using Domains.Economy.DistrictOpenCondition.Components;
using Domains.Economy.DistrictOpenCondition.Configs;
using Domains.Economy.DistrictOpenCondition.Data;
using Domains.Economy.DistrictOpenCondition.Tags;
using JetBrains.Annotations;
using EcsExtensions;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Handles SingleDistrictOpenConditionConfig: creates one entity carrying the gated district type (FK), the
    // shared condition discriminator, and its kind + state columns (SingleOpen / Closed). No payload — the rule
    // has no params.
    [UsedImplicitly]
    internal sealed class DistrictSingleOpenConditionSpawnSubSystem : DistrictOpenConditionSpawnSubSystem
    {
        public override int Priority => SystemPriorities.SubSystems.DistrictOpenConditionSpawn.Single;

        public DistrictSingleOpenConditionSpawnSubSystem(EntityStore world) : base(world)
        {
        }

        public override bool TrySpawn(DistrictOpenConditionConfig config)
        {
            if (config is not DistrictSingleOpenConditionConfig singleConfig)
                return false;

            var entity = World.CreateEntity();
            entity.AddComponent(new DistrictTypeFKComponent { Value = singleConfig.DistrictType });
            entity.AddTag<DistrictOpenConditionTag>();
            entity.AddComponent(new DistrictOpenConditionKindComponent { Value = DistrictOpenConditionKind.SingleOpen });
            entity.AddComponent(new DistrictOpenStateComponent { Value = DistrictOpenState.Closed });

            return true;
        }
    }
}
