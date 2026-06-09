using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultECSExtensions;

namespace Modules.Boot.Implementation
{
    /// <summary>
    ///     A single high-level game state. The machine calls <see cref="EnterAsync" /> once on entry, then
    ///     <see cref="Tick" /> / <see cref="LateTick" /> every frame while active, then <see cref="Exit" /> on leave.
    ///     A state requests a transition by exposing a non-null <see cref="RequestedMode" />; the machine reads it
    ///     after each <see cref="Tick" /> and switches. The state resets it on the next <see cref="EnterAsync" />.
    /// </summary>
    public interface IAppState
    {
        /// <summary>The mode this state represents.</summary>
        GameMode Mode { get; }

        /// <summary>Non-null when the state wants the machine to switch to another mode.</summary>
        GameMode? RequestedMode { get; }

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
