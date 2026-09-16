using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.UI.Archetypes;
using Presentation.UI.MainHud.Systems;
using Presentation.UI.MainHud.TurnPanel.Components;
using Presentation.UI.MainHud.TurnPanel.Views;

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
        private readonly Archetype _archetype;

        public override int Priority => SystemPriorities.SubSystems.MainHudSpawn.TurnPanel;

        public TurnPanelSpawnSubSystem(EntityStorages storages) : base(storages)
        {
            _archetype = PresentationUIArchetypes.TurnPanel(storages.World);
        }

        public override UniTask Update(CancellationToken cancellationToken)
        {
            var mainUi = ReadMainHudRoot();

            // GetComponentInChildren (not GetComponent): the view carries its own UIDocument, so it lives on a
            // child GameObject of the Main UI root, not the root itself.
            var view = mainUi.GetComponentInChildren<TurnPanelView>(true);
            if (view == null)
                throw new InvalidOperationException(
                    "TurnPanelSpawnSubSystem: TurnPanelView is missing from the Main UI prefab.");

            var entity = _archetype.CreateEntity();
            entity.AddComponent(new TurnPanelViewComponent(view));

            // Whole bottom panel hidden until Gameplay; TurnPanelViewSystem reveals it.
            view.Hide();

            return UniTask.CompletedTask;
        }
    }
}
