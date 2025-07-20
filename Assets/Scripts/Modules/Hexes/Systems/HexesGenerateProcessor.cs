using System;
using System.Collections.Generic;
using System.Threading;
using Core.DataLayer;
using Core.EventDataBus;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Modules.Hexes.DataLayer;
using Modules.Hexes.DataTypes;
using Modules.Hexes.Operations;
using UnityEngine;

namespace Modules.Hexes.Systems
{
    [UsedImplicitly]
    public class HexesGenerateProcessor : IProcessor<HexesGenerateOperation>, IDisposable
    {
        private readonly IBus<HexesGenerateOperation> _busGenerate;
        private readonly IDataContainer<HexesDataLayer> _dataContainer;

        public HexesGenerateProcessor(IBus<HexesGenerateOperation> busGenerate, IDataContainer<HexesDataLayer> dataContainer)
        {
            _busGenerate = busGenerate;
            _dataContainer = dataContainer;

            _busGenerate.Subscribe(this, 0);
        }

        public void Dispose()
        {
            _busGenerate.Unsubscribe(0);
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

            var container = await _dataContainer.GetAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Debug.Log($"[skh] process end. Count: {hexesToSpawn.Count}");
            var dataLayer = container.DataLayer;

            if (container.Exist)
            {
                dataLayer.Hexes.Clear();
            }

            dataLayer.Hexes = new List<HexData>();
            foreach (var hexId in hexesToSpawn)
            {
                dataLayer.Hexes.Add(new HexData
                {
                    HexId = hexId
                });
            }

            await _dataContainer.AddOrUpdateAsync(dataLayer, cancellationToken);
        }
    }
}
