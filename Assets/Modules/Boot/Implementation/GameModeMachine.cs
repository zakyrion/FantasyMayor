using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Modules.Boot.Core;

namespace Modules.Boot.Implementation
{
    /// <summary>
    ///     Files every <see cref="IAppState" /> under its mode and carries out <see cref="Switch" />. Boot decides
    ///     when to switch and reads <see cref="IsEntryCompleted" /> before ticking — the machine never switches on
    ///     its own. Entry of one state can be cancelled by a subsequent switch or by <see cref="Stop" />.
    /// </summary>
    public sealed class GameModeMachine
    {
        private readonly IReadOnlyDictionary<AppState, IAppState> _statesByMode;

        private IAppState _current;
        private bool _entering;
        private CancellationTokenSource _enterCts;

        /// <summary>The mode of the current state.</summary>
        public AppState CurrentMode => _current.Mode;

        /// <summary>The mode the current state requests, or nothing.</summary>
        public AppState? RequestedMode => _current.RequestedMode;

        /// <summary>Whether a state is current and its entry has finished.</summary>
        public bool IsEntryCompleted => _current != null && !_entering;

        public GameModeMachine(IReadOnlyList<IAppState> states)
        {
            var statesByMode = new Dictionary<AppState, IAppState>(states.Count);
            foreach (var state in states)
                statesByMode.Add(state.Mode, state);

            _statesByMode = statesByMode;
        }

        /// <summary>Leaves the current state and starts the entry of the state filed under <paramref name="mode" />.</summary>
        /// <exception cref="KeyNotFoundException"><paramref name="mode" /> has no state filed.</exception>
        public void Switch(AppState mode)
        {
            _enterCts?.Cancel();
            _enterCts?.Dispose();

            _current?.Exit();
            _current = _statesByMode[mode];

            _entering = true;
            _enterCts = new CancellationTokenSource();
            EnterAsync(_current, _enterCts.Token).Forget();
        }

        /// <summary>Ticks the current state.</summary>
        public void Tick(GameState state)
        {
            _current.Tick(state);
        }

        /// <summary>Late-ticks the current state.</summary>
        public void LateTick(GameState state)
        {
            _current.LateTick(state);
        }

        /// <summary>Cancels an entry in flight and exits the current state.</summary>
        public void Stop()
        {
            _enterCts?.Cancel();
            _enterCts?.Dispose();
            _enterCts = null;
            _current?.Exit();
            _current = null;
        }

        private async UniTask EnterAsync(IAppState state, CancellationToken cancellationToken)
        {
            try
            {
                await state.EnterAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!cancellationToken.IsCancellationRequested)
                _entering = false;
        }
    }
}
