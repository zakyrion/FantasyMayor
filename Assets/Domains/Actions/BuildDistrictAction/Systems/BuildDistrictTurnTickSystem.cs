using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using JetBrains.Annotations;
using Modules.Turn.Data;
using Modules.Turn.Systems;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    // Turn phase (Upkeep band): hops to the main thread, decrements every in-progress build's turn countdown by
    // one (floored at 0) and — while ANY countdown sits at 0 — raises one BuildDistrictCompleteEvent pulse for
    // BuildDistrictCompletionSystem. LEVEL-TRIGGERED BY DESIGN: the pulse re-raises every turn until completion
    // consumes the entity, so a pulse lost to the EventCleanup frame window costs one turn of latency, never
    // correctness (never "optimize" this into raise-once). All world writes — the value decrement AND the
    // structural event-entity write — happen on the main thread (SwitchToMainThread), which is what makes the
    // pulse safe to produce from the async Turn pipeline. See Flows/FLOW_DISTRICT_BUILD.md.
    [UsedImplicitly]
    internal sealed class BuildDistrictTurnTickSystem : TurnPhaseSubSystem
    {
        // Self-maintaining view of the in-progress builds to count down (not system state).
        private readonly EntitySet _inProgress;
        private readonly World _world;

        public override int Priority => SystemPriorities.TurnPhase.BuildDistrictTurnTick;

        public BuildDistrictTurnTickSystem(World world)
        {
            _world = world;

            _inProgress = _world.GetEntities()
                .With<BuildDistrictInProgressTag>()
                .With<BuildDistrictTurnsComponent>()
                .AsSet();
        }

        public override async UniTask Update(TurnPhaseStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            await UniTask.SwitchToMainThread();
            // ReadOnlySpan<Entity> reads cannot live in an async method (CS4012); run the world work in a sync helper.
            Tick();

            await UniTask.SwitchToThreadPool();
        }

        public override void Dispose()
        {
            _inProgress.Dispose();
        }

        private void Tick()
        {
            var raiseEvent = false;
            foreach (var entity in _inProgress.GetEntities())
            {
                var turns = entity.Get<BuildDistrictTurnsComponent>();
                var turnsLeft = turns.TurnsLeft-1;

                if (turnsLeft <= 0)
                {
                    raiseEvent = true;
                }

                entity.Set(new BuildDistrictTurnsComponent { TurnsLeft = turnsLeft, TurnsToBuild = turns.TurnsToBuild });
            }

            if (raiseEvent)
            {
                var eventEntity = _world.CreateEntity();
                eventEntity.Set(new BuildDistrictCompleteEvent());
                eventEntity.Set(new EventTag());
            }
        }
    }
}
