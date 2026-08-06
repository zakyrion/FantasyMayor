using System;
using System.Collections.Generic;
using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.Archetypes;
using Presentation.HexIcons.Components;
using Presentation.HexIcons.Configs;
using Presentation.HexIcons.Events;
using Presentation.HexIcons.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace Presentation.HexIcons.Systems
{
    /// <summary>
    ///     Renders per-hex resource icons in response to a <see cref="HexIconsVisibilityChangedEvent" />.
    ///     The system's base set IS the event set, so it only runs on the frame an event exists (one-frame
    ///     events are disposed each tick by <c>EventCleanupSystem</c>) — zero idle cost. On an event it reads
    ///     the current <see cref="HexIconsVisibilityComponent" /> and either clears every container or rebuilds
    ///     each one from that hex's actual resources, looking the sprite up in <see cref="HexResourceIconConfig" />.
    ///     No render state is cached: every event is a full clear-and-rebuild.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexIconsVisibilitySystem : UpdatedSystem
    {
        private readonly EntityStorages _storages;
        private readonly Archetype _containerSet;
        private readonly Archetype _resourceSet;

        public override int Priority => SystemPriorities.RuntimeTick.HexIconsVisibility;

        public HexIconsVisibilitySystem(EntityStorages storages)
            : base(storages.World, EventArchetypes.Of<HexIconsVisibilityChangedEvent>(storages.World))
        {
            _storages = storages;
            _containerSet = PresentationArchetypes.HexIconContainer(storages.World);
            _resourceSet = MapArchetypes.HexResource(storages.World);
        }

        // Fires once per event (normally one per frame). Resolves prerequisites fail-loud, then clears and —
        // when visible — rebuilds every container from its hex's resources. The container×resource scan is
        // O(containers × resources) but runs only on event frames, so no per-hex lookup index is cached.
        protected override void Update(GameState state, in Entity entity)
        {
            if (!EcsEventExtensions.IsRipe(entity))
                return;

            if (!_storages.Singletons.Has<HexIconsVisibilityComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexIconsVisibilityComponent is missing.");
            if (!_storages.Singletons.Has<HexIconsViewComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexIconsViewComponent is missing.");
            if (!_storages.Singletons.Has<HexIconsConfigComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexIconsConfigComponent is missing.");
            if (!_storages.Singletons.Has<HexResourceIconConfigComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexResourceIconConfigComponent is missing.");

            var isVisible = _storages.Singletons.Get<HexIconsVisibilityComponent>().IsVisible;
            var view = _storages.Singletons.Get<HexIconsViewComponent>().View;
            var iconSize = _storages.Singletons.Get<HexIconsConfigComponent>().Value.IconSize;
            var entries = _storages.Singletons.Get<HexResourceIconConfigComponent>().Value.Entries;

            foreach (var containerEntity in _containerSet.Entities)
            {
                var container = containerEntity.GetComponent<HexIconContainerComponent>().Container;
                container.Clear();

                if (!isVisible)
                    continue;

                var coords = containerEntity.GetComponent<HexIdFKComponent>().Coords;
                foreach (var resourceEntity in _resourceSet.Entities)
                {
                    if (!resourceEntity.GetComponent<HexIdFKComponent>().Coords.Value.Equals(coords.Value))
                        continue;

                    var type = resourceEntity.GetComponent<HexResourceComponent>().Type;
                    if (TryFindSprite(entries, type, out var sprite))
                        AddIcon(view, container, iconSize, type, sprite);
                }
            }
        }

        // Adds a fixed-size (iconSize) icon as a flow child of the hex's container (the container stacks icons
        // vertically). Picking does not propagate to children in UI Toolkit, so the icon is marked individually
        // so the whole overlay stays click-through.
        private void AddIcon(HexIconsView view, VisualElement container, float iconSize, HexResourceType type,
            Sprite sprite)
        {
            var icon = new VisualElement { name = $"hex-icon-{type}-{container.childCount}" };
            icon.style.width = iconSize;
            icon.style.height = iconSize;
            icon.style.flexShrink = 0;
            icon.style.backgroundImage = new StyleBackground(Background.FromSprite(sprite));

            view.MakeRaycastTransparent(icon);
            container.Add(icon);
        }

        // Linear scan for a resource's sprite — there is no Sprite-by-ResourceType lookup API yet. A missing
        // sprite (no entry, or a null sprite on the entry) is a skip, not an error: not every resource needs art.
        private bool TryFindSprite(IReadOnlyList<HexResourceIconConfig.ResourceIconEntry> entries,
            HexResourceType type, out Sprite sprite)
        {
            foreach (var entry in entries)
            {
                if (entry.HexResourceType != type || entry.Sprite == null)
                    continue;

                sprite = entry.Sprite;
                return true;
            }

            sprite = null;
            return false;
        }
    }
}
