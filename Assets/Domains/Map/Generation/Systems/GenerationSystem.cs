using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domains.Map.Archetypes;
using Domains.Map.Generation.Components;
using Domains.Map.Generation.Utils;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.Hex.Utils;
using Core;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Unity.Collections;
using Domains.Map.Generation.Configs;

namespace Domains.Map.Generation.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 100). Creates the hex grid, runs generation subsystems
    ///     by priority, and synchronizes terrain tags with the final level of each hex.
    ///     Run by MapCreation's entry pipeline in Priority order, not by an event subscription.
    /// </summary>
    [UsedImplicitly]
    internal sealed class GenerationSystem : IPipelineStageSystem
    {
        private readonly Archetype _hexArchetype;
        private readonly EntityStorages _storages;

        [StateAllowed]
        private readonly IReadOnlyList<IPrioritizedUniTaskSystem> _subSystems;

        /// <inheritdoc />
        public AppState AppState { get; }

        /// <inheritdoc />
        public int Priority => SystemPriorities.WorldInit.Generation;

        /// <param name="appState">The game state this pipeline stage belongs to.</param>
        /// <param name="storages">Named ECS storages used to query and populate the game world.</param>
        /// <param name="allSubSystems">Every sub-system on the contract; this host keeps the ones naming it.</param>
        public GenerationSystem(AppState appState, EntityStorages storages, IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
        {
            AppState = appState;
            _storages = storages;
            _hexArchetype = MapArchetypes.Hex(storages.World);

            _subSystems = OrchestratorSubSystems.SelectForOrchestrator(typeof(GenerationSystem), allSubSystems);
        }

        /// <inheritdoc />
        public async UniTask Execute(CancellationToken cancellationToken)
        {
            var config = _storages.Get<TerrainGenerationConfig>();

            Generate(config);
            await OrchestratorSubSystems.RunAsync(_subSystems, cancellationToken);
            AssignHexTypes();
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>
        ///     Assigns each hex its terrain type from the final generated level. HexTypeComponent is a birth
        ///     column (default <see cref="HexType.Unknown" />) on every hex, but Friflo still throws
        ///     <see cref="StructuralChangeException" /> on any <c>AddComponent</c> call while enumerating the
        ///     query it is called from — value-only upsert included — so this snapshots ids first, same idiom
        ///     as <c>ForestDespawnSystem</c>.
        /// </summary>
        private void AssignHexTypes()
        {
            var hexIds = new NativeList<int>(_hexArchetype.Count, Allocator.Temp);
            foreach (var entity in _hexArchetype.Entities)
                hexIds.Add(entity.Id);

            for (var i = 0; i < hexIds.Length; i++)
            {
                if (_storages.World.TryGetEntityById(hexIds[i], out var entity))
                {
                    entity.AddComponent(entity.GetComponent<HexLevelComponent>().ToHexType());
                }
            }

            hexIds.Dispose();
        }

        /// <summary>Creates the flat hex grid for the configured number of waves.</summary>
        /// <param name="config">Terrain generation parameters.</param>
        private void Generate(TerrainGenerationConfig config)
        {
            var hexCount = HexesUtil.GetTotalHexCountForWaves(config.WaveCount);

            for (var index = 0; index < hexCount; index++)
            {
                var hexCoords = HexesUtil.IndexToAxialCoords(index);

                var entity = _hexArchetype.CreateEntity();
                entity.AddComponent(new HexIdPKComponent { Coords = hexCoords });
                entity.AddComponent(new HexLevelComponent { Level = 0 });
            }
        }
    }
}
