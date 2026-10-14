using Core;
using Core.Events;
using EcsExtensions;
using JetBrains.Annotations;

namespace Modules.Boot.Implementation.Systems
{
    // Deviation (:deviation/in-code): a per-frame system with no work table and no tick anchor — every other
    // per-frame system reads a row or an event. It has neither because the app-state request it raises has
    // nowhere else to originate from: MapCreationState only runs the world-building pipeline, it never learns
    // the machine has started ticking it. "Once per entry" is not this class's contract — it holds because
    // Boot switches state right after the tick that reads the request, and the count resets on the next entry.
    [UsedImplicitly]
    public sealed class MapCreationSettleSystem : IUpdatedSystem
    {
        // A few frames let the per-frame settle systems finish reacting to anything the pipeline raised
        // before handing off to Gameplay; 3 is a safe margin. The systems here are idempotent, so extra
        // ticks are no-ops.
        private const int SettleTicks = 3;

        private readonly EntityStorages _storages;

        [StateAllowed("ticks since MapCreation entry completed; reset when the request is raised")]
        private int _ticksSinceEntry;

        public AppState AppState { get; }
        public int Priority => SystemPriorities.RuntimeTick.MapCreationSettle;

        public MapCreationSettleSystem(AppState appState, EntityStorages storages)
        {
            AppState = appState;
            _storages = storages;
        }

        public void Update(GameState state)
        {
            _ticksSinceEntry++;

            if (_ticksSinceEntry < SettleTicks)
                return;

            _storages.Events.Raise(new AppStateRequestedEvent { Requested = AppState.Gameplay });
            _ticksSinceEntry = 0;
        }
    }
}
