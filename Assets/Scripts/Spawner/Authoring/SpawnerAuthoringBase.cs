using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace HW.Authoring
{
    public abstract class SpawnerAuthoringBase : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _prefabs;

        public List<GameObject> Prefabs => _prefabs;
    }

    public abstract class SpawnerBaker<TAuthoring, TBufferComponent> : Baker<TAuthoring>
        where TAuthoring : SpawnerAuthoringBase
        where TBufferComponent : unmanaged, IHexSpawnerComponent
    {
        public override void Bake(TAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var buffer = AddBuffer<TBufferComponent>(entity);

            foreach (var hexPrefab in authoring.Prefabs)
            {
                var prefab = GetEntity(hexPrefab, TransformUsageFlags.Dynamic);
                buffer.Add(new TBufferComponent
                {
                    Prefab = prefab
                });
            }
        }
    }
}