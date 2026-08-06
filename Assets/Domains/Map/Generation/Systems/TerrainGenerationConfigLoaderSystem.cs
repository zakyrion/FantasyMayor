using Core;
using Cysharp.Threading.Tasks;
using Domains.Map.Generation.Components;
using Domains.Map.Generation.Configs;
using Domains.Map.Generation.Data;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using System.Threading;
using System;

namespace Domains.Map.Generation.Systems
{
    [UsedImplicitly]
    internal class TerrainGenerationConfigLoaderSystem : ConfigLoaderSystem
    {
        private readonly EntityStorages _storages;
        private const string TERRAIN_GENERATION_CONFIG = "TerrainGenerationConfig";

        public TerrainGenerationConfigLoaderSystem(IAddressable addressable, EntityStorages storages)
            : base(addressable)
        {
            _storages = storages;
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var terrainGenerationConfig = Box<TerrainGenerationConfig>.Empty();

            try
            {
                terrainGenerationConfig = await LoadConfigAsync<TerrainGenerationConfig>(TERRAIN_GENERATION_CONFIG, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return;

                var config = terrainGenerationConfig.Value;

                _storages.Singletons.Set(TerrainGenerationConfigComponent.FromConfig(config));
                CreateMountainConfigComponent(config);
                CreateWaterConfigComponent(config);

                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref terrainGenerationConfig);
            }
        }

        private void CreateMountainConfigComponent(TerrainGenerationConfig config)
        {
            _storages.Singletons.Set(new MountainConfigComponent
            {
                SizeFraction         = config.HillSizeFraction,
                SeedCount            = config.SeedCount,
                MaxSeedDistance      = config.MaxSeedDistance,
                MinFoothillNeighbors = config.MinFoothillNeighbors,
                MinFoothillCount     = config.MinFoothillCount,
                MinFoothillDistance  = config.MinFoothillDistance
            });
        }

        /// <summary>
        ///     Creates a water-type-specific config component entity.
        ///     <see cref="WaterType.River"/>, <see cref="WaterType.Lake"/> and
        ///     <see cref="WaterType.Sea"/> create dedicated config entities.
        /// </summary>
        private void CreateWaterConfigComponent(TerrainGenerationConfig config)
        {
            switch (config.WaterType)
            {
                case WaterType.River:
                    CreateRiverConfigComponent(config.RiverConfig);
                    break;
                case WaterType.Lake:
                    CreateLakeConfigComponent(config.LakeConfig);
                    break;
                case WaterType.Sea:
                    CreateSeaConfigComponent(config.SeaConfig);
                    break;
                case WaterType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(config.WaterType), config.WaterType, "Unhandled WaterType.");
            }
        }

        private void CreateRiverConfigComponent(RiverConfig riverConfig)
        {
            if (riverConfig == null)
                throw new InvalidOperationException($"{nameof(RiverConfig)} is not assigned in TerrainGenerationConfig but WaterType is River.");

            _storages.Singletons.Set(new RiverConfigComponent
            {
                CornerOffsetTiles = riverConfig.CornerOffsetTiles
            });
        }

        private void CreateLakeConfigComponent(LakeConfig lakeConfig)
        {
            if (lakeConfig == null)
                throw new InvalidOperationException($"{nameof(LakeConfig)} is not assigned in TerrainGenerationConfig but WaterType is Lake.");

            _storages.Singletons.Set(new LakeConfigComponent
            {
                EdgeMarginTiles = lakeConfig.EdgeMarginTiles,
                SizeFraction = lakeConfig.SizeFraction
            });
        }

        private void CreateSeaConfigComponent(SeaConfig seaConfig)
        {
            if (seaConfig == null)
                throw new InvalidOperationException($"{nameof(SeaConfig)} is not assigned in TerrainGenerationConfig but WaterType is Sea.");

            _storages.Singletons.Set(new SeaConfigComponent
            {
                SizeFraction = seaConfig.SizeFraction
            });
        }
    }
}
