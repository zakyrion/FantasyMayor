using System;
using DefaultEcs;
using JetBrains.Annotations;
using Presentation.UI.EndTurn.Components;
using Presentation.UI.EndTurn.Views;
using Presentation.UI.Systems;
using Presentation.UI.Tags;
using UnityEngine;
using DefaultECSExtensions;

namespace Presentation.UI.EndTurn.Systems
{
    /// <summary>
    ///     Main UI spawn subsystem: resolves the end-turn view from the shared Main UI instance and publishes
    ///     EndTurnViewComponent. Instantiates nothing — the orchestrator owns the Main UI handle. Leaves the
    ///     whole bottom-panel shell hidden; EndTurnViewSystem reveals it in Gameplay.
    /// </summary>
    [UsedImplicitly]
    internal sealed class EndTurnSpawnSubSystem : MainUISpawnSubSystem
    {
        private readonly World _world;

        public override int Priority => SystemPriorities.SubSystems.MainUiSpawn.EndTurn;

        public EndTurnSpawnSubSystem(World world)
        {
            _world = world;
        }

        public override void Prepare(GameObject mainUi)
        {
            // GetComponentInChildren (not GetComponent): the view carries its own UIDocument, so it lives on a
            // child GameObject of the Main UI root, not the root itself.
            var view = mainUi.GetComponentInChildren<EndTurnView>(true);
            if (view == null)
                throw new InvalidOperationException(
                    "EndTurnSpawnSubSystem: EndTurnView is missing from the Main UI prefab.");

            var entity = _world.CreateEntity();
            entity.Set(new EndTurnViewComponent(view));
            entity.Set<UITag>();

            // Whole bottom panel hidden until Gameplay; EndTurnViewSystem reveals it.
            view.Hide();
        }
    }
}
