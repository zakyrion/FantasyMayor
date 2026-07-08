using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using Domains.Actors.Mayor.Components;
using JetBrains.Annotations;
using Modules.Turn.Data;
using Modules.Turn.Systems;
using DefaultECSExtensions;
using Domains.Actors.Mayor.Tags;

namespace Domains.Actions.Systems
{
    // Turn phase (Upkeep band): at the start of each new turn it resets every Mayor's live ActionPoint
    // resource stack to MayorAPRestoreComponent.Value. Action Points do not carry over between turns
    // (GAMEPLAY_FOUNDATION.md), so this is a SET (reset to full), never an accumulate. Idempotent — a
    // re-run lands the same value.
    [UsedImplicitly]
    internal sealed class MayorAPRestoreSubSystem : TurnPhaseSubSystem
    {
        // Declarative query caches (self-maintaining views, not system state): the Mayor rows that carry a
        // restore rule, and the Mayor-owned resource stacks indexed by the owner FK (Table Rule).
        private readonly EntitySet _mayors;

        public override int Priority => SystemPriorities.TurnPhase.MayorApRestore;

        public MayorAPRestoreSubSystem(World world)
        {
            _mayors = world.GetEntities().With<MayorIdComponent>().With<MayorTag>().With<MayorAPRestoreComponent>().With<MayorAPComponent>().AsSet();
        }

        public override async UniTask Update(TurnPhaseStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            // Phases compute off the main thread; every world write goes back on the main thread (TURN.md).
            //await UniTask.SwitchToMainThread(cancellationToken);

            // The ReadOnlySpan<Entity> reads below cannot live in an async method (CS4012), so the world work
            // runs in a synchronous helper invoked after the thread hop.
            RestoreActionPoints();
        }

        private void RestoreActionPoints()
        {
            foreach (var mayor in _mayors.GetEntities())
            {
                var restore = mayor.Get<MayorAPRestoreComponent>().Value;
                mayor.Set(new MayorAPComponent { Value = restore });
            }
        }
    }
}
