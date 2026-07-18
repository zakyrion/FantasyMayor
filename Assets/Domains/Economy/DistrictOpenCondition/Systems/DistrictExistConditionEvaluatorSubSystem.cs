using Friflo.Engine.ECS;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Tags;
using Domains.Economy.DistrictOpenCondition.Components;
using Domains.Economy.DistrictOpenCondition.Data;
using Domains.Economy.DistrictOpenCondition.Tags;
using JetBrains.Annotations;
using EcsExtensions;

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
        private readonly ComponentIndex<DistrictOpenConditionKindComponent, DistrictOpenConditionKind> _conditionsByKind;
        private readonly ComponentIndex<DistrictTypeComponent, DistrictType> _districtsByType;

        public override int Priority => SystemPriorities.SubSystems.DistrictOpenConditionEvaluator.Exist;

        public DistrictExistConditionEvaluatorSubSystem(EntityStore world) : base(world)
        {
            _conditionsByKind = world.ComponentIndex<DistrictOpenConditionKindComponent, DistrictOpenConditionKind>();
            _districtsByType = world.ComponentIndex<DistrictTypeComponent, DistrictType>();
        }

        public override void Evaluate()
        {
            foreach (var condition in _conditionsByKind[DistrictOpenConditionKind.Exist])
            {
                var requiredType = condition.GetComponent<DistrictExistConditionComponent>().RequiredDistrict;
                var targetState = HasBuiltDistrictOfType(requiredType)
                    ? DistrictOpenState.Buildable
                    : DistrictOpenState.Closed;

                if (condition.GetComponent<DistrictOpenStateComponent>().Value != targetState)
                    condition.AddComponent(new DistrictOpenStateComponent { Value = targetState });
            }
        }

        private bool HasBuiltDistrictOfType(DistrictType districtType)
        {
            foreach (var row in _districtsByType[districtType])
                if (row.GetComponent<DistrictBuildStateComponent>().Value == DistrictBuildState.Built)
                    return true;

            return false;
        }
    }
}
