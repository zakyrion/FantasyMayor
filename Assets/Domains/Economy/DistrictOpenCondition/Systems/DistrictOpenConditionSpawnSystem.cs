using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Domains.Economy.DistrictOpenCondition.Configs;
using Domains.Economy.DistrictOpenCondition.Tags;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Map-creation stage: reads the loaded conditions catalogue and lets every DI-collected subsystem walk it for
    // the entries of its own kind, materializing one entity per condition. No domain logic of its own — entity
    // construction lives in the per-type subsystems. Fails loud when a catalogue entry got no row.
    [UsedImplicitly]
    internal sealed class DistrictOpenConditionSpawnSystem : IPipelineStageSystem
    {
        private readonly EntityStorages _storages;

        [StateAllowed]
        private readonly IReadOnlyList<IPrioritizedUniTaskSystem> _subSystems;

        private readonly ArchetypeQuery _conditionRows;

        public AppState AppState { get; }

        public int Priority => SystemPriorities.WorldInit.DistrictOpenConditionSpawn;

        public DistrictOpenConditionSpawnSystem(
            AppState appState, EntityStorages storages, IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
        {
            AppState = appState;
            _storages = storages;
            _subSystems = OrchestratorSubSystems.SelectForOrchestrator(typeof(DistrictOpenConditionSpawnSystem), allSubSystems);
            _conditionRows = storages.World.Query().AllTags(Friflo.Engine.ECS.Tags.Get<DistrictOpenConditionTag>());
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            await OrchestratorSubSystems.RunAsync(_subSystems, cancellationToken);
            EnsureEveryConditionSpawned();
        }

        // Every catalogue entry is validated non-null at load (IValidatableConfig) — a row missing here means a
        // condition kind no subsystem handles (fail loud, decision c14-unhandled-entry-throws).
        private void EnsureEveryConditionSpawned()
        {
            var conditions = _storages.Get<DistrictOpenConditionsConfig>().Conditions;

            if (_conditionRows.Count != conditions.Length)
            {
                var kinds = conditions.Select(condition => condition.GetType().Name).Distinct();
                throw new InvalidOperationException(
                    $"DistrictOpenConditionSpawnSystem: {_conditionRows.Count} condition rows spawned, " +
                    $"{conditions.Length} catalogue entries expected. Catalogue kinds: {string.Join(", ", kinds)}.");
            }
        }

        public void Dispose()
        {
        }
    }
}
