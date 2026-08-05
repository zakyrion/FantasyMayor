using System;
using Friflo.Engine.ECS;
using Unity.Collections;
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
        private readonly EntityStorages _storages;
        // Declarative query caches (Table Rule): every condition row keyed by its kind column, and every
        // District row keyed by its type (District table's legal self-index) — stage is read per-row below,
        // since the self-index cannot key on two columns at once.
        private readonly ComponentIndex<DistrictOpenConditionKindComponent, DistrictOpenConditionKind> _conditionsByKind;
        private readonly ComponentIndex<DistrictTypeComponent, DistrictType> _districtsByType;

        public override int Priority => SystemPriorities.SubSystems.DistrictOpenConditionEvaluator.Exist;

        public DistrictExistConditionEvaluatorSubSystem(EntityStorages storages) : base(storages.World)
        {
            _storages = storages;
            _conditionsByKind = storages.World.ComponentIndex<DistrictOpenConditionKindComponent, DistrictOpenConditionKind>();
            _districtsByType = storages.World.ComponentIndex<DistrictTypeComponent, DistrictType>();
        }

        public override void Evaluate()
        {
            var conditions = _conditionsByKind[DistrictOpenConditionKind.Exist];

            // AddComponent is a structural change and throws StructuralChangeException while the index slice
            // is enumerating — snapshot ids first, then re-fetch to write (ECS_CONVENTIONS → Structural
            // changes during iteration).
            var conditionIds = new NativeList<int>(Math.Max(1, conditions.Count), Allocator.Temp);
            try
            {
                foreach (var condition in conditions)
                    conditionIds.Add(condition.Id);

                for (var i = 0; i < conditionIds.Length; i++)
                {
                    if (!_storages.World.TryGetEntityById(conditionIds[i], out var condition))
                        continue;

                    var requiredType = condition.GetComponent<DistrictExistConditionComponent>().RequiredDistrict;
                    var targetState = HasBuiltDistrictOfType(requiredType)
                        ? DistrictOpenState.Buildable
                        : DistrictOpenState.Closed;

                    if (condition.GetComponent<DistrictOpenStateComponent>().Value != targetState)
                        condition.AddComponent(new DistrictOpenStateComponent { Value = targetState });
                }
            }
            finally
            {
                conditionIds.Dispose();
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
