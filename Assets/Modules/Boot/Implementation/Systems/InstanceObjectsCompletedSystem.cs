using Core;
using Core.Events;
using EcsExtensions;
using JetBrains.Annotations;

namespace Modules.Boot.Implementation.Systems
{
    // Deviation (:deviation/in-code): a per-frame system with no work table and no tick anchor — every other
    // per-frame system reads a row or an event. It has neither because the app-state request it raises has
    // nowhere else to originate from: InstanceObjectsState only runs its spawn systems, it never learns the
    // machine has started ticking it. "Once" is not this class's contract — it holds because Boot switches
    // state right after the tick that reads the request.
    [UsedImplicitly]
    public sealed class InstanceObjectsCompletedSystem : IUpdatedSystem
    {
        private readonly EntityStorages _storages;

        public AppState AppState { get; }
        public int Priority => SystemPriorities.RuntimeTick.InstanceObjectsCompleted;

        public InstanceObjectsCompletedSystem(AppState appState, EntityStorages storages)
        {
            AppState = appState;
            _storages = storages;
        }

        public void Update(GameState state)
        {
            _storages.Events.Raise(new AppStateRequestedEvent { Requested = AppState.MainMenu });
        }
    }
}
