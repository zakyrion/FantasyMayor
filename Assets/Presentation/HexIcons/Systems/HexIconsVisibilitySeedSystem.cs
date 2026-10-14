using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Presentation.HexIcons.Components;
using Presentation.HexIcons.Events;

namespace Presentation.HexIcons.Systems
{
    [UsedImplicitly]
    public sealed class HexIconsVisibilitySeedSystem : IUniTaskSystem
    {
        private readonly EntityStorages _storages;

        public AppState AppState { get; }

        public HexIconsVisibilitySeedSystem(AppState appState, EntityStorages storages)
        {
            AppState = appState;
            _storages = storages;
        }

        public UniTask Execute(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Producer (variant B): write the initial visibility state, then raise a log event so the
            // consumer renders icons on the first Gameplay tick. The player toggles this later via UI by
            // writing HexIconsVisibilityComponent and raising the same event.
            _storages.Singletons.Set(new HexIconsVisibilityComponent(true));
            _storages.Events.Raise(new HexIconsVisibilityChangedEvent());

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
