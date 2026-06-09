using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultECSExtensions;

namespace Modules.Boot.Implementation
{
    /// <summary>
    ///     Drives the active <see cref="IAppState" />. Only the active state ticks; while a state's
    ///     <see cref="IAppState.EnterAsync" /> is in flight, ticking is suspended. A state-requested transition
    ///     (<see cref="IAppState.RequestedMode" />) is applied after the tick. Entry of one state can be cancelled
    ///     by a subsequent switch or by disposal.
    /// </summary>
    public sealed class GameModeMachine : IDisposable
    {
        private readonly IReadOnlyDictionary<GameMode, IAppState> _states;

        private IAppState _current;
        private bool _entering;
        private CancellationTokenSource _enterCts;

        public GameModeMachine(IReadOnlyDictionary<GameMode, IAppState> states)
        {
            _states = states ?? throw new ArgumentNullException(nameof(states));
        }

        public void Switch(GameMode mode)
        {
            _enterCts?.Cancel();
            _enterCts?.Dispose();

            _current?.Exit();
            _current = _states[mode];

            _entering = true;
            _enterCts = new CancellationTokenSource();
            EnterAsync(_current, _enterCts.Token).Forget();
        }

        public void Tick(GameState state)
        {
            if (_entering || _current == null)
                return;

            _current.Tick(state);

            var requested = _current.RequestedMode;
            if (requested.HasValue)
                Switch(requested.Value);
        }

        public void LateTick(GameState state)
        {
            if (_entering || _current == null)
                return;

            _current.LateTick(state);
        }

        public void Dispose()
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
