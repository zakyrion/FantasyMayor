using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Core.Data;
using Core.DataLayer;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Modules.Hexes.Creators;
using Modules.Hexes.DataLayer;
using Modules.Hexes.DataTypes;
using UnityEngine;

namespace Modules.Hexes.ViewManagers
{
    [UsedImplicitly]
    internal class HexesViewManager : IDisposable
    {
        private readonly IHexesCreator _hexesCreator;
        private readonly IDataContainer<HexesDataLayer> _hexesDataLayer;
        private readonly DataSubscribeResult _hexesDataLayerSubscription;
        private readonly IDataContainer<HexesViewDataLayer> _hexesViewDataLayer;

        public HexesViewManager(IDataHub dataHub, IHexesCreator hexesCreator)
        {
            _hexesDataLayer = dataHub.GetDataLayer<HexesDataLayer>();
            _hexesViewDataLayer = dataHub.GetDataLayer<HexesViewDataLayer>();
            _hexesCreator = hexesCreator;

            _hexesDataLayerSubscription = _hexesDataLayer.SubscribeOnUpdate(ProcessHexesDataLayer, 0);
        }

        public void Dispose()
        {
            if (_hexesDataLayerSubscription.SubscribeResult == SubscribeResult.Success)
            {
                _hexesDataLayer.UnsubscribeOnUpdate(_hexesDataLayerSubscription.SubscriptionId);
            }
        }

        private async UniTask<Box<HexView>[]> CreateHexViewsAsync(IEnumerable<HexData> hexDatas, CancellationToken token)
        {
            var spawnTasks = new List<UniTask<Box<HexView>>>();
            foreach (var hex in hexDatas)
            {
                spawnTasks.Add(_hexesCreator.CreateHexAsync(token));
            }

            var views = await UniTask.WhenAll(spawnTasks);
            if (token.IsCancellationRequested)
            {
                foreach (var view in views)
                {
                    view.Dispose();
                }
                return null;
            }

            return views;
        }

        private async UniTask ProcessHexesDataLayer(DataContext<HexesDataLayer> context, CancellationToken token)
        {
            Debug.Log($"[skh] process. Old: {context.Old.Exist}, New: {context.New.Exist}");

            var viewDataLayerResult = await _hexesViewDataLayer.GetAsync(token);
            if (token.IsCancellationRequested || !viewDataLayerResult.Exist)
            {
                return;
            }

            var viewDataLayer = viewDataLayerResult.DataLayer;

            if (!context.Old.Exist)
            {
                var views = await CreateHexViewsAsync(context.New.DataLayer.Hexes, token);

                for (var i = 0; i < views.Length; i++)
                {
                    viewDataLayer.HexViews = viewDataLayer.HexViews.Add(context.New.DataLayer.Hexes[i].HexId, views[i]);
                }

                await _hexesViewDataLayer.AddOrUpdateAsync(viewDataLayer, token);
                return;
            }

            var newHexes = context.New.DataLayer.Hexes.Except(context.Old.DataLayer.Hexes);
            var deletedHexes = context.Old.DataLayer.Hexes.Except(context.New.DataLayer.Hexes);

            var newHexesViews = await CreateHexViewsAsync(newHexes, token);
            var index = 0;
            foreach (var hexData in newHexes)
            {
                viewDataLayer.HexViews = viewDataLayer.HexViews.Add(hexData.HexId, newHexesViews[index]);
                index++;
            }

            foreach (var hexData in deletedHexes)
            {
                if (viewDataLayer.HexViews.TryGetValue(hexData.HexId, out var view))
                {
                    view.Dispose();
                    viewDataLayer.HexViews = viewDataLayer.HexViews.Remove(hexData.HexId);
                }
            }

            await _hexesViewDataLayer.AddOrUpdateAsync(viewDataLayer, token);
        }
    }
}
