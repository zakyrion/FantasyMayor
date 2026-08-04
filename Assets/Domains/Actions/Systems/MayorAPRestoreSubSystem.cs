using System.Threading;
using Cysharp.Threading.Tasks;
using Domains.Actors.Archetypes;
using Domains.Actors.Mayor.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Turn.Data;
using Modules.Turn.Systems;
using Unity.Collections;

namespace Domains.Actions.Systems
{
    // Turn phase (Upkeep band): at the start of each new turn it resets every Mayor's ActionPoint
    // (MayorAPComponent.Value) to MayorAPRestoreComponent.Value. Action Points do not carry over between
    // turns (GAMEPLAY_FOUNDATION.md), so this is a SET (reset to full), never an accumulate. Idempotent —
    // a re-run lands the same value. Phases run inline on the main thread (Law 1: store I/O is main-thread only).
    [UsedImplicitly]
    internal sealed class MayorAPRestoreSubSystem : TurnPhaseSubSystem
    {
        // Declarative query cache (a self-maintaining view, not system state): the Mayor rows that carry a
        // restore rule and hold the AP component to reset.
        private readonly Archetype _mayors;
        private readonly EntityStore _world;

        public override int Priority => SystemPriorities.TurnPhase.MayorApRestore;

        public MayorAPRestoreSubSystem(EntityStore world)
        {
            _world = world;
            _mayors = ActorsArchetypes.Mayor(world);
        }

        public override UniTask Update(TurnPhaseStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            RestoreActionPoints();
            return UniTask.CompletedTask;
        }

        private void RestoreActionPoints()
        {
            var entities = _mayors.Entities;

            // AddComponent inside Entities enumeration is a structural change (StructuralChangeException) —
            // snapshot ids first, then re-fetch by id to write.
            var ids = new NativeList<int>(entities.Count, Allocator.Temp);
            try
            {
                foreach (var mayor in entities)
                    ids.Add(mayor.Id);

                for (var i = 0; i < ids.Length; i++)
                {
                    _world.TryGetEntityById(ids[i], out var mayor);
                    var restore = mayor.GetComponent<MayorAPRestoreComponent>().Value;
                    mayor.AddComponent(new MayorAPComponent { Value = restore });
                }
            }
            finally
            {
                ids.Dispose();
            }
        }
    }
}
