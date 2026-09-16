using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Domains.Economy.Archetypes;
using Domains.Economy.DistrictBuildOutcome.Configs;

namespace Domains.Economy.DistrictBuildOutcome.Systems{
    // Map-creation stage: reads the loaded outcomes catalogue and lets every DI-collected subsystem walk it for
    // the entries of its own kind, materializing one entity per outcome. No domain logic of its own — entity
    // construction lives in the per-type subsystems. Fails loud when a catalogue entry got no row. Priority 930
    // keeps it in the domain-spawn cluster, after the DistrictOpenCondition spawn (920) / bootstrap (925).
    [UsedImplicitly]
    internal sealed class DistrictBuildOutcomeSpawnSystem : IPipelineStageSystem
    {
        private readonly EntityStorages _storages;

        [StateAllowed]
        private readonly IReadOnlyList<IPrioritizedUniTaskSystem> _subSystems;

        private readonly Archetype _outcomeRows;

        public AppState AppState { get; }

        public int Priority => SystemPriorities.WorldInit.DistrictBuildOutcomeSpawn;

        public DistrictBuildOutcomeSpawnSystem(
            AppState appState, EntityStorages storages, IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
        {
            AppState = appState;
            _storages = storages;
            _subSystems = OrchestratorSubSystems.SelectForOrchestrator(typeof(DistrictBuildOutcomeSpawnSystem), allSubSystems);
            _outcomeRows = EconomyArchetypes.BuildOutcome(storages.World);
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            await OrchestratorSubSystems.RunAsync(_subSystems, cancellationToken);
            EnsureEveryOutcomeSpawned();
        }

        // Every catalogue entry is validated non-null at load (IValidatableConfig) — a row missing here means an
        // outcome kind no subsystem handles (fail loud, decision c14-unhandled-entry-throws).
        private void EnsureEveryOutcomeSpawned()
        {
            var outcomes = _storages.Get<DistrictBuildOutcomesConfig>().Outcomes;

            if (_outcomeRows.Count != outcomes.Length)
            {
                var kinds = outcomes.Select(outcome => outcome.GetType().Name).Distinct();
                throw new InvalidOperationException(
                    $"DistrictBuildOutcomeSpawnSystem: {_outcomeRows.Count} outcome rows spawned, " +
                    $"{outcomes.Length} catalogue entries expected. Catalogue kinds: {string.Join(", ", kinds)}.");
            }
        }

        public void Dispose()
        {
        }
    }
}
