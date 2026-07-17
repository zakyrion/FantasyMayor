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
    // while zero built instances of it exist. Change-only writes DistrictOpenStateComponent (Closed/Buildable).
    // The District table (DistrictTag + DistrictTypeComponent) is pure scaffold today — nothing spawns it yet —
    // so this always finds zero built instances; the query is written against the real table shape, not
    // stubbed, so it activates automatically once a build/spawn mechanic lands.
    [UsedImplicitly]
    internal sealed class DistrictSingleOpenConditionEvaluatorSubSystem : DistrictOpenConditionEvaluatorSubSystem
    {
        // Declarative query caches (Table Rule): every condition row keyed by its kind column, and the
        // built-district rows indexed by their own type (District table's legal self-index) — 1:N,
        // self-maintaining, never a bare-key scan.
        private readonly EntityMultiMap<DistrictOpenConditionKindComponent> _conditionsByKind;
        private readonly EntityMultiMap<DistrictTypeComponent> _builtDistrictsByType;

        public override int Priority => SystemPriorities.SubSystems.DistrictOpenConditionEvaluator.Single;

        public DistrictSingleOpenConditionEvaluatorSubSystem(World world) : base(world)
        {
            _conditionsByKind = world.GetEntities()
                .With<DistrictOpenConditionTag>()
                .AsMultiMap<DistrictOpenConditionKindComponent>();

            _builtDistrictsByType = world.GetEntities()
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
                var canBuild = !_builtDistrictsByType.TryGetEntities(
                    new DistrictTypeComponent { Value = districtType }, out _);

                var targetState = canBuild ? DistrictOpenState.Buildable : DistrictOpenState.Closed;
                if (condition.Get<DistrictOpenStateComponent>().Value != targetState)
                    condition.Set(new DistrictOpenStateComponent { Value = targetState });
            }
        }

        public override void Dispose()
        {
            _conditionsByKind.Dispose();
            _builtDistrictsByType.Dispose();
        }
    }
}
