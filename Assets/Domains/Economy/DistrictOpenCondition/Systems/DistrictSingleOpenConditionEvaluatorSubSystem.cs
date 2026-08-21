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
    // Evaluates DistrictOpenConditionKind.SingleOpen-kind conditions: the gated district type is buildable only
    // while no District row of that type exists, in ANY stage — a build already under way must not be offered a
    // second time. Change-only writes DistrictOpenStateComponent (Closed/Buildable). Query unchanged since the
    // row unification (FLOW_DISTRICT_BUILD, 2026-07-17): the District row now exists from CONFIRM, so this
    // multimap already spans Planned AND Built rows — the in-progress case arrives for free, no
    // BuildDistrictInProgressTag read and no Economy→Actions edge needed.
    [UsedImplicitly]
    internal sealed class DistrictSingleOpenConditionEvaluatorSubSystem : DistrictOpenConditionEvaluatorSubSystem
    {
        private readonly EntityStorages _storages;
        // Declarative query caches (Table Rule): every condition row keyed by its kind column, and every
        // District row indexed by its own type (District table's legal self-index) — 1:N, self-maintaining,
        // never a bare-key scan.
        private readonly ComponentIndex<DistrictOpenConditionKindComponent, DistrictOpenConditionKind> _conditionsByKind;
        private readonly ComponentIndex<DistrictTypeComponent, DistrictType> _districtsByType;

        public override int Priority => SystemPriorities.SubSystems.DistrictOpenConditionEvaluator.Single;

        public DistrictSingleOpenConditionEvaluatorSubSystem(EntityStorages storages) : base(storages.World)
        {
            _storages = storages;
            _conditionsByKind = storages.World.ComponentIndex<DistrictOpenConditionKindComponent, DistrictOpenConditionKind>();
            _districtsByType = storages.World.ComponentIndex<DistrictTypeComponent, DistrictType>();
        }

        public override void Evaluate()
        {
            var conditions = _conditionsByKind[DistrictOpenConditionKind.SingleOpen];

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

                    var districtType = condition.GetComponent<DistrictTypeFKComponent>().Value;
                    var canBuild = _districtsByType[districtType].Count == 0;

                    var targetState = canBuild ? DistrictOpenState.Buildable : DistrictOpenState.Closed;
                    if (condition.GetComponent<DistrictOpenStateComponent>().Value != targetState)
                        condition.AddComponent(new DistrictOpenStateComponent { Value = targetState });
                }
            }
            finally
            {
                conditionIds.Dispose();
            }
        }
    }
}
