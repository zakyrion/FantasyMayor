using Core;
using Core.Events;
using EcsExtensions;
using UnityEngine;
using VContainer;

namespace Modules.Boot.Implementation
{
    /// <summary>
    ///     Entry point MonoBehaviour. Owns no system and drives no state itself: it only tells
    ///     <see cref="GameModeMachine" /> when to switch and into which mode. It starts in
    ///     <see cref="AppState.Initialization" />; afterwards any system asks for a mode by raising
    ///     <see cref="AppStateRequestedEvent" />. Boot reads its reader after every tick and switches only then —
    ///     never inside the tick, where the current state is still running its systems. Every state and every
    ///     first-order system is composed by the container from registrations — Boot names neither.
    /// </summary>
    public class Boot : MonoBehaviour
    {
        private EventReader<AppStateRequestedEvent> _appStateRequests;
        private GameModeMachine _machine;

        private void Start()
        {
            Application.targetFrameRate = 30;
            _machine.Switch(AppState.Initialization);
        }

        private void Update()
        {
            _machine.Tick(new GameState(Time.deltaTime));

            if (!TryReadLastRequestedMode(out var requestedMode))
                return;

            if (requestedMode == _machine.CurrentMode)
                return;

            Debug.Log($"[skh] new mode: {requestedMode}");
            _machine.Switch(requestedMode);
        }

        private void LateUpdate()
        {
            _machine.LateTick(new GameState(Time.deltaTime));
        }

        private void OnDestroy()
        {
            _machine?.Stop();
        }

        /// <summary>Receives the machine the container composed from every registered <see cref="IAppState" />.</summary>
        [Inject]
        public void Construct(GameModeMachine machine, EventReader<AppStateRequestedEvent> appStateRequests)
        {
            _machine = machine;
            _appStateRequests = appStateRequests;
        }

        // Drains every request raised this tick; the last one in the batch wins (:same-tick-delivery — a
        // request raised before Boot's own tick priority is visible the same tick it was raised).
        private bool TryReadLastRequestedMode(out AppState requestedMode)
        {
            requestedMode = default;
            var found = false;

            while (_appStateRequests.TryRead(out var request))
            {
                requestedMode = request.Requested;
                found = true;
            }

            return found;
        }
    }
}
