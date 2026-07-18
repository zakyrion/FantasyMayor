using System;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.UI.MainHud.ContextTabs.Components;
using Presentation.UI.MainHud.ContextTabs.Data;
using Presentation.UI.MainHud.ContextTabs.Views;
using Presentation.UI.MainHud.Systems;
using UnityEngine;
using EcsExtensions;

namespace Presentation.UI.MainHud.ContextTabs.Systems
{
    /// <summary>
    ///     Main UI spawn subsystem: resolves the context-tabs view from the shared Main UI instance and seeds
    ///     the ContextTabsViewComponent + ActiveContextTabComponent (Overview) world singletons, then applies the
    ///     initial highlight. This window has no singleton entity. Instantiates nothing — the orchestrator owns
    ///     the Main UI handle.
    /// </summary>
    [UsedImplicitly]
    internal sealed class ContextTabsSpawnSubSystem : MainHudSpawnSubSystem
    {
        private const ContextTab DefaultTab = ContextTab.Overview;

        private readonly EntityStore _world;

        public override int Priority => SystemPriorities.SubSystems.MainHudSpawn.ContextTabs;

        public ContextTabsSpawnSubSystem(EntityStore world)
        {
            _world = world;
        }

        public override void Prepare(GameObject mainUi)
        {
            // GetComponentInChildren (not GetComponent): the view carries its own UIDocument, so it lives on a
            // child GameObject of the Main UI root, not the root itself.
            var view = mainUi.GetComponentInChildren<ContextTabsView>(true);
            if (view == null)
                throw new InvalidOperationException(
                    "ContextTabsSpawnSubSystem: ContextTabsView is missing from the Main UI prefab.");

            // The view and the active tab are world singletons (mirror TurnCountComponent) — no entity is created.
            _world.SetWorldComponent(new ContextTabsViewComponent(view));
            _world.SetWorldComponent(new ActiveContextTabComponent(DefaultTab));
            view.SetActive(DefaultTab);
        }
    }
}
