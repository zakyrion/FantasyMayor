using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Modules.HexCore.Components;
using Modules.HexCore.Tags;
using Modules.HexesCore.Utils;
using Modules.TerrainGenerator.Components;

namespace Modules.TerrainGenerator.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 100). Creates the hex grid, runs generation subsystems
    ///     by priority, and synchronizes terrain tags with the final level of each hex.
    ///     Driven by the Boot world-init orchestrator, not by an event subscription.
    /// </summary>
    [UsedImplicitly]
    internal sealed class TerrainGenerationSystem : IPrioritizedUniTaskSystem<TerrainGenerationStep>
    {
        private const int ExecutionPriority = 100;
        private const int FoothillLevel = 1;
        private const int MountainLevel = 2;
        private const int PlainLevel = 0;
        private const int WaterLevel = -1;

        private readonly IReadOnlyList<TerrainGenerationSubSystem> _generationSubSystems;
        private readonly EntitySet _hexSet;
        private readonly World _world;

        /// <inheritdoc />
        public int Priority => ExecutionPriority;

        /// <param name="world">The ECS world to query and populate.</param>
        /// <param name="generationSubSystems">Generation subsystems executed in priority order.</param>
        public TerrainGenerationSystem(World world, IReadOnlyList<TerrainGenerationSubSystem> generationSubSystems)
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
        public UniTask Update(TerrainGenerationStep state, CancellationToken cancellationToken)
        {
            if (!_world.Has<TerrainGenerationConfigComponent>())
                return UniTask.CompletedTask;

            ref readonly var config = ref _world.Get<TerrainGenerationConfigComponent>();

            Generate(in config);
            RunGenerationSubSystems();
            SyncHexTags();

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
        ///     Synchronizes terrain classification tags with the final generated level of each hex entity.
        /// </summary>
        private void SyncHexTags()
        {
            foreach (ref readonly var entity in _hexSet.GetEntities())
            {
                var level = entity.Get<HexLevelComponent>().Level;

                SyncTag<HexPlainTag>(entity, level == PlainLevel);
                SyncTag<HexMountTag>(entity, level == MountainLevel);
                SyncTag<HexWaterTag>(entity, level == WaterLevel);
                SyncTag<HexBedhillTag>(entity, level == FoothillLevel);
            }
        }

        /// <summary>
        ///     Adds or removes a tag component so it matches the requested state.
        /// </summary>
        /// <typeparam name="TTag">Tag component type to synchronize.</typeparam>
        /// <param name="entity">Entity whose tag should be updated.</param>
        /// <param name="shouldExist">Whether the tag should be present after synchronization.</param>
        private void SyncTag<TTag>(Entity entity, bool shouldExist)
            where TTag : struct
        {
            var hasTag = entity.Has<TTag>();

            if (shouldExist && !hasTag)
            {
                entity.Set<TTag>();
                return;
            }

            if (!shouldExist && hasTag)
                entity.Remove<TTag>();
        }
    }
}
