using EcsExtensions;
using Domains.Map.Generation.Components;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using static Unity.AppUI.UI.VisualElementExtensions;

namespace Presentation.UI.GeneratorMenu.Views
{
    public class HexesUI : MonoBehaviour
    {
        private const string GENERATE_BUTTON_ID = "GenerateButton";

        [SerializeField]
        private UIDocument _document;

        private EntityStorages _storages;

        private void Start()
        {
            _document.rootVisualElement.Q<Button>(GENERATE_BUTTON_ID).clicked += GenerateHexes;
            ApplyRaycastTransparent();
        }

        private void ApplyRaycastTransparent()
        {
            foreach (var element in _document.rootVisualElement.Query(className: "raycast-transparent").ToList())
                element.EnablePicking(false);
        }

        [Inject]
        public void Construct(EntityStorages storages)
        {
            _storages = storages;
        }

        private void GenerateHexes()
        {
            _storages.World.CreateEvent(new TerrainGenerationGenerateEventComponent());
        }
    }
}
