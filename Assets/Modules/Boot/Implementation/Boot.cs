using System;
using EcsExtensions;
using Modules.Boot.Core;
using UnityEngine;
using VContainer;

namespace Modules.Boot.Implementation
{
    /// <summary>
    ///     Entry point MonoBehaviour. Owns no system and drives no state itself: it only tells
    ///     <see cref="GameModeMachine" /> when to switch and into which mode. On <see cref="Start" /> it walks
    ///     <see cref="StartupOrder" /> one mode at a time, advancing as each one's entry completes; from
    ///     <see cref="AppState.MainMenu" /> on, it applies whatever mode the current state requests. Every state
    ///     and every first-order system is composed by the container from registrations — Boot names neither.
    /// </summary>
    public class Boot : MonoBehaviour
    {
        private static readonly AppState[] StartupOrder =
        {
            AppState.Initialization, AppState.ConfigLoading, AppState.InstanceObjects, AppState.MainMenu
        };

        private GameModeMachine _machine;

        /// <summary>Receives the machine the container composed from every registered <see cref="IAppState" />.</summary>
        [Inject]
        public void Construct(GameModeMachine machine)
        {
            _machine = machine;
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            _machine.Switch(StartupOrder[0]);
        }

        private void Update()
        {
            if (!_machine.IsEntryCompleted)
                return;

            _machine.Tick(new GameState(Time.deltaTime));
            AdvanceMode();
        }

        private void LateUpdate()
        {
            if (!_machine.IsEntryCompleted)
                return;

            _machine.LateTick(new GameState(Time.deltaTime));
        }

        private void OnDestroy()
        {
            _machine?.Stop();
        }

        /// <summary>
        ///     Switches into the next mode of <see cref="StartupOrder" /> while the startup walk still has one,
        ///     otherwise into whatever mode the current state requests.
        /// </summary>
        private void AdvanceMode()
        {
            var nextMode = NextStartupMode(_machine.CurrentMode) ?? _machine.RequestedMode;
            if (nextMode.HasValue)
                _machine.Switch(nextMode.Value);
        }

        /// <summary>
        ///     Names the startup mode after <paramref name="currentMode" />, or nothing once it is the last of
        ///     <see cref="StartupOrder" /> or outside it.
        /// </summary>
        private static AppState? NextStartupMode(AppState currentMode)
        {
            var index = Array.IndexOf(StartupOrder, currentMode);
            return index >= 0 && index < StartupOrder.Length - 1 ? StartupOrder[index + 1] : (AppState?)null;
        }
    }
}
