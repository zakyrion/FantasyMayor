using System.Threading;
using Cysharp.Threading.Tasks;
using Friflo.Engine.ECS;
using Domains.Actors.Mayor.Components;
using JetBrains.Annotations;
using Modules.Turn.Data;
using Modules.Turn.Systems;
using EcsExtensions;
using Domains.Actors.Mayor.Tags;

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
        private readonly ArchetypeQuery _mayors;

        public override int Priority => SystemPriorities.TurnPhase.MayorApRestore;

        public MayorAPRestoreSubSystem(EntityStore world)
        {
            _mayors = world.Query<MayorIdComponent, MayorAPRestoreComponent, MayorAPComponent>()
                .AllTags(Friflo.Engine.ECS.Tags.Get<MayorTag>());
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
            foreach (var mayor in _mayors.Entities)
            {
                var restore = mayor.GetComponent<MayorAPRestoreComponent>().Value;
                mayor.AddComponent(new MayorAPComponent { Value = restore });
            }
        }
    }
}
