using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Systems;
using Domains.Actions.Systems;
using Modules.Boot.Core;
using Modules.Turn.Systems;
using VContainer;
using VContainer.Unity;

namespace Domains.Actions.Installer
{
    // Registers the Actions domain's runtime systems:
    //  - turn phases — each registered as TurnPhaseSubSystem so VContainer collects them into the
    //    IReadOnlyList<TurnPhaseSubSystem> that TurnProcessorSystem runs (same pattern as HexResourcesViewInstaller).
    //  - build-district-action draft lifecycle — reactive systems registered as concrete singletons; Boot.Construct
    //    wires them into GameplayState (that composition + the UI raise-sites are a later slice, not yet done).
    public sealed class ActionsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<MayorAPRestoreSubSystem>(Lifetime.Singleton)
                .As<MayorAPRestoreSubSystem, TurnPhaseSubSystem>();

            builder.Register<BuildDistrictDraftSpawnSystem>(Lifetime.Singleton);
            builder.Register<BuildDistrictDraftDiscardSystem>(Lifetime.Singleton);
        }
    }
}
