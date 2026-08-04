using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.Boot.Core;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Tags;
using Presentation.Archetypes;
using Presentation.HexIcons.Components;
using Presentation.HexIcons.Views;
using Unity.Mathematics;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Presentation.HexIcons.Systems
{
    [UsedImplicitly]
    internal sealed class HexIconsSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private readonly EntityStore _world;
        private readonly Archetype _containerArchetype;

        // Cached at spawn for the container builders.
        private HexIconsView _view;

        public int Priority => SystemPriorities.WorldInit.HexIconsSpawn;

        public HexIconsSpawnSystem(EntityStore world)
        {
            _world = world;
            _containerArchetype = PresentationArchetypes.HexIconContainer(world);
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            if (!_world.HasWorldComponent<HexIconsConfigComponent>())
                throw new InvalidOperationException("HexIconsSpawnSystem: HexIconsConfigComponent is missing.");

            var iconConfig = _world.GetWorldComponent<HexIconsConfigComponent>();

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

            _world.SetWorldComponent(new HexIconsViewComponent(view));

            CreateContainers();

            return UniTask.CompletedTask;
        }

        // Eagerly creates one container entity per hex (HexIdFKComponent + HexIconContainerComponent).
        // HexTag is the hex-only marker, so the hex read here is the PK; the container row carries the FK
        // into the Hex space. Containers are positioned per frame by HexIconsContainerPositionSystem, not here.
        private void CreateContainers()
        {
            var hexSet = _world.Query<HexIdComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexTag>());

            // AddComponent/AddTag on the freshly spawned container entity is a structural change while
            // hexSet.Entities is enumerating (StructuralChangeException, store-wide) — collect spawn data
            // first, wire components after the query loop closes. Managed exception to the "Unity.Collections
            // in ECS systems" rule: VisualElement is a managed UI Toolkit reference. See ARCHITECTURE.md
            // (collections rule).
            var pending = new List<(int Id, HexCoord Coords, VisualElement Container)>();

            foreach (var hexEntity in hexSet.Entities)
            {
                var hexId = hexEntity.GetComponent<HexIdComponent>();
                var container = CreateContainerElement(hexId.Coords.Value);

                var containerEntity = _containerArchetype.CreateEntity();
                pending.Add((containerEntity.Id, hexId.Coords, container));
            }

            foreach (var (id, coords, container) in pending)
            {
                _world.TryGetEntityById(id, out var containerEntity);
                containerEntity.AddComponent(new HexIdFKComponent { Coords = coords });
                containerEntity.AddComponent(new HexIconContainerComponent(container));
            }
        }

        // Builds the per-hex container: auto-fit (sizes to its icons), absolutely positioned, lays icons out
        // as a centered vertical list, and self-centers on its anchor via a percentage translate (so the
        // position system only sets left/top). Created after ApplyRaycastTransparent, so picking is disabled
        // here so it does not swallow world/hex clicks.
        private VisualElement CreateContainerElement(int2 coord)
        {
            var container = new VisualElement { name = $"hex-container-{coord.x}-{coord.y}" };
            container.style.position = UnityEngine.UIElements.Position.Absolute;
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
