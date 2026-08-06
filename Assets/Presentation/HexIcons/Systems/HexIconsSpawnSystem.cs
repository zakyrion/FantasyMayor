using System;
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
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Presentation.HexIcons.Systems
{
    [UsedImplicitly]
    internal sealed class HexIconsSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private readonly EntityStorages _storages;
        private readonly Archetype _containerArchetype;

        // Cached at spawn for the container builders.
        private HexIconsView _view;

        public int Priority => SystemPriorities.WorldInit.HexIconsSpawn;

        public HexIconsSpawnSystem(EntityStorages storages)
        {
            _storages = storages;
            _containerArchetype = PresentationArchetypes.HexIconContainer(storages.World);
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            if (!_storages.Singletons.Has<HexIconsConfigComponent>())
                throw new InvalidOperationException("HexIconsSpawnSystem: HexIconsConfigComponent is missing.");

            var iconConfig = _storages.Singletons.Get<HexIconsConfigComponent>();

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

            _storages.Singletons.Set(new HexIconsViewComponent(view));

            CreateContainers();

            return UniTask.CompletedTask;
        }

        // Eagerly creates one container entity per hex (HexIdFKComponent + HexIconContainerComponent).
        // HexTag is the hex-only marker, so the hex read here is the PK; the container row carries the FK
        // into the Hex space. Containers are positioned per frame by HexIconsContainerPositionSystem, not here.
        private void CreateContainers()
        {
            var hexSet = _storages.World.Query<HexIdComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexTag>());

            // Snapshot-before-iterate: birth via _containerArchetype.CreateEntity() is NOT a structural
            // change, but the AddComponent writes that follow it are, and they would throw while
            // hexSet.Entities enumerates (ECS_CONVENTIONS → Structural changes during iteration).
            // Snapshotting the hex ids closes that enumeration, so spawn and wiring both happen in one pass —
            // no managed buffer.
            var hexIds = new NativeList<int>(Math.Max(1, hexSet.Count), Allocator.Temp);
            try
            {
                foreach (var hexEntity in hexSet.Entities)
                    hexIds.Add(hexEntity.Id);

                for (var i = 0; i < hexIds.Length; i++)
                {
                    if (!_storages.World.TryGetEntityById(hexIds[i], out var hexEntity))
                        continue;

                    var hexId = hexEntity.GetComponent<HexIdComponent>();
                    var container = CreateContainerElement(hexId.Coords.Value);

                    var containerEntity = _containerArchetype.CreateEntity();
                    containerEntity.AddComponent(new HexIdFKComponent { Coords = hexId.Coords });
                    containerEntity.AddComponent(new HexIconContainerComponent(container));
                }
            }
            finally
            {
                hexIds.Dispose();
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
