using System.Threading;
using Cysharp.Threading.Tasks;
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
    // Handles SingleDistrictOpenConditionConfig: creates one entity carrying the gated district type (FK), its
    // archetype's main tag beside the family label, and its kind + state columns (SingleOpen / Closed). No payload —
    // the rule has no params.
    [UsedImplicitly]
    internal sealed class DistrictSingleOpenConditionSpawnSubSystem : DistrictOpenConditionSpawnSubSystem
    {
        public override int Priority => SystemPriorities.SubSystems.DistrictOpenConditionSpawn.Single;

        private readonly Archetype _archetype;

        public DistrictSingleOpenConditionSpawnSubSystem(EntityStorages storages) : base(storages)
        {
            _archetype = EconomyArchetypes.OpenConditionSingle(storages.World);
        }

        // Walks the whole catalogue and spawns only its own kind — another entry is another part's to handle.
        public override UniTask Update(CancellationToken cancellationToken)
        {
            var conditions = Storages.Get<DistrictOpenConditionsConfig>().Conditions;

            for (var index = 0; index < conditions.Length; index++)
            {
                if (conditions[index] is not DistrictSingleOpenConditionConfig singleConfig)
                    continue;

                var entity = _archetype.CreateEntity();
                entity.AddComponent(new DistrictTypeFKComponent { Value = singleConfig.DistrictType });
                entity.AddComponent(new DistrictOpenConditionKindComponent { Value = DistrictOpenConditionKind.SingleOpen });
                entity.AddComponent(new DistrictOpenStateComponent { Value = DistrictOpenState.Closed });
            }

            return UniTask.CompletedTask;
        }
    }
}
