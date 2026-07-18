using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
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
    ///     view singleton, and leaves it hidden. DistrictBuildUISystem reveals it on the build request.
    ///     Owns the single addressable handle for the overlay (mirrors the loading half of MainHudSpawnSystem).
    /// </summary>
    [UsedImplicitly]
    internal sealed class DistrictBuildUISpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private const string DistrictBuildActionPath = "UI/DistrictBuildAction";

        private readonly IAddressable _addressable;
        private readonly IMainCanvasProvider _canvasProvider;
        private readonly EntityStore _world;

        public int Priority => SystemPriorities.WorldInit.DistrictBuildUiSpawn;

        public DistrictBuildUISpawnSystem(EntityStore world,
            IAddressable addressable,
            IMainCanvasProvider canvasProvider)
        {
            _addressable = addressable;
            _canvasProvider = canvasProvider;
            _world = world;
        }

        public async UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (_world.HasWorldComponent<DistrictBuildUIRootComponent>())
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
                    $"DistrictBuildUISpawnSystem: failed to load the overlay by address '{DistrictBuildActionPath}'.");

            // The view carries its own UIDocument, so it may live on a child GameObject of the overlay root.
            var view = result.Box.Value.GetComponentInChildren<DistrictBuildUIView>(true);
            if (view == null)
            {
                result.Box.Dispose();
                throw new InvalidOperationException(
                    "DistrictBuildUISpawnSystem: DistrictBuildUIView is missing from the overlay prefab.");
            }

            // Each overlay section is its own MonoBehaviour view, resolved like the root and published as a world
            // component its section subsystem reads. Fail loud if any section is missing from the prefab.
            var listView = result.Box.Value.GetComponentInChildren<DistrictBuildListUIView>(true);
            var hexResourcesView = result.Box.Value.GetComponentInChildren<DistrictBuildHexResourcesUIView>(true);
            var priceView = result.Box.Value.GetComponentInChildren<DistrictBuildPriceUIView>(true);
            var actionsView = result.Box.Value.GetComponentInChildren<DistrictBuildActionsUIView>(true);

            if (listView == null || hexResourcesView == null || priceView == null || actionsView == null)
            {
                result.Box.Dispose();
                throw new InvalidOperationException(
                    "DistrictBuildUISpawnSystem: a section view (List/HexResources/Price/Actions) is missing from "
                    + "the overlay prefab.");
            }

            _world.SetWorldComponent(new DistrictBuildUIRootComponent { RootBox = result.Box });
            _world.SetWorldComponent(new DistrictBuildListUIViewComponent(listView));
            _world.SetWorldComponent(new DistrictBuildHexResourcesUIViewComponent(hexResourcesView));
            _world.SetWorldComponent(new DistrictBuildPriceUIViewComponent(priceView));
            _world.SetWorldComponent(new DistrictBuildActionsUIViewComponent(actionsView));

            var entity = _world.CreateEntity();
            entity.AddComponent(new DistrictBuildUIViewComponent(view));
            entity.AddTag<UITag>();

            // Spawned hidden so it never flashes during map creation; DistrictBuildUISystem shows it on the
            // build request and hides it on close.
            view.Hide();
        }

        public void Dispose()
        {
            if (_world != null && _world.HasWorldComponent<DistrictBuildUIRootComponent>())
            {
                _world.GetWorldComponent<DistrictBuildUIRootComponent>().RootBox.Dispose();
                _world.RemoveWorldComponent<DistrictBuildUIRootComponent>();
            }
        }
    }
}
