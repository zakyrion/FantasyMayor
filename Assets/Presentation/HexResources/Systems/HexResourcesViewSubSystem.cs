using System;
using System.Collections.Generic;
using Domains.Map.Archetypes;
using Domains.Map.Hex.Utils;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using Presentation.HexResources.Components;
using Presentation.Terrain.Components;
using UnityEngine;

namespace Presentation.HexResources.Systems
{
    internal abstract class HexResourcesViewSubSystem : ISystem<GameState>
    {
        private readonly EntityStore _world;
        private readonly Archetype _resourceSet;

        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }
        protected abstract HexResourceType TargetHexResourceType { get; }

        protected HexResourcesViewSubSystem(EntityStore world)
        {
            _world = world;
            _resourceSet = MapArchetypes.HexResource(world);
        }

        public abstract void Update(GameState state);

        protected bool TryGetPrefab(out GameObject prefab)
        {
            prefab = null;

            if (!_world.HasWorldComponent<HexResourcesViewConfigComponent>())
                return false;

            var viewConfig = _world.GetWorldComponent<HexResourcesViewConfigComponent>().Value;
            foreach (var resource in viewConfig.Resources)
            {
                if (resource.Type != TargetHexResourceType)
                    continue;

                prefab = resource.Prefab;
                return prefab != null;
            }

            return false;
        }

        protected bool TryGetPrefabs(out GameObject[] prefabs)
        {
            prefabs = Array.Empty<GameObject>();

            if (!_world.HasWorldComponent<HexResourcesViewConfigComponent>())
                return false;

            var viewConfig = _world.GetWorldComponent<HexResourcesViewConfigComponent>().Value;

            // Managed exception to the "Unity.Collections in ECS systems" rule: the elements are GameObject
            // (managed), which a NativeContainer cannot hold. See ARCHITECTURE.md (collections rule).
            var result = new List<GameObject>();

            foreach (var resource in viewConfig.Resources)
                if (resource.Type == TargetHexResourceType && resource.Prefab != null)
                    result.Add(resource.Prefab);

            prefabs = result.ToArray();
            return prefabs.Length > 0;
        }

        protected bool TryGetVertexGrid(out VertexGrid vertexGrid)
        {
            vertexGrid = default;

            if (!_world.HasWorldComponent<VertexGridComponent>())
                return false;

            vertexGrid = _world.GetWorldComponent<VertexGridComponent>().Grid;
            return true;
        }

        protected Entity[] GetTargetResourceEntities()
        {
            var resourceEntities = _resourceSet.Entities;
            var matchCount = 0;

            foreach (var resourceEntity in resourceEntities)
            {
                if (resourceEntity.GetComponent<HexResourceComponent>().Type == TargetHexResourceType)
                    matchCount++;
            }

            if (matchCount == 0)
                return Array.Empty<Entity>();

            var matchedResources = new Entity[matchCount];
            var resultIndex = 0;

            foreach (var resourceEntity in resourceEntities)
            {
                if (resourceEntity.GetComponent<HexResourceComponent>().Type != TargetHexResourceType)
                    continue;

                matchedResources[resultIndex++] = resourceEntity;
            }

            return matchedResources;
        }

        public virtual void Dispose()
        {
        }
    }
}
