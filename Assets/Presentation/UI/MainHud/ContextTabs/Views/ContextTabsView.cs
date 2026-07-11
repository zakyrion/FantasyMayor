using System;
using DefaultEcs;
using DefaultECSExtensions;
using Presentation.UI.MainHud.ContextTabs.Components;
using Presentation.UI.MainHud.ContextTabs.Data;
using Presentation.UI.MainHud.ContextTabs.Events;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using static Unity.AppUI.UI.VisualElementExtensions;

namespace Presentation.UI.MainHud.ContextTabs.Views
{
    /// <summary>
    ///     View for the context sub-panel's tab row (Огляд / Будівлі / Дії). A dumb view over the shared Main
    ///     UI PanelRenderer. The three tabs are a single-select toggle group whose active tab is owned by ECS
    ///     (ActiveContextTabComponent): on the "on" edge of a tab the view raises a one-frame
    ///     ContextTabChangedEvent (mirrors EndTurnView's click-emit) and holds no game state. The active tab
    ///     cannot be turned off (there is always exactly one). The systems push state in via SetActive (the
    ///     checked tab + the shown content pane) and SetTabEnabled (per-tab availability). Tabs bind by name
    ///     constant — the markup authors ui:Toggle elements named TabOverview / TabBuildings / TabActions (the
    ///     active one is shown via :checked) plus the matching content panes OverviewPane / BuildingsPane /
    ///     ActionsPane (one shown at a time). Both the tabs and the panes live in the shared Main UI document;
    ///     the pane swap rides inside SetActive (the active-tab state is owned by ECS, the view only mirrors it).
    ///     PanelRenderer builds its tree asynchronously, so the toggle callbacks are (re)registered and the
    ///     active tab + per-tab availability replayed in the reload callback.
    /// </summary>
    public sealed class ContextTabsView : MonoBehaviour
    {
        private const string TabOverviewName = "TabOverview";
        private const string TabBuildingsName = "TabBuildings";
        private const string TabActionsName = "TabActions";
        private const string OverviewPaneName = "OverviewPane";
        private const string BuildingsPaneName = "BuildingsPane";
        private const string ActionsPaneName = "ActionsPane";

        [SerializeField] private PanelRenderer _renderer;

        private World _world;
        private VisualElement _root;
        private Toggle _tabOverview;
        private Toggle _tabBuildings;
        private Toggle _tabActions;
        private VisualElement _overviewPane;
        private VisualElement _buildingsPane;
        private VisualElement _actionsPane;
        private EventCallback<ChangeEvent<bool>> _onOverview;
        private EventCallback<ChangeEvent<bool>> _onBuildings;
        private EventCallback<ChangeEvent<bool>> _onActions;
        private bool _cached;

        // Logical state replayed on (re)bind. The reactive ContextTab systems re-push, but replay covers the
        // seeded initial selection and a live UI reload. Tabs default to all-enabled (the availability stub).
        private ContextTab _active;
        private bool _activeSet;
        private bool _overviewEnabled = true;
        private bool _buildingsEnabled = true;
        private bool _actionsEnabled = true;

        [Inject]
        public void Construct(World world)
        {
            _world = world;
        }

        private void OnEnable()
        {
            // PanelRenderer delivers the (re)built root via this callback — for both the initial build and a
            // late registration (it invokes a pending callback if the tree already exists) — so binding never
            // races the async tree build. The callback also re-fires on a live UXML reload.
            _renderer.RegisterUIReloadCallback(OnUiReloaded);
        }

        private void OnDisable()
        {
            _renderer.UnregisterUIReloadCallback(OnUiReloaded);
            UnhookToggles();
        }

        // PanelRenderer (re)built its visual tree: re-query elements, re-hook the (recreated) toggles, reapply
        // picking, and replay the active tab + availability.
        private void OnUiReloaded(PanelRenderer renderer, VisualElement root)
        {
            UnhookToggles();
            _root = root;
            _cached = false;

            if (!TryCache())
                return;

            HookToggles();
            ApplyRaycastTransparent();
            ApplyState();
        }

        /// <summary>
        ///     Reflects the active tab: the single checked toggle AND the single shown content pane (the two
        ///     other panes are hidden). Idempotent; raises no ChangeEvent.
        /// </summary>
        public void SetActive(ContextTab active)
        {
            _active = active;
            _activeSet = true;
            if (TryCache())
                ApplyActive();
        }

        /// <summary>Enables/disables one tab — SetEnabled blocks its clicks and applies the :disabled look.</summary>
        public void SetTabEnabled(ContextTab tab, bool enabled)
        {
            StoreEnabled(tab, enabled);
            if (TryCache())
                Resolve(tab).SetEnabled(enabled);
        }

