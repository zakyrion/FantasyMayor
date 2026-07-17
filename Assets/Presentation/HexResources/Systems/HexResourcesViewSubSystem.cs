using System;
using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using DefaultECSExtensions;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Utils;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using Presentation.HexResources.Components;
using Presentation.Terrain.Components;
using UnityEngine;
using Domains.Map.HexResources.Tags;

namespace Presentation.HexResources.Systems
{
    internal abstract class HexResourcesViewSubSystem : ISystem<GameState>
    {
        private readonly World _world;
        private readonly EntitySet _resourceSet;

        public bool IsEnabled { get; set; } = true;

        public abstract int Priority { get; }
        protected abstract HexResourceType TargetHexResourceType { get; }

        protected HexResourcesViewSubSystem(World world)
        {
            _world = world;
            _resourceSet = world.GetEntities()
                .With<HexIdFKComponent>()
                .With<HexResourceComponent>().With<HexResourceTag>()
                .AsSet();
        }

        public abstract void Update(GameState state);

        protected bool TryGetPrefab(out GameObject prefab)
        {
            prefab = null;

            if (!_world.Has<HexResourcesViewConfigComponent>())
                return false;

            var viewConfig = _world.Get<HexResourcesViewConfigComponent>().Value;
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

            if (!_world.Has<HexResourcesViewConfigComponent>())
                return false;

            var viewConfig = _world.Get<HexResourcesViewConfigComponent>().Value;

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

            if (!_world.Has<VertexGridComponent>())
                return false;

            vertexGrid = _world.Get<VertexGridComponent>().Grid;
            return true;
        }

        protected Entity[] GetTargetResourceEntities()
        {
            var resourceEntities = _resourceSet.GetEntities();
            var matchCount = 0;

            for (var i = 0; i < resourceEntities.Length; i++)
            {
                if (resourceEntities[i].Get<HexResourceComponent>().Type == TargetHexResourceType)
                    matchCount++;
            }

            if (matchCount == 0)
                return Array.Empty<Entity>();

            var matchedResources = new Entity[matchCount];
            var resultIndex = 0;

            for (var i = 0; i < resourceEntities.Length; i++)
            {
                if (resourceEntities[i].Get<HexResourceComponent>().Type != TargetHexResourceType)
                    continue;

                matchedResources[resultIndex++] = resourceEntities[i];
            }

            return matchedResources;
        }

        public virtual void Dispose()
        {
            _resourceSet.Dispose();
        }
    }
}
