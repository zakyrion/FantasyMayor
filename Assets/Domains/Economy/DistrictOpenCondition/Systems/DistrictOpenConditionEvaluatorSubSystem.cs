using System;
using Friflo.Engine.ECS;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Abstract base for a per-kind condition evaluator. One concrete subsystem per condition kind
    // (SingleOpen, Exist). Each subsystem self-queries its own kind-slice of the condition table and the
    // District table — there is no shared read-model and no routing: every enabled subsystem runs
    // unconditionally each call. Public (PATTERN_ORCHESTRATOR_SUBSYSTEM default): the reactive table-changed
    // host names IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> directly in Boot.Construct.
    public abstract class DistrictOpenConditionEvaluatorSubSystem : IDisposable
    {
        protected readonly EntityStore World;

        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }

        protected DistrictOpenConditionEvaluatorSubSystem(EntityStore world)
        {
            World = world;
        }

        // Reconciles DistrictOpenStateComponent for this subsystem's condition kind against current world state.
        // Idempotent: a re-run with no real change is a no-op (the state column is written only on an actual
        // value change).
        public abstract void Evaluate();

        public virtual void Dispose()
        {
        }
    }
}
