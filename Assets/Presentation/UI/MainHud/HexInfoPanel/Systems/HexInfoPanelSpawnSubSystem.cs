using System;
using DefaultEcs;
using JetBrains.Annotations;
using Presentation.UI.MainHud.HexInfoPanel.Components;
using Presentation.UI.MainHud.HexInfoPanel.Views;
using Presentation.UI.MainHud.Systems;
using Presentation.UI.Tags;
using UnityEngine;
using DefaultECSExtensions;

namespace Presentation.UI.MainHud.HexInfoPanel.Systems
{
    /// <summary>
    ///     Main UI spawn subsystem: resolves the hex info panel view from the shared Main UI instance and
    ///     publishes HexInfoPanelViewComponent. Instantiates nothing — the orchestrator owns the Main UI
    ///     handle. Sets the context sub-panel to its empty state; HexInfoPanelSystem fills it on selection.
    ///     The bottom-panel shell stays hidden (TurnPanelSpawnSubSystem) until Gameplay.
    /// </summary>
    [UsedImplicitly]
    internal sealed class HexInfoPanelSpawnSubSystem : MainHudSpawnSubSystem
    {
        private readonly World _world;

        public override int Priority => SystemPriorities.SubSystems.MainHudSpawn.HexInfoPanel;

        public HexInfoPanelSpawnSubSystem(World world)
        {
            _world = world;
        }

        public override void Prepare(GameObject mainUi)
        {
            // GetComponentInChildren (not GetComponent): the view carries its own UIDocument, so it lives on a
            // child GameObject of the Main UI root, not the root itself.
            var view = mainUi.GetComponentInChildren<HexInfoPanelView>(true);
            if (view == null)
                throw new InvalidOperationException(
                    "HexInfoPanelSpawnSubSystem: HexInfoPanelView is missing from the Main UI prefab.");

            var panelEntity = _world.CreateEntity();
            panelEntity.Set(new HexInfoPanelViewComponent(view));
            panelEntity.Set<UITag>();

            // Empty context until a hex is selected; HexInfoPanelSystem swaps in the filled blocks.
            view.ShowEmpty();
        }
    }
}
