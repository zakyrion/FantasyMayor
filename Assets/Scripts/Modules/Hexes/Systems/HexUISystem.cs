using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Modules.Hexes.Components;
using Modules.Hexes.Creators;
using Modules.Hexes.View.UI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Modules.Hexes.Systems
{
    public partial class HexUISystem : SystemBase
    {
        private CancellationTokenSource _cts;
        private EntityQuery _generateEventQuery;
        private EntityQuery _showUIQuery;
        private Box<HexesUIView> _uiBox;

        private IHexesUICreator _uiCreator;
        private EntityQuery _uiQuery;

        private EntityArchetype _uiStateArchetype;

        protected override void OnDestroy()
        {
            _cts.Cancel();
            _uiBox.Dispose();

            base.OnDestroy();
        }

        public void Init(IHexesUICreator uiCreator)
        {
            _uiCreator = uiCreator;
            Debug.Log("[skh] HexUISystem.Init()");
        }

        protected override void OnCreate()
        {
            Debug.Log("[skh] HexUISystem.OnCreate()");

            _cts = new CancellationTokenSource();

            _generateEventQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<HexesGenerateButtonEvent>());
            _showUIQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<HexesShowUIEvent>());
            _uiQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<HexesUIStatusComponent>());
            _uiStateArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<HexesUIStatusComponent>());

            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            CreateUIHandler();
            GenerateTerrainHandler();

        }

        private void CreateUIHandler()
        {
            if (_showUIQuery.IsEmptyIgnoreFilter)
                return;

            Entities.WithDeferredPlaybackSystem<EndSimulationEntityCommandBufferSystem>()
                .ForEach((Entity entity, EntityCommandBuffer ecb, in HexesShowUIEvent e) =>
                {
                    ecb.DestroyEntity(entity);
                }).Schedule();

            InstanceUIAsync(_cts.Token).Forget();
        }

        private void GenerateTerrainHandler()
        {
            if (_generateEventQuery.IsEmptyIgnoreFilter)
                return;

            Debug.Log("Call first event");

            Entities.WithDeferredPlaybackSystem<EndSimulationEntityCommandBufferSystem>()
                .ForEach((Entity entity, EntityCommandBuffer ecb, in HexesGenerateButtonEvent e) =>
                {
                    ecb.DestroyEntity(entity);
                }).Schedule();
        }

        private async UniTaskVoid InstanceUIAsync(CancellationToken cancellationToken)
        {
            var box = await _uiCreator.CreateHexesUIAsync(_cts.Token);
            if (cancellationToken.IsCancellationRequested)
            {
                box.Dispose();
                return;
            }

            _uiBox = box;

            using var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var e2 = ecb.CreateEntity(_uiStateArchetype);
            ecb.SetComponent(e2, new HexesUIStatusComponent()
            {
                Active = true,
                ObjectRef = box.Value
            });

            ecb.Playback(EntityManager);
        }
    }
}
