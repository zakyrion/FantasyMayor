using DefaultEcs;
using Domains.Economy.District.Components;
using Domains.Economy.DistrictOpenCondition.Configs;
using Domains.Economy.DistrictOpenCondition.Tags;
using JetBrains.Annotations;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Handles SingleDistrictOpenConditionConfig: creates one entity carrying the gated district type (FK), the
    // shared condition discriminator, and the single-instance kind marker. No payload — the rule has no params.
    [UsedImplicitly]
    internal sealed class DistrictSingleOpenConditionSpawnSubSystem : DistrictOpenConditionSpawnSubSystem
    {
        private const int ExecutionPriority = 200;

        public override int Priority => ExecutionPriority;

        public DistrictSingleOpenConditionSpawnSubSystem(World world) : base(world)
        {
        }

        public override bool TrySpawn(DistrictOpenConditionConfig config)
        {
            if (config is not DistrictSingleOpenConditionConfig singleConfig)
                return false;

            var entity = World.CreateEntity();
            entity.Set(new DistrictTypeComponent { Value = singleConfig.DistrictType });
            entity.Set(new DistrictOpenConditionTag());
            entity.Set(new DistrictSingleOpenConditionTag());

            return true;
        }
    }
}
