using System;
using System.Threading;
using Core.Data;
using Core.DataLayer;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Modules.Hexes.DataLayer;
using UnityEngine;

namespace Modules.Hexes.ViewManagers
{
    [UsedImplicitly]
    internal class HexesViewManager : IDisposable
    {
        private readonly IDataContainer<HexesDataLayer> _hexesDataLayer;
        private readonly DataSubscribeResult _hexesDataLayerSubscription;
        private IDataContainer<HexesViewDataLayer> _hexesViewDataLayer;

        public HexesViewManager(IDataContainer<HexesDataLayer> hexesDataLayer, IDataContainer<HexesViewDataLayer> hexesViewDataLayer)
        {
            _hexesDataLayer = hexesDataLayer;
            _hexesViewDataLayer = hexesViewDataLayer;

            _hexesDataLayerSubscription = hexesDataLayer.SubscribeOnUpdate(ProcessHexesDataLayer, 0);
        }

        public void Dispose()
        {
            if (_hexesDataLayerSubscription.SubscribeResult == SubscribeResult.Success)
            {
                _hexesDataLayer.UnsubscribeOnUpdate(_hexesDataLayerSubscription.SubscriptionId);
            }
        }

        private async UniTask ProcessHexesDataLayer(HexesDataLayer dataLayer, CancellationToken token)
        {
            Debug.Log("[skh] process");
        }
    }
}
