using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domains.Map.Archetypes;
using Domains.Map.Generation.Components;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.Hex.Utils;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Unity.Collections;

namespace Domains.Map.Generation.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 100). Creates the hex grid, runs generation subsystems
    ///     by priority, and synchronizes terrain tags with the final level of each hex.
    ///     Driven by the Boot world-init orchestrator, not by an event subscription.
    /// </summary>
    [UsedImplicitly]
    internal sealed class GenerationSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private const int FoothillLevel = 1;
        private const int MountainLevel = 2;
        private const int PlainLevel = 0;
        private const int WaterLevel = -1;

        private readonly IReadOnlyList<GenerationSubSystem> _generationSubSystems;
        private readonly Archetype _hexArchetype;
        private readonly EntityStorages _storages;

        /// <inheritdoc />
        public int Priority => SystemPriorities.WorldInit.Generation;

        /// <param name="storages">Named ECS storages used to query and populate the game world.</param>
        /// <param name="generationSubSystems">Generation subsystems executed in priority order.</param>
        public GenerationSystem(EntityStorages storages, IReadOnlyList<GenerationSubSystem> generationSubSystems)
        {
            _storages = storages;
            _hexArchetype = MapArchetypes.Hex(storages.World);

            _generationSubSystems = generationSubSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        /// <inheritdoc />
        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (!_storages.World.HasWorldComponent<TerrainGenerationConfigComponent>())
                return UniTask.CompletedTask;

            var config = _storages.World.GetWorldComponent<TerrainGenerationConfigComponent>();

            Generate(in config);
            RunGenerationSubSystems();
            AssignHexTypes();

            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>Creates the flat hex grid for the configured number of waves.</summary>
        /// <param name="config">Terrain generation parameters.</param>
        private void Generate(in TerrainGenerationConfigComponent config)
        {
            var hexCount = HexesUtil.GetTotalHexCountForWaves(config.WaveCount);

            for (var index = 0; index < hexCount; index++)
            {
                var hexCoords = HexesUtil.IndexToAxialCoords(index);

                var entity = _hexArchetype.CreateEntity();
                entity.AddComponent(new HexIdComponent { Coords = hexCoords });
                entity.AddComponent(new HexLevelComponent { Level = 0 });
            }
        }

        private void RunGenerationSubSystems()
        {
            // Generation is one-shot; deltaTime is irrelevant, so a default GameState is passed through.
            var state = default(GameState);

            foreach (var generationSubSystem in _generationSubSystems)
            {
                if (!generationSubSystem.IsEnabled)
                    continue;

                generationSubSystem.Update(state);
            }
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
                if (_storages.World.TryGetEntityById(hexIds[i], out var entity))
                {
                    var level = entity.GetComponent<HexLevelComponent>().Level;
                    entity.AddComponent(new HexTypeComponent { Type = LevelToType(level) });
                }

            hexIds.Dispose();
        }

        // Unmapped/unexpected levels fall back to Plain (matches the old "no terrain tag → Plain" default).
        private static HexType LevelToType(int level) => level switch
        {
            MountainLevel => HexType.Mount,
            FoothillLevel => HexType.Bedhill,
            WaterLevel => HexType.Water,
            _ => HexType.Plain
        };
    }
}
