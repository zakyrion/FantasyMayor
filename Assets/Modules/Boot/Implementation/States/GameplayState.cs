using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Modules.Boot.Core;
using Presentation.HexIcons.Components;
using Presentation.HexIcons.Events;
using Modules.Turn.Components;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Steady-state play. Runs the systems flagged <see cref="AppState.Gameplay" />, ticking its update and
    ///     late-update systems every frame in priority order. Does not request a transition — returning to the
    ///     menu (with world teardown) is a separate concern.
    /// </summary>
    public sealed class GameplayState : IAppState
    {
        private readonly AppStateSystems _systems;
        private readonly EntityStorages _storages;

        public AppState Mode => AppState.Gameplay;
        public AppState? RequestedMode => null;

        public GameplayState(EntityStorages storages, IReadOnlyList<IAppStateSystem> allSystems)
        {
            _storages = storages;
            _systems = AppStateSystems.Filter(Mode, allSystems);
        }

        public async UniTask EnterAsync(CancellationToken cancellationToken)
        {
            await _systems.RunEntryAsync(cancellationToken);
            SeedStartOfPlay();
        }

        private void SeedStartOfPlay()
        {
            // Producer (variant B): write the initial visibility state, then raise a one-frame event so the
            // consumer renders icons on the first Gameplay tick. The player toggles this later via UI by
            // writing HexIconsVisibilityComponent and raising the same event.
            _storages.Singletons.Set(new HexIconsVisibilityComponent(true));

            _storages.World.CreateEvent(new HexIconsVisibilityChangedEvent());

            // The game opens on the first Mayor Phase = turn 1; TurnCountSystem increments it on each
            // turn boundary. Seeded here so the turn cluster can show "Хід N" from the first frame.
            _storages.Singletons.Set(new TurnCountComponent(1));
        }

        public void Tick(GameState state)
        {
            _systems.Tick(state);
        }

        public void LateTick(GameState state)
        {
            _systems.LateTick(state);
        }

        public void Exit()
        {
        }
    }
}
