using Modules.Hexes.Components;
using Unity.Entities;
using UnityEngine;

namespace Modules.Hexes.Systems
{
    public partial class HexUISystem : SystemBase
    {
        private EntityQuery _generateEventQuery;

        protected override void OnCreate()
        {
            Debug.Log("[skh] HexUISystem.OnCreate()");
            _generateEventQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GenerateHexButtonEvent>());

            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            if (_generateEventQuery.IsEmptyIgnoreFilter)
                return;

            Debug.Log("Call first event");

            Entities.WithDeferredPlaybackSystem<EndSimulationEntityCommandBufferSystem>()
                .ForEach((Entity entity, EntityCommandBuffer ecb, in GenerateHexButtonEvent e) =>
                {
                    ecb.DestroyEntity(entity);
                }).Schedule();
        }
    }
}
