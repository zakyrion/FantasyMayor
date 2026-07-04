using DefaultECSExtensions;
using Domains.Economy.District.Systems;
using Domains.Economy.DistrictBuildCost.Systems;
using Domains.Economy.DistrictBuildOutcome.Systems;
using Domains.Economy.DistrictOpenCondition.Systems;
using Modules.Boot.Core;
using Modules.Turn.Systems;
using VContainer;
using VContainer.Unity;

namespace Domains.Economy.Installer
{
    // Economy ships the District config loader (catalogue of buildable districts) and the district-open-condition
    // config loader + spawn pipeline. Resource is data types + the generic ResourceLoadoutSpawner mechanism.
    // District build/operate systems will register here later.
    public sealed class EconomyInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<DistrictsBuildConfigLoaderSystem>(Lifetime.Singleton)
                .As<DistrictsBuildConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<DistrictBuildCostsConfigLoaderSystem>(Lifetime.Singleton)
                .As<DistrictBuildCostsConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<DistrictBuildOutcomesConfigLoaderSystem>(Lifetime.Singleton)
                .As<DistrictBuildOutcomesConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<DistrictBuildOutcomeSpawnSystem>(Lifetime.Singleton)
                .As<DistrictBuildOutcomeSpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<SpawnCityCenterOutcomeSubSystem>(Lifetime.Singleton)
                .As<SpawnCityCenterOutcomeSubSystem, DistrictBuildOutcomeSpawnSubSystem>();

            builder.Register<DistrictOpenConditionsConfigLoaderSystem>(Lifetime.Singleton)
                .As<DistrictOpenConditionsConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<DistrictOpenConditionSpawnSystem>(Lifetime.Singleton)
                .As<DistrictOpenConditionSpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<DistrictExistConditionSpawnSubSystem>(Lifetime.Singleton)
                .As<DistrictExistConditionSpawnSubSystem, DistrictOpenConditionSpawnSubSystem>();

            builder.Register<DistrictSingleOpenConditionSpawnSubSystem>(Lifetime.Singleton)
                .As<DistrictSingleOpenConditionSpawnSubSystem, DistrictOpenConditionSpawnSubSystem>();

            builder.Register<DistrictOpenConditionEvaluatorBootstrapSystem>(Lifetime.Singleton)
                .As<DistrictOpenConditionEvaluatorBootstrapSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<DistrictOpenConditionEvaluatorSystem>(Lifetime.Singleton)
                .As<DistrictOpenConditionEvaluatorSystem, TurnPhaseSubSystem>();

            builder.Register<DistrictSingleOpenConditionEvaluatorSubSystem>(Lifetime.Singleton)
                .As<DistrictSingleOpenConditionEvaluatorSubSystem, DistrictOpenConditionEvaluatorSubSystem>();
        }
    }
}
