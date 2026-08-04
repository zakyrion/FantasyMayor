using System;
using Domains.Actors.Archetypes;
using Domains.Actors.Mayor.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Turn.Components;
using Presentation.UI.Archetypes;
using Presentation.UI.MainHud.TurnPanel.Components;
using Presentation.UI.MainHud.TurnPanel.Views;

namespace Presentation.UI.MainHud.TurnPanel.Systems
{
    /// <summary>
    ///     Drives the turn corner each Gameplay frame and owns the bottom-panel shell reveal: reveals the whole
    ///     panel (it spawns hidden) since the turn corner is its always-present part, reflects whether a turn is
    ///     running — Processing while a <see cref="TurnProcessorComponent" /> exists, Ready otherwise — pushes the
    ///     current turn number from <see cref="TurnCountComponent" /> into "Хід N", and feeds the two AP tiles:
    ///     «ДІЇ ЗАРАЗ» from <see cref="MayorAPComponent" /> (the Mayor's Action Points; reset to full each turn,
    ///     not yet decremented on spend), «НАСТ. ХІД» from <see cref="MayorAPRestoreComponent" />. The
    ///     click→NextTurnEvent emit lives in the view; this only reads and pushes. Stateless: the view setters are
    ///     idempotent and the view itself drops a redundant redraw. Anchored on the button-view singleton so it
    ///     ticks once per frame, mirroring ResourceBarSystem.
    /// </summary>
    [UsedImplicitly]
    public sealed class TurnPanelViewSystem : UpdatedSystem
    {
        // Declarative query cache (a self-maintaining view, not system state): the single Mayor row carrying the
        // live AP and the per-turn restore rule.
        private readonly Archetype _mayors;

        private readonly EntityStore _world;

        public override int Priority => SystemPriorities.RuntimeTick.TurnPanelView;

        public TurnPanelViewSystem(EntityStore world)
            : base(world, PresentationUIArchetypes.TurnPanel(world))
        {
            _world = world;
            _mayors = ActorsArchetypes.Mayor(world);
        }

        protected override void Update(GameState state, in Entity entity)
        {
            var view = entity.GetComponent<TurnPanelViewComponent>().View;
            if (view == null)
                return;

            if (!_world.HasWorldComponent<TurnCountComponent>())
                throw new InvalidOperationException(
                    "TurnPanelViewSystem: TurnCountComponent is missing — it must be seeded on Gameplay enter.");

            view.Show();
            view.SetProcessing(_world.HasWorldComponent<TurnProcessorComponent>());
            view.SetTurnNumber(_world.GetWorldComponent<TurnCountComponent>().Value);

            PushActionPoints(view);
        }

        // Always exactly one Mayor: «ДІЇ ЗАРАЗ» = MayorAPComponent.Value, «НАСТ. ХІД» = MayorAPRestoreComponent.Value.
        // Fail-loud if the Mayor is unseeded (MayorSpawnSystem owns it).
        private void PushActionPoints(TurnPanelView view)
        {
            if (!_mayors.TryGetFirst(out var mayor))
                throw new InvalidOperationException(
                    "TurnPanelViewSystem: no Mayor with MayorAPComponent/MayorAPRestoreComponent — MayorSpawnSystem must seed it.");

            var current = mayor.GetComponent<MayorAPComponent>().Value;
            var next = mayor.GetComponent<MayorAPRestoreComponent>().Value;

            view.SetActionPointsCurrent(current);
            view.SetActionPointsNext(next);
        }
    }
}
