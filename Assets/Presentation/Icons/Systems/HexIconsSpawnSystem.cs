using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Tags;
using Presentation.Icons.Components;
using Presentation.Icons.Views;
using Unity.Mathematics;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Presentation.Icons.Systems
{
    [UsedImplicitly]
    internal sealed class HexIconsSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private const int ExecutionPriority = 700;

        private readonly World _world;

        // Cached at spawn for the container builders.
        private HexIconsView _view;

        public int Priority => ExecutionPriority;

        public HexIconsSpawnSystem(World world)
        {
            _world = world;
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            if (!_world.Has<HexIconsConfigComponent>())
                throw new InvalidOperationException("HexIconsSpawnSystem: HexIconsConfigComponent is missing.");

            var iconConfig = _world.Get<HexIconsConfigComponent>();

            var prefab = iconConfig.Value.Prefab;
            if (prefab == null)
                throw new InvalidOperationException("HexIconsSpawnSystem: prefab is null.");

            var instance = Object.Instantiate(prefab);
            var view = instance.GetComponent<HexIconsView>();
            if (view == null)
            {
                Object.Destroy(instance);
                throw new InvalidOperationException(
                    $"HexIconsSpawnSystem: prefab '{prefab.name}' has no HexIconsView component.");
            }

            // Screen-space overlay: no WorldDocumentRaycaster, no world-space sizing, no transform placement.
            // The panel covers the screen via its PanelSettings. The full-screen overlay must not eat
            // world/hex clicks, so mark the UXML root transparent.
            view.ApplyRaycastTransparent();

            _view = view;

            _world.Set(new HexIconsViewComponent(view));

            CreateContainers();

            return UniTask.CompletedTask;
        }

        // Eagerly creates one container entity per hex (HexIdComponent FK + HexIconContainerComponent).
        // HexTag is the hex-only marker, so it excludes the resource / resource-view parallel tables that
        // also carry HexIdComponent. Containers are positioned per frame by HexIconsContainerPositionSystem,
        // not here.
        private void CreateContainers()
        {
            using var hexSet = _world.GetEntities().With<HexTag>().With<HexIdComponent>().AsSet();
            foreach (var hexEntity in hexSet.GetEntities())
            {
                var hexId = hexEntity.Get<HexIdComponent>();
                var container = CreateContainerElement(hexId.Coords.Value);

                var containerEntity = _world.CreateEntity();
                containerEntity.Set(hexId);
                containerEntity.Set(new HexIconContainerComponent(container));
            }
        }

        // Builds the per-hex container: auto-fit (sizes to its icons), absolutely positioned, lays icons out
        // as a centered vertical list, and self-centers on its anchor via a percentage translate (so the
        // position system only sets left/top). Created after ApplyRaycastTransparent, so picking is disabled
        // here so it does not swallow world/hex clicks.
        private VisualElement CreateContainerElement(int2 coord)
        {
            var container = new VisualElement { name = $"hex-container-{coord.x}-{coord.y}" };
            container.style.position = Position.Absolute;
            container.style.flexDirection = FlexDirection.Column;
            container.style.alignItems = Align.Center;
            container.style.translate = new Translate(Length.Percent(-50f), Length.Percent(-50f));

            _view.MakeRaycastTransparent(container);
            _view.Root.Add(container);
            return container;
        }

        public void Dispose() { }
    }
}
