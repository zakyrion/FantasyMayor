using Domains.Actions.Systems;
using Modules.Turn.Systems;
using VContainer;
using VContainer.Unity;

namespace Domains.Actions.Installer
{
    // Registers the Actions domain's turn phases. Each phase is registered as TurnPhaseSubSystem so VContainer
    // collects them into the IReadOnlyList<TurnPhaseSubSystem> that TurnProcessorSystem runs (same pattern as
    // HexResourcesViewInstaller for view subsystems).
    public sealed class ActionsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<MayorActionPointsRestoreSubSystem>(Lifetime.Singleton)
                .As<MayorActionPointsRestoreSubSystem, TurnPhaseSubSystem>();
        }
    }
}
