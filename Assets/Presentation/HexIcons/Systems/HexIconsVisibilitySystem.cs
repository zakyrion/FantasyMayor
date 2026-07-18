using System;
using System.Collections.Generic;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Domains.Map.Hex.Components;
using Presentation.HexIcons.Components;
using Presentation.HexIcons.Configs;
using Presentation.HexIcons.Events;
using Presentation.HexIcons.Views;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using UnityEngine;
using UnityEngine.UIElements;
using Presentation.HexIcons.Tags;
using Domains.Map.HexResources.Tags;

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
        private readonly EntityStore _world;
        private readonly ArchetypeQuery _containerSet;
        private readonly ArchetypeQuery _resourceSet;

        public override int Priority => SystemPriorities.RuntimeTick.HexIconsVisibility;

        public HexIconsVisibilitySystem(EntityStore world)
            : base(world.Query<HexIconsVisibilityChangedEvent>())
        {
            _world = world;
            _containerSet = world.Query<HexIdFKComponent, HexIconContainerComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexIconContainerTag>());
            _resourceSet = world.Query<HexIdFKComponent, HexResourceComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexResourceTag>());
        }

        // Fires once per event (normally one per frame). Resolves prerequisites fail-loud, then clears and —
        // when visible — rebuilds every container from its hex's resources. The container×resource scan is
        // O(containers × resources) but runs only on event frames, so no per-hex lookup index is cached.
        protected override void Update(GameState state, in Entity entity)
        {
            if (!EcsEventExtensions.IsRipe(entity))
                return;

            if (!_world.HasWorldComponent<HexIconsVisibilityComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexIconsVisibilityComponent is missing.");
            if (!_world.HasWorldComponent<HexIconsViewComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexIconsViewComponent is missing.");
            if (!_world.HasWorldComponent<HexIconsConfigComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexIconsConfigComponent is missing.");
            if (!_world.HasWorldComponent<HexResourceIconConfigComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexResourceIconConfigComponent is missing.");

            var isVisible = _world.GetWorldComponent<HexIconsVisibilityComponent>().IsVisible;
            var view = _world.GetWorldComponent<HexIconsViewComponent>().View;
            var iconSize = _world.GetWorldComponent<HexIconsConfigComponent>().Value.IconSize;
            var entries = _world.GetWorldComponent<HexResourceIconConfigComponent>().Value.Entries;

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
