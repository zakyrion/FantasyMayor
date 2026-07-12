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
    //  - BuildDistrictActionSystem / BuildDistrictActionCancelSystem / BuildDistrictCompletionSystem — the
    //    build-district Gameplay trio: the first (reactive) creates the in-progress entity on confirm, the second
    //    (reactive) refunds and disposes it on cancel, the third (per-frame) materialises the Economy District fact
    //    once its turn countdown hits 0. The countdown itself is ticked by BuildDistrictTurnTickSystem (a turn
    //    phase, above). All three Gameplay systems registered concrete so Boot injects them into the Gameplay
    //    state by hand (per-frame/reactive systems are wired in Boot.Construct, not by role).
    public sealed class ActionsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<MayorAPRestoreSubSystem>(Lifetime.Singleton)
                .As<MayorAPRestoreSubSystem, TurnPhaseSubSystem>();

            builder.Register<BuildDistrictTurnTickSystem>(Lifetime.Singleton)
                .As<BuildDistrictTurnTickSystem, TurnPhaseSubSystem>();

            builder.Register<BuildDistrictActionSystem>(Lifetime.Singleton)
                .As<BuildDistrictActionSystem>();

            builder.Register<BuildDistrictActionCancelSystem>(Lifetime.Singleton)
                .As<BuildDistrictActionCancelSystem>();

            builder.Register<BuildDistrictCompletionSystem>(Lifetime.Singleton)
                .As<BuildDistrictCompletionSystem>();
        }
    }
}
