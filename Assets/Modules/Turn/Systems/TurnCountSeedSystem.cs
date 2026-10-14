using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Turn.Components;

namespace Modules.Turn.Systems
{
    [UsedImplicitly]
    public sealed class TurnCountSeedSystem : IUniTaskSystem
    {
        private readonly EntityStorages _storages;

        public AppState AppState { get; }

        public TurnCountSeedSystem(AppState appState, EntityStorages storages)
        {
            AppState = appState;
            _storages = storages;
        }

        public UniTask Execute(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The game opens on the first Mayor Phase = turn 1; TurnCountSystem increments it on each
            // turn boundary. Seeded here so the turn cluster can show "Хід N" from the first frame.
            _storages.Singletons.Set(new TurnCountComponent(1));

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
