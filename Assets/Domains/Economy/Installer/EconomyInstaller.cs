using EcsExtensions;
using Domains.Economy.DistrictBuildOutcome.Systems;
using Domains.Economy.DistrictOpenCondition.Systems;
using Modules.Boot.Core;
using VContainer;
using VContainer.Unity;
using Domains.Economy.DistrictBuild.Configs;
using Domains.Economy.DistrictBuildCost.Configs;
using Domains.Economy.DistrictBuildOutcome.Configs;
using Domains.Economy.DistrictOpenCondition.Configs;

namespace Domains.Economy.Installer
{
    // Economy ships the district catalogue configs (builds, costs, outcomes, open conditions) and the
    // district-open-condition spawn pipeline. Resource is data types + the generic ResourceLoadoutSpawner mechanism.
    // District build/operate systems will register here later.
    public sealed class EconomyInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterAppStateSystem<ConfigLoaderSystem<DistrictBuildsConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_BUILDS_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<DistrictBuildCostsConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_BUILD_COSTS_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<DistrictBuildOutcomesConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_BUILD_OUTCOMES_CONFIG);

            builder.RegisterAppStateSystem<DistrictBuildOutcomeSpawnSystem>(Lifetime.Scoped, AppState.MapCreation);

            builder.Register<SpawnCityCenterOutcomeSubSystem>(Lifetime.Scoped)
                .As<IPrioritizedUniTaskSystem>();

            builder.RegisterAppStateSystem<ConfigLoaderSystem<DistrictOpenConditionsConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_OPEN_CONDITIONS_CONFIG);

            builder.RegisterAppStateSystem<DistrictOpenConditionSpawnSystem>(Lifetime.Scoped, AppState.MapCreation);

            builder.Register<DistrictExistConditionSpawnSubSystem>(Lifetime.Scoped)
                .As<IPrioritizedUniTaskSystem>();

            builder.Register<DistrictSingleOpenConditionSpawnSubSystem>(Lifetime.Scoped)
                .As<IPrioritizedUniTaskSystem>();

            builder.RegisterAppStateSystem<DistrictOpenConditionEvaluatorBootstrapSystem>(Lifetime.Scoped, AppState.MapCreation);

            builder.Register<DistrictOpenConditionEvaluatorSystem>(Lifetime.Scoped)
                .As<IPrioritizedUniTaskSystem>();

            // The evaluator family stays exactly as today — untouched by the sub-system contract (owner rewrites
            // it himself later): collected by its abstract base, not by OrchestratorType.
            builder.Register<DistrictSingleOpenConditionEvaluatorSubSystem>(Lifetime.Scoped)
                .As<DistrictSingleOpenConditionEvaluatorSubSystem, DistrictOpenConditionEvaluatorSubSystem>();

            builder.Register<DistrictExistConditionEvaluatorSubSystem>(Lifetime.Scoped)
                .As<DistrictExistConditionEvaluatorSubSystem, DistrictOpenConditionEvaluatorSubSystem>();

            builder.RegisterAppStateSystem<DistrictOpenConditionEvaluatorTableChangedSystem>(Lifetime.Scoped, AppState.Gameplay);
        }
    }
}
