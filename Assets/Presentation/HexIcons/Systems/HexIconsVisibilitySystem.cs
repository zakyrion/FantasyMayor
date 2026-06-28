using System;
using System.Collections.Generic;
using DefaultEcs;
using DefaultECSExtensions;
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
        private const int ExecutionPriority = 800;

        private readonly World _world;
        private readonly EntitySet _containerSet;
        private readonly EntitySet _resourceSet;

        public override int Priority => ExecutionPriority;

        public HexIconsVisibilitySystem(World world)
            : base(world.GetEntities()
                .With<HexIconsVisibilityChangedEvent>()
                .AsSet())
        {
            _world = world;
            _containerSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexIconContainerComponent>()
                .AsSet();
            _resourceSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexResourceComponent>()
                .AsSet();
        }

        // Fires once per event (normally one per frame). Resolves prerequisites fail-loud, then clears and —
        // when visible — rebuilds every container from its hex's resources. The container×resource scan is
        // O(containers × resources) but runs only on event frames, so no per-hex lookup index is cached.
        protected override void Update(GameState state, in Entity entity)
        {
            if (!_world.Has<HexIconsVisibilityComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexIconsVisibilityComponent is missing.");
            if (!_world.Has<HexIconsViewComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexIconsViewComponent is missing.");
            if (!_world.Has<HexIconsConfigComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexIconsConfigComponent is missing.");
            if (!_world.Has<HexResourceIconConfigComponent>())
                throw new InvalidOperationException(
                    "HexIconsVisibilitySystem: HexResourceIconConfigComponent is missing.");

            var isVisible = _world.Get<HexIconsVisibilityComponent>().IsVisible;
            var view = _world.Get<HexIconsViewComponent>().View;
            var iconSize = _world.Get<HexIconsConfigComponent>().Value.IconSize;
            var entries = _world.Get<HexResourceIconConfigComponent>().Value.Entries;

            foreach (var containerEntity in _containerSet.GetEntities())
            {
                var container = containerEntity.Get<HexIconContainerComponent>().Container;
                container.Clear();

                if (!isVisible)
                    continue;

                var coords = containerEntity.Get<HexIdComponent>().Coords;
                foreach (var resourceEntity in _resourceSet.GetEntities())
                {
                    if (!resourceEntity.Get<HexIdComponent>().Coords.Value.Equals(coords.Value))
                        continue;

                    var type = resourceEntity.Get<HexResourceComponent>().Type;
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
