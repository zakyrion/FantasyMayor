using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Presentation.HexIcons.Components;
using Presentation.HexIcons.Events;
using Modules.Turn.Components;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Steady-state play. Ticks its update and late-update systems every frame in priority order.
    ///     Does not auto-transition — returning to the menu (with world teardown) is a separate concern.
    /// </summary>
    public sealed class GameplayState : IAppState
    {
        private readonly IReadOnlyList<IUpdatedSystem> _updateSystems;
        private readonly IReadOnlyList<ILateUpdatedSystem> _lateUpdateSystems;
        private readonly EntityStorages _storages;

        public GameMode Mode => GameMode.Gameplay;
        public GameMode? RequestedMode => null;

        public GameplayState(
            EntityStorages storages,
            IReadOnlyList<IUpdatedSystem> updateSystems,
            IReadOnlyList<ILateUpdatedSystem> lateUpdateSystems)
        {
            _storages = storages;
            _updateSystems = updateSystems.OrderBy(system => system.Priority).ToArray();
            _lateUpdateSystems = lateUpdateSystems.OrderBy(system => system.Priority).ToArray();
        }

        public UniTask EnterAsync(CancellationToken cancellationToken)
        {
            // Producer (variant B): write the initial visibility state, then raise a one-frame event so the
            // consumer renders icons on the first Gameplay tick. The player toggles this later via UI by
            // writing HexIconsVisibilityComponent and raising the same event.
            _storages.Singletons.Set(new HexIconsVisibilityComponent(true));

            _storages.World.CreateEvent(new HexIconsVisibilityChangedEvent());

            // The game opens on the first Mayor Phase = turn 1; TurnCountSystem increments it on each
            // turn boundary. Seeded here so the turn cluster can show "Хід N" from the first frame.
            _storages.Singletons.Set(new TurnCountComponent(1));

            return UniTask.CompletedTask;
        }

        public void Tick(GameState state)
        {
            foreach (var system in _updateSystems)
                system.Update(state);
        }

        public void LateTick(GameState state)
        {
            foreach (var system in _lateUpdateSystems)
                system.Update(state);
        }

        public void Exit()
        {
        }
    }
}
