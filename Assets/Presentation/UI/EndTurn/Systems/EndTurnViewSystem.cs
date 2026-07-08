using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actors.Mayor.Components;
using Domains.Economy.Resource.Components;
using Domains.Economy.Resource.Tags;
using JetBrains.Annotations;
using Modules.Turn.Components;
using Presentation.UI.EndTurn.Components;
using Presentation.UI.EndTurn.Views;

namespace Presentation.UI.EndTurn.Systems
{
    /// <summary>
    ///     Drives the turn corner each Gameplay frame and owns the bottom-panel shell reveal: reveals the whole
    ///     panel (it spawns hidden) since the turn corner is its always-present part, reflects whether a turn is
    ///     running — Processing while a <see cref="TurnProcessorComponent" /> exists, Ready otherwise — pushes the
    ///     current turn number from <see cref="TurnCountComponent" /> into "Хід N", and feeds the two AP tiles:
    ///     «ДІЇ ЗАРАЗ» from the Mayor's live ActionPoint resource stack, «НАСТ. ХІД» from
    ///     <see cref="MayorAPRestoreComponent" />. The click→NextTurnEvent emit lives in the view; this only reads
    ///     and pushes. Stateless: the view setters are idempotent and the view itself drops a redundant redraw.
    ///     Anchored on the button-view singleton so it ticks once per frame, mirroring ResourceBarSystem.
    /// </summary>
    [UsedImplicitly]
    public sealed class EndTurnViewSystem : UpdatedSystem
    {
        private readonly EntityMultiMap<MayorIdComponent> _mayorResources;

        // Declarative query caches (self-maintaining views, not system state): the single Mayor row carrying a
        // restore rule, and the Mayor-owned resource stacks indexed by the owner FK (Table Rule).
        private readonly EntitySet _mayors;

        private readonly World _world;

        public override int Priority => SystemPriorities.RuntimeTick.EndTurnView;

        public EndTurnViewSystem(World world)
            : base(world.GetEntities().With<EndTurnViewComponent>().AsSet())
        {
            _world = world;
            _mayors = world.GetEntities().With<MayorIdComponent>().With<MayorAPRestoreComponent>().AsSet();
            _mayorResources = world.GetEntities()
                .With<MayorIdComponent>().With<ResourceTag>().AsMultiMap<MayorIdComponent>();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            var view = entity.Get<EndTurnViewComponent>().View;
            if (view == null)
                return;

            if (!_world.Has<TurnCountComponent>())
                throw new InvalidOperationException(
                    "EndTurnViewSystem: TurnCountComponent is missing — it must be seeded on Gameplay enter.");

            view.Show();
            view.SetProcessing(_world.Has<TurnProcessorComponent>());
            view.SetTurnNumber(_world.Get<TurnCountComponent>().Value);

            PushActionPoints(view);
        }

        public override void Dispose()
        {
            _mayors.Dispose();
            _mayorResources.Dispose();
            base.Dispose();
        }

        private static bool TryGetActionPoints(ReadOnlySpan<Entity> stacks, out int amount)
        {
            foreach (var stack in stacks)
            {
                var resource = stack.Get<ResourceComponent>();
                amount = resource.Amount;
                return true;
            }

            amount = 0;
            return false;
        }

        // Always exactly one Mayor: «НАСТ. ХІД» = MayorAPRestoreComponent.Value, «ДІЇ ЗАРАЗ» = the live
        // ActionPoint stack Amount. Fail-loud if the Mayor or its AP stack is unseeded (MayorSpawnSystem owns it).
        private void PushActionPoints(EndTurnView view)
        {
            var mayors = _mayors.GetEntities();
            if (mayors.Length == 0)
                throw new InvalidOperationException(
                    "EndTurnViewSystem: no Mayor with MayorAPRestoreComponent — MayorSpawnSystem must seed it.");

            var mayor = mayors[0];
            var id = mayor.Get<MayorIdComponent>();
            var next = mayor.Get<MayorAPRestoreComponent>().Value;

            if (!_mayorResources.TryGetEntities(id, out var stacks))
                throw new InvalidOperationException(
                    $"EndTurnViewSystem: Mayor {id.Value} has no resource stacks — MayorSpawnSystem must seed them.");

            if (!TryGetActionPoints(stacks, out var current))
                throw new InvalidOperationException(
                    $"EndTurnViewSystem: Mayor {id.Value} has no ActionPoint stack — MayorSpawnSystem must seed it.");

            view.SetActionPointsCurrent(current);
            view.SetActionPointsNext(next);
        }
    }
}