        private void ApplyState()
        {
            if (_activeSet)
                ApplyActive();

            _tabOverview.SetEnabled(_overviewEnabled);
            _tabBuildings.SetEnabled(_buildingsEnabled);
            _tabActions.SetEnabled(_actionsEnabled);
        }

        private void ApplyActive()
        {
            _tabOverview.SetValueWithoutNotify(_active == ContextTab.Overview);
            _tabBuildings.SetValueWithoutNotify(_active == ContextTab.Buildings);
            _tabActions.SetValueWithoutNotify(_active == ContextTab.Actions);

            _overviewPane.style.display = Display(_active == ContextTab.Overview);
            _buildingsPane.style.display = Display(_active == ContextTab.Buildings);
            _actionsPane.style.display = Display(_active == ContextTab.Actions);
        }

        private StyleEnum<DisplayStyle> Display(bool shown) =>
            shown ? DisplayStyle.Flex : DisplayStyle.None;

        private void StoreEnabled(ContextTab tab, bool enabled)
        {
            switch (tab)
            {
                case ContextTab.Overview:
                    _overviewEnabled = enabled;
                    break;
                case ContextTab.Buildings:
                    _buildingsEnabled = enabled;
                    break;
                case ContextTab.Actions:
                    _actionsEnabled = enabled;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tab), tab, "ContextTabsView: not a real tab.");
            }
        }

        private void OnToggled(ContextTab tab, bool isOn)
        {
            // The active tab cannot be turned off — revert that click; selection never becomes empty.
            if (!isOn)
            {
                Resolve(tab).SetValueWithoutNotify(true);
                return;
            }

            // Reflect the new selection immediately so two tabs are never checked at once. The reactive
            // ContextTabSelectionSystem reconciles authoritatively against ActiveContextTabComponent; SetActive
            // is idempotent, so the optimistic update and the reconcile agree.
            SetActive(tab);
            Emit(tab);
        }

        private void Emit(ContextTab tab)
        {
            // Record the selection in the world state, then raise a payload-less pulse — ContextTabSelectionSystem
            // reconciles the group against ActiveContextTabComponent (the pulse carries no data by design).
            _world.Set(new ActiveContextTabComponent(tab));

            var entity = _world.CreateEntity();
            entity.Set(new ContextTabChangedEvent());
            entity.Set(new EventTag());
        }

        private Toggle Resolve(ContextTab tab)
        {
            switch (tab)
            {
                case ContextTab.Overview: return _tabOverview;
                case ContextTab.Buildings: return _tabBuildings;
                case ContextTab.Actions: return _tabActions;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tab), tab, "ContextTabsView: not a real tab.");
            }
        }

        // Single-select toggle group: a tab fires ChangeEvent on user click. Selection commits only on the
        // "on" edge; turning the active tab off is reverted (there is always exactly one active tab).
        private void HookToggles()
        {
            _onOverview ??= evt => OnToggled(ContextTab.Overview, evt.newValue);
            _onBuildings ??= evt => OnToggled(ContextTab.Buildings, evt.newValue);
            _onActions ??= evt => OnToggled(ContextTab.Actions, evt.newValue);

            _tabOverview.RegisterValueChangedCallback(_onOverview);
            _tabBuildings.RegisterValueChangedCallback(_onBuildings);
            _tabActions.RegisterValueChangedCallback(_onActions);
        }

        private void UnhookToggles()
        {
            if (_tabOverview != null && _onOverview != null)
                _tabOverview.UnregisterValueChangedCallback(_onOverview);
            if (_tabBuildings != null && _onBuildings != null)
                _tabBuildings.UnregisterValueChangedCallback(_onBuildings);
            if (_tabActions != null && _onActions != null)
                _tabActions.UnregisterValueChangedCallback(_onActions);
        }

        private void ApplyRaycastTransparent()
        {
            foreach (var element in _root.Query(className: "raycast-transparent").ToList())
                element.EnablePicking(false);
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

            // PanelRenderer hands the root via the reload callback; until then there is nothing to bind.
            if (_root == null)
                return;

            _tabOverview = _root.Q<Toggle>(TabOverviewName);
            _tabBuildings = _root.Q<Toggle>(TabBuildingsName);
            _tabActions = _root.Q<Toggle>(TabActionsName);
            _overviewPane = _root.Q<VisualElement>(OverviewPaneName);
            _buildingsPane = _root.Q<VisualElement>(BuildingsPaneName);
            _actionsPane = _root.Q<VisualElement>(ActionsPaneName);
            _cached = true;
        }
    }
}
