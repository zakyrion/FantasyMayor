using Modules.Turn.Systems;
using VContainer;
using VContainer.Unity;

namespace Modules.Turn.Installer
{
    public sealed class TurnInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            // Turn phases are registered by their owning domain (.As<…, TurnPhaseSubSystem>()); VContainer
            // collects them into the IReadOnlyList<TurnPhaseSubSystem> that TurnProcessorSystem consumes.
            // First phase: Domains.Actions.MayorActionPointsRestoreSubSystem (see ActionsInstaller).
            builder.Register<TurnProcessorSystem>(Lifetime.Singleton)
                .As<TurnProcessorSystem>();

            builder.Register<TurnCountSystem>(Lifetime.Singleton)
                .As<TurnCountSystem>();
        }
    }
}
