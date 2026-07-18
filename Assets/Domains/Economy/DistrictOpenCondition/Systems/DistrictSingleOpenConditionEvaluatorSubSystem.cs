using DefaultEcs;
using Domains.Economy.District.Components;
using Domains.Economy.District.Tags;
using Domains.Economy.DistrictOpenCondition.Components;
using Domains.Economy.DistrictOpenCondition.Data;
using Domains.Economy.DistrictOpenCondition.Tags;
using JetBrains.Annotations;
using DefaultECSExtensions;

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
        // Declarative query caches (Table Rule): every condition row keyed by its kind column, and every
        // District row indexed by its own type (District table's legal self-index) — 1:N, self-maintaining,
        // never a bare-key scan.
        private readonly EntityMultiMap<DistrictOpenConditionKindComponent> _conditionsByKind;
        private readonly EntityMultiMap<DistrictTypeComponent> _districtsByType;

        public override int Priority => SystemPriorities.SubSystems.DistrictOpenConditionEvaluator.Single;

        public DistrictSingleOpenConditionEvaluatorSubSystem(World world) : base(world)
        {
            _conditionsByKind = world.GetEntities()
                .With<DistrictOpenConditionTag>()
                .AsMultiMap<DistrictOpenConditionKindComponent>();

            _districtsByType = world.GetEntities()
                .With<DistrictTag>()
                .With<DistrictTypeComponent>()
                .AsMultiMap<DistrictTypeComponent>();
        }

        public override void Evaluate()
        {
            if (!_conditionsByKind.TryGetEntities(
                    new DistrictOpenConditionKindComponent { Value = DistrictOpenConditionKind.SingleOpen },
                    out var conditions))
                return;

            foreach (var condition in conditions)
            {
                var districtType = condition.Get<DistrictTypeFKComponent>().Value;
                var canBuild = !_districtsByType.TryGetEntities(
                    new DistrictTypeComponent { Value = districtType }, out _);

                var targetState = canBuild ? DistrictOpenState.Buildable : DistrictOpenState.Closed;
                if (condition.Get<DistrictOpenStateComponent>().Value != targetState)
                    condition.Set(new DistrictOpenStateComponent { Value = targetState });
            }
        }

        public override void Dispose()
        {
            _conditionsByKind.Dispose();
            _districtsByType.Dispose();
        }
    }
}
