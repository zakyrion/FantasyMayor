using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Core.Data;
using Core.DataLayer;
using Core.EventDataBus;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Modules.AppFlow.Data;
using Modules.AppFlow.DataLayers;
using Modules.Hexes.DataLayer;
using Modules.Hexes.DataTypes;
using Modules.Hexes.Operations;
using UnityEngine;

namespace Modules.Hexes.Systems
{
    [UsedImplicitly]
    public class HexesGenerateProcessor : IProcessor<HexesGenerateOperation>, IDisposable
    {
        private readonly IDataContainer<AppFlowDataLayer> _appFlowContainer;

        private readonly DataSubscribeResult _appFlowSubscription;
        private readonly IBus<HexesGenerateOperation> _busGenerate;
        private readonly IDataContainer<HexesDataLayer> _dataContainer;
        private readonly IDataContainer<HexesSettingsDataLayer> _settingsDataContainer;

        public HexesGenerateProcessor(IBus<HexesGenerateOperation> busGenerate, IDataHub dataHub)
        {
            _busGenerate = busGenerate;
            _dataContainer = dataHub.GetDataLayer<HexesDataLayer>();
            _appFlowContainer = dataHub.GetDataLayer<AppFlowDataLayer>();
            _settingsDataContainer = dataHub.GetDataLayer<HexesSettingsDataLayer>();

            _busGenerate.Subscribe(this, 0);
            _appFlowSubscription = _appFlowContainer.SubscribeOnUpdate(CreateNewTerrain, 0);
        }

        public void Dispose()
        {
            _busGenerate.Unsubscribe(0);
            _appFlowContainer.UnsubscribeOnUpdate(_appFlowSubscription.SubscriptionId);
        }


        public async UniTask ProcessAsync(HexesGenerateOperation data, CancellationToken cancellationToken)
        {
            Debug.Log("[skh] process");
            var wave = 0;
            var hexesToSpawn = new List<HexId>
            {
                new()
            };

            while (wave++ < data.WaveCount)
            {
                var count = hexesToSpawn.Count;
                for (var i = 0; i < count; i++)
                {
                    var hexId = hexesToSpawn[i];
                    var neighbours = HexUtil.Neighbours(hexId.Coords);
                    foreach (var neighbour in neighbours)
                    {
                        if (hexesToSpawn.Contains(neighbour))
                            continue;

                        hexesToSpawn.Add(neighbour);
                    }
                }
            }

            Debug.Log($"[skh] process end. Count: {hexesToSpawn.Count}");
            var dataLayer = new HexesDataLayer();

            var hexes = new List<HexData>(hexesToSpawn.Count);
            foreach (var hexId in hexesToSpawn)
            {
                hexes.Add(new HexData
                {
                    HexId = hexId
                });
            }

            dataLayer.Hexes = hexes.ToImmutableArray();
            await _dataContainer.AddOrUpdateAsync(dataLayer, cancellationToken);
        }

        private async UniTask CreateNewTerrain(DataContext<AppFlowDataLayer> dataContext, CancellationToken cancellationToken)
        {
            if (dataContext.New.DataLayer.State != AppState.GenerateNewGame)
            {
                return;
            }

            var settings = await _settingsDataContainer.GetAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested || !settings.Exist)
            {
                return;
            }

            Debug.Log("[skh] process");
            var wave = 0;
            var hexesToSpawn = new List<HexId>
            {
                new()
            };

            while (wave++ < settings.DataLayer.WaveCount)
            {
                var count = hexesToSpawn.Count;
                for (var i = 0; i < count; i++)
                {
                    var hexId = hexesToSpawn[i];
                    var neighbours = HexUtil.Neighbours(hexId.Coords);
                    foreach (var neighbour in neighbours)
                    {
                        if (hexesToSpawn.Contains(neighbour))
                            continue;

                        hexesToSpawn.Add(neighbour);
                    }
                }
            }

            Debug.Log($"[skh] process end. Count: {hexesToSpawn.Count}");
            var dataLayer = new HexesDataLayer();

            var hexes = new List<HexData>(hexesToSpawn.Count);
            foreach (var hexId in hexesToSpawn)
            {
                hexes.Add(new HexData
                {
                    HexId = hexId
                });
            }

            dataLayer.Hexes = hexes.ToImmutableArray();
            await _dataContainer.AddOrUpdateAsync(dataLayer, cancellationToken);
        }
    }
}
