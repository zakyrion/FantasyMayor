using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using Domains.Actors.Mayor.Components;
using Domains.Economy.Resource.Components;
using Domains.Economy.Resource.Data;
using Domains.Economy.Resource.Tags;
using JetBrains.Annotations;
using Modules.Turn.Data;
using Modules.Turn.Systems;

namespace Domains.Actions.Systems
{
    // Turn phase (Upkeep band): at the start of each new turn it resets every Mayor's live ActionPoint
    // resource stack to MayorAPRestoreComponent.Value. Action Points do not carry over between turns
    // (GAMEPLAY_FOUNDATION.md), so this is a SET (reset to full), never an accumulate. Idempotent — a
    // re-run lands the same value.
    [UsedImplicitly]
    internal sealed class MayorActionPointsRestoreSubSystem : TurnPhaseSubSystem
    {
        // Upkeep band. Only phase today; relative value, easy to retune when the other phases land.
        private const int ExecutionPriority = 500;

        // Declarative query caches (self-maintaining views, not system state): the Mayor rows that carry a
        // restore rule, and the Mayor-owned resource stacks indexed by the owner FK (Table Rule).
        private readonly EntitySet _mayors;
        private readonly EntityMultiMap<MayorIdComponent> _mayorResources;

        public override int Priority => ExecutionPriority;

        public MayorActionPointsRestoreSubSystem(World world)
        {
            _mayors = world.GetEntities().With<MayorIdComponent>().With<MayorAPRestoreComponent>().AsSet();
            _mayorResources = world.GetEntities().With<ResourceTag>().With<MayorIdComponent>()
                .AsMultiMap<MayorIdComponent>();
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
                var id = mayor.Get<MayorIdComponent>();
                var restore = mayor.Get<MayorAPRestoreComponent>().Value;

                if (!_mayorResources.TryGetEntities(id, out var stacks))
                    throw new InvalidOperationException(
                        $"MayorActionPointsRestoreSubSystem: Mayor {id.Value} has no resource stacks — MayorSpawnSystem must seed them.");

                if (!TrySetActionPoints(stacks, restore))
                    throw new InvalidOperationException(
                        $"MayorActionPointsRestoreSubSystem: Mayor {id.Value} has no ActionPoint stack — MayorSpawnSystem must seed it.");
            }
        }

        private static bool TrySetActionPoints(ReadOnlySpan<Entity> stacks, int amount)
        {
            foreach (var stack in stacks)
            {
                if (stack.Get<ResourceComponent>().Type != ResourceType.ActionPoint)
                    continue;

                // Publishing write (Set, never ref-mutation) so the AP stack stays reactive-observable.
                stack.Set(new ResourceComponent { Type = ResourceType.ActionPoint, Amount = amount });
                return true;
            }

            return false;
        }
    }
}
