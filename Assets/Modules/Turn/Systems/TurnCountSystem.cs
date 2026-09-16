using System;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Modules.Turn.Components;
using Modules.Turn.Events;

namespace Modules.Turn.Systems
{
    /// <summary>
    ///     Advances the turn counter. Reactive on the <see cref="TurnCompletedEvent" /> log event raised by
    ///     <see cref="TurnProcessorSystem" /> when a turn resolves — it does NOT poll the processor, so the
    ///     completion-detection logic lives in one place.
    /// </summary>
    [UsedImplicitly]
    public sealed class TurnCountSystem : IUpdatedSystem
    {
        private readonly EntityStorages _storages;
        private readonly EventReader<TurnCompletedEvent> _turnCompletions;

        public AppState AppState { get; }
        public int Priority => SystemPriorities.RuntimeTick.TurnCount;

        public TurnCountSystem(AppState appState, EntityStorages storages, EventReader<TurnCompletedEvent> turnCompletions)
        {
            AppState = appState;
            _storages = storages;
            _turnCompletions = turnCompletions;
        }

        public void Update(GameState state)
        {
            while (_turnCompletions.TryRead(out _))
                AdvanceTurnCount();
        }

        private void AdvanceTurnCount()
        {
            if (!_storages.Singletons.Has<TurnCountComponent>())
                throw new InvalidOperationException(
                    "TurnCountSystem: TurnCountComponent is missing — it must be seeded on Gameplay enter.");

            var current = _storages.Singletons.Get<TurnCountComponent>().Value;
            _storages.Singletons.Set(new TurnCountComponent(current + 1));
        }
    }
}
