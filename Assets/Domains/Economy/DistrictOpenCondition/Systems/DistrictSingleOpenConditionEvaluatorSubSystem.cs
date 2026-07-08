using DefaultEcs;
using Domains.Economy.District.Components;
using Domains.Economy.District.Tags;
using Domains.Economy.DistrictOpenCondition.Tags;
using JetBrains.Annotations;
using DefaultECSExtensions;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Evaluates DistrictSingleOpenConditionTag-kind conditions: the gated district type is buildable only
    // while zero built instances of it exist. Sets/removes DistrictCanBeBuildTag accordingly. The District
    // table (DistrictTag + DistrictTypeComponent) is pure scaffold today — nothing spawns it yet — so this
    // always finds zero built instances; the query is written against the real table shape, not stubbed, so
    // it activates automatically once a build/spawn mechanic lands.
    [UsedImplicitly]
    internal sealed class DistrictSingleOpenConditionEvaluatorSubSystem : DistrictOpenConditionEvaluatorSubSystem
    {
        // Declarative query caches (Table Rule): the Single-kind condition rows, and the built-district rows
        // indexed by their FK (DistrictTypeComponent) — 1:N, self-maintaining, never a bare-key scan.
        private readonly EntitySet _singleConditions;
        private readonly EntityMultiMap<DistrictTypeComponent> _builtDistrictsByType;

        public override int Priority => SystemPriorities.SubSystems.DistrictOpenConditionEvaluator.Single;

        public DistrictSingleOpenConditionEvaluatorSubSystem(World world) : base(world)
        {
            _singleConditions = world.GetEntities()
                .With<DistrictOpenConditionTag>()
                .With<DistrictSingleOpenConditionTag>()
                .With<DistrictTypeComponent>()
                .AsSet();

            _builtDistrictsByType = world.GetEntities()
                .With<DistrictTag>()
                .With<DistrictTypeComponent>()
                .AsMultiMap<DistrictTypeComponent>();
        }

        public override void Evaluate()
        {
            foreach (var condition in _singleConditions.GetEntities())
            {
                var canBuild = !_builtDistrictsByType.TryGetEntities(
                    condition.Get<DistrictTypeComponent>(), out _);

                if (canBuild)
                {
                    if (!condition.Has<DistrictCanBeBuildTag>())
                        condition.Set(new DistrictCanBeBuildTag());
                }
                else
                {
                    if (condition.Has<DistrictCanBeBuildTag>())
                        condition.Remove<DistrictCanBeBuildTag>();
                }
            }
        }

        public override void Dispose()
        {
            _singleConditions.Dispose();
            _builtDistrictsByType.Dispose();
        }
    }
}
