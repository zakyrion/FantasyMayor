using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Domains.Economy.DistrictBuildOutcome.Configs;
using Domains.Economy.DistrictBuildOutcome.Components;

namespace Domains.Economy.DistrictBuildOutcome.Systems{
    // Pipeline Orchestrator (MapGenerationStep, one-shot): reads the loaded outcomes catalogue and routes each
    // authored config to the subsystem that handles its concrete type, materializing one entity per outcome.
    // No domain logic of its own — entity construction lives in the per-type subsystems. Fails loud on a null
    // entry or a config type no subsystem handles. Priority 930 keeps it in the domain-spawn cluster, after the
    // DistrictOpenCondition spawn (920) / bootstrap (925).
    [UsedImplicitly]
    internal sealed class DistrictBuildOutcomeSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private readonly EntityStorages _storages;

        [StateAllowed]
        private readonly IReadOnlyList<DistrictBuildOutcomeSpawnSubSystem> _subSystems;

        public int Priority => SystemPriorities.WorldInit.DistrictBuildOutcomeSpawn;

        public DistrictBuildOutcomeSpawnSystem(
            EntityStorages storages, IReadOnlyList<DistrictBuildOutcomeSpawnSubSystem> subSystems)
        {
            _storages = storages;
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (!_storages.World.HasWorldComponent<DistrictBuildOutcomesConfigComponent>())
                throw new InvalidOperationException(
                    "BuildDistrictOutcomeSpawnSystem: BuildDistrictOutcomesConfigComponent is missing.");

            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            var outcomes = _storages.World.GetWorldComponent<DistrictBuildOutcomesConfigComponent>().Value.Outcomes;

            for (var index = 0; index < outcomes.Length; index++)
            {
                var outcome = outcomes[index];
                if (outcome == null)
                    throw new InvalidOperationException(
                        $"BuildDistrictOutcomeSpawnSystem: outcome entry at index {index} is null.");

                if (!TrySpawn(outcome))
                    throw new InvalidOperationException(
                        $"BuildDistrictOutcomeSpawnSystem: no subsystem handles outcome type " +
                        $"{outcome.GetType().Name}.");
            }

            return UniTask.CompletedTask;
        }

        private bool TrySpawn(DistrictBuildOutcomeConfig outcome)
        {
            for (var i = 0; i < _subSystems.Count; i++)
            {
                if (!_subSystems[i].IsEnabled)
                    continue;

                if (_subSystems[i].TrySpawn(outcome))
                    return true;
            }

            return false;
        }

        public void Dispose()
        {
        }
    }
}
