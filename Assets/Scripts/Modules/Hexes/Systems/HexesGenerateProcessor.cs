using System;
using System.Threading;
using Core.EventDataBus;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Modules.Hexes.Operations;
using UnityEngine;

namespace Modules.Hexes.Systems
{
    [UsedImplicitly]
    public class HexesGenerateProcessor : IProcessor<HexesGenerateOperation>, IDisposable
    {
        private readonly IBus<HexesGenerateOperation> _busGenerate;
        private IBus<HexesCreateViewOperation> _busCreateView;


        public HexesGenerateProcessor(IBus<HexesGenerateOperation> busGenerate, IBus<HexesCreateViewOperation> busCreateView)
        {
            _busGenerate = busGenerate;
            _busCreateView = busCreateView;
            _busGenerate.Subscribe(this, 0);
        }

        public void Dispose()
        {
            _busGenerate.Unsubscribe(0);
        }


        public async UniTask ProcessAsync(HexesGenerateOperation data, CancellationToken cancellationToken)
        {
            Debug.Log("[skh] process");
        }
    }
}
