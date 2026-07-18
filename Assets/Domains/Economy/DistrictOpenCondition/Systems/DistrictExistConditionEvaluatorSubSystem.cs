using DefaultEcs;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Tags;
using Domains.Economy.DistrictOpenCondition.Components;
using Domains.Economy.DistrictOpenCondition.Data;
using Domains.Economy.DistrictOpenCondition.Tags;
using JetBrains.Annotations;
using DefaultECSExtensions;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Evaluates DistrictOpenConditionKind.Exist-kind conditions: the gated district type is buildable only once
    // its own REQUIRED district type stands Built — a district still Planned does not count (user: «лише якщо
    // він збудований»). Change-only writes DistrictOpenStateComponent (Closed/Buildable).
    [UsedImplicitly]
    internal sealed class DistrictExistConditionEvaluatorSubSystem : DistrictOpenConditionEvaluatorSubSystem
    {
        // Declarative query caches (Table Rule): every condition row keyed by its kind column, and every
        // District row keyed by its type (District table's legal self-index) — stage is read per-row below,
        // since the self-index cannot key on two columns at once.
        private readonly EntityMultiMap<DistrictOpenConditionKindComponent> _conditionsByKind;
        private readonly EntityMultiMap<DistrictTypeComponent> _districtsByType;

        public override int Priority => SystemPriorities.SubSystems.DistrictOpenConditionEvaluator.Exist;

        public DistrictExistConditionEvaluatorSubSystem(World world) : base(world)
        {
            _conditionsByKind = world.GetEntities()
                .With<DistrictOpenConditionTag>()
                .AsMultiMap<DistrictOpenConditionKindComponent>();

            _districtsByType = world.GetEntities()
                .With<DistrictTag>()
                .With<DistrictBuildStateComponent>()
                .AsMultiMap<DistrictTypeComponent>();
        }

        public override void Evaluate()
        {
            if (!_conditionsByKind.TryGetEntities(
                    new DistrictOpenConditionKindComponent { Value = DistrictOpenConditionKind.Exist },
                    out var conditions))
                return;

            foreach (var condition in conditions)
            {
                var requiredType = condition.Get<DistrictExistConditionComponent>().RequiredDistrict;
                var targetState = HasBuiltDistrictOfType(requiredType)
                    ? DistrictOpenState.Buildable
                    : DistrictOpenState.Closed;

                if (condition.Get<DistrictOpenStateComponent>().Value != targetState)
                    condition.Set(new DistrictOpenStateComponent { Value = targetState });
            }
        }

        private bool HasBuiltDistrictOfType(DistrictType districtType)
        {
            if (!_districtsByType.TryGetEntities(new DistrictTypeComponent { Value = districtType }, out var rows))
                return false;

            foreach (var row in rows)
                if (row.Get<DistrictBuildStateComponent>().Value == DistrictBuildState.Built)
                    return true;

            return false;
        }

        public override void Dispose()
        {
            _conditionsByKind.Dispose();
            _districtsByType.Dispose();
        }
    }
}
