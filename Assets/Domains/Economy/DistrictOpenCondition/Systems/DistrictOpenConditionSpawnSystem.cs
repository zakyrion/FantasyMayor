using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Economy.DistrictOpenCondition.Components;
using Domains.Economy.DistrictOpenCondition.Configs;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Pipeline Orchestrator (MapGenerationStep, one-shot): reads the loaded conditions catalogue and routes each
    // authored config to the subsystem that handles its concrete type, materializing one entity per condition.
    // No domain logic of its own — entity construction lives in the per-type subsystems. Fails loud on a null
    // entry or a config type no subsystem handles.
    [UsedImplicitly]
    internal sealed class DistrictOpenConditionSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private readonly World _world;

        [StateAllowed]
        private readonly IReadOnlyList<DistrictOpenConditionSpawnSubSystem> _subSystems;

        public int Priority => SystemPriorities.WorldInit.DistrictOpenConditionSpawn;

        public DistrictOpenConditionSpawnSystem(
            World world, IReadOnlyList<DistrictOpenConditionSpawnSubSystem> subSystems)
        {
            _world = world;
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (!_world.Has<DistrictOpenConditionsConfigComponent>())
                throw new InvalidOperationException(
                    "DistrictOpenConditionSpawnSystem: DistrictOpenConditionsConfigComponent is missing.");

            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            var conditions = _world.Get<DistrictOpenConditionsConfigComponent>().Value.Conditions;

            for (var index = 0; index < conditions.Length; index++)
            {
                var condition = conditions[index];
                if (condition == null)
                    throw new InvalidOperationException(
                        $"DistrictOpenConditionSpawnSystem: condition entry at index {index} is null.");

                if (!TrySpawn(condition))
                    throw new InvalidOperationException(
                        $"DistrictOpenConditionSpawnSystem: no subsystem handles condition type " +
                        $"{condition.GetType().Name}.");
            }

            return UniTask.CompletedTask;
        }

        private bool TrySpawn(DistrictOpenConditionConfig condition)
        {
            for (var i = 0; i < _subSystems.Count; i++)
            {
                if (!_subSystems[i].IsEnabled)
                    continue;

                if (_subSystems[i].TrySpawn(condition))
                    return true;
            }

            return false;
        }

        public void Dispose()
        {
        }
    }
}
