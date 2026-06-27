using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.Boot.Core;
using Modules.MainCanvas.Core;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Views;
using Presentation.UI.Tags;

namespace Presentation.UI.DistrictBuild.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 810). Instantiates the district-build overlay — a SEPARATE
    ///     UIDocument from the shared Main UI, authored with a higher sort order so it renders above the HUD and
    ///     its full-screen scrim blocks input below — under the main canvas, resolves its view, publishes the
    ///     view singleton, and leaves it hidden. DistrictBuildActionSystem reveals it on the build request.
    ///     Owns the single addressable handle for the overlay (mirrors the loading half of MainUISpawnSystem).
    /// </summary>
    [UsedImplicitly]
    internal sealed class DistrictBuildActionSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private const int ExecutionPriority = 810;
        private const string DistrictBuildActionPath = "UI/DistrictBuildAction";

        private readonly IAddressable _addressable;
        private readonly IMainCanvasProvider _canvasProvider;
        private readonly World _world;

        public int Priority => ExecutionPriority;

        public DistrictBuildActionSpawnSystem(World world,
            IAddressable addressable,
            IMainCanvasProvider canvasProvider)
        {
            _addressable = addressable;
            _canvasProvider = canvasProvider;
            _world = world;
        }

        public async UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (_world.Has<DistrictBuildActionRootComponent>())
                return;

            var canvas = _canvasProvider.RootGO;

            if (cancellationToken.IsCancellationRequested)
                return;

            var result =
                await _addressable.LoadAndInstanceAsync(DistrictBuildActionPath, cancellationToken, canvas.transform);

            if (cancellationToken.IsCancellationRequested)
            {
                if (result.Status == Status.Success)
                    result.Box.Dispose();

                return;
            }

            if (result.Status != Status.Success || !result.Box.Exist)
                throw new InvalidOperationException(
                    $"DistrictBuildActionSpawnSystem: failed to load the overlay by address '{DistrictBuildActionPath}'.");

            // The view carries its own UIDocument, so it may live on a child GameObject of the overlay root.
            var view = result.Box.Value.GetComponentInChildren<DistrictBuildActionView>(true);
            if (view == null)
            {
                result.Box.Dispose();
                throw new InvalidOperationException(
                    "DistrictBuildActionSpawnSystem: DistrictBuildActionView is missing from the overlay prefab.");
            }

            _world.Set(new DistrictBuildActionRootComponent { RootBox = result.Box });

            var entity = _world.CreateEntity();
            entity.Set(new DistrictBuildActionViewComponent(view));
            entity.Set<UITag>();

            // Spawned hidden so it never flashes during map creation; DistrictBuildActionSystem shows it on the
            // build request and hides it on close.
            view.Hide();
        }

        public void Dispose()
        {
            if (_world != null && _world.Has<DistrictBuildActionRootComponent>())
            {
                _world.Get<DistrictBuildActionRootComponent>().RootBox.Dispose();
                _world.Remove<DistrictBuildActionRootComponent>();
            }
        }
    }
}
