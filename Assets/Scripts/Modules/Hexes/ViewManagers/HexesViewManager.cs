using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Core;
using Core.Data;
using Core.DataLayer;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Modules.Hexes.Creators;
using Modules.Hexes.DataLayer;
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

        public HexesViewManager(IDataContainer<HexesDataLayer> hexesDataLayer, IDataContainer<HexesViewDataLayer> hexesViewDataLayer, IHexesCreator hexesCreator)
        {
            _hexesDataLayer = hexesDataLayer;
            _hexesViewDataLayer = hexesViewDataLayer;
            _hexesCreator = hexesCreator;

            _hexesDataLayerSubscription = hexesDataLayer.SubscribeOnUpdate(ProcessHexesDataLayer, 0);
        }

        public void Dispose()
        {
            if (_hexesDataLayerSubscription.SubscribeResult == SubscribeResult.Success)
            {
                _hexesDataLayer.UnsubscribeOnUpdate(_hexesDataLayerSubscription.SubscriptionId);
            }
        }

        private async UniTask<HexesViewDataLayer> CreateViewsAsync(DataContext<HexesDataLayer> context, CancellationToken token)
        {
            var hexesDataLayer = context.New.DataLayer;
            var viewResult = await _hexesViewDataLayer.GetAsync(token);

            if (viewResult.Exist && viewResult.DataLayer.HexViews.Length == context.New.DataLayer.Hexes.Length)
            {
                return default;
            }

            var hexViewDataLayer = viewResult.DataLayer;
            if (viewResult.Exist && viewResult.DataLayer.HexViews.Length > hexesDataLayer.Hexes.Length)
            {
                return hexViewDataLayer;
            }

            var missingViewsCount = !viewResult.Exist ? hexesDataLayer.Hexes.Length : hexesDataLayer.Hexes.Length - viewResult.DataLayer.HexViews.Length;
            var spawnTasks = new List<UniTask<Box<HexView>>>(missingViewsCount);
            for (var i = 0; i < missingViewsCount; i++)
            {
                spawnTasks.Add(_hexesCreator.CreateHexAsync(token));
            }

            var views = await UniTask.WhenAll(spawnTasks);
            if (token.IsCancellationRequested)
            {
                return hexViewDataLayer;
            }

            hexViewDataLayer.HexViews = viewResult.Exist ? hexViewDataLayer.HexViews.AddRange(views) : views.ToImmutableArray();

            return hexViewDataLayer;
        }

        private HexesViewDataLayer DeleteViews(HexesViewDataLayer hexViewDataLayer, DataContext<HexesDataLayer> context)
        {
            var hexesDataLayer = context.New.DataLayer;
            var missingViewsCount = hexViewDataLayer.HexViews.Length - hexesDataLayer.Hexes.Length;

            if (hexesDataLayer.Hexes.Length >= hexViewDataLayer.HexViews.Length)
            {
                return hexViewDataLayer;
            }

            var views = hexViewDataLayer.HexViews;
            for (var i = 1; i <= missingViewsCount; i++)
            {
                views[^i].Dispose();
            }

            hexViewDataLayer.HexViews = views.RemoveRange(views.Length - missingViewsCount, missingViewsCount);
            return hexViewDataLayer;
        }

        private async UniTask ProcessHexesDataLayer(DataContext<HexesDataLayer> context, CancellationToken token)
        {
            Debug.Log($"[skh] process. Old: {context.Old.Exist}, New: {context.New.Exist}");
            var viewDataLayer = await CreateViewsAsync(context, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            viewDataLayer = DeleteViews(viewDataLayer, context);
            await _hexesViewDataLayer.AddOrUpdateAsync(viewDataLayer, token);
        }
    }
}
