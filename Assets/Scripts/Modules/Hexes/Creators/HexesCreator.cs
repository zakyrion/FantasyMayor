using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Modules.Addressable;
using UnityEngine;
using Zenject;

namespace Modules.Hexes.Creators
{
    [UsedImplicitly]
    internal class HexesCreator : IHexesCreator
    {
        private const string HEX_VIEW_PREFAB = "Hexes/HexView";

        private readonly IAddressable _addressable;
        private readonly GameObject _root;

        public HexesCreator(IAddressable addressable, [Inject(Id = Constants.HEXES_ROOT_ID)] GameObject root)
        {
            _addressable = addressable;
            _root = root;
        }

        public async UniTask<Box<HexView>> CreateHexAsync(CancellationToken cancellationToken)
        {
            var result = await _addressable.LoadAndInstanceAsync(HEX_VIEW_PREFAB, cancellationToken, _root.transform);
            if (result.Status == AddressableStatus.Success)
            {
                var go = result.Box.Value;
                var hexView = go.GetComponent<HexView>();
                if (hexView)
                {
                    return Box<HexView>.Wrap(hexView, _ => result.Box.Dispose());
                }
            }

            return Box<HexView>.Empty();
        }
    }
}
