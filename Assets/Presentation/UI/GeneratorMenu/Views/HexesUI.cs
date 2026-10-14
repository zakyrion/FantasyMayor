using System;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.AppUI.UI.VisualElementExtensions;

namespace Presentation.UI.GeneratorMenu.Views
{
    /// <summary>
    ///     The map generator menu. Raises <see cref="GenerateClicked" /> when the player presses Generate; the
    ///     system that shows it subscribes. The other two buttons are not wired yet.
    /// </summary>
    public class HexesUI : MonoBehaviour
    {
        private const string GENERATE_BUTTON_ID = "GenerateButton";

        [SerializeField]
        private UIDocument _document;

        public event Action GenerateClicked;

        private void Start()
        {
            _document.rootVisualElement.Q<Button>(GENERATE_BUTTON_ID).clicked += OnGenerateClicked;
            ApplyRaycastTransparent();
        }

        private void ApplyRaycastTransparent()
        {
            foreach (var element in _document.rootVisualElement.Query(className: "raycast-transparent").ToList())
                element.EnablePicking(false);
        }

        private void OnGenerateClicked()
        {
            GenerateClicked?.Invoke();
        }
    }
}
