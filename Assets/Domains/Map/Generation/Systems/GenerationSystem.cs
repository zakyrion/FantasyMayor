using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.Hex.Tags;
using Domains.Map.Hex.Utils;
using Domains.Map.Generation.Components;

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
        private readonly ArchetypeQuery _hexQuery;
        private readonly Archetype _hexArchetype;
        private readonly EntityStore _world;

        /// <inheritdoc />
        public int Priority => SystemPriorities.WorldInit.Generation;

        /// <param name="world">The ECS world to query and populate.</param>
        /// <param name="generationSubSystems">Generation subsystems executed in priority order.</param>
        public GenerationSystem(EntityStore world, IReadOnlyList<GenerationSubSystem> generationSubSystems)
        {
            _world = world;
            _hexQuery = world.Query<HexIdComponent, HexLevelComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexTag>());
            _hexArchetype = MapArchetypes.Hex(world);

            _generationSubSystems = generationSubSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        /// <inheritdoc />
        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (!_world.HasWorldComponent<TerrainGenerationConfigComponent>())
                return UniTask.CompletedTask;

            var config = _world.GetWorldComponent<TerrainGenerationConfigComponent>();

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
        ///     Assigns each hex its terrain type from the final generated level. HexTypeComponent is a
        ///     birth column (default <see cref="HexType.Unknown" />) on every hex, so this is a plain value
        ///     upsert into an existing column — never a structural change — and needs no snapshot.
        /// </summary>
        private void AssignHexTypes()
        {
            foreach (var entity in _hexQuery.Entities)
            {
                var level = entity.GetComponent<HexLevelComponent>().Level;
                entity.AddComponent(new HexTypeComponent { Type = LevelToType(level) });
            }
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
