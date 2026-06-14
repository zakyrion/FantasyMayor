using System;
using DefaultEcs;
using JetBrains.Annotations;
using Modules.MainUI.EndTurn.Components;
using Modules.MainUI.EndTurn.Views;
using Modules.MainUI.Systems;
using UnityEngine;

namespace Modules.MainUI.EndTurn.Systems
{
    /// <summary>
    ///     Main UI spawn subsystem: resolves the end-turn button view from the shared Main UI instance and
    ///     publishes EndTurnViewComponent. Instantiates nothing — the orchestrator owns the Main UI handle.
    ///     Leaves the button hidden; EndTurnSystem reveals it in Gameplay.
    /// </summary>
    [UsedImplicitly]
    internal sealed class EndTurnSpawnSubSystem : MainUISpawnSubSystem
    {
        private const int ExecutionPriority = 10;

        private readonly World _world;

        public override int Priority => ExecutionPriority;

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

            // Hidden until Gameplay; EndTurnSystem shows it.
            view.Hide();
        }
    }
}
