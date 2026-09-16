using EcsExtensions;
using Modules.Boot.Core;
using Modules.Turn.Components;
using Modules.Turn.Data;
using Modules.Turn.Systems;
using VContainer;
using VContainer.Unity;

namespace Modules.Turn.Installer
{
    public sealed class TurnInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
                container.Resolve<EntityStorages>().Singletons.Set(
                    new TurnProcessorComponent { Status = TurnProcessorStatus.Idle }));

            // Turn phases are registered by their owning domain, exposed as the sub-system contract
            // (IPrioritizedUniTaskSystem); VContainer collects them into the list TurnProcessorSystem filters by
            // OrchestratorType. First phase: Domains.Actions.MayorAPRestoreSubSystem (see ActionsInstaller).
            builder.RegisterAppStateSystem<TurnProcessorSystem>(Lifetime.Singleton, AppState.Gameplay);

            builder.RegisterAppStateSystem<TurnCountSystem>(Lifetime.Singleton, AppState.Gameplay);
        }
    }
}
