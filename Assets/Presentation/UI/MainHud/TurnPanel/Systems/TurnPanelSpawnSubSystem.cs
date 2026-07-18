using System;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.UI.MainHud.TurnPanel.Components;
using Presentation.UI.MainHud.TurnPanel.Views;
using Presentation.UI.MainHud.Systems;
using Presentation.UI.Tags;
using UnityEngine;
using EcsExtensions;

namespace Presentation.UI.MainHud.TurnPanel.Systems
{
    /// <summary>
    ///     Main UI spawn subsystem: resolves the turn-panel view from the shared Main UI instance and publishes
    ///     TurnPanelViewComponent. Instantiates nothing — the orchestrator owns the Main UI handle. Leaves the
    ///     whole bottom-panel shell hidden; TurnPanelViewSystem reveals it in Gameplay.
    /// </summary>
    [UsedImplicitly]
    internal sealed class TurnPanelSpawnSubSystem : MainHudSpawnSubSystem
    {
        private readonly EntityStore _world;

        public override int Priority => SystemPriorities.SubSystems.MainHudSpawn.TurnPanel;

        public TurnPanelSpawnSubSystem(EntityStore world)
        {
            _world = world;
        }

        public override void Prepare(GameObject mainUi)
        {
            // GetComponentInChildren (not GetComponent): the view carries its own UIDocument, so it lives on a
            // child GameObject of the Main UI root, not the root itself.
            var view = mainUi.GetComponentInChildren<TurnPanelView>(true);
            if (view == null)
                throw new InvalidOperationException(
                    "TurnPanelSpawnSubSystem: TurnPanelView is missing from the Main UI prefab.");

            var entity = _world.CreateEntity();
            entity.AddComponent(new TurnPanelViewComponent(view));
            entity.AddTag<UITag>();

            // Whole bottom panel hidden until Gameplay; TurnPanelViewSystem reveals it.
            view.Hide();
        }
    }
}
