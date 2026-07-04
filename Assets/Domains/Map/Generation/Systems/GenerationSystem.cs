using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;
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
        private const int ExecutionPriority = 100;
        private const int FoothillLevel = 1;
        private const int MountainLevel = 2;
        private const int PlainLevel = 0;
        private const int WaterLevel = -1;

        private readonly IReadOnlyList<GenerationSubSystem> _generationSubSystems;
        private readonly EntitySet _hexSet;
        private readonly World _world;

        /// <inheritdoc />
        public int Priority => ExecutionPriority;

        /// <param name="world">The ECS world to query and populate.</param>
        /// <param name="generationSubSystems">Generation subsystems executed in priority order.</param>
        public GenerationSystem(World world, IReadOnlyList<GenerationSubSystem> generationSubSystems)
        {
            _world = world;
            _hexSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexLevelComponent>()
                .AsSet();

            _generationSubSystems = generationSubSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        /// <inheritdoc />
        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (!_world.Has<TerrainGenerationConfigComponent>())
                return UniTask.CompletedTask;

            ref readonly var config = ref _world.Get<TerrainGenerationConfigComponent>();

            Generate(in config);
            RunGenerationSubSystems();
            AssignHexTypes();

            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _hexSet.Dispose();
        }

        /// <summary>Creates the flat hex grid for the configured number of waves.</summary>
        /// <param name="config">Terrain generation parameters.</param>
        private void Generate(in TerrainGenerationConfigComponent config)
        {
            var hexCount = HexesUtil.GetTotalHexCountForWaves(config.WaveCount);

            for (var index = 0; index < hexCount; index++)
            {
                var hexCoords = HexesUtil.IndexToAxialCoords(index);

                var entity = _world.CreateEntity();
                entity.Set(new HexIdComponent { Coords = hexCoords });
                entity.Set(new HexLevelComponent { Level = 0 });
                entity.Set(new HexTag());
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
        ///     Assigns each hex its terrain type — a single <see cref="HexTypeComponent" /> column — from
        ///     the final generated level. Written via Set so any maintained
        ///     <c>AsMultiMap&lt;HexTypeComponent&gt;</c> index stays in sync.
        /// </summary>
        private void AssignHexTypes()
        {
            foreach (ref readonly var entity in _hexSet.GetEntities())
            {
                var level = entity.Get<HexLevelComponent>().Level;
                entity.Set(new HexTypeComponent { Type = LevelToType(level) });
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
