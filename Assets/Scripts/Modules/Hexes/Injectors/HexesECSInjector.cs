using Modules.CanvasProvider.Models;
using Modules.Hexes.Creators;
using Modules.Hexes.Systems;
using Unity.Entities;
using UnityEngine;
using Zenject;

namespace Modules.Hexes.Injectors
{
    public class HexesECSInjector : MonoBehaviour
    {
        private ICanvasProviderModel _canvasProvider;
        private IHexesCreator _hexesCreator;
        private IHexesUICreator _uiCreator;

        public void Awake()
        {
            Debug.Log("[skh] HexesECSInjector.Initialize()");
            var hexUISystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<HexUISystem>();
            hexUISystem.Init(_uiCreator);
        }

        [Inject]
        private void Init(ICanvasProviderModel canvasProvider, IHexesCreator hexesCreator, IHexesUICreator uiCreator)
        {
            Debug.Log("[skh] HexesECSInjector.Init()");
            _canvasProvider = canvasProvider;
            _hexesCreator = hexesCreator;
            _uiCreator = uiCreator;
        }
    }
}
