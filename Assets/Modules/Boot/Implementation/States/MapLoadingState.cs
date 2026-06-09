using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultECSExtensions;

namespace Modules.Boot.Implementation.States
{
    /// <summary>
    ///     Placeholder for restoring a saved world (deserialize → settle → Gameplay). Not yet wired:
    ///     no save/load flow exists. Kept as an explicit mode so the machine and call sites already
    ///     account for it.
    /// </summary>
    public sealed class MapLoadingState : IAppState
    {
        public GameMode Mode => GameMode.MapLoading;
        public GameMode? RequestedMode => null;

        public UniTask EnterAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public void Tick(GameState state)
        {
        }

        public void LateTick(GameState state)
        {
        }

        public void Exit()
        {
        }
    }
}
