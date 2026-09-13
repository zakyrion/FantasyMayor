using EcsExtensions;
using Domains.Economy.DistrictBuildOutcome.Systems;
using Domains.Economy.DistrictOpenCondition.Systems;
using Modules.Boot.Core;
using Modules.Turn.Systems;
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
            builder.Register<ConfigLoaderSystem<DistrictBuildsConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_BUILDS_CONFIG);

            builder.Register<ConfigLoaderSystem<DistrictBuildCostsConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_BUILD_COSTS_CONFIG);

            builder.Register<ConfigLoaderSystem<DistrictBuildOutcomesConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_BUILD_OUTCOMES_CONFIG);

            builder.Register<DistrictBuildOutcomeSpawnSystem>(Lifetime.Scoped)
                .As<DistrictBuildOutcomeSpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<SpawnCityCenterOutcomeSubSystem>(Lifetime.Scoped)
                .As<SpawnCityCenterOutcomeSubSystem, DistrictBuildOutcomeSpawnSubSystem>();

            builder.Register<ConfigLoaderSystem<DistrictOpenConditionsConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_OPEN_CONDITIONS_CONFIG);

            builder.Register<DistrictOpenConditionSpawnSystem>(Lifetime.Scoped)
                .As<DistrictOpenConditionSpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<DistrictExistConditionSpawnSubSystem>(Lifetime.Scoped)
                .As<DistrictExistConditionSpawnSubSystem, DistrictOpenConditionSpawnSubSystem>();

            builder.Register<DistrictSingleOpenConditionSpawnSubSystem>(Lifetime.Scoped)
                .As<DistrictSingleOpenConditionSpawnSubSystem, DistrictOpenConditionSpawnSubSystem>();

            builder.Register<DistrictOpenConditionEvaluatorBootstrapSystem>(Lifetime.Scoped)
                .As<DistrictOpenConditionEvaluatorBootstrapSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<DistrictOpenConditionEvaluatorSystem>(Lifetime.Scoped)
                .As<DistrictOpenConditionEvaluatorSystem, TurnPhaseSubSystem>();

            builder.Register<DistrictSingleOpenConditionEvaluatorSubSystem>(Lifetime.Scoped)
                .As<DistrictSingleOpenConditionEvaluatorSubSystem, DistrictOpenConditionEvaluatorSubSystem>();

            builder.Register<DistrictExistConditionEvaluatorSubSystem>(Lifetime.Scoped)
                .As<DistrictExistConditionEvaluatorSubSystem, DistrictOpenConditionEvaluatorSubSystem>();

            builder.Register<DistrictOpenConditionEvaluatorTableChangedSystem>(Lifetime.Scoped)
                .As<DistrictOpenConditionEvaluatorTableChangedSystem>();
        }
    }
}
