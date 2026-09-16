using EcsExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Modules.Boot.Implementation.Events;

namespace Modules.Boot.Implementation.Systems
{
    // Deviation (:deviation/in-code): a per-frame system with no work table and no tick anchor — every other
    // per-frame system reads a row or an event. It has neither because the app-state request it raises has
    // nowhere else to originate from before the (paused) AppStateRunner task gives Initialization its own
    // startup step; :initialization-request-kept accepts the system-loop form until then. "Once" is not this
    // class's contract — it holds because Boot switches state right after the tick that reads the request.
    [UsedImplicitly]
    public sealed class InitializationSystem : IUpdatedSystem
    {
        private readonly EntityStorages _storages;

        public AppState AppState { get; }
        public int Priority => SystemPriorities.RuntimeTick.Initialization;

        public InitializationSystem(AppState appState, EntityStorages storages)
        {
            AppState = appState;
            _storages = storages;
        }

        public void Update(GameState state)
        {
            _storages.Events.Raise(new AppStateRequestedEvent { Requested = AppState.ConfigLoading });
        }
    }
}
