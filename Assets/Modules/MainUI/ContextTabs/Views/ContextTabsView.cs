using System;
using DefaultEcs;
using DefaultECSExtensions;
using Modules.MainUI.ContextTabs.Components;
using Modules.MainUI.ContextTabs.Data;
using Modules.MainUI.ContextTabs.Events;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using static Unity.AppUI.UI.VisualElementExtensions;

namespace Modules.MainUI.ContextTabs.Views
{
    /// <summary>
    ///     View for the context sub-panel's tab row (Огляд / Будівлі / Дії). A dumb view over the shared Main
    ///     UI UIDocument. The three tabs are a single-select toggle group whose active tab is owned by ECS
    ///     (ActiveContextTabComponent): on the "on" edge of a tab the view raises a one-frame
    ///     ContextTabChangedEvent (mirrors EndTurnView's click-emit) and holds no state. The active tab cannot
    ///     be turned off (there is always exactly one). The systems push state in via SetActive (the checked
    ///     tab) and SetTabEnabled (per-tab availability). Tabs bind by name constant — the markup authors
    ///     ui:Toggle elements named TabOverview / TabBuildings / TabActions; the active one is shown via :checked.
    /// </summary>
    public sealed class ContextTabsView : MonoBehaviour
    {
        private const string TabOverviewName = "TabOverview";
        private const string TabBuildingsName = "TabBuildings";
        private const string TabActionsName = "TabActions";

        [SerializeField] private UIDocument _document;

        private World _world;
        private Toggle _tabOverview;
        private Toggle _tabBuildings;
        private Toggle _tabActions;
        private EventCallback<ChangeEvent<bool>> _onOverview;
        private EventCallback<ChangeEvent<bool>> _onBuildings;
        private EventCallback<ChangeEvent<bool>> _onActions;
        private bool _cached;

        [Inject]
        public void Construct(World world)
        {
            _world = world;
        }

        private void Start()
        {
            EnsureCached();

            // Single-select toggle group: a tab fires ChangeEvent on user click. Selection commits only on the
            // "on" edge; turning the active tab off is reverted (there is always exactly one active tab).
            _onOverview = evt => OnToggled(ContextTab.Overview, evt.newValue);
            _onBuildings = evt => OnToggled(ContextTab.Buildings, evt.newValue);
            _onActions = evt => OnToggled(ContextTab.Actions, evt.newValue);
            _tabOverview.RegisterValueChangedCallback(_onOverview);
            _tabBuildings.RegisterValueChangedCallback(_onBuildings);
            _tabActions.RegisterValueChangedCallback(_onActions);

            ApplyRaycastTransparent();
        }

        private void OnDestroy()
        {
            if (_tabOverview != null && _onOverview != null)
                _tabOverview.UnregisterValueChangedCallback(_onOverview);
            if (_tabBuildings != null && _onBuildings != null)
                _tabBuildings.UnregisterValueChangedCallback(_onBuildings);
            if (_tabActions != null && _onActions != null)
                _tabActions.UnregisterValueChangedCallback(_onActions);
        }

        /// <summary>Reflects the active tab as the single checked toggle (idempotent; raises no ChangeEvent).</summary>
        public void SetActive(ContextTab active)
        {
            EnsureCached();
            _tabOverview.SetValueWithoutNotify(active == ContextTab.Overview);
            _tabBuildings.SetValueWithoutNotify(active == ContextTab.Buildings);
            _tabActions.SetValueWithoutNotify(active == ContextTab.Actions);
        }

        /// <summary>Enables/disables one tab — SetEnabled blocks its clicks and applies the :disabled look.</summary>
        public void SetTabEnabled(ContextTab tab, bool enabled)
        {
            EnsureCached();
            Resolve(tab).SetEnabled(enabled);
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

        private void ApplyRaycastTransparent()
        {
            foreach (var element in _document.rootVisualElement.Query(className: "raycast-transparent").ToList())
                element.EnablePicking(false);
        }

        private void EnsureCached()
        {
            if (_cached)
                return;

            var root = _document.rootVisualElement;
            _tabOverview = root.Q<Toggle>(TabOverviewName);
            _tabBuildings = root.Q<Toggle>(TabBuildingsName);
            _tabActions = root.Q<Toggle>(TabActionsName);
            _cached = true;
        }
    }
}
