using Domains.Actions.BuildDistrictAction.Systems;
using Domains.Actions.Systems;
using Modules.Turn.Systems;
using VContainer;
using VContainer.Unity;

namespace Domains.Actions.Installer
{
    // Registers the Actions domain's runtime systems:
    //  - turn phases — each registered as TurnPhaseSubSystem so VContainer collects them into the
    //    IReadOnlyList<TurnPhaseSubSystem> that TurnProcessorSystem runs (same pattern as HexResourcesViewInstaller).
    //  - BuildDistrictActionSystem — the build-district reactive-orchestrator, registered concrete so Boot injects
    //    it into the Gameplay state by hand (per-frame/reactive systems are wired in Boot.Construct, not by role).
    public sealed class ActionsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<MayorAPRestoreSubSystem>(Lifetime.Singleton)
                .As<MayorAPRestoreSubSystem, TurnPhaseSubSystem>();

            builder.Register<BuildDistrictActionSystem>(Lifetime.Singleton)
                .As<BuildDistrictActionSystem>();

            // Build-district draft lifecycle, all reactive systems wired into Gameplay by Boot: spawn (open) and
            // discard (cancel); promote-on-confirm is BuildDistrictActionSystem itself (registered above).
            builder.Register<BuildDistrictTemplateSpawnSystem>(Lifetime.Singleton)
                .As<BuildDistrictTemplateSpawnSystem>();
            builder.Register<BuildDistrictTemplateCancelSystem>(Lifetime.Singleton)
                .As<BuildDistrictTemplateCancelSystem>();
        }
    }
}
