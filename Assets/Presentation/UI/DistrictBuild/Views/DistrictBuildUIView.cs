using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Data;
using Modules.AxialSystem;
using Presentation.Terrain.Components;
using Presentation.Terrain.Tags;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Events;
using Presentation.UI.DistrictBuild.Tags;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Presentation.UI.DistrictBuild.Views
{
    /// <summary>
    ///     Chrome + visibility shell for the district-build overlay (a separate UIDocument from the HUD). It is the
    ///     MonoBehaviour DI resolves; it owns ONLY the overlay show/hide and the chrome (close / scrim / confirm).
    ///     All section content (list / requirements / price / actions) lives in its own section MonoBehaviour view
    ///     driven by its own subsystem — this view holds no section data or read-model. Confirm reads the selected
    ///     hex + district from ECS and raises DistrictBuildConfirmedEvent (payload) + DistrictBuildUIClosedEvent
    ///     (build + hide); dismiss (close / scrim) raises only DistrictBuildUIClosedEvent (hide). There is no draft,
    ///     so a dismiss has nothing to discard. PanelRenderer builds its tree asynchronously, so chrome is
    ///     (re)hooked in the reload callback.
    /// </summary>
    public sealed class DistrictBuildUIView : MonoBehaviour
    {
        private const string OverlayName = "DistrictBuildRoot";
        private const string ScrimName = "Scrim";
        private const string CloseButtonName = "CloseButton";
        private const string ConfirmButtonName = "ConfirmButton";

        [SerializeField] private PanelRenderer _renderer;

        private World _world;
        private VisualElement _root;
        private VisualElement _overlay;
        private VisualElement _scrim;
        private Button _closeButton;
        private Button _confirmButton;
        private bool _cached;
        private bool _visible;

        [Inject]
        public void Construct(World world)
        {
            _world = world;
        }

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

        private void OnConfirmClicked()
        {
            RaiseConfirm();
            RaiseClose();
        }

        // Dismiss (X / scrim): hide the window. There is no draft to discard, so dismiss is close-only.
        private void OnCloseClicked() => RaiseClose();
        private void OnScrimClicked(ClickEvent evt) => RaiseClose();

        // One-frame confirm pulse on its own entity, carrying the selected hex + district read from ECS: the
        // BuildDistrictActionSystem creates the committed build entity directly from the payload (the Actions
        // assembly can't read the Presentation selection). Kept separate from close — confirm builds, close hides.
        private void RaiseConfirm()
        {
            var (coords, type) = ReadSelection();

            var entity = _world.CreateEntity();
            entity.Set(new DistrictBuildConfirmedEvent { Coords = coords, Type = type });
            entity.Set(new EventTag());
        }

        // The overlay is modal, so the selected hex + district cannot change while it is open. Read them once at
        // confirm via one-shot sets. Missing either is a broken invariant (the overlay only opens with a selected
        // hex and default-selects a district) — fail loud rather than build an Unknown district.
        private (HexCoord coords, DistrictType type) ReadSelection()
        {
            using var hexSet = _world.GetEntities().With<HexSelectedComponent>().With<HexSelectionTag>().AsSet();
            using var selectionSet = _world.GetEntities()
                .With<DistrictBuildSelectionComponent>().With<DistrictBuildSelectionTag>().AsSet();

            if (hexSet.Count == 0)
                throw new InvalidOperationException("DistrictBuildUIView: confirm with no selected hex.");
            if (selectionSet.Count == 0)
                throw new InvalidOperationException("DistrictBuildUIView: confirm with no selected district.");

            var coords = hexSet.GetEntities()[0].Get<HexSelectedComponent>().Coords;
            var type = selectionSet.GetEntities()[0].Get<DistrictBuildSelectionComponent>().Selected;
            return (coords, type);
        }

        // One-frame close pulse; DistrictBuildUISystem hides the window.
        private void RaiseClose()
        {
            var entity = _world.CreateEntity();
            entity.Set(new DistrictBuildUIClosedEvent());
            entity.Set(new EventTag());
        }

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
