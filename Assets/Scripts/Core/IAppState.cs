using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core
{
    /// <summary>
    ///     A single high-level game state. <see cref="GameModeMachine" /> calls <see cref="EnterAsync" /> once on
    ///     entry, then <see cref="Tick" /> / <see cref="LateTick" /> every frame while active, then
    ///     <see cref="Exit" /> on leave; the machine withholds ticks until entry completes. A state requests
    ///     nothing itself — a system raises <see cref="AppStateRequestedEvent" /> and the machine switches once
    ///     it reads the request.
    /// </summary>
    public interface IAppState
    {
        /// <summary>The mode this state represents.</summary>
        AppState Mode { get; }

        /// <summary>Runs the (possibly async) entry sequence; ticking is suspended until this completes.</summary>
        UniTask EnterAsync(CancellationToken cancellationToken);

        /// <summary>Per-frame update while active.</summary>
        void Tick(GameState state);

        /// <summary>Per-late-frame update while active.</summary>
        void LateTick(GameState state);

        /// <summary>Cleanup on leaving the state.</summary>
        void Exit();
    }
}
