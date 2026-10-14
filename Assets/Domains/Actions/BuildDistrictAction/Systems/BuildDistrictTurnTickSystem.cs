using System.Threading;
using Cysharp.Threading.Tasks;
using Friflo.Engine.ECS;
using EcsExtensions;
using Domains.Actions.Archetypes;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using JetBrains.Annotations;
using Modules.Turn.Data;
using Modules.Turn.Systems;
using Unity.Collections;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    // Turn phase (Upkeep band): decrements every in-progress build's turn countdown by one (floored at 0) and
    // raises BuildDistrictCompleteEvent once this turn, the first time any countdown reaches zero (:lossy-producer
    // — the event log holds the event until BuildDistrictCompletionSystem reads it, so a single raise is enough;
    // an eviction before that read is an accepted risk, see CASCADE.md contra c-2). Phases run inline on the main
    // thread (Law 1: store I/O is main-thread only). See Flows/FLOW_DISTRICT_BUILD.md.
    [UsedImplicitly]
    internal sealed class BuildDistrictTurnTickSystem : TurnPhaseSubSystem
    {
        // Self-maintaining view of the in-progress builds to count down (not system state).
        private readonly Archetype _inProgress;
        private readonly EntityStorages _storages;

        public override int Priority => SystemPriorities.TurnPhase.BuildDistrictTurnTick;

        public BuildDistrictTurnTickSystem(EntityStorages storages)
        {
            _storages = storages;

            _inProgress = ActionsArchetypes.BuildDistrictInProgress(storages.World);
        }

        public override UniTask Update(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            Tick();
            return UniTask.CompletedTask;
        }

        private void Tick()
        {
            var anyBuildFinishedThisTurn = false;
            var entities = _inProgress.Entities;

            // AddComponent inside Entities enumeration is a structural change (StructuralChangeException) —
            // snapshot ids first, then re-fetch by id to write.
            var ids = new NativeList<int>(entities.Count, Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                    ids.Add(entity.Id);

                for (var i = 0; i < ids.Length; i++)
                {
                    _storages.World.TryGetEntityById(ids[i], out var entity);
                    var turns = entity.GetComponent<BuildDistrictTurnsComponent>();
                    var turnsLeft = turns.TurnsLeft - 1;

                    // Only the turn a countdown first reaches zero counts — a build already at 0 does not
                    // re-trigger the event every subsequent turn.
                    if (turnsLeft == 0)
                        anyBuildFinishedThisTurn = true;

                    entity.AddComponent(new BuildDistrictTurnsComponent { TurnsLeft = turnsLeft, TurnsToBuild = turns.TurnsToBuild });
                }
            }
            finally
            {
                ids.Dispose();
            }

            if (anyBuildFinishedThisTurn)
                _storages.Events.Raise(new BuildDistrictCompleteEvent());
        }
    }
}
