using DefaultECSExtensions;
using Domains.Actions.Systems;
using Modules.Boot.Core;
using Modules.Turn.Systems;
using VContainer;
using VContainer.Unity;

namespace Domains.Actions.Installer
{
    // Registers the Actions domain's runtime systems:
    //  - config loaders (ConfigLoadStep) — Boot runs them once at startup; ActionsDistrictsBuildConfigLoaderSystem
    //    publishes the district-build cost catalogue;
    //  - turn phases — each registered as TurnPhaseSubSystem so VContainer collects them into the
    //    IReadOnlyList<TurnPhaseSubSystem> that TurnProcessorSystem runs (same pattern as HexResourcesViewInstaller).
    public sealed class ActionsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<DistrictsBuildCostConfigLoaderSystem>(Lifetime.Singleton)
                .As<DistrictsBuildCostConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<MayorActionPointsRestoreSubSystem>(Lifetime.Singleton)
                .As<MayorActionPointsRestoreSubSystem, TurnPhaseSubSystem>();
        }
    }
}
