using System.Collections.Generic;
using System.Linq;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.HexesCore.Components;
using Modules.HexesCore.Utils;
using Modules.TerrainGenerator.Components;
using UnityEngine;

namespace Modules.TerrainGenerator.Systems
{
    /// <summary>
    ///     Reacts to <see cref="TerrainGenerationGenerateEventComponent" /> entities and runs terrain generation.
    ///     Consumes the event entity after processing.
    /// </summary>
    [UsedImplicitly]
    internal sealed class TerrainGenerationSystem : UpdatedSystem
    {
        private const int FoothillLevel = 1;
        private const int ExecutionPriority = 100;
        private const int MountainLevel = 2;
        private const int WaterLevel = -1;

        private readonly EntitySet _configSet;
        private readonly IReadOnlyList<TerrainGenerationSubSystem> _generationSubSystems;
        private readonly EntitySet _hexSet;

        /// <inheritdoc />
        public override int Priority => ExecutionPriority;

        /// <param name="world">The ECS world to query.</param>
        public TerrainGenerationSystem(World world, IReadOnlyList<TerrainGenerationSubSystem> generationSubSystems)
            : base(world.GetEntities()
                .WhenAdded<TerrainGenerationGenerateEventComponent>()
                .AsSet())
        {
            Debug.Log("[skh] init terrain generation system");
            _configSet = world.GetEntities()
                .With<TerrainGenerationConfigComponent>()
                .AsSet();
            _hexSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexLevelComponent>()
                .AsSet();

            _generationSubSystems = generationSubSystems
                .OrderBy(system => system.Priority)
                .ToArray();

            Debug.Log($"[skh] generation sub systems: {_generationSubSystems.Count}");
        }

        /// <inheritdoc />
        protected override void Update(GameState state, in Entity entity)
        {
            if (_configSet.Count == 0)
            {
                entity.Dispose();
                return;
            }

            ref readonly var config = ref _configSet.GetEntities()[0]
                .Get<TerrainGenerationConfigComponent>();

            Generate(entity.World, in config);
            RunGenerationSubSystems(state);
            SyncHexTags();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();
            _configSet.Dispose();
            _hexSet.Dispose();
        }

        /// <summary>Runs the terrain generation pipeline using the loaded config.</summary>
        /// <param name="config">Terrain generation parameters.</param>
        private void Generate(World world, in TerrainGenerationConfigComponent config)
        {
            Debug.Log($"[skh] generate config: {config}");
            var hexCount = HexesUtil.GetTotalHexCountForWaves(config.WaveCount);

            for (var index = 0; index < hexCount; index++)
            {
                var hexCoords = HexesUtil.IndexToAxialCoords(index);

                var entity = world.CreateEntity();
                entity.Set(new HexIdComponent { Coords = hexCoords });
                entity.Set(new HexLevelComponent { Level = 0 });
            }
        }

        private void RunGenerationSubSystems(in GameState state)
        {
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
