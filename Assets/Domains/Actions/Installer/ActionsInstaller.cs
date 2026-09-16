using Domains.Actions.BuildDistrictAction.Systems;
using Domains.Actions.Systems;
using EcsExtensions;
using Modules.Boot.Core;
using VContainer;
using VContainer.Unity;

namespace Domains.Actions.Installer
{
    // Registers the Actions domain's runtime systems:
    //  - turn phases — each registered as the sub-system contract (IPrioritizedUniTaskSystem) so VContainer
    //    collects them into the list TurnProcessorSystem filters by OrchestratorType (same pattern across every
    //    installer that registers a sub-system).
    //  - BuildDistrictActionSystem / BuildDistrictActionCancelSystem / BuildDistrictCompletionSystem — the
    //    build-district Gameplay trio: the first (reactive) creates the in-progress entity on confirm, the second
    //    (reactive) refunds and disposes it on cancel, the third (per-frame) materialises the Economy District fact
    //    once its turn countdown hits 0. The countdown itself is ticked by BuildDistrictTurnTickSystem (a turn
    //    phase, above). All three Gameplay systems reach the Gameplay state through the one registration rule
    //    (RegisterAppStateSystem), exposed as IAppStateSystem alongside their own first-order kind.
    public sealed class ActionsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<MayorAPRestoreSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();

            builder.Register<BuildDistrictTurnTickSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();

            builder.RegisterAppStateSystem<BuildDistrictActionSystem>(Lifetime.Singleton, AppState.Gameplay);

            builder.RegisterAppStateSystem<BuildDistrictActionCancelSystem>(Lifetime.Singleton, AppState.Gameplay);

            builder.RegisterAppStateSystem<BuildDistrictCompletionSystem>(Lifetime.Singleton, AppState.Gameplay);
        }
    }
}
