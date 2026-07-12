using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Presentation.UI.DistrictBuild.Views
{
    /// <summary>
    ///     Chrome + visibility shell for the district-build overlay (a separate UIDocument from the HUD). It is the
    ///     MonoBehaviour DI resolves; it owns ONLY the overlay show/hide and the chrome (close / scrim / confirm).
    ///     All section content (list / requirements / price / actions) lives in its own section MonoBehaviour view
    ///     driven by its own subsystem — this view holds no section data or read-model. It is World-free: the chrome
    ///     buttons raise the local C# events Confirmed (confirm) and Closed (close / scrim), which DistrictBuildUISystem
    ///     handles — the system reads the selection from ECS, builds, and hides. There is no draft, so a dismiss has
    ///     nothing to discard. PanelRenderer builds its tree asynchronously, so chrome is (re)hooked in the reload
    ///     callback.
    /// </summary>
    public sealed class DistrictBuildUIView : MonoBehaviour
    {
        private const string OverlayName = "DistrictBuildRoot";
        private const string ScrimName = "Scrim";
        private const string CloseButtonName = "CloseButton";
        private const string ConfirmButtonName = "ConfirmButton";

        [SerializeField] private PanelRenderer _renderer;

        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _scrim;
        private Button _closeButton;
        private Button _confirmButton;
        private bool _cached;
        private bool _visible;

        // Confirm builds the selected district; Closed hides the overlay (close / scrim). Kept separate — confirm
        // both builds and hides (DistrictBuildUISystem sequences it), close only hides.
        public event Action Confirmed;
        public event Action Closed;

        public void Show()
        {
            _visible = true;
            if (TryCache())
                _overlay.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _visible = false;
            if (TryCache())
                _overlay.style.display = DisplayStyle.None;
        }

        // Affordability gate (driven by DistrictBuildPriceUISubSystem): disable «Збудувати» when the selected payer
        // cannot afford the district. SetEnabled gives the built-in :disabled state and blocks the click, so the
        // BuildDistrictActionSystem spend-guard throw stays a pure invariant net, never a player-facing crash.
        public void SetConfirmEnabled(bool enabled)
        {
            if (TryCache())
                _confirmButton.SetEnabled(enabled);
        }

        private void OnEnable()
        {
            // Fail loud at init (NOT in the reload callback — a throw there runs inside the global panel-update
            // loop and would blank every UIDocument, not just this overlay).
            if (_renderer == null)
                throw new InvalidOperationException("DistrictBuildUIView: PanelRenderer is not assigned on the prefab.");

            _renderer.RegisterUIReloadCallback(OnUiReloaded);
        }

        private void OnDisable()
        {
            _renderer.UnregisterUIReloadCallback(OnUiReloaded);
            UnhookChrome();
        }

        private void OnUiReloaded(PanelRenderer renderer, VisualElement root)
        {
            UnhookChrome();
            _root = root;
            _cached = false;

            if (!TryCache())
                return;

            _closeButton.clicked += OnCloseClicked;
            _confirmButton.clicked += OnConfirmClicked;
            _scrim.RegisterCallback<ClickEvent>(OnScrimClicked);
            _overlay.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnConfirmClicked() => Confirmed?.Invoke();

        // Dismiss (X / scrim): hide the window. There is no draft to discard, so dismiss is close-only.
        private void OnCloseClicked() => Closed?.Invoke();
        private void OnScrimClicked(ClickEvent evt) => Closed?.Invoke();

        private void UnhookChrome()
        {
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseClicked;
            if (_confirmButton != null)
                _confirmButton.clicked -= OnConfirmClicked;
            _scrim?.UnregisterCallback<ClickEvent>(OnScrimClicked);
        }

        private bool TryCache()
        {
            EnsureCached();
            return _cached;
        }

        private void EnsureCached()
        {
            if (_cached)
                return;
            if (_root == null)
                return;

            _overlay = _root.Q<VisualElement>(OverlayName);
            _scrim = _root.Q<VisualElement>(ScrimName);
            _closeButton = _root.Q<Button>(CloseButtonName);
            _confirmButton = _root.Q<Button>(ConfirmButtonName);

            if (_overlay == null || _scrim == null || _closeButton == null || _confirmButton == null)
                return;

            _cached = true;
        }
    }
}
