using DefaultEcs;
using DefaultECSExtensions;
using Modules.TerrainGenerator.Components;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Modules.HexesUI
{
    public class HexesUI : MonoBehaviour
    {
        private const string GENERATE_BUTTON_ID = "GenerateButton";

        [SerializeField]
        private UIDocument _document;

        private World _world;

        private void Awake()
        {
            _document.rootVisualElement.Q<Button>(GENERATE_BUTTON_ID).clicked += GenerateHexes;
        }

        [Inject]
        public void Construct(World world)
        {
            _world = world;
        }

        private void GenerateHexes()
        {
            Debug.Log("[skh] Generating hexes");
            var entity = _world.CreateEntity();
            entity.Set(new TerrainGenerationGenerateEventComponent());
            entity.Set(new EventMarkerComponent());
        }
    }
}
