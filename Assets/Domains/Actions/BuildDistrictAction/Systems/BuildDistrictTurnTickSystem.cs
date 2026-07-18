using System.Threading;
using Cysharp.Threading.Tasks;
using Friflo.Engine.ECS;
using EcsExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using JetBrains.Annotations;
using Modules.Turn.Data;
using Modules.Turn.Systems;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    // Turn phase (Upkeep band): decrements every in-progress build's turn countdown by one (floored at 0) and —
    // while ANY countdown sits at 0 — raises one BuildDistrictCompleteEvent pulse for BuildDistrictCompletionSystem.
    // LEVEL-TRIGGERED BY DESIGN: the pulse re-raises every turn until completion consumes the entity, so a pulse
    // lost to the EventCleanup frame window costs one turn of latency, never correctness (never "optimize" this
    // into raise-once). Phases run inline on the main thread (Law 1: store I/O is main-thread only).
    // See Flows/FLOW_DISTRICT_BUILD.md.
    [UsedImplicitly]
    internal sealed class BuildDistrictTurnTickSystem : TurnPhaseSubSystem
    {
        // Self-maintaining view of the in-progress builds to count down (not system state).
        private readonly ArchetypeQuery _inProgress;
        private readonly EntityStore _world;

        public override int Priority => SystemPriorities.TurnPhase.BuildDistrictTurnTick;

        public BuildDistrictTurnTickSystem(EntityStore world)
        {
            _world = world;

            _inProgress = _world.Query<BuildDistrictTurnsComponent>()
                .AllTags(Friflo.Engine.ECS.Tags.Get<BuildDistrictInProgressTag>());
        }

        public override UniTask Update(TurnPhaseStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            Tick();
            return UniTask.CompletedTask;
        }

        private void Tick()
        {
            var raiseEvent = false;
            foreach (var entity in _inProgress.Entities)
            {
                var turns = entity.GetComponent<BuildDistrictTurnsComponent>();
                var turnsLeft = turns.TurnsLeft-1;

                if (turnsLeft <= 0)
                {
                    raiseEvent = true;
                }

                entity.AddComponent(new BuildDistrictTurnsComponent { TurnsLeft = turnsLeft, TurnsToBuild = turns.TurnsToBuild });
            }

            if (raiseEvent)
                _world.CreateEvent(new BuildDistrictCompleteEvent());
        }
    }
}
